using System.Buffers.Binary;
using System.Text;
using Gagamba.Execution.Linux.M1.Launch;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Launch;

public sealed class CoordinatorProtocolQualificationTests
{
    private static readonly Guid Execution = Guid.Parse("f0824257-f9d0-4238-9210-642197e180a1");
    private static readonly Guid Preparation = Guid.Parse("a6b34322-b90a-4a9c-90eb-c2da2a0a77f3");
    private static readonly byte[] Digest = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();

    private static byte[] Frame(ushort kind = 1, byte[]? payload = null)
    {
        payload ??= Array.Empty<byte>();
        var result = new byte[CoordinatorProtocol.HeaderBytes + payload.Length];
        Encoding.ASCII.GetBytes("GGM1CTL\0").CopyTo(result, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(10), kind);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(12), checked((uint)result.Length));
        BinaryPrimitives.WriteUInt64LittleEndian(result.AsSpan(16), 1);
        CoordinatorProtocol.GuidToRfcBytes(Execution).CopyTo(result, 24);
        CoordinatorProtocol.GuidToRfcBytes(Preparation).CopyTo(result, 40);
        Digest.CopyTo(result, 56);
        payload.CopyTo(result, CoordinatorProtocol.HeaderBytes);
        return result;
    }

    private static bool Decode(byte[] frame) => CoordinatorProtocol.TryDecode(
        frame, Execution, Preparation, Digest, 1, out _, out _);

    [Fact]
    public void RejectsIncompleteCoordinatorFrame()
    {
        Assert.False(Decode(Frame()[..(CoordinatorProtocol.HeaderBytes - 1)]));
    }

    [Fact]
    public void RejectsMalformedCoordinatorHeader()
    {
        var frame = Frame();
        frame[0] ^= 0xff;
        Assert.False(Decode(frame));
    }

    [Fact]
    public void RejectsWrongCoordinatorMessageType()
    {
        Assert.False(Decode(Frame(kind: 2)));
    }

    [Theory]
    [InlineData("execution")]
    [InlineData("preparation")]
    [InlineData("generation-digest")]
    [InlineData("sequence")]
    public void RejectsWrongCoordinatorIdentityOrSequence(string field)
    {
        var frame = Frame();
        switch (field)
        {
            case "execution": frame[24] ^= 1; break;
            case "preparation": frame[40] ^= 1; break;
            case "generation-digest": frame[56] ^= 1; break;
            case "sequence": BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(16), 2); break;
        }

        Assert.False(Decode(frame));
    }

    [Fact]
    public void RejectsCoordinatorFrameLengthMismatchAndReservedBits()
    {
        var wrongLength = Frame();
        BinaryPrimitives.WriteUInt32LittleEndian(wrongLength.AsSpan(12), (uint)wrongLength.Length + 1);
        Assert.False(Decode(wrongLength));

        var reserved = Frame();
        reserved[88] = 1;
        Assert.False(Decode(reserved));
    }

    [Fact]
    public void RejectsMalformedHeldReportAndIncompleteNamespaceIdentity()
    {
        var payload = MinimalHeldPayload();
        payload[2] = 0;
        Assert.True(CoordinatorProtocol.TryDecode(Frame(payload: payload), Execution, Preparation,
            Digest, 1, out var decoded, out var error), error);
        Assert.False(CoordinatorProtocol.TryDecodeHeldReport(decoded!, out _, out _));

        payload = MinimalHeldPayload();
        var frame = Frame(payload: payload);
        Array.Clear(frame, CoordinatorProtocol.HeaderBytes + 96, sizeof(ulong));
        Assert.True(CoordinatorProtocol.TryDecode(frame, Execution, Preparation, Digest, 1,
            out decoded, out error), error);
        Assert.False(CoordinatorProtocol.TryDecodeHeldReport(decoded!, out _, out _));
    }

    [Theory]
    [InlineData("cgroup-length")]
    [InlineData("mount-length")]
    public void RejectsInvalidHeldReportTextLengths(string field)
    {
        var payload = MinimalHeldPayload();
        if (field == "cgroup-length")
            BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(192), 257);
        else
            BinaryPrimitives.WriteUInt16LittleEndian(
                payload.AsSpan(194 + Encoding.UTF8.GetByteCount("/delegated/domain")), 0);

        Assert.True(CoordinatorProtocol.TryDecode(Frame(payload: payload), Execution, Preparation,
            Digest, 1, out var decoded, out var error), error);
        Assert.False(CoordinatorProtocol.TryDecodeHeldReport(decoded!, out _, out _));
    }

    private static byte[] MinimalHeldPayload()
    {
        var cgroup = Encoding.UTF8.GetBytes("/delegated/domain");
        var mount = Encoding.UTF8.GetBytes("invalid mount report");
        var payload = new byte[194 + 2 + cgroup.Length + 2 + mount.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(payload, 1);
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(2), 0x000f);
        for (var i = 0; i < 6; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(96 + i * 8), (ulong)(100 + i));
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(192), (ushort)cgroup.Length);
        cgroup.CopyTo(payload, 194);
        var mountLengthAt = 194 + cgroup.Length;
        BinaryPrimitives.WriteUInt16LittleEndian(payload.AsSpan(mountLengthAt), (ushort)mount.Length);
        mount.CopyTo(payload, mountLengthAt + 2);
        return payload;
    }
}
