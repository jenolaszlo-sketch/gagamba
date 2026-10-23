using System.Collections.Concurrent;
using System.Reflection;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests;

namespace Gagamba.Execution.Linux.M1.Tests.Execution;

public sealed class Wp05D69C1AmbiguousIntentTests
{
    private const string Directory = "/wp05-d6-9-c1";
    private static readonly TimeSpan Watchdog = TimeSpan.FromSeconds(5);

    private sealed class Clock : ITransactionClock { public long NowNanoseconds { get; set; } = 1_000; }
    private sealed class Transport : IReleaseWriteChannel
    {
        public int Attempts { get; private set; }
        public ReleasePipeWriteResult TryWriteOnce(ReadOnlyMemory<byte> frame)
        {
            Attempts++;
            return ReleasePipeWriteResult.Delivered;
        }
    }

    private sealed class Harness : IDisposable
    {
        private bool _writerReleased;
        internal Harness()
        {
            Clock = new Clock();
            var manifest = SampleManifest();
            var identity = new PreparationIdentity(RecoveryTestSupport.Execution,
                RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation,
                RecoveryTestSupport.Preparation, ManifestCodec.ComputeDigestBytes(manifest));
            var store = new PreparedTransaction.PreparationStore(
                RecoveryTestSupport.Issuer, RecoveryTestSupport.Generation, Clock);
            Capability = store.Mint(manifest, identity, 1_000_000_000, TestClosure.Empty());
            Storage = new FakeJournalStorage();
            Journal = new OwnershipJournal(Storage, Directory);
            Assert.True(Journal.TryAcquireWriter(out var writer, out var error), error);
            Writer = writer!;
            var preparation = new RecordedPreparation(identity.ExecutionId, identity.IssuerId,
                identity.GenerationId, identity.PreparationId, identity.ManifestDigest.ToArray());
            Assert.True(Journal.WriteNext(Writer,
                RecoveryTestSupport.V3Snapshot(preparation: preparation)).Ok);
            Transport = new Transport();
            Arbiter = NewArbiter();
        }

        internal Clock Clock { get; }
        internal FakeJournalStorage Storage { get; }
        internal OwnershipJournal Journal { get; }
        internal JournalWriterLease Writer { get; }
        internal PreparedTransaction Capability { get; }
        internal Transport Transport { get; }
        internal ReleaseArbiter Arbiter { get; }
        internal ReleasePrerequisites Prerequisites { get; } = new(true, true, true, true);
        internal long SetupDeadline { get; } = 10_000;
        internal long OverallDeadline { get; } = 30_000;
        internal ReleaseRequest Request => new(Capability.Identity, SetupDeadline, OverallDeadline);
        internal ReleaseArbiter NewArbiter() => new(Capability, Journal, Writer, Clock, () => Prerequisites, Transport);

        internal JournalIoResult WriteAmbiguousIntent()
        {
            var latest = Journal.ReadLatest();
            Assert.True(latest.Ok, latest.Error);
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(latest.Snapshot!, Clock.NowNanoseconds,
                SetupDeadline, OverallDeadline, out var intent, out var planError), planError);
            Storage.FailAt("unlink", 2);
            return Journal.WriteTransition(Writer, latest.Snapshot!, intent!);
        }

        internal ReleaseDecision RequestAmbiguousRelease()
        {
            Storage.FailAt("unlink", 2);
            return Arbiter.RequestRelease(Request);
        }

        internal OwnershipSnapshot DecodeSlot(string slot)
        {
            var bytes = Storage.ReadAll(Directory, slot);
            Assert.NotNull(bytes);
            Assert.True(OwnershipSnapshotCodec.TryDecode(bytes!, out var snapshot, out var error), error);
            return snapshot!;
        }

        internal void ReleaseWriter()
        {
            if (_writerReleased) return;
            _writerReleased = true;
            Writer.Dispose();
        }

        public void Dispose() { Capability.Dispose(); ReleaseWriter(); }
    }

    [Fact]
    public void C1T01_DefinitePreIntentWriteFailureRemainsNotCommitted()
    {
        using var h = new Harness();
        h.Storage.FailAt("write", 1);
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, decision.DurableRelease);
        Assert.True(h.Capability.IsLive);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.Null(h.Journal.ReadLatest().Snapshot!.Release);
        Assert.False(h.Storage.Exists(Directory, OwnershipJournal.SealName));
    }

    [Fact]
    public void C1T02_FinalSealUnlinkFailureIsTypedAndDoesNotAcknowledge()
    {
        using var h = new Harness();
        var baseline = h.Writer.BaselineSequence;
        var result = h.WriteAmbiguousIntent();
        Assert.False(result.Ok);
        Assert.Equal("AmbiguousAfterContent", JournalFailureKindName(result));
        Assert.Equal(baseline, h.Writer.BaselineSequence);
        Assert.Equal(baseline, h.Journal.LastSequence);
        Assert.True(h.Storage.Exists(Directory, OwnershipJournal.SealName));
    }

    [Fact]
    public void C1T03_BothSlotsContainIntentWhileSealKeepsReadUncertain()
    {
        using var h = new Harness();
        var result = h.WriteAmbiguousIntent();
        Assert.False(result.Ok);
        var slotA = h.DecodeSlot(OwnershipJournal.SlotA);
        var slotB = h.DecodeSlot(OwnershipJournal.SlotB);
        Assert.Equal(slotA.Sequence, slotB.Sequence);
        Assert.Equal(ReleaseTag.Intent, slotA.Release!.Tag);
        Assert.Equal(ReleaseTag.Intent, slotB.Release!.Tag);
        Assert.True(h.Storage.Exists(Directory, OwnershipJournal.SealName));
        Assert.False(h.Journal.ReadLatest().Ok);
    }

    [Fact]
    public async Task C1T04_AmbiguityRetainsBeforeConcurrentStopAndReleasesGate()
    {
        using var h = new Harness();
        using var retained = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        h.Storage.FailAt("unlink", 2);
        h.Arbiter.TestBarrier = name =>
        {
            if (name != "after-ambiguous-retention") return;
            retained.Set();
            if (!resume.Wait(Watchdog, TestContext.Current.CancellationToken))
                throw new TimeoutException("retention barrier watchdog expired");
        };
        var releaseTask = Task.Run(() => h.Arbiter.RequestRelease(h.Request));
        try
        {
            Assert.True(retained.Wait(Watchdog, TestContext.Current.CancellationToken), "ambiguity was not retained before tail stop");
            var stopTask = Task.Run(() => h.Arbiter.RequestStop(StopCauseKind.CallerStop));
            var stop = await stopTask.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            Assert.True(stop.Accepted);
            resume.Set();
            var decision = await releaseTask.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            AssertAmbiguousDecision(decision);
            Assert.False(h.Capability.IsLive);
            Assert.Equal(0, h.Transport.Attempts);
        }
        finally { resume.Set(); }
    }

    [Fact]
    public void C1T05_AmbiguityTailStopConsumesCapability()
    {
        using var h = new Harness();
        var decision = h.RequestAmbiguousRelease();
        AssertAmbiguousDecision(decision);
        Assert.False(h.Capability.IsLive);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.True(h.Storage.Exists(Directory, OwnershipJournal.SealName));
    }

    [Fact]
    public void C1T06_RepeatedReleaseDuplicatesRetainedAmbiguityWithoutRetry()
    {
        using var h = new Harness();
        var first = h.RequestAmbiguousRelease();
        var sequenceBefore = h.DecodeSlot(OwnershipJournal.SlotA).Sequence;
        var duplicate = h.Arbiter.RequestRelease(h.Request);
        AssertAmbiguousDecision(first);
        Assert.Equal("Duplicate", duplicate.Kind.ToString());
        Assert.Equal(first.DurableRelease, duplicate.DurableRelease);
        Assert.Equal(first.Delivery, duplicate.Delivery);
        Assert.Equal(first.DurableCommitted, duplicate.DurableCommitted);
        Assert.Equal(first.LiveExecutionExcluded, duplicate.LiveExecutionExcluded);
        Assert.Equal(first.Reason, duplicate.Reason);
        Assert.Equal(sequenceBefore, h.DecodeSlot(OwnershipJournal.SlotA).Sequence);
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public async Task C1T07_StopWinsAfterJournalGateAndKeepsExactRefusal()
    {
        using var h = new Harness();
        using var journalReleased = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        h.Storage.FailAt("unlink", 2);
        h.Arbiter.TestBarrier = name =>
        {
            if (name != "after-ambiguous-intent") return;
            journalReleased.Set();
            if (!resume.Wait(Watchdog, TestContext.Current.CancellationToken))
                throw new TimeoutException("journal-release barrier watchdog expired");
        };
        var releaseTask = Task.Run(() => h.Arbiter.RequestRelease(h.Request));
        try
        {
            Assert.True(journalReleased.Wait(Watchdog, TestContext.Current.CancellationToken), "ambiguity did not reach post-journal barrier");
            var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
            Assert.True(stop.Accepted);
            Assert.False(stop.DurableStop);
            resume.Set();
            var decision = await releaseTask.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
            Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
            Assert.Equal(DeliveryResult.NotAttempted, decision.Delivery);
            Assert.False(decision.DurableCommitted);
            Assert.True(decision.LiveExecutionExcluded);
            Assert.Equal("release gate refused: stop won during durable intent", decision.Reason);
            Assert.Equal(0, h.Transport.Attempts);
            var duplicate = h.Arbiter.RequestRelease(h.Request);
            Assert.Equal("Duplicate", duplicate.Kind.ToString());
            Assert.Equal(decision.Reason, duplicate.Reason);
        }
        finally { resume.Set(); }
    }

    [Fact]
    public void C1T08_UnavailableClockStopConsumesCapabilityWithoutDeadlineOrRecord()
    {
        using var h = new Harness();
        h.Storage.FailAt("unlink", 2);
        h.Arbiter.TestBarrier = name => { if (name == "after-ambiguous-intent") h.Clock.NowNanoseconds = 0; };
        var decision = h.Arbiter.RequestRelease(h.Request);
        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        AssertAmbiguousDecision(decision);
        Assert.True(stop.Accepted);
        Assert.Equal(0, stop.StopAcceptedAtNanoseconds);
        Assert.Equal(0, stop.StopDeadlineNanoseconds);
        Assert.False(stop.DurableStop);
        Assert.False(h.Capability.IsLive);
        Assert.True(h.Storage.Exists(Directory, OwnershipJournal.SealName));
        Assert.False(h.Journal.ReadLatest().Ok);
    }

    [Fact]
    public void C1T09_DeadlineOverflowDoesNotInventDeadlineOrRecord()
    {
        using var h = new Harness();
        h.Storage.FailAt("unlink", 2);
        h.Arbiter.TestBarrier = name => { if (name == "after-ambiguous-intent") h.Clock.NowNanoseconds = long.MaxValue - 1; };
        var decision = h.Arbiter.RequestRelease(h.Request);
        var stop = h.Arbiter.RequestStop(StopCauseKind.CallerStop);
        AssertAmbiguousDecision(decision);
        Assert.True(stop.Accepted);
        Assert.Equal(0, stop.StopDeadlineNanoseconds);
        Assert.False(stop.DurableStop);
        Assert.False(h.Capability.IsLive);
        Assert.True(h.Storage.Exists(Directory, OwnershipJournal.SealName));
    }

    [Fact]
    public void C1T10_FailedStopPersistenceCannotRestoreSharedCapability()
    {
        using var h = new Harness();
        var decision = h.RequestAmbiguousRelease();
        var replacement = h.NewArbiter().RequestRelease(h.Request);
        AssertAmbiguousDecision(decision);
        Assert.False(h.Capability.IsLive);
        Assert.Equal(ReleaseDecisionKind.Refused, replacement.Kind);
        Assert.Equal(ReleaseDisposition.NotCommitted, replacement.DurableRelease);
        Assert.Equal(0, h.Transport.Attempts);
        Assert.False(h.Journal.ReadLatest().Ok);
    }

    [Fact]
    public void C1T11_FreshWriterRecoversIntentAsMayHaveExecuted()
    {
        using var h = new Harness();
        var decision = h.RequestAmbiguousRelease();
        var blocked = new OwnershipJournal(h.Storage, Directory);
        Assert.False(blocked.TryAcquireWriter(out _, out var blockedError));
        Assert.Contains("lock", blockedError, StringComparison.OrdinalIgnoreCase);
        h.ReleaseWriter();
        var recovered = new OwnershipJournal(h.Storage, Directory);
        Assert.True(recovered.TryRecoverCommit(out var snapshot, out var error), error);
        Assert.NotNull(snapshot);
        Assert.Equal(ReleaseTag.Intent, snapshot!.Release!.Tag);
        var classification = ReleaseRecoveryClassifier.Classify(snapshot);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, classification.Release);
        Assert.Equal(ExecDisposition.NotAttempted, classification.Exec);
        Assert.Equal(0, h.Transport.Attempts);
        AssertAmbiguousDecision(decision);
    }

    [Fact]
    public void C1T12_RecoveryAndFreshArbiterNeverResendRelease()
    {
        using var h = new Harness();
        h.RequestAmbiguousRelease();
        h.ReleaseWriter();
        var recovered = new OwnershipJournal(h.Storage, Directory);
        Assert.True(recovered.TryRecoverCommit(out var snapshot, out var error), error);
        Assert.Equal(ReleaseTag.Intent, snapshot!.Release!.Tag);
        Assert.True(recovered.TryAcquireWriter(out var freshWriter, out var acquireError), acquireError);
        using (freshWriter!)
        {
            var replacement = new ReleaseArbiter(h.Capability, recovered, freshWriter!, h.Clock,
                () => h.Prerequisites, h.Transport);
            var decision = replacement.RequestRelease(h.Request);
            Assert.Equal(ReleaseDecisionKind.Refused, decision.Kind);
            Assert.Equal(ReleaseDisposition.NotCommitted, decision.DurableRelease);
        }
        var latest = recovered.ReadLatest();
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Intent, latest.Snapshot!.Release!.Tag);
        Assert.Equal(0, h.Transport.Attempts);
    }

    [Fact]
    public async Task C1T13_ConcurrentCallerCannotWriteSecondIntentOrReachFd4()
    {
        using var h = new Harness();
        using var journalReleased = new ManualResetEventSlim();
        using var resume = new ManualResetEventSlim();
        h.Storage.FailAt("unlink", 2);
        h.Arbiter.TestBarrier = name =>
        {
            if (name != "after-ambiguous-intent") return;
            journalReleased.Set();
            if (!resume.Wait(Watchdog, TestContext.Current.CancellationToken))
                throw new TimeoutException("concurrent release barrier watchdog expired");
        };
        var firstTask = Task.Run(() => h.Arbiter.RequestRelease(h.Request));
        try
        {
            Assert.True(journalReleased.Wait(Watchdog, TestContext.Current.CancellationToken), "first caller missed the post-journal barrier");
            var secondTask = Task.Run(() => h.Arbiter.RequestRelease(h.Request));
            var second = await secondTask.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            Assert.Equal(ReleaseDecisionKind.Refused, second.Kind);
            resume.Set();
            var first = await firstTask.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
            Assert.Equal("AmbiguousPersistence", first.Kind.ToString());
            Assert.Equal(0, h.Transport.Attempts);
            Assert.Equal(2, h.DecodeSlot(OwnershipJournal.SlotA).Sequence);
            Assert.Equal(2, h.DecodeSlot(OwnershipJournal.SlotB).Sequence);
        }
        finally { resume.Set(); }
    }

    [Fact]
    public void C1T14_ReleaseFirstSuccessStillWritesOneCommittedRelease()
    {
        using var h = new Harness();
        var decision = h.Arbiter.RequestRelease(h.Request);
        Assert.Equal(ReleaseDecisionKind.Released, decision.Kind);
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(DeliveryResult.Delivered, decision.Delivery);
        Assert.True(decision.DurableCommitted);
        Assert.Equal(1, h.Transport.Attempts);
        var latest = h.Journal.ReadLatest();
        Assert.True(latest.Ok, latest.Error);
        Assert.Equal(ReleaseTag.Committed, latest.Snapshot!.Release!.Tag);
    }

    [Fact]
    public async Task C1T15_JournalRetentionAndTailStopCompleteInLockSafeOrder()
    {
        using var h = new Harness();
        var observed = new ConcurrentQueue<string>();
        h.Storage.FailAt("unlink", 2);
        h.Arbiter.TestBarrier = name => observed.Enqueue(name);
        var task = Task.Run(() => h.Arbiter.RequestRelease(h.Request));
        var decision = await task.WaitAsync(Watchdog, TestContext.Current.CancellationToken);
        Assert.Equal(new[] { "after-ambiguous-intent", "after-ambiguous-retention", "after-stop" }, observed.ToArray());
        Assert.Equal("AmbiguousPersistence", decision.Kind.ToString());
        Assert.False(h.Capability.IsLive);
    }

    [Fact]
    public void NonAmbiguousFailuresDoNotReceivePostContentClassification()
    {
        using var h = new Harness();
        var latest = h.Journal.ReadLatest();
        Assert.True(latest.Ok, latest.Error);
        Assert.True(OwnershipTransitions.TryPlanReleaseIntent(latest.Snapshot!, h.Clock.NowNanoseconds,
            h.SetupDeadline, h.OverallDeadline, out var intent, out var error), error);
        var missingWriter = h.Journal.WriteTransition(null, latest.Snapshot!, intent!);
        var staleBaseline = h.Journal.WriteTransition(h.Writer,
            latest.Snapshot! with { Sequence = latest.Snapshot!.Sequence - 1 }, intent!);
        Assert.NotEqual("AmbiguousAfterContent", JournalFailureKindName(missingWriter));
        Assert.NotEqual("AmbiguousAfterContent", JournalFailureKindName(staleBaseline));
    }

    private static string? JournalFailureKindName(JournalIoResult result) => typeof(JournalIoResult)
        .GetProperty("FailureKind", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        ?.GetValue(result)?.ToString();

    private static void AssertAmbiguousDecision(ReleaseDecision decision)
    {
        Assert.Equal("AmbiguousPersistence", decision.Kind.ToString());
        Assert.Equal(ReleaseDisposition.MayHaveExecuted, decision.DurableRelease);
        Assert.Equal(DeliveryResult.NotAttempted, decision.Delivery);
        Assert.False(decision.DurableCommitted);
        Assert.True(decision.LiveExecutionExcluded);
        Assert.Contains("ambiguous journal persistence", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
