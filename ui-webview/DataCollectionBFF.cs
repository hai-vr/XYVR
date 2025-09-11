using System.Runtime.InteropServices;
using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Login;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface IDataCollectionBFF
{
    Task<string> GetConnectors();
    Task<string> CreateConnector(string connectorType);
    Task DeleteConnector(string guid);
    Task<string> TryLogin(string guid, string login__sensitive, string password__sensitive, bool stayLoggedIn);
    Task<string> TryTwoFactor(string guid, bool isTwoFactorEmail, string twoFactorCode__sensitive, bool stayLoggedIn);
    Task<string> TryLogout(string guid);
    Task StartDataCollection();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class DataCollectionBFF : IDataCollectionBFF
{
    private readonly MainWindow _mainWindow;
    private readonly JsonSerializerSettings _serializer;
    private static readonly SemaphoreSlim Lock = new(1, 1);
    
    private bool _isRunningDataCollection;

    public DataCollectionBFF(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _serializer = BFFUtils.NewSerializer();
    }

    public async Task StartDataCollection()
    {
        if (_isRunningDataCollection) return;
        
        await Lock.WaitAsync(TimeSpan.FromSeconds(1));
        try
        {
            _isRunningDataCollection = true;

            var repository = _mainWindow.IndividualRepository;
            var connectors = _mainWindow.ConnectorsMgt;
            var credentials = _mainWindow.CredentialsMgt;
            var storage = new ResponseCollectionStorage();

            var dataCollection = new CompoundDataCollection(repository, (await Task.WhenAll(connectors.Connectors
                    .Where(connector => connector.refreshMode != RefreshMode.ManualUpdatesOnly)
                    .Select(async connector => await credentials.GetConnectedDataCollectionOrNull(connector, repository, storage))
                    .ToList()))
                .Where(collection => collection != null)
                .Cast<IDataCollection>()
                .ToList()) as IDataCollection;

            await dataCollection.IncrementalUpdateRepository(new UIProgressJobHandler(repository, async individual =>
            {
                await _mainWindow.SendEventToReact(FrontEvents.EventForIndividualUpdated, FrontIndividual.FromCore(individual, _mainWindow.LiveStatusMonitoring));
            }));
            await Scaffolding.SaveRepository(repository);
        }
        finally
        {
            _isRunningDataCollection = false;
            Lock.Release();
        }
    }
    
    public async Task<string> GetConnectors()
    {
        var connectors = _mainWindow.ConnectorsMgt.Connectors;
        
        var connectorF = (await Task.WhenAll(connectors
            .Select(async connector => FrontConnector.FromCore(connector, await _mainWindow.CredentialsMgt.IsLoggedInWithoutRequest(connector)))
            .ToList())).ToList();
        
        return ToJSON(connectorF);
    }

    public async Task<string> CreateConnector(string connectorType)
    {
        var newConnector = _mainWindow.ConnectorsMgt.CreateNewConnector(Enum.Parse<ConnectorType>(connectorType));
        await Scaffolding.SaveConnectors(_mainWindow.ConnectorsMgt);
        
        return ToJSON(newConnector);
    }

    public async Task DeleteConnector(string guid)
    {
        _mainWindow.ConnectorsMgt.DeleteConnector(guid);
        await Scaffolding.SaveConnectors(_mainWindow.ConnectorsMgt);
    }

    public async Task<string> TryLogin(string guid, string login__sensitive, string password__sensitive, bool stayLoggedIn)
    {
        var connector = _mainWindow.ConnectorsMgt.GetConnector(guid);
        var connectionResult = await _mainWindow.CredentialsMgt.TryConnect(connector, new ConnectionAttempt
        {
            connector = connector,
            login__sensitive = login__sensitive,
            password__sensitive = password__sensitive,
            stayLoggedIn = stayLoggedIn
        });
        await ContinueLogin(connectionResult, stayLoggedIn);
    
        return ToJSON(connectionResult);
    }

    public async Task<string> TryLogout(string guid)
    {
        var connector = _mainWindow.ConnectorsMgt.GetConnector(guid);
        var connectionResult = await _mainWindow.CredentialsMgt.TryLogout(connector);
        await Scaffolding.SaveCredentials(await _mainWindow.CredentialsMgt.SerializeCredentials());
    
        return ToJSON(connectionResult);
    }

    public async Task<string> TryTwoFactor(string guid, bool isTwoFactorEmail, string twoFactorCode__sensitive, bool stayLoggedIn)
    {
        var connector = _mainWindow.ConnectorsMgt.GetConnector(guid);
        var connectionResult = await _mainWindow.CredentialsMgt.TryConnect(connector, new ConnectionAttempt
        {
            connector = connector,
            twoFactorCode__sensitive = twoFactorCode__sensitive,
            stayLoggedIn = stayLoggedIn,
            isTwoFactorEmail = isTwoFactorEmail
        });
        await ContinueLogin(connectionResult, stayLoggedIn);
    
        return ToJSON(connectionResult);
    }

    private async Task ContinueLogin(ConnectionAttemptResult connectionResult, bool stayLoggedIn)
    {
        if (connectionResult.type == ConnectionAttemptResultType.Success)
        {
            if (stayLoggedIn)
            {
                await Scaffolding.SaveCredentials(await _mainWindow.CredentialsMgt.SerializeCredentials());
            }
            
            var connector = _mainWindow.ConnectorsMgt.GetConnector(connectionResult.guid);
            connector.account = connectionResult.account;
            _mainWindow.ConnectorsMgt.UpdateConnector(connector);
            
            await Scaffolding.SaveConnectors(_mainWindow.ConnectorsMgt);
        }
    }

    private string ToJSON(object result)
    {
        return JsonConvert.SerializeObject(result, Formatting.None, _serializer);
    }
}