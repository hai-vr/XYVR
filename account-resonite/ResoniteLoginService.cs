﻿using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteLoginService : ILoginService
{
    private readonly Func<Task<string>> _resoniteUidProviderFn;

    public ResoniteLoginService(Func<Task<string>> resoniteUidProviderFn)
    {
        _resoniteUidProviderFn = resoniteUidProviderFn;
    }

    public async Task<ConnectionAttemptResult> Connect(ICredentialsStorage credentialsStorage, string guid, ConnectionAttempt connectionAttempt)
    {
        if (connectionAttempt.login__sensitive == null || connectionAttempt.password__sensitive == null)
        {
            throw new InvalidOperationException("Login and password are mandatory");
        }
        
        var communicator = new ResoniteCommunicator(
            new DoNotStoreAnythingStorage(),
            connectionAttempt.stayLoggedIn,
            await _resoniteUidProviderFn(),
            credentialsStorage
        );
        
        XYVRLogging.WriteLine(this, "Connecting to Resonite...");
        try
        {
            await communicator.ResoniteLogin(connectionAttempt.login__sensitive, connectionAttempt.password__sensitive);
            
            var callerAccount = await communicator.CallerAccount();
            var connectorAccount = ILoginService.AsConnectorAccount(callerAccount);
            
            return new ConnectionAttemptResult
            {
                guid = guid,
                type = ConnectionAttemptResultType.Success,
                account = connectorAccount
            };
        }
        catch (Exception _)
        {
            return new ConnectionAttemptResult { guid = guid, type = ConnectionAttemptResultType.Failure };
        }
    }

    public async Task<ConnectionAttemptResult> Logout(ICredentialsStorage credentialsStorage, string guid)
    {
        var communicator = new ResoniteCommunicator(
            new DoNotStoreAnythingStorage(),
            true,
            await _resoniteUidProviderFn(),
            credentialsStorage
        );

        await communicator.ResoniteLogout();
        
        return new ConnectionAttemptResult
        {
            guid = guid,
            type = ConnectionAttemptResultType.LoggedOut
        };
    }

    public async Task<bool> IsLoggedInWithoutRequest(ICredentialsStorage copyOfCredentialsStorage)
    {
        // TODO: implement resonite
        // communicator.isloggedin?????????????????????????
        await Task.CompletedTask;

        // FIXME: this is completely wrong
        return true;
    }
}