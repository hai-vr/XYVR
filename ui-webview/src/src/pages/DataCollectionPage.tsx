import {useEffect, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import './DataCollectionPage.css'
import '../Header.css'
import Connector from "../components/Connector.tsx";
import DarkModeToggleButton from "../components/DarkModeToggleButton.tsx";
import {ConnectorType, type ConnectorTypeType, type FrontConnector} from "../types/ConnectorTypes.ts";
import type {DebugFlags} from "../types/DebugFlags.ts";

interface DataCollectionPageProps {
    isDark: boolean;
    setIsDark: (isDark: boolean) => void;
    debugMode: DebugFlags;
}

interface DeleteStateType {
    confirming: boolean;
    firstClick: number;
}

function DataCollectionPage({ isDark, setIsDark, debugMode }: DataCollectionPageProps) {
    const navigate = useNavigate()
    const [initialized, setInitialized] = useState(false);
    const [connectors, setConnectors] = useState<FrontConnector[]>([]);
    const [deleteStates, setDeleteStates] = useState<{ [key: string]: DeleteStateType }>({});

    useEffect(() => {
        const initializeApi = async () => {
            const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
            const arr = JSON.parse(json);
            setConnectors(arr);
            setInitialized(true);
        };
        
        initializeApi();
    }, []);

    const createNewConnector = async (connectorType: ConnectorTypeType) => {
        await window.chrome.webview.hostObjects.dataCollectionApi.CreateConnector(connectorType);

        const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
        const arr = JSON.parse(json);
        setConnectors(arr);
    }

    const handleDeleteClick = (guid: string) => {
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

    const deleteConnector = async (guid: string) => {
        await window.chrome.webview.hostObjects.dataCollectionApi.DeleteConnector(guid);

        const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
        const arr = JSON.parse(json);
        setConnectors(arr);
    }

    const refreshConnectors = async () => {
        const json = await window.chrome.webview.hostObjects.dataCollectionApi.GetConnectors();
        const arr = JSON.parse(json);
        setConnectors(arr);
    }

    const startDataCollection = async () => {
        await window.chrome.webview.hostObjects.dataCollectionApi.StartDataCollection();
    }

    return (
        <div className="data-collection-container">
            <div className="header-group">
                <div className="header-section">
                    <div className="header-content">
                        <h2 className="header-title">
                            Connections
                        </h2>

                        <DarkModeToggleButton isDark={isDark} setIsDark={setIsDark} />
                    </div>
                </div>
                <div className="header-thin-right">
                    <h2 className="header-title">
                        <button className="header-nav" title="Back to address book" onClick={() => navigate('/address-book')}>✕</button>
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
                                    debugMode={debugMode}
                                />
                            ))}
                        </div>
                    </div>
                    <div className="connector-actions">
                        <button
                            onClick={() => createNewConnector(ConnectorType.ResoniteAPI)}
                            title="Create new Resonite connection"
                        >
                            + Add Resonite connection
                        </button>
                        <button
                            onClick={() => createNewConnector(ConnectorType.VRChatAPI)}
                            title="Create new VRChat connection"
                        >
                            + Add VRChat connection
                        </button>
                        <button
                            onClick={() => createNewConnector(ConnectorType.ChilloutVRAPI)}
                            title="Create new ChilloutVR connection"
                        >
                            + Add ChilloutVR connection
                        </button>
                        <button
                            onClick={() => createNewConnector(ConnectorType.Offline)}
                            title="Create offline connection"
                        >
                            + Import offline data
                        </button>
                    </div>
                </>
            )}

            <button
                onClick={() => startDataCollection()}
                title="Start data collection"
            >
                Start data collection
            </button>
        </div>
    )
}

export default DataCollectionPage