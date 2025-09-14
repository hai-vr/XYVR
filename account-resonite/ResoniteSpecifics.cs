namespace XYVR.AccountAuthority.Resonite;

internal record ImmutableResoniteLiveSessionSpecifics
{
    public string? sessionHash { get; init; }
    public string? userHashSalt { get; init; }
}