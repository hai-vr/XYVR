using Newtonsoft.Json;
using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.Cluster;

public class ClusterLoginService : ILoginService
{
    public async Task<ConnectionAttemptResult> Connect(ICredentialsStorage credentialsStorage, string guid, ConnectionAttempt connectionAttempt)
    {
        try
        {
            var authStorage = new ClusterAuthStorage
            {
                bearer = connectionAttempt.password__sensitive,
                build = "",
                version = ""
            };
            
            var api = new ClusterAPI(new DoNotStoreAnythingStorage(), new CancellationTokenSource());
            api.Provide(authStorage);
        
            XYVRLogging.WriteLine(this, "Connecting to cluster.mu...");
            var caller = await api.GetCallerAccount(DataCollectionReason.ManualRequest);
            XYVRLogging.WriteLine(this, $"Got a good result");

            var authCredentialsStorage__sensitive = authStorage;

            await credentialsStorage.StoreCookieOrToken(JsonConvert.SerializeObject(authCredentialsStorage__sensitive));

            return new ConnectionAttemptResult
            {
                account = new ConnectorAccount
                {
                    namedApp = NamedApp.Cluster,
                    qualifiedAppName = ClusterAuthority.QualifiedAppName,
                    inAppIdentifier = caller.inAppIdentifier,
                    inAppDisplayName = caller.inAppDisplayName,
                },
                guid = guid,
                type = ConnectionAttemptResultType.Success,
                isTwoFactorEmail = false
            };
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            return new ConnectionAttemptResult
            {
                guid = guid,
                type = ConnectionAttemptResultType.Failure
            };
        }
    }

    public async Task<ConnectionAttemptResult> Logout(ICredentialsStorage credentialsStorage, string guid)
    {
        XYVRLogging.WriteLine(this, "Logging out of Cluster is not implemented. The session will not be invalidated.");
        
        await credentialsStorage.DeleteCookieOrToken();
        
        return new ConnectionAttemptResult
        {
            guid = guid,
            type = ConnectionAttemptResultType.LoggedOut,
        };
    }

    public async Task<bool> IsLoggedInWithoutRequest(ICredentialsStorage copyOfCredentialsStorage)
    {
        return await copyOfCredentialsStorage.RequireCookieOrToken() != null;
    }
}