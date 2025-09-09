using XYVR.Core;

namespace XYVR.UI.WebviewUI;

internal class FrontIndividual
{
    public string guid;
    public List<FrontAccount> accounts = new();
    public string displayName;
    public string? note;
    public bool isAnyContact;
    public bool isExposed;
    public string? customName;

    public OnlineStatus? onlineStatus;
    public string? customStatus;
    
    public static string? ToFrontNote(Note note)
    {
        return note.status == NoteState.Exists ? note.text : null;
    }

    internal static FrontIndividual FromCore(Individual individual, LiveStatusMonitoring live)
    {
        var accounts = individual.accounts
            .Select(account =>
            {
                var sessionState = live.GetLiveSessionStateOrNull(account.namedApp, account.inAppIdentifier);
                return FrontAccount.ToFrontAccount(account, sessionState);
            }).ToList();

        var nonNullStatus = accounts.Select(account => account.onlineStatus).Where(status => status != null).ToList();
        return new FrontIndividual
        {
            guid = individual.guid,
            accounts = accounts,
            displayName = individual.displayName,
            note = ToFrontNote(individual.note),
            isAnyContact = individual.isAnyContact,
            isExposed = individual.isExposed,
            customName = individual.customName,
            
            onlineStatus = nonNullStatus.Count > 0 ? nonNullStatus.FirstOrDefault(it => it != OnlineStatus.Offline, OnlineStatus.Offline) : null,
        };
    }
}

internal class FrontAccount
{
    public string guid;
    public NamedApp namedApp;
    public string qualifiedAppName;
    public string inAppIdentifier;
    public string inAppDisplayName;
    public List<FrontCallerAccount> callers;
    public bool isTechnical;
    public object? specifics;
    public List<string> allDisplayNames;
    public bool isPendingUpdate;
    
    public bool isAnyCallerContact;
    public bool isAnyCallerNote;
    
    public OnlineStatus? onlineStatus;
    public string? customStatus;

    public static FrontAccount ToFrontAccount(Account account, LiveUpdate? sessionState)
    {
        return new FrontAccount
        {
            guid = account.guid,
            namedApp = account.namedApp,
            qualifiedAppName = account.qualifiedAppName,
            inAppIdentifier = account.inAppIdentifier,
            inAppDisplayName = account.inAppDisplayName,
            specifics = account.specifics,
            callers = account.callers.Select(FrontCallerAccount.FromCore).ToList(),
            isTechnical = account.isTechnical,
            isAnyCallerContact = account.callers.Any(caller => caller.isContact),
            isAnyCallerNote = account.callers.Any(caller => caller.note.status == NoteState.Exists),
            allDisplayNames = account.allDisplayNames,
            isPendingUpdate = account.isPendingUpdate,

            onlineStatus = sessionState?.onlineStatus,
            customStatus = sessionState?.customStatus
        };
    }
}

public class FrontCallerAccount
{
    public bool isAnonymous;
    public string? inAppIdentifier; // Can only be null if it's an anonymous caller.
    
    public string? note;
    public bool isContact;

    public static FrontCallerAccount FromCore(CallerAccount callerAccount)
    {
        return new FrontCallerAccount
        {
            inAppIdentifier = callerAccount.inAppIdentifier,
            isAnonymous = callerAccount.isAnonymous,
            isContact = callerAccount.isContact,
            note = FrontIndividual.ToFrontNote(callerAccount.note),
        };
    }
}

public class FrontConnector
{
    public string guid;
    public string displayName;
    public ConnectorType type;
    public RefreshMode refreshMode;
    public LiveMode liveMode;
    public FrontConnectorAccount? account;

    public bool isLoggedIn;

    public static FrontConnector FromCore(Connector connector, bool isLoggedIn)
    {
        return new FrontConnector
        {
            guid = connector.guid,
            displayName = connector.displayName,
            type = connector.type,
            refreshMode = connector.refreshMode,
            liveMode = connector.liveMode,
            account = connector.account != null ? FrontConnectorAccount.FromCore(connector.account) : null,

            isLoggedIn = isLoggedIn,
        };
    }
}

public class FrontConnectorAccount
{
    public NamedApp namedApp;
    public string qualifiedAppName;

    public string inAppIdentifier;
    public string inAppDisplayName;

    public static FrontConnectorAccount FromCore(ConnectorAccount connectorAccount)
    {
        return new FrontConnectorAccount
        {
            namedApp = connectorAccount.namedApp,
            qualifiedAppName = connectorAccount.qualifiedAppName,
            inAppIdentifier = connectorAccount.inAppIdentifier,
            inAppDisplayName = connectorAccount.inAppDisplayName,
        };
    }
}