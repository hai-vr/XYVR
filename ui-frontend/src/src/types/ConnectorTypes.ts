import {type NamedAppType} from "./CoreTypes.ts";

export const ConnectorType =
{
    Offline: 'Offline',
    ResoniteAPI: 'ResoniteAPI',
    VRChatAPI: 'VRChatAPI',
    ChilloutVRAPI: 'ChilloutVRAPI',
} as const;

export type ConnectorTypeType = typeof ConnectorType[keyof typeof ConnectorType];

export const RefreshMode =
{
    ManualUpdatesOnly: 'ManualUpdatesOnly',
    ContinuousLightUpdates: 'ContinuousLightUpdates',
    ContinuousFullUpdates: 'ContinuousFullUpdates',
} as const;

export type RefreshModeType = typeof RefreshMode[keyof typeof RefreshMode];

export const LiveMode =
{
    NoLiveFunction: 'NoLiveFunction',
    OnlyInGameStatus: 'OnlyInGameStatus',
    FullStatus: 'FullStatus',
} as const;

export type LiveModeType = typeof LiveMode[keyof typeof LiveMode];

export interface FrontConnector {
    guid: string;
    displayName: string;
    type: ConnectorTypeType;
    refreshMode: RefreshModeType;
    liveMode: LiveModeType;
    account?: FrontConnectorAccount;

    isLoggedIn?: boolean;
}

export interface FrontConnectorAccount {
    namedApp: NamedAppType;
    qualifiedAppName: string;

    inAppIdentifier: string;
    inAppDisplayName: string;
}