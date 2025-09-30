using System.Text.Json.Serialization;

namespace XYVR.AccountAuthority.VRChat;

#pragma warning disable CS8618
#pragma warning disable 0649
[Serializable]
internal class VRChatInstance
{
    // public bool? active;
    // public bool? ageGate;
    // public bool? canRequestInvite;
    public int? capacity;
    // public string? clientNumber;
    // public ContentSettings? contentSettings;
    public string? displayName;
    // public bool? full;
    // public int? gameServerVersion;
    // public string? id;
    // public string? instanceId;
    // public string? instancePersistenceEnabled;
    // public string? location;
    public int? n_users;
    // public string? name;
    // public string? ownerId;
    // public bool? permanent;
    // public string? photonRegion;
    // public Platforms? platforms;
    // public bool? playerPersistenceEnabled;
    // public string? region;
    // public string? secureName;
    // public string? shortName;
    // public string[]? tags;
    // public string? type;
    // public string? worldId;
    // public string? hidden;
    // public string? friends;
    // [JsonPropertyName("private")]
    // public string? privateAccess;
    // public bool? queueEnabled;
    // public int? queueSize;
    // public int? recommendedCapacity;
    // public bool? roleRestricted;
    // public bool? strict;
    public int? userCount;
    // public World? world;
    // public User[]? users;
    // public string? groupAccessType;
    // public bool? hasCapacityForYou;
    // public string? nonce;
    // public DateTime? closedAt;
    // public bool? hardClose;
}

[Serializable]
internal class ContentSettings
{
    public bool? drones;
    public bool? emoji;
    public bool? pedestals;
    public bool? prints;
    public bool? stickers;
    public bool? props;
}

[Serializable]
internal class Platforms
{
    public int? android;
    public int? ios;
    public int? standalonewindows;
}

[Serializable]
internal class World
{
    public string? authorId;
    public string? authorName;
    public int? capacity;
    public int? recommendedCapacity;
    public DateTime? created_at;
    public ContentSettings? defaultContentSettings;
    public string? description;
    public int? favorites;
    public bool? featured;
    public int? heat;
    public string? id;
    public string? imageUrl;
    public object[][]? instances;
    public string? labsPublicationDate;
    public string? name;
    [JsonPropertyName("namespace")]
    public string? namespaceValue;
    public int? occupants;
    public string? organization;
    public int? popularity;
    public string? previewYoutubeId;
    public int? privateOccupants;
    public int? publicOccupants;
    public string? publicationDate;
    public string? releaseStatus;
    public string? storeId;
    public string[]? tags;
    public string? thumbnailImageUrl;
    public UnityPackage[]? unityPackages;
    public DateTime? updated_at;
    public string[]? urlList;
    public int? version;
    public int? visits;
    public string[]? udonProducts;
}

[Serializable]
internal class UnityPackage
{
    public string? id;
    public string? assetUrl;
    public object? assetUrlObject;
    public int? assetVersion;
    public DateTime? created_at;
    public string? impostorizerVersion;
    public string? performanceRating;
    public string? platform;
    public string? pluginUrl;
    public object? pluginUrlObject;
    public long? unitySortNumber;
    public string? unityVersion;
    public string? worldSignature;
    public string? impostorUrl;
    public string? scanStatus;
    public string? variant;
}

[Serializable]
internal class User
{
    public string? ageVerificationStatus;
    public bool? ageVerified;
    public bool? allowAvatarCopying;
    public string? bio;
    public string[]? bioLinks;
    public string? currentAvatarImageUrl;
    public string? currentAvatarThumbnailImageUrl;
    public string[]? currentAvatarTags;
    public DateTime? date_joined;
    public string? developerType;
    public string? displayName;
    public string? friendKey;
    public string? id;
    public bool? isFriend;
    public string? imageUrl;
    public string? last_platform;
    public DateTime? last_activity;
    public DateTime? last_mobile;
    public string? platform;
    public string? profilePicOverride;
    public string? profilePicOverrideThumbnail;
    public string? pronouns;
    public string? state;
    public string? status;
    public string? statusDescription;
    public string[]? tags;
    public string? userIcon;
}
#pragma warning restore 0649
#pragma warning restore CS8618