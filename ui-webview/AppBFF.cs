using System.Runtime.InteropServices;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.Core;

namespace XYVR.UI.WebviewUI;

[ComVisible(true)]
public interface IAppBFF
{
    string GetAppVersion();
    string GetAllExposedIndividualsOrderedByContact();
    void ShowMessage(string message);
    string GetCurrentTime();
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
        var responseObj = _mainWindow.IndividualRepository.Individuals
            .Where(individual => individual.isExposed)
            .OrderByDescending(individual => individual.isAnyContact)
            .Select(ToFront)
            .ToList();
        
        return JsonConvert.SerializeObject(responseObj, Formatting.None, _serializer);
    }

    internal static FrontIndividual ToFront(Individual individual)
    {
        return new FrontIndividual
        {
            guid = individual.guid,
            accounts = individual.accounts
                .Select(account => new FrontAccount
                {
                    guid = account.guid,
                    namedApp = account.namedApp,
                    qualifiedAppName = account.qualifiedAppName,
                    inAppIdentifier = account.inAppIdentifier,
                    inAppDisplayName = account.inAppDisplayName,
                    specifics = account.specifics,
                    callers = account.callers,
                    isTechnical = account.isTechnical,
                    isAnyCallerContact = account.callers.Any(caller => caller.isContact),
                    isAnyCallerNote = account.callers.Any(caller => caller.note.status == NoteState.Exists),
                    allDisplayNames = account.allDisplayNames,
                    isPendingUpdate = account.isPendingUpdate
                }).ToList(),
            displayName = individual.displayName,
            note = individual.note,
            isAnyContact = individual.isAnyContact,
            isExposed = individual.isExposed,
            customName = individual.customName
        };
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