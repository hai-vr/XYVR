using XYVR.Core;

namespace XYVR.Scaffold;

public interface IAuthorityScaffolder
{
    public Task<IAuthority> CreateAuthority(CancellationTokenSource cancellationTokenSource);

    public static async Task<List<IAuthority>> FindAll(CancellationTokenSource cancellationTokenSource)
    {
        // TODO: Use namespace attributes or something to declare authority scaffolders and collect them.
        List<IAuthorityScaffolder> scaffolders = [
            new ResoniteAuthorityScaffolder(),
            new VRChatAuthorityScaffolder(),
            new ChilloutVRAuthorityScaffolder()
        ];
        return (await Task.WhenAll(scaffolders.Select(x => x.CreateAuthority(cancellationTokenSource)))).ToList();
    }
}