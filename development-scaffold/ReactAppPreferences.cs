namespace XYVR.Scaffold;

public record ReactAppPreferences
{
    public bool isDark { get; init; } = true;
    public bool showOnlyContacts { get; init; } = false;
    public bool compactMode { get; init; } = false;
    public string lang { get; init; } = "en";
}