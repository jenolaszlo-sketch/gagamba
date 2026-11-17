namespace Gagamba.Execution.Linux.M1.Bootstrap;

internal sealed class ValidationCustodyModel : IDisposable
{
    private readonly object gate = new();
    private ValidationCustodyBinding? expected;
    private ValidationCustodyState state;
    private long lastObservedNs;

    private ValidationCustodyModel(ValidationCustodyBinding expected, long nowNs)
    {
        this.expected = expected.Copy();
        state = ValidationCustodyState.PendingModel;
        lastObservedNs = nowNs;
    }

    internal static ValidationCustodyRefusal TryCreate(
        ValidationCustodyExpectations? expectations,
        ValidationCustodyEvidence? evidence,
        long nowNs,
        bool stopObserved,
        out ValidationCustodyModel? model)
    {
        model = null;
        if (expectations?.Binding is not { IsWellFormed: true } expected)
        {
            return ValidationCustodyRefusal.Context;
        }

        var refusal = CheckObservation(expected, evidence, nowNs, 0, stopObserved,
            ValidationCustodyState.PendingModel);
        if (refusal == ValidationCustodyRefusal.None)
        {
            model = new ValidationCustodyModel(expected, nowNs);
        }

        return refusal;
    }

    internal ValidationCustodyRefusal Advance(
        ValidationCustodyEvent next,
        ValidationCustodyEvidence? evidence,
        long nowNs,
        bool stopObserved,
        out ValidationCustodyModelSnapshot? snapshot)
    {
        snapshot = null;
        lock (gate)
        {
            try
            {
                var nextState = GetNextState(state, next);
                if (nextState is null || expected is not { } original)
                {
                    return ValidationCustodyRefusal.Transition;
                }

                var refusal = CheckObservation(original, evidence, nowNs, lastObservedNs, stopObserved,
                    nextState.Value);
                if (refusal != ValidationCustodyRefusal.None)
                {
                    return refusal;
                }

                state = nextState.Value;
                lastObservedNs = nowNs;
                var result = new ValidationCustodyModelSnapshot(state, original);
                if (state == ValidationCustodyState.Closed)
                {
                    Close();
                }

                snapshot = result;
                return ValidationCustodyRefusal.None;
            }
            finally
            {
                // Covers both explicit refusals and exceptions before publication of a snapshot.
                if (snapshot is null)
                {
                    Close();
                }
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            Close();
        }
    }

    private void Close()
    {
        state = ValidationCustodyState.Closed;
        expected = null;
    }

    private static ValidationCustodyState? GetNextState(ValidationCustodyState state, ValidationCustodyEvent next) =>
        (state, next) switch
        {
            (ValidationCustodyState.PendingModel, ValidationCustodyEvent.ObserveIssue) =>
                ValidationCustodyState.IssuedModel,
            (ValidationCustodyState.IssuedModel, ValidationCustodyEvent.ObserveOffer) =>
                ValidationCustodyState.OfferedModel,
            (ValidationCustodyState.OfferedModel, ValidationCustodyEvent.ObserveDrop) =>
                ValidationCustodyState.DroppedModel,
            (ValidationCustodyState.DroppedModel, ValidationCustodyEvent.ObserveConsume) =>
                ValidationCustodyState.ConsumedModel,
            (ValidationCustodyState.ConsumedModel, ValidationCustodyEvent.Complete) =>
                ValidationCustodyState.Closed,
            _ => null
        };

    private static ValidationCustodyRefusal CheckObservation(
        ValidationCustodyBinding expected,
        ValidationCustodyEvidence? evidence,
        long nowNs,
        long earliestNs,
        bool stopObserved,
        ValidationCustodyState destination)
    {
        if (evidence is not { HasDefinedFacts: true, Binding: { IsWellFormed: true } observed } ||
            nowNs < earliestNs)
        {
            return ValidationCustodyRefusal.Context;
        }

        if (stopObserved)
        {
            return ValidationCustodyRefusal.Stopped;
        }

        // Both deadlines were checked for shape. The original admission bound is the tighter one.
        long deadline = destination == ValidationCustodyState.Closed
            ? expected.SessionEndNs
            : expected.AdmitBeforeNs;
        if (nowNs >= deadline)
        {
            return ValidationCustodyRefusal.Expired;
        }

        if (!expected.Matches(observed))
        {
            return ValidationCustodyRefusal.Comparison;
        }

        return evidence.MatchesStage(destination) ? ValidationCustodyRefusal.None : ValidationCustodyRefusal.Evidence;
    }
}
