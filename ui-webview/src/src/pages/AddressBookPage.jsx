import { useEffect, useState, useMemo, useCallback, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import './AddressBookPage.css'
import Individual from "../components/Individual.jsx"
import {
    isIndividualVisible,
    hasDisplayNameMatch,
    hasIdentifierMatch,
    shouldShowBio
} from './searchUtils.js'

function AddressBookPage({ isDark, setIsDark }) {
    const navigate = useNavigate()
    const searchInputRef = useRef(null)
    const [individuals, setIndividuals] = useState([]);
    const [searchTerm, setSearchTerm] = useState('');
    const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('');
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

    // Reset displayed count when debounced search term or filter changes
    useEffect(() => {
        setDisplayedCount(50);
    }, [debouncedSearchTerm, showOnlyContacts]);

    // Create sorted and filtered individuals array (now using debouncedSearchTerm)
    const sortedAndFilteredIndividuals = useMemo(() => {
        const visibleIndividuals = individuals.filter(ind => isIndividualVisible(ind, debouncedSearchTerm, showOnlyContacts));

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

    // Check if bio should be shown based on search terms
    const showBio = useMemo(() => {
        return shouldShowBio(debouncedSearchTerm);
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

    return (
        <>
            <div className="individuals-container">
                <div className="header-section">
                    <div className="header-content">
                        <h2 className="header-title">
                            {showOnlyContacts && 'Contacts' || 'Contacts & Notes'} ({totalFilteredCount})
                        </h2>

                        <div className="header-buttons">
                            <button
                                className="data-collection-btn"
                                onClick={() => navigate('/data-collection')}
                                title="Go to Data Collection"
                            >
                                📊 Data Collection
                            </button>

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
                </div>

                <div className="search-container">
                    <input
                        ref={searchInputRef}
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
                
                <div>
                    {debouncedSearchTerm && (
                        <div className="search-results-info">
                            {totalFilteredCount === 0
                                ? `No results found for "${debouncedSearchTerm}"`
                                : <>Showing {totalFilteredCount} results.
                                    Type <code className="inline-code-clickable"onClick={() => { setSearchTerm(':help '); focusSearchInput(); }}>:help</code> for help.
                                    Type <code className="inline-code-clickable"onClick={() => { setSearchTerm(searchTerm + ' bio:'); focusSearchInput(); }}>bio:</code> to show bios.</>
                            }
                        </div>
                    )}
                </div>

                <div className="individuals-grid">
                    {displayedIndividuals.map((individual, index) => (
                        <Individual
                            key={individual.id || index}
                            individual={individual}
                            isVisible={true}
                            showBio={showBio}
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
                        <div className="no-results-text">No individuals found matching
                            "<strong>{debouncedSearchTerm}</strong>"
                        </div>
                        <div className="no-results-hint">
                            <p>Try searching by name, note content, or use special terms like:</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:resonite '); focusSearchInput(); }}>app:resonite</code> for Resonite account owners.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:vrchat '); focusSearchInput(); }}>app:vrchat</code> for VRChat account owners.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:cluster '); focusSearchInput(); }}>app:cluster</code> for Cluster account owners.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('app:resonite app:vrchat '); focusSearchInput(); }}>app:resonite app:vrchat</code> for Resonite account owners who also have a VRChat account.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('accounts:>1 '); focusSearchInput(); }}>accounts:&gt;1</code> for users who have more than one account.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('has:alt '); focusSearchInput(); }}>has:alt</code> for users who have more than one non-bot account on the same app.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('links:'); focusSearchInput(); }}>links:<i>misskey</i></code> to search in the links.</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchTerm('bio:'); focusSearchInput(); }}>bio:<i>creator</i></code> to display and search in the bio.</p>
                        </div>
                    </div>
                )}
            </div>
        </>
    )
}

export default AddressBookPage