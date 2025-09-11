using XYVR.AccountAuthority.VRChat;
using XYVR.API.VRChat;
using XYVR.Core;
using XYVR.Login;

namespace XYVR.Data.Collection;

public class VRChatLoginService : ILoginService
{
    public async Task<ConnectionAttemptResult> Connect(ConnectionAttempt connectionAttempt, ICredentialsStorage credentialsStorage, string guid)
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
}