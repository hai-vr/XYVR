import { HashRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { useState, useEffect } from 'react'
import AddressBookPage from '../pages/AddressBookPage.jsx'
import DataCollectionPage from '../pages/DataCollectionPage.jsx'
import Navigation from './Navigation.jsx'
import './AppRouter.css'

function AppRouter() {
    const [isDark, setIsDark] = useState(false)

    // Handle theme changes
    useEffect(() => {
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light')
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