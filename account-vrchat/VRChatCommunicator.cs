using XYVR.API.VRChat;
using XYVR.Core;

namespace XYVR.AccountAuthority.VRChat;

internal class VRChatCommunicator
{
    public const string VRChatQualifiedAppName = "vrchat";
    
    private readonly IResponseCollector _responseCollector;

    private readonly ICredentialsStorage _credentialsStorage;
    private VRChatAPI? _api;
    private string _callerUserId;

    public VRChatCommunicator(IResponseCollector responseCollector, ICredentialsStorage credentialsStorage)
    {
        _responseCollector = responseCollector;
        _credentialsStorage = credentialsStorage;
    }

    public async Task<ImmutableNonIndexedAccount> CallerAccount()
    {
        _api ??= await InitializeAPI();

        var user = await _api.GetUserLenient(_callerUserId, DataCollectionReason.CollectCallerAccount);
        if (user == null) throw new Exception("Unable to get the caller's account data"); // FIXME: Get a better exception type.

        return UserAsAccount((VRChatUser)user, _callerUserId);
    }
    
    /// This lists the friends and then the notes. Some notes may refer to friends that have already been returned, so there may be multiple references to the same account.
    /// The caller of this method needs to take that into consideration.
    public async IAsyncEnumerable<ImmutableIncompleteAccount> FindIncompleteAccountsMayIncludeDuplicateReferences()
    {
        _api ??= await InitializeAPI();

        var contactsAsyncEnum = _api.ListFriends(ListFriendsRequestType.OnlyOnline, DataCollectionReason.FindUndiscoveredAccounts)
            .Concat(_api.ListFriends(ListFriendsRequestType.OnlyOffline, DataCollectionReason.FindUndiscoveredAccounts));
        await foreach (var friend in contactsAsyncEnum)
        {
            var acc = new ImmutableIncompleteAccount
            {
                namedApp = NamedApp.VRChat,
                qualifiedAppName = VRChatQualifiedAppName,
                inAppIdentifier = friend.id,
                inAppDisplayName = friend.displayName,
                callers =
                [
                    new ImmutableIncompleteCallerAccount
                    {
                        isAnonymous = false,
                        inAppIdentifier = _callerUserId,
                        isContact = true,
                        note = null
                    }
                ]
            };
            yield return acc;
        }
        
        await foreach (var note in _api.ListUserNotes(DataCollectionReason.FindUndiscoveredAccounts))
        {
            var hasNote = !string.IsNullOrWhiteSpace(note.note);
            var acc = new ImmutableIncompleteAccount
            {
                namedApp = NamedApp.VRChat,
                qualifiedAppName = VRChatQualifiedAppName,
                inAppIdentifier = note.targetUserId,
                inAppDisplayName = note.targetUser.displayName,
                callers =
                [
                    new ImmutableIncompleteCallerAccount
                    {
                        isAnonymous = false,
                        inAppIdentifier = _callerUserId,
                        isContact = null, // We don't know if it's a contact.
                        note = new ImmutableNote
                        {
                            status = hasNote ? NoteState.Exists : NoteState.NeverHad,
                            text = hasNote ? note.note : null
                        }
                    }
                ]
            };

            yield return acc;
        }
    }

    /// Given a list of user IDs that may or may not exist, return a list of accounts.<br/>
    /// The returned list may be smaller than the input list, especially if some accounts no longer exist.<br/>
    /// User IDs do not necessarily start with usr_ as this supports some oldschool accounts.
    public async Task<List<ImmutableNonIndexedAccount>> CollectAllLenient(List<string> notNecessarilyValidUserIds)
    {
        var distinctNotNecessarilyValidUserIds = notNecessarilyValidUserIds
            .Distinct() // Get rid of duplicates
            .ToList();
        
        _api ??= await InitializeAPI();

        var accounts = new List<ImmutableNonIndexedAccount>();
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

    public ImmutableNonIndexedAccount ConvertUserAsAccount(VRChatUser user, string callerUserId)
    {
        return UserAsAccount(user, callerUserId);
    }

    private static ImmutableNonIndexedAccount UserAsAccount(VRChatUser user, string callerUserId)
    {
        return new ImmutableNonIndexedAccount
        {
            namedApp = NamedApp.VRChat,
            qualifiedAppName = VRChatQualifiedAppName,
            inAppIdentifier = user.id,
            inAppDisplayName = user.displayName,
            specifics = new ImmutableVRChatSpecifics
            {
                urls = user.bioLinks == null ? [] : [..user.bioLinks.Where(s => s != null).Cast<string>()],
                bio = user.bio ?? "",
                pronouns = user.pronouns ?? ""
            },
            callers = [
                new ImmutableCallerAccount
                {
                    isAnonymous = false,
                    inAppIdentifier = callerUserId,
                    isContact = user.isFriend,
                    note = new ImmutableNote
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
        if (userinput_cookies__sensitive == null)
        {
            throw new ArgumentException("User must have already logged in before establishing communication");
        }

        api.ProvideCookies(userinput_cookies__sensitive);

        // TODO: Check token expiration
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