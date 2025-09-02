
import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import './DataCollectionPage.css'
import Account from "../components/Account.jsx";

function DataCollectionPage() {
    const navigate = useNavigate()
    const [isDark, setIsDark] = useState(false)
    const [connectors, setConnectors] = useState([]);
    const [deleteStates, setDeleteStates] = useState({}); // Track delete confirmation states

    // Separate useEffect for theme changes
    useEffect(() => {
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
    }, [isDark]);

    useEffect(() => {
        const initializeApi = async () => {
            if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
                try {
                    const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
                    const arr = JSON.parse(json);
                    setConnectors(arr);
                } catch (error) {
                    console.error('API not ready yet:', error);
                }
            }
        };
        
        initializeApi();

        // Also listen for when the DOM is fully loaded
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initializeApi);
        }

        return () => {
            document.removeEventListener('DOMContentLoaded', initializeApi);
        };
    }, []);

    const createNewConnector = async () => {
        if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
            await window.chrome.webview.hostObjects.dataCollectionApi.CreateConnector();

            const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
            const arr = JSON.parse(json);
            setConnectors(arr);
        }
    }

    const handleDeleteClick = (guid) => {
        const currentTime = Date.now();
        const deleteState = deleteStates[guid];

        if (deleteState && currentTime - deleteState.firstClick < 2000) {
            // Second click within 2 seconds - actually delete
            deleteConnector(guid);
            // Reset the delete state
            setDeleteStates(prev => {
                const newStates = { ...prev };
                delete newStates[guid];
                return newStates;
            });
        } else {
            // First click or click after timeout - set up confirmation state
            setDeleteStates(prev => ({
                ...prev,
                [guid]: {
                    firstClick: currentTime,
                    confirming: true
                }
            }));

            // Clear the confirmation state after 2 seconds
            setTimeout(() => {
                setDeleteStates(prev => {
                    const newStates = { ...prev };
                    if (newStates[guid] && newStates[guid].firstClick === currentTime) {
                        delete newStates[guid];
                    }
                    return newStates;
                });
            }, 2000);
        }
    };

    const deleteConnector = async (guid) => {
        if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
            await window.chrome.webview.hostObjects.dataCollectionApi.DeleteConnector(guid);

            const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
            const arr = JSON.parse(json);
            setConnectors(arr);
        }
    }

    return (
        <div className="data-collection-container">
            <div className="header-section">
                <div className="header-content">
                    <h2 className="header-title">
                        Data Collection
                    </h2>

                    <div className="header-buttons">
                        <button
                            className="data-collection-btn"
                            onClick={() => navigate('/address-book')}
                            title="Go to Address Book"
                        >
                            Back
                        </button>
                        
                        <button
                            className="theme-toggle-btn"
                            onClick={() => setIsDark(!isDark)}
                            title={`Switch to ${isDark ? 'Light' : 'Dark'} Mode`}
                        >
                            {isDark ? '🌙' : '☀️'}
                        </button>
                    </div>
                </div>
            </div>

            {connectors && connectors.length > 0 && (
                <div className="connectors-section">
                    <div className="connectors-grid">
                        {connectors.map((connector, index) => (
                            <div key={index} className="connector-card">
                                {connector.account && (
                                    <Account account={connector.account} />
                                )}

                                <div className="connector-actions">
                                    <button
                                        className="connector-action-btn"
                                        title="Update"
                                    >
                                        📋 Update TODO
                                    </button>
                                    <button
                                        className={`connector-action-btn delete-btn ${deleteStates[connector.guid]?.confirming ? 'confirming' : ''}`}
                                        onClick={() => handleDeleteClick(connector.guid)}
                                        title={deleteStates[connector.guid]?.confirming ? 'Click again to confirm delete' : 'Delete connector (requires double-click)'}
                                    >
                                        {deleteStates[connector.guid]?.confirming ? '⚠️ Really remove?' : '🗑️ Remove'}
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}
            <div className="connector-actions">
                <button
                    className="connector-action-btn"
                    onClick={() => createNewConnector()}
                    title="Create new connection"
                >
                    + Create new connection
                </button>
            </div>
        </div>
    )
}

export default DataCollectionPage