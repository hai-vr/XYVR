using ImageMagick;
using XYVR.Scaffold;

namespace XYVR.UI.Backend.AuxiliaryRepositories;

public class ProfileIllustrationRepository
{
    private readonly ProfileIllustrationStorage _storage;
    
    public ProfileIllustrationRepository(ProfileIllustrationStorage storage)
    {
        _storage = storage;
    }

    public ProfileIllustrationStorage SerializeStorage()
    {
        return _storage;
    }
    
    public async Task AssignIllustration(string individualGuid, byte[] data, string type)
    {
        await _storage.Store(individualGuid, data, type);
    }

    public async Task<ProfileIllustration?> GetOrNull(string individualGuid)
    {
        var (type, bytes) = await _storage.RetrieveOrNull(individualGuid);
        if (type == null) return null;
        
        var value = new ProfileIllustration { individualGuid = individualGuid, data = bytes, type = type };
        if (value.type.Contains("gif")) return value;

        using var image = new MagickImage(bytes);

        uint targetWidth = 150;
        uint targetHeight = 250;
        
        var scaleX = (double)image.Width / targetWidth;
        var scaleY = (double)image.Height / targetHeight;
        var scale = Math.Min(scaleX, scaleY);
        
        uint newWidth;
        uint newHeight;
        // if (scale > 1.0)
        {
            image.FilterType = FilterType.Lanczos;
            newWidth = (uint)(image.Width / scale);
            newHeight = (uint)(image.Height / scale);
            image.Resize(newWidth, newHeight);
        }
        // else
        // {
            // newWidth = image.Width;
            // newHeight = image.Height;
        // }
        
        image.Crop(targetWidth, targetHeight, Gravity.Center);
        
        image.Format = MagickFormat.Png;
            
        return value with
        {
            data = image.ToByteArray(),
            type = "image/png"
        };
    }
}

public record ProfileIllustration
{
    public string individualGuid { get; init; }
    public byte[] data { get; init; }
    public string type { get; init; }
}