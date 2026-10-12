// WP03 journal tests: bounded/versioned/checksummed snapshots, durability
// ordering, torn detection, prior-snapshot survival and writer fencing.
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class JournalTests
{
    private const string Dir = "/recovery";

    private static OwnershipJournal NewJournal(FakeJournalStorage storage) => new(storage, Dir);

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
        Assert.True(journal.WriteNext(RecoveryTestSupport.Snapshot()).Ok);
        var read = journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(1, read.Snapshot!.Sequence);
    }

    [Fact]
    public void TornRecordIsDetected()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        Assert.True(journal.WriteNext(RecoveryTestSupport.Snapshot()).Ok);
        storage.Corrupt(Dir, OwnershipJournal.DefaultSnapshotName);
        var read = journal.ReadLatest();
        Assert.False(read.Ok);
        Assert.Contains("checksum", read.Error);
    }

    [Fact]
    public void OverlargeRecordRefused()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        var many = new List<AllocationEntry>();
        for (var i = 0; i < OwnershipSnapshotCodec.MaxAllocations + 1; i++)
        {
            many.Add(new AllocationEntry(AllocationKind.Staging, $"s-{i}", 1, new byte[32]));
        }

        var snapshot = RecoveryTestSupport.Snapshot(allocations: many);
        var write = journal.Write(snapshot);
        Assert.False(write.Ok);
    }

    [Fact]
    public void PriorSnapshotSurvivesFailedReplacement()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        Assert.True(journal.WriteNext(RecoveryTestSupport.Snapshot()).Ok);

        storage.Fault = step => step == "rename" ? JournalIoResult.Failure("rename fault") : null;
        var second = RecoveryTestSupport.Snapshot() with { DomainInode = 300 };
        var failed = journal.Write(second with { Sequence = 2 });
        Assert.False(failed.Ok);

        storage.Fault = null;
        var read = journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(200UL, read.Snapshot!.DomainInode);
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
        storage.Fault = s => s == step ? JournalIoResult.Failure($"{step} fault") : null;
        var write = journal.Write(RecoveryTestSupport.Snapshot());
        Assert.False(write.Ok);
        Assert.Contains(step, write.Error);
    }

    [Fact]
    public void SequenceIsMonotonic()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        Assert.True(journal.WriteNext(RecoveryTestSupport.Snapshot()).Ok);
        Assert.True(journal.WriteNext(RecoveryTestSupport.Snapshot()).Ok);
        var read = journal.ReadLatest();
        Assert.Equal(2, read.Snapshot!.Sequence);
    }

    [Fact]
    public void WriterLockIsExclusive()
    {
        var storage = new FakeJournalStorage();
        var journal = NewJournal(storage);
        Assert.True(journal.TryAcquireExclusiveWriter().Ok);
        Assert.True(journal.IsWriterHeld());
        Assert.False(journal.TryAcquireExclusiveWriter().Ok);
        Assert.True(journal.ReleaseExclusiveWriter().Ok);
        Assert.False(journal.IsWriterHeld());
        Assert.True(journal.TryAcquireExclusiveWriter().Ok);
    }
}
