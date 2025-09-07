using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.API.VRChat;
using XYVR.Core;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;
    private readonly VRChatWebsocketClient _wsClient;
    
    public event LiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task LiveUpdateReceived(LiveUpdate liveUpdate);

    public VRChatLiveCommunicator(ICredentialsStorage credentialsStorage, string callerInAppIdentifier)
    {
        _credentialsStorage = credentialsStorage;
        _callerInAppIdentifier = callerInAppIdentifier;

        _wsClient = new VRChatWebsocketClient();
        _wsClient.Connected += WhenConnected;
        _wsClient.MessageReceived += WhenMessageReceived;
        _wsClient.Disconnected += WhenDisconnected;
    }

    public async Task Connect()
    {
        await _wsClient.Connect(await GetToken__sensitive());
    }

    public async Task Disconnect()
    {
        await _wsClient.Disconnect();
    }

    private void WhenMessageReceived(string msg)
    {
        try
        {
            var rootObj = JObject.Parse(msg);
            var type = rootObj["type"].Value<string>();
            Console.WriteLine($"Received message of type {type} from vrc ws api");
        
            if (type is "friend-online" or "friend-update" or "friend-offline" or "friend-location" or "friend-active")
            {
                var content = JsonConvert.DeserializeObject<VRChatWebsocketContentContainingUser>(rootObj["content"].Value<string>())!;
                OnLiveUpdateReceived?.Invoke(new LiveUpdate
                {
                    namedApp = NamedApp.VRChat,
                    qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                    inAppIdentifier = content.userId,
                    onlineStatus = ParseStatus(content.user),
                    callerInAppIdentifier = _callerInAppIdentifier,
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private OnlineStatus ParseStatus(VRChatUser vrcUser)
    {
        return vrcUser.status switch
        {
            "offline" => OnlineStatus.Offline,
            "active" => OnlineStatus.Online,
            "busy" => OnlineStatus.VRChatDND,
            "ask me" => OnlineStatus.VRChatAskMe,
            "join me" => OnlineStatus.VRChatJoinMe,
            _ => OnlineStatus.Indeterminate
        };
    }

    private void WhenConnected()
    {
    }

    private void WhenDisconnected(string reason)
    {
    }

    private async Task<string> GetToken__sensitive()
    {
        return JsonConvert.DeserializeObject<VRChatAPI.VrcAuthenticationCookies>(await _credentialsStorage.RequireCookieOrToken())
            .auth.Value;
    }
}