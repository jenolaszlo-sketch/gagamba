using Gagamba.Execution.Linux.M1.Bootstrap;
using static Gagamba.Execution.Linux.M1.Tests.Bootstrap.Gava1ContextVectors;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class Gava1ContextCodecTests
{
    [Theory]
    [InlineData(4, 2, 3)]
    [InlineData(4, 3, 4)]
    [InlineData(5, 3, 2)]
    [InlineData(5, 4, 3)]
    [InlineData(6, 2, 4)]
    [InlineData(7, 4, 2)]
    public void V01_V02_ExactRoutesAndRepresentations(byte type, byte sender, byte recipient)
    {
        var frame = Pack(type, sender);
        Assert.Equal(Convert.FromHexString("67452301ab89efcd0123456789abcdef"),
            frame.AsSpan(16, 16).ToArray());
        Assert.Equal(type is 4 or 6 ? 128 : 96, frame.Length);
        var expected = Expect(type, sender, recipient);
        Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.Decode(frame, expected, out var data));
        Assert.NotNull(data);
        Assert.Equal(expected.Message, data.Message);
        Assert.Equal(expected.Sender, data.Sender);
        Assert.Equal(expected.Recipient, data.Recipient);
        Assert.Equal(WireIds, data.Originals.Ids);
        Assert.Equal(Capability, data.Originals.CapabilityDigest);
        Assert.Equal(Catalog, data.Originals.CatalogDigest);
        for (var size = 0; size < frame.Length; size++)
            Refuses(frame.AsSpan(0, size), expected, Gava1Refusal.Framing);
        Refuses([.. frame, 0], expected, Gava1Refusal.Framing);
        for (var offset = 0; offset < frame.Length; offset++)
        {
            var mutated = (byte[])frame.Clone();
            mutated[offset] ^= 0x80;
            Refuses(mutated, expected);
        }
    }

    [Fact]
    public void V01_InvalidLengthsEndianAndTrailingBytes()
    {
        foreach (var length in new byte[][] { [0, 0, 0, 64], [255, 255, 255, 255], [128, 0, 0, 0], [129, 0, 0, 0] })
        {
            var frame = Pack();
            length.CopyTo(frame, 8);
            Refuses(frame, Expect(), Gava1Refusal.Framing);
        }
        Refuses(new byte[193], Expect(), Gava1Refusal.Framing);
        Refuses([.. Pack(5, 3), .. Catalog], Expect(5, 3, 2), Gava1Refusal.Framing);
        Refuses(Pack(4, 2, WireIds, Catalog, Capability), Expect(), Gava1Refusal.Comparison);
        var sequence = Pack();
        sequence.AsSpan(12, 4).Clear();
        Refuses(sequence, Expect(), Gava1Refusal.Comparison);
        sequence.AsSpan(12, 4).Fill(255);
        Refuses(sequence, Expect(), Gava1Refusal.Comparison);
        Refuses(Pack(4, 2, WireIds with { Session = Guid.Empty }, Capability, Catalog), Expect());
    }

    [Fact]
    public void V02_V03_MissingInconsistentAndSpentContext()
    {
        var expected = Expect();
        var frame = Pack();
        foreach (var invalid in new[]
        {
            default, expected with { Originals = null }, expected with { Pending = false },
            expected with { OriginalAssociationMatches = false }, expected with { AncillaryMatches = false },
            expected with { Sequence = 0 }, expected with { Sequence = uint.MaxValue },
            expected with { Recipient = Gava1Role.Owner }, expected with { Sender = (Gava1Role)255 },
            expected with { Originals = new(WireIds with { Lease = Guid.Empty }, Capability, Catalog) },
            expected with { Originals = new(WireIds, new byte[31], Catalog) },
            expected with { Originals = new(WireIds, Capability, new byte[33]) },
        })
            Refuses(frame, invalid, Gava1Refusal.Context);
        foreach (byte type in new byte[] { 1, 2, 3, 8, 255 })
            Refuses(frame, Expect(type), Gava1Refusal.Context);
        Refuses(Pack(5, 3), Expect(5, 3, 2) with { PriorContextEstablished = false }, Gava1Refusal.Context);
        Refuses(Pack(6, 2), Expect(6, 2, 4) with { PriorContextEstablished = false }, Gava1Refusal.Context);
        Refuses(Pack(7, 4), Expect(7, 4, 2) with { PriorContextEstablished = false }, Gava1Refusal.Context);
    }

    [Fact]
    public void V03_RetainedDataOwnsComparedBytes()
    {
        var capability = Capability;
        var catalog = Catalog;
        var expected = Expect(originals: new Gava1Originals(WireIds, capability, catalog));
        Array.Clear(capability);
        Array.Clear(catalog);
        var frame = Pack();
        Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.Decode(frame, expected, out var data));
        Assert.NotNull(data);
        Array.Clear(frame);
        Array.Clear(data.Originals.CapabilityDigest);
        Array.Clear(data.Originals.CatalogDigest);
        Assert.Equal(Capability, data.Originals.CapabilityDigest);
        Assert.Equal(Catalog, data.Originals.CatalogDigest);
        Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.Decode(Pack(), expected, out _));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void V04_OriginalR2HandoffAndDistinctReleaseWitness(int topology)
    {
        var payload = ValidationPreCodecVectors.Payload(topology);
        var capability = ValidationPreCodecVectors.CapabilityDigest;
        var projection = ValidationPreCodecVectors.Projection(topology);
        var pre = new ValidationPreContext(3, (ValidationPreTopology)topology);
        Assert.Equal(ValidationPreRefusal.None,
            ValidationPreCodec.DecodeBootstrap(payload, pre, capability, projection, out var decoded));
        Assert.NotNull(decoded);
        var fields = decoded.Fields;
        var bootstrap = new Gava1PreBootstrap(fields.GetProperty("Version").GetUInt16(),
            (Gava1PreTopology)decoded.Topology, fields.GetProperty("AuthorityMode").GetString(),
            new Gava1Ids(fields.GetProperty("Session").GetGuid(), fields.GetProperty("Execution").GetGuid(),
                fields.GetProperty("Generation").GetGuid()),
            Convert.FromHexString(fields.GetProperty("AuthorityHash").GetString()!));
        var originals = new Gava1Originals(BootstrapIds, capability, Catalog);
        var external = new Gava1Originals(BootstrapIds, capability, Catalog);
        var frame = Pack(4, 3, BootstrapIds, capability, Catalog);
        Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.Decode(frame, Expect(4, 3, 4, external), out var context));
        Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.MatchPreBootstrap(bootstrap, context,
            (Gava1PreTopology)topology, originals, out var handoff));
        Assert.NotNull(handoff);
        Assert.Equal(BootstrapIds, handoff.Ids);
        Assert.Equal(capability, handoff.CapabilityDigest);
        Assert.Equal(Catalog, handoff.CatalogDigest);
        Assert.Equal(32, ReleaseWitness.Length);
        Assert.False(handoff.CapabilityDigest.AsSpan().SequenceEqual(ReleaseWitness));
        Assert.False(handoff.CatalogDigest.AsSpan().SequenceEqual(ReleaseWitness));
        var identity = topology == 1 ? fields.GetProperty("B2InstallationIdentity") :
            fields.GetProperty("Integrated").GetProperty("InstallationIdentity");
        var installationGeneration = Convert.FromHexString(identity.GetProperty("Generation").GetString()!);
        Assert.Equal(32, installationGeneration.Length);
        Assert.NotEqual(BootstrapIds.Lease, new Guid(installationGeneration.AsSpan(0, 16)));
        foreach (var invalid in new[]
        {
            default, bootstrap with { Version = 2 }, bootstrap with { Mode = null },
            bootstrap with { Topology = 0 }, bootstrap with { Capability = ReleaseWitness },
            bootstrap with { Topology = (Gava1PreTopology)(3 - topology) },
            bootstrap with { Ids = BootstrapIds with { Lease = new Guid(installationGeneration.AsSpan(0, 16)) } },
            bootstrap with { Ids = BootstrapIds with { Execution = WireIds.Execution } },
            bootstrap with { Ids = BootstrapIds with { Session = WireIds.Session } },
        })
            RefusesHandoff(invalid, context, (Gava1PreTopology)topology, originals);
        RefusesHandoff(bootstrap, context, (Gava1PreTopology)topology,
            new Gava1Originals(BootstrapIds, capability, ReleaseWitness));
        RefusesHandoff(bootstrap, null, (Gava1PreTopology)topology, originals);
        RefusesHandoff(bootstrap, context, (Gava1PreTopology)topology, null);
        Assert.NotEqual(ValidationPreRefusal.None,
            ValidationPreCodec.DecodeBootstrap(payload, pre, ReleaseWitness, projection, out var failure));
        Assert.Null(failure);
    }

    private static void Refuses(ReadOnlySpan<byte> frame, Gava1Expectation expected, Gava1Refusal? refusal = null)
    {
        var actual = Gava1ContextCodec.Decode(frame, expected, out var data);
        Assert.NotEqual(Gava1Refusal.None, actual);
        if (refusal.HasValue)
            Assert.Equal(refusal.Value, actual);
        Assert.Null(data);
    }

    private static void RefusesHandoff(Gava1PreBootstrap bootstrap, Gava1ContextData? context,
        Gava1PreTopology topology, Gava1Originals? originals)
    {
        Assert.NotEqual(Gava1Refusal.None,
            Gava1ContextCodec.MatchPreBootstrap(bootstrap, context, topology, originals, out var data));
        Assert.Null(data);
    }
}
