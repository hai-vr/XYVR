using XYVR.Core;
using XYVR.Login;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatAuthority : IAuthority
{
    private readonly WorldNameCache _worldNameCache;

    public VRChatAuthority(WorldNameCache worldNameCache)
    {
        _worldNameCache = worldNameCache;
    }

    public Task<ILoginService> NewLoginService()
    {
        return Task.FromResult<ILoginService>(new VRChatLoginService());
    }

    public Task<IDataCollection> NewDataCollection(IndividualRepository repository, IResponseCollector storage, ICredentialsStorage credentialsStorage)
    {
        return Task.FromResult<IDataCollection>(new VRChatDataCollection(repository, storage, credentialsStorage));
    }

    public Task<ILiveMonitoring> NewLiveMonitoring(ICredentialsStorage credentialsStorage, LiveStatusMonitoring monitoring)
    {
        return Task.FromResult<ILiveMonitoring>(new VRChatLiveMonitoring(credentialsStorage, monitoring, _worldNameCache));
    }

    public async Task<NonIndexedAccount> ResolveCallerAccount(ICredentialsStorage credentialsStorage)
    {
        var res = new VRChatCommunicator(new DoNotStoreAnythingStorage(), credentialsStorage);
        return await res.CallerAccount();
    }
}