namespace XYVR.Core;

public interface IDataCollection
{
    /// Using a data collection storage, try to rebuild account data.
    Task<List<ImmutableNonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails);
    
    Task<List<ImmutableAccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler);
    
    /// Return true if this data collector can attempt an incremental update of the given identification.
    bool CanAttemptIncrementalUpdateOn(ImmutableAccountIdentification identification);

    /// Attempt an incremental update of the given identification, which MUST have been passed to CanAttemptIncrementalUpdateOn beforehand.<br/>
    /// Attempt can fail on accounts removed by the upstream service, which is the reason this function returns nullable.<br/>
    /// Return whether it was successful.
    Task<ImmutableNonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(ImmutableAccountIdentification toTryUpdate);
}