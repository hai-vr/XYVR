using Newtonsoft.Json;
using XYVR.Core;

namespace XYVR.AccountAuthority.ChilloutVR;

public class ChilloutVRLiveMonitoring(LiveStatusMonitoring monitoring, ICredentialsStorage credentialsStorage) : ILiveMonitoring
{
    private string _callerInAppIdentifier = null!;
    private ChilloutVRAPI? _api;

    public async Task StartMonitoring()
    {
        _api ??= await InitializeAPI();

        var contacts = await _api.GetContacts();
    }

    public Task StopMonitoring()
    {
        return Task.CompletedTask;
    }

    public Task DefineCaller(string callerInAppIdentifier)
    {
        _callerInAppIdentifier = callerInAppIdentifier;
        return Task.CompletedTask;
    }

    private async Task<ChilloutVRAPI> InitializeAPI()
    {
        var api = new ChilloutVRAPI();
        var token__sensitive = await credentialsStorage.RequireCookieOrToken();
        if (token__sensitive != null)
        {
            api.Provide(JsonConvert.DeserializeObject<ChilloutVRAuthStorage>(token__sensitive)!);
        }

        return api;
    }
}