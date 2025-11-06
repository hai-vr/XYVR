namespace XYVR.UI.Backend.AuxiliaryRepositories;

public class ProfileIllustrationRepository
{
    private readonly Dictionary<string, ProfileIllustration> _illustrations = new();
    
    public async Task AssignIllustration(string individualGuid, byte[] data, string type)
    {
        _illustrations[individualGuid] = new ProfileIllustration { individualGuid = individualGuid, data = data, type = type };
    }

    public async Task<ProfileIllustration?> GetOrNull(string individualGuid)
    {
        return _illustrations.GetValueOrDefault(individualGuid);
    }
}

public record ProfileIllustration
{
    public string individualGuid { get; init; }
    public byte[] data { get; init; }
    public string type { get; init; }
}