using XYVR.Core;

namespace XYVR.Data.Collection;

public class CompoundDataCollection(IndividualRepository repository, List<IDataCollection> collectors) : IDataCollection
{
    private readonly List<IDataCollection> _collectors = collectors.ToList();

    public async Task<List<NonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
    {
        var results = new List<NonIndexedAccount>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.RebuildFromDataCollectionStorage(trails));
        }
        
        return results;
    }

    public async Task<List<AccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler)
    {
        var updatedSoFar = new List<AccountIdentification>();

        foreach (var dataCollection in _collectors)
        {
            updatedSoFar.AddRange(await dataCollection.IncrementalUpdateRepository(jobHandler));
        }

        var notUpdated = repository.Individuals
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
                        repository.MergeAccounts([result]);
                        await jobHandler.NotifyAccountUpdated([toTryUpdate]);
                        
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

    public async Task<NonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(AccountIdentification notUpdatedIdentification)
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