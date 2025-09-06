using XYVR.Core;

namespace XYVR.UI.WebviewUI;

internal class FrontIndividual
{
    public string guid;
    public List<FrontAccount> accounts = new();
    public string displayName;
    public Note note = new();
    public bool isAnyContact;
    public bool isExposed;
    public string? customName;

    public OnlineStatus? onlineStatus;
}

internal class FrontAccount
{
    public string guid;
    public NamedApp namedApp;
    public string qualifiedAppName;
    public string inAppIdentifier;
    public string inAppDisplayName;
    public List<CallerAccount> callers;
    public bool isTechnical;
    public bool isAnyCallerContact;
    public bool isAnyCallerNote;
    public object? specifics;
    public List<string> allDisplayNames;
    public bool isPendingUpdate;
    
    public OnlineStatus? onlineStatus;
}

public class FrontConnector(Connector connector, bool isLoggedIn)
{
    public string guid = connector.guid;
    public string displayName = connector.displayName;
    public ConnectorType type = connector.type;
    public RefreshMode refreshMode = connector.refreshMode;
    public LiveMode liveMode = connector.liveMode;
    public ConnectorAccount? account = connector.account;

    public bool isLoggedIn = isLoggedIn;
}