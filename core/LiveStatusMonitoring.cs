namespace XYVR.Core;

public class LiveStatusMonitoring
{
    private readonly Dictionary<NamedApp,
        Dictionary<string, LiveUpdate>
    > _liveUpdates = new();
    
    private event LiveUpdateMerged? OnLiveUpdateMerged;
    public delegate Task LiveUpdateMerged(LiveUpdate liveUpdate);

    public LiveStatusMonitoring()
    {
        foreach (var namedApp in Enum.GetValues<NamedApp>())
        {
            _liveUpdates[namedApp] = new Dictionary<string, LiveUpdate>();
        }
    }

    public List<LiveUpdate> GetAll(NamedApp namedApp)
    {
        return _liveUpdates[namedApp].Values.ToList();
    }

    public List<LiveUpdate> GetAll()
    {
        return _liveUpdates.Values.SelectMany(it => it.Values).ToList();
    }
    
    public async Task Merge(LiveUpdate liveUpdate)
    {
        // TODO:
        // It is possible to have the same inAppIdentifier being updated with a different status,
        // if the caller has multiple connections on the same app.
        // For example, if someone's status is set to invisible, it may be possible that the status
        // of that account is shown as Offline on one connection, and Online/InSameInstance for another connection.
        // In that case, we may need to avoid deduplicating status by inAppIdentifier alone,
        // and also use the callerInAppIdentifier, to know where we got the status from.
        // The UI side or BFF would have to decide what status to associate with that account.
        _liveUpdates[liveUpdate.namedApp][liveUpdate.inAppIdentifier] = liveUpdate;

        if (OnLiveUpdateMerged != null)
        {
            await OnLiveUpdateMerged.Invoke(liveUpdate);
        }
    }

    public void AddMergeListener(LiveUpdateMerged listener)
    {
        OnLiveUpdateMerged -= listener;
        OnLiveUpdateMerged += listener;
    }

    public void RemoveListener(LiveUpdateMerged listener)
    {
        OnLiveUpdateMerged -= listener;
    }

    public LiveUpdate? GetLiveSessionStateOrNull(NamedApp accountNamedApp, string accountInAppIdentifier)
    {
        return _liveUpdates[accountNamedApp].GetValueOrDefault(accountInAppIdentifier);
    }
}