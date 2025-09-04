using System.Security.Cryptography;
using System.Text;

namespace XYVR.Scaffold;

/// We encrypt the session data file just so that in the event the user accidentally shares or uploads the session data file
/// with someone else, it's harder to read the data unless the computer itself has been compromised.<br/>
/// This is not foolproof.
public static class EncryptionOfSessionData
{
    public static string GenerateEncryptionKey()
    {
        using var aes = Aes.Create();
        
        aes.KeySize = 256;
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }

    public static string EncryptString(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        var keyBytes = Convert.FromBase64String(key);
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

        using (var aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor())
            using (var msEncrypt = new MemoryStream())
            {
                msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(plainTextBytes, 0, plainTextBytes.Length);
                }

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
        }
    }

    public static string DecryptString(string cipherText, string key)
    {
        if (string.IsNullOrEmpty(cipherText))
            return string.Empty;

        var keyBytes = Convert.FromBase64String(key);
        var cipherBytes = Convert.FromBase64String(cipherText);

        using (var aes = Aes.Create())
        {
            aes.Key = keyBytes;

            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            var encryptedData = new byte[cipherBytes.Length - iv.Length];
            Array.Copy(cipherBytes, iv.Length, encryptedData, 0, encryptedData.Length);

            using (var decryptor = aes.CreateDecryptor())
            using (var msDecrypt = new MemoryStream(encryptedData))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (var srDecrypt = new StreamReader(csDecrypt, Encoding.UTF8))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }
}