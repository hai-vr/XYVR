import React from 'react';
import './Account.css';
import {CircleDot, CircleOff, Clipboard, DiamondMinus, Globe, SquareCheck} from "lucide-react";

const Account = ({ account, imposter, showAlias }) => {
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
    
    const getOnlineStatusEmoji = (onlineStatus) => {
        switch (onlineStatus) {
            case 'Online':
                return <span className="status-char status-online">⬤</span>;
            case 'ResoniteBusy':
            case 'VRChatDND':
                return <CircleOff className="status-icon status-busy" />;
            case 'ResoniteAway':
                return <CircleDot className="status-icon status-away" />;
            case 'VRChatAskMe':
                return <DiamondMinus className="status-icon status-askme" />;
            case 'ResoniteSociable':
            case 'VRChatJoinMe':
                return <span className="status-char status-joinme">■</span>;
            case 'Offline':
                return '';
            default:
                return '';
        }
    };
    
    const getOnlineStatusText = (onlineStatus) => {
        switch (onlineStatus) {
            case 'Online':
                return 'Online';
            case 'ResoniteBusy':
                return 'Busy';
            case 'VRChatDND':
                return 'Do Not Disturb';
            case 'ResoniteAway':
                return 'Away';
            case 'VRChatAskMe':
                return 'Ask Me';
            case 'ResoniteSociable':
                return 'Sociable';
            case 'VRChatJoinMe':
                return 'Join Me';
            case 'Offline':
                return '';
            case '':
            default:
                return onlineStatus;
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
                        <div className="account-display-name" title={!imposter && account.allDisplayNames?.join('\n') || ``}>
                            {account.inAppDisplayName} {getOnlineStatusEmoji(account.onlineStatus)} {getOnlineStatusText(account.onlineStatus)}
                        </div>
                        {!imposter && showAlias && account.allDisplayNames && account.allDisplayNames
                            .toReversed()
                            .filter((displayName) => displayName !== account.inAppDisplayName)
                            .map((displayName, index) => (
                            <div key={index} className="account-display-name">
                                {displayName}
                            </div>
                        ))}
                        <div className="account-app-name">
                            {!account.customStatus && getAppDisplayName(account)} {account.customStatus}
                        </div>
                    </div>
                </div>
                {!imposter && (<div className="account-badges">
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
                            className="icon-button"
                            title={`Open ${account.namedApp} Profile`}
                        >
                            <Globe size={16} />
                        </a>
                    )}
                    <button
                        onClick={copyInAppIdentifier}
                        className="icon-button"
                        title={`Copy ID: ${account.inAppIdentifier}`}
                    >
                        <Clipboard size={16} />
                    </button>
                </div>)}
            </div>

            {account.isPendingUpdate && (
                <p className="warning-message">
                    <span className="warning-icon">⚠️</span>
                    We have not yet collected all information for this account. Notes, bio, and links may be missing.
                </p>
            )}

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