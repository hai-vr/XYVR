namespace XYVR.Core;

public class WorldNameCache
{
    private const int LatestVersion = 3;
    
    public int cacheVersion = LatestVersion;
    public string warning = "THE CONTENTS OF THIS FILE CAN BE DELETED AT ANY TIME. You must NOT use this file as a world data archival source; it is not the purpose of this file.";
    public string purpose = "The purpose of this file is to prevent repetitive requests to the VRChat API to show the world name of active sessions when restarting the application.";
    
    public Dictionary<string, CachedWorld> VRCWorlds = new();

    public void PreProcess()
    {
        if (cacheVersion != LatestVersion)
        {
            foreach (var value in VRCWorlds.Values)
            {
                value.isObsolete = true;
                value.needsRefresh = true;
            }
            XYVRLogging.WriteLine(this, $"Cache data is now marked as obsolete. Cache version was {cacheVersion} and it is now {LatestVersion}.");
            cacheVersion = LatestVersion;
        }

        var now = DateTime.Now;
        foreach (var value in VRCWorlds.Values)
        {
            if (!value.isObsolete)
            {
                if ((now - value.cachedAt).Duration().TotalHours > 6)
                {
                    value.needsRefresh = true;
                }
            }
        }
    }

    public CachedWorld? GetValidOrNull(string worldId)
    {
        var value = VRCWorlds.GetValueOrDefault(worldId);
        if (value != null && !value.isObsolete)
        {
            return value;
        }

        return null;
    }
}

[Serializable]
public class CachedWorld
{
    public bool isObsolete;
    
    public required DateTime cachedAt;
    [NonSerialized] public bool needsRefresh;
    
    public required string worldId;
    public required string name;
    public required string author;
    public required string authorName;
    public required string thumbnailUrl;
    public required string releaseStatus;
    public required string description;
    public required int capacity;
}