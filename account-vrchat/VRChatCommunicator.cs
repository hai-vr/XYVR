using XYVR.API.VRChat;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatCommunicator
{
    private const string VRChatQualifiedAppName = "vrchat";
    
    private readonly IResponseCollector _responseCollector;
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ICredentialsStorage _credentialsStorage;
    private VRChatAPI? _api;
    private string _callerUserId;

    public VRChatCommunicator(IResponseCollector responseCollector, ICredentialsStorage credentialsStorage)
    {
        _responseCollector = responseCollector;
        _credentialsStorage = credentialsStorage;
    }

    public async Task<Account> CallerAccount()
    {
        _api ??= await InitializeAPI();

        var user = await _api.GetUserLenient(_callerUserId, DataCollectionReason.CollectCallerAccount);
        if (user == null) throw new Exception("Unable to get the caller's account data"); // FIXME: Get a better exception type.

        return UserAsAccount((VRChatUser)user, _callerUserId);
    }
    
    /// Calls various APIs to collect possible accounts haven't been collected yet.<br/>
    /// This can include friend lists (containing only friends) and the recently updated notes (containing a mix of friends and non-friends).<br/>
    /// Only returns user IDs that aren't in the repository yet.
    public async Task<List<IncompleteAccount>> FindUndiscoveredIncompleteAccounts(IndividualRepository individualRepository)
    {
        var vrchatAccountIdentifiers = individualRepository.CollectAllInAppIdentifiers(NamedApp.VRChat);
        
        _api ??= await InitializeAPI();

        var onlineFriends = await _api.ListFriends(ListFriendsRequestType.OnlyOnline, DataCollectionReason.FindUndiscoveredAccounts).ToListAsync();
        var offlineFriends = await _api.ListFriends(ListFriendsRequestType.OnlyOffline, DataCollectionReason.FindUndiscoveredAccounts).ToListAsync();
        var userNotes = await _api.ListUserNotes(DataCollectionReason.FindUndiscoveredAccounts).ToListAsync();

        var friendsAsAccounts = onlineFriends.Concat(offlineFriends)
            .Where(friend => !vrchatAccountIdentifiers.Contains(friend.id))
            .Select(friend => new IncompleteAccount
            {
                namedApp = NamedApp.VRChat,
                qualifiedAppName = VRChatQualifiedAppName,
                inAppIdentifier = friend.id,
                inAppDisplayName = friend.displayName,
                callers =
                [
                    new IncompleteCallerAccount
                    {
                        isAnonymous = false,
                        inAppIdentifier = _callerUserId,
                        isContact = true
                    }
                ]
            })
            .ToList();
        
        var accountsCollectedSoFar = new HashSet<string>(vrchatAccountIdentifiers);
        accountsCollectedSoFar.UnionWith(friendsAsAccounts.Select(account => account.inAppIdentifier));

        var notesAsAccounts = userNotes
            // TODO: We could add .note to IncompleteAccount and set it next, even if a friend was already collected above without a note
            .Where(note => !accountsCollectedSoFar.Contains(note.targetUserId))
            .Select(full => new IncompleteAccount
            {
                namedApp = NamedApp.VRChat,
                qualifiedAppName = VRChatQualifiedAppName,
                inAppIdentifier = full.targetUserId,
                inAppDisplayName = full.targetUser.displayName,
                callers =
                [
                    new IncompleteCallerAccount
                    {
                        isAnonymous = false,
                        inAppIdentifier = _callerUserId,
                        isContact = null // We don't know if it's a contact.
                    }
                ]
            })
            .ToList();

        return friendsAsAccounts.Concat(notesAsAccounts).ToList();
    }
    
    public async IAsyncEnumerable<IncompleteAccount> FindIncompleteAccounts()
    {
        _api ??= await InitializeAPI();

        var accountsCollectedSoFar = new HashSet<string>();
        
        var contactsAsyncEnum = _api.ListFriends(ListFriendsRequestType.OnlyOnline, DataCollectionReason.FindUndiscoveredAccounts)
            .Concat(_api.ListFriends(ListFriendsRequestType.OnlyOffline, DataCollectionReason.FindUndiscoveredAccounts));
        await foreach (var friend in contactsAsyncEnum)
        {
            var acc = new IncompleteAccount
            {
                namedApp = NamedApp.VRChat,
                qualifiedAppName = VRChatQualifiedAppName,
                inAppIdentifier = friend.id,
                inAppDisplayName = friend.displayName,
                callers =
                [
                    new IncompleteCallerAccount
                    {
                        isAnonymous = false,
                        inAppIdentifier = _callerUserId,
                        isContact = true
                    }
                ]
            };
            accountsCollectedSoFar.Add(acc.inAppIdentifier);
            yield return acc;
        }
        
        await foreach (var note in _api.ListUserNotes(DataCollectionReason.FindUndiscoveredAccounts))
        {
            var acc = new IncompleteAccount
            {
                namedApp = NamedApp.VRChat,
                qualifiedAppName = VRChatQualifiedAppName,
                inAppIdentifier = note.targetUserId,
                inAppDisplayName = note.targetUser.displayName,
                callers =
                [
                    new IncompleteCallerAccount
                    {
                        isAnonymous = false,
                        inAppIdentifier = _callerUserId,
                        isContact = null // We don't know if it's a contact.
                    }
                ]
            };

            if (!accountsCollectedSoFar.Contains(acc.inAppIdentifier))
            {
                yield return acc;
            }
        }
    }

    /// Given a list of user IDs that may or may not exist, return a list of accounts.<br/>
    /// This does not return accounts that already exist in the repository.<br/>
    /// The returned list may be smaller than the input list, especially if some accounts no longer exist.<br/>
    /// User IDs do not necessarily start with usr_ as this supports some oldschool accounts.
    public async Task<List<Account>> CollectUndiscoveredLenient(IndividualRepository repository, List<string> notNecessarilyValidUserIds)
    {
        var vrchatAccountIdentifiers = repository.CollectAllInAppIdentifiers(NamedApp.VRChat);
        var undiscoveredAndNotNecessarilyValidUserIds = notNecessarilyValidUserIds
            .Where(userId => !vrchatAccountIdentifiers.Contains(userId))
            .Distinct() // Get rid of duplicates
            .ToList();
        
        _api ??= await InitializeAPI();

        var accounts = new List<Account>();
        foreach (var userId in undiscoveredAndNotNecessarilyValidUserIds)
        {
            var user = await _api.GetUserLenient(userId, DataCollectionReason.CollectUndiscoveredAccount);
            if (user != null)
            {
                accounts.Add(UserAsAccount((VRChatUser)user, _callerUserId));
            }
        }

        return accounts;
    }

    /// Given a list of user IDs that may or may not exist, return a list of accounts.<br/>
    /// The returned list may be smaller than the input list, especially if some accounts no longer exist.<br/>
    /// User IDs do not necessarily start with usr_ as this supports some oldschool accounts.
    public async Task<List<Account>> CollectAllLenient(List<string> notNecessarilyValidUserIds)
    {
        var distinctNotNecessarilyValidUserIds = notNecessarilyValidUserIds
            .Distinct() // Get rid of duplicates
            .ToList();
        
        _api ??= await InitializeAPI();

        var accounts = new List<Account>();
        foreach (var userId in distinctNotNecessarilyValidUserIds)
        {
            var user = await _api.GetUserLenient(userId, DataCollectionReason.CollectExistingAccount);
            if (user != null)
            {
                accounts.Add(UserAsAccount((VRChatUser)user, _callerUserId));
            }
        }

        return accounts;
    }

    public Account ConvertUserAsAccount(VRChatUser user, string callerUserId)
    {
        return UserAsAccount(user, callerUserId);
    }

    private static Account UserAsAccount(VRChatUser user, string callerUserId)
    {
        return new Account
        {
            guid = Guid.NewGuid().ToString(),
            namedApp = NamedApp.VRChat,
            qualifiedAppName = VRChatQualifiedAppName,
            inAppIdentifier = user.id,
            inAppDisplayName = user.displayName,
            specifics = new VRChatSpecifics
            {
                urls = user.bioLinks == null ? [] : user.bioLinks.Where(s => s != null).Cast<string>().ToList(),
                bio = user.bio ?? "",
                pronouns = user.pronouns ?? ""
            },
            callers = [
                new CallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = callerUserId,
                    isContact = user.isFriend,
                    note = new Note
                    {
                        status = string.IsNullOrWhiteSpace(user.note) ? NoteState.NeverHad : NoteState.Exists,
                        text = string.IsNullOrWhiteSpace(user.note) ? null : user.note
                    }
                }
            ]
        };
    }

    private async Task<VRChatAPI> InitializeAPI()
    {
        var api = new VRChatAPI(_responseCollector);
        var userinput_cookies__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (userinput_cookies__sensitive != null)
        {
            api.ProvideCookies(userinput_cookies__sensitive);
        }

        if (!api.IsLoggedIn)
        {
            throw new ArgumentException("User must be already logged in before establishing communication");
        }

        var authUser = await api.GetAuthUser(DataCollectionReason.CollectCallerAccount);
        _callerUserId = authUser.id;

        return api;
    }

    public async Task<bool> SoftIsLoggedIn()
    {
        var api = new VRChatAPI(_responseCollector);
        var userinput_cookies__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (userinput_cookies__sensitive != null)
        {
            api.ProvideCookies(userinput_cookies__sensitive);
        }

        return api.IsLoggedIn;
    }
}