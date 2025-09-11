using XYVR.Core;

namespace XYVR.Login;

public interface ILoginService
{
    Task<ConnectionAttemptResult> Connect(ConnectionAttempt connectionAttempt, ICredentialsStorage credentialsStorage, string guid);
    Task<ConnectionAttemptResult> Logout(Connector connector, ICredentialsStorage credentialsStorage);
    // Task<bool> IsLoggedInWithoutRequest(Connector connector, string guid)
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