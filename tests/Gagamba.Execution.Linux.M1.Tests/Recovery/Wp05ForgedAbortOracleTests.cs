// Negative and positive controls for the WP05 forged-identity abort oracle.
// These are portable qualification-harness tests. The corrected oracle must
// reject forged release acceptance, transport activity, capability
// consumption, missing cleanup, journal release/stop facts, bare numeric
// termination, normal exit 137, unexplained SIGKILL, identity mismatches, and
// contradictory or missing raw termination evidence. It must still accept a
// proven coordinator self-exit 133 and a proven authorized-stop SIGKILL.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05ForgedAbortOracleTests
{
    private static readonly Guid ExecutionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GenerationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PreparationId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly OwnedWrapperIdentity ValidIdentity =
        new(ExecutionId, GenerationId, PreparationId, PidFd: 42, Pid: 4242);

    private static readonly Wp05ForgedAbortOracle.DecisionState ValidDecision =
        new(ReleaseDecisionKind.Refused, ReleaseDisposition.NotCommitted,
            DeliveryResult.NotAttempted, DurableCommitted: false);

    private static readonly Wp05ForgedAbortOracle.JournalState ValidJournal =
        new(Ok: true, Stage: PreparationStage.Prepared, HasRelease: false,
            HasStop: false, Error: string.Empty);

    private static readonly AuthorizedStopEvidence ValidStop =
        new(ValidIdentity, Stopped: true, WrapperReaped: true,
            Error: string.Empty, NoTargetInitializationMarkerAbsent: true);

    private static readonly ObservedTermination CoordinatorSelfExit =
        new(ValidIdentity, ObservedTerminationKind.Exited, RawCode: 1,
            ExitStatus: 133, Signal: null,
            TerminationObservationSource.PidFdWaitIdWnowait, Error: string.Empty);

    private static readonly ObservedTermination AuthorizedSigkill =
        new(ValidIdentity, ObservedTerminationKind.Killed, RawCode: 2,
            ExitStatus: null, Signal: 9,
            TerminationObservationSource.PidFdWaitIdWnowait, Error: string.Empty);

    [Fact]
    public void ContractualAbortOutcomesAreAccepted()
    {
        Assert.Null(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop, CoordinatorSelfExit));
        Assert.Null(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop, AuthorizedSigkill));
    }

    [Theory]
    [InlineData(133)]
    [InlineData(137)]
    [InlineData(139)]
    public void BareNumericExitCodesAreRejected(int exitCode)
    {
        var abort = new Wp05ForgedAbortOracle.AbortState(
            Stopped: true, WrapperReaped: true, Error: string.Empty,
            NoTargetInitializationMarkerAbsent: true);
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, abort, exitCode));
    }

    [Fact]
    public void ForgedReleaseAcceptanceIsRejected()
    {
        var decision = ValidDecision with { Kind = ReleaseDecisionKind.Released };
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            decision, capabilityLive: true, ValidJournal, ValidStop, CoordinatorSelfExit));
    }

    [Fact]
    public void DurableReleaseFactsAndDeliveryAreRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision with { DurableRelease = ReleaseDisposition.Committed },
            capabilityLive: true, ValidJournal, ValidStop, CoordinatorSelfExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision with { DurableCommitted = true },
            capabilityLive: true, ValidJournal, ValidStop, CoordinatorSelfExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision with { Delivery = DeliveryResult.Delivered },
            capabilityLive: true, ValidJournal, ValidStop, CoordinatorSelfExit));
    }

    [Fact]
    public void ConsumedCapabilityIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: false, ValidJournal, ValidStop, CoordinatorSelfExit));
    }

    [Fact]
    public void MissingAbortOrCleanupEvidenceIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidStop with { Stopped = false }, CoordinatorSelfExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidStop with { WrapperReaped = false }, CoordinatorSelfExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidStop with { Error = "cleanup failed" }, CoordinatorSelfExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidStop with { NoTargetInitializationMarkerAbsent = false }, CoordinatorSelfExit));
    }

    [Fact]
    public void JournalReleaseOrStopFactsAreRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true,
            ValidJournal with { HasRelease = true }, ValidStop, CoordinatorSelfExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true,
            ValidJournal with { HasStop = true }, ValidStop, CoordinatorSelfExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true,
            ValidJournal with { Stage = PreparationStage.None }, ValidStop, CoordinatorSelfExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true,
            ValidJournal with { Ok = false, Error = "read failed" }, ValidStop, CoordinatorSelfExit));
    }

    [Fact]
    public void NormalExit137IsRejectedAsAuthorizedForcedStop()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            CoordinatorSelfExit with { ExitStatus = 137 }));
    }

    [Fact]
    public void KilledWithoutStopProvenanceIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidStop with { Stopped = false }, AuthorizedSigkill));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidStop with { WrapperReaped = false }, AuthorizedSigkill));
    }

    [Fact]
    public void MissingOrUnusableExpectedIdentityIsRejected()
    {
        var missing = new OwnedWrapperIdentity(
            Guid.Empty, Guid.Empty, Guid.Empty, PidFd: -1, Pid: 0);
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            new AuthorizedStopEvidence(
                missing, Stopped: true, WrapperReaped: true,
                Error: string.Empty, NoTargetInitializationMarkerAbsent: true),
            CoordinatorSelfExit));
    }

    [Fact]
    public void TerminationIdentityMismatchIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            AuthorizedSigkill with { Identity = ValidIdentity with { PidFd = 43 } }));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            AuthorizedSigkill with { Identity = ValidIdentity with { Pid = 4243 } }));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            AuthorizedSigkill with
            {
                Identity = ValidIdentity with { ExecutionId = Guid.NewGuid() },
            }));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            AuthorizedSigkill with
            {
                Identity = ValidIdentity with { GenerationId = Guid.NewGuid() },
            }));
    }

    [Fact]
    public void StopForDifferentDomainIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidStop with { Identity = ValidIdentity with { PreparationId = Guid.NewGuid() } },
            AuthorizedSigkill));
    }

    [Fact]
    public void WrongSignalIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            AuthorizedSigkill with { Signal = 15 }));
    }

    [Fact]
    public void ContradictoryTerminationsAreRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            CoordinatorSelfExit with { Signal = 9 }));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            AuthorizedSigkill with { ExitStatus = 127 }));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            AuthorizedSigkill with { RawCode = 1 }));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            CoordinatorSelfExit with { RawCode = 2 }));
    }

    [Fact]
    public void MissingOrConsumedRawWaitEvidenceIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            CoordinatorSelfExit with
            {
                Kind = ObservedTerminationKind.Unknown,
                Source = TerminationObservationSource.None,
                Error = "waitid errno 10 (ECHILD)",
            }));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            CoordinatorSelfExit with
            {
                Identity = ValidIdentity with { PidFd = -1, Pid = 0 },
            }));
    }

    [Fact]
    public void DumpedTerminationIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            new ObservedTermination(ValidIdentity, ObservedTerminationKind.Dumped, RawCode: 3,
                ExitStatus: null, Signal: 9,
                TerminationObservationSource.PidFdWaitIdWnowait, Error: string.Empty)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(130)]
    [InlineData(139)]
    [InlineData(143)]
    public void UnexplainedNormalExitCodesAreRejected(int exitCode)
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidStop,
            CoordinatorSelfExit with { ExitStatus = exitCode }));
    }
}
