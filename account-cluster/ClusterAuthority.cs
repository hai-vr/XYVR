using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.Cluster;

public class ClusterAuthority : IAuthority
{
    public ConnectorType GetConnectorType()
    {
        return ConnectorType.ClusterAPI;
    }

    public Task<ILoginService> NewLoginService()
    {
        return Task.FromResult<ILoginService>(new ClusterLoginService());
    }

    public Task<IDataCollection> NewDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage)
    {
        throw new NotImplementedException();
    }

    public Task<ILiveMonitoring> NewLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage)
    {
        return Task.FromResult<ILiveMonitoring>(new ClusterLiveMonitoring(monitoring, credentialsStorage));
    }

    public Task<ImmutableNonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage)
    {
        throw new NotImplementedException();
    }

    public Task SaveWhateverNecessary()
    {
        return Task.CompletedTask;
    }

    public const string QualifiedAppName = "cluster";
}