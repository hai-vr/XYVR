using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

namespace XYVR.Scaffold;

public class VRChatAuthorityScaffolder : IAuthorityScaffolder
{
    public async Task<IAuthority> CreateAuthority(CancellationTokenSource cancellationTokenSource)
    {
        var worldNameCache = await Scaffolding.OpenWorldNameCache();
        var thumbnailCache = Scaffolding.ThumbnailCache();
        return new VRChatAuthority(worldNameCache, thumbnailCache, async () => await Scaffolding.SaveWorldNameCache(worldNameCache), cancellationTokenSource);
    }
}