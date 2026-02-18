namespace XYVR.Core;

// TODO: document this interface and it's methods.
public interface IIncrementalDataCollectionJobHandler
{
    public Task NotifyAccountUpdated(List<ImmutableAccountIdentification> increment);
    public Task<IncrementalEnumerationTracker> NewEnumerationTracker(string name);
    public Task NotifyEnumeration(IncrementalEnumerationTracker tracker, bool shouldSave);
    public Task NotifyProspective(IncrementalEnumerationTracker tracker);
}

public class IncrementalEnumerationTracker(IIncrementalDataCollectionJobHandler handler, string name)
{
    public string Name => name;
    public int TotalCount { get; private set; }
    public int AccomplishedCount { get; private set; }

    private int _lastTotalCount;
    private int _lastAccomplishedCount;

    public async Task Update(int accomplishedCount, int totalCount_canBeZero)
    {
        AccomplishedCount = accomplishedCount;
        TotalCount = totalCount_canBeZero;
        var shouldSave = false;

        if (TotalCount - _lastTotalCount >= 100)
        {
            _lastTotalCount = TotalCount;
            shouldSave = true;
        }
        if(AccomplishedCount - _lastAccomplishedCount >= 100)
        {
            _lastAccomplishedCount = AccomplishedCount;
            shouldSave = true;
        }

        await handler.NotifyEnumeration(this, shouldSave);
    }
}