namespace XYVR.API.VRChat;

public class VRChatFriend
{
    public string bio;
    public string[] bioLinks;
    public string currentAvatarImageUrl;
    public string currentAvatarThumbnailImageUrl;
    public string[] currentAvatarTags;
    public string developerType;
    public string displayName;
    public string friendKey;
    public string id;
    public bool isFriend;
    public string imageUrl;
    public string last_platform;
    public string location;
    public DateTime? last_login;
    public DateTime? last_activity;
    public DateTime? last_mobile;
    public string platform;
    public string profilePicOverride;
    public string profilePicOverrideThumbnail;
    public string status;
    public string statusDescription;
    public string[] tags;
    public string userIcon;
}

public struct VRChatUser
{
    public string ageVerificationStatus;
    public bool ageVerified;
    public bool allowAvatarCopying;
    public VRChatBadge[] badges;
    public string bio;
    public string[] bioLinks;
    public string currentAvatarImageUrl;
    public string currentAvatarThumbnailImageUrl;
    public string[] currentAvatarTags;
    public string date_joined;
    public string developerType;
    public string displayName;
    public string friendKey;
    public string friendRequestStatus;
    public string id;
    public string instanceId;
    public bool isFriend;
    public string last_activity;
    public string last_login;
    public string last_mobile;
    public string last_platform;
    public string location;
    public string note;
    public string platform;
    public string profilePicOverride;
    public string profilePicOverrideThumbnail;
    public string pronouns;
    public string state;
    public string status;
    public string statusDescription;
    public string[] tags;
    public string travelingToInstance;
    public string travelingToLocation;
    public string travelingToWorld;
    public string userIcon;
    public string username;
    public string worldId;
}

public struct VRChatBadge
{
    public string assignedAt;
    public string badgeDescription;
    public string badgeId;
    public string badgeImageUrl;
    public string badgeName;
    public bool hidden;
    public bool showcased;
    public string updatedAt;
}

public struct VRChatNote
{
    public string note;
}