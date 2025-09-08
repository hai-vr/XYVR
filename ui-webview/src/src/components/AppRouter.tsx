import {HashRouter as Router, Routes, Route, Navigate} from 'react-router-dom'
import {useState, useEffect} from 'react'
// @ts-ignore
import AddressBookPage from '../pages/AddressBookPage.jsx'
// @ts-ignore
import DataCollectionPage from '../pages/DataCollectionPage.jsx'
import './AppRouter.css'
import {Toaster} from "react-hot-toast";
import type {ReactAppPreferences} from "../types/APITypes.ts";

// @ts-ignore
const AppRouter = ({ appVersion }: { appVersion: string }) => {
    const [isDark, setIsDark] = useState(true)
    const [showOnlyContacts, setShowOnlyContacts] = useState(false)
    const [compactMode, setCompactMode] = useState(false)
    const [showNotes, setShowNotes] = useState(true)
    const [preferences, setPreferences] = useState<ReactAppPreferences>({isDark: true, showOnlyContacts: false})
    const [isPreferencesObtained, setIsPreferencesObtained] = useState(false)
    const [debugMode, setDebugMode] = useState(false)

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
        const updatePreferences = async () => {
            if (isPreferencesObtained) {
                await window.chrome.webview.hostObjects.preferencesApi.SetPreferences(JSON.stringify(preferences));
            }
        };

        updatePreferences();

    }, [preferences])

    useEffect(() => {
        const initializeApi = async () => {
            if (!isPreferencesObtained) {
                const prefs: ReactAppPreferences = JSON.parse(await window.chrome.webview.hostObjects.preferencesApi.GetPreferences());
                setPreferences(prefs);
                setIsDark(prefs.isDark);
                setShowOnlyContacts(prefs.showOnlyContacts);
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
                setDebugMode(prevMode => !prevMode)
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
                                                                              demoMode={debugMode}
                        />}/>
                        <Route path="/data-collection"
                               element={<DataCollectionPage isDark={isDark} setIsDark={setIsDark}
                                                            demoMode={debugMode}/>}/>
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