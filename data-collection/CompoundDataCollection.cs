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