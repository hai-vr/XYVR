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

    public Note note = new();
    
    public bool isAnyContact;
    
    /// An Individual is exposed when at least one of the following is true:<br/>
    /// - That Individual is a Contact on any app, or<br/>
    /// - that Individual has any Note attached to any of its Accounts, or<br/>
    /// - that Individual has any Note attached to the Individual itself.<br/>
    /// When false, this should cause the Individual to disappear even in non-Contact views.<br/>
    /// <br/>
    /// The only reason we need to keep track of non-Exposed Individuals is so that we can
    /// use the API to fetch updates and find out whether any of that Individual's Account has a new Note on them:
    /// Notes are not specific to Contacts and can be attached to anyone.
    public bool isExposed;
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

    public Note note = new();
    
    public bool isContact;
}

public class Note
{
    public NoteState status = NoteState.NeverHad;
    public string? text;
}

public enum NoteState
{
    NeverHad = 1,
    Exists = 2,
    WasRemoved = 3,
}

public enum NamedApp
{
    NotNamed = 0,
    Resonite = 1,
    VRChat = 2
}