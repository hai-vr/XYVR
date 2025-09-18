import i18n from "i18next";
import {initReactI18next} from "react-i18next";
import en from './locales/en.json'
import ja from './locales/ja.json'

i18n.use(initReactI18next).init({
    lng: "en",
    fallbackLng: "en",
    resources: {
        en: {translation: en},
        ja: {translation: ja}
    }
});

export default i18n;