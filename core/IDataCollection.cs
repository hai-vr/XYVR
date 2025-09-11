using XYVR.Data.Collection;

namespace XYVR.Core;

public interface IDataCollection
{
    /// Using a data collection storage, try to rebuild account data.
    Task<List<NonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails);
    
    Task<List<AccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler);
    
    /// Return true if this data collector can attempt an incremental update of the given identification.
    bool CanAttemptIncrementalUpdateOn(AccountIdentification identification);

    /// Attempt an incremental update of the given identification, which MUST have been passed to CanAttemptIncrementalUpdateOn beforehand.<br/>
    /// Attempt can fail on accounts removed by the upstream service, which is the reason this function returns nullable.<br/>
    /// Return whether it was successful.
    Task<NonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(AccountIdentification toTryUpdate);
}