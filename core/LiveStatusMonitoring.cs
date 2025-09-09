namespace XYVR.Core;

public class LiveStatusMonitoring
{
    private readonly Dictionary<NamedApp, Dictionary<string, LiveUserUpdate>> _liveUpdatesByAppByUser = new();
    private readonly List<LiveSession> _sessions = new();
    private readonly Dictionary<string, LiveSession> _guidToSession = new();
    private readonly Dictionary<NamedApp, Dictionary<string, LiveSession>> _namedAppToInAppIdToSession = new();

    private event LiveUpdateMerged? OnLiveUserUpdateMerged;
    public delegate Task LiveUpdateMerged(LiveUserUpdate liveUpdate);

    public LiveStatusMonitoring()
    {
        foreach (var namedApp in Enum.GetValues<NamedApp>())
        {
            _liveUpdatesByAppByUser[namedApp] = new Dictionary<string, LiveUserUpdate>();
            _namedAppToInAppIdToSession[namedApp] = new Dictionary<string, LiveSession>();
        }
    }

    public List<LiveUserUpdate> GetAll(NamedApp namedApp)
    {
        return _liveUpdatesByAppByUser[namedApp].Values.ToList();
    }

    public List<LiveUserUpdate> GetAll()
    {
        return _liveUpdatesByAppByUser.Values.SelectMany(it => it.Values).ToList();
    }
    
    public async Task MergeUser(LiveUserUpdate liveUpdate)
    {
        // TODO:
        // It is possible to have the same inAppIdentifier being updated with a different status,
        // if the caller has multiple connections on the same app.
        // For example, if someone's status is set to invisible, it may be possible that the status
        // of that account is shown as Offline on one connection, and Online/InSameInstance for another connection.
        // In that case, we may need to avoid deduplicating status by inAppIdentifier alone,
        // and also use the callerInAppIdentifier, to know where we got the status from.
        // The UI side or BFF would have to decide what status to associate with that account.
        _liveUpdatesByAppByUser[liveUpdate.namedApp][liveUpdate.inAppIdentifier] = liveUpdate;

        if (liveUpdate.mainSession != null && liveUpdate.mainSession.knowledge == LiveSessionKnowledge.Known)
        {
            var userKnownSession = liveUpdate.mainSession.knownSession as LiveUserKnownSession;
            var session = new NonIndexedLiveSession
            {
                namedApp = liveUpdate.namedApp,
                qualifiedAppName = liveUpdate.qualifiedAppName,
                inAppSessionIdentifier = userKnownSession.inAppSessionIdentifier,
                inAppSessionName = userKnownSession.inAppSessionName,
                inAppVirtualSpaceName = userKnownSession.inAppVirtualSpaceName,
                inAppHost = userKnownSession.inAppHost != null
                    ? new LiveSessionHost
                    {
                        inAppHostDisplayName = userKnownSession.inAppHost.inAppHostDisplayName,
                        inAppHostIdentifier = userKnownSession.inAppHost.inAppHostIdentifier
                    }
                    : null,
            };
            await MergeSession(session);
        }

        if (OnLiveUserUpdateMerged != null)
        {
            await OnLiveUserUpdateMerged.Invoke(liveUpdate);
        }
    }

    public async Task MergeSession(NonIndexedLiveSession inputSession)
    {
        if (_namedAppToInAppIdToSession[inputSession.namedApp].TryGetValue(inputSession.inAppSessionIdentifier, out var existingSession))
        {
            if (inputSession.inAppSessionName != null) existingSession.inAppSessionName = inputSession.inAppSessionName;
            if (inputSession.inAppVirtualSpaceName != null) existingSession.inAppVirtualSpaceName = inputSession.inAppVirtualSpaceName;
        }
        else
        {
            var liveSession = NonIndexedLiveSession.MakeIndexed(inputSession);
            
            _sessions.Add(liveSession);
            _guidToSession[liveSession.guid] = liveSession;
            _namedAppToInAppIdToSession[liveSession.namedApp][liveSession.inAppSessionIdentifier] = liveSession;
        }
    }

    public void AddMergeListener(LiveUpdateMerged listener)
    {
        OnLiveUserUpdateMerged -= listener;
        OnLiveUserUpdateMerged += listener;
    }

    public void RemoveListener(LiveUpdateMerged listener)
    {
        OnLiveUserUpdateMerged -= listener;
    }

    public LiveUserUpdate? GetLiveSessionStateOrNull(NamedApp accountNamedApp, string accountInAppIdentifier)
    {
        return _liveUpdatesByAppByUser[accountNamedApp].GetValueOrDefault(accountInAppIdentifier);
    }
}