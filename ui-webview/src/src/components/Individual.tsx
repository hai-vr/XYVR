import Account from "./Account.tsx";
import "./Individual.css";
import React, { useState, useRef, useEffect } from "react";
import {Clipboard, Phone} from "lucide-react";
import {accountMatchesFromRegularTerms, anyAccountMatchesSpecialTerms, parseSearchField} from "../pages/searchUtils.ts";
import {_D, _D2} from "../haiUtils.ts";
import type {FrontAccount, FrontIndividual} from "../types/CoreTypes.ts";
import {type DebugFlags, DemonstrationMode} from "../types/DebugFlags.ts";

interface IndividualProps {
    individual: FrontIndividual;
    isVisible?: boolean;
    showBio?: boolean;
    showAlias?: boolean;
    setMergeAccountGuidOrUnd: (guid: string | undefined) => void;
    isBeingMerged?: boolean;
    displayNameOfOtherBeingMergedOrUnd?: string;
    fusionAccounts: (guid: string) => Promise<void>;
    unmergeAccounts: (guid: string) => Promise<void>;
    compactMode: boolean;
    searchField: string;
    showNotes: boolean;
    debugMode: DebugFlags;
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
                        debugMode
                    }: IndividualProps) {
    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const [filteredAccounts, setFilteredAccounts] = useState<FrontAccount[]>([]);
    const dropdownRef = useRef<HTMLDivElement>(null);

    // Get all VRChat account links and filter to only show http/https URLs
    const vrChatLinks = individual.accounts
        ?.filter(account => account.namedApp === "VRChat" && account.specifics?.urls?.length > 0)
        ?.flatMap(account => account.specifics.urls)
        // Some users have links that are an empty string. We don't want this because clicking it causes the page to reload.
        // Generally, prevent links that aren't http:// nor https://
        ?.filter(url => url && (url.startsWith('http://') || url.startsWith('https://'))) || [];

    // Get all VRChat account bios
    const vrcBios: string[] = showBio && individual.accounts
        ?.filter(account => account.namedApp === "VRChat" && account.specifics?.bio)
        ?.map(account => account.specifics.bio)
        ?.filter(bio => bio.trim() !== '') || [];

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
                await fusionAccounts(individual.guid);
                break;
            case 'unmerge':
                await unmergeAccounts(individual.guid);
                break;
            case 'cancelMerge':
                setMergeAccountGuidOrUnd(undefined);
                break;
            case 'merge':
                setMergeAccountGuidOrUnd(individual.guid);
                break;
            case 'details':
                // TODO: Implement show details functionality
                console.log('Show details clicked for:', individual.displayName);
                break;
            default:
                break;
        }
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

    // Helper function to get the first non-punctuation character
    const getFirstNonPunctuationChar = (str: string) => {
        if (!str) return '?';

        // Regular expression to match various punctuation marks including:
        // - Basic ASCII punctuation
        // - Unicode punctuation categories (Pc, Pd, Pe, Pf, Pi, Po, Ps)
        // - Common CJK punctuation symbols like 【】「」〈〉《》etc.
        const punctuationRegex = /[\p{P}\u3000-\u303F\uFF00-\uFFEF\u2000-\u206F\u2E00-\u2E7F]/u;

        for (let i = 0; i < str.length; i++) {
            const char = str.charAt(i);
            if (!punctuationRegex.test(char) && char.trim() !== '') {
                return char.toUpperCase();
            }
        }

        // If all characters are punctuation or whitespace, return the first character or '?'
        return str.charAt(0).toUpperCase() || '?';
    };

    return (
        <div className={`${!compactMode ? 'individual-container' : 'individual-container-compact'} ${!isVisible ? 'hidden' : ''} ${isBeingMerged ? 'being-merged' : ''}`}>
            {!compactMode && (<>
                <div className="individual-header">
                    <div className="individual-avatar">
                        {debugMode.demoMode !== DemonstrationMode.Disabled ? '?' : getFirstNonPunctuationChar(individual.displayName)}
                    </div>
                    <h3 className="individual-name">
                        {_D(individual.displayName, debugMode)}
                    </h3>
                    {individual.isAnyContact && (
                        <span className="contact-badge">
                            <Phone size={16} />
                            <span>Contact</span>
                        </span>
                    )}
                    <div className="individual-menu" ref={dropdownRef}>
                        <button
                            className="menu-button"
                            onClick={(e) => {
                                e.stopPropagation();
                                setIsDropdownOpen(!isDropdownOpen);
                            }}
                            title="More actions"
                        >
                            ⋯
                        </button>
                        {isDropdownOpen && (
                            <div className="dropdown-menu">
                                {displayNameOfOtherBeingMergedOrUnd !== undefined && !isBeingMerged && (<button
                                    className="dropdown-item"
                                    onClick={(e) => handleMenuAction('confirmMerge', e)}
                                >
                                    Merge {_D(displayNameOfOtherBeingMergedOrUnd, debugMode)} into this
                                </button>)}
                                {displayNameOfOtherBeingMergedOrUnd === undefined && <button
                                    className="dropdown-item"
                                    onClick={(e) => handleMenuAction('merge', e)}
                                >
                                    Merge account...
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
                    </div>
                </div>
            </>)}

            <div className="accounts-container">
                {filteredAccounts && filteredAccounts.length > 0 ? (
                    <div className="accounts-grid">
                        {filteredAccounts.map((account) => (
                            <Account key={account.guid} account={account} showAlias={showAlias} showNotes={showNotes} debugMode={debugMode} imposter={false} showSession={true} isSessionView={false} />
                        ))}
                    </div>
                ) : (
                    <div className="no-accounts">
                        📭 No accounts found
                    </div>
                )}
            </div>

            {!compactMode && vrChatLinks.length > 0 && (
                <div className="vrchat-links-list">
                    {vrChatLinks.map((url, linkIndex) => (
                        <div key={linkIndex} className="vrchat-link-item">
                            <a
                                href={debugMode.demoMode !== DemonstrationMode.Disabled ? 'https://example.com' : url}
                                rel="noopener noreferrer"
                                className="vrchat-link"
                            >
                                {debugMode.demoMode !== DemonstrationMode.Disabled ? 'https://' + _D2(url.replace('http://', '').replace('https://', ''), debugMode, '/') : _D2(url, debugMode, '/')}
                            </a>
                            <button
                                onClick={(e) => copyToClipboard(url, e)}
                                className="icon-button"
                                title="Copy link to clipboard"
                            >
                                <Clipboard size={16} />
                            </button>
                        </div>
                    ))}
                </div>
            )}

            {vrcBios.length > 0 && showBio && (
                <div className="vrchat-bios-container">
                    <div className="vrchat-links-list">
                        {vrcBios.map((bio, bioIndex) => (
                            <div key={bioIndex} className="vrchat-bio-item">
                                {bio.split('\n').map((line, lineIndex) => (
                                    <span key={lineIndex}>
                                        {_D(line, debugMode)}
                                        {lineIndex < bio.split('\n').length - 1 && <br/>}
                                    </span>
                                ))}
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}

export default Individual;