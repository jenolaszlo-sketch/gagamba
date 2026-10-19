// Independent WP05 release-path qualification probes. Review evidence, not
// product implementation: these tests compose the real production chain
// (delegated cgroup v2 ownership, staging copy-in, trusted Bubblewrap setup,
// the pinned production coordinator, the single-use publication store and the
// release arbiter) in the test process without the owner program, and drive
// the sole RELEASE write from the gated arbiter to the real control pipe.
// Bounded scope: no target fork/exec (WP10 boundary), no extra-bytes frame
// variant on the production reader, no capture/resource prerequisite work.
using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
public sealed class Wp05ReleasePathIndependentQualificationTests
{
    private static string? DelegatedParent => Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP");

    private const long SetupDeadlineLeadNanoseconds = 10_000_000_000L;
    private const long OverallDeadlineLeadNanoseconds = 30_000_000_000L;
    private const long ShortSetupDeadlineLeadNanoseconds = 3_500_000_000L;
    private const long ShortOverallDeadlineLeadNanoseconds = 6_000_000_000L;

    [Fact]
    public async Task HeldProductionCoordinatorAcceptsSingleArbitratedReleaseAndReachesPreExecBoundary()
    {
        SkipUnlessQualified();
        using var harness = new ProbeHarness(SetupDeadlineLeadNanoseconds, OverallDeadlineLeadNanoseconds);

        Assert.True(harness.Launch.Held, harness.Launch.Error);
            var execution = harness.Launch.Execution!;
            ClassifyHeldReport(execution.Report!);
            var identity = harness.Identity;

            var arbiter = new ReleaseArbiter(
                harness.Capability!,
                harness.Journal,
                harness.Writer,
                new ReviewerClock(),
                harness.Prerequisites(execution),
                execution);

        var decision = arbiter.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Released, decision.Kind);
        Assert.Equal(DeliveryResult.Delivered, decision.Delivery);
        Assert.True(decision.DurableCommitted, decision.Reason);
        Assert.False(decision.LiveExecutionExcluded);

        var duplicate = arbiter.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Kind);

        var finalRead = harness.Journal.ReadLatest();
        Assert.True(finalRead.Ok, finalRead.Error);
        Assert.Equal(ReleaseTag.Committed, finalRead.Snapshot!.Release!.Tag);
        Assert.Equal(DeliveryResult.Delivered, finalRead.Snapshot.Release.DeliveryResult);
        Assert.Null(finalRead.Snapshot.Stop);

        var reaped = execution.WrapperReaper.TryReap(5000, out var exitCode, out var reapError);
        Assert.True(reaped, reapError);
        Assert.Equal(0, exitCode);

        var drained = ProbeHarness.DrainStatus(execution, identity);
        Assert.False(drained.HadFrame, drained.Error);
        Assert.Null(drained.Observed);

        var abort = await harness.Launcher.AbortAsync(
            execution, harness.PrimaryLease!, CancellationToken.None);
        Assert.True(abort.Stopped, abort.Error);
        Assert.True(abort.WrapperReaped, abort.Error);
    }

    [Fact]
    public async Task ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport()
    {
        SkipUnlessQualified();
        using var harness = new ProbeHarness(SetupDeadlineLeadNanoseconds, OverallDeadlineLeadNanoseconds);

        Assert.True(harness.Launch.Held, harness.Launch.Error);
        var execution = harness.Launch.Execution!;
        ClassifyHeldReport(execution.Report!);
        var forged = new PreparationIdentity(
            harness.Identity.ExecutionId,
            harness.Identity.IssuerId,
            harness.Identity.GenerationId,
            Guid.NewGuid(),
            harness.Identity.ManifestDigest.ToArray());

        var arbiter = new ReleaseArbiter(
            harness.Capability!,
            harness.Journal,
            harness.Writer,
            new ReviewerClock(),
            harness.Prerequisites(execution),
            execution);

        var decision = arbiter.RequestRelease(
            new ReleaseRequest(forged, harness.SetupDeadline, harness.OverallDeadline));
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, decision.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, decision.Delivery);
        Assert.False(decision.DurableCommitted);
        Assert.True(harness.Capability!.IsLive, "a refused request must not consume the capability");

        var latest = harness.Journal.ReadLatest();
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(PreparationStage.Prepared, latest.Snapshot!.Stage);
        Assert.Null(latest.Snapshot.Release);

        var abort = await harness.Launcher.AbortAsync(
            execution, harness.PrimaryLease!, CancellationToken.None);
        Assert.True(abort.Stopped, abort.Error);
        Assert.True(abort.WrapperReaped, abort.Error);

        var reapedForged = execution.WrapperReaper.TryReap(5000, out var forgedExit, out var forgedReapError);
        Assert.True(reapedForged, forgedReapError);

        // Implementation-branch oracle remediation (2026-10-19): the accepted
        // abort contract does not fix one wrapper exit code. AbortAsync closes
        // control and then force-stops the domain before reaping; the wrapper
        // may exit via the coordinator's own fail(33) (133) or via this test's
        // SIGKILL stop (137). The original 139 expectation reflected the old
        // seccomp SIGSEGV and is no longer accepted. The oracle below retains
        // every security check: refused decision, no durable release, no
        // transport activity, live capability, unchanged prepared journal,
        // successful abort/reap/marker checks, and no unexplained exit code.
        var afterAbort = harness.Journal.ReadLatest();
        var oracleError = Wp05ForgedAbortOracle.Validate(
            new Wp05ForgedAbortOracle.DecisionState(
                decision.Kind, decision.DurableRelease, decision.Delivery, decision.DurableCommitted),
            harness.Capability!.IsLive,
            new Wp05ForgedAbortOracle.JournalState(
                afterAbort.Ok,
                afterAbort.Snapshot?.Stage ?? PreparationStage.None,
                afterAbort.Snapshot?.Release is not null,
                afterAbort.Snapshot?.Stop is not null,
                afterAbort.Error),
            new Wp05ForgedAbortOracle.AbortState(
                abort.Stopped, abort.WrapperReaped, abort.Error,
                abort.NoTargetInitializationMarkerAbsent),
            forgedExit);
        Assert.True(oracleError is null, oracleError);
    }

    [Fact]
    public async Task RealCoordinatorOverallExpiryWritesProtocolFailed30AndTheGateRefusesExpiredIntent()
    {
        SkipUnlessQualified();
        using var harness = new ProbeHarness(ShortSetupDeadlineLeadNanoseconds, ShortOverallDeadlineLeadNanoseconds);

        Assert.True(harness.Launch.Held, harness.Launch.Error);
        var execution = harness.Launch.Execution!;
        ClassifyHeldReport(execution.Report!);
        ProbeHarness.SleepToNanoseconds(harness.OverallDeadline + 300_000_000L);

        var reaped = execution.WrapperReaper.TryReap(5000, out var exitCode, out var reapError);
        Assert.True(reaped, reapError);
        Assert.Equal(130, exitCode);

        var observation = ProbeHarness.DrainStatus(execution, harness.Identity);
        Assert.True(observation.HadFrame, observation.Error);
        Assert.True(observation.Observed!.ProtocolFailed);
        Assert.Equal(30u, observation.Observed.FailureCode);
        Assert.Null(observation.Observed.Exec);

        var arbiter = new ReleaseArbiter(
            harness.Capability!,
            harness.Journal,
            harness.Writer,
            new ReviewerClock(),
            harness.Prerequisites(execution),
            execution);
        var decision = arbiter.RequestRelease(
            new ReleaseRequest(harness.Identity, harness.SetupDeadline, harness.OverallDeadline));
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, decision.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, decision.Delivery);
        Assert.False(decision.DurableCommitted);

        var latest = harness.Journal.ReadLatest();
        Assert.True(latest.Ok, latest.Error);
        Assert.Null(latest.Snapshot!.Release);
    }

    private static void ClassifyHeldReport(CoordinatorHeldReport report)
    {
        Assert.Equal(0u, report.Uid);
        Assert.Equal(0UL, report.CapEff);
        Assert.Equal(1u, report.NoNewPrivs);
        Assert.Equal(2u, report.SeccompMode);
    }

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

        if (!Directory.Exists(DelegatedParent!))
        {
            Assert.Skip("Unavailable: delegated parent path does not exist");
        }

        var root = RepositoryRoot();
        foreach (var artifact in new[]
                 {
                     "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture",
                     "artifacts/linux-m1/wp04/exec-coordinator",
                     "artifacts/linux-m1/wp04/seccomp-filter.bin",
                 })
        {
            if (!File.Exists(System.IO.Path.Combine(root, artifact)))
            {
                Assert.Skip($"Unavailable: missing artifact {artifact}");
            }
        }
    }

    private static int RealUid => int.Parse(StatusField("Uid").Split('\t')[0]);

    private static string CapEff => StatusField("CapEff");

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

    private static string Boot() => CgroupInterop.TryReadBootId(out var id, out _) ? id! : "unknown";

    private static long MonotonicNanoseconds()
    {
        var ticks = Stopwatch.GetTimestamp();
        return ticks / Stopwatch.Frequency * 1_000_000_000L
            + ticks % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency;
    }

    private sealed class ReviewerClock : ITransactionClock
    {
        public long NowNanoseconds => MonotonicNanoseconds();
    }

    private sealed record HeldAdmission(bool Held, HeldExecution? Execution, string Error);

    private sealed record StatusOutcome(bool HadFrame, CoordinatorStatusObservation? Observed, string Error);

    private sealed class ProbeHarness : IDisposable
    {
        public TempDirectory Source { get; } = new("gagamba-rel-src-");
        public TempDirectory Staging { get; } = new("gagamba-rel-stg-");
        public TempDirectory JournalSpace { get; } = new("gagamba-rel-jrn-");
        public OwnershipJournal Journal { get; private set; } = null!;
        public JournalWriterLease Writer { get; private set; } = null!;
        public DomainLease PrimaryLease { get; private set; } = null!;
        public DomainLease RecoveryLease { get; private set; } = null!;
        public PreparedTransaction Capability { get; private set; } = null!;
        public BubblewrapAdapter Launcher { get; } = new();
        public RestrictedManifest Manifest { get; private set; } = null!;
        public PreparationIdentity Identity { get; private set; } = null!;
        public HeldAdmission Launch { get; private set; } = new(false, null, "harness construction failed");
        public long SetupDeadline { get; private set; }
        public long OverallDeadline { get; private set; }
        internal string ChildName { get; } = "d-" + Guid.NewGuid().ToString("N")[..12];

        private readonly long _expiresAt;
        private readonly Guid _execId = Guid.NewGuid();
        private readonly Guid _leaseGeneration = Guid.NewGuid();
        private readonly Guid _issuerId = Guid.NewGuid();
        private readonly Guid _storeGenId = Guid.NewGuid();
        private readonly Guid _prepId = Guid.NewGuid();
        private readonly OwnerPreparationSpecification _specification;
        private readonly PreparedClosure _closure;
        private readonly SafeFileHandle _stagingParentAuthority = null!;
        private readonly SafeFileHandle _parentAuthority = null!;
        private bool _disposed;

        public ProbeHarness(long setupLead, long overallLead)
        {
            foreach (var temp in new[] { Source, Staging, JournalSpace })
            {
                Directory.CreateDirectory(temp.Path);
            }

            var artifacts = RepositoryRoot();
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "tool"), File.ReadAllBytes(
                System.IO.Path.Combine(artifacts, "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture")));
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "input"), new byte[64]);
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "coordinator"), File.ReadAllBytes(
                System.IO.Path.Combine(artifacts, "artifacts/linux-m1/wp04/exec-coordinator")));
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "filter"), File.ReadAllBytes(
                System.IO.Path.Combine(artifacts, "artifacts/linux-m1/wp04/seccomp-filter.bin")));

            Journal = new OwnershipJournal(new JournalFileStorage(), JournalSpace.Path);
            Assert.True(Journal.TryAcquireWriter(out var writer, out var writerError), writerError);
            Writer = writer!;

            var accepted = MonotonicNanoseconds();
            SetupDeadline = accepted + setupLead;
            OverallDeadline = accepted + overallLead;
            _expiresAt = accepted + 60_000_000_000L;

            var objects = new[]
            {
                new CopyObjectPlan("tool", "tool", 4UL * 1024 * 1024, null),
                new CopyObjectPlan("input", "input", 4UL * 1024 * 1024, null),
                new CopyObjectPlan("coordinator", "coordinator", 4UL * 1024 * 1024, null),
                new CopyObjectPlan("filter", "filter", 4UL * 1024 * 1024, null),
            };
            var environment = Array.Empty<KeyValuePair<string, string>>();

            var stagingErrno = PreparationInterop.OpenDirectory(Staging.Path, out var stagingFd);
            Assert.True(stagingErrno == 0 && stagingFd >= 0);
            Assert.True(PreparationInterop.StatFd(stagingFd, out var stagingStat) == 0);
            Assert.True(PreparationInterop.IsDirectory(stagingStat.Mode));
            _stagingParentAuthority = new SafeFileHandle((IntPtr)stagingFd, ownsHandle: true);

            var delegationErrno = PreparationInterop.OpenDirectory(DelegatedParent!, out var delegatedFd);
            Assert.True(delegationErrno == 0 && delegatedFd >= 0);
            Assert.True(CgroupInterop.TryVerifyDelegatedParent(delegatedFd, out var verifyError), verifyError);
            _parentAuthority = new SafeFileHandle((IntPtr)delegatedFd, ownsHandle: true);

            var copyPlan = new CopyPlan(
                Source.Path,
                Staging.Path,
                "prep-001",
                objects,
                16UL * 1024 * 1024,
                128,
                environment,
                Acknowledge: null,
                Hooks: null,
                StagingParentAuthority: _stagingParentAuthority);

            _specification = new OwnerPreparationSpecification(
                Writer.Directory,
                ChildName,
                _execId,
                _leaseGeneration,
                Boot(),
                _issuerId,
                _storeGenId,
                Source.Path,
                Staging.Path,
                stagingStat.Device,
                stagingStat.Inode,
                "prep-001",
                objects,
                16UL * 1024 * 1024,
                128,
                environment,
                _expiresAt,
                null,
                true,
                accepted);

            var intent = OwnerPreparationSpecificationCodec.BuildIntent(_specification);
            Assert.True(Journal.WriteNext(Writer, intent).Ok);

            Assert.True(ScopedAllocationContext.TryCreate(
                (int)_parentAuthority.DangerousGetHandle(), Journal, Writer, intent,
                _execId, _leaseGeneration, Boot(),
                out var context, out var contextError), contextError);

            Assert.True(CgroupInterop.TryCreateDomain(context!, ChildName,
                out var backend, out var domainIdentity, out var domainError), domainError);
            PrimaryLease = new DomainLease(backend!, domainIdentity!);
            RecoveryLease = new DomainLease(
                (CgroupBackend)backend!.DuplicateForRecovery(), domainIdentity!);

            var registered = MirrorRegistered(_specification, domainIdentity!);
            Assert.True(Journal.WriteNext(Writer, registered).Ok);

            Assert.True(PrimaryLease.TryInstallWp04Limits(out var limitsError), limitsError);

            var copy = SafeCopyIn.Prepare(copyPlan, LibcCopyHost.Instance, CancellationToken.None);
            Assert.True(copy.Succeeded, copy.Detail);
            _closure = copy.Closure!;

            var descriptions = _closure.Objects.Select(staged => new StagedObjectDescription(
                staged.Role, staged.Sha256, staged.Length, staged.Device, staged.Inode, staged.StagingName)).ToArray();
            Manifest = BuildManifest(descriptions);
            Identity = new PreparationIdentity(
                _execId, _issuerId, _storeGenId, _prepId, ManifestCodec.ComputeDigestBytes(Manifest));

            var prepared = registered with
            {
                Stage = PreparationStage.Prepared,
                Preparation = new RecordedPreparation(
                    Identity.ExecutionId, Identity.IssuerId, Identity.GenerationId,
                    Identity.PreparationId, Identity.ManifestDigest.ToArray()),
            };
            Assert.True(Journal.WriteNext(Writer, prepared).Ok);

            var store = new PreparedTransaction.PreparationStore(_issuerId, _storeGenId, new ReviewerClock());
            var publication = PreparationPublisher.Publish(
                store, _closure, Manifest, Identity, _expiresAt, _ => true, CancellationToken.None);
            Assert.True(publication.Published, publication.Detail);
            Capability = publication.Capability!;

            var held = Launcher.PrepareHeldAsync(
                    PrimaryLease, _closure, Manifest, Identity, SetupDeadline, OverallDeadline,
                    CancellationToken.None)
                .GetAwaiter().GetResult();
            Launch = new HeldAdmission(held.Held, held.Execution, held.Error);
        }

        internal ReleaseRequest Request => new(Identity, SetupDeadline, OverallDeadline);

        internal Func<ReleasePrerequisites> Prerequisites(HeldExecution execution) => () =>
            new ReleasePrerequisites(
                ResourcesReady: PrimaryLease!.TryReadWp04Limits(out _, out _),
                CaptureReady: execution.CoordinatorStatusReader is { IsClosed: false, IsInvalid: false },
                CallerAlive: Environment.ProcessId > 0,
                RecoveryAlive: RecoveryLease!.AuthorityAlive);

        internal static StatusOutcome DrainStatus(HeldExecution execution, PreparationIdentity identity)
        {
            var fd = (int)execution.CoordinatorStatusReader.DangerousGetHandle();
            var source = new FdStatusFrameSource(fd);
            var parser = new CoordinatorStatusReader(
                source, identity.ExecutionId, identity.PreparationId,
                identity.ManifestDigest.ToArray(), initialSequence: 2);
            var read = parser.TryReadNext(
                MonotonicNanoseconds() + 2_000_000_000L, out var observation, out var error);
            return new StatusOutcome(read, observation, error);
        }

        internal static void SleepToNanoseconds(long deadline)
        {
            while (MonotonicNanoseconds() < deadline)
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        internal static string DrainText(SafeFileHandle handle)
        {
            var buffer = new byte[4096];
            var builder = new System.Text.StringBuilder();
            while (true)
            {
                var read = NativeLaunchInterop.Read(
                    (int)handle.DangerousGetHandle(), buffer, (nuint)buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                builder.Append(System.Text.Encoding.UTF8.GetString(buffer, 0, (int)read));
                if (builder.Length > 8192)
                {
                    break;
                }
            }

            return builder.ToString();
        }

        private RestrictedManifest BuildManifest(IReadOnlyList<StagedObjectDescription> descriptions)
        {
            var baseline = Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests.SampleManifest();
            var closureObjects = descriptions.Select(item => new ClosureObject(
                item.Role,
                item.Sha256,
                item.Length,
                item.Role == "filter" ? "fd:6" : "/" + item.Role,
                item.Role == "filter" ? "p07-seccomp-filter" : "static-test-object"));
            return new RestrictedManifest(
                RestrictedManifest.CurrentSchemaVersion,
                RestrictedManifest.ImplementationIdentifier,
                baseline.Authority,
                baseline.Resources,
                new ClosureIdentity(
                    closureObjects, new[] { "bubblewrap-0.11.1:" + HelperInstallation.PinnedSha256 }),
                baseline.Invocation,
                baseline.Channels);
        }

        private static OwnershipSnapshot MirrorRegistered(
            OwnerPreparationSpecification specification, DomainIdentity domain)
        {
            var allocations = OwnerPreparationSpecificationCodec.PlannedAllocations(specification).ToList();
            for (var index = 0; index < allocations.Count; index++)
            {
                if (allocations[index].Kind == AllocationKind.Domain)
                {
                    allocations[index] = allocations[index] with
                    {
                        Device = domain.ChildDevice,
                        Inode = domain.ChildInode,
                        Confirmed = true,
                    };
                }
            }

            return new OwnershipSnapshot(
                OwnershipSnapshotCodec.LegacyVersion, 0, specification.ExecutionId, specification.BootId,
                domain.CgroupMountId, domain.CgroupMountFsType,
                domain.ParentDevice, domain.ParentInode, domain.ChildName,
                domain.ChildDevice, domain.ChildInode, specification.LeaseGeneration,
                OwnershipWriterKind.Owner, OwnershipCleanupState.NotStarted,
                PreparationStage.AllocationRegistered,
                specification.StagingParentDevice, specification.StagingParentInode, allocations, Preparation: null);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            var execution = Launch.Execution;
            try
            {
                if (execution is not null)
                {
                    _ = Launcher.AbortAsync(execution, PrimaryLease, CancellationToken.None)
                        .GetAwaiter().GetResult();
                }
            }
            catch { }

            try
            {
                var child = System.IO.Path.Combine(DelegatedParent ?? string.Empty, ChildName);
                if (child.Length > 0 && Directory.Exists(child))
                {
                    try { File.WriteAllText(System.IO.Path.Combine(child, "cgroup.kill"), "1"); } catch { }
                    Directory.Delete(child);
                }
            }
            catch { }

            PrimaryLease.Dispose();
            RecoveryLease.Dispose();
            Capability.Dispose();
            Writer.Dispose();
            _stagingParentAuthority.Dispose();
            _parentAuthority.Dispose();
            _closure.Dispose();
            Source.Dispose();
            Staging.Dispose();
            JournalSpace.Dispose();
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory(string prefix) =>
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
            GC.SuppressFinalize(this);
        }
    }
}
