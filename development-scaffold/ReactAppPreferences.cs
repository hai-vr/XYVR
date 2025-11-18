namespace XYVR.Scaffold;

public record ReactAppPreferences
{
    public bool isDark { get; init; } = true;
    public bool showOnlyContacts { get; init; } = false;
    public bool compactMode { get; init; } = false;
    public string lang { get; init; } = "en";
    
    public bool resoniteShowSubSessions { get; init; } = true;
    
    public double windowWidth { get; init; } = 600;
    public double windowHeight { get; init; } = 1000;
    public double windowLeft { get; init; } = 100;
    public double windowTop { get; init; } = 100;
}