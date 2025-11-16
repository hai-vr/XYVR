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

export const makePersonalLinkPresentable = (url: string) => {
    let presentedUrl = url.replace('https://', '');

    // Remove trailing slash from the displayed URL, but ONLY if the URL is a hostname.
    if (presentedUrl.includes('/') && presentedUrl.split('/').length === 2 && presentedUrl.endsWith('/')) {
        presentedUrl = presentedUrl.slice(0, -1);
    }

    if (presentedUrl.includes('?')) return [presentedUrl];
    if (presentedUrl.includes('#')) return [presentedUrl];

    const split = presentedUrl.split('/');

    // TLDs
    if (split.length === 1 && split[0].toLowerCase().endsWith('.booth.pm')) {
        let dotSplit = split[0].split('.');
        if (dotSplit.length === 3) {
            return [dotSplit[0], 'Booth.pm'];
        }
    }
    if (split.length === 1 && split[0].toLowerCase().endsWith('.gumroad.com')) {
        let dotSplit = split[0].split('.');
        if (dotSplit.length === 3) {
            return [dotSplit[0], 'Gumroad'];
        }
    }
    if (split.length === 1 && split[0].toLowerCase().endsWith('.bsky.social')) {
        let dotSplit = split[0].split('.');
        if (dotSplit.length === 3) {
            return [split[0], 'Bluesky']; // NOTE: We intentionally show the entire URL here.
        }
    }
    if (split.length === 1 && split[0].toLowerCase().endsWith('.carrd.co')) {
        let dotSplit = split[0].split('.');
        if (dotSplit.length === 3) {
            return [dotSplit[0], 'Carrd'];
        }
    }
    if (split.length === 1 && split[0].toLowerCase().endsWith('.fanbox.cc')) {
        let dotSplit = split[0].split('.');
        if (dotSplit.length === 3) {
            return [dotSplit[0], 'pixivFANBOX'];
        }
    }
    if (split.length === 1 && split[0].toLowerCase().endsWith('.bandcamp.com')) {
        let dotSplit = split[0].split('.');
        if (dotSplit.length === 3) {
            return [dotSplit[0], 'Bandcamp'];
        }
    }

    if (split.length === 1) {
        return [presentedUrl];
    }

    // Order matters (presentedUrl is mutated throughout)
    let lowercaseUrl = presentedUrl.toLowerCase();

    // Social Media
    if (lowercaseUrl.startsWith('twitter.com/') || lowercaseUrl.startsWith('www.twitter.com/') || lowercaseUrl.startsWith('mobile.twitter.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'Twitter'];
    }
    if (lowercaseUrl.startsWith('x.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'X'];
    }
    if (lowercaseUrl.startsWith('twitch.tv/') || lowercaseUrl.startsWith('www.twitch.tv/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'Twitch'];
    }
    if (lowercaseUrl.startsWith('bsky.app/profile/')) {
        if (split.length === 3) return [split[2], 'Bluesky'];
    }
    if (lowercaseUrl.startsWith('misskey.niri.la/')) {
        if (split.length === 2) return [split[1], 'Misskey.niri.la'];
    }
    if (lowercaseUrl.startsWith('misskey.io/')) {
        if (split.length === 2) return [split[1], 'Misskey.io'];
    }
    if (lowercaseUrl.startsWith('linktr.ee/')) {
        if (split.length === 2) return [split[1], 'Linktree'];
    }
    if (lowercaseUrl.startsWith('lit.link/')) {
        if (split.length === 2) return [split[1], 'lit.link'];
    }
    if (lowercaseUrl.startsWith('www.youtube.com/c/')) {
        if (split.length === 3) return [split[2], 'YouTube'];
    }
    if (lowercaseUrl.startsWith('www.youtube.com/@')) { // Note: YouTube links that don't start with @ are sort of legacy, don't use those.
        if (split.length === 2) return [split[1], 'YouTube'];
    }
    if (lowercaseUrl.startsWith('www.instagram.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'Instagram'];
    }
    if (lowercaseUrl.startsWith('www.tiktok.com/')) {
        if (split.length === 2) return [split[1], 'TikTok'];
    }
    if (lowercaseUrl.startsWith('artstation.com/') || lowercaseUrl.startsWith('www.artstation.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'Artstation'];
    }
    if (lowercaseUrl.startsWith('soundcloud.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'SoundCloud'];
    }
    if (lowercaseUrl.startsWith('www.mixcloud.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'Mixcloud'];
    }
    if (lowercaseUrl.startsWith('steamcommunity.com/id/')) {
        // Notice how this is different from the others
        if (split.length === 3 || split.length === 4 && split[3] === '') return [split[2], 'Steam'];
    }
    if (lowercaseUrl.startsWith('facebook.com/') || lowercaseUrl.startsWith('facebook.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'Facebook'];
    }

    // Development
    if (lowercaseUrl.startsWith('github.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'GitHub'];
    }
    if (lowercaseUrl.startsWith('gitlab.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'GitLab'];
    }

    // Social VR
    if (lowercaseUrl.startsWith('cluster.mu/u/')) {
        if (split.length === 3) return [split[2], 'cluster.mu'];
    }

    // Crowdfunding
    if (lowercaseUrl.startsWith('ko-fi.com/')) {
        if (split.length === 2) return [split[1], 'Ko-fi'];
    }
    if (lowercaseUrl.startsWith('patreon.com/') || lowercaseUrl.startsWith('www.patreon.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'Patreon'];
    }
    if (lowercaseUrl.startsWith('fantia.jp/')) {
        if (split.length === 2) return [split[1], 'Fantia'];
    }
    if (lowercaseUrl.startsWith('fansly.com/')) {
        if (split.length === 2 || split.length === 3 && split[2] === '') return [split[1], 'Fansly'];
    }

    // Other stores
    if (lowercaseUrl.startsWith('booth.pm/')) {
        if (split.length === 2) return [split[1], 'Booth.pm'];
    }
    if (lowercaseUrl.startsWith('skeb.jp/')) {
        if (split.length === 2) return [split[1], 'Skeb'];
    }
    // Note: I haven't seen a single Jinxxy link on my contact list that isn't also a product link, so I don't have working examples to test with

    return [presentedUrl];
};