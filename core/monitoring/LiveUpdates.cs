using System.Collections.Immutable;

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

public record ImmutableLiveSession
{
    public string guid { get; init; }

    public NamedApp namedApp { get; init; }
    public string qualifiedAppName { get; init; }
    
    public string inAppSessionIdentifier { get; init; }
    
    public string? inAppSessionName { get; init; }
    public string? inAppVirtualSpaceName { get; init; }
    
    public ImmutableLiveSessionHost? inAppHost { get; init; }

    public ImmutableArray<ImmutableParticipant> participants { get; init; } = ImmutableArray<ImmutableParticipant>.Empty;

    public virtual bool Equals(ImmutableLiveSession? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return guid == other.guid &&
               namedApp == other.namedApp &&
               qualifiedAppName == other.qualifiedAppName &&
               inAppSessionIdentifier == other.inAppSessionIdentifier &&
               inAppSessionName == other.inAppSessionName &&
               inAppVirtualSpaceName == other.inAppVirtualSpaceName &&
               Equals(inAppHost, other.inAppHost) &&
               participants.SequenceEqual(other.participants);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = guid.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)namedApp;
            hashCode = (hashCode * 397) ^ qualifiedAppName.GetHashCode();
            hashCode = (hashCode * 397) ^ inAppSessionIdentifier.GetHashCode();
            hashCode = (hashCode * 397) ^ (inAppSessionName != null ? inAppSessionName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (inAppVirtualSpaceName != null ? inAppVirtualSpaceName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (inAppHost != null ? inAppHost.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ participants.Aggregate(0, (h, a) => h ^ a.GetHashCode());
            return hashCode;
        }
    }
}

public record ImmutableNonIndexedLiveSession
{
    public NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }
    
    public required string inAppSessionIdentifier { get; init; }
    
    public string? inAppSessionName { get; init; }
    public string? inAppVirtualSpaceName { get; init; }
    
    public ImmutableLiveSessionHost? inAppHost { get; init; }

    public static ImmutableLiveSession MakeIndexed(ImmutableNonIndexedLiveSession inputSession)
    {
        return new ImmutableLiveSession
        {
            guid = XYVRGuids.ForSession(),
            namedApp = inputSession.namedApp,
            qualifiedAppName = inputSession.qualifiedAppName,
            inAppSessionIdentifier = inputSession.inAppSessionIdentifier,
            inAppSessionName = inputSession.inAppSessionName,
            inAppVirtualSpaceName = inputSession.inAppVirtualSpaceName,
            inAppHost = inputSession.inAppHost,
            participants = ImmutableArray<ImmutableParticipant>.Empty
        };
    }
}

public record ImmutableParticipant
{
    public bool isKnown { get; init; }
    public ImmutableKnownParticipantAccount? knownAccount { get; init; }
    public ImmutableUnknownParticipantAccount? unknownAccount { get; init; }

    public bool isHost { get; init; }
}

public record ImmutableKnownParticipantAccount
{
    public string inAppIdentifier { get; init; }
}

public record ImmutableUnknownParticipantAccount
{
    public string? inAppIdentifier { get; init; }
    public string? inAppDisplayName { get; init; }
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
    
    public ImmutableLiveSessionHost? inAppHost;

    public ImmutableLiveUserKnownSession ToImmutable()
    {
        return new ImmutableLiveUserKnownSession
        {
            inAppSessionIdentifier = inAppSessionIdentifier,
            inAppSessionName = inAppSessionName,
            inAppVirtualSpaceName = inAppVirtualSpaceName,
            isJoinable = isJoinable,
            inAppHost = inAppHost
        };
    }
}
