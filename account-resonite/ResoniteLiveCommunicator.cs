using System.Text.RegularExpressions;
using Newtonsoft.Json;
using XYVR.API.Resonite;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

public partial class ResoniteLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;
    private readonly string _uid;
    private readonly IResponseCollector _responseCollector;
    private readonly ResoniteSignalRClient _srClient;
    private readonly Dictionary<string, SessionUpdateJsonObject> _sessionIdToSessionUpdate = new();
    
    private ResoniteAPI _api;

    public event LiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task LiveUpdateReceived(LiveUserUpdate liveUpdate);
    
    public event Action OnReconnected;

    public ResoniteLiveCommunicator(ICredentialsStorage credentialsStorage, string callerInAppIdentifier, string uid__sensitive, IResponseCollector responseCollector)
    {
        _credentialsStorage = credentialsStorage;
        _callerInAppIdentifier = callerInAppIdentifier;
        _uid = uid__sensitive;
        _responseCollector = responseCollector;

        _srClient = new ResoniteSignalRClient();
        _srClient.OnStatusUpdate += WhenStatusUpdate;
        _srClient.OnSessionUpdate += WhenSessionUpdate;
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
        var liveSessionState = await DeriveSessionState(statusUpdate, status);

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

    private Task WhenSessionUpdate(SessionUpdateJsonObject sessionUpdate)
    {
        // FIXME: Resonite sends a massive amount of session updates objects per second. This needs to be restricted further
        _sessionIdToSessionUpdate.TryAdd(sessionUpdate.hostUserSessionId, sessionUpdate);
        return Task.CompletedTask;
    }

    private async Task<LiveUserSessionState> DeriveSessionState(UserStatusUpdate statusUpdate, OnlineStatus status)
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

        string? worldName;
        if (_sessionIdToSessionUpdate.TryGetValue(statusUpdate.userSessionId, out var sessionUpdate))
        {
            worldName = ExtractTextFromColorTags(sessionUpdate.name);
        }
        else
        {
            worldName = null;
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
                } : null,
                inAppVirtualSpaceName = worldName
            }
        };
    }

    private static string ExtractTextFromColorTags(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var withoutOpenTags = R0().Replace(input, "");
        var withoutCloseTags = R1().Replace(withoutOpenTags, "");
        var cleanedUp = R2().Replace(withoutCloseTags, "");
        
        return cleanedUp;
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

    private async Task<ResoniteAPI> InitializeApi()
    {
        var api = new ResoniteAPI(XYVRGuids.ForResoniteMachineId(), _uid, _responseCollector);

        var userAndToken__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (userAndToken__sensitive == null)
        {
            // TODO: Check token expiration
            throw new ArgumentException("User must have already logged in before establishing communication");
        }
        
        api.ProvideUserAndToken(userAndToken__sensitive);
        
        return api;
    }

    [GeneratedRegex(@"<color[^>]*>", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex R0();
    [GeneratedRegex(@"</color>", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex R1();
    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex R2();
}