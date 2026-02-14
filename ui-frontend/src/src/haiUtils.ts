// Redact fully, even spaces. Best used for usernames.
import {type DebugFlags, DemonstrationMode, type DemonstrationModeType} from "./types/DebugFlags.ts";

export const _D = (inputStr: string, debugFlags: DebugFlags) => {
    return debugFlags.demoMode !== DemonstrationMode.Disabled ? (inputStr && '█'.repeat(inputStr.length) || '') : inputStr;
}

/// Redact, but keep spaces or another given character.
export const _D2 = (inputStr: string, debugFlags: DebugFlags, character = ' ', showWhenDemonstrationModeIsEqualTo?: DemonstrationModeType) => {
    if (debugFlags.demoMode !== DemonstrationMode.Disabled && (!showWhenDemonstrationModeIsEqualTo || debugFlags.demoMode !== showWhenDemonstrationModeIsEqualTo)) {
        if (!inputStr) return inputStr;
        return inputStr.split(character).map(it => _D(it, debugFlags)).join(character);
    } else {
        return inputStr;
    }
}

type TLDHandler = {
    displayName: string;
    domain: string;
    showFullUrl?: boolean;
};

type DomainHandler = {
    prefixes: string[];
    displayName: string;
    // By default, it's allowed. We specified this value in the entries below explicitly for the cases where a slash was spotted in the wild.
    // This may be removed entirely.
    allowTrailingEmpty?: boolean;
    // Calculated based on the number of slashes in the first prefix.
    extractIndex?: number;
    minPathParts?: number;
    exclude?: string[];
};

const USER_IN_SUBDOMAIN_HANDLERS: TLDHandler[] = [
    { displayName: 'Booth.pm', domain: 'booth.pm' },
    { displayName: 'Gumroad', domain: 'gumroad.com' },
    { displayName: 'Bluesky', domain: 'bsky.social', showFullUrl: true },
    { displayName: 'Carrd', domain: 'carrd.co' },
    { displayName: 'pixivFANBOX', domain: 'fanbox.cc' },
    { displayName: 'Bandcamp', domain: 'bandcamp.com' },
];

const USER_IN_PATH_HANDLERS: DomainHandler[] = [
    { displayName: 'Twitter', prefixes: ['twitter.com/', 'www.twitter.com/', 'mobile.twitter.com/'], allowTrailingEmpty: true },
    { displayName: 'X', prefixes: ['x.com/'], allowTrailingEmpty: true },
    { displayName: 'Twitch', prefixes: ['twitch.tv/', 'www.twitch.tv/'], allowTrailingEmpty: true },
    { displayName: 'Bluesky', prefixes: ['bsky.app/profile/'] },
    { displayName: 'Misskey.niri.la', prefixes: ['misskey.niri.la/'] },
    { displayName: 'Misskey.io', prefixes: ['misskey.io/'] },
    { displayName: 'Facebook', prefixes: ['facebook.com/'], allowTrailingEmpty: true },
    { displayName: 'Linktree', prefixes: ['linktr.ee/'] },
    { displayName: 'lit.link', prefixes: ['lit.link/'] },
    { displayName: 'YouTube', prefixes: ['www.youtube.com/c/'] }, // Because the number of slashes is different in the prefix, we must not merge those.
    { displayName: 'YouTube', prefixes: ['www.youtube.com/@'] }, // Because the number of slashes is different in the prefix, we must not merge those.
    { displayName: 'Instagram', prefixes: ['www.instagram.com/'], allowTrailingEmpty: true },
    { displayName: 'TikTok', prefixes: ['www.tiktok.com/'] },
    { displayName: 'Artstation', prefixes: ['artstation.com/', 'www.artstation.com/'], allowTrailingEmpty: true },
    { displayName: 'SoundCloud', prefixes: ['soundcloud.com/'], allowTrailingEmpty: true },
    { displayName: 'Mixcloud', prefixes: ['www.mixcloud.com/'], allowTrailingEmpty: true },
    { displayName: 'Steam', prefixes: ['steamcommunity.com/id/'], allowTrailingEmpty: true },
    { displayName: 'GitHub', prefixes: ['github.com/'], allowTrailingEmpty: true },
    { displayName: 'GitLab', prefixes: ['gitlab.com/'], allowTrailingEmpty: true },
    { displayName: 'cluster.mu', prefixes: ['cluster.mu/u/'] },
    { displayName: 'Ko-fi', prefixes: ['ko-fi.com/'] },
    { displayName: 'Patreon', prefixes: ['patreon.com/', 'www.patreon.com/'], allowTrailingEmpty: true },
    { displayName: 'Fantia', prefixes: ['fantia.jp/'] },
    { displayName: 'Fansly', prefixes: ['fansly.com/'], allowTrailingEmpty: true },
    { displayName: 'Booth.pm', prefixes: ['booth.pm/'], exclude: ['wish_list_names'] },
    { displayName: 'Skeb', prefixes: ['skeb.jp/'] },
    { displayName: 'Jinxxy', prefixes: ['jinxxy.com/'] },
];

for (const value of USER_IN_PATH_HANDLERS) {
    // We assume that the number of slashes is the same for all prefixes in the array
    let numberOfSlashes = value.prefixes[0].split('/').length - 1;
    value.extractIndex = numberOfSlashes;
    value.minPathParts = numberOfSlashes + 1;
}

const matchesSplitCriteria = (splitLength: number, handler: DomainHandler): boolean => {
    const min = handler.minPathParts ?? splitLength;
    const max = splitLength;

    let allowTrailingEmpty = handler.allowTrailingEmpty === undefined ? true : handler.allowTrailingEmpty;
    if (allowTrailingEmpty && splitLength === max + 1 && !splitLength.toString().endsWith('')) {
        return true;
    }
    
    return splitLength === min || (allowTrailingEmpty && splitLength === min + 1);
};

export const makePersonalLinkPresentable = (url: string) => {
    let presentedUrl = url.replace('https://', '');

    // Remove trailing slash from the displayed URL, but ONLY if the URL is a hostname.
    if (presentedUrl.includes('/') && presentedUrl.split('/').length === 2 && presentedUrl.endsWith('/')) {
        presentedUrl = presentedUrl.slice(0, -1);
    }

    if (presentedUrl.includes('?') || presentedUrl.includes('#')) {
        return [presentedUrl];
    }

    const slashSplit = presentedUrl.split('/');
    const lowercaseUrl = presentedUrl.toLowerCase();
    
    if (slashSplit.length === 1) {
        const dotSplit = slashSplit[0].split('.');
        if (dotSplit.length === 3) {
            // The hostname of a link can end with a dot (e.g. "booth.pm."); we are deliberately ignoring links that use those
            // because practically no one uses that other than in internal enterprise systems.
            // See: https://en.wikipedia.org/wiki/Fully_qualified_domain_name
            if (dotSplit[2] !== '') {
                const domain = slashSplit[0].toLowerCase();
                for (const handler of USER_IN_SUBDOMAIN_HANDLERS) {
                    // We need to do the dot concatenation because things like "notbooth.pm" should not be mapped to "booth.pm"
                    // Example: If "x.com" used subdomains (it doesn't), then "netflix.com" would also cause this issue.
                    if (domain.endsWith('.' + handler.domain)) {
                        const identifier = handler.showFullUrl ? slashSplit[0] : dotSplit[0];
                        return [identifier, handler.displayName];
                    }
                }
            }
        }
        return [presentedUrl];
    }

    for (const handler of USER_IN_PATH_HANDLERS) {
        for (const prefix of handler.prefixes) {
            if (lowercaseUrl.startsWith(prefix)) {
                if (matchesSplitCriteria(slashSplit.length, handler)) {
                    const extracted = slashSplit[handler.extractIndex!];
                    if (handler.exclude?.includes(extracted.toLowerCase())) {
                        return [presentedUrl];
                    }
                    return [extracted, handler.displayName];
                }
            }
        }
    }

    return [presentedUrl];
};