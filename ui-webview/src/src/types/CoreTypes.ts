import type {ConnectorAccount, ConnectorTypeType, LiveModeType, RefreshModeType} from "./ConnectorTypes.ts";

export const NamedApp = {
    NotNamed: "NotNamed",
    Resonite: "Resonite",
    VRChat: "VRChat",
    Cluster: "Cluster",
    ChilloutVR: "ChilloutVR",
} as const;

export type NamedAppType = typeof NamedApp[keyof typeof NamedApp];

export const NoteState = {
    NeverHad: "NeverHad",
    Exists: "Exists",
    WasRemoved: "WasRemoved",
} as const;

export type NoteStateType = typeof NoteState[keyof typeof NoteState];

export type Note = {
    status: NoteStateType;
    text?: string;
};

export type CallerAccount = {
    isAnonymous: boolean;
    inAppIdentifier?: string;
    note: Note;
    isContact: boolean;
};

export type AccountIdentification = {
    namedApp: NamedAppType;
    qualifiedAppName: string;
    inAppIdentifier: string;
};

export type VRChatSpecifics = {
    urls: string[];
    bio: string;
    pronouns: string;
};

export type AccountType = {
    guid: string;
    namedApp: NamedAppType;
    qualifiedAppName: string;
    inAppIdentifier: string;
    inAppDisplayName: string;
    specifics?: any;
    callers: CallerAccount[];
    allDisplayNames: string[];
    isPendingUpdate: boolean;
    isTechnical: boolean;
};

export type IndividualType = {
    guid: string;
    accounts: AccountType[];
    displayName: string;
    isAnyContact: boolean;
    isExposed: boolean;
    customName?: string;
    note: Note;
};

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

export type FrontAccount = {
    guid: string;
    namedApp: NamedAppType;
    qualifiedAppName: string;
    inAppIdentifier: string;
    inAppDisplayName: string;
    specifics?: any;
    callers: CallerAccount[];
    allDisplayNames: string[];
    isPendingUpdate: boolean;
    isTechnical: boolean;

    isAnyCallerContact: boolean;
    isAnyCallerNote: boolean;

    onlineStatus?: OnlineStatusType;
    customStatus?: string;
};

export type FrontIndividual = {
    guid: string;
    accounts: FrontAccount[];
    displayName: string;
    isAnyContact: boolean;
    isExposed: boolean;
    customName?: string;
    note: Note;

    onlineStatus?: OnlineStatusType;
    customStatus?: string;
};

export type ConnectorTypeWithExtraTracking = {
    guid: string;
    displayName: string;
    type: ConnectorTypeType;
    refreshMode: RefreshModeType;
    liveMode: LiveModeType;
    account?: ConnectorAccount;

    isLoggedIn?: boolean;
}