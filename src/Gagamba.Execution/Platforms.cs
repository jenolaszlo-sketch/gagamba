// GP-2 per-platform capability matrix. Every cell cites the leg evidence
// that earns it; see docs/contracts/execution-domain.md for the full table.
// Levels follow the weakest demonstrated behavior, never the hoped-for one.
namespace Gagamba.Execution;

    /// <summary>
    /// Well-known platform profiles. Member names stay neutral on purpose
    /// (no Job/Cgroup/Launchd concepts in the contract surface); the
    /// Platform STRINGS below are evidence pointers ("which backend row"),
    /// not mechanism exposure. See the opacity test.
    /// </summary>
    public static class WellKnownPlatforms
    {
    private static CapabilityGrant FullNative(string evidence) =>
        new(CapabilityLevel.Full, GuaranteeKind.Native, evidence);

    private static CapabilityGrant PartialNative(string evidence) =>
        new(CapabilityLevel.Partial, GuaranteeKind.Native, evidence);

    private static CapabilityGrant PartialConstructed(string evidence) =>
        new(CapabilityLevel.Partial, GuaranteeKind.Constructed, evidence);

    private static CapabilityGrant Absent(string evidence) =>
        new(CapabilityLevel.Absent, GuaranteeKind.None, evidence);

    /// <summary>Windows Job Objects (GQ-1: L5-JOB-OWNERSHIP, all 8 phases).</summary>
    public static PlatformCapabilities Windows { get; } = new(
        "windows-job",
        new Dictionary<ExecutionCapability, CapabilityGrant>
        {
            [ExecutionCapability.UnitTermination] =
                FullNative("J1/J2: TerminateJobObject kills root and depth tree"),
            [ExecutionCapability.SurvivesRootExit] =
                FullNative("J3/J5: close/dispose kills survivors; job owns post-root tree"),
            [ExecutionCapability.OwnerDeathCleanup] =
                FullNative("J6: crashed supervisor leaves none (kernel close-handle kill)"),
            [ExecutionCapability.RecursiveMembership] =
                FullNative("J2: root->mid->ping all in job, all killed"),
            [ExecutionCapability.EscapeResistant] =
                FullNative("Direct descendants inherit the job; normal breakaway flags are disabled. Broker-mediated creation (WMI/services/scheduled work) is outside this guarantee"),
            [ExecutionCapability.KernelOwnedLifecycle] =
                FullNative("J3-J6: lifetime enforced by the kernel, no userspace reaper"),
        });

    /// <summary>Linux cgroup v2 (GL-1A: L1-L14).</summary>
    public static PlatformCapabilities Linux { get; } = new(
        "linux-cgroup-v2",
        new Dictionary<ExecutionCapability, CapabilityGrant>
        {
            [ExecutionCapability.UnitTermination] =
                FullNative("L11: cgroup.kill kills the tree; L12 recursively incl. nested"),
            [ExecutionCapability.SurvivesRootExit] =
                FullNative("L10: descendants remain owned and in-cgroup after root exit"),
            [ExecutionCapability.OwnerDeathCleanup] =
                Absent("L13: workload survives supervisor death; membership is not ownership"),
            [ExecutionCapability.RecursiveMembership] =
                FullNative("L9/L12: descendants inherit; nested cgroups killed recursively"),
            [ExecutionCapability.EscapeResistant] =
                PartialNative("Membership/inheritance strong (L9/L12); cgroup.procs-write escape path untested — depends on fs exposure, not claimed"),
            [ExecutionCapability.KernelOwnedLifecycle] =
                Absent("Kill itself is kernel-backed, but nothing automatic fires on owner death (L13)"),
        });

    /// <summary>macOS launchd + process groups (GM-1A: M1-M12, arm64).</summary>
    public static PlatformCapabilities MacOs { get; } = new(
        "macos-launchd-pg",
        new Dictionary<ExecutionCapability, CapabilityGrant>
        {
            [ExecutionCapability.UnitTermination] =
                PartialNative("PG-scoped killpg (M3); launchd cleans same-PG on job death (M7). No subtree primitive"),
            [ExecutionCapability.SurvivesRootExit] =
                FullNative("M2: descendants remain addressable in PG after root exit"),
            [ExecutionCapability.OwnerDeathCleanup] =
                Absent("M10: job outlives its client; M4: PG workload survives supervisor death"),
            [ExecutionCapability.RecursiveMembership] =
                PartialNative("Children inherit PG (M1); groups do not nest hierarchically"),
            [ExecutionCapability.EscapeResistant] =
                Absent("M5/M8/M12: setsid escape survives group kill and launchd cleanup"),
            [ExecutionCapability.KernelOwnedLifecycle] =
                Absent("No automatic owner-death action; launchd cleanup is PG-scoped, not owned"),
        });

    public static IReadOnlyList<PlatformCapabilities> All { get; } =
        new[] { Windows, Linux, MacOs };

    /// <summary>
    /// Composed guarantees Gagamba itself can provide, keyed by platform
    /// name. Each value is an overlay: capability to the composed grant.
    /// Reported as Constructed, never equated with Native.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<ExecutionCapability, CapabilityGrant>>
        ComposedOwnerDeath { get; } =
        new Dictionary<string, IReadOnlyDictionary<ExecutionCapability, CapabilityGrant>>
        {
            ["linux-cgroup-v2"] = new Dictionary<ExecutionCapability, CapabilityGrant>
            {
                [ExecutionCapability.OwnerDeathCleanup] = PartialConstructed(
                    "L14: pdeathsig watchdog + cgroup.kill (strong recursive primitive underneath)"),
            },
            ["macos-launchd-pg"] = new Dictionary<ExecutionCapability, CapabilityGrant>
            {
                [ExecutionCapability.OwnerDeathCleanup] = PartialConstructed(
                    "M11: pipe-EOF watchdog + killpg (PG-only underneath; escapees survive per M12)"),
            },
        };
}
