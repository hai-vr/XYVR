namespace XYVR.Core;

public class CompoundDataCollection(IndividualRepository repository, List<IDataCollection> collectors) : IDataCollection
{
    private readonly List<IDataCollection> _collectors = collectors.ToList();

    public async Task<List<ImmutableNonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
    {
        var results = new List<ImmutableNonIndexedAccount>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.RebuildFromDataCollectionStorage(trails));
        }
        
        return results;
    }

    public async Task<List<ImmutableAccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler)
    {
        var updatedSoFar = new List<ImmutableAccountIdentification>();

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

    public bool CanAttemptIncrementalUpdateOn(ImmutableAccountIdentification identification)
    {
        return _collectors.Any(collection => collection.CanAttemptIncrementalUpdateOn(identification));
    }

    public async Task<ImmutableNonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(ImmutableAccountIdentification notUpdatedIdentification)
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