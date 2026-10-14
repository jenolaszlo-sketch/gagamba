using System.Buffers.Binary;
using System.Diagnostics;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

/// <summary>The one authoritative transaction implementation, called only
/// by the owner executable. The recovery scope participates through the
/// bounded protocol but never receives the store, capability, primary
/// domain lease or transaction writer.</summary>
internal static class OwnerPreparationTransaction
{
    private sealed class OwnerClock : ITransactionClock
    {
        public long NowNanoseconds
        {
            get
            {
                var timestamp = Stopwatch.GetTimestamp();
                return (timestamp / Stopwatch.Frequency * 1_000_000_000L)
                    + (timestamp % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency);
            }
        }
    }

    private sealed class OwnerDomainClock : IDomainClock
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        public long NowNanoseconds => _watch.ElapsedTicks * (1_000_000_000L / Stopwatch.Frequency);
        public void Delay(TimeSpan duration) => Thread.Sleep(duration);
    }

    internal static int Run(int controlFd, Guid executionId, Guid leaseGeneration)
    {
        using var channel = new OwnerProtocolChannel(controlFd, executionId, leaseGeneration, ownsSocket: true);
        if (!channel.TryReceive(OwnerProtocolMessageType.Start, out var start, out var startError)
            || !OwnerPreparationSpecificationCodec.TryDecode(start!.Payload, out var request, out startError))
        {
            SendAbort(channel, startError);
            return 20;
        }

        if (request!.ExecutionId != executionId || request.LeaseGeneration != leaseGeneration)
        {
            SendAbort(channel, "start request identity does not match the owner bootstrap");
            return 20;
        }

        return RunTransaction(request, channel);
    }

    private static int RunTransaction(OwnerPreparationSpecification request, OwnerProtocolChannel channel)
    {
        using var storage = new JournalFileStorage();
        var journal = new OwnershipJournal(storage, request.JournalDirectory);
        if (!journal.TryAcquireWriter(out var writer, out var writerError))
        {
            SendAbort(channel, $"owner writer acquisition failed: {writerError}");
            return 21;
        }

        var activeWriter = writer!;
        using (activeWriter)
        {
            if (!CgroupInterop.TryReadBootId(out var actualBootId, out var bootError)
                || actualBootId != request.BootId)
            {
                SendAbort(channel, $"owner boot identity refused: {bootError}");
                return 22;
            }

            var expectedIntent = OwnerPreparationSpecificationCodec.BuildIntent(request);
            if (!journal.TryProveAllocationIntent(activeWriter, expectedIntent, out _, out var intentError))
            {
                SendAbort(channel, $"owner AllocationIntent proof failed: {intentError}");
                return 23;
            }

            if (!channel.TrySend(OwnerProtocolMessageType.WriterReady, ReadOnlySpan<byte>.Empty, out var channelError))
            {
                return 24;
            }

            if (!channel.TryReceiveTransfer(
                    OwnerProtocolMessageType.StartAuthority,
                    expectedDescriptorCount: 2,
                    expectedPayloadBytes: 2,
                    out var authorityMessage,
                    out var authorityFds,
                    out channelError))
            {
                SendAbort(channel, channelError);
                return 26;
            }

            if (authorityMessage!.Payload[0] != 0 || authorityMessage.Payload[1] != 1)
            {
                CloseAll(authorityFds);
                SendAbort(channel, "owner bootstrap descriptor roles are invalid");
                return 27;
            }

            using var parent = new SafeFileHandle(new IntPtr(authorityFds[0]), ownsHandle: true);
            using var stagingParent = new SafeFileHandle(new IntPtr(authorityFds[1]), ownsHandle: true);
            if (PreparationInterop.StatFd(parent.DangerousGetHandle().ToInt32(), out var parentStat) != 0
                || !PreparationInterop.IsDirectory(parentStat.Mode)
                || PreparationInterop.StatFd(stagingParent.DangerousGetHandle().ToInt32(), out var stagingParentStat) != 0
                || !PreparationInterop.IsDirectory(stagingParentStat.Mode)
                || stagingParentStat.Device != request.StagingParentDevice
                || stagingParentStat.Inode != request.StagingParentInode)
            {
                SendAbort(channel, "retained owner parent authority identity mismatch");
                return 28;
            }

            // D1 is after the owner has accepted StartAuthority but before
            // allocation context creation or domain creation. The owner
            // keeps the transaction writer while the scope observes this
            // barrier, so a real owner death releases the writer fence.
            if (!WaitAtBarrier(request, channel, "after-intent", out channelError))
            {
                return 25;
            }

            if (!ScopedAllocationContext.TryCreate(
                    parent.DangerousGetHandle().ToInt32(), journal, activeWriter, expectedIntent,
                    request.ExecutionId, request.LeaseGeneration, request.BootId,
                    out var allocationContext, out var contextError))
            {
                SendAbort(channel, $"scoped allocation context refused: {contextError}");
                return 29;
            }

            if (!CgroupInterop.TryCreateDomain(allocationContext!, request.DomainChildName,
                    out var primaryBackend, out var domainIdentity, out var domainError))
            {
                SendAbort(channel, $"owner domain creation failed: {domainError}");
                return 30;
            }

            using var primaryLease = new DomainLease(primaryBackend!, domainIdentity!);
            var recoveryBackend = (CgroupBackend)primaryBackend!.DuplicateForRecovery();
            var domainPayload = new byte[] { checked((byte)DomainHandleTransfer.ExactDescriptorCount) };
            var domainSent = channel.TrySendTransfer(
                OwnerProtocolMessageType.DomainRecoveryAuthority,
                domainPayload,
                recoveryBackend.OrderedFds(),
                out channelError);
            recoveryBackend.Dispose();
            if (!domainSent)
            {
                SendAbort(channel, $"recovery domain transfer failed: {channelError}");
                return 31;
            }

            if (!channel.TryReceive(OwnerProtocolMessageType.DomainAuthorityRetained, out _, out channelError))
            {
                SendAbort(channel, $"scope did not retain recovery domain authority: {channelError}");
                return 32;
            }

            if (!WaitAtBarrier(request, channel, "after-domain-retained", out channelError))
            {
                return 33;
            }

            var registered = BuildRegistered(request, domainIdentity!, stagingParentStat.Device, stagingParentStat.Inode);
            if (!journal.WriteNext(activeWriter, registered).Ok)
            {
                SendAbort(channel, "AllocationRegistered was not durably acknowledged");
                return 34;
            }

            if (!channel.TrySend(OwnerProtocolMessageType.DomainRegistered, ReadOnlySpan<byte>.Empty, out channelError)
                || !channel.TryReceive(OwnerProtocolMessageType.DomainRegistrationRetained, out _, out channelError))
            {
                SendAbort(channel, $"scope did not confirm registered domain state: {channelError}");
                return 35;
            }

            if (!WaitAtBarrier(request, channel, "after-registered", out channelError))
            {
                return 36;
            }

            var accounting = new OwnerAccounting(journal, activeWriter, registered);
            var stageDirectoryAuthorized = false;
            var accountedObjects = 0;

            var copyPlan = new CopyPlan(
                request.SourceRootPath,
                request.StagingParentPath,
                request.StagingDirectoryName,
                request.Objects,
                request.TotalBudgetBytes,
                request.MaxObjects,
                request.Environment,
                Acknowledge: null,
                Hooks: null,
                OnObjectAccounted: (_, staged) =>
                {
                    accounting.OnObject(staged);
                    accountedObjects++;
                    if (accountedObjects == 1
                        && !WaitAtBarrier(request, channel, "mid-staging", out var barrierError))
                    {
                        throw new IOException(barrierError);
                    }
                },
                OnStagingDirectoryCreated: stagingDirectory =>
                {
                    if (stageDirectoryAuthorized)
                    {
                        throw new InvalidOperationException("staging directory authority was transferred twice");
                    }

                    if (PreparationInterop.StatFd(stagingDirectory.DangerousGetHandle().ToInt32(), out var stat) != 0)
                    {
                        throw new InvalidOperationException("staging directory identity unavailable");
                    }

                    var identityPayload = new byte[16];
                    BinaryPrimitives.WriteUInt64LittleEndian(identityPayload.AsSpan(0, 8), stat.Device);
                    BinaryPrimitives.WriteUInt64LittleEndian(identityPayload.AsSpan(8, 8), stat.Inode);
                    var descriptor = stagingDirectory.DangerousGetHandle().ToInt32();
                    if (!channel.TrySendTransfer(
                            OwnerProtocolMessageType.StagingAuthority,
                            identityPayload,
                            new[] { descriptor },
                            out var transferError)
                        || !channel.TryReceive(
                            OwnerProtocolMessageType.StagingAuthorityRetained,
                            out _, out transferError))
                    {
                        throw new InvalidOperationException($"scope staging authority retention failed: {transferError}");
                    }

                    var durable = accounting.OnStagingDirectory(stat.Device, stat.Inode);
                    if (!durable)
                    {
                        throw new InvalidOperationException("staging directory accounting was not durable");
                    }

                    var acknowledged = new byte[16];
                    BinaryPrimitives.WriteUInt64LittleEndian(acknowledged.AsSpan(0, 8), stat.Device);
                    BinaryPrimitives.WriteUInt64LittleEndian(acknowledged.AsSpan(8, 8), stat.Inode);
                    if (!channel.TrySend(OwnerProtocolMessageType.StagingAccounted, acknowledged, out transferError)
                        || !channel.TryReceive(OwnerProtocolMessageType.StagingMayProceed, out _, out transferError))
                    {
                        throw new InvalidOperationException($"scope did not acknowledge durable staging identity: {transferError}");
                    }

                    stageDirectoryAuthorized = true;
                    if (!WaitAtBarrier(request, channel, "after-staging-accounted", out transferError))
                    {
                        throw new IOException(transferError);
                    }
                },
                StagingParentAuthority: stagingParent);

            var copy = SafeCopyIn.Prepare(copyPlan, LibcCopyHost.Instance, CancellationToken.None);
            if (!copy.Succeeded || copy.Closure is null)
            {
                SendAbort(channel, $"owner SafeCopyIn failed: {copy.Failure}/{copy.Detail}");
                copy.Closure?.Dispose();
                return 37;
            }

            using var closure = copy.Closure;
            if (!WaitAtBarrier(request, channel, "after-copy", out channelError))
            {
                return 37;
            }

            var descriptions = closure.Objects.Select(staged => new StagedObjectDescription(
                staged.Role, staged.Sha256, staged.Length, staged.Device, staged.Inode, staged.StagingName)).ToArray();
            var stagedPayload = OwnerPreparationSpecificationCodec.EncodeStagedObjects(descriptions);
            if (!channel.TrySend(OwnerProtocolMessageType.StagedObjects, stagedPayload, out channelError)
                || !channel.TryReceive(OwnerProtocolMessageType.ManifestAuthorization, out var authMessage, out channelError)
                || !OwnerManifestAuthorizationCodec.TryDecode(authMessage!.Payload, out var authorization, out channelError))
            {
                SendAbort(channel, $"manifest authorization failed: {channelError}");
                return 38;
            }

            var manifest = authorization!.Manifest;
            var identity = authorization.Identity;
            if (identity.ExecutionId != request.ExecutionId
                || identity.IssuerId != request.IssuerId
                || identity.GenerationId != request.StoreGenerationId)
            {
                SendAbort(channel, "manifest authorization identity does not match the owner transaction");
                return 39;
            }

            var description = new OwnerPreparedDescription(
                identity.ExecutionId, identity.IssuerId, identity.GenerationId,
                identity.PreparationId, identity.ManifestDigest.ToArray());
            var preparedRecord = accounting.Current with
            {
                Stage = PreparationStage.Prepared,
                Preparation = new RecordedPreparation(
                    identity.ExecutionId, identity.IssuerId, identity.GenerationId,
                    identity.PreparationId, identity.ManifestDigest.ToArray()),
            };
            if (!journal.WriteNext(activeWriter, preparedRecord).Ok)
            {
                SendAbort(channel, "Prepared was not durably acknowledged");
                return 40;
            }

            var encodedDescription = OwnerPreparationSpecificationCodec.EncodePreparedDescription(description);
            if (!channel.TrySend(OwnerProtocolMessageType.PreparedDescription, encodedDescription, out channelError)
                || !channel.TryReceive(OwnerProtocolMessageType.PreparedAcknowledgement, out _, out channelError))
            {
                SendAbort(channel, $"scope did not acknowledge Prepared: {channelError}");
                return 41;
            }

            if (!WaitAtBarrier(request, channel, "after-prepared-before-mint", out channelError))
            {
                return 42;
            }

            var store = new PreparedTransaction.PreparationStore(
                request.IssuerId, request.StoreGenerationId, new OwnerClock());
            var publication = PreparationPublisher.Publish(
                store, closure, manifest, identity, request.ExpiresAtNs,
                _ => true, CancellationToken.None);
            if (!publication.Published || publication.Capability is null)
            {
                SendAbort(channel, $"owner Mint failed: {publication.Failure}/{publication.Detail}");
                return 43;
            }

            if (!channel.TrySend(OwnerProtocolMessageType.Completed, encodedDescription, out channelError))
            {
                return 44;
            }

            // Keep the store, capability, primary domain lease and exclusive
            // writer in this process for exactly the recovery-scope lifetime.
            var scopeAlive = channel.WaitForScopeLifetime(out _);
            GC.KeepAlive(publication.Capability);
            GC.KeepAlive(store);
            GC.KeepAlive(closure);
            GC.KeepAlive(primaryLease);
            GC.KeepAlive(activeWriter);
            return scopeAlive ? 0 : 45;
        }
    }

    private static OwnershipSnapshot BuildRegistered(
        OwnerPreparationSpecification request,
        DomainIdentity domain,
        ulong stagingParentDevice,
        ulong stagingParentInode)
    {
        var allocations = OwnerPreparationSpecificationCodec.PlannedAllocations(request).ToList();
        for (var index = 0; index < allocations.Count; index++)
        {
            if (allocations[index].Kind == AllocationKind.Domain)
            {
                allocations[index] = allocations[index] with
                {
                    Device = domain.ChildDevice,
                    Inode = domain.ChildInode,
                    Confirmed = true,
                };
            }
        }

        return new OwnershipSnapshot(
            OwnershipSnapshotCodec.CurrentVersion, 0, request.ExecutionId, request.BootId,
            domain.CgroupMountId, domain.CgroupMountFsType,
            domain.ParentDevice, domain.ParentInode, domain.ChildName,
            domain.ChildDevice, domain.ChildInode, request.LeaseGeneration,
            OwnershipWriterKind.Owner, OwnershipCleanupState.NotStarted,
            PreparationStage.AllocationRegistered,
            stagingParentDevice, stagingParentInode, allocations, Preparation: null);
    }

    private static bool WaitAtBarrier(
        OwnerPreparationSpecification request,
        OwnerProtocolChannel channel,
        string barrier,
        out string error)
    {
        error = string.Empty;
        if (!string.Equals(request.TestBarrier, barrier, StringComparison.Ordinal))
        {
            return true;
        }

        if (!channel.TrySend(OwnerProtocolMessageType.BarrierReached, Encoding.UTF8.GetBytes(barrier), out error))
        {
            return false;
        }

        return channel.TryReceive(OwnerProtocolMessageType.Continue, out _, out error);
    }

    private static void SendAbort(OwnerProtocolChannel channel, string detail)
    {
        var payload = Encoding.UTF8.GetBytes(detail.Length > 1024 ? detail[..1024] : detail);
        channel.TrySend(OwnerProtocolMessageType.Aborted, payload, out _);
    }

    private static void CloseAll(IEnumerable<int> descriptors)
    {
        foreach (var fd in descriptors)
        {
            PreparationInterop.CloseFd(fd);
        }
    }

    private sealed class OwnerAccounting
    {
        private readonly OwnershipJournal _journal;
        private readonly JournalWriterLease _writer;
        private OwnershipSnapshot _current;

        internal OwnerAccounting(OwnershipJournal journal, JournalWriterLease writer, OwnershipSnapshot registered)
        {
            _journal = journal;
            _writer = writer;
            _current = registered;
        }

        internal OwnershipSnapshot Current => _current;

        internal bool OnStagingDirectory(ulong device, ulong inode)
        {
            var allocations = _current.Allocations.ToList();
            var found = false;
            for (var index = 0; index < allocations.Count; index++)
            {
                if (allocations[index].Kind == AllocationKind.StagingDirectory)
                {
                    allocations[index] = allocations[index] with
                    {
                        Device = device,
                        Inode = inode,
                        Confirmed = true,
                    };
                    found = true;
                }
            }

            if (!found)
            {
                return false;
            }

            var updated = _current with { Allocations = allocations };
            if (!_journal.WriteNext(_writer, updated).Ok)
            {
                return false;
            }

            _current = updated;
            return true;
        }

        internal void OnObject(StagedObject staged)
        {
            var allocations = _current.Allocations.ToList();
            var found = false;
            for (var index = 0; index < allocations.Count; index++)
            {
                if (allocations[index].Kind == AllocationKind.Staging
                    && allocations[index].Name == staged.StagingName)
                {
                    allocations[index] = new AllocationEntry(
                        AllocationKind.Staging, staged.StagingName, staged.Length,
                        (byte[])staged.Sha256.Clone(), staged.Device, staged.Inode, Confirmed: true);
                    found = true;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("staged object is absent from the durable intent");
            }

            var updated = _current with { Allocations = allocations };
            if (!_journal.WriteNext(_writer, updated).Ok)
            {
                throw new InvalidOperationException("staged-object accounting was not durable");
            }

            _current = updated;
        }
    }
}
