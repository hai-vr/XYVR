using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface IAppApi
{
    string GetAppVersion();
    string GetAllExposedIndividuals();
    void ShowMessage(string message);
    string GetCurrentTime();
    void CloseApp();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class AppApi : IAppApi
{
    private readonly MainWindow _mainWindow;

    public AppApi(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public string GetAppVersion()
    {
        return "1.0.0";
    }

    public string GetAllExposedIndividuals()
    {
        var responseObj = _mainWindow.IndividualRepository.Individuals
            .Where(individual => individual.isExposed)
            .ToList();
        
        return JsonConvert.SerializeObject(responseObj);
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
