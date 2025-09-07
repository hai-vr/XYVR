import Account from "./Account.jsx";
import "./Individual.css";
import { useState, useRef, useEffect } from "react";
import {Clipboard, Phone} from "lucide-react";
import {accountMatchesFromRegularTerms, anyAccountMatchesSpecialTerms, parseSearchTerms} from "../pages/searchUtils.js";
import {_D, _D2} from "../haiUtils.js";

function Individual({
                        individual,
                        isVisible = true,
                        showBio = false,
                        showAlias = false,
                        setMergeAccountGuidOrUnd,
                        isBeingMerged = false,
                        displayNameOfOtherBeingMergedOrUnd = undefined,
                        fusionAccounts,
                        compactMode,
                        searchTerm,
                        showNotes,
                        demoMode
                    }) {
    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const [filteredAccounts, setFilteredAccounts] = useState([]);
    const dropdownRef = useRef(null);

    // Get all VRChat account links and filter to only show http/https URLs
    const vrChatLinks = individual.accounts
        ?.filter(account => account.namedApp === "VRChat" && account.specifics?.urls?.length > 0)
        ?.flatMap(account => account.specifics.urls)
        // Some users have links that are an empty string. We don't want this because clicking it causes the page to reload.
        // Generally, prevent links that aren't http:// nor https://
        ?.filter(url => url && (url.startsWith('http://') || url.startsWith('https://'))) || [];

    // Get all VRChat account bios
    const vrcBios = showBio && individual.accounts
        ?.filter(account => account.namedApp === "VRChat" && account.specifics?.bio)
        ?.map(account => account.specifics.bio)
        ?.filter(bio => bio.trim() !== '') || [];

    // Close dropdown when clicking outside
    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsDropdownOpen(false);
            }
        };

        document.addEventListener('mousedown', handleClickOutside);
        return () => {
            document.removeEventListener('mousedown', handleClickOutside);
        };
    }, []);

    const copyToClipboard = async (url, event) => {
        event.stopPropagation(); // Prevent the container click event
        await navigator.clipboard.writeText(url);
    };

    const handleMenuAction = async (action, event) => {
        event.stopPropagation();
        setIsDropdownOpen(false);

        switch (action) {
            case 'confirmMerge':
                await fusionAccounts(individual.guid);
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
        if (!compactMode || !searchTerm) {
            setFilteredAccounts(individual.accounts);
            return;
        }

        if (individual.accounts) {
            const { specialTerms, regularTerms } = parseSearchTerms(searchTerm);

            const filtered = individual.accounts.filter(account => {
                if (specialTerms.length > 0 && !anyAccountMatchesSpecialTerms([account], specialTerms, true)) return false;
                if (regularTerms.length === 0) {
                    return true;
                }
                if (accountMatchesFromRegularTerms(account, regularTerms)) return true;

                return false;
            });

            setFilteredAccounts(filtered);
        }
    }, [individual, searchTerm, compactMode])

    // Helper function to get the first non-punctuation character
    const getFirstNonPunctuationChar = (str) => {
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
                        {demoMode ? '?' : getFirstNonPunctuationChar(individual.displayName)}
                    </div>
                    <h3 className="individual-name">
                        {_D(individual.displayName, demoMode)}
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
                                    Merge {_D(displayNameOfOtherBeingMergedOrUnd, demoMode)} into this
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
                        {filteredAccounts.map((account, accountIndex) => (
                            <Account key={account.guid} account={account} showAlias={showAlias} showNotes={showNotes} demoMode={demoMode} />
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
                                href={demoMode ? 'https://example.com' : url}
                                rel="noopener noreferrer"
                                className="vrchat-link"
                            >
                                {demoMode ? 'https://' + _D2(url.replace('http://', '').replace('https://', ''), demoMode, '/') : _D2(url, demoMode, '/')}
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
                                        {_D(line, demoMode)}
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