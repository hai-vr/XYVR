namespace XYVR.AccountAuthority.ChilloutVR;

#pragma warning disable CS8618
#pragma warning disable 0649
[Serializable]
internal class CvrContactsResponse
{
    public string? message;
    public CvrContactsResponseData[] data;
}

[Serializable]
internal class CvrContactsResponseData
{
    public object[] categories;
    public string id;
    public string name;
    public string imageUrl;
}
#pragma warning restore 0649
#pragma warning restore CS8618