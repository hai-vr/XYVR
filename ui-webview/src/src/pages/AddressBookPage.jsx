import {useCallback, useEffect, useMemo, useRef, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import './AddressBookPage.css'
import '../Header.css'
import Individual from "../components/Individual.jsx"
import {
    hasDisplayNameMatch,
    hasIdentifierMatch,
    isIndividualVisible,
    shouldShowAlias,
    shouldShowBio,
    shouldShowHelp,
    getOnlineStatusPriority
} from './searchUtils.js'
import {
    NotebookPen,
    Moon,
    Sun,
    Glasses,
    Search,
    X,
    Settings,
    UserStar,
    UserPen,
    Binoculars,
    NotebookText, Notebook
} from 'lucide-react'
import DarkModeToggleButton from "../components/DarkModeToggleButton.jsx";
import {_D2} from "../haiUtils.js";

const sortIndividuals = (individuals, searchTerm) => {
    if (!searchTerm) {
        // Sort by online status first, even when there's no search term
        return [...individuals].sort((a, b) => {
            const aPriority = getOnlineStatusPriority(a.onlineStatus);
            const bPriority = getOnlineStatusPriority(b.onlineStatus);

            // First priority: online status (lower number = higher priority)
            if (aPriority !== bPriority) return aPriority - bPriority;

            // If both have same online status, maintain original order
            return 0;
        });
    }

    // Sort by priority: online status first, then display name matches, then identifier matches, then original order
    return [...individuals].sort((a, b) => {
        const aPriority = getOnlineStatusPriority(a.onlineStatus);
        const bPriority = getOnlineStatusPriority(b.onlineStatus);
        const aHasDisplayNameMatch = hasDisplayNameMatch(a, searchTerm);
        const bHasDisplayNameMatch = hasDisplayNameMatch(b, searchTerm);
        const aHasIdentifierMatch = hasIdentifierMatch(a, searchTerm);
        const bHasIdentifierMatch = hasIdentifierMatch(b, searchTerm);

        // display name matches
        if (aHasDisplayNameMatch && !bHasDisplayNameMatch) return -1;
        if (!aHasDisplayNameMatch && bHasDisplayNameMatch) return 1;

        // identifier matches (only if both don't have display name matches and same online status)
        if (!aHasDisplayNameMatch && !bHasDisplayNameMatch) {
            if (aHasIdentifierMatch && !bHasIdentifierMatch) return -1;
            if (!aHasIdentifierMatch && bHasIdentifierMatch) return 1;
        }

        // online status (lower number = higher priority)
        if (aPriority !== bPriority) return aPriority - bPriority;

        // If both have the same priority level, maintain original order
        return 0;
    });
};

// Custom hook for filtering individuals
function AddressBookPage({ isDark, setIsDark, showOnlyContacts, setShowOnlyContacts, compactMode, setCompactMode, showNotes, setShowNotes, demoMode }) {
    const navigate = useNavigate()
    const searchInputRef = useRef(null)
    const [initialized, setInitialized] = useState(false);
    const [individuals, setIndividuals] = useState([]);
    const [sortedIndividuals, setSortedIndividuals] = useState([]);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('');

    const [mergeAccountGuidOrUnd, setMergeAccountGuidOrUnd] = useState(undefined);
    const mergeAccountGuidOrUndRef = useRef(mergeAccountGuidOrUnd);
    const [displayNameOfOtherBeingMergedOrUnd, setDisplayNameOfOtherBeingMergedOrUnd] = useState(undefined);

    // Infinite scrolling state
    const [displayedCount, setDisplayedCount] = useState(50); // Start with 50 items
    const [isLoading, setIsLoading] = useState(false);
    const ITEMS_PER_LOAD = 25; // Load 25 more items each time
    const SEARCH_DELAY = 100; // 300ms delay for search

    useEffect(() => {
        const firstInd = individuals.filter(ind => ind.guid === mergeAccountGuidOrUnd).at(0);
        if (firstInd !== undefined) {
            setDisplayNameOfOtherBeingMergedOrUnd(firstInd.displayName);
        }
        else {
            setDisplayNameOfOtherBeingMergedOrUnd(undefined);
        }
    }, [mergeAccountGuidOrUnd, individuals]);

    // Debounce search term
    useEffect(() => {
        const timer = setTimeout(() => {
            setDebouncedSearchTerm(searchTerm);
        }, SEARCH_DELAY);

        return () => clearTimeout(timer);
    }, [searchTerm, SEARCH_DELAY]);

    useEffect(() => {
        const initializeApi = async () => {
            // Load individuals when the component loads
            const allIndividuals = await window.chrome.webview.hostObjects.appApi.GetAllExposedIndividualsOrderedByContact();
            const individualsArray = JSON.parse(allIndividuals);
            setIndividuals(individualsArray);
            setInitialized(true);
        };

        initializeApi();
    }, []);

    // Reset displayed count when debounced search term or filter changes
    useEffect(() => {
        setDisplayedCount(50);
    }, [debouncedSearchTerm, showOnlyContacts]);

    useEffect(() => {
        const individualUpdated = (event) => {
            console.log('Individual updated event:', event.detail);
            const updatedIndividual = event.detail;

            setIndividuals(prevIndividuals => {
                const existingIndex = prevIndividuals.findIndex(ind => ind.guid === updatedIndividual.guid);

                if (existingIndex !== -1) {
                    const newIndividuals = [...prevIndividuals];
                    newIndividuals[existingIndex] = updatedIndividual;
                    return newIndividuals;
                } else {
                    return [...prevIndividuals, updatedIndividual];
                }
            });
        };
        const liveUpdateMerged = (event) => {
            // TODO: we should just use individualUpdated event and drive-by update the status from there
            console.log('Live update merge event:', event.detail);
            const liveUpdate = event.detail;

            setIndividuals(prevIndividuals => {
                // FIXME: This should be attached to the account itself
                const index = prevIndividuals.findIndex(ind => ind.accounts?.some(acc => acc.qualifiedAppName === liveUpdate.qualifiedAppName && acc.inAppIdentifier === liveUpdate.inAppIdentifier));
                if (index !== -1) {
                    console.log('Found individual to update at index: ' + index);
                    const newIndividuals = [...prevIndividuals];
                    let accounts = prevIndividuals[index].accounts?.map(acc =>
                        acc.qualifiedAppName === liveUpdate.qualifiedAppName && acc.inAppIdentifier === liveUpdate.inAppIdentifier
                            ? {
                                ...acc,
                                onlineStatus: liveUpdate.onlineStatus || acc.onlineStatus,
                                customStatus: liveUpdate.customStatus || acc.customStatus,
                            }
                            : acc
                    );
                    let onlineStatusVals = accounts?.filter(acc => acc.onlineStatus);

                    // Determine the best online status using priority system
                    let bestOnlineStatus = undefined;
                    if (onlineStatusVals.length > 0) {
                        // Find the status with the highest priority (lowest priority number)
                        bestOnlineStatus = onlineStatusVals.reduce((best, acc) => {
                            const bestPriority = getOnlineStatusPriority(best?.onlineStatus);
                            const accPriority = getOnlineStatusPriority(acc.onlineStatus);
                            return accPriority < bestPriority ? acc : best;
                        }).onlineStatus;
                    }

                    newIndividuals[index] = {
                        ...prevIndividuals[index],
                        accounts: [...accounts],
                        onlineStatus: bestOnlineStatus
                    };

                    return newIndividuals;

                } else {
                    console.log('Individual not found for update');
                    return prevIndividuals;
                }
            });
        };

        window.addEventListener('individualUpdated', individualUpdated);
        window.addEventListener('liveUpdateMerged', liveUpdateMerged);
        return () => {
            window.removeEventListener('individualUpdated', individualUpdated);
            window.removeEventListener('liveUpdateMerged', liveUpdateMerged);
        };
    }, []);

    // Use the custom hooks for sorting and filtering
    useEffect(() => {
        console.log(`useFilteredIndividuals useEffect running ${individuals.length} individuals, searchTerm: ${debouncedSearchTerm || 'none'}`);
        // it's faster to filter first then sort on a subset of the data
        setSortedIndividuals(sortIndividuals(individuals.filter(ind => isIndividualVisible(ind, debouncedSearchTerm, showOnlyContacts, mergeAccountGuidOrUnd)), debouncedSearchTerm));
    }, [individuals, debouncedSearchTerm, showOnlyContacts, mergeAccountGuidOrUnd]);

    // Get the currently displayed individuals (for infinite scrolling)
    const displayedIndividuals = useMemo(() => {
        return sortedIndividuals.slice(0, displayedCount);
    }, [sortedIndividuals, displayedCount]);

    // Infinite scroll handler
    const loadMoreItems = useCallback(() => {
        if (isLoading) return;

        setIsLoading(true);

        // Simulate a small delay for better UX
        setTimeout(() => {
            setDisplayedCount(prev => {
                let nextCount = Math.min(prev + ITEMS_PER_LOAD, sortedIndividuals.length);
                if (nextCount > 199) {
                    // If the user scrolls too much, just show everything immediately
                    return sortedIndividuals.length;
                }
                return nextCount;
            });
            setIsLoading(false);
        }, 100);
    }, [isLoading, sortedIndividuals.length]);

    // Scroll event handler
    useEffect(() => {
        const handleScroll = () => {
            if (isLoading) return;

            const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
            const windowHeight = window.innerHeight;
            const documentHeight = document.documentElement.offsetHeight;

            // Load more when user is 200px from the bottom
            if (scrollTop + windowHeight >= documentHeight - 200) {
                if (displayedCount < sortedIndividuals.length) {
                    loadMoreItems();
                }
            }
        };

        window.addEventListener('scroll', handleScroll);
        return () => window.removeEventListener('scroll', handleScroll);
    }, [loadMoreItems, displayedCount, sortedIndividuals.length, isLoading]);

    // Calculate visible individuals count using the filtered array
    const totalFilteredCount = sortedIndividuals.length;

    // Check if bio should be shown based on search terms
    const showBio = useMemo(() => {
        return shouldShowBio(debouncedSearchTerm);
    }, [debouncedSearchTerm]);

    const showHelp = useMemo(() => {
        return shouldShowHelp(debouncedSearchTerm);
    }, [debouncedSearchTerm]);

    const showAlias = useMemo(() => {
        return shouldShowAlias(debouncedSearchTerm);
    }, [debouncedSearchTerm]);

    // Function to focus search input and move cursor to end
    const focusSearchInput = () => {
        if (searchInputRef.current) {
            searchInputRef.current.focus()
            // Move cursor to the end
            const length = searchInputRef.current.value.length
            searchInputRef.current.setSelectionRange(length, length)
        }
    }

    // Show load more button helper
    const hasMoreItems = displayedCount < totalFilteredCount;

    useEffect(() => {
        mergeAccountGuidOrUndRef.current = mergeAccountGuidOrUnd;
    }, [mergeAccountGuidOrUnd]);

    const fusionAccounts = async function (toAugment) {
        const toDestroy = mergeAccountGuidOrUndRef.current;
        if (toDestroy === undefined) return;
        if (toDestroy === toAugment) return;
        if (toAugment === undefined) return;

        await window.chrome.webview.hostObjects.appApi.FusionIndividuals(toAugment, toDestroy);
        setMergeAccountGuidOrUnd(undefined);

        const allIndividuals = await window.chrome.webview.hostObjects.appApi.GetAllExposedIndividualsOrderedByContact();
        const individualsArray = JSON.parse(allIndividuals);
        setIndividuals(individualsArray);
    };

    const unmergeAccounts = async function (toDesolidarize) {
        if (toDesolidarize === undefined) return;

        await window.chrome.webview.hostObjects.appApi.DesolidarizeIndividuals(toDesolidarize);

        const allIndividuals = await window.chrome.webview.hostObjects.appApi.GetAllExposedIndividualsOrderedByContact();
        const individualsArray = JSON.parse(allIndividuals);
        setIndividuals(individualsArray);
    };

    return (
        <>
            <div className="individuals-container">
                <div className="header-group">
                    <div className="header-section">
                        <div className="header-content">
                            <h2 className="header-title">
                                {showOnlyContacts && 'Contacts' || 'Contacts & Notes'} {initialized && <>({totalFilteredCount})</> || <>(...)</>}
                            </h2>

                            <div className="header-buttons">
                                <button
                                    className="theme-toggle-btn"
                                    onClick={() => setCompactMode(!compactMode)}
                                    aria-pressed={compactMode}
                                    title={`${compactMode ? 'Switch to full mode' : 'Switch to compact mode'}`}
                                >
                                    {compactMode ? <Binoculars /> : <Glasses />}
                                </button>
                                <button
                                    className="theme-toggle-btn"
                                    onClick={() => setShowNotes(!showNotes)}
                                    aria-pressed={showNotes}
                                    title={`${showNotes ? 'Switch to hide notes' : 'Switch to show notes'}`}
                                >
                                    {showNotes ? <NotebookText /> : <Notebook />}
                                </button>
                                <button
                                    className="theme-toggle-btn"
                                    onClick={() => setShowOnlyContacts(!showOnlyContacts)}
                                    aria-pressed={showOnlyContacts}
                                    title={`${showOnlyContacts ? 'Switch to show contacts and users with notes' : 'Switch to show only contacts'}`}
                                >
                                    {showOnlyContacts ? <UserStar /> : <UserPen />}
                                </button>
                                <DarkModeToggleButton isDark={isDark} setIsDark={setIsDark} />
                            </div>
                        </div>
                    </div>
                    <div className="header-thin-right">
                        <h2 className="header-title">
                            <button className="header-nav" title="Configure connections" onClick={() => navigate('/data-collection')}><Settings /></button>
                        </h2>
                    </div>
                </div>

                <div className="search-container">
                    <input
                        ref={searchInputRef}
                        type={demoMode ? 'password' : 'text'}
                        placeholder="Search by name or note..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        className="search-input"
                    />
                    <span className="search-icon">
                        <Search />
                    </span>
                    {searchTerm && (
                        <button
                            onClick={() => setSearchTerm('')}
                            className="icon-button search-clear-button"
                        >
                            <X size={16} />
                        </button>
                    )}
                </div>

                {debouncedSearchTerm && (totalFilteredCount === 0 || showHelp) && (
                    <div className="no-results-message">
                        <div className="no-results-icon"><Search size={48}/></div>
                        {!showHelp && <>
                            <div className="no-results-text">No individuals found matching
                                "<strong>{_D2(debouncedSearchTerm, demoMode)}</strong>"
                            </div>
                        </>}
                        <div className="no-results-hint">
                            <p>Try searching by name, note content, or use special terms like:</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('bio:'); focusSearchInput(); }}>bio:<i>creator</i></code> to display and search in the bio.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('links:'); focusSearchInput(); }}>links:<i>misskey</i></code> to search in the links.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('alias:'); focusSearchInput(); }}>alias:<i>aoi</i></code> to search in previous user names.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('accounts:>1 '); focusSearchInput(); }}>accounts:&gt;1</code> for users who have more than one account.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('has:alt '); focusSearchInput(); }}>has:alt</code> for users who have several accounts on the same app.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('on: '); focusSearchInput(); }}>on:</code> for currently online users on any app.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:resonite '); focusSearchInput(); }}>app:resonite</code> for Resonite account owners, and <code className="inline-code-clickable" onClick={() => { setSearchTerm('on:resonite '); focusSearchInput(); }}>on:resonite</code> for currently online users.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:vrchat '); focusSearchInput(); }}>app:vrchat</code> for VRChat account owners, and <code className="inline-code-clickable" onClick={() => { setSearchTerm('on:vrchat '); focusSearchInput(); }}>on:vrchat</code> for currently online users.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:cluster '); focusSearchInput(); }}>app:cluster</code> for Cluster account owners, and <code className="inline-code-clickable" onClick={() => { setSearchTerm('on:cluster '); focusSearchInput(); }}>on:cluster</code> for currently online users.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:chilloutvr '); focusSearchInput(); }}>app:chilloutvr</code> for ChilloutVR account owners, and <code className="inline-code-clickable" onClick={() => { setSearchTerm('on:chilloutvr '); focusSearchInput(); }}>on:chilloutvr</code> for currently online users.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:resonite app:vrchat '); focusSearchInput(); }}>app:resonite app:vrchat</code> for Resonite account owners who also have a VRChat account.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm(':confusables ' + searchTerm); focusSearchInput(); }}>:confusables</code> converts some cyrillic and special characters visually similar to latin when searching for names.</p>

                            <p>Open the <a title="Open https://docs.hai-vr.dev/docs/products/xyvr/search in your browser" href="https://docs.hai-vr.dev/docs/products/xyvr/search">search documentation</a> in your browser.</p>
                        </div>
                    </div>
                )}

                <div>
                    {debouncedSearchTerm && (
                        <div className="search-results-info">
                            {totalFilteredCount === 0
                                ? `No results found for "${debouncedSearchTerm}"`
                                : <>{totalFilteredCount > 1 && `Showing ${totalFilteredCount} results. ` || `Only one result. `}
                                    Type <code className="inline-code-clickable"onClick={() => { setSearchTerm(':help '); focusSearchInput(); }}>:help</code> for help.
                                    Type <code className="inline-code-clickable"onClick={() => { setSearchTerm(searchTerm + ' alias:'); focusSearchInput(); }}>alias:</code> to show previous names.
                                    Type <code className="inline-code-clickable"onClick={() => { setSearchTerm(searchTerm + ' bio:'); focusSearchInput(); }}>bio:</code> to show bios.</>
                            }
                        </div>
                    )}
                </div>

                <div className={`individuals-grid ${compactMode ? 'compact-mode' : ''}`}>
                    {displayedIndividuals.map((individual, index) => (
                        <Individual
                            key={individual.guid || index}
                            individual={individual}
                            isVisible={true}
                            showBio={showBio}
                            showAlias={showAlias}
                            setMergeAccountGuidOrUnd={setMergeAccountGuidOrUnd}
                            isBeingMerged={mergeAccountGuidOrUnd === individual.guid}
                            displayNameOfOtherBeingMergedOrUnd={displayNameOfOtherBeingMergedOrUnd}
                            fusionAccounts={fusionAccounts}
                            unmergeAccounts={unmergeAccounts}
                            compactMode={compactMode}
                            searchTerm={debouncedSearchTerm}
                            showNotes={showNotes}
                            demoMode={demoMode}
                        />
                    ))}
                </div>

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
                            >
                                Load More ({totalFilteredCount - displayedCount} remaining)
                            </button>
                        )}
                    </div>
                )}
            </div>
        </>
    )
}

export default AddressBookPage