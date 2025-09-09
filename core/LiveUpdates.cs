namespace XYVR.Core;

public class LiveUpdate
{
    public NamedApp namedApp;
    public string trigger;
    public string qualifiedAppName;
    public string inAppIdentifier;

    public OnlineStatus? onlineStatus;
    public LiveSessionState? mainSession;
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

public class LiveSessionState
{
    public LiveKnownSession? knownSession;
    public LiveSessionKnowledge knowledge;
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

public class LiveKnownSession
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
}