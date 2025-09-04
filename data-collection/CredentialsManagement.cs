using System.Collections.Concurrent;
using XYVR.AccountAuthority.Resonite;
using XYVR.AccountAuthority.VRChat;
using XYVR.API.VRChat;
using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.Data.Collection;

public class CredentialsManagement
{
    private readonly Func<Task<string>> _resoniteUidProviderFn;
    private readonly ConcurrentDictionary<string, InMemoryCredentialsStorage> _connectorGuidToCredentialsStorageState = new();
    private readonly ConcurrentDictionary<string, bool> _isPersistent = new();

    public CredentialsManagement(SerializedCredentials serializedCredentials, Func<Task<string>> resoniteUidProviderFn)
    {
        // We need to call this as late as possible so that UID doesn't generate for users who never use Resonite.
        _resoniteUidProviderFn = resoniteUidProviderFn;

        if (serializedCredentials.hasAnything)
        {
            foreach (var keyValuePair in serializedCredentials.guidToPayload)
            {
                _connectorGuidToCredentialsStorageState[keyValuePair.Key] = new InMemoryCredentialsStorage(keyValuePair.Value);
                _isPersistent[keyValuePair.Key] = true;
            }
        }
    }

    public async Task<SerializedCredentials> SerializeCredentials()
    {
        var toSerialize = new Dictionary<string, string>();
        foreach (var storage in _connectorGuidToCredentialsStorageState)
        {
            if (_isPersistent.TryGetValue(storage.Key, out var isPersistent) && isPersistent)
            {
                var tokenN = await storage.Value.RequireCookieOrToken();
                if (tokenN != null)
                {
                    toSerialize.Add(storage.Key, tokenN);
                }
            }
        }

        var hasAnything = toSerialize.Count > 0;
        return new SerializedCredentials
        {
            hasAnything = hasAnything,
            guidToPayload = hasAnything ? toSerialize : null
        };
    }

    public async Task<IDataCollection?> GetConnectedDataCollectionOrNull(Connector connector, IndividualRepository repository, ResponseCollectionStorage storage)
    {
        if (!_connectorGuidToCredentialsStorageState.TryGetValue(connector.guid, out var credentialsStorage)) return null;

        return connector.type switch
        {
            ConnectorType.Offline => null,
            ConnectorType.ResoniteAPI => new ResoniteDataCollection(repository, storage, await _resoniteUidProviderFn(), credentialsStorage),
            ConnectorType.VRChatAPI => new VRChatDataCollection(repository, storage, credentialsStorage),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

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

    private async Task<bool> VrcCheckIsLoggedIn(string cookieOrToken)
    {
        var communicator = new VRChatCommunicator(
            new DoNotStoreAnythingStorage(),
            new InMemoryCredentialsStorage(cookieOrToken)
        );
        
        // FIXME: This does not handle disconnections
        return await communicator.SoftIsLoggedIn();
    }

    private async Task<bool> ResoniteCheckIsLoggedIn(string cookieOrToken)
    {
        var communicator = new ResoniteCommunicator(
            new DoNotStoreAnythingStorage(),
            null, null, await _resoniteUidProviderFn(),
            new InMemoryCredentialsStorage(cookieOrToken)
        );
        
        // TODO: implement resonite
        // communicator.isloggedin?????????????????????????
        await Task.CompletedTask;

        // FIXME: this is completely wrong
        return true;
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

        var vrcApi = new VRChatAPI(new DoNotStoreAnythingStorage());
        var userinput_cookies__sensitive = await credentialsStorage.RequireCookieOrToken();
        if (userinput_cookies__sensitive != null)
        {
            vrcApi.ProvideCookies(userinput_cookies__sensitive);
        }
        
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
            var connectorAccount = AsConnectorAccount(callerAccount);
                
            if (connectionAttempt.stayLoggedIn)
            {
                _isPersistent[guid] = true;
            }

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

    private async Task<ConnectionAttemptResult> ConnectToResonite(ConnectionAttempt connectionAttempt)
    {
        var guid = connectionAttempt.connector.guid;
        
        var credentialsStorage = _connectorGuidToCredentialsStorageState.GetOrAdd(guid, _ => new InMemoryCredentialsStorage(null));
        
        var communicator = new ResoniteCommunicator(
            new DoNotStoreAnythingStorage(),
            connectionAttempt.login__sensitive,
            connectionAttempt.password__sensitive,
            await _resoniteUidProviderFn(),
            credentialsStorage
        );
        
        Console.WriteLine("Connecting to Resonite...");
        try
        {
            await communicator.ResoniteLogin();
            
            var callerAccount = await communicator.CallerAccount();
            var connectorAccount = AsConnectorAccount(callerAccount);
            
            if (connectionAttempt.stayLoggedIn)
            {
                _isPersistent[guid] = true;
            }
            
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
        _connectorGuidToCredentialsStorageState.Remove(connector.guid, out _);
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