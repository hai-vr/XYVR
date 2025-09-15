using System.Runtime.InteropServices;
using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.Backend;

[ComVisible(true)]
public interface IAppBFF
{
    string GetAppVersion();
    string GetAllExposedIndividualsOrderedByContact();
    Task FusionIndividuals(string toDesolidarize, string toDestroy);
    Task DesolidarizeIndividuals(string toDesolidarize);
}

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
public class AppBFF : IAppBFF
{
    private readonly AppLifecycle _appLifecycle;
    private readonly JsonSerializerSettings _serializer;

    public AppBFF(AppLifecycle appLifecycle)
    {
        _appLifecycle = appLifecycle;
        _serializer = BFFUtils.NewSerializer();
    }

    public string GetAppVersion()
    {
        return VERSION.version;
    }

    public string GetAllExposedIndividualsOrderedByContact()
    {
        var live = _appLifecycle.LiveStatusMonitoring;
        
        var responseObj = _appLifecycle.IndividualRepository.Individuals
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
        
        var to = _appLifecycle.IndividualRepository.GetByGuid(toDesolidarize);
        var beingDestroyed = _appLifecycle.IndividualRepository.GetByGuid(toDestroy);
        _appLifecycle.IndividualRepository.FusionIndividuals(to, beingDestroyed);
        await Scaffolding.SaveRepository(_appLifecycle.IndividualRepository);
    }

    public async Task DesolidarizeIndividuals(string toDesolidarize)
    {
        Console.WriteLine($"Desolidarize was called: {toDesolidarize}");
        
        var individual = _appLifecycle.IndividualRepository.GetByGuid(toDesolidarize);
        if (individual.accounts.Length <= 1) return;
        
        _appLifecycle.IndividualRepository.DesolidarizeIndividualAccounts(individual);
        await Scaffolding.SaveRepository(_appLifecycle.IndividualRepository);
    }
}