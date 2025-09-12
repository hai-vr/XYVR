namespace XYVR.Core;

public class LiveStatusMonitoring
{
    private readonly Dictionary<NamedApp, Dictionary<string, ImmutableLiveUserUpdate>> _liveUpdatesByAppByUser = new();
    private readonly List<LiveSession> _sessions = new();
    private readonly Dictionary<string, LiveSession> _guidToSession = new();
    private readonly Dictionary<NamedApp, Dictionary<string, LiveSession>> _namedAppToInAppIdToSession = new();

    private event LiveUserUpdateMerged? OnLiveUserUpdateMerged;
    public delegate Task LiveUserUpdateMerged(ImmutableLiveUserUpdate liveUpdate);

    private event LiveSessionUpdated? OnLiveSessionUpdated;
    public delegate Task LiveSessionUpdated(LiveSession session);

    public LiveStatusMonitoring()
    {
        foreach (var namedApp in Enum.GetValues<NamedApp>())
        {
            _liveUpdatesByAppByUser[namedApp] = new Dictionary<string, ImmutableLiveUserUpdate>();
            _namedAppToInAppIdToSession[namedApp] = new Dictionary<string, LiveSession>();
        }
    }

    public List<ImmutableLiveUserUpdate> GetAll(NamedApp namedApp)
    {
        return _liveUpdatesByAppByUser[namedApp].Values.ToList();
    }

    public List<ImmutableLiveUserUpdate> GetAll()
    {
        return _liveUpdatesByAppByUser.Values.SelectMany(it => it.Values).ToList();
    }
    
    public async Task MergeUser(ImmutableLiveUserUpdate liveUpdate)
    {
        bool liveUpdateWasChanged;
        
        // TODO:
        // It is possible to have the same inAppIdentifier being updated with a different status,
        // if the caller has multiple connections on the same app.
        // For example, if someone's status is set to invisible, it may be possible that the status
        // of that account is shown as Offline on one connection, and Online/InSameInstance for another connection.
        // In that case, we may need to avoid deduplicating status by inAppIdentifier alone,
        // and also use the callerInAppIdentifier, to know where we got the status from.
        // The UI side or BFF would have to decide what status to associate with that account.
        if (_liveUpdatesByAppByUser[liveUpdate.namedApp].TryGetValue(liveUpdate.inAppIdentifier, out var existingLiveUpdate))
        {
            var modifiedRecord = existingLiveUpdate;
            
            if (liveUpdate.onlineStatus != null) modifiedRecord = modifiedRecord with { onlineStatus = liveUpdate.onlineStatus };
            if (liveUpdate.customStatus != null) modifiedRecord = modifiedRecord with { customStatus = liveUpdate.customStatus };
            if (liveUpdate.mainSession != null) modifiedRecord = modifiedRecord with { mainSession = liveUpdate.mainSession };

            // Did anything actually change?
            if (modifiedRecord != existingLiveUpdate)
            {
                // The trigger is only relevant if an event actually causes any content to change
                modifiedRecord = modifiedRecord with { trigger = liveUpdate.trigger };
                
                _liveUpdatesByAppByUser[liveUpdate.namedApp][liveUpdate.inAppIdentifier] = modifiedRecord;
                liveUpdateWasChanged = true;
            }
            else
            {
                Console.WriteLine($"A LiveUpdate on {existingLiveUpdate.inAppIdentifier} has resulted in no change (triggered by {liveUpdate.trigger}, there will be no OnLiveUserUpdateMerged emitted.");
                liveUpdateWasChanged = false;
            }
        }
        else
        {
            _liveUpdatesByAppByUser[liveUpdate.namedApp][liveUpdate.inAppIdentifier] = liveUpdate;
            liveUpdateWasChanged = true;
        }

        LiveSession? liveSession = null;
        if (liveUpdate.mainSession is { knowledge: LiveUserSessionKnowledge.Known })
        {
            var userKnownSession = liveUpdate.mainSession.knownSession!;
            var nonIndexedSession = new NonIndexedLiveSession
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
            liveSession = InternalMergeSessionAndGet(nonIndexedSession);
        }

        // We want to send the updates in a preferred order:
        // - Session first, so that the receiver side may register that session.
        // - User next, so that the receiver side may have a better luck associating the user with that session.
        // This isn't required.
        if (liveSession != null && OnLiveSessionUpdated != null)
        {
            await OnLiveSessionUpdated.Invoke(liveSession);
        }
        if (liveUpdateWasChanged && OnLiveUserUpdateMerged != null)
        {
            await OnLiveUserUpdateMerged.Invoke(liveUpdate);
        }
    }

    public async Task MergeSession(NonIndexedLiveSession inputSession)
    {
        var liveSession = InternalMergeSessionAndGet(inputSession);
        
        if (OnLiveSessionUpdated != null)
        {
            await OnLiveSessionUpdated.Invoke(liveSession);
        }
    }

    private LiveSession InternalMergeSessionAndGet(NonIndexedLiveSession inputSession)
    {
        if (_namedAppToInAppIdToSession[inputSession.namedApp].TryGetValue(inputSession.inAppSessionIdentifier, out var existingSession))
        {
            if (inputSession.inAppSessionName != null) existingSession.inAppSessionName = inputSession.inAppSessionName;
            if (inputSession.inAppVirtualSpaceName != null) existingSession.inAppVirtualSpaceName = inputSession.inAppVirtualSpaceName;
            return existingSession;
        }
        else
        {
            var liveSession = NonIndexedLiveSession.MakeIndexed(inputSession);
            
            _sessions.Add(liveSession);
            _guidToSession[liveSession.guid] = liveSession;
            _namedAppToInAppIdToSession[liveSession.namedApp][liveSession.inAppSessionIdentifier] = liveSession;
            
            return liveSession;
        }
    }

    public void AddUserUpdateMergedListener(LiveUserUpdateMerged listener)
    {
        OnLiveUserUpdateMerged -= listener;
        OnLiveUserUpdateMerged += listener;
    }

    public void RemoveUserUpdateMergedListener(LiveUserUpdateMerged listener)
    {
        OnLiveUserUpdateMerged -= listener;
    }

    public void AddSessionUpdatedListener(LiveSessionUpdated listener)
    {
        OnLiveSessionUpdated -= listener;
        OnLiveSessionUpdated += listener;
    }

    public void RemoveSessionUpdatedListener(LiveSessionUpdated listener)
    {
        OnLiveSessionUpdated -= listener;
    }

    public ImmutableLiveUserUpdate? GetLiveSessionStateOrNull(NamedApp accountNamedApp, string accountInAppIdentifier)
    {
        return _liveUpdatesByAppByUser[accountNamedApp].GetValueOrDefault(accountInAppIdentifier);
    }
}