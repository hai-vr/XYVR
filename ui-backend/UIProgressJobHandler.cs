using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.Backend;

public class UIProgressJobHandler(IndividualRepository repository, Func<ImmutableIndividual, Task> individualUpdatedEventFn) : IIncrementalDataCollectionJobHandler
{
    private int _prevEnumTotalCount;

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

    public async Task<IncrementalEnumerationTracker> NewEnumerationTracker()
    {
        XYVRLogging.WriteLine(this, "Saving repository...");
        await Scaffolding.SaveRepository(repository);
        
        return new IncrementalEnumerationTracker();
    }

    public async Task NotifyEnumeration(IncrementalEnumerationTracker tracker, int enumerationAccomplished, int enumerationTotalCount_canBeZero)
    {
        if (_prevEnumTotalCount != enumerationTotalCount_canBeZero)
        {
            _prevEnumTotalCount = enumerationTotalCount_canBeZero;
            if (enumerationTotalCount_canBeZero != 0 && enumerationTotalCount_canBeZero % 100 == 0)
            {
                XYVRLogging.WriteLine(this, "Saving repository...");
                await Scaffolding.SaveRepository(repository);
            }
        }
        
        if (enumerationAccomplished != 0 && enumerationAccomplished % 100 == 0)
        {
            XYVRLogging.WriteLine(this, "Saving repository...");
            await Scaffolding.SaveRepository(repository);
        }
        
        XYVRLogging.WriteLine(this, $"Progress: {enumerationAccomplished} / {enumerationTotalCount_canBeZero}");
    }

    public Task NotifyProspective(IncrementalEnumerationTracker tracker)
    {
        return Task.CompletedTask;
    }
}