// WP05 v3 durable schema codec and invariant tests. These tests are pure and
// never execute a target, start an owner or exercise release transport.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class OwnershipV3CodecTests
{
    [Fact]
    public void StableDurableEnumValuesMatchAcceptedNumbers()
    {
        Assert.Equal(0, (byte)ReleaseTag.Absent);
        Assert.Equal(1, (byte)ReleaseTag.Intent);
        Assert.Equal(2, (byte)ReleaseTag.Committed);

        Assert.Equal(0, (byte)DeliveryResult.NotAttempted);
        Assert.Equal(1, (byte)DeliveryResult.Delivered);
        Assert.Equal(2, (byte)DeliveryResult.WouldBlock);
        Assert.Equal(3, (byte)DeliveryResult.BrokenPipe);
        Assert.Equal(4, (byte)DeliveryResult.Partial);
        Assert.Equal(5, (byte)DeliveryResult.Unknown);

        Assert.Equal(0, (byte)StopCauseKind.CallerStop);
        Assert.Equal(7, (byte)StopCauseKind.TransportFailure);

        Assert.Equal(0, (byte)ExecTag.NotAttempted);
        Assert.Equal(1, (byte)ExecTag.FailedBeforeExec);
        Assert.Equal(2, (byte)ExecTag.Confirmed);
        Assert.Equal(3, (byte)ExecTag.Unknown);

        Assert.Equal(0, (byte)ExecBasis.None);
        Assert.Equal(1, (byte)ExecBasis.CoordinatorFrame);
        Assert.Equal(2, (byte)ExecBasis.ExecutableObjectIdentity);
        Assert.Equal(3, (byte)ExecBasis.ReviewedNormalExitRule);

        Assert.Equal(0, (byte)RootTag.NotObserved);
        Assert.Equal(1, (byte)RootTag.Observed);

        Assert.Equal(0, (byte)RootKind.Unknown);
        Assert.Equal(1, (byte)RootKind.NotStarted);
        Assert.Equal(2, (byte)RootKind.Exited);
        Assert.Equal(3, (byte)RootKind.Signalled);
    }

    [Fact]
    public void Version2RecordRemainsLegacyAndAccepted()
    {
        var snapshot = RecoveryTestSupport.Snapshot() with { Sequence = 1 };
        var bytes = OwnershipSnapshotCodec.Encode(snapshot);
        Assert.Equal((byte)OwnershipSnapshotCodec.LegacyVersion, bytes[0]);
        Assert.Equal(0, bytes[1]);
        Assert.Equal(
            "5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE",
            Convert.ToHexString(SHA256.HashData(bytes)));
        Assert.True(OwnershipSnapshotCodec.TryDecode(bytes, out var decoded, out var error), error);
        Assert.NotNull(decoded);
        Assert.Equal(OwnershipSnapshotCodec.LegacyVersion, decoded!.Version);
        Assert.Null(decoded.Release);
        Assert.Null(decoded.Stop);
        Assert.Null(decoded.Exec);
        Assert.Null(decoded.Root);
        Assert.Equal(PreparationStage.AllocationRegistered, decoded.Stage);
        Assert.Equal(RecoveryTestSupport.Execution, decoded.ExecutionId);
        Assert.Equal("boot-1", decoded.BootId);
        Assert.Equal(200UL, decoded.DomainInode);
    }

    [Fact]
    public void V3AllFactsRoundTripAndExactTailBytes()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            stop: RecoveryTestSupport.Stop(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootExited(rawWait: true));
        var bytes = OwnershipSnapshotCodec.Encode(snapshot);
        Assert.Equal((byte)OwnershipSnapshotCodec.CurrentVersion, bytes[0]);
        var tail = TailOffset(bytes);
        Assert.Equal((byte)ReleaseTag.Committed, bytes[tail]);
        Assert.Equal((byte)DeliveryResult.Delivered, bytes[tail + 33]);
        Assert.Equal(1, bytes[tail + 34]);
        Assert.Equal((byte)StopCauseKind.CallerStop, bytes[tail + 43]);
        Assert.Equal((byte)ExecTag.Confirmed, bytes[tail + 52]);
        Assert.Equal((byte)ExecBasis.CoordinatorFrame, bytes[tail + 53]);
        Assert.Equal((byte)RootTag.Observed, bytes[tail + 54]);
        Assert.Equal((byte)RootKind.Exited, bytes[tail + 55]);

        Assert.True(OwnershipSnapshotCodec.TryDecode(bytes, out var decoded, out var error), error);
        Assert.NotNull(decoded);
        Assert.Equal(OwnershipSnapshotCodec.CurrentVersion, decoded!.Version);
        Assert.Equal(ReleaseTag.Committed, decoded.Release!.Tag);
        Assert.Equal(DeliveryResult.Delivered, decoded.Release.DeliveryResult);
        Assert.Equal(2_000, decoded.Release.CommittedAtNanoseconds);
        Assert.Equal(StopCauseKind.CallerStop, decoded.Stop!.Cause);
        Assert.Equal(ExecTag.Confirmed, decoded.Exec!.Tag);
        Assert.Equal(ExecBasis.CoordinatorFrame, decoded.Exec.Basis);
        Assert.Equal(RootKind.Exited, decoded.Root!.Kind);
        Assert.True(decoded.Root.RawWaitPresent);
        Assert.Equal(0, decoded.Root.RawWaitStatus);
        Assert.Equal(RecoveryTestSupport.Execution, decoded.Preparation!.ExecutionId);
        Assert.Equal(RecoveryTestSupport.Issuer, decoded.Preparation.IssuerId);
        Assert.Equal(RecoveryTestSupport.Generation, decoded.Preparation.GenerationId);
        Assert.Equal(RecoveryTestSupport.Preparation, decoded.Preparation.PreparationId);
        Assert.Equal(32, decoded.Preparation.ManifestDigest.Length);
    }

    [Fact]
    public void LazyEncodingKeepsNoFactSnapshotsAtVersion2AndUpgradesOnFirstFact()
    {
        var legacy = RecoveryTestSupport.Snapshot();
        Assert.Equal((byte)OwnershipSnapshotCodec.LegacyVersion, OwnershipSnapshotCodec.Encode(legacy)[0]);

        var withStop = RecoveryTestSupport.V3Snapshot(stop: RecoveryTestSupport.Stop());
        Assert.Equal((byte)OwnershipSnapshotCodec.CurrentVersion, OwnershipSnapshotCodec.Encode(withStop)[0]);

        var withIntent = RecoveryTestSupport.V3Snapshot(release: RecoveryTestSupport.ReleaseIntent());
        Assert.Equal((byte)OwnershipSnapshotCodec.CurrentVersion, OwnershipSnapshotCodec.Encode(withIntent)[0]);
    }

    [Fact]
    public void UnsupportedVersionFailsClosedEvenWithValidChecksum()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecConfirmed());
        var bytes = OwnershipSnapshotCodec.Encode(snapshot);
        var mutated = MutateAndRechecksum(bytes, 0, 4, 0);
        Assert.False(OwnershipSnapshotCodec.TryDecode(mutated, out _, out var error));
        Assert.Contains("unsupported snapshot version", error);
    }

    [Fact]
    public void InvalidDurableEnumBytesFailClosed()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            stop: RecoveryTestSupport.Stop(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootExited(rawWait: true));
        var bytes = OwnershipSnapshotCodec.Encode(snapshot);
        var tail = TailOffset(bytes);

        AssertRefused(bytes, tail, "invalid release tag"); // Wait: tail is release tag offset 0.
        AssertRefused(bytes, tail + 33, "delivery result");
        AssertRefused(bytes, tail + 43, "stop cause");
        AssertRefused(bytes, tail + 52, "exec tag");
        AssertRefused(bytes, tail + 53, "basis");
        AssertRefused(bytes, tail + 54, "root tag");
        AssertRefused(bytes, tail + 55, "root kind");
    }

    [Fact]
    public void InvalidManagedCombinationsFailClosed()
    {
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(delivery: DeliveryResult.NotAttempted)));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: new ReleaseRecord(ReleaseTag.Intent, 1_000, 10_000, 30_000, 2_000)));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseIntent(),
            exec: RecoveryTestSupport.ExecConfirmed()));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecUnknown(),
            root: RecoveryTestSupport.RootExited()));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootNotStarted()));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecFailed(),
            root: RecoveryTestSupport.RootUnknown()));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: new ExecObservation(ExecTag.FailedBeforeExec, 0)));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: new ExecObservation(ExecTag.Confirmed, Basis: ExecBasis.None)));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: new RootObservation(RootKind.Exited, RawWaitPresent: true, RawWaitStatus: 1)));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: new RootObservation(RootKind.Signalled, Signal: 0)));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            stop: new StopRecord(2_000, StopCauseKind.CallerStop, 2_001)));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            stop: new StopRecord(2_000, (StopCauseKind)99,
                2_000 + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds)));
        AssertInvalid(RecoveryTestSupport.Snapshot() with
        {
            Version = OwnershipSnapshotCodec.LegacyVersion,
            Release = RecoveryTestSupport.ReleaseIntent(),
        });
        AssertInvalid(RecoveryTestSupport.Snapshot() with
        {
            Version = OwnershipSnapshotCodec.CurrentVersion,
            Stage = PreparationStage.Prepared,
            Release = RecoveryTestSupport.ReleaseIntent(),
            Preparation = null,
        });
    }

    public static IEnumerable<object[]> ValidObservationStates() =>
        new[]
        {
            RecoveryTestSupport.V3Snapshot(release: RecoveryTestSupport.ReleaseIntent()),
            RecoveryTestSupport.V3Snapshot(release: RecoveryTestSupport.ReleaseCommitted()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                exec: RecoveryTestSupport.ExecFailed(),
                root: RecoveryTestSupport.RootNotStarted()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                exec: RecoveryTestSupport.ExecConfirmed()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                exec: RecoveryTestSupport.ExecConfirmed(),
                root: RecoveryTestSupport.RootUnknown()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                exec: RecoveryTestSupport.ExecConfirmed(),
                root: RecoveryTestSupport.RootExited()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                exec: RecoveryTestSupport.ExecConfirmed(),
                root: RecoveryTestSupport.RootSignalled()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                exec: RecoveryTestSupport.ExecUnknown(),
                root: RecoveryTestSupport.RootUnknown()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(delivery: DeliveryResult.Unknown),
                exec: RecoveryTestSupport.ExecConfirmed(),
                root: RecoveryTestSupport.RootExited()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(delivery: DeliveryResult.Partial),
                exec: RecoveryTestSupport.ExecUnknown(),
                root: RecoveryTestSupport.RootUnknown()),
            RecoveryTestSupport.V3Snapshot(
                release: RecoveryTestSupport.ReleaseCommitted(),
                stop: RecoveryTestSupport.Stop(),
                exec: RecoveryTestSupport.ExecConfirmed(),
                root: RecoveryTestSupport.RootExited()),
        }.Select(snapshot => new object[] { snapshot });

    [Theory]
    [MemberData(nameof(ValidObservationStates))]
    public void ValidObservationStatesRoundTrip(object state)
    {
        var snapshot = (OwnershipSnapshot)state;
        var bytes = OwnershipSnapshotCodec.Encode(snapshot);
        Assert.True(OwnershipSnapshotCodec.TryDecode(bytes, out var decoded, out var error), error);
        Assert.NotNull(decoded);
        Assert.Equal(snapshot.Release, decoded!.Release);
        Assert.Equal(snapshot.Stop, decoded.Stop);
        Assert.Equal(snapshot.Exec, decoded.Exec);
        Assert.Equal(snapshot.Root, decoded.Root);
    }

    [Fact]
    public void FailedDeliveryWithConfirmedObservationFailsClosed()
    {
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(delivery: DeliveryResult.WouldBlock),
            exec: RecoveryTestSupport.ExecConfirmed()));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(delivery: DeliveryResult.BrokenPipe),
            exec: RecoveryTestSupport.ExecFailed()));
        AssertInvalid(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(delivery: DeliveryResult.Partial),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootExited()));
    }

    private static void AssertInvalid(OwnershipSnapshot snapshot)
    {
        Assert.False(OwnershipSnapshotValidator.TryValidate(snapshot, out var error), error);
    }

    private static void AssertRefused(byte[] encoded, int offset, string expected)
    {
        var mutated = MutateAndRechecksum(encoded, offset, 99);
        Assert.False(OwnershipSnapshotCodec.TryDecode(mutated, out _, out var error));
        Assert.Contains(expected, error, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] MutateAndRechecksum(byte[] encoded, int offset, params byte[] values)
    {
        var copy = (byte[])encoded.Clone();
        for (var i = 0; i < values.Length; i++)
        {
            copy[offset + i] = values[i];
        }

        var payloadLength = copy.Length - 32;
        var checksum = SHA256.HashData(copy.AsSpan(0, payloadLength));
        checksum.CopyTo(copy, payloadLength);
        return copy;
    }

    private static int TailOffset(byte[] encoded)
    {
        var offset = 0;
        ReadU16(encoded, ref offset);
        ReadI64(encoded, ref offset);
        ReadGuid(encoded, ref offset);
        SkipString(encoded, ref offset);
        ReadU64(encoded, ref offset);
        SkipString(encoded, ref offset);
        ReadU64(encoded, ref offset);
        ReadU64(encoded, ref offset);
        SkipString(encoded, ref offset);
        ReadU64(encoded, ref offset);
        ReadU64(encoded, ref offset);
        ReadGuid(encoded, ref offset);
        offset += 4; // writer, cleanup, stage, completed
        ReadU64(encoded, ref offset);
        ReadU64(encoded, ref offset);
        var count = ReadU32(encoded, ref offset);
        for (var i = 0; i < count; i++)
        {
            offset++;
            SkipString(encoded, ref offset);
            ReadU64(encoded, ref offset);
            SkipBytes(encoded, ref offset);
            ReadU64(encoded, ref offset);
            ReadU64(encoded, ref offset);
            offset++;
        }

        var hasPreparation = encoded[offset++];
        if (hasPreparation == 1)
        {
            ReadGuid(encoded, ref offset);
            ReadGuid(encoded, ref offset);
            ReadGuid(encoded, ref offset);
            ReadGuid(encoded, ref offset);
            SkipBytes(encoded, ref offset);
        }

        return offset;
    }

    private static ushort ReadU16(byte[] bytes, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset));
        offset += 2;
        return value;
    }

    private static uint ReadU32(byte[] bytes, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
        offset += 4;
        return value;
    }

    private static ulong ReadU64(byte[] bytes, ref int offset)
    {
        var value = BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(offset));
        offset += 8;
        return value;
    }

    private static long ReadI64(byte[] bytes, ref int offset)
    {
        var value = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(offset));
        offset += 8;
        return value;
    }

    private static Guid ReadGuid(byte[] bytes, ref int offset)
    {
        var value = new Guid(bytes.AsSpan(offset, 16));
        offset += 16;
        return value;
    }

    private static void SkipString(byte[] bytes, ref int offset)
    {
        var length = checked((int)ReadU32(bytes, ref offset));
        offset += length;
    }

    private static void SkipBytes(byte[] bytes, ref int offset)
    {
        var length = checked((int)ReadU32(bytes, ref offset));
        offset += length;
    }
}
