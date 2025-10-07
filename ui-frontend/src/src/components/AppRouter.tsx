import {HashRouter as Router, Routes, Route, Navigate} from 'react-router-dom'
import {useState, useEffect} from 'react'
import AddressBookPage from '../pages/AddressBookPage.tsx'
import SettingsPage from '../pages/SettingsPage.tsx'
import './AppRouter.css'
import {Toaster} from "react-hot-toast";
import type {ReactAppPreferences} from "../types/APITypes.ts";
import {type DebugFlags, DemonstrationMode} from "../types/DebugFlags.ts";
import {DotNetApi} from "../DotNetApi.ts";
import {useTranslation} from "react-i18next";

// @ts-ignore
const AppRouter = ({ appVersion }: { appVersion: string }) => {
    const dotNetApi = new DotNetApi();
    const { i18n } = useTranslation();

    const [isDark, setIsDark] = useState(true)
    const [showOnlyContacts, setShowOnlyContacts] = useState(false)
    const [compactMode, setCompactMode] = useState(false)
    const [lang, setLang] = useState('en')
    const [showNotes, setShowNotes] = useState(true)
    const [preferences, setPreferences] = useState<ReactAppPreferences>({isDark: true, showOnlyContacts: false, compactMode: false, lang: 'en'})
    const [isPreferencesObtained, setIsPreferencesObtained] = useState(false)
    const [debugMode, setDebugMode] = useState<DebugFlags>({debugMode: false, demoMode: DemonstrationMode.Disabled})

    // Apply theme and persist
    useEffect(() => {
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light')
        try {
            localStorage.setItem('theme', isDark ? 'dark' : 'light')
        } catch {
            // Ignore persistence errors (e.g., storage disabled)
            void 0;
        }

        const updatedPreferences = {...preferences, isDark};
        setPreferences(updatedPreferences);

    }, [isDark])

    useEffect(() => {
        const updatedPreferences = {...preferences, showOnlyContacts};
        setPreferences(updatedPreferences);

    }, [showOnlyContacts])

    useEffect(() => {
        const updatedPreferences = {...preferences, compactMode};
        setPreferences(updatedPreferences);

    }, [compactMode])

    useEffect(() => {
        const changeLang = async () => {
            await i18n.changeLanguage(lang);
        };

        changeLang();

        const updatedPreferences = {...preferences, lang};
        setPreferences(updatedPreferences);

    }, [lang])

    useEffect(() => {
        const updatePreferences = async () => {
            if (isPreferencesObtained) {
                await dotNetApi.preferencesApiSetPreferences(JSON.stringify(preferences));
            }
        };

        updatePreferences();

    }, [preferences])

    useEffect(() => {
        const initializeApi = async () => {
            if (!isPreferencesObtained) {
                const prefs: ReactAppPreferences = JSON.parse(await dotNetApi.preferencesApiGetPreferences());
                setPreferences(prefs);
                setIsDark(prefs.isDark);
                setShowOnlyContacts(prefs.showOnlyContacts);
                setCompactMode(prefs.compactMode);
                setLang(prefs.lang)
                setIsPreferencesObtained(true);
            }
        };

        initializeApi();
    }, [])

    // Keyboard shortcut handler for CTRL-SHIFT-D
    useEffect(() => {
        const handleKeyDown = (event: any) => {
            if (event.ctrlKey && event.shiftKey && event.key === 'D') {
                event.preventDefault()
                setDebugMode(prevMode => {
                    let prevDemoMode = prevMode.demoMode;
                    return ({
                        ...prevMode,
                        demoMode: prevDemoMode === DemonstrationMode.Everything ? DemonstrationMode.Disabled : DemonstrationMode.Everything
                    });
                })
            }
            if (event.ctrlKey && event.shiftKey && event.key === 'E') {
                event.preventDefault()
                setDebugMode(prevMode => {
                    let prevDemoMode = prevMode.demoMode;
                    return ({
                        ...prevMode,
                        demoMode: prevDemoMode === DemonstrationMode.EverythingButSessionNames ? DemonstrationMode.Disabled : DemonstrationMode.EverythingButSessionNames
                    });
                })
            }
        }

        document.addEventListener('keydown', handleKeyDown)

        return () => {
            document.removeEventListener('keydown', handleKeyDown)
        }
    }, [])

    return (
        <Router>
            <div className="app-container">
                <main className="page-content">
                    <Routes>
                        <Route path="/" element={<Navigate to="/address-book" replace/>}/>
                        <Route path="/address-book" element={<AddressBookPage isDark={isDark}
                                                                              setIsDark={setIsDark}
                                                                              showOnlyContacts={showOnlyContacts}
                                                                              setShowOnlyContacts={setShowOnlyContacts}
                                                                              compactMode={compactMode}
                                                                              setCompactMode={setCompactMode}
                                                                              showNotes={showNotes}
                                                                              setShowNotes={setShowNotes}
                                                                              debugMode={debugMode}
                        />}/>
                        <Route path="/data-collection"
                               element={<SettingsPage isDark={isDark} setIsDark={setIsDark}
                                                      setLang={setLang}
                                                      debugMode={debugMode}/>}/>
                    </Routes>
                </main>
                <Toaster
                    position="bottom-right"
                    toastOptions={{
                        duration: 4000,
                        style: {
                            background: '#333',
                            color: '#fff',
                        },
                    }}
                />
            </div>
        </Router>
    )
}

export default AppRouter