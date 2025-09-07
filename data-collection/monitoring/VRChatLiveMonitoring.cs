using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

namespace XYVR.Data.Collection.monitoring;

public class VRChatLiveMonitoring : ILiveMonitoring
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly LiveStatusMonitoring _monitoring;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    private string _callerInAppIdentifier;
    private bool _isConnected;
    private VRChatLiveCommunicator _liveComms;

    public VRChatLiveMonitoring(ICredentialsStorage credentialsStorage, LiveStatusMonitoring monitoring)
    {
        _credentialsStorage = credentialsStorage;
        _monitoring = monitoring;
    }

    public async Task StartMonitoring()
    {
        if (_callerInAppIdentifier == null) throw new InvalidOperationException("Caller must be defined to start monitoring");
        await _operationLock.WaitAsync();
        try
        {
            if (_isConnected) return;
            
            var serializer = new JsonSerializerSettings()
            {
                Converters = { new StringEnumConverter() }
            };
            
            _liveComms = new VRChatLiveCommunicator(_credentialsStorage, _callerInAppIdentifier, new DoNotStoreAnythingStorage());
            _liveComms.OnLiveUpdateReceived += async update =>
            {
                Console.WriteLine($"OnLiveUpdateReceived: {JsonConvert.SerializeObject(update, serializer)}");
                await _monitoring.Merge(update);
            };
            await _liveComms.Connect();
            
            _isConnected = true;
        }
        finally
        {
            _operationLock.Release();
        }
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