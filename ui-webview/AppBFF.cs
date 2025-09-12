using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface IAppBFF
{
    string GetAppVersion();
    string GetAllExposedIndividualsOrderedByContact();
    Task FusionIndividuals(string toDesolidarize, string toDestroy);
    Task DesolidarizeIndividuals(string toDesolidarize);
    void CloseApp();
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class AppBFF : IAppBFF
{
    private readonly MainWindow _mainWindow;
    private readonly JsonSerializerSettings _serializer;

    public AppBFF(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _serializer = BFFUtils.NewSerializer();
    }

    public string GetAppVersion()
    {
        return VERSION.version;
    }

    public string GetAllExposedIndividualsOrderedByContact()
    {
        var live = _mainWindow.LiveStatusMonitoring;
        
        var responseObj = _mainWindow.IndividualRepository.Individuals
            .Where(individual => individual.isExposed)
            .OrderByDescending(individual => individual.isAnyContact)
            .Select(individual => FrontIndividual.FromCore(individual, live))
            .ToList();
        
        return JsonConvert.SerializeObject(responseObj, Formatting.None, _serializer);
    }

    public async Task FusionIndividuals(string toDesolidarize, string toDestroy)
    {
        Console.WriteLine($"Fusion individuals was called: {toDesolidarize}, {toDestroy}");
        if (toDesolidarize == toDestroy) throw new ArgumentException("Cannot fusion an Individual with itself");
        
        var to = _mainWindow.IndividualRepository.GetByGuid(toDesolidarize);
        var beingDestroyed = _mainWindow.IndividualRepository.GetByGuid(toDestroy);
        _mainWindow.IndividualRepository.FusionIndividuals(to, beingDestroyed);
        await Scaffolding.SaveRepository(_mainWindow.IndividualRepository);
    }

    public async Task DesolidarizeIndividuals(string toDesolidarize)
    {
        Console.WriteLine($"Desolidarize was called: {toDesolidarize}");
        
        var individual = _mainWindow.IndividualRepository.GetByGuid(toDesolidarize);
        if (individual.accounts.Length <= 1) return;
        
        _mainWindow.IndividualRepository.DesolidarizeIndividualAccounts(individual);
        await Scaffolding.SaveRepository(_mainWindow.IndividualRepository);
    }

    public void CloseApp()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _mainWindow.Close();
        });
    }
}