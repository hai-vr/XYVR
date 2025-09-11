using XYVR.AccountAuthority.Resonite;
using XYVR.Core;

namespace XYVR.Scaffold;

public class ResoniteAuthorityScaffolder : IAuthorityScaffolder
{
    public Task<IAuthority> CreateAuthority()
    {
        return Task.FromResult<IAuthority>(new ResoniteAuthority(Scaffolding.ResoniteUIDLateInitializerFn()));
    }
}