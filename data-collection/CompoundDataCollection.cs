using XYVR.Core;

namespace XYVR.Data.Collection;

public class CompoundDataCollection(IndividualRepository repository, List<IDataCollection> collectors) : IDataCollection
{
    private readonly IndividualRepository _repository = repository;
    private readonly List<IDataCollection> _collectors = collectors.ToList();

    public async Task<List<Account>> CollectAllUndiscoveredAccounts()
    {
        var results = new List<Account>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.CollectAllUndiscoveredAccounts());
        }

        return results;
    }

    public async Task<List<Account>> CollectReturnedAccounts()
    {
        var results = new List<Account>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.CollectReturnedAccounts());
        }

        return results;
    }

    public async Task<List<Account>> CollectExistingAccounts()
    {
        // TODO: We need special handling when two collectors are of the same type so that we don't iterate on both.
        
        var results = new List<Account>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.CollectExistingAccounts());
        }

        return results;
    }

    public async Task<List<Account>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
    {
        var results = new List<Account>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.RebuildFromDataCollectionStorage(trails));
        }
        
        return results;
    }

    public async Task<List<AccountIdentification>> IncrementalUpdateRepository(Func<List<AccountIdentification>, Task> incrementFn)
    {
        var updatedSoFar = new List<AccountIdentification>();

        foreach (var dataCollection in _collectors)
        {
            updatedSoFar.AddRange(await dataCollection.IncrementalUpdateRepository(incrementFn));
        }

        var notUpdated = _repository.Individuals
            .SelectMany(individual => individual.accounts.Select(account => account.AsIdentification()))
            .ToHashSet();
        notUpdated.ExceptWith(updatedSoFar);

        foreach (var toTryUpdate in notUpdated)
        {
            foreach (var collector in _collectors)
            {
                if (collector.CanAttemptIncrementalUpdateOn(toTryUpdate))
                {
                    var result = await collector.TryGetForIncrementalUpdate__Flawed__NonContactOnly(toTryUpdate);
                    if (result != null)
                    {
                        _repository.MergeAccounts([result]);
                        await incrementFn([toTryUpdate]);
                        
                        updatedSoFar.Add(toTryUpdate);
                        
                        break;
                    }
                }
            }
        }

        return updatedSoFar;
    }

    public bool CanAttemptIncrementalUpdateOn(AccountIdentification identification)
    {
        return _collectors.Any(collection => collection.CanAttemptIncrementalUpdateOn(identification));
    }

    public async Task<Account?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(AccountIdentification notUpdatedIdentification)
    {
        foreach (var collector in _collectors)
        {
            if (collector.CanAttemptIncrementalUpdateOn(notUpdatedIdentification))
            {
                var tryUpdate = await collector.TryGetForIncrementalUpdate__Flawed__NonContactOnly(notUpdatedIdentification);
                if (tryUpdate != null) return tryUpdate;
            }
        }

        return null;
    }
}