namespace XYVR.AccountAuthority.Cluster;

[Serializable]
internal record ClusterAuthStorage
{
    public required string bearer { get; init; }
    public required string version { get; init; }
    public required string build { get; init; }
}