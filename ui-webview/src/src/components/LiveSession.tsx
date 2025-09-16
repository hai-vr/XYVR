import {Component} from "react";
import {AppIcon} from "./AppIcon.tsx";
import Account from "./Account.tsx";
import type {FrontLiveSession} from "../types/LiveUpdateTypes.ts";
import type {FrontIndividual} from "../types/CoreTypes.ts";
import {type DebugFlags, DemonstrationMode} from "../types/DebugFlags.ts";
import {_D, _D2} from "../haiUtils.ts";

interface LiveSessionProps {
    liveSession: FrontLiveSession,
    individuals: FrontIndividual[],
    debugMode: DebugFlags,
    mini: boolean
}

export class LiveSession extends Component<LiveSessionProps> {
    render() {
        const {liveSession, individuals, debugMode, mini} = this.props;

        const capacityStr = `${liveSession.currentAttendance || '?'} / ${(liveSession.sessionCapacity || liveSession.virtualSpaceDefaultCapacity || '?')}`;
        return (<div key={liveSession.guid} className="live-session-card live-session-thumbnail-bg">
            <div style={{
                position: 'relative',
                height: '100%'
            }}>
                <div style={{
                    background: `url(${liveSession.thumbnailUrl}), var(--live-session-overlay)`,
                    // filter: liveSession.isVirtualSpacePrivate || debugMode.demoMode === DemonstrationMode.Everything ? 'blur(10px)' : 'none',
                    // backdropFilter: liveSession.isVirtualSpacePrivate || debugMode.demoMode === DemonstrationMode.Everything ? 'blur(10px)' : 'none',
                    backgroundBlendMode: 'var(--live-session-blend-mode), normal',
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                    backgroundRepeat: 'no-repeat',
                    position: 'absolute',
                    inset: 0,
                    filter: 'var(--live-session-grayscale)',
                    borderRadius: '6px',
                }}></div>
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
                        <div className="live-session-world" style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            {!mini && <AppIcon namedApp={liveSession.namedApp}/>}
                            <span title={capacityStr}>{_D2(liveSession.inAppVirtualSpaceName || '', debugMode, undefined, DemonstrationMode.EverythingButSessionNames) || 'Unknown World'}</span>
                        </div>
                    </div>
                    {!mini && <div className="live-session-participants">
                        <div className="session-accounts-container">
                            <div className="accounts-grid">
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
                                            />
                                        );
                                    }
                                })}
                            </div>
                        </div>
                    </div>}

                    <div className="count-container">
                        <div style={{
                            textWrap: 'nowrap',
                            textAlign: 'left',
                            lineHeight: '13px',
                            margin: '0 auto',
                            marginBottom: '5px',
                            width: 'fit-content'
                        }}>
                            {Array.from({ length: liveSession.currentAttendance || 0 }, (_, index) => (
                                <>
                                    {index % 20 == 0 && index != 0 && <br/>}
                                    <span key={index} style={{
                                        width: '10px',
                                        height: '10px',
                                        display: 'inline-block',
                                        background: index < liveSession.participants.length ? 'var(--status-online)' : 'var(--text-primary)',
                                        margin: '1px',
                                        marginRight: '2px',
                                        marginBottom: '-1px',
                                        lineHeight: 0
                                    }}></span>
                                </>
                            ))}
                        </div>
                        {liveSession.currentAttendance || '?'}&nbsp;/&nbsp;{liveSession.sessionCapacity || liveSession.virtualSpaceDefaultCapacity || '?'}
                    </div>
                </div>
            </div>
        </div>);
    }
}