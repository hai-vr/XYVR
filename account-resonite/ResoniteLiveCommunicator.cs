using Newtonsoft.Json;
using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;
    private readonly ResoniteSignalRClient _srClient;
    
    public event LiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task LiveUpdateReceived(LiveUserUpdate liveUpdate);
    
    public event Action OnReconnected;

    public ResoniteLiveCommunicator(ICredentialsStorage credentialsStorage, string callerInAppIdentifier)
    {
        _credentialsStorage = credentialsStorage;
        _callerInAppIdentifier = callerInAppIdentifier;

        _srClient = new ResoniteSignalRClient();
        _srClient.OnStatusUpdate += WhenStatusUpdate;
        _srClient.OnReconnected += async () =>
        {
            await _srClient.SubmitRequestStatus();
            OnReconnected?.Invoke();
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
            await OnLiveUpdateReceived(new LiveUserUpdate
            {
                trigger = "SignalR-OnStatusUpdate",
                namedApp = NamedApp.Resonite,
                qualifiedAppName = ResoniteCommunicator.ResoniteQualifiedAppName,
                inAppIdentifier = statusUpdate.userId,
                onlineStatus = status,
                mainSession = liveSessionState,
                callerInAppIdentifier = _callerInAppIdentifier
            });
        }
    }

    private static LiveUserSessionState DeriveSessionState(UserStatusUpdate statusUpdate, OnlineStatus status)
    {
        if (status == OnlineStatus.Offline)
        {
            return new LiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.Indeterminate,
                knownSession = null
            };
        }

        Session? session;
        var index = statusUpdate.currentSessionIndex;
        if (index >= 0 && index < statusUpdate.sessions.Count)
        {
            session = statusUpdate.sessions[index];
        }
        else
        {
            session = null;
        }
        
        return new LiveUserSessionState
        {
            knowledge = LiveUserSessionKnowledge.Known,
            knownSession = new LiveUserKnownSession
            {
                inAppSessionIdentifier = statusUpdate.userSessionId,
                inAppHost = session is { isHost: true } ? new LiveSessionHost
                {
                    inAppHostIdentifier = statusUpdate.userId,
                } : null
            }
        };
    }

    private OnlineStatus ParseOnlineStatus(string onlineStatus)
    {
        return onlineStatus switch
        {
            "Offline" => OnlineStatus.Offline,
            "Online" => OnlineStatus.Online,
            "Away" => OnlineStatus.ResoniteAway,
            "Busy" => OnlineStatus.ResoniteBusy,
            "Sociable" => OnlineStatus.ResoniteSociable,
            _ => OnlineStatus.Indeterminate
        };
    }

    private async Task<ResAuthenticationStorage?> GetToken__sensitive()
    {
        return JsonConvert.DeserializeObject<ResAuthenticationStorage>(await _credentialsStorage.RequireCookieOrToken());
    }

    public async Task RequestFullUpdate()
    {
        await _srClient.SubmitRequestStatus();
    }

    public async Task RequestPartialUpdate(List<string> userIds)
    {
        foreach (var userId in userIds)
        {
            await _srClient.SubmitRequestStatus(userId);
        }
    }

    public async Task ListenOnContact(string userId)
    {
        await _srClient.ListenOnContact(userId);
    }
}