// B5 T4: typed stop dispatch and population outcomes. Classification derives
// from typed fields only; diagnostic text is never parsed.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05B5C3TypedOutcomeTests
{
    private const long Accepted = 10_000_000_000;
    private const long Deadline = Accepted + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds;

    private static OwnershipSnapshot Record() =>
        RecoveryTestSupport.V3Snapshot(
            stop: new StopRecord(Accepted, StopCauseKind.CallerStop, Deadline),
            cleanup: OwnershipCleanupState.InProgress,
            completedCleanup: (byte)CleanupComponents.DomainRemoval);

    private static (OwnershipJournal Journal, JournalWriterLease Writer) NewWriter()
    {
        var journal = new OwnershipJournal(new FakeJournalStorage(), "/recovery");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        return (journal, lease!);
    }

    private static ComponentState State(ReconcileResult result, CleanupComponents component) =>
        Assert.Single(result.Components!, c => c.Component == component).State;

    private static ReconcileResult Run(FakeDomainBackend backend, long now, out FakeDomainClock clock)
    {
        clock = new FakeDomainClock { NowNanoseconds = now };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var (journal, writer) = NewWriter();
        using (writer)
        {
            return new Reconciler(clock, TimeSpan.FromSeconds(5))
                .Reconcile(Record(), "boot-1", lease, null, journal, writer);
        }
    }

    [Fact]
    public void AcceptedKillAndEmptyObservationConfirmBothDomainComponents()
    {
        var result = Run(
            new FakeDomainBackend { Populated = true, KillAccepted = true },
            Deadline - 1_000_000_000,
            out _);

        Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.JournalFinalization));
        Assert.Equal(ReconcileOutcome.CleanupConfirmed, result.Outcome);
    }

    [Fact]
    public void AcceptedKillThatStaysPopulatedIsUnknownNotFailed()
    {
        var result = Run(
            new FakeDomainBackend { Populated = true, KillAccepted = false },
            Deadline - 100_000_000,
            out _);

        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void AcceptedKillWithUnavailableObservationIsUnknown()
    {
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false, ReadFault = true };
        var result = Run(backend, Deadline - 1_000_000_000, out _);

        Assert.Equal(1, backend.ReadPopulationCalls);
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void DefinitiveKillFailureStaysFailed()
    {
        var result = Run(
            new FakeDomainBackend { Populated = true, KillFault = true },
            Deadline - 1_000_000_000,
            out _);

        Assert.Equal(ComponentState.Failed, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
    }

    [Fact]
    public void DefinitiveKillFailureWithIndependentEmptyObservationStillFailed()
    {
        var result = Run(
            new FakeDomainBackend { Populated = false, KillFault = true },
            Deadline - 1_000_000_000,
            out _);

        Assert.Equal(ComponentState.Failed, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Confirmed, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ReconcileOutcome.Failed, result.Outcome);
    }

    [Fact]
    public void UnknownKillDispatchIsUnknownNotFailed()
    {
        var result = Run(
            new FakeDomainBackend { Populated = true, KillDispatchUnknown = true },
            Deadline - 1_000_000_000,
            out _);

        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainStop));
        Assert.Equal(ComponentState.Unknown, State(result, CleanupComponents.DomainEmpty));
        Assert.Equal(ReconcileOutcome.QuarantinedUnknown, result.Outcome);
    }

    [Fact]
    public void TimeoutDoesNotProveKillFailure()
    {
        var timeout = StopOutcome.Timeout("bounded wait expired");
        Assert.Equal(KillDispatchOutcome.Accepted, timeout.Kill);
        Assert.Equal(StopPopulationObservation.Populated, timeout.Population);
        Assert.True(timeout.TimedOut);
        Assert.False(timeout.Stopped);
    }

    [Fact]
    public void SuccessfulKillWriteDoesNotProveEmptiness()
    {
        var populated = new StopOutcome(
            KillDispatchOutcome.Accepted,
            StopPopulationObservation.Populated,
            TimedOut: true,
            Error: string.Empty);
        Assert.False(populated.Stopped);

        var unknown = new StopOutcome(
            KillDispatchOutcome.Accepted,
            StopPopulationObservation.Unknown,
            TimedOut: false,
            Error: "observation unavailable");
        Assert.False(unknown.Stopped);
    }

    [Fact]
    public void StoppedRequiresAcceptedDispatchAndObservedEmpty()
    {
        Assert.True(StopOutcome.Success.Stopped);
        Assert.False(new StopOutcome(KillDispatchOutcome.NotAttempted,
            StopPopulationObservation.Empty, false, string.Empty).Stopped);
        Assert.False(new StopOutcome(KillDispatchOutcome.Failed,
            StopPopulationObservation.Empty, false, string.Empty).Stopped);
        Assert.False(new StopOutcome(KillDispatchOutcome.Unknown,
            StopPopulationObservation.Empty, false, string.Empty).Stopped);
        Assert.False(new StopOutcome(KillDispatchOutcome.Accepted,
            StopPopulationObservation.NotObserved, false, string.Empty).Stopped);
    }
}
