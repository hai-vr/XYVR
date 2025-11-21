import React from 'react';
import './SearchFilter.css';
import {type NamedAppType} from "../types/CoreTypes.ts";
import {AppIcon} from "./AppIcon.tsx";
import {useTranslation} from "react-i18next";

interface SearchFilterProps {
    userCount: number,
    namedApp: NamedAppType,
    onClick: (event: React.MouseEvent<HTMLDivElement>) => void
}

export function SearchFilter({userCount, namedApp, onClick}: SearchFilterProps) {
    const { t } = useTranslation();

    return (
        <div className="search-filter" onClick={onClick}>
            <AppIcon mini={true} namedApp={namedApp}/> <span>{t('searchFilter.onlineCount', { count: userCount })}</span>
        </div>
    );
}
