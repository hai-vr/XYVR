using System.Security.Cryptography;
using System.Text;
using XYVR.AccountAuthority.VRChat;
using XYVR.Core;

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

    public Task<bool> ExistsInCache(string thumbnailUrl)
    {
        var sha__mustNotContainPathTraversal = Sha(thumbnailUrl);
        var path = Path.Combine(_thumbnailCacheFolderPath, sha__mustNotContainPathTraversal);

        var existsInCache = File.Exists(path);
        return Task.FromResult(existsInCache);
    }

    public static string Sha(string thumbnailUrl)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(thumbnailUrl));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    public async Task<byte[]?> GetByShaOrNull(string sha__mustNotContainPathTraversal)
    {
        if (ContainsPathTraversalElements(sha__mustNotContainPathTraversal))
        {
            throw new ArgumentException("Sha must not contain path traversal characters.");
        }
        var path = Path.Combine(_thumbnailCacheFolderPath, sha__mustNotContainPathTraversal);

        if (!File.Exists(path)) return null;
        return await File.ReadAllBytesAsync(path);
    }

    public static bool ContainsPathTraversalElements(string mustNotContainPathTraversal)
    {
        return mustNotContainPathTraversal.Contains("/")
               || mustNotContainPathTraversal.Contains("\\")
               || mustNotContainPathTraversal.Contains(".");
    }

    public void KeepOnly(HashSet<string> keep)
    {
        var fullFilePathsToDelete = Directory.EnumerateFiles(_thumbnailCacheFolderPath)
            .Where(fullFilePath =>
            {
                var fileName = Path.GetFileName(fullFilePath);
                
                var makeSureThisIsReallyAShaFilename = fileName.Length == 64;
                return makeSureThisIsReallyAShaFilename && !keep.Contains(fileName);
            })
            .ToList();
        
        foreach (var fullFilePath in fullFilePathsToDelete)
        {
            XYVRLogging.WriteLine(this, $"Deleting unused thumbnail {fullFilePath}");
            File.Delete(fullFilePath);
        }
    }
}