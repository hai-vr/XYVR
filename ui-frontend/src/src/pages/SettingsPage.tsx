import {useEffect, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import './SettingsPage.css'
import '../Header.css'
import Connector from "../components/Connector.tsx";
import DarkModeToggleButton from "../components/DarkModeToggleButton.tsx";
import {ConnectorType, type ConnectorTypeType, type FrontConnector} from "../types/ConnectorTypes.ts";
import type {DebugFlags} from "../types/DebugFlags.ts";
import {DotNetApi} from "../DotNetApi.ts";
import {useTranslation} from "react-i18next";
import {availableLanguages} from "../i18n.ts";
import type {LanguageInfo} from "../types/AvailableLang.ts";

interface SettingsPageProps {
    isDark: boolean,
    setIsDark: (isDark: boolean) => void,
    debugMode: DebugFlags,
    setLang: (lang: string) => void
}

interface DeleteStateType {
    confirming: boolean;
    firstClick: number;
}

function SettingsPage({isDark, setIsDark, debugMode, setLang}: SettingsPageProps) {
    const dotNetApi = new DotNetApi();
    const {t} = useTranslation();

    const navigate = useNavigate()
    const [initialized, setInitialized] = useState(false);
    const [connectors, setConnectors] = useState<FrontConnector[]>([]);
    const [deleteStates, setDeleteStates] = useState<{ [key: string]: DeleteStateType }>({});

    useEffect(() => {
        const initializeApi = async () => {
            const json = await dotNetApi.dataCollectionApiGetConnectors();
            const arr: FrontConnector[] = JSON.parse(json);
            setConnectors(arr);
            setInitialized(true);
        };

        initializeApi();
    }, []);

    const createNewConnector = async (connectorType: ConnectorTypeType) => {
        await dotNetApi.dataCollectionApiCreateConnector(connectorType);

        const json = await dotNetApi.dataCollectionApiGetConnectors();
        const arr: FrontConnector[] = JSON.parse(json);
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
                const newStates = {...prev};
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
                    const newStates = {...prev};
                    if (newStates[guid] && newStates[guid].firstClick === currentTime) {
                        delete newStates[guid];
                    }
                    return newStates;
                });
            }, 2000);
        }
    };

    const deleteConnector = async (guid: string) => {
        await dotNetApi.dataCollectionApiDeleteConnector(guid);

        const json = await dotNetApi.dataCollectionApiGetConnectors();
        const arr: FrontConnector[] = JSON.parse(json);
        setConnectors(arr);
    }

    const refreshConnectors = async () => {
        const json = await dotNetApi.dataCollectionApiGetConnectors();
        const arr: FrontConnector[] = JSON.parse(json);
        setConnectors(arr);
    }

    const startDataCollection = async () => {
        await dotNetApi.dataCollectionApiStartDataCollection();
    }

    const openPrivacyDocs = async () => {
        await dotNetApi.appApiOpenLink('https://docs.hai-vr.dev/docs/xyvr/privacy');
    };

    return (
        <div className="data-collection-container">
            <div className="header-group">
                <div className="header-section">
                    <div className="header-content">
                        <h2 className="header-title">
                            {t('section.connections')}
                        </h2>

                        <DarkModeToggleButton isDark={isDark} setIsDark={setIsDark}/>
                    </div>
                </div>
                <div className="header-thin-right">
                    <h2 className="header-title">
                        <button className="header-nav" title={t('nav.backToAddressBook.title')}
                                onClick={() => navigate('/address-book')}>✕
                        </button>
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
                            <div className="connector-card">
                                <div className="connector-add">
                                    <button
                                        onClick={() => createNewConnector(ConnectorType.ResoniteAPI)}
                                        title={t('connectors.addConnection.title', {connectionName: 'Resonite'})}
                                    >
                                        + {t('connectors.addConnection.label', {connectionName: 'Resonite'})}
                                    </button>
                                    <button
                                        onClick={() => createNewConnector(ConnectorType.VRChatAPI)}
                                        title={t('connectors.addConnection.title', {connectionName: 'VRChat'})}
                                    >
                                        + {t('connectors.addConnection.label', {connectionName: 'VRChat'})}
                                    </button>
                                    <button
                                        onClick={() => createNewConnector(ConnectorType.ChilloutVRAPI)}
                                        title={t('connectors.addConnection.title', {connectionName: 'ChilloutVR'})}
                                    >
                                        + {t('connectors.addConnection.label', {connectionName: 'ChilloutVR'})}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </>
            )}

            <div className="settings-buttons">
                <a className="link-pointer" title="Open privacy and data considerations docs in your browser" onClick={openPrivacyDocs} onAuxClick={(e) => e.button === 1 && openPrivacyDocs()} onMouseDown={(e) => e.preventDefault()}>Learn more about our privacy considerations.</a>
                <button
                    onClick={() => startDataCollection()}
                    title={t('dataCollection.start.title')}
                >
                    {t('dataCollection.start.label')}
                </button>
            </div>

            <h2>Languages</h2>
            <div className="settings-buttons">
                {availableLanguages.availableLanguages.map((lang: LanguageInfo) => (
                    <button key={lang.code} title={lang.englishName} onClick={() => setLang(lang.code)}>{lang.displayName}</button>
                ))}
            </div>
        </div>
    )
}

export default SettingsPage