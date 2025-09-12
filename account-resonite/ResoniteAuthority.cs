using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.Resonite;

public class ResoniteAuthority : IAuthority
{
    private readonly Func<Task<string>> _resoniteUidProviderFn;

    public ResoniteAuthority(Func<Task<string>> resoniteUidProviderFn)
    {
        // We need to call this as late as possible so that UID doesn't generate for users who never use Resonite.
        _resoniteUidProviderFn = resoniteUidProviderFn;
    }

    public Task SaveWhateverNecessary()
    {
        // Do nothing
        return Task.CompletedTask;
    }

    public ConnectorType GetConnectorType()
    {
        return ConnectorType.ResoniteAPI;
    }

    public Task<ILoginService> NewLoginService()
    {
        return Task.FromResult<ILoginService>(new ResoniteLoginService(_resoniteUidProviderFn));
    }

    public async Task<IDataCollection> NewDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage)
    {
        return new ResoniteDataCollection(repository, storage, await _resoniteUidProviderFn(), credentialsStorage);
    }

    public async Task<ILiveMonitoring> NewLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage)
    {
        return new ResoniteLiveMonitoring(credentialsStorage, monitoring, await _resoniteUidProviderFn());
    }

    public async Task<ImmutableNonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage)
    {
        // Reminder: The same authority may be used for multiple connectors (different caller accounts).
        
        var res = new ResoniteCommunicator(
            new DoNotStoreAnythingStorage(), false, await _resoniteUidProviderFn(),
            credentialsStorage
        );
        return await res.CallerAccount();
    }
}