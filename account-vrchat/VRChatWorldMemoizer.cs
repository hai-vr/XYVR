using XYVR.API.VRChat;

namespace XYVR.AccountAuthority.VRChat;

public class VRChatWorldMemoizer
{
    private readonly Dictionary<string, MemoizedWorld> _memoizedWorlds = new(); 
    
    public bool HasMemoized(string worldId)
    {
        return _memoizedWorlds.ContainsKey(worldId); 
    }
    
    public void Found(string worldId, VRChatWorld worldData)
    {
        Console.WriteLine($"Memoizing world {worldId} as {worldData.name}");
        _memoizedWorlds[worldId] = new MemoizedWorld
        {
            wasFound = true,
            world = worldData,
            worldId = worldId
        };
    }

    public void NotFound(string worldId)
    {
        Console.WriteLine($"Memoizing world {worldId} as NOT FOUND");
        _memoizedWorlds[worldId] = new MemoizedWorld
        {
            wasFound = false,
            worldId = worldId
        };
    }

    public MemoizedWorld Get(string worldId)
    {
        return _memoizedWorlds[worldId];
    }
}

public class MemoizedWorld
{
    public bool wasFound;
    public VRChatWorld? world;
    public string worldId;
}