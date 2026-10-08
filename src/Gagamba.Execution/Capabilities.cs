// GP-2 execution-domain contract: capabilities a platform can guarantee,
// derived from GW-1B/GQ-1 (Windows), GL-1A (Linux), GM-1A (macOS) evidence.
// Deliberately absent: provider primitives (JobObject, cgroup, launchd),
// any backend implementation, and any boolean shaped like IsSandboxed.
namespace Gagamba.Execution;

/// <summary>
/// Lifecycle/containment capabilities, one per evidenced question. Names
/// follow the experiment legs, not any single OS API.
/// </summary>
public enum ExecutionCapability
{
    /// <summary>Terminate the workload as a unit without PID enumeration.</summary>
    UnitTermination,
    /// <summary>Descendants stay correctly owned after the original root exits.</summary>
    SurvivesRootExit,
    /// <summary>The workload dies if the Gagamba owner/supervisor disappears.</summary>
    OwnerDeathCleanup,
    /// <summary>Descendants are automatically included in the owned domain.</summary>
    RecursiveMembership,
    /// <summary>An ordinarily created direct descendant cannot break away
    /// from its lifecycle domain. Work created through an external broker
    /// (such as WMI or a service) is outside this capability.</summary>
    EscapeResistant,
    /// <summary>The OS itself enforces the full lifecycle relationship.</summary>
    KernelOwnedLifecycle,
}

/// <summary>How much of a capability holds. Partial always names its bound.</summary>
public enum CapabilityLevel
{
    Absent,
    Partial,
    Full,
}

/// <summary>How a Full/Partial grant is achieved. None for Absent.</summary>
public enum GuaranteeKind
{
    None,
    /// <summary>Intrinsic to the OS primitive (close-handle kill, cgroup.kill).</summary>
    Native,
    /// <summary>Composed by Gagamba (watchdog + kill primitive). Weaker by
    /// construction; never reported as identical to Native.</summary>
    Constructed,
}

/// <summary>One matrix cell: level, kind, and the evidence that earns it.</summary>
public sealed record CapabilityGrant(
    CapabilityLevel Level,
    GuaranteeKind Kind,
    string Evidence)
{
    public bool Satisfies(CapabilityLevel minimum, bool allowConstructed) =>
        Level >= minimum
        && Level != CapabilityLevel.Absent
        && (Kind == GuaranteeKind.Native || (allowConstructed && Kind == GuaranteeKind.Constructed));
}

/// <summary>What one platform guarantees, with receipts.</summary>
public sealed record PlatformCapabilities(
    string Platform,
    IReadOnlyDictionary<ExecutionCapability, CapabilityGrant> Grants)
{
    public bool TryGet(ExecutionCapability capability, out CapabilityGrant grant) =>
        Grants.TryGetValue(capability, out grant!);
}
