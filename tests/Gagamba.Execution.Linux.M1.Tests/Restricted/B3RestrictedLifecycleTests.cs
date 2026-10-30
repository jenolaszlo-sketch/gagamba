// RC2B-03 B3 real-owner integration tests. They compose the authoritative
// owner transaction (delegated cgroup, staging copy, owner-local mint, HELD
// release arbiter and exactly one RELEASE write) with the fixed experimental
// fixture and, where the installation closure is present, with the real B2
// recovery process. Every claim is derived from a real process, durable
// journal, coordinator status frame, kernel observation or fixture output;
// each test records which basis it used in its assertions.
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Restricted;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Contracts;
using Gagamba.Execution.Restricted.Control;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Restricted;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[Trait("Category", "ProcessGlobalReaping")]
public sealed class B3RestrictedLifecycleTests
{
    private static readonly byte[] InputBytes = Bytes(0x45, 64);

    private static string? DelegatedParent => Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP");

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Gagamba.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Gagamba repository root not found");
    }

    private static string Artifact(string relative) => Path.Combine(RepositoryRoot(), relative);

    private static string? LocateOwnerDll()
    {
        var configured = Environment.GetEnvironmentVariable("GAGAMBA_OWNER_DLL");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var fullPath = Path.GetFullPath(configured);
            return File.Exists(fullPath) ? fullPath : null;
        }

        foreach (var configuration in new[] { "Release", "Debug" })
        {
            var candidate = Artifact(Path.Combine("src", "Gagamba.Execution.Linux.M1.Owner", "bin",
                configuration, "net10.0", "Gagamba.Execution.Linux.M1.Owner.dll"));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void SkipUnlessQualified()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
        }

        if (Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP") is not { Length: > 0 } parent
            || !Directory.Exists(parent))
        {
            Assert.Skip("Unavailable: GAGAMBA_DELEGATED_CGROUP not set; B3 requires an operator-supplied delegated parent");
        }

        if (RealUid == 0)
        {
            Assert.Skip("Unqualified: test process is root; the delegated topology requires uid != 0");
        }

        if (CapEff != "0000000000000000")
        {
            Assert.Skip($"Unqualified: effective capabilities are not empty (CapEff={CapEff})");
        }

        foreach (var artifact in new[]
                 {
                     "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture",
                     "artifacts/linux-m1/wp04/seccomp-filter.bin",
                     "artifacts/linux-m1/b3/b3-release-coordinator",
                 })
        {
            if (!File.Exists(Artifact(artifact)))
            {
                Assert.Skip($"Unavailable: missing artifact {artifact}");
            }
        }

        if (LocateOwnerDll() is null)
        {
            Assert.Skip("Unavailable: owner executable missing from the test build");
        }
    }

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
    private static string CapEff => StatusField("CapEff");

    private static string Boot() => CgroupInterop.TryReadBootId(out var id, out _) ? id : "unknown";

    private static string Sha256Hex(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static long MonotonicNowNanoseconds()
    {
        var ticks = Stopwatch.GetTimestamp();
        return ticks / Stopwatch.Frequency * 1_000_000_000L
            + ticks % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency;
    }

    private static byte[] Bytes(byte value, int length)
    {
        var bytes = new byte[length];
        Array.Fill(bytes, value);
        return bytes;
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "gagamba-b3-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            // Qualification runs may retain the directory (bounded evidence
            // files only) for independent inspection.
            if (Environment.GetEnvironmentVariable("GAGAMBA_B3_KEEP_EVIDENCE") != "1")
            {
                try { Directory.Delete(Path, true); } catch { }
            }

            GC.SuppressFinalize(this);
        }
    }

    private sealed class Fixture : IDisposable
    {
        public TempDir Source { get; } = new();
        public TempDir Staging { get; } = new();
        public TempDir JournalSpace { get; } = new();
        public TempDir Evidence { get; } = new();
        public byte[] Tool { get; }
        public byte[] Coordinator { get; }
        public byte[] Filter { get; }
        public string OwnerDll { get; }
        public Guid Exec { get; } = Guid.NewGuid();
        public Guid LeaseGeneration { get; } = Guid.NewGuid();
        public Guid Issuer { get; } = Guid.NewGuid();
        public Guid StoreGeneration { get; } = Guid.NewGuid();
        public string ChildName { get; } = "b3-" + Guid.NewGuid().ToString("N");

        public Fixture()
        {
            OwnerDll = LocateOwnerDll() ?? throw new InvalidOperationException("owner executable missing");
            Tool = File.ReadAllBytes(Artifact("tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture"));
            Coordinator = File.ReadAllBytes(Artifact("artifacts/linux-m1/b3/b3-release-coordinator"));
            Filter = File.ReadAllBytes(Artifact("artifacts/linux-m1/wp04/seccomp-filter.bin"));
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "tool"), Tool);
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "input"), InputBytes);
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "coordinator"), Coordinator);
            File.WriteAllBytes(System.IO.Path.Combine(Source.Path, "filter"), Filter);
        }

        public B3FixtureProfile Profile(long expiresLead = 60_000_000_000L) => new(
            OwnerDll, Sha256Hex(File.ReadAllBytes(OwnerDll)),
            DelegatedParent!, Source.Path, Staging.Path, JournalSpace.Path,
            "tool", "input", "coordinator", "filter",
            Sha256Hex(Tool), Sha256Hex(InputBytes), Sha256Hex(Coordinator),
            "enrolled-tool", "enrolled-input",
            16UL * 1024 * 1024, 128, expiresLead);

        public void Dispose()
        {
            try
            {
                var child = System.IO.Path.Combine(DelegatedParent!, ChildName);
                if (Directory.Exists(child))
                {
                    try { File.WriteAllText(System.IO.Path.Combine(child, "cgroup.kill"), "1"); } catch { }
                    try { Directory.Delete(child); } catch { }
                }
            }
            catch { }

            Source.Dispose();
            Staging.Dispose();
            JournalSpace.Dispose();
            Evidence.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    // ---- scope-level fixture: the authoritative transaction driven directly ---

    private sealed class ScopeFixture : IDisposable
    {
        private readonly Fixture _fixture = new();
        public M1RecoveryScope Scope { get; } = new();
        public OwnershipJournal Journal { get; }
        public CopyPlan Plan { get; }
        public Fixture Raw => _fixture;
        public Guid Exec => _fixture.Exec;
        public Guid LeaseGeneration => _fixture.LeaseGeneration;
        public Guid Issuer => _fixture.Issuer;
        public Guid StoreGeneration => _fixture.StoreGeneration;
        public string ChildName => _fixture.ChildName;

        public ScopeFixture()
        {
            Assert.True(Scope.TryInitialize().Ok);
            Assert.True(Scope.TryOpenDelegatedParent(DelegatedParent!).Ok, "delegated parent refused");
            Journal = new OwnershipJournal(new JournalFileStorage(), _fixture.JournalSpace.Path);
            var objects = new List<CopyObjectPlan>
            {
                new("tool", "tool", 4UL * 1024 * 1024, null),
                new("input", "input", 4UL * 1024 * 1024, null),
                new("coordinator", "coordinator", 4UL * 1024 * 1024, null),
                new("filter", "filter", 4UL * 1024 * 1024, null),
            };
            Plan = new CopyPlan(_fixture.Source.Path, _fixture.Staging.Path, "prep-001", objects,
                16UL * 1024 * 1024, 128, Array.Empty<KeyValuePair<string, string>>(), _ => true, null);
        }

        public OwnerPreparationRequest Request(string? barrier = null, long expiresLead = 60_000_000_000L,
            bool enableHeldCoordinator = true)
        {
            Assert.True(Journal.TryAcquireWriter(out var writer, out var error), error);
            return new(_fixture.OwnerDll, ChildName, Exec, LeaseGeneration, Boot(), Plan, Journal, writer!,
                Issuer, StoreGeneration,
                staged =>
                {
                    var baseline = Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests.SampleManifest();
                    var closure = staged.Select(item => new ClosureObject(
                        item.Role, item.Sha256, item.Length,
                        item.Role == "filter" ? "fd:6" : "/" + item.Role,
                        item.Role == "filter" ? "p07-seccomp-filter" : "static-test-object"));
                    var manifest = new RestrictedManifest(
                        RestrictedManifest.CurrentSchemaVersion,
                        RestrictedManifest.ImplementationIdentifier,
                        baseline.Authority,
                        baseline.Resources,
                        new ClosureIdentity(closure, new[]
                        {
                            "bubblewrap-0.11.1:" + HelperInstallation.PinnedSha256,
                        }),
                        baseline.Invocation,
                        baseline.Channels);
                    return (manifest, new PreparationIdentity(Exec, Issuer, StoreGeneration, Guid.NewGuid(),
                        ManifestCodec.ComputeDigestBytes(manifest)));
                },
                MonotonicNowNanoseconds() + expiresLead,
                barrier,
                EnableHeldCoordinator: enableHeldCoordinator);
        }

        public OwnerPreparationOutcome Start(string? barrier = null, long expiresLead = 60_000_000_000L)
        {
            var outcome = Scope.StartOwnerPreparation(Request(barrier, expiresLead), CancellationToken.None);
            Assert.True(outcome.Ok, outcome.Error);
            Assert.True(outcome.Held, "the owner must establish HELD");
            Assert.True(outcome.NoTargetInitializationMarkerAbsent, "the pre-HELD marker oracle must hold");
            return outcome;
        }

        public OwnershipSnapshot Latest() => Journal.ReadLatest().Snapshot!;

        public void Dispose()
        {
            Scope.Dispose();
            _fixture.Dispose();
        }
    }

    private static readonly ImmutableArray<byte> Correlation =
        ImmutableArray.Create(Enumerable.Repeat((byte)0x5a, 32).ToArray());

    // ---- tests -----------------------------------------------------------------

    [Fact]
    public async Task B3B1OperationEstablishesHeldThroughTheRealOwnerAndCleansUp()
    {
        SkipUnlessQualified();
        using var fixture = new Fixture();
        var creation = B3Qualification.TryCreate(fixture.Profile());
        Assert.NotNull(creation.Operation);
        Assert.Equal(RestrictedRefusalReason.None, creation.Reason);
        var operation = creation.Operation!;

        var prepared = Assert.IsType<PreparedRestrictedExecution>(
            (await operation.PrepareAsync(new("enrolled-tool", "enrolled-input"),
                waitCancellation: TestContext.Current.CancellationToken)).Preparation);
        File.WriteAllText(System.IO.Path.Combine(fixture.Evidence.Path, "b3-preparation.json"),
            System.Text.Json.JsonSerializer.Serialize(new
            {
                prepared.Reference.ExecutionId,
                prepared.Reference.IssuerId,
                prepared.Reference.GenerationId,
                prepared.Reference.PreparationId,
                ManifestSha256 = Convert.ToHexString(prepared.Reference.ManifestSha256.AsSpan()),
                prepared.Deadlines.AcceptedAtNs,
                prepared.Deadlines.PreparationExpiresAtNs,
                prepared.Deadlines.SetupDeadlineNs,
                prepared.Deadlines.OverallDeadlineNs,
                prepared.Deadlines.ClockId,
                BootId = prepared.Deadlines.BootId,
                prepared.Deadlines.EpochId,
                prepared.Deadlines.FrequencyHz,
                DeadlineGenerationId = prepared.Deadlines.GenerationId,
                EstablishmentSha256 = Convert.ToHexString(prepared.Establishment.Sha256.AsSpan()),
                EstablishmentBytes = prepared.Establishment.Bytes.Length,
            }));
        // Owner-origin identity: non-empty complete identity and the exact digest.
        Assert.NotEqual(Guid.Empty, prepared.Reference.ExecutionId);
        Assert.NotEqual(Guid.Empty, prepared.Reference.PreparationId);
        Assert.NotEqual(Guid.Empty, prepared.Reference.GenerationId);
        Assert.True(RestrictedManifestCodec.VerifySha256(prepared.Manifest,
            prepared.Reference.ManifestSha256.AsSpan()));
        // Original deadline set: strict owner-origin ordering and identity.
        Assert.True(prepared.Deadlines.AcceptedAtNs > 0);
        Assert.True(prepared.Deadlines.SetupDeadlineNs > prepared.Deadlines.AcceptedAtNs);
        Assert.True(prepared.Deadlines.OverallDeadlineNs > prepared.Deadlines.SetupDeadlineNs);
        Assert.True(prepared.Deadlines.PreparationExpiresAtNs > prepared.Deadlines.AcceptedAtNs);
        Assert.Equal("owner-monotonic-stopwatch-v1", prepared.Deadlines.ClockId);
        Assert.Equal(prepared.Reference.GenerationId, prepared.Deadlines.GenerationId);
        Assert.NotEqual(Guid.Empty, prepared.Deadlines.BootId);
        Assert.NotEqual(Guid.Empty, prepared.Deadlines.EpochId);
        Assert.Equal((ulong)Stopwatch.Frequency, prepared.Deadlines.FrequencyHz);
        // Owner-origin establishment receipt (the coordinator HELD frame the
        // owner validated before HELD was accepted).
        Assert.True(prepared.Establishment.Bytes.Length >= CoordinatorProtocol.HeaderBytes);
        Assert.Equal(32, prepared.Establishment.Sha256.Length);
        Assert.Equal("GGM1CTL\0", Encoding.ASCII.GetString(prepared.Establishment.Bytes.AsSpan(0, 8)));
        // Nothing was released and no target output exists while HELD.
        var heldObservation = await operation.ObserveAsync(waitCancellation: TestContext.Current.CancellationToken);
        Assert.Null(heldObservation.Release);
        Assert.Null(heldObservation.Exec);

        await operation.DisposeAsync();
        var cleanup = (await operation.ObserveAsync(waitCancellation: TestContext.Current.CancellationToken)).Cleanup;
        Assert.Equal(RestrictedEvidenceState.Confirmed, cleanup.DomainEmpty);
        Assert.Equal(RestrictedEvidenceState.Confirmed, cleanup.Reaped);
        Assert.Equal(RestrictedEvidenceState.Confirmed, cleanup.StagingRemoved);
        Assert.Equal(RestrictedEvidenceState.Confirmed, cleanup.JournalFinalized);
        Assert.Equal(RestrictedEvidenceState.Confirmed, cleanup.State);
    }

    [Fact]
    public void B3ScopeReleaseExecutesTheFixedFixtureExactlyOnceWithTrustedNativeOutcome()
    {
        SkipUnlessQualified();
        using var fx = new ScopeFixture();
        var held = fx.Start();

        var release = fx.Scope.ReleaseHeld(held, Correlation);
        Assert.True(release.Accepted, release.Error);
        Assert.NotNull(release.Decision);
        Assert.Equal(ReleaseDecisionKind.Released, release.Decision!.Kind);
        Assert.Equal(DeliveryResult.Delivered, release.Decision.Delivery);
        Assert.True(release.Decision.DurableCommitted, release.Decision.Reason);
        Assert.Equal(ReleaseCorrelationBinding.Accepted, release.Decision.Binding);

        // Trusted-native outcome: coordinator exec confirmation, target raw wait
        // status, domain population and the bounded fixture output digests.
        Assert.NotNull(release.Outcome);
        var outcome = release.Outcome!;
        Assert.Equal(RestrictedOutcome.Succeeded, outcome.Outcome);
        Assert.Equal(ExecTag.Confirmed, outcome.Exec);
        Assert.Equal(RootKind.Exited, outcome.Root);
        Assert.Equal(0, outcome.ExitCode);
        Assert.True(outcome.RawWaitPresent);
        Assert.Equal(0, outcome.RawWaitStatus);
        Assert.True(outcome.WrapperExited);
        Assert.True(outcome.DomainKnown && !outcome.DomainPopulated, "the domain must be empty after the target exits");
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes("gagamba-m1-fixture: useful-work\n")),
            outcome.StdoutSha256);
        Assert.Equal(SHA256.HashData(Encoding.UTF8.GetBytes("gagamba-m1-fixture: ctor-marker\n")),
            outcome.StderrSha256);
        Assert.True(outcome.ObservedAtNanoseconds > 0);

        // Durable release record + coordinator-derived exec/root observations.
        var snapshot = fx.Latest();
        Assert.NotNull(snapshot.Release);
        Assert.Equal(ReleaseTag.Committed, snapshot.Release!.Tag);
        Assert.Equal(DeliveryResult.Delivered, snapshot.Release.DeliveryResult);
        Assert.Null(snapshot.Stop);
        Assert.Equal(ExecTag.Confirmed, snapshot.Exec!.Tag);
        Assert.Equal(ExecBasis.CoordinatorFrame, snapshot.Exec.Basis);
        Assert.Equal(RootKind.Exited, snapshot.Root!.Kind);
        Assert.Equal(0, snapshot.Root.ExitCode);

        // At most one physical RELEASE: the retained arbiter returns Duplicate
        // and the journal carries exactly one release record.
        var duplicate = fx.Scope.ReleaseHeld(held, Correlation);
        Assert.True(duplicate.Accepted);
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Decision!.Kind);
        Assert.Null(duplicate.Outcome);
        Assert.Equal(ReleaseTag.Committed, fx.Latest().Release!.Tag);

        var abort = fx.Scope.AbortHeld(held);
        Assert.True(abort.Accepted, abort.Error);
        Assert.True(abort.OwnerExited, abort.Error);
        Assert.True(abort.ReleaseAttempted, "the abort must report the earlier physical release attempt");
        Assert.True(abort.CleanupRequested, abort.Error);
    }

    [Fact]
    public void B3StopBeforeReleaseConsumesCapabilityWithoutAnyReleaseWrite()
    {
        SkipUnlessQualified();
        using var fx = new ScopeFixture();
        var held = fx.Start();

        var stop = fx.Scope.StopHeld(held, StopCauseKind.CallerStop);
        Assert.True(stop.Accepted, stop.Error);
        Assert.True(stop.Decision!.Accepted);
        Assert.True(stop.Decision.DurableStop, stop.Decision.Reason);
        Assert.False(stop.Decision.ReleaseAlreadyCommitted);

        var release = fx.Scope.ReleaseHeld(held, Correlation);
        Assert.True(release.Accepted, release.Error);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Decision!.Kind);
        // Stop won before any durable intent: the refusal is NotCommitted and
        // the capability stays irreversibly consumed (C2).
        Assert.Equal(ReleaseDisposition.NotCommitted, release.Decision.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, release.Decision.Delivery);
        Assert.Equal(RestrictedRefusalReason.StopRequested, release.Decision.RefusalReason);
        Assert.Null(release.Outcome);
        var repeated = fx.Scope.ReleaseHeld(held, Correlation);
        Assert.Equal(ReleaseDecisionKind.Duplicate, repeated.Decision!.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, repeated.Decision.DurableRelease);

        var snapshot = fx.Latest();
        Assert.Null(snapshot.Release);
        Assert.NotNull(snapshot.Stop);
        Assert.Equal(StopCauseKind.CallerStop, snapshot.Stop!.Cause);
        Assert.Null(snapshot.Exec);
        Assert.Null(snapshot.Root);

        var abort = fx.Scope.AbortHeld(held);
        Assert.True(abort.Accepted, abort.Error);
        Assert.True(abort.OwnerExited, abort.Error);
        Assert.False(abort.ReleaseAttempted, "stop won before any release attempt");
        Assert.True(abort.NoTargetInitializationMarkerAbsent,
            "the fixture constructor marker must never appear when stop wins before release");
    }

    [Fact]
    public void B3BarrierStopWinsTheReleaseArbitrationWithoutPhysicalWrite()
    {
        SkipUnlessQualified();
        using var fx = new ScopeFixture();
        var held = fx.Start("release-after-intent");

        HeldOwnerStopOutcome? stopDuringBarrier = null;
        var release = fx.Scope.ReleaseHeld(held, Correlation, (_, barrierName) =>
        {
            Assert.Equal("release-after-intent", barrierName);
            stopDuringBarrier = fx.Scope.StopHeld(held, StopCauseKind.CallerStop);
            Assert.True(fx.Scope.ContinueHeld(held), "the barrier continue must be deliverable");
        });
        Assert.NotNull(stopDuringBarrier);
        Assert.True(stopDuringBarrier!.Accepted, stopDuringBarrier.Error);
        Assert.True(stopDuringBarrier.Decision!.DurableStop);

        Assert.True(release.Accepted, release.Error);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Decision!.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, release.Decision.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, release.Decision.Delivery);
        Assert.Equal(RestrictedRefusalReason.StopRequested, release.Decision.RefusalReason);
        Assert.Null(release.Outcome);

        // The durable release INTENT exists (written before the gate) but was
        // never committed, the tail stop is durable and no exec evidence or
        // physical write followed it.
        var snapshot = fx.Latest();
        Assert.NotNull(snapshot.Release);
        Assert.Equal(ReleaseTag.Intent, snapshot.Release!.Tag);
        Assert.Equal(DeliveryResult.NotAttempted, snapshot.Release.DeliveryResult);
        Assert.NotNull(snapshot.Stop);
        Assert.Null(snapshot.Exec);
        Assert.Null(snapshot.Root);
        var abort = fx.Scope.AbortHeld(held);
        Assert.True(abort.Accepted, abort.Error);
        Assert.True(abort.NoTargetInitializationMarkerAbsent);
        Assert.False(abort.ReleaseAttempted);
    }

    [Fact]
    public void B3ReleaseWinsThenStopPreservesTheOriginalReleaseDecision()
    {
        SkipUnlessQualified();
        using var fx = new ScopeFixture();
        var held = fx.Start();

        var release = fx.Scope.ReleaseHeld(held, Correlation);
        Assert.True(release.Accepted, release.Error);
        Assert.Equal(ReleaseDecisionKind.Released, release.Decision!.Kind);

        var stop = fx.Scope.StopHeld(held, StopCauseKind.CallerStop);
        Assert.True(stop.Accepted, stop.Error);
        Assert.True(stop.Decision!.Accepted);
        Assert.True(stop.Decision.ReleaseAlreadyCommitted,
            "a stop submitted after the physical release must report the committed release");
        Assert.True(stop.Decision.DurableStop, stop.Decision.Reason);

        // The release record is not falsified and no second write exists.
        var snapshot = fx.Latest();
        Assert.Equal(ReleaseTag.Committed, snapshot.Release!.Tag);
        Assert.Equal(DeliveryResult.Delivered, snapshot.Release.DeliveryResult);
        Assert.NotNull(snapshot.Stop);
        var duplicate = fx.Scope.ReleaseHeld(held, Correlation);
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Decision!.Kind);
        Assert.Equal(ReleaseTag.Committed, fx.Latest().Release!.Tag);

        var abort = fx.Scope.AbortHeld(held);
        Assert.True(abort.Accepted, abort.Error);
        Assert.True(abort.ReleaseAttempted);
    }

    [Fact]
    public void B3ControllerChannelLossWhileHeldAbortsWithoutRelease()
    {
        SkipUnlessQualified();
        using var fx = new ScopeFixture();
        var held = fx.Start();
        var owner = held.Owner!;

        Assert.True(fx.Scope.CloseOwnerLifeline(owner), "the controller lifeline closure must be available");
        var exit = fx.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(10));
        Assert.True(exit.Exited, exit.Error);
        // The owner aborted autonomously; it could not acknowledge over the
        // closed channel, so the channel-loss abort exit codes apply.
        Assert.True(exit.ExitCode is 48 or 49, $"unexpected controller-loss abort exit {exit.ExitCode}");

        var snapshot = fx.Latest();
        Assert.Null(snapshot.Release);
        Assert.Null(snapshot.Stop);
        Assert.Null(snapshot.Exec);
        Assert.Equal(PreparationStage.Prepared, snapshot.Stage);
        var lease = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.True(lease.AuthorityAlive);
        var population = lease.ObservePopulation();
        Assert.True(population.Known && population.Empty,
            "the owner's controller-loss abort must stop the domain");
    }

    [Fact]
    public void B3OwnerDeathWhileHeldLeavesPreparedStateWithoutRelease()
    {
        SkipUnlessQualified();
        using var fx = new ScopeFixture();
        var held = fx.Start();
        var owner = held.Owner!;

        owner.Kill();
        var exit = fx.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(10));
        Assert.True(exit.Exited, exit.Error);
        Assert.NotEqual(0, exit.ExitCode);

        // The controller association is dead: release admits nothing.
        var release = fx.Scope.ReleaseHeld(held, Correlation);
        Assert.False(release.Accepted);
        var snapshot = fx.Latest();
        Assert.Equal(PreparationStage.Prepared, snapshot.Stage);
        Assert.Null(snapshot.Release);
        Assert.Null(snapshot.Exec);
        Assert.Null(snapshot.Root);
        var lease = Assert.Single(fx.Scope.RetainedRecoveryLeases);
        Assert.True(lease.AuthorityAlive, "recovery authority must survive owner death");
    }

    [Fact]
    public void B3HeldOverallDeadlineStopsWithoutControllerRequest()
    {
        SkipUnlessQualified();
        using var fx = new ScopeFixture();
        // Owner-derived deadlines: setup = min(accepted+10s, expires) and
        // overall = min(accepted+30s, expires) with a 12s expiry lead.
        var held = fx.Start(expiresLead: 12_000_000_000L);
        var owner = held.Owner!;

        var exit = fx.Scope.WaitForOwner(owner, TimeSpan.FromSeconds(25));
        Assert.True(exit.Exited, exit.Error);
        Assert.Equal(53, exit.ExitCode); // retained overall deadline ended the held lifetime
        var snapshot = fx.Latest();
        Assert.Null(snapshot.Release);
        Assert.Null(snapshot.Exec);
        Assert.Null(snapshot.Root);
        Assert.True(snapshot.Stop is not null || snapshot.Stage == PreparationStage.Prepared,
            "deadline stop retention or prepared state must be observable");
    }

    // ---- B2 recovery topology composition --------------------------------------

    private static string? B2InstallRoot()
    {
        var configured = Environment.GetEnvironmentVariable("GAGAMBA_B2_INSTALL");
        var root = !string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(configured)
            : Artifact(System.IO.Path.Combine("artifacts", "b2", "install"));
        return Directory.Exists(root) ? root : null;
    }

    private static void SkipUnlessB2Installation()
    {
        SkipUnlessQualified();
        if (B2InstallRoot() is null)
        {
            Assert.Skip("Unavailable: B2 installation closure missing (artifacts/b2/install or GAGAMBA_B2_INSTALL)");
        }
    }

    private static PinnedExecutable Pin(string folder, string entry)
    {
        var directory = Path.Combine(B2InstallRoot()!, folder);
        var names = new[]
        {
            entry + ".dll", entry + ".deps.json", entry + ".runtimeconfig.json",
            "Gagamba.Execution.Linux.M1.dll", "Gagamba.Execution.dll",
        };
        var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            using var stream = File.OpenRead(Path.Combine(directory, name));
            hashes[name] = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        return new PinnedExecutable(directory, hashes);
    }

    private static RecoveryConfiguration RecoveryConfig(string evidenceDirectory, string? delegatedParent)
    {
        return new RecoveryConfiguration(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Environment.ProcessId, (uint)RealUid, RandomNumberGenerator.GetBytes(32),
            Pin("recovery", "Gagamba.Execution.Linux.M1.Recovery"),
            Pin("owner", "Gagamba.Execution.Linux.M1.Owner"),
            evidenceDirectory, delegatedParent);
    }

    private static bool WaitForFile(string path, int seconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return false;
    }

    private static RecoverySnapshot? WaitForSnapshot(RecoveryConnection connection,
        Func<RecoverySnapshot, bool> predicate, int seconds)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            var snapshot = connection.Inspect();
            if (snapshot is not null && predicate(snapshot))
            {
                return snapshot;
            }

            Thread.Sleep(50);
        }

        return null;
    }

    [Fact]
    public void B3RecoveryTopologyRetainsItsDomainWithoutReleaseAuthority()
    {
        SkipUnlessB2Installation();
        using var evidence = new TempDir();
        using var connection = RecoveryConnection.Start(RecoveryConfig(evidence.Path, DelegatedParent!));
        try
        {
            var ready = connection.Inspect();
            Assert.NotNull(ready);
            Assert.False(ready!.ReleaseAuthority, "the recovery process must have no release authority");
            Assert.True(ready.Subreaper);
            Assert.NotEqual(0, ready.OwnerPid);
            Assert.Equal(1, ready.RetainedDomains);
            Assert.True(WaitForFile(System.IO.Path.Combine(evidence.Path, "ready.json"), 10));

            // The recovery process processes only the accepted B2 wire: stop,
            // bounded observation and identity-checked empty-domain removal.
            Assert.True(connection.Send(RecoveryMessage.Stop));
            var stopped = WaitForSnapshot(connection,
                snapshot => snapshot is { DomainRemoved: true, StopCount: >= 1 }, 15);
            Assert.NotNull(stopped);
            Assert.False(stopped!.ReleaseAuthority);
            Assert.Equal("KillWrittenEmptyObserved", stopped.NativeStop);
        }
        finally
        {
            if (!connection.Process.HasExited)
            {
                connection.Process.Kill();
                connection.Process.WaitForExit(5000);
            }

            connection.Dispose();
        }
    }

    [Fact]
    public void B3TopologySeparationDemonstrationUnrelatedRecoveryDeathDoesNotSuperviseThisOwner()
    {
        SkipUnlessB2Installation();
        using var fx = new ScopeFixture();
        using var evidence = new TempDir();
        using var connection = RecoveryConnection.Start(RecoveryConfig(evidence.Path, delegatedParent: null));
        var ready = connection.Inspect();
        Assert.NotNull(ready);
        Assert.False(ready!.ReleaseAuthority);
        var recoveryPid = ready.RecoveryPid;

        var held = fx.Start();
        var owner = held.Owner!;
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var exited, out _) && !exited);

        // The B2 recovery process dies; the transaction owner and the
        // controller's own association are unaffected.
        using (var recovery = Process.GetProcessById(recoveryPid))
        {
            recovery.Kill();
            recovery.WaitForExit(5000);
        }

        Assert.True(WaitForFile(System.IO.Path.Combine(evidence.Path, "owner-stop.json"), 10));
        Assert.True(fx.Scope.TryOwnerPidFdExited(owner, 0, out var stillRunning, out var pidfdError)
            && !stillRunning, pidfdError);
        connection.Dispose();

        var release = fx.Scope.ReleaseHeld(held, Correlation);
        Assert.True(release.Accepted, release.Error);
        Assert.Equal(ReleaseDecisionKind.Released, release.Decision!.Kind);
        Assert.Equal(RestrictedOutcome.Succeeded, release.Outcome!.Outcome);
        var abort = fx.Scope.AbortHeld(held);
        Assert.True(abort.Accepted, abort.Error);
        Assert.True(abort.ReleaseAttempted);
    }
}
