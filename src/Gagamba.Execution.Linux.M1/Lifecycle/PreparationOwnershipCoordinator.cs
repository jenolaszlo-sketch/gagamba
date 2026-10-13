// WP03 preparation-ownership integration. This is the seam that replaces
// WP02's fake/delegate preparation acknowledgement with the real durable
// journal and recovery authority, WITHOUT changing WP02 copy/binding. The
// required order is AllocationIntent durable -> staging -> WP02 copy/bind
// -> recovery scope retains cleanup authority -> AllocationRegistered
// durable -> Prepared durable -> PreparationStore.Mint. If the intent
// cannot be made durable before staging, preparation refuses (fail closed).
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Lifecycle;

/// <summary>Recovery authority sink: the scope must retain recovery
/// duplicate handles and staging cleanup authority before registration is
/// durable. Returns false if authority cannot be secured.</summary>
internal interface ICleanupAuthoritySink
{
    bool TryRetainRecoveryHandles(DomainLease recoveryLease, PreparedClosure closure, out string error);
}

internal sealed record PreparedTransactionOutcome(
    bool Published,
    CopyFailureKind Failure,
    string Detail,
    PreparedTransaction? Capability,
    PreparedClosure? Closure);

internal sealed class PreparationOwnershipCoordinator
{
    private readonly OwnershipJournal _journal;
    private readonly OwnershipSnapshot _intentTemplate;

    internal PreparationOwnershipCoordinator(OwnershipJournal journal, OwnershipSnapshot intentTemplate)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _intentTemplate = intentTemplate ?? throw new ArgumentNullException(nameof(intentTemplate));
    }

    /// <summary>Full integrated preparation. The caller holds the exclusive
    /// journal writer lease; every durable record in this path requires it.</summary>
    internal PreparedTransactionOutcome Run(
        ICopyHost copyHost,
        CopyPlan plan,
        DomainLease domain,
        ICleanupAuthoritySink authority,
        JournalWriterLease writer,
        PreparedTransaction.PreparationStore store,
        RestrictedManifest manifest,
        PreparationIdentity identity,
        long expiresAtNs,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(copyHost);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(authority);
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(identity);

        // 1. AllocationIntent must be durable before any staging/domain
        //    allocation. The domain itself is created by the caller before
        //    this call; the intent record already names it. If the durable
        //    write fails, refuse without creating staging.
        var intent = _intentTemplate with
        {
            Writer = OwnershipWriterKind.Owner,
            Cleanup = OwnershipCleanupState.NotStarted,
            Stage = PreparationStage.AllocationIntent,
        };
        var intentWrite = _journal.WriteNext(writer, intent);
        if (!intentWrite.Ok)
        {
            return new PreparedTransactionOutcome(false, CopyFailureKind.PublicationFailure, $"AllocationIntent not durable: {intentWrite.Error}", null, null);
        }

        // 2. WP02 protected copy + binding (unchanged semantics).
        var prepared = SafeCopyIn.Prepare(plan, copyHost, cancellationToken);
        if (!prepared.Succeeded)
        {
            return new PreparedTransactionOutcome(false, prepared.Failure, prepared.Detail, null, prepared.Closure);
        }

        var closure = prepared.Closure!;

        // 3. Acknowledgement replaces the fake delegate: retain recovery
        //    authority, then durably record AllocationRegistered and
        //    Prepared. Any failure publishes nothing and retains accounted
        //    objects for explicit cleanup.
        var ack = PrepareAcknowledgement(domain, authority, writer, closure, identity);
        var published = PreparationPublisher.Publish(
            store, closure, manifest, identity, expiresAtNs, ack, cancellationToken);

        return new PreparedTransactionOutcome(
            published.Published, published.Failure, published.Detail,
            published.Capability, published.Closure);
    }

    private Func<CancellationToken, bool> PrepareAcknowledgement(
        DomainLease domain,
        ICleanupAuthoritySink authority,
        JournalWriterLease writer,
        PreparedClosure closure,
        PreparationIdentity identity)
    {
        return _ =>
        {
            // Retain recovery duplicate handles before registration is durable.
            var recoveryLease = domain.DuplicateForRecovery();
            if (!authority.TryRetainRecoveryHandles(recoveryLease, closure, out var retainError))
            {
                recoveryLease.Dispose();
                return false;
            }

            var allocations = new List<AllocationEntry>
            {
                new(AllocationKind.Domain, domain.Identity.ChildName, 0, Array.Empty<byte>(), domain.Identity.ChildDevice, domain.Identity.ChildInode, true),
            };
            foreach (var staged in closure.Objects)
            {
                allocations.Add(new AllocationEntry(AllocationKind.Staging, staged.StagingName, staged.Length, (byte[])staged.Sha256.Clone(), staged.Device, staged.Inode, true));
            }

            var registered = _intentTemplate with
            {
                Writer = OwnershipWriterKind.Owner,
                Cleanup = OwnershipCleanupState.NotStarted,
                Stage = PreparationStage.AllocationRegistered,
                Allocations = allocations,
                Preparation = new RecordedPreparation(
                    identity.ExecutionId, identity.IssuerId, identity.GenerationId,
                    identity.PreparationId, identity.ManifestDigest.ToArray()),
            };
            var registeredWrite = _journal.WriteNext(writer, registered);
            if (!registeredWrite.Ok)
            {
                return false;
            }

            var preparedWrite = _journal.WriteNext(writer, registered with { Stage = PreparationStage.Prepared });
            return preparedWrite.Ok;
        };
    }
}
