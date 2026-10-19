// Test-only structured termination evidence for the WP05 forged-abort oracle.
// This preserves the kernel's normal-exit versus signal distinction instead of
// reducing an observation to an ambiguous integer.
namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

internal enum ObservedTerminationKind
{
    Unknown = 0,
    Exited = 1,
    Killed = 2,
    Dumped = 3,
}

internal enum TerminationObservationSource
{
    None = 0,
    PidFdWaitIdWnowait = 1,
}

internal readonly record struct OwnedWrapperIdentity(
    Guid ExecutionId,
    Guid GenerationId,
    Guid PreparationId,
    int PidFd,
    int Pid)
{
    internal bool IsUsable =>
        ExecutionId != Guid.Empty &&
        GenerationId != Guid.Empty &&
        PreparationId != Guid.Empty &&
        PidFd >= 0 &&
        Pid > 0;
}

internal readonly record struct ObservedTermination(
    OwnedWrapperIdentity Identity,
    ObservedTerminationKind Kind,
    int RawCode,
    int? ExitStatus,
    int? Signal,
    TerminationObservationSource Source,
    string Error)
{
    internal bool IsUsable =>
        Identity.IsUsable &&
        Source == TerminationObservationSource.PidFdWaitIdWnowait &&
        string.IsNullOrEmpty(Error);
}

internal readonly record struct AuthorizedStopEvidence(
    OwnedWrapperIdentity Identity,
    bool Stopped,
    bool WrapperReaped,
    string Error,
    bool NoTargetInitializationMarkerAbsent);
