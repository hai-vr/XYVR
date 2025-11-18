import Account from "./Account.tsx";
import "./Individual.css";
import React, { useState, useRef, useEffect } from "react";
import {Clipboard/*, Phone*/} from "lucide-react";
import {accountMatchesFromRegularTerms, anyAccountMatchesSpecialTerms, parseSearchField} from "../pages/searchUtils.ts";
import {_D, _D2, makePersonalLinkPresentable} from "../haiUtils.ts";
import {type FrontAccount, type FrontIndividual, NamedApp} from "../types/CoreTypes.ts";
import {type DebugFlags, DemonstrationMode} from "../types/DebugFlags.ts";
import {DotNetApi} from "../DotNetApi.ts";
import { useTranslation } from "react-i18next";
import IndividualDetailsModal from "./IndividualDetailsModal.tsx";
import clsx from "clsx";

interface IndividualProps {
    individual: FrontIndividual;
    isVisible?: boolean;
    showBio?: boolean;
    showAlias?: boolean;
    setMergeAccountGuidOrUnd: (guid: string | undefined) => void;
    isBeingMerged?: boolean;
    displayNameOfOtherBeingMergedOrUnd?: string;
    fusionAccounts?: (guid: string) => Promise<void>;
    unmergeAccounts?: (guid: string) => Promise<void>;
    compactMode: boolean;
    searchField: string;
    showNotes: boolean;
    debugMode: DebugFlags;
    setModalIndividual?: (individual: FrontIndividual) => void
    showCopyToClipboard?: boolean;
}

function Individual({
                        individual,
                        isVisible = true,
                        showBio = false,
                        showAlias = false,
                        setMergeAccountGuidOrUnd,
                        isBeingMerged = false,
                        displayNameOfOtherBeingMergedOrUnd = undefined,
                        fusionAccounts,
                        unmergeAccounts,
                        compactMode,
                        searchField,
                        showNotes,
                        debugMode,
                        setModalIndividual = undefined,
                        showCopyToClipboard
                    }: IndividualProps) {
    const dotNetApi = new DotNetApi();
    const { t } = useTranslation();

    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const [filteredAccounts, setFilteredAccounts] = useState<FrontAccount[]>([]);
    const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);
    const dropdownRef = useRef<HTMLDivElement>(null);
    
    const isModalPresentation: boolean = fusionAccounts === undefined;

    // Get all VRChat account links and filter to only show http/https URLs
    const vrChatLinks = individual.accounts
        ?.filter(account => account.namedApp === NamedApp.VRChat && account.specifics?.urls?.length > 0)
        ?.flatMap(account => account.specifics.urls)
        // Some users have links that are an empty string. We don't want this because clicking it causes the page to reload.
        // Generally, prevent links that aren't http:// nor https://
        ?.filter(url => url && (url.startsWith('http://') || url.startsWith('https://'))) || [];

    // Get all VRChat account bios
    const vrcBios = showBio && individual.accounts
        ?.filter(account => account.namedApp === NamedApp.VRChat && account.specifics?.bio && account.specifics.bio.trim() !== '')
        || [];

    // Close dropdown when clicking outside
    useEffect(() => {
        const handleClickOutside = (event: any) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsDropdownOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    const copyToClipboard = async (url: string, event: React.MouseEvent<HTMLButtonElement>) => {
        event.stopPropagation(); // Prevent the container click event
        await navigator.clipboard.writeText(url);
    };

    const handleMenuAction = async (action: string, event: React.MouseEvent<HTMLButtonElement>) => {
        event.stopPropagation();
        setIsDropdownOpen(false);

        switch (action) {
            case 'confirmMerge':
                await fusionAccounts?.(individual.guid);
                break;
            case 'unmerge':
                await unmergeAccounts?.(individual.guid);
                break;
            case 'cancelMerge':
                setMergeAccountGuidOrUnd(undefined);
                break;
            case 'merge':
                setMergeAccountGuidOrUnd(individual.guid);
                break;
            case 'details':
                setIsDetailsModalOpen(true);
                break;
            default:
                break;
        }
    };

    const handleNameClick = () => {
        setIsDetailsModalOpen(true);
    };

    useEffect(() => {
        if (!compactMode || !searchField) {
            setFilteredAccounts(individual.accounts);
            return;
        }

        if (individual.accounts) {
            const { specialTerms, regularTerms } = parseSearchField(searchField);
            var convertConfusables = specialTerms.includes(':confusables');

            const filtered = individual.accounts.filter(account => {
                if (specialTerms.length > 0 && !anyAccountMatchesSpecialTerms([account], specialTerms, true, convertConfusables)) return false;
                if (regularTerms.length === 0) {
                    return true;
                }
                if (accountMatchesFromRegularTerms(account, regularTerms, convertConfusables)) return true;

                return false;
            });
            
            if (filtered.length === 0) {
                // If none match, show all. This is to support weird stuff like `app:resonite app:vrchat` or `app:resonite somenote` where `somenote` actually comes from a VRChat account.
                setFilteredAccounts(individual.accounts);
                return;
            }

            setFilteredAccounts(filtered);
        }
    }, [individual, searchField, compactMode])


    const openLink = async (url: string) => {
        const realUrl = debugMode.demoMode !== DemonstrationMode.Disabled ? 'https://example.com' : url
        await dotNetApi.appApiOpenLink(realUrl);
    };

    return (
        <>
            <div className={`${!compactMode ? 'individual-container' : 'individual-container-compact'} ${!isVisible ? 'hidden' : ''} ${isBeingMerged ? 'being-merged' : ''}`}>
                <div className="individual-top">
                    {!compactMode && <div style={{position: 'relative', width: 150, height: 225}}>
                        <div style={{
                            background: `var(--account-illustrative-overlay), url("individualprofile://${individual.guid}"), var(--bg-primary)`,
                            backgroundBlendMode: 'normal',
                            backgroundSize: 'cover',
                            backgroundPosition: 'center',
                            backgroundRepeat: 'no-repeat',
                            position: 'absolute',
                            inset: 0,
                        }}></div>
                    </div>}
                    {!compactMode && (<>
                        <div style={{position: 'relative', width: "calc(100% - 150px)", top: 0, right: 0}}>
                            <div className="individual-header">
                                <h3 className={clsx('individual-name', fusionAccounts && 'modal-pointer')} onClick={fusionAccounts && handleNameClick}>
                                    {_D(individual.displayName, debugMode)}
                                </h3>
                                {/*{individual.isAnyContact && (*/}
                                {/*    <span className="contact-badge">*/}
                                {/*    <Phone size={16} />*/}
                                {/*    <span>{t('individual.contact.label')}</span>*/}
                                {/*</span>*/}
                                {/*)}*/}
                                {!isModalPresentation && <div className="individual-menu" ref={dropdownRef}>
                                    <button
                                        className="menu-button"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            setIsDropdownOpen(!isDropdownOpen);
                                        }}
                                        title={t('individual.moreActions.title')}
                                    >
                                        ⋯
                                    </button>
                                    {isDropdownOpen && (
                                        <div className="dropdown-menu">
                                            {displayNameOfOtherBeingMergedOrUnd !== undefined && !isBeingMerged && (<button
                                                className="dropdown-item"
                                                onClick={(e) => handleMenuAction('confirmMerge', e)}
                                                title={t('individual.confirmMerge.title')}
                                            >
                                                {t('individual.confirmMerge.label', { name: _D(displayNameOfOtherBeingMergedOrUnd, debugMode) })}
                                            </button>)}
                                            {displayNameOfOtherBeingMergedOrUnd === undefined && <button
                                                className="dropdown-item"
                                                onClick={(e) => handleMenuAction('merge', e)}
                                                title={t('individual.mergeAccount.title')}
                                            >
                                                {t('individual.mergeAccount.label')}
                                            </button>}
                                            {displayNameOfOtherBeingMergedOrUnd !== undefined && <button
                                                className="dropdown-item"
                                                onClick={(e) => handleMenuAction('cancelMerge', e)}
                                            >
                                                Cancel merge
                                            </button>}
                                            {individual.accounts.length > 1 && <button
                                                className="dropdown-item"
                                                onClick={(e) => handleMenuAction('unmerge', e)}
                                            >
                                                Unmerge accounts
                                            </button>}
                                            <button
                                                className="dropdown-item"
                                                onClick={(e) => handleMenuAction('details', e)}
                                            >
                                                Show details
                                            </button>
                                        </div>
                                    )}
                                </div>}
                            </div>

                            {vrChatLinks.length > 0 && (
                                <div className="vrchat-links-list">
                                    {vrChatLinks.map((url, linkIndex) => {
                                        let presentedUrl = makePersonalLinkPresentable(url);
                                        return (
                                            <div key={linkIndex} className="vrchat-link-item">
                                                <span>
                                                {debugMode.demoMode !== DemonstrationMode.Disabled
                                                    ? ''
                                                    : presentedUrl.length === 2 ? <span style={{paddingRight: '4px'}}>{presentedUrl[1]}</span> : ''}
                                                <a
                                                    onClick={() => openLink(url)}
                                                    onAuxClick={(e) => e.button === 1 && openLink(url)}
                                                    onMouseDown={(e) => e.preventDefault()}
                                                    rel="noopener noreferrer"
                                                    className="vrchat-link link-pointer"
                                                    title={url}
                                                >
                                                    {debugMode.demoMode !== DemonstrationMode.Disabled
                                                        ? 'https://' + _D2(url.replace('http://', '').replace('https://', ''), debugMode, '/')
                                                        : presentedUrl.length === 2 ? _D2(presentedUrl[0], debugMode, '/') : _D2(presentedUrl[0], debugMode, '/')}
                                                </a>
                                                </span>
                                                <button
                                                    onClick={(e) => copyToClipboard(url, e)}
                                                    className="icon-button"
                                                    title="Copy link to clipboard"
                                                >
                                                    <Clipboard size={16}/>
                                                </button>
                                            </div>
                                        );
                                    })}
                                </div>
                            )}
                        </div>
                    </>)}
                </div>
    
                <div className="individual-underside">
                <div className="accounts-container">
                    {filteredAccounts && filteredAccounts.length > 0 ? (
                        <div className="accounts-grid">
                            {filteredAccounts.map((account) => (
                                <Account key={account.guid}
                                         account={account}
                                         showAlias={showAlias}
                                         showNotes={showNotes}
                                         debugMode={debugMode}
                                         imposter={false}
                                         showSession={true}
                                         isSessionView={false}
                                         showCopyToClipboard={showCopyToClipboard}
                                         setModalIndividual={!isModalPresentation && setModalIndividual || undefined}
                                         clickOpensIndividual={!isModalPresentation && individual || undefined}
                                />
                            ))}
                        </div>
                    ) : (
                        <div className="no-accounts">
                            📭 No accounts found
                        </div>
                    )}
                </div>
    
                {vrcBios.length > 0 && showBio && (
                    <div className="vrchat-bios-container">
                        {vrcBios.map((account, bioIndex) => (
                            <div key={bioIndex} className="vrchat-bio-item">
                                {vrcBios.length >= 2 && <Account account={account} imposter={false} showAlias={false} showNotes={false} debugMode={debugMode} isSessionView={true} resoniteShowSubSessions={false} />}
                                {account.specifics.bio.split('\n').map((line: string, lineIndex: number) => (
                                    <span key={lineIndex}>
                                        {_D(line, debugMode)}
                                        {lineIndex < account.specifics.bio.split('\n').length - 1 && <br/>}
                                    </span>
                                ))}
                            </div>
                        ))}
                    </div>
                )}
            </div>
            </div>

            {!isModalPresentation && <IndividualDetailsModal
                isOpen={isDetailsModalOpen}
                onClose={() => setIsDetailsModalOpen(false)}
                individual={individual}
                debugMode={debugMode}
            />}
        </>
    );
}

export default Individual;