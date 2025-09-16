namespace XYVR.AccountAuthority.Resonite;

[Serializable]
public record ResoniteAuthStorage
{
    public required string userId { get; init; }
    public required string token { get; init; }
}