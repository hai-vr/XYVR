﻿import {useState} from 'react';
import Account from './Account.tsx';
import './Connector.css';
import '../InputFields.css';
import {TriangleAlert, X} from "lucide-react";
import {type FrontAccount, NamedApp} from "../types/CoreTypes.ts";
import {ConnectorType, type FrontConnector, type FrontConnectorAccount} from "../types/ConnectorTypes.ts";
import {type DebugFlags, DemonstrationMode, DISABLED_DEBUG_FLAGS} from "../types/DebugFlags.ts";
import {DotNetApi} from "../DotNetApi.ts";
import {
    type ConnectionAttemptResult,
    ConnectionAttemptResultType
} from "../types/ConnectionAttemptTypes.ts";
import {useTranslation} from "react-i18next";

interface DeleteState {
    confirming: boolean;
    firstClick?: number;
}

interface ConnectorProps {
    connector: FrontConnector;
    onDeleteClick: (guid: string) => void;
    deleteState?: DeleteState;
    onConnectorUpdated: () => void;
    debugMode: DebugFlags;
}

const Connector = ({ connector, onDeleteClick, deleteState, onConnectorUpdated, debugMode } : ConnectorProps) => {
    const dotNetApi = new DotNetApi();
    const { t } = useTranslation();

    const [login, setLogin] = useState('');
    const [password, setPassword] = useState('');
    const [twoFactorCode, setTwoFactorCode] = useState('');
    const [isTwoFactorEmail, setIsTwoFactorEmail] = useState(false);
    const [stayLoggedIn, setStayLoggedIn] = useState(true);
    const [useAccessKey, setUseAccessKey] = useState(false);
    const [isInTwoFactorMode, setIsInTwoFactorMode] = useState(false);
    const [isRequestInProgress, setIsRequestInProgress] = useState(false);

    let virtualApp = connector.type === ConnectorType.VRChatAPI && NamedApp.VRChat
        || connector.type === ConnectorType.ResoniteAPI && NamedApp.Resonite
        || connector.type === ConnectorType.ChilloutVRAPI && NamedApp.ChilloutVR
        || NamedApp.NotNamed;

    const tempAccount: FrontConnectorAccount = {
        inAppDisplayName: `Adding a new ${virtualApp == NamedApp.NotNamed && 'Offline' || virtualApp} connection...`,
        qualifiedAppName: 'internal.imposter',
        inAppIdentifier: '???',
        namedApp: virtualApp,
    };

    const tryLogin = async () => {
        setIsRequestInProgress(true);
        if (!isInTwoFactorMode) {
            const json = await dotNetApi.dataCollectionApiTryLogin(connector.guid, login, password, stayLoggedIn);
            setIsRequestInProgress(false);
            const obj: ConnectionAttemptResult = JSON.parse(json);
            if (obj.type === ConnectionAttemptResultType.NeedsTwoFactorCode) {
                setIsInTwoFactorMode(true);
                setIsTwoFactorEmail(obj.isTwoFactorEmail)
                setLogin('');
                setPassword('');
            }
            else if (obj.type === ConnectionAttemptResultType.Success) {
                setIsInTwoFactorMode(false);
                setLogin('');
                setPassword('');
                setIsTwoFactorEmail(false)
                setTwoFactorCode('')
                connector.isLoggedIn = true; // Prevents the input fields from being shown until the parent fetches the connector state
                onConnectorUpdated();
            }
        }
        else {
            const json = await dotNetApi.dataCollectionApiTryTwoFactor(connector.guid, isTwoFactorEmail, twoFactorCode, stayLoggedIn);
            setIsRequestInProgress(false);
            const obj: ConnectionAttemptResult = JSON.parse(json);
            if (obj.type === ConnectionAttemptResultType.Success) {
                setIsInTwoFactorMode(false);
                setLogin('');
                setPassword('');
                setTwoFactorCode('')
                connector.isLoggedIn = true; // Prevents the input fields from being shown until the parent fetches the connector state
                onConnectorUpdated();
            }
        }
    }

    const tryLogout = async () => {
        setIsRequestInProgress(true);
        const json = await dotNetApi.dataCollectionApiTryLogout(connector.guid);
        setIsRequestInProgress(false);
        const obj: ConnectionAttemptResult = JSON.parse(json);
        if (obj.type === ConnectionAttemptResultType.LoggedOut) {
            connector.isLoggedIn = false;
            onConnectorUpdated();
        }
    }

    const openPrivacyDocs = async () => {
        await dotNetApi.appApiOpenLink('https://docs.hai-vr.dev/docs/xyvr/privacy');
    };

    return (
        <div className="connector-card">
            {connector.account && (
                <Account account={connector.account as FrontAccount} debugMode={debugMode} imposter={false} showAlias={false} showNotes={false} isSessionView={false} />
            )}
            {!connector.account && (
                <Account account={tempAccount as FrontAccount} debugMode={DISABLED_DEBUG_FLAGS} imposter={true} showAlias={false} showNotes={false} isSessionView={false} />
            )}

            {!connector.isLoggedIn && (
                <div className="input-fields">
                    {(connector.type !== 'Offline' && !isInTwoFactorMode) && (
                        <>
                            <h3 className="input-title">Connect to your {virtualApp} account</h3>
                            {connector.type === ConnectorType.ChilloutVRAPI && (<div className="pill-toggle">
                                <button
                                    type="button"
                                    className={`pill-option ${!useAccessKey ? 'active' : ''}`}
                                    onClick={() => setUseAccessKey(false)}
                                    disabled={isRequestInProgress}
                                    title={t('connector.email.title')}
                                    aria-label={t('connector.email.label')}
                                >
                                    {t('connector.email.label')}
                                </button>
                                <button
                                    type="button"
                                    className={`pill-option ${useAccessKey ? 'active' : ''}`}
                                    onClick={() => setUseAccessKey(true)}
                                    disabled={isRequestInProgress}
                                    title={t('connector.accessKey.title')}
                                    aria-label={t('connector.accessKey.label')}
                                >
                                    {t('connector.accessKey.label')}
                                </button>
                            </div>)}
                            <input
                                type={debugMode.demoMode !== DemonstrationMode.Disabled && 'password' || 'text'}
                                placeholder={connector.type === ConnectorType.VRChatAPI && "Username/Email" || connector.type === ConnectorType.ChilloutVRAPI && "Email" || "Username"}
                                value={login}
                                onChange={(e) => setLogin(e.target.value)}
                                className="login-input"
                                disabled={isRequestInProgress}
                            />
                            <input
                                type="password"
                                placeholder="Password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                className="password-input"
                                disabled={isRequestInProgress}
                            />
                            <label className="checkbox-container">
                                <input
                                    type="checkbox"
                                    checked={stayLoggedIn}
                                    onChange={(e) => setStayLoggedIn(e.target.checked)}
                                    disabled={isRequestInProgress}
                                />
                                Stay logged in
                            </label>
                            {connector.type === 'ResoniteAPI' && login.toLowerCase().startsWith('u-')
                                && <p className="warning-message">
                                    <span className="warning-icon">⚠️</span>
                                    Please enter your username; not your user ID.
                                </p>
                            }
                            {connector.type === ConnectorType.ResoniteAPI && /.@./.test(login)
                                && <p className="warning-message">
                                    <span className="warning-icon">⚠️</span>
                                    Please enter your username; not your email address.<br/>
                                    If your username has a @ symbol in it, ignore this message and continue to login.
                                </p>
                            }
                            <button title={t('connector.login.title', { app: virtualApp })} onClick={() => tryLogin()} disabled={!login || !password || isRequestInProgress}>{t('connector.login.label', { app: virtualApp })}</button>
                            <p className="info-message">
                                {stayLoggedIn && connector.type === ConnectorType.ResoniteAPI && 'This application does not store your email and password, only a connection token that expires in 30 days. '}
                                {stayLoggedIn && connector.type === ConnectorType.VRChatAPI && 'This application does not store your password, only a cookie. '}
                                {stayLoggedIn && connector.type === ConnectorType.ChilloutVRAPI && 'This application does not store your email and password, only an authentication key. '}
                                {connector.type === ConnectorType.ResoniteAPI && <>All data is stored locally. Requests are sent directly to the Resonite API from your machine. XYVR does not operate any server, and data is never transmitted to a third-party. </>}
                                {connector.type === ConnectorType.VRChatAPI && <>All data is stored locally. Requests are sent directly to the VRChat API from your machine. XYVR does not operate any server, and data is never transmitted to a third-party. </>}
                                {connector.type === ConnectorType.ChilloutVRAPI && <>All data is stored locally. Requests are sent directly to the ChilloutVR API from your machine. XYVR does not operate any server, and data is never transmitted to a third-party. </>}
                                <a className="link-pointer" title="Open privacy and data considerations docs in your browser" onClick={openPrivacyDocs} onAuxClick={(e) => e.button === 1 && openPrivacyDocs()} onMouseDown={(e) => e.preventDefault()}>Learn more about our privacy considerations.</a>
                            </p>

                        </>
                    )}
                    {isInTwoFactorMode && (
                        <>
                            <h3 className="input-title">{t('connector.enter2fa.title', { app: virtualApp, method: isTwoFactorEmail ? 'Email' : 'Authenticator' })}</h3>
                            <input
                                type={debugMode.demoMode !== DemonstrationMode.Disabled && 'password' || 'text'}
                                placeholder={t('connector.enter2fa.placeholder', { method: isTwoFactorEmail ? 'Email' : 'Authenticator' })}
                                value={twoFactorCode}
                                onChange={(e) => setTwoFactorCode(e.target.value)}
                                className="login-input"
                                disabled={isRequestInProgress}
                            />
                            <button title={t('connector.confirm.title')} onClick={() => tryLogin()} disabled={!twoFactorCode || isRequestInProgress}>{t('connector.confirm.label')}</button>
                        </>
                    )}
                </div>
            )}

            <div className="connector-actions">
                {connector.isLoggedIn && (
                    <button title={t('connection.logOut.title')} className="delete-button" onClick={() => tryLogout()} disabled={isRequestInProgress}>{t('connection.logOut.label')}</button>
                )}
                <button
                    disabled={isRequestInProgress || !isInTwoFactorMode && (!!login || !!password) || isInTwoFactorMode && !!twoFactorCode}
                    className={`delete-button ${deleteState?.confirming ? '' : ''}`}
                    onClick={() => onDeleteClick(connector.guid)}
                    title={deleteState?.confirming ? t('connection.removeConfirm.title') : t('connection.remove.title')}
                >
                    {deleteState?.confirming ? <><TriangleAlert /> {t('connection.removeConfirm.label')}</> : <><X /><span>{t('connection.remove.label')}</span></>}
                </button>
            </div>
        </div>
    );
};

export default Connector;
