using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface IDataCollectionBFF
{
    void DataCollectionTriggerTest();
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

    public void DataCollectionTriggerTest()
    {
        MessageBox.Show("data collection trigger", "From WebView", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}