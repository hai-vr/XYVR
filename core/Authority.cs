using XYVR.Login;

namespace XYVR.Core;

public interface IAuthority
{
    public ConnectorType GetConnectorType(); 
    
    public Task<ILoginService> NewLoginService();
    public Task<IDataCollection> NewDataCollection(IndividualRepository repository, IResponseCollector storage, ICredentialsStorage credentialsStorage);
    public Task<ILiveMonitoring> NewLiveMonitoring(ICredentialsStorage credentialsStorage, LiveStatusMonitoring monitoring);

    public Task<NonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage);
}