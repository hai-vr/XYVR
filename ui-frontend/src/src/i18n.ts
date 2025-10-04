import i18n from "i18next";
import {initReactI18next} from "react-i18next";
import availableLanguagesData from './locales/available-lang.json'
import type {AvailableLanguages} from './types/AvailableLang';

export const availableLanguages: AvailableLanguages = availableLanguagesData;

const localeModules = import.meta.glob('./locales/*.js', { eager: true });

const isValidLocaleFile = (filename: string): boolean => {
    const validFilePattern = /^[a-zA-Z0-9-]+\.js$/;

    return validFilePattern.test(filename) &&
        !filename.includes('/') &&
        !filename.includes('\\') &&
        !filename.includes('..');
};

const resources = availableLanguages.availableLanguages.reduce((acc, lang) => {
    if (!isValidLocaleFile(lang.file)) {
        console.warn(`Invalid locale file name: ${lang.file} for language ${lang.code}`);
        return acc;
    }

    const fullPath = `./locales/${lang.file}`;

    if (localeModules[fullPath]) {
        acc[lang.code] = {
            translation: (localeModules[fullPath] as any).default
        };
    } else {
        console.warn(`Locale file not found: ${lang.file} for language ${lang.code}`);
    }

    return acc;
}, {} as Record<string, { translation: any }>);


i18n.use(initReactI18next).init({
    lng: "en",
    fallbackLng: "en",
    resources: resources
});

export default i18n;