﻿using System.Collections.Immutable;

namespace XYVR.Core;

public record ImmutableLiveUserUpdate
{
    public required NamedApp namedApp { get; init; }
    public required string trigger { get; init; }
    public required string qualifiedAppName { get; init; }
    public required string inAppIdentifier { get; init; }

    public OnlineStatus? onlineStatus { get; init; }
    public ImmutableLiveUserSessionState? mainSession { get; init; }
    public string? customStatus { get; init; }

    public required string callerInAppIdentifier { get; init; }
    
    public object? sessionSpecifics { get; init; }
    public ImmutableArray<string> multiSessionGuids { get; init; } = ImmutableArray<string>.Empty;

    public ImmutableParticipant AsNonHostParticipant()
    {
        return new ImmutableParticipant
        {
            isHost = false,
            isKnown = true,
            knownAccount = new ImmutableKnownParticipantAccount
            {
                inAppIdentifier = inAppIdentifier
            }
        };
    }

    public virtual bool Equals(ImmutableLiveUserUpdate? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return namedApp == other.namedApp &&
               trigger == other.trigger &&
               qualifiedAppName == other.qualifiedAppName &&
               inAppIdentifier == other.inAppIdentifier &&
               onlineStatus == other.onlineStatus &&
               Equals(mainSession, other.mainSession) &&
               customStatus == other.customStatus &&
               callerInAppIdentifier == other.callerInAppIdentifier &&
               Equals(sessionSpecifics, other.sessionSpecifics) &&
               multiSessionGuids.SequenceEqual(other.multiSessionGuids);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)namedApp;
            hashCode = (hashCode * 397) ^ trigger.GetHashCode();
            hashCode = (hashCode * 397) ^ qualifiedAppName.GetHashCode();
            hashCode = (hashCode * 397) ^ inAppIdentifier.GetHashCode();
            hashCode = (hashCode * 397) ^ onlineStatus.GetHashCode();
            hashCode = (hashCode * 397) ^ (mainSession != null ? mainSession.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (customStatus != null ? customStatus.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ callerInAppIdentifier.GetHashCode();
            hashCode = (hashCode * 397) ^ (sessionSpecifics != null ? sessionSpecifics.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ XYVRSequenceHash.HashCodeOf(multiSessionGuids);
            return hashCode;
        }
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

public record ImmutableLiveUserSessionState
{
    public required LiveUserSessionKnowledge knowledge { get; init; }
    public string? sessionGuid { get; init; } // Non-null if knowledge is set to Known
}

public record ImmutableLiveSession
{
    public required string guid { get; init; }

    public required NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }
    
    public required string inAppSessionIdentifier { get; init; }
    
    public string? inAppSessionName { get; init; }
    public string? inAppVirtualSpaceName { get; init; }
    
    public ImmutableLiveSessionHost? inAppHost { get; init; }

    public ImmutableArray<ImmutableParticipant> participants { get; init; } = ImmutableArray<ImmutableParticipant>.Empty;
    
    public int? virtualSpaceDefaultCapacity { get; init; }
    public int? sessionCapacity { get; init; }
    public int? currentAttendance { get; init; }
    
    public string? thumbnailUrl { get; init; }
    public bool? isVirtualSpacePrivate { get; init; }

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
               participants.SequenceEqual(other.participants) && 
               virtualSpaceDefaultCapacity == other.virtualSpaceDefaultCapacity &&
               sessionCapacity == other.sessionCapacity &&
               currentAttendance == other.currentAttendance &&
               isVirtualSpacePrivate == other.isVirtualSpacePrivate;
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
            hashCode = (hashCode * 397) ^ XYVRSequenceHash.HashCodeOf(participants);
            hashCode = (hashCode * 397) ^ virtualSpaceDefaultCapacity.GetHashCode();
            hashCode = (hashCode * 397) ^ sessionCapacity.GetHashCode();
            hashCode = (hashCode * 397) ^ currentAttendance.GetHashCode();
            hashCode = (hashCode * 397) ^ (isVirtualSpacePrivate != null ? isVirtualSpacePrivate.GetHashCode() : 0);
            return hashCode;
        }
    }

    public override string ToString()
    {
        var participantsStr = participants.IsEmpty 
            ? "[]" 
            : $"[{string.Join(", ", participants)}]";
            
        return $"ImmutableLiveSession {{ guid: {guid}, namedApp: {namedApp}, qualifiedAppName: {qualifiedAppName}, " +
               $"inAppSessionIdentifier: {inAppSessionIdentifier}, inAppSessionName: {inAppSessionName}, " +
               $"inAppVirtualSpaceName: {inAppVirtualSpaceName}, inAppHost: {inAppHost}, " +
               $"participants: {participantsStr}, virtualSpaceDefaultCapacity: {virtualSpaceDefaultCapacity}, " +
               $"sessionCapacity: {sessionCapacity}, currentAttendance: {currentAttendance} }}";
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
    
    public int? virtualSpaceDefaultCapacity { get; init; }
    public int? sessionCapacity { get; init; }
    public int? currentAttendance { get; init; }
    
    public string? thumbnailUrl { get; init; }
    
    public bool? isVirtualSpacePrivate { get; init; }

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
            participants = ImmutableArray<ImmutableParticipant>.Empty,
            virtualSpaceDefaultCapacity = inputSession.virtualSpaceDefaultCapacity,
            sessionCapacity = inputSession.sessionCapacity,
            currentAttendance = inputSession.currentAttendance,
            thumbnailUrl = inputSession.thumbnailUrl,
            isVirtualSpacePrivate = inputSession.isVirtualSpacePrivate
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
    public required string inAppIdentifier { get; init; }
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
    Offline,
    // Resonite
    ContactsOnlyWorld,
    PrivateSession,
    // VRChat
    PrivateWorld,
    OffPlatform,
    VRCTraveling,
    // ChilloutVR
    OfflineInstance,
    PrivateInstance
}

public record ImmutableLiveSessionHost
{
    public required string inAppHostIdentifier { get; init; }
    public string? inAppHostDisplayName { get; init; }
}