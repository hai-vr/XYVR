using XYVR.AccountAuthority.Cluster;
using XYVR.Core;

namespace XYVR.Scaffold;

public class ClusterAuthorityScaffolder : IAuthorityScaffolder
{
    public Task<IAuthority> CreateAuthority(CancellationTokenSource cancellationTokenSource)
    {
        return Task.FromResult<IAuthority>(new ClusterAuthority(cancellationTokenSource));
    }
}