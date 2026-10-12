// WP03 restricted-domain abstraction. The domain is a delegated cgroup v2
// child. Authority is live retained handles plus captured identity — never
// a path re-resolved later, never a journal PID. Raw IO goes through
// IDomainBackend so the same lease/stop/removal logic runs against the
// real cgroup filesystem natively and an in-memory model portably.
namespace Gagamba.Execution.Linux.M1.Lifecycle;

internal readonly record struct DomainIoResult(bool Ok, string Error)
{
    internal static DomainIoResult Success { get; } = new(true, string.Empty);

    internal static DomainIoResult Failure(string error) => new(false, error);
}

internal readonly record struct DomainPopulationResult(bool Known, bool Empty, string Error)
{
    internal static DomainPopulationResult Populated { get; } = new(true, false, string.Empty);

    internal static DomainPopulationResult Emptied { get; } = new(true, true, string.Empty);

    internal static DomainPopulationResult Unknown(string error) => new(false, false, error);
}

internal readonly record struct DomainStatResult(bool Ok, ulong Device, ulong Inode, uint Mode, string Error)
{
    internal static DomainStatResult Failure(string error) => new(false, 0, 0, 0, error);
}

internal readonly record struct DomainLookupResult(bool Ok, ulong Device, ulong Inode, uint Mode, string Error)
{
    internal static DomainLookupResult Failure(string error) => new(false, 0, 0, 0, error);
}

/// <summary>Raw operations over a set of retained domain handles. The
/// native implementation wraps real descriptors; the portable fake models
/// an in-memory cgroup with the same observable contract.</summary>
internal interface IDomainBackend : IDisposable
{
    /// <summary>True while this backend still holds its live authority
    /// handles. Once disposed or lost, records alone are non-authority.</summary>
    bool HandlesAlive { get; }

    DomainIoResult WriteKill();

    DomainPopulationResult ReadPopulation();

    DomainStatResult StatDomain();

    DomainStatResult StatParent();

    /// <summary>Beneath-only, no-follow lookup of the named child under the
    /// retained parent. Used to detect replacement before removal.</summary>
    DomainLookupResult LookupChild(string name);

    DomainIoResult RemoveChild(string name);

    /// <summary>An independent set of handles for the same domain, held by
    /// the recovery scope. Disposing one backend must not affect the other.</summary>
    IDomainBackend DuplicateForRecovery();
}

/// <summary>Stable domain identity: Section 10.2 fields plus the live
/// handles held by the backend. The identity is corroborating evidence;
/// only the live handles are authority.</summary>
internal sealed record DomainIdentity(
    Guid ExecutionId,
    string BootId,
    ulong CgroupMountId,
    string CgroupMountFsType,
    ulong ParentDevice,
    ulong ParentInode,
    string ChildName,
    ulong ChildDevice,
    ulong ChildInode,
    Guid LeaseGeneration)
{
    /// <summary>True when every security-relevant identity component
    /// agrees (all record fields; live handles are separate). Changing any
    /// single component — including child device/inode — yields false.</summary>
    internal bool Corroborates(DomainIdentity other) =>
        ExecutionId.Equals(other.ExecutionId)
        && string.Equals(BootId, other.BootId, StringComparison.Ordinal)
        && CgroupMountId == other.CgroupMountId
        && string.Equals(CgroupMountFsType, other.CgroupMountFsType, StringComparison.Ordinal)
        && ParentDevice == other.ParentDevice
        && ParentInode == other.ParentInode
        && string.Equals(ChildName, other.ChildName, StringComparison.Ordinal)
        && ChildDevice == other.ChildDevice
        && ChildInode == other.ChildInode
        && LeaseGeneration.Equals(other.LeaseGeneration);

    /// <summary>Detects a same-name replacement: the record fields agree
    /// except the child dev/inode differ.</summary>
    internal bool SameNameButReplaced(DomainIdentity other) =>
        string.Equals(ChildName, other.ChildName, StringComparison.Ordinal)
        && (ChildDevice != other.ChildDevice || ChildInode != other.ChildInode);
}
