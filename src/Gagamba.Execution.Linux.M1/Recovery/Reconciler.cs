// WP03 recovery planning and reconciliation. Records are evidence, never
// authority. Recovery eligibility is derived from the durable record, the
// current boot context and the actual retained live lease — never from a
// caller-supplied "identity matches" boolean. A plan can authorize
// destructive cleanup only when a live, complete-identity-corroborating
// lease exists for the same boot. Restart-only (no live handles),
// cross-boot and mismatched records yield Unknown/quarantine with no
// destructive adoption. Reconciliation is idempotent and never resumes
// work or resurrects a capability.
using Gagamba.Execution.Linux.M1.Lifecycle;

namespace Gagamba.Execution.Linux.M1.Recovery;

internal enum RecoveryPlanKind : byte
{
    KillAndClean = 0,
    QuarantineUnknown = 1,
    RefuseCrossBoot = 2,
    RefuseMismatch = 3,
}

internal sealed record RecoveryPlan(RecoveryPlanKind Kind, string Reason);

internal enum ReconcileOutcome : byte
{
    CleanupConfirmed = 0,
    QuarantinedUnknown = 1,
    Refused = 2,
    Failed = 3,
}

internal sealed record ReconcileResult(
    ReconcileOutcome Outcome,
    string Detail,
    bool DestructiveActionTaken,
    IReadOnlyList<ComponentOutcome>? Components = null);

/// <summary>Identity-checked cleanup of accounted allocations through
/// retained parent handles. Never arbitrary-path, never journal-only.</summary>
internal enum AllocationCleanOutcome : byte
{
    Cleaned,
    AlreadyAbsent,
    Mismatch,
    Failed,
}

internal readonly record struct AllocationCleanResult(AllocationCleanOutcome Outcome, string Detail);

internal interface IAllocationCleaner
{
    AllocationCleanResult CleanOne(AllocationEntry entry);
}

internal static class RecoveryPlanner
{
    /// <summary>Builds the identity the durable record asserts, used only
    /// for corroboration against a live lease.</summary>
    internal static DomainIdentity DeriveIdentity(OwnershipSnapshot record) =>
        new(
            record.ExecutionId,
            record.BootId,
            record.CgroupMountId,
            record.CgroupMountFsType,
            record.ParentDevice,
            record.ParentInode,
            record.DomainName,
            record.DomainDevice,
            record.DomainInode,
            record.LeaseGeneration);

    /// <summary>Eligibility is derived from the record, the current boot
    /// and the actual live lease. Caller-supplied identity assertions never
    /// grant authority.</summary>
    internal static RecoveryPlan Plan(OwnershipSnapshot record, string currentBootId, DomainLease? liveLease)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(currentBootId);

        if (!string.Equals(record.BootId, currentBootId, StringComparison.Ordinal))
        {
            return new RecoveryPlan(RecoveryPlanKind.RefuseCrossBoot, "boot identity mismatch; records are non-authority across boots");
        }

        if (liveLease is null || !liveLease.AuthorityAlive)
        {
            return new RecoveryPlan(RecoveryPlanKind.QuarantineUnknown, "no surviving live authority; restart-only quarantine");
        }

        var derived = DeriveIdentity(record);
        if (!liveLease.Identity.Corroborates(derived))
        {
            return new RecoveryPlan(RecoveryPlanKind.RefuseMismatch, "live lease identity does not corroborate the record");
        }

        return new RecoveryPlan(RecoveryPlanKind.KillAndClean, "live corroborated authority; proceed with stop and identity-checked cleanup");
    }
}

[Flags]
internal enum CleanupComponents : byte
{
    None = 0,
    DomainStop = 1,
    DomainEmpty = 2,
    Staging = 4,
    DomainRemoval = 8,
    JournalFinalization = 16,
}

internal enum ComponentState : byte
{
    Pending,
    Confirmed,
    Failed,
    Unknown,
}

internal sealed record ComponentOutcome(CleanupComponents Component, ComponentState State, string Detail);

internal sealed class Reconciler
{
    private readonly IDomainClock _clock;
    private readonly TimeSpan _stopAllowance;

    internal Reconciler(IDomainClock clock, TimeSpan stopAllowance)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _stopAllowance = stopAllowance;
    }

    /// <summary>Idempotent component-wise reconciliation. Every required
    /// component is evaluated from live authority and actual outcomes; a
    /// missing cleaner or missing authority yields Unknown debt, never
    /// assumed success. Progress persists monotonically under the writer
    /// lease when one is supplied; without it, finalization stays Unknown
    /// while urgent domain stop may still proceed.</summary>
    internal ReconcileResult Reconcile(
        OwnershipSnapshot record,
        string currentBootId,
        DomainLease? liveLease,
        IAllocationCleaner? cleaner,
        OwnershipJournal? journal,
        JournalWriterLease? writer)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(currentBootId);

        var plan = RecoveryPlanner.Plan(record, currentBootId, liveLease);
        switch (plan.Kind)
        {
            case RecoveryPlanKind.RefuseCrossBoot:
            case RecoveryPlanKind.RefuseMismatch:
                return new ReconcileResult(ReconcileOutcome.Refused, plan.Reason, DestructiveActionTaken: false);
            case RecoveryPlanKind.QuarantineUnknown:
                return new ReconcileResult(ReconcileOutcome.QuarantinedUnknown, plan.Reason, DestructiveActionTaken: false);
        }

        var outcomes = new List<ComponentOutcome>();
        var destructive = false;
        var bits = record.CompletedCleanup;
        var finalizationFailed = false;
        var finalizationDetail = string.Empty;

        bool TryPersist(byte newBits, OwnershipCleanupState cleanup, out string error)
        {
            error = string.Empty;
            if (journal is null || writer is null)
            {
                return false;
            }

            var persisted = journal.WriteNext(writer, record with { CompletedCleanup = newBits, Cleanup = cleanup });
            if (!persisted.Ok)
            {
                error = persisted.Error;
                return false;
            }

            return true;
        }

        void Note(CleanupComponents component, ComponentState state, string detail) =>
            outcomes.Add(new ComponentOutcome(component, state, detail));

        // DomainStop (+DomainEmpty established by the same bounded wait).
        var stopState = ComponentState.Pending;
        if ((bits & (byte)CleanupComponents.DomainStop) != 0)
        {
            stopState = ComponentState.Confirmed;
            Note(CleanupComponents.DomainStop, stopState, "previously verified");
        }
        else
        {
            var verify = liveLease!.VerifyIdentity();
            if (!verify.Match)
            {
                return new ReconcileResult(ReconcileOutcome.Refused, verify.Error, false, outcomes);
            }

            destructive = true;
            var stop = liveLease.StopAndAwaitEmpty(_stopAllowance, _clock);
            if (!stop.Stopped)
            {
                Note(CleanupComponents.DomainStop, ComponentState.Failed, stop.Error);
                stopState = ComponentState.Failed;
            }
            else
            {
                bits |= (byte)(CleanupComponents.DomainStop | CleanupComponents.DomainEmpty);
                stopState = ComponentState.Confirmed;
                Note(CleanupComponents.DomainStop, stopState, "stopped and observed empty");
                if (!TryPersist(bits, record.Cleanup, out var persistError))
                {
                    finalizationFailed = true;
                    finalizationDetail = $"stop progress persistence failed: {persistError}";
                }
            }
        }

        // DomainEmpty.
        var emptyState = ComponentState.Pending;
        if ((bits & (byte)CleanupComponents.DomainEmpty) != 0)
        {
            emptyState = ComponentState.Confirmed;
            Note(CleanupComponents.DomainEmpty, emptyState, "previously verified");
        }
        else if (stopState == ComponentState.Confirmed)
        {
            // Established by the successful bounded wait above.
            emptyState = ComponentState.Confirmed;
            Note(CleanupComponents.DomainEmpty, emptyState, "established by stop");
        }
        else
        {
            var population = liveLease!.ObservePopulation();
            if (population.Known && population.Empty)
            {
                bits |= (byte)CleanupComponents.DomainEmpty;
                emptyState = ComponentState.Confirmed;
                Note(CleanupComponents.DomainEmpty, emptyState, "observed empty");
                if (!TryPersist(bits, record.Cleanup, out var persistError))
                {
                    finalizationFailed = true;
                    finalizationDetail = $"empty progress persistence failed: {persistError}";
                }
            }
            else if (population.Known)
            {
                emptyState = ComponentState.Failed;
                Note(CleanupComponents.DomainEmpty, emptyState, "domain still populated");
            }
            else
            {
                emptyState = ComponentState.Unknown;
                Note(CleanupComponents.DomainEmpty, emptyState, population.Error);
            }
        }

        // Staging: every Staging-kind allocation must be cleaned through
        // live authority, then confirmed StagingDirectory containers; a
        // missing cleaner is debt, never success. Unconfirmed directory
        // entries are skipped (their objects still resolve individually);
        // Domain-kind entries are covered by domain removal, not the
        // cleaner.
        var stagingState = ComponentState.Pending;
        var stagingObjects = record.Allocations.Where(a => a.Kind == AllocationKind.Staging).ToArray();
        var stagingDirs = record.Allocations
            .Where(a => a.Kind == AllocationKind.StagingDirectory && a.Confirmed)
            .ToArray();
        if (stagingObjects.Length == 0 && stagingDirs.Length == 0)
        {
            stagingState = ComponentState.Confirmed;
            Note(CleanupComponents.Staging, stagingState, "no staging allocations recorded");
        }
        else if ((bits & (byte)CleanupComponents.Staging) != 0)
        {
            stagingState = ComponentState.Confirmed;
            Note(CleanupComponents.Staging, stagingState, "previously verified");
        }
        else if (cleaner is null)
        {
            stagingState = ComponentState.Unknown;
            Note(CleanupComponents.Staging, stagingState, "recorded staging allocations but no cleanup authority");
        }
        else
        {
            destructive = true;
            var sawFailure = false;
            var sawMismatch = false;
            var mismatchDetail = string.Empty;
            var failureDetail = string.Empty;
            foreach (var allocation in stagingObjects)
            {
                var clean = cleaner.CleanOne(allocation);
                switch (clean.Outcome)
                {
                    case AllocationCleanOutcome.Cleaned:
                    case AllocationCleanOutcome.AlreadyAbsent:
                        break;
                    case AllocationCleanOutcome.Mismatch:
                        sawMismatch = true;
                        mismatchDetail = clean.Detail;
                        break;
                    default:
                        sawFailure = true;
                        failureDetail = clean.Detail;
                        break;
                }
            }

            // Containers are removed only when every object inside them is
            // already gone; a pending object debt keeps its directory.
            if (!sawFailure && !sawMismatch)
            {
                foreach (var directory in stagingDirs)
                {
                    var clean = cleaner.CleanOne(directory);
                    switch (clean.Outcome)
                    {
                        case AllocationCleanOutcome.Cleaned:
                        case AllocationCleanOutcome.AlreadyAbsent:
                            break;
                        case AllocationCleanOutcome.Mismatch:
                            sawMismatch = true;
                            mismatchDetail = clean.Detail;
                            break;
                        default:
                            sawFailure = true;
                            failureDetail = clean.Detail;
                            break;
                    }
                }
            }

            if (sawFailure)
            {
                stagingState = ComponentState.Failed;
                Note(CleanupComponents.Staging, stagingState, failureDetail);
            }
            else if (sawMismatch)
            {
                stagingState = ComponentState.Unknown;
                Note(CleanupComponents.Staging, stagingState, $"replacement preserved: {mismatchDetail}");
            }
            else
            {
                bits |= (byte)CleanupComponents.Staging;
                stagingState = ComponentState.Confirmed;
                Note(CleanupComponents.Staging, stagingState, "all staging allocations cleaned");
                if (!TryPersist(bits, record.Cleanup, out var persistError))
                {
                    finalizationFailed = true;
                    finalizationDetail = $"staging progress persistence failed: {persistError}";
                }
            }
        }

        // DomainRemoval.
        var removalState = ComponentState.Pending;
        if ((bits & (byte)CleanupComponents.DomainRemoval) != 0)
        {
            removalState = ComponentState.Confirmed;
            Note(CleanupComponents.DomainRemoval, removalState, "previously verified");
        }
        else
        {
            destructive = true;
            var removal = liveLease!.RemoveIfIdentityMatches();
            if (removal.Removed)
            {
                bits |= (byte)CleanupComponents.DomainRemoval;
                removalState = ComponentState.Confirmed;
                Note(CleanupComponents.DomainRemoval, removalState, "removed");
                if (!TryPersist(bits, record.Cleanup, out var persistError))
                {
                    finalizationFailed = true;
                    finalizationDetail = $"removal progress persistence failed: {persistError}";
                }
            }
            else if (removal.Refused)
            {
                removalState = ComponentState.Unknown;
                Note(CleanupComponents.DomainRemoval, removalState, removal.Error);
            }
            else
            {
                removalState = ComponentState.Failed;
                Note(CleanupComponents.DomainRemoval, removalState, removal.Error);
            }
        }

        // JournalFinalization + truthful aggregation.
        var finalizationState = ComponentState.Pending;
        var required = new[] { stopState, emptyState, stagingState, removalState };
        OwnershipCleanupState verdict;
        ReconcileOutcome outcome;
        string detail;
        if (required.Any(s => s == ComponentState.Failed) || finalizationFailed)
        {
            verdict = OwnershipCleanupState.Failed;
            outcome = ReconcileOutcome.Failed;
            detail = finalizationFailed && !required.Any(s => s == ComponentState.Failed)
                ? finalizationDetail
                : "one or more required cleanup components failed";
        }
        else if (required.Any(s => s == ComponentState.Unknown))
        {
            verdict = OwnershipCleanupState.Unknown;
            outcome = ReconcileOutcome.QuarantinedUnknown;
            detail = "required cleanup state cannot be established";
        }
        else
        {
            verdict = OwnershipCleanupState.Confirmed;
            outcome = ReconcileOutcome.CleanupConfirmed;
            detail = "all required cleanup components confirmed";
        }

        if (journal is null || writer is null)
        {
            finalizationState = ComponentState.Unknown;
            Note(CleanupComponents.JournalFinalization, finalizationState, "persistence uncertainty: no writer lease supplied");
            if (outcome == ReconcileOutcome.CleanupConfirmed)
            {
                outcome = ReconcileOutcome.QuarantinedUnknown;
                detail = "operations confirmed but persistence unavailable; explicit Unknown";
            }
        }
        else if (TryPersist(bits, verdict, out var finalError))
        {
            finalizationState = ComponentState.Confirmed;
            Note(CleanupComponents.JournalFinalization, finalizationState, "progress persisted");
        }
        else
        {
            finalizationState = ComponentState.Failed;
            Note(CleanupComponents.JournalFinalization, finalizationState, $"persistence failed: {finalError}");
            outcome = ReconcileOutcome.Failed;
            detail = $"cleanup progress persistence failed distinctly from operations: {finalError}";
        }

        return new ReconcileResult(outcome, detail, destructive, outcomes);
    }
}
