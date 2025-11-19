using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.Cluster;

public class ClusterAuthority : IAuthority
{
    private readonly CancellationTokenSource _cancellationTokenSource;

    public ClusterAuthority(CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
    }

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
        return Task.FromResult<IDataCollection>(new ClusterDataCollection(repository, credentialsStorage, storage, _cancellationTokenSource));
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