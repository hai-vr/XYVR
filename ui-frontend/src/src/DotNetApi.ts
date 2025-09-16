export class DotNetApi {
    // noinspection JSDeprecatedSymbols
    private _isPhotino: boolean = !!(window.external as any).sendMessage;

    async appApiGetAllExposedIndividualsOrderedByContact(): Promise<string> {
        console.log(`this is photino? ${this._isPhotino}`)
        if (this._isPhotino) return this.PhotinoSendMessage({
            endpoint: 'appApi',
            methodName: 'GetAllExposedIndividualsOrderedByContact',
            params: []
        })
        else return await window.chrome.webview.hostObjects.appApi.GetAllExposedIndividualsOrderedByContact();
    }
    
    private PhotinoSendMessage(photinoMsg: PhotinoMessage): any {
        console.log('PhotinoSendMessage', photinoMsg);

        // noinspection JSDeprecatedSymbols
        return (window.external as any).sendMessage(JSON.stringify(photinoMsg));
    }
}

interface PhotinoMessage {
    endpoint: string;
    methodName: string;
    params: any[];
}
