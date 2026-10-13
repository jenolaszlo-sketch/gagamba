// WP03 G2-10 non-root delegated qualification. Runs the real corrected
// R1-R4 topology under an operator-supplied, already-existing unprivileged
// cgroup v2 delegation: no root, no sudo, no capability, no host
// provisioning by Gagamba. The process must already live inside the
// delegated subtree. Owner pidfd is explicitly acquired, retained and used
// for owner-exit proof; missing pidfd refuses. Cases skip honestly unless
// GAGAMBA_DELEGATED_CGROUP is set and the process is genuinely non-root.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class NonRootQualificationTests
{
    [DllImport("libc", SetLastError = true)]
    private static extern int waitpid(int pid, out int status, int options);

    private static string? DelegatedParent => Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP");

    private static string StatusField(string name)
    {
        foreach (var line in File.ReadAllLines("/proc/self/status"))
        {
            if (line.StartsWith(name + ":", StringComparison.Ordinal))
            {
                return line[(name.Length + 1)..].Trim();
            }
        }

        return string.Empty;
    }

    private static int RealUid => int.Parse(StatusField("Uid").Split('\t')[0]);

    private static int EffectiveUid => int.Parse(StatusField("Uid").Split('\t')[1]);

    private static int RealGid => int.Parse(StatusField("Gid").Split('\t')[0]);

    private static string CapEff => StatusField("CapEff");

    private static string CapPrm => StatusField("CapPrm");

    private static string CapAmb => StatusField("CapAmb");

    private static string CapInh => StatusField("CapInh");

    private static string CapBnd => StatusField("CapBnd");

    private static string NoNewPrivs => StatusField("NoNewPrivs");

    private static void SkipUnlessQualified()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        if (string.IsNullOrEmpty(DelegatedParent))
        {
            Assert.Skip("Unavailable: GAGAMBA_DELEGATED_CGROUP not set; non-root delegated qualification requires an operator-supplied parent");
        }

        if (RealUid == 0)
        {
            Assert.Skip("Unqualified: test process is root; the delegated topology requires uid != 0");
        }

        if (CapEff != "0000000000000000")
        {
            Assert.Skip($"Unqualified: effective capabilities are not empty (CapEff={CapEff})");
        }

        if (!Directory.Exists(DelegatedParent))
        {
            Assert.Skip("Unavailable: delegated parent path does not exist");
        }
    }

    private static string Boot() => CgroupInterop.TryReadBootId(out var id, out _) ? id : "unknown";

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

    private static string[] OwnerArgs(string journalDir, string name, Guid exec, Guid leaseGen, string barrier) => new[]
    {
        "--journal-dir", journalDir,
        "--exec", exec.ToString("D"),
        "--lease", leaseGen.ToString("D"),
        "--domain", name,
        "--barrier", barrier,
    };

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

    private static int ReadPpid(int pid)
    {
        var stat = File.ReadAllText($"/proc/{pid}/stat");
        var close = stat.LastIndexOf(')');
        var rest = stat[(close + 2)..].Split(' ');
        return int.Parse(rest[1]);
    }

    private static OwnershipSnapshot RecordFrom(DomainIdentity id, params AllocationEntry[] allocations) =>
        new(
            OwnershipSnapshotCodec.CurrentVersion, 0, id.ExecutionId, id.BootId, id.CgroupMountId, id.CgroupMountFsType,
            id.ParentDevice, id.ParentInode, id.ChildName, id.ChildDevice, id.ChildInode, id.LeaseGeneration,
            OwnershipWriterKind.Owner, OwnershipCleanupState.NotStarted, PreparationStage.AllocationRegistered,
            StagingParentDevice: 0, StagingParentInode: 0,
            Allocations: allocations, Preparation: null);

    private sealed class FailingPidFdProvider : IPidFdProvider
    {
        public bool TryOpen(int pid, out int fd, out string error)
        {
            fd = -1;
            error = "injected pidfd failure";
            return false;
        }
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-r5-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }

    /// <summary>Owns the scope plus every domain child it created under the
    /// operator-delegated parent, so cleanup touches only test-owned
    /// allocations and never the delegation itself.</summary>
    private sealed class QualHost : IDisposable
    {
        private readonly List<string> _domains = new();

        public QualHost(IPidFdProvider? provider = null)
        {
            Scope = new M1RecoveryScope(provider);
            Assert.True(Scope.TryInitialize().Ok);
            Assert.True(Scope.TryOpenDelegatedParent(DelegatedParent!).Ok);
        }

        public M1RecoveryScope Scope { get; }

        public string NewDomainName()
        {
            var name = "d-" + Guid.NewGuid().ToString("N");
            _domains.Add(name);
            return name;
        }

        public void Dispose()
        {
            Scope.Dispose();
            foreach (var domain in _domains)
            {
                var path = System.IO.Path.Combine(DelegatedParent!, domain);
                try { File.WriteAllText(System.IO.Path.Combine(path, "cgroup.kill"), "1"); } catch { }
                try { Directory.Delete(path); } catch { }
            }
        }
    }

    private sealed class RealClock : IDomainClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();

        public long NowNanoseconds => _watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);

        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }

    private sealed class QualScopeCleaner : IAllocationCleaner
    {
        private readonly int _parentFd;
        private readonly SafeHandle _subdir;

        public QualScopeCleaner(int parentFd, int subdirFd)
        {
            _parentFd = parentFd;
            _subdir = new SafeFileHandle(new IntPtr(subdirFd), ownsHandle: false);
        }

        public AllocationCleanResult CleanOne(AllocationEntry entry)
        {
            if (entry.Kind == AllocationKind.StagingDirectory)
            {
                var dirFd = ((SafeFileHandle)_subdir).DangerousGetHandle().ToInt32();
                if (PreparationInterop.StatFd(dirFd, out var current) != 0
                    || current.Device != entry.Device
                    || current.Inode != entry.Inode)
                {
                    return new AllocationCleanResult(AllocationCleanOutcome.Mismatch, "staging directory identity mismatch");
                }

                var errno = PreparationInterop.RemoveDirectory(_parentFd, entry.Name);
                return errno == 0
                    ? new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty)
                    : new AllocationCleanResult(AllocationCleanOutcome.Failed, $"rmdir errno {errno}");
            }

            if (entry.Kind != AllocationKind.Staging)
            {
                return new AllocationCleanResult(AllocationCleanOutcome.Failed, $"unexpected entry kind {entry.Kind}");
            }

            var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, _subdir, entry.Name, entry.Device, entry.Inode);
            return result.Outcome switch
            {
                CleanupItemOutcome.Removed => new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty),
                CleanupItemOutcome.AlreadyAbsent => new AllocationCleanResult(AllocationCleanOutcome.AlreadyAbsent, string.Empty),
                CleanupItemOutcome.IdentityMismatch => new AllocationCleanResult(AllocationCleanOutcome.Mismatch, result.Error),
                _ => new AllocationCleanResult(AllocationCleanOutcome.Failed, result.Error),
            };
        }
    }

    // ---- Preflight and delegation ----

    [Fact]
    public void NonRootDelegatedPreflightSucceeds()
    {
        SkipUnlessQualified();
        Assert.NotEqual(0, RealUid);
        Assert.NotEqual(0, EffectiveUid);
        Assert.Equal("0000000000000000", CapEff);
        Assert.Equal("0000000000000000", CapPrm);
        Assert.Equal("0000000000000000", CapAmb);
        Assert.True(RecoverySubreaper.TryEnableAndVerify(out var subreaperError), subreaperError);
        Assert.NotEqual(Openat2SupportKind.Unsupported, PreparationInterop.ProbeOpenat2Support());
        Assert.True(CgroupInterop.TryReadCgroupMount(out var mount, out var mountError), mountError);
        Assert.Equal("cgroup2", mount!.FileSystemType);
        Assert.True(CgroupInterop.TryReadBootId(out var boot, out var bootError), bootError);

        using var host = new QualHost();
        var name = host.NewDomainName();
        var allocation = host.Scope.AllocateDomain(name, Guid.NewGuid(), Guid.NewGuid(), boot);
        Assert.True(allocation.Ok, allocation.Error);
        PreparationInterop.CloseFd(allocation.OwnerSocketFd);
        using var recovery = allocation.RecoveryLease!;
        Assert.True(recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock()).Stopped);
        Assert.True(recovery.RemoveIfIdentityMatches().Removed);

        Console.WriteLine(
            $"G2-10 inventory: uid={RealUid} euid={EffectiveUid} gid={RealGid} capInh={CapInh} capPrm={CapPrm} " +
            $"capEff={CapEff} capBnd={CapBnd} capAmb={CapAmb} noNewPrivs={NoNewPrivs} kernel={Environment.OSVersion.Version} " +
            $"boot={boot} mountId={mount.MountId} mountPoint={mount.MountPoint} parent={DelegatedParent}");
    }

    [Fact]
    public void MissingDelegationRefusesWithoutProvisioning()
    {
        SkipUnlessQualified();
        var missing = System.IO.Path.Combine(DelegatedParent!, "does-not-exist-" + Guid.NewGuid().ToString("N"));
        using (var scope = new M1RecoveryScope())
        {
            Assert.True(scope.TryInitialize().Ok);
            var result = scope.TryOpenDelegatedParent(missing);
            Assert.False(result.Ok);
        }

        Assert.False(Directory.Exists(missing), "Gagamba must not provision a missing delegated parent");

        // A directory that is not an already-delegated cgroup2 parent refuses.
        using var tmp = new TempDir();
        using (var scope = new M1RecoveryScope())
        {
            Assert.True(scope.TryInitialize().Ok);
            var result = scope.TryOpenDelegatedParent(tmp.Path);
            Assert.False(result.Ok);
        }
    }

    // ---- Owner pidfd contract ----

    [Fact]
    public void OwnerPidfdAcquiredAndRetained()
    {
        SkipUnlessQualified();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var host = new QualHost();
        using var journal = new TempDir();
        var name = host.NewDomainName();
        var exec = Guid.NewGuid();
        var leaseGen = Guid.NewGuid();
        var allocation = host.Scope.AllocateDomain(name, exec, leaseGen, Boot());
        Assert.True(allocation.Ok, allocation.Error);
        using var recovery = allocation.RecoveryLease!;
        var spawned = host.Scope.TrySpawnOwner(ownerDll, OwnerArgs(journal.Path, name, exec, leaseGen, "none"), allocation.OwnerSocketFd);
        Assert.True(spawned.Ok, spawned.Error);
        Assert.True(spawned.PidFd >= 0);
        Assert.True(PreparationInterop.HasCloexec(spawned.PidFd));

        using var owner = spawned.Process!;
        Assert.True(host.Scope.TryOwnerPidFdExited(owner, 0, out var live, out var liveError), liveError);
        Assert.False(live);
        var ready = ReadUntil(owner, l => l.StartsWith("REGISTERED", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        Assert.Contains("WRITER_LOCKED", ready);
        Assert.Contains("REGISTERED ok=True", ready);

        owner.StandardInput.WriteLine("die");
        owner.StandardInput.Flush();
        Assert.True(host.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);
        Assert.True(host.Scope.TryOwnerPidFdExited(owner, 1000, out var dead, out var deadError), deadError);
        Assert.True(dead);
    }

    [Fact]
    public void MissingPidfdRefuses()
    {
        SkipUnlessQualified();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var host = new QualHost(new FailingPidFdProvider());
        using var journal = new TempDir();
        var name = host.NewDomainName();
        var exec = Guid.NewGuid();
        var leaseGen = Guid.NewGuid();
        var allocation = host.Scope.AllocateDomain(name, exec, leaseGen, Boot());
        Assert.True(allocation.Ok, allocation.Error);
        using var recovery = allocation.RecoveryLease!;
        var spawned = host.Scope.TrySpawnOwner(ownerDll, OwnerArgs(journal.Path, name, exec, leaseGen, "none"), allocation.OwnerSocketFd);
        Assert.False(spawned.Ok);
        Assert.Null(spawned.Process);
        Assert.Contains("pidfd", spawned.Error);
    }

    [Fact]
    public void LiveOwnerPidfdBlocksRecoveryTakeover()
    {
        SkipUnlessQualified();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var host = new QualHost();
        using var journal = new TempDir();
        var name = host.NewDomainName();
        var exec = Guid.NewGuid();
        var leaseGen = Guid.NewGuid();
        var allocation = host.Scope.AllocateDomain(name, exec, leaseGen, Boot());
        Assert.True(allocation.Ok, allocation.Error);
        using var recovery = allocation.RecoveryLease!;
        var spawned = host.Scope.TrySpawnOwner(ownerDll, OwnerArgs(journal.Path, name, exec, leaseGen, "none"), allocation.OwnerSocketFd);
        Assert.True(spawned.Ok, spawned.Error);
        using var owner = spawned.Process!;
        var ready = ReadUntil(owner, l => l.StartsWith("REGISTERED", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        Assert.Contains("REGISTERED ok=True", ready);

        Assert.True(host.Scope.TryOwnerPidFdExited(owner, 0, out var live, out _));
        Assert.False(live);

        var rival = new OwnershipJournal(new JournalFileStorage(), journal.Path);
        Assert.False(rival.TryAcquireWriter(out var rivalLease, out _));
        Assert.Null(rivalLease);
        Assert.Equal(1, rival.ReadLatest().Snapshot!.Sequence);

        owner.StandardInput.WriteLine("die");
        owner.StandardInput.Flush();
        Assert.True(host.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);
        Assert.True(rival.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
        using (takeover!)
        {
        }
    }

    [Fact]
    public void OwnerDeathPidfdAllowsFencedTakeover()
    {
        SkipUnlessQualified();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var host = new QualHost();
        using var journal = new TempDir();
        var name = host.NewDomainName();
        var exec = Guid.NewGuid();
        var leaseGen = Guid.NewGuid();
        var allocation = host.Scope.AllocateDomain(name, exec, leaseGen, Boot());
        Assert.True(allocation.Ok, allocation.Error);
        using var recovery = allocation.RecoveryLease!;
        var spawned = host.Scope.TrySpawnOwner(ownerDll, OwnerArgs(journal.Path, name, exec, leaseGen, "after-registered"), allocation.OwnerSocketFd);
        Assert.True(spawned.Ok, spawned.Error);
        using var owner = spawned.Process!;
        ReadUntil(owner, l => l.StartsWith("BARRIER", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        Assert.True(host.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

        var takeover = new OwnershipJournal(new JournalFileStorage(), journal.Path);
        Assert.True(takeover.TryAcquireWriter(out var lease, out var acquireError), acquireError);
        Assert.Equal(1, lease!.BaselineSequence);
        using (lease)
        {
            Assert.True(takeover.WriteNext(lease, RecoveryTestSupport.Snapshot()).Ok);
        }

        Assert.Equal(2, takeover.ReadLatest().Snapshot!.Sequence);
    }

    // ---- Non-root authority and owner death ----

    [Fact]
    public void NonRootPrimaryAndRecoveryLeasesRemainIndependent()
    {
        SkipUnlessQualified();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var host = new QualHost();
        using var journal = new TempDir();
        var name = host.NewDomainName();
        var exec = Guid.NewGuid();
        var leaseGen = Guid.NewGuid();
        var allocation = host.Scope.AllocateDomain(name, exec, leaseGen, Boot());
        Assert.True(allocation.Ok, allocation.Error);
        using var recovery = allocation.RecoveryLease!;
        var spawned = host.Scope.TrySpawnOwner(ownerDll, OwnerArgs(journal.Path, name, exec, leaseGen, "none"), allocation.OwnerSocketFd);
        Assert.True(spawned.Ok, spawned.Error);
        using var owner = spawned.Process!;
        var ready = ReadUntil(owner, l => l.StartsWith("REGISTERED", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        Assert.Contains("PRIMARY", ready);

        owner.StandardInput.WriteLine("identity");
        owner.StandardInput.Flush();
        Assert.Equal("IDENTITY match=True", owner.StandardOutput.ReadLine());

        owner.StandardInput.WriteLine("stop");
        owner.StandardInput.Flush();
        Assert.Equal("STOPPED ok=True err=", owner.StandardOutput.ReadLine());
        Assert.True(recovery.ObservePopulation().Empty);

        owner.StandardInput.WriteLine("die");
        owner.StandardInput.Flush();
        Assert.True(host.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

        // The recovery duplicate remains independently usable after the
        // primary holder died.
        Assert.True(recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock()).Stopped);
        Assert.True(recovery.RemoveIfIdentityMatches().Removed);
    }

    [Fact]
    public void NonRootOwnerDeathBeforeRegistrationRecovers()
    {
        SkipUnlessQualified();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var host = new QualHost();
        using var journal = new TempDir();
        var sibling = System.IO.Path.Combine(DelegatedParent!, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        try
        {
            var name = host.NewDomainName();
            var exec = Guid.NewGuid();
            var leaseGen = Guid.NewGuid();
            var allocation = host.Scope.AllocateDomain(name, exec, leaseGen, Boot());
            Assert.True(allocation.Ok, allocation.Error);
            using var recovery = allocation.RecoveryLease!;
            var spawned = host.Scope.TrySpawnOwner(ownerDll, OwnerArgs(journal.Path, name, exec, leaseGen, "after-lock"), allocation.OwnerSocketFd);
            Assert.True(spawned.Ok, spawned.Error);
            using var owner = spawned.Process!;
            var output = ReadUntil(owner, l => l.StartsWith("BARRIER", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
            Assert.Contains("WRITER_LOCKED", output);
            Assert.True(host.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            var record = RecordFrom(recovery.Identity, new AllocationEntry(AllocationKind.Domain, recovery.Identity.ChildName, 0, Array.Empty<byte>(), 0, 0, true));
            var recoveryJournal = new OwnershipJournal(new JournalFileStorage(), journal.Path);
            Assert.True(recoveryJournal.TryAcquireWriter(out var writer, out var acquireError), acquireError);
            using (writer!)
            {
                var result = reconciler.Reconcile(record, Boot(), recovery, null, recoveryJournal, writer);
                Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            }

            Assert.False(Directory.Exists(System.IO.Path.Combine(DelegatedParent!, name)));
            Assert.True(Directory.Exists(sibling));
        }
        finally
        {
            try { Directory.Delete(sibling); } catch { }
        }
    }

    [Fact]
    public void NonRootOwnerDeathAfterRegistrationRecovers()
    {
        SkipUnlessQualified();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var host = new QualHost();
        using var journal = new TempDir();
        var sibling = System.IO.Path.Combine(DelegatedParent!, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        try
        {
            var name = host.NewDomainName();
            var exec = Guid.NewGuid();
            var leaseGen = Guid.NewGuid();
            var allocation = host.Scope.AllocateDomain(name, exec, leaseGen, Boot());
            Assert.True(allocation.Ok, allocation.Error);
            using var recovery = allocation.RecoveryLease!;
            var spawned = host.Scope.TrySpawnOwner(ownerDll, OwnerArgs(journal.Path, name, exec, leaseGen, "after-registered"), allocation.OwnerSocketFd);
            Assert.True(spawned.Ok, spawned.Error);
            using var owner = spawned.Process!;
            var output = ReadUntil(owner, l => l.StartsWith("BARRIER", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
            Assert.Contains("REGISTERED ok=True", output);
            Assert.True(host.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

            var durable = new OwnershipJournal(new JournalFileStorage(), journal.Path).ReadLatest();
            Assert.True(durable.Ok, durable.Error);
            Assert.Equal(PreparationStage.AllocationRegistered, durable.Snapshot!.Stage);
            Assert.Equal(recovery.Identity.ChildInode, durable.Snapshot.DomainInode);

            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            var recoveryJournal = new OwnershipJournal(new JournalFileStorage(), journal.Path);
            Assert.True(recoveryJournal.TryAcquireWriter(out var writer, out var acquireError), acquireError);
            using (writer!)
            {
                var result = reconciler.Reconcile(durable.Snapshot, Boot(), recovery, null, recoveryJournal, writer);
                Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            }

            Assert.False(Directory.Exists(System.IO.Path.Combine(DelegatedParent!, name)));
            Assert.True(Directory.Exists(sibling));
        }
        finally
        {
            try { Directory.Delete(sibling); } catch { }
        }
    }

    [Fact]
    public void NonRootOrphanAdoptedAndReapedByScope()
    {
        SkipUnlessQualified();
        var ownerDll = LocateOwnerDll();
        if (ownerDll is null)
        {
            Assert.Skip("owner executable not built");
            return;
        }

        using var host = new QualHost();
        using var journal = new TempDir();
        var name = host.NewDomainName();
        var exec = Guid.NewGuid();
        var leaseGen = Guid.NewGuid();
        var allocation = host.Scope.AllocateDomain(name, exec, leaseGen, Boot());
        Assert.True(allocation.Ok, allocation.Error);
        using var recovery = allocation.RecoveryLease!;
        var spawned = host.Scope.TrySpawnOwner(ownerDll, OwnerArgs(journal.Path, name, exec, leaseGen, "none"), allocation.OwnerSocketFd);
        Assert.True(spawned.Ok, spawned.Error);
        using var owner = spawned.Process!;
        ReadUntil(owner, l => l.StartsWith("REGISTERED", StringComparison.Ordinal), TimeSpan.FromSeconds(10));
        owner.StandardInput.WriteLine("orphan 3");
        owner.StandardInput.Flush();
        var orphanLine = owner.StandardOutput.ReadLine();
        Assert.StartsWith("ORPHAN_PID ", orphanLine);
        var orphanPid = int.Parse(orphanLine!.Substring("ORPHAN_PID ".Length).Trim());
        Assert.True(host.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

        Assert.Equal(Environment.ProcessId, ReadPpid(orphanPid));
        Assert.Equal(orphanPid, waitpid(orphanPid, out _, 0));
    }

    // ---- Non-root integrated R4 path ----

    private sealed class IntegratedFixture : IDisposable
    {
        public TempDir Source = new();
        public TempDir Staging = new();
        public TempDir JournalDir = new();
        public M1RecoveryScope Scope = new();
        public OwnershipJournal Journal;
        public JournalWriterLease Writer;
        public PreparedTransaction.PreparationStore Store;
        public TestClock Clock = new();
        public Guid Exec = Guid.NewGuid();
        public Guid LeaseGen = Guid.NewGuid();
        public string ChildName;

        public IntegratedFixture(byte[] tool, byte[]? input, CopyTestHooks? hooks)
        {
            Assert.True(Scope.TryInitialize().Ok);
            Assert.True(Scope.TryOpenDelegatedParent(DelegatedParent!).Ok);
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "tool"), tool);
            var objects = new List<CopyObjectPlan> { new("tool", "tool", 4UL * 1024 * 1024, null) };
            if (input is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "input"), input);
                objects.Add(new("input", "input", 4UL * 1024 * 1024, null));
            }

            Journal = new OwnershipJournal(new JournalFileStorage(), JournalDir.Path);
            Assert.True(Journal.TryAcquireWriter(out var writer, out var error), error);
            Writer = writer!;
            Store = new PreparedTransaction.PreparationStore(Issuer, Generation, Clock);
            ChildName = "d-" + Guid.NewGuid().ToString("N");
            Plan = new CopyPlan(
                Source.Path, Staging.Path, "prep-001", objects, 16UL * 1024 * 1024, 128,
                Array.Empty<KeyValuePair<string, string>>(), _ => true, hooks);
        }

        public CopyPlan Plan { get; }

        public IntegratedPreparationRequest Request(Action<string>? deathBarrier) =>
            new(
                ChildName, Exec, LeaseGen, Boot(),
                LibcCopyHost.Instance, Plan, Journal, Writer, Store,
                staged =>
                {
                    var manifest = ManifestWithClosure(staged);
                    return (manifest, IdentityFor(manifest));
                },
                1_000_000_000, deathBarrier);

        public void Dispose()
        {
            Writer.Dispose();
            Scope.Dispose();
            var path = System.IO.Path.Combine(DelegatedParent!, ChildName);
            try { File.WriteAllText(System.IO.Path.Combine(path, "cgroup.kill"), "1"); } catch { }
            try { Directory.Delete(path); } catch { }
            JournalDir.Dispose();
            Staging.Dispose();
            Source.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private static Action<string> DieAt(string target) => barrier =>
    {
        if (barrier == target)
        {
            throw new BarrierDeathException(barrier);
        }
    };

    [Fact]
    public void NonRootOwnerDeathBeforeRegistrationRecoversIntegrated()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x20, 1024), null, null);
        var sibling = System.IO.Path.Combine(DelegatedParent!, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        try
        {
            Assert.Throws<BarrierDeathException>(() =>
                fx.Scope.AllocatePreparation(fx.Request(DieAt("after-create")), CancellationToken.None));
            Assert.Equal(PreparationStage.AllocationIntent, fx.Journal.ReadLatest().Snapshot!.Stage);
            var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
            Assert.True(recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new RealClock()).Stopped);
            Assert.True(recovery.RemoveIfIdentityMatches().Removed);
            Assert.False(Directory.Exists(System.IO.Path.Combine(DelegatedParent!, fx.ChildName)));
            Assert.True(Directory.Exists(sibling));
        }
        finally
        {
            try { Directory.Delete(sibling); } catch { }
        }
    }

    [Fact]
    public void NonRootMidStagingDeathRecoversAccountedObject()
    {
        SkipUnlessQualified();
        var tool = Bytes(0x40, 1000);
        var input = Bytes(0x41, 200000);
        var chunks = 0;
        var hooks = new CopyTestHooks(
            _ =>
            {
                if (++chunks == 3)
                {
                    throw new BarrierDeathException("mid-copy");
                }
            },
            null);
        using var fx = new IntegratedFixture(tool, input, hooks);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(null), CancellationToken.None));

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        var toolEntry = Assert.Single(read.Snapshot!.Allocations, a => a.Kind == AllocationKind.Staging && a.Name == "tool-000.staged");
        Assert.True(toolEntry.Confirmed);
        var inputEntry = Assert.Single(read.Snapshot.Allocations, a => a.Kind == AllocationKind.Staging && a.Name == "input-001.staged");
        Assert.False(inputEntry.Confirmed);
        Assert.True(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged")));

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
        Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
        var cleaner = new QualScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged")));
        Assert.True(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "input-001.staged")));
        PreparationInterop.CloseFd(subdirFd);
        PreparationInterop.CloseFd(parentFd);
    }

    [Fact]
    public void NonRootPreparedBeforeMintDoesNotResurrectCapability()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x60, 2048), null, null);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-prepared")), CancellationToken.None));

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.Prepared, read.Snapshot!.Stage);
        Assert.NotNull(read.Snapshot.Preparation);
        // The durable Prepared record names a preparation that was never
        // minted: the store has no capability to resurrect.
        Assert.False(fx.Store.TryLookup(read.Snapshot.Preparation!.PreparationId, out _));

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
        Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
        var cleaner = new QualScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        PreparationInterop.CloseFd(subdirFd);
        PreparationInterop.CloseFd(parentFd);
    }

    // ---- Staging reconciliation truthfulness ----

    [Fact]
    public void NonRootCompleteCleanupIsConfirmed()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x50, 2048), Bytes(0x51, 4096), null);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-copy")), CancellationToken.None));
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.NotEqual(PreparationStage.Prepared, read.Snapshot!.Stage);

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
        Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
        var cleaner = new QualScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        PreparationInterop.CloseFd(subdirFd);
        PreparationInterop.CloseFd(parentFd);
    }

    [Fact]
    public void NonRootReplacementStagingObjectSurvives()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x52, 2048), null, null);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-copy")), CancellationToken.None));
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);

        // Replace the staged object with a distinct file at the same name.
        var stagedPath = System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged");
        File.Delete(stagedPath);
        File.WriteAllBytes(stagedPath, Bytes(0x77, 64));

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
        Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
        var cleaner = new QualScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.True(File.Exists(stagedPath), "replacement must be preserved");
        PreparationInterop.CloseFd(subdirFd);
        PreparationInterop.CloseFd(parentFd);
    }

    [Fact]
    public void NonRootUnrelatedSiblingUntouched()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x53, 2048), null, null);
        var sibling = System.IO.Path.Combine(DelegatedParent!, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        try
        {
            Assert.Throws<BarrierDeathException>(() =>
                fx.Scope.AllocatePreparation(fx.Request(DieAt("after-copy")), CancellationToken.None));
            var read = fx.Journal.ReadLatest();
            var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
            Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
            Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
            var cleaner = new QualScopeCleaner(parentFd, subdirFd);
            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.True(Directory.Exists(sibling));
            PreparationInterop.CloseFd(subdirFd);
            PreparationInterop.CloseFd(parentFd);
        }
        finally
        {
            try { Directory.Delete(sibling); } catch { }
        }
    }
}
