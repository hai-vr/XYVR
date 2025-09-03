namespace XYVR.Scaffold;

public class ReactAppPreferences
{
    public bool isDark = true;
    public bool showOnlyContacts = false;
    
    protected bool Equals(ReactAppPreferences other)
    {
        return isDark == other.isDark && showOnlyContacts == other.showOnlyContacts;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ReactAppPreferences)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (isDark.GetHashCode() * 397) ^ showOnlyContacts.GetHashCode();
        }
    }
}