using System.Collections.Immutable;

namespace XYVR.Core;

public class XYVRSequenceHash
{
    public static int HashCodeOf<T>(ImmutableArray<T> array)
    {
        return array.Aggregate(0, (h, a) => h ^ a.GetHashCode());
    }
}