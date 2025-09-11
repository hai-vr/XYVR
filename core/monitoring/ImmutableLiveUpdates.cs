namespace XYVR.Core;

public record ImmutableLiveUserUpdate
{
    public NamedApp namedApp { get; init; }
    public string trigger { get; init; }
    public string qualifiedAppName { get; init; }
    public string inAppIdentifier { get; init; }

    public OnlineStatus? onlineStatus { get; init; }
    public ImmutableLiveUserSessionState? mainSession { get; init; }
    public string? customStatus { get; init; }

    public string callerInAppIdentifier { get; init; }
}

public record ImmutableLiveUserSessionState
{
    public ImmutableLiveUserKnownSession? knownSession { get; init; }
    public LiveUserSessionKnowledge knowledge { get; init; }
}

public record class ImmutableLiveUserKnownSession
{
    public string inAppSessionIdentifier { get; init; }

    public string? inAppSessionName { get; init; }
    public string? inAppVirtualSpaceName { get; init; }

    public bool? isJoinable { get; init; }

    public ImmutableLiveSessionHost? inAppHost { get; init; }
}

public record class ImmutableLiveSessionHost
{
    public string inAppHostIdentifier { get; init; }
    public string? inAppHostDisplayName { get; init; }
}