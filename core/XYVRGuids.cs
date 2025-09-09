namespace XYVR.Core;

/// Having Guid.NewGuid() across the code is confusing because it doesn't semantically describe what it's used for.
/// I assembled all the uses of Guid.NewGuid() here so that we can trace back inappropriate new instances of Guids.
/// Guids should only be created the moment we know we need to index something new (Indexed objects).
public static class XYVRGuids
{
    public static string ForIndividual() => InternalNewGuid();
    public static string ForAccount() => InternalNewGuid();
    public static string ForConnector() => InternalNewGuid();
    public static string ForSession() => InternalNewGuid();
    public static string ForRequest() => InternalNewGuid();
    public static string ForTrail() => InternalNewGuid();
    
    public static string ForResoniteMachineId() => InternalNewGuid();

    private static string InternalNewGuid()
    {
        return Guid.NewGuid().ToString();
    }
}