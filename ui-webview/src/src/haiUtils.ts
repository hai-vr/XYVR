// Redact fully, even spaces. Best used for usernames.
export const _D = (inputStr: string, isDemoMode: boolean) => {
    return isDemoMode ? (inputStr && '█'.repeat(inputStr.length) || '') : inputStr;
}

/// Redact, but keep spaces or another given character.
export const _D2 = (inputStr: string, isDemoMode: boolean, character = ' ') => {
    if (isDemoMode) {
        if (!inputStr) return inputStr;
        return inputStr.split(character).map(it => _D(it, isDemoMode)).join(character);
    } else {
        return inputStr;
    }
}

