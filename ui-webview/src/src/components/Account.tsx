import './Account.css';
import {CircleDot, CircleOff, Clipboard, DiamondMinus, Globe, TriangleAlert} from "lucide-react";
import {_D, _D2} from "../haiUtils.ts";
import {
    type FrontAccount, NamedApp,
    OnlineStatus,
    type OnlineStatusType
} from "../types/CoreTypes.ts";
import {type DebugFlags, DemonstrationMode} from "../types/DebugFlags.ts";
import {LiveSessionKnowledge} from "../types/LiveUpdateTypes.ts";
import {AppIcon} from "./AppIcon.tsx";
import {LiveSession} from "../pages/LiveSession.tsx";

interface AccountProps {
    account: FrontAccount,
    imposter: boolean,
    showAlias: boolean,
    showNotes: boolean,
    debugMode: DebugFlags,
    showSession?: boolean,
    isSessionView: boolean
}

const Account = ({account, imposter, showAlias, showNotes, debugMode, showSession, isSessionView}: AccountProps) => {
    const hasNote = account.isAnyCallerNote;

    const copyInAppIdentifier = async () => {
        await navigator.clipboard.writeText(account.inAppIdentifier);
    };

    const getAppDisplayName = (account: FrontAccount) => {
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

    const getOnlineStatusIcon = (onlineStatus: OnlineStatusType, isKnownSession: boolean) => {
        switch (onlineStatus) {
            case OnlineStatus.Online:
                if (isKnownSession) return <span className="status-char status-online">⬤</span>;
                else return <span className="status-char status-online-conflicting">⬤</span>;
            case OnlineStatus.ResoniteBusy:
            case OnlineStatus.VRChatDND:
                return <CircleOff className="status-icon status-busy"/>;
            case OnlineStatus.ResoniteAway:
                return <CircleDot className="status-icon status-away"/>;
            case OnlineStatus.VRChatAskMe:
                return <DiamondMinus className="status-icon status-askme"/>;
            case OnlineStatus.ResoniteSociable:
            case OnlineStatus.VRChatJoinMe:
                if (isKnownSession) return <span className="status-char status-joinme">■</span>;
                else return <span className="status-char status-joinme-conflicting">■</span>;
            case OnlineStatus.Offline:
                return '';
            default:
                return '';
        }
    };

    const getOnlineStatusText = (onlineStatus: OnlineStatusType, isKnownSession: boolean) => {
        switch (onlineStatus) {
            case OnlineStatus.Online:
                if (isKnownSession) return 'Online';
                else return 'Online (Private)';
            case OnlineStatus.ResoniteBusy:
                return 'Busy';
            case OnlineStatus.VRChatDND:
                return 'Do Not Disturb';
            case OnlineStatus.ResoniteAway:
                if (isKnownSession) return 'Away';
                return 'Away (Private)';
            case OnlineStatus.VRChatAskMe:
                return 'Ask Me';
            case OnlineStatus.ResoniteSociable:
                return 'Sociable';
            case OnlineStatus.VRChatJoinMe:
                if (isKnownSession) return 'Join Me';
                else return 'Join Me (Private)';
            case OnlineStatus.Offline:
                return '';
            default:
                return onlineStatus;
        }
    };

    const isOffline = account.onlineStatus === OnlineStatus.Offline;
    const isKnownSession = account.mainSession && account.mainSession.knowledge === LiveSessionKnowledge.Known && true || false;
    const worldName = isKnownSession && account.mainSession && (account.mainSession.liveSession && (account.mainSession.liveSession.inAppVirtualSpaceName || 'Loading...')) || undefined;
    return (
        <div className="account-container">
            <div className="account-header">
                <div className="account-info">
                    {!isSessionView && <AppIcon namedApp={account.namedApp}/>}
                    <div>
                        <div className="account-display-name"
                             title={!imposter && account.allDisplayNames?.map(it => _D(it, debugMode)).join('\n') || ``}>
                            {isSessionView && (getOnlineStatusIcon(account.onlineStatus || OnlineStatus.Offline, isKnownSession))}
                            {isSessionView && ' '}
                            {_D(account.inAppDisplayName, debugMode)} {!isSessionView && getOnlineStatusIcon(account.onlineStatus || OnlineStatus.Offline, isKnownSession)} {!isSessionView && getOnlineStatusText(account.onlineStatus || OnlineStatus.Offline, isKnownSession)}
                        </div>
                        {!imposter && showAlias && account.allDisplayNames && account.allDisplayNames
                            .slice().reverse()
                            .filter((displayName: string) => displayName !== account.inAppDisplayName)
                            .map((displayName: string, index: number) => (
                                <div key={index} className="account-display-name">
                                    {_D(displayName, debugMode)}
                                </div>
                            ))}
                        <div className="account-app-name">
                            {!account.customStatus && getAppDisplayName(account)} {_D2(account.customStatus || '', debugMode)}
                        </div>
                        {showSession && !isOffline && worldName && <div className="account-app-name">
                            <span className="status-char status-online">⬤</span>
                            <i>{_D2(worldName, debugMode, undefined, DemonstrationMode.EverythingButSessionNames)}</i>
                        </div>}
                        {showSession && !isOffline && !worldName && account.mainSession
                            && account.mainSession.knowledge !== LiveSessionKnowledge.PrivateSession
                            && account.mainSession.knowledge !== LiveSessionKnowledge.PrivateWorld &&
                            <div className="account-app-name">
                                <CircleOff className="status-icon status-busy"/> {account.mainSession.knowledge}
                            </div>}
                    </div>
                </div>
                {!imposter && (<div className="account-badges">
                    {!account.isAnyCallerContact && hasNote && (
                        <span className="badge note">
                            📝 Note
                        </span>
                    )}
                    {!isSessionView && account.isAnyCallerContact && (
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
                            <Globe size={16}/>
                        </a>
                    )}
                    <button
                        onClick={copyInAppIdentifier}
                        className="icon-button"
                        title={`Copy ID: ${_D(account.inAppIdentifier, debugMode)}`}
                    >
                        <Clipboard size={16}/>
                    </button>
                </div>)}
            </div>

            {account.isPendingUpdate && (
                <p className="warning-message">
                    <span className="warning-icon"><TriangleAlert/></span>
                    We have not yet collected all information for this account. Notes, bio, and links may be missing.
                </p>
            )}

            {showNotes && account.callers && account.callers.filter(caller => caller.note).map((caller, index) => (
                <div key={index} className="note-container">
                    <div className="note-text">
                        {caller.note!.startsWith('mt ') ? (_D2('Met through ' + caller.note!.substring(3), debugMode)) : _D2(caller.note!, debugMode)}
                    </div>
                </div>
            ))}

            {!isSessionView && account.namedApp !== NamedApp.Resonite && account.mainSession && account.mainSession.liveSession && <LiveSession liveSession={account.mainSession.liveSession} individuals={[]} debugMode={debugMode} mini={true} />}
            {account.namedApp === NamedApp.Resonite && account.multiSessions.map((session) => (<LiveSession liveSession={session} individuals={[]} debugMode={debugMode} mini={true} />))}
        </div>
    );
};

export default Account;