import { useEffect, useState, useMemo, useCallback } from 'react'
import './AddressBookPage.css'
import Individual from "../Individual.jsx"

function AddressBookPage() {
    const [individuals, setIndividuals] = useState([]);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('');
    const [isDark, setIsDark] = useState(false)
    const [showOnlyContacts, setShowOnlyContacts] = useState(false)

    // Infinite scrolling state
    const [displayedCount, setDisplayedCount] = useState(50); // Start with 50 items
    const [isLoading, setIsLoading] = useState(false);
    const ITEMS_PER_LOAD = 25; // Load 25 more items each time
    const SEARCH_DELAY = 100; // 300ms delay for search

    // Debounce search term
    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearchTerm(searchTerm);
        }, SEARCH_DELAY);

        return () => clearTimeout(timer);
    }, [searchTerm, SEARCH_DELAY]);

    useEffect(() => {
        // Wait for WebView2 API to be available
        const initializeApi = async () => {
            if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
                try {
                    // Load individuals when the component loads
                    const allIndividuals = await window.chrome.webview.hostObjects.appApi.GetAllExposedIndividualsOrderedByContact();
                    const individualsArray = JSON.parse(allIndividuals);
                    setIndividuals(individualsArray);

                } catch (error) {
                    console.error('API not ready yet:', error);
                }
            }
        };

        // Try to initialize immediately
        initializeApi();

        // Also listen for when the DOM is fully loaded
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initializeApi);
        }

        return () => {
            document.removeEventListener('DOMContentLoaded', initializeApi);
        };
    }, []);

    // Separate useEffect for theme changes
    useEffect(() => {
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
    }, [isDark]);

    // Reset displayed count when debounced search term or filter changes
    useEffect(() => {
        setDisplayedCount(50);
    }, [debouncedSearchTerm, showOnlyContacts]);

    // Remove the filteredIndividuals calculation and pass search logic to Individual components
    const removeDiacritics = (str) => {
        return str.normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '') // Replaces diacritics
            .replace(/\u2024/g, '.') // Replaces the crappy "one dot leader" character with a proper period
            .replace(/[\uFF00-\uFFEF]/g, (char) => {
                // Convert full-width characters to half-width
                const code = char.charCodeAt(0);
                if (code >= 0xFF01 && code <= 0xFF5E) {
                    // Full-width ASCII characters
                    return String.fromCharCode(code - 0xFEE0);
                }
                return char;
            });

    };

    // This is massively flawed
    const romajiToHiragana = {
        'a': 'あ', 'i': 'い', 'u': 'う', 'e': 'え', 'o': 'お',
        'ka': 'か', 'ki': 'き', 'ku': 'く', 'ke': 'け', 'ko': 'こ',
        'ga': 'が', 'gi': 'ぎ', 'gu': 'ぐ', 'ge': 'げ', 'go': 'ご',
        'sa': 'さ', 'shi': 'し', 'su': 'す', 'se': 'せ', 'so': 'そ',
        'za': 'ざ', 'ji': 'じ', 'zu': 'ず', 'ze': 'ぜ', 'zo': 'ぞ',
        'ta': 'た', 'chi': 'ち', 'tsu': 'つ', 'te': 'て', 'to': 'と',
        'da': 'だ', 'di': 'ぢ', 'du': 'づ', 'de': 'で', 'do': 'ど',
        'na': 'な', 'ni': 'に', 'nu': 'ぬ', 'ne': 'ね', 'no': 'の',
        'ha': 'は', 'hi': 'ひ', 'fu': 'ふ', 'he': 'へ', 'ho': 'ほ',
        'ba': 'ば', 'bi': 'び', 'bu': 'ぶ', 'be': 'べ', 'bo': 'ぼ',
        'pa': 'ぱ', 'pi': 'ぴ', 'pu': 'ぷ', 'pe': 'ぺ', 'po': 'ぽ',
        'ma': 'ま', 'mi': 'み', 'mu': 'む', 'me': 'め', 'mo': 'も',
        'ya': 'や', 'yu': 'ゆ', 'yo': 'よ',
        'ra': 'ら', 'ri': 'り', 'ru': 'る', 're': 'れ', 'ro': 'ろ',
        'wa': 'わ', 'wi': 'ゐ', 'we': 'ゑ', 'wo': 'を', 'n': 'ん',
        'kya': 'きゃ', 'kyu': 'きゅ', 'kyo': 'きょ',
        'gya': 'ぎゃ', 'gyu': 'ぎゅ', 'gyo': 'ぎょ',
        'sha': 'しゃ', 'shu': 'しゅ', 'sho': 'しょ',
        'ja': 'じゃ', 'ju': 'じゅ', 'jo': 'じょ',
        'cha': 'ちゃ', 'chu': 'ちゅ', 'cho': 'ちょ',
        'nya': 'にゃ', 'nyu': 'にゅ', 'nyo': 'にょ',
        'hya': 'ひゃ', 'hyu': 'ひゅ', 'hyo': 'ひょ',
        'bya': 'びゃ', 'byu': 'びゅ', 'byo': 'びょ',
        'pya': 'ぴゃ', 'pyu': 'ぴゅ', 'pyo': 'ぴょ',
        'mya': 'みゃ', 'myu': 'みゅ', 'myo': 'みょ',
        'rya': 'りゃ', 'ryu': 'りゅ', 'ryo': 'りょ'
    };

    const hiraganaToKatakana = (hiragana) => {
        return hiragana.replace(/[\u3041-\u3096]/g, (char) => {
            return String.fromCharCode(char.charCodeAt(0) + 0x60);
        });
    };

    const convertRomajiToKana = (romaji) => {
        let result = romaji.toLowerCase();
        const sortedKeys = Object.keys(romajiToHiragana).sort((a, b) => b.length - a.length);

        for (const key of sortedKeys) {
            result = result.replace(new RegExp(key, 'g'), romajiToHiragana[key]);
        }

        return result;
    };

    const generateKanaVariants = (term) => {
        const variants = [term];

        // Try to convert romaji to hiragana
        const hiragana = convertRomajiToKana(term);
        if (hiragana !== term) {
            variants.push(hiragana);

            // Convert hiragana to katakana
            const katakana = hiraganaToKatakana(hiragana);
            variants.push(katakana);
        }

        return variants;
    };

    // Parse special search terms and separate them from regular search terms
    const parseSearchTerms = (searchTerm) => {
        const terms = searchTerm.toLowerCase().trim().split(' ').filter(term => term.trim() !== '');
        const specialTerms = [];
        const regularTerms = [];

        terms.forEach(term => {
            if (term.startsWith('app:') || term.startsWith('accounts:') || term.startsWith('links:') || term.startsWith('bio:') || term === 'has:alt' || term === 'has:bot') {
                specialTerms.push(term);
            } else {
                regularTerms.push(term);
            }
        });

        return { specialTerms, regularTerms };
    };

    // Check if individual matches special search terms
    const matchesSpecialTerms = (individual, specialTerms) => {
        return specialTerms.every(term => {
            if (term.startsWith('links:')) {
                const searchString = term.substring(6); // Remove 'links:' prefix
                if (!searchString) return true; // Empty search string matches all

                return individual.accounts?.some(account =>
                    account.specifics?.urls?.some(url =>
                        url.toLowerCase().includes(searchString)
                    )
                ) || false;
            }

            if (term.startsWith('bio:')) {
                const searchString = term.substring(4); // Remove 'bio:' prefix
                if (!searchString) return true; // Empty search string matches all

                return individual.accounts?.some(account =>
                    account.specifics?.bio?.toLowerCase().includes(searchString)
                ) || false;
            }

            switch (term) {
                case 'app:resonite':
                    return individual.accounts?.some(account => account.namedApp === "Resonite") || false;

                case 'app:vrchat':
                    return individual.accounts?.some(account => account.namedApp === "VRChat") || false;

                case 'app:cluster':
                    return individual.accounts?.some(account => account.namedApp === "Cluster") || false;

                case 'has:bot':
                    return individual.accounts?.some(account => account.isTechnical) || false;

                case 'has:alt': {
                    if (!individual.accounts) return false;

                    // Group accounts by namedApp, excluding technical accounts
                    const accountGroups = {};
                    individual.accounts.forEach(account => {
                        if (account.isTechnical === false || account.isTechnical === undefined) {
                            if (!accountGroups[account.namedApp]) {
                                accountGroups[account.namedApp] = 0;
                            }
                            accountGroups[account.namedApp]++;
                        }
                    });

                    // Check if any namedApp has more than one account
                    return Object.values(accountGroups).some(count => count > 1);
                }

                default:
                    if (term.startsWith('accounts:>')) {
                        const minCount = parseInt(term.substring(10));
                        if (isNaN(minCount)) return false;

                        const accountCount = individual.accounts?.length || 0;
                        return accountCount > minCount;
                    }
                    return false;
            }
        });
    };

    const isIndividualVisible = (individual, searchTerm) => {
        // First apply the contact filter
        if (showOnlyContacts && !individual.isAnyContact) {
            return false;
        }

        if (!searchTerm) return true;

        const { specialTerms, regularTerms } = parseSearchTerms(searchTerm);

        // Check special terms first
        if (specialTerms.length > 0 && !matchesSpecialTerms(individual, specialTerms)) {
            return false;
        }

        // If there are no regular terms, and special terms matched, return true
        if (regularTerms.length === 0) {
            return true;
        }

        // Check regular search terms (existing logic)
        const displayName = individual.displayName || '';
        const individualNote = individual.note?.text || '';

        const individualMatch = regularTerms.every(term => {
            const kanaVariants = generateKanaVariants(term);

            return kanaVariants.some(variant => {
                const variantNormalized = removeDiacritics(variant);
                return removeDiacritics(displayName.toLowerCase()).includes(variantNormalized) ||
                    removeDiacritics(individualNote.toLowerCase()).includes(variantNormalized);
            });
        });

        if (individualMatch) return true;

        const accountNotesMatch = individual.accounts?.some(account => {
            const accountNote = account.note?.text || '';
            const accountDisplayName = account.inAppDisplayName || '';
            const accountIdentifier = account.inAppIdentifier || '';

            // Check account-level matches
            const accountMatch = regularTerms.every(term => {
                const kanaVariants = generateKanaVariants(term);

                return kanaVariants.some(variant => {
                    const variantNormalized = removeDiacritics(variant);
                    const noteMatch = removeDiacritics(accountNote.toLowerCase()).includes(variantNormalized);
                    const displayNameMatch = removeDiacritics(accountDisplayName.toLowerCase()).includes(variantNormalized);

                    // Only search in inAppIdentifier if namedApp equals 3 (Cluster)
                    const identifierMatch = account.namedApp === "Cluster" &&
                        removeDiacritics(accountIdentifier.toLowerCase()).includes(variantNormalized);

                    return noteMatch || displayNameMatch || identifierMatch;
                });
            });

            if (accountMatch) return true;

            // Check caller notes
            const callerNotesMatch = account.callers?.some(caller => {
                const callerNote = caller.note?.text || '';

                return regularTerms.every(term => {
                    const kanaVariants = generateKanaVariants(term);

                    return kanaVariants.some(variant => {
                        const variantNormalized = removeDiacritics(variant);
                        return removeDiacritics(callerNote.toLowerCase()).includes(variantNormalized);
                    });
                });
            }) || false;

            return callerNotesMatch;
        }) || false;

        return accountNotesMatch;
    };

    // Function to check if search terms match display name
    const hasDisplayNameMatch = (individual, searchTerm) => {
        if (!searchTerm) return false;

        const { regularTerms } = parseSearchTerms(searchTerm);
        if (regularTerms.length === 0) return false;

        const displayName = individual.displayName || '';

        return regularTerms.every(term => {
            const kanaVariants = generateKanaVariants(term);

            return kanaVariants.some(variant => {
                const variantNormalized = removeDiacritics(variant);
                return removeDiacritics(displayName.toLowerCase()).includes(variantNormalized);
            });
        });
    };

    // Function to check if search terms match inAppIdentifier (only for namedApp === 3)
    const hasIdentifierMatch = (individual, searchTerm) => {
        if (!searchTerm) return false;

        const { regularTerms } = parseSearchTerms(searchTerm);
        if (regularTerms.length === 0) return false;

        return individual.accounts?.some(account => {
            // Only check identifier for namedApp === 3 (Cluster)
            if (account.namedApp !== "Cluster") return false;

            const accountIdentifier = account.inAppIdentifier || '';

            return regularTerms.every(term => {
                const kanaVariants = generateKanaVariants(term);

                return kanaVariants.some(variant => {
                    const variantNormalized = removeDiacritics(variant);
                    return removeDiacritics(accountIdentifier.toLowerCase()).includes(variantNormalized);
                });
            });
        }) || false;
    };

    // Create sorted and filtered individuals array (now using debouncedSearchTerm)
    const sortedAndFilteredIndividuals = useMemo(() => {
        const visibleIndividuals = individuals.filter(ind => isIndividualVisible(ind, debouncedSearchTerm));

        if (!debouncedSearchTerm) {
            return visibleIndividuals;
        }

        // Sort by priority: display name matches first, then identifier matches, then original order
        return visibleIndividuals.sort((a, b) => {
            const aHasDisplayNameMatch = hasDisplayNameMatch(a, debouncedSearchTerm);
            const bHasDisplayNameMatch = hasDisplayNameMatch(b, debouncedSearchTerm);
            const aHasIdentifierMatch = hasIdentifierMatch(a, debouncedSearchTerm);
            const bHasIdentifierMatch = hasIdentifierMatch(b, debouncedSearchTerm);

            // First priority: display name matches
            if (aHasDisplayNameMatch && !bHasDisplayNameMatch) return -1;
            if (!aHasDisplayNameMatch && bHasDisplayNameMatch) return 1;

            // Second priority: identifier matches (only if both don't have display name matches)
            if (!aHasDisplayNameMatch && !bHasDisplayNameMatch) {
                if (aHasIdentifierMatch && !bHasIdentifierMatch) return -1;
                if (!aHasIdentifierMatch && bHasIdentifierMatch) return 1;
            }

            // If both have the same priority level, maintain original order
            return 0;
        });
    }, [individuals, debouncedSearchTerm, showOnlyContacts]);

    // Get the currently displayed individuals (for infinite scrolling)
    const displayedIndividuals = useMemo(() => {
        return sortedAndFilteredIndividuals.slice(0, displayedCount);
    }, [sortedAndFilteredIndividuals, displayedCount]);

    // Infinite scroll handler
    const loadMoreItems = useCallback(() => {
        if (isLoading) return;

        setIsLoading(true);

        // Simulate a small delay for better UX
        setTimeout(() => {
            setDisplayedCount(prev => Math.min(prev + ITEMS_PER_LOAD, sortedAndFilteredIndividuals.length));
            setIsLoading(false);
        }, 100);
    }, [isLoading, sortedAndFilteredIndividuals.length]);

    // Scroll event handler
    useEffect(() => {
        const handleScroll = () => {
            if (isLoading) return;

            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            const windowHeight = window.innerHeight;
            const documentHeight = document.documentElement.offsetHeight;

            // Load more when user is 200px from the bottom
            if (scrollTop + windowHeight >= documentHeight - 200) {
                if (displayedCount < sortedAndFilteredIndividuals.length) {
                    loadMoreItems();
                }
            }
        };

        window.addEventListener('scroll', handleScroll);
        return () => window.removeEventListener('scroll', handleScroll);
    }, [loadMoreItems, displayedCount, sortedAndFilteredIndividuals.length, isLoading]);

    // Calculate visible individuals count using the sorted array
    const totalFilteredCount = sortedAndFilteredIndividuals.length;

    // Add this after the existing parseSearchTerms function call in isIndividualVisible or create a new useMemo (now using debouncedSearchTerm)
    const showBio = useMemo(() => {
        if (!debouncedSearchTerm) return false;
        const { specialTerms } = parseSearchTerms(debouncedSearchTerm);
        return specialTerms.some(term => term.startsWith('bio:'));
    }, [debouncedSearchTerm]);

    // Show load more button helper
    const hasMoreItems = displayedCount < totalFilteredCount;

    return (
        <>
            {individuals.length > 0 && (
                <div className="individuals-container">
                    <div className="header-section">
                        <div className="header-content">
                            <h2 className="header-title">
                                Users & Accounts ({totalFilteredCount})
                            </h2>

                            <div className="header-buttons">
                                <button
                                    className={`contacts-filter-btn ${showOnlyContacts ? 'active' : ''}`}
                                    onClick={() => setShowOnlyContacts(!showOnlyContacts)}
                                    title={`${showOnlyContacts ? 'Show all individuals' : 'Show only contacts'}`}
                                >
                                    Only Contacts
                                </button>

                                <button
                                    className="theme-toggle-btn"
                                    onClick={() => setIsDark(!isDark)}
                                    title={`Switch to ${isDark ? 'Light' : 'Dark'} Mode`}
                                >
                                    {isDark ? '🌙' : '☀️'}
                                </button>
                            </div>
                        </div>

                        {debouncedSearchTerm && (
                            <div className="search-results-info">
                                {totalFilteredCount === 0
                                    ? `No results found for "${debouncedSearchTerm}"`
                                    : `Showing ${displayedCount} of ${totalFilteredCount} results${displayedCount < totalFilteredCount ? ' (scroll for more)' : ''}`
                                }
                            </div>
                        )}
                    </div>

                    {/* Search field */}
                    <div className="search-container">
                        <input
                            type="text"
                            placeholder="Search by name or note..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className="search-input"
                        />
                        <span className="search-icon">
                            🔍
                        </span>
                        {searchTerm && (
                            <button
                                onClick={() => setSearchTerm('')}
                                className="search-clear-button"
                            >
                                ✕
                            </button>
                        )}
                    </div>

                    <div className="individuals-grid">
                        {displayedIndividuals.map((individual, index) => (
                            <Individual
                                key={individual.id || index}
                                individual={individual}
                                index={index}
                                isVisible={true}
                                showBio={showBio}
                            />
                        ))}
                    </div>

                    {/* Loading indicator and load more button */}
                    {hasMoreItems && (
                        <div className="load-more-section">
                            {isLoading ? (
                                <div className="loading-indicator">
                                    <div className="loading-spinner"></div>
                                    <span>Loading more results...</span>
                                </div>
                            ) : (
                                <button
                                    onClick={loadMoreItems}
                                    className="load-more-button"
                                >
                                    Load More ({totalFilteredCount - displayedCount} remaining)
                                </button>
                            )}
                        </div>
                    )}

                    {debouncedSearchTerm && totalFilteredCount === 0 && (
                        <div className="no-results-message">
                            <div className="no-results-icon">🔍</div>
                            <div className="no-results-text">No individuals found matching "<strong>{debouncedSearchTerm}</strong>"</div>
                            <div className="no-results-hint">
                                Try searching by name, note content, or use special terms like app:resonite, app:vrchat, app:cluster, accounts:&gt;1, has:alt, has:bot, links:misskey, bio:creator
                            </div>
                        </div>
                    )}
                </div>
            )}
        </>
    )
}

export default AddressBookPage