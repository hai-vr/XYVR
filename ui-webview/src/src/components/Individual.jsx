import Account from "./Account.jsx";
import "./Individual.css";
import { useState, useRef, useEffect } from "react";

function Individual({ individual, isVisible = true, showBio = false, showAlias = false, setMergeAccountGuidOrUnd, isBeingMerged = false }) {
    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
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

    const handleMenuAction = (action, event) => {
        event.stopPropagation();
        setIsDropdownOpen(false);

        switch (action) {
            case 'merge':
                if (isBeingMerged) {
                    setMergeAccountGuidOrUnd(undefined);
                }
                else {
                    setMergeAccountGuidOrUnd(individual.guid);
                }
                break;
            case 'details':
                // TODO: Implement show details functionality
                console.log('Show details clicked for:', individual.displayName);
                break;
            default:
                break;
        }
    };

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
        <div className={`individual-container ${!isVisible ? 'hidden' : ''} ${isBeingMerged ? 'being-merged' : ''}`}>
            <div className="individual-header">
                <div className="individual-avatar">
                    {getFirstNonPunctuationChar(individual.displayName)}
                </div>
                <h3 className="individual-name">
                    {individual.displayName}
                </h3>
                {individual.isAnyContact && (
                    <span className="contact-badge">
                        📞 Contact
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
                            <button
                                className="dropdown-item"
                                onClick={(e) => handleMenuAction('merge', e)}
                            >
                                {isBeingMerged ? 'Cancel merge' : 'Merge account...'}
                            </button>
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

            <div className="accounts-container">
                {individual.accounts && individual.accounts.length > 0 ? (
                    <div className="accounts-grid">
                        {individual.accounts.map((account, accountIndex) => (
                            <Account key={account.guid} account={account} showAlias={showAlias} />
                        ))}
                    </div>
                ) : (
                    <div className="no-accounts">
                        📭 No accounts found
                    </div>
                )}
            </div>

            {vrChatLinks.length > 0 && (
                <div className="vrchat-links-list">
                    {vrChatLinks.map((url, linkIndex) => (
                        <div key={linkIndex} className="vrchat-link-item">
                            <a
                                href={url}
                                rel="noopener noreferrer"
                                className="vrchat-link"
                            >
                                {url}
                            </a>
                            <button
                                onClick={(e) => copyToClipboard(url, e)}
                                className="icon-button"
                                title="Copy link to clipboard"
                            >
                                📋
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
                                        {line}
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