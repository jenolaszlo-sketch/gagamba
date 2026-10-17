// WP05 v3 monotonic transition, stale-baseline, writer-fence and
// observation-merge tests. Pure journal tests; no owner, coordinator, target
// or release transport is exercised.
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class OwnershipV3TransitionTests
{
    private const string Dir = "/recovery";

    private static OwnershipSnapshot V2Prepared() =>
        RecoveryTestSupport.Snapshot(
            stage: PreparationStage.Prepared,
            preparation: RecoveryTestSupport.PreparationIdentity());

    private static (OwnershipJournal Journal, JournalWriterLease Lease, OwnershipSnapshot Latest) Seed(
        OwnershipSnapshot snapshot)
    {
        var storage = new FakeJournalStorage();
        var journal = new OwnershipJournal(storage, Dir);
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        Assert.True(journal.WriteNext(lease, snapshot).Ok);
        var latest = journal.ReadLatest();
        Assert.True(latest.Ok, latest.Error);
        return (journal, lease!, latest.Snapshot!);
    }

    [Fact]
    public void LazyV2ToV3IntentPreservesCompleteSnapshot()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.Equal(OwnershipSnapshotCodec.LegacyVersion, latest.Version);
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                latest, 1_000, 10_000, 30_000, out var intent, out var error), error);
            Assert.NotNull(intent);
            Assert.Equal(OwnershipSnapshotCodec.CurrentVersion, intent!.Version);
            var write = journal.WriteTransition(lease, latest, intent);
            Assert.True(write.Ok, write.Error);

            var read = journal.ReadLatest();
            Assert.True(read.Ok, read.Error);
            Assert.Equal(OwnershipSnapshotCodec.CurrentVersion, read.Snapshot!.Version);
            Assert.Equal(ReleaseTag.Intent, read.Snapshot.Release!.Tag);
            Assert.Equal(latest.ExecutionId, read.Snapshot.ExecutionId);
            Assert.Equal(latest.BootId, read.Snapshot.BootId);
            Assert.Equal(latest.LeaseGeneration, read.Snapshot.LeaseGeneration);
            Assert.Equal(latest.Stage, read.Snapshot.Stage);
            Assert.True(latest.Preparation!.ManifestDigest.AsSpan()
                .SequenceEqual(read.Snapshot.Preparation!.ManifestDigest));
            Assert.Equal(latest.Allocations.Count, read.Snapshot.Allocations.Count);
            Assert.Null(read.Snapshot.Stop);
            Assert.Null(read.Snapshot.Exec);
            Assert.Null(read.Snapshot.Root);
        }
    }

    [Fact]
    public void IntentThenStopPreservesIntent()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                latest, 1_000, 10_000, 30_000, out var intent, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, intent!).Ok);
            var afterIntent = journal.ReadLatest().Snapshot!;

            Assert.True(OwnershipTransitions.TryPlanStop(
                afterIntent, 2_000, StopCauseKind.CallerStop, out var stop, out error), error);
            Assert.True(journal.WriteTransition(lease, afterIntent, stop!).Ok);

            var read = journal.ReadLatest().Snapshot!;
            Assert.Equal(ReleaseTag.Intent, read.Release!.Tag);
            Assert.NotNull(read.Stop);
            Assert.Equal(2_000 + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds,
                read.Stop!.StopDeadlineNanoseconds);
        }
    }

    [Fact]
    public void StopFirstForbidsLaterReleaseIntent()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanStop(
                latest, 2_000, StopCauseKind.CallerStop, out var stop, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, stop!).Ok);
            var afterStop = journal.ReadLatest().Snapshot!;
            Assert.False(OwnershipTransitions.TryPlanReleaseIntent(
                afterStop, 3_000, 10_000, 30_000, out _, out error));
            Assert.Contains("forbidden after a durable stop", error);
            Assert.Null(journal.ReadLatest().Snapshot!.Release);
        }
    }

    [Fact]
    public void CommitPreservesStopAndLaterStopPreservesCommit()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                latest, 1_000, 10_000, 30_000, out var intent, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, intent!).Ok);
            var afterIntent = journal.ReadLatest().Snapshot!;

            Assert.True(OwnershipTransitions.TryPlanReleaseCommitted(
                afterIntent, 2_000, DeliveryResult.Delivered, out var committed, out error), error);
            Assert.True(journal.WriteTransition(lease, afterIntent, committed!).Ok);
            var afterCommit = journal.ReadLatest().Snapshot!;

            Assert.True(OwnershipTransitions.TryPlanStop(
                afterCommit, 3_000, StopCauseKind.CallerStop, out var stop, out error), error);
            Assert.True(journal.WriteTransition(lease, afterCommit, stop!).Ok);

            var read = journal.ReadLatest().Snapshot!;
            Assert.Equal(ReleaseTag.Committed, read.Release!.Tag);
            Assert.Equal(DeliveryResult.Delivered, read.Release.DeliveryResult);
            Assert.Equal(2_000, read.Release.CommittedAtNanoseconds);
            Assert.NotNull(read.Stop);
        }
    }

    [Fact]
    public void ObservationsMergeWithoutErasingReleaseOrStop()
    {
        var baseline = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            stop: RecoveryTestSupport.Stop());
        var (journal, lease, latest) = Seed(baseline);
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanExecObservation(
                latest, RecoveryTestSupport.ExecConfirmed(), out var exec, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, exec!).Ok);
            var afterExec = journal.ReadLatest().Snapshot!;
            Assert.Equal(ReleaseTag.Committed, afterExec.Release!.Tag);
            Assert.Equal(DeliveryResult.Delivered, afterExec.Release.DeliveryResult);
            Assert.NotNull(afterExec.Stop);
            Assert.NotNull(afterExec.Exec);

            Assert.True(OwnershipTransitions.TryPlanRootObservation(
                afterExec, RecoveryTestSupport.RootExited(), out var root, out error), error);
            Assert.True(journal.WriteTransition(lease, afterExec, root!).Ok);
            var afterRoot = journal.ReadLatest().Snapshot!;
            Assert.Equal(ReleaseTag.Committed, afterRoot.Release!.Tag);
            Assert.Equal(DeliveryResult.Delivered, afterRoot.Release.DeliveryResult);
            Assert.NotNull(afterRoot.Stop);
            Assert.NotNull(afterRoot.Exec);
            Assert.NotNull(afterRoot.Root);
            Assert.Equal(RootKind.Exited, afterRoot.Root!.Kind);
        }
    }

    [Fact]
    public void CleanupMutationPreservesAllDurableFacts()
    {
        var baseline = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            stop: RecoveryTestSupport.Stop(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootUnknown());
        var (journal, lease, latest) = Seed(baseline);
        using (lease)
        {
            var write = journal.WriteMutation(lease, current => current with
            {
                Cleanup = OwnershipCleanupState.Confirmed,
                CompletedCleanup = (byte)(current.CompletedCleanup | 0x1F),
            });
            Assert.True(write.Ok, write.Error);

            var read = journal.ReadLatest().Snapshot!;
            Assert.Equal(OwnershipCleanupState.Confirmed, read.Cleanup);
            Assert.Equal((byte)0x1F, read.CompletedCleanup);
            Assert.Equal(baseline.Release, read.Release);
            Assert.Equal(baseline.Stop, read.Stop);
            Assert.Equal(baseline.Exec, read.Exec);
            Assert.Equal(baseline.Root, read.Root);
        }
    }

    [Fact]
    public void MutationCannotClearDurableFacts()
    {
        var baseline = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            stop: RecoveryTestSupport.Stop(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootUnknown());
        var (journal, lease, latest) = Seed(baseline);
        using (lease)
        {
            Assert.False(journal.WriteMutation(lease, current => current with { Release = null }).Ok);
            Assert.False(journal.WriteMutation(lease, current => current with { Stop = null }).Ok);
            Assert.False(journal.WriteMutation(lease, current => current with { Exec = null }).Ok);
            Assert.False(journal.WriteMutation(lease, current => current with { Root = null }).Ok);

            var read = journal.ReadLatest().Snapshot!;
            Assert.NotNull(read.Release);
            Assert.NotNull(read.Stop);
            Assert.NotNull(read.Exec);
            Assert.NotNull(read.Root);
        }
    }

    [Fact]
    public void StaleBaselineCannotEraseNewerFacts()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            var stale = latest;
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                latest, 1_000, 10_000, 30_000, out var intent, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, intent!).Ok);

            var staleAttempt = journal.WriteTransition(lease, stale, intent!);
            Assert.False(staleAttempt.Ok);
            Assert.Contains("stale baseline", staleAttempt.Error);

            var current = journal.ReadLatest().Snapshot!;
            Assert.NotNull(current.Release);
            Assert.Equal(ReleaseTag.Intent, current.Release!.Tag);
        }
    }

    [Fact]
    public void StaleBaselineCannotEraseStopOrObservations()
    {
        var baseline = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted(),
            stop: RecoveryTestSupport.Stop(),
            exec: RecoveryTestSupport.ExecConfirmed(),
            root: RecoveryTestSupport.RootUnknown());
        var (journal, lease, latest) = Seed(baseline);
        using (lease)
        {
            var stale = latest;
            var write = journal.WriteMutation(lease, current => current with
            {
                Cleanup = OwnershipCleanupState.Confirmed,
                CompletedCleanup = 1,
            });
            Assert.True(write.Ok, write.Error);

            var staleAttempt = journal.WriteTransition(lease, stale, stale);
            Assert.False(staleAttempt.Ok);
            Assert.Contains("stale baseline", staleAttempt.Error);

            var current = journal.ReadLatest().Snapshot!;
            Assert.NotNull(current.Stop);
            Assert.NotNull(current.Exec);
            Assert.NotNull(current.Root);
            Assert.Equal(OwnershipCleanupState.Confirmed, current.Cleanup);
        }
    }

    [Fact]
    public void StaleBaselineCannotEraseCommittedRelease()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                latest, 1_000, 10_000, 30_000, out var intent, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, intent!).Ok);
            var afterIntent = journal.ReadLatest().Snapshot!;

            Assert.True(OwnershipTransitions.TryPlanReleaseCommitted(
                afterIntent, 2_000, DeliveryResult.Delivered, out var committed, out error), error);
            Assert.True(journal.WriteTransition(lease, afterIntent, committed!).Ok);

            var staleAttempt = journal.WriteTransition(lease, afterIntent, afterIntent);
            Assert.False(staleAttempt.Ok);
            Assert.Contains("stale baseline", staleAttempt.Error);
            Assert.Equal(ReleaseTag.Committed, journal.ReadLatest().Snapshot!.Release!.Tag);
        }
    }

    [Fact]
    public void ObservationsAreSingleAssignmentAndSurviveLaterStop()
    {
        var baseline = RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted());
        var (journal, lease, latest) = Seed(baseline);
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanExecObservation(
                latest, RecoveryTestSupport.ExecConfirmed(), out var exec, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, exec!).Ok);
            var afterExec = journal.ReadLatest().Snapshot!;
            Assert.True(OwnershipTransitions.TryPlanExecObservation(
                afterExec, RecoveryTestSupport.ExecConfirmed(), out var sameExec, out error), error);
            Assert.Equal(afterExec.Sequence, sameExec!.Sequence);
            Assert.False(OwnershipTransitions.TryPlanExecObservation(
                afterExec, RecoveryTestSupport.ExecUnknown(), out _, out error));
            Assert.Contains("single-assignment", error);

            Assert.True(OwnershipTransitions.TryPlanRootObservation(
                afterExec, RecoveryTestSupport.RootExited(), out var root, out error), error);
            Assert.True(journal.WriteTransition(lease, afterExec, root!).Ok);
            var afterRoot = journal.ReadLatest().Snapshot!;
            Assert.True(OwnershipTransitions.TryPlanRootObservation(
                afterRoot, RecoveryTestSupport.RootExited(), out var sameRoot, out error), error);
            Assert.Equal(afterRoot.Sequence, sameRoot!.Sequence);
            Assert.False(OwnershipTransitions.TryPlanRootObservation(
                afterRoot, RecoveryTestSupport.RootUnknown(), out _, out error));

            Assert.True(OwnershipTransitions.TryPlanStop(
                afterRoot, 3_000, StopCauseKind.CallerStop, out var stop, out error), error);
            Assert.True(journal.WriteTransition(lease, afterRoot, stop!).Ok);
            var read = journal.ReadLatest().Snapshot!;
            Assert.NotNull(read.Exec);
            Assert.NotNull(read.Root);
            Assert.NotNull(read.Stop);
            Assert.Equal(ReleaseTag.Committed, read.Release!.Tag);
        }
    }

    [Fact]
    public void ChangedLineageAndForeignLeaseRefused()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            var changed = latest with { DomainInode = 999 };
            var changedAttempt = journal.WriteTransition(lease, changed, latest);
            Assert.False(changedAttempt.Ok);
            Assert.Contains("stale baseline", changedAttempt.Error);

            var otherStorage = new FakeJournalStorage();
            var other = new OwnershipJournal(otherStorage, Dir);
            Assert.False(other.WriteTransition(lease, latest, latest).Ok);
        }
    }

    [Fact]
    public void WrongSequenceAndNoFactTransitionRefused()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.False(journal.Write(lease, latest with { Sequence = latest.Sequence + 2 }).Ok);
            Assert.False(journal.Write(lease, latest with { Sequence = latest.Sequence }).Ok);

            var noOp = journal.WriteTransition(lease, latest, latest);
            Assert.False(noOp.Ok);
            Assert.Contains("no durable fact", noOp.Error);
        }
    }

    [Fact]
    public void ReleaseIdentityAndDeadlinesCannotMutate()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanReleaseIntent(
                latest, 1_000, 10_000, 30_000, out var intent, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, intent!).Ok);
            var current = journal.ReadLatest().Snapshot!;

            var changedIdentity = current with
            {
                Preparation = RecoveryTestSupport.PreparationIdentity(0x22),
            };
            Assert.False(journal.WriteTransition(lease, current, changedIdentity).Ok);

            var changedDeadline = current with
            {
                Release = current.Release! with { SetupDeadlineNanoseconds = 20_000 },
            };
            Assert.False(journal.WriteTransition(lease, current, changedDeadline).Ok);
        }
    }

    [Fact]
    public void CommittedWithoutPriorIntentRefused()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            var committed = latest with
            {
                Version = OwnershipSnapshotCodec.CurrentVersion,
                Release = RecoveryTestSupport.ReleaseCommitted(),
            };
            var attempt = journal.WriteTransition(lease, latest, committed);
            Assert.False(attempt.Ok);
            Assert.Contains("prior acknowledged intent", attempt.Error);
            Assert.Null(journal.ReadLatest().Snapshot!.Release);
        }
    }

    [Fact]
    public void OneFactPerTransitionAndDiscoveryRules()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            var intent = new ReleaseRecord(ReleaseTag.Intent, 1_000, 10_000, 30_000);
            var stop = RecoveryTestSupport.Stop(2_000);
            var combined = latest with
            {
                Version = OwnershipSnapshotCodec.CurrentVersion,
                Release = intent,
                Stop = stop,
            };
            var attempt = journal.WriteTransition(lease, latest, combined);
            Assert.False(attempt.Ok);
            Assert.Contains("exactly one", attempt.Error);

            var execWithoutCommit = latest with
            {
                Version = OwnershipSnapshotCodec.CurrentVersion,
                Exec = RecoveryTestSupport.ExecUnknown(),
            };
            Assert.False(journal.WriteTransition(lease, latest, execWithoutCommit).Ok);
        }
    }

    [Fact]
    public void SecondStopCannotRewriteFirstStopDeadline()
    {
        var (journal, lease, latest) = Seed(V2Prepared());
        using (lease)
        {
            Assert.True(OwnershipTransitions.TryPlanStop(
                latest, 2_000, StopCauseKind.CallerStop, out var stop, out var error), error);
            Assert.True(journal.WriteTransition(lease, latest, stop!).Ok);
            var current = journal.ReadLatest().Snapshot!;

            Assert.False(OwnershipTransitions.TryPlanStop(
                current, 3_000, StopCauseKind.RecoveryTakeover, out _, out error));
            Assert.Contains("immutable", error);

            var rewritten = current with
            {
                Stop = new StopRecord(3_000, StopCauseKind.RecoveryTakeover,
                    3_000 + OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds),
            };
            Assert.False(journal.WriteTransition(lease, current, rewritten).Ok);
            Assert.Equal(current.Stop, journal.ReadLatest().Snapshot!.Stop);
        }
    }

    [Fact]
    public void Wp04LegacyIntentStillEncodesVersion2()
    {
        var specification = new OwnerPreparationSpecification(
            Path.GetFullPath("journal"), "domain-a", RecoveryTestSupport.Execution,
            RecoveryTestSupport.Lease, "boot-a", Guid.NewGuid(), Guid.NewGuid(),
            Path.GetFullPath("source"), Path.GetFullPath("staging"), 12, 34, "prep-a",
            new[] { new CopyObjectPlan("tool", "tool.bin", 4096, null) }, 8192, 8,
            Array.Empty<KeyValuePair<string, string>>(), 100_000_000);
        var intent = OwnerPreparationSpecificationCodec.BuildIntent(specification);
        Assert.Equal(OwnershipSnapshotCodec.LegacyVersion, intent.Version);
        Assert.Equal((byte)OwnershipSnapshotCodec.LegacyVersion,
            OwnershipSnapshotCodec.Encode(intent)[0]);
    }
}
