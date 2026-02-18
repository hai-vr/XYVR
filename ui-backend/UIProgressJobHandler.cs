using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.Backend;

public class UIProgressJobHandler(IndividualRepository repository, Func<ImmutableIndividual, Task> individualUpdatedEventFn, Func<IncrementalEnumerationTracker, Task> enumerationUpdatedFn) : IIncrementalDataCollectionJobHandler
{
    public async Task NotifyAccountUpdated(List<ImmutableAccountIdentification> increment)
    {
        XYVRLogging.WriteLine(this, $"Updated the following {increment.Count} accounts: {string.Join(", ", increment)}");
        foreach (var accountIdentification in increment)
        {
            XYVRLogging.WriteLine(this, $"Getting account to send to front...: {accountIdentification}");
            var individual = repository.GetIndividualByAccount(accountIdentification);
            await individualUpdatedEventFn.Invoke(individual);
        }
    }

    public async Task<IncrementalEnumerationTracker> NewEnumerationTracker(string name)
    {
        XYVRLogging.WriteLine(this, "Saving repository...");
        await Scaffolding.SaveRepository(repository);
        
        return new IncrementalEnumerationTracker(this, name);
    }

    public async Task NotifyEnumeration(IncrementalEnumerationTracker tracker, bool shouldSave)
    {
        if(shouldSave)
        {
            XYVRLogging.WriteLine(this, "Saving repository...");
            await Scaffolding.SaveRepository(repository);
        }
        
        XYVRLogging.WriteLine(this, $"Progress for '{tracker.Name}': {tracker.AccomplishedCount} / {tracker.TotalCount}");
        await enumerationUpdatedFn.Invoke(tracker);
    }

    public async Task NotifyProspective(IncrementalEnumerationTracker tracker)
    {
        await enumerationUpdatedFn.Invoke(tracker);
    }
}