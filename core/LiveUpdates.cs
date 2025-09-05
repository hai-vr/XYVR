namespace XYVR.Core;

public class LiveUpdate
{
    public NamedApp namedApp;
    public string qualifiedAppName;
    public string inAppIdentifier;

    public OnlineStatus? onlineStatus;
    public LiveKnownSession? mainSession;
}

public enum OnlineStatus
{
    Offline,
    Online,
    // Resonite
    Busy,
    Away,
    Sociable,
    // VRChat
    AskMe,
    DoNotDisturb,
    JoinMe
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
    
    public string inAppVirtualSpaceName;
    
    public LiveSessionHost? inAppHost;
}

public class LiveSessionHost
{
    public string inAppHostIdentifier;
    public string? inAppHostDisplayName;
}