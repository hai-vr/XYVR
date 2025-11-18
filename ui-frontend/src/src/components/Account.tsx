import './Account.css';
import {CircleDot, CircleOff, DiamondMinus, Clipboard, Globe, Hash, TriangleAlert} from "lucide-react";
import {_D, _D2} from "../haiUtils.ts";
import {
    type FrontAccount, type FrontIndividual, NamedApp,
    OnlineStatus,
    type OnlineStatusType
} from "../types/CoreTypes.ts";
// @ts-ignore
import {type DebugFlags, DemonstrationMode} from "../types/DebugFlags.ts";
import {LiveSessionKnowledge} from "../types/LiveUpdateTypes.ts";
import {AppIcon} from "./AppIcon.tsx";
import {LiveSession} from "./LiveSession.tsx";
import {DotNetApi} from "../DotNetApi.ts";
import {useTranslation} from "react-i18next";
import clsx from "clsx";
import {useEffect, useState} from "react";

interface AccountProps {
    account: FrontAccount,
    imposter: boolean,
    showAlias: boolean,
    showNotes: boolean,
    debugMode: DebugFlags,
    showSession?: boolean,
    isSessionView: boolean,
    resoniteShowSubSessions?: boolean,
    clickOpensIndividual?: FrontIndividual,
    setModalIndividual?: (individual: FrontIndividual) => void,
    showCopyToClipboard?: boolean,
    illustrativeDisplay?: boolean,
    showAccountIcon?: boolean
}

// @ts-ignore
const Account = ({
                     account,
                     imposter,
                     showAlias,
                     showNotes,
                     debugMode,
                     showSession,
                     isSessionView,
                     resoniteShowSubSessions = true,
                     clickOpensIndividual,
                     setModalIndividual = undefined,
                     showCopyToClipboard,
                     illustrativeDisplay,
                     showAccountIcon
                 }: AccountProps) => {
    const dotNetApi = new DotNetApi();
    const {t} = useTranslation();

    const [isAltDown, setIsAltDown] = useState(false);

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Alt" || e.altKey) {
                setIsAltDown(true);
            }
        };

        const handleKeyUp = (e: KeyboardEvent) => {
            if (e.key === "Alt" || !e.altKey) {
                setIsAltDown(false);
            }
        };

        window.addEventListener("keydown", handleKeyDown);
        window.addEventListener("keyup", handleKeyUp);

        return () => {
            window.removeEventListener("keydown", handleKeyDown);
            window.removeEventListener("keyup", handleKeyUp);
        };
    }, []);

    const hasNote = account.isAnyCallerNote;

    const copyInAppIdentifier = async () => {
        await navigator.clipboard.writeText(account.inAppIdentifier);
    };

    function getProfileLink() {
        return `${account.namedApp === NamedApp.VRChat && 'https://vrchat.com/home/user/' || 'https://hub.chilloutvr.net/social/profile?guid='}${account.inAppIdentifier}`;
    }

    const copyLinkToProfileIdentifier = async () => {
        const link = getProfileLink();
        await navigator.clipboard.writeText(link);
    };

    const openLink = async () => {
        const link = getProfileLink();
        await dotNetApi.appApiOpenLink(link);
    };

    const handleNameClick = () => {
        if (setModalIndividual && clickOpensIndividual) setModalIndividual(clickOpensIndividual);
    };

    const getAppDisplayName = (account: FrontAccount) => {
        switch (account.namedApp) {
            case NamedApp.Resonite:
                return 'Resonite';
            case NamedApp.VRChat:
                return 'VRChat';
            case NamedApp.Cluster:
                return `Cluster (@${account.inAppIdentifier})`;
            case NamedApp.ChilloutVR:
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
                if (isKnownSession) return t('account.status.online');
                else return t('account.status.onlinePrivate');
            case OnlineStatus.ResoniteBusy:
                return t('account.status.busy');
            case OnlineStatus.VRChatDND:
                return t('account.status.doNotDisturb');
            case OnlineStatus.ResoniteAway:
                if (isKnownSession) return t('account.status.away');
                return t('account.status.awayPrivate');
            case OnlineStatus.VRChatAskMe:
                return t('account.status.askMe');
            case OnlineStatus.ResoniteSociable:
                return t('account.status.sociable');
            case OnlineStatus.VRChatJoinMe:
                if (isKnownSession) return t('account.status.joinMe');
                else return t('account.status.joinMePrivate');
            case OnlineStatus.Offline:
                return '';
            default:
                return onlineStatus;
        }
    };

    const isConnector = !((account as any).multiSessions); // This is a hack, multiSessions can only be null because we're casting a FrontConnectorAccount to a FrontAccount.
    // @ts-ignore
    const isOffline = account.onlineStatus === OnlineStatus.Offline;
    const isKnownSession = account.mainSession && account.mainSession.knowledge === LiveSessionKnowledge.Known && true || false;
    // @ts-ignore
    const worldName = isKnownSession && account.mainSession && (account.mainSession.liveSession && (account.mainSession.liveSession.inAppSessionName || account.mainSession.liveSession.inAppVirtualSpaceName || 'Loading...')) || undefined;

    return (
        <>
            <div className={clsx("account-container", illustrativeDisplay && 'account-illustrative')}
                 style={{position: 'relative'}}>
                {illustrativeDisplay && clickOpensIndividual && <div style={{
                    background: `var(--account-illustrative-overlay), url("individualprofile://${clickOpensIndividual.guid}"), var(--bg-primary)`,
                    backgroundBlendMode: 'normal',
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                    backgroundRepeat: 'no-repeat',
                    position: 'absolute',
                    inset: 0,
                }}></div>}
                {illustrativeDisplay && clickOpensIndividual && showAccountIcon &&
                    <div style={{position: 'absolute', bottom: 0, right: 0}}>
                        <AppIcon namedApp={account.namedApp} mini={true}/>
                    </div>}
                <div className="account-header" style={{zIndex: 1}}>
                    <div className="account-info">
                        {!isSessionView &&
                            <div style={{marginRight: '12px'}}><AppIcon namedApp={account.namedApp}/></div>}
                        <div>
                            <div className={clsx("account-display-name", clickOpensIndividual && 'modal-pointer')}
                                 onClick={clickOpensIndividual && handleNameClick || undefined}
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
                            {/*{showSession && !isOffline && worldName && <div className="account-app-name">*/}
                            {/*    <span className="status-char status-online">⬤</span>*/}
                            {/*    <i>{_D2(worldName, debugMode, undefined, DemonstrationMode.EverythingButSessionNames)}</i>*/}
                            {/*</div>}*/}
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
                                📝 {t('account.badge.note')}
                            </span>
                        )}
                        {!isSessionView && account.isAnyCallerContact && (
                            <span className="badge contact">
                                {account.namedApp === NamedApp.VRChat || account.namedApp === NamedApp.ChilloutVR ? t('account.badge.friend') : t('account.badge.contact')}
                            </span>
                        )}
                        {account.isTechnical && (
                            <span className="badge bot">
                                {t('account.badge.bot')}
                            </span>
                        )}
                        {showCopyToClipboard && (isAltDown || account.namedApp === NamedApp.Resonite) && <button
                            onClick={copyInAppIdentifier}
                            className="icon-button"
                            title={t('account.copyId.title', {id: _D(account.inAppIdentifier, debugMode)})}
                        >
                            <Hash size={16}/>
                        </button>}
                        {showCopyToClipboard && (account.namedApp === NamedApp.VRChat || account.namedApp === NamedApp.ChilloutVR) && <button
                            onClick={copyLinkToProfileIdentifier}
                            className="icon-button"
                            title={t('account.copyLinkToProfile.title', {app: account.namedApp})}
                        >
                            <Clipboard size={16}/>
                        </button>}
                        {!illustrativeDisplay && (account.namedApp === NamedApp.VRChat || account.namedApp === NamedApp.ChilloutVR) && (
                            <a
                                onClick={openLink} onAuxClick={(e) => e.button === 1 && openLink()}
                                onMouseDown={(e) => e.preventDefault()}
                                rel="noopener noreferrer"
                                className="icon-button link-pointer"
                                title={t('account.openProfile.title', {app: account.namedApp})}
                            >
                                <Globe size={16}/>
                            </a>
                        )}
                    </div>)}
                </div>

                {account.isPendingUpdate && (
                    <p className="warning-message">
                        <span className="warning-icon"><TriangleAlert/></span>
                        {t('account.pendingUpdate.message')}
                    </p>
                )}

                {showNotes && account.callers && account.callers.filter(caller => caller.note).map((caller, index) => (
                    <div key={index} className="note-container">
                        <div className="note-text">
                            {caller.note!.startsWith('mt ')
                                ? t('account.note.metThrough', {location: _D2(caller.note!.substring(3), debugMode)})
                                : _D2(caller.note!, debugMode)
                            }
                        </div>
                    </div>
                ))}

                {!isConnector && !isSessionView && account.mainSession?.liveSession
                    && <LiveSession liveSession={account.mainSession.liveSession} individuals={[]} debugMode={debugMode}
                                    mini={true}/>}
                {!illustrativeDisplay && !isConnector && account.namedApp === NamedApp.Resonite && resoniteShowSubSessions && account.multiSessions
                    .map((session) => (session.guid != account.mainSession?.sessionGuid &&
                        <LiveSession liveSession={session} individuals={[]} debugMode={debugMode} mini={true}/>))}
            </div>
        </>
    );
};

export default Account;