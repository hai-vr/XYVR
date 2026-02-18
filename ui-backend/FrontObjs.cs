using System.Collections.Immutable;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.Backend;

[Serializable]
internal record FrontIndividual
{
    public required string guid { get; init; }
    public ImmutableArray<FrontAccount> accounts { get; init; } = ImmutableArray<FrontAccount>.Empty;
    public required string displayName { get; init; }
    public string? note { get; init; }
    public bool isAnyContact { get; init; }
    public bool isExposed { get; init; }
    public string? customName { get; init; }

    public OnlineStatus? onlineStatus { get; init; }
    public string? customStatus { get; init; }
    
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
            accounts = [..accounts],
            displayName = individual.displayName,
            note = ToFrontNote(individual.note),
            isAnyContact = individual.isAnyContact,
            isExposed = individual.isExposed,
            customName = individual.customName,
            
            onlineStatus = nonNullStatus.Count > 0 ? nonNullStatus.FirstOrDefault(it => it != OnlineStatus.Offline, OnlineStatus.Offline) : null,
        };
    }
}

[Serializable]
internal record FrontAccount
{
    public required string guid { get; init; }
    public NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }
    public required string inAppIdentifier { get; init; }
    public required string inAppDisplayName { get; init; }
    public required ImmutableArray<FrontCallerAccount> callers { get; init; } = ImmutableArray<FrontCallerAccount>.Empty;
    public bool isTechnical { get; init; }
    public object? specifics { get; init; }
    public required ImmutableArray<string> allDisplayNames { get; init; } = ImmutableArray<string>.Empty;
    public bool isPendingUpdate { get; init; }
    
    public bool isAnyCallerContact { get; init; }
    public bool isAnyCallerNote { get; init; }
    
    public OnlineStatus? onlineStatus { get; init; }
    public string? customStatus { get; init; }
    public FrontLiveUserSessionState? mainSession { get; init; }
    public required ImmutableArray<FrontLiveSession> multiSessions { get; init; } = ImmutableArray<FrontLiveSession>.Empty;

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
            callers = [..account.callers.Select(FrontCallerAccount.FromCore)],
            isTechnical = account.isTechnical,
            isAnyCallerContact = account.callers.Any(caller => caller.isContact),
            isAnyCallerNote = account.callers.Any(caller => caller.note.status == NoteState.Exists),
            allDisplayNames = [..account.allDisplayNames],
            isPendingUpdate = account.isPendingUpdate,

            onlineStatus = liveSessionState?.onlineStatus,
            customStatus = liveSessionState?.customStatus,
            mainSession = liveSessionState?.mainSession != null ? FrontLiveUserSessionState.FromCore(liveSessionState.mainSession, liveSession) : null,
            multiSessions = liveSessionState != null ? [
                ..liveSessionState.multiSessionGuids
                    .Select(live.GetSessionByGuid)
                    .Where(aSession => aSession != null)
                    .Cast<ImmutableLiveSession>()
                    .Select(FrontLiveSession.FromCore)
            ] : [],
        };
    }
}

[Serializable]
internal record FrontCallerAccount
{
    public bool isAnonymous { get; init; }
    public string? inAppIdentifier { get; init; } // Can only be null if it's an anonymous caller.
    
    public string? note { get; init; }
    public bool isContact { get; init; }

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

[Serializable]
internal record FrontConnector
{
    public required string guid { get; init; }
    public required string displayName { get; init; }
    public ConnectorType type { get; init; }
    public RefreshMode refreshMode { get; init; }
    public LiveMode liveMode { get; init; }
    public FrontConnectorAccount? account { get; init; }

    public bool isLoggedIn { get; init; }

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

[Serializable]
internal record FrontConnectorAccount
{
    public NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }

    public required string inAppIdentifier { get; init; }
    public required string inAppDisplayName { get; init; }

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

[Serializable]
internal record FrontLiveUserUpdate
{
    public NamedApp namedApp { get; init; }
    public required string trigger { get; init; }
    public required string qualifiedAppName { get; init; }
    public required string inAppIdentifier { get; init; }

    public OnlineStatus? onlineStatus { get; init; }
    public string? customStatus { get; init; }
    public FrontLiveUserSessionState? mainSession { get; init; }
    public required ImmutableArray<FrontLiveSession> multiSessions { get; init; }

    public required string callerInAppIdentifier { get; init; }
    
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
            multiSessions = [
                ..liveUserUpdate.multiSessionGuids
                    .Select(live.GetSessionByGuid)
                    .Where(liveSession => liveSession != null)                
                    .Cast<ImmutableLiveSession>()
                    .Select(FrontLiveSession.FromCore)
            ],
            customStatus = liveUserUpdate.customStatus,
            callerInAppIdentifier = liveUserUpdate.callerInAppIdentifier
        };
    }
}

[Serializable]
internal record FrontLiveUserSessionState
{
    public LiveUserSessionKnowledge knowledge { get; init; }
    public string? sessionGuid { get; init; }
    public FrontLiveSession? liveSession { get; init; }
    
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

[Serializable]
internal record FrontLiveSession
{
    public required string guid { get; init; }

    public NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }
    
    public required string inAppSessionIdentifier { get; init; }
    
    public string? inAppSessionName { get; init; }
    public string? inAppVirtualSpaceName { get; init; }
    
    public FrontLiveSessionHost? inAppHost { get; init; }

    public ImmutableArray<FrontParticipant> participants  { get; init; } = ImmutableArray<FrontParticipant>.Empty;
    public int? virtualSpaceDefaultCapacity { get; init; }
    public int? sessionCapacity { get; init; }
    public int? currentAttendance { get; init; }
    public string? thumbnailUrl { get; init; }
    public string? thumbnailHash { get; init; }
    public bool? isVirtualSpacePrivate { get; init; }
    public bool? ageGated { get; init; }
    public ImmutableArray<LiveSessionMarker> markers { get; init; } = ImmutableArray<LiveSessionMarker>.Empty;
    public ImmutableArray<FrontParticipant> allParticipants { get; init; } = ImmutableArray<FrontParticipant>.Empty;

    public required string callerInAppIdentifier { get; init; }
    public string? supplementalIdentifier { get; init; }

    public static FrontLiveSession FromCore(ImmutableLiveSession liveSession)
    {
        var isVrc = liveSession.namedApp == NamedApp.VRChat;
        return new FrontLiveSession
        {
            guid = liveSession.guid,
            namedApp = liveSession.namedApp,
            qualifiedAppName = liveSession.qualifiedAppName,
            inAppSessionIdentifier = liveSession.inAppSessionIdentifier,
            inAppSessionName = liveSession.inAppSessionName,
            inAppVirtualSpaceName = liveSession.inAppVirtualSpaceName,
            inAppHost = liveSession.inAppHost != null ? FrontLiveSessionHost.FromCore(liveSession.inAppHost) : null,
            participants = [.. liveSession.participants.Select(FrontParticipant.FromCore)],
            virtualSpaceDefaultCapacity = liveSession.virtualSpaceDefaultCapacity,
            sessionCapacity = liveSession.sessionCapacity,
            currentAttendance = liveSession.currentAttendance,
            thumbnailUrl = isVrc ? null : liveSession.thumbnailUrl,
            thumbnailHash = liveSession.thumbnailUrl != null && isVrc ? VRChatThumbnailCache.Sha(liveSession.thumbnailUrl) : null,
            isVirtualSpacePrivate = liveSession.isVirtualSpacePrivate,
            ageGated = liveSession.ageGated,
            markers = liveSession.markers,
            allParticipants = [.. liveSession.allParticipants.Select(FrontParticipant.FromCore)],
            callerInAppIdentifier = liveSession.callerInAppIdentifier,
            supplementalIdentifier = liveSession.supplementalIdentifier,
        };
    }
}

[Serializable]
internal record FrontParticipant
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

[Serializable]
public record FrontImmutableKnownParticipantAccount
{
    public required string inAppIdentifier { get; init; }

    public static FrontImmutableKnownParticipantAccount FromCore(ImmutableKnownParticipantAccount participantKnownAccount)
    {
        return new FrontImmutableKnownParticipantAccount
        {
            inAppIdentifier = participantKnownAccount.inAppIdentifier
        };
    }
}

[Serializable]
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

[Serializable]
internal record FrontLiveSessionHost
{
    public required string inAppHostIdentifier;
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

[Serializable]
internal record FrontProgressTracker
{
    public required string name;
    public required int accomplished;
    public required int total;

    public static FrontProgressTracker FromCore(IncrementalEnumerationTracker tracker)
    {
        return new FrontProgressTracker
        {
            name = tracker.Name,
            accomplished = tracker.AccomplishedCount,
            total = tracker.TotalCount
        };
    }
}