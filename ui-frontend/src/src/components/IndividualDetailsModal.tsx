import Modal from './Modal';
import {_D} from '../haiUtils';
import './IndividualDetailsModal.css';
import type {FrontIndividual} from "../types/CoreTypes.ts";
import type {DebugFlags} from "../types/DebugFlags.ts";
import Individual from "./Individual.tsx";

interface IndividualDetailsModalProps {
    isOpen: boolean;
    onClose: () => void;
    individual: FrontIndividual;
    debugMode: DebugFlags;
}

function IndividualDetailsModal({isOpen, onClose, individual, debugMode}: IndividualDetailsModalProps) {
    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title={_D(individual.displayName, debugMode)}
            maxWidth="700px"
        >
            <Individual individual={individual} isVisible={true} showBio={true} showAlias={true}
                        setMergeAccountGuidOrUnd={() => {}}
                        isBeingMerged={false} displayNameOfOtherBeingMergedOrUnd={undefined}
                        compactMode={false} searchField="" showNotes={true} debugMode={debugMode} />
        </Modal>
    );
}

export default IndividualDetailsModal;