namespace XYVR.AccountAuthority.ChilloutVR;

#pragma warning disable CS8618
#pragma warning disable 0649
[Serializable]
internal class CvrContactsResponse
{
    public string? message;
    public CvrContactsResponseData[] data;
}

[Serializable]
internal class CvrContactsResponseData
{
    public object[] categories;
    public string id;
    public string name;
    public string imageUrl;
}

[Serializable]
internal class CvrInstanceResponse
{
    public string? message;
    public CvrInstanceResponseData data;
}

[Serializable]
internal class CvrInstanceResponseData
{
    public string instanceSettingPrivacy;
    public string privacy;
    public CvrInstanceResponsePlayer author;
    public CvrInstanceResponseOwner owner;
    public CvrInstanceResponseGroup? group;
    public string id;
    public string name;
    public string gameModeId;
    public string gameModeName;
    public string region;
    public CvrInstanceResponseWorld world;
    public int maxPlayer;
    public int currentPlayerCount;
    public CvrInstanceResponsePlayer[] members;
    public bool reserved;
}

[Serializable]
internal class CvrInstanceResponsePlayer
{
    public string id;
    public string name;
    public string imageUrl;
}

[Serializable]
internal class CvrInstanceResponseOwner
{
    public string rank;
    public CvrInstanceResponseFeaturedBadge featuredBadge;
    public CvrInstanceResponseGroup? featuredGroup;
    public CvrInstanceResponseAvatar avatar;
    public string id;
    public string name;
    public string imageUrl;
}
[Serializable]
internal class CvrInstanceResponseFeaturedBadge
{
    public string name;
    public string image;
    public int badgeLevel;
}
[Serializable]
internal class CvrInstanceResponseGroup
{
    public string id;
    public string name;
    public string image;
}
[Serializable]
internal class CvrInstanceResponseAvatar
{
    public string id;
    public string name;
    public string imageUrl;
}
[Serializable]
internal class CvrInstanceResponseWorld
{
    public object[] tags;
    public string id;
    public string name;
    public string imageUrl;
}

[Serializable]
internal class CvrWebsocketOnlineFriendsResponse
{
    public string Id;
    public bool IsOnline;
    public bool IsConnected;
    public CvrWebsocketInstance? Instance;
}
[Serializable]
internal class CvrWebsocketInstance
{
    public string Id;
    public string Name;
    public int Privacy;
}
#pragma warning restore 0649
#pragma warning restore CS8618