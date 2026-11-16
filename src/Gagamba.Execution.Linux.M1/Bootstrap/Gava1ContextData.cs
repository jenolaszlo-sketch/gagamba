namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal enum Gava1Message : byte { Init = 4, ContextAck, DirectContext, DirectAck }
internal enum Gava1Role : byte { Launcher = 1, Controller, Recovery, Owner }
internal enum Gava1Refusal { None, Context, Framing, Comparison }
internal enum Gava1PreTopology { B2 = 1, Integrated }

internal readonly record struct Gava1Ids(Guid Session, Guid Execution, Guid Lease)
{
    internal bool Complete => Session != Guid.Empty && Execution != Guid.Empty && Lease != Guid.Empty;
}

internal sealed class Gava1Originals
{
    private readonly byte[] capability;
    private readonly byte[] catalog;

    internal Gava1Originals(Gava1Ids ids, ReadOnlySpan<byte> capability, ReadOnlySpan<byte> catalog)
    {
        Ids = ids;
        this.capability = capability.Length == 32 ? capability.ToArray() : [];
        this.catalog = catalog.Length == 32 ? catalog.ToArray() : [];
    }

    internal Gava1Ids Ids { get; }
    internal bool Complete => Ids.Complete && capability.Length == 32 && catalog.Length == 32;
    internal byte[] CapabilityDigest => (byte[])capability.Clone();
    internal byte[] CatalogDigest => (byte[])catalog.Clone();
    internal bool MatchesCapability(ReadOnlySpan<byte> bytes) => bytes.SequenceEqual(capability);
    internal bool MatchesCatalog(ReadOnlySpan<byte> bytes) => bytes.SequenceEqual(catalog);

    internal bool Matches(Gava1Originals other) =>
        Ids == other.Ids && other.MatchesCapability(capability) && other.MatchesCatalog(catalog);
}

// These are caller-supplied premises for structural comparison, with no authority attached.
internal readonly record struct Gava1Expectation(
    Gava1Message Message, Gava1Role Sender, Gava1Role Recipient, uint Sequence,
    Gava1Originals? Originals, bool Pending, bool OriginalAssociationMatches,
    bool AncillaryMatches, bool PriorContextEstablished);

internal sealed record Gava1ContextData(
    Gava1Message Message, Gava1Role Sender, Gava1Role Recipient, Gava1Originals Originals);

internal readonly record struct Gava1PreBootstrap(
    ushort Version, Gava1PreTopology Topology, string? Mode, Gava1Ids Ids, ReadOnlyMemory<byte> Capability);
