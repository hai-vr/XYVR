using XYVR.Login;

namespace XYVR.Core;

public interface IAuthority
{
    public ConnectorType GetConnectorType(); 
    
    public Task<ILoginService> NewLoginService();
    public Task<IDataCollection> NewDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage);
    public Task<ILiveMonitoring> NewLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage);

    public Task<NonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage);
}