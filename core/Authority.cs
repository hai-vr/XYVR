using XYVR.Login;

namespace XYVR.Core;

/// Authorities may be used for different connectors (different accounts on the same social VR app), so the objects shouldn't be reused across connectors.
public interface IAuthority
{
    /// Returns the connector type of that authority. A social VR app may have multiple different connector types capable of providing
    /// data pertaining to that social VR app; for instance, an offline connector that reconstructs data from logs.
    public ConnectorType GetConnectorType(); 
    
    /// Returns a new instance of login service. The caller is responsible for storing that instance if necessary,
    /// as the authority should not keep a reference to created instances.
    /// Not all authorities may have a login service; for instance, an offline connector will not have one, so a no-op service should be returned instead.
    public Task<ILoginService> NewLoginService();
    
    /// Returns a new instance of data collection. The caller is responsible for storing that instance if necessary,
    /// as the authority should not keep a reference to created instances.
    public Task<IDataCollection> NewDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage);
    
    /// Returns a new instance of live monitoring. The caller is responsible for storing that instance if necessary,
    /// as the authority should not keep a reference to created instances.
    /// Not all authorities may have live monitoring; for instance, an offline connector will not have one, so a no-op service should be returned instead.
    public Task<ILiveMonitoring> NewLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage);

    /// Return the account which is used to connect. This not necessarily the credentials used for logging in: if the user logs in with a username
    /// such as all lowercase "hai", and the username is the in-app identifier, then the account returned here must contain the information that
    /// the authority actually presents such as "Hai", which may have different capitalization.
    public Task<ImmutableNonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage);
    
    /// If this authority needs to store any data that isn't important but helpful, such as a cache, this is the moment to do it.
    /// This is called when the application is about to close.
    public Task SaveWhateverNecessary();
}