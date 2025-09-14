using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteLiveMonitoring : ILiveMonitoring, IDisposable
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly LiveStatusMonitoring _monitoring;
    private readonly string _uid__sensitive;
    
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _isConnected;
    private string? _callerInAppIdentifier;
    
    private ResoniteLiveCommunicator? _liveComms;
    private CancellationTokenSource _cancellationTokenSource;

    public ResoniteLiveMonitoring(ICredentialsStorage credentialsStorage, LiveStatusMonitoring monitoring, string uid__sensitive)
    {
        _credentialsStorage = credentialsStorage;
        _monitoring = monitoring;
        _uid__sensitive = uid__sensitive;
    }

    public Task DefineCaller(string callerInAppIdentifier)
    {
        _callerInAppIdentifier = callerInAppIdentifier;
        return Task.CompletedTask;
    }

    public async Task StartMonitoring()
    {
        if (_callerInAppIdentifier == null) throw new InvalidOperationException("Caller must be defined to start monitoring");
        
        await _operationLock.WaitAsync();
        try
        {
            if (_isConnected) return;
            _cancellationTokenSource = new CancellationTokenSource();
            
            _liveComms = new ResoniteLiveCommunicator(_credentialsStorage, _callerInAppIdentifier, _uid__sensitive, new DoNotStoreAnythingStorage(), TryGetSessionIdToSessionGuid);
            
            var serializer = new JsonSerializerSettings()
            {
                Converters = { new StringEnumConverter() }
            };
            
            var alreadyListeningTo = new HashSet<string>();
            _liveComms.OnLiveUpdateReceived += async update =>
            {
                Console.WriteLine($"OnLiveUpdateReceived: {JsonConvert.SerializeObject(update, serializer)}");
                await _monitoring.MergeUser(update);

                if (!alreadyListeningTo.Contains(update.inAppIdentifier))
                {
                    await _liveComms.ListenOnContact(update.inAppIdentifier);
                    alreadyListeningTo.Add(update.inAppIdentifier);
                }
            };
            _liveComms.OnReconnected += async () =>
            {
                foreach (var inAppIdentifier in alreadyListeningTo)
                {
                    await _liveComms.ListenOnContact(inAppIdentifier);
                }
            };
            _liveComms.OnSessionUpdated += async sessionUpdate =>
            {
                var sessionId = sessionUpdate.sessionId;
                var correspondingSession = await _monitoring.MergeSession(new ImmutableNonIndexedLiveSession
                {
                    namedApp = NamedApp.Resonite,
                    qualifiedAppName = ResoniteCommunicator.ResoniteQualifiedAppName,
                    inAppVirtualSpaceName = ResoniteLiveCommunicator.ExtractTextFromColorTags(sessionUpdate.name),
                    inAppSessionIdentifier = sessionId,
                    inAppHost = new ImmutableLiveSessionHost
                    {
                        inAppHostIdentifier = sessionUpdate.hostUserId,
                        inAppHostDisplayName = sessionUpdate.hostUsername
                    },
                    currentAttendance = sessionUpdate.joinedUsers,
                    sessionCapacity = sessionUpdate.maxUsers,
                    virtualSpaceDefaultCapacity = sessionUpdate.maxUsers,
                });
                
                foreach (var userUpdate in _monitoring.GetAllUserData(NamedApp.Resonite))
                {
                    if (userUpdate.mainSession?.knowledge == LiveUserSessionKnowledge.KnownButNoData)
                    {
                        var resoniteSession = (ImmutableResoniteLiveSessionSpecifics)userUpdate.sessionSpecifics!;
                        if (resoniteSession is { sessionHash: not null, userHashSalt: not null }
                            && await ResoniteHash.Rehash(sessionId, resoniteSession.userHashSalt) == resoniteSession.sessionHash)
                        {
                            Console.WriteLine("Received a session for which a user had an unresolved hash for.");
                            
                            await _monitoring.MergeUser(userUpdate with
                            {
                                mainSession = new ImmutableLiveUserSessionState
                                {
                                    knowledge = LiveUserSessionKnowledge.Known,
                                    sessionGuid = correspondingSession.guid
                                }
                            });
                        }
                    }
                }
            };
            
            await _liveComms.Connect();
            
            _ = Task.Run(BackgroundTask, _cancellationTokenSource.Token);
            _isConnected = true;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private string? TryGetSessionIdToSessionGuid(string sessionId)
    {
        return _monitoring.GetAllSessions(NamedApp.Resonite)
            .FirstOrDefault(session => session.inAppSessionIdentifier == sessionId)?.guid;
    }

    private async Task BackgroundTask()
    {
        try
        {
            while (true) // Canceled by token
            {
                // Unsure why, but if it runs for a while, we won't receive any updates until the user actually starts the game?
                // Request a full update every so often
                await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                await _liveComms.RequestFullUpdate();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task StopMonitoring()
    {
        await _operationLock.WaitAsync();
        try
        {
            if (!_isConnected) return;
            
            Console.WriteLine("Will try to cancel token");
            // await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.CancelAsync(); // FIXME: we have a problem when we wait for this to finish, it never completes. Why?
            Console.WriteLine("Token cancelled. Will try to disconnect");
            
            await _liveComms.Disconnect();
            Console.WriteLine("Disconnected.");
            
            _liveComms = null;
            _isConnected = false;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public void Dispose()
    {
        _operationLock.Dispose();
    }
}