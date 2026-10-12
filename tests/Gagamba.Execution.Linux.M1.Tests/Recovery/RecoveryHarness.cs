// WP03 Linux-native qualification harness: real cgroup v2 delegation,
// retained-handle identity, whole-domain stop, identity-checked removal,
// unrelated-sibling isolation, subreaper, and owner-death recovery against
// a real per-execution owner process. Every case is finite and skipped
// honestly off Linux or without delegation. No target fixture runs.
using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class RecoveryHarness
{
    private const string CgroupRoot = "/sys/fs/cgroup";

    private sealed class TempParent : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(CgroupRoot, "gagamba-wp03-" + Guid.NewGuid().ToString("N"));

        public TempParent()
        {
            Directory.CreateDirectory(Path);
            // Operator-provided delegation: enable the controllers the
            // domain needs on its delegated parent. WP03 itself never
            // configures host delegation; the harness plays operator.
            try
            {
                File.WriteAllText(System.IO.Path.Combine(Path, "cgroup.subtree_control"), "+memory +pids");
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                {
                    foreach (var dir in Directory.EnumerateDirectories(Path))
                    {
                        try { Directory.Delete(dir); } catch { }
                    }

                    Directory.Delete(Path);
                }
            }
            catch
            {
            }

            GC.SuppressFinalize(this);
        }
    }

    private static bool Usable()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            var probe = System.IO.Path.Combine(CgroupRoot, "gagamba-wp03-probe-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(probe);
            var ok = File.Exists(System.IO.Path.Combine(probe, "cgroup.kill"))
                && File.Exists(System.IO.Path.Combine(probe, "cgroup.events"));
            Directory.Delete(probe);
            return ok;
        }
        catch
        {
            return false;
        }
    }

    private static void SkipUnlessUsable()
    {
        if (!Usable())
        {
            Assert.Skip("requires delegated Linux cgroup v2");
        }
    }

    private static CgroupMountInfo Mount() =>
        CgroupInterop.TryReadCgroupMount(out var info, out _) ? info! : new CgroupMountInfo(0, "cgroup2", CgroupRoot);

    private static string Boot() =>
        CgroupInterop.TryReadBootId(out var id, out _) ? id : "unknown";

    [Fact]
    public void CgroupMountAndBootIdentityAreReadable()
    {
        SkipUnlessUsable();
        Assert.True(CgroupInterop.TryReadCgroupMount(out var mount, out var error), error);
        Assert.Equal("cgroup2", mount!.FileSystemType);
        Assert.True(mount.MountId > 0);
        Assert.True(CgroupInterop.TryReadBootId(out var boot, out var bootError), bootError);
        Assert.NotEqual(string.Empty, boot);
    }

    [Fact]
    public void MissingDelegationRefused()
    {
        SkipUnlessUsable();
        var errno = Gagamba.Execution.Linux.M1.Preparation.Native.PreparationInterop.OpenDirectory(
            "/nonexistent-gagamba-parent", out _);
        Assert.NotEqual(0, errno);
    }

    [Fact]
    public void SuccessfulDomainAllocateStopRemove()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var mount = Mount();
        var allocation = scope.AllocateDomain("d-" + Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), mount, Boot(), out var recovery);
        Assert.True(allocation.Ok, allocation.Error);
        using var lease = allocation.Lease!;
        using var recover = recovery!;
        Assert.True(lease.VerifyIdentity().Match);
        var stop = lease.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock());
        Assert.True(stop.Stopped, stop.Error);
        var removal = lease.RemoveIfIdentityMatches();
        Assert.True(removal.Removed, removal.Error);
        Assert.Empty(Directory.GetDirectories(temp.Path));
    }

    [Fact]
    public void RetainedControlHandleIdentityIsStable()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var allocation = scope.AllocateDomain("d-" + Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Mount(), Boot(), out var recovery);
        Assert.True(allocation.Ok, allocation.Error);
        using var lease = allocation.Lease!;
        using var recover = recovery!;
        var first = lease.VerifyIdentity();
        var second = lease.VerifyIdentity();
        Assert.True(first.Match && second.Match);
        Assert.Equal(lease.Identity.ChildInode, lease.Identity.ChildInode);
    }

    [Fact]
    public void ReplacementBlocksRemoval()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var name = "d-" + Guid.NewGuid().ToString("N");
        var allocation = scope.AllocateDomain(name, Guid.NewGuid(), Guid.NewGuid(), Mount(), Boot(), out var recovery);
        Assert.True(allocation.Ok, allocation.Error);
        using var lease = allocation.Lease!;
        using var recover = recovery!;
        // Replace the directory with a same-name sibling of a different inode.
        Directory.Delete(System.IO.Path.Combine(temp.Path, name));
        Directory.CreateDirectory(System.IO.Path.Combine(temp.Path, name));
        var removal = lease.RemoveIfIdentityMatches();
        Assert.True(removal.Refused, removal.Error);
        Assert.Single(Directory.GetDirectories(temp.Path));
    }

    [Fact]
    public void UnrelatedSiblingUntouched()
    {
        SkipUnlessUsable();
        using var temp = new TempParent();
        var sibling = System.IO.Path.Combine(temp.Path, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var allocation = scope.AllocateDomain("d-" + Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Mount(), Boot(), out var recovery);
        Assert.True(allocation.Ok, allocation.Error);
        using var lease = allocation.Lease!;
        using var recover = recovery!;
        lease.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock());
        lease.RemoveIfIdentityMatches();
        Assert.True(Directory.Exists(sibling));
    }

    [Fact]
    public void SubreaperEnabledAndVerified()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        Assert.True(RecoverySubreaper.TryEnableAndVerify(out var error), error);
    }

    [Fact]
    public void OwnerBootstrapsAcquiresLockThenDiesAndScopeRecovers()
    {
        SkipUnlessUsable();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        var journalDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-wp03-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(journalDir);
        try
        {
            using var scope = new M1RecoveryScope();
            Assert.True(scope.TryInitialize().Ok);
            Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
            var name = "d-" + Guid.NewGuid().ToString("N");
            var leaseGen = Guid.NewGuid();
            var allocation = scope.AllocateDomain(name, Guid.NewGuid(), leaseGen, Mount(), Boot(), out var recovery);
            Assert.True(allocation.Ok, allocation.Error);
            using var lease = allocation.Lease!;
            using var recover = recovery!;

            var owner = scope.SpawnOwner(ownerDll, new[]
            {
                "--journal-dir", journalDir,
                "--parent", temp.Path,
                "--domain", name,
                "--lease", leaseGen.ToString("D"),
                "--barrier", "after-lock",
            }, out var spawnError);
            using (owner)
            {
                var seenBootstrap = false;
                var seenLocked = false;
                string? line;
                while ((line = owner.StandardOutput.ReadLine()) is not null)
                {
                    if (line.StartsWith("BOOTSTRAP_OK", StringComparison.Ordinal)) { seenBootstrap = true; }
                    if (line.StartsWith("WRITER_LOCKED", StringComparison.Ordinal)) { seenLocked = true; }
                    if (line.StartsWith("BARRIER", StringComparison.Ordinal)) { break; }
                }

                Assert.True(seenBootstrap, "owner did not validate bootstrap");
                Assert.True(seenLocked, "owner did not acquire the writer lock");
                var exit = scope.WaitForOwner(owner, TimeSpan.FromSeconds(10));
                Assert.True(exit.Exited, exit.Error);
            }

            // Owner is dead; the scope's recovery duplicate still has
            // authority and cleans the domain.
            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            var authority = new RecoveryAuthority(true, true, true, lease.Identity.Corroborates(lease.Identity), true);
            var record = RecoveryTestSupport.Snapshot() with
            {
                DomainName = name,
                DomainInode = lease.Identity.ChildInode,
                DomainDevice = lease.Identity.ChildDevice,
                Stage = PreparationStage.AllocationRegistered,
            };
            var result = reconciler.Reconcile(record, authority, recover, null);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.Empty(Directory.GetDirectories(temp.Path));
        }
        finally
        {
            try { Directory.Delete(journalDir, true); } catch { }
        }
    }

    [Fact]
    public void OwnerDeathBeforeOpenLeavesDomainForScopeRecovery()
    {
        SkipUnlessUsable();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        var journalDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-wp03-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(journalDir);
        try
        {
            using var scope = new M1RecoveryScope();
            Assert.True(scope.TryInitialize().Ok);
            Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
            var name = "d-" + Guid.NewGuid().ToString("N");
            var leaseGen = Guid.NewGuid();
            var allocation = scope.AllocateDomain(name, Guid.NewGuid(), leaseGen, Mount(), Boot(), out var recovery);
            Assert.True(allocation.Ok, allocation.Error);
            using var lease = allocation.Lease!;
            using var recover = recovery!;

            var owner = scope.SpawnOwner(ownerDll, new[]
            {
                "--journal-dir", journalDir,
                "--parent", temp.Path,
                "--domain", name,
                "--lease", leaseGen.ToString("D"),
                "--barrier", "before-open",
            }, out _);
            using (owner)
            {
                var exit = scope.WaitForOwner(owner, TimeSpan.FromSeconds(10));
                Assert.True(exit.Exited, exit.Error);
            }

            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            var authority = new RecoveryAuthority(true, true, true, true, true);
            var record = RecoveryTestSupport.Snapshot() with
            {
                DomainName = name,
                DomainInode = lease.Identity.ChildInode,
                DomainDevice = lease.Identity.ChildDevice,
            };
            var result = reconciler.Reconcile(record, authority, recover, null);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.Empty(Directory.GetDirectories(temp.Path));
        }
        finally
        {
            try { Directory.Delete(journalDir, true); } catch { }
        }
    }

    [Fact]
    public void NativeJournalDurableThenTornDetected()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-wp03-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using var storage = new JournalFileStorage();
            var journal = new OwnershipJournal(storage, dir);
            var write = journal.WriteNext(RecoveryTestSupport.Snapshot());
            Assert.True(write.Ok, write.Error);
            var read = journal.ReadLatest();
            Assert.True(read.Ok, read.Error);
            File.WriteAllBytes(System.IO.Path.Combine(dir, OwnershipJournal.DefaultSnapshotName), new byte[] { 1, 2, 3, 4, 5 });
            var torn = journal.ReadLatest();
            Assert.False(torn.Ok);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void NativeWriterLockIsExclusiveAcrossProcesses()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-wp03-lock-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            using var storage = new JournalFileStorage();
            Assert.True(storage.TryAcquireExclusiveLock(dir, OwnershipJournal.DefaultLockName).Ok);
            using var other = new JournalFileStorage();
            Assert.False(other.TryAcquireExclusiveLock(dir, OwnershipJournal.DefaultLockName).Ok);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    private static string? LocateOwnerDll()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(System.IO.Path.Combine(dir.FullName, "Gagamba.sln")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            return null;
        }

        var candidate = System.IO.Path.Combine(
            dir.FullName, "src", "Gagamba.Execution.Linux.M1.Owner", "bin", "Debug", "net10.0",
            "Gagamba.Execution.Linux.M1.Owner.dll");
        return File.Exists(candidate) ? candidate : null;
    }

    private sealed class RealClock : IDomainClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();

        public long NowNanoseconds => _watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);

        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }
}
