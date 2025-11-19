import type {NamedAppType, OnlineStatusType} from "./CoreTypes.ts";

export const LiveSessionKnowledge = {
    Indeterminate: "Indeterminate",
    Known: "Known",
    ContactsOnlyWorld: "ContactsOnlyWorld",
    PrivateSession: "PrivateSession",
    PrivateWorld: "PrivateWorld",
    OffPlatform: "OffPlatform",
    VRCTraveling: "VRCTraveling",
    OfflineInstance: "OfflineInstance",
    PrivateInstance: "PrivateInstance",
    ClusterOnlinePrivate: "ClusterOnlinePrivate"
} as const;

export type LiveUserSessionKnowledgeType = typeof LiveSessionKnowledge[keyof typeof LiveSessionKnowledge];

export const LiveSessionMarker = {
    Indeterminate: "Indeterminate",
    // VRChat
    VRCPublic: "VRCPublic",
    VRCInvitePlus: "VRCInvitePlus",
    VRCInvite: "VRCInvite",
    VRCFriends: "VRCFriends",
    VRCFriendsPlus: "VRCFriendsPlus",
    VRCGroup: "VRCGroup",
    VRCGroupPublic: "VRCGroupPublic",
    VRCGroupPlus: "VRCGroupPlus",
    // Resonite
    ResoniteAnyone: "ResoniteAnyone",
    ResoniteRegisteredUsers: "ResoniteRegisteredUsers",
    ResoniteContactsPlus: "ResoniteContactsPlus",
    ResoniteContacts: "ResoniteContacts",
    ResoniteLAN: "ResoniteLAN",
    ResonitePrivate: "ResonitePrivate",
    // ChilloutVR
    CVRPublic: "CVRPublic",
    CVRFriendsOfFriends: "CVRFriendsOfFriends",
    CVRFriends: "CVRFriends",
    CVRGroup: "CVRGroup",
    CVREveryoneCanInvite: "CVREveryoneCanInvite",
    CVROwnerMustInvite: "CVROwnerMustInvite",
    CVRGroupPlus: "CVRGroupPlus",
    CVRGroupPublic: "CVRGroupPublic",
    // Resonite non-access
    ResoniteHeadless: "ResoniteHeadless",
    ClusterEvent: "ClusterEvent",
} as const;

export type LiveSessionMarkerType = typeof LiveSessionMarker[keyof typeof LiveSessionMarker];

export interface FrontLiveUserUpdate {
    namedApp: NamedAppType;
    trigger: string;
    qualifiedAppName: string;
    inAppIdentifier: string;
    onlineStatus?: OnlineStatusType;
    mainSession?: FrontLiveUserSessionState;
    customStatus?: string;
    callerInAppIdentifier: string;
    multiSessions: FrontLiveSession[];
}

export interface FrontLiveUserSessionState {
    knowledge: LiveUserSessionKnowledgeType;
    sessionGuid?: string;
    liveSession?: FrontLiveSession;
}

export interface FrontLiveSession {
    guid: string;
    namedApp: NamedAppType;
    qualifiedAppName: string;
    inAppSessionIdentifier: string;
    inAppSessionName?: string;
    inAppVirtualSpaceName?: string;
    inAppHost?: FrontLiveSessionHost;
    participants: FrontParticipant[];
    virtualSpaceDefaultCapacity?: number;
    sessionCapacity?: number;
    currentAttendance?: number;
    thumbnailUrl?: string;
    thumbnailHash?: string;
    isVirtualSpacePrivate?: boolean;
    ageGated?: boolean;
    markers: LiveSessionMarkerType[];
    allParticipants: FrontParticipant[];
    callerInAppIdentifier: string;
}

export interface FrontParticipant {
    isKnown: boolean;
    knownAccount?: FrontKnownParticipantAccount;
    unknownAccount?: FrontUnknownParticipantAccount;
    isHost: boolean;
}

export interface FrontKnownParticipantAccount {
    inAppIdentifier: string;
}

export interface FrontUnknownParticipantAccount {
    inAppIdentifier?: string;
    inAppDisplayName?: string;
}

export interface FrontLiveSessionHost {
    inAppHostIdentifier: string;
    inAppHostDisplayName?: string;
}