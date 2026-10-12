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

internal sealed record ReconcileResult(ReconcileOutcome Outcome, string Detail, bool DestructiveActionTaken);

/// <summary>Identity-checked cleanup of accounted allocations through
/// retained parent handles. Never arbitrary-path, never journal-only.</summary>
internal interface IAllocationCleaner
{
    DomainIoResult CleanOne(AllocationEntry entry);
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

internal sealed class Reconciler
{
    private readonly IDomainClock _clock;
    private readonly TimeSpan _stopAllowance;

    internal Reconciler(IDomainClock clock, TimeSpan stopAllowance)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _stopAllowance = stopAllowance;
    }

    /// <summary>Idempotent reconciliation. Repeating a call returns the
    /// same facts plus any new cleanup observations.</summary>
    internal ReconcileResult Reconcile(
        OwnershipSnapshot record,
        string currentBootId,
        DomainLease? liveLease,
        IAllocationCleaner? cleaner)
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

        // KillAndClean: re-verify the retained handle still refers to the
        // captured object, then stop/observe/clean by retained authority.
        var verify = liveLease!.VerifyIdentity();
        if (!verify.Match)
        {
            return new ReconcileResult(ReconcileOutcome.Refused, verify.Error, false);
        }

        var stop = liveLease.StopAndAwaitEmpty(_stopAllowance, _clock);
        if (!stop.Stopped)
        {
            return new ReconcileResult(ReconcileOutcome.Failed, stop.Error, DestructiveActionTaken: true);
        }

        if (cleaner is not null)
        {
            foreach (var allocation in record.Allocations)
            {
                var clean = cleaner.CleanOne(allocation);
                if (!clean.Ok)
                {
                    return new ReconcileResult(ReconcileOutcome.Failed, clean.Error, DestructiveActionTaken: true);
                }
            }
        }

        var removal = liveLease.RemoveIfIdentityMatches();
        if (removal.Refused)
        {
            return new ReconcileResult(ReconcileOutcome.QuarantinedUnknown, removal.Error, DestructiveActionTaken: true);
        }

        if (!removal.Removed)
        {
            return new ReconcileResult(ReconcileOutcome.Failed, removal.Error, DestructiveActionTaken: true);
        }

        return new ReconcileResult(ReconcileOutcome.CleanupConfirmed, "domain stopped, allocations cleaned, domain removed", DestructiveActionTaken: true);
    }
}
