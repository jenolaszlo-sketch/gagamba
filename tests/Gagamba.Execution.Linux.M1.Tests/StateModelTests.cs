// WP01 state-model tests: legal transitions, refusals without mutation,
// single-write release arbitration, stop stability and terminal truth.
// Portable pure logic with manually advanced fake dependencies.
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Restricted;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests;

public sealed class StateModelTests
{
    private sealed class FakeClock : ITransactionClock
    {
        public long NowNanoseconds { get; set; }
    }

    private sealed class FakeJournal : ITransactionJournal
    {
        private readonly List<JournalEntry> _entries = new();

        public Action<string>? OnAppend { get; set; }

        public IReadOnlyList<JournalEntry> Entries => _entries;

        public void Append(string kind, string detail, long atNs)
        {
            OnAppend?.Invoke(kind);
            _entries.Add(new JournalEntry(kind, detail, atNs));
        }
    }

    private sealed class FakeTransport : IReleaseTransport
    {
        private readonly ReleaseSendResult _result;

        public FakeTransport(ReleaseSendResult result = ReleaseSendResult.Delivered)
        {
            _result = result;
        }

        public int Attempts { get; private set; }

        public byte[] LastFrame { get; private set; } = Array.Empty<byte>();

        public ReleaseSendResult TrySend(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            LastFrame = frame.ToArray();
            return _result;
        }
    }

    private static readonly Guid Issuer = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Generation = new("22222222-2222-2222-2222-222222222222");

    private sealed class Harness
    {
        public Harness(ReleaseSendResult transportResult = ReleaseSendResult.Delivered)
        {
            Clock = new FakeClock();
            Store = new PreparedTransaction.PreparationStore(Issuer, Generation, Clock);
            Journal = new FakeJournal();
            Transport = new FakeTransport(transportResult);
            Model = new TransactionStateModel(Store, Clock, Journal, Transport);
            var manifest = SampleManifest();
            var identity = new PreparationIdentity(
                new Guid("33333333-3333-3333-3333-333333333333"),
                Issuer,
                Generation,
                new Guid("44444444-4444-4444-4444-444444444444"),
                ManifestCodec.ComputeDigestBytes(manifest));
            Capability = Store.Mint(manifest, identity, 1_000_000_000);
        }

        public FakeClock Clock { get; }

        public PreparedTransaction.PreparationStore Store { get; }

        public FakeJournal Journal { get; }

        public FakeTransport Transport { get; }

        public TransactionStateModel Model { get; }

        public PreparedTransaction Capability { get; }

        public void DriveToOwned()
        {
            var published = Model.PublishPrepared(Capability);
            Assert.True(published.Applied);
            var owned = Model.MarkOwned(OwnedPrerequisites.Satisfied());
            Assert.True(owned.Applied);
        }

        public void DriveToCommitted()
        {
            DriveToOwned();
            var committed = Model.TryCommitRelease(Capability.Identity, Capability.Identity.ManifestDigest.ToArray());
            Assert.True(committed.Applied);
            Assert.Equal(TransactionState.ReleaseCommitted, Model.State);
        }
    }

    private static RootResult Exited(int code) =>
        new(RootResultKind.Exited, code, 0, false, code << 8);

    private static CaptureResult CompleteCapture() =>
        new(100, 100, 50, 50, 150, 150, false, false, false, false, true, true, true, FinalDrainDisposition.Complete);

    private static CaptureResult IncompleteCapture() =>
        new(100, 100, 50, 50, 150, 150, false, false, false, false, true, true, false, FinalDrainDisposition.Incomplete);

    [Fact]
    public void CompleteTransitionTable()
    {
        var h = new Harness();
        Assert.Equal(TransactionState.Preparing, h.Model.State);

        // Illegal before publication: every gated call refuses without mutation.
        Assert.False(h.Model.MarkOwned(OwnedPrerequisites.Satisfied()).Applied);
        Assert.False(h.Model.TryCommitRelease(h.Capability.Identity, new byte[32]).Applied);
        Assert.False(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.False(h.Model.ObserveRoot(Exited(0)).Applied);
        Assert.False(h.Model.ObserveDomainEmpty(true).Applied);
        Assert.False(h.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Preparing, h.Model.State);
        Assert.Null(h.Model.TerminalObservation);

        // Legal prefix.
        Assert.True(h.Model.PublishPrepared(h.Capability).Applied);
        Assert.Equal(TransactionState.Prepared, h.Model.State);
        Assert.False(h.Model.PublishPrepared(h.Capability).Applied);
        Assert.False(h.Model.TryCommitRelease(h.Capability.Identity, new byte[32]).Applied);
        Assert.False(h.Model.ObserveRoot(Exited(0)).Applied);
        Assert.Equal(TransactionState.Prepared, h.Model.State);

        Assert.True(h.Model.MarkOwned(OwnedPrerequisites.Satisfied()).Applied);
        Assert.Equal(TransactionState.Owned, h.Model.State);
        Assert.True(h.Model.ObserveDomainEmpty(true).Applied);
        Assert.False(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.Equal(TransactionState.Owned, h.Model.State);

        // Terminal immutability: publish, then every mutation refuses.
        // The harness is already Owned here, so commit directly instead of
        // re-driving (helpers assume a fresh harness).
        var committed = h.Model.TryCommitRelease(
            h.Capability.Identity, h.Capability.Identity.ManifestDigest.ToArray());
        Assert.True(committed.Applied);
        Assert.Equal(TransactionState.ReleaseCommitted, h.Model.State);
        Assert.True(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.Equal(TransactionState.Running, h.Model.State);
        Assert.True(h.Model.ObserveRoot(Exited(0)).Applied);
        Assert.True(h.Model.ObserveDomainEmpty(true).Applied);
        Assert.True(h.Model.ObserveCaptureTerminal(CompleteCapture()).Applied);
        Assert.True(h.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Completed, h.Model.State);
        Assert.False(h.Model.PublishTerminal().Applied);
        Assert.False(h.Model.AcceptStop(StopCauseKind.CallerStop).Applied);
        Assert.False(h.Model.ObserveRoot(Exited(1)).Applied);
        Assert.Equal(TransactionState.Completed, h.Model.State);
        Assert.Equal(0, h.Model.TerminalObservation!.Root.ExitCode);
    }

    [Fact]
    public void CommitSuccessWritesIntentBeforeSend()
    {
        var h = new Harness();
        h.DriveToOwned();
        var digest = h.Capability.Identity.ManifestDigest.ToArray();
        var attempt = h.Model.TryCommitRelease(h.Capability.Identity, (byte[])digest.Clone());
        Assert.True(attempt.Applied);
        Assert.Equal(TransactionState.ReleaseCommitted, h.Model.State);
        Assert.Equal(ReleaseDisposition.Committed, h.Model.Release);
        Assert.Equal(1, h.Transport.Attempts);
        Assert.Equal(digest, h.Transport.LastFrame);
        Assert.Equal(32, h.Transport.LastFrame.Length);
        var kinds = h.Journal.Entries.Select(e => e.Kind).ToArray();
        Assert.True(Array.IndexOf(kinds, "release-intent") < Array.IndexOf(kinds, "release-delivered"));
        Assert.False(h.Model.TryCommitRelease(h.Capability.Identity, digest).Applied);
        Assert.Equal(1, h.Transport.Attempts);
    }

    [Fact]
    public void CommitExpiredRefused()
    {
        var h = new Harness();
        h.DriveToOwned();
        h.Clock.NowNanoseconds = 1_000_000_000;
        var attempt = h.Model.TryCommitRelease(
            h.Capability.Identity, h.Capability.Identity.ManifestDigest.ToArray());
        Assert.False(attempt.Applied);
        Assert.Equal(InvariantViolation.Expired, attempt.Violation);
        Assert.Equal(TransactionState.Owned, h.Model.State);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.DoesNotContain(h.Journal.Entries, e => e.Kind == "release-intent");
    }

    [Fact]
    public void CommitForeignIdentityRefused()
    {
        var h = new Harness();
        h.DriveToOwned();
        var foreign = new PreparationIdentity(
            Guid.NewGuid(), Issuer, Generation, Guid.NewGuid(),
            h.Capability.Identity.ManifestDigest.ToArray());
        var attempt = h.Model.TryCommitRelease(foreign, h.Capability.Identity.ManifestDigest.ToArray());
        Assert.False(attempt.Applied);
        Assert.Equal(InvariantViolation.Foreign, attempt.Violation);
        Assert.Equal(TransactionState.Owned, h.Model.State);
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public void CommitForeignDigestRefused()
    {
        var h = new Harness();
        h.DriveToOwned();
        var attempt = h.Model.TryCommitRelease(h.Capability.Identity, new byte[32]);
        Assert.False(attempt.Applied);
        Assert.Equal(InvariantViolation.Foreign, attempt.Violation);
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public void CommitConsumedAndAbortedRefused()
    {
        var h = new Harness();
        h.DriveToOwned();
        Assert.True(h.Store.TryConsume(h.Capability, out _));
        var attempt = h.Model.TryCommitRelease(
            h.Capability.Identity, h.Capability.Identity.ManifestDigest.ToArray());
        Assert.False(attempt.Applied);
        Assert.Equal(InvariantViolation.Consumed, attempt.Violation);

        var h2 = new Harness();
        h2.DriveToOwned();
        h2.Capability.Dispose();
        var aborted = h2.Model.TryCommitRelease(
            h2.Capability.Identity, h2.Capability.Identity.ManifestDigest.ToArray());
        Assert.False(aborted.Applied);
        Assert.Equal(InvariantViolation.Aborted, aborted.Violation);
    }

    [Fact]
    public void StopWinsBeforeRelease()
    {
        var h = new Harness();
        h.DriveToOwned();
        Assert.True(h.Model.AcceptStop(StopCauseKind.CallerStop).Applied);
        Assert.Equal(TransactionState.Stopping, h.Model.State);
        var attempt = h.Model.TryCommitRelease(
            h.Capability.Identity, h.Capability.Identity.ManifestDigest.ToArray());
        Assert.False(attempt.Applied);
        Assert.Equal(InvariantViolation.StoppedFirst, attempt.Violation);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.True(h.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Cancelled, h.Model.State);
        Assert.Equal(ExecutionOutcome.CancelledBeforeRelease, h.Model.TerminalOutcome);
        Assert.Equal(RootResultKind.NotStarted, h.Model.TerminalObservation!.Root.Kind);
    }

    [Fact]
    public void TransportFailuresStopWithMayHaveExecuted()
    {
        foreach (var fault in new[] { ReleaseSendResult.WouldBlock, ReleaseSendResult.BrokenPipe, ReleaseSendResult.Partial })
        {
            var h = new Harness(fault);
            h.DriveToOwned();
            var attempt = h.Model.TryCommitRelease(
                h.Capability.Identity, h.Capability.Identity.ManifestDigest.ToArray());
            Assert.True(attempt.Applied);
            Assert.Equal(TransactionState.Stopping, h.Model.State);
            Assert.Equal(ReleaseDisposition.MayHaveExecuted, h.Model.Release);
            Assert.Equal(StopCauseKind.TransportFailure, h.Model.FirstStopCause);
            Assert.Equal(1, h.Transport.Attempts);
            Assert.True(h.Model.PublishTerminal().Applied);
            Assert.Equal(TransactionState.Unknown, h.Model.State);
            Assert.Equal(ExecutionOutcome.Unknown, h.Model.TerminalOutcome);
            Assert.Equal(RootResultKind.NotStarted, h.Model.TerminalObservation!.Root.Kind);
            Assert.Equal(CleanupState.Unknown, h.Model.TerminalCleanup!.State);
        }
    }

    [Fact]
    public void ReentrantStopDuringIntentWriteWins()
    {
        var h = new Harness();
        h.DriveToOwned();
        h.Journal.OnAppend = kind =>
        {
            if (kind == "release-intent")
            {
                Assert.True(h.Model.AcceptStop(StopCauseKind.SetupFault).Applied);
            }
        };
        var attempt = h.Model.TryCommitRelease(
            h.Capability.Identity, h.Capability.Identity.ManifestDigest.ToArray());
        Assert.False(attempt.Applied);
        Assert.Equal(InvariantViolation.StoppedFirst, attempt.Violation);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, h.Model.Release);
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public void StopCauseIsStableAndLaterCausesBounded()
    {
        var h = new Harness();
        h.DriveToOwned();
        Assert.True(h.Model.AcceptStop(StopCauseKind.CallerStop).Applied);
        for (var i = 0; i < 10; i++)
        {
            Assert.True(h.Model.AcceptStop(StopCauseKind.RecoveryTakeover).Applied);
        }

        Assert.Equal(StopCauseKind.CallerStop, h.Model.FirstStopCause);
        Assert.Equal(8, h.Model.LaterStopCauses.Count);
        Assert.All(h.Model.LaterStopCauses, c => Assert.Equal(StopCauseKind.RecoveryTakeover, c));
    }

    [Fact]
    public void KnownRootUnknownCleanupCombinations()
    {
        var h = new Harness();
        h.DriveToCommitted();
        Assert.True(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.True(h.Model.ObserveRoot(Exited(0)).Applied);
        Assert.True(h.Model.ObserveDomainEmpty(true).Applied);
        Assert.True(h.Model.ObserveCaptureTerminal(CompleteCapture()).Applied);
        Assert.True(h.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Completed, h.Model.State);
        Assert.Equal(ExecutionOutcome.Succeeded, h.Model.TerminalOutcome);
        Assert.Equal(CleanupState.Unknown, h.Model.TerminalCleanup!.State);
        Assert.True(h.Model.TerminalCleanup.DomainEmpty);
        Assert.Null(h.Model.TerminalCleanup.Reaped);

        var h2 = new Harness();
        h2.DriveToCommitted();
        Assert.True(h2.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.True(h2.Model.ObserveRoot(Exited(17)).Applied);
        Assert.True(h2.Model.ObserveDomainEmpty(true).Applied);
        Assert.True(h2.Model.ObserveCaptureTerminal(CompleteCapture()).Applied);
        Assert.True(h2.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Completed, h2.Model.State);
        Assert.Equal(ExecutionOutcome.TargetFailed, h2.Model.TerminalOutcome);
        Assert.Equal(CleanupState.Unknown, h2.Model.TerminalCleanup!.State);

        var h3 = new Harness();
        h3.DriveToCommitted();
        Assert.True(h3.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.True(h3.Model.ObserveDomainEmpty(true).Applied);
        Assert.True(h3.Model.ObserveCaptureTerminal(CompleteCapture()).Applied);
        Assert.True(h3.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Unknown, h3.Model.State);
        Assert.Equal(ExecutionOutcome.Unknown, h3.Model.TerminalOutcome);
        Assert.Equal(RootResultKind.Unknown, h3.Model.TerminalObservation!.Root.Kind);
    }

    [Fact]
    public void SignalledStoppedPreservesRoot()
    {
        var h = new Harness();
        h.DriveToCommitted();
        Assert.True(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.True(h.Model.ObserveRoot(new RootResult(RootResultKind.Signalled, 0, 9, false, 9)).Applied);
        Assert.True(h.Model.AcceptStop(StopCauseKind.CallerStop).Applied);
        Assert.True(h.Model.ObserveDomainEmpty(true).Applied);
        Assert.True(h.Model.ObserveCaptureTerminal(CompleteCapture()).Applied);
        Assert.True(h.Model.PublishTerminal().Applied);
        Assert.Equal(ExecutionOutcome.Stopped, h.Model.TerminalOutcome);
        Assert.Equal(RootResultKind.Signalled, h.Model.TerminalObservation!.Root.Kind);
        Assert.Equal(9, h.Model.TerminalObservation.Root.Signal);
    }

    [Fact]
    public void IncompleteCaptureBlocksCompletion()
    {
        var h = new Harness();
        h.DriveToCommitted();
        Assert.True(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.True(h.Model.ObserveRoot(Exited(0)).Applied);
        Assert.True(h.Model.ObserveDomainEmpty(true).Applied);
        var partial = IncompleteCapture();
        Assert.True(h.Model.ObserveCaptureTerminal(partial).Applied);
        Assert.True(h.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Unknown, h.Model.State);
        Assert.Equal(ExecutionOutcome.Unknown, h.Model.TerminalOutcome);
        Assert.Equal(RootResultKind.Exited, h.Model.TerminalObservation!.Root.Kind);
    }

    [Fact]
    public void FailedBeforeExecTerminal()
    {
        var h = new Harness();
        h.DriveToCommitted();
        Assert.True(h.Model.ObserveExec(ExecDisposition.FailedBeforeExec, 2).Applied);
        Assert.Equal(2, h.Model.ExecErrno);
        Assert.True(h.Model.AcceptStop(StopCauseKind.SetupFault).Applied);
        Assert.True(h.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Failed, h.Model.State);
        Assert.Equal(ExecutionOutcome.FailedToEstablish, h.Model.TerminalOutcome);
        Assert.Equal(RootResultKind.NotStarted, h.Model.TerminalObservation!.Root.Kind);
    }

    [Fact]
    public void SetupFaultTerminal()
    {
        var h = new Harness();
        h.DriveToOwned();
        Assert.True(h.Model.AcceptStop(StopCauseKind.SetupExpired).Applied);
        Assert.True(h.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Failed, h.Model.State);
        Assert.Equal(ExecutionOutcome.FailedToEstablish, h.Model.TerminalOutcome);
        Assert.Equal(RootResultKind.NotStarted, h.Model.TerminalObservation!.Root.Kind);
    }

    [Fact]
    public void ExecConfirmedMovesToRunning()
    {
        var h = new Harness();
        h.DriveToCommitted();
        Assert.Equal(TransactionState.ReleaseCommitted, h.Model.State);
        Assert.True(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.Equal(TransactionState.Running, h.Model.State);
    }

    [Fact]
    public void TerminalObservationShape()
    {
        var h = new Harness();
        h.Clock.NowNanoseconds = 100;
        h.DriveToCommitted();
        h.Clock.NowNanoseconds = 200;
        Assert.True(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.True(h.Model.ObserveRoot(Exited(0)).Applied);
        Assert.True(h.Model.ObserveDomainEmpty(true).Applied);
        Assert.True(h.Model.ObserveCaptureTerminal(CompleteCapture()).Applied);
        Assert.True(h.Model.PublishTerminal().Applied);
        var terminal = h.Model.TerminalObservation!;
        Assert.Equal(1, terminal.EvidenceVersion);
        Assert.Equal(h.Capability.Identity, terminal.Identity);
        Assert.Same(h.Capability.Manifest, terminal.RequestedManifest);
        Assert.Equal(
            new[] { "PublishPrepared", "MarkOwned", "ReleaseCommitted", "ReleaseDelivered", "ObserveExec", "ObserveRoot", "ObserveDomainEmpty", "ObserveCaptureTerminal", "PublishTerminal" },
            terminal.TransitionNames.ToArray());
        Assert.Equal(terminal.TransitionNames.Count, terminal.TransitionAtNs.Count);
        for (var i = 1; i < terminal.TransitionAtNs.Count; i++)
        {
            Assert.True(terminal.TransitionAtNs[i] >= terminal.TransitionAtNs[i - 1]);
        }
    }

    [Fact]
    public async Task ObserverCancellationNeverStopsFakeExecution()
    {
        var h = new Harness();
        h.DriveToOwned();
        using var cancelled = new CancellationTokenSource();
        var waiter = Task.Run(() =>
        {
            while (!h.Model.TerminalPublished && !cancelled.Token.IsCancellationRequested)
            {
                Thread.Sleep(1);
            }

            return cancelled.Token.IsCancellationRequested;
        });
        cancelled.Cancel();
        Assert.True(await waiter);
        Assert.True(h.Model.TryCommitRelease(
            h.Capability.Identity, h.Capability.Identity.ManifestDigest.ToArray()).Applied);
        Assert.True(h.Model.ObserveExec(ExecDisposition.Confirmed).Applied);
        Assert.True(h.Model.ObserveRoot(Exited(0)).Applied);
        Assert.True(h.Model.ObserveDomainEmpty(true).Applied);
        Assert.True(h.Model.ObserveCaptureTerminal(CompleteCapture()).Applied);
        Assert.True(h.Model.PublishTerminal().Applied);
        Assert.Equal(TransactionState.Completed, h.Model.State);
        Assert.Equal(ExecutionOutcome.Succeeded, h.Model.TerminalOutcome);
    }
}
