import {NamedApp} from "./types/CoreTypes.ts";

export interface SupportedApp {
    namedApp: string;
    displayName: string;
    searchTerm: string;
    isSessionCapacityKnowable: boolean;
    isTotalNumberOfUsersKnowable: boolean;
    areOtherSessionUsersKnowable: boolean;
}

export const ResoniteApp: SupportedApp = {
    namedApp: NamedApp.Resonite,
    displayName: "Resonite",
    searchTerm: 'resonite',
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: true,
};

export const VRChatApp: SupportedApp = {
    namedApp: NamedApp.VRChat,
    displayName: "VRChat",
    searchTerm: 'vrchat',
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: false,
};

export const ChilloutVRApp: SupportedApp = {
    namedApp: NamedApp.ChilloutVR,
    displayName: "ChilloutVR",
    searchTerm: 'chilloutvr',
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: true,
}

export const ClusterVRApp: SupportedApp = {
    namedApp: NamedApp.Cluster,
    displayName: "Cluster",
    searchTerm: 'cluster',
    isSessionCapacityKnowable: false,
    isTotalNumberOfUsersKnowable: false,
    areOtherSessionUsersKnowable: false,
}

export const SupportedAppsByNamedApp: Record<string, SupportedApp> = {
    [NamedApp.Resonite]: ResoniteApp,
    [NamedApp.VRChat]: VRChatApp,
    [NamedApp.ChilloutVR]: ChilloutVRApp,
    [NamedApp.Cluster]: ClusterVRApp,
};