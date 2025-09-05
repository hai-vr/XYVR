namespace XYVR.Core;

public class LiveStatusMonitoring
{
    private readonly Dictionary<NamedApp, Dictionary<string, LiveUpdate>> _liveUpdates = new();

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
    
    public void Merge(LiveUpdate liveUpdate)
    {
        _liveUpdates[liveUpdate.namedApp][liveUpdate.inAppIdentifier] = liveUpdate;
    }
}