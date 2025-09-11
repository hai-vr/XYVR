using System.Collections.Concurrent;
using XYVR.Core;
using XYVR.Login;

namespace XYVR.Data.Collection;

public class CredentialsManagement
{
    private readonly ConcurrentDictionary<string, InMemoryCredentialsStorage> _connectorGuidToCredentialsStorageState = new();
    private readonly ConcurrentDictionary<string, bool> _isPersistent = new();
    
    private readonly Dictionary<ConnectorType, IAuthority> _authorities = new();

    public CredentialsManagement(SerializedCredentials serializedCredentials, List<IAuthority> inputAuthorities)
    {
        if (serializedCredentials.hasAnything)
        {
            foreach (var keyValuePair in serializedCredentials.guidToPayload)
            {
                _connectorGuidToCredentialsStorageState[keyValuePair.Key] = new InMemoryCredentialsStorage(keyValuePair.Value);
                _isPersistent[keyValuePair.Key] = true;
            }
        }

        foreach (var authority in inputAuthorities)
        {
            _authorities[authority.GetConnectorType()] = authority;
        }
    }

    private IAuthority AuthorityFor(Connector connector)
    {
        if (_authorities.TryGetValue(connector.type, out var authority))
        {
            return authority;
        }

        throw new ArgumentOutOfRangeException($"Unknown connector type: {connector.type}");
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

    public async Task<IDataCollection?> GetConnectedDataCollectionOrNull(Connector connector, IndividualRepository repository, IResponseCollector storage)
    {
        if (!_connectorGuidToCredentialsStorageState.TryGetValue(connector.guid, out var credentialsStorage)) return null;

        return await AuthorityFor(connector).NewDataCollection(repository, credentialsStorage, storage);
    }

    public async Task<bool> IsLoggedInWithoutRequest(Connector connector)
    {
        if (!_connectorGuidToCredentialsStorageState.TryGetValue(connector.guid, out var credentialsStorage)) return false;
        
        var cookieOrToken = await credentialsStorage.RequireCookieOrToken();
        if (cookieOrToken == null) return false;
        
        var copyOfCredentialsStorage = new InMemoryCredentialsStorage(cookieOrToken);
        
        var loginService = await AuthorityFor(connector).NewLoginService();
        return await loginService.IsLoggedInWithoutRequest(copyOfCredentialsStorage);
    }

    public async Task<ConnectionAttemptResult> TryConnect(Connector connector, ConnectionAttempt connectionAttempt)
    {
        var guid = connectionAttempt.connector.guid;
        var credentialsStorage = _connectorGuidToCredentialsStorageState.GetOrAdd(guid, _ => new InMemoryCredentialsStorage(null));
        
        var loginService = await AuthorityFor(connector).NewLoginService();
        var result = await loginService.Connect(credentialsStorage, guid, connectionAttempt);
        
        if (result.type == ConnectionAttemptResultType.Success && connectionAttempt.stayLoggedIn)
        {
            _isPersistent[connectionAttempt.connector.guid] = true;
        }
        
        return result;
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
        
        var loginService = await AuthorityFor(connector).NewLoginService();
        return await loginService.Logout(credentialsStorage, connector.guid);
    }

    public async Task<ILiveMonitoring?> GetConnectedLiveMonitoringOrNull(Connector connector, LiveStatusMonitoring monitoring)
    {
        if (!_connectorGuidToCredentialsStorageState.TryGetValue(connector.guid, out var credentialsStorage)) return null;

        if (connector.type == ConnectorType.Offline) return null;
        
        var authority = AuthorityFor(connector);
        var liveMonitoring = await authority.NewLiveMonitoring(monitoring, credentialsStorage);
        
        var caller = await authority.ResolveCallerAccount(credentialsStorage);
        await liveMonitoring.DefineCaller(caller.inAppIdentifier);
        
        return liveMonitoring;
    }
}