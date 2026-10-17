// WP05 fixed RELEASE frame codec and golden vector. Exactly 128 bytes; no
// descriptors are attached and no target is involved.
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Launch;

public sealed class ReleaseFrameTests
{
    private static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-00000000000a");
    private static readonly Guid Issuer = new("bbbbbbbb-0000-0000-0000-00000000000b");
    private static readonly Guid Generation = new("cccccccc-0000-0000-0000-00000000000c");
    private static readonly Guid Preparation = new("dddddddd-0000-0000-0000-00000000000d");
    private const long OverallDeadline = 30_000_000_000;

    private static PreparationIdentity Identity() =>
        new(Execution, Issuer, Generation, Preparation,
            Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());

    [Fact]
    public void GoldenReleaseFrameIsExactly128Bytes()
    {
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        Assert.Equal(128, frame.Length);
        Assert.Equal("GGM1CTL\0", System.Text.Encoding.ASCII.GetString(frame, 0, 8));
        Assert.Equal(1, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(8)));
        Assert.Equal(2, BinaryPrimitives.ReadUInt16LittleEndian(frame.AsSpan(10)));
        Assert.Equal(128u, BinaryPrimitives.ReadUInt32LittleEndian(frame.AsSpan(12)));
        Assert.Equal(1ul, BinaryPrimitives.ReadUInt64LittleEndian(frame.AsSpan(16)));
        Assert.Equal(Execution, ReadRfcGuid(frame.AsSpan(24, 16)));
        Assert.Equal(Preparation, ReadRfcGuid(frame.AsSpan(40, 16)));
        Assert.Equal(Identity().ManifestDigest.AsSpan().ToArray(), frame[56..88]);
        Assert.All(frame[88..96], value => Assert.Equal(0, value));
        Assert.Equal(Generation, ReadRfcGuid(frame.AsSpan(96, 16)));
        Assert.Equal((ulong)OverallDeadline, BinaryPrimitives.ReadUInt64LittleEndian(frame.AsSpan(112)));
        Assert.All(frame[120..128], value => Assert.Equal(0, value));
        Assert.Equal(
            "90A07CDEF85B18B5B2A0360D7D01705CF7B9E4E9B2D52E7C56DA76A16201E59B",
            Convert.ToHexString(SHA256.HashData(frame)));
    }

    [Fact]
    public void ReleaseFrameDecodesWithExactIdentityAndDeadline()
    {
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        Assert.True(CoordinatorProtocol.TryDecodeRelease(
            frame, Execution, Preparation, Identity().ManifestDigest.AsSpan(),
            Generation, OverallDeadline, out var decoded, out var error), error);
        Assert.NotNull(decoded);
        Assert.Equal(1ul, decoded!.Sequence);
        Assert.Equal(Execution, decoded.ExecutionId);
        Assert.Equal(Preparation, decoded.PreparationId);
        Assert.Equal(Generation, decoded.GenerationId);
        Assert.Equal(OverallDeadline, decoded.OverallDeadlineNanoseconds);
        Assert.Equal(Identity().ManifestDigest.AsSpan().ToArray(), decoded.ManifestDigest);
    }

    [Theory]
    [InlineData(0)] // wrong magic
    [InlineData(8)] // version
    [InlineData(10)] // kind
    [InlineData(12)] // total length
    [InlineData(16)] // sequence
    [InlineData(24)] // execution
    [InlineData(40)] // preparation
    [InlineData(56)] // digest
    [InlineData(88)] // header reserved
    [InlineData(96)] // generation
    [InlineData(112)] // overall deadline
    [InlineData(120)] // payload reserved
    public void MalformedReleaseFramesFailClosed(int offset)
    {
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        frame[offset] ^= 1;
        Assert.False(CoordinatorProtocol.TryDecodeRelease(
            frame, Execution, Preparation, Identity().ManifestDigest.AsSpan(),
            Generation, OverallDeadline, out _, out _));
    }

    [Fact]
    public void TruncatedAndExtraReleaseFramesFailClosed()
    {
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        Assert.False(CoordinatorProtocol.TryDecodeRelease(
            frame[..127], Execution, Preparation, Identity().ManifestDigest.AsSpan(),
            Generation, OverallDeadline, out _, out _));
        Assert.False(CoordinatorProtocol.TryDecodeRelease(
            frame.Concat(new byte[] { 0 }).ToArray(), Execution, Preparation,
            Identity().ManifestDigest.AsSpan(), Generation, OverallDeadline, out _, out _));
    }

    [Fact]
    public void WrongExpectedIdentityOrDeadlineFailsClosed()
    {
        var frame = CoordinatorProtocol.EncodeRelease(Identity(), OverallDeadline);
        Assert.False(CoordinatorProtocol.TryDecodeRelease(
            frame, Guid.NewGuid(), Preparation, Identity().ManifestDigest.AsSpan(),
            Generation, OverallDeadline, out _, out _));
        Assert.False(CoordinatorProtocol.TryDecodeRelease(
            frame, Execution, Guid.NewGuid(), Identity().ManifestDigest.AsSpan(),
            Generation, OverallDeadline, out _, out _));
        Assert.False(CoordinatorProtocol.TryDecodeRelease(
            frame, Execution, Preparation, new byte[32],
            Generation, OverallDeadline, out _, out _));
        Assert.False(CoordinatorProtocol.TryDecodeRelease(
            frame, Execution, Preparation, Identity().ManifestDigest.AsSpan(),
            Guid.NewGuid(), OverallDeadline, out _, out _));
        Assert.False(CoordinatorProtocol.TryDecodeRelease(
            frame, Execution, Preparation, Identity().ManifestDigest.AsSpan(),
            Generation, OverallDeadline - 1, out _, out _));
    }

    [Fact]
    public void InvalidEncodeInputsRefused()
    {
        Assert.Throws<ArgumentException>(() =>
            CoordinatorProtocol.EncodeRelease(
                new PreparationIdentity(Execution, Issuer, Generation, Preparation,
                    new byte[] { 1 }), OverallDeadline));
        Assert.Throws<ArgumentException>(() =>
            CoordinatorProtocol.EncodeRelease(Identity(), 0));
    }

    private static Guid ReadRfcGuid(ReadOnlySpan<byte> bytes)
    {
        Span<byte> mixed = stackalloc byte[16];
        mixed[0] = bytes[3]; mixed[1] = bytes[2]; mixed[2] = bytes[1]; mixed[3] = bytes[0];
        mixed[4] = bytes[5]; mixed[5] = bytes[4]; mixed[6] = bytes[7]; mixed[7] = bytes[6];
        bytes[8..16].CopyTo(mixed[8..]);
        return new Guid(mixed);
    }
}
