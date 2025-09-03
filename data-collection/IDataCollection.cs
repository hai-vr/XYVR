using XYVR.Core;

namespace XYVR.Data.Collection;

public interface IDataCollection
{
    /// Collects all accounts from upstream but only returns accounts that have not been discovered yet.<br/>
    /// This does not return information about existing accounts, even if those existing accounts had been modified upstream.<br/>
    /// Internally, this lists all current friends and notes but only retrieves and returns the full account information of IDs that aren't in the repository yet.<br/>
    /// <br/>
    /// This does not modify the repository.
    Task<List<Account>> CollectAllUndiscoveredAccounts();
    
    /// Collects all accounts from upstream and returns only those accounts.<br/>
    /// Internally, this lists all current friends and notes but and retrieves all of those.<br/>
    /// <br/>
    /// This does not modify the repository.
    Task<List<Account>> CollectReturnedAccounts();

    /// Collects all accounts that are in the repository. This can include accounts that are not related to the caller.<br/>
    /// This does return new accounts, even some calls have implied the existence of new accounts.<br/>
    /// This can return fewer accounts than there actually are in the repository if some accounts were removed upstream.<br/>
    /// <br/>
    /// This does not modify the repository.
    Task<List<Account>> CollectExistingAccounts();

    /// Using a data collection storage, try to rebuild account data.
    Task<List<Account>> RebuildFromDataCollectionStorage(List<DataCollectionTrail> trails);
}