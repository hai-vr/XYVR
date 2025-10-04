export interface AvailableLanguages {
    availableLanguages: LanguageInfo[];
    authors: Record<string, AuthorInfo>;
}

export interface LanguageInfo {
    code: string;
    file: string;
    displayName: string;
    englishName: string;
    technicalCode: string;
    authors: string[];
}

export interface AuthorInfo {
    gitHub: string;
    introduction: string;
}