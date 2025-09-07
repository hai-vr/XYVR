using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XYVR.API.VRChat;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatLiveCommunicator
{
    private readonly ICredentialsStorage _credentialsStorage;
    private readonly string _callerInAppIdentifier;
    private readonly VRChatWebsocketClient _wsClient;
    private readonly IResponseCollector _responseCollector;
    
    private VRChatAPI? _api;

    public event LiveUpdateReceived? OnLiveUpdateReceived;
    public delegate Task LiveUpdateReceived(LiveUpdate liveUpdate);

    public VRChatLiveCommunicator(ICredentialsStorage credentialsStorage, string callerInAppIdentifier, IResponseCollector responseCollector)
    {
        _credentialsStorage = credentialsStorage;
        _callerInAppIdentifier = callerInAppIdentifier;
        _responseCollector = responseCollector;

        _wsClient = new VRChatWebsocketClient();
        _wsClient.Connected += WhenConnected;
        _wsClient.MessageReceived += WhenMessageReceived;
        _wsClient.Disconnected += WhenDisconnected;
    }

    public async Task Connect()
    {
        _api ??= await InitializeAPI();
        
        var contactsAsyncEnum = _api.ListFriends(ListFriendsRequestType.OnlyOnline, DataCollectionReason.FindUndiscoveredAccounts)
            .Concat(_api.ListFriends(ListFriendsRequestType.OnlyOffline, DataCollectionReason.FindUndiscoveredAccounts));
        await foreach (var friend in contactsAsyncEnum)
        {
            if (OnLiveUpdateReceived != null)
            {
                await OnLiveUpdateReceived(new LiveUpdate
                {
                    namedApp = NamedApp.VRChat,
                    qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                    inAppIdentifier = friend.id,
                    onlineStatus = ParseStatus("xxx", friend.platform, friend.status),
                    callerInAppIdentifier = _callerInAppIdentifier,
                    customStatus = friend.statusDescription
                });
            }
        }
        
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
        
            if (type is "friend-online" or "friend-update" or "friend-offline" or "friend-location" or "friend-active")
            {
                Console.Write($"{type} ");
                var content = JsonConvert.DeserializeObject<VRChatWebsocketContentContainingUser>(rootObj["content"].Value<string>())!;
                if (OnLiveUpdateReceived != null)
                {
                    // FIXME: This is a task???
                    OnLiveUpdateReceived(new LiveUpdate
                    {
                        namedApp = NamedApp.VRChat,
                        qualifiedAppName = VRChatCommunicator.VRChatQualifiedAppName,
                        inAppIdentifier = content.userId,
                        onlineStatus = ParseStatus(type, content.user.platform, content.user.status),
                        callerInAppIdentifier = _callerInAppIdentifier,
                        customStatus = content.user.statusDescription
                    });
                }
            }
            else
            {
                Console.WriteLine($"Received UNHANDLED message of type {type} from vrc ws api");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private OnlineStatus ParseStatus(string type, string platform, string userStatus)
    {
        if (platform == "web") return OnlineStatus.Offline;
        if (type == "friend-offline") return OnlineStatus.Offline;
        
        return userStatus switch
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
    
    private async Task<VRChatAPI> InitializeAPI()
    {
        var api = new VRChatAPI(_responseCollector);
        var userinput_cookies__sensitive = await _credentialsStorage.RequireCookieOrToken();
        if (userinput_cookies__sensitive != null)
        {
            api.ProvideCookies(userinput_cookies__sensitive);
        }

        if (!api.IsLoggedIn)
        {
            throw new ArgumentException("User must be already logged in before establishing communication");
        }

        return api;
    }
}