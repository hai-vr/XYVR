import {useState} from "react";
import {AppIcon} from "./AppIcon.tsx";
import Account from "./Account.tsx";
import type {FrontLiveSession} from "../types/LiveUpdateTypes.ts";
import type {FrontIndividual} from "../types/CoreTypes.ts";
import {type DebugFlags, DemonstrationMode} from "../types/DebugFlags.ts";
import {_D, _D2} from "../haiUtils.ts";
import {useTranslation} from "react-i18next";
import {DotNetApi} from "../DotNetApi.ts";
import {SquareArrowDownRight} from "lucide-react";

interface LiveSessionProps {
    liveSession: FrontLiveSession,
    individuals: FrontIndividual[],
    debugMode: DebugFlags,
    mini: boolean,
    resoniteShowSubSessions?: boolean,
    setModalIndividual?: (individual: FrontIndividual) => void
}

export function LiveSession({
                                liveSession,
                                individuals,
                                debugMode,
                                mini,
                                resoniteShowSubSessions = true,
                                setModalIndividual = undefined
                            }: LiveSessionProps) {
    const dotNetApi = new DotNetApi();
    const {t} = useTranslation();

    // @ts-ignore
    const [showSlots, setShowSlots] = useState(false);
    const [showParticipants, setShowParticipants] = useState(false);

    let specialCapacity = liveSession.sessionCapacity || liveSession.virtualSpaceDefaultCapacity || '?';
    const vscap = liveSession.virtualSpaceDefaultCapacity || liveSession.sessionCapacity || 0;
    const sesscap = liveSession.sessionCapacity || 0;
    if (vscap < sesscap) {
        specialCapacity = `${sesscap} (${vscap} + ${sesscap - vscap})`;
    }

    const showRemainingSlots = 5;
    const actualAttendance = Math.max(liveSession.currentAttendance || 0, liveSession.participants.length);
    // Replace all spaces with NBSP
    const capacityStr = `${actualAttendance || '?'} / ${specialCapacity}`.replaceAll(" ", "\u00a0");
    let capacityDisplay = actualAttendance > sesscap ? actualAttendance : Math.min(actualAttendance + showRemainingSlots, sesscap);
    const hasMore = sesscap - actualAttendance > showRemainingSlots;

    const makeGameClientJoinOrSelfInvite = async () => {
        await dotNetApi.liveApiMakeGameClientJoinOrSelfInvite(liveSession.namedApp, liveSession.callerInAppIdentifier, liveSession.inAppSessionIdentifier);
    };

    function ParseAccessLevel(markers: string[]) {
        if (markers.includes('Public')) return "Public";
        if (markers.includes('InvitePlus')) return "Invite+";
        if (markers.includes('Invite')) return "Invite";
        if (markers.includes('Friends')) return "Friends";
        if (markers.includes('FriendsPlus')) return "Friends+";
        if (markers.includes('Group')) return "Group";
        if (markers.includes('GroupPublic')) return "Group Public";
        if (markers.includes('GroupPlus')) return "Group+";

        if (markers.includes('Anyone')) return "Anyone";
        if (markers.includes('RegisteredUsers')) return "Registered Users";
        if (markers.includes('ContactsPlus')) return "Contacts+";
        if (markers.includes('Contacts')) return "Contacts";
        if (markers.includes('LAN')) return "LAN";
        if (markers.includes('Private')) return "Private";
        
        return '';
    }

    const isHeadless = liveSession.markers.includes('Headless');
    const accessLevel = ParseAccessLevel(liveSession.markers);

    const participationSquares = (
    <>
        <div style={{
            textWrap: 'nowrap',
            textAlign: 'left',
            lineHeight: '13px',
            margin: '0 auto',
            marginBottom: '5px',
            width: 'fit-content'
        }}>
            {Array.from({length: capacityDisplay}, (_, index) => (
                <>
                    {index % 20 == 0 && index != 0 && <br/>}
                    {index < actualAttendance && <span key={index} style={{
                        width: '10px',
                        height: '10px',
                        display: 'inline-block',
                        background: index < liveSession.participants.length ? 'var(--status-online)' : 'var(--text-primary)',
                        margin: '1px',
                        marginRight: '2px',
                        marginBottom: '-1px',
                        lineHeight: 0
                    }}></span>}
                    {showSlots && index >= actualAttendance && <span key={index} style={{
                        opacity: hasMore ? `${100 - (index - actualAttendance) / showRemainingSlots * 100}%` : '100%',
                        width: '10px',
                        height: '10px',
                        display: 'inline-block',
                        margin: '1px',
                        marginRight: '2px',
                        marginBottom: '-1px',
                        lineHeight: 0,
                        textAlign: 'center'
                    }}>&#183;</span>}
                </>
            ))}
        </div>
        <span title={capacityStr}>{capacityStr}</span>
    </>);

    let background = liveSession.thumbnailUrl && `url(${liveSession.thumbnailUrl}), var(--live-session-overlay)`
        || liveSession.thumbnailHash && `url(${dotNetApi.WorldThumbnailHashToUrl(liveSession.thumbnailHash)}), var(--live-session-overlay)`
        || 'var(--live-session-overlay)';
    return (<div key={liveSession.guid} className={`live-session-card live-session-thumbnail-bg ${liveSession.participants.length > 4 ? 'full-width' : ''}`}>
        <div style={{
            position: 'relative',
            height: '100%'
        }}>
            <div style={{
                background: background,
                // filter: liveSession.isVirtualSpacePrivate || debugMode.demoMode === DemonstrationMode.Everything ? 'blur(10px)' : 'none',
                // backdropFilter: liveSession.isVirtualSpacePrivate || debugMode.demoMode === DemonstrationMode.Everything ? 'blur(10px)' : 'none',
                backgroundBlendMode: 'var(--live-session-blend-mode), normal',
                backgroundSize: 'cover',
                backgroundPosition: 'center',
                backgroundRepeat: 'no-repeat',
                position: 'absolute',
                inset: 0,
                filter: 'var(--live-session-grayscale)',
            }} className="live-session-background-image"></div>
            <div style={{
                position: 'relative',
                display: 'flex',
                flexDirection: 'column',
                zIndex: 1,
                padding: '1rem',
                height: '100%',
                boxSizing: 'border-box'
            }}>
                <div className="live-session-header">
                    <div className="live-session-world" style={{display: 'flex', alignItems: 'center', gap: '0.5rem'}}>
                        {!mini && <div style={{marginRight: '12px'}}><AppIcon namedApp={liveSession.namedApp}/></div>}
                        <div style={{
                            display: 'flex',
                            flexDirection: 'column',
                            alignItems: 'flex-start',
                            gap: '0.25rem'
                        }}>
                            <span
                                title={capacityStr}>{_D2(liveSession.inAppVirtualSpaceName || '', debugMode, undefined, DemonstrationMode.EverythingButSessionNames) || t('live.session.unnamed')}</span>
                            {liveSession.inAppSessionName &&
                                <span className="live-session-name"
                                      title={capacityStr}>{_D2(liveSession.inAppSessionName || '', debugMode, undefined, DemonstrationMode.Everything) || t('live.session.unnamed')}</span>}
                        </div>
                        {liveSession.ageGated === true && <span>🔞</span>}
                    </div>
                </div>
                {!mini && <div className="live-session-participants">
                    {showParticipants && 
                    <div className="live-session-all-participants-outer">
                        <div className="live-session-all-participants">
                            {liveSession.allParticipants.filter(x=> x.unknownAccount != null).map(p => <span>{p.unknownAccount?.inAppDisplayName ?? p.unknownAccount?.inAppIdentifier}</span>)}
                        </div>
                    </div>}
                    <div className="session-accounts-container">
                        <div className="live-session-accounts-grid">
                            {liveSession.participants.filter(value => value.isKnown).map((participant, pIndex) => {
                                const matchingIndividual = individuals.find(ind =>
                                    ind.accounts?.some(acc =>
                                        acc.qualifiedAppName === liveSession.qualifiedAppName &&
                                        acc.inAppIdentifier === participant.knownAccount!.inAppIdentifier
                                    )
                                );

                                const matchingAccount = matchingIndividual?.accounts?.find(acc =>
                                    acc.qualifiedAppName === liveSession.qualifiedAppName &&
                                    acc.inAppIdentifier === participant.knownAccount!.inAppIdentifier
                                );

                                if (matchingAccount) {
                                    return (
                                        <Account
                                            key={participant.knownAccount!.inAppIdentifier || pIndex}
                                            account={matchingAccount}
                                            imposter={false}
                                            showAlias={false}
                                            showNotes={false}
                                            debugMode={debugMode}
                                            showSession={false}
                                            isSessionView={true}
                                            resoniteShowSubSessions={resoniteShowSubSessions}
                                            clickOpensIndividual={matchingIndividual}
                                            setModalIndividual={setModalIndividual}
                                            illustrativeDisplay={true}
                                        />
                                    );
                                }
                            })}
                        </div>
                    </div>
                </div>}

                {!mini && liveSession.allParticipants.length > 0 ?
                    <button onClick={() => setShowParticipants(!showParticipants)} className="count-container">
                        {participationSquares}
                    </button>
                    :
                    <div className="count-container">
                        <div style={{
                            textWrap: 'nowrap',
                            textAlign: 'left',
                            lineHeight: '13px',
                            margin: '0 auto',
                            marginBottom: '5px',
                            width: 'fit-content'
                        }}>
                            {participationSquares}
                        </div>
                    </div>}
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', justifyContent: 'space-between' }}>
                    <a
                        onClick={makeGameClientJoinOrSelfInvite}
                        onAuxClick={(e) => e.button === 1 && makeGameClientJoinOrSelfInvite()}
                        onMouseDown={(e) => e.preventDefault()}
                        rel="noopener noreferrer"
                        className="icon-button link-pointer"
                        title={t('ui.joinSession.title')}
                    >
                        <SquareArrowDownRight size={16}/>
                    </a>
                    <span>{isHeadless && <span>Headless</span>} {accessLevel}</span>
                </div>
            </div>
        </div>
    </div>);
}