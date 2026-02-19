using System.Collections.Immutable;
using System.Numerics;

namespace XYVR.Core;

public record ImmutableAddressBookState
{
    private const int InitialSchema = 1; // This should be 1 and never changes.
    private const int CurrentSchema = 2;
    
    public required int schema { get; init; } = InitialSchema;
    public ImmutableArray<ImmutableIndividual> individuals { get; init; } = ImmutableArray<ImmutableIndividual>.Empty;
    public ImmutableArray<ImmutableIllustration> illustrations { get; init; } = ImmutableArray<ImmutableIllustration>.Empty;
}

// - An individual may have one main illustration, or no illustration.
// - An individual has a history of current and previous main illustrations.
// - An account may have an illustration that overrides the individual's main illustration.
// - As a side effect of unmerging accounts of an individual, two different individuals may share the same illustration.

public record ImmutableIllustration
{
    public required string guid { get; init; }
    public required Vector2 center { get; init; } = new(0.5f, 0.5f);
    public required string mediaType { get; init; }
}
