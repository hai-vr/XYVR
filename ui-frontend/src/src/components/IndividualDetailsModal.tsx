import Modal from './Modal';
import {_D} from '../haiUtils';
import './IndividualDetailsModal.css';
import type {FrontIndividual} from "../types/CoreTypes.ts";
import type {DebugFlags} from "../types/DebugFlags.ts";
import Individual from "./Individual.tsx";
import {useState, useCallback} from 'react';
import {DotNetApi} from "../DotNetApi.ts";

interface IndividualDetailsModalProps {
    isOpen: boolean;
    onClose: () => void;
    individual: FrontIndividual;
    debugMode: DebugFlags;
}

function IndividualDetailsModal({isOpen, onClose, individual, debugMode}: IndividualDetailsModalProps) {
    const dotNetApi = new DotNetApi();
    
    const [isDragOver, setIsDragOver] = useState(false);

    const handleDragOver = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        if (e.dataTransfer.types.includes('Files')) {
            setIsDragOver(true);
        }
    }, []);

    const handleDragLeave = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        // Only set dragOver to false if leaving the modal entirely
        if (!e.currentTarget.contains(e.relatedTarget as Node)) {
            setIsDragOver(false);
        }
    }, []);

    const handleDrop = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragOver(false);

        const files = Array.from(e.dataTransfer.files);
        if (files.length > 0) {
            const file = files[0];
            const reader = new FileReader();
    
            reader.onload = async (event) => {
                const dataUrl = event.target?.result as string;
                // Extract base64 part (remove "data:mime/type;base64," prefix)
                const base64String = dataUrl.split(',')[1];
    
                const fileObject = {
                    name: file.name,
                    size: file.size,
                    type: file.type,
                    lastModified: file.lastModified,
                    base64Content: base64String
                };
    
                console.log('File as JSON object:', fileObject);
                
                const requestBody = {
                    individualGuid: individual.guid,
                    file: fileObject
                };

                await dotNetApi.appApiAssignProfileIllustration(JSON.stringify(requestBody));
            };
    
            reader.onerror = () => {
                console.error(`Error reading file ${file.name}`);
            };
    
            reader.readAsDataURL(file);
        }
    }, []);

    return (
        <Modal
            isOpen={isOpen}
            onClose={onClose}
            title={_D(individual.displayName, debugMode)}
            maxWidth="700px"
        >
            <div
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={handleDrop}
                className={`drag-drop-zone ${isDragOver ? 'drag-over' : ''}`}
                style={{width: '100%', height: '100%', position: 'relative'}}
            >
                <Individual
                    individual={individual}
                    isVisible={true}
                    showBio={true}
                    showAlias={true}
                    setMergeAccountGuidOrUnd={() => {
                    }}
                    isBeingMerged={false}
                    displayNameOfOtherBeingMergedOrUnd={undefined}
                    compactMode={false}
                    searchField=""
                    showNotes={true}
                    debugMode={debugMode}
                    showCopyToClipboard={true}
                />
                {isDragOver && (
                    <div className="drag-overlay">
                        <div className="drag-overlay-message">
                            Drop files here
                        </div>
                    </div>
                )}
            </div>
        </Modal>
    );
}

export default IndividualDetailsModal;