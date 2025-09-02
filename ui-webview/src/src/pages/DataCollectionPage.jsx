
import { useState, useEffect } from 'react'
import './DataCollectionPage.css'

function DataCollectionPage() {
    const [isDark, setIsDark] = useState(false)

    // Separate useEffect for theme changes
    useEffect(() => {
        document.documentElement.setAttribute('data-theme', isDark ? 'dark' : 'light');
    }, [isDark]);

    return (
        <div className="data-collection-container">
            <div className="header-section">
                <div className="header-content">
                    <h2 className="header-title">
                        Data Collection
                    </h2>

                    <div className="header-buttons">
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

            <div className="page-content">
                <div className="empty-state">
                    <div className="empty-state-icon">📊</div>
                    <h3 className="empty-state-title">Data Collection</h3>
                    <p className="empty-state-description">
                        This page is currently empty and ready for data collection functionality to be implemented.
                    </p>
                </div>
            </div>
        </div>
    )
}

export default DataCollectionPage