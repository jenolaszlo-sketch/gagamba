// WP03 M1RecoveryScope: the dedicated harness host. It sets and verifies
// child-subreaper, creates the delegated domain beneath a retained parent,
// retains independent recovery handles, spawns the per-execution owner as
// its direct child, and — when the owner dies — fences journal takeover
// and cleans the domain and preparation allocations by retained-handle
// identity. It never recreates a capability, release authority, target
// work or execution state from records alone.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

internal sealed record ScopeInitResult(bool Ok, string Error);

internal sealed record DomainAllocationResult(bool Ok, DomainLease? RecoveryLease, int OwnerSocketFd, string Error);

internal sealed record OwnerExitResult(bool Exited, int ExitCode, bool Crashed, string Error);

/// <summary>Real integrated preparation request. The manifest is built by
/// the caller from staged bindings after copy (never predicted); the death
/// barrier is test-only deterministic death injection (null in production).</summary>
internal sealed record IntegratedPreparationRequest(
    string DomainChildName,
    Guid ExecutionId,
    Guid LeaseGeneration,
    string BootId,
    ICopyHost CopyHost,
    CopyPlan Plan,
    OwnershipJournal Journal,
    JournalWriterLease Writer,
    PreparedTransaction.PreparationStore Store,
    Func<IReadOnlyList<StagedObject>, (RestrictedManifest Manifest, PreparationIdentity Identity)> ManifestFactory,
    long ExpiresAtNs,
    Action<string>? DeathBarrier);

internal sealed record IntegratedPreparationOutcome(
    bool Published,
    CopyFailureKind Failure,
    string Detail,
    PreparedTransaction? Capability,
    DomainLease? PrimaryLease,
    PreparedClosure? Closure)
{
    internal static IntegratedPreparationOutcome Failed(CopyFailureKind failure, string detail, PreparedClosure? retained = null) =>
        new(false, failure, detail, null, null, retained);

    internal static IntegratedPreparationOutcome Success(PreparedTransaction capability, DomainLease primary) =>
        new(true, CopyFailureKind.None, "Integrated preparation published.", capability, primary, null);
}

internal sealed class M1RecoveryScope : IDisposable
{
    private int _parentFd = -1;
    private bool _subreaperEnabled;
    private bool _disposed;
    private readonly List<DomainLease> _retainedRecoveryLeases = new();
    private readonly List<SafeHandle> _retainedStagingParents = new();
    private readonly List<SafeHandle> _retainedStagingDirs = new();

    internal bool SubreaperEnabled => _subreaperEnabled;

    /// <summary>Recovery authority retained across integrated operations:
    /// duplicate domain leases, staging parent handles and staging
    /// subdirectory handles. These survive simulated actor death and are
    /// what recovery may use with durable records.</summary>
    internal IReadOnlyList<DomainLease> RetainedRecoveryLeases => _retainedRecoveryLeases;

    internal IReadOnlyList<SafeHandle> RetainedStagingParents => _retainedStagingParents;

    internal IReadOnlyList<SafeHandle> RetainedStagingDirectories => _retainedStagingDirs;

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

    /// <summary>Real integrated allocation/preparation path (G2-03). Runs
    /// the required order with the caller-held writer lease: intent durable
    /// → create domain + open staging parent → capture identities → retain
    /// recovery authority → registered durable → WP02 copy/binding with
    /// incremental per-object accounting → authority acknowledgement →
    /// Prepared durable → Mint. A <see cref="BarrierDeathException"/> from
    /// the death barrier abandons the operation immediately with no unwind
    /// cleanup; only previously retained scope authority plus durable state
    /// may be used afterward. Test-only barrier; null in production.</summary>
    internal IntegratedPreparationOutcome AllocatePreparation(
        IntegratedPreparationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_disposed)
        {
            return IntegratedPreparationOutcome.Failed(CopyFailureKind.InvariantFailure, "scope disposed");
        }

        if (_parentFd < 0)
        {
            return IntegratedPreparationOutcome.Failed(CopyFailureKind.InvariantFailure, "no retained delegated parent");
        }

        DomainLease? primary = null;
        try
        {
            // Intent names everything planned (domain child, staging
            // subdir, object names) as unconfirmed; no identities yet.
            var intent = BuildIntent(request);
            var intentWrite = request.Journal.WriteNext(request.Writer, intent);
            if (!intentWrite.Ok)
            {
                return IntegratedPreparationOutcome.Failed(CopyFailureKind.PublicationFailure, $"AllocationIntent not durable: {intentWrite.Error}");
            }

            request.DeathBarrier?.Invoke("after-intent");

            // Create the domain and retain the recovery duplicate.
            if (!CgroupInterop.TryCreateDomain(
                _parentFd, request.DomainChildName, request.ExecutionId, request.LeaseGeneration, request.BootId,
                out var backend, out var identity, out var domainError))
            {
                return IntegratedPreparationOutcome.Failed(CopyFailureKind.StagingFailure, $"domain creation failed: {domainError}");
            }

            primary = new DomainLease(backend!, identity!);
            var recovery = primary.DuplicateForRecovery();
            _retainedRecoveryLeases.Add(recovery);

            request.DeathBarrier?.Invoke("after-domain-create");

            // Open and retain the staging parent (real FS authority).
            var openErrno = LibcStagingParent(request.Plan.StagingParentPath, out var parentHandle);
            if (openErrno != 0 || parentHandle is null)
            {
                return IntegratedPreparationOutcome.Failed(CopyFailureKind.StagingFailure, $"staging parent open errno {openErrno}");
            }

            _retainedStagingParents.Add(parentHandle);

            request.DeathBarrier?.Invoke("after-create");

            // Registered with actual domain + staging-parent identities.
            var registered = BuildRegistered(request, primary.Identity, parentHandle);
            var registeredWrite = request.Journal.WriteNext(request.Writer, registered);
            if (!registeredWrite.Ok)
            {
                return IntegratedPreparationOutcome.Failed(CopyFailureKind.PublicationFailure, $"AllocationRegistered not durable: {registeredWrite.Error}");
            }

            request.DeathBarrier?.Invoke("after-registered");

            // Copy with incremental per-object durable accounting.
            var accounted = new IntegratedAccounting(this, request.Journal, request.Writer, registered);
            var plan = request.Plan with { OnObjectAccounted = accounted.OnObject };
            var prepared = SafeCopyIn.Prepare(plan, request.CopyHost, cancellationToken);
            if (!prepared.Succeeded)
            {
                return IntegratedPreparationOutcome.Failed(
                    prepared.Failure, prepared.Detail, prepared.Closure);
            }

            var closure = prepared.Closure!;
            request.DeathBarrier?.Invoke("after-copy");

            // Build the manifest from staged bindings (never predicted)
            // before acknowledgement, so the Prepared record can name the
            // exact preparation it precedes.
            RestrictedManifest manifest;
            PreparationIdentity identity2;
            try
            {
                (manifest, identity2) = request.ManifestFactory(closure.Objects);
            }
            catch (Exception ex) when (ex is not BarrierDeathException)
            {
                return IntegratedPreparationOutcome.Failed(CopyFailureKind.PublicationFailure, $"Manifest construction failed: {ex.GetType().Name}.", closure);
            }

            // Acknowledge: verify retained authority is live, retain the
            // staging subdir duplicate, then write Prepared.
            var sink = new ScopeRecoverySink(this, request.ExecutionId, request.LeaseGeneration);
            if (!sink.TryVerifyAuthority(primary, closure, out var ackError))
            {
                return IntegratedPreparationOutcome.Failed(CopyFailureKind.PublicationFailure, ackError, closure);
            }

            var preparedRecord = accounted.Current with
            {
                Stage = PreparationStage.Prepared,
                Preparation = new RecordedPreparation(
                    identity2.ExecutionId, identity2.IssuerId, identity2.GenerationId,
                    identity2.PreparationId, identity2.ManifestDigest.ToArray()),
            };
            var preparedWrite = request.Journal.WriteNext(request.Writer, preparedRecord);
            if (!preparedWrite.Ok)
            {
                return IntegratedPreparationOutcome.Failed(CopyFailureKind.PublicationFailure, $"Prepared not durable: {preparedWrite.Error}", closure);
            }

            request.DeathBarrier?.Invoke("after-prepared");

            // Mint through the WP01 boundary.
            var ack = new Func<CancellationToken, bool>(_ => true);
            var published = PreparationPublisher.Publish(
                request.Store, closure, manifest, identity2, request.ExpiresAtNs, ack, cancellationToken);
            if (!published.Published)
            {
                return IntegratedPreparationOutcome.Failed(published.Failure, published.Detail, published.Closure);
            }

            // Success: the primary lease transfers to the caller with the
            // capability; recovery duplicates stay retained by this scope.
            var ownedPrimary = primary;
            primary = null;
            return IntegratedPreparationOutcome.Success(published.Capability!, ownedPrimary);
        }
        catch (BarrierDeathException)
        {
            // Simulated actor death: close operation-local authority
            // (simulating OS reclaim) and propagate with no unwind cleanup,
            // no journal writes and no capability. Scope-retained authority
            // and durable state are untouched for recovery.
            primary?.Dispose();
            primary = null;
            throw;
        }
        catch (OperationCanceledException)
        {
            primary?.Dispose();
            primary = null;
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected failure outside the engine (which reports its own
            // outcomes): fail closed without publishing. No capability.
            return IntegratedPreparationOutcome.Failed(CopyFailureKind.InvariantFailure, $"Integrated preparation failed: {ex.GetType().Name}.");
        }
    }

    private static int LibcStagingParent(string path, out SafeHandle? handle)
    {
        handle = null;
        var errno = PreparationInterop.OpenDirectory(path, out var fd);
        if (errno != 0)
        {
            return errno;
        }

        if (PreparationInterop.StatFd(fd, out var stat) != 0 || !PreparationInterop.IsDirectory(stat.Mode))
        {
            PreparationInterop.CloseFd(fd);
            return PreparationInterop.ENOTDIR;
        }

        handle = new SafeFileHandle(new IntPtr(fd), ownsHandle: true);
        return 0;
    }

    private static OwnershipSnapshot BuildIntent(IntegratedPreparationRequest request)
    {
        var allocations = new List<AllocationEntry>
        {
            new(AllocationKind.Domain, request.DomainChildName, 0, Array.Empty<byte>(), 0, 0, Confirmed: false),
            new(AllocationKind.StagingDirectory, request.Plan.StagingDirectoryName, 0, Array.Empty<byte>(), 0, 0, Confirmed: false),
        };
        var index = 0;
        foreach (var obj in request.Plan.Objects)
        {
            allocations.Add(new AllocationEntry(
                AllocationKind.Staging, $"{obj.Role}-{index:D3}.staged", 0, Array.Empty<byte>(), 0, 0, Confirmed: false));
            index++;
        }

        return new OwnershipSnapshot(
            OwnershipSnapshotCodec.CurrentVersion, 0, request.ExecutionId, request.BootId,
            0, string.Empty, 0, 0, request.DomainChildName, 0, 0, request.LeaseGeneration,
            OwnershipWriterKind.Owner, OwnershipCleanupState.NotStarted, PreparationStage.AllocationIntent,
            StagingParentDevice: 0, StagingParentInode: 0,
            Allocations: allocations, Preparation: null);
    }

    private static OwnershipSnapshot BuildRegistered(
        IntegratedPreparationRequest request, DomainIdentity domain, SafeHandle stagingParent)
    {
        PreparationInterop.StatFd(((SafeFileHandle)stagingParent).DangerousGetHandle().ToInt32(), out var parentStat);
        var allocations = new List<AllocationEntry>
        {
            new(AllocationKind.Domain, domain.ChildName, 0, Array.Empty<byte>(), domain.ChildDevice, domain.ChildInode, Confirmed: true),
            new(AllocationKind.StagingDirectory, request.Plan.StagingDirectoryName, 0, Array.Empty<byte>(), 0, 0, Confirmed: false),
        };
        var index = 0;
        foreach (var obj in request.Plan.Objects)
        {
            allocations.Add(new AllocationEntry(
                AllocationKind.Staging, $"{obj.Role}-{index:D3}.staged", 0, Array.Empty<byte>(), 0, 0, Confirmed: false));
            index++;
        }

        return new OwnershipSnapshot(
            OwnershipSnapshotCodec.CurrentVersion, 0, request.ExecutionId, request.BootId,
            domain.CgroupMountId, domain.CgroupMountFsType,
            domain.ParentDevice, domain.ParentInode,
            domain.ChildName, domain.ChildDevice, domain.ChildInode, domain.LeaseGeneration,
            OwnershipWriterKind.Owner, OwnershipCleanupState.NotStarted, PreparationStage.AllocationRegistered,
            StagingParentDevice: parentStat.Device, StagingParentInode: parentStat.Inode,
            Allocations: allocations, Preparation: null);
    }

    /// <summary>Per-object incremental accounting: confirms each bound
    /// object (and the staging subdir on first sight) durably under the
    /// writer fence as copying proceeds. The staging subdir descriptor is
    /// duplicated into scope retention on first sight so recovery never
    /// needs to reopen it by name.</summary>
    private sealed class IntegratedAccounting
    {
        private readonly M1RecoveryScope _scope;
        private readonly OwnershipJournal _journal;
        private readonly JournalWriterLease _writer;
        private OwnershipSnapshot _current;
        private bool _subdirRecorded;

        internal IntegratedAccounting(M1RecoveryScope scope, OwnershipJournal journal, JournalWriterLease writer, OwnershipSnapshot registered)
        {
            _scope = scope;
            _journal = journal;
            _writer = writer;
            _current = registered;
        }

        internal OwnershipSnapshot Current => _current;

        internal void OnObject(SafeHandle stagingDir, StagedObject staged)
        {
            var allocations = _current.Allocations.ToList();
            if (!_subdirRecorded)
            {
                var subdirFd = ((SafeFileHandle)stagingDir).DangerousGetHandle().ToInt32();
                PreparationInterop.StatFd(subdirFd, out var subdirStat);
                var dup = PreparationInterop.Dup(subdirFd);
                if (dup >= 0)
                {
                    _scope._retainedStagingDirs.Add(new SafeFileHandle(new IntPtr(dup), ownsHandle: true));
                }

                for (var i = 0; i < allocations.Count; i++)
                {
                    if (allocations[i].Kind == AllocationKind.StagingDirectory)
                    {
                        allocations[i] = allocations[i] with
                        {
                            Device = subdirStat.Device,
                            Inode = subdirStat.Inode,
                            Confirmed = true,
                        };
                    }
                }

                _subdirRecorded = true;
            }

            for (var i = 0; i < allocations.Count; i++)
            {
                if (allocations[i].Kind == AllocationKind.Staging
                    && allocations[i].Name == staged.StagingName)
                {
                    allocations[i] = new AllocationEntry(
                        AllocationKind.Staging, staged.StagingName, staged.Length,
                        (byte[])staged.Sha256.Clone(), staged.Device, staged.Inode, Confirmed: true);
                }
            }

            var updated = _current with { Allocations = allocations };
            var written = _journal.WriteNext(_writer, updated);
            if (!written.Ok)
            {
                throw new InvalidOperationException($"object accounting not durable: {written.Error}");
            }

            _current = updated;
        }
    }

    /// <summary>Real surviving-scope cleanup sink: verifies the presented
    /// primary lease and closure against the expected execution/generation,
    /// confirms live handles, and retains a duplicate of the staging
    /// subdirectory for recovery. It never manufactures authority from the
    /// journal; mismatched or dead authority is refused.</summary>
    private sealed class ScopeRecoverySink : ICleanupAuthoritySink
    {
        private readonly M1RecoveryScope _scope;
        private readonly Guid _executionId;
        private readonly Guid _leaseGeneration;

        internal ScopeRecoverySink(M1RecoveryScope scope, Guid executionId, Guid leaseGeneration)
        {
            _scope = scope;
            _executionId = executionId;
            _leaseGeneration = leaseGeneration;
        }

        public bool TryRetainRecoveryHandles(DomainLease recoveryLease, PreparedClosure closure, out string error)
        {
            error = string.Empty;
            ArgumentNullException.ThrowIfNull(recoveryLease);
            ArgumentNullException.ThrowIfNull(closure);
            return TryVerifyAuthority(recoveryLease, closure, out error);
        }

        internal bool TryVerifyAuthority(DomainLease primary, PreparedClosure closure, out string error)
        {
            error = string.Empty;
            ArgumentNullException.ThrowIfNull(primary);
            ArgumentNullException.ThrowIfNull(closure);
            if (!primary.Identity.ExecutionId.Equals(_executionId)
                || !primary.Identity.LeaseGeneration.Equals(_leaseGeneration))
            {
                error = "primary lease identity does not match this allocation";
                return false;
            }

            if (!primary.AuthorityAlive)
            {
                error = "primary lease has no live authority";
                return false;
            }

            foreach (var staged in closure.Objects)
            {
                if (staged.Handle.IsClosed)
                {
                    error = $"staged object {staged.Role} has no live handle";
                    return false;
                }
            }

            if (closure.StagingDirectory is null)
            {
                error = "closure has no retained staging directory";
                return false;
            }

            var dup = PreparationInterop.Dup(
                ((SafeFileHandle)closure.StagingDirectory).DangerousGetHandle().ToInt32());
            if (dup < 0)
            {
                error = "staging subdirectory duplication failed";
                return false;
            }

            _scope._retainedStagingDirs.Add(new SafeFileHandle(new IntPtr(dup), ownsHandle: true));
            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var lease in _retainedRecoveryLeases)
        {
            lease.Dispose();
        }

        _retainedRecoveryLeases.Clear();
        foreach (var handle in _retainedStagingParents)
        {
            handle.Dispose();
        }

        _retainedStagingParents.Clear();
        foreach (var handle in _retainedStagingDirs)
        {
            handle.Dispose();
        }

        _retainedStagingDirs.Clear();
        if (_parentFd >= 0)
        {
            PreparationInterop.CloseFd(_parentFd);
            _parentFd = -1;
        }

        GC.SuppressFinalize(this);
    }
}
