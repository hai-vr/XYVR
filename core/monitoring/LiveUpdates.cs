namespace XYVR.Core;

public class LiveUserUpdate
{
    public NamedApp namedApp;
    public string trigger;
    public string qualifiedAppName;
    public string inAppIdentifier;

    public OnlineStatus? onlineStatus;
    public LiveUserSessionState? mainSession;
    public string? customStatus;

    public string callerInAppIdentifier;

    public ImmutableLiveUserUpdate ToImmutable()
    {
        return new ImmutableLiveUserUpdate
        {
            namedApp = namedApp,
            trigger = trigger,
            qualifiedAppName = qualifiedAppName,
            inAppIdentifier = inAppIdentifier,
            onlineStatus = onlineStatus,
            mainSession = mainSession?.ToImmutable(),
            customStatus = customStatus,
            callerInAppIdentifier = callerInAppIdentifier
        };
    }
}

public enum OnlineStatus
{
    Indeterminate,
    Offline,
    Online,
    // Resonite
    ResoniteSociable,
    ResoniteBusy,
    ResoniteAway,
    ResoniteInvisible,
    // VRChat
    VRChatJoinMe,
    VRChatAskMe,
    VRChatDND,
}

public class LiveUserSessionState
{
    public LiveUserKnownSession? knownSession;
    public LiveUserSessionKnowledge knowledge;

    public ImmutableLiveUserSessionState ToImmutable()
    {
        return new ImmutableLiveUserSessionState
        {
            knownSession = knownSession?.ToImmutable(),
            knowledge = knowledge
        };
    }
}

public class LiveSession
{
    public string guid;

    public NamedApp namedApp;
    public string qualifiedAppName;
    
    public string inAppSessionIdentifier;
    
    public string? inAppSessionName;
    public string? inAppVirtualSpaceName;
    
    public LiveSessionHost? inAppHost;

    public List<Participant> participants = new();
}

public class NonIndexedLiveSession
{
    public NamedApp namedApp;
    public string qualifiedAppName;
    
    public string inAppSessionIdentifier;
    
    public string? inAppSessionName;
    public string? inAppVirtualSpaceName;
    
    public LiveSessionHost? inAppHost;

    public static LiveSession MakeIndexed(NonIndexedLiveSession inputSession)
    {
        return new LiveSession
        {
            guid = XYVRGuids.ForSession(),
            namedApp = inputSession.namedApp,
            qualifiedAppName = inputSession.qualifiedAppName,
            inAppSessionIdentifier = inputSession.inAppSessionIdentifier,
            inAppSessionName = inputSession.inAppSessionName,
            inAppVirtualSpaceName = inputSession.inAppVirtualSpaceName,
            inAppHost = inputSession.inAppHost?.ShallowCopy(),
            participants = new()
        };
    }
}

public class Participant
{
    public bool isKnown;
    public ImmutableAccount? knownAccount;
    public UnknownAccount? unknownAccount;

    public bool isHost;
}

public class UnknownAccount
{
    public string? inAppIdentifier;
    public string? inAppDisplayName;
}

public enum LiveUserSessionKnowledge
{
    Indeterminate,
    Known,
    KnownButNoData,
    // Resonite
    ContactsOnlyWorld,
    PrivateSession,
    // VRChat
    PrivateWorld,
    OffPlatform,
    VRCTraveling
}

public class LiveUserKnownSession
{
    public string inAppSessionIdentifier;
    
    public string? inAppSessionName;
    public string? inAppVirtualSpaceName;

    public bool? isJoinable;
    
    public LiveSessionHost? inAppHost;

    public ImmutableLiveUserKnownSession ToImmutable()
    {
        return new ImmutableLiveUserKnownSession
        {
            inAppSessionIdentifier = inAppSessionIdentifier,
            inAppSessionName = inAppSessionName,
            inAppVirtualSpaceName = inAppVirtualSpaceName,
            isJoinable = isJoinable,
            inAppHost = inAppHost?.ToImmutable()
        };
    }
}

public class LiveSessionHost
{
    public string inAppHostIdentifier;
    public string? inAppHostDisplayName;

    public LiveSessionHost ShallowCopy()
    {
        return (LiveSessionHost)this.MemberwiseClone();
    }

    public ImmutableLiveSessionHost ToImmutable()
    {
        return new ImmutableLiveSessionHost
        {
            inAppHostIdentifier = inAppHostIdentifier,
            inAppHostDisplayName = inAppHostDisplayName
        };
    }
}