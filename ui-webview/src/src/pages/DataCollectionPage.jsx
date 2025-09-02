
import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import './DataCollectionPage.css'
import '../Header.css'
import Connector from "../components/Connector.jsx";

function DataCollectionPage({ isDark, setIsDark }) {
    const navigate = useNavigate()
    const [initialized, setInitialized] = useState(false);
    const [connectors, setConnectors] = useState([]);
    const [deleteStates, setDeleteStates] = useState({}); // Track delete confirmation states

    useEffect(() => {
        const initializeApi = async () => {
            if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
                try {
                    const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
                    const arr = JSON.parse(json);
                    setConnectors(arr);
                    setInitialized(true);
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

    const createNewConnector = async (connectorType) => {
        if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
            await window.chrome.webview.hostObjects.dataCollectionApi.CreateConnector(connectorType);

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

    const refreshConnectors = async () => {
        if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
            const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
            const arr = JSON.parse(json);
            setConnectors(arr);
        }
    }

    return (
        <div className="data-collection-container">
            <div className="header-group">
                <div className="header-section">
                    <div className="header-content">
                        <h2 className="header-title">
                            Connections
                        </h2>

                        <div className="header-buttons">
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
                <div className="header-thin-right">
                    <h2 className="header-title">
                        <button className="header-nav" onClick={() => navigate('/address-book')}>✕</button>
                    </h2>
                </div>
            </div>

            {initialized && (
                <>
                    <div className="connectors-section">
                        <div className="connectors-grid">
                            {connectors.map((connector, index) => (
                                <Connector
                                    key={index}
                                    connector={connector}
                                    onDeleteClick={handleDeleteClick}
                                    deleteState={deleteStates[connector.guid]}
                                    onConnectorUpdated={refreshConnectors}
                                />
                            ))}
                        </div>
                    </div>
                    <div className="connector-actions">
                        <button
                            onClick={() => createNewConnector('ResoniteAPI')}
                            title="Create new Resonite connection"
                        >
                            + Add Resonite connection
                        </button>
                        <button
                            onClick={() => createNewConnector('VRChatAPI')}
                            title="Create new VRChat connection"
                        >
                            + Add VRChat connection
                        </button>
                        <button
                            onClick={() => createNewConnector('Offline')}
                            title="Create offline connection"
                        >
                            + Import offline data
                        </button>
                    </div>
                </>
            )}
        </div>
    )
}

export default DataCollectionPage