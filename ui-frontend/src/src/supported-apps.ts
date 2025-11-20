import {NamedApp} from "./types/CoreTypes.ts";

export interface SupportedApp {
    namedApp: string;
    displayName: string;
    isSessionCapacityKnowable: boolean;
    isTotalNumberOfUsersKnowable: boolean;
    areOtherSessionUsersKnowable: boolean;
}

export const ResoniteApp: SupportedApp = {
    namedApp: NamedApp.Resonite,
    displayName: "Resonite",
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: true,
};

export const VRChatApp: SupportedApp = {
    namedApp: NamedApp.VRChat,
    displayName: "VRChat",
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: false,
};

export const ChilloutVRApp: SupportedApp = {
    namedApp: NamedApp.ChilloutVR,
    displayName: "ChilloutVR",
    isSessionCapacityKnowable: true,
    isTotalNumberOfUsersKnowable: true,
    areOtherSessionUsersKnowable: true,
}

export const ClusterVRApp: SupportedApp = {
    namedApp: NamedApp.Cluster,
    displayName: "Cluster",
    isSessionCapacityKnowable: false,
    isTotalNumberOfUsersKnowable: false,
    areOtherSessionUsersKnowable: false,
}