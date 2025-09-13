using XYVR.AccountAuthority.ChilloutVR;
using XYVR.Core;

namespace XYVR.Scaffold;

public class ChilloutVRAuthorityScaffolder : IAuthorityScaffolder
{
    public Task<IAuthority> CreateAuthority()
    {
        return Task.FromResult<IAuthority>(new ChilloutVRAuthority());
    }
}