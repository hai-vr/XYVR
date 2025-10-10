using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using XYVR.Core;

namespace XYVR.AccountAuthority.ChilloutVR;

internal class ChilloutVRLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;

    private ChilloutVRWebsocketClient _wsClient;
    private ChilloutVRAPI? _api;
    private bool _hasInitiatedDisconnect;

    private readonly Lock _queueLock = new();
    private readonly ConcurrentQueue<string> _fetchInstanceQueue = new();
    private Task _queueTask = Task.CompletedTask;

    public event CvrLiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task CvrLiveUpdateReceived(ImmutableLiveUserUpdate liveUserUpdate);

    public event CvrLiveSessionReceived? OnLiveSessionReceived;
    public delegate Task<ImmutableLiveSession> CvrLiveSessionReceived(ImmutableNonIndexedLiveSession liveSession);

    public ChilloutVRLiveCommunicator(ICredentialsStorage credentialsStorage, string callerInAppIdentifier)
    {
        _credentialsStorage = credentialsStorage;
        _callerInAppIdentifier = callerInAppIdentifier;
    }

    public async Task Connect()
    {
        _hasInitiatedDisconnect = false;

        if (_wsClient != null)
        {
            _wsClient.Disconnected -= WhenDisconnected;
            try { await _wsClient.Disconnect(); }
            catch
            { // ignored
            }

            try { _wsClient.Dispose(); }
            catch
            { // ignored
            }
            _wsClient = null;
        }

        _wsClient = new ChilloutVRWebsocketClient();
        _wsClient.Connected += WhenConnected;
        _wsClient.MessageReceived += msg => WhenMessageReceived(msg);
        _wsClient.Disconnected += WhenDisconnected;

        _api ??= await InitializeAPI();

        var token__sensitive = await GetToken__sensitive();
        if (token__sensitive != null)
        {
            await _wsClient.Connect(token__sensitive.username, token__sensitive.accessKey);
        }
    }

    public async Task Disconnect()
    {
        _hasInitiatedDisconnect = true;
        await _wsClient.Disconnect();
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
                });
            }
        }
    }


    private async Task ProcessQueue()
    {
        XYVRLogging.WriteLine(this, "Processing queue");
        _api ??= await InitializeAPI();

        while (_fetchInstanceQueue.Count > 0)
        {
            var anythingDequeued = _fetchInstanceQueue.TryDequeue(out var instanceId);
            if (anythingDequeued && instanceId != null)
            {
                var locationResponse = await _api.GetInstance(instanceId);
                if (locationResponse != null)
                {
                    if (OnLiveSessionReceived != null)
                    {
                        var location = locationResponse.data;
                        await OnLiveSessionReceived(new ImmutableNonIndexedLiveSession
                        {
                            namedApp = NamedApp.ChilloutVR,
                            qualifiedAppName = ChilloutVRAuthority.QualifiedAppName,
                            inAppSessionIdentifier = instanceId,

                            inAppSessionName = location.name,
                            inAppVirtualSpaceName = location.world.name,

                            inAppHost = new ImmutableLiveSessionHost
                            {
                                inAppHostIdentifier = location.owner.id,
                                inAppHostDisplayName = location.owner.name
                            },
                            
                            virtualSpaceDefaultCapacity = location.maxPlayer,
                            sessionCapacity = location.maxPlayer,
                            currentAttendance = location.currentPlayerCount,

                            thumbnailUrl = location.world.imageUrl,

                        });
                    }
                }
            }
        }
    }


    private async Task WhenMessageReceived(string msg)
    {
        try
        {
            if (OnLiveUpdateReceived == null || OnLiveSessionReceived == null) return;

            var rootObj = JObject.Parse(msg);
            var type = (CvrWebsocketMessageType) rootObj["ResponseType"].Value<int>();

            if (type == CvrWebsocketMessageType.ONLINE_FRIENDS)
            {
                var friendData = rootObj["Data"].ToObject<CvrWebsocketOnlineFriendsResponse[]>();
                foreach (var friend in friendData)
                {
                    ImmutableLiveUserSessionState state = await GetSessionState(friend);

                    await OnLiveUpdateReceived(new ImmutableLiveUserUpdate()
                    {
                        trigger = $"WS-{type}",
                        namedApp = NamedApp.ChilloutVR,
                        qualifiedAppName = ChilloutVRAuthority.QualifiedAppName,
                        inAppIdentifier = friend.Id,
                        onlineStatus = friend.IsOnline ? OnlineStatus.Online : OnlineStatus.Offline,
                        callerInAppIdentifier = _callerInAppIdentifier,
                        mainSession = state,
                    });
                }
            }
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    private async Task<ImmutableLiveUserSessionState> GetSessionState(CvrWebsocketOnlineFriendsResponse friend)
    {
        if (!friend.IsOnline)
        {
            return new ImmutableLiveUserSessionState() { knowledge = LiveUserSessionKnowledge.Offline };
        }
        if (!friend.IsConnected)
        {
            return new ImmutableLiveUserSessionState() { knowledge = LiveUserSessionKnowledge.OfflineInstance };
        }
        if (friend.Instance != null && friend.Instance.Id != null)
        {
            var nonIndexed = new ImmutableNonIndexedLiveSession()
            {
                namedApp = NamedApp.ChilloutVR,
                
                qualifiedAppName = ChilloutVRAuthority.QualifiedAppName,
                inAppSessionIdentifier = friend.Instance.Id,

                inAppSessionName = friend.Instance.Name,
            };
            var instance = await OnLiveSessionReceived(nonIndexed);
            if (instance != null)
            {
                QueueUpdateInstance(friend.Instance.Id);
                return new ImmutableLiveUserSessionState() { knowledge = LiveUserSessionKnowledge.Known, sessionGuid = instance.guid };
            }
        }
        return new ImmutableLiveUserSessionState() { knowledge = LiveUserSessionKnowledge.PrivateInstance };
    }

    private void WhenConnected()
    {
    }

    private void WhenDisconnected(string reason)
    {
        try
        {
            XYVRLogging.WriteLine(this, $"We got disconnected from the CVR WS API. Reason: {reason}");
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
                            XYVRLogging.WriteLine(this, "Successfully reconnected to the CVR WS API.");
                            success = true;
                        }
                        catch (Exception e)
                        {
                            XYVRLogging.ErrorWriteLine(this, e);
                            var nextRetryDelay = NextRetryDelay(attempt);
                            XYVRLogging.WriteLine(this, $"Failed to reconnect to the CVR WS API ({attempt + 1} times), will try again in {nextRetryDelay.TotalSeconds} seconds...");
                            await Task.Delay(nextRetryDelay);
                            attempt++;
                        }
                    }
                }).Wait();
            }
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    public void QueueUpdateInstances(IEnumerable<ImmutableLiveSession> instances)
    {
        foreach (var instance in instances)
        {
            QueueUpdateInstance(instance.inAppSessionIdentifier);
        }
    }

    public void QueueUpdateInstance(string instanceId)
    {
        if (instanceId == null || _fetchInstanceQueue.Contains(instanceId)) return;
        _fetchInstanceQueue.Enqueue(instanceId);
        WakeUpQueue();
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

    private async Task<ChilloutVRAuthStorage?> GetToken__sensitive()
    {
        return JsonConvert.DeserializeObject<ChilloutVRAuthStorage>(await _credentialsStorage.RequireCookieOrToken());
    }

    private async Task<ChilloutVRAPI> InitializeAPI()
    {
        var api = new ChilloutVRAPI();
        var token__sensitive = await GetToken__sensitive();
        if (token__sensitive != null)
        {
            api.Provide(token__sensitive);
        }

        return api;
    }
}
