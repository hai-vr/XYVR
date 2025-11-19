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
}

[Serializable]
internal class ClusterUserState
{
    public string onlineStatus;
    public ClusterUserInfo user;
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