using XYVR.API.Resonite;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteCommunicator
{
    public const string ResoniteQualifiedAppName = "resonite";
    
    private readonly IResponseCollector _responseCollector;
    private readonly ICredentialsStorage _credentialsStorage;

    private readonly string _username__sensitive;
    private readonly string _password__sensitive;
    private readonly bool _stayLoggedIn;
    private readonly string _uid;
    
    private ResoniteAPI? _api;
    private string _callerUserId;
    private string _callerDisplayName;

    public ResoniteCommunicator(IResponseCollector responseCollector, string? username__sensitive, string? password__sensitive, bool stayLoggedIn, string uid__sensitive, ICredentialsStorage credentialsStorage)
    {
        _responseCollector = responseCollector;
        _credentialsStorage = credentialsStorage;

        _username__sensitive = username__sensitive!;
        _password__sensitive = password__sensitive!;
        _stayLoggedIn = stayLoggedIn;
        _uid = uid__sensitive;
        
        if (_uid == null)
        {
            Console.Error.WriteLine($"UID missing. Do you need one? Here's a random UID: {ResoniteAPI.RandomUID__NotCryptographicallySecure()}");
            throw new ArgumentException("Missing UID");
        }
    }

    public async Task ResoniteLogin()
    {
        var api = new ResoniteAPI(XYVRGuids.ForResoniteMachineId(), _uid, _responseCollector);
        
        _ = await api.Login(_username__sensitive, _password__sensitive, _stayLoggedIn);
        await _credentialsStorage.StoreCookieOrToken(api.GetAllUserAndToken__Sensitive());
    }

    public async Task<NonIndexedAccount> CallerAccount()
    {
        _api ??= await InitializeApi();
        // Initializing the API actually gets the caller account.

        return new NonIndexedAccount
        {
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteQualifiedAppName,
            inAppIdentifier = _callerUserId,
            inAppDisplayName = _callerDisplayName
        };
    }

    public async IAsyncEnumerable<IncompleteAccount> FindIncompleteAccounts()
    {
        _api ??= await InitializeApi();

        var contacts = await _api.GetUserContacts(DataCollectionReason.FindUndiscoveredAccounts);
        foreach (var contact in contacts)
        {
            yield return new IncompleteAccount
            {
                namedApp = NamedApp.Resonite,
                qualifiedAppName = ResoniteQualifiedAppName,
                inAppIdentifier = contact.id,
                inAppDisplayName = contact.contactUsername,
                callers = [new IncompleteCallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = _callerUserId,
                    isContact = contact.isAccepted,
                    note = null
                }]
            };
        }
    }
    
    public async Task<NonIndexedAccount?> GetUser(string id, bool isContact)
    {
        _api ??= await InitializeApi();

        var userN = await _api.GetUser(id, DataCollectionReason.CollectUndiscoveredAccount);
        if (userN == null) return null;
        
        var user = (UserResponseJsonObject)userN;
        return new NonIndexedAccount
        {
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteQualifiedAppName,
            inAppIdentifier = user.id,
            inAppDisplayName = user.username,
            callers =
            [
                new CallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = _callerUserId,
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

    public async Task<List<NonIndexedAccount>> CollectAllLenient(List<string> notNecessarilyValidUserIds, HashSet<string> resoniteContactIds)
    {
        var distinctNotNecessarilyValidUserIds = notNecessarilyValidUserIds
            .Distinct() // Get rid of duplicates
            .ToList();

        _api ??= await InitializeApi();

        var accounts = new List<NonIndexedAccount>();
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

    public NonIndexedAccount ConvertUserAsAccount(UserResponseJsonObject user, string callerUserId, HashSet<string> resoniteContactIds)
    {
        return UserAsAccount(user, callerUserId, resoniteContactIds);
    }

    private static NonIndexedAccount UserAsAccount(UserResponseJsonObject user, string callerUserId, HashSet<string> resoniteContactIds)
    {
        return new NonIndexedAccount
        {
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
        var api = new ResoniteAPI(XYVRGuids.ForResoniteMachineId(), _uid, _responseCollector);

        var userAndToken__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (userAndToken__sensitive != null)
        {
            api.ProvideUserAndToken(userAndToken__sensitive);
            var user = await api.GetUser__self(DataCollectionReason.CollectCallerAccount);
            _callerUserId = user.id;
            _callerDisplayName = user.username;
            
            return api;
        }
        
        var loginResult = await api.Login(_username__sensitive, _password__sensitive, false);
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
}