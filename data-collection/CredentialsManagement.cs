using System.Collections.Concurrent;
using XYVR.AccountAuthority.VRChat;
using XYVR.API.VRChat;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.Data.Collection;

public class CredentialsManagement
{
    private readonly ConcurrentDictionary<string, InMemoryCredentialsStorage> _connectorGuidToCredentialsStorageState = new();

    public async Task<bool> IsLoggedIn(Connector connector)
    {
        if (!_connectorGuidToCredentialsStorageState.TryGetValue(connector.guid, out var credentialsStorage)) return false;
        
        var cookieOrToken = await credentialsStorage.RequireCookieOrToken();
        if (cookieOrToken == null) return false;
        
        return connector.type switch
        {
            ConnectorType.VRChatAPI => await VrcCheckIsLoggedIn(cookieOrToken),
            ConnectorType.ResoniteAPI => await ResoniteCheckIsLoggedIn(cookieOrToken),
            ConnectorType.Offline => throw new ArgumentException("Cannot connect to offline connector"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<bool> VrcCheckIsLoggedIn(string? requireCookieOrToken)
    {
        var communicator = new VRChatCommunicator(
            new DoNotStoreAnythingStorage(),
            null, null, null,
            new InMemoryCredentialsStorage(requireCookieOrToken)
        );
        
        return await communicator.SoftIsLoggedIn();
    }

    private async Task<bool> ResoniteCheckIsLoggedIn(string cookieOrToken)
    {
        // TODO: implement resonite
        await Task.CompletedTask;
        
        return false;
    }

    public async Task<ConnectionAttemptResult> TryConnect(Connector connector, ConnectionAttempt connectionAttempt)
    {
        return connector.type switch
        {
            ConnectorType.VRChatAPI => await ConnectToVRChat(connectionAttempt),
            ConnectorType.ResoniteAPI => await ConnectToResonite(connectionAttempt),
            ConnectorType.Offline => throw new ArgumentException("Cannot connect to offline connector"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<ConnectionAttemptResult> ConnectToVRChat(ConnectionAttempt connectionAttempt)
    {
        var guid = connectionAttempt.connector.guid;
        
        var credentialsStorage = _connectorGuidToCredentialsStorageState.GetOrAdd(guid, _ => new InMemoryCredentialsStorage(null));
        
        var communicator = new VRChatCommunicator(
            new DoNotStoreAnythingStorage(),
            connectionAttempt.login__sensitive,
            connectionAttempt.password__sensitive,
            connectionAttempt.twoFactorCode__sensitive,
            credentialsStorage
        );
        
        Console.WriteLine("Connecting to VRChat...");
        LoginResponseStatus result;
        if (connectionAttempt.twoFactorCode__sensitive == null)
        {
            result = await communicator.VrcLoginUsingUsernameAndPassword();
        }
        else
        {
            result = await communicator.VrcLoginContinuationUsingTwoFactor();
        }
        
        Console.WriteLine($"The result was {result}");
        
        if (result == LoginResponseStatus.Success)
        {
            if (connectionAttempt.stayLoggedIn)
            {
                // TODO: Store the InMemoryCredentialsStorage token to a persistent storage
                // ????????????????????????
            }
            // _connectorGuidToCredentialsStorageState.Remove(guid, out _);

            var callerAccount = await communicator.CallerAccount();
            var connectorAccount = AsConnectorAccount(callerAccount);

            return new ConnectionAttemptResult
            {
                guid = guid,
                type = ConnectionAttemptResultType.Success,
                account = connectorAccount
            };
        }

        return result switch
        {
            LoginResponseStatus.Success => new ConnectionAttemptResult { guid = guid, type = ConnectionAttemptResultType.Success },
            LoginResponseStatus.RequiresTwofer => new ConnectionAttemptResult { guid = guid, type = ConnectionAttemptResultType.NeedsTwoFactorCode },
            _ => new ConnectionAttemptResult { guid = guid, type = ConnectionAttemptResultType.Failure }
        };
    }

    private async Task<ConnectionAttemptResult> ConnectToResonite(ConnectionAttempt connectionAttempt)
    {
        var guid = connectionAttempt.connector.guid;
        
        await Task.CompletedTask;
        
        // TODO: implement resonite
        return new ConnectionAttemptResult { guid = guid, type = ConnectionAttemptResultType.Failure };
    }
    
    public async Task<ConnectionAttemptResult> TryLogout(Connector connector)
    {
        if (!_connectorGuidToCredentialsStorageState.TryGetValue(connector.guid, out var credentialsStorage))
        {
            return new ConnectionAttemptResult { guid = connector.guid, type = ConnectionAttemptResultType.LoggedOut };
        }
        
        var cookieOrToken = await credentialsStorage.RequireCookieOrToken();
        if (cookieOrToken == null)
        {
            return new ConnectionAttemptResult { guid = connector.guid, type = ConnectionAttemptResultType.LoggedOut };
        }

        return connector.type switch
        {
            ConnectorType.VRChatAPI => await VrcLogout(connector, credentialsStorage),
            ConnectorType.ResoniteAPI => await ResoniteLogout(connector, credentialsStorage),
            ConnectorType.Offline => throw new ArgumentException("Cannot connect to offline connector"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<ConnectionAttemptResult> VrcLogout(Connector connector, InMemoryCredentialsStorage credentialsStorage)
    {
        var communicator = new VRChatCommunicator(
            new DoNotStoreAnythingStorage(),
            null, null, null,
            credentialsStorage
        );
        var logoutResponse = await communicator.Logout();
        switch (logoutResponse)
        {
            case LogoutResponseStatus.Unresolved:
            case LogoutResponseStatus.OutsideProtocol:
                return new ConnectionAttemptResult { guid = connector.guid, type = ConnectionAttemptResultType.Failure };
            case LogoutResponseStatus.Success:
            case LogoutResponseStatus.Unauthorized:
            case LogoutResponseStatus.NotLoggedIn:
                _connectorGuidToCredentialsStorageState.Remove(connector.guid, out _);
                return new ConnectionAttemptResult { guid = connector.guid, type = ConnectionAttemptResultType.LoggedOut };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task<ConnectionAttemptResult> ResoniteLogout(Connector connector, InMemoryCredentialsStorage credentialsStorage)
    {
        // TODO: implement resonite
        throw new NotImplementedException();
    }

    private static ConnectorAccount AsConnectorAccount(Account callerAccount)
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