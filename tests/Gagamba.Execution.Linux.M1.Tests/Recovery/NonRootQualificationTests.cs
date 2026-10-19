// R6D.2 non-root owner-owned transaction qualification on operator-delegated cgroup v2.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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
[Trait("Category", "ProcessGlobalReaping")]
public sealed class NonRootQualificationTests
{
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

    private static void AssertLiveOwnerAtBarrier(
        M1RecoveryScope scope, OwnerPreparationOutcome outcome, string expectedBarrier, OwnershipJournal journal)
    {
        Assert.Equal(expectedBarrier, outcome.Barrier);
        var owner = outcome.Owner ?? throw new InvalidOperationException("barrier acknowledgement had no owner process");
        Assert.NotEqual(Environment.ProcessId, owner.Id);
        Assert.True(scope.TryOwnerPidFdExited(owner, 0, out var exited, out var pidfdError), pidfdError);
        Assert.False(exited, "retained pidfd already reports owner exit at the barrier");
        owner.Refresh();
        Assert.False(owner.HasExited, "direct-child owner must still be alive immediately before kill");
        var competitor = new OwnershipJournal(new JournalFileStorage(), GetJournalDirectory(journal));
        Assert.False(competitor.TryAcquireWriter(out _, out _), "live owner must continue to hold the journal fence");
    }

    private static string GetJournalDirectory(OwnershipJournal journal)
    {
        var field = typeof(OwnershipJournal).GetField("_directory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        return (string)field!.GetValue(journal)!;
    }

    private static string Boot() => CgroupInterop.TryReadBootId(out var id, out _) ? id : "unknown";

    private static ulong ReadNamespaceInode(string name)
    {
        var target = new FileInfo($"/proc/self/ns/{name}").LinkTarget;
        Assert.NotNull(target);
        var open = target!.LastIndexOf('[');
        Assert.True(open > 0);
        Assert.True(ulong.TryParse(target.AsSpan(open + 1, target.Length - open - 2), out var inode));
        return inode;
    }

    private static void AssertScratchMountsAreDistinctChildrenOfSandboxRoot(string mountInfo)
    {
        var records = mountInfo.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line =>
            {
                var separator = line.IndexOf(" - ", StringComparison.Ordinal);
                Assert.True(separator > 0);
                var fields = line[..separator].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Assert.True(fields.Length >= 6);
                return (Id: fields[0], Parent: fields[1], Device: fields[2], Point: fields[4]);
            }).ToDictionary(record => record.Point, StringComparer.Ordinal);

        var root = records["/"].Id;
        Assert.Equal(root, records["/tmp"].Parent);
        Assert.Equal(root, records["/dev/shm"].Parent);
        Assert.NotEqual(records["/tmp"].Id, records["/dev/shm"].Id);
        Assert.NotEqual(records["/tmp"].Device, records["/dev/shm"].Device);
    }

    private sealed class FailingPidFdProvider : IPidFdProvider, IDisposable
    {
        private readonly ManualResetEventSlim _attempted = new(false);
        private readonly ManualResetEventSlim _release = new(false);

        internal Process? OwnerProcess { get; private set; }

        internal bool WaitForAttempt(TimeSpan timeout) => _attempted.Wait(timeout);

        internal void Release() => _release.Set();

        public bool TryOpen(int pid, out int fd, out string error)
        {
            fd = -1;
            try
            {
                OwnerProcess = Process.GetProcessById(pid);
            }
            catch (Exception ex)
            {
                error = $"could not retain owner process observation: {ex.GetType().Name}";
                _attempted.Set();
                return false;
            }

            _attempted.Set();
            _release.Wait(TimeSpan.FromSeconds(15));
            error = "injected pidfd failure";
            return false;
        }

        public void Dispose()
        {
            Release();
            OwnerProcess?.Dispose();
            _attempted.Dispose();
            _release.Dispose();
        }
    }

    private sealed class RecordingPidFdProvider : IPidFdProvider, IDisposable
    {
        internal Process? OwnerProcess { get; private set; }

        public bool TryOpen(int pid, out int fd, out string error)
        {
            OwnerProcess = Process.GetProcessById(pid);
            return PidFd.TryOpen(pid, out fd, out error);
        }

        public void Dispose()
        {
            OwnerProcess?.Dispose();
            OwnerProcess = null;
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

    private sealed class RegisteredDeathCleaner : IAllocationCleaner
    {
        private readonly int _parentFd;
        private readonly string _stagingDirectoryName;

        public RegisteredDeathCleaner(int parentFd, string stagingDirectoryName = "prep-001")
        {
            _parentFd = parentFd;
            _stagingDirectoryName = stagingDirectoryName;
        }

        public AllocationCleanResult CleanOne(AllocationEntry entry)
        {
            if (entry.Kind == AllocationKind.StagingDirectory)
                return ResolveDirectoryBeneathParent(entry);
            if (entry.Kind != AllocationKind.Staging)
                return new AllocationCleanResult(AllocationCleanOutcome.Failed, $"unexpected entry kind {entry.Kind}");

            var (directory, early) = OpenStagingDirectory();
            if (early.HasValue)
            {
                directory?.Dispose();
                return early.Value;
            }

            using (directory!)
            {
                var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, directory!, entry.Name, entry.Device, entry.Inode);
                return result.Outcome switch
                {
                    CleanupItemOutcome.Removed => new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty),
                    CleanupItemOutcome.AlreadyAbsent => new AllocationCleanResult(AllocationCleanOutcome.AlreadyAbsent, string.Empty),
                    CleanupItemOutcome.IdentityMismatch => new AllocationCleanResult(AllocationCleanOutcome.Mismatch, result.Error),
                    _ => new AllocationCleanResult(AllocationCleanOutcome.Failed, result.Error),
                };
            }
        }

        private (SafeFileHandle? Handle, AllocationCleanResult? Early) OpenStagingDirectory()
        {
            if (_stagingDirectoryName.Length == 0 || _stagingDirectoryName.Contains('/'))
                return (null, new AllocationCleanResult(AllocationCleanOutcome.Failed, "staging directory name is not a single component"));
            var errno = PreparationInterop.OpenRelative(
                _parentFd, _stagingDirectoryName,
                PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
                PreparationInterop.RequiredResolve, out var directoryFd);
            if (errno == PreparationInterop.ENOENT)
                return (null, new AllocationCleanResult(AllocationCleanOutcome.AlreadyAbsent, string.Empty));
            if (errno != 0 || directoryFd < 0)
                return (null, new AllocationCleanResult(AllocationCleanOutcome.Failed, $"staging inspection errno {errno}"));
            return (new SafeFileHandle(new IntPtr(directoryFd), ownsHandle: true), null);
        }

        private AllocationCleanResult ResolveDirectoryBeneathParent(AllocationEntry entry)
        {
            var (directory, early) = OpenStagingDirectory();
            if (early.HasValue)
            {
                directory?.Dispose();
                return early.Value;
            }

            using (directory!)
            {
                if (PreparationInterop.StatFd(directory!.DangerousGetHandle().ToInt32(), out var current) != 0
                    || current.Device != entry.Device || current.Inode != entry.Inode)
                    return new AllocationCleanResult(AllocationCleanOutcome.Mismatch, "staging directory identity mismatch");
                var errno = PreparationInterop.RemoveDirectory(_parentFd, entry.Name);
                return errno == 0
                    ? new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty)
                    : new AllocationCleanResult(AllocationCleanOutcome.Failed, $"rmdir errno {errno}");
            }
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

        using var scope = new M1RecoveryScope();
        Assert.True(scope.TryInitialize().Ok);
        Assert.True(scope.TryOpenDelegatedParent(DelegatedParent!).Ok);

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

    // ---- Non-root owner-owned R6D.2 path ----

    private sealed class IntegratedFixture : IDisposable
    {
        public TempDir Source = new();
        public TempDir Staging = new();
        public TempDir JournalDir = new();
        public M1RecoveryScope Scope = null!;
        public OwnershipJournal Journal;
        public JournalWriterLease Writer;
        public Guid Exec = Guid.NewGuid();
        public Guid LeaseGen = Guid.NewGuid();
        public string ChildName;

        public IntegratedFixture(byte[] tool, byte[]? input, CopyTestHooks? hooks,
            IPidFdProvider? pidFdProvider = null, byte[]? coordinator = null, byte[]? filter = null)
        {
            ChildName = "d-" + Guid.NewGuid().ToString("N");
            Scope = new M1RecoveryScope(pidFdProvider);
            Assert.True(Scope.TryInitialize().Ok);
            Assert.True(Scope.TryOpenDelegatedParent(DelegatedParent!).Ok);
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "tool"), tool);
            var objects = new List<CopyObjectPlan> { new("tool", "tool", 4UL * 1024 * 1024, null) };
            if (input is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "input"), input);
                objects.Add(new("input", "input", 4UL * 1024 * 1024, null));
            }
            if (coordinator is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "coordinator"), coordinator);
                objects.Add(new("coordinator", "coordinator", 4UL * 1024 * 1024, null));
            }
            if (filter is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "filter"), filter);
                objects.Add(new("filter", "filter", 4UL * 1024 * 1024, null));
            }

            Journal = new OwnershipJournal(new JournalFileStorage(), JournalDir.Path);
            Assert.True(Journal.TryAcquireWriter(out var writer, out var error), error);
            Writer = writer!;
            Plan = new CopyPlan(
                Source.Path, Staging.Path, "prep-001", objects, 16UL * 1024 * 1024, 128,
                Array.Empty<KeyValuePair<string, string>>(), _ => true, hooks);
        }

        public CopyPlan Plan { get; }

        public OwnerPreparationRequest Request(string? testBarrier, bool enableHeldCoordinator = false) =>
            new(
                RecoveryHarness.LocateOwnerDll() ?? throw new InvalidOperationException("owner executable missing from the test build"),
                ChildName, Exec, LeaseGen, Boot(), Plan, Journal, Writer, Issuer, Generation,
                staged =>
                {
                    var baseline = Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests.SampleManifest();
                    var closureObjects = staged.Select(item => new ClosureObject(
                        item.Role, item.Sha256, item.Length,
                        item.Role == "filter" ? "fd:6" : "/" + item.Role,
                        item.Role == "filter" ? "p07-seccomp-filter" : "static-test-object"));
                    var manifest = new RestrictedManifest(
                        RestrictedManifest.CurrentSchemaVersion,
                        RestrictedManifest.ImplementationIdentifier,
                        baseline.Authority,
                        baseline.Resources,
                        new ClosureIdentity(closureObjects, enableHeldCoordinator
                            ? new[] { "bubblewrap-0.11.1:" + Gagamba.Execution.Linux.M1.Launch.HelperInstallation.PinnedSha256 }
                            : Array.Empty<string>()),
                        baseline.Invocation,
                        baseline.Channels);
                    return (manifest, IdentityFor(manifest, Exec));
                },
                MonotonicNanoseconds() + 60_000_000_000L, testBarrier,
                EnableHeldCoordinator: enableHeldCoordinator);

        private static long MonotonicNanoseconds()
        {
            var ticks = Stopwatch.GetTimestamp();
            return ticks / Stopwatch.Frequency * 1_000_000_000L
                + ticks % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency;
        }

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

    [Fact]
    public void NonRootWp04HeldSetupReportsHeldWithoutTargetInitializationThenAbortsAndReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var coordinator = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/exec-coordinator"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            coordinator: coordinator, filter: filter);

        var outcome = fx.Scope.StartOwnerPreparation(fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        Assert.True(outcome.Ok, outcome.Error);
        Assert.True(outcome.Held);
        Assert.False(outcome.Completed, "WP04 must remain held and must not complete target execution");
        Assert.True(outcome.NoTargetInitializationMarkerAbsent);
        Assert.False(TargetExecPresentInDomain(fx), "target must not be present when HELD is accepted");
        Assert.NotNull(outcome.HeldReport);
        Assert.Equal(0u, outcome.HeldReport!.Uid);
        Assert.Equal(0UL, outcome.HeldReport.CapEff);
        Assert.NotEqual(ReadNamespaceInode("mnt"), outcome.HeldReport.NamespaceInodes[1]);
        AssertScratchMountsAreDistinctChildrenOfSandboxRoot(outcome.HeldReport.MountInfo);

        var owner = outcome.Owner!;
        Assert.NotEqual(Environment.ProcessId, owner.Id);
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var ownerExited, out var pidfdError), pidfdError);
        Assert.False(ownerExited, "the owner must remain alive while Bubblewrap/coordinator are HELD");
        owner.Refresh();
        Assert.False(owner.HasExited);
        var competitor = new OwnershipJournal(new JournalFileStorage(), GetJournalDirectory(fx.Journal));
        Assert.False(competitor.TryAcquireWriter(out _, out _), "the live owner must retain the journal writer while HELD");
        var retainedDomain = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.True(retainedDomain.AuthorityAlive);
        Assert.True(retainedDomain.TryReadWp04Limits(out var resourceLimits, out var limitsError), limitsError);
        Assert.Equal("134217728", resourceLimits["memory.max"]);
        Assert.Equal("0", resourceLimits["memory.swap.max"]);
        Assert.Equal("48", resourceLimits["pids.max"]);
        Assert.Single(fx.Scope.RetainedStagingDirectories);

        var aborted = fx.Scope.AbortHeld(outcome);
        Assert.True(aborted.Accepted, aborted.Error);
        Assert.True(aborted.OwnerExited, aborted.Error);
        Assert.True(aborted.CleanupRequested, aborted.Error);
        Assert.True(aborted.NoTargetInitializationMarkerAbsent,
            "the adapter's independent pre-reconciliation capture must show the target constructor marker absent after abort");
        Assert.False(TargetExecPresentInDomain(fx), "target must remain absent immediately after abort");

        var recoveryLease = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var parent = Assert.IsType<SafeFileHandle>(Assert.Single(fx.Scope.RetainedStagingParents));
        var staging = Assert.IsType<SafeFileHandle>(Assert.Single(fx.Scope.RetainedStagingDirectories));
        var cleaner = new QualScopeCleaner(parent.DangerousGetHandle().ToInt32(), staging.DangerousGetHandle().ToInt32());
        Assert.True(fx.Journal.TryAcquireWriter(out var recoveryWriter, out var recoveryError), recoveryError);
        using (recoveryWriter!)
        {
            var record = fx.Journal.ReadLatest().Snapshot!;
            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            var first = reconciler.Reconcile(record, Boot(), recoveryLease, cleaner, fx.Journal, recoveryWriter);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, first.Outcome);
            Assert.True(first.DestructiveActionTaken);
            var second = reconciler.Reconcile(fx.Journal.ReadLatest().Snapshot!, Boot(), recoveryLease, cleaner, fx.Journal, recoveryWriter);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, second.Outcome);
        }
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        Assert.False(Directory.Exists(System.IO.Path.Combine(DelegatedParent!, fx.ChildName)));
        Assert.False(TargetExecPresentInDomain(fx), "target must remain absent after final reconciliation");
        var tracePath = Environment.GetEnvironmentVariable("WP04_TRACE_LOG");
        if (!string.IsNullOrWhiteSpace(tracePath))
            AssertIndependentTraceMarkerAbsent(tracePath, owner.Id, requireCompleteDrains: true);
        AssertFinalCleanupTargetMarkerAbsentIfTraced();
    }

    private static void ReconcileAfterOwnerDeath(IntegratedFixture fx)
    {
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.Prepared, read.Snapshot!.Stage);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.True(recovery.AuthorityAlive);
        var parent = Assert.IsType<SafeFileHandle>(Assert.Single(fx.Scope.RetainedStagingParents));
        var staging = Assert.IsType<SafeFileHandle>(Assert.Single(fx.Scope.RetainedStagingDirectories));
        var cleaner = new QualScopeCleaner(parent.DangerousGetHandle().ToInt32(), staging.DangerousGetHandle().ToInt32());
        Assert.True(fx.Journal.TryAcquireWriter(out var recoveryWriter, out var writerError), writerError);
        using (recoveryWriter!)
        {
            var result = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5)).Reconcile(
                read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, recoveryWriter);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.True(result.DestructiveActionTaken);
        }

        Assert.True(fx.Scope.ReapAdoptedChildren(TimeSpan.FromSeconds(5), out _, out var reapError), reapError);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        Assert.False(Directory.Exists(System.IO.Path.Combine(DelegatedParent!, fx.ChildName)));
        Assert.Equal(OwnershipCleanupState.Confirmed, fx.Journal.ReadLatest().Snapshot!.Cleanup);

        AssertFinalCleanupTargetMarkerAbsentIfTraced();
    }

    private static byte[] BuildTestCoordinator(int mode)
        => BuildTestCoordinator(mode, null);

    private static byte[] BuildTestCoordinator(int mode, ulong? hostMntNamespaceInode)
    {
        using var temp = new TempDir();
        var root = RepositoryRoot();
        var source = System.IO.Path.Combine(root, "tests/Fixtures/wp04-qualification-coordinator.c");
        var output = System.IO.Path.Combine(temp.Path, "wp04-coordinator-" + mode);
        var start = new ProcessStartInfo("/usr/bin/cc")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        var arguments = new List<string> { "-static", "-O2", "-std=c11", "-DWP04_MODE=" + mode };
        if (hostMntNamespaceInode is { } hostMnt)
            arguments.Add("-DWP04_HOST_MNT_INODE=" + hostMnt.ToString(System.Globalization.CultureInfo.InvariantCulture));
        arguments.AddRange(new[] { source, "-o", output });
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var compiler = Process.Start(start) ?? throw new InvalidOperationException("test coordinator compiler did not start");
        var stdout = compiler.StandardOutput.ReadToEnd();
        var stderr = compiler.StandardError.ReadToEnd();
        Assert.True(compiler.WaitForExit(30_000), "test coordinator compile exceeded its bound");
        Assert.Equal(0, compiler.ExitCode);
        Assert.True(File.Exists(output), $"test coordinator artifact missing; stdout={stdout}; stderr={stderr}");
        return File.ReadAllBytes(output);
    }

    private static int WaitForCoordinatorInDomain(IntegratedFixture fx, TimeSpan timeout)
    {
        var path = System.IO.Path.Combine(DelegatedParent!, fx.ChildName);
        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        while (Stopwatch.GetTimestamp() < deadline)
        {
            try
            {
                foreach (var item in File.ReadAllLines(System.IO.Path.Combine(path, "cgroup.procs")))
                {
                    if (!int.TryParse(item, out var pid)) continue;
                    var command = Encoding.UTF8.GetString(File.ReadAllBytes($"/proc/{pid}/cmdline"));
                    var comm = File.ReadAllText($"/proc/{pid}/comm").Trim();
                    if (command.Contains("/coordinator", StringComparison.Ordinal)
                        || comm == "exec-coordinator") return pid;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            Thread.Yield();
        }
        return -1;
    }

    private static ulong ParseProcSyscallWord(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return ulong.Parse(value.AsSpan(2), System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture);
        return ulong.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool TryReadStoppedRead(Process process, int expectedLength, out int fd)
    {
        fd = -1;
        try
        {
            var state = File.ReadLines($"/proc/{process.Id}/status")
                .First(line => line.StartsWith("State:", StringComparison.Ordinal))[6..].TrimStart();
            if (state.Length == 0 || (state[0] != 't' && state[0] != 'T')) return false;
            var fields = File.ReadAllText($"/proc/{process.Id}/syscall")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 4 || ParseProcSyscallWord(fields[0]) != 0
                || ParseProcSyscallWord(fields[3]) != (ulong)expectedLength)
                return false;
            fd = checked((int)ParseProcSyscallWord(fields[1]));
            return fd >= 0;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
        catch (OverflowException) { return false; }
    }

    private static int WaitForInjectedRead(Process owner, int expectedLength, TimeSpan timeout)
    {
        var deadline = Stopwatch.GetTimestamp() + checked((long)(timeout.TotalSeconds * Stopwatch.Frequency));
        while (Stopwatch.GetTimestamp() < deadline)
        {
            if (TryReadStoppedRead(owner, expectedLength, out var fd)) return fd;
            Thread.Yield();
        }
        throw new InvalidOperationException(
            $"strace did not hold owner PID {owner.Id} at read(..., {expectedLength}) before timeout");
    }

    private static void AssertCoordinatorHeldWriteIsBufferedBeforeRead(int ownerPid, int readFd, string tracePath)
    {
        Assert.True(File.Exists(tracePath), $"strace artifact is missing: {tracePath}");
        var lines = File.ReadAllLines(tracePath);
        var heldWrite = lines.FirstOrDefault(line => line.Contains("write(5", StringComparison.Ordinal) && line.Contains("GGM1CTL\\0", StringComparison.Ordinal));
        Assert.True(heldWrite is not null,
            "strace must record the coordinator's complete HELD frame write before owner death");
        var resultMarker = heldWrite!.LastIndexOf(" = ", StringComparison.Ordinal);
        var callEnd = heldWrite!.LastIndexOf(") = ", StringComparison.Ordinal);
        var argumentEnd = heldWrite.LastIndexOf(", ", callEnd, StringComparison.Ordinal);
        var coordinatorPidEnd = heldWrite.IndexOf(' ');
        var coordinatorPid = 0;
        var hasCoordinatorPid = coordinatorPidEnd > 0
            && int.TryParse(heldWrite.AsSpan(0, coordinatorPidEnd), out coordinatorPid);
        Assert.True(hasCoordinatorPid, $"strace HELD writer PID is missing: {heldWrite}");
        Assert.True(resultMarker > 0 && argumentEnd > 0
            && int.TryParse(heldWrite.AsSpan(resultMarker + 3).Trim(), out var bytesWritten)
            && int.TryParse(heldWrite.AsSpan(argumentEnd + 2, callEnd - argumentEnd - 2), out var requestedBytes)
            && bytesWritten >= 96 && bytesWritten == requestedBytes,
            $"coordinator HELD frame write was not complete: {heldWrite}");
        Assert.Contains("GGM1CTL\\0\\1\\0\\1\\0", heldWrite, StringComparison.Ordinal);
        var ownerRead = lines.FirstOrDefault(line => line.StartsWith(ownerPid + " ", StringComparison.Ordinal)
            && (line.Contains($"read({readFd},", StringComparison.Ordinal) || line.Contains($"read({readFd}<", StringComparison.Ordinal))
            && line.Contains(", 96)", StringComparison.Ordinal));
        Assert.True(ownerRead is null,
            "the owner header read must still be stopped at syscall entry; no completed read may have consumed the HELD header");
        var fdTarget = new FileInfo($"/proc/{ownerPid}/fd/{readFd}").LinkTarget;
        Assert.StartsWith("pipe:[", fdTarget, StringComparison.Ordinal);
        Assert.Contains($"write(5<{fdTarget}>", heldWrite, StringComparison.Ordinal);
    }

    private static void AssertIndependentTraceMarkerAbsent(string tracePath, int ownerPid, bool requireCompleteDrains)
    {
        const string marker = "gagamba-m1-fixture: ctor-marker";
        Assert.True(File.Exists(tracePath), $"strace artifact is missing: {tracePath}");
        var lines = File.ReadAllLines(tracePath);
        Assert.DoesNotContain(marker, string.Join("\n", lines));
        if (!requireCompleteDrains) return;

        var eofPipes = lines.Where(line => line.StartsWith(ownerPid + " ", StringComparison.Ordinal)
                && line.Contains("read(", StringComparison.Ordinal)
                && line.Contains(", \"\", 4096) = 0", StringComparison.Ordinal))
            .Select(line =>
            {
                var start = line.IndexOf("read(", StringComparison.Ordinal) + 5;
                var end = line.IndexOf(',', start);
                return start >= 5 && end > start ? line[start..end] : string.Empty;
            })
            .Where(fd => fd.Length != 0)
            .Distinct(StringComparer.Ordinal)
            .Count();
        Assert.True(eofPipes >= 4,
            $"after cleanup, expected independent stdout/stderr/helper/coordinator EOF reads; observed {eofPipes}");
    }

    private static void AssertFinalCleanupTargetMarkerAbsentIfTraced()
    {
        var tracePath = Environment.GetEnvironmentVariable("WP04_FINAL_TRACE_LOG");
        if (!string.IsNullOrWhiteSpace(tracePath))
            AssertIndependentTraceMarkerAbsent(tracePath, ownerPid: -1, requireCompleteDrains: false);
    }
    private static bool TargetExecPresentInDomain(IntegratedFixture fx)
    {
        var path = System.IO.Path.Combine(DelegatedParent!, fx.ChildName);
        try
        {
            foreach (var item in File.ReadAllLines(System.IO.Path.Combine(path, "cgroup.procs")))
            {
                if (!int.TryParse(item, out var pid)) continue;
                var command = Encoding.UTF8.GetString(File.ReadAllBytes($"/proc/{pid}/cmdline"));
                if (command.Split('\0', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() == "/tool")
                    return true;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return false;
    }

    private static Process WaitForRecordedOwner(RecordingPidFdProvider provider, TimeSpan timeout)
    {
        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        while (Stopwatch.GetTimestamp() < deadline)
        {
            if (provider.OwnerProcess is { } owner) return owner;
            Thread.Yield();
        }
        throw new InvalidOperationException("direct owner process was not recorded before timeout");
    }

    [Fact]
    public async Task NonRootWp04RealOwnerLossDuringCoordinatorStartupBeforeHeldReconciles()
    {
        SkipUnlessQualified();
        var coordinator = BuildTestCoordinator(0);
        using var ownerObservation = new RecordingPidFdProvider();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            pidFdProvider: ownerObservation, coordinator: coordinator, filter: filter);
        var preparation = Task.Run(() => fx.Scope.StartOwnerPreparation(
            fx.Request(null, enableHeldCoordinator: true), CancellationToken.None));

        var coordinatorPid = WaitForCoordinatorInDomain(fx, TimeSpan.FromSeconds(20));
        Assert.True(coordinatorPid > 0, "test coordinator did not reach the real Bubblewrap execution domain");
        var owner = WaitForRecordedOwner(ownerObservation, TimeSpan.FromSeconds(10));
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var exited, out var pidfdError), pidfdError);
        Assert.False(exited);
        owner.Refresh();
        Assert.False(owner.HasExited);
        Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
        var domainPath = System.IO.Path.Combine(DelegatedParent!, fx.ChildName);
        Assert.Contains("populated 1", File.ReadAllText(System.IO.Path.Combine(domainPath, "cgroup.events")), StringComparison.Ordinal);
        Assert.False(TargetExecPresentInDomain(fx), "target executable was not initialized before owner death");
        owner.Kill(entireProcessTree: false);

        var outcome = await preparation.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
        Assert.False(outcome.Held);
        Assert.False(outcome.Completed);
        Assert.DoesNotContain("target constructor marker appeared", outcome.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
        ReconcileAfterOwnerDeath(fx);
    }

    [Fact]
    public async Task NonRootWp04RealOwnerLossAfterCoordinatorHeldFrameBeforeManagedAcceptanceReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        var tracePath = Environment.GetEnvironmentVariable("WP04_TRACE_LOG");
        if (string.IsNullOrWhiteSpace(tracePath))
        {
            using var ordinary = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
                coordinator: BuildTestCoordinator(11), filter: filter);
            var accepted = ordinary.Scope.StartOwnerPreparation(
                ordinary.Request(null, enableHeldCoordinator: true), CancellationToken.None);
            Assert.True(accepted.Ok, accepted.Error);
            Assert.True(accepted.Held, accepted.Error);
            Assert.False(accepted.Completed);
            var ordinaryAbort = ordinary.Scope.AbortHeld(accepted);
            Assert.True(ordinaryAbort.Accepted, ordinaryAbort.Error);
            Assert.True(ordinaryAbort.OwnerExited, ordinaryAbort.Error);
            ReconcileAfterOwnerDeath(ordinary);
            Assert.False(TargetExecPresentInDomain(ordinary));
            return;
        }

        using var ownerObservation = new RecordingPidFdProvider();
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            pidFdProvider: ownerObservation, coordinator: BuildTestCoordinator(11), filter: filter);

        Task<OwnerPreparationOutcome>? preparation = null;
        Process? owner = null;
        try
        {
            preparation = Task.Run(() => fx.Scope.StartOwnerPreparation(
                fx.Request(null, enableHeldCoordinator: true), CancellationToken.None));
            owner = WaitForRecordedOwner(ownerObservation, TimeSpan.FromSeconds(10));
            var readFd = WaitForInjectedRead(owner, 96, TimeSpan.FromSeconds(15));
            Assert.False(preparation.IsCompleted,
                "the managed owner/scope path must remain before HELD acceptance while the complete frame is buffered");
            AssertCoordinatorHeldWriteIsBufferedBeforeRead(owner.Id, readFd, tracePath);
            owner = ownerObservation.OwnerProcess!;
            Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var exited, out var pidfdError), pidfdError);
            Assert.False(exited, "owner must be live at the pre-accept kill barrier");
            owner.Refresh();
            Assert.False(owner.HasExited);
            Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
            Assert.True(Assert.Single(fx.Scope.RetainedRecoveryLeases).AuthorityAlive);
            Assert.Single(fx.Scope.RetainedStagingDirectories);
            var domainPath = System.IO.Path.Combine(DelegatedParent!, fx.ChildName);
            Assert.Contains("populated 1", File.ReadAllText(System.IO.Path.Combine(domainPath, "cgroup.events")), StringComparison.Ordinal);
            Assert.True(WaitForCoordinatorInDomain(fx, TimeSpan.FromSeconds(5)) > 0,
                "the coordinator must have emitted its complete HELD report from the real Bubblewrap domain");
            Assert.False(TargetExecPresentInDomain(fx), "target must be absent before managed HELD acceptance");

            // strace's syscall-entry delay holds the real owner before the
            // adapter consumes its complete, buffered 96-byte HELD header.
            // This is a real owner-process loss, not a thrown error or
            // cooperative exit.
            owner.Kill(entireProcessTree: false);
            var outcome = await preparation.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            Assert.False(outcome.Ok);
            Assert.False(outcome.Held);
            Assert.False(outcome.Completed);
            Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
            Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out exited, out pidfdError), pidfdError);
            Assert.True(exited, "the scope must observe the exact owner dead through its retained pidfd");
            Assert.False(TargetExecPresentInDomain(fx), "target remains absent after pre-accept owner death");
            ReconcileAfterOwnerDeath(fx);
            Assert.False(TargetExecPresentInDomain(fx), "target remains absent after final pre-accept reconciliation");
            AssertIndependentTraceMarkerAbsent(tracePath, owner.Id, requireCompleteDrains: false);
        }
        finally
        {
            if (owner is not null)
            {
                try
                {
                    owner.Refresh();
                    if (!owner.HasExited) owner.Kill(entireProcessTree: false);
                }
                catch (InvalidOperationException) { }
            }
            if (preparation is { IsCompleted: false })
            {
                try { await preparation.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken); }
                catch (TimeoutException) { }
            }
        }
    }

    [Fact]
    public async Task NonRootWp04RealOwnerLossDuringAbortDrainReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        var tracePath = Environment.GetEnvironmentVariable("WP04_TRACE_LOG");
        if (string.IsNullOrWhiteSpace(tracePath))
        {
            using var ordinary = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
                coordinator: BuildTestCoordinator(11), filter: filter);
            var accepted = ordinary.Scope.StartOwnerPreparation(
                ordinary.Request(null, enableHeldCoordinator: true), CancellationToken.None);
            Assert.True(accepted.Ok, accepted.Error);
            Assert.True(accepted.Held, accepted.Error);
            var ordinaryAbort = ordinary.Scope.AbortHeld(accepted);
            Assert.True(ordinaryAbort.Accepted, ordinaryAbort.Error);
            Assert.True(ordinaryAbort.OwnerExited, ordinaryAbort.Error);
            ReconcileAfterOwnerDeath(ordinary);
            Assert.False(TargetExecPresentInDomain(ordinary));
            return;
        }

        using var ownerObservation = new RecordingPidFdProvider();
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            pidFdProvider: ownerObservation, coordinator: BuildTestCoordinator(11), filter: filter);

        var held = fx.Scope.StartOwnerPreparation(fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        Assert.True(held.Ok, held.Error);
        Assert.True(held.Held, held.Error);
        Assert.False(held.Completed);
        Assert.True(held.NoTargetInitializationMarkerAbsent);
        Assert.False(TargetExecPresentInDomain(fx), "target must be absent at accepted HELD");
        var owner = held.Owner!;
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var exited, out var pidfdError), pidfdError);
        Assert.False(exited, "accepted HELD owner must be alive before abort begins");
        owner.Refresh();
        Assert.False(owner.HasExited);
        Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);

        var abort = Task.Run(() => fx.Scope.AbortHeld(held));
        try
        {
            var readFd = WaitForInjectedRead(owner, 4096, TimeSpan.FromSeconds(15));
            Assert.False(abort.IsCompleted,
                "abort must remain in its final drain before the owner reports HeldAborted");
            Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out exited, out pidfdError), pidfdError);
            Assert.False(exited, "owner must still be live at the abort-in-progress kill barrier");
            owner.Refresh();
            Assert.False(owner.HasExited);
            Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
            var domainPath = System.IO.Path.Combine(DelegatedParent!, fx.ChildName);
            Assert.Contains("populated 0", File.ReadAllText(System.IO.Path.Combine(domainPath, "cgroup.events")), StringComparison.Ordinal);
            Assert.Empty(File.ReadAllLines(System.IO.Path.Combine(domainPath, "cgroup.procs")));
            Assert.False(TargetExecPresentInDomain(fx), "target must remain absent after abort stopped and reaped the domain");
            var fdTarget = new FileInfo($"/proc/{owner.Id}/fd/{readFd}").LinkTarget;
            Assert.StartsWith("pipe:[", fdTarget, StringComparison.Ordinal);

            // This kill occurs after the retained domain is empty and wrapper
            // reaping has completed, while the owner is still draining abort
            // outputs and before it can report HeldAborted.
            owner.Kill(entireProcessTree: false);
            var abortOutcome = await abort.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(abortOutcome.Accepted, abortOutcome.Error);
            Assert.False(abortOutcome.OwnerExited);
            Assert.False(abortOutcome.CleanupRequested);
            Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out exited, out pidfdError), pidfdError);
            Assert.True(exited, "scope pidfd must report the killed aborting owner dead");
            var ownerExit = fx.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(10));
            Assert.True(ownerExit.Exited, ownerExit.Error);
            Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
            Assert.True(Assert.Single(fx.Scope.RetainedRecoveryLeases).AuthorityAlive,
                "recovery authority must survive owner death during abort drain");

            ReconcileAfterOwnerDeath(fx);
            Assert.False(TargetExecPresentInDomain(fx), "target remains absent after abort-loss reconciliation");
            AssertIndependentTraceMarkerAbsent(tracePath, owner.Id, requireCompleteDrains: false);
        }
        finally
        {
            if (!abort.IsCompleted)
            {
                try { owner.Kill(entireProcessTree: false); }
                catch (InvalidOperationException) { }
                try { await abort.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken); }
                catch (TimeoutException) { }
            }
        }
    }

    [Theory]
    [InlineData(1, "incomplete frame")]
    [InlineData(2, "malformed frame")]
    [InlineData(3, "wrong message type")]
    [InlineData(4, "wrong execution identity")]
    [InlineData(5, "wrong generation digest")]
    [InlineData(6, "wrong sequence")]
    [InlineData(7, "wrong preparation identity")]
    public void NonRootWp04CoordinatorProtocolNegativesRefuseAndReconcile(int mode, string caseName)
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            coordinator: BuildTestCoordinator(mode), filter: filter);

        var outcome = fx.Scope.StartOwnerPreparation(
            fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        Assert.False(outcome.Ok, caseName);
        Assert.False(outcome.Held, caseName);
        Assert.False(outcome.Completed, caseName);
        Assert.DoesNotContain("target constructor marker appeared", outcome.Error, StringComparison.OrdinalIgnoreCase);
        if (mode >= 5)
            Assert.Contains("coordinator frame identity, sequence, kind or length mismatch", outcome.Error, StringComparison.Ordinal);
        Assert.False(TargetExecPresentInDomain(fx), "invalid coordinator frame must not initialize the target");
        Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
        ReconcileAfterOwnerDeath(fx);
        Assert.False(TargetExecPresentInDomain(fx), "target remains absent after final cleanup");
    }

    [Fact]
    public void NonRootWp04CoordinatorExitBeforeHeldRefusesAndReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            coordinator: BuildTestCoordinator(8), filter: filter);

        var outcome = fx.Scope.StartOwnerPreparation(
            fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        AssertSetupRejectedAndReconciled(fx, outcome, "status channel closed before a complete record");
    }

    [Fact]
    public void NonRootWp04InvalidMountReportRefusesAndReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            coordinator: BuildTestCoordinator(9), filter: filter);

        var outcome = fx.Scope.StartOwnerPreparation(
            fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        AssertSetupRejectedAndReconciled(fx, outcome, "scratch tmpfs capacity mismatch");
    }

    [Fact]
    public void NonRootWp04HostNamespaceClaimRefusesAndReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        var hostMntNamespace = ReadNamespaceInode("mnt");
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            coordinator: BuildTestCoordinator(10, hostMntNamespace), filter: filter);

        var outcome = fx.Scope.StartOwnerPreparation(
            fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        AssertSetupRejectedAndReconciled(fx, outcome, "trusted coordinator did not enter a private mnt namespace");
    }

    [Fact]
    public void NonRootWp04CorruptPinnedFilterRefusesAndReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var coordinator = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/exec-coordinator"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        filter[0] ^= 0x01;
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            coordinator: coordinator, filter: filter);

        var outcome = fx.Scope.StartOwnerPreparation(
            fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        AssertSetupRejectedAndReconciled(fx, outcome, "staged seccomp filter digest does not match the approved artifact");
    }

    [Fact]
    public void NonRootWp04SyntheticHeldReportPositiveControlAbortsAndReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            coordinator: BuildTestCoordinator(11), filter: filter);

        var outcome = fx.Scope.StartOwnerPreparation(
            fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);
        Assert.True(outcome.Ok, outcome.Error);
        Assert.True(outcome.Held, outcome.Error);
        Assert.False(outcome.Completed);
        Assert.True(outcome.NoTargetInitializationMarkerAbsent);
        Assert.False(TargetExecPresentInDomain(fx));

        var aborted = fx.Scope.AbortHeld(outcome);
        Assert.True(aborted.Accepted, aborted.Error);
        Assert.True(aborted.OwnerExited, aborted.Error);
        Assert.True(aborted.CleanupRequested, aborted.Error);
        Assert.True(aborted.NoTargetInitializationMarkerAbsent);
        ReconcileAfterOwnerDeath(fx);
        Assert.False(TargetExecPresentInDomain(fx), "target remains absent after final reconciliation");
        AssertFinalCleanupTargetMarkerAbsentIfTraced();
    }

    private static void AssertSetupRejectedAndReconciled(
        IntegratedFixture fx, OwnerPreparationOutcome outcome, string expectedError)
    {
        Assert.False(outcome.Ok, expectedError);
        Assert.False(outcome.Held, expectedError);
        Assert.False(outcome.Completed, expectedError);
        Assert.Contains(expectedError, outcome.Error, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("target constructor marker appeared", outcome.Error, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(outcome.Owner);
        outcome.Owner!.Refresh();
        Assert.True(outcome.Owner.HasExited, "failed setup must stop and reap its owner before recovery");
        Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
        Assert.False(TargetExecPresentInDomain(fx), "refused setup must not initialize the target");

        ReconcileAfterOwnerDeath(fx);
        Assert.False(TargetExecPresentInDomain(fx), "target remains absent after bounded final reconciliation");
    }

    [Fact]
    public void NonRootWp04RealOwnerLossAfterHeldAcceptanceReconciles()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var coordinator = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/exec-coordinator"));
        var filter = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/seccomp-filter.bin"));
        using var ownerObservation = new RecordingPidFdProvider();
        var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null,
            pidFdProvider: ownerObservation, coordinator: coordinator, filter: filter);
        using var fixture = fx;
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        Assert.True(outcome.Ok, outcome.Error);
        Assert.True(outcome.Held, outcome.Error);
        Assert.False(outcome.Completed);
        Assert.True(outcome.NoTargetInitializationMarkerAbsent);
        var owner = outcome.Owner!;
        Assert.NotEqual(Environment.ProcessId, owner.Id);
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var exited, out var pidfdError), pidfdError);
        Assert.False(exited, "accepted HELD must retain a live owner before the real kill barrier");
        Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);

        var domainPath = System.IO.Path.Combine(DelegatedParent!, fx.ChildName);
        Assert.Contains("populated 1", File.ReadAllText(System.IO.Path.Combine(domainPath, "cgroup.events")), StringComparison.Ordinal);
        Assert.True(Assert.Single(fx.Scope.RetainedRecoveryLeases).AuthorityAlive);
        Assert.Single(fx.Scope.RetainedStagingDirectories);
        var competitor = new OwnershipJournal(new JournalFileStorage(), GetJournalDirectory(fx.Journal));
        Assert.False(competitor.TryAcquireWriter(out _, out _), "accepted HELD owner must retain the journal writer until process death");

        // Real SIGKILL after HELD has been accepted; no cancellation, exception,
        // or cooperative owner exit stands in for process loss.
        owner.Kill(entireProcessTree: false);
        var ownerExit = fx.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(10));
        Assert.True(ownerExit.Exited, ownerExit.Error);
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out exited, out pidfdError), pidfdError);
        Assert.True(exited, "pidfd must report the accepted-HELD owner dead before reconciliation");
        Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
        Assert.True(Assert.Single(fx.Scope.RetainedRecoveryLeases).AuthorityAlive,
            "retained recovery scope must survive owner death");
        Assert.False(TargetExecPresentInDomain(fx), "the HELD coordinator must not have initialized the target before cleanup");

        ReconcileAfterOwnerDeath(fx);
        Assert.True(outcome.NoTargetInitializationMarkerAbsent,
            "the adapter's independently collected constructor-marker oracle remains absent after final cleanup");
    }

    [Fact]
    public void NonRootWp04MissingFilterRefusesBeforeBubblewrapOrTargetInitialization()
    {
        SkipUnlessQualified();
        var root = RepositoryRoot();
        var fixtureImage = File.ReadAllBytes(System.IO.Path.Combine(root, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
        var coordinator = File.ReadAllBytes(System.IO.Path.Combine(root, "artifacts/linux-m1/wp04/exec-coordinator"));
        using var fx = new IntegratedFixture(fixtureImage, Bytes(0x45, 64), null, coordinator: coordinator);

        var outcome = fx.Scope.StartOwnerPreparation(
            fx.Request(null, enableHeldCoordinator: true), CancellationToken.None);

        Assert.False(outcome.Ok);
        Assert.False(outcome.Held);
        Assert.False(outcome.Completed);
        Assert.Contains("owner request refused", outcome.Error);
        Assert.False(Directory.Exists(System.IO.Path.Combine(DelegatedParent!, fx.ChildName)));
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        AssertFinalCleanupTargetMarkerAbsentIfTraced();
    }

    private static string RepositoryRoot()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("GAGAMBA_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var fullRoot = System.IO.Path.GetFullPath(configuredRoot);
            if (!File.Exists(System.IO.Path.Combine(fullRoot, "Gagamba.sln")))
                throw new DirectoryNotFoundException("GAGAMBA_REPOSITORY_ROOT does not identify the Gagamba repository");
            return fullRoot;
        }
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory.FullName, "Gagamba.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Gagamba repository root not found");
    }

    [Fact]
    public async Task NonRootMissingPidfdRefusesBeforeOwnerTransaction()
    {
        SkipUnlessQualified();
        using var pidFdProvider = new FailingPidFdProvider();
        using var fx = new IntegratedFixture(Bytes(0x19, 1024), null, null, pidFdProvider);
        var preparation = Task.Run(() => fx.Scope.StartOwnerPreparation(fx.Request(null), TestContext.Current.CancellationToken));

        try
        {
            Assert.True(await Task.Run(() => pidFdProvider.WaitForAttempt(TimeSpan.FromSeconds(5)), TestContext.Current.CancellationToken), "owner pidfd attempt was not observed");
            var observedOwner = pidFdProvider.OwnerProcess
                ?? throw new InvalidOperationException("spawned owner process was not retained for the live observation");
            Assert.False(observedOwner.HasExited, "owner must remain live while pidfd acquisition is held");
            Assert.Equal(PreparationStage.AllocationIntent, fx.Journal.ReadLatest().Snapshot!.Stage);
            Assert.False(Directory.Exists(System.IO.Path.Combine(DelegatedParent!, fx.ChildName)));
        }
        finally
        {
            pidFdProvider.Release();
            await preparation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }

        var outcome = await preparation;
        var ownerProcess = pidFdProvider.OwnerProcess
            ?? throw new InvalidOperationException("spawned owner process observation was lost");
        Assert.False(outcome.Completed);
        Assert.Null(outcome.Owner);
        Assert.Contains("pidfd", outcome.Error, StringComparison.OrdinalIgnoreCase);
        await ownerProcess.WaitForExitAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(ownerProcess.HasExited);
        Assert.Equal(PreparationStage.AllocationIntent, fx.Journal.ReadLatest().Snapshot!.Stage);
        Assert.False(Directory.Exists(System.IO.Path.Combine(DelegatedParent!, fx.ChildName)));
        Assert.Empty(fx.Scope.RetainedRecoveryLeases);

        Assert.True(fx.Journal.TryAcquireWriter(out var recovered, out var error), error);
        using (recovered!)
        {
            Assert.Equal(1, recovered!.BaselineSequence);
        }
    }
    [Fact]
    public void NonRootRecoveryScopeWaitsForOwnerDomainAuthorityBeforePreparationCompletes()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x1A, 1024), null, null);
        var scopePid = Environment.ProcessId;
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request(null), TestContext.Current.CancellationToken);

        Assert.True(outcome.Ok, outcome.Error);
        Assert.True(outcome.Completed);
        var owner = outcome.Owner!;
        Assert.NotEqual(scopePid, owner.Id);
        Assert.Equal(fx.Exec, outcome.Description!.ExecutionId);
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var exited, out var pidfdError), pidfdError);
        Assert.False(exited, "the owner must still be live while its local preparation capability is held");

        var retainedDomain = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.True(retainedDomain.AuthorityAlive);
        Assert.Equal(fx.Exec, retainedDomain.Identity.ExecutionId);
        Assert.Equal(fx.ChildName, retainedDomain.Identity.ChildName);
        var snapshot = fx.Journal.ReadLatest().Snapshot!;
        Assert.Equal(PreparationStage.Prepared, snapshot.Stage);
        var domainRecord = Assert.Single(snapshot.Allocations, allocation => allocation.Kind == AllocationKind.Domain);
        Assert.True(domainRecord.Confirmed);
        Assert.Equal(retainedDomain.Identity.ChildDevice, domainRecord.Device);
        Assert.Equal(retainedDomain.Identity.ChildInode, domainRecord.Inode);
        Assert.Single(fx.Scope.RetainedStagingParents);
        Assert.Single(fx.Scope.RetainedStagingDirectories);

        var competitor = new OwnershipJournal(new JournalFileStorage(), fx.JournalDir.Path);
        var writerExcluded = !competitor.TryAcquireWriter(out _, out _);
        Assert.True(writerExcluded, "the live owner must retain the exclusive journal writer");
        Console.WriteLine(
            $"R6D.2a non-root topology: uid={RealUid} euid={EffectiveUid} capEff={CapEff} " +
            $"delegatedParent={DelegatedParent} recoveryScopePid={scopePid} ownerPid={owner.Id} " +
            $"domain={domainRecord.Name}:{domainRecord.Inode} domainAuthorityRetained={retainedDomain.AuthorityAlive} " +
            $"stagingParents={fx.Scope.RetainedStagingParents.Count} stagingDirectories={fx.Scope.RetainedStagingDirectories.Count} " +
            $"writerExcluded={writerExcluded}");
    }

    [Fact]
    public void NonRootAfterIntentBarrierIsDispatchedAtTheOwnerIntentBoundary()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x1B, 1024), null, null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request("after-intent"), TestContext.Current.CancellationToken);

        Assert.False(outcome.Ok);
        Assert.Equal(string.Empty, outcome.Error);
        Assert.Equal("after-intent", outcome.Barrier);
        Assert.Equal(PreparationStage.AllocationIntent, fx.Journal.ReadLatest().Snapshot!.Stage);
        Assert.Empty(fx.Scope.RetainedRecoveryLeases);
        Assert.Empty(fx.Scope.RetainedStagingDirectories);
    }

    [Fact]
    public void NonRootAfterRegisteredBarrierIsDispatchedBeforeStagingAuthority()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x1C, 1024), null, null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request("after-registered"), TestContext.Current.CancellationToken);

        Assert.False(outcome.Ok);
        Assert.Equal(string.Empty, outcome.Error);
        Assert.Equal("after-registered", outcome.Barrier);
        Assert.Equal(PreparationStage.AllocationRegistered, fx.Journal.ReadLatest().Snapshot!.Stage);
        Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Empty(fx.Scope.RetainedStagingDirectories);
    }

    [Fact]
    public void NonRootWp04OwnerDeathAfterDomainRegistrationBeforeCoordinatorStartReconciles()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x1D, 1024), null, null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request("after-registered"), CancellationToken.None);

        AssertLiveOwnerAtBarrier(fx.Scope, outcome, "after-registered", fx.Journal);
        var owner = outcome.Owner!;
        var ownerStatus = File.ReadAllLines($"/proc/{owner.Id}/status");
        var parentLine = Assert.Single(ownerStatus, line => line.StartsWith("PPid:", StringComparison.Ordinal));
        Assert.Equal(Environment.ProcessId, int.Parse(parentLine.AsSpan("PPid:".Length).Trim()));
        var before = fx.Journal.ReadLatest();
        Assert.True(before.Ok, before.Error);
        Assert.Equal(PreparationStage.AllocationRegistered, before.Snapshot!.Stage);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.True(recovery.AuthorityAlive);
        var domainRecord = Assert.Single(before.Snapshot.Allocations, item => item.Kind == AllocationKind.Domain);
        Assert.True(domainRecord.Confirmed);
        Assert.Equal(recovery.Identity.ChildDevice, domainRecord.Device);
        Assert.Equal(recovery.Identity.ChildInode, domainRecord.Inode);
        var domainPath = System.IO.Path.Combine(DelegatedParent!, fx.ChildName);
        Assert.True(Directory.Exists(domainPath), "the durable registered domain must exist at the kill barrier");
        Assert.Contains("populated 0", File.ReadAllText(System.IO.Path.Combine(domainPath, "cgroup.events")), StringComparison.Ordinal);
        Assert.Empty(File.ReadAllLines(System.IO.Path.Combine(domainPath, "cgroup.procs")));
        Assert.Empty(fx.Scope.RetainedStagingDirectories);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        Assert.False(TargetExecPresentInDomain(fx));
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var exited, out var pidfdError), pidfdError);
        Assert.False(exited, "pidfd must prove the owner is live immediately before the real kill");

        owner.Kill(entireProcessTree: false);
        var ownerExit = fx.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(10));
        Assert.True(ownerExit.Exited, ownerExit.Error);
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out exited, out pidfdError), pidfdError);
        Assert.True(exited, "pidfd must observe the direct owner exit before recovery takeover");
        Assert.True(recovery.AuthorityAlive, "recovery authority must survive owner death");
        Assert.Equal(PreparationStage.AllocationRegistered, fx.Journal.ReadLatest().Snapshot!.Stage);

        var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var cleaner = new RegisteredDeathCleaner(parentFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        Assert.True(fx.Journal.TryAcquireWriter(out var recoveryWriter, out var writerError), writerError);
        using (recoveryWriter!)
        {
            var first = reconciler.Reconcile(before.Snapshot, Boot(), recovery, cleaner, fx.Journal, recoveryWriter);
            var reread = fx.Journal.ReadLatest();
            Assert.True(reread.Ok, reread.Error);
            var second = reconciler.Reconcile(reread.Snapshot!, Boot(), recovery, cleaner, fx.Journal, recoveryWriter);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, first.Outcome);
            Assert.Equal(first.Outcome, second.Outcome);
            Assert.Equal(OwnershipCleanupState.Confirmed, fx.Journal.ReadLatest().Snapshot!.Cleanup);

        AssertFinalCleanupTargetMarkerAbsentIfTraced();
        }

        Assert.True(fx.Scope.ReapAdoptedChildren(TimeSpan.FromSeconds(5), out _, out var reapError), reapError);
        Assert.False(Directory.Exists(domainPath));
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        Assert.False(TargetExecPresentInDomain(fx), "target remains absent after final reconciliation");
        AssertFinalCleanupTargetMarkerAbsentIfTraced();
    }

    [Fact]
    public void NonRootOwnerDeathBeforeRegistrationRecoversIntegrated()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x20, 1024), null, null);
        var sibling = System.IO.Path.Combine(DelegatedParent!, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        try
        {
            var outcome = fx.Scope.StartOwnerPreparation(fx.Request("after-domain-retained"), CancellationToken.None);
            Assert.Equal("after-domain-retained", outcome.Barrier);
            AssertLiveOwnerAtBarrier(fx.Scope, outcome, "after-domain-retained", fx.Journal);
            outcome.Owner!.Kill(entireProcessTree: false);
            Assert.True(fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5)).Exited);
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
    public void NonRootMidStagingDeathResolvesUnconfirmedAbsentInputAsGenuineAbsence()
    {
        SkipUnlessQualified();
        var tool = Bytes(0x40, 1000);
        var input = Bytes(0x41, 200000);
        using var fx = new IntegratedFixture(tool, input, null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request("mid-staging"), CancellationToken.None);
        Assert.Equal("mid-staging", outcome.Barrier);
        AssertLiveOwnerAtBarrier(fx.Scope, outcome, "mid-staging", fx.Journal);
        outcome.Owner!.Kill(entireProcessTree: false);
        Assert.True(fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5)).Exited);

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        var toolEntry = Assert.Single(read.Snapshot!.Allocations, a => a.Kind == AllocationKind.Staging && a.Name == "tool-000.staged");
        Assert.True(toolEntry.Confirmed);
        var inputEntry = Assert.Single(read.Snapshot.Allocations, a => a.Kind == AllocationKind.Staging && a.Name == "input-001.staged");
        Assert.False(inputEntry.Confirmed);
        Assert.True(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged")));
        Assert.False(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "input-001.staged")));

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var parentHandle = (SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents);
        var subdirHandle = (SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories);
        var parentFd = parentHandle.DangerousGetHandle().ToInt32();
        var subdirFd = subdirHandle.DangerousGetHandle().ToInt32();
        Assert.True(fx.Journal.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
        using (takeover!)
        {
        var cleaner = new QualScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged")));
        Assert.False(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "input-001.staged")));
        }
    }

    [Fact]
    public void NonRootPreparedBeforeMintDoesNotResurrectCapability()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x60, 2048), null, null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request("after-prepared-before-mint"), CancellationToken.None);
        Assert.Equal("after-prepared-before-mint", outcome.Barrier);
        Assert.NotNull(outcome.Description);
        AssertLiveOwnerAtBarrier(fx.Scope, outcome, "after-prepared-before-mint", fx.Journal);
        outcome.Owner!.Kill(entireProcessTree: false);
        Assert.True(fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5)).Exited);

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.Prepared, read.Snapshot!.Stage);
        Assert.NotNull(read.Snapshot.Preparation);
        // The durable Prepared record has only an identifier; the owner-local
        // store died with the process and has no scope-side mirror.
        Assert.Null(typeof(OwnerPreparationOutcome).GetProperty("Capability"));

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var subdirFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories)).DangerousGetHandle().ToInt32();
        var cleaner = new QualScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        Assert.True(fx.Journal.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
        using (takeover!)
        {
        var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        }
    }

    // ---- Staging reconciliation truthfulness ----

    [Fact]
    public void NonRootCompleteCleanupIsConfirmed()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x50, 2048), Bytes(0x51, 4096), null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request("after-copy"), CancellationToken.None);
        Assert.Equal("after-copy", outcome.Barrier);
        outcome.Owner!.Kill(entireProcessTree: false);
        Assert.True(fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5)).Exited);
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.NotEqual(PreparationStage.Prepared, read.Snapshot!.Stage);

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var subdirFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories)).DangerousGetHandle().ToInt32();
        var cleaner = new QualScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        Assert.True(fx.Journal.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
        using (takeover!)
        {
        var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        }
    }

    [Fact]
    public void NonRootReplacementStagingObjectSurvives()
    {
        SkipUnlessQualified();
        using var fx = new IntegratedFixture(Bytes(0x52, 2048), null, null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.Request("after-copy"), CancellationToken.None);
        Assert.Equal("after-copy", outcome.Barrier);
        outcome.Owner!.Kill(entireProcessTree: false);
        Assert.True(fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5)).Exited);
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);

        // Replace the staged object with a distinct file at the same name.
        var stagedPath = System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged");
        File.Delete(stagedPath);
        File.WriteAllBytes(stagedPath, Bytes(0x77, 64));

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var subdirFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories)).DangerousGetHandle().ToInt32();
        var cleaner = new QualScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
        Assert.True(fx.Journal.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
        using (takeover!)
        {
        var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.True(File.Exists(stagedPath), "replacement must be preserved");
        }
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
            var outcome = fx.Scope.StartOwnerPreparation(fx.Request("after-copy"), CancellationToken.None);
            Assert.Equal("after-copy", outcome.Barrier);
            outcome.Owner!.Kill(entireProcessTree: false);
            Assert.True(fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5)).Exited);
            var read = fx.Journal.ReadLatest();
            var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
            var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
            var subdirFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories)).DangerousGetHandle().ToInt32();
            var cleaner = new QualScopeCleaner(parentFd, subdirFd);
            var reconciler = new Reconciler(new RealClock(), TimeSpan.FromSeconds(5));
            Assert.True(fx.Journal.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
            using (takeover!)
            {
            var result = reconciler.Reconcile(read.Snapshot!, Boot(), recovery, cleaner, fx.Journal, takeover);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            }
            Assert.True(Directory.Exists(sibling));
        }
        finally
        {
            try { Directory.Delete(sibling); } catch { }
        }
    }
}
