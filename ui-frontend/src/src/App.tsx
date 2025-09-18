import { useEffect, useState } from 'react'
import './App.css'
import AppRouter from './components/AppRouter.tsx'
import {DotNetApi} from "./DotNetApi.ts";
import "./i18n.ts";

function App() {
    const dotNetApi = new DotNetApi();

// @ts-ignore
    const [appVersion, setAppVersion] = useState('');
    const [isBound, setIsBound] = useState(false);

    useEffect(() => {
        // Wait for WebView2 API to be available
        const initializeApi = async () => {
            const version = await dotNetApi.appApiGetAppVersion();
            setAppVersion(version);
        };

        initializeApi();

        DotNetApi.EnsureRegistered();
        setIsBound(DotNetApi.IsBound());
    }, []);

    return <>
        {isBound && <AppRouter appVersion={appVersion} />}
    </>
}

export default App