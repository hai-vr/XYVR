using System.Collections.Concurrent;
using XYVR.AccountAuthority.Resonite;
using XYVR.AccountAuthority.VRChat;
using XYVR.Core;
using XYVR.Login;
using XYVR.Scaffold;

namespace XYVR.Data.Collection;

public class CredentialsManagement
{
    private readonly Func<Task<string>> _resoniteUidProviderFn;
    private readonly WorldNameCache _worldNameCache;
    private readonly ConcurrentDictionary<string, InMemoryCredentialsStorage> _connectorGuidToCredentialsStorageState = new();
    private readonly ConcurrentDictionary<string, bool> _isPersistent = new();
    
    private readonly ILoginService _vrcLoginService;
    private readonly ResoniteLoginService _resoniteLoginService;

    public CredentialsManagement(SerializedCredentials serializedCredentials, Func<Task<string>> resoniteUidProviderFn, WorldNameCache worldNameCache)
    {
        // We need to call this as late as possible so that UID doesn't generate for users who never use Resonite.
        _resoniteUidProviderFn = resoniteUidProviderFn;
        _worldNameCache = worldNameCache;

        if (serializedCredentials.hasAnything)
        {
            foreach (var keyValuePair in serializedCredentials.guidToPayload)
            {
                _connectorGuidToCredentialsStorageState[keyValuePair.Key] = new InMemoryCredentialsStorage(keyValuePair.Value);
                _isPersistent[keyValuePair.Key] = true;
            }
        }

        _vrcLoginService = new VRChatLoginService();
        _resoniteLoginService = new ResoniteLoginService(_resoniteUidProviderFn);
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

    public async Task<bool> IsLoggedInWithoutRequest(Connector connector)
    {
        if (!_connectorGuidToCredentialsStorageState.TryGetValue(connector.guid, out var credentialsStorage)) return false;
        
        var cookieOrToken = await credentialsStorage.RequireCookieOrToken();
        if (cookieOrToken == null) return false;
        
        return connector.type switch
        {
            ConnectorType.VRChatAPI => await SOFT_VrcCheckIsLoggedIn(cookieOrToken),
            ConnectorType.ResoniteAPI => await ResoniteCheckIsLoggedIn(cookieOrToken),
            ConnectorType.Offline => throw new ArgumentException("Cannot connect to offline connector"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async Task<bool> SOFT_VrcCheckIsLoggedIn(string cookieOrToken)
    {
        // FIXME: I don't remember why we are copying this. Maybe it's so that the communicator doesn't try to delete the cookie from the storage or something
        // (there was an undesired side effect, maybe because it was overwriting the cookie?)
        var copyOfCredentialsStorage = new InMemoryCredentialsStorage(cookieOrToken);
        
        return await _vrcLoginService.IsLoggedInWithoutRequest(copyOfCredentialsStorage);
    }

    private async Task<bool> ResoniteCheckIsLoggedIn(string cookieOrToken)
    {
        return await _resoniteLoginService.IsLoggedInWithoutRequest(new InMemoryCredentialsStorage(cookieOrToken));
    }

    public async Task<ConnectionAttemptResult> TryConnect(Connector connector, ConnectionAttempt connectionAttempt)
    {
        var result = connector.type switch
        {
            ConnectorType.VRChatAPI => await ConnectToVRChat(connectionAttempt),
            ConnectorType.ResoniteAPI => await ConnectToResonite(connectionAttempt),
            ConnectorType.Offline => throw new ArgumentException("Cannot connect to offline connector"),
            _ => throw new ArgumentOutOfRangeException()
        };
        if (result.type == ConnectionAttemptResultType.Success && connectionAttempt.stayLoggedIn)
        {
            _isPersistent[connectionAttempt.connector.guid] = true;
        }
        return result;
    }

    private async Task<ConnectionAttemptResult> ConnectToVRChat(ConnectionAttempt connectionAttempt)
    {
        var guid = connectionAttempt.connector.guid;
        var credentialsStorage = _connectorGuidToCredentialsStorageState.GetOrAdd(guid, _ => new InMemoryCredentialsStorage(null));
        return await _vrcLoginService.Connect(credentialsStorage, guid, connectionAttempt);
    }

    private async Task<ConnectionAttemptResult> ConnectToResonite(ConnectionAttempt connectionAttempt)
    {
        var guid = connectionAttempt.connector.guid;
        var credentialsStorage = _connectorGuidToCredentialsStorageState.GetOrAdd(guid, _ => new InMemoryCredentialsStorage(null));
        
        return await _resoniteLoginService.Connect(credentialsStorage, guid, connectionAttempt);
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

        var result = connector.type switch
        {
            ConnectorType.VRChatAPI => await VrcLogout(connector, credentialsStorage),
            ConnectorType.ResoniteAPI => await ResoniteLogout(connector, credentialsStorage),
            ConnectorType.Offline => throw new ArgumentException("Cannot connect to offline connector"),
            _ => throw new ArgumentOutOfRangeException()
        };
        if (result.type == ConnectionAttemptResultType.LoggedOut)
        {
            _connectorGuidToCredentialsStorageState.Remove(connector.guid, out _);
        }
        
        return result;
    }

    private async Task<ConnectionAttemptResult> VrcLogout(Connector connector, InMemoryCredentialsStorage credentialsStorage)
    {
        return await _vrcLoginService.Logout(credentialsStorage, connector.guid);
    }

    private async Task<ConnectionAttemptResult> ResoniteLogout(Connector connector, InMemoryCredentialsStorage credentialsStorage)
    {
        return await _resoniteLoginService.Logout(credentialsStorage, connector.guid);
    }

    public async Task<ILiveMonitoring?> GetConnectedLiveMonitoringOrNull(Connector connector, LiveStatusMonitoring monitoring)
    {
        if (!_connectorGuidToCredentialsStorageState.TryGetValue(connector.guid, out var credentialsStorage)) return null;
        
        switch (connector.type)
        {
            case ConnectorType.Offline:
                return null;
            case ConnectorType.ResoniteAPI:
            {
                ILiveMonitoring liveMonitoring = new ResoniteLiveMonitoring(credentialsStorage, monitoring, await _resoniteUidProviderFn());
                var res = new ResoniteCommunicator(
                    new DoNotStoreAnythingStorage(), false, await _resoniteUidProviderFn(),
                    credentialsStorage
                );
                var caller = await res.CallerAccount();

                await liveMonitoring.DefineCaller(caller.inAppIdentifier);
                
                return liveMonitoring;
            }
            case ConnectorType.VRChatAPI:
            {
                ILiveMonitoring liveMonitoring = new VRChatLiveMonitoring(credentialsStorage, monitoring, _worldNameCache);
                var res = new VRChatCommunicator(new DoNotStoreAnythingStorage(), credentialsStorage);
                var caller = await res.CallerAccount();

                await liveMonitoring.DefineCaller(caller.inAppIdentifier);
                
                return liveMonitoring;
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}