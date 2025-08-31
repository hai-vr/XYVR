import { useEffect, useState, useMemo } from 'react'
import './App.css'
import Individual from "./Individual.jsx";

function App() {
    const [count, setCount] = useState(0)
    const [appVersion, setAppVersion] = useState('');
    const [individuals, setIndividuals] = useState([]);
    const [searchTerm, setSearchTerm] = useState('');

    useEffect(() => {
        // Wait for WebView2 API to be available
        const initializeApi = async () => {
            if (window.chrome && window.chrome.webview && window.chrome.webview.hostObjects) {
                try {
                    const version = await window.chrome.webview.hostObjects.appApi.GetAppVersion();
                    setAppVersion(version);
                    
                    // Also load individuals when the component loads
                    const allIndividuals = await window.chrome.webview.hostObjects.appApi.GetAllExposedIndividuals();
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
            if (term.startsWith('app:') || term.startsWith('accounts:') || term === 'has:alt' || term === 'has:bot') {
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
            switch (term) {
                case 'app:resonite':
                    return individual.accounts?.some(account => account.namedApp === 1) || false;
                
                case 'app:vrchat':
                    return individual.accounts?.some(account => account.namedApp === 2) || false;
                
                case 'app:cluster':
                    return individual.accounts?.some(account => account.namedApp === 3) || false;
                
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

            return regularTerms.every(term => {
                const kanaVariants = generateKanaVariants(term);

                return kanaVariants.some(variant => {
                    const variantNormalized = removeDiacritics(variant);
                    const noteMatch = removeDiacritics(accountNote.toLowerCase()).includes(variantNormalized);
                    const displayNameMatch = removeDiacritics(accountDisplayName.toLowerCase()).includes(variantNormalized);

                    // Only search in inAppIdentifier if namedApp equals 3 (Cluster)
                    const identifierMatch = account.namedApp === 3 &&
                        removeDiacritics(accountIdentifier.toLowerCase()).includes(variantNormalized);

                    return noteMatch || displayNameMatch || identifierMatch;
                });
            });
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
            if (account.namedApp !== 3) return false;

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

    // Create sorted and filtered individuals array
    const sortedAndFilteredIndividuals = useMemo(() => {
        const visibleIndividuals = individuals.filter(ind => isIndividualVisible(ind, searchTerm));

        if (!searchTerm) {
            return visibleIndividuals;
        }

        // Sort by priority: display name matches first, then identifier matches, then original order
        return visibleIndividuals.sort((a, b) => {
            const aHasDisplayNameMatch = hasDisplayNameMatch(a, searchTerm);
            const bHasDisplayNameMatch = hasDisplayNameMatch(b, searchTerm);
            const aHasIdentifierMatch = hasIdentifierMatch(a, searchTerm);
            const bHasIdentifierMatch = hasIdentifierMatch(b, searchTerm);

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
    }, [individuals, searchTerm]);


    const handleGetTime = async () => {
        if (window.chrome?.webview?.hostObjects?.appApi) {
            try {
                const time = await window.chrome.webview.hostObjects.appApi.GetCurrentTime();
                alert(`Current Time: ${time}`);
            } catch (error) {
                console.error('Error calling API:', error);
            }
        }
    };

    const handleGetAllExposedIndividuals = async () => {
        if (window.chrome?.webview?.hostObjects?.appApi) {
            try {
                const allIndividuals = await window.chrome.webview.hostObjects.appApi.GetAllExposedIndividuals();
                const individualsArray = JSON.parse(allIndividuals);
                setIndividuals(individualsArray);

            } catch (error) {
                console.error('Error calling API:', error);
            }
        }
    };

    const handleShowMessage = () => {
        if (window.chrome?.webview?.hostObjects?.appApi) {
            window.chrome.webview.hostObjects.appApi.ShowMessage('Hello from React!');
        }
    };

    const handleCloseApp = () => {
        if (window.chrome?.webview?.hostObjects?.appApi) {
            window.chrome.webview.hostObjects.appApi.CloseApp();
        }
    };

    // Calculate visible individuals count using the sorted array
    const visibleIndividualsCount = sortedAndFilteredIndividuals.length;

    return (
        <>
            {individuals.length > 0 && (
                <div className="individuals-container">
                    <div className="header-section">
                        <h2 className="header-title">
                            👥 Users & Accounts ({visibleIndividualsCount})
                        </h2>

                        {searchTerm && (
                            <div className="search-results-info">
                                {visibleIndividualsCount === 0
                                    ? `No results found for "${searchTerm}"`
                                    : `Showing ${visibleIndividualsCount} of ${individuals.length} results`
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
                        {sortedAndFilteredIndividuals.map((individual, index) => (
                            <Individual
                                key={individual.id || index}
                                individual={individual}
                                index={index}
                                isVisible={true} // Always visible since we're already filtering
                            />
                        ))}
                    </div>

                    {searchTerm && visibleIndividualsCount === 0 && (
                        <div className="no-results-message">
                            <div className="no-results-icon">🔍</div>
                            <div className="no-results-text">No individuals found matching "<strong>{searchTerm}</strong>"</div>
                            <div className="no-results-hint">
                                Try searching by name, note content, or use special terms like app:resonite, app:vrchat, app:cluster, accounts:&gt;1, has:alt, has:bot
                            </div>
                        </div>
                    )}
                </div>
            )}
        </>
    )
}

export default App