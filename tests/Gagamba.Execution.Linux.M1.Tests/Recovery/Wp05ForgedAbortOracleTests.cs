// Negative controls for the WP05 forged-identity abort oracle. These are
// portable qualification-harness tests; they prove that the corrected oracle
// still rejects forged release acceptance, transport activity, capability
// consumption, missing abort evidence, journal release/stop facts, and
// unexplained terminal exit codes. They do not modify production behavior.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class Wp05ForgedAbortOracleTests
{
    private static Wp05ForgedAbortOracle.DecisionState ValidDecision =>
        new(ReleaseDecisionKind.Refused, ReleaseDisposition.NotCommitted, DeliveryResult.NotAttempted, false);

    private static Wp05ForgedAbortOracle.JournalState ValidJournal =>
        new(true, PreparationStage.Prepared, HasRelease: false, HasStop: false, Error: string.Empty);

    private static Wp05ForgedAbortOracle.AbortState ValidAbort =>
        new(Stopped: true, WrapperReaped: true, Error: string.Empty, NoTargetInitializationMarkerAbsent: true);

    [Theory]
    [InlineData(133)]
    [InlineData(137)]
    public void ContractualAbortOutcomesAreAccepted(int exitCode)
    {
        Assert.Null(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidAbort, exitCode));
    }

    [Fact]
    public void ForgedReleaseAcceptanceIsRejected()
    {
        var decision = ValidDecision with { Kind = ReleaseDecisionKind.Released };
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            decision, capabilityLive: true, ValidJournal, ValidAbort, 133));
    }

    [Fact]
    public void DurableReleaseFactsAndDeliveryAreRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision with { DurableRelease = ReleaseDisposition.Committed },
            capabilityLive: true, ValidJournal, ValidAbort, 133));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision with { DurableCommitted = true },
            capabilityLive: true, ValidJournal, ValidAbort, 133));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision with { Delivery = DeliveryResult.Delivered },
            capabilityLive: true, ValidJournal, ValidAbort, 133));
    }

    [Fact]
    public void ConsumedCapabilityIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: false, ValidJournal, ValidAbort, 133));
    }

    [Fact]
    public void MissingAbortOrCleanupEvidenceIsRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidAbort with { Stopped = false }, 133));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidAbort with { WrapperReaped = false }, 133));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidAbort with { Error = "cleanup failed" }, 133));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal,
            ValidAbort with { NoTargetInitializationMarkerAbsent = false }, 133));
    }

    [Fact]
    public void JournalReleaseOrStopFactsAreRejected()
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true,
            ValidJournal with { HasRelease = true }, ValidAbort, 133));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true,
            ValidJournal with { HasStop = true }, ValidAbort, 133));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true,
            ValidJournal with { Stage = PreparationStage.None }, ValidAbort, 133));
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true,
            ValidJournal with { Ok = false, Error = "read failed" }, ValidAbort, 133));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(130)]
    [InlineData(139)]
    [InlineData(143)]
    public void UnexplainedTerminalExitCodesAreRejected(int exitCode)
    {
        Assert.NotNull(Wp05ForgedAbortOracle.Validate(
            ValidDecision, capabilityLive: true, ValidJournal, ValidAbort, exitCode));
    }
}
