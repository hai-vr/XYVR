import React, { useState } from 'react';
import Account from './Account.jsx';
import './Connector.css';
import '../InputFields.css';

const Connector = ({ connector, onDeleteClick, deleteState, onConnectorUpdated }) => {
    const [login, setLogin] = useState('');
    const [password, setPassword] = useState('');
    const [twoFactorCode, setTwoFactorCode] = useState('');
    const [stayLoggedIn, setStayLoggedIn] = useState(true);
    const [isInTwoFactorMode, setIsInTwoFactorMode] = useState(false);
    const [isRequestInProgress, setIsRequestInProgress] = useState(false);

    let virtualApp = connector.type === 'VRChatAPI' && 'VRChat'
        || connector.type === 'ResoniteAPI' && 'Resonite'
        || 'Offline';

    const tempAccount = {
        inAppDisplayName: `Adding a new ${virtualApp} connection...`,
        inAppIdentifier: '???',
        namedApp: virtualApp,
        isTechnical: false
    };

    const tryLogin = async () => {
        if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
            setIsRequestInProgress(true);
            if (!isInTwoFactorMode) {
                const json = await window.chrome.webview.hostObjects.dataCollectionApi.TryLogin(connector.guid, login, password, stayLoggedIn);
                setIsRequestInProgress(false);
                const obj = JSON.parse(json);
                if (obj.type === 'NeedsTwoFactorCode') {
                    setIsInTwoFactorMode(true);
                    setLogin('');
                    setPassword('');
                }
                else if (obj.type === 'Success') {
                    setIsInTwoFactorMode(false);
                    setLogin('');
                    setPassword('');
                    setTwoFactorCode('')
                    connector.isLoggedIn = true; // Prevents the input fields from being shown until the parent fetches the connector state
                    onConnectorUpdated();
                }
            }
            else {
                const json = await window.chrome.webview.hostObjects.dataCollectionApi.TryTwoFactor(connector.guid, twoFactorCode, stayLoggedIn);
                setIsRequestInProgress(false);
                const obj = JSON.parse(json);
                if (obj.type === 'Success') {
                    setIsInTwoFactorMode(false);
                    setLogin('');
                    setPassword('');
                    setTwoFactorCode('')
                    connector.isLoggedIn = true; // Prevents the input fields from being shown until the parent fetches the connector state
                    onConnectorUpdated();
                }
            }
        }
    }

    const tryLogout = async () => {
        if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
            setIsRequestInProgress(true);
            const json = await window.chrome.webview.hostObjects.dataCollectionApi.TryLogout(connector.guid);
            setIsRequestInProgress(false);
            const obj = JSON.parse(json);
            if (obj.type === 'LoggedOut') {
                connector.isLoggedIn = false;
                onConnectorUpdated();
            }
        }
    }

    return (
        <div className="connector-card">
            {connector.account && (
                <Account account={connector.account} />
            )}
            {!connector.account && (
                <Account account={tempAccount} imposter={true} />
            )}

            {!connector.isLoggedIn && (
                <div className="input-fields">
                    {(connector.type !== 'Offline' && !isInTwoFactorMode) && (
                        <>
                            <h3 className="input-title">Connect to your {virtualApp} account</h3>
                            <input
                                type="text"
                                placeholder={connector.type === 'VRChatAPI' && "Username/Email" || "Username"}
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
                            <button title="Login" onClick={() => tryLogin()} disabled={!login || !password || isRequestInProgress}>Login</button>
                            {stayLoggedIn && <>{connector.type === 'ResoniteAPI' && <p className="info-message">We do not store your username and password, only a connection token that expires in 30 days.</p>
                                || <p className="info-message">We do not store your username and password, only a cookie.</p>}</>}

                        </>
                    )}
                    {isInTwoFactorMode && (
                        <>
                            <h3 className="input-title">Enter your {virtualApp} 2FA code</h3>
                            <input
                                type="text"
                                placeholder="2FA Code"
                                value={twoFactorCode}
                                onChange={(e) => setTwoFactorCode(e.target.value)}
                                className="login-input"
                                disabled={isRequestInProgress}
                            />
                            <button title="Confirm" onClick={() => tryLogin()} disabled={!twoFactorCode || isRequestInProgress}>Confirm</button>
                        </>
                    )}
                </div>
            )}
            {connector.isLoggedIn && (
                <div className="input-fields">
                    <button title="Confirm" onClick={() => tryLogout()} disabled={isRequestInProgress}>Log out</button>
                </div>
            )}

            <div className="connector-actions">
                <button
                    disabled={isRequestInProgress || !isInTwoFactorMode && (login || password) || isInTwoFactorMode && twoFactorCode}
                    className={`delete-button ${deleteState?.confirming ? '' : ''}`}
                    onClick={() => onDeleteClick(connector.guid)}
                    title={deleteState?.confirming ? 'Click again to confirm remove' : 'Remove connector'}
                >
                    {deleteState?.confirming ? '⚠️ Really remove?' : '❌ Remove'}
                </button>
            </div>
        </div>
    );
};

export default Connector;
