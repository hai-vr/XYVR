export const DemonstrationMode = {
    Disabled: 0,
    Everything: 1,
    EverythingButSessionNames: 2
} as const;

export type DemonstrationModeType = typeof DemonstrationMode[keyof typeof DemonstrationMode];

export interface DebugFlags {
    debugMode: boolean;
    demoMode: DemonstrationModeType;
}

export const DISABLED_DEBUG_FLAGS: DebugFlags = {
    debugMode: false,
    demoMode: DemonstrationMode.Disabled
};
