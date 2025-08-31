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

    const isIndividualVisible = (individual, searchTerm) => {
        if (!searchTerm) return true;

        const displayName = individual.displayName || '';
        const individualNote = individual.note?.text || '';

        const searchTerms = searchTerm.toLowerCase().split(' ').filter(term => term.trim() !== '');

        if (searchTerms.length === 0) return true;

        const individualMatch = searchTerms.every(term => {
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

            return searchTerms.every(term => {
                const kanaVariants = generateKanaVariants(term);

                return kanaVariants.some(variant => {
                    const variantNormalized = removeDiacritics(variant);
                    return removeDiacritics(accountNote.toLowerCase()).includes(variantNormalized) ||
                        removeDiacritics(accountDisplayName.toLowerCase()).includes(variantNormalized);
                });
            });
        }) || false;

        return accountNotesMatch;
    };



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

    // Calculate visible individuals count once using useMemo
    const visibleIndividualsCount = useMemo(() => {
        return individuals.filter(ind => isIndividualVisible(ind, searchTerm)).length;
    }, [individuals, searchTerm]);

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
                        {individuals.map((individual, index) => (
                            <Individual
                                key={individual.id || index}
                                individual={individual}
                                index={index}
                                isVisible={isIndividualVisible(individual, searchTerm)}
                            />
                        ))}
                    </div>

                    {searchTerm && visibleIndividualsCount === 0 && (
                        <div className="no-results-message">
                            <div className="no-results-icon">🔍</div>
                            <div className="no-results-text">No individuals found matching "<strong>{searchTerm}</strong>"</div>
                            <div className="no-results-hint">
                                Try searching by name or note content
                            </div>
                        </div>
                    )}
                </div>
            )}
        </>
    )
}

export default App