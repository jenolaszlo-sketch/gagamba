// WP03 recovery planning and reconciliation. Records are evidence, never
// authority. Recovery eligibility is derived from the durable record, the
// current boot context and the actual retained live lease — never from a
// caller-supplied "identity matches" boolean. A plan can authorize
// destructive cleanup only when a live, complete-identity-corroborating
// lease exists for the same boot. Restart-only (no live handles),
// cross-boot and mismatched records yield Unknown/quarantine with no
// destructive adoption. Reconciliation is idempotent and never resumes
// work or resurrects a capability.
using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Restricted;

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
    private readonly TimeSpan _noRecordCleanupAllowance;

    /// <summary>Production composition. The sealed clock is the only source of
    /// durable StopRecord deadline arithmetic; a caller-supplied allowance is
    /// retained only for records that carry no accepted durable stop.</summary>
    internal Reconciler(StopwatchDomainClock clock, TimeSpan noRecordCleanupAllowance)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _noRecordCleanupAllowance = noRecordCleanupAllowance;
    }

    /// <summary>Explicitly test-only deterministic seam. Production call sites
    /// use the sealed <see cref="StopwatchDomainClock"/> constructor, so a
    /// foreign clock (including one declaring a Qpc label) cannot be passed
    /// from production composition without a reviewed source change.</summary>
    internal Reconciler(IDomainClock clock, TimeSpan stopAllowance)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _noRecordCleanupAllowance = stopAllowance;
    }

#if GAGAMBA_WP05_D6_4_TEST_FIXTURE
    private readonly NotAttemptedStopTestFixture? _notAttemptedStopFixture;

    internal Reconciler(
        IDomainClock clock,
        TimeSpan stopAllowance,
        NotAttemptedStopTestFixture fixture)
        : this(clock, stopAllowance)
    {
        _notAttemptedStopFixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }
#endif

    /// <summary>Derives the bounded stop window from the durable record. A
    /// valid StopRecord is authoritative: the caller-supplied allowance is
    /// ignored. Any unverifiable provenance, epoch, frequency, timestamp or
    /// arithmetic refuses before destructive action so no fresh window can be
    /// manufactured.</summary>
    private bool TryResolveStopBudget(
        StopRecord? stop,
        out long budgetNanoseconds,
        out string refusal)
    {
        budgetNanoseconds = 0;
        refusal = string.Empty;

        if (stop is null)
        {
            if (_noRecordCleanupAllowance < TimeSpan.Zero)
            {
                refusal = "cleanup allowance is negative; no durable stop deadline exists";
                return false;
            }

            budgetNanoseconds = (long)(_noRecordCleanupAllowance.TotalSeconds * 1_000_000_000d);
            return true;
        }

        if (stop.StopAcceptedAtNanoseconds <= 0
            || stop.StopDeadlineNanoseconds <= 0
            || stop.StopDeadlineNanoseconds < stop.StopAcceptedAtNanoseconds)
        {
            refusal = "durable stop deadline is invalid; refusing destructive action";
            return false;
        }

        if ((byte)stop.Cause > (byte)StopCauseKind.TransportFailure)
        {
            refusal = "durable stop cause is invalid; refusing destructive action";
            return false;
        }

        long declaredAllowance;
        try
        {
            checked { declaredAllowance = stop.StopDeadlineNanoseconds - stop.StopAcceptedAtNanoseconds; }
        }
        catch (OverflowException)
        {
            refusal = "durable stop deadline arithmetic overflowed";
            return false;
        }

        if (declaredAllowance != OwnershipSnapshotCodec.StopConfirmationAllowanceNanoseconds)
        {
            refusal = "durable stop deadline does not match the accepted allowance";
            return false;
        }

        var basis = _clock.Basis;
        if (basis.Epoch != MonotonicEpoch.Qpc || basis.Frequency != Stopwatch.Frequency)
        {
            refusal = "monotonic clock epoch/frequency is incompatible with the durable stop timestamp basis";
            return false;
        }

        long now;
        try
        {
            now = _clock.NowNanoseconds;
        }
        catch (Exception)
        {
            refusal = "monotonic clock is unavailable";
            return false;
        }

        if (now <= 0)
        {
            refusal = "monotonic clock timestamp is invalid";
            return false;
        }

        long remaining;
        try
        {
            checked { remaining = stop.StopDeadlineNanoseconds - now; }
        }
        catch (OverflowException)
        {
            refusal = "durable stop remaining-time arithmetic overflowed";
            return false;
        }

        budgetNanoseconds = remaining > 0 ? remaining : 0;
        return true;
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
        var journalProgressUncertain = false;
        var journalProgressDetail = string.Empty;

        bool TryPersist(byte newBits, OwnershipCleanupState cleanup, out string error)
        {
            error = string.Empty;
            if (journal is null || writer is null)
            {
                return false;
            }

            var latest = journal.ReadLatest();
            if (latest.Ok && latest.Snapshot is not null
                && latest.Snapshot.Cleanup == cleanup
                && (latest.Snapshot.CompletedCleanup | newBits) == latest.Snapshot.CompletedCleanup)
            {
                return true;
            }

            JournalIoResult persisted;
            if (!latest.Ok && latest.Error == "no durable snapshot exists")
            {
                persisted = journal.WriteNext(writer, record with
                {
                    CompletedCleanup = newBits,
                    Cleanup = cleanup,
                });
            }
            else if (!latest.Ok)
            {
                persisted = JournalIoResult.Failure($"cleanup baseline is uncertain: {latest.Error}");
            }
            else
            {
                persisted = journal.WriteMutation(writer, current => current with
                {
                    CompletedCleanup = (byte)(current.CompletedCleanup | newBits),
                    Cleanup = cleanup,
                });
            }

            if (!persisted.Ok)
            {
                error = persisted.Error;
                return false;
            }

            return true;
        }

        void Note(CleanupComponents component, ComponentState state, string detail) =>
            outcomes.Add(new ComponentOutcome(component, state, detail));

        void NoteProgressFailure(string stage, string error)
        {
            journalProgressUncertain = true;
            journalProgressDetail = $"{stage} progress persistence failed: {error}";
        }

        // C3: when the durable stop is still outstanding, the persisted
        // absolute deadline is the only authoritative bound. Resolve it
        // before any destructive action; an unverifiable clock or deadline
        // refuses rather than fabricating a fresh allowance.
        var stopBudgetNanoseconds = 0L;
        if ((bits & (byte)CleanupComponents.DomainStop) == 0)
        {
            if (!TryResolveStopBudget(record.Stop, out stopBudgetNanoseconds, out var budgetRefusal))
            {
                return new ReconcileResult(
                    ReconcileOutcome.QuarantinedUnknown, budgetRefusal, DestructiveActionTaken: false);
            }
        }

        // DomainStop (+DomainEmpty established by the same bounded wait).
        var stopState = ComponentState.Pending;
        var kill = KillDispatchOutcome.NotAttempted;
        var population = StopPopulationObservation.NotObserved;
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
#if GAGAMBA_WP05_D6_4_TEST_FIXTURE
            var stop = _notAttemptedStopFixture is null
                ? liveLease.StopAndAwaitEmpty(stopBudgetNanoseconds, _clock)
                : _notAttemptedStopFixture.CreateOutcome();
#else
            var stop = liveLease.StopAndAwaitEmpty(stopBudgetNanoseconds, _clock);
#endif
            kill = stop.Kill;
            population = stop.Population;

            switch (stop.Kill)
            {
                case KillDispatchOutcome.Accepted when stop.Population == StopPopulationObservation.Empty:
                    bits |= (byte)(CleanupComponents.DomainStop | CleanupComponents.DomainEmpty);
                    stopState = ComponentState.Confirmed;
                    Note(CleanupComponents.DomainStop, stopState, "stopped and observed empty");
                    if (!TryPersist(bits, record.Cleanup, out var persistError))
                    {
                        NoteProgressFailure("stop", persistError);
                    }

                    break;
                case KillDispatchOutcome.Failed:
                    // Definitive kill operation failure. A later empty
                    // observation may confirm emptiness but cannot erase it.
                    stopState = ComponentState.Failed;
                    Note(CleanupComponents.DomainStop, stopState, stop.Error);
                    break;
                case KillDispatchOutcome.NotAttempted:
                    stopState = ComponentState.Unknown;
                    Note(CleanupComponents.DomainStop, stopState,
                        $"kill dispatch not attempted: {stop.Error}");
                    break;
                default:
                    stopState = ComponentState.Unknown;
                    Note(CleanupComponents.DomainStop, stopState,
                        $"kill dispatch unproven: {stop.Error}");
                    break;
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
        else if (kill is KillDispatchOutcome.Accepted or KillDispatchOutcome.Unknown)
        {
            // An accepted kill that left the domain populated (including the
            // zero-wait Policy A path) or an unproven dispatch is uncertainty,
            // never a second wait and never fabricated emptiness.
            emptyState = ComponentState.Unknown;
            Note(CleanupComponents.DomainEmpty, emptyState,
                kill == KillDispatchOutcome.Accepted
                    ? "kill accepted but domain emptiness unconfirmed"
                    : "kill dispatch unproven; population not adopted");
        }
        else
        {
            // Kill failed or was not attempted. A trusted, authorized fallback
            // observation may independently confirm emptiness; it never
            // implies a successful kill.
            var fallback = liveLease!.ObservePopulation();
            if (fallback.Known && fallback.Empty)
            {
                bits |= (byte)CleanupComponents.DomainEmpty;
                emptyState = ComponentState.Confirmed;
                Note(CleanupComponents.DomainEmpty, emptyState,
                    "independently observed empty without a confirmed kill dispatch");
                if (!TryPersist(bits, record.Cleanup, out var persistError))
                {
                    NoteProgressFailure("empty fallback", persistError);
                }
            }
            else if (fallback.Known)
            {
                emptyState = ComponentState.Unknown;
                Note(CleanupComponents.DomainEmpty, emptyState,
                    "fallback observation still populated");
            }
            else
            {
                emptyState = ComponentState.Unknown;
                Note(CleanupComponents.DomainEmpty, emptyState, fallback.Error);
            }
        }

        // Staging: every recorded staging component must contribute to the
        // verdict. Absence of confirmed ownership is not evidence of absence:
        // an unconfirmed StagingDirectory entry must never be filtered out
        // (G2-08). Components are resolved only through live authority (the
        // identity-checked cleaner, which refuses destructive removal
        // without a matching identity) or genuine absence; a missing cleaner
        // is debt, never success. Domain-kind entries are covered by domain
        // removal, not the staging cleaner.
        var stagingState = ComponentState.Pending;
        var stagingObjects = record.Allocations.Where(a => a.Kind == AllocationKind.Staging).ToArray();
        var stagingDirs = record.Allocations.Where(a => a.Kind == AllocationKind.StagingDirectory).ToArray();
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
            // already resolved; any unresolved object debt keeps its
            // directory.
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
                    NoteProgressFailure("staging", persistError);
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
                    NoteProgressFailure("removal", persistError);
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

        // JournalFinalization + truthful aggregation. Operation failures and
        // persistence uncertainty stay separate: a known component failure is
        // never hidden, and persistence uncertainty alone never becomes an
        // invented operation failure or a fabricated confirmation.
        var finalizationState = ComponentState.Pending;
        var required = new[] { stopState, emptyState, stagingState, removalState };
        var componentFailed = required.Any(s => s == ComponentState.Failed);
        var componentUnknown = required.Any(s => s == ComponentState.Unknown);

        OwnershipCleanupState verdict;
        string componentDetail;
        if (componentFailed)
        {
            verdict = OwnershipCleanupState.Failed;
            componentDetail = "one or more required cleanup components failed";
        }
        else if (componentUnknown || journalProgressUncertain)
        {
            verdict = OwnershipCleanupState.Unknown;
            componentDetail = journalProgressUncertain
                ? journalProgressDetail
                : "required cleanup state cannot be established";
        }
        else
        {
            verdict = OwnershipCleanupState.Confirmed;
            componentDetail = "all required cleanup components confirmed";
        }

        ReconcileOutcome outcome;
        string detail;
        if (journal is null || writer is null)
        {
            finalizationState = ComponentState.Unknown;
            Note(CleanupComponents.JournalFinalization, finalizationState,
                "persistence uncertainty: no writer lease supplied");
            outcome = componentFailed ? ReconcileOutcome.Failed : ReconcileOutcome.QuarantinedUnknown;
            detail = componentFailed
                ? $"{componentDetail}; durable finalization unavailable"
                : "operations concluded but persistence unavailable; explicit Unknown";
        }
        else if (TryPersist(bits, verdict, out var finalError))
        {
            if (journalProgressUncertain)
            {
                finalizationState = ComponentState.Unknown;
                Note(CleanupComponents.JournalFinalization, finalizationState,
                    $"persistence uncertain: {journalProgressDetail}");
                outcome = componentFailed ? ReconcileOutcome.Failed : ReconcileOutcome.QuarantinedUnknown;
                detail = componentFailed
                    ? $"{componentDetail}; {journalProgressDetail}"
                    : journalProgressDetail;
            }
            else
            {
                finalizationState = ComponentState.Confirmed;
                Note(CleanupComponents.JournalFinalization, finalizationState, "progress persisted");
                outcome = verdict switch
                {
                    OwnershipCleanupState.Failed => ReconcileOutcome.Failed,
                    OwnershipCleanupState.Confirmed => ReconcileOutcome.CleanupConfirmed,
                    _ => ReconcileOutcome.QuarantinedUnknown,
                };
                detail = componentDetail;
            }
        }
        else
        {
            finalizationState = ComponentState.Unknown;
            Note(CleanupComponents.JournalFinalization, finalizationState,
                $"persistence failed: {finalError}");
            outcome = componentFailed ? ReconcileOutcome.Failed : ReconcileOutcome.QuarantinedUnknown;
            detail = componentFailed
                ? $"{componentDetail}; final journal write failed: {finalError}"
                : $"cleanup persistence failed without an operation failure: {finalError}";
        }

        return new ReconcileResult(outcome, detail, destructive, outcomes);
    }
}

#if GAGAMBA_WP05_D6_4_TEST_FIXTURE
/// <summary>D6.4 fixed test-only fixture. It accepts no caller-supplied
/// result, delegate or payload and can only express a kill that was never
/// attempted with no population observation. Everything else in the shared
/// Reconciler path remains production source.</summary>
internal sealed class NotAttemptedStopTestFixture
{
    internal StopOutcome CreateOutcome() => new(
        KillDispatchOutcome.NotAttempted,
        StopPopulationObservation.NotObserved,
        TimedOut: false,
        Error: "D6.4 test fixture");
}
#endif
