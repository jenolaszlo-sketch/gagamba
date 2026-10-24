using System.Collections.Immutable;

namespace Gagamba.Execution.Restricted.Contracts;

/// <summary>Passive correlation only. No expiry, liveness, establishment or permission to execute.
/// D4a timing comes from explicit enrolled/synthetic data; later D4b timing comes only from
/// an actual provider-origin prepared execution.</summary>
public sealed class RestrictedPreparationReference : IEquatable<RestrictedPreparationReference>
{
    public RestrictedPreparationReference(Guid executionId, Guid issuerId, Guid generationId,
        Guid preparationId, ReadOnlySpan<byte> manifestSha256)
    {
        if (executionId == Guid.Empty) throw new ArgumentException("A nonempty GUID is required.", nameof(executionId));
        if (issuerId == Guid.Empty) throw new ArgumentException("A nonempty GUID is required.", nameof(issuerId));
        if (generationId == Guid.Empty) throw new ArgumentException("A nonempty GUID is required.", nameof(generationId));
        if (preparationId == Guid.Empty) throw new ArgumentException("A nonempty GUID is required.", nameof(preparationId));
        if (manifestSha256.Length != 32) throw new ArgumentException("Exactly 32 bytes are required.", nameof(manifestSha256));
        ExecutionId = executionId;
        IssuerId = issuerId;
        GenerationId = generationId;
        PreparationId = preparationId;
        ManifestSha256 = ImmutableArray.Create(manifestSha256);
    }

    public Guid ExecutionId { get; }
    public Guid IssuerId { get; }
    public Guid GenerationId { get; }
    public Guid PreparationId { get; }
    public ImmutableArray<byte> ManifestSha256 { get; }

    public bool Equals(RestrictedPreparationReference? other) => other is not null &&
        ExecutionId == other.ExecutionId && IssuerId == other.IssuerId &&
        GenerationId == other.GenerationId && PreparationId == other.PreparationId &&
        ManifestSha256.AsSpan().SequenceEqual(other.ManifestSha256.AsSpan());

    public override bool Equals(object? obj) => obj is RestrictedPreparationReference other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ExecutionId); hash.Add(IssuerId); hash.Add(GenerationId); hash.Add(PreparationId);
        foreach (var value in ManifestSha256) hash.Add(value);
        return hash.ToHashCode();
    }
}
