namespace XYVR.Core;

public class WorldNameCache
{
    private const int LatestVersion = 3;
    private const string Warning = "THE CONTENTS OF THIS FILE CAN BE DELETED AT ANY TIME. You must NOT use this file as a world data archival source; it is not the purpose of this file.";
    private const string Purpose = "The purpose of this file is to prevent repetitive requests to the VRChat API to show the world name of active sessions when restarting the application.";

    public int cacheVersion = LatestVersion;
    public string warning = Warning;
    public string purpose = Purpose;
    
    public Dictionary<string, CachedWorld> VRCWorlds = new();

    public void PreProcess()
    {
        warning = Warning;
        purpose = Purpose;
        
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

        // Delete entries from the world cache older than 45 days. This is because the world cache could grow indefinitely,
        // and we are loading the data into RAM, so we don't want unnecessary data to be loaded to RAM every time.
        // The value in "cachedAt" is refreshed along with the data when someone is witnessed in that world after 6 hours.
        var keys = VRCWorlds.Keys.ToList();
        foreach (var key in keys)
        {
            var world = VRCWorlds[key];
            if ((now - world.cachedAt).Duration().TotalDays > 45)
            {
                VRCWorlds.Remove(key);
            }
        }
        
        foreach (var world in VRCWorlds.Values)
        {
            if (!world.isObsolete)
            {
                // We refresh the data in case the world name, thumbnail URL, or capacity has changed.
                if ((now - world.cachedAt).Duration().TotalHours > 6)
                {
                    world.needsRefresh = true;
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