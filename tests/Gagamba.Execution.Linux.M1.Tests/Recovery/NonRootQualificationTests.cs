// R6D.2 non-root owner-owned transaction qualification on operator-delegated cgroup v2.
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
            ChildName = "d-" + Guid.NewGuid().ToString("N");
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
        Assert.NotNull(outcome.HeldReport);
        Assert.Equal(0u, outcome.HeldReport!.Uid);
        Assert.Equal(0UL, outcome.HeldReport.CapEff);

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
        Assert.True(aborted.NoTargetInitializationMarkerAbsent);

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
    }

    private static string RepositoryRoot()
    {
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
