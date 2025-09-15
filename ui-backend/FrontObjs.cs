using XYVR.Core;

namespace XYVR.UI.Backend;

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
                var sessionGuid = sessionState?.mainSession?.sessionGuid;
                var session = sessionGuid != null ? live.GetSessionByGuid(sessionGuid) : null;
                return FrontAccount.ToFrontAccount(account, sessionState, session, live);
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
    public List<FrontLiveSession> multiSessions;

    public static FrontAccount ToFrontAccount(ImmutableAccount account, ImmutableLiveUserUpdate? liveSessionState, ImmutableLiveSession? liveSession, LiveStatusMonitoring live)
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
            mainSession = liveSessionState?.mainSession != null ? FrontLiveUserSessionState.FromCore(liveSessionState.mainSession, liveSession) : null,
            multiSessions = liveSessionState != null ? liveSessionState.multiSessionGuids
                .Select(live.GetSessionByGuid)
                .Where(aSession => aSession != null)
                .Cast<ImmutableLiveSession>()
                .Select(FrontLiveSession.FromCore)
                .ToList() : [],
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
    public List<FrontLiveSession> multiSessions;

    public string callerInAppIdentifier;
    
    public static FrontLiveUserUpdate FromCore(ImmutableLiveUserUpdate liveUserUpdate, LiveStatusMonitoring live)
    {
        var sessionGuid = liveUserUpdate.mainSession?.sessionGuid;
        var session = sessionGuid != null ? live.GetSessionByGuid(sessionGuid) : null;
        return new FrontLiveUserUpdate
        {
            namedApp = liveUserUpdate.namedApp,
            trigger = liveUserUpdate.trigger,
            qualifiedAppName = liveUserUpdate.qualifiedAppName,
            inAppIdentifier = liveUserUpdate.inAppIdentifier,
            onlineStatus = liveUserUpdate.onlineStatus,
            mainSession = liveUserUpdate.mainSession != null ? FrontLiveUserSessionState.FromCore(liveUserUpdate.mainSession, session) : null,
            multiSessions = liveUserUpdate.multiSessionGuids
                .Select(live.GetSessionByGuid)
                .Where(liveSession => liveSession != null)                
                .Cast<ImmutableLiveSession>()
                .Select(FrontLiveSession.FromCore)
                .ToList(),
            customStatus = liveUserUpdate.customStatus,
            callerInAppIdentifier = liveUserUpdate.callerInAppIdentifier
        };
    }
}

internal class FrontLiveUserSessionState
{
    public LiveUserSessionKnowledge knowledge;
    public string? sessionGuid;
    public FrontLiveSession? liveSession;
    
    public static FrontLiveUserSessionState FromCore(ImmutableLiveUserSessionState liveUserSessionState, ImmutableLiveSession? liveSession)
    {
        return new FrontLiveUserSessionState
        {
            knowledge = liveUserSessionState.knowledge,
            sessionGuid = liveUserSessionState.sessionGuid,
            liveSession = liveSession != null ? FrontLiveSession.FromCore(liveSession) : null
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
    public int? virtualSpaceDefaultCapacity;
    public int? sessionCapacity;
    public int? currentAttendance;
    public string? thumbnailUrl;

    public static FrontLiveSession FromCore(ImmutableLiveSession liveSession)
    {
        return new FrontLiveSession
        {
            guid = liveSession.guid,
            namedApp = liveSession.namedApp,
            qualifiedAppName = liveSession.qualifiedAppName,
            inAppSessionIdentifier = liveSession.inAppSessionIdentifier,
            inAppSessionName = liveSession.inAppSessionName,
            inAppVirtualSpaceName = liveSession.inAppVirtualSpaceName,
            inAppHost = liveSession.inAppHost != null ? FrontLiveSessionHost.FromCore(liveSession.inAppHost) : null,
            participants = liveSession.participants.Select(FrontParticipant.FromCore).ToList(),
            virtualSpaceDefaultCapacity = liveSession.virtualSpaceDefaultCapacity,
            sessionCapacity = liveSession.sessionCapacity,
            currentAttendance = liveSession.currentAttendance,
            thumbnailUrl = liveSession.thumbnailUrl,
        };
    }
}

internal class FrontParticipant
{
    public bool isKnown;
    public FrontImmutableKnownParticipantAccount? knownAccount;
    public FrontImmutableUnknownParticipantAccount? unknownAccount;

    public bool isHost;
    
    public static FrontParticipant FromCore(ImmutableParticipant participant)
    {
        return new FrontParticipant
        {
            isKnown = participant.isKnown,
            knownAccount = participant.knownAccount != null ? FrontImmutableKnownParticipantAccount.FromCore(participant.knownAccount) : null,
            unknownAccount = participant.unknownAccount != null ? FrontImmutableUnknownParticipantAccount.FromCore(participant.unknownAccount) : null,
            isHost = participant.isHost
        };
    }
}

public record FrontImmutableKnownParticipantAccount
{
    public string inAppIdentifier { get; init; }

    public static FrontImmutableKnownParticipantAccount FromCore(ImmutableKnownParticipantAccount participantKnownAccount)
    {
        return new FrontImmutableKnownParticipantAccount
        {
            inAppIdentifier = participantKnownAccount.inAppIdentifier
        };
    }
}

public record FrontImmutableUnknownParticipantAccount
{
    public string? inAppIdentifier { get; init; }
    public string? inAppDisplayName { get; init; }

    public static FrontImmutableUnknownParticipantAccount FromCore(ImmutableUnknownParticipantAccount participantUnknownAccount)
    {
        return new FrontImmutableUnknownParticipantAccount
        {
            inAppIdentifier = participantUnknownAccount.inAppIdentifier,
            inAppDisplayName = participantUnknownAccount.inAppDisplayName,
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