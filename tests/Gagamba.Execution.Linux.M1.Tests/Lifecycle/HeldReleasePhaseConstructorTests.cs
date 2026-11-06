using System.Reflection;
using System.Runtime.CompilerServices;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Installed;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Tests.Installation;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;
using Microsoft.Win32.SafeHandles;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Lifecycle;

// P3-R1: execute the real HELD constructor, then retain its actual arbiter and
// generation callback. Reflection supplies bounded native-free fixtures only;
// there is no new production injection seam or protected-installation claim.
public sealed class HeldReleasePhaseConstructorTests
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

    private static FieldInfo Field(Type type, string name) => type.GetField(name, Instance)!;
    private static T Read<T>(object target, string name) => (T)Field(target.GetType(), name).GetValue(target)!;
    private static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);

    private static OwnerAuthorityContext.Qualified Qualified()
    {
        var selection = InstalledResult.Qualified(
            InstalledManifest.Parse(InstalledMetadataTests.Vector()), "Q1", new string('b', 64));
        // Use the production retention factory. Only the protected acquisition
        // is synthetic: a golden qualified tuple is put into LoadedInstallation.
        var loaded = (LoadedInstallation)typeof(LoadedInstallation)
            .GetConstructors(Instance).Single().Invoke([selection]);
        return OwnerAuthorityContext.Qualified.FromVerified(loaded);
    }

    private static OwnerAuthorityContext Validation() => (OwnerAuthorityContext)
        typeof(OwnerAuthorityContext.Validation).GetConstructors(Instance).Single().Invoke([]);

    private static OwnerPreparationSpecification Request(InstallationIdentity? installation) =>
        new("/journal", "domain", Guid.NewGuid(), Guid.NewGuid(), "boot", Guid.NewGuid(),
            Guid.NewGuid(), "/source", "/staging", 1, 1, "stage", [], 1024, 1, [], 30_000,
            EnableHeldCoordinator: true,
            DirectIdentity: installation is null ? null : new OwnerDirectIdentity(
                Guid.NewGuid(), null!, null!, null!, installation));

    private sealed class Clock : ITransactionClock
    {
        public long NowNanoseconds { get; set; } = 1_000;
    }

    private sealed class Transport : IReleaseWriteChannel
    {
        public int Attempts { get; private set; }
        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            return ReleasePipeWriteResult.Delivered;
        }
    }

    private sealed class Harness : IDisposable
    {
        private readonly SafeFileHandle[] _handles = Enumerable.Range(0, 5)
            .Select(_ => new SafeFileHandle(IntPtr.Zero, ownsHandle: false)).ToArray();
        public Clock Clock { get; } = new();
        public Transport Transport { get; } = new();
        public PreparationIdentity Identity { get; }
        public PreparedTransaction Capability { get; }
        public OwnershipJournal Journal { get; }
        public JournalWriterLease Writer { get; }
        public OwnerSafetyMonitor Safety { get; }
        public HeldExecution Execution { get; }
        public ReleaseRequest Release => new(Identity, 10_000, 30_000);

        public Harness()
        {
            var manifest = SampleManifest();
            Identity = new(RecoveryTestSupport.Execution, RecoveryTestSupport.Issuer,
                RecoveryTestSupport.Generation, RecoveryTestSupport.Preparation,
                ManifestCodec.ComputeDigestBytes(manifest));
            var store = new PreparedTransaction.PreparationStore(Identity.IssuerId, Identity.GenerationId, Clock);
            Capability = store.Mint(manifest, Identity, 1_000_000_000, TestClosure.Empty());
            Journal = new OwnershipJournal(new FakeJournalStorage(), "/held-constructor");
            Assert.True(Journal.TryAcquireWriter(out var writer, out var error), error);
            Writer = writer!;
            var preparation = new RecordedPreparation(Identity.ExecutionId, Identity.IssuerId,
                Identity.GenerationId, Identity.PreparationId, Identity.ManifestDigest.ToArray());
            Assert.True(Journal.WriteNext(Writer, RecoveryTestSupport.Snapshot(
                stage: PreparationStage.Prepared, preparation: preparation)).Ok);

            // Avoid starting a native monitor thread. BindArbiter only needs its
            // initialized lock and a clear stop latch. No monitor method runs.
            Safety = (OwnerSafetyMonitor)RuntimeHelpers.GetUninitializedObject(typeof(OwnerSafetyMonitor));
            Set(Safety, "_gate", new object());
            Execution = new HeldExecution(null!, _handles[0], _handles[1], _handles[2],
                _handles[3], _handles[4], 0, null!);
        }

        public HeldReleasePhase Construct(OwnerAuthorityContext authority, bool installed = true)
        {
            var identity = installed ? Qualified().Identity : null;
            return new HeldReleasePhase(Request(identity), null!, null!, Execution, null!,
                Journal, Writer, Capability, 10_000, 30_000, authority, Safety);
        }

        public ReleaseArbiter BoundedArbiter(HeldReleasePhase phase)
        {
            var arbiter = Read<ReleaseArbiter>(phase, "_arbiter");
            // Replace only OS-dependent prerequisites, time, and physical I/O.
            // Keep the constructor-installed generation callback untouched.
            Set(arbiter, "_clock", Clock);
            Set(arbiter, "_prerequisites", (Func<ReleasePrerequisites>)(() => new(true, true, true, true)));
            Set(arbiter, "_transport", Transport);
            return arbiter;
        }

        public OwnershipSnapshot Latest() => Journal.ReadLatest().Snapshot!;
        public void Dispose()
        {
            Capability.Dispose();
            Writer.Dispose();
            Execution.CloseAbortControl();
            foreach (var handle in _handles) handle.Dispose();
            // The synthetic monitor/reaper own no native resources or thread.
        }
    }

    [Fact]
    public void MatchingQualifiedContextConstructsRealHeldWithProductionVerifier()
    {
        using var h = new Harness();
        var qualified = Qualified();
        var phase = h.Construct(qualified);
        var arbiter = Read<ReleaseArbiter>(phase, "_arbiter");
        var verifier = Read<Func<OwnerGenerationGateRefusal?>>(arbiter, "_ownerGenerationGate");

        Assert.Same(qualified, phase.Authority);
        Assert.Same(phase, verifier.Target);
        Assert.Equal("VerifyOwnerReleaseGeneration", verifier.Method.Name);
        Assert.Same(h.Execution, Read<IReleaseWriteChannel>(arbiter, "_transport"));
        Assert.Same(arbiter, Read<ReleaseArbiter>(h.Safety, "_arbiter"));
        Assert.Single(IlCallScanner.CallOffsets(verifier.Method,
            typeof(OwnerReleaseGenerationGate).GetMethod("Verify", BindingFlags.Static | BindingFlags.NonPublic)!));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QualifiedContextCannotSelectUngatedLegacyReleaseByOmittingWireIdentity(bool installed)
    {
        using var h = new Harness();
        var phase = h.Construct(Qualified(), installed);
        Assert.NotNull(Read<Delegate>(Read<ReleaseArbiter>(phase, "_arbiter"), "_ownerGenerationGate"));
    }

    [Theory]
    [InlineData("missing", false)]
    [InlineData("missing", true)]
    [InlineData("legacy", true)]
    [InlineData("validation", false)]
    [InlineData("validation", true)]
    public void MissingOrMismatchedAuthorityRefusesBeforeArbiterBinding(string kind, bool installed)
    {
        using var h = new Harness();
        var authority = kind switch
        {
            "legacy" => OwnerAuthorityContext.LegacyContext,
            "validation" => Validation(),
            _ => null!,
        };
        Assert.Throws<InvalidDataException>(() => h.Construct(authority, installed));
        Assert.Null(Field(typeof(OwnerSafetyMonitor), "_arbiter").GetValue(h.Safety));
        Assert.True(h.Capability.IsLive);
        Assert.Null(h.Latest().Release);
        Assert.Equal(0, h.Execution.PhysicalReleaseAttempts);
    }

    [Fact]
    public void LegacyConstructorKeepsUngatedReleaseAndDuplicateMakesOnlyOneAttempt()
    {
        using var h = new Harness();
        var phase = h.Construct(OwnerAuthorityContext.LegacyContext, installed: false);
        var arbiter = h.BoundedArbiter(phase);
        Assert.Null(Field(typeof(ReleaseArbiter), "_ownerGenerationGate").GetValue(arbiter));
        Assert.Equal(ReleaseDecisionKind.Released, arbiter.RequestRelease(h.Release).Kind);
        Assert.Equal(ReleaseDecisionKind.Duplicate, arbiter.RequestRelease(h.Release).Kind);
        Assert.Equal(1, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Committed, h.Latest().Release!.Tag);
    }

    [Fact]
    public void ConstructedQualifiedArbiterExecutesRealGateAndRefusesMissingLoadedAuthorityWithoutAttempt()
    {
        using var h = new Harness();
        // This Debug-only shaped context cannot provide a LoadedInstallation.
        // It must reach the real callback and refuse, never fall back to legacy.
        var qualified = OwnerAuthorityContext.Qualified.ForTest(Qualified().Selection);
        var arbiter = h.BoundedArbiter(h.Construct(qualified));
        var result = arbiter.RequestRelease(h.Release);
        Assert.Equal(ReleaseDecisionKind.Refused, result.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, result.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, result.Delivery);
        Assert.Equal(RestrictedRefusalReason.UnqualifiedInstallation, result.RefusalReason);
        Assert.Contains("retained qualified selection has no verified loaded installation", result.Reason);
        Assert.False(result.DurableCommitted);
        Assert.False(arbiter.RuntimeReleaseCommitted);
        Assert.False(h.Capability.IsLive);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
        Assert.Equal(StopCauseKind.SetupFault, h.Latest().Stop!.Cause);
        var sequence = h.Latest().Sequence;
        Assert.Equal(ReleaseDecisionKind.Duplicate, arbiter.RequestRelease(h.Release).Kind);
        Assert.Equal(sequence, h.Latest().Sequence);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(0, h.Execution.PhysicalReleaseAttempts);
    }

    [Fact]
    public async Task StopDuringConstructedVerifierRetainsPrecedence()
    {
        using var h = new Harness();
        var arbiter = h.BoundedArbiter(h.Construct(Qualified()));
        Assert.NotNull(Read<Delegate>(arbiter, "_ownerGenerationGate"));
        using var entered = new ManualResetEventSlim();
        using var proceed = new ManualResetEventSlim();
        // Substitute a bounded successful scan to exercise the winner check.
        // Production callback installation/execution is tested separately.
        Set(arbiter, "_ownerGenerationGate", (Func<OwnerGenerationGateRefusal?>)(() =>
        {
            entered.Set();
            Assert.True(proceed.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            return null;
        }));
        var releasing = Task.Run(() => arbiter.RequestRelease(h.Release), TestContext.Current.CancellationToken);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            var stop = arbiter.RequestStop(StopCauseKind.CallerStop);
            Assert.True(stop.Accepted);
            Assert.True(stop.DurableStop);
        }
        finally { proceed.Set(); }
        var result = await releasing.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRefusalReason.StopRequested, result.RefusalReason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(StopCauseKind.CallerStop, h.Latest().Stop!.Cause);
    }

    [Theory]
    [InlineData(10_000)]
    [InlineData(30_000)]
    public void ConstructedArbiterCannotOverrideOriginalDeadlineWithSuccessfulVerification(long now)
    {
        using var h = new Harness();
        var arbiter = h.BoundedArbiter(h.Construct(Qualified()));
        Assert.NotNull(Read<Delegate>(arbiter, "_ownerGenerationGate"));
        Set(arbiter, "_ownerGenerationGate", (Func<OwnerGenerationGateRefusal?>)(() =>
        {
            h.Clock.NowNanoseconds = now;
            return null;
        }));
        var result = arbiter.RequestRelease(h.Release);
        Assert.Equal(RestrictedRefusalReason.DeadlineExpired, result.RefusalReason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.False(h.Capability.IsLive);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
    }
}
