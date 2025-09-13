using XYVR.Core;

namespace XYVR.AccountAuthority.ChilloutVR;

public class ChilloutVRDataCollection : IDataCollection
{
    public Task<List<ImmutableNonIndexedAccount>> RebuildFromDataCollectionStorage(List<ResponseCollectionTrail> trails)
    {
        throw new NotImplementedException();
    }

    public Task<List<ImmutableAccountIdentification>> IncrementalUpdateRepository(IIncrementalDataCollectionJobHandler jobHandler)
    {
        throw new NotImplementedException();
    }

    public bool CanAttemptIncrementalUpdateOn(ImmutableAccountIdentification identification)
    {
        throw new NotImplementedException();
    }

    public Task<ImmutableNonIndexedAccount?> TryGetForIncrementalUpdate__Flawed__NonContactOnly(ImmutableAccountIdentification toTryUpdate)
    {
        throw new NotImplementedException();
    }
}