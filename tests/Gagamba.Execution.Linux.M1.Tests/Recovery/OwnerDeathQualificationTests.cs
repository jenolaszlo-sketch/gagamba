// R6D.3 D1-D6 owner-death qualification: real owner OS processes reach real
// production barriers, are really killed, and the surviving recovery scope
// takes over the writer and reconciles from durable state with retained
// authority. No BarrierDeathException, no simulated death, no synthetic
// records. Linux-only; every case is finite and skips honestly elsewhere.
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class OwnerDeathQualificationTests
{
    private const string FallbackCgroupRoot = "/sys/fs/cgroup";

    private readonly ITestOutputHelper _output;

    public OwnerDeathQualificationTests(ITestOutputHelper output) => _output = output;

    private static string ParentRoot =>
        Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP") is { } root && Directory.Exists(root)
            ? root
            : FallbackCgroupRoot;

    private sealed class TempParent : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(ParentRoot, "gagamba-r6d3-" + Guid.NewGuid().ToString("N"));

        public TempParent()
        {
            Directory.CreateDirectory(Path);
            try { File.WriteAllText(System.IO.Path.Combine(Path, "cgroup.subtree_control"), "+memory +pids"); } catch { }
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
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-r6d3-" + Guid.NewGuid().ToString("N"));

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
            var probe = System.IO.Path.Combine(ParentRoot, "gagamba-r6d3-probe-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(probe);
            var ok = File.Exists(System.IO.Path.Combine(probe, "cgroup.kill"));
            Directory.Delete(probe);
            return ok && PreparationInterop.ProbeOpenat2Support() != Openat2SupportKind.Unsupported;
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
            Assert.Skip("requires delegated Linux cgroup v2 with openat2");
        }
    }

    private static string Boot() =>
        CgroupInterop.TryReadBootId(out var id, out _) ? id : "unknown";

    private sealed class Fixture : IDisposable
    {
        public TempParent Temp = new();
        public TempDir Source = new();
        public TempDir Staging = new();
        public TempDir JournalDir = new();
        public M1RecoveryScope Scope = new();
        public OwnershipJournal Journal;
        public JournalWriterLease Writer;
        public Guid Exec = Guid.NewGuid();
        public Guid LeaseGen = Guid.NewGuid();
        public string ChildName = "d-" + Guid.NewGuid().ToString("N");

        public Fixture(byte[] toolContent, byte[]? inputContent)
        {
            Assert.True(Scope.TryInitialize().Ok);
            Assert.True(Scope.TryOpenDelegatedParent(Temp.Path).Ok);
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "tool"), toolContent);
            var objects = new List<CopyObjectPlan> { new("tool", "tool", 4UL * 1024 * 1024, null) };
            if (inputContent is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "input"), inputContent);
                objects.Add(new("input", "input", 4UL * 1024 * 1024, null));
            }

            Journal = new OwnershipJournal(new JournalFileStorage(), JournalDir.Path);
            Assert.True(Journal.TryAcquireWriter(out var writer, out var error), error);
            Writer = writer!;
            Plan = new CopyPlan(
                Source.Path, Staging.Path, "prep-001", objects, 16UL * 1024 * 1024, 128,
                Array.Empty<KeyValuePair<string, string>>(), _ => true, null);
        }

        public CopyPlan Plan { get; }

        public OwnerPreparationRequest OwnerRequest(string? testBarrier) =>
            new(
                RecoveryHarness.LocateOwnerDll() ?? throw new InvalidOperationException("owner executable missing from the test build"),
                ChildName, Exec, LeaseGen, Boot(), Plan, Journal, Writer, Issuer, Generation,
                staged =>
                {
                    var manifest = ManifestWithClosure(staged);
                    return (manifest, IdentityFor(manifest, Exec));
                },
                MonotonicNanoseconds() + 60_000_000_000L,
                testBarrier);

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
            JournalDir.Dispose();
            Staging.Dispose();
            Source.Dispose();
            Temp.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private sealed class DeathQualificationCleaner : IAllocationCleaner
    {
        private readonly int _parentFd;
        private readonly SafeHandle? _subdir;
        private readonly string _stagingDirectoryName;

        public DeathQualificationCleaner(int parentFd, int subdirFd, string stagingDirectoryName = "prep-001")
        {
            _parentFd = parentFd;
            _subdir = subdirFd >= 0 ? new SafeFileHandle(new IntPtr(subdirFd), ownsHandle: false) : null;
            _stagingDirectoryName = stagingDirectoryName;
        }

        public AllocationCleanResult CleanOne(AllocationEntry entry)
        {
            if (entry.Kind == AllocationKind.StagingDirectory)
            {
                if (_subdir is not null)
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

                // No retained subdirectory handle (staging never started):
                // resolve the directory name beneath the retained parent
                // only. Genuine absence resolves; anything present with an
                // unconfirmable identity is preserved as mismatch.
                return ResolveDirectoryBeneathParent(entry);
            }

            if (entry.Kind != AllocationKind.Staging)
            {
                return new AllocationCleanResult(AllocationCleanOutcome.Failed, $"unexpected entry kind {entry.Kind}");
            }

            SafeHandle scope;
            SafeHandle? owned = null;
            try
            {
                if (_subdir is not null)
                {
                    scope = _subdir;
                }
                else
                {
                    // Objects live inside the staging directory: open that
                    // container beneath the retained parent first. A missing
                    // container means every object inside it is genuinely
                    // absent.
                    var (handle, early) = OpenStagingDirectory();
                    if (early.HasValue)
                    {
                        handle?.Dispose();
                        return early.Value;
                    }

                    owned = handle;
                    scope = owned!;
                }

                var result = StagingCleanup.CleanOne(LibcCopyHost.Instance, scope, entry.Name, entry.Device, entry.Inode);
                return result.Outcome switch
                {
                    CleanupItemOutcome.Removed => new AllocationCleanResult(AllocationCleanOutcome.Cleaned, string.Empty),
                    CleanupItemOutcome.AlreadyAbsent => new AllocationCleanResult(AllocationCleanOutcome.AlreadyAbsent, string.Empty),
                    CleanupItemOutcome.IdentityMismatch => new AllocationCleanResult(AllocationCleanOutcome.Mismatch, result.Error),
                    _ => new AllocationCleanResult(AllocationCleanOutcome.Failed, result.Error),
                };
            }
            finally
            {
                owned?.Dispose();
            }
        }

        private (SafeHandle? Handle, AllocationCleanResult? Early) OpenStagingDirectory()
        {
            if (_stagingDirectoryName.Contains('/') || _stagingDirectoryName.Length == 0)
            {
                return (null, new AllocationCleanResult(AllocationCleanOutcome.Failed, "staging directory name is not a single component"));
            }

            var openErrno = PreparationInterop.OpenRelative(
                _parentFd, _stagingDirectoryName,
                PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
                PreparationInterop.RequiredResolve, out var dirFd);
            if (openErrno == PreparationInterop.ENOENT)
            {
                return (null, new AllocationCleanResult(AllocationCleanOutcome.AlreadyAbsent, string.Empty));
            }

            if (openErrno != 0 || dirFd < 0)
            {
                return (null, new AllocationCleanResult(AllocationCleanOutcome.Failed, $"staging inspection errno {openErrno}"));
            }

            return (new SafeFileHandle(new IntPtr(dirFd), ownsHandle: true), null);
        }

        private AllocationCleanResult ResolveDirectoryBeneathParent(AllocationEntry entry)
        {
            var (handle, early) = OpenStagingDirectory();
            if (early.HasValue)
            {
                handle?.Dispose();
                return early.Value;
            }

            using (handle!)
            {
                var dirFd = ((SafeFileHandle)handle!).DangerousGetHandle().ToInt32();
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
        }
    }

    private sealed class QualificationClock : IDomainClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();

        public long NowNanoseconds => _watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);

        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }

    private void LogDeathEvidence(
        string barrier, int scopePid, Process owner, OwnershipJournal journal,
        int retainedLeases, int retainedStagingDirs)
    {
        var latest = journal.ReadLatest();
        _output.WriteLine(
            $"R6D.3 barrier={barrier} scopePid={scopePid} ownerPid={owner.Id} ownerExitCode={owner.ExitCode} " +
            $"journalSeq={latest.Snapshot?.Sequence} journalStage={latest.Snapshot?.Stage} " +
            $"retainedLeases={retainedLeases} retainedStagingDirs={retainedStagingDirs}");
    }

    private static Process KillAndProveExit(
        M1RecoveryScope scope, OwnerPreparationOutcome outcome, string expectedBarrier,
        OwnershipJournal journal)
    {
        Assert.Equal(expectedBarrier, outcome.Barrier);
        var owner = outcome.Owner ?? throw new InvalidOperationException("barrier acknowledgement had no owner process");
        Assert.True(scope.TryOwnerPidFdExited(owner, 0, out var exited, out var pidfdAliveError), pidfdAliveError);
        Assert.False(exited, "retained pidfd already reports owner exit at the kill point");
        owner.Refresh();
        Assert.False(owner.HasExited, "direct-child owner must remain live at the kill point");
        AssertWriterExcludedWhileOwnerLive(journal);
        owner.Kill(entireProcessTree: false);
        var exit = scope.WaitForOwner(owner, TimeSpan.FromSeconds(10));
        Assert.True(exit.Exited, exit.Error);
        Assert.True(owner.HasExited);
        Assert.True(scope.TryOwnerPidFdExited(owner, 0, out var pidfdExited, out var pidfdError), pidfdError);
        Assert.True(pidfdExited, "pidfd must observe the owner exit");
        Assert.Equal(137, owner.ExitCode);
        return owner;
    }

    private static void AssertWriterExcludedWhileOwnerLive(OwnershipJournal journal)
    {
        var competitor = new OwnershipJournal(new JournalFileStorage(), GetJournalDirectory(journal));
        Assert.False(competitor.TryAcquireWriter(out _, out _));
    }

    private static string GetJournalDirectory(OwnershipJournal journal)
    {
        // The takeover competitor must open the same journal directory; the
        // directory travels on the writer lease issued at acquisition.
        var field = typeof(OwnershipJournal).GetField("_directory", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (string)field!.GetValue(journal)!;
    }

    private static void AssertPidfdRetainedCloexec(M1RecoveryScope scope, int ownerId)
    {
        var field = typeof(M1RecoveryScope).GetField("_retainedOwnerPidFds", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var pidfds = (Dictionary<int, SafeHandle>)field!.GetValue(scope)!;
        Assert.True(pidfds.TryGetValue(ownerId, out var pidfd));
        Assert.True(PreparationInterop.HasCloexec(pidfd.DangerousGetHandle().ToInt32()));
    }

    private static JournalWriterLease TakeOver(OwnershipJournal journal)
    {
        Assert.True(journal.TryAcquireWriter(out var takeover, out var error), error);
        Assert.NotNull(takeover);
        Assert.Equal(journal.ReadLatest().Snapshot!.Sequence, takeover!.BaselineSequence);
        return takeover;
    }

    private ReconcileResult ReconcileTwice(
        OwnershipSnapshot snapshot, DomainLease? lease, IAllocationCleaner cleaner,
        OwnershipJournal journal, JournalWriterLease writer)
    {
        var reconciler = new Reconciler(new QualificationClock(), TimeSpan.FromSeconds(5));
        var first = reconciler.Reconcile(snapshot, Boot(), lease, cleaner, journal, writer);
        var second = reconciler.Reconcile(
            journal.ReadLatest().Snapshot ?? snapshot, Boot(), lease, cleaner, journal, writer);
        Assert.Equal(first.Outcome, second.Outcome);
        return first;
    }

    [Fact]
    public void D1OwnerDeathAfterDurableIntentLeavesNoHiddenAllocation()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0xA1, 1024), null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest("after-intent"), CancellationToken.None);

        Assert.Equal("after-intent", outcome.Barrier);
        Assert.False(outcome.Completed);
        Assert.Null(outcome.Description);
        Assert.NotNull(outcome.Owner);
        Assert.NotEqual(Environment.ProcessId, outcome.Owner!.Id);
        Assert.Equal(PreparationStage.AllocationIntent, fx.Journal.ReadLatest().Snapshot!.Stage);
        Assert.Empty(fx.Scope.RetainedRecoveryLeases);
        Assert.Empty(fx.Scope.RetainedStagingDirectories);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Temp.Path, fx.ChildName)));
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        Assert.Null(typeof(OwnerPreparationOutcome).GetProperty("Capability"));
        AssertNoStoreOrCapabilityInScope(fx.Scope);
        AssertPidfdRetainedCloexec(fx.Scope, outcome.Owner.Id);
        Assert.False(outcome.Owner.HasExited, "owner must be alive while holding the writer at after-intent");
        AssertWriterExcludedWhileOwnerLive(fx.Journal);
        _output.WriteLine(
            $"D1 live barrier acknowledgement={outcome.Barrier} scopePid={Environment.ProcessId} " +
            $"ownerPid={outcome.Owner.Id} pidfdLive=true writerExcluded=true domain=false staging=false capability=false");

        var scopePid = Environment.ProcessId;
        KillAndProveExit(fx.Scope, outcome, "after-intent", fx.Journal);
        LogDeathEvidence("after-intent", scopePid, outcome.Owner, fx.Journal, 0, 0);

        using var takeover = TakeOver(fx.Journal);
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.AllocationIntent, read.Snapshot!.Stage);
        var stagingParentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var result = ReconcileTwice(read.Snapshot!, null,
            new DeathQualificationCleaner(stagingParentFd, -1), fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Temp.Path, fx.ChildName)));
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
    }

    [Fact]
    public void D2OwnerDeathAfterDomainRetentionRefusesUncorroboratedIntentThenCleansByRetainedHandle()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0xA2, 1024), null);
        var sibling = System.IO.Path.Combine(fx.Temp.Path, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        try
        {
            var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest("after-domain-retained"), CancellationToken.None);

            Assert.Equal("after-domain-retained", outcome.Barrier);
            Assert.False(outcome.Completed);
            var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
            Assert.True(recovery.AuthorityAlive);
            Assert.Equal(fx.Exec, recovery.Identity.ExecutionId);
            Assert.Equal(fx.ChildName, recovery.Identity.ChildName);
            Assert.True(Directory.Exists(System.IO.Path.Combine(fx.Temp.Path, fx.ChildName)));
            Assert.Equal(PreparationStage.AllocationIntent, fx.Journal.ReadLatest().Snapshot!.Stage);
            AssertPidfdRetainedCloexec(fx.Scope, outcome.Owner!.Id);
            AssertWriterExcludedWhileOwnerLive(fx.Journal);

            var scopePid = Environment.ProcessId;
            KillAndProveExit(fx.Scope, outcome, "after-domain-retained", fx.Journal);
            LogDeathEvidence("after-domain-retained", scopePid, outcome.Owner, fx.Journal, 1, 0);

            Assert.True(recovery.AuthorityAlive, "retained recovery handles must survive owner death");
            using var takeover = TakeOver(fx.Journal);
            var read = fx.Journal.ReadLatest();
            Assert.True(read.Ok, read.Error);
            var stagingParentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
            var cleaner = new DeathQualificationCleaner(stagingParentFd, -1);
            var result = ReconcileTwice(read.Snapshot!, recovery, cleaner, fx.Journal, takeover);
            Assert.Equal(ReconcileOutcome.Refused, result.Outcome);
            Assert.Contains("live lease identity does not corroborate the record", result.Detail, StringComparison.Ordinal);
            Assert.True(recovery.AuthorityAlive, "conservative journal refusal must not discard retained authority");
            _output.WriteLine(
                $"D2 retained domain identity: execution={recovery.Identity.ExecutionId} " +
                $"child={recovery.Identity.ChildName} device={recovery.Identity.ChildDevice} inode={recovery.Identity.ChildInode}");

            // The intent-stage journal has no registered domain identity, so
            // Reconciler correctly refuses to infer one. The survivor may
            // still stop/remove the exact domain through its retained lease.
            Assert.True(recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new QualificationClock()).Stopped);
            Assert.True(recovery.RemoveIfIdentityMatches().Removed);
            _output.WriteLine("D2 retained-handle stop=Confirmed removal=Confirmed; Reconciler remains Refused; no CleanupConfirmed written");
            Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Temp.Path, fx.ChildName)));
            Assert.True(Directory.Exists(sibling));
        }
        finally
        {
            try { Directory.Delete(sibling); } catch { }
        }
    }

    [Fact]
    public void D3OwnerDeathAfterRegistrationPreservesExactIdentity()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0xA3, 1024), null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest("after-registered"), CancellationToken.None);

        Assert.Equal("after-registered", outcome.Barrier);
        Assert.False(outcome.Completed);
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.AllocationRegistered, read.Snapshot!.Stage);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.True(recovery.AuthorityAlive);
        var domainEntry = Assert.Single(read.Snapshot.Allocations, item => item.Kind == AllocationKind.Domain);
        Assert.True(domainEntry.Confirmed);
        Assert.Equal(recovery.Identity.ChildDevice, domainEntry.Device);
        Assert.Equal(recovery.Identity.ChildInode, domainEntry.Inode);
        Assert.Equal(recovery.Identity.ExecutionId, read.Snapshot.ExecutionId);
        AssertPidfdRetainedCloexec(fx.Scope, outcome.Owner!.Id);
        AssertWriterExcludedWhileOwnerLive(fx.Journal);

        var scopePid = Environment.ProcessId;
        KillAndProveExit(fx.Scope, outcome, "after-registered", fx.Journal);
        LogDeathEvidence("after-registered", scopePid, outcome.Owner, fx.Journal, 1, 0);

            using var takeover = TakeOver(fx.Journal);
            var reread = fx.Journal.ReadLatest();
            Assert.True(reread.Ok, reread.Error);
            var stagingParentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
            var cleaner = new DeathQualificationCleaner(stagingParentFd, -1);
            var result = ReconcileTwice(reread.Snapshot!, recovery, cleaner, fx.Journal, takeover);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
            Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Temp.Path, fx.ChildName)));
    }

    [Fact]
    public void D4OwnerDeathAfterStagingAccountedKeepsExactDirectoryDebt()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0xA4, 1024), null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest("after-staging-accounted"), CancellationToken.None);

        Assert.Equal("after-staging-accounted", outcome.Barrier);
        Assert.False(outcome.Completed);
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        var directoryEntry = Assert.Single(read.Snapshot!.Allocations, item => item.Kind == AllocationKind.StagingDirectory);
        Assert.True(directoryEntry.Confirmed);
        Assert.NotEqual(0UL, directoryEntry.Inode);
        Assert.All(read.Snapshot.Allocations.Where(item => item.Kind == AllocationKind.Staging), item => Assert.False(item.Confirmed));
        var subdir = Assert.Single(fx.Scope.RetainedStagingDirectories);
        Assert.True(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        Assert.Empty(Directory.GetFiles(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        AssertPidfdRetainedCloexec(fx.Scope, outcome.Owner!.Id);
        AssertWriterExcludedWhileOwnerLive(fx.Journal);

        var scopePid = Environment.ProcessId;
        KillAndProveExit(fx.Scope, outcome, "after-staging-accounted", fx.Journal);
        LogDeathEvidence("after-staging-accounted", scopePid, outcome.Owner, fx.Journal, 1, 1);

        using var takeover = TakeOver(fx.Journal);
        var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var subdirFd = ((SafeFileHandle)subdir).DangerousGetHandle().ToInt32();
        var cleaner = new DeathQualificationCleaner(parentFd, subdirFd);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var result = ReconcileTwice(fx.Journal.ReadLatest().Snapshot!, recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
    }

    [Fact]
    public void D5OwnerDeathMidStagingCleansAccountedObjectThroughRetainedAuthority()
    {
        SkipUnlessUsable();
        var tool = Bytes(0xA5, 1000);
        var input = Bytes(0x46, 200_000);
        using var fx = new Fixture(tool, input);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest("mid-staging"), CancellationToken.None);

        Assert.Equal("mid-staging", outcome.Barrier);
        Assert.False(outcome.Completed);
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        var toolEntry = Assert.Single(read.Snapshot!.Allocations, item => item.Name == "tool-000.staged");
        Assert.True(toolEntry.Confirmed);
        var inputEntry = Assert.Single(read.Snapshot.Allocations, item => item.Name == "input-001.staged");
        Assert.False(inputEntry.Confirmed);
        Assert.True(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged")));
        AssertPidfdRetainedCloexec(fx.Scope, outcome.Owner!.Id);
        AssertWriterExcludedWhileOwnerLive(fx.Journal);

        var scopePid = Environment.ProcessId;
        KillAndProveExit(fx.Scope, outcome, "mid-staging", fx.Journal);
        LogDeathEvidence("mid-staging", scopePid, outcome.Owner, fx.Journal, 1, 1);

        using var takeover = TakeOver(fx.Journal);
        var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var subdirFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories)).DangerousGetHandle().ToInt32();
        var cleaner = new DeathQualificationCleaner(parentFd, subdirFd);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var result = ReconcileTwice(fx.Journal.ReadLatest().Snapshot!, recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged")));
        Assert.False(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "input-001.staged")));
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
    }

    [Fact]
    public void D5MidStagingReplacementSurvivesAsUnknownDebt()
    {
        SkipUnlessUsable();
        var tool = Bytes(0xA5, 1000);
        var input = Bytes(0x46, 200_000);
        using var fx = new Fixture(tool, input);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest("mid-staging"), CancellationToken.None);

        Assert.Equal("mid-staging", outcome.Barrier);
        var scopePid = Environment.ProcessId;
        KillAndProveExit(fx.Scope, outcome, "mid-staging", fx.Journal);
        LogDeathEvidence("mid-staging-replaced", scopePid, outcome.Owner!, fx.Journal, 1, 1);

        // Hostile change after death, before recovery: replace the accounted
        // tool file so its identity no longer matches the durable record.
        var toolPath = System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged");
        Assert.True(File.Exists(toolPath));
        File.Delete(toolPath);
        File.WriteAllBytes(toolPath, Bytes(0x47, 1000));

        using var takeover = TakeOver(fx.Journal);
        var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var subdirFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories)).DangerousGetHandle().ToInt32();
        var cleaner = new DeathQualificationCleaner(parentFd, subdirFd);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var reconciler = new Reconciler(new QualificationClock(), TimeSpan.FromSeconds(5));
        var first = reconciler.Reconcile(
            fx.Journal.ReadLatest().Snapshot!, Boot(), recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, first.Outcome);
        Assert.True(File.Exists(toolPath), "replacement must be preserved, never name-deleted");
        var staging = Assert.Single(first.Components!, item => item.Component == CleanupComponents.Staging);
        Assert.NotEqual(ComponentState.Confirmed, staging.State);
        var second = reconciler.Reconcile(
            fx.Journal.ReadLatest().Snapshot!, Boot(), recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(first.Outcome, second.Outcome);
        Assert.True(File.Exists(toolPath));
    }

    [Fact]
    public void D6OwnerDeathAfterPreparedBeforeMintCreatesNoCapability()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0xA6, 2048), null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest("after-prepared-before-mint"), CancellationToken.None);

        Assert.Equal("after-prepared-before-mint", outcome.Barrier);
        Assert.False(outcome.Completed);
        Assert.NotNull(outcome.Description);
        Assert.Null(typeof(OwnerPreparationOutcome).GetProperty("Capability"));
        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.Prepared, read.Snapshot!.Stage);
        Assert.NotNull(read.Snapshot.Preparation);
        AssertNoStoreOrCapabilityInScope(fx.Scope);
        AssertPidfdRetainedCloexec(fx.Scope, outcome.Owner!.Id);
        AssertWriterExcludedWhileOwnerLive(fx.Journal);

        var scopePid = Environment.ProcessId;
        KillAndProveExit(fx.Scope, outcome, "after-prepared-before-mint", fx.Journal);
        LogDeathEvidence("after-prepared-before-mint", scopePid, outcome.Owner, fx.Journal, 1, 1);

        using var takeover = TakeOver(fx.Journal);
        var reread = fx.Journal.ReadLatest();
        Assert.True(reread.Ok, reread.Error);
        Assert.Equal(PreparationStage.Prepared, reread.Snapshot!.Stage);
        AssertNoStoreOrCapabilityInScope(fx.Scope);
        var parentFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents)).DangerousGetHandle().ToInt32();
        var subdirFd = ((SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories)).DangerousGetHandle().ToInt32();
        var cleaner = new DeathQualificationCleaner(parentFd, subdirFd);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        var result = ReconcileTwice(reread.Snapshot!, recovery, cleaner, fx.Journal, takeover);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        AssertNoStoreOrCapabilityInScope(fx.Scope);
    }

    private static void AssertNoStoreOrCapabilityInScope(M1RecoveryScope scope)
    {
        foreach (var field in typeof(M1RecoveryScope).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var value = field.GetValue(scope);
            Assert.False(value is PreparedTransaction.PreparationStore,
                $"recovery scope must never hold a PreparationStore (field {field.Name})");
        }

        foreach (var property in typeof(OwnerPreparedDescription).GetProperties())
        {
            Assert.False(typeof(SafeHandle).IsAssignableFrom(property.PropertyType),
                $"prepared description must not carry handles (property {property.Name})");
            Assert.False(typeof(IDisposable).IsAssignableFrom(property.PropertyType)
                && property.PropertyType != typeof(string)
                && property.PropertyType.IsClass,
                $"prepared description must not carry disposable authority (property {property.Name})");
        }
    }
}
