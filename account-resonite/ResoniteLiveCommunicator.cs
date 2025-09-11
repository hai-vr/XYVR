using System.Text.RegularExpressions;
using Newtonsoft.Json;
using XYVR.API.Resonite;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.AccountAuthority.Resonite;

public partial class ResoniteLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;
    private readonly string _uid;
    private readonly IResponseCollector _responseCollector;
    private readonly ResoniteSignalRClient _srClient;
    private readonly Dictionary<string, SessionUpdateJsonObject> _sessionIdToSessionUpdate = new();
    private readonly Dictionary<string, string> _resolvedMixedHashToSessionId = new();
    private readonly HashSet<string> _sessionIdsToWatch = new();

    private ResoniteAPI? _api;

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
        
        _api ??= await InitializeApi();
        var sessionsTemp = await _api.GetSessions__Temp(DataCollectionReason.CollectSessionLocationInformation);
        Console.WriteLine(sessionsTemp);
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
        var anySessionUpdated = false;
        
        // FIXME: Resonite sends a massive amount of session updates objects per second. This needs to be restricted further
        if (_sessionIdToSessionUpdate.TryAdd(sessionUpdate.sessionId, sessionUpdate))
        {
            Console.WriteLine($"Storing for the first time information about {sessionUpdate.sessionId}, which is {sessionUpdate.name}");
            anySessionUpdated = true;
        }
        else
        {
            if (_sessionIdsToWatch.Contains(sessionUpdate.sessionId))
            {
                Console.WriteLine($"Updating information about a session we actually care about: {sessionUpdate.sessionId}, which is {sessionUpdate.name}");
                _sessionIdToSessionUpdate[sessionUpdate.sessionId] = sessionUpdate;
                anySessionUpdated = true;
            }
        }

        if (anySessionUpdated)
        {
            // TODO: Reemit information about the users that pertains to this session.
        }

        return Task.CompletedTask;
    }

    private async Task<LiveUserSessionState> DeriveSessionState(UserStatusUpdate userStatusUpdate, OnlineStatus status)
    {
        if (status == OnlineStatus.Offline)
        {
            return new LiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.Indeterminate,
                knownSession = null
            };
        }

        var session = SessionOrNull(userStatusUpdate);

        string? worldName;
        if (session != null && userStatusUpdate.hashSalt != null && !session.sessionHidden && session.accessLevel != "Private")
        {
            SessionUpdateJsonObject? sessionUpdate;
            
            var existingSessionId = _resolvedMixedHashToSessionId.GetValueOrDefault(session.sessionHash);
            if (existingSessionId != null)
            {
                sessionUpdate = _sessionIdToSessionUpdate.GetValueOrDefault(existingSessionId);
            }
            else
            {
                sessionUpdate = await FindSessionOrNull(session.sessionHash, userStatusUpdate.hashSalt);
                if (sessionUpdate != null)
                {
                    _resolvedMixedHashToSessionId[session.sessionHash] = sessionUpdate.sessionId;
                    _sessionIdsToWatch.Add(sessionUpdate.sessionId);
                }
            }
            worldName = sessionUpdate != null ? ExtractTextFromColorTags(sessionUpdate.name) : null;
        }
        else
        {
            worldName = null;
        }

        return session != null
            ? new LiveUserSessionState
            {
                knowledge = session.accessLevel == "Private" ? LiveUserSessionKnowledge.PrivateSession : LiveUserSessionKnowledge.Known,
                knownSession = new LiveUserKnownSession
                {
                    inAppSessionIdentifier = session.sessionHash,
                    inAppHost = session is { isHost: true }
                        ? new LiveSessionHost
                        {
                            inAppHostIdentifier = userStatusUpdate.userId,
                        }
                        : null,
                    inAppVirtualSpaceName = worldName
                }
            }
            : new LiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.KnownButNoData,
                knownSession = null
            };
    }

    private async Task<SessionUpdateJsonObject?> FindSessionOrNull(string wantedHash, string hashSalt)
    {
        foreach (var sessionUpdateJsonObject in _sessionIdToSessionUpdate)
        {
            var sessionId = sessionUpdateJsonObject.Key;
            var possibleHash = await ResoniteHash.Rehash(sessionId, hashSalt);
            if (wantedHash == possibleHash)
            {
                Console.WriteLine($"We FOUND the hash.");
                return sessionUpdateJsonObject.Value;
            }
        }

        return null;
    }

    private static Session? SessionOrNull(UserStatusUpdate statusUpdate)
    {
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

        return session;
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