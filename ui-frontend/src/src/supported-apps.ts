import {NamedApp} from "./types/CoreTypes.ts";

export interface SupportedApp {
    namedApp: string;
    displayName: string;
}

export const ResoniteApp: SupportedApp = {
    namedApp: NamedApp.Resonite,
    displayName: "Resonite"
};

export const VRChatApp: SupportedApp = {
    namedApp: NamedApp.VRChat,
    displayName: "VRChat"
};

export const ChilloutVRApp: SupportedApp = {
    namedApp: NamedApp.ChilloutVR,
    displayName: "ChilloutVR"
}