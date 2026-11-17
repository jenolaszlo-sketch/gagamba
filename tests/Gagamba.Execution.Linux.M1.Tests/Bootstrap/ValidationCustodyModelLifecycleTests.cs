using System.Reflection;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Bootstrap.ValidationCustodyModelVectors;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class ValidationCustodyModelLifecycleTests
{
    [Fact]
    public void R07_R08_R12_StateByEventMatrixRejectsAndCloses()
    {
        for (int done = 0; done <= 5; done++)
        {
            foreach (byte eventNumber in new byte[] { 0, 1, 2, 3, 4, 5, 6, 255 })
            {
                if (eventNumber == done + 1 && done < 5)
                    continue;

                using var model = Create();
                for (int step = 1; step <= done; step++)
                    Accept(model, step, step);
                int intended = Math.Min(done + 1, 5);
                AdvanceRefuses($"R07/R08 transition from step {done} to event {eventNumber}",
                    model, (ValidationCustodyEvent)eventNumber, Observed(intended), intended,
                    ValidationCustodyRefusal.Transition, retryStep: intended, retryAt: intended);
            }
        }
    }

    [Fact]
    public void R09_OriginalMonotonicInt64Bounds()
    {
        var input = new Inputs { Admit = long.MaxValue - 1, End = long.MaxValue };
        using (var model = Create(input, long.MaxValue - 2))
        {
            for (int step = 1; step <= 4; step++)
                Accept(model, step, long.MaxValue - 2, Observed(step, input));
            Accept(model, 5, long.MaxValue - 1, Observed(5, input));
        }
        CreateRefuses("R09 negative time", ValidationCustodyRefusal.Context, Expected(), Observed(), long.MinValue);
        CreateRefuses("R09 creation admit equality", ValidationCustodyRefusal.Expired, Expected(input),
            Observed(inputs: input), long.MaxValue - 1);
        CreateRefuses("R09 creation past both deadlines", ValidationCustodyRefusal.Expired, Expected(),
            Observed(), long.MaxValue);
        using (var model = Create(now: 2))
            AdvanceRefuses("R09 backward", model, ValidationCustodyEvent.ObserveIssue, Observed(1), 1,
                ValidationCustodyRefusal.Context, retryAt: 2);
        using (var model = Create(input))
            AdvanceRefuses("R09 issue admit equality", model, ValidationCustodyEvent.ObserveIssue,
                Observed(1, input), long.MaxValue - 1, ValidationCustodyRefusal.Expired, original: input);
        for (int stage = 2; stage <= 5; stage++)
        {
            using var model = Create(input);
            for (int prior = 1; prior < stage; prior++)
                Accept(model, prior, 1, Observed(prior, input));
            long boundary = stage == 5 ? long.MaxValue : long.MaxValue - 1;
            AdvanceRefuses($"R09 event {stage} deadline equality", model, Sequence[stage - 1].Event,
                Observed(stage, input), boundary, ValidationCustodyRefusal.Expired, retryStep: stage,
                original: input);
        }
    }

    [Fact]
    public void R10_R11_PreciseRefusalPrecedence()
    {
        var original = new Inputs();
        var wrong = new Inputs { Controller = Label(55) };
        var malformed = new Inputs { Controller = Guid.Empty };
        var premature = StageFacts(0);
        premature[5] = ValidationCustodyFact.Consistent;
        var badStageAndComparison = Evidence(wrong.Binding(), premature);
        CreateRefuses("R11 Context before Stopped", ValidationCustodyRefusal.Context, Expected(),
            Observed(inputs: malformed), original.End, true);
        CreateRefuses("R11 Stopped before Expired", ValidationCustodyRefusal.Stopped, Expected(),
            badStageAndComparison, original.End, true);
        CreateRefuses("R11 Expired before Comparison", ValidationCustodyRefusal.Expired, Expected(),
            badStageAndComparison, original.End);
        CreateRefuses("R11 Comparison before Evidence", ValidationCustodyRefusal.Comparison, Expected(),
            badStageAndComparison);

        (ValidationCustodyEvent Next, ValidationCustodyEvidence? Evidence, long Time, bool Stop,
            ValidationCustodyRefusal Refusal)[] adjacent =
        [
            (ValidationCustodyEvent.Complete, null, -1, true, ValidationCustodyRefusal.Transition),
            ((ValidationCustodyEvent)255, Observed(1), -1, true, ValidationCustodyRefusal.Transition),
            (ValidationCustodyEvent.ObserveIssue, null, 100, true, ValidationCustodyRefusal.Context),
            (ValidationCustodyEvent.ObserveIssue, Observed(1, malformed), 100, true, ValidationCustodyRefusal.Context),
            (ValidationCustodyEvent.ObserveIssue, Observed(1, original), 100, true, ValidationCustodyRefusal.Stopped),
            (ValidationCustodyEvent.ObserveIssue, Observed(0, wrong), 100, false, ValidationCustodyRefusal.Expired),
            (ValidationCustodyEvent.ObserveIssue, Observed(0, wrong), 1, false, ValidationCustodyRefusal.Comparison),
            (ValidationCustodyEvent.ObserveIssue, Observed(0, original), 1, false, ValidationCustodyRefusal.Evidence)
        ];
        foreach (var row in adjacent)
        {
            using var model = Create();
            AdvanceRefuses("R11 precedence", model, row.Next, row.Evidence, row.Time, row.Refusal, row.Stop);
        }

        using var complete = Create();
        for (int step = 1; step <= 4; step++)
            Accept(complete, step, step);
        AdvanceRefuses("R10 stopped before completion", complete, ValidationCustodyEvent.Complete,
            Observed(5), 5, ValidationCustodyRefusal.Stopped, true, retryStep: 5, retryAt: 5);
    }

    [Fact]
    public void R12_R24_ShapeAndDisposalPublishNoAuthority()
    {
        const BindingFlags declaration = BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var modelType = typeof(ValidationCustodyModel);
        Assert.True(modelType.IsNotPublic && modelType.IsSealed);
        Assert.True(Assert.Single(modelType.GetConstructors(declaration)).IsPrivate);
        Assert.Equal(new[] { "Dispose" }, modelType.GetMethods(declaration).Where(method => method.IsPublic)
            .Select(method => method.Name));
        Assert.Equal(new[] { "Binding", "State" }, typeof(ValidationCustodyModelSnapshot)
            .GetProperties(declaration).Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(typeof(ValidationCustodyModelSnapshot).GetMethods(declaration),
            method => !method.IsSpecialName);

        using var model = Create();
        var prior = Accept(model, 1, 1);
        model.Dispose();
        model.Dispose();
        AdvanceRefuses("R12 disposed", model, ValidationCustodyEvent.ObserveOffer, Observed(2), 2,
            ValidationCustodyRefusal.Transition, retryStep: 2, retryAt: 2);
        Assert.Equal(ValidationCustodyState.IssuedModel, prior.State);
        Assert.Equal(new Inputs().Controller, prior.Binding.ControllerOriginal);
    }

    [Fact]
    public void R13_EveryIntakeAndOutputBufferIsDefensive()
    {
        var expectationInput = new Inputs();
        var evidenceInput = new Inputs();
        var expectationBinding = expectationInput.Binding();
        var evidenceBinding = evidenceInput.Binding();
        Clear(expectationInput.Digests!);
        Clear(evidenceInput.Digests!);
        var expectation = new ValidationCustodyExpectations(expectationBinding);
        var evidence = Evidence(evidenceBinding, StageFacts(0));
        Assert.NotSame(expectationBinding, expectation.Binding);
        Assert.NotSame(evidenceBinding, evidence.Binding);
        Clear(Buffers(expectation.Binding));
        Clear(Buffers(evidence.Binding));
        Assert.Equal(ValidationCustodyRefusal.None,
            ValidationCustodyModel.TryCreate(expectation, evidence, 0, false, out var model));
        Assert.NotNull(model);
        using (model)
        {
            var issue = Accept(model, 1, 1);
            Assert.NotSame(expectation.Binding, issue.Binding);
            Clear(Buffers(issue.Binding));
            var offer = Accept(model, 2, 2);
            for (int i = 0; i < 15; i++)
            {
                Assert.Equal(Enumerable.Repeat((byte)(i + 1), 32), Buffers(issue.Binding)[i]);
                Assert.Equal(Enumerable.Repeat((byte)(i + 1), 32), Buffers(offer.Binding)[i]);
            }
        }
    }

    [Fact]
    public async Task R14_DuplicateContendersCommitAtMostOnce()
    {
        using var model = Create();
        using var atBoundary = new Barrier(3);
        var timeout = TimeSpan.FromSeconds(10);
        var cancellation = TestContext.Current.CancellationToken;
        Task<(ValidationCustodyRefusal Refusal, ValidationCustodyModelSnapshot? Snapshot)> Contender() =>
            Task.Run(() =>
            {
                Assert.True(atBoundary.SignalAndWait(timeout, cancellation));
                var refusal = model.Advance(ValidationCustodyEvent.ObserveIssue, Observed(1), 1, false, out var snapshot);
                return (refusal, snapshot);
            }, cancellation);
        var first = Contender();
        var second = Contender();
        Assert.True(atBoundary.SignalAndWait(timeout, cancellation));
        var results = await Task.WhenAll(first, second).WaitAsync(timeout, cancellation);
        Assert.Equal(ValidationCustodyState.IssuedModel,
            Assert.Single(results, result => result.Refusal == ValidationCustodyRefusal.None).Snapshot!.State);
        Assert.Null(Assert.Single(results, result => result.Refusal == ValidationCustodyRefusal.Transition).Snapshot);
        AdvanceRefuses("R14 duplicate closed the model", model, ValidationCustodyEvent.ObserveOffer,
            Observed(2), 2, ValidationCustodyRefusal.Transition, retryStep: 2, retryAt: 2);
    }

    [Fact]
    public async Task R14_DisposeBeforeAdvancePublishesNoSnapshot()
    {
        using var model = Create();
        var cancellation = TestContext.Current.CancellationToken;
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var advance = Task.Run(async () =>
        {
            await disposed.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellation);
            AdvanceRefuses("R14 dispose-first", model, ValidationCustodyEvent.ObserveIssue, Observed(1), 1,
                ValidationCustodyRefusal.Transition);
        }, cancellation);
        model.Dispose();
        disposed.SetResult();
        await advance.WaitAsync(TimeSpan.FromSeconds(10), cancellation);
    }

    [Fact]
    public void R15_WithdrawalBeforeIssueAndOriginalAfterIssue()
    {
        using (var withdrawn = Create())
        {
            var premises = StageFacts(1);
            premises[2] = ValidationCustodyFact.Contradictory;
            AdvanceRefuses("R15 withdrawn before issue", withdrawn, ValidationCustodyEvent.ObserveIssue,
                Evidence(new Inputs().Binding(), premises), 1, ValidationCustodyRefusal.Evidence);
        }

        using var issued = Create();
        Accept(issued, 1, 1);
        // Later observations retain the originally bound approval and issue premises.
        Accept(issued, 2, 2, Observed(2));
        var replacement = new Inputs();
        replacement.Digests[13]![0] ^= 0x80;
        AdvanceRefuses("R15 substituted approval", issued, ValidationCustodyEvent.ObserveDrop,
            Observed(3, replacement), 3, ValidationCustodyRefusal.Comparison, retryStep: 3, retryAt: 3);
    }

    private static void Clear(IEnumerable<byte[]?> arrays)
    {
        foreach (var buffer in arrays)
            Array.Clear(buffer!);
    }
}
