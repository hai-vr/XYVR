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
            }
            cacheVersion = LatestVersion;
        }

        var now = DateTime.Now;
        foreach (var value in VRCWorlds.Values)
        {
            if (!value.isObsolete)
            {
                if ((now - value.cachedAt).Duration().TotalDays > 3)
                {
                    Console.WriteLine($"Cache data about world {value.worldId} ({value.name} by {value.authorName}) has now been marked as obsolete.");
                    value.isObsolete = true;
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

public class CachedWorld
{
    public bool isObsolete;
    
    public DateTime cachedAt;
    
    public string worldId;
    public string name;
    public string author;
    public string authorName;
    public string thumbnailUrl;
    public string releaseStatus;
    public string description;
}