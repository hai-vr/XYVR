namespace XYVR.AccountAuthority.VRChat;

public interface IThumbnailCache
{
    public Task Save(string thumbnailUrl, byte[] data);
}