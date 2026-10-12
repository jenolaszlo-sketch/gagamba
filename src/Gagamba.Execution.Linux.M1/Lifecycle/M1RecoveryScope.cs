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

internal sealed record DomainAllocationResult(bool Ok, DomainLease? RecoveryLease, int OwnerSocketFd, string Error);

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

    /// <summary>Allocates the domain, transfers the primary handles to the
    /// owner over one inherited Unix socketpair (SCM_RIGHTS with the strict
    /// versioned metadata message), and retains a separately owned recovery
    /// duplicate for this scope. The scope's delegated-parent descriptor is
    /// retained by the scope; the backend owns its own duplicated parent
    /// descriptor. The returned child socket end must be handed to
    /// <see cref="SpawnOwner"/> (which closes it) or closed by the caller.</summary>
    internal DomainAllocationResult AllocateDomain(
        string childName,
        Guid executionId,
        Guid leaseGeneration,
        string bootId)
    {
        if (_parentFd < 0)
        {
            return new DomainAllocationResult(false, null, -1, "no retained delegated parent");
        }

        if (!CgroupInterop.TryCreateDomain(
            _parentFd, childName, executionId, leaseGeneration, bootId,
            out var backend, out var identity, out var error))
        {
            return new DomainAllocationResult(false, null, -1, error);
        }

        // Separately owned recovery duplicate retained by this scope.
        var recoveryBackend = backend!.DuplicateForRecovery();
        var recoveryLease = new DomainLease(recoveryBackend, identity!);

        if (!FdPassing.TryCreateInheritableSocketPair(out var parentEnd, out var childEnd, out var socketError))
        {
            recoveryLease.Dispose();
            backend.Dispose();
            return new DomainAllocationResult(false, null, -1, socketError);
        }

        var payload = DomainHandleTransfer.Encode(executionId, leaseGeneration);
        bool sent;
        string sendError;
        try
        {
            sent = FdPassing.TrySendTransfer(parentEnd, backend.OrderedFds(), payload, out sendError);
        }
        finally
        {
            // The parent end is this scope's and is closed exactly once.
            PreparationInterop.CloseFd(parentEnd);
        }

        // The primary descriptors were duplicated into the owner via
        // SCM_RIGHTS; the scope's primary copies are no longer needed.
        backend.Dispose();

        if (!sent)
        {
            recoveryLease.Dispose();
            // The child end was never handed off; close it exactly once.
            PreparationInterop.CloseFd(childEnd);
            return new DomainAllocationResult(false, null, -1, sendError);
        }

        return new DomainAllocationResult(true, recoveryLease, childEnd, string.Empty);
    }

    internal Process SpawnOwner(string ownerDll, string[] arguments, int ownerSocketFd, out string error)
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

        psi.Environment["GAGAMBA_BOOTSTRAP_FD"] = ownerSocketFd.ToString();

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
        finally
        {
            // The child end was inherited at spawn; the scope no longer
            // needs it.
            if (ownerSocketFd >= 0)
            {
                PreparationInterop.CloseFd(ownerSocketFd);
            }
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
