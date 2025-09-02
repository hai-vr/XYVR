import { HashRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import AddressBookPage from '../pages/AddressBookPage.jsx'
import DataCollectionPage from '../pages/DataCollectionPage.jsx'
import Navigation from './Navigation.jsx'
import './AppRouter.css'

function AppRouter() {
    const [isDark, setIsDark] = useState(() => {
        // Initialize from localStorage or prefers-color-scheme
        try {
            const stored = localStorage.getItem('theme');
            if (stored === 'dark') return true;
            if (stored === 'light') return false;
        } catch {
            // localStorage may be unavailable; fall back to media query
            void 0;
        }
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    })

    // Apply theme and persist
    useEffect(() => {
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light')
        try {
            localStorage.setItem('theme', isDark ? 'dark' : 'light')
        } catch {
            // Ignore persistence errors (e.g., storage disabled)
            void 0;
        }
    }, [isDark])

    return (
        <Router>
            <div className="app-container">
                <Navigation isDark={isDark} setIsDark={setIsDark} />
                <main className="page-content">
                    <Routes>
                        <Route path="/" element={<Navigate to="/address-book" replace />} />
                        <Route path="/address-book" element={<AddressBookPage isDark={isDark} setIsDark={setIsDark} />} />
                        <Route path="/data-collection" element={<DataCollectionPage isDark={isDark} setIsDark={setIsDark} />} />
                    </Routes>
                </main>
            </div>
        </Router>
    )
}

export default AppRouter