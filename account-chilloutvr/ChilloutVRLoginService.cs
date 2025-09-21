using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.ChilloutVR;

public class ChilloutVRLoginService : ILoginService
{
    public async Task<ConnectionAttemptResult> Connect(ICredentialsStorage credentialsStorage, string guid, ConnectionAttempt connectionAttempt)
    {
        var api = new ChilloutVRAPI();
        
        XYVRLogging.WriteLine(this, "Connecting to ChilloutVR...");
        var result = await api.Login(connectionAttempt.login__sensitive, connectionAttempt.password__sensitive);
        XYVRLogging.WriteLine(this, $"The result was {result.Status}");
        
        if (result.Status == CvrLoginResponseStatus.Success)
        {
            var auth__sensitive = result.Auth!;

            var authCredentialsStorage__sensitive = new ChilloutVRAuthStorage
            {
                username = auth__sensitive.data.username,
                accessKey = auth__sensitive.data.accessKey,
                userId = auth__sensitive.data.userId,
            };

            await credentialsStorage.StoreCookieOrToken(JsonConvert.SerializeObject(authCredentialsStorage__sensitive));
            
            return new ConnectionAttemptResult
            {
                guid = guid,
                type = ConnectionAttemptResultType.Success,
                account = new ConnectorAccount
                {
                    namedApp = NamedApp.ChilloutVR,
                    qualifiedAppName = ChilloutVRAuthority.QualifiedAppName,
                    inAppDisplayName = auth__sensitive.data.username,
                    inAppIdentifier = auth__sensitive.data.userId,
                }
            };
        }
        else
        {
            return new ConnectionAttemptResult
            {
                guid = guid,
                type = ConnectionAttemptResultType.Failure
            };
        }
    }

    public Task<ConnectionAttemptResult> Logout(ICredentialsStorage credentialsStorage, string guid)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsLoggedInWithoutRequest(ICredentialsStorage copyOfCredentialsStorage)
    {
        return await copyOfCredentialsStorage.RequireCookieOrToken() != null;
    }
}