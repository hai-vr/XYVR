using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.API.VRChat;
using XYVR.Core;

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

    public event VrcLiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task VrcLiveUpdateReceived(ImmutableLiveUserUpdate liveUserUpdate);

    public event VrcLiveSessionReceived? OnLiveSessionReceived;
    public delegate Task<ImmutableLiveSession> VrcLiveSessionReceived(ImmutableNonIndexedLiveSession liveSession);

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
                    capacity = worldLenient.capacity,
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
        _wsClient.MessageReceived += msg => WhenMessageReceived(msg);
        _wsClient.Disconnected += WhenDisconnected;
        
        _api ??= await InitializeAPI();

        var contactsAsyncEnum = _api.ListFriends(ListFriendsRequestType.OnlyOnline, DataCollectionReason.FindUndiscoveredAccounts);
        await foreach (var friend in contactsAsyncEnum)
        {
            if (OnLiveUpdateReceived != null && OnLiveSessionReceived != null)
            {
                try
                {
                    var worldId = LocationAsWorldIdOrNull(friend.location);
                    CachedWorld? cachedWorld = worldId != null ? GetOrQueueWorldFetch(worldId) : null;

                    LiveUserSessionKnowledge? knowledgePartial = friend.location == "traveling" ? LiveUserSessionKnowledge.VRCTraveling
                        : friend.location == "private" ? LiveUserSessionKnowledge.PrivateWorld
                        : friend.location == "offline" ? LiveUserSessionKnowledge.Offline
                        : null;
                    
                    ImmutableLiveUserSessionState sessionState;
                    if (knowledgePartial == null && cachedWorld != null)
                    {
                        var actualSession = await OnLiveSessionReceived(new ImmutableNonIndexedLiveSession
                        {
                            namedApp = NamedApp.VRChat,
                            qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                            inAppSessionIdentifier = friend.location,
                            virtualSpaceDefaultCapacity = cachedWorld.capacity,
                            inAppVirtualSpaceName = cachedWorld.name,
                        });
                        sessionState = new ImmutableLiveUserSessionState
                        {
                            knowledge = LiveUserSessionKnowledge.Known,
                            sessionGuid = actualSession.guid
                        };
                    }
                    else
                    {
                        sessionState = new ImmutableLiveUserSessionState
                        {
                            knowledge = knowledgePartial ?? LiveUserSessionKnowledge.KnownButNoData,
                        };
                    }

                    await OnLiveUpdateReceived(new ImmutableLiveUserUpdate
                    {
                        trigger = "API-ListFriends",
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppIdentifier = friend.id,
                        onlineStatus = ParseStatus("xxx", friend.location, friend.platform, friend.status),
                        callerInAppIdentifier = _callerInAppIdentifier,
                        customStatus = friend.statusDescription,
                        mainSession = sessionState,
                        sessionSpecifics = new ImmutableVRChatLiveSessionSpecifics
                        {
                            worldId = worldId
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

    private async Task WhenMessageReceived(string msg)
    {
        if (OnLiveUpdateReceived == null || OnLiveSessionReceived == null) return;
        
        try
        {
            var rootObj = JObject.Parse(msg);
            var type = rootObj["type"].Value<string>();
        
            if (type is "friend-online" or "friend-update" or "friend-offline" or "friend-location" or "friend-active" or "user-location")
            {
                // FIXME: We are ignoring user-update for now, it causes issues where the user is considered to be back online even though they are just on the website
                // despite the `onlineStatus = type is "user-update" ? null : ...` below
                
                var content = JsonConvert.DeserializeObject<VRChatWebsocketContentContainingUser>(rootObj["content"].Value<string>())!;

                var worldId = content.location != null ? LocationAsWorldIdOrNull(content.location) : null;
                CachedWorld? cachedWorld;
                if (worldId != null)
                {
                    cachedWorld = GetOrQueueWorldFetch(worldId);
                }
                else
                {
                    cachedWorld = null;
                }

                ImmutableLiveSession session = null;
                if (content.location != null)
                {
                    session = await OnLiveSessionReceived(new ImmutableNonIndexedLiveSession
                    {
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppSessionIdentifier = content.location,
                        inAppVirtualSpaceName = cachedWorld?.name,
                        virtualSpaceDefaultCapacity = cachedWorld?.capacity,
                    });
                }
                
                // FIXME: This is a task???
                OnlineStatus? onlineStatus = type is "user-update" ? null : ParseStatus(type, content.location, content.user.platform, content.user.status);
                OnLiveUpdateReceived(new ImmutableLiveUserUpdate
                {
                    trigger = $"WS-{type}",
                    namedApp = NamedApp.VRChat,
                    qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                    inAppIdentifier = content.userId,
                    onlineStatus = onlineStatus,
                    callerInAppIdentifier = _callerInAppIdentifier,
                    customStatus = content.user.statusDescription,
                    mainSession = FigureOutSessionStateOrNull(type, onlineStatus, content, session)
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

    private ImmutableLiveUserSessionState? FigureOutSessionStateOrNull(string type, OnlineStatus? onlineStatus, VRChatWebsocketContentContainingUser content, ImmutableLiveSession? session)
    {
        // Order matters. This checks acts on non-friend-location types.
        if (onlineStatus == OnlineStatus.Offline)
        {
            return new ImmutableLiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.Offline,
            };
        }
        else if (type == "friend-location")
        {
            // Order matters. "private" check comes before checking for location.
            if (content.worldId == "private" || content.location == "private")
            {
                return new ImmutableLiveUserSessionState
                {
                    knowledge = LiveUserSessionKnowledge.PrivateWorld,
                };
            }
            else
            {
                var location = content.location;
                if (location == "traveling")
                {
                    return new ImmutableLiveUserSessionState
                    {
                        knowledge = LiveUserSessionKnowledge.VRCTraveling,
                    };
                }
                else
                {
                    var sessionGuid = session?.guid;
                    if (sessionGuid == null)
                    {
                        return new ImmutableLiveUserSessionState
                        {
                            knowledge = LiveUserSessionKnowledge.KnownButNoData,
                        };
                    }
                    else
                    {
                        return new ImmutableLiveUserSessionState
                        {
                            knowledge = LiveUserSessionKnowledge.Known,
                            sessionGuid = sessionGuid
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

    private OnlineStatus ParseStatus(string type, string? contentLocation, string platform, string userStatus)
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