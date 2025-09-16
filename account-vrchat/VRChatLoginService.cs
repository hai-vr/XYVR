using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatLoginService : ILoginService
{
    public async Task<ConnectionAttemptResult> Connect(ICredentialsStorage credentialsStorage, string guid, ConnectionAttempt connectionAttempt)
    {
        var vrcApi = new VRChatAPI(new DoNotStoreAnythingStorage());
        var userinput_cookies__sensitive = await credentialsStorage.RequireCookieOrToken();
        if (userinput_cookies__sensitive != null) vrcApi.ProvideCookies(userinput_cookies__sensitive);

        LoginResponse result;
        if (connectionAttempt.twoFactorCode__sensitive == null)
        {
            Console.WriteLine("Connecting to VRChat...");
            result = await vrcApi.Login(connectionAttempt.login__sensitive, connectionAttempt.password__sensitive);
            Console.WriteLine($"The result was {result.Status}");
        }
        else
        {
            var twoferMethod = connectionAttempt.isTwoFactorEmail ? TwoferMethod.Email : TwoferMethod.Other;
            Console.WriteLine($"Verifying 2FA for VRChat ({twoferMethod})...");
            result = await vrcApi.VerifyTwofer(connectionAttempt.twoFactorCode__sensitive, twoferMethod);
            Console.WriteLine($"The result was {result.Status}");
        }

        if (result.Status == LoginResponseStatus.Success)
        {
            await credentialsStorage.StoreCookieOrToken(vrcApi.GetAllCookies__Sensitive());

            var callerAccount = await new VRChatCommunicator(new DoNotStoreAnythingStorage(), credentialsStorage).CallerAccount();
            var connectorAccount = ILoginService.AsConnectorAccount(callerAccount);

            return new ConnectionAttemptResult
            {
                guid = guid,
                type = ConnectionAttemptResultType.Success,
                account = connectorAccount
            };
        }

        if (result.Status == LoginResponseStatus.RequiresTwofer)
        {
            await credentialsStorage.StoreCookieOrToken(vrcApi.GetAllCookies__Sensitive());

            return new ConnectionAttemptResult
            {
                guid = guid,
                type = ConnectionAttemptResultType.NeedsTwoFactorCode,
                isTwoFactorEmail = result.TwoferMethod == TwoferMethod.Email
            };
        }

        return new ConnectionAttemptResult
        {
            guid = guid,
            type = ConnectionAttemptResultType.Failure
        };
    }

    public async Task<ConnectionAttemptResult> Logout(ICredentialsStorage credentialsStorage, string guid)
    {
        var vrcApi = new VRChatAPI(new DoNotStoreAnythingStorage());
        var userinput_cookies__sensitive = await credentialsStorage.RequireCookieOrToken();
        if (userinput_cookies__sensitive != null)
        {
            vrcApi.ProvideCookies(userinput_cookies__sensitive);
        }
        
        var logoutResponse = await vrcApi.Logout();
        switch (logoutResponse)
        {
            case LogoutResponseStatus.Unresolved:
            case LogoutResponseStatus.OutsideProtocol:
                return new ConnectionAttemptResult { guid = guid, type = ConnectionAttemptResultType.Failure };
            case LogoutResponseStatus.Success:
            case LogoutResponseStatus.Unauthorized:
            case LogoutResponseStatus.NotLoggedIn:
                return new ConnectionAttemptResult { guid = guid, type = ConnectionAttemptResultType.LoggedOut };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public async Task<bool> IsLoggedInWithoutRequest(ICredentialsStorage copyOfCredentialsStorage)
    {
        var communicator = new VRChatCommunicator(new DoNotStoreAnythingStorage(), copyOfCredentialsStorage);
        return await communicator.SoftIsLoggedIn();
    }
}