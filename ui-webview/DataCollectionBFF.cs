using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Data.Collection;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface IDataCollectionBFF
{
    void DataCollectionTriggerTest();
    string GetConnectors();
    Task<string> CreateConnector(string connectorType);
    Task DeleteConnector(string guid);
    Task<string> TryLogin(string guid, string login__sensitive, string password__sensitive, bool stayLoggedIn);
    Task<string> TryTwoFactor(string guid, string twoFactorCode__sensitive, bool stayLoggedIn);
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class DataCollectionBFF : IDataCollectionBFF
{
    private readonly MainWindow _mainWindow;
    private readonly JsonSerializerSettings _serializer;

    public DataCollectionBFF(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _serializer = BFFUtils.NewSerializer();
    }

    public string GetConnectors()
    {
        return ToJSON(_mainWindow.ConnectorsMgt.Connectors);
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
    
        return ToJSON(connectionResult);
    }

    public async Task<string> TryTwoFactor(string guid, string twoFactorCode__sensitive, bool stayLoggedIn)
    {
        var connector = _mainWindow.ConnectorsMgt.GetConnector(guid);
        var connectionResult = await _mainWindow.CredentialsMgt.TryConnect(connector, new ConnectionAttempt
        {
            connector = connector,
            twoFactorCode__sensitive = twoFactorCode__sensitive,
            stayLoggedIn = stayLoggedIn
        });
    
        return ToJSON(connectionResult);
    }

    public void DataCollectionTriggerTest()
    {
        MessageBox.Show("data collection trigger", "From WebView", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private string ToJSON(object result)
    {
        return JsonConvert.SerializeObject(result, Formatting.None, _serializer);
    }
}