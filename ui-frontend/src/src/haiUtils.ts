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

