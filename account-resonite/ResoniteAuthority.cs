using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteAuthority : IAuthority
{
    private readonly Func<Task<string>> _resoniteUidProviderFn;

    public ResoniteAuthority(Func<Task<string>> resoniteUidProviderFn)
    {
        _resoniteUidProviderFn = resoniteUidProviderFn;
    }

    public Task<ILoginService> NewLoginService()
    {
        return Task.FromResult<ILoginService>(new ResoniteLoginService(_resoniteUidProviderFn));
    }

    public async Task<IDataCollection> NewDataCollection(IndividualRepository repository, IResponseCollector storage, ICredentialsStorage credentialsStorage)
    {
        return new ResoniteDataCollection(repository, storage, await _resoniteUidProviderFn(), credentialsStorage);
    }

    public async Task<ILiveMonitoring> NewLiveMonitoring(ICredentialsStorage credentialsStorage, LiveStatusMonitoring monitoring)
    {
        return new ResoniteLiveMonitoring(credentialsStorage, monitoring, await _resoniteUidProviderFn());
    }

    public async Task<NonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage)
    {
        var res = new ResoniteCommunicator(
            new DoNotStoreAnythingStorage(), false, await _resoniteUidProviderFn(),
            credentialsStorage
        );
        return await res.CallerAccount();
    }
}