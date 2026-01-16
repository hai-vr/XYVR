import {useCallback, useEffect, useMemo, useRef, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import './AddressBookPage.css'
import '../components/LiveSession.css'
import '../components/Individual.css'
import '../Header.css'
import Individual from "../components/Individual.tsx"
import {
    getOnlineStatusPriority,
    hasDisplayNameMatch,
    hasIdentifierMatch,
    isIndividualVisible,
    parseSearchField,
    shouldShowAlias,
    shouldShowBio,
    shouldShowHelp
} from './searchUtils.ts'
import {
    Binoculars,
    Glasses, Image,
    ImageOff,
    Notebook,
    NotebookText,
    Search,
    Settings,
    UserPen,
    UserStar,
    X
} from 'lucide-react'
import DarkModeToggleButton from "../components/DarkModeToggleButton.tsx";
import {_D2} from "../haiUtils.ts";
import {type FrontIndividual, type NamedAppType, OnlineStatus} from "../types/CoreTypes.ts";
import {type FrontLiveSession, type FrontLiveUserUpdate, LiveSessionKnowledge} from "../types/LiveUpdateTypes.ts";
import {type DebugFlags, DemonstrationMode} from "../types/DebugFlags.ts";
import {LiveSession} from "../components/LiveSession.tsx";
import {SearchFilter} from "../components/SearchFilter.tsx";
import {DotNetApi} from "../DotNetApi.ts";
import {useTranslation} from "react-i18next";
import IndividualDetailsModal from "../components/IndividualDetailsModal.tsx";
import Account from "../components/Account.tsx";
import {SupportedApps, SupportedAppsByNamedApp} from "../supported-apps.tsx";

const sortIndividuals = (individuals: FrontIndividual[], unparsedSearchField: string) => {
    if (!unparsedSearchField) {
        // Sort by online status first, even when there's no search term
        return [...individuals].sort((a, b) => {
            let aHasAnyKnownSession = a.accounts.some(it => it.mainSession?.knowledge === LiveSessionKnowledge.Known);
            let bHasAnyKnownSession = b.accounts.some(it => it.mainSession?.knowledge === LiveSessionKnowledge.Known);
            if (aHasAnyKnownSession && !bHasAnyKnownSession) return -1;
            if (!aHasAnyKnownSession && bHasAnyKnownSession) return 1;

            // First priority: online status (lower number = higher priority)
            const aPriority = getOnlineStatusPriority(a.onlineStatus || OnlineStatus.Offline);
            const bPriority = getOnlineStatusPriority(b.onlineStatus || OnlineStatus.Offline);
            if (aPriority !== bPriority) return aPriority - bPriority;

            return 0;
        });
    }

    const { regularTerms } = parseSearchField(unparsedSearchField);

    // Sort by priority: online status first, then display name matches, then identifier matches, then original order
    return [...individuals].sort((a, b) => {
        const aHasDisplayNameMatch = hasDisplayNameMatch(a, regularTerms);
        const bHasDisplayNameMatch = hasDisplayNameMatch(b, regularTerms);

        // display name matches
        if (aHasDisplayNameMatch && !bHasDisplayNameMatch) return -1;
        if (!aHasDisplayNameMatch && bHasDisplayNameMatch) return 1;

        const aHasIdentifierMatch = hasIdentifierMatch(a, regularTerms);
        const bHasIdentifierMatch = hasIdentifierMatch(b, regularTerms);

        // identifier matches (only if both don't have display name matches and same online status)
        if (!aHasDisplayNameMatch && !bHasDisplayNameMatch) {
            if (aHasIdentifierMatch && !bHasIdentifierMatch) return -1;
            if (!aHasIdentifierMatch && bHasIdentifierMatch) return 1;
        }

        let aHasAnyKnownSession = a.accounts.some(it => it.mainSession?.knowledge === LiveSessionKnowledge.Known);
        let bHasAnyKnownSession = b.accounts.some(it => it.mainSession?.knowledge === LiveSessionKnowledge.Known);
        if (aHasAnyKnownSession && !bHasAnyKnownSession) return -1;
        if (!aHasAnyKnownSession && bHasAnyKnownSession) return 1;

        // online status (lower number = higher priority)
        const aPriority = getOnlineStatusPriority(a.onlineStatus || OnlineStatus.Offline);
        const bPriority = getOnlineStatusPriority(b.onlineStatus || OnlineStatus.Offline);
        if (aPriority !== bPriority) return aPriority - bPriority;

        // If both have the same priority level, maintain original order
        return 0;
    });
};

interface AddressBookPageProps {
    isDark: boolean;
    setIsDark: (isDark: boolean) => void;
    showOnlyContacts: boolean;
    setShowOnlyContacts: (showOnlyContacts: boolean) => void;
    compactMode: boolean;
    setCompactMode: (compactMode: boolean) => void;
    portraits: boolean;
    setPortraits: (portraits: boolean) => void;
    showNotes: boolean;
    setShowNotes: (showNotes: boolean) => void;
    debugMode: DebugFlags;
    resoniteShowSubSessions: boolean,
}

function AddressBookPage({ isDark,
                             setIsDark,
                             showOnlyContacts,
                             setShowOnlyContacts,
                             compactMode,
                             setCompactMode,
                             portraits,
                             setPortraits,
                             showNotes,
                             setShowNotes,
                             debugMode,
                             resoniteShowSubSessions,
}: AddressBookPageProps) {
    const dotNetApi = new DotNetApi();
    const { t } = useTranslation();

    const navigate = useNavigate()
    const searchInputRef = useRef<HTMLInputElement>(null)
    // @ts-ignore
    const [initialized, setInitialized] = useState(false);
    const [individuals, setIndividuals] = useState<FrontIndividual[]>([]);
    const [sortedIndividuals, setSortedIndividuals] = useState<FrontIndividual[]>([]);
    const [searchField, setSearchField] = useState('');
    const [debouncedSearchField, setDebouncedSearchField] = useState('');

    const [mergeAccountGuidOrUnd, setMergeAccountGuidOrUnd] = useState<string | undefined>(undefined);
    const mergeAccountGuidOrUndRef = useRef(mergeAccountGuidOrUnd);
    const [displayNameOfOtherBeingMergedOrUnd, setDisplayNameOfOtherBeingMergedOrUnd] = useState<string | undefined>(undefined);

    const [liveSessionArray, setLiveSessionArray] = useState<FrontLiveSession[]>([]);
    const [modalIndividual, setModalIndividual] = useState<FrontIndividual | undefined>(undefined);

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
            setDebouncedSearchField(searchField);
        }, SEARCH_DELAY);

        return () => clearTimeout(timer);
    }, [searchField, SEARCH_DELAY]);

    useEffect(() => {
        const initializeApi = async () => {
            // Load individuals when the component loads
            const allIndividuals = await dotNetApi.appApiGetAllExposedIndividualsOrderedByContact();
            const individualsArray: FrontIndividual[] = JSON.parse(allIndividuals);

            const allLiveSessionData = await dotNetApi.liveApiGetAllExistingLiveSessionData();
            const liveSessionArray = JSON.parse(allLiveSessionData);

            setIndividuals(individualsArray);
            setLiveSessionArray(liveSessionArray);
            setInitialized(true);
        };

        initializeApi();
    }, []);

    // Reset displayed count when debounced search term or filter changes
    useEffect(() => {
        setDisplayedCount(50);
    }, [debouncedSearchField, showOnlyContacts]);

    useEffect(() => {
        const individualUpdated = (event: any) => {
            console.log('Individual updated event:', event.detail);
            const updatedIndividual: FrontIndividual = event.detail;

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
        const liveUpdateMerged = (event: any) => {
            // TODO: we should just use individualUpdated event and drive-by update the status from there
            console.log('Live update merge event:', event.detail);
            const liveUpdate: FrontLiveUserUpdate = event.detail;

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
                                mainSession: liveUpdate.mainSession || acc.mainSession,
                                multiSessions: liveUpdate.multiSessions || acc.multiSessions,
                            }
                            : acc
                    );
                    let onlineStatusVals = accounts?.filter(acc => acc.onlineStatus);

                    // Determine the best online status using priority system
                    let bestOnlineStatus = undefined;
                    if (onlineStatusVals.length > 0) {
                        // Find the status with the highest priority (lowest priority number)
                        bestOnlineStatus = onlineStatusVals.reduce((best, acc) => {
                            const bestPriority = getOnlineStatusPriority(best?.onlineStatus || OnlineStatus.Offline);
                            const accPriority = getOnlineStatusPriority(acc.onlineStatus || OnlineStatus.Offline);
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
        const liveSessionUpdated = (event: any) => {
            console.log('Live session updated event:', event.detail);
            const liveSession: FrontLiveSession = event.detail;

            setLiveSessionArray(prevSessions => {
                const existingIndex = prevSessions.findIndex(session => session.guid === liveSession.guid);

                if (existingIndex !== -1) {
                    const newSessions = [...prevSessions];
                    newSessions[existingIndex] = liveSession;
                    return newSessions;
                } else {
                    return [...prevSessions, liveSession];
                }
            });
        }

        window.addEventListener('individualUpdated', individualUpdated);
        window.addEventListener('liveUpdateMerged', liveUpdateMerged);
        window.addEventListener('liveSessionUpdated', liveSessionUpdated);
        return () => {
            window.removeEventListener('individualUpdated', individualUpdated);
            window.removeEventListener('liveUpdateMerged', liveUpdateMerged);
            window.removeEventListener('liveSessionUpdated', liveSessionUpdated);
        };
    }, []);

    // Use the custom hooks for sorting and filtering
    useEffect(() => {
        console.log(`useFilteredIndividuals useEffect running ${individuals.length} individuals, searchTerm: ${debouncedSearchField || 'none'}`);
        // it's faster to filter first then sort on a subset of the data
        setSortedIndividuals(sortIndividuals(individuals.filter(ind => isIndividualVisible(ind, debouncedSearchField, showOnlyContacts, mergeAccountGuidOrUnd)), debouncedSearchField));
    }, [individuals, debouncedSearchField, showOnlyContacts, mergeAccountGuidOrUnd]);

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
        return shouldShowBio(debouncedSearchField);
    }, [debouncedSearchField]);

    const showHelp = useMemo(() => {
        return shouldShowHelp(debouncedSearchField);
    }, [debouncedSearchField]);

    const showAlias = useMemo(() => {
        return shouldShowAlias(debouncedSearchField);
    }, [debouncedSearchField]);

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
    
    const isSimpleSearch = !debouncedSearchField.includes(' ') && debouncedSearchField.startsWith('on:');

    useEffect(() => {
        mergeAccountGuidOrUndRef.current = mergeAccountGuidOrUnd;
    }, [mergeAccountGuidOrUnd]);

    const fusionAccounts = async function (toAugment?: string) {
        const toDestroy = mergeAccountGuidOrUndRef.current;
        if (toDestroy === undefined) return;
        if (toDestroy === toAugment) return;
        if (toAugment === undefined) return;

        await dotNetApi.appApiFusionIndividuals(toAugment, toDestroy);
        setMergeAccountGuidOrUnd(undefined);

        const allIndividuals = await dotNetApi.appApiGetAllExposedIndividualsOrderedByContact();
        const individualsArray: FrontIndividual[] = JSON.parse(allIndividuals);
        setIndividuals(individualsArray);
    };

    const unmergeAccounts = async function (toDesolidarize?: string) {
        if (toDesolidarize === undefined) return;

        await dotNetApi.appApiDesolidarizeIndividuals(toDesolidarize);

        const allIndividuals = await dotNetApi.appApiGetAllExposedIndividualsOrderedByContact();
        const individualsArray: FrontIndividual[] = JSON.parse(allIndividuals);
        setIndividuals(individualsArray);
    };

    const openSearchDocs = async () => {
        await dotNetApi.appApiOpenLink('https://docs.hai-vr.dev/docs/xyvr/search');
    };

    const online = sortedIndividuals
        .filter(individual => {
            return individual.accounts.some(account => account.onlineStatus !== OnlineStatus.Offline && account.mainSession && account.mainSession.knowledge !== LiveSessionKnowledge.Known)
        });
    const filteredLiveSessions = liveSessionArray
        .filter(liveSession => liveSession.participants.some(p => p.isKnown &&
            sortedIndividuals.some(ind =>
                ind.accounts?.some(acc =>
                    acc.qualifiedAppName === liveSession.qualifiedAppName &&
                    acc.inAppIdentifier === p.knownAccount!.inAppIdentifier
                )
            )
        ));

    // Calculate online user counts per named app
    const onlineUserCountPerApp = useMemo(() => {
        const counts = new Map<NamedAppType, number>();

        individuals.forEach(individual => {
            individual.accounts?.forEach(account => {
                if (account.onlineStatus && account.onlineStatus !== OnlineStatus.Offline && account.onlineStatus !== OnlineStatus.Indeterminate) {
                    const currentCount = counts.get(account.namedApp) || 0;
                    counts.set(account.namedApp, currentCount + 1);
                }
            });
        });

        return counts;
    }, [individuals]);

    return (
        <>
            <div className="individuals-container">
                <div className="header-group">
                    <div className="header-section">
                        <div className="header-content">
                            {/*<h2 className="header-title">*/}
                            {/*    {showOnlyContacts && t('section.contacts') || t('section.contactsAndNotes')} {initialized && <>({totalFilteredCount})</> || <>(...)</>}*/}
                            {/*</h2>*/}

                            <div className="header-title header-search search-container">
                                <>
                                    <input
                                        ref={searchInputRef}
                                        type={debugMode.demoMode !== DemonstrationMode.Disabled ? 'password' : 'text'}
                                        placeholder={t('addressBook.search.placeholder')}
                                        value={searchField}
                                        onChange={(e) => setSearchField(e.target.value)}
                                        className="search-input"
                                    />
                                    {searchField && (
                                        <button
                                            onClick={() => setSearchField('')}
                                            className="icon-button search-clear-button"
                                            title={t('search.clear.title')}
                                        >
                                            <X size={16} />
                                        </button>
                                    )}
                                </>
                            </div>

                            <div className="header-buttons">
                                <button
                                    className="theme-toggle-btn"
                                    onClick={() => setCompactMode(!compactMode)}
                                    aria-pressed={compactMode}
                                    title={compactMode ? t('ui.switchToFullMode.title') : t('ui.switchToCompactMode.title')}
                                >
                                    {compactMode ? <Binoculars /> : <Glasses />}
                                </button>
                                <button
                                    className="theme-toggle-btn"
                                    onClick={() => setPortraits(!portraits)}
                                    aria-pressed={portraits}
                                    title={portraits ? t('ui.switchToHidePortraits.title') : t('ui.switchToShowPortraits.title')}
                                >
                                    {portraits ? <Image /> : <ImageOff />}
                                </button>
                                <button
                                    className="theme-toggle-btn"
                                    onClick={() => setShowNotes(!showNotes)}
                                    aria-pressed={showNotes}
                                    title={showNotes ? t('ui.hideNotes.title') : t('ui.showNotes.title')}
                                >
                                    {showNotes ? <NotebookText /> : <Notebook />}
                                </button>
                                <button
                                    className="theme-toggle-btn"
                                    onClick={() => setShowOnlyContacts(!showOnlyContacts)}
                                    aria-pressed={showOnlyContacts}
                                    title={showOnlyContacts ? t('ui.showContactsAndNotes.title') : t('ui.showOnlyContacts.title')}
                                >
                                    {showOnlyContacts ? <UserStar /> : <UserPen />}
                                </button>
                                <DarkModeToggleButton isDark={isDark} setIsDark={setIsDark} />
                            </div>
                        </div>
                    </div>
                    <div className="header-thin-right">
                        <h2 className="header-title">
                            <button className="header-nav" title={t('nav.configureConnections.title')} onClick={() => navigate('/data-collection')}><Settings /></button>
                        </h2>
                    </div>
                </div>

                {(() => {
                    const appsWithUsers = SupportedApps.filter(supportedApp => {
                        const userCount = onlineUserCountPerApp.get(supportedApp.namedApp) || 0;
                        return userCount > 0;
                    });

                    if (appsWithUsers.length <= 1) return null;

                    return (
                        <div className="search-filters">
                            {appsWithUsers.map(supportedApp => {
                                const namedApp = supportedApp.namedApp;
                                const userCount = onlineUserCountPerApp.get(namedApp) || 0;

                                return (
                                    <SearchFilter
                                        key={namedApp}
                                        namedApp={namedApp}
                                        userCount={userCount}
                                        onClick={() => { setSearchField(`on:${SupportedAppsByNamedApp[namedApp].searchTerm}`); focusSearchInput(); }}
                                    />
                                );
                            })}
                        </div>
                    );
                })()}

                {debouncedSearchField && (totalFilteredCount === 0 || showHelp) && (
                    <div className="no-results-message">
                        <div className="no-results-icon"><Search size={48}/></div>
                        {!showHelp && <>
                            <div className="no-results-text">
                                {t('addressBook.noResults.text', { searchTerm: _D2(debouncedSearchField, debugMode) })}
                            </div>
                        </>}
                        <div className="no-results-hint">
                            <p>{t('addressBook.noResults.help')}</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField('bio:'); focusSearchInput(); }}>bio:<i>creator</i></code> {t('addressBook.search.bio.example')}</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField('links:'); focusSearchInput(); }}>links:<i>misskey</i></code> {t('addressBook.search.links.example')}</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField('alias:'); focusSearchInput(); }}>alias:<i>aoi</i></code> {t('addressBook.search.alias.example')}</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField('session:'); focusSearchInput(); }}>session:<i>mmc</i></code> {t('addressBook.search.session.example')}</p>
                            <p>{t('addressBook.search.quotes.help')} <code className="inline-code-clickable" onClick={() => { setSearchField('session:"'); focusSearchInput(); }}>session:<i>"h p"</i></code></p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField('accounts:>1 '); focusSearchInput(); }}>accounts:&gt;1</code> {t('addressBook.search.accounts.example')}</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField('has:alt '); focusSearchInput(); }}>has:alt</code> {t('addressBook.search.hasAltAccount.example')}</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField('on: '); focusSearchInput(); }}>on:</code> {t('addressBook.search.on.example')}</p>
                            {SupportedApps
                                .map(app => {
                                    return (
                                        <p key={app.namedApp}>
                                            <code className="inline-code-clickable" onClick={() => { setSearchField(`app:${app.searchTerm} `); focusSearchInput(); }}>app:{app.searchTerm}</code> {t('addressBook.search.app.example', { app: app.displayName })} <code className="inline-code-clickable" onClick={() => { setSearchField(`on:${app.searchTerm} `); focusSearchInput(); }}>on:{app.searchTerm}</code> {t('addressBook.search.online.example')}
                                        </p>
                                    );
                                })
                            }
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField('app:resonite app:vrchat '); focusSearchInput(); }}>app:resonite app:vrchat</code> {t('addressBook.search.multipleApps.example')}</p>
                            <p><code className="inline-code-clickable" onClick={() => { setSearchField(':confusables ' + searchField); focusSearchInput(); }}>:confusables</code> {t('addressBook.search.confusables.example')}</p>

                            <p>{t('addressBook.search.docs.link')} <a title={t('addressBook.search.docs.title')} className="link-pointer" onClick={openSearchDocs} onAuxClick={(e) => e.button === 1 && openSearchDocs()} onMouseDown={(e) => e.preventDefault()}>{t('addressBook.search.docs.link')}</a>.</p>
                        </div>
                    </div>
                )}

                <div>
                    {debouncedSearchField && (
                        <div className="search-results-info">
                            {totalFilteredCount === 0
                                ? t('addressBook.results.noResults', { searchTerm: debouncedSearchField })
                                : <>{totalFilteredCount > 1
                                    ? t('addressBook.results.multipleResults', { count: totalFilteredCount })
                                    : t('addressBook.results.singleResult')} {' '}
                                    Type <code className="inline-code-clickable"onClick={() => { setSearchField(':help '); focusSearchInput(); }}>:help</code> {t('addressBook.results.typeHelp')} {' '}
                                    Type <code className="inline-code-clickable"onClick={() => { setSearchField(searchField + ' alias:'); focusSearchInput(); }}>alias:</code> {t('addressBook.results.typeAlias')} {' '}
                                    Type <code className="inline-code-clickable"onClick={() => { setSearchField(searchField + ' bio:'); focusSearchInput(); }}>bio:</code> {t('addressBook.results.typeBio')}</>
                            }
                        </div>
                    )}
                </div>

                {filteredLiveSessions.length > 0 && (
                    <div className="live-sessions-section">
                        <div className="live-sessions-grid">
                            {filteredLiveSessions
                                .sort((a, b) => {
                                    const aKnownCount = a.participants.filter(p => p.isKnown).length;
                                    const bKnownCount = b.participants.filter(p => p.isKnown).length;
                                    const dKnownCount = bKnownCount - aKnownCount; // Descending order (most participants first)
                                    if (dKnownCount !== 0) return dKnownCount;

                                    const aAttendance = a.currentAttendance || a.participants.length;
                                    const bAttendance = b.currentAttendance || b.participants.length;
                                    const dAttendance = bAttendance - aAttendance;
                                    if (dAttendance !== 0) return dAttendance;

                                    const aCapacity = a.sessionCapacity || a.virtualSpaceDefaultCapacity || 0;
                                    const bCapacity = b.sessionCapacity || b.virtualSpaceDefaultCapacity || 0;
                                    const dCapacity = bCapacity - aCapacity;
                                    if (dCapacity !== 0) return -dCapacity; // Prioritize low capacity by negating it

                                    return 0;
                                })
                                .map((liveSession) => (
                                    <LiveSession key={liveSession.guid} liveSession={liveSession} individuals={individuals} debugMode={debugMode} mini={false} resoniteShowSubSessions={resoniteShowSubSessions} setModalIndividual={setModalIndividual} portraits={portraits} />
                                ))}
                        </div>
                    </div>
                )}

                {/*{!debouncedSearchField && online.length > 0 && <div className={`individuals-grid ${compactMode ? 'compact-mode' : ''}`}>*/}
                {/*    {online*/}
                {/*        .map((individual, index) => (*/}
                {/*            <Individual*/}
                {/*                key={individual.guid || index}*/}
                {/*                individual={individual}*/}
                {/*                isVisible={true}*/}
                {/*                showBio={showBio}*/}
                {/*                showAlias={showAlias}*/}
                {/*                setMergeAccountGuidOrUnd={setMergeAccountGuidOrUnd}*/}
                {/*                isBeingMerged={mergeAccountGuidOrUnd === individual.guid}*/}
                {/*                displayNameOfOtherBeingMergedOrUnd={displayNameOfOtherBeingMergedOrUnd}*/}
                {/*                fusionAccounts={fusionAccounts}*/}
                {/*                unmergeAccounts={unmergeAccounts}*/}
                {/*                compactMode={true}*/}
                {/*                searchField={debouncedSearchField}*/}
                {/*                showNotes={false}*/}
                {/*                debugMode={debugMode}*/}
                {/*                setModalIndividual={setModalIndividual}*/}
                {/*            />*/}
                {/*    ))}*/}
                {/*</div>}*/}

                {/*{!debouncedSearchField && online.length > 0 && <div className={`individuals-grid ${compactMode ? 'compact-mode' : ''}`}>*/}
                {online.length > 0 && <div className={`live-session-accounts-grid ${compactMode ? 'compact-mode' : ''}`}>
                    {online
                        .map((individual) => (
                            individual.accounts
                                .filter(account => account.onlineStatus !== OnlineStatus.Offline && account.mainSession && account.mainSession.knowledge !== LiveSessionKnowledge.Known)
                                .map((account) => (
                                    <Account account={account} imposter={false} showAlias={false} showNotes={false}
                                             debugMode={debugMode} isSessionView={true}
                                             clickOpensIndividual={individual} setModalIndividual={setModalIndividual}
                                             showSession={false} illustrativeDisplay={true} showAccountIcon={true} portrait={portraits} />)
                                )
                            
                            // <Individual
                            //     key={individual.guid || index}
                            //     individual={individual}
                            //     isVisible={true}
                            //     showBio={showBio}
                            //     showAlias={showAlias}
                            //     setMergeAccountGuidOrUnd={setMergeAccountGuidOrUnd}
                            //     isBeingMerged={mergeAccountGuidOrUnd === individual.guid}
                            //     displayNameOfOtherBeingMergedOrUnd={displayNameOfOtherBeingMergedOrUnd}
                            //     fusionAccounts={fusionAccounts}
                            //     unmergeAccounts={unmergeAccounts}
                            //     compactMode={true}
                            //     searchField={debouncedSearchField}
                            //     showNotes={false}
                            //     debugMode={debugMode}
                            //     setModalIndividual={setModalIndividual}
                            // />
                    ))}
                </div>}

                {!isSimpleSearch && debouncedSearchField && <>
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
                                searchField={debouncedSearchField}
                                showNotes={showNotes}
                                debugMode={debugMode}
                                setModalIndividual={setModalIndividual}
                            />
                        ))}
                    </div>

                    {hasMoreItems && (
                        <div className="load-more-section">
                            {isLoading ? (
                                <div className="loading-indicator">
                                    <div className="loading-spinner"></div>
                                    <span>{t('address.loadingMore.label')}</span>
                                </div>
                            ) : (
                                <button
                                    onClick={loadMoreItems}
                                    title={t('address.loadMore.title')}
                                >
                                    {t('address.loadMore.label', { remaining: totalFilteredCount - displayedCount })}
                                </button>
                            )}
                        </div>
                    )}
                </>}
            </div>
            
            {modalIndividual && <IndividualDetailsModal
                isOpen={true}
                onClose={() => setModalIndividual(undefined)}
                individual={modalIndividual}
                debugMode={debugMode}
            />}
        </>
    )
}

export default AddressBookPage