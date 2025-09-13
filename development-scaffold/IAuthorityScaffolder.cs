using XYVR.Core;

namespace XYVR.Scaffold;

public interface IAuthorityScaffolder
{
    public Task<IAuthority> CreateAuthority();

    public static async Task<List<IAuthority>> FindAll()
    {
        // TODO: Use namespace attributes or something to declare authority scaffolders and collect them.
        List<IAuthorityScaffolder> scaffolders = [
            new ResoniteAuthorityScaffolder(),
            new VRChatAuthorityScaffolder(),
            new ChilloutVRAuthorityScaffolder()
        ];
        return (await Task.WhenAll(scaffolders.Select(x => x.CreateAuthority()))).ToList();
    }
}