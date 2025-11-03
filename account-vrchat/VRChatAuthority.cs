using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatAuthority : IAuthority
{
    private readonly WorldNameCache _worldNameCache;
    private readonly Func<Task> _saveFn;
    private IThumbnailCache _thumbnailCache;
    private CancellationTokenSource _cancellationTokenSource;

    public VRChatAuthority(WorldNameCache worldNameCache, IThumbnailCache thumbnailCache, Func<Task> saveFn, CancellationTokenSource cancellationTokenSource)
    {
        _worldNameCache = worldNameCache;
        _saveFn = saveFn;
        _cancellationTokenSource = cancellationTokenSource;
        _thumbnailCache = thumbnailCache;
    }

    public async Task SaveWhateverNecessary()
    {
        await _saveFn();
    }

    public ConnectorType GetConnectorType()
    {
        return ConnectorType.VRChatAPI;
    }

    public Task<ILoginService> NewLoginService()
    {
        return Task.FromResult<ILoginService>(new VRChatLoginService(_cancellationTokenSource));
    }

    public Task<IDataCollection> NewDataCollection(IndividualRepository repository, ICredentialsStorage credentialsStorage, IResponseCollector storage)
    {
        return Task.FromResult<IDataCollection>(new VRChatDataCollection(repository, storage, credentialsStorage, _cancellationTokenSource));
    }

    public Task<ILiveMonitoring> NewLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage)
    {
        return Task.FromResult<ILiveMonitoring>(new VRChatLiveMonitoring(credentialsStorage, monitoring, _worldNameCache, _thumbnailCache));
    }

    public async Task<ImmutableNonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage)
    {
        // Reminder: The same authority may be used for multiple connectors (different caller accounts).
        
        var res = new VRChatCommunicator(new DoNotStoreAnythingStorage(), credentialsStorage, _cancellationTokenSource);
        return await res.CallerAccount();
    }
}