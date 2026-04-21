import {useState} from 'react'
import {useNavigate} from 'react-router-dom'
import './SettingsPage.css'
import '../Header.css'
import {DotNetApi} from "../DotNetApi.ts";
import {useTranslation} from "react-i18next";
import type {Acknowledgements, AcknowledgementData} from "../types/Acknowledgements.ts";
import DarkModeToggleButton from "../components/DarkModeToggleButton.tsx";

// @ts-ignore
import acknowledgementsRaw from "../third-party-acknowledgements/third-party-acknowledgements.js";
const acknowledgements = acknowledgementsRaw as Acknowledgements;

function AcknowledgementsPage({isDark, setIsDark}: AcknowledgementsPageProps) {
    const dotNetApi = new DotNetApi();
    const {t} = useTranslation();
    const navigate = useNavigate();

    const [expandedIndex, setExpandedIndex] = useState<number | null>(null);

    const openLink = async (url: string) => {
        await dotNetApi.appApiOpenLink(url);
    };

    return (
        <div className="data-collection-container">
            <div className="header-group">
                <div className="header-section">
                    <div className="header-content">
                        <h2 className="header-title">
                            {t('settings.acknowledgements.title')}
                        </h2>

                        <DarkModeToggleButton isDark={isDark} setIsDark={setIsDark}/>
                    </div>
                </div>
                <div className="header-thin-right">
                    <h2 className="header-title">
                        <button className="header-nav" title={t('nav.backToAddressBook.title')}
                                onClick={() => navigate('/data-collection')}>✕
                        </button>
                    </h2>
                </div>
            </div>

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
                            {ack.kind === 'license' && ack.licenseData && <a onClick={() => openLink(ack.licenseData?.licenseUrl || "")}
                                                          className="link-pointer">{ack.licenseData?.licenseName}</a>}
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
    );
}

interface AcknowledgementsPageProps {
    isDark: boolean;
    setIsDark: (isDark: boolean) => void;
}

export default AcknowledgementsPage;
