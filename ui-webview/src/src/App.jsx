import { useEffect, useState } from 'react'
import './App.css'
import AppRouter from './components/AppRouter.jsx'

function App() {
    const [appVersion, setAppVersion] = useState('');

    useEffect(() => {
        // Wait for WebView2 API to be available
        const initializeApi = async () => {
            const version = await window.chrome.webview.hostObjects.appApi.GetAppVersion();
            setAppVersion(version);
        };

        initializeApi();
    }, []);

    return <AppRouter />
}

export default App