import {Component} from "react";
import {AppIcon} from "../components/AppIcon.tsx";
import Account from "../components/Account.tsx";
import type {FrontLiveSession} from "../types/LiveUpdateTypes.ts";
import type {FrontIndividual} from "../types/CoreTypes.ts";
import type {DebugFlags} from "../types/DebugFlags.ts";

interface LiveSessionProps {
    liveSession: FrontLiveSession,
    individuals: FrontIndividual[],
    debugMode: DebugFlags
}

export class LiveSession extends Component<LiveSessionProps> {
    render() {
        const {liveSession, individuals, debugMode} = this.props;

        return (<div key={liveSession.guid} className="live-session-card">
            <div className="live-session-header">
                <div className="live-session-world">
                    <AppIcon namedApp={liveSession.namedApp}/>
                    <div>
                        {liveSession.inAppVirtualSpaceName || 'Unknown World'} ({liveSession.currentAttendance || '?'}&nbsp;/&nbsp;{liveSession.sessionCapacity || liveSession.virtualSpaceDefaultCapacity || '?'})
                    </div>
                </div>
            </div>
            <div className="live-session-participants">
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
                                        imposter={true}
                                        showAlias={false}
                                        showNotes={false}
                                        debugMode={debugMode}
                                        showSession={false}
                                        isSessionView={false}
                                    />
                                );
                            }
                        })}
                    </div>
                </div>
            </div>
        </div>);
    }
}