using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

namespace XYVR.Scaffold;

public class VRChatAuthorityScaffolder : IAuthorityScaffolder
{
    public async Task<IAuthority> CreateAuthority(CancellationTokenSource cancellationTokenSource)
    {
        var worldNameCache = await Scaffolding.OpenWorldNameCache();
        var thumbnailCache = Scaffolding.ThumbnailCache();

        var keep = new HashSet<string>();
        foreach (var world in worldNameCache.VRCWorlds.Values)
        {
            keep.Add(VRChatThumbnailCache.Sha(world.thumbnailUrl));
        }
        thumbnailCache.KeepOnly(keep);
        
        return new VRChatAuthority(worldNameCache, thumbnailCache, async () => await Scaffolding.SaveWorldNameCache(worldNameCache), cancellationTokenSource);
    }
}