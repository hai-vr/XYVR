using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.Core;

namespace XYVR.AccountAuthority.VRChat;

internal interface IQueueJob
{
}

internal record WorldQueueJob : IQueueJob
{
    public string worldId { get; init; }
}

internal record InstanceQueueJob : IQueueJob
{
    public string worldIdAndInstanceId { get; init; }
    public bool useFastFetch { get; init; } = false;
}

internal class VRChatLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;
    private readonly IResponseCollector _responseCollector;
    private readonly WorldNameCache _worldNameCache;

    private readonly Lock _queueLock = new();
    private readonly HashSet<IQueueJob> _allQueued = new();
    private readonly Queue<IQueueJob> _queue = new();
    private readonly Queue<IQueueJob> _highPriorityQueue = new();
    private Task _queueTask = Task.CompletedTask;

    private VRChatWebsocketClient _wsClient;
    private VRChatAPI? _api;
    private bool _hasInitiatedDisconnect;
    private readonly Dictionary<string, VRChatInstance> _locationToInstance = new Dictionary<string, VRChatInstance>();

    public event VrcLiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task VrcLiveUpdateReceived(ImmutableLiveUserUpdate liveUserUpdate);

    public event VrcLiveSessionReceived? OnLiveSessionReceived;
    public delegate Task<ImmutableLiveSession> VrcLiveSessionReceived(ImmutableNonIndexedLiveSession liveSession);

    public event WorldResolved? OnWorldCached;
    public delegate Task WorldResolved(CachedWorld world);

    public event SessionRetrieved? OnSessionRetrieved;
    public delegate Task SessionRetrieved(VRChatInstance world);

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
                _queueTask = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessQueue();
                    }
                    catch (Exception e)
                    {
                        XYVRLogging.WriteLine(this, e);
                        throw;
                    }
                });
            }
        }
    }

    private async Task ProcessQueue()
    {
        XYVRLogging.WriteLine(this, "Processing queue");
        _api ??= await InitializeAPI();
        
        while (_queue.Count > 0 || _highPriorityQueue.Count > 0)
        {
            var dequeued = _highPriorityQueue.Count > 0 ? _highPriorityQueue.Dequeue() : _queue.Dequeue();
            if (dequeued is WorldQueueJob worldQueueJob)
            {
                var worldId = worldQueueJob.worldId;
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
            else if (dequeued is InstanceQueueJob instanceQueueJob)
            {
                var worldIdAndInstanceId = instanceQueueJob.worldIdAndInstanceId;
                var locationInformation = await _api.GetInstanceLenient(DataCollectionReason.CollectSessionLocationInformation, worldIdAndInstanceId, instanceQueueJob.useFastFetch);
                if (locationInformation != null)
                {
                    _locationToInstance.TryAdd(instanceQueueJob.worldIdAndInstanceId, locationInformation);

                    if (OnLiveSessionReceived != null)
                    {
                        await OnLiveSessionReceived(new ImmutableNonIndexedLiveSession
                        {
                            namedApp = NamedApp.VRChat,
                            qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                            inAppSessionIdentifier = instanceQueueJob.worldIdAndInstanceId,
                            sessionCapacity = locationInformation.capacity,
                            currentAttendance = locationInformation.userCount
                        });
                    }
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
                        var actualSession = await OnLiveSessionReceived(MakeNonIndexedBasedOnWorld(friend.location, cachedWorld));
                        sessionState = new ImmutableLiveUserSessionState
                        {
                            knowledge = LiveUserSessionKnowledge.Known,
                            sessionGuid = actualSession.guid
                        };
                    }
                    else if (knowledgePartial == null && worldId != null)
                    {
                        var actualSession = await OnLiveSessionReceived(MakeNonIndexedBasedOnWorld(friend.location, null));
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
                    
                    await QueueSessionFetchIfApplicable(friend.location, useFastFetch: true);

                    await OnLiveUpdateReceived(new ImmutableLiveUserUpdate
                    {
                        trigger = "API-ListFriends",
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppIdentifier = friend.id,
                        onlineStatus = friend.platform == "web" ? OnlineStatus.Offline : ParseStatus(friend.status, friend.platform),
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
                    XYVRLogging.WriteLine(this, e);
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
            
            var contentddd = JsonConvert.DeserializeObject<VRChatWebsocketContentContainingUser>(rootObj["content"].Value<string>())!;
            XYVRLogging.WriteLine(this, $"-- [[{type}]] from vrc ws api. {contentddd.userId} ({contentddd.user?.displayName}) location: {contentddd.location} -- platform: {contentddd.user?.platform} --- status: {contentddd.user?.status}");
            
            var isSessionKnowable = type is "friend-online" or "friend-location" or "friend-offline" or "friend-active";
            if (isSessionKnowable)
            {
                var content = JsonConvert.DeserializeObject<VRChatWebsocketContentContainingUser>(rootObj["content"].Value<string>())!;
                
                var isOffline = type is "friend-offline" or "friend-active";
                if (!isOffline)
                {
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
                    if (content.location != null && content.location.StartsWith("wrld_"))
                    {
                        session = await OnLiveSessionReceived(MakeNonIndexedBasedOnWorld(content.location, cachedWorld));

                        await QueueSessionFetchIfApplicable(content.location);
                    }
                
                    OnlineStatus? onlineStatus = content.user != null ? ParseStatus(content.user.status, content.user.platform) : null;
                    var figureOutSessionStateOrNull = session != null ? new ImmutableLiveUserSessionState
                    {
                        knowledge = LiveUserSessionKnowledge.Known,
                        sessionGuid = session.guid
                    } : FigureOutSessionStateOrNull(type, onlineStatus, content, session);
                    await OnLiveUpdateReceived(new ImmutableLiveUserUpdate
                    {
                        trigger = $"WS-{type}",
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppIdentifier = content.userId,
                        onlineStatus = onlineStatus,
                        callerInAppIdentifier = _callerInAppIdentifier,
                        customStatus = content.user?.statusDescription,
                        mainSession = isSessionKnowable ? figureOutSessionStateOrNull : null
                    });
                }
                else
                {
                    await OnLiveUpdateReceived(new ImmutableLiveUserUpdate
                    {
                        trigger = $"WS-{type}",
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppIdentifier = content.userId,
                        onlineStatus = OnlineStatus.Offline,
                        callerInAppIdentifier = _callerInAppIdentifier,
                        customStatus = content.user?.statusDescription,
                        mainSession = new ImmutableLiveUserSessionState
                        {
                            knowledge = LiveUserSessionKnowledge.Offline,
                        }
                    });
                };
            }
            else if (type is "friend-update" or "user-location" or "user-update")
            {
                var content = JsonConvert.DeserializeObject<VRChatWebsocketContentContainingUser>(rootObj["content"].Value<string>())!;
                
                await OnLiveUpdateReceived(new ImmutableLiveUserUpdate
                {
                    trigger = $"WS-{type}",
                    namedApp = NamedApp.VRChat,
                    qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                    inAppIdentifier = content.userId,
                    onlineStatus = null,
                    callerInAppIdentifier = _callerInAppIdentifier,
                    customStatus = content.user?.statusDescription,
                    mainSession = null
                });
            }
            else
            {
                XYVRLogging.WriteLine(this, $"Received UNHANDLED message of type {type} from vrc ws api");
            }
        }
        catch (Exception e)
        {
            XYVRLogging.WriteLine(this, e);
            throw;
        }
    }

    public async Task QueueUpdateSessionsIfApplicable(List<ImmutableLiveSession> sessionsToUpdate)
    {
        foreach (var session in sessionsToUpdate)
        {
            await QueueSessionFetchIfApplicable(session.inAppSessionIdentifier, onlyConsiderItemsInQueue: true, useFastFetch: false);
        }
    }

    private async Task QueueSessionFetchIfApplicable(string location, bool onlyConsiderItemsInQueue = false, bool useFastFetch = false)
    {
        _api ??= await InitializeAPI();
        
        var worldIdAndInstanceId = WorldIdAndInstanceIdOrNull(location);
        if (worldIdAndInstanceId != null)// && !_locationToInstance.TryGetValue(location, out var instance))
        {
            var queueJob = new InstanceQueueJob
            {
                worldIdAndInstanceId = worldIdAndInstanceId,
                useFastFetch = useFastFetch
            };
            if (!onlyConsiderItemsInQueue && !_allQueued.Contains(queueJob)
                || onlyConsiderItemsInQueue && !_queue.Contains(queueJob) && !_highPriorityQueue.Contains(queueJob))
            {
                _allQueued.Add(queueJob);
                _queue.Enqueue(queueJob);
                WakeUpQueue();
            }
        }
    }

    private string? WorldIdAndInstanceIdOrNull(string contentLocation)
    {
        if (!contentLocation.StartsWith("wrld_")) return null;
        return contentLocation;
    }

    public static ImmutableNonIndexedLiveSession MakeNonIndexedBasedOnWorld(string location, CachedWorld? cachedWorld)
    {
        return new ImmutableNonIndexedLiveSession
        {
            namedApp = NamedApp.VRChat,
            qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
            inAppSessionIdentifier = location,
            inAppVirtualSpaceName = cachedWorld?.name,
            virtualSpaceDefaultCapacity = cachedWorld?.capacity,
            isVirtualSpacePrivate = cachedWorld?.releaseStatus == "private",
            thumbnailUrl = cachedWorld?.thumbnailUrl,
        };
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

        if (content.location == "private")
        {
            return new ImmutableLiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.PrivateWorld,
            };
        }
        else if (content.location == "traveling")
        {
            return new ImmutableLiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.VRCTraveling,
            };
        }
        else if (type == "friend-location")
        {
            var sessionGuid = session?.guid;
            if (sessionGuid == null)
            {
                return new ImmutableLiveUserSessionState
                {
                    knowledge = LiveUserSessionKnowledge.Indeterminate,
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
        else
        {
            return null;
        }
    }

    private string? LocationAsWorldIdOrNull(string location)
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
                XYVRLogging.WriteLine(this, $"Location is not parseable as a world: {location}");
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    private CachedWorld? GetOrQueueWorldFetch(string worldId)
    {
        var cachedWorldNullable = _worldNameCache.GetValidOrNull(worldId);

        var shouldAttemptQueueing = cachedWorldNullable == null || cachedWorldNullable.needsRefresh;

        var job = new WorldQueueJob { worldId = worldId };
        if (shouldAttemptQueueing && !_allQueued.Contains(job))
        {
            _allQueued.Add(job);
            if (cachedWorldNullable != null)
            {
                XYVRLogging.WriteLine(this, $"We don't know world id {worldId}, will queue fetch...");
                _highPriorityQueue.Enqueue(job);
            }
            else
            {
                XYVRLogging.WriteLine(this, $"We need to refresh our knowledge of world id {worldId}, will queue fetch...");
                _queue.Enqueue(job);
            }

            WakeUpQueue();
        }
            
        return cachedWorldNullable;
    }

    private OnlineStatus ParseStatus(string userStatus, string platform)
    {
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
        XYVRLogging.WriteLine(this, $"We got disconnected from the vrc ws api. Reason: {reason}");
        if (!_hasInitiatedDisconnect)
        {
            XYVRLogging.WriteLine(this, "Will try reconnecting.");
            Task.Run(async () =>
            {
                try
                {
                    await Connect();
                }
                catch (Exception e)
                {
                    XYVRLogging.WriteLine(this, e);
                    throw;
                }
            }).Wait();
        }
    }

    private async Task<string> GetToken__sensitive()
    {
        return JsonConvert.DeserializeObject<VRChatAuthStorage>(await _credentialsStorage.RequireCookieOrToken())
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