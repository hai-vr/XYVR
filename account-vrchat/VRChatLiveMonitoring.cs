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
            
            var serializer = new JsonSerializerSettings()
            {
                Converters = { new StringEnumConverter() }
            };
            
            _liveComms = new VRChatLiveCommunicator(_credentialsStorage, _callerInAppIdentifier, new DoNotStoreAnythingStorage(), _worldNameCache);
            _liveComms.OnLiveUpdateReceived += async update =>
            {
                Console.WriteLine($"OnLiveUpdateReceived: {JsonConvert.SerializeObject(update, serializer)}");
                await _monitoring.MergeUser(update);
            };
            _liveComms.OnLiveSessionReceived += async session =>
            {
                Console.WriteLine($"OnLiveSessionReceived: {JsonConvert.SerializeObject(session, serializer)}");
                return await _monitoring.MergeSession(session);
            };
            _liveComms.OnWorldCached += async world =>
            {
                Console.WriteLine($"OnWorldCached: {JsonConvert.SerializeObject(world, serializer)}");

                var allSessionsOnThisWorld = _monitoring.GetAllSessions(NamedApp.VRChat)
                    .Where(session => session.inAppSessionIdentifier.StartsWith(world.worldId))
                    .ToList();
                
                foreach (var sessionOnThisWorld in allSessionsOnThisWorld)
                {
                    await _monitoring.MergeSession(new ImmutableNonIndexedLiveSession
                    {
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppSessionIdentifier = sessionOnThisWorld.inAppSessionIdentifier,
                        inAppVirtualSpaceName = world.name,
                        virtualSpaceDefaultCapacity = world.capacity,
                    });
                }
            };
            await _liveComms.Connect();
            
            _isConnected = true;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private string? TryLocationToSessionGuid(string location)
    {
        return _monitoring.GetAllSessions(NamedApp.VRChat)
            .FirstOrDefault(session => session.inAppSessionIdentifier == location)?.guid;
    }

    public async Task StopMonitoring()
    {
        await _operationLock.WaitAsync();
        try
        {
            if (!_isConnected) return;
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