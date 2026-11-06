// P3 (GAGAMBA-RC2B-06-B6-ALT-B) deterministic owner pre-RELEASE
// generation-gate tests through the REAL release arbiter with a counting
// write channel, a controlled clock and the real journal. They prove the
// verification runs after durable intent, outside the gates, that every
// refusal yields zero physical attempts and the conservative post-intent
// shape, that the existing stop path wins while the verifier is blocked,
// and that a deadline can never be overridden by a successful verification.
using System.Reflection;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Tests.Lifecycle;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Execution;

public sealed class OwnerReleaseGenerationGateArbiterTests
{
    private sealed class FakeClock : ITransactionClock
    {
        public long NowNanoseconds { get; set; } = 1_000;
    }

    private sealed class CountingTransport : IReleaseWriteChannel
    {
        public int Attempts { get; private set; }

        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            return ReleasePipeWriteResult.Delivered;
        }
    }

    private sealed class CrashException : Exception
    {
    }

    private sealed class GateHarness : IDisposable
    {
        private int _verificationCalls;

        public GateHarness(Func<OwnerGenerationGateRefusal?>? ownerGate)
        {
            Clock = new FakeClock();
            var manifest = SampleManifest();
            Identity = new PreparationIdentity(
                RecoveryTestSupport.Execution,
                RecoveryTestSupport.Issuer,
                RecoveryTestSupport.Generation,
                RecoveryTestSupport.Preparation,
                ManifestCodec.ComputeDigestBytes(manifest));
            Store = new PreparedTransaction.PreparationStore(
                RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, Clock);
            Capability = Store.Mint(manifest, Identity, 1_000_000_000, TestClosure.Empty());
            Storage = new FakeJournalStorage();
            Journal = new OwnershipJournal(Storage, "/release");
            Assert.True(Journal.TryAcquireWriter(out var lease, out var error), error);
            Lease = lease!;
            var preparation = new RecordedPreparation(
                Identity.ExecutionId, Identity.IssuerId, Identity.GenerationId,
                Identity.PreparationId, Identity.ManifestDigest.ToArray());
            var seed = RecoveryTestSupport.Snapshot(
                stage: PreparationStage.Prepared, preparation: preparation);
            Assert.True(Journal.WriteNext(Lease, seed).Ok);
            Transport = new CountingTransport();
            OwnerGate = ownerGate;
            Arbiter = new ReleaseArbiter(
                Capability, Journal, Lease, Clock, () => Prerequisites, Transport,
                ownerGate is null ? null : Verify);
        }

        public FakeClock Clock { get; }
        public PreparationIdentity Identity { get; }
        public PreparedTransaction.PreparationStore Store { get; }
        public PreparedTransaction Capability { get; }
        public FakeJournalStorage Storage { get; }
        public OwnershipJournal Journal { get; }
        public JournalWriterLease Lease { get; }
        public CountingTransport Transport { get; }
        public ReleaseArbiter Arbiter { get; }
        public Func<OwnerGenerationGateRefusal?>? OwnerGate { get; set; }
        public int VerificationCalls => _verificationCalls;
        public ReleasePrerequisites Prerequisites { get; set; } = new(true, true, true, true);
        public long SetupDeadline { get; set; } = 10_000;
        public long OverallDeadline { get; set; } = 30_000;

        public ReleaseRequest Request => new(Identity, SetupDeadline, OverallDeadline);

        public OwnershipSnapshot Latest()
        {
            var read = Journal.ReadLatest();
            Assert.True(read.Ok, read.Error);
            return read.Snapshot!;
        }

        private OwnerGenerationGateRefusal? Verify()
        {
            _verificationCalls++;
            return OwnerGate!();
        }

        public void Dispose()
        {
            Capability.Dispose();
            Lease.Dispose();
        }
    }

    private static OwnerGenerationGateRefusal Refusal(RestrictedRefusalReason reason, string error) =>
        new(reason, error);

    [Fact]
    public void QualifiedVerifiedObservationStillReleasesExactlyOnce()
    {
        using var h = new GateHarness(() => null);
        var decision = h.Arbiter.RequestRelease(h.Request);

        Assert.Equal(ReleaseDecisionKind.Released, decision.Kind);
        Assert.Equal(1, h.Transport.Attempts);
        Assert.Equal(1, h.VerificationCalls);
        Assert.Equal(ReleaseTag.Committed, h.Latest().Release!.Tag);
    }

    [Fact]
    public void UnqualifiedRefusalHasZeroPhysicalAttemptsAndPostIntentShape()
    {
        using var h = new GateHarness(() => Refusal(
            RestrictedRefusalReason.UnqualifiedInstallation,
            "q10 active-generation reread refused: original S2 selection no longer qualified"));
        var decision = h.Arbiter.RequestRelease(h.Request);

        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, decision.Delivery);
        Assert.False(decision.DurableCommitted);
        Assert.True(decision.LiveExecutionExcluded);
        Assert.Equal(ReleaseCorrelationBinding.Accepted, decision.Binding);
        Assert.Equal(RestrictedRefusalReason.UnqualifiedInstallation, decision.RefusalReason);
        Assert.StartsWith("release gate refused: owner pre-RELEASE generation verification failed", decision.Reason);
        Assert.Contains("original S2 selection no longer qualified", decision.Reason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(1, h.VerificationCalls);
        Assert.False(h.Capability.IsLive);

        var latest = h.Latest();
        Assert.Equal(ReleaseTag.Intent, latest.Release!.Tag);
    }

    [Fact]
    public void UnavailableRefusalAlsoRefusesWithZeroPhysicalAttempts()
    {
        using var h = new GateHarness(() => Refusal(
            RestrictedRefusalReason.InstallationUnavailable,
            "q10: no retained trusted installation for the bound generation"));
        var decision = h.Arbiter.RequestRelease(h.Request);

        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(RestrictedRefusalReason.InstallationUnavailable, decision.RefusalReason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
    }

    [Fact]
    public void RefusalSubmitsSetupFaultStopTailOutsideTheGate()
    {
        using var h = new GateHarness(() => Refusal(
            RestrictedRefusalReason.UnqualifiedInstallation, "generation changed"));
        var decision = h.Arbiter.RequestRelease(h.Request);

        var latest = h.Latest();
        Assert.NotNull(latest.Stop);
        Assert.Equal(StopCauseKind.SetupFault, latest.Stop!.Cause);
        Assert.Equal(ReleaseTag.Intent, latest.Release!.Tag);
        Assert.DoesNotContain("tail stop limitation", decision.Reason);
    }

    [Fact]
    public void DuplicateAfterRefusalReturnsC4WithoutSecondAttemptOrStop()
    {
        using var h = new GateHarness(() => Refusal(
            RestrictedRefusalReason.UnqualifiedInstallation, "generation changed"));
        Assert.Equal(ReleaseDecisionKind.Refused, h.Arbiter.RequestRelease(h.Request).Kind);
        var sequence = h.Latest().Sequence;
        var callsAfterFirst = h.VerificationCalls;

        var duplicate = h.Arbiter.RequestRelease(h.Request);

        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Kind);
        Assert.Equal(ReleaseCorrelationBinding.Accepted, duplicate.Binding);
        Assert.Equal(RestrictedRefusalReason.None, duplicate.RefusalReason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(callsAfterFirst, h.VerificationCalls);
        Assert.Equal(sequence, h.Latest().Sequence);
    }

    [Fact]
    public async Task StopDuringBlockedVerificationWinsWithoutPhysicalAttempt()
    {
        using var entered = new ManualResetEventSlim(false);
        using var proceed = new ManualResetEventSlim(false);
        using var h = new GateHarness(() =>
        {
            entered.Set();
            _ = proceed.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
            return null; // the observation would have verified had stop not won
        });

        var releasing = Task.Run(
            () => h.Arbiter.RequestRelease(h.Request), TestContext.Current.CancellationToken);
        Assert.True(
            entered.Wait(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken),
            "verification never started");

        // The verification runs outside every gate, so a concurrent stop is
        // admitted and settled while the verifier is blocked.
        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.DurableStop);
        proceed.Set();

        var decision = await releasing;
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(RestrictedRefusalReason.StopRequested, decision.RefusalReason);
        Assert.Contains("stop won during durable intent", decision.Reason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.False(h.Capability.IsLive);

        var latest = h.Latest();
        Assert.Equal(ReleaseTag.Intent, latest.Release!.Tag);
        Assert.Equal(StopCauseKind.CallerStop, latest.Stop!.Cause);
    }

    [Fact]
    public void DeadlineExpiryDuringVerificationCannotBeOverriddenBySuccess()
    {
        using var h = new GateHarness(() => null);
        h.OwnerGate = () =>
        {
            // Time passes while the protected generation is scanned.
            h.Clock.NowNanoseconds = h.SetupDeadline;
            return null; // a successful observation is not a deadline renewal
        };

        var decision = h.Arbiter.RequestRelease(h.Request);

        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(RestrictedRefusalReason.DeadlineExpired, decision.RefusalReason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(ReleaseTag.Intent, h.Latest().Release!.Tag);
    }

    [Fact]
    public void VerificationFaultRefusesFailClosedAndStillStops()
    {
        using var h = new GateHarness(() =>
            throw new InvalidOperationException("protected fixed root unreadable"));

        var decision = h.Arbiter.RequestRelease(h.Request);

        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(RestrictedRefusalReason.InstallationUnavailable, decision.RefusalReason);
        Assert.Contains("verification could not complete", decision.Reason);
        Assert.Contains("protected fixed root unreadable", decision.Reason);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Equal(StopCauseKind.SetupFault, h.Latest().Stop!.Cause);
    }

    [Fact]
    public void NewArbiterOverDurableIntentCannotVerifyOrResend()
    {
        using var h = new GateHarness(() => null);
        h.Arbiter.TestBarrier = name => { if (name == "after-intent") throw new CrashException(); };
        Assert.Throws<CrashException>(() => h.Arbiter.RequestRelease(h.Request));

        var secondStore = new PreparedTransaction.PreparationStore(
            RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, h.Clock);
        var secondCapability = secondStore.Mint(
            SampleManifest(), h.Identity, 1_000_000_000, TestClosure.Empty());
        using (secondCapability)
        {
            var verifyCalls = 0;
            var secondArbiter = new ReleaseArbiter(
                secondCapability, h.Journal, h.Lease, h.Clock,
                () => new ReleasePrerequisites(true, true, true, true), h.Transport,
                () => { verifyCalls++; return null; });
            var decision = secondArbiter.RequestRelease(h.Request);

            Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
            Assert.Equal(0, h.Transport.Attempts);
            Assert.Equal(0, verifyCalls);
        }
    }

    [Fact]
    public void LegacyNoGateKeepsTheExistingReleasePath()
    {
        using var h = new GateHarness(null);
        var decision = h.Arbiter.RequestRelease(h.Request);

        Assert.Equal(ReleaseDecisionKind.Released, decision.Kind);
        Assert.Equal(1, h.Transport.Attempts);
        Assert.Equal(0, h.VerificationCalls);
        Assert.Equal(ReleaseTag.Committed, h.Latest().Release!.Tag);
    }

    [Fact]
    public void RefusedDecisionRoundTripsOnTheOwnerWireForBothReasons()
    {
        foreach (var reason in new[]
        {
            RestrictedRefusalReason.UnqualifiedInstallation,
            RestrictedRefusalReason.InstallationUnavailable,
        })
        {
            using var h = new GateHarness(() => Refusal(reason,
                "release gate refused: owner pre-RELEASE generation verification failed"));
            var decision = h.Arbiter.RequestRelease(h.Request);

            var encoded = OwnerHeldWireCodec.EncodeReleaseDecision(decision);
            Assert.True(OwnerHeldWireCodec.TryDecodeReleaseDecision(encoded, out var decoded, out var error), error);
            Assert.Equal(decision, decoded);
            Assert.Equal(reason, decoded!.RefusalReason);
        }
    }

    // ---- IL wiring pins (P2 style). No method is executed; these read the
    // production call graph so the wiring cannot silently regress. ----

    private static MethodInfo Method(Type type, string name) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static |
                BindingFlags.Instance)
            .Single(method => method.Name == name && method.DeclaringType == type);

    private static MethodBase SingleConstructor(Type type) =>
        type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Single();

    [Fact]
    public void ArbiterStoresVerificationAsAReadonlyConstructionDependency()
    {
        var field = typeof(ReleaseArbiter).GetField(
            "_ownerGenerationGate", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.True(field!.IsInitOnly, "the verification dependency must be construction-time only");
        Assert.Equal(typeof(Func<OwnerGenerationGateRefusal?>), field.FieldType);

        var constructor = SingleConstructor(typeof(ReleaseArbiter));
        var parameters = constructor.GetParameters();
        Assert.Equal(7, parameters.Length);
        Assert.True(parameters[6].IsOptional);
        Assert.Null(parameters[6].DefaultValue);
        Assert.Equal(typeof(Func<OwnerGenerationGateRefusal?>), parameters[6].ParameterType);
        Assert.Single(IlCallScanner.CallOffsets(constructor, field));
    }

    [Fact]
    public void RequestReleaseVerifiesAfterIntentAndBeforeConsumptionAndWrite()
    {
        var requestRelease = Method(typeof(ReleaseArbiter), "RequestRelease");
        var verify = Method(typeof(ReleaseArbiter), "InvokeOwnerGenerationGate");
        var intent = Method(typeof(OwnershipJournal), "WriteTransition");
        var consume = Method(typeof(PreparedTransaction), "TryConsume");
        var write = typeof(IReleaseWriteChannel).GetMethod("TryWriteOnce")!;

        var verifyOffset = Assert.Single(IlCallScanner.CallOffsets(requestRelease, verify));
        var intentOffset = Assert.Single(IlCallScanner.CallOffsets(requestRelease, intent));
        var consumeOffsets = IlCallScanner.CallOffsets(requestRelease, consume);
        var writeOffsets = IlCallScanner.CallOffsets(requestRelease, write);
        Assert.NotEmpty(consumeOffsets);
        Assert.NotEmpty(writeOffsets);

        Assert.True(intentOffset < verifyOffset,
            "the fresh verification must run after the durable intent");
        Assert.True(verifyOffset < consumeOffsets.Min(),
            "the verification must run before capability consumption");
        Assert.True(verifyOffset < writeOffsets.Min(),
            "the verification must run before the single physical write");
    }

    [Fact]
    public void OwnerPhasePinsTheSingleSharedVerificationDecision()
    {
        var constructor = SingleConstructor(typeof(HeldReleasePhase));
        var phaseVerify = Method(typeof(HeldReleasePhase), "VerifyOwnerReleaseGeneration");
        var sharedVerify = Method(typeof(OwnerReleaseGenerationGate), "Verify");

        // The constructor wires the owner phase method group, not the shared
        // decision directly, and the phase method forwards to exactly one
        // shared Q10 decision.
        Assert.Single(IlCallScanner.CallOffsets(constructor, phaseVerify));
        Assert.Empty(IlCallScanner.CallOffsets(constructor, sharedVerify));
        Assert.Single(IlCallScanner.CallOffsets(phaseVerify, sharedVerify));
    }

    [Fact]
    public void SharedVerificationIsReferencedOnlyFromTheOwnerPhase()
    {
        var sharedVerify = Method(typeof(OwnerReleaseGenerationGate), "Verify");
        var callSites = new List<string>();
        foreach (var type in typeof(OwnerReleaseGenerationGate).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetMethodBody() is null)
                {
                    continue;
                }

                if (IlCallScanner.CallOffsets(method, sharedVerify).Count > 0)
                {
                    callSites.Add($"{type.Name}.{method.Name}");
                }
            }
        }

        Assert.Equal(new[] { "HeldReleasePhase.VerifyOwnerReleaseGeneration" },
            callSites.OrderBy(site => site, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void DebugObserverHasNoProductionCaller()
    {
        var observer = typeof(OwnerReleaseGenerationGate).GetMethod(
            "VerifyObserved", BindingFlags.NonPublic | BindingFlags.Static);
        if (observer is null)
        {
            Assert.Skip("the debug-only observer is compiled out of this flavored assembly");
        }

        var callSites = new List<string>();
        foreach (var type in typeof(OwnerReleaseGenerationGate).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (method.GetMethodBody() is null)
                {
                    continue;
                }

                if (IlCallScanner.CallOffsets(method, observer!).Count > 0)
                {
                    callSites.Add($"{type.Name}.{method.Name}");
                }
            }
        }

        Assert.Empty(callSites);
    }
}
