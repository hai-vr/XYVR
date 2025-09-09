import type {FrontLiveUserSessionState} from "./LiveUpdateTypes.ts";

export const NamedApp = {
    NotNamed: "NotNamed",
    Resonite: "Resonite",
    VRChat: "VRChat",
    Cluster: "Cluster",
    ChilloutVR: "ChilloutVR",
} as const;

export type NamedAppType = typeof NamedApp[keyof typeof NamedApp];

export const OnlineStatus = {
    Indeterminate: "Indeterminate",
    Offline: "Offline",
    Online: "Online",
    ResoniteSociable: "ResoniteSociable",
    ResoniteBusy: "ResoniteBusy",
    ResoniteAway: "ResoniteAway",
    ResoniteInvisible: "ResoniteInvisible",
    VRChatJoinMe: "VRChatJoinMe",
    VRChatAskMe: "VRChatAskMe",
    VRChatDND: "VRChatDND",
} as const;

export type OnlineStatusType = typeof OnlineStatus[keyof typeof OnlineStatus];

export interface FrontAccount {
    guid: string;
    namedApp: NamedAppType;
    qualifiedAppName: string;
    inAppIdentifier: string;
    inAppDisplayName: string;
    specifics?: any;
    callers: FrontCallerAccount[];
    allDisplayNames: string[];
    isPendingUpdate: boolean;
    isTechnical: boolean;

    isAnyCallerContact: boolean;
    isAnyCallerNote: boolean;

    onlineStatus?: OnlineStatusType;
    customStatus?: string;
    mainSession?: FrontLiveUserSessionState
}

export interface FrontIndividual {
    guid: string;
    accounts: FrontAccount[];
    displayName: string;
    isAnyContact: boolean;
    isExposed: boolean;
    customName?: string;
    note?: string;

    onlineStatus?: OnlineStatusType;
    customStatus?: string;
}

export interface FrontCallerAccount {
    isAnonymous: boolean;
    inAppIdentifier?: string;
    note?: string;
    isContact: boolean;
}

export interface VRChatSpecifics {
    urls: string[];
    bio: string;
    pronouns: string;
}
