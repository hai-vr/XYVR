import type {FrontAccount, NamedAppType, OnlineStatusType} from "./CoreTypes.ts";

export const LiveSessionKnowledge = {
    Indeterminate: "Indeterminate",
    Known: "Known",
    ContactsOnlyWorld: "ContactsOnlyWorld",
    PrivateSession: "PrivateSession",
    PrivateWorld: "PrivateWorld",
    OffPlatform: "OffPlatform",
    VRCTraveling: "VRCTraveling",
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
}

export interface FrontLiveUserSessionState {
    knownSession?: FrontLiveUserKnownSession;
    knowledge: LiveUserSessionKnowledgeType;
}

export interface FrontLiveUserKnownSession {
    inAppSessionIdentifier: string;
    inAppSessionName?: string;
    inAppVirtualSpaceName?: string;
    inAppHost?: FrontLiveSessionHost;
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
}

export interface FrontParticipant {
    isKnown: boolean;
    knownAccount?: FrontAccount;
    unknownAccount?: FrontUnknownAccount;
    isHost: boolean;
}

export interface FrontUnknownAccount {
    inAppIdentifier?: string;
    inAppDisplayName?: string;
}

export interface FrontLiveSessionHost {
    inAppHostIdentifier: string;
    inAppHostDisplayName?: string;
}