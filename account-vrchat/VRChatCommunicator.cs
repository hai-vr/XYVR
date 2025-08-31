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

    public VRChatCommunicator()
    {
        _account__sensitive = Environment.GetEnvironmentVariable(XYVREnvVar.VRChatAccount)!;
        _password__sensitive = Environment.GetEnvironmentVariable(XYVREnvVar.VRChatPassword)!;
        if (_account__sensitive == null || _password__sensitive == null) throw new ArgumentException("Missing environment variables");
        
        _twoFactor__sensitive = Environment.GetEnvironmentVariable(XYVREnvVar.VRChatTwoFactorCode);
    }
    
    public async Task<List<Account>> FindUndiscoveredAccounts(IndividualRepository individualRepository)
    {
        var vrchatAccountIdentifiers = individualRepository.CollectAllInAppIdentifiers(NamedApp.VRChat);
        
        var api = await InitializeAPI();

        var onlineFriends = await api.ListFriends(ListFriendsRequestType.OnlyOnline);
        var offlineFriends = await api.ListFriends(ListFriendsRequestType.OnlyOffline);
        
        return onlineFriends.Concat(offlineFriends)
            .Where(friend => !vrchatAccountIdentifiers.Contains(friend.id))
            .Select(AsAccount)
            .ToList();
    }

    public async Task<List<Account>> CollectUndiscoveredLenient(IndividualRepository repository, List<string> notNecessarilyValidUserIds)
    {
        var vrchatAccountIdentifiers = repository.CollectAllInAppIdentifiers(NamedApp.VRChat);
        var undiscoveredAndNotNecessarilyValidUserIds = notNecessarilyValidUserIds
            .Where(userId => !vrchatAccountIdentifiers.Contains(userId))
            .Distinct() // Get rid of duplicates
            .ToList();
        
        var api = await InitializeAPI();

        var accounts = new List<Account>();
        foreach (var userId in undiscoveredAndNotNecessarilyValidUserIds)
        {
            var user = await api.GetUserLenient(userId);
            if (user != null)
            {
                accounts.Add(UserAsAccount((VRChatUser)user));
            }
        }

        return accounts;
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

        return api;
    }

    private Account AsAccount(VRChatFriend friend)
    {
        return new Account
        {
            namedApp = NamedApp.VRChat,
            qualifiedAppName = "vrchat",
            inAppIdentifier = friend.id,
            inAppDisplayName = friend.displayName,
            liveServerData = friend,
            isContact = friend.isFriend,
        };
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