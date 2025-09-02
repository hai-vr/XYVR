import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import AddressBookPage from '../pages/AddressBookPage.jsx'
import DataCollectionPage from '../pages/DataCollectionPage.jsx'
import Navigation from './Navigation.jsx'
import './AppRouter.css'

function AppRouter() {
    return (
        <Router>
            <div className="app-container">
                <Navigation />
                <main className="page-content">
                    <Routes>
                        <Route path="/" element={<Navigate to="/address-book" replace />} />
                        <Route path="/address-book" element={<AddressBookPage />} />
                        <Route path="/data-collection" element={<DataCollectionPage />} />
                    </Routes>
                </main>
            </div>
        </Router>
    )
}

export default AppRouter