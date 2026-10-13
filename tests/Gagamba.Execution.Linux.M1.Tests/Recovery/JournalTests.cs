// WP03 journal tests (G2-04/G2-05): bounded two-generation durability,
// explicit resolution rules, per-boundary failure injection, writer-lease
// fencing, monotonic sequence, and bounded reads.
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class JournalTests
{
    private const string Dir = "/recovery";

    private static OwnershipJournal NewJournal(FakeJournalStorage storage) => new(storage, Dir);

    private static JournalWriterLease Acquire(OwnershipJournal journal)
    {
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        Assert.NotNull(lease);
        return lease!;
    }

    [Fact]
    public void RoundTripsSnapshot()
    {
        var snapshot = RecoveryTestSupport.Snapshot();
        var bytes = OwnershipSnapshotCodec.Encode(snapshot);
        Assert.True(OwnershipSnapshotCodec.TryDecode(bytes, out var decoded, out var error), error);
        Assert.NotNull(decoded);
        Assert.Equal(snapshot.ExecutionId, decoded!.ExecutionId);
        Assert.Equal(snapshot.LeaseGeneration, decoded.LeaseGeneration);
        Assert.Equal(snapshot.Stage, decoded.Stage);
        Assert.Equal(snapshot.DomainInode, decoded.DomainInode);
    }

    [Fact]
    public void DurableWriteThenRead()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        var read = journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(1, read.Snapshot!.Sequence);
    }

    [Fact]
    public void WriteWithoutLeaseRefused()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        Assert.False(journal.Write(null, RecoveryTestSupport.Snapshot() with { Sequence = 1 }).Ok);
        Assert.False(journal.WriteNext(null, RecoveryTestSupport.Snapshot()).Ok);
        Assert.False(journal.ReadLatest().Ok);
    }

    [Fact]
    public void ReleasedLeaseCannotWrite()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        var lease = Acquire(journal);
        lease.Dispose();
        Assert.False(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        // A second journal cannot bypass the fence either.
        var other = NewJournal(storage);
        Assert.False(other.WriteNext(null, RecoveryTestSupport.Snapshot()).Ok);
    }

    [Fact]
    public void FailedLockYieldsNoLease()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        storage.Fault = step => step == "lock" ? JournalIoResult.Failure("lock fault") : null;
        Assert.False(journal.TryAcquireWriter(out var lease, out _));
        Assert.Null(lease);
    }

    [Fact]
    public void TornRecordIsDetected()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        storage.Corrupt(Dir, OwnershipJournal.SlotA);
        var read = journal.ReadLatest();
        Assert.False(read.Ok);
    }

    [Fact]
    public void OverlargeRecordRefused()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        var many = new List<AllocationEntry>();
        for (var i = 0; i < OwnershipSnapshotCodec.MaxAllocations + 1; i++)
        {
            many.Add(new AllocationEntry(AllocationKind.Staging, $"s-{i}", 1, new byte[32], 0, 0, true));
        }

        var snapshot = RecoveryTestSupport.Snapshot(allocations: many);
        Assert.False(journal.Write(lease, snapshot with { Sequence = 1 }).Ok);
    }

    [Fact]
    public void OverlongFileRefusedBeforeFullRead()
    {
        var storage = new FakeJournalStorage();
        storage.DirectPut(Dir, OwnershipJournal.SlotA, new byte[OwnershipSnapshotCodec.MaxSnapshotBytes + 1]);
        var journal = NewJournal(storage);
        Assert.False(journal.ReadLatest().Ok);
    }

    [Fact]
    public void PriorSnapshotSurvivesFailedReplacement()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);

        storage.Fault = step => step == "rename" ? JournalIoResult.Failure("rename fault") : null;
        var second = RecoveryTestSupport.Snapshot() with { DomainInode = 300, Sequence = 2 };
        Assert.False(journal.Write(lease, second).Ok);

        storage.Fault = null;
        var read = journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(200UL, read.Snapshot!.DomainInode);
        Assert.Equal(1, read.Snapshot.Sequence);
    }

    [Fact]
    public void AstraReplacementCaseKeepsOldAcknowledged()
    {
        // Old durable record names one identity; a replacement naming
        // another identity fails at the post-candidate directory sync; the
        // reader must still return the old acknowledged generation.
        // (Second fsyncdir: the seal's own sync is occurrence 1.)
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);

        storage.FailAt("fsyncdir", 2);
        var replacement = RecoveryTestSupport.Snapshot() with { DomainInode = 777, Sequence = 2 };
        Assert.False(journal.Write(lease, replacement).Ok);

        var read = journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(200UL, read.Snapshot!.DomainInode);
        Assert.Equal(1, read.Snapshot.Sequence);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("write")]
    [InlineData("fsyncfile")]
    [InlineData("rename")]
    [InlineData("fsyncdir")]
    public void EachDurabilityStepFailureIsReported(string step)
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        storage.Fault = s => s == step ? JournalIoResult.Failure($"{step} fault") : null;
        var write = journal.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 1 });
        Assert.False(write.Ok);
        Assert.Contains(step, write.Error);
    }

    [Fact]
    public void SealReleaseFailureIsReportedAndUncertain()
    {
        // The sealed protocol never unlinks generations, so the analogous
        // post-acknowledgement failure is the seal release. It must surface
        // as failure and leave explicit uncertainty, never a silently
        // promoted candidate.
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        storage.Fault = s => s == "unlink" ? JournalIoResult.Failure("unlink fault") : null;
        var write = journal.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 2 });
        Assert.False(write.Ok);
        Assert.Contains("seal", write.Error);
        Assert.Equal(1, journal.LastSequence);
        var fresh = NewJournal(storage);
        Assert.False(fresh.ReadLatest().Ok);
    }

    [Fact]
    public void SequenceIsMonotonic()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        var read = journal.ReadLatest();
        Assert.Equal(2, read.Snapshot!.Sequence);
    }

    [Fact]
    public void SequenceRegressionAndDuplicatesRefuse()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        Assert.False(journal.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 1 }).Ok);
        Assert.False(journal.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 0 }).Ok);
        Assert.False(journal.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 3 }).Ok);
        var read = journal.ReadLatest();
        Assert.Equal(1, read.Snapshot!.Sequence);
    }

    [Fact]
    public void FreshWriterLoadsDurableSequence()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using (var first = Acquire(journal))
        {
            Assert.True(journal.WriteNext(first, RecoveryTestSupport.Snapshot()).Ok);
            Assert.True(journal.WriteNext(first, RecoveryTestSupport.Snapshot()).Ok);
        }

        using var second = Acquire(journal);
        Assert.Equal(2, second.BaselineSequence);
        Assert.True(journal.WriteNext(second, RecoveryTestSupport.Snapshot()).Ok);
        Assert.Equal(3, journal.ReadLatest().Snapshot!.Sequence);
    }

    [Fact]
    public void WriterLockIsExclusive()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        var first = Acquire(journal);
        Assert.False(journal.TryAcquireWriter(out _, out _));
        first.Dispose();
        using var second = Acquire(journal);
        Assert.Equal(0, second.BaselineSequence);
    }

    [Fact]
    public void UncertainStateRefusesAcquisition()
    {
        var storage = new FakeJournalStorage();
        storage.DirectPut(Dir, OwnershipJournal.SlotA, new byte[] { 9, 9, 9 });
        var journal = NewJournal(storage);
        Assert.False(journal.TryAcquireWriter(out var lease, out _));
        Assert.Null(lease);
    }

    // G2-04 durability fault matrix. A sealed commit performs, in order:
    // seal create(1), seal fsyncfile, seal fsyncdir(1), tmp create(2), tmp
    // write(1), tmp fsyncfile, rename(1), fsyncdir(2) [candidate staged],
    // tmp create(3), tmp write(2), tmp fsyncfile, rename(2), fsyncdir(3)
    // [both slots hold the new generation], seal unlink. The seal removal
    // needs no confirmation (a torn removal only leaves a stale,
    // recoverable override), so fsyncdir(3) is the final confirmable
    // durability step. Every faulted write must report failure without
    // advancing in-memory state, and a fresh reader must yield the old
    // acknowledged generation, explicit uncertainty, or (genesis, nothing
    // ever staged) nothing — never the failed candidate.
    public static TheoryData<string, int, bool, bool, string> DurabilityMatrix() => new()
    {
        // step, occurrence, genesis, apiOk, readerKind(old/new/uncertain/none)
        { "create", 1, false, false, "old" }, // seal creation fails
        { "create", 1, true, false, "none" }, // genesis seal creation fails
        { "fsyncfile", 1, false, false, "old" }, // seal file sync fails, slots untouched
        { "fsyncfile", 1, true, false, "uncertain" },
        { "fsyncdir", 1, false, false, "old" }, // seal dir sync fails, slots untouched
        { "fsyncdir", 1, true, false, "uncertain" },
        { "create", 2, false, false, "old" }, // candidate temp creation fails
        { "create", 2, true, false, "uncertain" },
        { "write", 1, false, false, "old" }, // candidate write fails
        { "write", 1, true, false, "uncertain" },
        { "fsyncfile", 2, false, false, "old" }, // candidate file sync fails
        { "fsyncfile", 2, true, false, "uncertain" },
        { "rename", 1, false, false, "old" }, // candidate rename fails
        { "rename", 1, true, false, "uncertain" },
        { "fsyncdir", 2, false, false, "old" }, // post-candidate dir sync fails
        { "fsyncdir", 2, true, false, "uncertain" }, // GENESIS_FAIL shape
        { "create", 3, false, false, "old" }, // mirror temp creation fails
        { "create", 3, true, false, "uncertain" },
        { "write", 2, false, false, "old" }, // mirror write fails
        { "write", 2, true, false, "uncertain" },
        { "fsyncfile", 3, false, false, "old" }, // mirror file sync fails
        { "fsyncfile", 3, true, false, "uncertain" },
        { "rename", 2, false, false, "old" }, // mirror rename fails
        { "rename", 2, true, false, "uncertain" },
        { "fsyncdir", 3, false, false, "uncertain" }, // final content sync fails: both slots new
        { "fsyncdir", 3, true, false, "uncertain" },
        { "unlink", 1, false, true, "new" }, // entry scratch cleanup is non-acking
        { "unlink", 1, true, true, "new" },
        { "unlink", 2, false, false, "uncertain" }, // seal release fails
        { "unlink", 2, true, false, "uncertain" },
    };

    [Theory]
    [MemberData(nameof(DurabilityMatrix))]
    public void DurabilityFaultMatrix(string step, int occurrence, bool genesis, bool apiOk, string readerKind)
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        long baseline = 0;
        if (!genesis)
        {
            Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
            baseline = 1;
        }

        storage.FailAt(step, occurrence);
        var attempt = genesis
            ? RecoveryTestSupport.Snapshot() with { Sequence = 1 }
            : RecoveryTestSupport.Snapshot() with { DomainInode = 300, Sequence = 2 };
        var write = journal.Write(lease, attempt);
        Assert.Equal(apiOk, write.Ok);
        var expectedSequence = apiOk ? baseline + 1 : baseline;
        Assert.Equal(expectedSequence, journal.LastSequence);
        Assert.Equal(expectedSequence, lease.BaselineSequence);

        var fresh = NewJournal(storage);
        var read = fresh.ReadLatest();
        switch (readerKind)
        {
            case "old":
                Assert.True(read.Ok, read.Error);
                Assert.Equal(baseline, read.Snapshot!.Sequence);
                Assert.Equal(200UL, read.Snapshot!.DomainInode);
                break;
            case "new":
                Assert.True(read.Ok, read.Error);
                Assert.Equal(baseline + 1, read.Snapshot!.Sequence);
                break;
            case "uncertain":
                Assert.False(read.Ok);
                break;
            default: // none
                Assert.False(read.Ok);
                Assert.Equal("no durable snapshot exists", read.Error);
                break;
        }
    }

    [Fact]
    public void FinalDirectorySyncFailureNeverPromotesCandidate()
    {
        // Exact prior FSYNC_FAIL_2 shape: acknowledged seq 1 exists, the
        // candidate seq 2 is fully staged, the final content directory
        // sync fails.
        // The old protocol promoted seq 2 on reopen; the sealed protocol
        // must report explicit uncertainty instead.
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        storage.FailAt("fsyncdir", 3);
        var candidate = RecoveryTestSupport.Snapshot() with { DomainInode = 777, Sequence = 2 };
        Assert.False(journal.Write(lease, candidate).Ok);
        Assert.Equal(1, journal.LastSequence);
        Assert.Equal(1, lease.BaselineSequence);
        var fresh = NewJournal(storage);
        var read = fresh.ReadLatest();
        Assert.False(read.Ok);
        Assert.NotEqual(2, fresh.LastSequence);
    }

    [Fact]
    public void GenesisFinalSyncFailureNeverPromotesCandidate()
    {
        // Exact prior GENESIS_FAIL shape: no acknowledged generation, the
        // lone candidate fails its durability confirmation. Reopen must not
        // acknowledge sequence 1.
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        storage.FailAt("fsyncdir", 2);
        Assert.False(journal.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 1 }).Ok);
        Assert.Equal(0, journal.LastSequence);
        var fresh = NewJournal(storage);
        Assert.False(fresh.ReadLatest().Ok);
    }

    [Fact]
    public void FailedCandidateNeverBecomesBaseline()
    {
        // A failed candidate must not seed a later writer's baseline, and a
        // fresh writer must refuse ambiguous state rather than adopt the
        // highest visible sequence. Explicit recovery completes the
        // fully-staged commit then.
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using (var first = Acquire(journal))
        {
            Assert.True(journal.WriteNext(first, RecoveryTestSupport.Snapshot()).Ok);
        }

        using (var second = Acquire(journal))
        {
            storage.FailAt("fsyncdir", 3);
            Assert.False(journal.Write(second, RecoveryTestSupport.Snapshot() with { Sequence = 2 }).Ok);
        }

        var fresh = NewJournal(storage);
        Assert.False(fresh.TryAcquireWriter(out _, out var error));
        Assert.Contains("seal", error);
        Assert.True(fresh.TryRecoverCommit(out var committed, out var recoverError), recoverError);
        Assert.Equal(2, committed!.Sequence);
        using var recovered = Acquire(fresh);
        Assert.Equal(2, recovered.BaselineSequence);
        Assert.True(fresh.WriteNext(recovered, RecoveryTestSupport.Snapshot()).Ok);
        Assert.Equal(3, fresh.ReadLatest().Snapshot!.Sequence);
    }

    [Fact]
    public void RecoveryCompletesFullyStagedCommit()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using (var lease = Acquire(journal))
        {
            Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        }

        using (var lease = Acquire(journal))
        {
            storage.FailAt("fsyncdir", 3);
            Assert.False(journal.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 2 }).Ok);
        }

        var fresh = NewJournal(storage);
        Assert.True(fresh.TryRecoverCommit(out var committed, out var error), error);
        Assert.Equal(2, committed!.Sequence);
        var read = fresh.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(2, read.Snapshot!.Sequence);
    }

    [Fact]
    public void RecoveryRollsBackDivergentCandidate()
    {
        // Both slots decodable but unequal behind a seal: the higher one is
        // necessarily uncommitted (acknowledgement requires both-equal), so
        // recovery demotes it by removing only the seal.
        var storage = new FakeJournalStorage();
        storage.DirectPut(Dir, OwnershipJournal.SlotA, OwnershipSnapshotCodec.Encode(RecoveryTestSupport.Snapshot() with { Sequence = 1 }));
        storage.DirectPut(Dir, OwnershipJournal.SlotB, OwnershipSnapshotCodec.Encode(RecoveryTestSupport.Snapshot() with { Sequence = 2 }));
        storage.DirectPut(Dir, OwnershipJournal.SealName, Array.Empty<byte>());
        var journal = NewJournal(storage);
        Assert.True(journal.TryRecoverCommit(out var committed, out var error), error);
        Assert.Equal(1, committed!.Sequence);
        var read = journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(1, read.Snapshot!.Sequence);
    }

    [Fact]
    public void RecoveryRefusesUninterpretableSealedState()
    {
        var storage = new FakeJournalStorage();
        storage.DirectPut(Dir, OwnershipJournal.SlotA, OwnershipSnapshotCodec.Encode(RecoveryTestSupport.Snapshot() with { Sequence = 1 }));
        storage.DirectPut(Dir, OwnershipJournal.SealName, Array.Empty<byte>());
        var journal = NewJournal(storage);
        Assert.False(journal.TryRecoverCommit(out _, out _));
        Assert.False(journal.ReadLatest().Ok);
    }

    [Fact]
    public void RecoveryReportsNothingToRecover()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        Assert.False(journal.TryRecoverCommit(out _, out _));
    }

    [Fact]
    public void RecoveryRefusedWhileWriterLive()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.False(journal.TryRecoverCommit(out _, out _));
    }

    [Fact]
    public void StaleLeaseCannotWriteAfterFenceLoss()
    {
        // Losing the kernel fence behind a logically-live lease (e.g. the
        // storage object is disposed) must revoke mutation authority even
        // though the lease's own boolean still looks live.
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        var lease = Acquire(journal);
        storage.ReleaseExclusiveLock(Dir, OwnershipJournal.DefaultLockName);
        Assert.False(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        lease.Dispose();
    }

    [Fact]
    public void ForeignLeaseCannotWriteOnOtherJournal()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        var other = NewJournal(storage);
        Assert.False(other.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        Assert.False(other.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 1 }).Ok);
    }

    [Fact]
    public void DuplicateLocalAcquisitionRefused()
    {
        // The exact old overlapping-lease shape at the journal level: a
        // second acquisition on the same journal while one lease is usable
        // must refuse rather than manufacture a second independently
        // disposable authority over one fence.
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        var first = Acquire(journal);
        Assert.False(journal.TryAcquireWriter(out var second, out var error));
        Assert.Null(second);
        Assert.Contains("already active", error);
        Assert.True(first.IsLive);
        first.Dispose();
        using var replacement = Acquire(journal);
        Assert.Equal(0, replacement.BaselineSequence);
    }
}
