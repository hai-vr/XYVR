namespace XYVR.Core;

public interface IIncrementalDataCollectionJobHandler
{
    public Task NotifyAccountUpdated(List<AccountIdentification> increment);
    public Task<IncrementalEnumerationTracker> NewEnumerationTracker();
    public Task NotifyEnumeration(IncrementalEnumerationTracker tracker, int enumerationAccomplished, int enumerationTotalCount_canBeZero);
    public Task NotifyProspective(IncrementalEnumerationTracker tracker);
}

public class IncrementalEnumerationTracker {}