using System.Runtime.InteropServices;
using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Login;
using XYVR.Scaffold;

namespace XYVR.UI.Backend;

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
    private readonly AppLifecycle _appLifecycle;
    private readonly JsonSerializerSettings _serializer;
    private static readonly SemaphoreSlim Lock = new(1, 1);
    
    private bool _isRunningDataCollection;

    public DataCollectionBFF(AppLifecycle appLifecycle)
    {
        _appLifecycle = appLifecycle;
        _serializer = BFFUtils.NewSerializer();
    }

    public async Task StartDataCollection()
    {
        if (_isRunningDataCollection) return;
        
        await Lock.WaitAsync(TimeSpan.FromSeconds(1));
        try
        {
            _isRunningDataCollection = true;

            var repository = _appLifecycle.IndividualRepository;
            var connectors = _appLifecycle.ConnectorsMgt;
            var credentials = _appLifecycle.CredentialsMgt;
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
                await _appLifecycle.SendEventToReact(FrontEvents.EventForIndividualUpdated, FrontIndividual.FromCore(individual, _appLifecycle.LiveStatusMonitoring));
            }));
            await Scaffolding.SaveRepository(repository);
        }
        catch (Exception e)
        {
            XYVRLogging.WriteLine(this, e);
            throw;
        }
        finally
        {
            _isRunningDataCollection = false;
            Lock.Release();
        }
    }
    
    public async Task<string> GetConnectors()
    {
        var connectors = _appLifecycle.ConnectorsMgt.Connectors;
        
        var connectorF = (await Task.WhenAll(connectors
            .Select(async connector => FrontConnector.FromCore(connector, await _appLifecycle.CredentialsMgt.IsLoggedInWithoutRequest(connector)))
            .ToList())).ToList();
        
        return ToJSON(connectorF);
    }

    public async Task<string> CreateConnector(string connectorType)
    {
        var newConnector = _appLifecycle.ConnectorsMgt.CreateNewConnector(Enum.Parse<ConnectorType>(connectorType));
        await Scaffolding.SaveConnectors(_appLifecycle.ConnectorsMgt);
        
        return ToJSON(newConnector);
    }

    public async Task DeleteConnector(string guid)
    {
        _appLifecycle.ConnectorsMgt.DeleteConnector(guid);
        await Scaffolding.SaveConnectors(_appLifecycle.ConnectorsMgt);
    }

    public async Task<string> TryLogin(string guid, string login__sensitive, string password__sensitive, bool stayLoggedIn)
    {
        var connector = _appLifecycle.ConnectorsMgt.GetConnector(guid);
        var connectionResult = await _appLifecycle.CredentialsMgt.TryConnect(connector, new ConnectionAttempt
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
        var connector = _appLifecycle.ConnectorsMgt.GetConnector(guid);
        var connectionResult = await _appLifecycle.CredentialsMgt.TryLogout(connector);
        await Scaffolding.SaveCredentials(await _appLifecycle.CredentialsMgt.SerializeCredentials());
    
        return ToJSON(connectionResult);
    }

    public async Task<string> TryTwoFactor(string guid, bool isTwoFactorEmail, string twoFactorCode__sensitive, bool stayLoggedIn)
    {
        var connector = _appLifecycle.ConnectorsMgt.GetConnector(guid);
        var connectionResult = await _appLifecycle.CredentialsMgt.TryConnect(connector, new ConnectionAttempt
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
                await Scaffolding.SaveCredentials(await _appLifecycle.CredentialsMgt.SerializeCredentials());
            }
            
            var connector = _appLifecycle.ConnectorsMgt.GetConnector(connectionResult.guid);
            connector.account = connectionResult.account;
            _appLifecycle.ConnectorsMgt.UpdateConnector(connector);
            
            await Scaffolding.SaveConnectors(_appLifecycle.ConnectorsMgt);
        }
    }

    private string ToJSON(object result)
    {
        return JsonConvert.SerializeObject(result, Formatting.None, _serializer);
    }
}