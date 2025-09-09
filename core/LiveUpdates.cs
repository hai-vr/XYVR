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
    public LiveSessionKnowledge knowledge;
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
    public Account? knownAccount;
    public UnknownAccount? unknownAccount;

    public bool isHost;
}

public class UnknownAccount
{
    public string? inAppIdentifier;
    public string? inAppDisplayName;
}

public enum LiveSessionKnowledge
{
    Indeterminate,
    Known,
    // Resonite
    ContactsOnlyWorld,
    PrivateSession,
    // VRChat
    PrivateWorld,
    OffPlatform,
}

public class LiveUserKnownSession
{
    public string inAppSessionIdentifier;
    
    public string? inAppSessionName;
    public string? inAppVirtualSpaceName;
    
    public LiveSessionHost? inAppHost;
}

public class LiveSessionHost
{
    public string inAppHostIdentifier;
    public string? inAppHostDisplayName;

    public LiveSessionHost ShallowCopy()
    {
        return (LiveSessionHost)this.MemberwiseClone();
    }
}