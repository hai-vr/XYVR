import React from 'react';
import './Account.css';

const Account = ({ account }) => {
    const hasNote = account.isAnyCallerNote;

    const copyInAppIdentifier = async () => {
        await navigator.clipboard.writeText(account.inAppIdentifier);
    };

    const getAppIconClass = (namedApp) => {
        switch (namedApp) {
            case "Resonite":
                return "app-icon resonite";
            case "VRChat":
                return "app-icon vrchat";
            case "Cluster":
                return "app-icon cluster";
            case "ChilloutVR":
                return "app-icon chilloutvr";
            default:
                return "app-icon default";
        }
    };

    const getAppIcon = (namedApp) => {
        switch (namedApp) {
            case "Resonite":
                return '⚡';
            case "VRChat":
                return '💬';
            case "Cluster":
                return '☁️';
            case "ChilloutVR":
                return '🌆';
            default:
                return '❓';
        }
    };

    const getAppDisplayName = (account) => {
        switch (account.namedApp) {
            case "Resonite":
                return 'Resonite';
            case "VRChat":
                return 'VRChat';
            case "Cluster":
                return `Cluster (@${account.inAppIdentifier})`;
            case "ChilloutVR":
                return 'ChilloutVR';
            default:
                return account.qualifiedAppName;
        }
    };

    return (
        <div className="account-container">
            <div className="account-header">
                <div className="account-info">
                    <div className={getAppIconClass(account.namedApp)}>
                        {getAppIcon(account.namedApp)}
                    </div>
                    <div>
                        <div className="account-display-name">
                            {account.inAppDisplayName}
                        </div>
                        <div className="account-app-name">
                            {getAppDisplayName(account)}
                        </div>
                    </div>
                </div>
                <div className="account-badges">
                    {!account.isAnyCallerContact && hasNote && (
                        <span className="badge note">
                            📝 Note
                        </span>
                    )}
                    {account.isAnyCallerContact && (
                        <span className="badge contact">
                            {account.namedApp === "VRChat" || account.namedApp === "ChilloutVR" ? 'Friend' : 'Contact'}
                        </span>
                    )}
                    {account.isTechnical && (
                        <span className="badge bot">
                            Bot
                        </span>
                    )}
                    {(account.namedApp === "VRChat" || account.namedApp === "ChilloutVR") && (
                        <a
                            href={`${account.namedApp === "VRChat" && 'https://vrchat.com/home/user/' || 'https://hub.chilloutvr.net/social/profile?guid='}${account.inAppIdentifier}`}
                            rel="noopener noreferrer"
                            className="any-button"
                            title={`Open ${account.namedApp} Profile`}
                        >
                            🌍
                        </a>
                    )}
                    <button
                        onClick={copyInAppIdentifier}
                        className="any-button"
                        title={`Copy ID: ${account.inAppIdentifier}`}
                    >
                        📋
                    </button>
                </div>
            </div>

            {account.callers && account.callers.filter(caller => caller.note.status === "Exists").map((caller, index) => (
                <div key={index} className="note-container">
                    <div className="note-header">
                        📝 Note:
                    </div>
                    <div className="note-text">
                        {caller.note.text.startsWith('mt ') ? ('Met through ' + caller.note.text.substring(3)) : caller.note.text}
                    </div>
                </div>
            ))}
        </div>
    );
};

export default Account;