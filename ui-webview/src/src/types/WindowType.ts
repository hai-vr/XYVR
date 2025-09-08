declare global {
    interface Window {
        chrome: {
            webview: {
                hostObjects: {
                    appApi: {
                        GetAppVersion(): Promise<string>;
                    };
                    preferencesApi: {
                        GetPreferences(): Promise<string>;
                        SetPreferences(preferences: string): Promise<void>;
                    };
                };
            };
        };
        addEventListener(type: string, listener: (event: any) => void): void;
        removeEventListener(type: string, listener: (event: any) => void): void;
    }
}

export {};
