namespace XYVR.Core;

public class Connector
{
    public string guid;
    
    public string displayName;
    public ConnectorType type;
    public RefreshMode refreshMode;
    public LiveMode liveMode;

    public ConnectorAccount? account;
}

public class ConnectorAccount
{
    public NamedApp namedApp;
    public string qualifiedAppName;

    public string inAppIdentifier;
    public string inAppDisplayName;
}

public enum ConnectorType
{
    Offline,
    ResoniteAPI,
    VRChatAPI,
    ChilloutVRAPI
}

public enum RefreshMode
{
    ManualUpdatesOnly,
    ContinuousLightUpdates,
    ContinuousFullUpdates,
}

public enum LiveMode
{
    NoLiveFunction,
    OnlyInGameStatus,
    FullStatus,
}

public class ConnectorCredentials
{
    public string associatedGuid;
    
    public string? login;
    public string? password;
    public string? twoFactorCode;

    public ConnectorToken? token;
}

public class ConnectorToken
{
    public DateTime tokenCreatedAt;
    public string token;
    
    public DateTime? tokenExpiresAfter;
}
