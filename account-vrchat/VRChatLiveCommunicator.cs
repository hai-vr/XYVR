using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.API.VRChat;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.AccountAuthority.VRChat;

internal class VRChatLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;
    private readonly IResponseCollector _responseCollector;
    private readonly WorldNameCache _worldNameCache;

    private readonly Lock _queueLock = new();
    private readonly HashSet<string> _allQueued = new();
    private readonly Queue<string> _queue = new();
    private Task _queueTask = Task.CompletedTask;

    private VRChatWebsocketClient _wsClient;
    private VRChatAPI? _api;
    private bool _hasInitiatedDisconnect;

    public event LiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task LiveUpdateReceived(LiveUserUpdate liveUpdate);

    public event WorldResolved? OnWorldCached;
    public delegate Task WorldResolved(CachedWorld world);

    public VRChatLiveCommunicator(ICredentialsStorage credentialsStorage, string callerInAppIdentifier, IResponseCollector responseCollector, WorldNameCache worldNameCache)
    {
        _credentialsStorage = credentialsStorage;
        _callerInAppIdentifier = callerInAppIdentifier;
        _responseCollector = responseCollector;
        _worldNameCache = worldNameCache;
    }

    private void WakeUpQueue()
    {
        lock (_queueLock)
        {
            if (_queueTask.IsCompleted)
            {
                _queueTask = Task.Run(ProcessQueue);
            }
        }
    }

    private async Task ProcessQueue()
    {
        Console.WriteLine("Processing queue");
        _api ??= await InitializeAPI();
        
        while (_queue.Count > 0)
        {
            var worldId = _queue.Dequeue();
            var worldLenient = await _api.GetWorldLenient(DataCollectionReason.CollectSessionLocationInformation, worldId);
            if (worldLenient != null)
            {
                var cache = new CachedWorld
                {
                    cachedAt = DateTime.Now,
                    name = worldLenient.name,
                    worldId = worldId,
                    author = worldLenient.authorId,
                    authorName = worldLenient.authorName,
                    releaseStatus = worldLenient.releaseStatus,
                    description = worldLenient.description,
                    thumbnailUrl = worldLenient.thumbnailImageUrl,
                };
                _worldNameCache.VRCWorlds[worldId] = cache;

                if (OnWorldCached != null)
                {
                    await OnWorldCached.Invoke(cache);
                }
            }
        }
    }

    public async Task Connect()
    {
        _hasInitiatedDisconnect = false;
        
        if (_wsClient != null)
        {
            _wsClient.Disconnected -= WhenDisconnected;
            try { await _wsClient.Disconnect(); }
            catch { // ignored
            }

            try { _wsClient.Dispose(); }
            catch { // ignored
            }
            _wsClient = null;
        }
        
        _wsClient = new VRChatWebsocketClient();
        _wsClient.Connected += WhenConnected;
        _wsClient.MessageReceived += WhenMessageReceived;
        _wsClient.Disconnected += WhenDisconnected;
        
        _api ??= await InitializeAPI();

        var contactsAsyncEnum = _api.ListFriends(ListFriendsRequestType.OnlyOnline, DataCollectionReason.FindUndiscoveredAccounts);
        await foreach (var friend in contactsAsyncEnum)
        {
            if (OnLiveUpdateReceived != null)
            {
                try
                {
                    var worldId = LocationAsWorldIdOrNull(friend.location);
                    CachedWorld? cachedWorld = worldId != null ? GetOrQueueWorldFetch(worldId) : null;
                    
                    var session = friend.location != "private" && friend.location != "offline" && friend.location != "traveling" ? new LiveUserKnownSession
                    {
                        inAppSessionIdentifier = friend.location,
                        inAppHost = null,
                        inAppSessionName = null,
                        inAppVirtualSpaceName = cachedWorld?.name,
                        isJoinable = null
                    } : null;
                    
                    await OnLiveUpdateReceived(new LiveUserUpdate
                    {
                        trigger = "API-ListFriends",
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppIdentifier = friend.id,
                        onlineStatus = ParseStatus("xxx", friend.location, friend.platform, friend.status),
                        callerInAppIdentifier = _callerInAppIdentifier,
                        customStatus = friend.statusDescription,
                        mainSession = new LiveUserSessionState
                        {
                            knowledge = friend.location == "private" ? LiveUserSessionKnowledge.PrivateWorld
                                : (session != null ? LiveUserSessionKnowledge.Known : LiveUserSessionKnowledge.KnownButNoData),
                            knownSession = session
                        }
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
        
        await _wsClient.Connect(await GetToken__sensitive());
    }

    public async Task Disconnect()
    {
        _hasInitiatedDisconnect = true;
        await _wsClient.Disconnect();
    }

    private void WhenMessageReceived(string msg)
    {
        if (OnLiveUpdateReceived == null) return;
        
        try
        {
            var rootObj = JObject.Parse(msg);
            var type = rootObj["type"].Value<string>();
        
            if (type is "friend-online" or "friend-update" or "friend-offline" or "friend-location" or "friend-active" or "user-location")
            {
                // FIXME: We are ignoring user-update for now, it causes issues where the user is considered to be back online even though they are just on the website
                // despite the `onlineStatus = type is "user-update" ? null : ...` below
                
                var content = JsonConvert.DeserializeObject<VRChatWebsocketContentContainingUser>(rootObj["content"].Value<string>())!;

                // FIXME: This is a task???
                OnlineStatus? onlineStatus = type is "user-update" ? null : ParseStatus(type, content.location, content.user.platform, content.user.status);
                OnLiveUpdateReceived(new LiveUserUpdate
                {
                    trigger = $"WS-{type}",
                    namedApp = NamedApp.VRChat,
                    qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                    inAppIdentifier = content.userId,
                    onlineStatus = onlineStatus,
                    callerInAppIdentifier = _callerInAppIdentifier,
                    customStatus = content.user.statusDescription,
                    mainSession = FigureOutSessionStateOrNull(type, onlineStatus, content)
                });
            }
            else
            {
                Console.WriteLine($"Received UNHANDLED message of type {type} from vrc ws api");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private LiveUserSessionState? FigureOutSessionStateOrNull(string type, OnlineStatus? onlineStatus, VRChatWebsocketContentContainingUser content)
    {
        // Order matters. This checks acts on non-friend-location types.
        if (onlineStatus == OnlineStatus.Offline)
        {
            return new LiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.KnownButNoData,
                knownSession = null
            };
        }
        else if (type == "friend-location")
        {
            // Order matters. "private" check comes before checking for location.
            if (content.worldId == "private" || content.location == "private")
            {
                return new LiveUserSessionState
                {
                    knowledge = LiveUserSessionKnowledge.PrivateWorld,
                    knownSession = null
                };
            }
            else
            {
                var location = content.location;
                if (location == "traveling")
                {
                    return new LiveUserSessionState
                    {
                        knowledge = LiveUserSessionKnowledge.VRCTraveling,
                        knownSession = new LiveUserKnownSession
                        {
                            inAppSessionIdentifier = content.travelingToLocation,
                            inAppHost = null,
                            inAppSessionName = null,
                            inAppVirtualSpaceName = null,
                            isJoinable = false
                        }
                    };
                }
                else
                {
                    var worldId = LocationAsWorldIdOrNull(location);
                    if (worldId != null)
                    {
                        CachedWorld? cachedWorld = GetOrQueueWorldFetch(worldId);
                        return new LiveUserSessionState
                        {
                            knowledge = LiveUserSessionKnowledge.Known,
                            knownSession = new LiveUserKnownSession
                            {
                                inAppSessionIdentifier = location,
                                inAppHost = null,
                                inAppSessionName = null,
                                inAppVirtualSpaceName = cachedWorld?.name,
                                isJoinable = true // FIXME
                            }
                        };
                    }
                    else
                    {
                        return new LiveUserSessionState
                        {
                            knowledge = LiveUserSessionKnowledge.Known,
                            knownSession = new LiveUserKnownSession
                            {
                                inAppSessionIdentifier = location,
                                inAppHost = null,
                                inAppSessionName = null,
                                inAppVirtualSpaceName = null,
                                isJoinable = null
                            }
                        };
                    }
                }
            }
        }
        else
        {
            return null;
        }
    }

    public string? LocationAsWorldIdOrNull(string location)
    {
        if (location.StartsWith("wrld_"))
        {
            var separator = location.IndexOf(':');
            if (separator != -1)
            {
                var worldId = location.Substring(0, separator);
                return worldId;
            }
            else
            {
                Console.WriteLine($"Location is not parseable as a world: {location}");
            }
        }
        else
        {
            Console.WriteLine($"Unknown location: {location}");
        }

        return null;
    }

    private CachedWorld? GetOrQueueWorldFetch(string worldId)
    {
        var cachedWorldNullable = _worldNameCache.GetValidOrNull(worldId);

        var shouldAttemptQueueing = cachedWorldNullable == null || cachedWorldNullable.needsRefresh;
        if (shouldAttemptQueueing && !_allQueued.Contains(worldId))
        {
            if (cachedWorldNullable != null) Console.WriteLine($"We don't know world id {worldId}, will queue fetch...");
            else Console.WriteLine($"We need to refresh our knowledge of world id {worldId}, will queue fetch...");

            _allQueued.Add(worldId);
            _queue.Enqueue(worldId);
            WakeUpQueue();
        }
            
        return cachedWorldNullable;
    }

    private OnlineStatus ParseStatus(string type, string contentLocation, string platform, string userStatus)
    {
        if (contentLocation == "offline:offline") return OnlineStatus.Offline;
        
        if (type == "friend-active") return OnlineStatus.Offline;
        if (type == "friend-offline") return OnlineStatus.Offline;
        
        if (platform == "web") return OnlineStatus.Offline;
        
        return userStatus switch
        {
            "offline" => OnlineStatus.Offline,
            "active" => OnlineStatus.Online,
            "busy" => OnlineStatus.VRChatDND,
            "ask me" => OnlineStatus.VRChatAskMe,
            "join me" => OnlineStatus.VRChatJoinMe,
            _ => OnlineStatus.Indeterminate
        };
    }

    private void WhenConnected()
    {
    }

    private void WhenDisconnected(string reason)
    {
        Console.WriteLine($"We got disconnected from the vrc ws api. Reason: {reason}");
        if (!_hasInitiatedDisconnect)
        {
            Console.WriteLine("Will try reconnecting.");
            Task.Run(async () =>
            {
                await Connect();
            }).Wait();
        }
    }

    private async Task<string> GetToken__sensitive()
    {
        return JsonConvert.DeserializeObject<VRChatAPI.VrcAuthenticationCookies>(await _credentialsStorage.RequireCookieOrToken())
            .auth.Value;
    }
    
    private async Task<VRChatAPI> InitializeAPI()
    {
        var api = new VRChatAPI(_responseCollector);
        var userinput_cookies__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (userinput_cookies__sensitive != null)
        {
            api.ProvideCookies(userinput_cookies__sensitive);
        }

        if (!api.IsLoggedIn)
        {
            throw new ArgumentException("User must be already logged in before establishing communication");
        }

        return api;
    }
}