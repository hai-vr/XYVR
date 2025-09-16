namespace XYVR.Core;

[Serializable]
public class Connector
{
    public required string guid;
    
    public required string displayName;
    public required ConnectorType type;
    public required RefreshMode refreshMode;
    public required LiveMode liveMode;

    public ConnectorAccount? account;
}

[Serializable]
public class ConnectorAccount
{
    public required NamedApp namedApp;
    public required string qualifiedAppName;

    public required string inAppIdentifier;
    public required string inAppDisplayName;
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