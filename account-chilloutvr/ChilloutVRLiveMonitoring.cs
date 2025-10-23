using XYVR.Core;

namespace XYVR.AccountAuthority.ChilloutVR;

public class ChilloutVRLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage) : ILiveMonitoring
{
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    
    private string _callerInAppIdentifier = null!;
    private bool _isConnected;
    private ChilloutVRLiveCommunicator _liveComms;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly HashSet<string> _sessionsOfInterest = new();

    public async Task StartMonitoring()
    {
        if (_callerInAppIdentifier == null) throw new InvalidOperationException("Caller must be defined to start monitoring");

        await _operationLock.WaitAsync();
        try
        {
            if (_isConnected) return;
            _cancellationTokenSource = new CancellationTokenSource();

            _liveComms = new ChilloutVRLiveCommunicator(credentialsStorage, _callerInAppIdentifier);
            _liveComms.OnLiveUpdateReceived += async update =>
            {
                 //XYVRLogging.WriteLine(this, $"OnLiveUpdateReceived: {JsonConvert.SerializeObject(update)}");
                await monitoring.MergeUser(update);
            };
            _liveComms.OnLiveSessionReceived += async session =>
            {
                _sessionsOfInterest.Add(session.inAppSessionIdentifier);
                 //XYVRLogging.WriteLine(this, $"OnLiveSessionReceived: {JsonConvert.SerializeObject(session)}");
                return await monitoring.MergeSession(session);
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
                var sessionsToUpdate = monitoring.GetAllSessions(NamedApp.ChilloutVR)
                    .Where(session => _sessionsOfInterest.Contains(session.inAppSessionIdentifier))
                    .Where(session => session.participants.Length > 0)
                    .OrderByDescending(session => session.currentAttendance ?? session.participants.Length)
                    .ToList();
                XYVRLogging.WriteLine(this, $"Requesting to refresh all sessions of our interest (total of {sessionsToUpdate.Count} sessions)");
                _liveComms.QueueUpdateInstances(sessionsToUpdate);
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

    public Task MakeGameClientJoinOrSelfInvite(string sessionId)
    {
        // TODO: Currently not implemented
        return Task.CompletedTask;
    }
}