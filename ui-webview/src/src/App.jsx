import { useEffect, useState } from 'react'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'
import Account from "./Account.jsx";

function App() {
    const [count, setCount] = useState(0)
    const [appVersion, setAppVersion] = useState('');
    const [individuals, setIndividuals] = useState([]);

    useEffect(() => {
        // Wait for WebView2 API to be available
        const initializeApi = async () => {
            if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
                try {
                    const version = await window.chrome.webview.hostObjects.appApi.GetAppVersion();
                    setAppVersion(version);
                } catch (error) {
                    console.error('API not ready yet:', error);
                }
            }
        };

        // Try to initialize immediately
        initializeApi();

        // Also listen for when the DOM is fully loaded
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initializeApi);
        }

        return () => {
            document.removeEventListener('DOMContentLoaded', initializeApi);
        };
    }, []);

    const handleGetTime = async () => {
        if (window.chrome?.webview?.hostObjects?.appApi) {
            try {
                const time = await window.chrome.webview.hostObjects.appApi.GetCurrentTime();
                alert(`Current Time: ${time}`);
            } catch (error) {
                console.error('Error calling API:', error);
            }
        }
    };

    const handleGetAllExposedIndividuals = async () => {
        if (window.chrome?.webview?.hostObjects?.appApi) {
            try {
                const allIndividuals = await window.chrome.webview.hostObjects.appApi.GetAllExposedIndividuals();
                const individualsArray = JSON.parse(allIndividuals);
                setIndividuals(individualsArray);

            } catch (error) {
                console.error('Error calling API:', error);
            }
        }
    };

    const handleShowMessage = () => {
        if (window.chrome?.webview?.hostObjects?.appApi) {
            window.chrome.webview.hostObjects.appApi.ShowMessage('Hello from React!');
        }
    };

    const handleCloseApp = () => {
        if (window.chrome?.webview?.hostObjects?.appApi) {
            window.chrome.webview.hostObjects.appApi.CloseApp();
        }
    };
    
    return (
        <>
            <h1>WebView2 + React App</h1>
            <p>App Version: {appVersion}</p>
            <button onClick={handleGetTime}>Get Current Time</button>
            <button onClick={handleGetAllExposedIndividuals}>Get Individuals</button>
            <button onClick={handleShowMessage}>Show Message Box</button>
            <button onClick={handleCloseApp}>Close Application</button>

            {individuals.length > 0 && (
                <div style={{ marginTop: '30px', maxWidth: '1200px', margin: '30px auto', padding: '0 20px' }}>
                    <div style={{
                        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                        color: 'white',
                        padding: '20px',
                        borderRadius: '12px',
                        marginBottom: '20px',
                        boxShadow: '0 4px 15px rgba(0,0,0,0.1)'
                    }}>
                        <h2 style={{ margin: 0, fontSize: '24px', fontWeight: '600' }}>
                            👥 Users & Accounts ({individuals.length})
                        </h2>
                    </div>

                    <div style={{ display: 'grid', gap: '20px' }}>
                        {individuals.map((individual, index) => (
                            <div key={index} style={{
                                background: 'white',
                                border: '1px solid #e1e5e9',
                                borderRadius: '12px',
                                padding: '24px',
                                boxShadow: '0 2px 8px rgba(0,0,0,0.06)',
                                transition: 'transform 0.2s ease, box-shadow 0.2s ease',
                                cursor: 'pointer'
                            }}
                                 /*onMouseEnter={(e) => {
                                     e.target.style.transform = 'translateY(-2px)';
                                     e.target.style.boxShadow = '0 4px 16px rgba(0,0,0,0.12)';
                                 }}
                                 onMouseLeave={(e) => {
                                     e.target.style.transform = 'translateY(0)';
                                     e.target.style.boxShadow = '0 2px 8px rgba(0,0,0,0.06)';
                                 }}*/>
                                <div style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    marginBottom: '16px',
                                    paddingBottom: '12px',
                                    borderBottom: '2px solid #f8f9fa'
                                }}>
                                    <div style={{
                                        width: '40px',
                                        height: '40px',
                                        borderRadius: '50%',
                                        background: 'linear-gradient(135deg, #4facfe 0%, #00f2fe 100%)',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        color: 'white',
                                        fontSize: '18px',
                                        fontWeight: 'bold',
                                        marginRight: '12px'
                                    }}>
                                        {individual.displayName?.charAt(0).toUpperCase() || '?'}
                                    </div>
                                    <h3 style={{
                                        margin: 0,
                                        fontSize: '20px',
                                        fontWeight: '600',
                                        color: '#2c3e50'
                                    }}>
                                        {individual.displayName}
                                    </h3>
                                    {individual.isAnyContact && (
                                        <span style={{
                                            marginLeft: '12px',
                                            background: '#e8f5e8',
                                            color: '#27ae60',
                                            padding: '4px 8px',
                                            borderRadius: '12px',
                                            fontSize: '12px',
                                            fontWeight: '500'
                                        }}>
                            📞 Contact
                        </span>
                                    )}
                                </div>

                                <div>
                                    <div style={{
                                        display: 'flex',
                                        alignItems: 'center',
                                        marginBottom: '12px'
                                    }}>
                                        <h4 style={{
                                            margin: 0,
                                            fontSize: '16px',
                                            fontWeight: '600',
                                            color: '#34495e'
                                        }}>
                                            🎮 Accounts ({individual.accounts?.length || 0})
                                        </h4>
                                    </div>

                                    {individual.accounts && individual.accounts.length > 0 ? (
                                        <div style={{
                                            display: 'grid',
                                            gap: '8px',
                                            gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))'
                                        }}>
                                            {individual.accounts.map((account, accountIndex) => (
                                                <Account key={accountIndex} account={account} />
                                            ))}
                                        </div>
                                    ) : (
                                        <div style={{
                                            textAlign: 'center',
                                            padding: '20px',
                                            color: '#6c757d',
                                            fontStyle: 'italic',
                                            background: '#f8f9fa',
                                            borderRadius: '8px',
                                            border: '2px dashed #dee2e6'
                                        }}>
                                            📭 No accounts found
                                        </div>
                                    )}
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}

            <div>
                <a href="https://vite.dev" target="_blank">
                    <img src={viteLogo} className="logo" alt="Vite logo"/>
                </a>
                <a href="https://react.dev" target="_blank">
                    <img src={reactLogo} className="logo react" alt="React logo"/>
                </a>
            </div>
            <h1>Vite + React</h1>
            <div className="card">
                <button onClick={() => setCount((count) => count + 1)}>
                    count is {count}
                </button>
                <p>
                    Edit <code>src/App.jsx</code> and save to test HMR
                </p>
            </div>
            <p className="read-the-docs">
                Click on the Vite and React logos to learn more
            </p>
        </>
    )
}

export default App
