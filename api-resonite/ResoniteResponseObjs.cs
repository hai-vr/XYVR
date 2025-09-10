using Newtonsoft.Json;

namespace XYVR.API.Resonite;

public struct LoginResponseJsonObject
{
    public LoginResponseEntityJsonObject entity;
}

public struct LoginResponseEntityJsonObject
{
    public string userId;
    public string token;
    public DateTime created;
    public DateTime expire;
    public bool rememberMe;
    public string secretMachineIdHash;
    public string secretMachineIdSalt;
    public string uidHash;
    public string uidSalt;
    public string originalLoginType;
    public string originalLoginId;
    public bool logoutUrlClientSide;
    public int sessionLoginCounter;
    public string sourceIP;
    public string userAgent;
    public bool isMachineBound;
    public string partitionKey;
    public string rowKey;
    public string? eTag;
}

public struct ContactResponseElementJsonObject
{
    public string id;
    public string contactUsername;
    public string contactStatus;
    public bool isAccepted;
    public ContactResponseElementProfileJsonObject? profile;
    public DateTime latestMessageTime;
    public bool isMigrated;
    public bool isCounterpartMigrated;
    public string ownerId;
}

public struct ContactResponseElementProfileJsonObject
{
    public string iconUrl;
    public string[] displayBadges;
}

public struct UserResponseJsonObject
{
    public string id;
    public string username;
    public string normalizedUsername;
    public DateTime registrationDate;
    public bool isVerified;
    public bool isLocked;
    public bool supressBanEvasion;
    [JsonProperty("2fa_login")]
    public bool twoFactorLogin;
    public string[]? tags;
    public UserProfileJsonObject profile;
    public SupporterMetadataJsonObject[] supporterMetadata;
    public EntitlementJsonObject[] entitlements;
    public bool isActiveSupporter;
    public MigratedDataJsonObject migratedData;
}

public struct UserProfileJsonObject
{
    public string iconUrl;
    public string[] displayBadges;
}

public struct SupporterMetadataJsonObject
{
    [JsonProperty("$type")]
    public string type;
    public bool isActiveSupporter;
    public bool isActive;
    public int totalSupportMonths;
    public int totalSupportCents;
    public int lastTierCents;
    public int highestTierCents;
    public int lowestTierCents;
    public DateTime firstSupportTimestamp;
    public DateTime lastSupportTimestamp;
}

public struct EntitlementJsonObject
{
    [JsonProperty("$type")]
    public string type;
    public string? creditType;
    public string? friendlyDescription;
    public string? badgeType;
    public int? badgeCount;
    public string[] entitlementOrigins;
}

public struct MigratedDataJsonObject
{
    public string username;
    public string userId;
    public long quotaBytes;
    public long usedBytes;
    public QuotaByteSourcesJsonObject quotaBytesSources;
    public DateTime registrationDate;
}

public struct QuotaByteSourcesJsonObject
{
    [JsonProperty("base")]
    public long baseQuota;
    public long patreon;
}

public class SessionUpdateJsonObject
{
    public string name;
    public string description;
    // public CorrespondingWorldIdJsonObject correspondingWorldId;
    // public List<string> tags;
    public string sessionId;
    public string normalizedSessionId;
    public string hostUserId;
    public string hostUserSessionId;
    public string hostMachineId;
    public string hostUsername;
    // public string compatibilityHash;
    // public string systemCompatibilityHash;
    // public List<DataModelAssemblyJsonObject> dataModelAssemblies;
    // public string universeId;
    // public string appVersion;
    // public bool headlessHost;
    // public List<string> sessionURLs;
    // public List<string> parentSessionIds;
    // public List<string> nestedSessionIds;
    // public List<string> sessionUsers;
    // public string thumbnailUrl;
    // public int joinedUsers;
    // public int activeUsers;
    // public int totalJoinedUsers;
    // public int totalActiveUsers;
    // public int maxUsers;
    // public bool mobileFriendly;
    // public DateTime sessionBeginTime;
    // public DateTime lastUpdate;
    // public string accessLevel;
    // public bool hideFromListing;
    // public string broadcastKey;
    // public bool awayKickEnabled;
    // public int awayKickMinutes;
    // public bool hasEnded;
    // public bool isValid;
}

public class CorrespondingWorldIdJsonObject
{
    public string recordId;
    public string ownerId;
}

public class DataModelAssemblyJsonObject
{
    public string name;
    public string compatibilityHash;
}
