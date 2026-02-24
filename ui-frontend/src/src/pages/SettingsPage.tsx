import {useEffect, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import './SettingsPage.css'
import '../Header.css'
import Connector from "../components/Connector.tsx";
import DarkModeToggleButton from "../components/DarkModeToggleButton.tsx";
import {ConnectorType, type ConnectorTypeType, type FrontConnector, type FrontProgressTracker} from "../types/ConnectorTypes.ts";
import type {DebugFlags} from "../types/DebugFlags.ts";
import {DotNetApi} from "../DotNetApi.ts";
import {useTranslation} from "react-i18next";
import {availableLanguages} from "../i18n.ts";
import type {LanguageInfo} from "../types/AvailableLang.ts";
import type {Acknowledgements, AcknowledgementData} from "../types/Acknowledgements.ts";

// @ts-ignore
import acknowledgementsRaw from "../third-party-acknowledgements/third-party-acknowledgements.js";
const acknowledgements = acknowledgementsRaw as Acknowledgements;

interface SettingsPageProps {
    isDark: boolean,
    setIsDark: (isDark: boolean) => void,
    debugMode: DebugFlags,
    setLang: (lang: string) => void,
    resoniteShowSubSessions: boolean,
    setResoniteShowSubSessions: (resoniteShowSubSessions: boolean) => void,
    deprioritizedVirtualSpaceNames: string[],
    setDeprioritizedVirtualSpaceNames: (deprioritizedVirtualSpaceNames: string[]) => void
}

interface DeleteStateType {
    confirming: boolean;
    firstClick: number;
}

function SettingsPage({
                          isDark,
                          setIsDark,
                          debugMode,
                          setLang,
                          resoniteShowSubSessions,
                          setResoniteShowSubSessions,
                          deprioritizedVirtualSpaceNames,
                          setDeprioritizedVirtualSpaceNames
                      }: SettingsPageProps) {
    const dotNetApi = new DotNetApi();
    const {t} = useTranslation();

    const navigate = useNavigate()
    const [initialized, setInitialized] = useState(false);
    const [connectors, setConnectors] = useState<FrontConnector[]>([]);
    const [deleteStates, setDeleteStates] = useState<{ [key: string]: DeleteStateType }>({});
    const [dataCollectionProgress, setDataCollectionProgress] = useState<FrontProgressTracker | null>(null);
    const [newDeprioritizedName, setNewDeprioritizedName] = useState("");
    const [expandedIndex, setExpandedIndex] = useState<number | null>(null);

    useEffect(() => {
        const initializeApi = async () => {
            const json = await dotNetApi.dataCollectionApiGetConnectors();
            const arr: FrontConnector[] = JSON.parse(json);
            setConnectors(arr);

            const json2 = await dotNetApi.dataCollectionApiGetCurrentDataCollectionProgress();
            const progress: FrontProgressTracker|null = JSON.parse(json2);
            setDataCollectionProgress(progress);

            setInitialized(true);
        };
        const dataCollectionUpdated = (event: any) => {
            console.log('Data collection updated event:', event.detail);
            const currentProgress: FrontProgressTracker | null = event.detail;
            setDataCollectionProgress(currentProgress);
        }

        window.addEventListener('dataCollectionUpdated', dataCollectionUpdated);
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

    const openLink = async (url: string) => {
        await dotNetApi.appApiOpenLink(url);
    };

    const addDeprioritizedName = () => {
        if (newDeprioritizedName && !deprioritizedVirtualSpaceNames.includes(newDeprioritizedName)) {
            setDeprioritizedVirtualSpaceNames([...deprioritizedVirtualSpaceNames, newDeprioritizedName]);
            setNewDeprioritizedName("");
        }
    };

    const removeDeprioritizedName = (name: string) => {
        setDeprioritizedVirtualSpaceNames(deprioritizedVirtualSpaceNames.filter(n => n !== name));
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
                                    <button
                                        onClick={() => createNewConnector(ConnectorType.ClusterAPI)}
                                        title={t('connectors.addConnection.title', {connectionName: 'cluster.mu'})}
                                    >
                                        + {t('connectors.addConnection.label', {connectionName: 'cluster.mu'})}
                                    </button>
                                    <button
                                        onClick={() => createNewConnector(ConnectorType.GenericServerViewer)}
                                        title={t('connectors.addServerViewer.title')}
                                    >
                                        + {t('connectors.addServerViewer.label')}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </>
            )}

            <div className="settings-buttons">
                <a className="link-pointer" title={t('settings.privacy.link.title')}
                    onClick={openPrivacyDocs} onAuxClick={(e) => e.button === 1 && openPrivacyDocs()}
                    onMouseDown={(e) => e.preventDefault()}>{t('settings.privacy.link.label')}</a>
                <button
                    disabled={!(initialized && dataCollectionProgress == null)}
                    onClick={() => startDataCollection()}
                    title={t('dataCollection.start.title')}
                >
                    {dataCollectionProgress != null ? 
                        t('settings.dataCollection.collecting', {name: dataCollectionProgress.name, accomplished: dataCollectionProgress.accomplished, total: dataCollectionProgress.total})
                        : t('dataCollection.start.label')}
                </button>
            </div>

            <h2>{t('settings.resonite.title')}</h2>
            <div className="settings-section">
                <label className="checkbox-container">
                    <input
                        type="checkbox"
                        checked={resoniteShowSubSessions ?? true}
                        onChange={(e) => setResoniteShowSubSessions(e.target.checked)}
                    />
                    {t('settings.resonite.showSubSessions.title')}
                </label>
            </div>

            <h2>{t('settings.deprioritized.title')}</h2>
            <div className="settings-section">
                <div className="deprioritized-sessions-list">
                    {deprioritizedVirtualSpaceNames.map(name => (
                        <div key={name} className="deprioritized-session-item">
                            <span>{name}</span>
                            <button onClick={() => removeDeprioritizedName(name)}>✕</button>
                        </div>
                    ))}
                </div>
                <div className="deprioritized-sessions-add">
                    <input
                        type="text"
                        value={newDeprioritizedName}
                        onChange={(e) => setNewDeprioritizedName(e.target.value)}
                        placeholder={t('settings.deprioritized.add.placeholder')}
                        onKeyDown={(e) => e.key === 'Enter' && addDeprioritizedName()}
                    />
                    <button onClick={addDeprioritizedName}>{t('settings.deprioritized.add.button')}</button>
                </div>
            </div>

            <h2>{t('settings.languages.title')}</h2>
            <div className="settings-buttons">
                {availableLanguages.availableLanguages.map((lang: LanguageInfo) => (
                    <button key={lang.code} title={lang.englishName}
                            onClick={() => setLang(lang.code)}>{lang.displayName}</button>
                ))}
            </div>

            <h2>{t('settings.acknowledgements.title')}</h2>
            <div className="acknowledgements-list">
                {acknowledgements.data.map((ack: AcknowledgementData, index: number) => (
                    <div key={index} className="acknowledgement-item">
                        <div className="acknowledgement-header">
                            <h3 className="acknowledgement-title">
                                <a onClick={() => openLink(ack.url)} className="link-pointer">{ack.title}</a>
                            </h3>
                            {ack.kind === 'license' && ack.licenseData && (
                                <button onClick={() => setExpandedIndex(expandedIndex === index ? null : index)}>
                                    {t('settings.acknowledgements.license.button')}
                                </button>
                            )}
                        </div>
                        <div className="acknowledgement-meta">
                            {ack.maintainer && ack.maintainerUrl && (
                                <div>
                                    {t('settings.acknowledgements.maintainer', {name: ''})}
                                    <a onClick={() => openLink(ack.maintainerUrl!)}
                                       className="link-pointer">{ack.maintainer}</a>
                                </div>
                            )}
                        </div>
                        <div className="acknowledgement-reasons">
                            {ack.reasons.map((reason, i) => (
                                <p key={i}>{reason}</p>
                            ))}
                        </div>
                        <div className="acknowledgement-meta">
                            {ack.integratedIntoXYVRby && (
                                <div>
                                    {t('settings.acknowledgements.integratedBy', {name: ''})}
                                    <a onClick={() => openLink(`https://${ack.integratedIntoXYVRby}`)}
                                       className="link-pointer">{ack.integratedIntoXYVRby}</a>
                                </div>
                            )}
                        </div>
                        {ack.kind === 'license' && ack.licenseData && expandedIndex === index && (
                            <div className="acknowledgement-license-content">
                                {ack.licenseData.licenseFullText}
                            </div>
                        )}
                    </div>
                ))}
            </div>

        </div>
    )
}

export default SettingsPage
