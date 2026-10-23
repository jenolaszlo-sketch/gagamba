using System.Buffers.Binary;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Execution;

public sealed class Wp05ReleaseContractProbes
{
    private sealed class MutableClock : ITransactionClock
    {
        public long NowNanoseconds { get; set; } = 1_000;
    }

    private sealed class RecordingTransport : IReleaseWriteChannel
    {
        internal RecordingTransport(ReleasePipeWriteResult result) => Result = result;

        internal ReleasePipeWriteResult Result { get; }
        internal int Attempts { get; private set; }
        internal byte[] LastFrame { get; private set; } = Array.Empty<byte>();

        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            LastFrame = frame.ToArray();
            return Result;
        }
    }

    private sealed class Harness : IDisposable
    {
        private bool _writerReleased;

        internal Harness(ReleasePipeWriteResult result = ReleasePipeWriteResult.Delivered)
        {
            var manifest = SampleManifest();
            var identity = new PreparationIdentity(
                RecoveryTestSupport.Execution,
                RecoveryTestSupport.Issuer,
                RecoveryTestSupport.Generation,
                RecoveryTestSupport.Preparation,
                ManifestCodec.ComputeDigestBytes(manifest));
            var store = new PreparedTransaction.PreparationStore(
                RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, Clock);
            Capability = store.Mint(manifest, identity, 1_000_000_000, TestClosure.Empty());
            Storage = new FakeJournalStorage();
            Journal = new OwnershipJournal(Storage, "/wp05-release-probe");
            Assert.True(Journal.TryAcquireWriter(out var writer, out var error), error);
            Writer = writer!;
            var preparation = new RecordedPreparation(
                identity.ExecutionId, identity.IssuerId, identity.GenerationId,
                identity.PreparationId, identity.ManifestDigest.ToArray());
            Assert.True(Journal.WriteNext(Writer,
                RecoveryTestSupport.Snapshot(stage: PreparationStage.Prepared, preparation: preparation)).Ok);
            Transport = new RecordingTransport(result);
            Arbiter = new ReleaseArbiter(Capability, Journal, Writer, Clock,
                () => Prerequisites, Transport);
        }

        internal MutableClock Clock { get; } = new();
        internal FakeJournalStorage Storage { get; }
        internal OwnershipJournal Journal { get; }
        internal JournalWriterLease Writer { get; private set; }
        internal RecordingTransport Transport { get; }
        internal ReleaseArbiter Arbiter { get; }
        internal PreparedTransaction Capability { get; }
        internal ReleasePrerequisites Prerequisites { get; set; } = new(true, true, true, true);
        internal long SetupDeadline { get; } = 10_000;
        internal long OverallDeadline { get; } = 30_000;
        internal ReleaseRequest Request => new(Capability.Identity, SetupDeadline, OverallDeadline);

        internal void ReleaseWriter()
        {
            if (_writerReleased) return;
            _writerReleased = true;
            Writer.Dispose();
        }

        public void Dispose()
        {
            Capability.Dispose();
            if (!_writerReleased)
            {
                _writerReleased = true;
                Writer.Dispose();
            }
        }
    }

    [Fact]
    public void StopWithUnavailableClockConsumesCapabilityC2()
    {
        using var harness = new Harness();
        harness.Clock.NowNanoseconds = 0;

        var stop = harness.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.False(stop.DurableStop);
        Assert.Equal(0, stop.StopAcceptedAtNanoseconds);
        Assert.Equal(0, stop.StopDeadlineNanoseconds);
        Assert.False(harness.Capability.IsLive);

        var release = harness.Arbiter.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(0, harness.Transport.Attempts);
        Assert.False(harness.Capability.IsLive);
        Assert.Null(harness.Journal.ReadLatest().Snapshot!.Stop);
    }

    [Fact]
    public void ExpiredCapabilityOverflowStopAcceptedC2()
    {
        using var harness = new Harness();
        harness.Clock.NowNanoseconds = long.MaxValue - 2_000_000_000L;

        var stop = harness.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.False(stop.DurableStop);
        Assert.Equal(long.MaxValue - 2_000_000_000L, stop.StopAcceptedAtNanoseconds);
        Assert.Equal(0, stop.StopDeadlineNanoseconds);
        Assert.False(harness.Capability.IsLive);

        var release = harness.Arbiter.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(0, harness.Transport.Attempts);
    }

    [Fact]
    public void AmbiguousSealFailureRetainsUncertaintyAndRequestsTailStop()
    {
        using var harness = new Harness();
        harness.Storage.FailAt("unlink", 2);

        var decision = harness.Arbiter.RequestRelease(harness.Request);
        Assert.Equal("AmbiguousPersistence", decision.Kind.ToString());
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, decision.Delivery);
        Assert.False(decision.DurableCommitted);
        Assert.True(decision.LiveExecutionExcluded);
        Assert.Contains("ambiguous journal persistence", decision.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, harness.Transport.Attempts);
        Assert.False(harness.Capability.IsLive);

        var latest = harness.Journal.ReadLatest();
        Assert.False(latest.Ok);
        Assert.Contains("seal", latest.Error);
        Assert.True(harness.Storage.Exists("/wp05-release-probe", OwnershipJournal.SealName));

        var duplicate = harness.Arbiter.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Duplicate, duplicate.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, duplicate.DurableRelease);
        Assert.Equal(0, harness.Transport.Attempts);

        harness.ReleaseWriter();
        var recovered = new OwnershipJournal(harness.Storage, "/wp05-release-probe");
        Assert.True(recovered.TryRecoverCommit(out var snapshot, out var error), error);
        Assert.NotNull(snapshot);
        Assert.Equal(ReleaseTag.Intent, snapshot!.Release!.Tag);
        var classification = ReleaseRecoveryClassifier.Classify(snapshot);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, classification.Release);
        Assert.Equal(ExecDisposition.NotAttempted, classification.Exec);
        Assert.Equal(RootResultKind.Unknown, classification.Root);
    }

    [Fact]
    public void DefiniteIntentFailureIsDistinctFromAmbiguousPersistence()
    {
        using var harness = new Harness();
        harness.Storage.FailAt("write", 1);

        var decision = harness.Arbiter.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, decision.DurableRelease);
        Assert.Equal(0, harness.Transport.Attempts);

        var latest = harness.Journal.ReadLatest();
        Assert.True(latest.Ok);
        Assert.Null(latest.Snapshot!.Release);
        Assert.True(harness.Capability.IsLive);
    }

    [Fact]
    public void StopFirstConsumesCapabilityC2()
    {
        using var harness = new Harness();
        var stop = harness.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.True(stop.DurableStop);
        Assert.False(harness.Capability.IsLive);

        var release = harness.Arbiter.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(0, harness.Transport.Attempts);
        Assert.False(harness.Capability.IsLive);
    }

    [Fact]
    public void DurableStopBlocksReleaseIntentFromSecondArbiter()
    {
        using var harness = new Harness();
        Assert.True(harness.Arbiter.RequestStop(StopCauseKind.CallerStop).DurableStop);

        var second = new ReleaseArbiter(
            harness.Capability, harness.Journal, harness.Writer, harness.Clock,
            () => harness.Prerequisites, harness.Transport);
        var release = second.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(0, harness.Transport.Attempts);
        Assert.Null(harness.Journal.ReadLatest().Snapshot!.Release);
        Assert.NotNull(harness.Journal.ReadLatest().Snapshot!.Stop);
    }

    [Fact]
    public void StopPersistenceFailureConsumesCapabilityAndBlocksSecondArbiterC2()
    {
        using var harness = new Harness();
        harness.Storage.FailAt("write", 1);
        var baseline = harness.Journal.ReadLatest().Snapshot!.Sequence;

        var stop = harness.Arbiter.RequestStop(StopCauseKind.CallerStop);
        Assert.True(stop.Accepted);
        Assert.False(stop.DurableStop);
        Assert.False(harness.Capability.IsLive);
        Assert.Null(harness.Journal.ReadLatest().Snapshot!.Stop);

        var second = new ReleaseArbiter(
            harness.Capability, harness.Journal, harness.Writer, harness.Clock,
            () => harness.Prerequisites, harness.Transport);
        var release = second.RequestRelease(harness.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, release.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, release.DurableRelease);
        Assert.Equal(0, harness.Transport.Attempts);
        Assert.Null(harness.Journal.ReadLatest().Snapshot!.Release);
        Assert.Equal(baseline, harness.Journal.ReadLatest().Snapshot!.Sequence);
    }

    [Fact]
    public async Task ConcurrentDuplicateReleaseProducesOnePhysicalWrite()
    {
        using var harness = new Harness();
        using var atIntent = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        harness.Arbiter.TestBarrier = name =>
        {
            if (name == "after-intent")
            {
                atIntent.Set();
                if (!resume.Wait(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("after-intent probe barrier timed out");
            }
        };

        var winnerTask = Task.Run(() => harness.Arbiter.RequestRelease(harness.Request));
        var cancellation = TestContext.Current.CancellationToken;
        Assert.True(atIntent.Wait(TimeSpan.FromSeconds(5), cancellation), "winner did not reach durable intent");
        var concurrent = harness.Arbiter.RequestRelease(harness.Request);
        resume.Set();
        var winner = await winnerTask.WaitAsync(TimeSpan.FromSeconds(5), cancellation);
        var later = harness.Arbiter.RequestRelease(harness.Request);

        Assert.Equal(ReleaseDecisionKind.Released, winner.Kind);
        Assert.Equal(ReleaseDecisionKind.Refused, concurrent.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, concurrent.DurableRelease);
        Assert.Equal(ReleaseDecisionKind.Duplicate, later.Kind);
        Assert.Equal(1, harness.Transport.Attempts);
        Assert.Equal(ReleaseTag.Committed, harness.Journal.ReadLatest().Snapshot!.Release!.Tag);
    }

    [Fact]
    public void ReconcilerUsesCallerAllowanceAfterPersistedStopDeadlineExpired()
    {
        var acceptedAt = 1_000L;
        var start = acceptedAt + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds + 1;
        var clock = new FakeDomainClock { NowNanoseconds = start };
        var stop = RecoveryTestSupport.Stop(acceptedAt: acceptedAt);
        var record = RecoveryTestSupport.V3Snapshot(
            stage: PreparationStage.AllocationRegistered, stop: stop);
        var backend = new FakeDomainBackend { Populated = true, KillAccepted = false };
        var lease = new DomainLease(backend, RecoveryTestSupport.Identity());
        var reconciler = new Reconciler(clock, TimeSpan.FromSeconds(5));

        var result = reconciler.Reconcile(record, "boot-1", lease, null, null, null);

        Assert.True(clock.NowNanoseconds - start >= 5_000_000_000L);
        Assert.True(backend.KillWrites > 0);
        Assert.NotEqual(ReconcileOutcome.CleanupConfirmed, result.Outcome);
    }

    [Fact]
    public void StatusBufferDoesNotPersistBeforeDurableCommitted()
    {
        var (journal, writer) = SeedCommittedJournal();
        using (writer)
        {
            var buffer = new ReleaseObservationBuffer();
            Assert.True(buffer.OfferExec(RecoveryTestSupport.ExecConfirmed()));
            Assert.True(buffer.OfferRoot(RecoveryTestSupport.RootExited()));
            Assert.True(buffer.TryFlush(journal, writer, out var error), error);
            Assert.Null(journal.ReadLatest().Snapshot!.Exec);

            buffer.MarkDurableCommitted();
            Assert.True(buffer.TryFlush(journal, writer, out error), error);
            var snapshot = journal.ReadLatest().Snapshot!;
            Assert.NotNull(snapshot.Exec);
            Assert.NotNull(snapshot.Root);
        }
    }

    [Fact]
    public void StatusBeforeCommittedPersistenceFailureDoesNotCreateObservation()
    {
        var (journal, writer, storage) = SeedCommittedJournalWithStorage();
        using (writer)
        {
            var buffer = new ReleaseObservationBuffer();
            Assert.True(buffer.OfferExec(RecoveryTestSupport.ExecConfirmed()));
            buffer.MarkDurableCommitted();
            storage.FailAt("write", 1);
            Assert.False(buffer.TryFlush(journal, writer, out var error));
            Assert.Contains("exec", error);
            Assert.Null(journal.ReadLatest().Snapshot!.Exec);
        }
    }

    [Fact]
    public void StatusReaderRejectsWrongIdentityAndSequence()
    {
        var source = new QueueStatusSource();
        var frame = StatusFrame(CoordinatorStatusKind.ExecConfirmed, 2);
        frame[24] ^= 1;
        source.Add(frame);
        var reader = new CoordinatorStatusReader(
            source, Execution, Preparation, Digest);
        Assert.False(reader.TryReadNext(1_000_000, out _, out _));

        var sequenceSource = new QueueStatusSource();
        sequenceSource.Add(StatusFrame(CoordinatorStatusKind.ExecConfirmed, 3));
        var sequenceReader = new CoordinatorStatusReader(
            sequenceSource, Execution, Preparation, Digest);
        Assert.False(sequenceReader.TryReadNext(1_000_000, out _, out _));
    }

    [Fact]
    public void StatusReaderRejectsEofAndMalformedFrames()
    {
        var eof = new CoordinatorStatusReader(
            new QueueStatusSource(), Execution, Preparation, Digest);
        Assert.False(eof.TryReadNext(1_000_000, out _, out _));

        var source = new QueueStatusSource();
        var malformed = StatusFrame(CoordinatorStatusKind.ExecConfirmed, 2);
        malformed[10] ^= 1;
        source.Add(malformed);
        var reader = new CoordinatorStatusReader(
            source, Execution, Preparation, Digest);
        Assert.False(reader.TryReadNext(1_000_000, out _, out _));
    }

    private static (OwnershipJournal Journal, JournalWriterLease Writer) SeedCommittedJournal()
    {
        var (journal, writer, _) = SeedCommittedJournalWithStorage();
        return (journal, writer);
    }

    private static (OwnershipJournal Journal, JournalWriterLease Writer, FakeJournalStorage Storage)
        SeedCommittedJournalWithStorage()
    {
        var storage = new FakeJournalStorage();
        var journal = new OwnershipJournal(storage, "/wp05-status-probe");
        Assert.True(journal.TryAcquireWriter(out var writer, out var error), error);
        Assert.True(journal.WriteNext(writer!,
            RecoveryTestSupport.V3Snapshot(release: RecoveryTestSupport.ReleaseCommitted())).Ok);
        return (journal, writer!, storage);
    }

    private static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-0000000000a1");
    private static readonly Guid Preparation = new("dddddddd-0000-0000-0000-0000000000a2");
    private static readonly byte[] Digest = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();

    private sealed class QueueStatusSource : IStatusFrameSource
    {
        private readonly Queue<byte[]> _frames = new();

        internal void Add(byte[] frame) => _frames.Enqueue(frame);

        public bool TryReadFrame(long deadlineNanoseconds, out byte[]? frame, out string error)
        {
            if (_frames.Count == 0)
            {
                frame = null;
                error = "queue empty";
                return false;
            }

            frame = _frames.Dequeue();
            error = string.Empty;
            return true;
        }
    }

    private static byte[] StatusFrame(CoordinatorStatusKind kind, ulong sequence, uint value = 0)
    {
        var payload = kind == CoordinatorStatusKind.ExecConfirmed ? 0 : 4;
        var frame = new byte[CoordinatorProtocol.HeaderBytes + payload];
        System.Text.Encoding.ASCII.GetBytes("GGM1CTL\0").CopyTo(frame, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(10), (ushort)kind);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(12), (uint)frame.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(16), sequence);
        CoordinatorProtocol.GuidToRfcBytes(Execution).CopyTo(frame, 24);
        CoordinatorProtocol.GuidToRfcBytes(Preparation).CopyTo(frame, 40);
        Digest.CopyTo(frame, 56);
        if (payload != 0)
            BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(96), value);
        return frame;
    }
}
