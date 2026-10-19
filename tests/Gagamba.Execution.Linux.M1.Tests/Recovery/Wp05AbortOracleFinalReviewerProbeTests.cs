// Independent reviewer probes for the R9 structured abort-oracle boundary.
// These do not modify the implementation oracle or the preserved Q1 probe.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05AbortOracleFinalReviewerProbeTests
{
    private static readonly OwnedWrapperIdentity Identity = new(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
        PidFd: 71,
        Pid: 7171);

    private static readonly Wp05ForgedAbortOracle.DecisionState Refused = new(
        ReleaseDecisionKind.Refused,
        ReleaseDisposition.NotCommitted,
        DeliveryResult.NotAttempted,
        DurableCommitted: false);

    private static readonly Wp05ForgedAbortOracle.JournalState Prepared = new(
        Ok: true,
        Stage: PreparationStage.Prepared,
        HasRelease: false,
        HasStop: false,
        Error: string.Empty);

    private static readonly AuthorizedStopEvidence CompletedAbort = new(
        Identity,
        Stopped: true,
        WrapperReaped: true,
        Error: string.Empty,
        NoTargetInitializationMarkerAbsent: true);

    private static readonly ObservedTermination CoordinatorExit = new(
        Identity,
        ObservedTerminationKind.Exited,
        RawCode: 1,
        ExitStatus: 133,
        Signal: null,
        TerminationObservationSource.PidFdWaitIdWnowait,
        Error: string.Empty);

    private static readonly ObservedTermination AuthorizedKill = new(
        Identity,
        ObservedTerminationKind.Killed,
        RawCode: 2,
        ExitStatus: null,
        Signal: 9,
        TerminationObservationSource.PidFdWaitIdWnowait,
        Error: string.Empty);

    [Fact]
    public void PreservedBare137InputCannotBeAccepted()
    {
        var abort = new Wp05ForgedAbortOracle.AbortState(
            Stopped: true,
            WrapperReaped: true,
            Error: string.Empty,
            NoTargetInitializationMarkerAbsent: true);

        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared, abort, wrapperExitCode: 137));
    }

    [Fact]
    public void NormalExit137CannotMasqueradeAsAuthorizedStop()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared, CompletedAbort,
            CoordinatorExit with { ExitStatus = 137 }));
    }

    [Fact]
    public void SignalNineNeedsCompletedAbortEvidence()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared,
            CompletedAbort with { Stopped = false }, AuthorizedKill));
    }

    [Fact]
    public void PidFdAndObservedPidMustMatchIndependentStopRecord()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared,
            CompletedAbort with { Identity = Identity with { PidFd = 72 } }, AuthorizedKill));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared,
            CompletedAbort with { Identity = Identity with { Pid = 7172 } }, AuthorizedKill));
    }

    [Fact]
    public void RefusalTransportAndCapabilityAreLoadBearing()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused with { Kind = ReleaseDecisionKind.Released },
            capabilityLive: true, Prepared, CompletedAbort, CoordinatorExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused with { Delivery = DeliveryResult.Delivered },
            capabilityLive: true, Prepared, CompletedAbort, CoordinatorExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: false, Prepared, CompletedAbort, CoordinatorExit));
    }

    [Fact]
    public void JournalMustRemainPreparedWithoutReleaseOrStopFacts()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared with { HasRelease = true },
            CompletedAbort, CoordinatorExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared with { HasStop = true },
            CompletedAbort, CoordinatorExit));
    }

    [Fact]
    public void CleanupAndConstructorMarkerAreLoadBearing()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared,
            CompletedAbort with { WrapperReaped = false }, CoordinatorExit));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared,
            CompletedAbort with { NoTargetInitializationMarkerAbsent = false }, CoordinatorExit));
    }

    [Fact]
    public void UnknownAndDumpedWaitEvidenceAreRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared, CompletedAbort,
            CoordinatorExit with
            {
                Kind = ObservedTerminationKind.Unknown,
                Source = TerminationObservationSource.None,
                Error = "observer status unavailable",
            }));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared, CompletedAbort,
            CoordinatorExit with
            {
                Kind = ObservedTerminationKind.Dumped,
                RawCode = 3,
                ExitStatus = null,
                Signal = 9,
            }));
    }

    [Fact]
    public void ContractualStructuredOutcomesRemainRepresentable()
    {
        Assert.Null(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared, CompletedAbort, CoordinatorExit));
        Assert.Null(Wp05ForgedAbortOracle.Validate(
            Refused, capabilityLive: true, Prepared, CompletedAbort, AuthorizedKill));
    }
}
