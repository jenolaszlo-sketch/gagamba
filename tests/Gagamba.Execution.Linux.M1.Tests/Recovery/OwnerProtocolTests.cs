using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class OwnerProtocolTests
{
    private static readonly Guid Execution = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Lease = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void FrameRoundTripsBoundedPayloadAndIdentity()
    {
        var sequencer = new OwnerProtocolSequencer(Execution, Lease);
        var encoded = sequencer.Create(OwnerProtocolMessageType.Start, new byte[] { 1, 2, 3 });
        var receiver = new OwnerProtocolSequencer(Execution, Lease);

        Assert.True(receiver.TryAccept(encoded, OwnerProtocolMessageType.Start, out var message, out var error), error);
        Assert.Equal((uint)1, message!.Sequence);
        Assert.Equal(new byte[] { 1, 2, 3 }, message.Payload);
    }

    [Fact]
    public void WrongIdentityTypeSequenceAndLengthAreRejected()
    {
        var sender = new OwnerProtocolSequencer(Execution, Lease);
        var first = sender.Create(OwnerProtocolMessageType.Start);

        Assert.False(OwnerProtocol.TryDecode(first, Guid.NewGuid(), Lease, out _, out _));
        var wrongTypeReceiver = new OwnerProtocolSequencer(Execution, Lease);
        Assert.False(wrongTypeReceiver.TryAccept(first, OwnerProtocolMessageType.WriterReady, out _, out _));

        var receiver = new OwnerProtocolSequencer(Execution, Lease);
        Assert.True(receiver.TryAccept(first, OwnerProtocolMessageType.Start, out _, out _));
        Assert.False(receiver.TryAccept(first, OwnerProtocolMessageType.Start, out _, out _));
        Assert.False(OwnerProtocol.TryDecode(first.AsSpan(0, first.Length - 1), Execution, Lease, out _, out _));
    }

    [Fact]
    public void OversizedPayloadCannotBeEncoded()
    {
        var sender = new OwnerProtocolSequencer(Execution, Lease);
        Assert.Throws<ArgumentException>(() => sender.Create(
            OwnerProtocolMessageType.Start,
            new byte[OwnerProtocol.MaxPayloadBytes + 1]));
    }

    [Fact]
    public void PreparationSpecificationRoundTripsAndBuildsMatchingIntent()
    {
        var specification = new OwnerPreparationSpecification(
            Path.GetFullPath("journal"), "domain-a", Execution, Lease, "boot-a", Guid.NewGuid(), Guid.NewGuid(),
            Path.GetFullPath("source"), Path.GetFullPath("staging"), 12, 34, "prep-a",
            new[] { new CopyObjectPlan("tool", "tool.bin", 4096, null) }, 8192, 8,
            Array.Empty<KeyValuePair<string, string>>(), 100_000_000);

        var encoded = OwnerPreparationSpecificationCodec.Encode(specification);
        Assert.True(OwnerPreparationSpecificationCodec.TryDecode(encoded, out var decoded, out var error), error);
        Assert.Equal(encoded, OwnerPreparationSpecificationCodec.Encode(decoded!));
        var intent = OwnerPreparationSpecificationCodec.BuildIntent(decoded!);
        Assert.Equal(PreparationStage.AllocationIntent, intent.Stage);
        Assert.Equal((ulong)12, intent.StagingParentDevice);
        Assert.Equal((ulong)34, intent.StagingParentInode);
        Assert.Equal(3, intent.Allocations.Count);
        Assert.Equal("tool-000.staged", intent.Allocations[2].Name);
    }

    [Fact]
    public void PreparationSpecificationRejectsRelativePathsAndUnknownBarriers()
    {
        var invalid = new OwnerPreparationSpecification(
            "relative-journal", "domain-a", Execution, Lease, "boot-a", Guid.NewGuid(), Guid.NewGuid(),
            Path.GetFullPath("source"), Path.GetFullPath("staging"), 12, 34, "prep-a",
            new[] { new CopyObjectPlan("tool", "tool.bin", 4096, null) }, 8192, 8,
            Array.Empty<KeyValuePair<string, string>>(), 100_000_000, "unlisted");

        Assert.Throws<ArgumentException>(() => OwnerPreparationSpecificationCodec.Encode(invalid));
    }

    [Fact]
    public void ManifestAuthorizationRoundTripsAndBindsItsDigest()
    {
        var issuer = Guid.NewGuid();
        var generation = Guid.NewGuid();
        var preparation = Guid.NewGuid();
        var manifest = Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests.SampleManifest();
        var identity = new PreparationIdentity(
            Execution, issuer, generation, preparation, ManifestCodec.ComputeDigestBytes(manifest));
        var authorization = new OwnerManifestAuthorization(manifest, identity);

        var encoded = OwnerPreparationSpecificationCodec.EncodeAuthorization(authorization);
        Assert.True(OwnerPreparationSpecificationCodec.TryDecodeAuthorization(
            encoded, out var decoded, out var error), error);
        Assert.Equal(ManifestCodec.ComputeDigestHex(manifest),
            ManifestCodec.ComputeDigestHex(decoded!.Manifest));
        Assert.Equal(preparation, decoded.Identity.PreparationId);
    }
}
