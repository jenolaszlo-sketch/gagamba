// WP03 narrow cgroup v2 interop: locate the real cgroup2 mount, read the
// host boot id, create a delegated child domain under a retained parent
// descriptor, and open the required controls. Every primitive is
// descriptor-relative with beneath-only, no-follow resolution. The
// resulting backend holds live authority handles; records are never
// authority. Non-Linux callers fail closed with Unsupported.
using System.Globalization;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Native;

internal sealed record CgroupMountInfo(ulong MountId, string FileSystemType, string MountPoint);

internal sealed record CgroupBootInfo(string BootId);

/// <summary>Native IDomainBackend over real cgroup descriptors. Holds the
/// parent descriptor plus the domain directory and control descriptors.
/// Dispose closes every owned descriptor; nothing here re-resolves by
/// path after creation.</summary>
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

    /// <summary>Primary handles in fixed order for SCM_RIGHTS transfer to
    /// the owner: parent, domain dir, kill, events, memory.max,
    /// memory.swap.max, pids.max.</summary>
    internal int[] OrderedFds()
    {
        var list = new List<int> { _parentFd, _domainFd, _killFd, _eventsFd };
        foreach (var name in new[] { "memory.max", "memory.swap.max", "pids.max" })
        {
            list.Add(_readbackFds[name]);
        }

        return list.ToArray();
    }

    /// <summary>Rebuilds a backend from received descriptors in the same
    /// fixed order used by <see cref="OrderedFds"/>.</summary>
    internal static CgroupBackend FromOrderedFds(int[] fds)
    {
        if (fds.Length != 7)
        {
            throw new ArgumentException("expected 7 ordered descriptors", nameof(fds));
        }

        var readbacks = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["memory.max"] = fds[4],
            ["memory.swap.max"] = fds[5],
            ["pids.max"] = fds[6],
        };
        return new CgroupBackend(fds[0], fds[1], fds[2], fds[3], readbacks);
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

        // Reading advances the cgroup.events cursor; re-open is not used.
        // Seek back to the start through the retained descriptor.
        PreparationInterop.SeekStart(fd);
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
}

internal static class CgroupInterop
{
    internal static bool IsLinux => PreparationInterop.IsLinux;

    /// <summary>Parses cgroup2 mount identity from /proc/self/mountinfo.
    /// Mount identity comes from the real mount, never a supplied string.</summary>
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

                // Mount point is field 5 (index 4) in the left section.
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

    /// <summary>Creates a delegated child domain beneath the retained parent
    /// descriptor and opens its required controls. Fails closed if any
    /// primitive or control is unavailable.</summary>
    internal static bool TryCreateDomain(
        int parentFd,
        string childName,
        Guid executionId,
        Guid leaseGeneration,
        CgroupMountInfo mount,
        string bootId,
        out CgroupBackend? backend,
        out DomainIdentity? identity,
        out string error)
    {
        backend = null;
        identity = null;
        error = string.Empty;
        if (!IsLinux)
        {
            error = "Unsupported: not Linux";
            return false;
        }

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

        // Bootstrap the domain name under the retained parent. EEXIST means
        // a same-name object exists: refuse rather than adopt.
        var makeErrno = PreparationInterop.MakeDirectory(parentFd, childName, 0x1ED /*0755*/);
        if (makeErrno != 0)
        {
            error = makeErrno == PreparationInterop.EEXIST
                ? "domain name already exists; refusing to adopt"
                : $"mkdirat errno {makeErrno}";
            return false;
        }

        var opened = new List<int>();
        try
        {
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
            if (PreparationInterop.StatFd(domainFd, out var domainIdentity) != 0
                || !PreparationInterop.IsDirectory(domainIdentity.Mode)
                || domainIdentity.Device != parentIdentity.Device)
            {
                error = "domain identity/type mismatch after creation";
                return false;
            }

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
            var readbacks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var control in new[] { "memory.max", "memory.swap.max", "pids.max" })
            {
                var errno = PreparationInterop.OpenRelative(
                    domainFd, control,
                    PreparationInterop.O_RDWR | PreparationInterop.O_CLOEXEC,
                    PreparationInterop.RequiredResolve, out var fd);
                if (errno != 0)
                {
                    error = $"{control} open errno {errno}";
                    return false;
                }

                opened.Add(fd);
                if (PreparationInterop.StatFd(fd, out var controlIdentity) != 0
                    || PreparationInterop.IsDirectory(controlIdentity.Mode)
                    || controlIdentity.Device != domainIdentity.Device)
                {
                    error = $"{control} membership check failed";
                    return false;
                }

                readbacks[control] = fd;
            }

            identity = new DomainIdentity(
                executionId, bootId, mount.MountId, mount.FileSystemType,
                parentIdentity.Device, parentIdentity.Inode,
                childName, domainIdentity.Device, domainIdentity.Inode, leaseGeneration);
            backend = new CgroupBackend(parentFd, domainFd, killFd, eventsFd, readbacks);
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
