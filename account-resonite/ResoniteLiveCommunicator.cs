using System.Text.RegularExpressions;
using Newtonsoft.Json;
using XYVR.Core;
using System.Runtime.Caching;

namespace XYVR.AccountAuthority.Resonite;

internal partial class ResoniteLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;
    private readonly string _uid;
    private readonly IResponseCollector _responseCollector;
    private readonly Func<string, string?> _trySessionIdToSessionGuidFn;
    private readonly HashToSession _hashToSession;
    private readonly ResoniteSignalRClient _srClient;
    private readonly MemoryCache/*<string, SessionUpdateJsonObject>*/ _sessionIdToSessionUpdate = MemoryCache.Default;
    private readonly HashSet<string> _sessionIdsToWatch = new();

    private ResoniteAPI? _api;

    public event LiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task LiveUpdateReceived(ImmutableLiveUserUpdate liveUpdate);
    
    public event Action? OnReconnected;
    
    public event Action<SessionUpdateJsonObject>? OnSessionUpdated;

    public ResoniteLiveCommunicator(ICredentialsStorage credentialsStorage, string callerInAppIdentifier, string uid__sensitive, IResponseCollector responseCollector, Func<string, string?> trySessionIdToSessionGuidFn, HashToSession hashToSession)
    {
        _credentialsStorage = credentialsStorage;
        _callerInAppIdentifier = callerInAppIdentifier;
        _uid = uid__sensitive;
        _responseCollector = responseCollector;
        _trySessionIdToSessionGuidFn = trySessionIdToSessionGuidFn;
        _hashToSession = hashToSession;

        _srClient = new ResoniteSignalRClient();
        _srClient.OnStatusUpdate += WhenStatusUpdate;
        _srClient.OnSessionUpdate += WhenSessionUpdate;
        _srClient.OnReconnected += async () =>
        {
            try
            {
                await _srClient.SubmitRequestStatus();
                OnReconnected?.Invoke();
            }
            catch (Exception e)
            {
                XYVRLogging.ErrorWriteLine(this, e);
                throw;
            }
        };
    }

    public async Task Connect()
    {
        await _srClient.StartAsync((await GetToken__sensitive())!);
        await _srClient.SubmitRequestStatus();
        
        _api ??= await InitializeApi();
        // var sessionsTemp = await _api.GetSessions__Temp(DataCollectionReason.CollectSessionLocationInformation);
        // XYVRLogging.WriteLine(this, sessionsTemp);
    }
    
    public async Task Disconnect()
    {
        await _srClient.StopAsync();
    }

    private async Task WhenStatusUpdate(UserStatusUpdate statusUpdate)
    {
        try
        {
            var status = ParseOnlineStatus(statusUpdate.onlineStatus);
            var liveSessionState = await DeriveSessionState(statusUpdate, status);
        
            var session = SessionOrNull(statusUpdate);
        
            if (OnLiveUpdateReceived != null)
            {
                var sessionHashes = statusUpdate.sessions.Select(sess => sess.sessionHash).ToList();
                var guidified = statusUpdate.hashSalt != null
                    ? await ResolveSessionGuids(sessionHashes, statusUpdate.hashSalt)
                    : [];
            
                await OnLiveUpdateReceived(new ImmutableLiveUserUpdate
                {
                    trigger = "SignalR-OnStatusUpdate",
                    namedApp = NamedApp.Resonite,
                    qualifiedAppName = ResoniteCommunicator.ResoniteQualifiedAppName,
                    inAppIdentifier = statusUpdate.userId,
                    onlineStatus = status,
                    mainSession = liveSessionState,
                    sessionSpecifics = new ImmutableResoniteLiveSessionSpecifics
                    {
                        sessionHash = session?.sessionHash,
                        userHashSalt = statusUpdate.hashSalt,
                        sessionHashes = [..sessionHashes]
                    },
                    multiSessionGuids = [..guidified],
                    callerInAppIdentifier = _callerInAppIdentifier
                });
            }
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    private async Task<List<string>> ResolveSessionGuids(List<string> sessionHashes, string hashSalt)
    {
        var sessionGuids = new List<string>();
        foreach (var wantedHash in sessionHashes)
        {
            var session = await _hashToSession.ResolveSession(wantedHash, hashSalt);
            if (session != null)
            {
                sessionGuids.Add(session.sessionGuid);
            }
        }

        return sessionGuids;
    }

    private Task WhenSessionUpdate(SessionUpdateJsonObject sessionUpdate)
    {
        try
        {
            var anySessionUpdated = false;
        
            // FIXME: Resonite sends a massive amount of session updates objects per second. This needs to be restricted further
            var has = _sessionIdToSessionUpdate.Contains(sessionUpdate.sessionId);
            if (!has)
            {
                _sessionIdToSessionUpdate.Add(sessionUpdate.sessionId, sessionUpdate, new CacheItemPolicy
                {
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                });
                XYVRLogging.WriteLine(this, $"Storing for the first time information about {sessionUpdate.sessionId}, which is {ExtractTextFromColorTags(sessionUpdate.name)}");
                anySessionUpdated = true;
            }
            else
            {
                if (_sessionIdsToWatch.Contains(sessionUpdate.sessionId))
                {
                    _sessionIdToSessionUpdate[sessionUpdate.sessionId] = sessionUpdate;
                    anySessionUpdated = true;
                }
            }

            if (anySessionUpdated)
            {
                OnSessionUpdated?.Invoke(sessionUpdate);
                // TODO: Reemit information about the users that pertains to this session.
            }

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            XYVRLogging.ErrorWriteLine(this, e);
            throw;
        }
    }

    private async Task<ImmutableLiveUserSessionState> DeriveSessionState(UserStatusUpdate userStatusUpdate, OnlineStatus status)
    {
        if (status == OnlineStatus.Offline)
        {
            return new ImmutableLiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.Indeterminate,
            };
        }

        var session = SessionOrNull(userStatusUpdate);
        SessionUpdateJsonObject? sessionUpdate = null;

        if (session != null && userStatusUpdate.hashSalt != null && !session.sessionHidden && session.accessLevel != "Private")
        {
            var sess = await _hashToSession.ResolveSession(session.sessionHash, userStatusUpdate.hashSalt);
            if (sess != null)
            {
                sessionUpdate = (SessionUpdateJsonObject?)_sessionIdToSessionUpdate.Get(sess.sessionId);
                var added = _sessionIdsToWatch.Add(sess.sessionId);
                if (added && sessionUpdate != null)
                {
                    await _srClient.ListenOnKey(sessionUpdate.broadcastKey);
                }
            }
        }
        
        if (session != null && sessionUpdate != null)
        {
            if (session.accessLevel == "Private")
            {
                return new ImmutableLiveUserSessionState
                {
                    knowledge = LiveUserSessionKnowledge.PrivateSession
                };
            }
            else
            {
                var sessionGuid = _trySessionIdToSessionGuidFn(sessionUpdate.sessionId);
                if (sessionGuid != null)
                {
                    return new ImmutableLiveUserSessionState
                    {
                        knowledge = LiveUserSessionKnowledge.Known,
                        sessionGuid = sessionGuid
                    };
                }
                else
                {
                    XYVRLogging.WriteLine(this, $"We don't know the session GUID for {sessionUpdate.sessionId}");
                    return new ImmutableLiveUserSessionState
                    {
                        knowledge = LiveUserSessionKnowledge.KnownButNoData
                    };
                }
            }
        }
        else
        {
            return new ImmutableLiveUserSessionState
            {
                knowledge = LiveUserSessionKnowledge.Indeterminate
            };
        }
    }

    private async Task<SessionUpdateJsonObject?> FindSessionOrNull(string wantedHash, string hashSalt)
    {
        var resolveSession = await _hashToSession.ResolveSession(wantedHash, hashSalt);
        if (resolveSession != null)
        {
            var sessionUpdate = (SessionUpdateJsonObject?)_sessionIdToSessionUpdate[resolveSession.sessionId];
            return sessionUpdate;
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

    public static string ExtractTextFromColorTags(string input)
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

    private async Task<ResoniteAuthStorage?> GetToken__sensitive()
    {
        return JsonConvert.DeserializeObject<ResoniteAuthStorage>(await _credentialsStorage.RequireCookieOrToken());
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