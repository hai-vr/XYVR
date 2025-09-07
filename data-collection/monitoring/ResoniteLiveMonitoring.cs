using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.AccountAuthority.Resonite;
using XYVR.Core;

namespace XYVR.Data.Collection.monitoring;

public class ResoniteLiveMonitoring : ILiveMonitoring, IDisposable
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly LiveStatusMonitoring _monitoring;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _isConnected;
    private string? _callerInAppIdentifier;
    
    private ResoniteLiveCommunicator? _liveComms;

    public ResoniteLiveMonitoring(ICredentialsStorage credentialsStorage, LiveStatusMonitoring monitoring)
    {
        _credentialsStorage = credentialsStorage;
        _monitoring = monitoring;
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
            _liveComms = new ResoniteLiveCommunicator(_credentialsStorage, _callerInAppIdentifier);
            
            var serializer = new JsonSerializerSettings()
            {
                Converters = { new StringEnumConverter() }
            };
            
            var alreadyListeningTo = new HashSet<string>();
            _liveComms.OnLiveUpdateReceived += async update =>
            {
                Console.WriteLine($"OnLiveUpdateReceived: {JsonConvert.SerializeObject(update, serializer)}");
                await _monitoring.Merge(update);

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