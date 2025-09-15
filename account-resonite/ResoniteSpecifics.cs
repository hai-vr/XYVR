using System.Collections.Immutable;
using XYVR.Core;

namespace XYVR.AccountAuthority.Resonite;

internal record ImmutableResoniteLiveSessionSpecifics
{
    public string? sessionHash { get; init; }
    public string? userHashSalt { get; init; }
    public required ImmutableArray<string> sessionHashes { get; init; }

    public virtual bool Equals(ImmutableResoniteLiveSessionSpecifics? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return sessionHash == other.sessionHash
               && userHashSalt == other.userHashSalt
               && sessionHashes.SequenceEqual(other.sessionHashes);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (sessionHash != null ? sessionHash.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (userHashSalt != null ? userHashSalt.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (XYVRSequenceHash.HashCodeOf(sessionHashes));
            return hashCode;
        }
    }
}