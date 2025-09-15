import {Component} from "react";
import {AppIcon} from "../components/AppIcon.tsx";
import Account from "../components/Account.tsx";
import type {FrontLiveSession} from "../types/LiveUpdateTypes.ts";
import type {FrontIndividual} from "../types/CoreTypes.ts";
import type {DebugFlags} from "../types/DebugFlags.ts";

interface LiveSessionProps {
    liveSession: FrontLiveSession,
    individuals: FrontIndividual[],
    debugMode: DebugFlags,
    mini: boolean
}

export class LiveSession extends Component<LiveSessionProps> {
    render() {
        const {liveSession, individuals, debugMode, mini} = this.props;

        return (<div key={liveSession.guid} className="live-session-card live-session-thumbnail-bg"

                     style={{
                         background: `url(${liveSession.thumbnailUrl})`,
                         backgroundSize: 'cover',
                         backgroundPosition: 'center',
                         backgroundRepeat: 'no-repeat',
                     }}>
            <div className="live-session-header">
                <div className="live-session-world">
                    {!mini && <AppIcon namedApp={liveSession.namedApp}/>}
                    <div>
                        {liveSession.inAppVirtualSpaceName || 'Unknown World'} ({liveSession.currentAttendance || '?'}&nbsp;/&nbsp;{liveSession.sessionCapacity || liveSession.virtualSpaceDefaultCapacity || '?'})
                    </div>
                </div>
            </div>
            {!mini && <div className="live-session-participants">
                <div className="accounts-container">
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
        </div>);
    }
}