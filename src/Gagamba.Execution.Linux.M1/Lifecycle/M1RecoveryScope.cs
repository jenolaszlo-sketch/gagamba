// WP03 M1RecoveryScope: the dedicated harness host. It sets and verifies
// child-subreaper, creates the delegated domain beneath a retained parent,
// retains independent recovery handles, spawns the per-execution owner as
// its direct child, and — when the owner dies — fences journal takeover
// and cleans the domain and preparation allocations by retained-handle
// identity. It never recreates a capability, release authority, target
// work or execution state from records alone.
using System.Diagnostics;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

internal sealed record ScopeInitResult(bool Ok, string Error);

internal sealed record DomainAllocationResult(bool Ok, DomainLease? Lease, string Error);

internal sealed record OwnerExitResult(bool Exited, int ExitCode, bool Crashed, string Error);

internal sealed class M1RecoveryScope : IDisposable
{
    private int _parentFd = -1;
    private bool _subreaperEnabled;
    private bool _disposed;

    internal bool SubreaperEnabled => _subreaperEnabled;

    /// <summary>Enable and verify child-subreaper before creating children.</summary>
    internal ScopeInitResult TryInitialize()
    {
        if (!CgroupInterop.IsLinux)
        {
            return new ScopeInitResult(false, "Unsupported: not Linux");
        }

        if (!RecoverySubreaper.TryEnableAndVerify(out var error))
        {
            return new ScopeInitResult(false, error);
        }

        _subreaperEnabled = true;
        return new ScopeInitResult(true, string.Empty);
    }

    /// <summary>Opens the operator-delegated cgroup parent once and retains
    /// the descriptor. The path is host-supplied/trusted.</summary>
    internal ScopeInitResult TryOpenDelegatedParent(string parentPath)
    {
        if (!_subreaperEnabled)
        {
            return new ScopeInitResult(false, "scope not initialized");
        }

        var errno = PreparationInterop.OpenDirectory(parentPath, out var fd);
        if (errno != 0)
        {
            return new ScopeInitResult(false, $"delegation open errno {errno}");
        }

        // The parent must be a directory on the cgroup2 filesystem and hold
        // the required controls for children to be delegated.
        if (PreparationInterop.StatFd(fd, out var stat) != 0 || !PreparationInterop.IsDirectory(stat.Mode))
        {
            PreparationInterop.CloseFd(fd);
            return new ScopeInitResult(false, "delegated parent is not a directory");
        }

        _parentFd = fd;
        return new ScopeInitResult(true, string.Empty);
    }

    /// <summary>Allocates the domain and returns the owner lease plus its
    /// independent recovery duplicate held by the scope.</summary>
    internal DomainAllocationResult AllocateDomain(
        string childName,
        Guid executionId,
        Guid leaseGeneration,
        CgroupMountInfo mount,
        string bootId,
        out DomainLease? recoveryLease)
    {
        recoveryLease = null;
        if (_parentFd < 0)
        {
            return new DomainAllocationResult(false, null, "no retained delegated parent");
        }

        if (!CgroupInterop.TryCreateDomain(
            _parentFd, childName, executionId, leaseGeneration, mount, bootId,
            out var backend, out var identity, out var error))
        {
            return new DomainAllocationResult(false, null, error);
        }

        var lease = new DomainLease(backend!, identity!);
        recoveryLease = lease.DuplicateForRecovery();
        return new DomainAllocationResult(true, lease, string.Empty);
    }

    internal Process SpawnOwner(string ownerDll, string[] arguments, out string error)
    {
        error = string.Empty;
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(ownerDll);
        foreach (var argument in arguments)
        {
            psi.ArgumentList.Add(argument);
        }

        try
        {
            var process = Process.Start(psi);
            if (process is null)
            {
                error = "owner process start returned null";
                throw new InvalidOperationException(error);
            }

            return process;
        }
        catch (Exception ex)
        {
            error = $"owner start failed: {ex.GetType().Name}";
            throw;
        }
    }

    /// <summary>Waits for the direct-child owner to exit within a bound.</summary>
    internal OwnerExitResult WaitForOwner(Process owner, TimeSpan timeout)
    {
        if (!owner.WaitForExit((int)timeout.TotalMilliseconds))
        {
            return new OwnerExitResult(false, -1, false, "owner did not exit within the bound");
        }

        var crashed = owner.ExitCode != 0;
        return new OwnerExitResult(true, owner.ExitCode, crashed, string.Empty);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_parentFd >= 0)
        {
            PreparationInterop.CloseFd(_parentFd);
            _parentFd = -1;
        }

        GC.SuppressFinalize(this);
    }
}
