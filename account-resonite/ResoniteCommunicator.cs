using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

internal class ResoniteCommunicator
{
    public const string ResoniteQualifiedAppName = "resonite";
    
    private readonly IResponseCollector _responseCollector;
    private readonly ICredentialsStorage _credentialsStorage;

    private readonly bool _stayLoggedIn;
    private readonly string _uid;
    
    private ResoniteAPI? _api;
    private string _callerUserId = null!;
    private string _callerDisplayName = null!;

    public ResoniteCommunicator(IResponseCollector responseCollector, bool stayLoggedIn, string uid__sensitive, ICredentialsStorage credentialsStorage)
    {
        _responseCollector = responseCollector;
        _credentialsStorage = credentialsStorage;

        _stayLoggedIn = stayLoggedIn;
        _uid = uid__sensitive;
    }

    public async Task ResoniteLogin(string username__sensitive, string password__sensitive, string? twoFactorCode__sensitive)
    {
        var api = new ResoniteAPI(XYVRGuids.ForResoniteMachineId(), _uid, _responseCollector);
        
        _ = await api.Login(username__sensitive, password__sensitive, _stayLoggedIn, twoFactorCode__sensitive);
        await _credentialsStorage.StoreCookieOrToken(api.GetAllUserAndToken__Sensitive());
    }

    public async Task ResoniteLogout()
    {
        _api ??= await InitializeApi();
        
        await _api.Logout();
        await _credentialsStorage.DeleteCookieOrToken();
    }

    public async Task<ImmutableNonIndexedAccount> CallerAccount()
    {
        _api ??= await InitializeApi();
        // Initializing the API actually gets the caller account.

        return new ImmutableNonIndexedAccount
        {
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteQualifiedAppName,
            inAppIdentifier = _callerUserId,
            inAppDisplayName = _callerDisplayName
        };
    }

    public async IAsyncEnumerable<ImmutableIncompleteAccount> FindIncompleteAccounts()
    {
        _api ??= await InitializeApi();

        var contacts = await _api.GetUserContacts(DataCollectionReason.FindUndiscoveredAccounts);
        foreach (var contact in contacts)
        {
            yield return new ImmutableIncompleteAccount
            {
                namedApp = NamedApp.Resonite,
                qualifiedAppName = ResoniteQualifiedAppName,
                inAppIdentifier = contact.id,
                inAppDisplayName = contact.contactUsername,
                callers = [new ImmutableIncompleteCallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = _callerUserId,
                    isContact = contact.isAccepted,
                    note = null
                }]
            };
        }
    }
    
    public async Task<ImmutableNonIndexedAccount?> GetUser(string id, bool isContact)
    {
        _api ??= await InitializeApi();

        var userN = await _api.GetUser(id, DataCollectionReason.CollectUndiscoveredAccount);
        if (userN == null) return null;
        
        var user = (UserResponseJsonObject)userN;
        return new ImmutableNonIndexedAccount
        {
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteQualifiedAppName,
            inAppIdentifier = user.id,
            inAppDisplayName = user.username,
            callers =
            [
                new ImmutableCallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = _callerUserId,
                    isContact = isContact,
                    note = new ImmutableNote
                    {
                        status = NoteState.NeverHad,
                        text = null
                    }
                }
            ]
        };
    }

    public async Task<List<ImmutableNonIndexedAccount>> CollectAllLenient(List<string> notNecessarilyValidUserIds, HashSet<string> resoniteContactIds)
    {
        var distinctNotNecessarilyValidUserIds = notNecessarilyValidUserIds
            .Distinct() // Get rid of duplicates
            .ToList();

        _api ??= await InitializeApi();

        var accounts = new List<ImmutableNonIndexedAccount>();
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

    public ImmutableNonIndexedAccount ConvertUserAsAccount(UserResponseJsonObject user, string callerUserId, HashSet<string> resoniteContactIds)
    {
        return UserAsAccount(user, callerUserId, resoniteContactIds);
    }

    private static ImmutableNonIndexedAccount UserAsAccount(UserResponseJsonObject user, string callerUserId, HashSet<string> resoniteContactIds)
    {
        return new ImmutableNonIndexedAccount
        {
            namedApp = NamedApp.Resonite,
            qualifiedAppName = ResoniteQualifiedAppName,
            inAppIdentifier = user.id,
            inAppDisplayName = user.username,
            callers =
            [
                new ImmutableCallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = callerUserId,
                    isContact = resoniteContactIds.Contains(user.id),
                    note = new ImmutableNote
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
        if (userAndToken__sensitive == null)
        {
            // TODO: Check token expiration
            throw new ArgumentException("User must have already logged in before establishing communication");
        }
        
        api.ProvideUserAndToken(userAndToken__sensitive);
        
        var user = await api.GetUser__self(DataCollectionReason.CollectCallerAccount);

        if (user.email == null)
        {
            throw new ArgumentException("Api response does not contain email. This means your token expired.");
        }

        _callerUserId = user.id;
        _callerDisplayName = user.username;
            
        return api;
    }
}