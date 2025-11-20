using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.ChilloutVR;

public class ChilloutVRAuthority : IAuthority
{
    public ConnectorType GetConnectorType()
    {
        return ConnectorType.ChilloutVRAPI;
    }

    public Task<ILoginService> NewLoginService()
    {
        return Task.FromResult<ILoginService>(new ChilloutVRLoginService());
    }

    public Task<IDataCollection> NewDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage)
    {
        return Task.FromResult<IDataCollection>(new ChilloutVRDataCollection(repository, credentialsStorage, storage));
    }

    public Task<ILiveMonitoring> NewLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage)
    {
        return Task.FromResult<ILiveMonitoring>(new ChilloutVRLiveMonitoring(monitoring, credentialsStorage));
    }

    public Task<ImmutableNonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage)
    {
        return Task.FromResult(new ImmutableNonIndexedAccount
        {
            qualifiedAppName = QualifiedAppName,
            inAppIdentifier = "todo_identifier",
            inAppDisplayName = "todo_displayname"
        });
    }

    public Task SaveWhateverNecessary()
    {
        return Task.CompletedTask;
    }

    public const string QualifiedAppName = "chilloutvr";
}