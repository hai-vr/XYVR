using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace XYVR.Core;

/// A non-indexed account is built from data coming from external services. Thus, it doesn't have a Guid because
/// it represents objects that are not indexed. It also doesn't have a list of past display names.
public record ImmutableNonIndexedAccount
{
    public NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }
    public required string inAppIdentifier { get; init; }
    public required string inAppDisplayName { get; init; }
    [JsonConverter(typeof(SpecificsConverter))]
    public object? specifics { get; init; }
    public ImmutableArray<ImmutableCallerAccount> callers { get; init; } = ImmutableArray<ImmutableCallerAccount>.Empty;

    public ImmutableAccountIdentification AsIdentification()
    {
        return new ImmutableAccountIdentification
        {
            inAppIdentifier = inAppIdentifier,
            namedApp = namedApp,
            qualifiedAppName = qualifiedAppName,
        };
    }

    public static ImmutableAccount MakeIndexed(ImmutableNonIndexedAccount nonIndexedAccount)
    {
        return new ImmutableAccount
        {
            guid = XYVRGuids.ForAccount(),
            namedApp = nonIndexedAccount.namedApp,
            qualifiedAppName = nonIndexedAccount.qualifiedAppName,
            inAppIdentifier = nonIndexedAccount.inAppIdentifier,
            inAppDisplayName = nonIndexedAccount.inAppDisplayName,
            specifics = nonIndexedAccount.specifics,
            callers = nonIndexedAccount.callers,
        };
    }
}

/// A non-indexed account is built from data coming from external services. However, it doesn't have notes or specific information
/// in it. When we receive information when getting contacts from an external service, we can index it and have it
/// show up in the UI as early as possible without actually needing to make calls and retrieve additional information about them.<br/>
/// The consequence of this is that when we convert this incomplete account to an indexed account, it is marked in the UI as pending,
/// so that the user knows the information about that account is incomplete.
public record ImmutableIncompleteAccount
{
    public NamedApp namedApp { get; init; }
    public required string qualifiedAppName { get; init; }
    
    public required string inAppIdentifier { get; init; }
    public required string inAppDisplayName { get; init; }
    
    public ImmutableArray<ImmutableIncompleteCallerAccount> callers { get; init; } = ImmutableArray<ImmutableIncompleteCallerAccount>.Empty;
    
    public ImmutableAccountIdentification AsIdentification()
    {
        return new ImmutableAccountIdentification
        {
            inAppIdentifier = inAppIdentifier,
            namedApp = namedApp,
            qualifiedAppName = qualifiedAppName,
        };
    }

    public static ImmutableAccount MakeIndexed(ImmutableIncompleteAccount incompleteAccount)
    {
        return new ImmutableAccount
        {
            guid = XYVRGuids.ForAccount(),
            namedApp = incompleteAccount.namedApp,
            qualifiedAppName = incompleteAccount.qualifiedAppName,
            inAppIdentifier = incompleteAccount.inAppIdentifier,
            inAppDisplayName = incompleteAccount.inAppDisplayName,
            callers = [..incompleteAccount.callers.Select(ImmutableIncompleteCallerAccount.MakeComplete)],
            allDisplayNames = [incompleteAccount.inAppDisplayName],
            isTechnical = false,
            
            isPendingUpdate = true // true because indexing an IncompleteAccount means we're missing note and specifics information in it.
        };
    }
}

/// The caller account may be incomplete, as we do not necessarily know if that account is a contact of the account it is attached to;
/// for example, when fetching recent notes as notes can be attached to non-contacts.<br/>
/// When that incomplete caller account is ingested and merged into an existing account, we only write about what we know about this caller.
public record ImmutableIncompleteCallerAccount
{
    public bool isAnonymous { get; init; }
    public string? inAppIdentifier { get; init; } // Can only be null if it's an anonymous caller.
    
    public bool? isContact { get; init; }
    public ImmutableNote? note { get; init; }

    public static ImmutableCallerAccount MakeComplete(ImmutableIncompleteCallerAccount incomplete)
    {
        return new ImmutableCallerAccount
        {
            inAppIdentifier = incomplete.inAppIdentifier,
            isAnonymous = incomplete.isAnonymous,
            isContact = incomplete.isContact ?? false,
            note = incomplete.note ?? new ImmutableNote
            {
                status = NoteState.NeverHad,
                text = null
            }
        };
    }
}
