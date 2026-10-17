// Independent WP05 v3 qualification probes. These tests are review evidence,
// not product implementation, and exercise hostile bytes and stale snapshots.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05V3IndependentQualificationTests
{
    private const string JournalDirectory = "/wp05-independent";

    [Fact]
    public void RecomputesV2GoldenAndChecksLazyUpgradePreservesIdentity()
    {
        var v2 = RecoveryTestSupport.Snapshot() with { Sequence = 1 };
        var encodedV2 = OwnershipSnapshotCodec.Encode(v2);
        Assert.Equal(
            "5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE",
            Convert.ToHexString(SHA256.HashData(encodedV2)));
        Assert.True(OwnershipSnapshotCodec.TryDecode(encodedV2, out var decodedV2, out var error), error);
        Assert.NotNull(decodedV2);
        Assert.Null(decodedV2!.Release);
        Assert.Null(decodedV2.Stop);
        Assert.Null(decodedV2.Exec);
        Assert.Null(decodedV2.Root);
        Assert.Equal(PreparationStage.AllocationRegistered, decodedV2.Stage);
        Assert.Equal(v2.BootId, decodedV2.BootId);

        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                latest, 1_000, 10_000, 30_000, out var intent, out error), error);
            Assert.NotNull(intent);
            Assert.True(journal.WriteTransition(lease, latest, intent!).Ok);

            var upgraded = journal.ReadLatest();
            Assert.True(upgraded.Ok, upgraded.Error);
            var snapshot = upgraded.Snapshot!;
            Assert.Equal(OwnershipSnapshotCodec.CurrentVersion, snapshot.Version);
            Assert.Equal(latest.ExecutionId, snapshot.ExecutionId);
            Assert.Equal(latest.BootId, snapshot.BootId);
            Assert.Equal(latest.LeaseGeneration, snapshot.LeaseGeneration);
            Assert.Equal(latest.Stage, snapshot.Stage);
            Assert.Equal(latest.Preparation!.ExecutionId, snapshot.Preparation!.ExecutionId);
            Assert.Equal(latest.Preparation.IssuerId, snapshot.Preparation.IssuerId);
            Assert.Equal(latest.Preparation.GenerationId, snapshot.Preparation.GenerationId);
            Assert.Equal(latest.Preparation.PreparationId, snapshot.Preparation.PreparationId);
            Assert.Equal(latest.Preparation.ManifestDigest, snapshot.Preparation.ManifestDigest);
            Assert.Equal(latest.Allocations, snapshot.Allocations);
            Assert.Equal(ReleaseTag.Intent, snapshot.Release!.Tag);
            Assert.Null(snapshot.Stop);
            Assert.Null(snapshot.Exec);
            Assert.Null(snapshot.Root);
        }
    }

    [Fact]
    public void RechecksummedHostileV3BytesFailClosed()
    {
        var snapshot = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            stop: RecoveryTestSupport.Stop(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootExited(rawWait: true));
        var encoded = OwnershipSnapshotCodec.Encode(snapshot);
        Assert.True(OwnershipSnapshotCodec.TryDecode(encoded, out _, out var error), error);
        var tail = TailOffset(encoded);

        AssertRefused(encoded[..^1]);
        var corrupted = (byte[])encoded.Clone();
        corrupted[8] ^= 1;
        AssertRefused(corrupted);
        AssertRefused(MutateAndRechecksum(encoded, 0, 4));
        AssertRefused(MutateAndRechecksum(encoded, tail, 99));
        AssertRefused(MutateAndRechecksum(encoded, tail + 33, (byte)DeliveryResult.WouldBlock));
        AssertRefused(MutateAndRechecksum(encoded, tail + 43, 99));
        AssertRefused(MutateAndRechecksum(encoded, tail + 52, 99));
        AssertRefused(MutateAndRechecksum(encoded, tail + 53, 99));
        AssertRefused(MutateAndRechecksum(encoded, tail + 54, 99));
        AssertRefused(MutateAndRechecksum(encoded, tail + 55, 99));
        AssertRefused(MutateAndRechecksum(encoded, tail + 55, (byte)RootKind.NotStarted));
        AssertRefused(MutateAndRechecksum(encoded, tail + 66, 1));

        var payloadWithTrailingByte = encoded[..^32].Concat(new byte[] { 0 }).ToArray();
        AssertRefused(WithChecksum(payloadWithTrailingByte));
    }

    [Fact]
    public void StaleSnapshotsCannotOverwriteAnyAdvancedDurableFact()
    {
        var (journal, lease, initial) = Seed(V2Prepared());
        using (lease)
        {
            var current = initial;

            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                current, 1_000, 10_000, 30_000, out var intent, out var error), error);
            Assert.True(journal.WriteTransition(lease, current, intent!).Ok);
            AssertStaleRefused(journal, lease, current);
            current = Latest(journal);

            Assert.True(OwnershipTransitions.TryPlanReleaseCommitted(
                current, 2_000, DeliveryResult.Delivered, out var committed, out error), error);
            Assert.True(journal.WriteTransition(lease, current, committed!).Ok);
            AssertStaleRefused(journal, lease, current);
            current = Latest(journal);

            Assert.True(OwnershipTransitions.TryPlanStop(
                current, 3_000, StopCauseKind.CallerStop, out var stopped, out error), error);
            Assert.True(journal.WriteTransition(lease, current, stopped!).Ok);
            Assert.Equal(3_000 + 5_000_000_000, Latest(journal).Stop!.StopDeadlineNanoseconds);
            AssertStaleRefused(journal, lease, current);
            current = Latest(journal);

            Assert.True(OwnershipTransitions.TryPlanExecObservation(
                current, RecoveryTestSupport.ExecConfirmed(), out var exec, out error), error);
            Assert.True(journal.WriteTransition(lease, current, exec!).Ok);
            AssertStaleRefused(journal, lease, current);
            current = Latest(journal);

            Assert.True(OwnershipTransitions.TryPlanRootObservation(
                current, RecoveryTestSupport.RootExited(), out var root, out error), error);
            Assert.True(journal.WriteTransition(lease, current, root!).Ok);
            AssertStaleRefused(journal, lease, current);
            current = Latest(journal);

            Assert.True(journal.WriteMutation(lease, value => value with
            {
                Cleanup = OwnershipCleanupState.Confirmed,
                CompletedCleanup = 0x1F,
            }).Ok);
            AssertStaleRefused(journal, lease, current);

            var final = Latest(journal);
            Assert.Equal(ReleaseTag.Committed, final.Release!.Tag);
            Assert.Equal(DeliveryResult.Delivered, final.Release.DeliveryResult);
            Assert.NotNull(final.Stop);
            Assert.NotNull(final.Exec);
            Assert.NotNull(final.Root);
            Assert.Equal(OwnershipCleanupState.Confirmed, final.Cleanup);
            Assert.Equal((byte)0x1F, final.CompletedCleanup);

            Assert.False(journal.WriteMutation(lease, value => value with { Release = null }).Ok);
            Assert.False(journal.WriteMutation(lease, value => value with { Stop = null }).Ok);
            Assert.False(journal.WriteMutation(lease, value => value with { Exec = null }).Ok);
            Assert.False(journal.WriteMutation(lease, value => value with { Root = null }).Ok);
            Assert.Equal(final.Release, Latest(journal).Release);
            Assert.Equal(final.Stop, Latest(journal).Stop);
            Assert.Equal(final.Exec, Latest(journal).Exec);
            Assert.Equal(final.Root, Latest(journal).Root);
        }
    }

    [Fact]
    public void UnknownRootAndCleanupRemainIndependentInRecoveryProjection()
    {
        var uncertain = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootUnknown());
        var before = ReleaseRecoveryClassifier.Classify(uncertain);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, before.Release);
        Assert.Equal(RootResultKind.Unknown, before.Root);
        Assert.Equal(ExecutionOutcome.Unknown, before.Outcome);

        var cleaned = uncertain with
        {
            Cleanup = OwnershipCleanupState.Confirmed,
            CompletedCleanup = 0x1F,
        };
        var after = ReleaseRecoveryClassifier.Classify(cleaned);
        Assert.Equal(before.Release, after.Release);
        Assert.Equal(before.Delivery, after.Delivery);
        Assert.Equal(before.Exec, after.Exec);
        Assert.Equal(before.Root, after.Root);
        Assert.Equal(before.Outcome, after.Outcome);
        Assert.Equal(OwnershipCleanupState.Confirmed, after.Cleanup);

        var partial = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(delivery: DeliveryResult.Partial),
            exec: RecoveryTestSupport.ExecUnknown(),
            root: RecoveryTestSupport.RootUnknown());
        var partialResult = ReleaseRecoveryClassifier.Classify(partial);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, partialResult.Release);
        Assert.Equal(DeliveryResult.Partial, partialResult.Delivery);
        Assert.Equal(RootResultKind.Unknown, partialResult.Root);
        Assert.Equal(ExecutionOutcome.Unknown, partialResult.Outcome);
    }

    [Fact]
    public void StopDeadlineOverflowAndImmutableDeadlineAreRefused()
    {
        Assert.False(OwnershipTransitions.TryPlanStop(
            V2Prepared(), long.MaxValue - 1, StopCauseKind.CallerStop, out _, out _));

        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                latest, 1_000, 10_000, 30_000, out var intent, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, intent!).Ok);
            var withIntent = Latest(journal);
            var changed = withIntent with
            {
                Release = withIntent.Release! with { SetupDeadlineNanoseconds = 11_000 },
            };
            Assert.False(journal.WriteTransition(lease, withIntent, changed).Ok);

            Assert.True(OwnershipTransitions.TryPlanStop(
                withIntent, 2_000, StopCauseKind.CallerStop, out var stop, out error), error);
            Assert.Equal(2_000 + 5_000_000_000, stop!.Stop!.StopDeadlineNanoseconds);
            Assert.True(journal.WriteTransition(lease, withIntent, stop!).Ok);
            var withStop = Latest(journal);
            Assert.False(OwnershipTransitions.TryPlanStop(
                withStop, 9_000_000_000, StopCauseKind.RecoveryTakeover, out _, out error));
            Assert.Contains("immutable", error, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2_000 + 5_000_000_000, Latest(journal).Stop!.StopDeadlineNanoseconds);
        }
    }

    private static OwnershipSnapshot V2Prepared() => RecoveryTestSupport.Snapshot(
        stage: PreparationStage.Prepared,
        preparation: RecoveryTestSupport.PreparationIdentity());

    private static (OwnershipJournal Journal, JournalWriterLease Lease, OwnershipSnapshot Latest) Seed(
        OwnershipSnapshot initial)
    {
        var journal = new OwnershipJournal(new FakeJournalStorage(), JournalDirectory);
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        Assert.True(journal.WriteNext(lease, initial).Ok);
        var read = journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        return (journal, lease!, read.Snapshot!);
    }

    private static OwnershipSnapshot Latest(OwnershipJournal journal)
    {
        var read = journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        return read.Snapshot!;
    }

    private static void AssertStaleRefused(
        OwnershipJournal journal, JournalWriterLease lease, OwnershipSnapshot stale)
    {
        var result = journal.WriteTransition(lease, stale, stale);
        Assert.False(result.Ok);
        Assert.Contains("stale", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.True(OwnershipSnapshotCodec.TryDecode(
            OwnershipSnapshotCodec.Encode(Latest(journal)), out _, out var error), error);
    }

    private static void AssertRefused(byte[] bytes) =>
        Assert.False(OwnershipSnapshotCodec.TryDecode(bytes, out _, out _));

    private static byte[] MutateAndRechecksum(byte[] encoded, int offset, byte value)
    {
        var copy = (byte[])encoded.Clone();
        copy[offset] = value;
        return WithChecksum(copy[..^32]);
    }

    private static byte[] WithChecksum(byte[] payload)
    {
        var result = new byte[payload.Length + 32];
        payload.CopyTo(result, 0);
        SHA256.HashData(payload).CopyTo(result, payload.Length);
        return result;
    }

    private static int TailOffset(byte[] encoded)
    {
        var offset = 0;
        offset += 2 + 8 + 16;
        SkipString(encoded, ref offset);
        offset += 8;
        SkipString(encoded, ref offset);
        offset += 8 + 8;
        SkipString(encoded, ref offset);
        offset += 8 + 8 + 16 + 4 + 8 + 8;
        var count = ReadU32(encoded, ref offset);
        for (var index = 0; index < count; index++)
        {
            offset++;
            SkipString(encoded, ref offset);
            offset += 8;
            SkipBytes(encoded, ref offset);
            offset += 8 + 8 + 1;
        }

        if (encoded[offset++] == 1)
        {
            offset += 16 * 4;
            SkipBytes(encoded, ref offset);
        }

        return offset;
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

    private static uint ReadU32(byte[] bytes, ref int offset)
    {
        var result = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));
        offset += 4;
        return result;
    }
}
