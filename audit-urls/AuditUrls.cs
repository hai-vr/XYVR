namespace XYVR.API.Audit;

public static class AuditUrls
{
    // https://github.com/vrchatapi/specification/commit/558c0ca50202c45194a49d515f27e64f62079ba4#diff-5fa520d3bb34f9ae444cdbdf2b9eccff2361eb89a0cd3f4dba1e2e0fa9bba452R15
    // https://discord.com/channels/418093857394262020/418512124529344523/1303873667473866752
    // "Yes, going forward, all API requests need to go through api.vrchat.cloud instead"
    /*
- Hai:
    Hello, is there an official source that proves that https://api.vrchat.cloud is really the official endpoint that is supposed to be used by consumers of the API?
    I was trying to audit my own code, and it was using api.vrchat.cloud, and I could have sworn the official website UI used that back in December 2024, but it no longer appears to be the case? 
- Aries:
    It's in a private server VRChat uses for them to communicate breaking changes to us.
    https://files.aries.fyi/2025/09/04/282867529a3fd6b5.png
    
    They wanted to segment traffic, the site uses vrchat.com/api, which has aggressive anti-bot Cloudflare rules applied to it.
    While api.vrchat.cloud has the self identification user agent rule applied, & other rate limits.
    
https://discord.com/channels/418093857394262020/418512124529344523/1413288167846707310
    */
    public const string VrcApiUrl = "https://api.vrchat.cloud/api/1";
    public const string VrcCookieDomainBit = "api.vrchat.cloud";
    public const string VrcCookieDomain = $"https://{VrcCookieDomainBit}";
    // (sub-domain of above) https://vrchat.community/websocket#:~:text=VRChat%20webhook%20server%20is%20done%20via%20the%20URL
    public const string VrcWebsocketUrl = "wss://pipeline.vrchat.cloud/";
    
    // https://wiki.resonite.com/API#:~:text=communicate%20with%20Resonite.-,The%20main%20API%20URL%20is,-https%3A//api.resonite
    public const string ResoniteApiUrl = "https://api.resonite.com";
    // https://wiki.resonite.com/API#:~:text=be%20easily%20emulated.-,The%20hub%20address%20is,-https%3A//api.resonite
    public const string ResoniteHubUrl = "https://api.resonite.com/hub";
    
    public const string ChilloutVrApiUrlV1 = "https://api.chilloutvr.net/1";
    public const string ChilloutVrWebsocketUrl = "wss://api.chilloutvr.net/1/users/ws";
    
    public const string ClusterApiUrlV1 = "https://api.cluster.mu/v1";
    
    // We're not sure how much to trust thumbnail URLs coming from Resonite API endpoints. As a precaution,
    // only accept URLs if the host is equal to this, of if the host is a subdomain of this.
    // Most thumbnails seem to come from skyfrost-archive.resonite.com as this time of writing.
    public const string ResoniteSessionThumbnailsPermittedHostAndSubdomainHost = "resonite.com";
    public const string ClusterAllowedThumbnailUrl = "https://cluster-file-storage.imgix.net/";
}