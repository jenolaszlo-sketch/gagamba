// Deterministic B3 contract tests: R1-F03 Option A pre-retention admission,
// single-submission semantics, retained-decision projection and the
// owner-observation mapping. No native workload or real owner is exercised;
// the Linux integration path is covered separately.
using System.Collections.Immutable;
using Gagamba.Execution.Restricted.Contracts;
using Gagamba.Execution.Restricted.Control;
using Gagamba.Execution.Linux.M1.Tests.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.RestrictedControl;

public sealed class RestrictedControlB3Tests
{
    private static readonly RestrictedPreparationInputs Inputs = new("enrolled-tool", "enrolled-input");
    private static readonly ImmutableArray<byte> CorrelationA = ImmutableArray.Create(
        Enumerable.Repeat((byte)0x11, 32).ToArray());
    private static readonly ImmutableArray<byte> CorrelationB = ImmutableArray.Create(
        Enumerable.Repeat((byte)0x22, 32).ToArray());
    private static readonly ImmutableArray<byte> AllZero = ImmutableArray.Create(new byte[32]);
    private static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class Owner : IRestrictedOperationOwner
    {
        public readonly Queue<TaskCompletionSource<RestrictedOwnerReleaseResult>> ReleaseQueue = new();
        public readonly TaskCompletionSource<bool> ReleaseEntered = Signal<bool>();
        public int ReleaseCalls;
        public bool Live = true;
        public RestrictedReleaseDecision? RetainedRelease { get; set; }

        public ValueTask<RestrictedOwnerPrepareResult> PrepareAsync(RestrictedPreparationInputs inputs) =>
            new(Task.FromResult(new RestrictedOwnerPrepareResult(RestrictedPrepareKind.Prepared, Candidate!)));

        public ValueTask<RestrictedOwnerReleaseResult> ReleaseAsync(ImmutableArray<byte> approvalCorrelationSha256)
        {
            Interlocked.Increment(ref ReleaseCalls);
            ReleaseEntered.TrySetResult(true);
            lock (ReleaseQueue)
            {
                return new(ReleaseQueue.Dequeue().Task);
            }
        }

        public ValueTask<RestrictedStopReply> StopAsync(RestrictedStopReason reason)
        {
            Live = false;
            return new(Task.FromResult(new RestrictedStopReply(
                RestrictedStopSubmission.RuntimeAccepted, false, reason, false, 20, 70)));
        }

        public ValueTask<RestrictedCleanup> DisposeAsync()
        {
            Live = false;
            return new(Task.FromResult((RestrictedCleanup)RestrictedCleanup.Unknown));
        }

        public bool IsLive(RestrictedPreparationReference reference) => Live;

        public RestrictedOwnerPreparation? Candidate;
    }

    private sealed class Harness
    {
        public readonly RestrictedProviderDescriptor Descriptor = new();
        public readonly Owner Owner = new();
        public readonly RestrictedExecutionOperation Operation;

        public Harness()
        {
            Operation = new(Descriptor, Owner);
            var manifest = new RestrictedManifestView(ManifestTests.SampleManifest());
            var reference = new RestrictedPreparationReference(Guid.NewGuid(), Guid.NewGuid(),
                Guid.NewGuid(), Guid.NewGuid(), RestrictedManifestCodec.ComputeSha256(manifest).AsSpan());
            Owner.Candidate = new(reference, manifest, Descriptor,
                new RestrictedDeadlines(10, 50_000, 10_200, 30_400, "synthetic-clock", Guid.NewGuid(),
                    Guid.NewGuid(), 1_000_000_000, reference.GenerationId),
                new RestrictedEstablishmentReceipt(1, [1, 2, 3]));
        }

        public async Task<PreparedRestrictedExecution> Prepare() =>
            Assert.IsType<PreparedRestrictedExecution>((await Operation.PrepareAsync(Inputs,
                waitCancellation: TestContext.Current.CancellationToken)).Preparation);

        public TaskCompletionSource<RestrictedOwnerReleaseResult> ExpectCall() =>
            Push(Released() with { Execution = null });

        public TaskCompletionSource<RestrictedOwnerReleaseResult> Push(RestrictedOwnerReleaseResult result)
        {
            var completion = Signal<RestrictedOwnerReleaseResult>();
            lock (Owner.ReleaseQueue)
            {
                Owner.ReleaseQueue.Enqueue(completion);
            }

            return completion;
        }
    }

    internal static RestrictedOwnerReleaseResult RefusedPreRetention(
        RestrictedRefusalReason reason = RestrictedRefusalReason.DeadlineExpired,
        RestrictedRequestBinding binding = RestrictedRequestBinding.Unknown) => new(
        RestrictedReleaseKind.Refused, RestrictedReleaseEvidence.NotCommitted, RestrictedDelivery.NotAttempted,
        RestrictedEvidenceState.Unknown, null, binding, reason);

    internal static RestrictedOwnerReleaseResult Released(RestrictedOwnerExecutionFacts? facts = null) => new(
        RestrictedReleaseKind.Released, RestrictedReleaseEvidence.Committed, RestrictedDelivery.Delivered,
        RestrictedEvidenceState.Confirmed, false, RestrictedRequestBinding.Accepted,
        RestrictedRefusalReason.None, facts);

    internal static RestrictedOwnerExecutionFacts SuccessFacts() => new(
        RestrictedOutcome.Succeeded,
        new RestrictedExecResult(RestrictedExecKind.Confirmed, confirmationBasis: "coordinator-exec-confirmed-frame"),
        new RestrictedProcessResult(RestrictedProcessKind.Exited, exitCode: 0, rawWaitStatus: 0),
        Helper: null, RestrictedDomainState.Empty, 123456789L);

    [Fact]
    public async Task FirstAdmissibleCallBindsAndLaterMatchingCallsAreEvaluatedUnderOriginalGates()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.ExpectCall().SetResult(RefusedPreRetention(binding: RestrictedRequestBinding.Accepted));
        var first = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Refused, first.Kind);
        Assert.Equal(RestrictedRequestBinding.Accepted, first.Binding);
        Assert.Null(first.OriginalDecision);
        Assert.Equal(1, h.Owner.ReleaseCalls);

        // A mismatching call is refused locally without any owner interaction.
        var mismatch = await h.Operation.RequestReleaseAsync(prepared, CorrelationB,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Refused, mismatch.Kind);
        Assert.Equal(RestrictedRequestBinding.Mismatch, mismatch.Binding);
        Assert.Equal(RestrictedRefusalReason.InvalidBinding, mismatch.Reason);
        Assert.Equal(1, h.Owner.ReleaseCalls);

        // The original matching correlation may still be evaluated later.
        h.ExpectCall().SetResult(Released(SuccessFacts()));
        var matching = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Released, matching.Kind);
        Assert.Equal(RestrictedRequestBinding.Accepted, matching.Binding);
        Assert.NotNull(matching.OriginalDecision);
        Assert.True(matching.OriginalDecision!.ApprovalCorrelationSha256.AsSpan().SequenceEqual(CorrelationA.AsSpan()));
        Assert.Equal(2, h.Owner.ReleaseCalls);

        // C4 repeats keep the retained decision with independent binding status.
        var duplicate = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Duplicate, duplicate.Kind);
        Assert.Equal(RestrictedRequestBinding.Accepted, duplicate.Binding);
        Assert.Same(matching.OriginalDecision, duplicate.OriginalDecision);
        var duplicateMismatch = await h.Operation.RequestReleaseAsync(prepared, CorrelationB,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Duplicate, duplicateMismatch.Kind);
        Assert.Equal(RestrictedRequestBinding.Mismatch, duplicateMismatch.Binding);
        Assert.Same(matching.OriginalDecision, duplicateMismatch.OriginalDecision);
        Assert.Equal(2, h.Owner.ReleaseCalls);
    }

    [Fact]
    public async Task PreRetentionMismatchDoesNotConsumeRetainResetOrReplaceAnything()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.ExpectCall().SetResult(RefusedPreRetention(binding: RestrictedRequestBinding.Accepted));
        _ = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        var before = await h.Operation.ObserveAsync(waitCancellation: TestContext.Current.CancellationToken);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var mismatch = await h.Operation.RequestReleaseAsync(prepared, CorrelationB,
                waitCancellation: TestContext.Current.CancellationToken);
            Assert.Equal(RestrictedRequestBinding.Mismatch, mismatch.Binding);
            Assert.Null(mismatch.OriginalDecision);
        }

        Assert.Equal(1, h.Owner.ReleaseCalls); // no submission from any mismatch
        Assert.Null(h.Owner.RetainedRelease);
        // The prepared view, deadlines and operation state are untouched.
        Assert.Same(prepared, (await h.Operation.PrepareAsync(Inputs,
            waitCancellation: TestContext.Current.CancellationToken)).Preparation);
        Assert.Same(prepared.Deadlines, (await h.Operation.PrepareAsync(Inputs,
            waitCancellation: TestContext.Current.CancellationToken)).Preparation!.Deadlines);
        Assert.Equal(before.Reference, (await h.Operation.ObserveAsync(
            waitCancellation: TestContext.Current.CancellationToken)).Reference);
    }

    [Fact]
    public async Task AbsentVersusAllZeroCorrelationAreDistinctBindings()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.ExpectCall().SetResult(RefusedPreRetention(binding: RestrictedRequestBinding.Accepted));
        var absent = await h.Operation.RequestReleaseAsync(prepared,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRequestBinding.Accepted, absent.Binding);
        var allZero = await h.Operation.RequestReleaseAsync(prepared, AllZero,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRequestBinding.Mismatch, allZero.Binding);
        Assert.Equal(1, h.Owner.ReleaseCalls);
    }

    [Fact]
    public async Task ConcurrentMatchingAndMismatchingCallsKeepTheFirstBinding()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        // The first admissible call binds CorrelationA; from then on the
        // binding is stable even while a matching submission is in flight.
        h.ExpectCall().SetResult(RefusedPreRetention(binding: RestrictedRequestBinding.Accepted));
        _ = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);

        var winner = h.ExpectCall();
        var start = Signal<bool>();
        async Task<RestrictedReleaseReply> Call(ImmutableArray<byte> correlation)
        {
            await start.Task;
            return await h.Operation.RequestReleaseAsync(prepared, correlation,
                waitCancellation: TestContext.Current.CancellationToken);
        }

        var matching = Call(CorrelationA);
        var mismatching = Call(CorrelationB);
        start.SetResult(true);
        Assert.Equal(RestrictedRequestBinding.Mismatch, (await mismatching).Binding);
        await h.Owner.ReleaseEntered.Task;
        winner.SetResult(Released());
        Assert.Equal(RestrictedReleaseKind.Released, (await matching).Kind);
        Assert.Equal(2, h.Owner.ReleaseCalls);
    }

    [Fact]
    public async Task ConcurrentLoserIsRefusedWhileTheSubmissionIsInFlight()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        var pending = h.ExpectCall();
        var first = h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken).AsTask();
        await h.Owner.ReleaseEntered.Task;
        var loser = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Refused, loser.Kind);
        Assert.Equal(RestrictedRefusalReason.ReleaseInProgress, loser.Reason);
        Assert.False(loser.Binding == RestrictedRequestBinding.Mismatch);
        pending.SetResult(Released());
        Assert.Equal(RestrictedReleaseKind.Released, (await first).Kind);
        Assert.Equal(1, h.Owner.ReleaseCalls);
    }

    [Fact]
    public async Task CancelledReleaseWaitDoesNotCancelOrRetryTheSubmission()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        var pending = h.ExpectCall();
        using var cancellation = new CancellationTokenSource();
        var waiting = h.Operation.RequestReleaseAsync(prepared, CorrelationA, cancellation.Token).AsTask();
        await h.Owner.ReleaseEntered.Task;
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        Assert.Equal(1, h.Owner.ReleaseCalls);
        pending.SetResult(Released(SuccessFacts()));
        // The submitted decision still lands as the retained fact.
        await h.Operation.ReleaseSubmission!;
        var repeat = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Duplicate, repeat.Kind);
        Assert.Equal(RestrictedReleaseKind.Released, repeat.OriginalDecision!.Kind);
        Assert.Equal(1, h.Owner.ReleaseCalls);
    }

    [Fact]
    public async Task LostReleaseAckIsUnknownAndTheOwnersRetainedTruthSurfacesOnExplicitRepeat()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.ExpectCall().SetException(new IOException("synthetic lost release acknowledgement"));
        var lost = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Unknown, lost.Kind);
        Assert.Equal(RestrictedRefusalReason.OwnerUnavailable, lost.Reason);
        Assert.Null(lost.OriginalDecision);
        // No automatic retry happened; an explicit repeat re-evaluates the
        // original gates and surfaces the owner's retained decision.
        Assert.Equal(1, h.Owner.ReleaseCalls);
        h.ExpectCall().SetResult(Released(SuccessFacts()));
        var repeat = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Released, repeat.Kind);
        Assert.Equal(2, h.Owner.ReleaseCalls);
    }

    [Fact]
    public async Task OwnerRetainedRefusalBecomesTheC4FactWithOriginalCorrelation()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.ExpectCall().SetResult(new RestrictedOwnerReleaseResult(RestrictedReleaseKind.Refused,
            RestrictedReleaseEvidence.MayHaveExecuted, RestrictedDelivery.NotAttempted,
            RestrictedEvidenceState.Unknown, true, RestrictedRequestBinding.Accepted,
            RestrictedRefusalReason.StopRequested));
        var refused = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Refused, refused.Kind);
        Assert.NotNull(refused.OriginalDecision);
        Assert.Equal(RestrictedReleaseEvidence.MayHaveExecuted, refused.OriginalDecision!.Evidence);
        var mismatch = await h.Operation.RequestReleaseAsync(prepared, CorrelationB,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Duplicate, mismatch.Kind);
        Assert.Equal(RestrictedRequestBinding.Mismatch, mismatch.Binding);
        Assert.Same(refused.OriginalDecision, mismatch.OriginalDecision);
        Assert.Equal(1, h.Owner.ReleaseCalls);
    }

    [Fact]
    public async Task ObservationMapsOnlyOwnerObservedAxes()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.ExpectCall().SetResult(Released(SuccessFacts()));
        var released = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Released, released.Kind);
        var observation = await h.Operation.ObserveAsync(waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedProvenance.LiveOwner, observation.Provenance);
        Assert.Equal(RestrictedOutcome.Succeeded, observation.Outcome);
        Assert.Equal(RestrictedExecKind.Confirmed, observation.Exec!.Kind);
        Assert.Equal(RestrictedProcessKind.Exited, observation.Root!.Kind);
        Assert.Equal(0, observation.Root.ExitCode);
        Assert.Equal(RestrictedDomainState.Empty, observation.Domain);
        Assert.Equal(123456789L, observation.ObservedAtNs);
        Assert.Null(observation.Capture);
        Assert.Equal(RestrictedStopCompletion.Unknown, observation.StopCompletion);
        Assert.NotNull(observation.Release);
    }

    [Fact]
    public async Task DeliveredWriteIsNeverConvertedIntoASuccessfulExecutionClaim()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        h.ExpectCall().SetResult(Released()); // delivered write, no native facts
        var released = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Released, released.Kind);
        Assert.Equal(RestrictedDelivery.Delivered, released.OriginalDecision!.Delivery);
        var observation = await h.Operation.ObserveAsync(waitCancellation: TestContext.Current.CancellationToken);
        Assert.Null(observation.Outcome);
        Assert.Null(observation.Exec);
        Assert.Null(observation.Root);
        Assert.Equal(RestrictedDomainState.Unknown, observation.Domain);
    }

    [Fact]
    public async Task StopWinsBeforeAnyOwnerSubmissionAndDisposalKeepsRetainedFacts()
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        await h.Operation.RequestStopAsync(RestrictedStopReason.CallerStop,
            waitCancellation: TestContext.Current.CancellationToken);
        var stopped = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRefusalReason.StopRequested, stopped.Reason);
        Assert.Equal(0, h.Owner.ReleaseCalls);

        var releasedHarness = new Harness();
        var releasedPrepared = await releasedHarness.Prepare();
        releasedHarness.ExpectCall().SetResult(Released(SuccessFacts()));
        var released = await releasedHarness.Operation.RequestReleaseAsync(releasedPrepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        await releasedHarness.Operation.DisposeAsync();
        var afterDisposal = await releasedHarness.Operation.RequestReleaseAsync(releasedPrepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Duplicate, afterDisposal.Kind);
        Assert.Same(released.OriginalDecision, afterDisposal.OriginalDecision);
        Assert.Equal(1, releasedHarness.Owner.ReleaseCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    public async Task InvalidCorrelationLengthRefusesBeforeAnyBindingOrSubmission(int length)
    {
        var h = new Harness();
        var prepared = await h.Prepare();
        var reply = await h.Operation.RequestReleaseAsync(prepared, ImmutableArray.Create(new byte[length]),
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedRequestBinding.InvalidArgument, reply.Binding);
        Assert.Equal(RestrictedRefusalReason.InvalidCorrelation, reply.Reason);
        Assert.Equal(0, h.Owner.ReleaseCalls);
        // No binding was admitted, so a valid call still proceeds.
        h.ExpectCall().SetResult(Released());
        var valid = await h.Operation.RequestReleaseAsync(prepared, CorrelationA,
            waitCancellation: TestContext.Current.CancellationToken);
        Assert.Equal(RestrictedReleaseKind.Released, valid.Kind);
        Assert.Equal(1, h.Owner.ReleaseCalls);
    }
}
