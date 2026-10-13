// WP03 narrow cgroup v2 interop: derive the real cgroup2 mount identity
// from retained descriptors, create a delegated child domain under a
// retained parent, and validate every primary handle's role, membership
// and access mode before a lease is accepted. Every primitive is
// descriptor-relative with beneath-only, no-follow resolution. The
// resulting backend owns its own descriptors; records are never
// authority. Non-Linux callers fail closed with Unsupported.
using System.Globalization;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Native;

internal sealed record CgroupMountInfo(ulong MountId, string FileSystemType, string MountPoint);

internal sealed record CgroupBootInfo(string BootId);

internal enum ControlAccess : byte
{
    Read = 0,
    Write = 1,
    ReadWrite = 2,
}

/// <summary>Native IDomainBackend over real cgroup descriptors. It owns the
/// parent descriptor plus the domain directory and control descriptors;
/// each descriptor has exactly one owning backend. Dispose closes every
/// owned descriptor exactly once.</summary>
internal sealed class CgroupBackend : IDomainBackend
{
    private readonly int _parentFd;
    private readonly int _domainFd;
    private readonly int _killFd;
    private readonly int _eventsFd;
    private readonly Dictionary<string, int> _readbackFds;
    private bool _disposed;

    internal CgroupBackend(
        int parentFd,
        int domainFd,
        int killFd,
        int eventsFd,
        Dictionary<string, int> readbackFds)
    {
        _parentFd = parentFd;
        _domainFd = domainFd;
        _killFd = killFd;
        _eventsFd = eventsFd;
        _readbackFds = readbackFds;
    }

    internal IReadOnlyDictionary<string, int> ReadbackFds => _readbackFds;

    /// <summary>Primary handles in the fixed role order for SCM_RIGHTS
    /// transfer to the owner: parent, domain dir, kill, events,
    /// memory.max, memory.swap.max, pids.max.</summary>
    internal int[] OrderedFds()
    {
        var list = new List<int> { _parentFd, _domainFd, _killFd, _eventsFd };
        foreach (var role in new[] { "memory.max", "memory.swap.max", "pids.max" })
        {
            list.Add(_readbackFds[role]);
        }

        return list.ToArray();
    }

    public bool HandlesAlive => !_disposed && _domainFd >= 0 && _killFd >= 0 && _eventsFd >= 0;

    public DomainIoResult WriteKill()
    {
        if (_disposed)
        {
            return DomainIoResult.Failure("backend disposed");
        }

        var payload = new byte[] { (byte)'1' };
        var errno = PreparationInterop.WriteAll(_killFd, payload, 0, 1);
        return errno == 0 ? DomainIoResult.Success : DomainIoResult.Failure($"cgroup.kill write errno {errno}");
    }

    public DomainPopulationResult ReadPopulation()
    {
        if (_disposed)
        {
            return DomainPopulationResult.Unknown("backend disposed");
        }

        var text = ReadAllFd(_eventsFd);
        if (text is null)
        {
            return DomainPopulationResult.Unknown("cgroup.events read failed");
        }

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed == "populated 0")
            {
                return DomainPopulationResult.Emptied;
            }

            if (trimmed == "populated 1")
            {
                return DomainPopulationResult.Populated;
            }
        }

        return DomainPopulationResult.Unknown("cgroup.events lacks a populated line");
    }

    public DomainStatResult StatDomain()
    {
        if (_disposed)
        {
            return DomainStatResult.Failure("backend disposed");
        }

        return PreparationInterop.StatFd(_domainFd, out var identity) == 0
            ? new DomainStatResult(true, identity.Device, identity.Inode, identity.Mode, string.Empty)
            : DomainStatResult.Failure("domain fstat failed");
    }

    public DomainStatResult StatParent()
    {
        if (_disposed)
        {
            return DomainStatResult.Failure("backend disposed");
        }

        return PreparationInterop.StatFd(_parentFd, out var identity) == 0
            ? new DomainStatResult(true, identity.Device, identity.Inode, identity.Mode, string.Empty)
            : DomainStatResult.Failure("parent fstat failed");
    }

    public DomainLookupResult LookupChild(string name)
    {
        if (_disposed)
        {
            return DomainLookupResult.Failure("backend disposed");
        }

        var errno = PreparationInterop.OpenRelative(
            _parentFd, name, PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY
                | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
            PreparationInterop.RequiredResolve, out var fd);
        if (errno != 0)
        {
            return DomainLookupResult.Failure($"child lookup errno {errno}");
        }

        try
        {
            if (PreparationInterop.StatFd(fd, out var identity) != 0)
            {
                return DomainLookupResult.Failure("child fstat failed");
            }

            return new DomainLookupResult(true, identity.Device, identity.Inode, identity.Mode, string.Empty);
        }
        finally
        {
            PreparationInterop.CloseFd(fd);
        }
    }

    public DomainIoResult RemoveChild(string name)
    {
        if (_disposed)
        {
            return DomainIoResult.Failure("backend disposed");
        }

        var errno = PreparationInterop.RemoveDirectory(_parentFd, name);
        return errno == 0 ? DomainIoResult.Success : DomainIoResult.Failure($"rmdir errno {errno}");
    }

    public IDomainBackend DuplicateForRecovery()
    {
        if (_disposed)
        {
            throw new InvalidOperationException("cannot duplicate a disposed backend.");
        }

        var parent = PreparationInterop.Dup(_parentFd);
        var domain = PreparationInterop.Dup(_domainFd);
        var kill = PreparationInterop.Dup(_killFd);
        var events = PreparationInterop.Dup(_eventsFd);
        var readbacks = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in _readbackFds)
        {
            readbacks[pair.Key] = PreparationInterop.Dup(pair.Value);
        }

        return new CgroupBackend(parent, domain, kill, events, readbacks);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Close(_domainFd);
        Close(_killFd);
        Close(_eventsFd);
        foreach (var fd in _readbackFds.Values)
        {
            Close(fd);
        }

        Close(_parentFd);
        GC.SuppressFinalize(this);
    }

    private static void Close(int fd)
    {
        if (fd >= 0)
        {
            PreparationInterop.CloseFd(fd);
        }
    }

    private static string? ReadAllFd(int fd)
    {
        var buffer = new List<byte>(256);
        var chunk = new byte[256];
        while (true)
        {
            var errno = PreparationInterop.ReadChunk(fd, chunk, 0, chunk.Length, out var read);
            if (errno != 0)
            {
                return null;
            }

            if (read == 0)
            {
                break;
            }

            for (var i = 0; i < read; i++)
            {
                buffer.Add(chunk[i]);
            }

            if (buffer.Count > 4096)
            {
                break;
            }
        }

        PreparationInterop.SeekStart(fd);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
}

internal static class CgroupInterop
{
    internal static bool IsLinux => PreparationInterop.IsLinux;

    internal static readonly string[] ControlNames = { "cgroup.kill", "cgroup.events", "memory.max", "memory.swap.max", "pids.max" };

    /// <summary>Parses cgroup2 mount info from /proc/self/mountinfo. Used
    /// only for informational/expected comparison; lease identity derives
    /// its mount ID from the actual retained descriptors.</summary>
    internal static bool TryReadCgroupMount(out CgroupMountInfo? info, out string error)
    {
        info = null;
        error = string.Empty;
        if (!IsLinux)
        {
            error = "not Linux";
            return false;
        }

        try
        {
            foreach (var line in File.ReadLines("/proc/self/mountinfo"))
            {
                var dash = line.IndexOf(" - ", StringComparison.Ordinal);
                if (dash < 0)
                {
                    continue;
                }

                var left = line[..dash].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var right = line[(dash + 3)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (right.Length < 2 || right[0] != "cgroup2")
                {
                    continue;
                }

                if (!ulong.TryParse(left[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var mountId))
                {
                    continue;
                }

                var mountPoint = left.Length >= 5 ? left[4] : string.Empty;
                info = new CgroupMountInfo(mountId, "cgroup2", mountPoint);
                return true;
            }

            error = "no cgroup2 mount found";
            return false;
        }
        catch (Exception ex)
        {
            error = $"mountinfo read failed: {ex.GetType().Name}";
            return false;
        }
    }

    internal static bool TryReadBootId(out string bootId, out string error)
    {
        bootId = string.Empty;
        error = string.Empty;
        if (!IsLinux)
        {
            error = "not Linux";
            return false;
        }

        try
        {
            bootId = File.ReadAllText("/proc/sys/kernel/random/boot_id").Trim();
            if (bootId.Length == 0)
            {
                error = "boot id empty";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"boot id read failed: {ex.GetType().Name}";
            return false;
        }
    }

    /// <summary>Derives mount identity from a retained descriptor: the
    /// actual cgroup2 mount ID via statx(STATX_MNT_ID) and filesystem type
    /// via fstatfs. Fails closed if the primitive is unavailable.</summary>
    internal static bool TryDeriveMount(int fd, out ulong mountId, out string fsType, out string error)
    {
        mountId = 0;
        fsType = string.Empty;
        error = string.Empty;
        if (!PreparationInterop.TryStatxMountId(fd, out mountId, out var statxError))
        {
            error = statxError;
            return false;
        }

        if (!PreparationInterop.TryFsType(fd, out var magic, out var fsError))
        {
            error = fsError;
            return false;
        }

        if (magic != PreparationInterop.CGROUP2_SUPER_MAGIC)
        {
            error = $"descriptor is not on cgroup2 (f_type 0x{magic:X})";
            return false;
        }

        fsType = "cgroup2";
        return true;
    }

    /// <summary>Reads a cgroup control file relative to a retained directory
    /// descriptor (bounded, no path re-resolution).</summary>
    internal static bool TryReadControlText(int dirFd, string name, out string text, out string error)
    {
        text = string.Empty;
        error = string.Empty;
        var errno = PreparationInterop.OpenRelative(
            dirFd, name,
            PreparationInterop.O_RDONLY | PreparationInterop.O_CLOEXEC,
            PreparationInterop.RequiredResolve, out var fd);
        if (errno != 0)
        {
            error = $"{name} open errno {errno}";
            return false;
        }

        try
        {
            var buffer = new byte[4096];
            var total = 0;
            while (total < buffer.Length)
            {
                var rc = PreparationInterop.ReadChunk(fd, buffer, total, buffer.Length - total, out var read);
                if (rc != 0)
                {
                    error = $"{name} read errno {rc}";
                    return false;
                }

                if (read == 0)
                {
                    break;
                }

                total += read;
            }

            text = System.Text.Encoding.ASCII.GetString(buffer, 0, total).Trim();
            return true;
        }
        finally
        {
            PreparationInterop.CloseFd(fd);
        }
    }

    /// <summary>Verifies an operator-delegated parent: it is on the cgroup2
    /// filesystem and has already delegated the controllers the M1 domain
    /// requires (memory and pids). Missing controller/delegation support is
    /// an explicit refusal; Gagamba never repairs or provisions it.</summary>
    internal static bool TryVerifyDelegatedParent(int parentFd, out string error)
    {
        error = string.Empty;
        if (!TryDeriveMount(parentFd, out _, out _, out var mountError))
        {
            error = $"delegated parent is not on cgroup2: {mountError}";
            return false;
        }

        if (!TryReadControlText(parentFd, "cgroup.controllers", out var controllers, out var controllersError))
        {
            error = $"delegated parent controllers unreadable: {controllersError}";
            return false;
        }

        if (!TryReadControlText(parentFd, "cgroup.subtree_control", out var subtree, out var subtreeError))
        {
            error = $"delegated parent subtree_control unreadable: {subtreeError}";
            return false;
        }

        var available = Tokens(controllers);
        var delegated = Tokens(subtree);
        foreach (var required in new[] { "memory", "pids" })
        {
            if (!available.Contains(required))
            {
                error = $"delegated parent does not offer controller '{required}'";
                return false;
            }

            if (!delegated.Contains(required))
            {
                error = $"delegated parent has not delegated controller '{required}' to its children";
                return false;
            }
        }

        return true;
    }

    private static HashSet<string> Tokens(string text) =>
        new(text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);

    private static bool TryValidateControl(
        int domainFd, long domainDevice, string controlName, int fd, ControlAccess required, out string error)
    {
        error = string.Empty;
        // Probe the control with an access mode compatible with its role.
        // cgroup.kill is write-only for an unprivileged delegate, so a
        // read-only probe (which only root could satisfy) would falsely
        // reject a valid delegated domain.
        var probeAccess = required switch
        {
            ControlAccess.Read => PreparationInterop.O_RDONLY,
            ControlAccess.Write => PreparationInterop.O_WRONLY,
            _ => PreparationInterop.O_RDWR,
        };
        var openErrno = PreparationInterop.OpenRelative(
            domainFd, controlName,
            probeAccess | PreparationInterop.O_CLOEXEC,
            PreparationInterop.RequiredResolve, out var probe);
        if (openErrno != 0)
        {
            error = $"{controlName} probe open errno {openErrno}";
            return false;
        }

        try
        {
            if (PreparationInterop.StatFd(probe, out var probeIdentity) != 0
                || PreparationInterop.StatFd(fd, out var received) != 0)
            {
                error = $"{controlName} stat failed";
                return false;
            }

            if (probeIdentity.Device != received.Device || probeIdentity.Inode != received.Inode)
            {
                error = $"{controlName} is not the child domain's control";
                return false;
            }

            if (received.Device != (ulong)domainDevice)
            {
                error = $"{controlName} is on a different device than the child domain";
                return false;
            }
        }
        finally
        {
            PreparationInterop.CloseFd(probe);
        }

        var access = PreparationInterop.AccessMode(fd);
        var ok = required switch
        {
            ControlAccess.Read => access is 0 or 2,
            ControlAccess.Write => access is 1 or 2,
            ControlAccess.ReadWrite => access == 2,
            _ => false,
        };
        if (!ok)
        {
            error = $"{controlName} access mode {access} incompatible with required {required}";
            return false;
        }

        return true;
    }

    /// <summary>Validates the seven ordered primary descriptors against
    /// their roles, membership and the expected child locator, and builds
    /// the owning backend plus the derived identity. Any failure leaves the
    /// caller the descriptors to close; a success transfers ownership to
    /// the returned backend.</summary>
    internal static bool TryValidateDomainHandles(
        int[] fds,
        string childName,
        Guid executionId,
        Guid leaseGeneration,
        string bootId,
        out CgroupBackend? backend,
        out DomainIdentity? identity,
        out string error)
    {
        backend = null;
        identity = null;
        error = string.Empty;
        if (fds.Length != DomainHandleTransfer.ExactDescriptorCount)
        {
            error = $"expected {DomainHandleTransfer.ExactDescriptorCount} descriptors, got {fds.Length}";
            return false;
        }

        foreach (var fd in fds)
        {
            if (!PreparationInterop.HasCloexec(fd))
            {
                error = $"descriptor {fd} lacks FD_CLOEXEC";
                return false;
            }
        }

        var parentFd = fds[(int)DomainTransferRole.Parent];
        var domainFd = fds[(int)DomainTransferRole.DomainDirectory];
        var killFd = fds[(int)DomainTransferRole.Kill];
        var eventsFd = fds[(int)DomainTransferRole.Events];

        if (PreparationInterop.StatFd(parentFd, out var parentStat) != 0 || !PreparationInterop.IsDirectory(parentStat.Mode))
        {
            error = "parent descriptor is not a directory";
            return false;
        }

        if (PreparationInterop.StatFd(domainFd, out var domainStat) != 0 || !PreparationInterop.IsDirectory(domainStat.Mode))
        {
            error = "domain descriptor is not a directory";
            return false;
        }

        if (parentStat.Device != domainStat.Device)
        {
            error = "parent and child are on different devices";
            return false;
        }

        if (parentStat.Inode == domainStat.Inode)
        {
            error = "parent and child alias the same object";
            return false;
        }

        // Mount identity derived from the retained parent descriptor.
        if (!TryDeriveMount(parentFd, out var mountId, out var fsType, out var mountError))
        {
            error = $"parent mount derivation failed: {mountError}";
            return false;
        }

        if (!TryDeriveMount(domainFd, out var childMountId, out _, out var childMountError))
        {
            error = $"child mount derivation failed: {childMountError}";
            return false;
        }

        if (childMountId != mountId)
        {
            error = "parent and child are on different cgroup2 mounts";
            return false;
        }

        // Child name is a locator under the retained parent; the retained
        // child identity must match it exactly.
        if (!string.IsNullOrEmpty(childName))
        {
            var locatorErrno = PreparationInterop.OpenRelative(
                parentFd, childName,
                PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY
                    | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
                PreparationInterop.RequiredResolve, out var locator);
            if (locatorErrno != 0)
            {
                error = $"child locator open errno {locatorErrno}";
                return false;
            }

            try
            {
                if (PreparationInterop.StatFd(locator, out var locatorIdentity) != 0
                    || locatorIdentity.Device != domainStat.Device
                    || locatorIdentity.Inode != domainStat.Inode)
                {
                    error = "child name does not resolve to the retained child identity";
                    return false;
                }
            }
            finally
            {
                PreparationInterop.CloseFd(locator);
            }
        }

        if (!TryValidateControl(domainFd, (long)domainStat.Device, "cgroup.kill", killFd, ControlAccess.Write, out error))
        {
            return false;
        }

        if (!TryValidateControl(domainFd, (long)domainStat.Device, "cgroup.events", eventsFd, ControlAccess.Read, out error))
        {
            return false;
        }

        var readbacks = new Dictionary<string, int>(StringComparer.Ordinal);
        var resourceRoles = new[]
        {
            ("memory.max", fds[(int)DomainTransferRole.MemoryMax]),
            ("memory.swap.max", fds[(int)DomainTransferRole.MemorySwapMax]),
            ("pids.max", fds[(int)DomainTransferRole.PidsMax]),
        };
        foreach (var (name, fd) in resourceRoles)
        {
            if (!TryValidateControl(domainFd, (long)domainStat.Device, name, fd, ControlAccess.ReadWrite, out error))
            {
                return false;
            }

            readbacks[name] = fd;
        }

        identity = new DomainIdentity(
            executionId, bootId, mountId, fsType,
            parentStat.Device, parentStat.Inode,
            childName ?? string.Empty, domainStat.Device, domainStat.Inode, leaseGeneration);
        backend = new CgroupBackend(parentFd, domainFd, killFd, eventsFd, readbacks);
        return true;
    }

    /// <summary>Creates a delegated child domain beneath the retained parent
    /// descriptor, opens its required controls, derives mount identity from
    /// the retained descriptors, and validates every handle's role and
    /// membership. Fails closed if any primitive or control is unavailable.</summary>
    internal static bool TryCreateDomain(
        ScopedAllocationContext context,
        string childName,
        out CgroupBackend? backend,
        out DomainIdentity? identity,
        out string error)
    {
        backend = null;
        identity = null;
        error = string.Empty;
        if (context is null)
        {
            error = "domain creation requires a scoped allocation context";
            return false;
        }

        if (!context.IsLive)
        {
            error = "scoped allocation context is no longer live";
            return false;
        }

        if (!IsLinux)
        {
            error = "Unsupported: not Linux";
            return false;
        }

        var parentFd = context.ParentFd;
        var executionId = context.ExecutionId;
        var leaseGeneration = context.LeaseGeneration;
        var bootId = context.BootId;

        if (!PreparationInterop.IsSupportedArchitecture())
        {
            error = "Unsupported: architecture not pinned for openat2";
            return false;
        }

        if (PreparationInterop.StatFd(parentFd, out var parentIdentity) != 0
            || !PreparationInterop.IsDirectory(parentIdentity.Mode))
        {
            error = "delegated parent is not a directory handle";
            return false;
        }

        var makeErrno = PreparationInterop.MakeDirectory(parentFd, childName, 0x1ED /*0755*/);
        if (makeErrno != 0)
        {
            error = makeErrno == PreparationInterop.EEXIST
                ? "domain name already exists; refusing to adopt"
                : $"mkdirat errno {makeErrno}";
            return false;
        }

        // The backend owns its own parent descriptor; the caller's retained
        // parent FD is never handed to, or closed by, the backend.
        var opened = new List<int>();
        try
        {
            var backendParentFd = PreparationInterop.Dup(parentFd);
            if (backendParentFd < 0)
            {
                error = "parent descriptor duplication failed";
                return false;
            }

            opened.Add(backendParentFd);

            var dirErrno = PreparationInterop.OpenRelative(
                parentFd, childName,
                PreparationInterop.O_RDONLY | PreparationInterop.O_DIRECTORY
                    | PreparationInterop.O_NOFOLLOW | PreparationInterop.O_CLOEXEC,
                PreparationInterop.RequiredResolve, out var domainFd);
            if (dirErrno != 0)
            {
                error = $"domain open errno {dirErrno}";
                return false;
            }

            opened.Add(domainFd);

            var killErrno = PreparationInterop.OpenRelative(
                domainFd, "cgroup.kill",
                PreparationInterop.O_WRONLY | PreparationInterop.O_CLOEXEC,
                PreparationInterop.RequiredResolve, out var killFd);
            if (killErrno != 0)
            {
                error = $"cgroup.kill open errno {killErrno}";
                return false;
            }

            opened.Add(killFd);

            var eventsErrno = PreparationInterop.OpenRelative(
                domainFd, "cgroup.events",
                PreparationInterop.O_RDONLY | PreparationInterop.O_CLOEXEC,
                PreparationInterop.RequiredResolve, out var eventsFd);
            if (eventsErrno != 0)
            {
                error = $"cgroup.events open errno {eventsErrno}";
                return false;
            }

            opened.Add(eventsFd);

            var ordered = new int[DomainHandleTransfer.ExactDescriptorCount];
            ordered[(int)DomainTransferRole.Parent] = backendParentFd;
            ordered[(int)DomainTransferRole.DomainDirectory] = domainFd;
            ordered[(int)DomainTransferRole.Kill] = killFd;
            ordered[(int)DomainTransferRole.Events] = eventsFd;
            foreach (var (name, role) in new[]
            {
                ("memory.max", DomainTransferRole.MemoryMax),
                ("memory.swap.max", DomainTransferRole.MemorySwapMax),
                ("pids.max", DomainTransferRole.PidsMax),
            })
            {
                var errno = PreparationInterop.OpenRelative(
                    domainFd, name,
                    PreparationInterop.O_RDWR | PreparationInterop.O_CLOEXEC,
                    PreparationInterop.RequiredResolve, out var fd);
                if (errno != 0)
                {
                    error = $"{name} open errno {errno}";
                    return false;
                }

                opened.Add(fd);
                ordered[(int)role] = fd;
            }

            if (!TryValidateDomainHandles(ordered, childName, executionId, leaseGeneration, bootId, out var built, out var derived, out var validateError))
            {
                error = $"domain handle validation failed: {validateError}";
                return false;
            }

            backend = built;
            identity = derived;
            // Ownership of every opened descriptor transfers to the backend.
            opened.Clear();
            return true;
        }
        finally
        {
            foreach (var fd in opened)
            {
                PreparationInterop.CloseFd(fd);
            }
        }
    }
}
