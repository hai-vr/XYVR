import type {NamedAppType} from "./CoreTypes.ts";

export interface ConnectionAttemptResult {
    guid: string;
    type: ConnectionAttemptResultTypeType;
    account: ConnectorAccount;
    isTwoFactorEmail: boolean;
}

export const ConnectionAttemptResultType = {
    Failure: "Failure",
    Success: "Success",
    NeedsTwoFactorCode: "NeedsTwoFactorCode",
    LoggedOut: "LoggedOut",
} as const;

export type ConnectionAttemptResultTypeType = typeof ConnectionAttemptResultType[keyof typeof ConnectionAttemptResultType];

export interface ConnectorAccount {
    namedApp: NamedAppType;
    qualifiedAppName: string;

    inAppIdentifier: string;
    inAppDisplayName: string;
}