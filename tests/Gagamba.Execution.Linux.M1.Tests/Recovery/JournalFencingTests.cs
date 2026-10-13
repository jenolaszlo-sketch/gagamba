// WP03 native writer-fencing regressions (G2-05): exact overlapping-lease
// shape against the real flock-backed storage, cross-object exclusion,
// lock-identity keying, and fence-loss revocation. Linux-only; every case
// is finite and skips honestly elsewhere.
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class JournalFencingTests
{
    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-r6c-fence-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
            GC.SuppressFinalize(this);
        }
    }

    private static void SkipUnlessLinux()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("requires Linux");
        }
    }

    private static JournalWriterLease Acquire(OwnershipJournal journal)
    {
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        Assert.NotNull(lease);
        return lease!;
    }

    [Fact]
    public void RealStorageRoundTrip()
    {
        SkipUnlessLinux();
        using var dir = new TempDir();
        using var storage = new JournalFileStorage();
        var journal = new OwnershipJournal(storage, dir.Path);
        using (var lease = Acquire(journal))
        {
            Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
            Assert.True(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        }

        var read = new OwnershipJournal(storage, dir.Path).ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(2, read.Snapshot!.Sequence);
    }

    [Fact]
    public void SameStorageSecondJournalRefusedWhileLive()
    {
        // One storage object, two journal objects, one directory: the
        // second acquisition must refuse while the first lease is usable,
        // and succeed after it is released.
        SkipUnlessLinux();
        using var dir = new TempDir();
        using var storage = new JournalFileStorage();
        var first = new OwnershipJournal(storage, dir.Path);
        var second = new OwnershipJournal(storage, dir.Path);
        var live = Acquire(first);
        Assert.False(second.TryAcquireWriter(out var duplicate, out var error));
        Assert.Null(duplicate);
        Assert.Contains("already held", error);
        Assert.True(live.IsLive);
        live.Dispose();
        using var replacement = Acquire(second);
        Assert.Equal(0, replacement.BaselineSequence);
    }

    [Fact]
    public void CrossObjectAcquireRefusedWhileLive()
    {
        // Separate storage objects over one directory are still one fence:
        // flock denies the second open while the first holder lives.
        SkipUnlessLinux();
        using var dir = new TempDir();
        using var storage = new JournalFileStorage();
        var journal = new OwnershipJournal(storage, dir.Path);
        using var live = Acquire(journal);
        using var rival = new JournalFileStorage();
        Assert.False(rival.TryAcquireExclusiveLock(dir.Path, OwnershipJournal.DefaultLockName).Ok);
        Assert.True(rival.TryAcquireExclusiveLock(dir.Path, "other.lock").Ok);
        Assert.True(rival.ReleaseExclusiveLock(dir.Path, "other.lock").Ok);
    }

    [Fact]
    public void LockIdentityIncludesDirectory()
    {
        // The same lock name in two directories is two independent fences.
        SkipUnlessLinux();
        using var first = new TempDir();
        using var second = new TempDir();
        using var storage = new JournalFileStorage();
        Assert.True(storage.TryAcquireExclusiveLock(first.Path, OwnershipJournal.DefaultLockName).Ok);
        Assert.True(storage.TryAcquireExclusiveLock(second.Path, OwnershipJournal.DefaultLockName).Ok);
        Assert.True(storage.ReleaseExclusiveLock(first.Path, OwnershipJournal.DefaultLockName).Ok);
        Assert.True(storage.ReleaseExclusiveLock(second.Path, OwnershipJournal.DefaultLockName).Ok);
    }

    [Fact]
    public void DisposedStorageKillsLeaseAuthority()
    {
        // The fence object going away revokes mutation authority even
        // though the lease handle still exists.
        SkipUnlessLinux();
        using var dir = new TempDir();
        var storage = new JournalFileStorage();
        var journal = new OwnershipJournal(storage, dir.Path);
        var lease = Acquire(journal);
        storage.Dispose();
        Assert.False(journal.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        lease.Dispose();
    }

    [Fact]
    public void OverlappingLeaseReproductionRefused()
    {
        // Exact old counterexample shape, corrected: L1 acquired; a second
        // logical lease over the same fence must refuse (so disposal of one
        // can never release the kernel lock under another usable lease);
        // after L1 is released a rival acquires the real fence; the
        // disposed survivor cannot mutate while the rival writes.
        SkipUnlessLinux();
        using var dir = new TempDir();
        using var storage = new JournalFileStorage();
        var journal = new OwnershipJournal(storage, dir.Path);
        using (var seed = Acquire(journal))
        {
            Assert.True(journal.WriteNext(seed, RecoveryTestSupport.Snapshot()).Ok);
        }

        var survivor = Acquire(journal);
        Assert.False(journal.TryAcquireWriter(out var overlap, out _));
        Assert.Null(overlap);
        survivor.Dispose();

        using var rivalStorage = new JournalFileStorage();
        var rival = new OwnershipJournal(rivalStorage, dir.Path);
        using var rivalLease = Acquire(rival);
        Assert.False(journal.WriteNext(survivor, RecoveryTestSupport.Snapshot()).Ok);
        Assert.True(rival.WriteNext(rivalLease, RecoveryTestSupport.Snapshot()).Ok);
        Assert.Equal(2, rival.ReadLatest().Snapshot!.Sequence);
    }
}
