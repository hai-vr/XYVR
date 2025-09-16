import { useEffect, useState } from 'react'
import './App.css'
import AppRouter from './components/AppRouter.tsx'
import {DotNetApi} from "./DotNetApi.ts";

function App() {
// @ts-ignore
    const [appVersion, setAppVersion] = useState('');

    useEffect(() => {
        // Wait for WebView2 API to be available
        const initializeApi = async () => {
            const version = await window.chrome.webview.hostObjects.appApi.GetAppVersion();
            setAppVersion(version);
        };

        initializeApi();

        DotNetApi.EnsureRegistered();
    }, []);

    return <AppRouter appVersion={appVersion} />
}

export default App