using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

namespace XYVR.Scaffold;

public class VRChatAuthorityScaffolder : IAuthorityScaffolder
{
    public async Task<IAuthority> CreateAuthority()
    {
        return new VRChatAuthority(await Scaffolding.OpenWorldNameCache());
    }
}