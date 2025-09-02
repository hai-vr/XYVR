using XYVR.Core;

namespace XYVR.Data.Collection;

public class CompoundDataCollection : IDataCollection
{
    private readonly List<IDataCollection> _collectors;

    public CompoundDataCollection(List<IDataCollection> collectors)
    {
        _collectors = collectors.ToList();
    }

    public async Task<List<Account>> CollectAllUndiscoveredAccounts()
    {
        var results = new List<Account>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.CollectAllUndiscoveredAccounts());
        }

        return results;
    }

    public async Task<List<Account>> CollectExistingAccounts()
    {
        var results = new List<Account>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.CollectExistingAccounts());
        }

        return results;
    }

    public async Task<List<Account>> RebuildFromDataCollectionStorage(List<DataCollectionTrail> trails)
    {
        var results = new List<Account>();
        
        foreach (var dataCollection in _collectors)
        {
            results.AddRange(await dataCollection.RebuildFromDataCollectionStorage(trails));
        }
        
        return results;
    }
}