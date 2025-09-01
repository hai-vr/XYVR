using System.Text;
using XYVR.API.VRChat;
using XYVR.Core;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatCommunicator
{
    private const string CookieFileName = "vrc.cookies.txt";
    
    private readonly string _account__sensitive;
    private readonly string _password__sensitive;
    private readonly string? _twoFactor__sensitive;
    private VRChatAPI? _api;
    private string _caller;

    public VRChatCommunicator()
    {
        _account__sensitive = Environment.GetEnvironmentVariable(XYVREnvVar.VRChatAccount)!;
        _password__sensitive = Environment.GetEnvironmentVariable(XYVREnvVar.VRChatPassword)!;
        if (_account__sensitive == null || _password__sensitive == null) throw new ArgumentException("Missing environment variables");
        
        _twoFactor__sensitive = Environment.GetEnvironmentVariable(XYVREnvVar.VRChatTwoFactorCode);
    }
    
    /// Calls various APIs to collect possible accounts haven't been collected yet.<br/>
    /// This can include friend lists (containing only friends) and the recently updated notes (containing a mix of friends and non-friends).<br/>
    /// Only returns user IDs that aren't in the repository yet.
    public async Task<List<IncompleteAccount>> FindUndiscoveredIncompleteAccounts(IndividualRepository individualRepository)
    {
        var vrchatAccountIdentifiers = individualRepository.CollectAllInAppIdentifiers(NamedApp.VRChat);
        
        _api ??= await InitializeAPI();

        var onlineFriends = await _api.ListFriends(ListFriendsRequestType.OnlyOnline);
        var offlineFriends = await _api.ListFriends(ListFriendsRequestType.OnlyOffline);
        var userNotes = await _api.ListUserNotes();

        var friendsAsAccounts = onlineFriends.Concat(offlineFriends)
            .Where(friend => !vrchatAccountIdentifiers.Contains(friend.id))
            .Select(friend => new IncompleteAccount
            {
                namedApp = NamedApp.VRChat,
                qualifiedAppName = "vrchat",
                inAppIdentifier = friend.id,
                inAppDisplayName = friend.displayName,
                liveServerData = friend,
                callers =
                [
                    new IncompleteCallerAccount
                    {
                        isAnonymous = false,
                        inAppIdentifier = _caller
                    }
                ]
            })
            .ToList();
        
        var accountsCollectedSoFar = new HashSet<string>(vrchatAccountIdentifiers);
        accountsCollectedSoFar.UnionWith(friendsAsAccounts.Select(account => account.inAppIdentifier));

        var notesAsAccounts = userNotes
            .Where(note => !accountsCollectedSoFar.Contains(note.targetUserId))
            .Select(full => new IncompleteAccount
            {
                namedApp = NamedApp.VRChat,
                qualifiedAppName = "vrchat",
                inAppIdentifier = full.targetUserId,
                inAppDisplayName = full.targetUser.displayName,
                liveServerData = full,
                callers =
                [
                    new IncompleteCallerAccount
                    {
                        isAnonymous = false,
                        inAppIdentifier = _caller
                    }
                ]
            })
            .ToList();

        return friendsAsAccounts.Concat(notesAsAccounts).ToList();
    }

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
            var user = await _api.GetUserLenient(userId);
            if (user != null)
            {
                accounts.Add(UserAsAccount((VRChatUser)user));
            }
        }

        return accounts;
    }

    public async Task<Note?> CollectNoteFromUser(Account vrcAccount)
    {
        _api ??= await InitializeAPI();
        
        var resultN = await _api.GetUserLenient(vrcAccount.inAppIdentifier);
        if (resultN == null) return null;
        
        var result = (VRChatUser)resultN;
        if (string.IsNullOrWhiteSpace(result.note))
        {
            return new Note
            {
                status = NoteState.NeverHad,
                text = null
            };
        }

        return new Note
        {
            status = NoteState.Exists,
            text = result.note
        };
    }

    public async Task<List<VRChatNoteFull>> TempGetNotes()
    {
        _api ??= await InitializeAPI();

        var resultN = await _api.ListUserNotes();
        return resultN;
    }

    private Account UserAsAccount(VRChatUser user)
    {
        return new Account
        {
            namedApp = NamedApp.VRChat,
            qualifiedAppName = "vrchat",
            inAppIdentifier = user.id,
            inAppDisplayName = user.displayName,
            liveServerData = user,
            isContact = user.isFriend,
            note = new Note
            {
                status = string.IsNullOrWhiteSpace(user.note) ? NoteState.NeverHad : NoteState.Exists,
                text = user.note
            }
        };
    }

    private async Task<VRChatAPI> InitializeAPI()
    {
        var api = new VRChatAPI();
        if (File.Exists(CookieFileName))
        {
            var userinput_cookies__sensitive = await File.ReadAllTextAsync(CookieFileName, Encoding.UTF8);
            api.ProvideCookies(userinput_cookies__sensitive);
        }

        if (!api.IsLoggedIn)
        {
            await TryLogin(api);
        }

        var authUser = await api.GetAuthUser();
        _caller = authUser.id;

        return api;
    }

    private async Task TryLogin(VRChatAPI api)
    {
        if (_twoFactor__sensitive != null)
        {
            var result = await api.VerifyTwofer(_twoFactor__sensitive, TwoferMethod.Email); // FIXME: Only email 2FA is supported here
            if (result.Status == LoginResponseStatus.Success)
            {
                await SaveCookiesIntoFile(api);
                return; // Success
            }
            else
            {
                Console.Error.WriteLine($"Two factor authentication failed: {result.Status}");
            }
            // Otherwise, login again
        }
        
        var loginResult = await api.Login(_account__sensitive, _password__sensitive);
        if (loginResult.Status == LoginResponseStatus.RequiresTwofer)
        {
            await SaveCookiesIntoFile(api);
            throw new ArgumentException($"Needs two factor authentication, check your {loginResult.TwoferMethod}");
        }
    }

    private async Task SaveCookiesIntoFile(VRChatAPI api)
    {
        await File.WriteAllTextAsync(CookieFileName, api.GetAllCookies__Sensitive(), Encoding.UTF8);
    }
}