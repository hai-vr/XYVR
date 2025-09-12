using System.Collections.Immutable;
using Newtonsoft.Json;

namespace XYVR.Core;

// - Every individual must have at least one account.
// - An individual may have one or more accounts across several apps.
// - An individual may be on some apps and not some others.
// - An individual may have multiple accounts on the same app.
// - Two different individuals must not have the same account.

public record ImmutableIndividual
{
    public required string guid { get; init; }
    public ImmutableArray<ImmutableAccount> accounts { get; init; } = ImmutableArray<ImmutableAccount>.Empty;
    public required string displayName { get; init; }
    
    public bool isAnyContact { get; init; }
    
    /// An Individual is exposed when at least one of the following is true:<br/>
    /// - That Individual is a Contact on any app, or<br/>
    /// - that Individual has any Note attached to any of its Accounts, or<br/>
    /// - that Individual has any Note attached to the Individual itself.<br/>
    /// When false, this should cause the Individual to disappear even in non-Contact views.<br/>
    /// <br/>
    /// The only reason we need to keep track of non-Exposed Individuals is so that we can
    /// use the API to fetch updates and find out whether any of that Individual's Account has a new Note on them:
    /// Notes are not specific to Contacts and can be attached to anyone.
    public bool isExposed { get; init; }
    
    // This field is up to the app users' judgement
    public string? customName { get; init; }
    public ImmutableNote note { get; init; } = new();

    public virtual bool Equals(ImmutableIndividual? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return guid == other.guid && accounts.SequenceEqual(other.accounts) && displayName == other.displayName && isAnyContact == other.isAnyContact && isExposed == other.isExposed && customName == other.customName && note.Equals(other.note);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = guid.GetHashCode();
            hashCode = (hashCode * 397) ^ accounts.Aggregate(0, (h, a) => h ^ a.GetHashCode());
            hashCode = (hashCode * 397) ^ displayName.GetHashCode();
            hashCode = (hashCode * 397) ^ isAnyContact.GetHashCode();
            hashCode = (hashCode * 397) ^ isExposed.GetHashCode();
            hashCode = (hashCode * 397) ^ (customName != null ? customName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ note.GetHashCode();
            return hashCode;
        }
    }
}

public record ImmutableAccount
{
    // These fields are not supposed to change.
    public required string guid { get; init; }
    public NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }
    public required string inAppIdentifier { get; init; }
    
    public required string inAppDisplayName { get; init; }

    [JsonConverter(typeof(SpecificsConverter))]
    public object? specifics { get; init; }
    
    // As the account can be retrieved by different connections, we need to keep track of which connection caused this account
    // to be retrieved: That's the caller account.
    // In addition, there is information specific to that caller account, such as notes, or whether it's a contact.
    public ImmutableArray<ImmutableCallerAccount> callers { get; init; } = ImmutableArray<ImmutableCallerAccount>.Empty;
    
    public ImmutableArray<string> allDisplayNames { get; init; } = ImmutableArray<string>.Empty;
    
    // Maintenance fields
    public bool isPendingUpdate;

    // This field is up to the app users' judgement
    public bool isTechnical;

    public bool IsAnyCallerContact()
    {
        return callers.Any(caller => caller.isContact);
    }

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

    public virtual bool Equals(ImmutableAccount? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return isPendingUpdate == other.isPendingUpdate &&
               isTechnical == other.isTechnical &&
               guid == other.guid &&
               namedApp == other.namedApp &&
               qualifiedAppName == other.qualifiedAppName &&
               inAppIdentifier == other.inAppIdentifier &&
               inAppDisplayName == other.inAppDisplayName &&
               Equals(specifics, other.specifics) &&
               callers.SequenceEqual(other.callers) &&
               allDisplayNames.SequenceEqual(other.allDisplayNames);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = isPendingUpdate.GetHashCode();
            hashCode = (hashCode * 397) ^ isTechnical.GetHashCode();
            hashCode = (hashCode * 397) ^ guid.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)namedApp;
            hashCode = (hashCode * 397) ^ qualifiedAppName.GetHashCode();
            hashCode = (hashCode * 397) ^ inAppIdentifier.GetHashCode();
            hashCode = (hashCode * 397) ^ inAppDisplayName.GetHashCode();
            hashCode = (hashCode * 397) ^ (specifics != null ? specifics.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ callers.Aggregate(0, (h, a) => h ^ a.GetHashCode());
            hashCode = (hashCode * 397) ^ allDisplayNames.Aggregate(0, (h, a) => h ^ a.GetHashCode());
            return hashCode;
        }
    }
}

public record ImmutableAccountIdentification
{
    public NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }

    public required string inAppIdentifier { get; init; }
    
    public override string ToString()
    {
        return $"{namedApp}:{qualifiedAppName}:{inAppIdentifier}";
    }
}

public record ImmutableCallerAccount
{
    public bool isAnonymous { get; init; }
    public string? inAppIdentifier { get; init; } // Can only be null if it's an anonymous caller.

    public required ImmutableNote note { get; init; } = new();
    public bool isContact { get; init; }
}

public record ImmutableVRChatSpecifics
{
    public ImmutableArray<string> urls { get; init; } = ImmutableArray<string>.Empty;
    public required string bio { get; init; }
    public required string pronouns { get; init; }

    public virtual bool Equals(ImmutableVRChatSpecifics? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return urls.SequenceEqual(other.urls) && bio == other.bio && pronouns == other.pronouns;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = urls.Aggregate(0, (h, a) => h ^ a.GetHashCode());
            hashCode = (hashCode * 397) ^ bio.GetHashCode();
            hashCode = (hashCode * 397) ^ pronouns.GetHashCode();
            return hashCode;
        }
    }
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