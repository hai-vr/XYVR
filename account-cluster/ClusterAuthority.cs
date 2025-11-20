using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.Cluster;

public class ClusterAuthority : IAuthority
{
    // TODO: Paginated friends
    // TODO: Better login, if feasible
    // TODO: Button that links to cluster username
    // TODO: Specialized serialization of cluster bio
    // TODO: Display cluster bio in individual
    
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
        return Task.FromResult<ILiveMonitoring>(new ClusterLiveMonitoring(monitoring, credentialsStorage, _cancellationTokenSource));
    }

    public Task<ImmutableNonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage)
    {
        return Task.FromResult(new ImmutableNonIndexedAccount
        {
            qualifiedAppName = QualifiedAppName,
            inAppIdentifier = "todo_identifier",
            inAppDisplayName = "todo_displayname"
        });
    }

    public Task SaveWhateverNecessary()
    {
        return Task.CompletedTask;
    }

    public const string QualifiedAppName = "cluster";
}