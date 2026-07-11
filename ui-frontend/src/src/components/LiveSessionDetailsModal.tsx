import Modal from './Modal';
import {_D2} from '../haiUtils';
import './LiveSessionDetailsModal.css';
import type {FrontLiveSession} from "../types/LiveUpdateTypes.ts";
import type {DebugFlags} from "../types/DebugFlags.ts";
import {LiveSession} from "./LiveSession.tsx";
import type {FrontIndividual} from "../types/CoreTypes.ts";
import { useTranslation } from "react-i18next";

interface LiveSessionDetailsModalProps {
    isOpen: boolean;
    onClose: () => void;
    liveSession: FrontLiveSession;
    individuals: FrontIndividual[];
    debugMode: DebugFlags;
}

function LiveSessionDetailsModal({isOpen, onClose, liveSession, individuals, debugMode}: LiveSessionDetailsModalProps) {
    const { t } = useTranslation();
    
    // We use DemonstrationMode.EverythingButSessionNames to match LiveSession.tsx's title logic
    const sessionTitle = _D2(liveSession.inAppSessionName || liveSession.inAppVirtualSpaceName || '', debugMode) || t('live.session.unnamed');

    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title={sessionTitle}
            maxWidth="700px"
        >
            <div className="live-session-details-modal-content">
                <LiveSession
                    liveSession={liveSession}
                    individuals={individuals}
                    debugMode={debugMode}
                    mini={false}
                    portraits={true}
                />
            </div>
        </Modal>
    );
}

export default LiveSessionDetailsModal;
