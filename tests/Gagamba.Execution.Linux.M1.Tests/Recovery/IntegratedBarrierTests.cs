// R6D.2 owner-process transaction and recovery-scope death barriers.
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Preparation.CopyTestSupport;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class IntegratedBarrierTests
{
    private const string CgroupRoot = "/sys/fs/cgroup";
    [DllImport("libc", SetLastError = true)]
    private static extern int open(string path, int flags);

    private sealed class TempParent : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(CgroupRoot, "gagamba-r4-" + Guid.NewGuid().ToString("N"));

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
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gagamba-r4-" + Guid.NewGuid().ToString("N"));

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
            var probe = System.IO.Path.Combine(CgroupRoot, "gagamba-r4-probe-" + Guid.NewGuid().ToString("N"));
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

        public Fixture(byte[] toolContent, byte[]? inputContent, CopyTestHooks? hooks)
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
                Array.Empty<KeyValuePair<string, string>>(), _ => true, hooks);
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

    [Fact]
    public void OwnerProcessPerformsPreparationAndKeepsCapabilityLocal()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x71, 2048), null, null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest(null), CancellationToken.None);

        Assert.True(outcome.Ok, outcome.Error);
        Assert.NotNull(outcome.Owner);
        Assert.NotEqual(Environment.ProcessId, outcome.Owner!.Id);
        Assert.Equal(fx.Exec, outcome.Description!.ExecutionId);
        Assert.Equal(PreparationStage.Prepared, fx.Journal.ReadLatest().Snapshot!.Stage);
        Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Single(fx.Scope.RetainedStagingParents);
        Assert.Single(fx.Scope.RetainedStagingDirectories);
        Assert.Null(typeof(OwnerPreparationOutcome).GetProperty("Capability"));

        var competitor = new OwnershipJournal(new JournalFileStorage(), fx.JournalDir.Path);
        Assert.False(competitor.TryAcquireWriter(out _, out _));

        outcome.Owner.Kill(entireProcessTree: false);
        var exit = fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5));
        Assert.True(exit.Exited, exit.Error);
        Assert.True(fx.Journal.TryAcquireWriter(out var recoveryWriter, out var writerError), writerError);
        using (recoveryWriter!)
        {
            var snapshot = fx.Journal.ReadLatest().Snapshot!;
            var stagingParent = (SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingParents);
            var stagingDirectory = (SafeFileHandle)Assert.Single(fx.Scope.RetainedStagingDirectories);
            var cleaner = new ScopeCleaner(
                stagingParent.DangerousGetHandle().ToInt32(),
                stagingDirectory.DangerousGetHandle().ToInt32());
            var result = new Reconciler(new BarrierClock(), TimeSpan.FromSeconds(5)).Reconcile(
                snapshot, Boot(), Assert.Single(fx.Scope.RetainedRecoveryLeases), cleaner, fx.Journal, recoveryWriter);
            Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        }
    }

    [Fact]
    public void OwnerDoesNotCopyUntilScopeAcknowledgesStagingRetention()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x75, 1024), null, null);
        var request = fx.OwnerRequest(null) with { WithholdStagingRetentionAckForTest = true };
        var outcome = fx.Scope.StartOwnerPreparation(request, CancellationToken.None);

        Assert.False(outcome.Completed);
        Assert.Contains("staging retention acknowledgement", outcome.Error, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(outcome.Owner);
        Assert.True(fx.Scope.TryOwnerPidFdExited(outcome.Owner!, 0, out var exited, out var pidfdError), pidfdError);
        Assert.False(exited, "owner must remain blocked waiting for the missing acknowledgement");

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.AllocationRegistered, read.Snapshot!.Stage);
        var directory = Assert.Single(read.Snapshot.Allocations, item => item.Kind == AllocationKind.StagingDirectory);
        Assert.False(directory.Confirmed);
        Assert.Equal(0UL, directory.Inode);
        Assert.Single(fx.Scope.RetainedStagingDirectories);
        var path = System.IO.Path.Combine(fx.Staging.Path, "prep-001");
        Assert.True(Directory.Exists(path));
        Assert.Empty(Directory.GetFiles(path));
        Assert.All(read.Snapshot.Allocations.Where(item => item.Kind == AllocationKind.Staging), item => Assert.False(item.Confirmed));
        var competitor = new OwnershipJournal(new JournalFileStorage(), fx.JournalDir.Path);
        Assert.False(competitor.TryAcquireWriter(out _, out _));
        outcome.Owner!.Kill(entireProcessTree: false);
        Assert.True(fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5)).Exited);
        Assert.True(fx.Journal.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
        using (takeover!)
        {
            Assert.Equal(fx.Journal.ReadLatest().Snapshot!.Sequence, takeover!.BaselineSequence);
        }
    }
    [Fact]
    public void ScopeDisposalDoesNotCloseReusedOwnerPidfd()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x74, 1024), null, null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest("after-copy"), CancellationToken.None);
        Assert.Equal("after-copy", outcome.Barrier);
        var owner = outcome.Owner!;
        owner.Kill(entireProcessTree: false);
        Assert.True(fx.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(5)).Exited);

        var field = typeof(M1RecoveryScope).GetField("_retainedOwnerPidFds", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var pidfds = (Dictionary<int, SafeHandle>)field!.GetValue(fx.Scope)!;
        var retained = pidfds[owner.Id];
        var descriptor = retained.DangerousGetHandle().ToInt32();
        retained.Dispose();

        var sentinel = open("/dev/null", 0);
        Assert.Equal(descriptor, sentinel);
        fx.Scope.Dispose();
        Assert.True(PreparationInterop.IsValidFd(sentinel), "scope disposal closed an unrelated descriptor reusing its pidfd number");
        PreparationInterop.CloseFd(sentinel);
    }
    [Theory]
    [InlineData("after-intent", "AllocationIntent", 0, 0)]
    [InlineData("after-domain-retained", "AllocationIntent", 1, 0)]
    [InlineData("after-registered", "AllocationRegistered", 1, 0)]
    [InlineData("after-staging-accounted", "AllocationRegistered", 1, 1)]
    [InlineData("mid-staging", "AllocationRegistered", 1, 1)]
    [InlineData("after-copy", "AllocationRegistered", 1, 1)]
    [InlineData("after-prepared-before-mint", "Prepared", 1, 1)]
    public void RealOwnerDeathAtPreparationBarrierLeavesRecoverableBoundary(
        string barrier,
        string expectedStage,
        int expectedDomains,
        int expectedStagingDirectories)
    {
        SkipUnlessUsable();
        using var fx = new Fixture(
            Bytes(0x72, 2048),
            barrier is "mid-staging" or "after-copy" ? Bytes(0x73, 200_000) : null,
            null);
        var outcome = fx.Scope.StartOwnerPreparation(fx.OwnerRequest(barrier), CancellationToken.None);

        Assert.Equal(barrier, outcome.Barrier);
        Assert.False(outcome.Completed);
        Assert.NotNull(outcome.Owner);
        if (barrier == "after-prepared-before-mint")
        {
            Assert.NotNull(outcome.Description);
        }
        else
        {
            Assert.Null(outcome.Description);
        }

        outcome.Owner!.Kill(entireProcessTree: false);
        var exit = fx.Scope.WaitForOwner(outcome.Owner, TimeSpan.FromSeconds(5));
        Assert.True(exit.Exited, exit.Error);

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(expectedStage, read.Snapshot!.Stage.ToString());
        Assert.Equal(expectedDomains, fx.Scope.RetainedRecoveryLeases.Count);
        Assert.Equal(expectedStagingDirectories, fx.Scope.RetainedStagingDirectories.Count);
        if (barrier == "after-staging-accounted")
        {
            var directoryEntry = Assert.Single(read.Snapshot.Allocations, item => item.Kind == AllocationKind.StagingDirectory);
            Assert.True(directoryEntry.Confirmed);
            Assert.True(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
            Assert.Empty(Directory.GetFiles(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
            Assert.All(read.Snapshot.Allocations.Where(item => item.Kind == AllocationKind.Staging), item => Assert.False(item.Confirmed));
        }
        if (barrier == "mid-staging")
        {
            Assert.True(Assert.Single(read.Snapshot.Allocations, item => item.Name == "tool-000.staged").Confirmed);
            Assert.False(Assert.Single(read.Snapshot.Allocations, item => item.Name == "input-001.staged").Confirmed);
        }
        if (expectedDomains == 1)
        {
            var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
            Assert.True(recovery.AuthorityAlive);
            Assert.True(recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new BarrierClock()).Stopped);
            Assert.True(recovery.RemoveIfIdentityMatches().Removed);
        }

        Assert.True(fx.Journal.TryAcquireWriter(out var takeover, out var takeoverError), takeoverError);
        using (takeover!)
        {
            Assert.Equal(read.Snapshot.Sequence, takeover!.BaselineSequence);
        }
    }

    // G2-03 1A: no journal-free production allocation path exists.
    [Fact]
    public void RawDomainAllocationRequiresScopedIntent()
    {
        var scopeType = typeof(M1RecoveryScope);
        Assert.Null(scopeType.GetMethod("AllocateDomain", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.Null(scopeType.GetMethod("AllocateDomainUnderTransaction", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.Null(scopeType.GetMethod("AllocatePreparation", BindingFlags.NonPublic | BindingFlags.Instance));

        var create = typeof(CgroupInterop).GetMethod(
            "TryCreateDomain", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(create);
        Assert.Equal(typeof(ScopedAllocationContext), create!.GetParameters()[0].ParameterType);

        // No context without a live writer and an acknowledged durable intent.
        Assert.False(ScopedAllocationContext.TryCreate(
            0, null, null, null, RecoveryTestSupport.Execution, RecoveryTestSupport.Lease, Boot(), out var none, out _));
        Assert.Null(none);

        var journal = new OwnershipJournal(new FakeJournalStorage(), "/intent");
        Assert.True(journal.TryAcquireWriter(out var writer, out var error), error);
        Assert.False(ScopedAllocationContext.TryCreate(
            0, journal, writer, null, RecoveryTestSupport.Execution, RecoveryTestSupport.Lease, Boot(), out _, out _));
        var intent = RecoveryTestSupport.Snapshot(
            stage: PreparationStage.AllocationIntent,
            allocations: Array.Empty<AllocationEntry>(),
            bootId: Boot());
        Assert.True(journal.WriteNext(writer!, intent).Ok);
        Assert.False(ScopedAllocationContext.TryCreate(
            0, journal, writer, intent with { ExecutionId = Guid.NewGuid() },
            RecoveryTestSupport.Execution, RecoveryTestSupport.Lease, Boot(), out _, out _));
        Assert.False(ScopedAllocationContext.TryCreate(
            0, journal, writer, intent with { DomainName = "another-domain" },
            RecoveryTestSupport.Execution, RecoveryTestSupport.Lease, Boot(), out _, out _));
        Assert.True(ScopedAllocationContext.TryCreate(
            0, journal, writer, intent, RecoveryTestSupport.Execution, RecoveryTestSupport.Lease, Boot(), out var ctx, out _));
        writer!.Dispose();
        Assert.False(ctx!.IsLive);
    }

    // G2-08: a real unconfirmed staging directory stays untouched debt and
    // reconciliation does not improve to Confirmed without new evidence.
    [Fact]
    public void UnconfirmedDirectoryRemainsUntouchedAndDebtIsIdempotent()
    {
        SkipUnlessUsable();
        using var staging = new TempDir();
        var dirPath = System.IO.Path.Combine(staging.Path, "prep-001");
        Directory.CreateDirectory(dirPath);
        File.WriteAllBytes(System.IO.Path.Combine(dirPath, "tool-000.staged"), Bytes(0x11, 100));
        var record = RecoveryTestSupport.Snapshot(allocations: new[]
        {
            new AllocationEntry(AllocationKind.StagingDirectory, "prep-001", 0, Array.Empty<byte>(), 0, 0, false),
        });
        var lease = new DomainLease(new FakeDomainBackend { Populated = true, KillAccepted = true }, RecoveryTestSupport.Identity());
        using var journalDir = new TempDir();
        var journal = new OwnershipJournal(new JournalFileStorage(), journalDir.Path);
        Assert.True(journal.TryAcquireWriter(out var writer, out var error), error);
        using (writer)
        {
            var reconciler = new Reconciler(new BarrierClock(), TimeSpan.FromSeconds(5));
            var first = reconciler.Reconcile(record, "boot-1", lease, null, journal, writer);
            Assert.NotEqual(ReconcileOutcome.CleanupConfirmed, first.Outcome);
            Assert.Equal(ComponentState.Unknown, Assert.Single(first.Components!, c => c.Component == CleanupComponents.Staging).State);
            Assert.True(Directory.Exists(dirPath));
            Assert.True(File.Exists(System.IO.Path.Combine(dirPath, "tool-000.staged")));

            var second = reconciler.Reconcile(journal.ReadLatest().Snapshot!, "boot-1", lease, null, journal, writer);
            Assert.NotEqual(ReconcileOutcome.CleanupConfirmed, second.Outcome);
            Assert.True(Directory.Exists(dirPath));
        }
    }

    private sealed class ScopeCleaner : IAllocationCleaner
    {
        private readonly int _parentFd;
        private readonly SafeHandle _subdir;

        public ScopeCleaner(int parentFd, int subdirFd)
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

    private sealed class BarrierClock : IDomainClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();

        public long NowNanoseconds => _watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);

        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }
}
