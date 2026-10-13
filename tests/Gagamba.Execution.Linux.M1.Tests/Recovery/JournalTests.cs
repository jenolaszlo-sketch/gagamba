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
        // another identity fails at the final durability barrier; the
        // reader must still return the old acknowledged generation.
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);

        storage.Fault = step => step == "fsyncdir" ? JournalIoResult.Failure("final fsync fault") : null;
        var replacement = RecoveryTestSupport.Snapshot() with { DomainInode = 777, Sequence = 2 };
        Assert.False(journal.Write(lease, replacement).Ok);

        storage.Fault = null;
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
    public void DebrisUnlinkFailureIsReported()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        using var lease = Acquire(journal);
        Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        // Stage unconfirmed debris in the free slot behind the lease's back.
        var debris = OwnershipSnapshotCodec.Encode(RecoveryTestSupport.Snapshot() with { Sequence = 9 });
        storage.DirectPut(Dir, OwnershipJournal.SlotB, debris);
        storage.Fault = s => s == "unlink" ? JournalIoResult.Failure("unlink fault") : null;
        var write = journal.Write(lease, RecoveryTestSupport.Snapshot() with { Sequence = 2 });
        Assert.False(write.Ok);
        Assert.Contains("slot", write.Error);
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
}
