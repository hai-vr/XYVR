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
    
    public static string? ToFrontNote(ImmutableNote note)
    {
        return note.status == NoteState.Exists ? note.text : null;
    }

    internal static FrontIndividual FromCore(ImmutableIndividual individual, LiveStatusMonitoring live)
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
    public FrontLiveUserSessionState? mainSession;

    public static FrontAccount ToFrontAccount(ImmutableAccount account, ImmutableLiveUserUpdate? liveSessionState)
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
            allDisplayNames = account.allDisplayNames.ToList(),
            isPendingUpdate = account.isPendingUpdate,

            onlineStatus = liveSessionState?.onlineStatus,
            customStatus = liveSessionState?.customStatus,
            mainSession = liveSessionState?.mainSession != null ? FrontLiveUserSessionState.FromCore(liveSessionState.mainSession) : null,
        };
    }
}

internal class FrontCallerAccount
{
    public bool isAnonymous;
    public string? inAppIdentifier; // Can only be null if it's an anonymous caller.
    
    public string? note;
    public bool isContact;

    public static FrontCallerAccount FromCore(ImmutableCallerAccount callerAccount)
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

internal class FrontConnector
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

internal class FrontConnectorAccount
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

internal class FrontLiveUserUpdate
{
    public NamedApp namedApp;
    public string trigger;
    public string qualifiedAppName;
    public string inAppIdentifier;

    public OnlineStatus? onlineStatus;
    public string? customStatus;
    public FrontLiveUserSessionState? mainSession;

    public string callerInAppIdentifier;
    
    public static FrontLiveUserUpdate FromCore(ImmutableLiveUserUpdate liveUserUpdate)
    {
        return new FrontLiveUserUpdate
        {
            namedApp = liveUserUpdate.namedApp,
            trigger = liveUserUpdate.trigger,
            qualifiedAppName = liveUserUpdate.qualifiedAppName,
            inAppIdentifier = liveUserUpdate.inAppIdentifier,
            onlineStatus = liveUserUpdate.onlineStatus,
            mainSession = liveUserUpdate.mainSession != null ? FrontLiveUserSessionState.FromCore(liveUserUpdate.mainSession) : null,
            customStatus = liveUserUpdate.customStatus,
            callerInAppIdentifier = liveUserUpdate.callerInAppIdentifier
        };
    }
}

internal class FrontLiveUserSessionState
{
    public FrontLiveUserKnownSession? knownSession;
    public LiveUserSessionKnowledge knowledge;
    
    public static FrontLiveUserSessionState FromCore(ImmutableLiveUserSessionState liveUserSessionState)
    {
        return new FrontLiveUserSessionState
        {
            knownSession = liveUserSessionState.knownSession != null ? FrontLiveUserKnownSession.FromCore(liveUserSessionState.knownSession) : null,
            knowledge = liveUserSessionState.knowledge
        };
    }
}

internal class FrontLiveUserKnownSession
{
    public string inAppSessionIdentifier;
    
    public string? inAppSessionName;
    public string? inAppVirtualSpaceName;
    
    public FrontLiveSessionHost? inAppHost;
    
    public static FrontLiveUserKnownSession FromCore(ImmutableLiveUserKnownSession liveUserKnownSession)
    {
        return new FrontLiveUserKnownSession
        {
            inAppSessionIdentifier = liveUserKnownSession.inAppSessionIdentifier,
            inAppSessionName = liveUserKnownSession.inAppSessionName,
            inAppVirtualSpaceName = liveUserKnownSession.inAppVirtualSpaceName,
            inAppHost = liveUserKnownSession.inAppHost != null ? FrontLiveSessionHost.FromCore(liveUserKnownSession.inAppHost) : null
        };
    }
}

internal class FrontLiveSession
{
    public string guid;

    public NamedApp namedApp;
    public string qualifiedAppName;
    
    public string inAppSessionIdentifier;
    
    public string? inAppSessionName;
    public string? inAppVirtualSpaceName;
    
    public FrontLiveSessionHost? inAppHost;

    public List<FrontParticipant> participants = new();
    
    public static FrontLiveSession FromCore(LiveSession liveSession, LiveStatusMonitoring monitoring)
    {
        return new FrontLiveSession
        {
            guid = liveSession.guid,
            namedApp = liveSession.namedApp,
            qualifiedAppName = liveSession.qualifiedAppName,
            inAppSessionIdentifier = liveSession.inAppSessionIdentifier,
            inAppSessionName = liveSession.inAppSessionName,
            inAppVirtualSpaceName = liveSession.inAppVirtualSpaceName,
            inAppHost = liveSession.inAppHost != null ? FrontLiveSessionHost.FromCore(liveSession.inAppHost.ToImmutable()) : null,
            participants = liveSession.participants.Select(participant =>
            {
                var sessionState = participant.isKnown
                    ? monitoring.GetLiveSessionStateOrNull(liveSession.namedApp, participant.knownAccount!.inAppIdentifier)
                    : null;

                return FrontParticipant.FromCore(participant, sessionState);
            }).ToList()
        };
    }
}

internal class FrontParticipant
{
    public bool isKnown;
    public FrontAccount? knownAccount;
    public FrontUnknownAccount? unknownAccount;

    public bool isHost;
    
    public static FrontParticipant FromCore(Participant participant, ImmutableLiveUserUpdate? liveSessionState)
    {
        return new FrontParticipant
        {
            isKnown = participant.isKnown,
            knownAccount = participant.knownAccount != null ? FrontAccount.ToFrontAccount(participant.knownAccount, liveSessionState) : null,
            unknownAccount = participant.unknownAccount != null ? FrontUnknownAccount.FromCore(participant.unknownAccount) : null,
            isHost = participant.isHost
        };
    }
}

internal class FrontUnknownAccount
{
    public string? inAppIdentifier;
    public string? inAppDisplayName;
    
    public static FrontUnknownAccount FromCore(UnknownAccount unknownAccount)
    {
        return new FrontUnknownAccount
        {
            inAppIdentifier = unknownAccount.inAppIdentifier,
            inAppDisplayName = unknownAccount.inAppDisplayName
        };
    }
}

internal class FrontLiveSessionHost
{
    public string inAppHostIdentifier;
    public string? inAppHostDisplayName;
    
    public static FrontLiveSessionHost FromCore(ImmutableLiveSessionHost liveSessionHost)
    {
        return new FrontLiveSessionHost
        {
            inAppHostIdentifier = liveSessionHost.inAppHostIdentifier,
            inAppHostDisplayName = liveSessionHost.inAppHostDisplayName
        };
    }
}