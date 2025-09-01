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
            .Select(ToFront)
            .ToList();
        
        return JsonConvert.SerializeObject(responseObj, Formatting.None, _serializer);
    }

    private FrontIndividual ToFront(Individual individual)
    {
        return new FrontIndividual
        {
            guid = individual.guid,
            accounts = individual.accounts
                .Select(account => new FrontAccount
                {
                    namedApp = account.namedApp,
                    qualifiedAppName = account.qualifiedAppName,
                    inAppIdentifier = account.inAppIdentifier,
                    inAppDisplayName = account.inAppDisplayName,
                    callers = account.callers,
                    isTechnical = account.isTechnical,
                    isAnyCallerContact = account.callers.Any(caller => caller.isContact),
                    isAnyCallerNote = account.callers.Any(caller => caller.note.status == NoteState.Exists)
                }).ToList(),
            displayName = individual.displayName,
            note = individual.note,
            isAnyContact = individual.isAnyContact,
            isExposed = individual.isExposed
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

internal class FrontIndividual
{
    public string guid;
    public List<FrontAccount> accounts = new();
    public string displayName;
    public Note note = new();
    public bool isAnyContact;
    public bool isExposed;
}

internal class FrontAccount
{
    public NamedApp namedApp;
    public string qualifiedAppName;
    public string inAppIdentifier;
    public string inAppDisplayName;
    public List<CallerAccount> callers;
    public bool isTechnical;
    public bool isAnyCallerContact;
    public bool isAnyCallerNote;
}
