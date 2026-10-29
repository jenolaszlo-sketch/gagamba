// Contract-only deterministic doubles. No native sandbox, process, real clock,
// installed provider, durable journal or recovery qualification is exercised.
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Gagamba.Execution.Restricted.Contracts;
using Gagamba.Execution.Restricted.Control;
using Gagamba.Execution.Linux.M1.Tests.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.RestrictedControl;

public sealed class RestrictedControlB1Tests
{
    private static readonly RestrictedPreparationInputs Inputs = new("enrolled-tool", "enrolled-input");
    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class FakeOwner : IRestrictedOperationOwner
    {
        public readonly TaskCompletionSource<RestrictedOwnerPrepareResult> Preparation = Signal<RestrictedOwnerPrepareResult>();
        public readonly TaskCompletionSource<RestrictedStopReply> StopAck = Signal<RestrictedStopReply>();
        public readonly TaskCompletionSource<RestrictedOwnerReleaseResult> Release = Signal<RestrictedOwnerReleaseResult>();
        public readonly TaskCompletionSource<bool> PrepareEntered = Signal<bool>();
        public readonly TaskCompletionSource<bool> StopEntered = Signal<bool>();
        public readonly TaskCompletionSource<bool> ReleaseEntered = Signal<bool>();
        public int PrepareCalls;
        public int StopCalls;
        public int ReleaseCalls;
        public int DisposeCalls;
        public bool Live = true;
        public RestrictedReleaseDecision? RetainedRelease { get; set; }
        public RestrictedCleanup Cleanup = RestrictedCleanup.Unknown;
        public bool LoseDisposalReply;

        public ValueTask<RestrictedOwnerPrepareResult> PrepareAsync(RestrictedPreparationInputs inputs)
        {
            Interlocked.Increment(ref PrepareCalls);
            PrepareEntered.TrySetResult(true);
            return new(Preparation.Task);
        }
        public ValueTask<RestrictedOwnerReleaseResult> ReleaseAsync(ImmutableArray<byte> approvalCorrelationSha256)
        {
            Interlocked.Increment(ref ReleaseCalls);
            ReleaseEntered.TrySetResult(true);
            return new(Release.Task);
        }
        public ValueTask<RestrictedStopReply> StopAsync(RestrictedStopReason reason)
        {
            Interlocked.Increment(ref StopCalls);
            Live = false; // Test-only owner acceptance, independent of its delayed ACK.
            StopEntered.TrySetResult(true);
            return new(StopAck.Task);
        }
        public ValueTask<RestrictedCleanup> DisposeAsync()
        {
            Interlocked.Increment(ref DisposeCalls);
            Live = false;
            if (LoseDisposalReply) throw new IOException("synthetic lost disposal acknowledgement");
            return ValueTask.FromResult(Cleanup);
        }
        public bool IsLive(RestrictedPreparationReference reference) => Live;
    }

    private sealed class Harness
    {
        public readonly RestrictedProviderDescriptor Descriptor = new();
        public readonly FakeOwner Owner = new();
        public readonly RestrictedExecutionOperation Operation;
        public readonly RestrictedOwnerPreparation Candidate;

        public Harness()
        {
            Operation = new(Descriptor, Owner);
            var manifest = new RestrictedManifestView(ManifestTests.SampleManifest());
            var reference = new RestrictedPreparationReference(Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), RestrictedManifestCodec.ComputeSha256(manifest).AsSpan());
            Candidate = new(reference, manifest, Descriptor,
                new RestrictedDeadlines(10, 50, 100, 200, "synthetic-clock", Guid.NewGuid(), Guid.NewGuid(),
                    1_000_000_000, reference.GenerationId), new RestrictedEstablishmentReceipt(1, [1, 2, 3]));
        }
        public async Task<PreparedRestrictedExecution> Prepare()
        {
            Owner.Preparation.SetResult(new(RestrictedPrepareKind.Prepared, Candidate));
            return Assert.IsType<PreparedRestrictedExecution>((await Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Preparation);
        }
        public async Task AcceptStop(RestrictedStopReason reason = RestrictedStopReason.CallerStop,
            long? accepted = 20, long? deadline = 70)
        {
            Owner.StopAck.SetResult(new(RestrictedStopSubmission.RuntimeAccepted, false, reason,
                false, accepted, deadline, RestrictedEvidenceState.Unknown));
            await Operation.StopSubmission.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
    }

    private static RestrictedReleaseDecision Ambiguous(ImmutableArray<byte> correlation = default) => new(
        RestrictedReleaseKind.AmbiguousPersistence, RestrictedReleaseEvidence.MayHaveExecuted,
        RestrictedDelivery.NotAttempted, RestrictedEvidenceState.Unknown, true, correlation,
        RestrictedEvidenceState.Unknown);

    internal static RestrictedOwnerReleaseResult RefusedPreRetention(
        RestrictedRefusalReason reason = RestrictedRefusalReason.DeadlineExpired,
        RestrictedRequestBinding binding = RestrictedRequestBinding.Unknown) => new(
        RestrictedReleaseKind.Refused, RestrictedReleaseEvidence.NotCommitted, RestrictedDelivery.NotAttempted,
        RestrictedEvidenceState.Unknown, null, binding, reason);

    internal static RestrictedOwnerReleaseResult Released(RestrictedOwnerExecutionFacts? facts = null) => new(
        RestrictedReleaseKind.Released, RestrictedReleaseEvidence.Committed, RestrictedDelivery.Delivered,
        RestrictedEvidenceState.Confirmed, false, RestrictedRequestBinding.Accepted,
        RestrictedRefusalReason.None, facts);

    internal static RestrictedOwnerReleaseResult StoppedDuringIntent() => new(
        RestrictedReleaseKind.Refused, RestrictedReleaseEvidence.MayHaveExecuted, RestrictedDelivery.NotAttempted,
        RestrictedEvidenceState.Unknown, true, RestrictedRequestBinding.Accepted,
        RestrictedRefusalReason.StopRequested);

    [Fact]
    public void AuthorityTypesHaveNoPublicConstructionCloneFactoryOrDeserialization()
    {
        foreach (var type in new[] { typeof(PreparedRestrictedExecution), typeof(RestrictedExecutionOperation) })
        {
            Assert.True(type.IsSealed);
            Assert.Empty(type.GetConstructors());
            Assert.Empty(type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
            Assert.DoesNotContain(type.GetMethods(), m => m.Name.Contains("Clone", StringComparison.Ordinal));
            Assert.DoesNotContain(type.GetProperties(), p => p.SetMethod?.IsPublic == true);
        }
        var deserialize = Record.Exception(() => JsonSerializer.Deserialize<PreparedRestrictedExecution>("{}"));
        Assert.True(deserialize is NotSupportedException or InvalidOperationException);
        Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<RestrictedExecutionOperation>("{}"));
        Assert.False(typeof(IExecutionProvider).IsAssignableFrom(typeof(IRestrictedExecutionProvider)));
        Assert.False(typeof(IRestrictedExecutionProvider).IsAssignableFrom(typeof(IExecutionProvider)));
    }

    [Theory]
    [InlineData("unknown", RestrictedRefusalReason.UnsupportedProfile)]
    [InlineData("restricted-offline-closure-v1", RestrictedRefusalReason.InstallationUnavailable)]
    [InlineData("ae4-linux-fixed-fixture-v1", RestrictedRefusalReason.InstallationUnavailable)]
    public void SelectionCannotInventQualificationOrSetup(string profile, RestrictedRefusalReason reason)
    {
        var provider = new UnavailableRestrictedExecutionProvider();
        var result = provider.CreateOperation(new(profile, "1", "linux-x64", "caller-asserted", new byte[32]));
        Assert.Null(result.Operation);
        Assert.Equal(reason, result.Reason);
        var descriptor = provider.Describe();
        Assert.Equal(RestrictedInstallationState.Unavailable, descriptor.InstallationState);
        Assert.Null(descriptor.ProviderPackageId);
        Assert.Null(descriptor.ContractPackageVersion);
        Assert.Null(descriptor.OwnerProtocolVersion);
        Assert.Null(descriptor.QualificationId);
        Assert.Empty(descriptor.Artifacts);
        Assert.True(descriptor.ArtifactSetSha256.IsDefault);
    }

    [Theory]
    [InlineData(0)] [InlineData(31)] [InlineData(33)]
    public void InvalidSelectionPinsFailBeforeAnyOperation(int length) => Assert.Throws<ArgumentException>(() =>
        new RestrictedProfileSelection("profile", "1", "linux-x64", "qualification", new byte[length]));

    [Theory]
    [InlineData("")] [InlineData(" ")] [InlineData("../tool")] [InlineData("C:\\tool")] [InlineData("x/y")]
    public void InvalidSourceIdsCannotBecomePathsOrPreparation(string id) =>
        Assert.Throws<ArgumentException>(() => new RestrictedPreparationInputs(id, "input"));

    [Fact]
    public void InputBoundsAndCopiesAreEnforced()
    {
        Assert.Throws<ArgumentException>(() => new RestrictedPreparationInputs(new string('x', 129), "input"));
        Assert.Throws<ArgumentNullException>(() => new RestrictedPreparationInputs(null!, "input"));
        var digest = new byte[32];
        var selection = new RestrictedProfileSelection("profile", "1", "linux-x64", "qualification", digest);
        digest[0] = 1;
        Assert.Equal((byte)0, selection.ArtifactSetSha256[0]);
        var receiptBytes = new byte[] { 1, 2 };
        var receipt = new RestrictedEstablishmentReceipt(1, receiptBytes);
        receiptBytes[0] = 3;
        Assert.Equal((byte)1, receipt.Bytes[0]);
        Assert.Equal(System.Security.Cryptography.SHA256.HashData(new byte[] { 1, 2 }), receipt.Sha256.ToArray());
    }

    [Fact]
    public async Task PassiveDecodeAndEqualReferencesYieldNoLiveAuthority()
    {
        var h = new Harness();
        var decoded = RestrictedManifestCodec.Decode(RestrictedManifestCodec.Encode(h.Candidate.Manifest)).Value!;
        var r = h.Candidate.Reference;
        var reconstructed = new RestrictedPreparationReference(r.ExecutionId, r.IssuerId, r.GenerationId,
            r.PreparationId, RestrictedManifestCodec.ComputeSha256(decoded).AsSpan());
        Assert.Equal(r, reconstructed);
        var provider = new UnavailableRestrictedExecutionProvider();
        Assert.Equal(RestrictedInspectionKind.Unavailable, (await provider.InspectExecutionAsync(reconstructed, waitCancellation: TestContext.Current.CancellationToken)).Kind);
        var reconcile = await provider.ReconcileAsync(reconstructed, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReconciliationKind.Refused, reconcile.Kind);
        Assert.Equal(RestrictedEvidenceState.Unknown, reconcile.Cleanup.State);
        Assert.DoesNotContain(typeof(IRestrictedExecutionProvider).GetMethods(), m =>
            m.ReturnType == typeof(PreparedRestrictedExecution) || m.Name.Contains("Release", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StopBeforePreparationPreventsOwnerSetupAndMint()
    {
        var h = new Harness();
        var stop = await h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedStopSubmission.LocallyQueued, stop.Submission);
        Assert.Null(stop.AcceptedAtNs);
        Assert.Null(stop.DeadlineNs);
        Assert.Equal(RestrictedPrepareKind.CancelledBeforeRelease, (await h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Kind);
        Assert.Equal(0, h.Owner.PrepareCalls);
        await h.AcceptStop();
        Assert.Equal(RestrictedPrepareKind.CancelledBeforeRelease, (await h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Kind);
    }

    [Fact]
    public async Task StopDuringPreparationWinsBeforeMintEvenWithLateSuccessfulOwnerReply()
    {
        var h = new Harness();
        var preparation = h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken).AsTask();
        await h.Owner.PrepareEntered.Task;
        await h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop, waitCancellation: TestContext.Current.CancellationToken);
        h.Owner.Preparation.SetResult(new(RestrictedPrepareKind.Prepared, h.Candidate));
        Assert.Equal(RestrictedPrepareKind.CancelledBeforeRelease, (await preparation).Kind);
        Assert.Null((await preparation).Preparation);
        await h.AcceptStop();
    }

    [Fact]
    public async Task MintWinsFirstButSubsequentLocalStopImmediatelyInhibitsRelease()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        // The owner refuses this pre-retention request; the call is admitted
        // (correlation bound) but nothing is retained.
        h.Owner.Release.SetResult(RefusedPreRetention());
        var before = await h.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Refused, before.Kind);
        Assert.Equal(RestrictedRefusalReason.DeadlineExpired, before.Reason);
        Assert.Null(before.OriginalDecision);
        await h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop, waitCancellation: TestContext.Current.CancellationToken);
        var afterStop = await h.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRefusalReason.StopRequested, afterStop.Reason);
        Assert.Equal(1, h.Owner.ReleaseCalls);
        Assert.Equal(RestrictedStopSubmission.LocallyQueued, (await h.Operation.ObserveAsync(waitCancellation: TestContext.Current.CancellationToken)).Stop!.Submission);
        await h.AcceptStop();
        Assert.Equal(RestrictedStopSubmission.RuntimeAccepted, (await h.Operation.ObserveAsync(waitCancellation: TestContext.Current.CancellationToken)).Stop!.Submission);
        Assert.Equal(RestrictedRefusalReason.StopRequested, (await h.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken)).Reason);
    }

    [Fact]
    public async Task PreparedObjectAndAllRetainedIdentityFieldsMustMatchExactOperation()
    {
        var first = new Harness();
        var prepared = await first.Prepare();
        var second = new Harness();
        await second.Prepare();
        Assert.Equal(RestrictedRefusalReason.InvalidBinding, (await second.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken)).Reason);
        // Even an equal passive tuple and the exact same receipt/manifest cannot
        // turn a newly constructed internal test object into the issued object.
        var impostor = new PreparedRestrictedExecution(new object(), first.Candidate);
        Assert.Equal(RestrictedRefusalReason.InvalidBinding, (await first.Operation.RequestReleaseAsync(impostor, waitCancellation: TestContext.Current.CancellationToken)).Reason);
        first.Owner.Live = false;
        Assert.Equal(RestrictedRefusalReason.InvalidBinding, (await first.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken)).Reason);
    }

    [Theory]
    [InlineData("digest")] [InlineData("generation")] [InlineData("descriptor")]
    public async Task InvalidOwnerPreparationAssociationsAreRefused(string changed)
    {
        var h = new Harness();
        var candidate = h.Candidate;
        if (changed == "digest")
        {
            var r = candidate.Reference;
            candidate = candidate with { Reference = new(r.ExecutionId, r.IssuerId, r.GenerationId, r.PreparationId, new byte[32]) };
        }
        if (changed == "generation")
            candidate = candidate with { Deadlines = new(10, 50, 100, 200, "synthetic-clock", Guid.NewGuid(),
                Guid.NewGuid(), 1_000_000_000, Guid.NewGuid()) };
        if (changed == "descriptor") candidate = candidate with { Provider = new() };
        h.Owner.Preparation.SetResult(new(RestrictedPrepareKind.Prepared, candidate));
        var result = await h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRefusalReason.InvalidBinding, result.Reason);
        Assert.Null(result.Preparation);
    }

    [Fact]
    public async Task RepeatedPreparationRetainsOriginalInputsObjectAndDeadlines()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        var repeat = await h.Operation.PrepareAsync(new("enrolled-tool", "enrolled-input"), waitCancellation: TestContext.Current.CancellationToken);
        Assert.Same(prepared, repeat.Preparation);
        Assert.Equal(1, h.Owner.PrepareCalls);
        Assert.Same(h.Candidate.Deadlines, repeat.Preparation!.Deadlines);
        Assert.Equal(50, repeat.Preparation.Deadlines.PreparationExpiresAtNs);
        Assert.Equal(RestrictedRefusalReason.PreparationAlreadyStarted,
            (await h.Operation.PrepareAsync(new("other-tool", "enrolled-input"), waitCancellation: TestContext.Current.CancellationToken)).Reason);
    }

    [Fact]
    public async Task InvalidatedOwnerIssuanceCannotBePublishedAsPrepared()
    {
        var h = new Harness();
        h.Owner.Live = false;
        h.Owner.Preparation.SetResult(new(RestrictedPrepareKind.Prepared, h.Candidate));
        var result = await h.Operation.PrepareAsync(Inputs, TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRefusalReason.InvalidBinding, result.Reason);
        Assert.Null(result.Preparation);
    }

    [Fact]
    public async Task CancelledPrepareWaitDoesNotCancelOrRestartUnderlyingPreparation()
    {
        var h = new Harness();
        using var cancellation = new CancellationTokenSource();
        var waiting = h.Operation.PrepareAsync(Inputs, cancellation.Token).AsTask();
        await h.Owner.PrepareEntered.Task;
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.Equal(1, h.Owner.PrepareCalls);
        h.Owner.Preparation.SetResult(new(RestrictedPrepareKind.Prepared, h.Candidate));
        Assert.Equal(RestrictedPrepareKind.Prepared, (await h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Kind);
        Assert.Equal(0, h.Owner.StopCalls);
    }

    [Fact]
    public async Task CancelledStopWaitStillLatchesAndSubmitsExactlyOnce()
    {
        var h = new Harness();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop, cancellation.Token).AsTask());
        Assert.Equal(1, h.Owner.StopCalls);
        Assert.Equal(RestrictedPrepareKind.CancelledBeforeRelease, (await h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Kind);
        await h.AcceptStop();
    }

    [Fact]
    public async Task RepeatedStopPreservesFirstReasonAndOriginalDeadline()
    {
        var h = new Harness();
        await h.Operation.RequestStopAsync(RestrictedStopReason.Deadline, waitCancellation: TestContext.Current.CancellationToken);
        var queuedRepeat = await h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop, waitCancellation: TestContext.Current.CancellationToken);
        Assert.True(queuedRepeat.Duplicate);
        Assert.Equal(RestrictedStopReason.Deadline, queuedRepeat.FirstReason);
        await h.AcceptStop(RestrictedStopReason.Deadline);
        var repeated = await h.Operation.RequestStopAsync(RestrictedStopReason.Invalidation, waitCancellation: TestContext.Current.CancellationToken);
        Assert.True(repeated.Duplicate);
        Assert.Equal(20, repeated.AcceptedAtNs);
        Assert.Equal(70, repeated.DeadlineNs);
        Assert.Equal(RestrictedEvidenceState.Unknown, repeated.DurableStop);
        Assert.Equal(1, h.Owner.StopCalls);
    }

    [Fact]
    public async Task LostStopAckNeverRestoresAuthorityOrResubmits()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        await h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop, waitCancellation: TestContext.Current.CancellationToken);
        h.Owner.StopAck.SetException(new IOException("synthetic lost ACK"));
        await h.Operation.StopSubmission;
        var repeat = await h.Operation.RequestStopAsync(RestrictedStopReason.Deadline, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedStopSubmission.Unknown, repeat.Submission);
        Assert.Null(repeat.DeadlineNs);
        Assert.Equal(1, h.Owner.StopCalls);
        Assert.Equal(RestrictedRefusalReason.StopRequested, (await h.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken)).Reason);
    }

    [Theory]
    [InlineData(0)] [InlineData(31)] [InlineData(33)]
    public async Task InvalidCorrelationIsTypedWithoutAdmission(int length)
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        var reply = await h.Operation.RequestReleaseAsync(prepared, ImmutableArray.Create(new byte[length]), waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRequestBinding.InvalidArgument, reply.Binding);
        Assert.Equal(RestrictedRefusalReason.InvalidCorrelation, reply.Reason);
        Assert.Null(reply.OriginalDecision);
        Assert.Null(h.Owner.RetainedRelease);
    }

    [Fact]
    public async Task B3ExchangeAdmitsFirstCorrelationAndPublicFacadeStillExposesNoReleaseSurface()
    {
        // B1 deliberately left pre-retention admission unwired and returned
        // LiveReleaseNotImplemented. B3 replaces that placeholder with the
        // approved R1-F03 Option A admission; this test records the change and
        // the invariant that the public provider interface still has no
        // release method at all.
        var h = new Harness();
        var prepared = await h.Prepare();
        h.Owner.Release.SetResult(Released());
        var reply = await h.Operation.RequestReleaseAsync(prepared,
            ImmutableArray.Create(new byte[32]), waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Released, reply.Kind);
        Assert.Equal(RestrictedRequestBinding.Accepted, reply.Binding);
        Assert.NotNull(reply.OriginalDecision);
        Assert.Single(typeof(IRestrictedOperationOwner).GetMethods(), m => m.Name == "ReleaseAsync");
        Assert.DoesNotContain(typeof(IRestrictedExecutionProvider).GetMethods(), m =>
            m.Name.Contains("Release", StringComparison.Ordinal) || m.Name.Contains("Prepare", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RetainedAmbiguityAlwaysReturnsDuplicateAndCannotReplay(bool stopFirst)
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        if (stopFirst) await h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop, waitCancellation: TestContext.Current.CancellationToken);
        h.Owner.RetainedRelease = Ambiguous(ImmutableArray.Create(new byte[32]));
        if (!stopFirst) await h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop, waitCancellation: TestContext.Current.CancellationToken);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var reply = await h.Operation.RequestReleaseAsync(prepared, ImmutableArray.Create(new byte[32]), waitCancellation: TestContext.Current.CancellationToken);
            Assert.Equal(RestrictedReleaseKind.Duplicate, reply.Kind);
            Assert.Equal(RestrictedRequestBinding.Accepted, reply.Binding);
            Assert.Same(h.Owner.RetainedRelease, reply.OriginalDecision);
            Assert.Equal(RestrictedReleaseEvidence.MayHaveExecuted, reply.OriginalDecision!.Evidence);
            Assert.Equal(RestrictedDelivery.NotAttempted, reply.OriginalDecision.Delivery);
        }
        await h.AcceptStop();
    }

    [Fact]
    public async Task RetainedMismatchDoesNotOverwriteDuplicateAndAllZeroIsPresent()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.Owner.RetainedRelease = Ambiguous(ImmutableArray.Create(new byte[32]));
        var absent = await h.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRequestBinding.Mismatch, absent.Binding);
        Assert.Equal(RestrictedReleaseKind.Duplicate, absent.Kind);
        var wrong = await h.Operation.RequestReleaseAsync(prepared,
            ImmutableArray.Create(Enumerable.Repeat((byte)1, 32).ToArray()), waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRequestBinding.Mismatch, wrong.Binding);
        Assert.Same(absent.OriginalDecision, wrong.OriginalDecision);
        var invalid = await h.Operation.RequestReleaseAsync(prepared, ImmutableArray<byte>.Empty, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRequestBinding.InvalidArgument, invalid.Binding);
        Assert.Equal(RestrictedReleaseKind.Duplicate, invalid.Kind);
        Assert.Same(absent.OriginalDecision, invalid.OriginalDecision);
    }

    [Theory]
    [InlineData(RestrictedReleaseKind.Refused, RestrictedReleaseEvidence.NotCommitted, RestrictedDelivery.NotAttempted)]
    [InlineData(RestrictedReleaseKind.Released, RestrictedReleaseEvidence.Committed, RestrictedDelivery.Delivered)]
    [InlineData(RestrictedReleaseKind.Stopped, RestrictedReleaseEvidence.MayHaveExecuted, RestrictedDelivery.Unknown)]
    public async Task DuplicateIsIndependentOfOriginalDecisionAndSurvivesDisposal(RestrictedReleaseKind kind,
        RestrictedReleaseEvidence evidence, RestrictedDelivery delivery)
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.Owner.RetainedRelease = new(kind, evidence, delivery, evidence == RestrictedReleaseEvidence.Committed
            ? RestrictedEvidenceState.Confirmed : RestrictedEvidenceState.Unknown, null, default, RestrictedEvidenceState.Unknown);
        await h.Operation.DisposeAsync();
        var reply = await h.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Duplicate, reply.Kind);
        Assert.Equal(kind, reply.OriginalDecision!.Kind);
        Assert.Equal(evidence, reply.OriginalDecision.Evidence);
        Assert.Equal(delivery, reply.OriginalDecision.Delivery);
        await h.AcceptStop(RestrictedStopReason.Disposal);
    }

    [Fact]
    public async Task ConcurrentContractCallsPreserveRetainedFactsWithoutWaitingForReleaseWinner()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.Owner.RetainedRelease = Ambiguous(ImmutableArray.Create(new byte[32]));
        var start = Signal<bool>();
        async Task<RestrictedReleaseReply> Call(ImmutableArray<byte> correlation)
        { await start.Task; return await h.Operation.RequestReleaseAsync(prepared, correlation, waitCancellation: TestContext.Current.CancellationToken); }
        var matching = Call(ImmutableArray.Create(new byte[32]));
        var mismatching = Call(default);
        start.SetResult(true);
        var replies = await Task.WhenAll(matching, mismatching);
        Assert.All(replies, reply => Assert.Equal(RestrictedReleaseKind.Duplicate, reply.Kind));
        Assert.Equal(RestrictedRequestBinding.Accepted, replies[0].Binding);
        Assert.Equal(RestrictedRequestBinding.Mismatch, replies[1].Binding);
        Assert.Same(replies[0].OriginalDecision, replies[1].OriginalDecision);
    }

    [Fact]
    public async Task PrepareAckLossIsUnknownAndNeverAutomaticallyReprepared()
    {
        var h = new Harness();
        h.Owner.Preparation.SetException(new IOException("synthetic lost prepare ACK"));
        Assert.Equal(RestrictedPrepareKind.Unknown, (await h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Kind);
        Assert.Equal(RestrictedPrepareKind.Unknown, (await h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Kind);
        Assert.Equal(1, h.Owner.PrepareCalls);
        var failed = new Harness();
        failed.Owner.Preparation.SetResult(new(RestrictedPrepareKind.Failed));
        Assert.Equal(RestrictedPrepareKind.Failed, (await failed.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Kind);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DisposalInhibitsReleaseAndNeverInventsCleanup(bool loseAck)
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.Owner.LoseDisposalReply = loseAck;
        await h.Operation.DisposeAsync();
        await h.Operation.DisposeAsync();
        Assert.Equal(1, h.Owner.DisposeCalls);
        Assert.Equal(1, h.Owner.StopCalls);
        Assert.Equal(RestrictedRefusalReason.Disposed, (await h.Operation.RequestReleaseAsync(prepared, waitCancellation: TestContext.Current.CancellationToken)).Reason);
        Assert.Equal(RestrictedEvidenceState.Unknown, (await h.Operation.ObserveAsync(waitCancellation: TestContext.Current.CancellationToken)).Cleanup.State);
        Assert.Equal(RestrictedRefusalReason.Disposed, (await h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken)).Reason);
        await h.AcceptStop(RestrictedStopReason.Disposal);
    }

    [Fact]
    public async Task DisposalDuringPreparationPreventsLateCapabilityPublication()
    {
        var h = new Harness();
        var preparing = h.Operation.PrepareAsync(Inputs, waitCancellation: TestContext.Current.CancellationToken).AsTask();
        await h.Owner.PrepareEntered.Task;
        await h.Operation.DisposeAsync();
        h.Owner.Preparation.SetResult(new(RestrictedPrepareKind.Prepared, h.Candidate));
        var result = await preparing;
        Assert.Null(result.Preparation);
        Assert.Equal(RestrictedRefusalReason.Disposed, result.Reason);
        await h.AcceptStop(RestrictedStopReason.Disposal);
    }

    [Fact]
    public void CleanupFailureHasPrecedenceWhilePersistenceOnlyUncertaintyRemainsUnknown()
    {
        var yes = RestrictedEvidenceState.Confirmed;
        var unknown = RestrictedEvidenceState.Unknown;
        Assert.Equal(RestrictedEvidenceState.Confirmed, new RestrictedCleanup(yes, yes, yes, yes).State);
        Assert.Equal(RestrictedEvidenceState.Unknown, new RestrictedCleanup(yes, yes, yes, unknown).State);
        Assert.Equal(RestrictedEvidenceState.Failed, new RestrictedCleanup(RestrictedEvidenceState.Failed, yes, yes, unknown).State);
        Assert.Equal(RestrictedEvidenceState.Unknown, new RestrictedCleanup(unknown, yes, yes, yes).State);
    }

    [Fact]
    public void TaggedEvidenceRejectsInventedPayloadsAndPreservesSeparateAxes()
    {
        Assert.Throws<ArgumentException>(() => new RestrictedProcessResult(RestrictedProcessKind.Unknown, exitCode: 0));
        Assert.Throws<ArgumentException>(() => new RestrictedProcessResult(RestrictedProcessKind.Signalled, signal: 9));
        Assert.Throws<ArgumentException>(() => new RestrictedExecResult(RestrictedExecKind.Unknown, errno: 1));
        Assert.Throws<ArgumentException>(() => new RestrictedExecResult(RestrictedExecKind.Confirmed));
        Assert.Throws<ArgumentException>(() => new RestrictedStopReply(RestrictedStopSubmission.LocallyQueued,
            false, RestrictedStopReason.CallerStop, acceptedAtNs: 10, deadlineNs: 20));
        Assert.Throws<ArgumentException>(() => new RestrictedStopReply(RestrictedStopSubmission.RuntimeAccepted,
            false, RestrictedStopReason.CallerStop, acceptedAtNs: 0));
        Assert.Throws<ArgumentException>(() => new RestrictedStopReply(RestrictedStopSubmission.RuntimeAccepted,
            false, RestrictedStopReason.CallerStop, durableStop: RestrictedEvidenceState.Confirmed));
        Assert.Throws<ArgumentException>(() => new RestrictedDeadlines(10, 50, 100, 100, "clock", Guid.NewGuid(),
            Guid.NewGuid(), 1, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new RestrictedStreamCapture(2, 1, false, false, false, true, true));
        Assert.Throws<ArgumentException>(() => new RestrictedStreamCapture(1, 1, false, false, false, false, true));
        Assert.Throws<ArgumentException>(() => new RestrictedReleaseDecision(RestrictedReleaseKind.AmbiguousPersistence,
            RestrictedReleaseEvidence.NotCommitted, RestrictedDelivery.NotAttempted, RestrictedEvidenceState.Unknown,
            true, default, RestrictedEvidenceState.Unknown));
        var delivered = new RestrictedReleaseDecision(RestrictedReleaseKind.Released,
            RestrictedReleaseEvidence.MayHaveExecuted, RestrictedDelivery.Delivered, RestrictedEvidenceState.Unknown,
            null, default, RestrictedEvidenceState.Unknown);
        Assert.Equal(RestrictedReleaseEvidence.MayHaveExecuted, delivered.Evidence);
        Assert.Null(delivered.LiveExecutionExcluded);
        var root = new RestrictedProcessResult(RestrictedProcessKind.Exited, exitCode: 0, rawWaitStatus: 0);
        var helper = new RestrictedProcessResult(RestrictedProcessKind.Signalled, signal: 9, coreDumped: false, rawWaitStatus: 9);
        Assert.NotEqual(root.Kind, helper.Kind);
    }

    [Fact]
    public async Task MissingObservationAxesStayUnknownAndWaitCancellationDoesNotStopOperation()
    {
        var h = new Harness();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => h.Operation.ObserveAsync(0, cancellation.Token).AsTask());
        Assert.Equal(0, h.Owner.StopCalls);
        var observation = await h.Operation.ObserveAsync(ulong.MaxValue, waitCancellation: TestContext.Current.CancellationToken);
        Assert.True(observation.GapOrLoss);
        Assert.Null(observation.ObservedAtNs);
        Assert.Null(observation.Outcome);
        Assert.Null(observation.Exec);
        Assert.Null(observation.Root);
        Assert.Null(observation.Helper);
        Assert.Null(observation.Capture);
        Assert.Equal(RestrictedDomainState.Unknown, observation.Domain);
    }
}
