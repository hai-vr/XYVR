using System.Text;
using core;

namespace XYVR.Scaffold;

[Obsolete("This stores cookies in a single file. This does not support multi-accounts, and therefore it is flawed")]
public class FlawedSingleGlobalCookieCredentialsStorage : ICredentialsStorage
{
    private const string CookieFileName = "vrc.cookies.txt";
    
    public async Task<string?> RequireCookieOrToken()
    {
        if (!File.Exists(CookieFileName)) return null;

        return await File.ReadAllTextAsync(CookieFileName, Encoding.UTF8);
    }

    public async Task StoreCookieOrToken(string cookie__sensitive)
    {
        await File.WriteAllTextAsync(CookieFileName, cookie__sensitive, Encoding.UTF8);
    }
}