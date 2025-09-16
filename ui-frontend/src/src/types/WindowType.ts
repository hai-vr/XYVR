declare global {
    // noinspection JSUnusedGlobalSymbols
    interface Window {
        chrome: {
            webview: {
                hostObjects: {
                    appApi: {
                        GetAppVersion(): Promise<string>;
                        FusionIndividuals(toAugment: string, toDestroy: string): Promise<void>;
                        DesolidarizeIndividuals(toDesolidarize: string): Promise<void>;
                        GetAllExposedIndividualsOrderedByContact(): Promise<string>;
                        OpenLink(url: string): Promise<void>;
                    };
                    preferencesApi: {
                        GetPreferences(): Promise<string>;
                        SetPreferences(preferences: string): Promise<void>;
                    };
                    dataCollectionApi: {
                        GetConnectors(): Promise<string>;
                        CreateConnector(connectorType: string): Promise<void>;
                        DeleteConnector(guid: string): Promise<void>;
                        StartDataCollection(): Promise<void>;
                        TryLogin(guid: string, login: string, password: string, stayLoggedIn: boolean): Promise<string>;
                        TryTwoFactor(guid: string, isTwoFactorEmail: boolean, twoFactorCode: string, stayLoggedIn: boolean): Promise<string>;
                        TryLogout(guid: string): Promise<string>;
                    };
                    liveApi: {
                        GetAllExistingLiveUserData(): Promise<string>;
                        GetAllExistingLiveSessionData(): Promise<string>;
                    }
                };
            };
        };
        addEventListener(type: string, listener: (event: any) => void): void;
        removeEventListener(type: string, listener: (event: any) => void): void;
    }
}

export {};
