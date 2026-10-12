// WP03 recovery planning and reconciliation. Records are evidence, never
// authority: a plan can authorize destructive cleanup only when the scope
// holds live, corroborated domain handles for the same boot and mount.
// Restart-only (no live handles), cross-boot and mismatched records yield
// Unknown/quarantine with no destructive adoption. Reconciliation is
// idempotent and never resumes work or resurrects a capability.
using Gagamba.Execution.Linux.M1.Lifecycle;

namespace Gagamba.Execution.Linux.M1.Recovery;

/// <summary>What live authority the recovery scope actually holds.</summary>
internal sealed record RecoveryAuthority(
    bool BootMatches,
    bool MountMatches,
    bool HasLiveDomainLease,
    bool LiveLeaseCorroborates,
    bool HasCleanupAuthority);

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
    internal static RecoveryPlan Plan(OwnershipSnapshot record, RecoveryAuthority authority)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(authority);

        // Cross-boot records cannot authorize any action on current objects.
        if (!authority.BootMatches)
        {
            return new RecoveryPlan(RecoveryPlanKind.RefuseCrossBoot, "boot identity mismatch; records are non-authority across boots");
        }

        if (!authority.MountMatches)
        {
            return new RecoveryPlan(RecoveryPlanKind.RefuseCrossBoot, "cgroup mount identity mismatch; refusing destructive recovery");
        }

        // No surviving live handles: restart-only. Quarantine; never adopt
        // an apparently matching current resource.
        if (!authority.HasLiveDomainLease)
        {
            return new RecoveryPlan(RecoveryPlanKind.QuarantineUnknown, "no surviving live authority; restart-only quarantine");
        }

        // Live handles present but they do not corroborate the record
        // identity: a possible replacement. Refuse.
        if (!authority.LiveLeaseCorroborates)
        {
            return new RecoveryPlan(RecoveryPlanKind.RefuseMismatch, "live lease does not corroborate the record identity");
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
        RecoveryAuthority authority,
        DomainLease? lease,
        IAllocationCleaner? cleaner)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(authority);

        var plan = RecoveryPlanner.Plan(record, authority);
        switch (plan.Kind)
        {
            case RecoveryPlanKind.RefuseCrossBoot:
            case RecoveryPlanKind.RefuseMismatch:
                return new ReconcileResult(ReconcileOutcome.Refused, plan.Reason, DestructiveActionTaken: false);
            case RecoveryPlanKind.QuarantineUnknown:
                return new ReconcileResult(ReconcileOutcome.QuarantinedUnknown, plan.Reason, DestructiveActionTaken: false);
        }

        // KillAndClean.
        if (lease is null)
        {
            return new ReconcileResult(ReconcileOutcome.QuarantinedUnknown, "plan requires live lease but none supplied", false);
        }

        var verify = lease.VerifyIdentity();
        if (!verify.Match)
        {
            return new ReconcileResult(ReconcileOutcome.Refused, verify.Error, false);
        }

        var stop = lease.StopAndAwaitEmpty(_stopAllowance, _clock);
        if (!stop.Stopped)
        {
            return new ReconcileResult(ReconcileOutcome.Failed, stop.Error, DestructiveActionTaken: true);
        }

        // Cleanup accounted allocations by identity through retained handles.
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

        var removal = lease.RemoveIfIdentityMatches();
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
