using System.Collections.Immutable;
using Newtonsoft.Json;

namespace XYVR.Core;

// - Every individual must have at least one account.
// - An individual may have one or more accounts across several apps.
// - An individual may be on some apps and not some others.
// - An individual may have multiple accounts on the same app.
// - Two different individuals must not have the same account.

public class Individual
{
    public string guid;
    public List<Account> accounts = new();
    public string displayName;
    
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
    
    // This field is up to the app users' judgement
    public string? customName;
    public ImmutableNote note = new();
}

public class Account
{
    // These fields are not supposed to change.
    public string guid;
    public NamedApp namedApp;
    public string qualifiedAppName;
    public string inAppIdentifier;
    
    public string inAppDisplayName;

    [JsonConverter(typeof(SpecificsConverter))]
    public object? specifics;
    
    // As the account can be retrieved by different connections, we need to keep track of which connection caused this account
    // to be retrieved: That's the caller account.
    // In addition, there is information specific to that caller account, such as notes, or whether it's a contact.
    public List<ImmutableCallerAccount> callers = new();
    
    public List<string> allDisplayNames = new();
    
    // Maintenance fields
    public bool isPendingUpdate;

    // This field is up to the app users' judgement
    public bool isTechnical;

    public bool IsAnyCallerContact() => callers.Any(caller => caller.isContact);
    public bool HasAnyCallerNote() => callers.Any(caller => caller.note.status == NoteState.Exists);

    public ImmutableAccountIdentification AsIdentification()
    {
        return new ImmutableAccountIdentification
        {
            inAppIdentifier = inAppIdentifier,
            namedApp = namedApp,
            qualifiedAppName = qualifiedAppName,
        };
    }
}

public record ImmutableAccountIdentification
{
    public NamedApp namedApp { get; init; }
    public string qualifiedAppName { get; init; }

    public string inAppIdentifier { get; init; }
    
    public override string ToString()
    {
        return $"{namedApp}:{qualifiedAppName}:{inAppIdentifier}";
    }
}

public record ImmutableCallerAccount
{
    public bool isAnonymous { get; init; }
    public string? inAppIdentifier { get; init; } // Can only be null if it's an anonymous caller.
    
    public ImmutableNote note { get; init; }
    public bool isContact { get; init; }
}

public class ImmutableVRChatSpecifics
{
    public ImmutableArray<string> urls { get; init; }
    public string bio { get; init; }
    public string pronouns { get; init; }
}

public record ImmutableNote
{
    public NoteState status { get; init; } = NoteState.NeverHad;
    public string? text { get; init; }
}

public enum NoteState
{
    NeverHad = 1, // FIXME: Made a mistake, this entire enum is shifted 1
    Exists = 2,
    WasRemoved = 3,
}

public enum NamedApp
{
    NotNamed = 0,
    Resonite = 1,
    VRChat = 2,
    Cluster = 3,
    ChilloutVR = 4,
}