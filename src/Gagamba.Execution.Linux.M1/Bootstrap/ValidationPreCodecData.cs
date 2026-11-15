using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal enum ValidationPreTopology
{
    B2 = 1,
    Integrated = 2
}

internal readonly record struct ValidationPreContext(ushort Version, ValidationPreTopology Topology)
{
    internal const string ProfileId = "ae4-linux-fixed-fixture-v1";
    internal const string ProfileVersion = "1";

    internal bool IsPre => Version == 3 && Topology is ValidationPreTopology.B2 or ValidationPreTopology.Integrated;
}

internal enum ValidationPreRefusal
{
    None,
    Context,
    Size,
    Encoding,
    Schema,
    Expectation,
    Comparison,
    Relationship,
    Layout,
    PeerRefusal
}

internal sealed class ValidationPreBootstrapData(ValidationPreTopology topology, JsonElement fields)
{
    internal ValidationPreTopology Topology { get; } = topology;
    internal JsonElement Fields { get; } = fields;
}

internal enum ValidationPreMessage
{
    RecoveryEcho = 9,
    RecoveryEchoResponse = 10,
    B2OwnerEchoAccepted = 12,
    OwnerEcho = 36,
    B2OwnerExpectation = 37
}

internal sealed class ValidationPreBinaryExpectations(ReadOnlyMemory<byte> artifactSetSha256)
{
    private readonly ReadOnlyMemory<byte> artifactSet = Snapshot(artifactSetSha256);

    // ReadOnlyMemory by itself does not own its backing array. Copy both at intake
    // and when returning the memory so neither reference can modify the binding.
    internal ReadOnlyMemory<byte> ArtifactSetSha256 => Snapshot(artifactSet);
    internal ReadOnlyMemory<byte> RecoveryClosureSha256
    {
        get => Snapshot(field);
        init => field = Snapshot(value);
    }
    internal ReadOnlyMemory<byte> OwnerClosureSha256
    {
        get => Snapshot(field);
        init => field = Snapshot(value);
    }
    internal ReadOnlyMemory<byte> CoordinatorSha256
    {
        get => Snapshot(field);
        init => field = Snapshot(value);
    }
    internal ReadOnlyMemory<byte> SeccompFilterSha256
    {
        get => Snapshot(field);
        init => field = Snapshot(value);
    }
    internal ReadOnlyMemory<byte> RuntimeInventorySha256
    {
        get => Snapshot(field);
        init => field = Snapshot(value);
    }

    private static ReadOnlyMemory<byte> Snapshot(ReadOnlyMemory<byte> digest) =>
        digest.Length == 32 ? digest.ToArray() : ReadOnlyMemory<byte>.Empty;
    internal Guid Session { get; init; }
    internal string? ProfileId { get; init; }
    internal string? ProfileVersion { get; init; }
}

// The caller supplies the already selected payload context and prior-message facts.
internal readonly record struct ValidationPreBinaryContext(
    ValidationPreContext Pre, ValidationPreMessage Message, bool MatchingPredecessor = false);
