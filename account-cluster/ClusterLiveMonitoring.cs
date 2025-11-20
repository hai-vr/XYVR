using Newtonsoft.Json;
using XYVR.API.Audit;
using XYVR.Core;

namespace XYVR.AccountAuthority.Cluster;

public class ClusterLiveMonitoring : ILiveMonitoring
{
    private readonly LiveStatusMonitoring _monitoring;
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private string _caller;
    private CancellationTokenSource _ourCancellationTokenSource;
    private ClusterAPI? _api;

    public ClusterLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage, CancellationTokenSource cancellationTokenSource)
    {
        _monitoring = monitoring;
        _credentialsStorage = credentialsStorage;
        _cancellationTokenSource = cancellationTokenSource;
    }

    public Task StartMonitoring()
    {
        _ourCancellationTokenSource = new CancellationTokenSource();
        Task.Run(() =>
        {
            try
            {
                return RunFunction();
            }
            catch (Exception e)
            {
                XYVRLogging.ErrorWriteLine(this, e);
                throw;
            }
        }, _ourCancellationTokenSource.Token);
        
        return Task.CompletedTask;
    }

    private async Task RunFunction()
    {
        _api ??= await InitializeApi();
        
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            var hots = await _api.GetHots(DataCollectionReason.CollectSessionLocationInformation);
            foreach (var hot in hots.contents)
            {
                var sessionIdentifier = hot.eventInfo?.worldRoomSetId ?? hot.spaceInfo?.worldRoomSetId;
                if (sessionIdentifier != null)
                {
                    await _monitoring.MergeSession(new ImmutableNonIndexedLiveSession
                    {
                        namedApp = NamedApp.Cluster,
                        qualifiedAppName = ClusterAuthority.QualifiedAppName,
                        inAppSessionIdentifier = sessionIdentifier,
                        callerInAppIdentifier = _caller,
                        inAppVirtualSpaceName = hot.title,
                        currentAttendance = hot.playerCount,
                        thumbnailUrl = hot.thumbnailUrl.StartsWith(AuditUrls.ClusterAllowedThumbnailUrl) ? hot.thumbnailUrl : null,
                    });
                }
            }

            var friends = await _api.GetFriends(DataCollectionReason.CollectSessionLocationInformation);
            foreach (var friend in friends)
            {
                var status = friend.onlineStatus == nameof(ClusterOnlineStatus.Offline)
                    ? OnlineStatus.Offline
                    : OnlineStatus.Online;
                
                if (friend.liveEntry != null)
                {
                    var isEvent = friend.liveEntry.worldRoomSetType == "Event";
                    await _monitoring.MergeSession(new ImmutableNonIndexedLiveSession
                    {
                        namedApp = NamedApp.Cluster,
                        qualifiedAppName = ClusterAuthority.QualifiedAppName,
                        inAppSessionIdentifier = friend.liveEntry.id,
                        callerInAppIdentifier = _caller,
                        inAppVirtualSpaceName = friend.liveEntry.name,
                        markers = isEvent ? [LiveSessionMarker.ClusterEvent] : []
                    });
                }
                
                await _monitoring.MergeUser(new ImmutableLiveUserUpdate
                {
                    namedApp = NamedApp.Cluster,
                    qualifiedAppName = ClusterAuthority.QualifiedAppName,
                    inAppIdentifier = friend.user.userId,
                    callerInAppIdentifier = _caller,
                    onlineStatus = status,
                    mainSession = new ImmutableLiveUserSessionState
                    {
                        knowledge = friend.onlineStatus == nameof(ClusterOnlineStatus.OnlinePublic)
                            ? LiveUserSessionKnowledge.Known
                            : status == OnlineStatus.Offline
                                ? LiveUserSessionKnowledge.Offline
                                : LiveUserSessionKnowledge.ClusterOnlinePrivate,
                        sessionGuid = _monitoring.GetAllSessions(NamedApp.Cluster)
                            .FirstOrDefault(session => session.inAppSessionIdentifier == friend.liveEntry?.id)
                            ?.guid,
                    },
                    trigger = "API-GetFriends"
                });
            }
            
            Task.Delay(TimeSpan.FromMinutes(4), _ourCancellationTokenSource.Token).Wait();
        }
    }

    public async Task StopMonitoring()
    {
        await _ourCancellationTokenSource.CancelAsync();
        _ourCancellationTokenSource = null;
    }

    public Task DefineCaller(string callerInAppIdentifier)
    {
        _caller = callerInAppIdentifier;
        
        return Task.CompletedTask;
    }

    public Task MakeGameClientJoinOrSelfInvite(string sessionId, CancellationTokenSource cancellationTokenSource)
    {
        return Task.CompletedTask;
    }

    private async Task<ClusterAPI> InitializeApi()
    {
        var api = new ClusterAPI(new DoNotStoreAnythingStorage(), _cancellationTokenSource);
        var token__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (token__sensitive != null)
        {
            var deserialized__sensitive = JsonConvert.DeserializeObject<ClusterAuthStorage>(token__sensitive)!;
            api.Provide(deserialized__sensitive);
        }
        return api;
    }
}