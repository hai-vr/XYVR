using XYVR.Login;

namespace XYVR.Core;

/// Authorities may be used for different connectors (different accounts), so the objects shouldn't be reused across connectors.
public interface IAuthority
{
    public ConnectorType GetConnectorType(); 
    
    public Task<ILoginService> NewLoginService();
    public Task<IDataCollection> NewDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage);
    public Task<ILiveMonitoring> NewLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage);

    public Task<ImmutableNonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage);
    public Task SaveWhateverNecessary();
}