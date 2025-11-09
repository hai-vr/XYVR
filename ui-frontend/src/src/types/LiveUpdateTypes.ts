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
    PrivateInstance: "PrivateInstance"
} as const;

export type LiveUserSessionKnowledgeType = typeof LiveSessionKnowledge[keyof typeof LiveSessionKnowledge];

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