// WP03 owner bootstrap: the owner accepts only inherited private
// descriptors with an exact versioned role/count allowlist. No release
// authority is ever transferred; no named listener, filesystem socket,
// reconnect API or serialized bearer token exists. Where descriptors must
// cross after owner creation, one inherited Unix socketpair is used
// solely for SCM_RIGHTS with fixed roles/count and CLOEXEC-on-receive.
namespace Gagamba.Execution.Linux.M1.Native;

internal enum BootstrapRole : byte
{
    ControlRead = 0,
    ControlWrite = 1,
    JournalLock = 2,
    DomainKill = 3,
    DomainEvents = 4,
    RecoveryEvidence = 5,
    HandleChannel = 6,
}

internal sealed record BootstrapDescriptor(int Fd, BootstrapRole Role);

internal sealed record BootstrapMap(ushort Version, IReadOnlyList<BootstrapDescriptor> Descriptors);

internal static class OwnerBootstrap
{
    internal const ushort CurrentVersion = 1;

    /// <summary>Roles a valid WP03 owner bootstrap must contain, exactly
    /// once each, and nothing else. The owner receives its primary
    /// DomainLease handles over one inherited Unix socketpair
    /// (<see cref="BootstrapRole.HandleChannel"/>); no release authority is
    /// ever transferred.</summary>
    internal static readonly BootstrapRole[] RequiredRoles =
    {
        BootstrapRole.ControlRead,
        BootstrapRole.ControlWrite,
        BootstrapRole.RecoveryEvidence,
        BootstrapRole.HandleChannel,
    };

    internal static bool TryValidate(BootstrapMap? map, out string error)
    {
        error = string.Empty;
        if (map is null)
        {
            error = "missing bootstrap map";
            return false;
        }

        if (map.Version != CurrentVersion)
        {
            error = $"unsupported bootstrap version {map.Version}";
            return false;
        }

        if (map.Descriptors.Count != RequiredRoles.Length)
        {
            error = $"bootstrap descriptor count {map.Descriptors.Count} != {RequiredRoles.Length}";
            return false;
        }

        var seen = new HashSet<BootstrapRole>();
        foreach (var descriptor in map.Descriptors)
        {
            if (descriptor.Fd < 0)
            {
                error = $"bootstrap descriptor for {descriptor.Role} is invalid";
                return false;
            }

            if (!RequiredRoles.Contains(descriptor.Role))
            {
                error = $"unexpected bootstrap role {descriptor.Role}";
                return false;
            }

            if (!seen.Add(descriptor.Role))
            {
                error = $"duplicate bootstrap role {descriptor.Role}";
                return false;
            }
        }

        foreach (var role in RequiredRoles)
        {
            if (!seen.Contains(role))
            {
                error = $"missing bootstrap role {role}";
                return false;
            }
        }

        return true;
    }
}
