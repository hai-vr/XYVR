using Newtonsoft.Json;
using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteLiveUpdates
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly ResoniteSignalRClient _srClient;
    
    public event LiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task LiveUpdateReceived(LiveUpdate liveUpdate);

    public ResoniteLiveUpdates(ICredentialsStorage credentialsStorage)
    {
        _credentialsStorage = credentialsStorage;
        
        _srClient = new ResoniteSignalRClient();
        _srClient.OnStatusUpdate += WhenStatusUpdate;
        _srClient.OnReconnected += async () =>
        {
            await _srClient.SubmitRequestStatus();
        };
    }

    public async Task Connect()
    {
        await _srClient.StartAsync((await GetToken__sensitive())!);
        await _srClient.SubmitRequestStatus();
    }
    
    public async Task Disconnect()
    {
        await _srClient.StopAsync();
    }

    private async Task WhenStatusUpdate(UserStatusUpdate statusUpdate)
    {
        var status = ParseOnlineStatus(statusUpdate.onlineStatus);
        var liveSessionState = DeriveSessionState(statusUpdate, status);

        if (OnLiveUpdateReceived != null)
        {
            await OnLiveUpdateReceived(new LiveUpdate
            {
                namedApp = NamedApp.Resonite,
                qualifiedAppName = ResoniteCommunicator.ResoniteQualifiedAppName,
                inAppIdentifier = statusUpdate.userId,
                onlineStatus = status,
                mainSession = liveSessionState
            });
        }
    }

    private static LiveSessionState DeriveSessionState(UserStatusUpdate statusUpdate, OnlineStatus status)
    {
        if (status == OnlineStatus.Offline)
        {
            return new LiveSessionState
            {
                knowledge = LiveSessionKnowledge.Indeterminate,
                knownSession = null
            };
        }

        return new LiveSessionState
        {
            knowledge = LiveSessionKnowledge.Known,
            knownSession = new LiveKnownSession
            {
                inAppSessionIdentifier = statusUpdate.userSessionId
            }
        };
    }

    private OnlineStatus ParseOnlineStatus(string onlineStatus)
    {
        return onlineStatus switch
        {
            "Offline" => OnlineStatus.Offline,
            "Online" => OnlineStatus.Online,
            "Away" => OnlineStatus.Away,
            "Busy" => OnlineStatus.Busy,
            "Sociable" => OnlineStatus.Sociable,
            _ => OnlineStatus.Indeterminate
        };
    }

    private async Task<ResAuthenticationStorage?> GetToken__sensitive()
    {
        return JsonConvert.DeserializeObject<ResAuthenticationStorage>(await _credentialsStorage.RequireCookieOrToken());
    }
}