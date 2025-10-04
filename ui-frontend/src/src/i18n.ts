import i18n from "i18next";
import {initReactI18next} from "react-i18next";
import en from './locales/en.json'
import ja from './locales/ja.json'
import availableLanguagesData from './locales/available-lang.json'
import type {AvailableLanguages} from './types/AvailableLang';

export const availableLanguages: AvailableLanguages = availableLanguagesData;

i18n.use(initReactI18next).init({
    lng: "en",
    fallbackLng: "en",
    resources: {
        en: {translation: en},
        ja: {translation: ja}
    }
});

export default i18n;