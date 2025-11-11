using System.Collections.Concurrent;
using System.Collections.Immutable;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.AccountAuthority.VRChat.ThirdParty;
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
    private readonly IThumbnailCache _thumbnailCache;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly Lock _queueLock = new();
    private readonly ConcurrentDictionary<IQueueJob, bool> _allQueued = new();
    private readonly ConcurrentQueue<IQueueJob> _queue = new();
    private readonly ConcurrentQueue<IQueueJob> _highPriorityQueue = new();
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

    public VRChatLiveCommunicator(ICredentialsStorage credentialsStorage, string callerInAppIdentifier, IResponseCollector responseCollector, WorldNameCache worldNameCache, IThumbnailCache thumbnailCache, CancellationTokenSource cancellationTokenSource)
    {
        _credentialsStorage = credentialsStorage;
        _callerInAppIdentifier = callerInAppIdentifier;
        _responseCollector = responseCollector;
        _worldNameCache = worldNameCache;
        _thumbnailCache = thumbnailCache;
        _cancellationTokenSource = cancellationTokenSource;
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
                        XYVRLogging.ErrorWriteLine(this, e);
                        throw;
                    }
                }, _cancellationTokenSource.Token);
            }
        }
    }

    private async Task ProcessQueue()
    {
        XYVRLogging.WriteLine(this, "Processing queue");
        _api ??= await InitializeAPI();

        var liveSessionsBatch = new List<ImmutableNonIndexedLiveSession>();
        
        while ((_queue.Count > 0 || _highPriorityQueue.Count > 0) && !_cancellationTokenSource.IsCancellationRequested)
        {
            var anythingDequeued = _highPriorityQueue.TryDequeue(out var dequeued) || _queue.TryDequeue(out dequeued);
            if (anythingDequeued && dequeued is WorldQueueJob worldQueueJob)
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
                    var thumbnail = await _api.DownloadThumbnailImage(worldLenient.thumbnailImageUrl);
                    if (thumbnail != null)
                    {
                        XYVRLogging.WriteLine(this, $"Downloaded world thumbnail {worldLenient.thumbnailImageUrl}, this will be cached.");
                        await _thumbnailCache.Save(worldLenient.thumbnailImageUrl, thumbnail);
                    }
                    else
                    {
                        XYVRLogging.ErrorWriteLine(this, $"Failed to download world thumbnail {worldLenient.thumbnailImageUrl}");
                    }

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
                    
                    liveSessionsBatch.Add(new ImmutableNonIndexedLiveSession
                    {
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppSessionIdentifier = instanceQueueJob.worldIdAndInstanceId,
                        inAppSessionName = locationInformation.displayName,
                        sessionCapacity = locationInformation.capacity,
                        currentAttendance = locationInformation.userCount,
                        ageGated = locationInformation.ageGate,
                        callerInAppIdentifier = _callerInAppIdentifier
                    });
                    XYVRLogging.WriteLine(this, $"Collected live session about {instanceQueueJob.worldIdAndInstanceId}, will batch results... ({liveSessionsBatch.Count} sessions batched so far)");
                }
            }

            if (_queue.Count(job => job is InstanceQueueJob) == 0)
            {
                // We batch the session updates, so that the UI doesn't reorder every time a session updates when the queue is being processed.
                if (liveSessionsBatch.Count > 0 && OnLiveSessionReceived != null)
                {
                    XYVRLogging.WriteLine(this, "Submitting batch of live sessions...");
                    foreach (var immutableNonIndexedLiveSession in liveSessionsBatch)
                    {
                        await OnLiveSessionReceived(immutableNonIndexedLiveSession);
                    }
                    liveSessionsBatch.Clear();
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
            if (_cancellationTokenSource.IsCancellationRequested)
            {
                throw new OperationCanceledException(_cancellationTokenSource.Token);
            }
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
                        var actualSession = await OnLiveSessionReceived(MakeNonIndexedBasedOnWorld(friend.location, cachedWorld, _callerInAppIdentifier));
                        sessionState = new ImmutableLiveUserSessionState
                        {
                            knowledge = LiveUserSessionKnowledge.Known,
                            sessionGuid = actualSession.guid
                        };
                    }
                    else if (knowledgePartial == null && worldId != null)
                    {
                        var actualSession = await OnLiveSessionReceived(MakeNonIndexedBasedOnWorld(friend.location, null, _callerInAppIdentifier));
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
                    XYVRLogging.ErrorWriteLine(this, e);
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
        if (_cancellationTokenSource.IsCancellationRequested) return;
        try
        {
            if (OnLiveUpdateReceived == null || OnLiveSessionReceived == null) return;

            var rootObj = JObject.Parse(msg);
            var type = rootObj["type"].Value<string>();
            
            var isSessionKnowable = type is "friend-online" or "friend-location" or "friend-offline" or "friend-active";
            if (isSessionKnowable)
            {
                var content = JsonConvert.DeserializeObject<VRChatWebsocketContentContainingUser>(rootObj["content"].Value<string>())!;
                
                var isOffline = type is "friend-offline" or "friend-active";
                if (!isOffline)
                {
                    await HandleSession(content, type, isSessionKnowable);
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

                if (type is "user-location")
                {
                    await HandleSession(content, type, true);
                }
                else
                {
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
            }
            else
            {
                XYVRLogging.WriteLine(this, $"Received UNHANDLED message of type {type} from VRC WS API");
            }
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    private async Task HandleSession(VRChatWebsocketContentContainingUser content, string? type, bool isSessionKnowable)
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
            session = await OnLiveSessionReceived(MakeNonIndexedBasedOnWorld(content.location, cachedWorld, _callerInAppIdentifier));

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
            if (!onlyConsiderItemsInQueue && !_allQueued.ContainsKey(queueJob)
                || onlyConsiderItemsInQueue && !_queue.Contains(queueJob) && !_highPriorityQueue.Contains(queueJob))
            {
                _allQueued[queueJob] = true;
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

    public static ImmutableNonIndexedLiveSession MakeNonIndexedBasedOnWorld(string location, CachedWorld? cachedWorld, string callerInAppIdentifier)
    {
        var vrcxLocationContext = VRCXLocation.ParseLocation(location);
        
        return new ImmutableNonIndexedLiveSession
        {
            namedApp = NamedApp.VRChat,
            qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
            inAppSessionIdentifier = location,
            inAppVirtualSpaceName = cachedWorld?.name,
            virtualSpaceDefaultCapacity = cachedWorld?.capacity,
            isVirtualSpacePrivate = cachedWorld?.releaseStatus == "private",
            thumbnailUrl = cachedWorld?.thumbnailUrl,
            callerInAppIdentifier = callerInAppIdentifier,
            markers = ToMarker(vrcxLocationContext.AccessType)
        };
    }

    private static ImmutableArray<LiveSessionMarker>? ToMarker(VRCXLocationInferredAccessType accessType)
    {
        return accessType switch
        {
            VRCXLocationInferredAccessType.Indeterminate => [],
            VRCXLocationInferredAccessType.Public => [LiveSessionMarker.VRCPublic],
            VRCXLocationInferredAccessType.InvitePlus => [LiveSessionMarker.VRCInvitePlus],
            VRCXLocationInferredAccessType.Invite => [LiveSessionMarker.VRCInvite],
            VRCXLocationInferredAccessType.Friends => [LiveSessionMarker.VRCFriends],
            VRCXLocationInferredAccessType.FriendsPlus => [LiveSessionMarker.VRCFriendsPlus],
            VRCXLocationInferredAccessType.Group => [LiveSessionMarker.VRCGroup],
            VRCXLocationInferredAccessType.GroupPublic => [LiveSessionMarker.VRCGroupPublic],
            VRCXLocationInferredAccessType.GroupPlus => [LiveSessionMarker.VRCGroupPlus],
            _ => throw new ArgumentOutOfRangeException(nameof(accessType), accessType, null)
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
        if (shouldAttemptQueueing && !_allQueued.ContainsKey(job))
        {
            _allQueued[job] = true;
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
        try
        {
            XYVRLogging.WriteLine(this, $"We got disconnected from the VRC WS API. Reason: {reason}");
            if (!_hasInitiatedDisconnect)
            {
                XYVRLogging.WriteLine(this, "Will try reconnecting.");
                Task.Run(async () =>
                {
                    var attempt = 0;
                    var success = false;
                    while (!success)
                    {
                        try
                        {
                            await Connect();
                            XYVRLogging.WriteLine(this, "Successfully reconnected to the VRC WS API.");
                            success = true;
                        }
                        catch (Exception e)
                        {
                            XYVRLogging.ErrorWriteLine(this, e);
                            var nextRetryDelay = NextRetryDelay(attempt);
                            XYVRLogging.WriteLine(this, $"Failed to reconnect to the VRC WS API ({attempt + 1} times), will try again in {nextRetryDelay.TotalSeconds} seconds...");
                            await Task.Delay(nextRetryDelay);
                            attempt++;
                        }
                    }
                }, _cancellationTokenSource.Token).Wait();
            }
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }
    
    public TimeSpan NextRetryDelay(int previousRetryCount)
    {
        return previousRetryCount switch
        {
            0 => TimeSpan.Zero,
            1 => TimeSpan.FromSeconds(2),
            2 => TimeSpan.FromSeconds(10),
            3 => TimeSpan.FromSeconds(30),
            _ => TimeSpan.FromSeconds(new Random().Next(60, 80))
        };
    }

    private async Task<string> GetToken__sensitive()
    {
        return JsonConvert.DeserializeObject<VRChatAuthStorage>(await _credentialsStorage.RequireCookieOrToken())
            .auth.Value;
    }
    
    private async Task<VRChatAPI> InitializeAPI()
    {
        var api = new VRChatAPI(_responseCollector, _cancellationTokenSource);
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

    public async Task InviteMyselfTo(string worldIdAndInstanceId)
    {
        XYVRLogging.WriteLine(this, $"Sending an invite to join session {worldIdAndInstanceId}");
        _api ??= await InitializeAPI();
        await _api.InviteMyselfTo(worldIdAndInstanceId);
    }
}