using JetBrains.Annotations;

namespace XYVR.Scaffold;

[Serializable]
public record ReactAppPreferences
{
    [UsedImplicitly] public bool isDark { get; init; } = true;
    [UsedImplicitly] public bool showOnlyContacts { get; init; } = false;
    [UsedImplicitly] public bool compactMode { get; init; } = false;
    [UsedImplicitly] public bool portraits { get; init; } = true;
    [UsedImplicitly] public string lang { get; init; } = "en";
    
    [UsedImplicitly] public bool resoniteShowSubSessions { get; init; } = true;
    
    [UsedImplicitly] public double windowWidth { get; init; } = 600;
    [UsedImplicitly] public double windowHeight { get; init; } = 1000;
    [UsedImplicitly] public double windowLeft { get; init; } = 100;
    [UsedImplicitly] public double windowTop { get; init; } = 100;
}