using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

namespace XYVR.Scaffold;

public class VRChatAuthorityScaffolder : IAuthorityScaffolder
{
    public async Task<IAuthority> CreateAuthority()
    {
        var worldNameCache = await Scaffolding.OpenWorldNameCache();
        return new VRChatAuthority(worldNameCache, async () => await Scaffolding.SaveWorldNameCache(worldNameCache));
    }
}