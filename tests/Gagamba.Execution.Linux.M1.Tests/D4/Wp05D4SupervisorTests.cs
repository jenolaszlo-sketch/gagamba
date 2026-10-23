// D4 test-only supervisor and real-process release probes. The supervisor
// creates only test-owned processes and files; it never holds the journal
// writer for release purposes. After owner death it may transiently acquire
// the fence only to prove the owner released it. Linux-only.
using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.D4;

[System.Runtime.Versioning.SupportedOSPlatform("linux")]
[Trait("Category", "ProcessGlobalReaping")]
public sealed class Wp05D4SupervisorTests
{
    private static readonly Lazy<string> OwnerDll = new(EnsureOwnerBuilt);
    private static readonly object BuildGate = new();

    [Fact]
    public async Task D4_1_RealHeldReleaseHasOneFd4ObservationAndCleanup()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-1");
        c.StartOwner("release", "--hold-before-release");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("release-ready", cancellation);
        Assert.True(c.OwnerAlive);
        Assert.Equal(c.Owner!.Id, c.ReadyOwnerPid);
        Assert.Equal(c.OwnerStart, c.LiveOwnerStart);
        Assert.True(c.OwnerStart.Contains($"{c.Owner.Id}:", StringComparison.Ordinal));
        Assert.True(HasFdLink(c.Owner.Id, c.JournalLockPath), "owner must hold the journal lock FD");
        Assert.False(HasFdLink(Environment.ProcessId, c.JournalLockPath), "supervisor must not hold the journal lock");
        Assert.True(HasFd(c.Owner.Id, c.ControlFd), "owner control writer FD missing");
        c.ReleaseGo();

        await c.WaitExit(cancellation);
        Assert.Equal(0, c.Owner.ExitCode);
        var result = c.ReadResult();
        Assert.Equal("release", result["mode"]);
        Assert.Equal("Released", result["decision_kind"]);
        Assert.Equal("Delivered", result["delivery"]);
        Assert.Equal("True", result["durable_committed"]);
        Assert.Equal("1", result["transport_attempts"]);
        Assert.Equal("0", result["coordinator_exit"]);
        var witness = D4Witness.Read(c.WitnessPath);
        Assert.Equal(1, Count(witness, "PreWriteWitnessAcknowledged"));
        Assert.Equal(1, Count(witness, "WriteReturned"));
        Assert.Equal(1, Count(witness, "CoordinatorExitObserved"));
        Assert.Equal(0, Count(witness, "TestTransportAttempt"));

        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Committed, latest.Snapshot!.Release!.Tag);
        Assert.Equal(DeliveryResult.Delivered, latest.Snapshot.Release.DeliveryResult);
        c.AssertJournalFenceReleased();

        var replacement = await c.StartReplacement("recover", cancellation);
        Assert.Equal(0, replacement.ExitCode);
        var replacementResult = c.ReadResult(replacement.ResultPath);
        Assert.Equal("false", replacementResult["release_attempted"]);
        Assert.Equal("Committed", replacementResult["release_tag"]);
        Assert.Equal(1, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_2_CrashAfterDurableIntentRecoversAndDoesNotResend()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-2");
        c.StartOwner("release", "--barrier-name", "after-intent");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-intent", cancellation);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Intent, latest.Snapshot!.Release!.Tag);
        c.AssertJournalFenceReleased();
        var replacement = await c.StartReplacement("recover", cancellation);
        Assert.Equal(0, replacement.ExitCode);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("Intent", result["release_tag"]);
        Assert.Equal("MayHaveExecuted", result["classification_release"]);
        Assert.Equal("false", result["release_attempted"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "TestTransportAttempt"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_2b_ReplacementRetryOverDurableIntentIsRefusedWithoutTransport()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-2b");
        c.StartOwner("release", "--barrier-name", "after-intent");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-intent", cancellation);
        await c.KillOwner(cancellation);
        var first = await c.StartReplacement("recover", cancellation);
        Assert.Equal(0, first.ExitCode);
        var retry = await c.StartReplacement("retry-release", cancellation);
        Assert.Equal(0, retry.ExitCode);
        var result = c.ReadResult(retry.ResultPath);
        Assert.Equal("true", result["release_attempted"]);
        Assert.Equal("Refused", result["replacement_decision"]);
        Assert.Equal("NotCommitted", result["replacement_durable"]);
        Assert.Equal("0", result["replacement_transport_attempts"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "TestTransportAttempt"));
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Intent, latest.Snapshot!.Release!.Tag);
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_3_CrashAfterFd4BeforeCommittedKeepsIntentAndObservedWriteOnly()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-3");
        c.StartOwner("release", "--barrier-name", "after-write");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-write", cancellation);
        Assert.Equal(1, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        Assert.Equal(1, Count(D4Witness.Read(c.WitnessPath), "WriteReturned"));
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Intent, latest.Snapshot!.Release!.Tag);
        c.AssertJournalFenceReleased();
        var replacement = await c.StartReplacement("recover", cancellation);
        Assert.Equal(0, replacement.ExitCode);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("Intent", result["release_tag"]);
        Assert.Equal("MayHaveExecuted", result["classification_release"]);
        Assert.Equal("false", result["release_attempted"]);
        Assert.Equal(1, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "CoordinatorExitObserved"));
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "TestTransportAttempt"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_4_CrashAfterCommittedPreservesCommitWithoutSecondWrite()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-4");
        c.StartOwner("release", "--barrier-name", "after-committed");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-committed", cancellation);
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Committed, latest.Snapshot!.Release!.Tag);
        c.AssertJournalFenceReleased();
        var replacement = await c.StartReplacement("recover", cancellation);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("Committed", result["release_tag"]);
        Assert.Equal("false", result["release_attempted"]);
        Assert.Equal(1, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_5_CrashAfterConsumeBeforeWriteHasNoFd4Witness()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-5");
        c.StartOwner("release", "--barrier-name", "after-consume");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-consume", cancellation);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Intent, latest.Snapshot!.Release!.Tag);
        var replacement = await c.StartReplacement("recover", cancellation);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("Intent", result["release_tag"]);
        Assert.Equal("false", result["release_attempted"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_6_AmbiguousSealFailureRecoversIntentAndDoesNotResend()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-6");
        c.StartOwner("ambiguous-intent", "--fault", "ambiguous-seal", "--hold");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-ambiguous", cancellation);
        var ownerResult = c.ReadResult();
        Assert.Equal("AmbiguousPersistence", ownerResult["decision_kind"]);
        Assert.Equal("MayHaveExecuted", ownerResult["durable_release"]);
        Assert.Equal("NotAttempted", ownerResult["delivery"]);
        Assert.Equal("False", ownerResult["durable_committed"]);
        Assert.Equal("True", ownerResult["live_execution_excluded"]);
        Assert.Equal("False", ownerResult["capability_live"]);
        Assert.Contains("ambiguous journal persistence", ownerResult["decision_reason"], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("0", ownerResult["transport_attempts"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        await c.KillOwner(cancellation);
        var before = ReadJournal(c.JournalPath);
        Assert.False(before.Ok);
        Assert.Contains("seal", before.Error, StringComparison.OrdinalIgnoreCase);
        var replacement = await c.StartReplacement("recover", cancellation);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("Intent", result["release_tag"]);
        Assert.Equal("MayHaveExecuted", result["classification_release"]);
        Assert.Equal("false", result["release_attempted"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_7_DurableStopRecoversWithoutRelease()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-7");
        c.StartOwner("stop", "--barrier-name", "after-stop");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-stop", cancellation);
        var ownerResult = c.ReadResult();
        Assert.Equal("True", ownerResult["stop_accepted"]);
        Assert.Equal("True", ownerResult["stop_durable"]);
        var acceptedAt = long.Parse(ownerResult["stop_accepted_at"]);
        var persistedDeadline = long.Parse(ownerResult["stop_deadline"]);
        Assert.Equal(
            acceptedAt + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds,
            persistedDeadline);
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.NotNull(latest.Snapshot!.Stop);
        Assert.Equal(acceptedAt, latest.Snapshot.Stop!.StopAcceptedAtNanoseconds);
        Assert.Equal(persistedDeadline, latest.Snapshot.Stop.StopDeadlineNanoseconds);
        var replacement = await c.StartReplacement("recover", cancellation);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("true", result["stop"]);
        Assert.Equal("false", result["release_attempted"]);
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_8_FailedDurableStopAllowsOnlyDiagnosticReplacementRelease()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-8");
        c.StartOwner("stop-fail", "--barrier-name", "after-stop");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-stop", cancellation);
        var ownerResult = c.ReadResult();
        Assert.Equal("True", ownerResult["stop_accepted"]);
        Assert.Equal("False", ownerResult["stop_durable"]);
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Null(latest.Snapshot!.Stop);
        var recovery = await c.StartReplacement("recover", cancellation);
        var recoveryResult = c.ReadResult(recovery.ResultPath);
        Assert.Equal("false", recoveryResult["stop"]);
        Assert.Equal("false", recoveryResult["release_attempted"]);

        var replacement = await c.StartReplacement("replace-release", cancellation);
        Assert.Equal(0, replacement.ExitCode);
        var replacementResult = c.ReadResult(replacement.ResultPath);
        Assert.Equal("true", replacementResult["release_attempted"]);
        Assert.Equal("new-test-only-capability", replacementResult["release_source"]);
        Assert.Equal("Released", replacementResult["replacement_decision"]);
        Assert.Equal("1", replacementResult["replacement_transport_attempts"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        Assert.Equal(1, Count(D4Witness.Read(c.WitnessPath), "TestTransportAttempt"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_9_OwnerConcurrentDuplicateHasOneFd4Attempt()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-9");
        c.StartOwner("duplicate", "--hold");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-duplicate", cancellation);
        var result = c.ReadResult();
        Assert.Equal("Released", result["winner_kind"]);
        Assert.Equal("Refused", result["concurrent_kind"]);
        Assert.Equal("Duplicate", result["later_kind"]);
        Assert.Equal("1", result["transport_attempts"]);
        Assert.Equal("0", result["coordinator_exit"]);
        Assert.Equal(1, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Committed, latest.Snapshot!.Release!.Tag);
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_10_PreIntentStorageGateLeavesNoIntentOrFd4()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-10");
        c.StartOwner("release", "--storage-gate", "create:gen.seal");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("storage:create:gen.seal", cancellation);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Null(latest.Snapshot!.Release);
        var replacement = await c.StartReplacement("recover", cancellation);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("false", result["release_attempted"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_11_DuringIntentStorageGatePreservesActualJournalState()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-11");
        c.StartOwner("release", "--storage-gate", "write:gen.tmp");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("storage:write:gen.tmp", cancellation);
        await c.KillOwner(cancellation);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        var latest = ReadJournal(c.JournalPath);
        Assert.False(latest.Ok, "an interrupted candidate write must not be reported as acknowledged");
        Assert.Contains("seal", latest.Error, StringComparison.OrdinalIgnoreCase);
        var replacement = await c.StartReplacement("recover", cancellation);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("false", result["release_attempted"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_12_ReplacementCannotAcquireWhileOwnerIsAlive()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-12");
        c.StartOwner("release", "--barrier-name", "after-intent");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-intent", cancellation);
        var early = await c.StartReplacement("recover", cancellation);
        Assert.Equal(3, early.ExitCode);
        var earlyResult = c.ReadResult(early.ResultPath);
        Assert.Equal("refused", earlyResult["writer"]);
        await c.KillOwner(cancellation);
        c.AssertJournalFenceReleased();
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_13_StopPersistenceGateDoesNotFabricateStopOrRelease()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-13");
        c.StartOwner("stop", "--storage-gate", "write:gen.tmp");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("storage:write:gen.tmp", cancellation);
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.False(latest.Ok, "interrupted stop persistence must remain explicitly uncertain");
        var replacement = await c.StartReplacement("recover", cancellation);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("false", result["release_attempted"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "TestTransportAttempt"));
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D4_14_TwoRecoveryContendersHaveOneEffectiveWriterAndNoRelease()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D4-14");
        var (first, firstResultPath) = c.StartReplacementProcess("contend", "--hold-ms", "3000");
        using (first)
        {
            await c.WaitBarrier("contend-held", cancellation);
            var second = await c.StartReplacement("contend", cancellation, "--hold-ms", "100");
            var secondResult = c.ReadResult(second.ResultPath);
            Assert.Equal("refused", secondResult["writer"]);
            await first.WaitForExitAsync(cancellation);
            var firstResult = c.ReadResult(firstResultPath);
            Assert.Equal("acquired", firstResult["writer"]);
            Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
            Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "TestTransportAttempt"));
        }
    }

    [Fact]
    public async Task D5_CR3_TwoIndependentOwnersHaveOneReleaseAndOneRefusal()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D5-CR3");
        c.StartOwner("release", "--hold-before-release");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("release-ready", cancellation);
        var fence = await c.StartReplacement("recover", cancellation);
        Assert.Equal(3, fence.ExitCode);
        Assert.Equal("refused", c.ReadResult(fence.ResultPath)["writer"]);
        var earlyOwner = await c.StartReplacement("release-nowriter", cancellation);
        Assert.Equal(0, earlyOwner.ExitCode);
        var earlyResult = c.ReadResult(earlyOwner.ResultPath);
        Assert.Equal("not-acquired", earlyResult["writer"]);
        Assert.Equal("Refused", earlyResult["decision_kind"]);
        Assert.Equal("0", earlyResult["transport_attempts"]);
        c.ReleaseGo();
        await c.WaitExit(cancellation);
        Assert.Equal(0, c.Owner!.ExitCode);
        var first = c.ReadResult();
        Assert.Equal("Released", first["decision_kind"]);
        Assert.Equal("1", first["transport_attempts"]);
        var second = await c.StartReplacement("retry-release", cancellation);
        Assert.Equal(0, second.ExitCode);
        var secondResult = c.ReadResult(second.ResultPath);
        Assert.Equal("true", secondResult["release_attempted"]);
        Assert.Equal("Refused", secondResult["replacement_decision"]);
        Assert.Equal("0", secondResult["replacement_transport_attempts"]);
        Assert.Equal(1, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "TestTransportAttempt"));
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Committed, latest.Snapshot!.Release!.Tag);
        c.AssertCgroupRemoved();
    }

    [Fact]
    public async Task D5_CR3b_DurableStopRefusesReleaseAcrossFreshLease()
    {
        SkipUnlessQualified();
        var cancellation = TestContext.Current.CancellationToken;
        using var c = new D4Case("D5-CR3b");
        c.StartOwner("stop-then-release", "--barrier-name", "after-stop");
        await c.WaitReady(cancellation);
        await c.WaitBarrier("after-stop", cancellation);
        var ownerResult = c.ReadResult();
        Assert.Equal("True", ownerResult["stop_accepted"]);
        Assert.Equal("True", ownerResult["stop_durable"]);
        Assert.Equal("Refused", ownerResult["release_kind"]);
        Assert.Equal("False", ownerResult["capability_live"]);
        await c.KillOwner(cancellation);
        var latest = ReadJournal(c.JournalPath);
        Assert.True(latest.Ok, latest.Error);
        Assert.NotNull(latest.Snapshot!.Stop);
        Assert.Null(latest.Snapshot.Release);
        var replacement = await c.StartReplacement("retry-release", cancellation);
        Assert.Equal(0, replacement.ExitCode);
        var result = c.ReadResult(replacement.ResultPath);
        Assert.Equal("true", result["release_attempted"]);
        Assert.Equal("Refused", result["replacement_decision"]);
        Assert.Equal("0", result["replacement_transport_attempts"]);
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "PreWriteWitnessAcknowledged"));
        Assert.Equal(0, Count(D4Witness.Read(c.WitnessPath), "TestTransportAttempt"));
        c.AssertCgroupRemoved();
    }

    private static void SkipUnlessQualified()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("linux only");
            return;
        }

        if (Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP") is not { Length: > 0 } delegated
            || !Directory.Exists(delegated))
        {
            Assert.Skip("Unavailable: GAGAMBA_DELEGATED_CGROUP is not set or missing");
            return;
        }

        var uid = StatusField("Uid").Split('\t')[0];
        if (uid == "0") Assert.Skip("Unqualified: test process is root");
        if (StatusField("CapEff") != "0000000000000000")
            Assert.Skip($"Unqualified: CapEff={StatusField("CapEff")}");
        var root = RepositoryRoot();
        foreach (var artifact in new[]
                 {
                     "tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture",
                     "artifacts/linux-m1/wp04/exec-coordinator",
                     "artifacts/linux-m1/wp04/seccomp-filter.bin",
                 })
        {
            if (!File.Exists(Path.Combine(root, artifact)))
                Assert.Skip($"HELD - REQUIRED ARTIFACT UNAVAILABLE: {artifact}");
        }
    }

    private static string StatusField(string name)
    {
        foreach (var line in File.ReadAllLines("/proc/self/status"))
        {
            if (line.StartsWith(name + ":", StringComparison.Ordinal))
                return line[(name.Length + 1)..].Trim();
        }

        return string.Empty;
    }

    private static string EnsureOwnerBuilt()
    {
        lock (BuildGate)
        {
            var root = RepositoryRoot();
            var project = Path.Combine(root, "tests", "Fixtures", "Wp05D4Owner", "Wp05D4Owner.csproj");
            var dll = Path.Combine(root, "tests", "Fixtures", "Wp05D4Owner", "bin", "Release", "net10.0", "Wp05D4Owner.dll");
            if (File.Exists(dll)) return dll;
            var start = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = root,
            };
            start.ArgumentList.Add("build");
            start.ArgumentList.Add(project);
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("Release");
            start.ArgumentList.Add("--nologo");
            using var build = Process.Start(start) ?? throw new InvalidOperationException("dotnet build did not start");
            var output = build.StandardOutput.ReadToEnd();
            var error = build.StandardError.ReadToEnd();
            build.WaitForExit();
            if (build.ExitCode != 0 || !File.Exists(dll))
                throw new InvalidOperationException($"D4 owner build failed ({build.ExitCode}): {output} {error}");
            return dll;
        }
    }

    private static string RepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable("GAGAMBA_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configured))
            return Path.GetFullPath(configured);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Gagamba.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Gagamba repository root not found");
    }

    private static JournalReadResult ReadJournal(string path) =>
        new OwnershipJournal(new JournalFileStorage(), path).ReadLatest();

    private static int Count(string[] lines, string eventName) =>
        lines.Count(line => line.Contains($"event={eventName} ", StringComparison.Ordinal));

    private static bool HasFd(int pid, string fd) =>
        int.TryParse(fd, out var number) && File.Exists($"/proc/{pid}/fd/{number}");

    private static bool HasFdLink(int pid, string target)
    {
        var directory = $"/proc/{pid}/fd";
        if (!Directory.Exists(directory)) return false;
        try
        {
            foreach (var fd in Directory.EnumerateFiles(directory))
            {
                var link = new FileInfo(fd).LinkTarget;
                if (link == target) return true;
            }
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        return false;
    }

    private sealed class D4Case : IDisposable
    {
        private readonly string _evidenceRoot;
        private readonly string _caseRoot;
        private Process? _owner;

        public D4Case(string name)
        {
            Name = name;
            _evidenceRoot = Environment.GetEnvironmentVariable("D4_EVIDENCE") ?? string.Empty;
            _caseRoot = Path.Combine(
                _evidenceRoot.Length > 0 ? _evidenceRoot : Path.GetTempPath(),
                "gagamba-d4-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_caseRoot);
            JournalPath = Path.Combine(_caseRoot, "journal");
            ReadyPath = Path.Combine(_caseRoot, "owner.ready");
            BarrierPath = Path.Combine(_caseRoot, "barrier.txt");
            GoPath = Path.Combine(_caseRoot, "go.txt");
            WitnessPath = Path.Combine(_caseRoot, "witness.txt");
            ResultPath = Path.Combine(_caseRoot, "result.txt");
            Directory.CreateDirectory(JournalPath);
        }

        public string Name { get; }
        public string JournalPath { get; }
        public string ReadyPath { get; }
        public string BarrierPath { get; }
        public string GoPath { get; }
        public string WitnessPath { get; }
        public string ResultPath { get; }
        public Process? Owner => _owner;
        public bool OwnerAlive => _owner is { HasExited: false };
        public Dictionary<string, string> Ready { get; private set; } = new();
        public int ReadyOwnerPid => int.Parse(Ready["owner_pid"]);
        public string OwnerStart => Ready["owner_start"];
        public string LiveOwnerStart => _owner is null ? "none" : StartIdentity(_owner.Id);
        public string JournalLockPath => Path.Combine(JournalPath, "ownership.lock");
        public string ControlFd => Ready["control_fd"];

        public void StartOwner(string mode, params string[] extra)
        {
            _owner = StartProcess(OwnerDll.Value, BuildArgs(mode, extra));
            D4Witness.Append(WitnessPath, Name, "SupervisorStartedOwner", "supervisor", _owner.Id.ToString());
        }

        public async Task WaitReady(CancellationToken cancellationToken)
        {
            await WaitForFile(ReadyPath, cancellationToken, TimeSpan.FromSeconds(60));
            Ready = await ReadStable(ReadyPath, new[] { "owner_pid", "journal", "child" }, cancellationToken);
        }

        public async Task WaitBarrier(string name, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(BarrierPath))
                {
                    var barrier = D4Result.Read(BarrierPath);
                    if (barrier.TryGetValue("name", out var actual) && actual == name)
                    {
                        D4Witness.Append(WitnessPath, Name, "SupervisorBarrierObserved", "supervisor",
                            Environment.ProcessId.ToString(), result: name);
                        return;
                    }
                }

                await Task.Delay(50, cancellationToken);
            }

            throw new TimeoutException($"barrier {name} not observed");
        }

        public async Task WaitExit(CancellationToken cancellationToken)
        {
            if (_owner is null) throw new InvalidOperationException("owner not started");
            await _owner.WaitForExitAsync(cancellationToken);
        }

        public void ReleaseGo() => File.WriteAllText(GoPath, "1");

        public async Task KillOwner(CancellationToken cancellationToken)
        {
            if (_owner is null) return;
            Assert.False(_owner.HasExited, "owner must be live at the kill point");
            var pid = _owner.Id;
            var start = StartIdentity(pid);
            D4Witness.Append(WitnessPath, Name, "SupervisorSigkill", "supervisor",
                pid.ToString(), result: start);
            _owner.Kill(entireProcessTree: false);
            await _owner.WaitForExitAsync(cancellationToken);
            Assert.True(_owner.HasExited);
            Assert.Equal(137, _owner.ExitCode);
            D4Witness.Append(WitnessPath, Name, "OwnerTerminated", "supervisor",
                pid.ToString(), result: $"exit={_owner.ExitCode}");
        }

        public (Process Process, string ResultPath) StartReplacementProcess(
            string mode, params string[] extra)
        {
            var result = Path.Combine(_caseRoot, $"replacement-{mode}-{Guid.NewGuid():N}.txt");
            var args = new List<string>
            {
                "--mode", mode, "--case", Name, "--result", result,
                "--witness", WitnessPath, "--barrier", BarrierPath, "--go", GoPath,
                "--journal", JournalPath,
            };
            if (Ready.TryGetValue("source", out var source))
            {
                args.Add("--source");
                args.Add(source);
            }
            args.AddRange(extra);
            return (StartProcess(OwnerDll.Value, args), result);
        }

        public async Task<(int ExitCode, string ResultPath)> StartReplacement(
            string mode, CancellationToken cancellationToken, params string[] extra)
        {
            var (process, result) = StartReplacementProcess(mode, extra);
            using (process)
            {
                await process.WaitForExitAsync(cancellationToken);
                D4Witness.Append(WitnessPath, Name, "RecoveryProcessExited", "supervisor",
                    process.Id.ToString(), result: process.ExitCode.ToString());
                return (process.ExitCode, result);
            }
        }

        public Dictionary<string, string> ReadResult() => ReadResult(ResultPath);

        public Dictionary<string, string> ReadResult(string path)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            while (!File.Exists(path))
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException($"result missing: {path}");
                Thread.Sleep(20);
            }

            return D4Result.Read(path);
        }

        public void AssertJournalFenceReleased()
        {
            var journal = new OwnershipJournal(new JournalFileStorage(), JournalPath);
            Assert.True(journal.TryAcquireWriter(out var writer, out var error), error);
            D4Witness.Append(WitnessPath, Name, "SupervisorFenceProbe", "supervisor",
                Environment.ProcessId.ToString(), baseline: writer!.BaselineSequence);
            writer.Dispose();
        }

        public void AssertCgroupRemoved()
        {
            if (!Ready.TryGetValue("child", out var child)) return;
            var parent = Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP");
            if (parent is null) return;
            var path = Path.Combine(parent, child);
            Wp05D4OwnerHost.CleanupCgroup(parent, child);
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (Directory.Exists(path) && DateTime.UtcNow < deadline)
                Thread.Sleep(20);
            Assert.False(Directory.Exists(path), $"test-owned cgroup remains: {path}");
        }

        public void Dispose()
        {
            try
            {
                if (_owner is { HasExited: false })
                {
                    _owner.Kill(entireProcessTree: false);
                    _owner.WaitForExit(5000);
                }
            }
            catch
            {
            }

            if (Ready.TryGetValue("child", out var child)
                && Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP") is { } parent)
                Wp05D4OwnerHost.CleanupCgroup(parent, child);
            if (_evidenceRoot.Length == 0)
            {
                try { Directory.Delete(_caseRoot, true); } catch { }
            }
        }

        private List<string> BuildArgs(string mode, string[] extra)
        {
            var args = new List<string>
            {
                "--mode", mode, "--case", Name, "--root", _caseRoot,
                "--result", ResultPath, "--witness", WitnessPath,
                "--barrier", BarrierPath, "--go", GoPath,
            };
            args.AddRange(extra);
            return args;
        }

        private static Process StartProcess(string dll, IReadOnlyList<string> args)
        {
            var info = new ProcessStartInfo("dotnet")
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(dll)!,
            };
            info.ArgumentList.Add("exec");
            info.ArgumentList.Add(dll);
            foreach (var arg in args) info.ArgumentList.Add(arg);
            info.Environment["GAGAMBA_REPOSITORY_ROOT"] = RepositoryRoot();
            if (Environment.GetEnvironmentVariable("GAGAMBA_DELEGATED_CGROUP") is { } cgroup)
                info.Environment["GAGAMBA_DELEGATED_CGROUP"] = cgroup;
            return Process.Start(info) ?? throw new InvalidOperationException("owner process did not start");
        }

        private static async Task WaitForFile(string path, CancellationToken cancellationToken, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            while (!File.Exists(path))
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException($"timed out waiting for {path}");
                await Task.Delay(50, cancellationToken);
            }
        }

        private static async Task<Dictionary<string, string>> ReadStable(
            string path, string[] required, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var values = D4Result.Read(path);
                    if (required.All(values.ContainsKey)) return values;
                }
                catch (IOException)
                {
                }

                await Task.Delay(20, cancellationToken);
            }

            throw new TimeoutException($"ready file incomplete: {path}");
        }

        private static string StartIdentity(int pid)
        {
            try
            {
                var stat = File.ReadAllText($"/proc/{pid}/stat");
                var close = stat.LastIndexOf(')');
                var fields = stat[(close + 2)..].Split(' ');
                return $"{pid}:{fields[19]}";
            }
            catch
            {
                return $"{pid}:unknown";
            }
        }
    }
}
