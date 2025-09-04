using XYVR.API.Resonite;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteCommunicator
{
    private const string ResoniteQualifiedAppName = "resonite";
    
    private readonly IResponseCollector _responseCollector;
    private readonly ICredentialsStorage _credentialsStorage;

    private readonly string _username__sensitive;
    private readonly string _password__sensitive;
    private readonly string _uid;
    
    private ResoniteAPI? _api;
    private string _callerUserId;
    private string _callerDisplayName;

    public ResoniteCommunicator(IResponseCollector responseCollector, string? username__sensitive, string? password__sensitive, string uid__sensitive, ICredentialsStorage credentialsStorage)
    {
        _responseCollector = responseCollector;
        _credentialsStorage = credentialsStorage;

        _username__sensitive = username__sensitive!;
        _password__sensitive = password__sensitive!;
        _uid = uid__sensitive;
        
        if (_uid == null)
        {
            Console.Error.WriteLine($"UID missing. Do you need one? Here's a random UID: {ResoniteAPI.RandomUID__NotCryptographicallySecure()}");
            throw new ArgumentException("Missing UID");
        }
    }

    public async Task ResoniteLogin()
    {
        var api = new ResoniteAPI(Guid.NewGuid().ToString(), _uid, _responseCollector);
        
        _ = await api.Login(_username__sensitive, _password__sensitive);
        await _credentialsStorage.StoreCookieOrToken(api.GetAllUserAndToken__Sensitive());
    }

    public async Task<Account> CallerAccount()
    {
        _api ??= await InitializeApi();
        // Initializing the API actually gets the caller account.

        return new Account
        {
            guid = Guid.NewGuid().ToString(),
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteQualifiedAppName,
            inAppIdentifier = _callerUserId,
            inAppDisplayName = _callerDisplayName
        };
    }
    
    /// Calls the User Contacts API to collect possible accounts haven't been collected yet.<br/>
    /// Only returns user IDs that aren't in the repository yet.
    public async Task<List<Account>> FindUndiscoveredAccounts(IndividualRepository individualRepository)
    {
        var resoniteAccountIdentifiers = individualRepository.CollectAllInAppIdentifiers(NamedApp.Resonite);

        _api ??= await InitializeApi();

        var contacts = await _api.GetUserContacts(DataCollectionReason.FindUndiscoveredAccounts);
        
        var undiscoveredContacts = contacts.Where(contact => !resoniteAccountIdentifiers.Contains(contact.id)).ToList();
        if (undiscoveredContacts.Count == 0) return [];
        
        var undiscoveredContactIdToUser = new Dictionary<string, CombinedContactAndUser>();
        foreach (var undiscoveredContact in undiscoveredContacts)
        {
            // Don't parallelize this. We don't want to abuse the Resonite API.
            var userN = await _api.GetUser(undiscoveredContact.id, DataCollectionReason.CollectUndiscoveredAccount);
            if (userN is { } user)
            {
                undiscoveredContactIdToUser.Add(undiscoveredContact.id, new CombinedContactAndUser(undiscoveredContact.id, undiscoveredContact, user));
            }
        }

        return undiscoveredContactIdToUser.Values
            .Select(user => AsAccount(user, _callerUserId, true))
            .ToList();
    }
    
    public async IAsyncEnumerable<Account> FindAccounts()
    {
        _api ??= await InitializeApi();

        var contacts = await _api.GetUserContacts(DataCollectionReason.FindUndiscoveredAccounts);
    
        var undiscoveredContacts = contacts.ToList();
        if (undiscoveredContacts.Count == 0) yield break;
    
        foreach (var undiscoveredContact in undiscoveredContacts)
        {
            // Don't parallelize this. We don't want to abuse the Resonite API.
            var userN = await _api.GetUser(undiscoveredContact.id, DataCollectionReason.CollectUndiscoveredAccount);
            if (userN is { } user)
            {
                var combinedContactAndUser = new CombinedContactAndUser(undiscoveredContact.id, undiscoveredContact, user);
                yield return AsAccount(combinedContactAndUser, _callerUserId, true);
            }
        }
    }

    /// Get the list of user contact IDs for the purposes of combining it with the users data.<br/>
    /// The intended purpose of this endpoint is to provide missing information about the user,
    /// as contact information is not available in the users endpoint.
    public async Task<HashSet<string>> CollectContactUserIdsToCombineWithUsers()
    {
        _api ??= await InitializeApi();

        var contacts = await _api.GetUserContacts(DataCollectionReason.CollectExistingAccount);

        return contacts
            .Select(contact => contact.id)
            .ToHashSet();
    }

    public async Task<List<Account>> CollectAllLenient(List<string> notNecessarilyValidUserIds, HashSet<string> resoniteContactIds)
    {
        var distinctNotNecessarilyValidUserIds = notNecessarilyValidUserIds
            .Distinct() // Get rid of duplicates
            .ToList();

        _api ??= await InitializeApi();

        var accounts = new List<Account>();
        foreach (var userId in distinctNotNecessarilyValidUserIds)
        {
            var userN = await _api.GetUser(userId, DataCollectionReason.CollectExistingAccount);
            if (userN is { } user)
            {
                accounts.Add(UserAsAccount(user, _callerUserId, resoniteContactIds));
            }
        }

        return accounts;
    }

    public Account ConvertUserAsAccount(UserResponseJsonObject user, string callerUserId, HashSet<string> resoniteContactIds)
    {
        return UserAsAccount(user, callerUserId, resoniteContactIds);
    }

    private static Account UserAsAccount(UserResponseJsonObject user, string callerUserId, HashSet<string> resoniteContactIds)
    {
        return new Account
        {
            guid = Guid.NewGuid().ToString(),
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteQualifiedAppName,
            inAppIdentifier = user.id,
            inAppDisplayName = user.username,
            callers =
            [
                new CallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = callerUserId,
                    isContact = resoniteContactIds.Contains(user.id),
                    note = new Note
                    {
                        status = NoteState.NeverHad,
                        text = null
                    }
                }
            ]
        };
    }

    private async Task<ResoniteAPI> InitializeApi()
    {
        var api = new ResoniteAPI(Guid.NewGuid().ToString(), _uid, _responseCollector);

        var userAndToken__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (userAndToken__sensitive != null)
        {
            api.ProvideUserAndToken(userAndToken__sensitive);
            var user = await api.GetUser__self(DataCollectionReason.CollectCallerAccount);
            _callerUserId = user.id;
            _callerDisplayName = user.username;
            
            return api;
        }
        
        var loginResult = await api.Login(_username__sensitive, _password__sensitive);
        await _credentialsStorage.StoreCookieOrToken(api.GetAllUserAndToken__Sensitive());
        
        var callerUserN = await api.GetUser(loginResult.entity.userId, DataCollectionReason.CollectCallerAccount);
        if (callerUserN == null)
        {
            throw new Exception("Not Found returned while getting the caller's account data"); // FIXME: Proper exception type
        }
        
        _callerUserId = loginResult.entity.userId;
        _callerDisplayName = ((UserResponseJsonObject)callerUserN).username;
        
        return api;
    }

    private static Account AsAccount(CombinedContactAndUser combined, string callerUserId, bool isContact)
    {
        return new Account
        {
            guid = Guid.NewGuid().ToString(),
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteQualifiedAppName,
            inAppIdentifier = combined.User.id,
            inAppDisplayName = combined.User.username,
            callers =
            [
                new CallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = callerUserId,
                    isContact = isContact,
                    note = new Note
                    {
                        status = NoteState.NeverHad,
                        text = null
                    }
                }
            ]
        };
    }
}