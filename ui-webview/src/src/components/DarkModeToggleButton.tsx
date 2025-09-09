import { Moon, Sun } from "lucide-react";

interface DarkModeToggleButtonProps {
    isDark: boolean;
    setIsDark: (isDark: boolean) => void;
}

const DarkModeToggleButton = ({ isDark, setIsDark }: DarkModeToggleButtonProps) => {
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