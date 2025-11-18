using XYVR.Core;

namespace XYVR.AccountAuthority.Cluster;

public class ClusterLiveMonitoring : ILiveMonitoring
{
    public ClusterLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage)
    {
        throw new NotImplementedException();
    }

    public Task StartMonitoring()
    {
        throw new NotImplementedException();
    }

    public Task StopMonitoring()
    {
        throw new NotImplementedException();
    }

    public Task DefineCaller(string callerInAppIdentifier)
    {
        throw new NotImplementedException();
    }

    public Task MakeGameClientJoinOrSelfInvite(string sessionId, CancellationTokenSource cancellationTokenSource)
    {
        throw new NotImplementedException();
    }
}