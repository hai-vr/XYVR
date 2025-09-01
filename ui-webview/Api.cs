using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.Core;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface IAppApi
{
    string GetAppVersion();
    string GetAllExposedIndividualsOrderedByContact();
    void ShowMessage(string message);
    string GetCurrentTime();
    void CloseApp();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class AppApi : IAppApi
{
    private readonly MainWindow _mainWindow;
    private readonly JsonSerializerSettings _serializer;

    public AppApi(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _serializer = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };
    }

    public string GetAppVersion()
    {
        return VERSION.version;
    }

    public string GetAllExposedIndividualsOrderedByContact()
    {
        var responseObj = _mainWindow.IndividualRepository.Individuals
            .Where(individual => individual.isExposed)
            .OrderByDescending(individual => individual.isAnyContact)
            .ToList();
        
        return JsonConvert.SerializeObject(responseObj, Formatting.None, _serializer);
    }

    public void ShowMessage(string message)
    {
        MessageBox.Show(message, "From WebView", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public string GetCurrentTime()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public void CloseApp()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _mainWindow.Close();
        });
    }
}
