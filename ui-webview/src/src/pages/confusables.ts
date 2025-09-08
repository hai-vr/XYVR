/**
 * Converts confusable Cyrillic characters to their Latin equivalents
 * Based on Unicode confusables and visual similarity
 * @param {string} text - Input text containing potentially confusable characters
 * @returns {string} Text with Cyrillic characters converted to Latin equivalents
 */
export function convertConfusablesToLatin(text: string): string {
    // Mapping of confusable Cyrillic characters to Latin equivalents
    const confusableMap: Record<string, string> = {
        // Lowercase Cyrillic to Latin
        'а': 'a', // U+0430 → U+0061
        'е': 'e', // U+0435 → U+0065
        'о': 'o', // U+043E → U+006F
        'р': 'p', // U+0440 → U+0070
        'с': 'c', // U+0441 → U+0063
        'у': 'y', // U+0443 → U+0079
        'х': 'x', // U+0445 → U+0078
        'і': 'i', // U+0456 → U+0069 (Ukrainian)
        'ј': 'j', // U+0458 → U+006A (Serbian)
        'ѕ': 's', // U+0455 → U+0073 (Macedonian)

        // Uppercase Cyrillic to Latin
        'А': 'A', // U+0410 → U+0041
        'В': 'B', // U+0412 → U+0042
        'Е': 'E', // U+0415 → U+0045
        'К': 'K', // U+041A → U+004B
        'М': 'M', // U+041C → U+004D
        'Н': 'H', // U+041D → U+0048
        'О': 'O', // U+041E → U+004F
        'Р': 'P', // U+0420 → U+0050
        'С': 'C', // U+0421 → U+0043
        'Т': 'T', // U+0422 → U+0054
        'У': 'Y', // U+0423 → U+0059
        'Х': 'X', // U+0425 → U+0058
        'І': 'I', // U+0406 → U+0049 (Ukrainian)
        'Ј': 'J', // U+0408 → U+004A (Serbian)
        'Ѕ': 'S', // U+0405 → U+0053 (Macedonian)

        // Additional visually similar characters
        'ο': 'o', // Greek omicron U+03BF → Latin o
        'Ο': 'O', // Greek Omicron U+039F → Latin O
        'Α': 'A', // Greek Alpha U+0391 → Latin A
        'Β': 'B', // Greek Beta U+0392 → Latin B
        'Ε': 'E', // Greek Epsilon U+0395 → Latin E
        'Ζ': 'Z', // Greek Zeta U+0396 → Latin Z
        'Η': 'H', // Greek Eta U+0397 → Latin H
        'Ι': 'I', // Greek Iota U+0399 → Latin I
        'Κ': 'K', // Greek Kappa U+039A → Latin K
        'Μ': 'M', // Greek Mu U+039C → Latin M
        'Ν': 'N', // Greek Nu U+039D → Latin N
        'Ρ': 'P', // Greek Rho U+03A1 → Latin P
        'Τ': 'T', // Greek Tau U+03A4 → Latin T
        'Υ': 'Y', // Greek Upsilon U+03A5 → Latin Y
        'Χ': 'X', // Greek Chi U+03A7 → Latin X
    };

    // Convert each character if it exists in the mapping
    return text.replace(/./g, char => confusableMap[char] || char);
}

export function detectConfusables(text: string) {
    const confusableChars = [];
    const converted = convertConfusablesToLatin(text);

    for (let i = 0; i < text.length; i++) {
        if (text[i] !== converted[i]) {
            confusableChars.push({
                original: text[i],
                converted: converted[i],
                position: i,
                codePoint: text[i].codePointAt(0)!.toString(16).toUpperCase()
            });
        }
    }

    return {
        hasConfusables: confusableChars.length > 0,
        confusables: confusableChars,
        originalText: text,
        convertedText: converted
    };
}
