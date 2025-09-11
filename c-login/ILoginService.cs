using XYVR.Core;

namespace XYVR.Login;

public interface ILoginService
{
    Task<ConnectionAttemptResult> Connect(string guid, ConnectionAttempt connectionAttempt, ICredentialsStorage credentialsStorage);
    Task<ConnectionAttemptResult> Logout(string guid, ICredentialsStorage credentialsStorage);
    Task<bool> IsLoggedInWithoutRequest(ICredentialsStorage copyOfCredentialsStorage);
    // Task<bool> IsLogginInWithRequest(Connector connector, ICredentialsStorage credentialsStorage, string guid);
    
    public static ConnectorAccount AsConnectorAccount(NonIndexedAccount callerAccount)
    {
        return new ConnectorAccount
        {
            namedApp = callerAccount.namedApp,
            qualifiedAppName = callerAccount.qualifiedAppName,
            inAppIdentifier = callerAccount.inAppIdentifier,
            inAppDisplayName = callerAccount.inAppDisplayName,
        };
    }

}