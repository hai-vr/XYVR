namespace XYVR.API.VRChat;

public class VRChatWebsocketContentContainingUser
{
    public string userId;
    public VRChatUser user;
    
    public string location; //": "offline:offline",
    public string instance; //": "offline",
    public string travelingToLocation; //": "offline",
    public string worldId;
}

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

// Incomplete. This is just what we need from that API.
public struct VRChatAuthUser
{
    public string id;
    public string displayName;
}

public struct VRChatUser
{
    public string ageVerificationStatus;
    public bool ageVerified;
    public bool allowAvatarCopying;
    public VRChatBadge[] badges;
    public string bio;
    public string?[]? bioLinks;
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

public class VRChatNoteFull
{
    public DateTime createdAt;
    public string id;
    public string note;
    public VRChatNoteFullTargetUser targetUser;
    public string targetUserId;
    public string userId;
}

public class VRChatNoteFullTargetUser
{
    public string[] currentAvatarTags;
    public string currentAvatarThumbnailImageUrl;
    public string displayName;
    public string id;
    public string profilePicOverride;
    public string userIcon;
}

public class VRChatWorld
{
    public string authorId;
    public string authorName;
    public int capacity;
    public int recommendedCapacity;
    public DateTime created_at;
    public VRChatWorldContentSettings defaultContentSettings;
    public string description;
    public int favorites;
    public bool featured;
    public int heat;
    public string id;
    public string imageUrl;
    public object[][] instances;
    public string labsPublicationDate;
    public string name;
    public string @namespace;
    public int occupants;
    public string organization;
    public int popularity;
    public string previewYoutubeId;
    public int privateOccupants;
    public int publicOccupants;
    public string publicationDate;
    public string releaseStatus;
    public string storeId;
    public string[] tags;
    public string thumbnailImageUrl;
    public VRChatUnityPackage[] unityPackages;
    public DateTime updated_at;
    public string[] urlList;
    public int version;
    public int visits;
    public string[] udonProducts;
}

public class VRChatWorldContentSettings
{
    public bool drones;
    public bool emoji;
    public bool pedestals;
    public bool prints;
    public bool stickers;
    public bool props;
}

public class VRChatUnityPackage
{
    public string id;
    public string assetUrl;
    public object assetUrlObject;
    public int assetVersion;
    public DateTime created_at;
    public string impostorizerVersion;
    public string performanceRating;
    public string platform;
    public string pluginUrl;
    public object pluginUrlObject;
    public long unitySortNumber;
    public string unityVersion;
    public string worldSignature;
    public string impostorUrl;
    public string scanStatus;
    public string variant;
}
