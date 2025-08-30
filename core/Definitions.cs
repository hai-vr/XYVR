namespace XYVR.Core;

// - Every individual must have at least one account.
// - An individual may have one or more accounts across several apps.
// - An individual may be on some apps and not some others.
// - An individual may have multiple accounts on the same app.
// - Individuals must have exactly one "main" account per app.
// - Two different individuals must not have the same account.

public class Individual
{
    public string guid;
    public List<Account> accounts = new();
    public string displayName;
    
    public bool isAnyContact;
}

public class Account
{
    public NamedApp namedApp;
    public string qualifiedAppName;
    
    public string inAppIdentifier;
    public string inAppDisplayName;

    public object? liveServerData;
    public object? previousLiveServerData;
    public object? preservedServerData;
    public Dictionary<string, string> userData = new();
    
    public bool isContact;
}

public enum NamedApp
{
    NotNamed,
    Resonite
}