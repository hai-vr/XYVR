namespace XYVR.Core;

public interface ICredentialsStorage
{
    public Task<string?> RequireCookieOrToken();
    public Task StoreCookieOrToken(string cookie__sensitive);
}