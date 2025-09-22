using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.Core;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatLiveMonitoring : ILiveMonitoring
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly LiveStatusMonitoring _monitoring;
    private readonly WorldNameCache _worldNameCache;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    private string _callerInAppIdentifier;
    private bool _isConnected;
    private VRChatLiveCommunicator _liveComms;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly HashSet<string> _sessionsOfInterest = new();

    public VRChatLiveMonitoring(ICredentialsStorage credentialsStorage, LiveStatusMonitoring monitoring, WorldNameCache worldNameCache)
    {
        _credentialsStorage = credentialsStorage;
        _monitoring = monitoring;
        _worldNameCache = worldNameCache;
    }

    public async Task StartMonitoring()
    {
        if (_callerInAppIdentifier == null) throw new InvalidOperationException("Caller must be define_d to start monitoring");
        await _operationLock.WaitAsync();
        try
        {
            if (_isConnected) return;
            _cancellationTokenSource = new CancellationTokenSource();
            
            _liveComms = new VRChatLiveCommunicator(_credentialsStorage, _callerInAppIdentifier, new DoNotStoreAnythingStorage(), _worldNameCache);
            _liveComms.OnLiveUpdateReceived += async update =>
            {
                // XYVRLogging.WriteLine(this, $"OnLiveUpdateReceived: {JsonConvert.SerializeObject(update, serializer)}");
                await _monitoring.MergeUser(update);
            };
            _liveComms.OnLiveSessionReceived += async session =>
            {
                _sessionsOfInterest.Add(session.inAppSessionIdentifier);
                // XYVRLogging.WriteLine(this, $"OnLiveSessionReceived: {JsonConvert.SerializeObject(session, serializer)}");
                return await _monitoring.MergeSession(session);
            };
            _liveComms.OnWorldCached += async world =>
            {
                XYVRLogging.WriteLine(this, $"A world was cached: {world.worldId} {world.name} by {world.authorName}");

                var allSessionsOnThisWorld = _monitoring.GetAllSessions(NamedApp.VRChat)
                    .Where(session => session.inAppSessionIdentifier.StartsWith(world.worldId))
                    .ToList();
                
                foreach (var sessionOnThisWorld in allSessionsOnThisWorld)
                {
                    var nonIndexedUpdate = VRChatLiveCommunicator.MakeNonIndexedBasedOnWorld(sessionOnThisWorld.inAppSessionIdentifier, world);
                    await _monitoring.MergeSession(nonIndexedUpdate);
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

    private async Task BackgroundTask()
    {
        try
        {
            while (true) // Canceled by token
            {
                await Task.Delay(TimeSpan.FromMinutes(5), _cancellationTokenSource.Token);
                var sessionsToUpdate = _monitoring.GetAllSessions(NamedApp.VRChat)
                    .Where(session => _sessionsOfInterest.Contains(session.inAppSessionIdentifier))
                    .Where(session => session.participants.Length > 0)
                    .OrderByDescending(session => session.currentAttendance ?? session.participants.Length)
                    .ToList();
                XYVRLogging.WriteLine(this, $"Requesting to refresh all sessions of our interest (total of {sessionsToUpdate.Count} sessions)");
                await _liveComms.QueueUpdateSessionsIfApplicable(sessionsToUpdate);
            }
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    public async Task StopMonitoring()
    {
        await _operationLock.WaitAsync();
        try
        {
            if (!_isConnected) return;
            
            XYVRLogging.WriteLine(this, "Will try to cancel token");
            // await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource.CancelAsync(); // FIXME: we have a problem when we wait for this to finish, it never completes. Why?
            XYVRLogging.WriteLine(this, "Token cancelled. Will try to disconnect");
            
            await _liveComms.Disconnect();
            _isConnected = false;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public Task DefineCaller(string callerInAppIdentifier)
    {
        _callerInAppIdentifier = callerInAppIdentifier;
        return Task.CompletedTask;
    }
}