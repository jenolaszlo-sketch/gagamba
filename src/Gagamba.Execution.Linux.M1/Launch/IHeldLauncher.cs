using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Launch;

/// <summary>WP04 exposes only prepare-held and abort. There is deliberately
/// no release operation, release writer, or target-launch method.</summary>
internal interface IHeldLauncher
{
    Task<HeldLaunchOutcome> PrepareHeldAsync(
        DomainLease domain,
        PreparedClosure closure,
        RestrictedManifest manifest,
        PreparationIdentity identity,
        long setupDeadlineNs,
        long overallDeadlineNs,
        CancellationToken cancellationToken);

    Task<HeldAbortOutcome> AbortAsync(
        HeldExecution execution,
        DomainLease domain,
        CancellationToken cancellationToken);
}

internal sealed record HeldLaunchOutcome(
    bool Held,
    HeldExecution? Execution,
    byte[]? CoordinatorStatusFrame,
    CoordinatorHeldReport? Report,
    string Error,
    bool NoTargetInitializationMarkerAbsent = false);

internal sealed record HeldAbortOutcome(bool Stopped, bool WrapperReaped, string Error,
    bool NoTargetInitializationMarkerAbsent = false);
