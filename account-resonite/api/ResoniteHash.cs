using System.Security.Cryptography;
using System.Text;

namespace XYVR.AccountAuthority.Resonite;

public static class ResoniteHash
{
    public static async Task<string> Rehash(string theThingToHash, string theSalt)
    {
        return await InternalRehash(theThingToHash + theSalt);
    }

    private static async Task<string> InternalRehash(string id)
    {
        var idBytes = Encoding.UTF8.GetBytes(id);
        
        using var sha256 = SHA256.Create();
        using var idByteStream = new MemoryStream(idBytes);
        
        var hashBytes = await sha256.ComputeHashAsync(idByteStream);
        
        return Convert.ToHexString(hashBytes).ToUpperInvariant();
    }
}