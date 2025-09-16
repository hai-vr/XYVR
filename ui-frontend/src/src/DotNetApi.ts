export class DotNetApi {
    private static G_dict: { [key: string]: IndexedPromise } = {};
    private static G_isRegistered: boolean;

    public static EnsureRegistered() {
        if (!DotNetApi.G_isRegistered) {
            // noinspection JSDeprecatedSymbols
            let isPhotino = !!(window.external as any).sendMessage;
            if (isPhotino) {
                // noinspection JSDeprecatedSymbols
                (window.external as any).receiveMessage((message: string) => DotNetApi.WhenPhotinoMessageReceived(message))
            }
            DotNetApi.G_isRegistered = true;
        }
    }

    // noinspection JSDeprecatedSymbols
    private _isPhotino: boolean = !!(window.external as any).sendMessage;

    async appApiGetAppVersion(): Promise<string> { return this.dispatch('appApi', 'GetAppVersion'); }
    async appApiGetAllExposedIndividualsOrderedByContact(): Promise<string> { return this.dispatch('appApi', 'GetAllExposedIndividualsOrderedByContact'); }
    async appApiFusionIndividuals(toAugment: string, toDestroy: string): Promise<string> { return this.dispatch('appApi', 'FusionIndividuals', [toAugment, toDestroy]); }
    async appApiDesolidarizeIndividuals(toDesolidarize: string): Promise<string> { return this.dispatch('appApi', 'DesolidarizeIndividuals', [toDesolidarize]); }
    async appApiOpenLink(url: string): Promise<string> { return this.dispatch('appApi', 'OpenLink', [url]); }

    async liveApiGetAllExistingLiveSessionData(): Promise<string> { return this.dispatch('liveApi', 'GetAllExistingLiveSessionData'); }

    async preferencesApiSetPreferences(str: string): Promise<string> { return this.dispatch('preferencesApi', 'SetPreferences', [str]); }
    async preferencesApiGetPreferences(): Promise<string> { return this.dispatch('preferencesApi', 'GetPreferences'); }

    async dataCollectionApiTryLogin(guid: string, login: string, password: string, stayLoggedIn: boolean): Promise<string> { return this.dispatch('dataCollectionApi', 'TryLogin', [guid, login, password, stayLoggedIn]); }
    async dataCollectionApiTryTwoFactor(guid: string, isTwoFactorEmail: boolean, twoFactorCode: string, stayLoggedIn: boolean): Promise<string> { return this.dispatch('dataCollectionApi', 'TryTwoFactor', [guid, isTwoFactorEmail, twoFactorCode, stayLoggedIn]); }
    async dataCollectionApiTryLogout(guid: string): Promise<string> { return this.dispatch('dataCollectionApi', 'TryLogout', [guid]); }

    async dataCollectionApiGetConnectors(): Promise<string> { return this.dispatch('dataCollectionApi', 'GetConnectors'); }
    async dataCollectionApiCreateConnector(connectorType: string): Promise<string> { return this.dispatch('dataCollectionApi', 'CreateConnector', [connectorType]); }
    async dataCollectionApiDeleteConnector(guid: string): Promise<string> { return this.dispatch('dataCollectionApi', 'DeleteConnector', [guid]); }
    async dataCollectionApiStartDataCollection(): Promise<string> { return this.dispatch('dataCollectionApi', 'StartDataCollection'); }

    private async dispatch(endpoint: string, methodName: string, parameters: any[] = []): Promise<string> {
        if (this._isPhotino) {
            return this.PhotinoSendMessage({endpoint, methodName, parameters});
        } else {
            const hostObject = (window.chrome.webview.hostObjects as any)[endpoint];
            return await hostObject[methodName](...parameters);
        }
    }

    
    private PhotinoSendMessage(payload: PhotinoSendMessagePayload): Promise<string> {
        const indexedPromise = this.makeIndexedPromise();
        (DotNetApi.G_dict)[indexedPromise.id] = indexedPromise;

        console.log(`PhotinoSendMessage[${indexedPromise.id}] = ${payload}`);

        const message: PhotinoSendMessage = {
            id: indexedPromise.id,
            payload: payload
        };

        // noinspection JSDeprecatedSymbols
        (window.external as any).sendMessage(JSON.stringify(message))

        return indexedPromise.promise;
    }

    public static WhenPhotinoMessageReceived(photinoReceivedMsg: string) {
        const receive: PhotinoReceiveMessage = JSON.parse(photinoReceivedMsg);
        if (!receive.isPhotinoMessage) return;

        if (receive.isEvent) {
            console.log(`Event received ${receive.id}`);
            const detail = JSON.parse(receive.payload!);
            window.dispatchEvent(new CustomEvent(receive.id, { detail: detail }));
        }
        else {
            if (receive.id in DotNetApi.G_dict) {
                const indexedPromise = DotNetApi.G_dict[receive.id];
                if (!receive.isError) {
                    console.log(`Resolving promise ${receive.id}`);
                    indexedPromise.resolve(receive.payload);
                }
                else {
                    console.log(`Rejecting promise ${receive.id}`);
                    indexedPromise.reject(receive.payload);
                }
                delete DotNetApi.G_dict[receive.id];
            }
            else {
                console.log(`Illegal received message has no promise associated with it PhotinoMessageReceived[${receive.id}] = ${receive.payload}`);
            }
        }
    }

    private makeIndexedPromise(): IndexedPromise {
        const id = crypto.randomUUID();

        let deferred: any = {};
        deferred.promise = new Promise((res, rej) => {
            deferred.resolve = res;
            deferred.reject = rej;
        });
        deferred.id = id;
        deferred.date =  new Date();

        return deferred;
    }
}

interface IndexedPromise {
    id: string;
    promise: Promise<string>;
    resolve: (value: any) => void;
    reject: (reason?: any) => void;
    date: Date;
}

interface PhotinoSendMessage {
    id: string;
    payload: PhotinoSendMessagePayload;
}

interface PhotinoSendMessagePayload {
    endpoint: string;
    methodName: string;
    parameters: any[];
}

interface PhotinoReceiveMessage {
    isPhotinoMessage: boolean; // Must be set to true for us to process it

    isEvent: boolean;

    id: string; // event type if object
    payload?: string; // null if void return

    isError: boolean;
}
