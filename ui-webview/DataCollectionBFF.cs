using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface IDataCollectionBFF
{
    void DataCollectionTriggerTest();
    string GetConnectors();
    Task<string> CreateConnector();
    Task DeleteConnector(string guid);
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

    public async Task<string> CreateConnector()
    {
        var newConnector = _mainWindow.ConnectorsMgt.CreateNewConnector();
        await Scaffolding.SaveConnectors(_mainWindow.ConnectorsMgt);
        
        return ToJSON(newConnector);
    }

    public async Task DeleteConnector(string guid)
    {
        _mainWindow.ConnectorsMgt.DeleteConnector(guid);
        await Scaffolding.SaveConnectors(_mainWindow.ConnectorsMgt);
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