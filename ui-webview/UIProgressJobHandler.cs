using XYVR.Core;
using XYVR.Scaffold;

namespace XYVR.UI.WebviewUI;

public class UIProgressJobHandler(IndividualRepository repository, Func<Individual, Task> individualUpdatedEventFn) : IIncrementalDataCollectionJobHandler
{
    private int _prevEnumTotalCount;

    public async Task NotifyAccountUpdated(List<AccountIdentification> increment)
    {
        Console.WriteLine($"Updated the following {increment.Count} accounts: {string.Join(", ", increment)}");
        foreach (var accountIdentification in increment)
        {
            Console.WriteLine($"Getting account to send to front...: {accountIdentification}");
            var individual = repository.GetIndividualByAccount(accountIdentification);
            await individualUpdatedEventFn.Invoke(individual);
        }
    }

    public async Task<IncrementalEnumerationTracker> NewEnumerationTracker()
    {
        Console.WriteLine("Saving repository...");
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
                Console.WriteLine("Saving repository...");
                await Scaffolding.SaveRepository(repository);
            }
        }
        
        if (enumerationAccomplished != 0 && enumerationAccomplished % 100 == 0)
        {
            Console.WriteLine("Saving repository...");
            await Scaffolding.SaveRepository(repository);
        }
        
        Console.WriteLine($"Progress: {enumerationAccomplished} / {enumerationTotalCount_canBeZero}");
    }

    public Task NotifyProspective(IncrementalEnumerationTracker tracker)
    {
        return Task.CompletedTask;
    }
}