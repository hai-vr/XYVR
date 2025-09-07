import React from 'react';
import { Moon, Sun } from "lucide-react";

const DarkModeToggleButton = ({ isDark, setIsDark }) => {
    return (
        <button
            className="theme-toggle-btn"
            onClick={() => setIsDark(!isDark)}
            title={`Switch to ${isDark ? 'Light' : 'Dark'} Mode`}
        >
            {isDark ? <Moon /> : <Sun />}
        </button>
    );
};

export default DarkModeToggleButton;