using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

namespace XYVR.Scaffold;

public class VRChatAuthorityScaffolder : IAuthorityScaffolder
{
    public async Task<IAuthority> CreateAuthority(CancellationTokenSource cancellationTokenSource)
    {
        var variousNameCache = await Scaffolding.OpenVariousNameCache();
        var thumbnailCache = Scaffolding.ThumbnailCache();

        var keep = new HashSet<string>();
        foreach (var world in variousNameCache.VRCWorlds.Values)
        {
            keep.Add(VRChatThumbnailCache.Sha(world.thumbnailUrl));
        }
        thumbnailCache.KeepOnly(keep);
        
        return new VRChatAuthority(variousNameCache, thumbnailCache, async () => await Scaffolding.SaveVariousNameCache(variousNameCache), cancellationTokenSource);
    }
}