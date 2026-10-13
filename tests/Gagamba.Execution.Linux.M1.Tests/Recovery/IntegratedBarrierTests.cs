// WP03 integrated-path death-barrier tests (G2-03): the real scope-driven
// allocation/preparation sequence with deterministic actor death at each
// barrier. Death is simulated by abandoning the operation at an exact
// point (operation-locals dropped exactly as OS reclaim would); recovery
// uses only durable journal state plus independently retained scope
// authority. Process-death itself is covered by the owner-death tests;
// Mint is in-process by necessity. No target execution.
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

    private static Action<string> DieAt(string target) => barrier =>
    {
        if (barrier == target)
        {
            throw new BarrierDeathException(barrier);
        }
    };

    private sealed class Fixture : IDisposable
    {
        public TempParent Temp = new();
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
            Store = new PreparedTransaction.PreparationStore(Issuer, Generation, Clock);
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
            JournalDir.Dispose();
            Staging.Dispose();
            Source.Dispose();
            Temp.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    [Fact]
    public void BarrierAfterIntentCreatesNothing()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x10, 1024), null, null);
        var ex = Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-intent")), CancellationToken.None));
        Assert.Equal("after-intent", ex.Barrier);

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.AllocationIntent, read.Snapshot!.Stage);
        Assert.Empty(Directory.GetDirectories(fx.Temp.Path, "d-*"));
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
    }

    [Fact]
    public void BarrierAfterCreateCleansDomainOnly()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x20, 1024), null, null);
        var sibling = System.IO.Path.Combine(fx.Temp.Path, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-create")), CancellationToken.None));

        // Only intent is durable; the domain exists with scope-retained authority.
        Assert.Equal(PreparationStage.AllocationIntent, fx.Journal.ReadLatest().Snapshot!.Stage);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.True(recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new BarrierClock()).Stopped);
        Assert.True(recovery.RemoveIfIdentityMatches().Removed);
        Assert.Empty(Directory.GetDirectories(fx.Temp.Path, "d-*"));
        Assert.True(Directory.Exists(fx.Staging.Path));
        Assert.True(Directory.Exists(sibling));
    }

    [Fact]
    public void BarrierAfterRegisteredCleansExactOriginals()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x30, 1024), null, null);
        var sibling = System.IO.Path.Combine(fx.Temp.Path, "sibling-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sibling);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-registered")), CancellationToken.None));

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.AllocationRegistered, read.Snapshot!.Stage);
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(recovery.Identity.ChildInode, read.Snapshot.DomainInode);
        Assert.Equal(recovery.Identity.ChildDevice, read.Snapshot.DomainDevice);
        Assert.True(recovery.StopAndAwaitEmpty(TimeSpan.FromSeconds(5), new BarrierClock()).Stopped);
        Assert.True(recovery.RemoveIfIdentityMatches().Removed);
        Assert.Empty(Directory.GetDirectories(fx.Temp.Path, "d-*"));
        Assert.True(Directory.Exists(sibling));
    }

    [Fact]
    public void BarrierMidCopyLeavesAccountedAndDebt()
    {
        SkipUnlessUsable();
        // Tool completes in one chunk; input dies on its third chunk.
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
        using var fx = new Fixture(tool, input, hooks);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(null), CancellationToken.None));

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        var toolEntry = Assert.Single(read.Snapshot!.Allocations, a => a.Kind == AllocationKind.Staging && a.Name == "tool-000.staged");
        Assert.True(toolEntry.Confirmed);
        var inputEntry = Assert.Single(read.Snapshot.Allocations, a => a.Kind == AllocationKind.Staging && a.Name == "input-001.staged");
        Assert.False(inputEntry.Confirmed);
        Assert.True(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged")));

        // Recovery with the scope-retained authority: exact tool removed,
        // unconfirmed partial preserved as debt, overall not Confirmed.
        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
        Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
        var cleaner = new ScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new BarrierClock(), TimeSpan.FromSeconds(5));
        var result = reconciler.Reconcile(read.Snapshot, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
        Assert.False(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "tool-000.staged")));
        Assert.True(File.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001", "input-001.staged")));
        PreparationInterop.CloseFd(subdirFd);
        PreparationInterop.CloseFd(parentFd);
    }

    [Fact]
    public void BarrierAfterCopyCleansExactClosure()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x50, 2048), Bytes(0x51, 4096), null);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-copy")), CancellationToken.None));

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.NotEqual(PreparationStage.Prepared, read.Snapshot!.Stage);
        Assert.All(read.Snapshot.Allocations.Where(a => a.Kind == AllocationKind.Staging), a => Assert.True(a.Confirmed));

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
        Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
        var cleaner = new ScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new BarrierClock(), TimeSpan.FromSeconds(5));
        var result = reconciler.Reconcile(read.Snapshot, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        PreparationInterop.CloseFd(subdirFd);
        PreparationInterop.CloseFd(parentFd);
    }

    [Fact]
    public void BarrierAfterPreparedNeverMints()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x60, 2048), null, null);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-prepared")), CancellationToken.None));

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        Assert.Equal(PreparationStage.Prepared, read.Snapshot!.Stage);
        Assert.NotNull(read.Snapshot.Preparation);

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
        Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
        var cleaner = new ScopeCleaner(parentFd, subdirFd);
        var reconciler = new Reconciler(new BarrierClock(), TimeSpan.FromSeconds(5));
        var result = reconciler.Reconcile(read.Snapshot, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        PreparationInterop.CloseFd(subdirFd);
        PreparationInterop.CloseFd(parentFd);
    }

    [Fact]
    public void FullIntegratedPathPublishesCapability()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x70, 2048), null, null);
        var outcome = fx.Scope.AllocatePreparation(fx.Request(null), CancellationToken.None);
        Assert.True(outcome.Published, outcome.Detail);
        Assert.NotNull(outcome.Capability);
        Assert.NotNull(outcome.PrimaryLease);
        outcome.PrimaryLease!.Dispose();
    }

    // G2-03 1B: the staging directory's identity is retained and durably
    // accounted at creation, before any object content is copied.
    [Fact]
    public void BarrierAfterStagingDirectoryRetainsAuthority()
    {
        SkipUnlessUsable();
        using var fx = new Fixture(Bytes(0x80, 1024), null, null);
        Assert.Throws<BarrierDeathException>(() =>
            fx.Scope.AllocatePreparation(fx.Request(DieAt("after-staging-dir")), CancellationToken.None));

        var read = fx.Journal.ReadLatest();
        Assert.True(read.Ok, read.Error);
        var subdir = Assert.Single(read.Snapshot!.Allocations, a => a.Kind == AllocationKind.StagingDirectory);
        Assert.True(subdir.Confirmed);
        Assert.NotEqual(0UL, subdir.Inode);
        Assert.NotEmpty(fx.Scope.RetainedStagingDirectories);
        Assert.Empty(Directory.GetFiles(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));

        var recovery = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.Equal(0, PreparationInterop.OpenDirectory(fx.Staging.Path, out var parentFd));
        Assert.Equal(0, PreparationInterop.OpenSubdirectory(parentFd, "prep-001", out var subdirFd));
        var cleaner = new ScopeCleaner(parentFd, subdirFd);
        var result = new Reconciler(new BarrierClock(), TimeSpan.FromSeconds(5))
            .Reconcile(read.Snapshot, Boot(), recovery, cleaner, fx.Journal, fx.Writer);
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
        Assert.False(Directory.Exists(System.IO.Path.Combine(fx.Staging.Path, "prep-001")));
        PreparationInterop.CloseFd(subdirFd);
        PreparationInterop.CloseFd(parentFd);
    }

    // G2-03 1A: no journal-free production allocation path exists.
    [Fact]
    public void RawDomainAllocationRequiresScopedIntent()
    {
        var scopeType = typeof(M1RecoveryScope);
        Assert.Null(scopeType.GetMethod("AllocateDomain", BindingFlags.NonPublic | BindingFlags.Instance));
        Assert.NotNull(scopeType.GetMethod("AllocateDomainUnderTransaction", BindingFlags.NonPublic | BindingFlags.Instance));

        var create = typeof(CgroupInterop).GetMethod(
            "TryCreateDomain", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(create);
        Assert.Equal(typeof(ScopedAllocationContext), create!.GetParameters()[0].ParameterType);

        // No context without a live writer and an acknowledged durable intent.
        Assert.False(ScopedAllocationContext.TryCreate(
            0, null, null, RecoveryTestSupport.Execution, RecoveryTestSupport.Lease, Boot(), out var none, out _));
        Assert.Null(none);

        var journal = new OwnershipJournal(new FakeJournalStorage(), "/intent");
        Assert.True(journal.TryAcquireWriter(out var writer, out var error), error);
        Assert.False(ScopedAllocationContext.TryCreate(
            0, journal, writer, RecoveryTestSupport.Execution, RecoveryTestSupport.Lease, Boot(), out _, out _));
        Assert.True(journal.WriteNext(writer!, RecoveryTestSupport.Snapshot()).Ok);
        Assert.True(ScopedAllocationContext.TryCreate(
            0, journal, writer, RecoveryTestSupport.Execution, RecoveryTestSupport.Lease, Boot(), out var ctx, out _));
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
