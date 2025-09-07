// Text normalization utilities
export const removeDiacritics = (str) => {
    return str.normalize('NFD')
        .replace(/[\u0300-\u036f]/g, '') // Replaces diacritics
        .replace(/\u2024/g, '.') // Replaces the crappy "one dot leader" character with a proper period
        .replace(/[\uFF00-\uFFEF]/g, (char) => {
            // Convert full-width characters to half-width
            const code = char.charCodeAt(0);
            if (code >= 0xFF01 && code <= 0xFF5E) {
                // Full-width ASCII characters
                return String.fromCharCode(code - 0xFEE0);
            }
            return char;
        });
};

// Japanese text conversion utilities
const romajiToHiragana = {
    'a': 'あ', 'i': 'い', 'u': 'う', 'e': 'え', 'o': 'お',
    'ka': 'か', 'ki': 'き', 'ku': 'く', 'ke': 'け', 'ko': 'こ',
    'ga': 'が', 'gi': 'ぎ', 'gu': 'ぐ', 'ge': 'げ', 'go': 'ご',
    'sa': 'さ', 'shi': 'し', 'su': 'す', 'se': 'せ', 'so': 'そ',
    'za': 'ざ', 'ji': 'じ', 'zu': 'ず', 'ze': 'ぜ', 'zo': 'ぞ',
    'ta': 'た', 'chi': 'ち', 'tsu': 'つ', 'te': 'て', 'to': 'と',
    'da': 'だ', 'di': 'ぢ', 'du': 'づ', 'de': 'で', 'do': 'ど',
    'na': 'な', 'ni': 'に', 'nu': 'ぬ', 'ne': 'ね', 'no': 'の',
    'ha': 'は', 'hi': 'ひ', 'fu': 'ふ', 'he': 'へ', 'ho': 'ほ',
    'ba': 'ば', 'bi': 'び', 'bu': 'ぶ', 'be': 'べ', 'bo': 'ぼ',
    'pa': 'ぱ', 'pi': 'ぴ', 'pu': 'ぷ', 'pe': 'ぺ', 'po': 'ぽ',
    'ma': 'ま', 'mi': 'み', 'mu': 'む', 'me': 'め', 'mo': 'も',
    'ya': 'や', 'yu': 'ゆ', 'yo': 'よ',
    'ra': 'ら', 'ri': 'り', 'ru': 'る', 're': 'れ', 'ro': 'ろ',
    'wa': 'わ', 'wi': 'ゐ', 'we': 'ゑ', 'wo': 'を', 'n': 'ん',
    'kya': 'きゃ', 'kyu': 'きゅ', 'kyo': 'きょ',
    'gya': 'ぎゃ', 'gyu': 'ぎゅ', 'gyo': 'ぎょ',
    'sha': 'しゃ', 'shu': 'しゅ', 'sho': 'しょ',
    'ja': 'じゃ', 'ju': 'じゅ', 'jo': 'じょ',
    'cha': 'ちゃ', 'chu': 'ちゅ', 'cho': 'ちょ',
    'nya': 'にゃ', 'nyu': 'にゅ', 'nyo': 'にょ',
    'hya': 'ひゃ', 'hyu': 'ひゅ', 'hyo': 'ひょ',
    'bya': 'びゃ', 'byu': 'びゅ', 'byo': 'びょ',
    'pya': 'ぴゃ', 'pyu': 'ぴゅ', 'pyo': 'ぴょ',
    'mya': 'みゃ', 'myu': 'みゅ', 'myo': 'みょ',
    'rya': 'りゃ', 'ryu': 'りゅ', 'ryo': 'りょ'
};

const hiraganaToKatakana = (hiragana) => {
    return hiragana.replace(/[\u3041-\u3096]/g, (char) => {
        return String.fromCharCode(char.charCodeAt(0) + 0x60);
    });
};

const convertRomajiToKana = (romaji) => {
    let result = romaji.toLowerCase();
    const sortedKeys = Object.keys(romajiToHiragana).sort((a, b) => b.length - a.length);

    for (const key of sortedKeys) {
        result = result.replace(new RegExp(key, 'g'), romajiToHiragana[key]);
    }

    return result;
};

export const generateKanaVariants = (term) => {
    const variants = [term];

    // Try to convert romaji to hiragana
    const hiragana = convertRomajiToKana(term);
    if (hiragana !== term) {
        variants.push(hiragana);

        // Convert hiragana to katakana
        const katakana = hiraganaToKatakana(hiragana);
        variants.push(katakana);
    }

    return variants;
};

// Search term parsing utilities
export const parseSearchTerms = (searchTerm) => {
    const terms = searchTerm.toLowerCase().trim().split(' ').filter(term => term.trim() !== '');
    const specialTerms = [];
    const regularTerms = [];

    terms.forEach(term => {
        if (term.startsWith('app:')
            || term.startsWith('accounts:')
            || term.startsWith('links:')
            || term.startsWith('bio:')
            || term.startsWith('alias:')
            || term.startsWith('on:')
            || term === 'has:alt'
            || term === 'has:bot'
            || term === ':help') {
            specialTerms.push(term);
        } else {
            regularTerms.push(term);
        }
    });

    return { specialTerms, regularTerms };
};

// Special search term matching
export const anyAccountMatchesSpecialTerms = (accounts, specialTerms, inAccountMode) => {
    return specialTerms.every(term => {
        if (term.startsWith('links:')) {
            const searchString = term.substring(6); // Remove 'links:' prefix
            if (!searchString) return true; // Empty search string matches all

            return accounts?.some(account =>
                account.specifics?.urls?.some(url =>
                    url.toLowerCase().includes(searchString)
                )
            ) || false;
        }

        if (term.startsWith('bio:')) {
            const searchString = term.substring(4); // Remove 'bio:' prefix
            if (!searchString) return true; // Empty search string matches all

            return accounts?.some(account =>
                account.specifics?.bio?.toLowerCase().includes(searchString)
            ) || false;
        }

        if (term.startsWith('alias:')) {
            const searchString = term.substring(6); // Remove 'alias:' prefix
            if (!searchString) return true; // Empty search string matches all

            const kanaVariants = generateKanaVariants(searchString);

            return accounts?.some(account => {
                if (!account.allDisplayNames || !Array.isArray(account.allDisplayNames)) {
                    return false;
                }

                return account.allDisplayNames.some(displayName => {
                    return kanaVariants.some(variant => {
                        const variantNormalized = removeDiacritics(variant);
                        return removeDiacritics(displayName.toLowerCase()).includes(variantNormalized);
                    });
                });
            }) || false;
        }

        switch (term) {
            case 'app:resonite':
                return accounts?.some(account => account.namedApp === "Resonite") || false;

            case 'app:vrchat':
                return accounts?.some(account => account.namedApp === "VRChat") || false;

            case 'app:cluster':
                return accounts?.some(account => account.namedApp === "Cluster") || false;

            case 'app:chilloutvr':
                return accounts?.some(account => account.namedApp === "ChilloutVR") || false;

            case 'on:':
                return accounts?.some(account => account.onlineStatus && account.onlineStatus !== 'Offline') || false;

            case 'on:resonite':
                return accounts?.some(account => account.namedApp === "Resonite" && account.onlineStatus && account.onlineStatus !== 'Offline') || false;

            case 'on:vrchat':
                return accounts?.some(account => account.namedApp === "VRChat" && account.onlineStatus && account.onlineStatus !== 'Offline') || false;

            case 'on:cluster':
                return accounts?.some(account => account.namedApp === "Cluster" && account.onlineStatus && account.onlineStatus !== 'Offline') || false;

            case 'on:chilloutvr':
                return accounts?.some(account => account.namedApp === "ChilloutVR" && account.onlineStatus && account.onlineStatus !== 'Offline') || false;

            case 'has:bot':
                return accounts?.some(account => account.isTechnical) || false;

            case 'has:alt': {
                if (!accounts) return false;

                // Group accounts by namedApp, excluding technical accounts
                const accountGroups = {};
                accounts.forEach(account => {
                    if (account.isTechnical === false || account.isTechnical === undefined) {
                        if (!accountGroups[account.namedApp]) {
                            accountGroups[account.namedApp] = 0;
                        }
                        accountGroups[account.namedApp]++;
                    }
                });

                // Check if any namedApp has more than one account
                return Object.values(accountGroups).some(count => count > 1);
            }

            default:
                if (inAccountMode) return true;

                if (term.startsWith('accounts:>')) {
                    const minCount = parseInt(term.substring(10));
                    if (isNaN(minCount)) return false;

                    const accountCount = accounts?.length || 0;
                    return accountCount > minCount;
                }
                return false;
        }
    });
};

export const accountMatchesFromRegularTerms = (account, regularTerms) => {
    const accountNote = account.note?.text || '';
    const accountDisplayName = account.inAppDisplayName || '';
    const accountIdentifier = account.inAppIdentifier || '';

    // Check account-level matches
    const accountMatch = regularTerms.every(term => {
        const kanaVariants = generateKanaVariants(term);

        return kanaVariants.some(variant => {
            const variantNormalized = removeDiacritics(variant);
            const noteMatch = removeDiacritics(accountNote.toLowerCase()).includes(variantNormalized);
            const displayNameMatch = removeDiacritics(accountDisplayName.toLowerCase()).includes(variantNormalized);

            // Only search in inAppIdentifier if namedApp equals 3 (Cluster)
            const identifierMatch = account.namedApp === "Cluster" &&
                removeDiacritics(accountIdentifier.toLowerCase()).includes(variantNormalized);

            return noteMatch || displayNameMatch || identifierMatch;
        });
    });

    if (accountMatch) return true;

    // Check caller notes
    const callerNotesMatch = account.callers?.some(caller => {
        const callerNote = caller.note?.text || '';

        return regularTerms.every(term => {
            const kanaVariants = generateKanaVariants(term);

            return kanaVariants.some(variant => {
                const variantNormalized = removeDiacritics(variant);
                return removeDiacritics(callerNote.toLowerCase()).includes(variantNormalized);
            });
        });
    }) || false;

    return callerNotesMatch;
};

// Main filtering function
export const isIndividualVisible = (individual, searchTerm, showOnlyContacts = false, mergeAccountGuidOrUnd = undefined) => {
    if (mergeAccountGuidOrUnd !== undefined && individual.guid === mergeAccountGuidOrUnd) return true;

    // First apply the contact filter
    if (showOnlyContacts && !individual.isAnyContact) {
        return false;
    }

    if (!searchTerm) return true;

    const { specialTerms, regularTerms } = parseSearchTerms(searchTerm);

    // Check special terms first
    if (specialTerms.length > 0 && !anyAccountMatchesSpecialTerms(individual.accounts, specialTerms, false)) {
        return false;
    }

    // If there are no regular terms, and special terms matched, return true
    if (regularTerms.length === 0) {
        return true;
    }
    
    if (regularTerms.some(searchTerm => individual.accounts?.some(account => account.inAppIdentifier === searchTerm))) {
        return true;
    }

    // Check regular search terms (existing logic)
    const displayName = individual.displayName || '';
    const individualNote = individual.note?.text || '';

    const individualMatch = regularTerms.every(term => {
        const kanaVariants = generateKanaVariants(term);

        return kanaVariants.some(variant => {
            const variantNormalized = removeDiacritics(variant);
            return removeDiacritics(displayName.toLowerCase()).includes(variantNormalized) ||
                removeDiacritics(individualNote.toLowerCase()).includes(variantNormalized);
        });
    });

    if (individualMatch) return true;

    const accountNotesMatch = individual.accounts?.some(account => {
        return accountMatchesFromRegularTerms(account, regularTerms);
    }) || false;

    return accountNotesMatch;
};

// Sorting helper functions
export const hasDisplayNameMatch = (individual, searchTerm) => {
    if (!searchTerm) return false;

    const { regularTerms } = parseSearchTerms(searchTerm);
    if (regularTerms.length === 0) return false;

    const displayName = individual.displayName || '';

    return regularTerms.every(term => {
        const kanaVariants = generateKanaVariants(term);

        return kanaVariants.some(variant => {
            const variantNormalized = removeDiacritics(variant);
            return removeDiacritics(displayName.toLowerCase()).includes(variantNormalized);
        });
    });
};

export const hasIdentifierMatch = (individual, searchTerm) => {
    if (!searchTerm) return false;

    const { regularTerms } = parseSearchTerms(searchTerm);
    if (regularTerms.length === 0) return false;

    return individual.accounts?.some(account => {
        // Only check identifier for namedApp === "Cluster"
        if (account.namedApp !== "Cluster") return false;

        const accountIdentifier = account.inAppIdentifier || '';

        return regularTerms.every(term => {
            const kanaVariants = generateKanaVariants(term);

            return kanaVariants.some(variant => {
                const variantNormalized = removeDiacritics(variant);
                return removeDiacritics(accountIdentifier.toLowerCase()).includes(variantNormalized);
            });
        });
    }) || false;
};

// Utility function to check if bio should be shown
export const shouldShowBio = (searchTerm) => {
    if (!searchTerm) return false;
    const { specialTerms } = parseSearchTerms(searchTerm);
    return specialTerms.some(term => term.startsWith('bio:'));
};

export const shouldShowHelp = (searchTerm) => {
    if (!searchTerm) return false;
    const { specialTerms } = parseSearchTerms(searchTerm);
    return specialTerms.some(term => term === ':help');
};

export const shouldShowAlias = (searchTerm) => {
    if (!searchTerm) return false;
    const { specialTerms } = parseSearchTerms(searchTerm);
    return specialTerms.some(term => term.startsWith('alias:'));
};

export const getOnlineStatusPriority = (onlineStatus) => {
    if (!onlineStatus || onlineStatus === "Offline") return 5;
    if (onlineStatus === "Indeterminate") return 6;
    if (onlineStatus === "ResoniteBusy" || onlineStatus === "VRChatDND") return 4;
    if (onlineStatus === "ResoniteAway" || onlineStatus === "VRChatAskMe") return 3;
    if (onlineStatus === "Online") return 2;
    if (onlineStatus === "ResoniteSociable" || onlineStatus === "VRChatJoinMe") return 1;
    return 5; // Default to same as offline for unknown statuses
};
