namespace XYVR.AccountAuthority.Cluster;

[Serializable]
internal class ClusterPaginatedUsersResponse
{
    public ClusterPagingInfo paging;
    public ClusterUserState[] users;
}

[Serializable]
internal class ClusterPagingInfo
{
    public string nextToken;
}

[Serializable]
internal enum ClusterOnlineStatus
{
    Offline,
    OnlinePrivate, // In the web UI, they show this as "Friend |> Playing" or "フレンド |> プレイ中"
    OnlinePublic, // In the web UI, they show this as the event name
}

[Serializable]
internal class ClusterUserState
{
    public string onlineStatus;
    public ClusterUserInfo user;
    public ClusterLiveEntry? liveEntry; // Probably not null if onlineStatus is equal to "OnlinePublic"
}

[Serializable]
internal class ClusterLiveEntry
{
    public string deepLinkUrl;
    public string entryURL;
    public string id;
    public string name;
    public string roomId;
    public string roomServerType;
    public string worldRoomSetType;
}

[Serializable]
internal class ClusterCallerUserInfo : ClusterUserInfo
{
    // The caller user info (at /login) is a superset of ClusterUserInfo, but we don't care about any of the fields from that superset for our purposes,
    // so we're not even gonna bother trying to deserialize them.
}

[Serializable]
internal class ClusterUserInfo
{
    public string bio;
    public string displayName;
    public bool isCertified;
    public bool isDeleted;
    public string photoUrl;
    public string rank;
    public string shareUrl;
    public string userId;
    public string username;
}

// Returned by calling /v1/live_activity/friend_hots?count=100
// This is called when at least one friend is in an event instance
[Serializable]
internal class ClusterHotsResponse
{
    public ClusterContent[] contents;
}

[Serializable]
internal class ClusterContent
{
    public string betaFeature;
    public string contentType;
    public string deepLink;
    public ClusterEventInfo eventInfo;
    public bool isTicketing;
    public string originalImageUrl;
    public ClusterUserInfo owner;
    public int playerCount;
    public string[] playerPhotoUrls;
    public string thumbnailUrl;
    public string title;
    public string webUrl;
}

[Serializable]
internal class ClusterEventInfo
{
    public string closeDatetime;
    public string eventStatus;
    public string openDatetime;
    public ClusterRequestUserStatus requestUserStatus;
    public string worldRoomSetId;
}

[Serializable]
internal class ClusterRequestUserStatus
{
    public bool isWatched;
}
