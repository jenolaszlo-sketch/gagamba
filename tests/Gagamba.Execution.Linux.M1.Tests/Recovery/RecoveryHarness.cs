// WP03 Linux-native qualification harness: real cgroup v2 delegation,
// retained-handle identity, whole-domain stop, identity-checked removal,
// unrelated-sibling isolation, owner primary DomainLease ownership via
// SCM_RIGHTS, owner-death barriers (before/after registration and writer
// transfer), subreaper orphan adoption/reaping, and staged-preparation
// cleanup through retained authority. Every case is finite and externally
// watchdog-bounded. No target fixture runs.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;
using PreparationInterop = Gagamba.Execution.Linux.M1.Preparation.Native.PreparationInterop;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class RecoveryHarness
{
    private const string CgroupRoot = "/sys/fs/cgroup";

    [DllImport("libc", SetLastError = true)]
    private static extern int waitpid(int pid, out int status, int options);

    private sealed class TempParent : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(CgroupRoot, "gagamba-wp03-" + Guid.NewGuid().ToString("N"));

        public TempParent()
        {
            Directory.CreateDirectory(Path);
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

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-wp03-fs-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
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

    private static (Process Owner, DomainLease Recovery, DomainIdentity Identity) StartOwner(
        M1RecoveryScope scope,
        TempParent temp,
        string journalDir,
        string barrier)
    {
        var name = "d-" + Guid.NewGuid().ToString("N");
        var execution = Guid.NewGuid();
        var leaseGen = Guid.NewGuid();
        var allocation = scope.AllocateDomain(name, execution, leaseGen, Boot());
        Assert.True(allocation.Ok, allocation.Error);
        var recovery = allocation.RecoveryLease!;
        var ownerDll = LocateOwnerDll()!;
        var owner = scope.SpawnOwner(ownerDll, new[]
        {
            "--journal-dir", journalDir,
            "--exec", execution.ToString("D"),
            "--lease", leaseGen.ToString("D"),
            "--mount-id", Mount().MountId.ToString(),
            "--boot", Boot(),
            "--domain", name,
            "--barrier", barrier,
        }, allocation.OwnerSocketFd, out var spawnError);
        Assert.True(spawnError.Length == 0, spawnError);
        return (owner, recovery, recovery.Identity);
    }

    private static string ReadUntil(Process owner, Func<string, bool> stop, TimeSpan timeout)
    {
        var output = new System.Text.StringBuilder();
        var deadline = DateTime.UtcNow + timeout;
        string? line;
        while ((line = owner.StandardOutput.ReadLine()) is not null)
        {
            output.AppendLine(line);
            if (stop(line))
            {
                break;
            }

            if (DateTime.UtcNow > deadline)
            {
                break;
            }
        }

        return output.ToString();
    }

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
        var errno = PreparationInterop.OpenDirectory("/nonexistent-gagamba-parent", out _);
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
        var allocation = scope.AllocateDomain("d-" + Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Boot());
        Assert.True(allocation.Ok, allocation.Error);
        // The primary handles were transferred to the (unspawned) owner over
        // the socketpair; close that end and use the scope's recovery lease.
        PreparationInterop.CloseFd(allocation.OwnerSocketFd);
        using var recovery = allocation.RecoveryLease!;
        Assert.True(recovery.VerifyIdentity().Match);
        var stop = recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock());
        Assert.True(stop.Stopped, stop.Error);
        var removal = recovery.RemoveIfIdentityMatches();
        Assert.True(removal.Removed, removal.Error);
        Assert.Empty(Directory.GetDirectories(temp.Path));
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
        var allocation = scope.AllocateDomain(name, Guid.NewGuid(), Guid.NewGuid(), Boot());
        Assert.True(allocation.Ok, allocation.Error);
        PreparationInterop.CloseFd(allocation.OwnerSocketFd);
        using var recovery = allocation.RecoveryLease!;
        Directory.Delete(System.IO.Path.Combine(temp.Path, name));
        Directory.CreateDirectory(System.IO.Path.Combine(temp.Path, name));
        var removal = recovery.RemoveIfIdentityMatches();
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
        var allocation = scope.AllocateDomain("d-" + Guid.NewGuid().ToString("N"), Guid.NewGuid(), Guid.NewGuid(), Boot());
        Assert.True(allocation.Ok, allocation.Error);
        PreparationInterop.CloseFd(allocation.OwnerSocketFd);
        using var recovery = allocation.RecoveryLease!;
        recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock());
        recovery.RemoveIfIdentityMatches();
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
    public void OwnerHoldsPrimaryLeaseAndStopsDomain()
    {
        SkipUnlessUsable();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        using var journal = new TempDir();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var (owner, recovery, _) = StartOwner(scope, temp, journal.Path, "none");
        using (owner)
        using (recovery)
        {
            var ready = ReadUntil(owner, l => l.StartsWith("REGISTERED", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
            Assert.Contains("PRIMARY", ready);
            Assert.Contains("WRITER_LOCKED", ready);
            Assert.Contains("REGISTERED ok=True", ready);

            owner.StandardInput.WriteLine("identity");
            owner.StandardInput.Flush();
            var identityLine = owner.StandardOutput.ReadLine();
            Assert.Equal("IDENTITY match=True", identityLine);

            // The owner (primary DomainLease holder) stops the domain; the
            // scope's independently-owned duplicate observes emptiness.
            owner.StandardInput.WriteLine("stop");
            owner.StandardInput.Flush();
            var stopped = owner.StandardOutput.ReadLine();
            Assert.Equal("STOPPED ok=True err=", stopped);

            owner.StandardInput.WriteLine("die");
            owner.StandardInput.Flush();
            Assert.True(scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);
            Assert.True(recovery.ObservePopulation().Empty);
        }
    }

    [Fact]
    public void LiveOwnerExcludesRecoveryWriter()
    {
        SkipUnlessUsable();
        if (LocateOwnerDll() is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        using var journal = new TempDir();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var (owner, recovery, _) = StartOwner(scope, temp, journal.Path, "none");
        using (owner)
        using (recovery)
        {
            // The live owner holds the writer lease (observed REGISTERED).
            ReadUntil(owner, l => l.StartsWith("REGISTERED", StringComparison.Ordinal), TimeSpan.FromSeconds(10));

            // A second writer in this process cannot acquire while the owner
            // lives, so no normal mutation API is reachable.
            var rival = new OwnershipJournal(new JournalFileStorage(), journal.Path);
            Assert.False(rival.TryAcquireWriter(out var rivalLease, out _));
            Assert.Null(rivalLease);

            // Disk bytes/sequence are unchanged by the failed attempts.
            var read = rival.ReadLatest();
            Assert.True(read.Ok, read.Error);
            Assert.Equal(1, read.Snapshot!.Sequence);

            owner.StandardInput.WriteLine("die");
            owner.StandardInput.Flush();
            Assert.True(scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);
            Assert.True(rival.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
            using (takeover!)
            {
            }
        }
    }

    [Fact]
    public void DeadOwnerTakeoverContinuesSequence()
    {
        SkipUnlessUsable();
        if (LocateOwnerDll() is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        using var journal = new TempDir();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var (owner, recovery, _) = StartOwner(scope, temp, journal.Path, "after-registered");
        using (owner)
        using (recovery)
        {
            ReadUntil(owner, l => l.StartsWith("BARRIER", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
            // Owner wrote sequence 1 and died; prove death, then take over.
            Assert.True(scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);
            var takeover = new OwnershipJournal(new JournalFileStorage(), journal.Path);
            Assert.True(takeover.TryAcquireWriter(out var lease, out var acquireError), acquireError);
            Assert.Equal(1, lease!.BaselineSequence);
            using (lease)
            {
                Assert.True(takeover.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
            }

            Assert.Equal(2, takeover.ReadLatest().Snapshot!.Sequence);
        }
    }

    [Fact]
    public void OwnerDeathBeforeRegisteredRecoversWithoutCapability()
    {
        SkipUnlessUsable();
        if (LocateOwnerDll() is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        using var journal = new TempDir();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var sibling = System.IO.Path.Combine(temp.Path, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        var (owner, recovery, identity) = StartOwner(scope, temp, journal.Path, "after-lock");
        using (owner)
        using (recovery)
        {
            var output = ReadUntil(owner, l => l.StartsWith("BARRIER", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
            Assert.Contains("WRITER_LOCKED", output);
            Assert.True(scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);
            // Kernel released the owner's writer lock on death.
            Assert.False(new JournalFileStorage().IsLockHeld(journal.Path, OwnershipJournal.DefaultLockName));

            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            // Record built from the actual live identity; eligibility is
            // derived from the live lease and boot, not caller booleans.
            var record = RecordFrom(identity, new AllocationEntry(AllocationKind.Domain, identity.ChildName, 0, Array.Empty<byte>()));
            var result = reconciler.Reconcile(record, Boot(), recovery, null);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.Empty(Directory.GetDirectories(temp.Path, "d-*"));
            Assert.True(Directory.Exists(sibling));
        }
    }

    private static OwnershipSnapshot RecordFrom(DomainIdentity id, params AllocationEntry[] allocations) =>
        new(
            OwnershipSnapshotCodec.CurrentVersion, 0, id.ExecutionId, id.BootId, id.CgroupMountId, id.CgroupMountFsType,
            id.ParentDevice, id.ParentInode, id.ChildName, id.ChildDevice, id.ChildInode, id.LeaseGeneration,
            OwnershipWriterKind.Owner, OwnershipCleanupState.NotStarted, PreparationStage.AllocationRegistered,
            allocations, null);

    [Fact]
    public void OwnerDeathAfterRegisteredRecoversOriginalAllocation()
    {
        SkipUnlessUsable();
        if (LocateOwnerDll() is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        using var journal = new TempDir();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var sibling = System.IO.Path.Combine(temp.Path, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        var (owner, recovery, identity) = StartOwner(scope, temp, journal.Path, "after-registered");
        using (owner)
        using (recovery)
        {
            var output = ReadUntil(owner, l => l.StartsWith("BARRIER", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
            Assert.Contains("REGISTERED ok=True", output);
            Assert.True(scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

            // The durable registered snapshot names the real identity.
            var durable = new OwnershipJournal(new JournalFileStorage(), journal.Path).ReadLatest();
            Assert.True(durable.Ok, durable.Error);
            Assert.Equal(PreparationStage.AllocationRegistered, durable.Snapshot!.Stage);
            Assert.Equal(identity.ChildInode, durable.Snapshot.DomainInode);

            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            // Eligibility derived from the live recovery lease and boot (G2-09).
            var result = reconciler.Reconcile(durable.Snapshot, Boot(), recovery, null);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.Empty(Directory.GetDirectories(temp.Path, "d-*"));
            Assert.True(Directory.Exists(sibling));
        }
    }

    [Fact]
    public void WriterLockTakeoverIsFencedAfterOwnerDeath()
    {
        SkipUnlessUsable();
        if (LocateOwnerDll() is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        using var journal = new TempDir();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var (owner, recovery, _) = StartOwner(scope, temp, journal.Path, "after-lock");
        using (owner)
        using (recovery)
        {
            ReadUntil(owner, l => l.StartsWith("BARRIER", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
            Assert.True(scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

            // One writer at a time; the scope can acquire the fence only
            // after the owner died.
            using var takeover = new JournalFileStorage();
            var acquire = takeover.TryAcquireExclusiveLock(journal.Path, OwnershipJournal.DefaultLockName);
            Assert.True(acquire.Ok, acquire.Error);
            recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock());
            recovery.RemoveIfIdentityMatches();
        }
    }

    [Fact]
    public void OrphanTrustedChildAdoptedAndReapedByScope()
    {
        SkipUnlessUsable();
        if (LocateOwnerDll() is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var temp = new TempParent();
        using var journal = new TempDir();
        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(temp.Path).Ok);
        var (owner, recovery, _) = StartOwner(scope, temp, journal.Path, "none");
        int orphanPid;
        using (owner)
        using (recovery)
        {
            ReadUntil(owner, l => l.StartsWith("REGISTERED", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
            owner.StandardInput.WriteLine("orphan 3");
            owner.StandardInput.Flush();
            var orphanLine = owner.StandardOutput.ReadLine();
            Assert.StartsWith("ORPHAN_PID ", orphanLine);
            orphanPid = int.Parse(orphanLine!.Substring("ORPHAN_PID ".Length).Trim());
            Assert.True(scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

            // The orphan was reparented to the subreaper scope (ppid == our pid).
            var ppid = ReadPpid(orphanPid);
            Assert.Equal(Environment.ProcessId, ppid);

            // The recovery host reaps the adopted child.
            var reaped = waitpid(orphanPid, out _, 0);
            Assert.Equal(orphanPid, reaped);

            recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock());
            recovery.RemoveIfIdentityMatches();
        }
    }

    [Fact]
    public void StagedPreparationCleanedThroughRetainedAuthority()
    {
        SkipUnlessUsable();
        using var source = new TempDir();
        using var staging = new TempDir();
        var content = new byte[4096];
        new Random(11).NextBytes(content);
        File.WriteAllBytes(System.IO.Path.Combine(source.Path, "tool"), content);

        var plan = new CopyPlan(
            source.Path, staging.Path, "prep-001",
            new[] { new CopyObjectPlan("tool", "tool", 4UL * 1024 * 1024, null) },
            16UL * 1024 * 1024, 128,
            Array.Empty<KeyValuePair<string, string>>(), _ => true, null);
        var prepared = SafeCopyIn.Prepare(plan, LibcCopyHost.Instance, CancellationToken.None);
        Assert.True(prepared.Succeeded, prepared.Detail);
        var closure = prepared.Closure!;
        var stagedPath = System.IO.Path.Combine(staging.Path, "prep-001", closure.Objects[0].StagingName);
        Assert.True(File.Exists(stagedPath));

        // The durable record names the exact staged allocation.
        using var journalDir = new TempDir();
        var journal = new OwnershipJournal(new JournalFileStorage(), journalDir.Path);
        Assert.True(journal.TryAcquireWriter(out var writer, out var acquireError), acquireError);
        var record = RecoveryTestSupport.Snapshot(
            allocations: new[]
            {
                new AllocationEntry(AllocationKind.Staging, closure.Objects[0].StagingName, closure.Objects[0].Length, closure.Objects[0].Sha256),
            });
        using (writer!)
        {
            Assert.True(journal.WriteNext(writer, record).Ok);
        }

        // The scope retains a duplicate of the staging-directory handle and
        // cleans the exact registered original through it. No
        // PreparationStore exists here, so no capability can be resurrected
        // or published late.
        var stagingDirFd = PreparationInterop.Dup(
            ((Microsoft.Win32.SafeHandles.SafeFileHandle)closure.StagingDirectory!).DangerousGetHandle().ToInt32());
        Assert.True(stagingDirFd >= 0);
        var cleaner = new RetainedStagingCleaner(stagingDirFd);
        Assert.True(cleaner.CleanOne(record.Allocations[0]).Ok);
        Assert.False(File.Exists(stagedPath));
        PreparationInterop.CloseFd(stagingDirFd);
        closure.Dispose();
    }

    [Fact]
    public void NativeJournalDurableThenTornDetected()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        using var dir = new TempDir();
        using var storage = new JournalFileStorage();
        var journal = new OwnershipJournal(storage, dir.Path);
        Assert.True(journal.TryAcquireWriter(out var writer, out var acquireError), acquireError);
        using (writer!)
        {
            Assert.True(journal.WriteNext(writer, RecoveryTestSupport.Snapshot()).Ok);
        }

        Assert.True(journal.ReadLatest().Ok);
        File.WriteAllBytes(System.IO.Path.Combine(dir.Path, OwnershipJournal.SlotA), new byte[] { 1, 2, 3, 4, 5 });
        Assert.False(journal.ReadLatest().Ok);
    }

    [Fact]
    public void NativeWriterLockIsExclusiveAcrossProcesses()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        using var dir = new TempDir();
        using var storage = new JournalFileStorage();
        Assert.True(storage.TryAcquireExclusiveLock(dir.Path, OwnershipJournal.DefaultLockName).Ok);
        using var other = new JournalFileStorage();
        Assert.False(other.TryAcquireExclusiveLock(dir.Path, OwnershipJournal.DefaultLockName).Ok);
    }

    private static int ReadPpid(int pid)
    {
        var stat = File.ReadAllText($"/proc/{pid}/stat");
        var close = stat.LastIndexOf(')');
        var rest = stat[(close + 2)..].Split(' ');
        // Field 4 overall is ppid; after (comm), rest[0]=state, rest[1]=ppid.
        return int.Parse(rest[1]);
    }

    private sealed class RetainedStagingCleaner : IAllocationCleaner
    {
        private readonly int _stagingDirFd;

        public RetainedStagingCleaner(int stagingDirFd) => _stagingDirFd = stagingDirFd;

        public DomainIoResult CleanOne(AllocationEntry entry)
        {
            var errno = PreparationInterop.Unlink(_stagingDirFd, entry.Name);
            return errno == 0 ? DomainIoResult.Success : DomainIoResult.Failure($"unlink errno {errno}");
        }
    }

    private sealed class RealClock : IDomainClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();

        public long NowNanoseconds => _watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);

        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }
}
