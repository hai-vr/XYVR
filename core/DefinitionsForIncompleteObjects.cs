using System.Text.Json.Serialization;

namespace XYVR.Core;

/// A non-indexed account is built from data coming from external services. Thus, it doesn't have a Guid because
/// it represents objects that are not indexed. It also doesn't have a list of past display names.
public class NonIndexedAccount
{
    public NamedApp namedApp;
    public string qualifiedAppName;
    public string inAppIdentifier;
    public string inAppDisplayName;
    [JsonConverter(typeof(SpecificsConverter))]
    public object? specifics;
    public List<CallerAccount> callers = new();

    public AccountIdentification AsIdentification()
    {
        return new AccountIdentification
        {
            inAppIdentifier = inAppIdentifier,
            namedApp = namedApp,
            qualifiedAppName = qualifiedAppName,
        };
    }

    public static Account MakeIndexed(NonIndexedAccount nonIndexedAccount)
    {
        return new Account
        {
            guid = XYVRGuids.ForAccount(),
            namedApp = nonIndexedAccount.namedApp,
            qualifiedAppName = nonIndexedAccount.qualifiedAppName,
            inAppIdentifier = nonIndexedAccount.inAppIdentifier,
            inAppDisplayName = nonIndexedAccount.inAppDisplayName,
            specifics = nonIndexedAccount.specifics,
            callers = nonIndexedAccount.callers.Select(caller => caller.ShallowCopy()).ToList(),
        };
    }
}

/// A non-indexed account is built from data coming from external services. However, it doesn't have notes or specific information
/// in it. When we receive information when getting contacts from an external service, we can index it and have it
/// show up in the UI as early as possible without actually needing to make calls and retrieve additional information about them.<br/>
/// The consequence of this is that when we convert this incomplete account to an indexed account, it is marked in the UI as pending,
/// so that the user knows the information about that account is incomplete.
public class IncompleteAccount
{
    public NamedApp namedApp;
    public string qualifiedAppName;
    
    public string inAppIdentifier;
    public string inAppDisplayName;
    
    public List<IncompleteCallerAccount> callers = new();
    
    public AccountIdentification AsIdentification()
    {
        return new AccountIdentification
        {
            inAppIdentifier = inAppIdentifier,
            namedApp = namedApp,
            qualifiedAppName = qualifiedAppName,
        };
    }

    public static Account MakeIndexed(IncompleteAccount incompleteAccount)
    {
        return new Account
        {
            guid = XYVRGuids.ForAccount(),
            namedApp = incompleteAccount.namedApp,
            qualifiedAppName = incompleteAccount.qualifiedAppName,
            inAppIdentifier = incompleteAccount.inAppIdentifier,
            inAppDisplayName = incompleteAccount.inAppDisplayName,
            callers = incompleteAccount.callers.Select(IncompleteCallerAccount.MakeComplete).ToList(),
            allDisplayNames = [incompleteAccount.inAppDisplayName],
            isTechnical = false,
            
            isPendingUpdate = true // true because indexing an IncompleteAccount means we're missing note and specifics information in it.
        };
    }
}

/// The caller account may be incomplete, as we do not necessarily know if that account is a contact of the account it is attached to;
/// for example, when fetching recent notes as notes can be attached to non-contacts.<br/>
/// When that incomplete caller account is ingested and merged into an existing account, we only write about what we know about this caller.
public class IncompleteCallerAccount
{
    public bool isAnonymous;
    public string? inAppIdentifier; // Can only be null if it's an anonymous caller.
    
    public bool? isContact;

    public static CallerAccount MakeComplete(IncompleteCallerAccount incomplete)
    {
        return new CallerAccount
        {
            inAppIdentifier = incomplete.inAppIdentifier,
            isAnonymous = incomplete.isAnonymous,
            isContact = incomplete.isContact ?? false,
            note = new Note
            {
                status = NoteState.NeverHad,
                text = null
            }
        };
    }
}
