using System.Security.Cryptography;
using System.Text;
using XYVR.AccountAuthority.VRChat;

namespace XYVR.Scaffold;

public class VRChatThumbnailCache : IThumbnailCache
{
    private readonly string _thumbnailCacheFolderPath;

    public VRChatThumbnailCache(string thumbnailCacheFolderPath)
    {
        _thumbnailCacheFolderPath = thumbnailCacheFolderPath;
    }

    public async Task Save(string thumbnailUrl, byte[] data)
    {
        var sha__mustNotContainPathTraversal = Sha(thumbnailUrl);
        var path = Path.Combine(_thumbnailCacheFolderPath, sha__mustNotContainPathTraversal);
        
        await File.WriteAllBytesAsync(path, data);
    }

    private string Sha(string thumbnailUrl)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(thumbnailUrl));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    public async Task<byte[]?> GetOrNull(string thumbnailUrl)
    {
        var sha__mustNotContainPathTraversal = Sha(thumbnailUrl);
        var path = Path.Combine(_thumbnailCacheFolderPath, sha__mustNotContainPathTraversal);

        if (!File.Exists(path)) return null;
        return await File.ReadAllBytesAsync(path);
    }
}