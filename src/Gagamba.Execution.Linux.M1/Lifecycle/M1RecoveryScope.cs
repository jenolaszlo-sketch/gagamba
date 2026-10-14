// WP03 M1RecoveryScope is the external recovery actor. It retains the delegated parent and independent domain/staging handles, owns the subreaper and owner pidfd lifecycle, and takes the journal writer only after proven owner exit.
// Domain creation, staging, Prepared, and Mint execute in the owner process. The scope never receives a preparation capability or release authority.
using System.Diagnostics;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

internal sealed record ScopeInitResult(bool Ok, string Error);

internal sealed record OwnerExitResult(bool Exited, int ExitCode, bool Crashed, string Error);

/// <summary>Spawn outcome carrying the retained owner pidfd. <see cref="Ok"/>
/// requires both a live owner direct child and a retained CLOEXEC pidfd; a
/// failed pidfd acquisition terminates the owner and refuses so the native
/// path never reaches a state whose owner-loss recovery would depend on a
/// missing pidfd.</summary>
internal sealed record OwnerSpawnResult(Process? Process, int PidFd, string Error)
{
    internal bool Ok => Process is not null && Error.Length == 0;
}

/// <summary>Inputs for launching the isolated owner transaction. The
/// callback builds a manifest from bounded, non-authoritative staged
/// descriptions; neither it nor any capability-bearing object crosses the
/// owner boundary.</summary>
internal sealed record OwnerPreparationRequest(
    string OwnerDllPath,
    string DomainChildName,
    Guid ExecutionId,
    Guid LeaseGeneration,
    string BootId,
    CopyPlan Plan,
    OwnershipJournal Journal,
    JournalWriterLease Writer,
    Guid IssuerId,
    Guid StoreGenerationId,
    Func<IReadOnlyList<StagedObjectDescription>, (RestrictedManifest Manifest, PreparationIdentity Identity)> ManifestFactory,
    long ExpiresAtNs,
    string? TestBarrier = null,
    bool WithholdStagingRetentionAckForTest = false);

/// <summary>Non-authoritative owner result. The capability and primary
/// domain lease remain in the live owner process; only a bounded
/// preparation description may return to the scope.</summary>
internal sealed record OwnerPreparationOutcome(
    bool Completed,
    OwnerPreparedDescription? Description,
    Process? Owner,
    string? Barrier,
    string Error)
{
    internal bool Ok => Completed && Error.Length == 0 && Description is not null && Owner is not null;
}

internal sealed class M1RecoveryScope : IDisposable
{
    private int _parentFd = -1;
    private bool _subreaperEnabled;
    private bool _disposed;
    private readonly IPidFdProvider _pidFdProvider;
    private readonly List<DomainLease> _retainedRecoveryLeases = new();
    private readonly List<SafeHandle> _retainedStagingParents = new();
    private readonly List<SafeHandle> _retainedStagingDirs = new();
    private readonly Dictionary<int, SafeHandle> _retainedOwnerPidFds = new();
    private readonly Dictionary<int, OwnerProtocolChannel> _ownerChannels = new();
    private readonly Dictionary<int, Process> _owners = new();

    internal M1RecoveryScope(IPidFdProvider? pidFdProvider = null)
    {
        _pidFdProvider = pidFdProvider ?? RealPidFdProvider.Instance;
    }

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

        // Verify the operator-delegated prerequisites (cgroup2 + memory/pids
        // already delegated). Gagamba refuses rather than provisioning.
        if (!CgroupInterop.TryVerifyDelegatedParent(fd, out var delegationError))
        {
            PreparationInterop.CloseFd(fd);
            return new ScopeInitResult(false, $"delegation unqualified: {delegationError}");
        }

        _parentFd = fd;
        return new ScopeInitResult(true, string.Empty);
    }

    /// <summary>Spawns the per-execution owner as this scope's direct child,
    /// then immediately acquires and retains a CLOEXEC pidfd for that exact
    /// PID before any owner-loss-dependent state exists. A missing or
    /// non-CLOEXEC pidfd terminates the owner and refuses (missing-pidfd
    /// refusal); no PID/`/proc`/journal fallback exists.</summary>
    internal OwnerSpawnResult TrySpawnOwner(string ownerDll, string[] arguments, int ownerSocketFd)
    {
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

        Process? process = null;
        try
        {
            process = Process.Start(psi);
            if (process is null)
            {
                return new OwnerSpawnResult(null, -1, "owner process start returned null");
            }

            if (!_pidFdProvider.TryOpen(process.Id, out var pidFd, out var pidError))
            {
                TerminateOwner(process);
                return new OwnerSpawnResult(null, -1, $"owner pidfd unavailable: {pidError}");
            }

            if (!PreparationInterop.HasCloexec(pidFd))
            {
                PreparationInterop.CloseFd(pidFd);
                TerminateOwner(process);
                return new OwnerSpawnResult(null, -1, "owner pidfd is missing FD_CLOEXEC");
            }

            var handle = new SafeFileHandle(new IntPtr(pidFd), ownsHandle: true);
            _retainedOwnerPidFds[process.Id] = handle;
            _owners[process.Id] = process;
            return new OwnerSpawnResult(process, pidFd, string.Empty);
        }
        catch (Exception ex)
        {
            if (process is not null)
            {
                TerminateOwner(process);
            }

            return new OwnerSpawnResult(null, -1, $"owner start failed: {ex.GetType().Name}");
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

    /// <summary>Runs one owner-owned allocation/preparation transaction.
    /// This scope writes only the initial durable intent, relinquishes the
    /// journal fence, spawns the issuer, and then retains/acknowledges
    /// recovery authority through the private protocol. It never runs
    /// SafeCopyIn, owns a PreparationStore, or receives a capability.</summary>
    internal OwnerPreparationOutcome StartOwnerPreparation(
        OwnerPreparationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (_disposed || _parentFd < 0 || !_subreaperEnabled)
        {
            return new OwnerPreparationOutcome(false, null, null, null, "recovery scope is not initialized");
        }

        if (!ReferenceEquals(request.Writer.Journal, request.Journal)
            || !request.Writer.IsLive
            || request.Writer.Directory.Length == 0)
        {
            return new OwnerPreparationOutcome(false, null, null, null, "scope does not hold the matching live intent writer");
        }

        var openErrno = LibcStagingParent(request.Plan.StagingParentPath, out var stagingParent);
        if (openErrno != 0 || stagingParent is null)
        {
            return new OwnerPreparationOutcome(false, null, null, null, $"staging parent open errno {openErrno}");
        }

        if (PreparationInterop.StatFd(stagingParent.DangerousGetHandle().ToInt32(), out var stagingStat) != 0)
        {
            stagingParent.Dispose();
            return new OwnerPreparationOutcome(false, null, null, null, "staging parent identity unavailable");
        }

        _retainedStagingParents.Add(stagingParent);
        var specification = new OwnerPreparationSpecification(
            request.Writer.Directory,
            request.DomainChildName,
            request.ExecutionId,
            request.LeaseGeneration,
            request.BootId,
            request.IssuerId,
            request.StoreGenerationId,
            request.Plan.SourceRootPath,
            request.Plan.StagingParentPath,
            stagingStat.Device,
            stagingStat.Inode,
            request.Plan.StagingDirectoryName,
            request.Plan.Objects,
            request.Plan.TotalBudgetBytes,
            request.Plan.MaxObjects,
            request.Plan.Environment ?? Array.Empty<KeyValuePair<string, string>>(),
            request.ExpiresAtNs,
            request.TestBarrier);

        byte[] encodedSpecification;
        OwnershipSnapshot intent;
        try
        {
            encodedSpecification = OwnerPreparationSpecificationCodec.Encode(specification);
            intent = OwnerPreparationSpecificationCodec.BuildIntent(specification);
        }
        catch (ArgumentException ex)
        {
            return new OwnerPreparationOutcome(false, null, null, null, $"owner request refused: {ex.Message}");
        }

        var intentWrite = request.Journal.WriteNext(request.Writer, intent);
        if (!intentWrite.Ok)
        {
            return new OwnerPreparationOutcome(false, null, null, null, $"AllocationIntent not durable: {intentWrite.Error}");
        }

        // Exact writer handoff: release this real flock before the owner is
        // launched. There is no interval with two usable writer leases.
        request.Writer.Dispose();

        if (!FdPassing.TryCreateInheritableSocketPair(out var parentSocket, out var childSocket, out var socketError))
        {
            return new OwnerPreparationOutcome(false, null, null, null, socketError);
        }

        var spawned = TrySpawnOwner(
            request.OwnerDllPath,
            new[] { "--exec", request.ExecutionId.ToString("D"), "--lease", request.LeaseGeneration.ToString("D") },
            childSocket);
        if (!spawned.Ok || spawned.Process is null)
        {
            PreparationInterop.CloseFd(parentSocket);
            return new OwnerPreparationOutcome(false, null, null, null, spawned.Error);
        }

        var owner = spawned.Process;
        var channel = new OwnerProtocolChannel(
            parentSocket, request.ExecutionId, request.LeaseGeneration, ownsSocket: true);
        _ownerChannels[owner.Id] = channel;
        var operationError = string.Empty;
        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return FailOwnerPreparation(owner, channel, "owner preparation cancelled before start");
            }

            if (!channel.TrySend(OwnerProtocolMessageType.Start, encodedSpecification, out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"owner start frame failed: {operationError}");
            }

            if (!ReceiveControlOrBarrier(channel, OwnerProtocolMessageType.WriterReady, out var message, out var barrier, out operationError))
            {
                return FailOwnerPreparation(owner, channel, operationError);
            }

            if (barrier is not null)
            {
                return new OwnerPreparationOutcome(false, null, owner, barrier, string.Empty);
            }

            var parentDuplicate = PreparationInterop.Dup(_parentFd);
            var stagingDuplicate = PreparationInterop.Dup(stagingParent.DangerousGetHandle().ToInt32());
            if (parentDuplicate < 0 || stagingDuplicate < 0)
            {
                if (parentDuplicate >= 0) PreparationInterop.CloseFd(parentDuplicate);
                if (stagingDuplicate >= 0) PreparationInterop.CloseFd(stagingDuplicate);
                return FailOwnerPreparation(owner, channel, "owner authority descriptor duplication failed");
            }

            var startAuthoritySent = channel.TrySendTransfer(
                OwnerProtocolMessageType.StartAuthority,
                new byte[] { 0, 1 },
                new[] { parentDuplicate, stagingDuplicate },
                out operationError);
            PreparationInterop.CloseFd(parentDuplicate);
            PreparationInterop.CloseFd(stagingDuplicate);
            if (!startAuthoritySent)
            {
                return FailOwnerPreparation(owner, channel, $"owner authority transfer failed: {operationError}");
            }

            if (!ReceiveTransferOrBarrier(
                    channel,
                    OwnerProtocolMessageType.DomainRecoveryAuthority,
                    DomainHandleTransfer.ExactDescriptorCount,
                    1,
                    out var domainMessage,
                    out var recoveryFds,
                    out barrier,
                    out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"domain recovery authority transfer failed: {operationError}");
            }

            if (barrier is not null)
            {
                return new OwnerPreparationOutcome(false, null, owner, barrier, string.Empty);
            }

            if (domainMessage!.Payload[0] != DomainHandleTransfer.ExactDescriptorCount)
            {
                CloseReceived(recoveryFds);
                return FailOwnerPreparation(owner, channel, "scope rejected owner domain authority role count");
            }

            if (!CgroupInterop.TryValidateDomainHandles(
                    recoveryFds, request.DomainChildName, request.ExecutionId, request.LeaseGeneration,
                    request.BootId, out var recoveryBackend, out var domainIdentity, out var domainError))
            {
                CloseReceived(recoveryFds);
                return FailOwnerPreparation(owner, channel, $"scope rejected owner domain authority: {domainError}");
            }

            var recoveryLease = new DomainLease(recoveryBackend!, domainIdentity!);
            _retainedRecoveryLeases.Add(recoveryLease);
            if (!channel.TrySend(OwnerProtocolMessageType.DomainAuthorityRetained, ReadOnlySpan<byte>.Empty, out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"domain retention acknowledgement failed: {operationError}");
            }

            if (!ReceiveControlOrBarrier(channel, OwnerProtocolMessageType.DomainRegistered,
                    out message, out barrier, out operationError))
            {
                return FailOwnerPreparation(owner, channel, operationError);
            }

            if (barrier is not null)
            {
                return new OwnerPreparationOutcome(false, null, owner, barrier, string.Empty);
            }

            var registeredRead = request.Journal.ReadLatest();
            if (!registeredRead.Ok || registeredRead.Snapshot is null
                || !IsExpectedRegistered(registeredRead.Snapshot, specification, domainIdentity!))
            {
                return FailOwnerPreparation(owner, channel, "owner AllocationRegistered state did not match retained domain authority");
            }

            if (!channel.TrySend(OwnerProtocolMessageType.DomainRegistrationRetained, ReadOnlySpan<byte>.Empty, out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"registered-domain acknowledgement failed: {operationError}");
            }

            if (!ReceiveTransferOrBarrier(
                    channel,
                    OwnerProtocolMessageType.StagingAuthority,
                    1,
                    16,
                    out var stagingMessage,
                    out var stagingFds,
                    out barrier,
                    out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"staging authority transfer failed: {operationError}");
            }

            if (barrier is not null)
            {
                return new OwnerPreparationOutcome(false, null, owner, barrier, string.Empty);
            }

            var stagingDevice = BinaryPrimitives.ReadUInt64LittleEndian(stagingMessage!.Payload.AsSpan(0, 8));
            var stagingInode = BinaryPrimitives.ReadUInt64LittleEndian(stagingMessage.Payload.AsSpan(8, 8));
            if (PreparationInterop.StatFd(stagingFds[0], out var receivedStagingStat) != 0
                || !PreparationInterop.IsDirectory(receivedStagingStat.Mode)
                || receivedStagingStat.Device != stagingDevice
                || receivedStagingStat.Inode != stagingInode)
            {
                CloseReceived(stagingFds);
                return FailOwnerPreparation(owner, channel, "received staging authority identity mismatch");
            }

            _retainedStagingDirs.Add(new SafeFileHandle(new IntPtr(stagingFds[0]), ownsHandle: true));
            if (request.WithholdStagingRetentionAckForTest)
            {
                return new OwnerPreparationOutcome(false, null, owner, null, "test withheld staging retention acknowledgement");
            }
            if (!channel.TrySend(OwnerProtocolMessageType.StagingAuthorityRetained, ReadOnlySpan<byte>.Empty, out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"staging retention acknowledgement failed: {operationError}");
            }

            if (!ReceiveControlOrBarrier(channel, OwnerProtocolMessageType.StagingAccounted,
                    out message, out barrier, out operationError))
            {
                return FailOwnerPreparation(owner, channel, operationError);
            }

            if (barrier is not null)
            {
                return new OwnerPreparationOutcome(false, null, owner, barrier, string.Empty);
            }

            if (message!.Payload.Length != 16
                || BinaryPrimitives.ReadUInt64LittleEndian(message.Payload.AsSpan(0, 8)) != stagingDevice
                || BinaryPrimitives.ReadUInt64LittleEndian(message.Payload.AsSpan(8, 8)) != stagingInode
                || !HasDurableStagingDirectory(request.Journal, specification, stagingDevice, stagingInode))
            {
                return FailOwnerPreparation(owner, channel, "staging identity was not durably accounted before continuation");
            }

            if (!channel.TrySend(OwnerProtocolMessageType.StagingMayProceed, ReadOnlySpan<byte>.Empty, out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"staging continuation acknowledgement failed: {operationError}");
            }

            if (!ReceiveControlOrBarrier(channel, OwnerProtocolMessageType.StagedObjects,
                    out message, out barrier, out operationError))
            {
                return FailOwnerPreparation(owner, channel, operationError);
            }

            if (barrier is not null)
            {
                return new OwnerPreparationOutcome(false, null, owner, barrier, string.Empty);
            }

            if (!OwnerPreparationSpecificationCodec.TryDecodeStagedObjects(
                    message!.Payload, out var stagedDescriptions, out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"owner staged-object description refused: {operationError}");
            }

            (RestrictedManifest Manifest, PreparationIdentity Identity) authorization;
            try
            {
                authorization = request.ManifestFactory(stagedDescriptions!);
            }
            catch (Exception ex)
            {
                return FailOwnerPreparation(owner, channel, $"manifest authorization failed: {ex.GetType().Name}");
            }

            if (authorization.Identity.ExecutionId != request.ExecutionId
                || authorization.Identity.IssuerId != request.IssuerId
                || authorization.Identity.GenerationId != request.StoreGenerationId
                || !authorization.Identity.ManifestDigest.AsSpan()
                    .SequenceEqual(ManifestCodec.ComputeDigestBytes(authorization.Manifest)))
            {
                return FailOwnerPreparation(owner, channel, "manifest authorization identity is not bound to this transaction");
            }

            var authorizationPayload = OwnerManifestAuthorizationCodec.Encode(
                new OwnerManifestAuthorization(authorization.Manifest, authorization.Identity));
            if (!channel.TrySend(OwnerProtocolMessageType.ManifestAuthorization, authorizationPayload, out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"manifest authorization transfer failed: {operationError}");
            }

            if (!ReceiveControlOrBarrier(channel, OwnerProtocolMessageType.PreparedDescription,
                    out message, out barrier, out operationError))
            {
                return FailOwnerPreparation(owner, channel, operationError);
            }

            if (barrier is not null)
            {
                return new OwnerPreparationOutcome(false, null, owner, barrier, string.Empty);
            }

            if (!OwnerPreparationSpecificationCodec.TryDecodePreparedDescription(
                    message!.Payload, out var preparedDescription, out operationError)
                || !MatchesPrepared(request.Journal, specification, preparedDescription!, authorization.Identity))
            {
                return FailOwnerPreparation(owner, channel, "Prepared description does not match the durable journal state");
            }

            if (!channel.TrySend(OwnerProtocolMessageType.PreparedAcknowledgement, ReadOnlySpan<byte>.Empty, out operationError))
            {
                return FailOwnerPreparation(owner, channel, $"Prepared acknowledgement failed: {operationError}");
            }

            if (!ReceiveControlOrBarrier(channel, OwnerProtocolMessageType.Completed,
                    out message, out barrier, out operationError))
            {
                return FailOwnerPreparation(owner, channel, operationError);
            }

            if (barrier is not null)
            {
                return new OwnerPreparationOutcome(false, preparedDescription, owner, barrier, string.Empty);
            }

            if (!OwnerPreparationSpecificationCodec.TryDecodePreparedDescription(
                    message!.Payload, out var completedDescription, out operationError)
                || !SameDescription(completedDescription!, preparedDescription!))
            {
                return FailOwnerPreparation(owner, channel, $"owner completion frame refused: {operationError}");
            }

            return new OwnerPreparationOutcome(true, preparedDescription, owner, null, string.Empty);
        }
        catch (Exception ex)
        {
            return FailOwnerPreparation(owner, channel, $"owner protocol failed: {ex.GetType().Name}");
        }
    }

    private OwnerPreparationOutcome FailOwnerPreparation(Process owner, OwnerProtocolChannel channel, string error)
    {
        TerminateOwner(owner);
        var exit = WaitForOwner(owner, TimeSpan.FromSeconds(5));
        if (!exit.Exited)
        {
            error = $"{error}; owner exit proof failed: {exit.Error}";
        }

        channel.Dispose();
        _ownerChannels.Remove(owner.Id);
        return new OwnerPreparationOutcome(false, null, owner, null, error);
    }

    private static bool ReceiveControlOrBarrier(
        OwnerProtocolChannel channel,
        OwnerProtocolMessageType expected,
        out OwnerProtocolMessage? message,
        out string? barrier,
        out string error)
    {
        barrier = null;
        if (!channel.TryReceiveAllowed(
                out message, out error,
                expected,
                OwnerProtocolMessageType.BarrierReached,
                OwnerProtocolMessageType.Aborted))
        {
            return false;
        }

        if (message!.Type == OwnerProtocolMessageType.Aborted)
        {
            error = Encoding.UTF8.GetString(message.Payload);
            return false;
        }

        if (message.Type == OwnerProtocolMessageType.BarrierReached)
        {
            barrier = DecodeBarrier(message.Payload, out error);
            return barrier is not null;
        }

        if (message.Type != expected)
        {
            error = $"unexpected owner transition {message.Type}; expected {expected}";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool ReceiveTransferOrBarrier(
        OwnerProtocolChannel channel,
        OwnerProtocolMessageType expectedTransferType,
        int expectedDescriptorCount,
        int expectedPayloadBytes,
        out OwnerProtocolMessage? message,
        out int[] descriptors,
        out string? barrier,
        out string error)
    {
        barrier = null;
        if (!channel.TryReceiveTransferOrControl(
                expectedTransferType, expectedDescriptorCount, expectedPayloadBytes,
                out message, out descriptors, out error))
        {
            return false;
        }

        if (message!.Type == OwnerProtocolMessageType.Aborted)
        {
            error = Encoding.UTF8.GetString(message.Payload);
            if (string.IsNullOrWhiteSpace(error))
            {
                error = "owner aborted the preparation transaction";
            }

            return false;
        }

        if (message.Type == OwnerProtocolMessageType.BarrierReached)
        {
            barrier = DecodeBarrier(message.Payload, out error);
            return barrier is not null;
        }

        error = string.Empty;
        return true;
    }

    private static string? DecodeBarrier(byte[] payload, out string error)
    {
        var value = Encoding.UTF8.GetString(payload);
        if (value is not ("after-intent" or "after-domain-retained" or "after-registered"
            or "after-staging-accounted" or "mid-staging" or "after-copy" or "after-prepared-before-mint"))
        {
            error = "owner sent an unknown test barrier";
            return null;
        }

        error = string.Empty;
        return value;
    }

    private static bool IsExpectedRegistered(
        OwnershipSnapshot snapshot,
        OwnerPreparationSpecification specification,
        DomainIdentity identity)
    {
        if (snapshot.ExecutionId != specification.ExecutionId
            || snapshot.LeaseGeneration != specification.LeaseGeneration
            || snapshot.BootId != specification.BootId
            || snapshot.DomainName != specification.DomainChildName
            || snapshot.Stage != PreparationStage.AllocationRegistered
            || snapshot.Writer != OwnershipWriterKind.Owner
            || snapshot.Cleanup != OwnershipCleanupState.NotStarted
            || snapshot.DomainDevice != identity.ChildDevice
            || snapshot.DomainInode != identity.ChildInode
            || snapshot.CgroupMountId != identity.CgroupMountId
            || snapshot.CgroupMountFsType != identity.CgroupMountFsType
            || snapshot.ParentDevice != identity.ParentDevice
            || snapshot.ParentInode != identity.ParentInode
            || snapshot.StagingParentDevice != specification.StagingParentDevice
            || snapshot.StagingParentInode != specification.StagingParentInode)
        {
            return false;
        }

        var domain = snapshot.Allocations.SingleOrDefault(item => item.Kind == AllocationKind.Domain);
        return domain is { Confirmed: true }
            && domain.Name == specification.DomainChildName
            && domain.Device == identity.ChildDevice
            && domain.Inode == identity.ChildInode;
    }

    private static bool HasDurableStagingDirectory(
        OwnershipJournal journal,
        OwnerPreparationSpecification specification,
        ulong device,
        ulong inode)
    {
        var read = journal.ReadLatest();
        if (!read.Ok || read.Snapshot is null
            || read.Snapshot.ExecutionId != specification.ExecutionId
            || read.Snapshot.LeaseGeneration != specification.LeaseGeneration
            || read.Snapshot.Stage != PreparationStage.AllocationRegistered
            || read.Snapshot.StagingParentDevice != specification.StagingParentDevice
            || read.Snapshot.StagingParentInode != specification.StagingParentInode)
        {
            return false;
        }

        var entry = read.Snapshot.Allocations.SingleOrDefault(item => item.Kind == AllocationKind.StagingDirectory);
        return entry is { Confirmed: true }
            && entry.Name == specification.StagingDirectoryName
            && entry.Device == device
            && entry.Inode == inode;
    }

    private static bool MatchesPrepared(
        OwnershipJournal journal,
        OwnerPreparationSpecification specification,
        OwnerPreparedDescription description,
        PreparationIdentity authorizedIdentity)
    {
        var read = journal.ReadLatest();
        var recorded = read.Snapshot?.Preparation;
        return read.Ok && read.Snapshot is not null
            && read.Snapshot.Stage == PreparationStage.Prepared
            && read.Snapshot.Writer == OwnershipWriterKind.Owner
            && read.Snapshot.ExecutionId == specification.ExecutionId
            && read.Snapshot.LeaseGeneration == specification.LeaseGeneration
            && description.ExecutionId == specification.ExecutionId
            && description.IssuerId == authorizedIdentity.IssuerId
            && description.GenerationId == authorizedIdentity.GenerationId
            && description.PreparationId == authorizedIdentity.PreparationId
            && description.ManifestDigest.AsSpan().SequenceEqual(authorizedIdentity.ManifestDigest.AsSpan())
            && recorded is not null
            && recorded.ExecutionId == description.ExecutionId
            && recorded.IssuerId == description.IssuerId
            && recorded.GenerationId == description.GenerationId
            && recorded.PreparationId == description.PreparationId
            && recorded.ManifestDigest.AsSpan().SequenceEqual(description.ManifestDigest);
    }

    private static bool SameDescription(OwnerPreparedDescription left, OwnerPreparedDescription right) =>
        left.ExecutionId == right.ExecutionId
        && left.IssuerId == right.IssuerId
        && left.GenerationId == right.GenerationId
        && left.PreparationId == right.PreparationId
        && left.ManifestDigest.AsSpan().SequenceEqual(right.ManifestDigest);

    private static void CloseReceived(IEnumerable<int> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            PreparationInterop.CloseFd(descriptor);
        }
    }

    private static void TerminateOwner(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: false);
                process.WaitForExit(5000);
            }
        }
        catch
        {
            // Best-effort containment; the pidfd refusal is already the
            // reported outcome.
        }
    }

    /// <summary>Polls the retained pidfd for the owner without reaping.
    /// True only with a retained pidfd; a missing pidfd is a refusal, never
    /// an assumed exit.</summary>
    internal bool TryOwnerPidFdExited(Process owner, int timeoutMilliseconds, out bool exited, out string error)
    {
        exited = false;
        error = string.Empty;
        if (!_retainedOwnerPidFds.TryGetValue(owner.Id, out var handle) || handle.IsClosed)
        {
            error = "no retained owner pidfd";
            return false;
        }

        return PidFd.TryExited(
            handle.DangerousGetHandle().ToInt32(), timeoutMilliseconds, out exited, out error);
    }

    /// <summary>Waits for the direct-child owner to exit within a bound.
    /// Exit is established via the retained pidfd and then the direct child
    /// is reaped exactly once. A missing pidfd refuses owner-loss takeover.</summary>
    internal OwnerExitResult WaitForOwner(Process owner, TimeSpan timeout)
    {
        if (!_retainedOwnerPidFds.TryGetValue(owner.Id, out var handle) || handle.IsClosed)
        {
            return new OwnerExitResult(false, -1, false, "no retained owner pidfd; owner-loss recovery not permitted");
        }

        var timeoutMs = (int)timeout.TotalMilliseconds;
        if (!PidFd.TryExited(handle.DangerousGetHandle().ToInt32(), timeoutMs, out var exited, out var pollError))
        {
            return new OwnerExitResult(false, -1, false, pollError);
        }

        if (!exited)
        {
            return new OwnerExitResult(false, -1, false, "owner pidfd did not signal exit within the bound");
        }

        if (!owner.WaitForExit(timeoutMs))
        {
            return new OwnerExitResult(false, -1, false, "owner was not reapable within the bound");
        }

        var crashed = owner.ExitCode != 0;
        return new OwnerExitResult(true, owner.ExitCode, crashed, string.Empty);
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
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var channel in _ownerChannels.Values)
        {
            channel.Dispose();
        }

        _ownerChannels.Clear();
        foreach (var (ownerId, owner) in _owners.ToArray())
        {
            var exit = WaitForOwner(owner, TimeSpan.FromSeconds(30));
            if (!exit.Exited)
            {
                TerminateOwner(owner);
                exit = WaitForOwner(owner, TimeSpan.FromSeconds(5));
            }

            if (_retainedOwnerPidFds.Remove(ownerId, out var ownerPidFd))
            {
                ownerPidFd.Dispose();
            }

            owner.Dispose();
        }

        _owners.Clear();
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
        foreach (var handle in _retainedOwnerPidFds.Values)
        {
            handle.Dispose();
        }

        _retainedOwnerPidFds.Clear();
        if (_parentFd >= 0)
        {
            PreparationInterop.CloseFd(_parentFd);
            _parentFd = -1;
        }

        GC.SuppressFinalize(this);
    }
}
