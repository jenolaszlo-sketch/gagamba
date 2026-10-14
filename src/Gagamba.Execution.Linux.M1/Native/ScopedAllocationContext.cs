// WP03 G2-03 structural allocation boundary. A real M1 domain may be
// created only inside a scoped, intent-first transaction. This type is the
// unforgeable proof: it has no public constructor and can be obtained only
// through TryCreate, which requires a journal, a live writer lease that
// belongs to that journal, an already-acknowledged durable AllocationIntent
// for the same execution/generation, and the retained delegated parent.
// There is deliberately no journal-free entry, so no production path can
// create a domain as an independent allocation authority.
using Gagamba.Execution.Linux.M1.Recovery;

namespace Gagamba.Execution.Linux.M1.Native;

internal sealed class ScopedAllocationContext
{
    private ScopedAllocationContext(
        int parentFd,
        OwnershipJournal journal,
        JournalWriterLease writer,
        AcknowledgedAllocationIntent intent,
        long intentSequence,
        Guid executionId,
        Guid leaseGeneration,
        string bootId)
    {
        ParentFd = parentFd;
        Journal = journal;
        Writer = writer;
        Intent = intent;
        IntentSequence = intentSequence;
        ExecutionId = executionId;
        LeaseGeneration = leaseGeneration;
        BootId = bootId;
    }

    internal int ParentFd { get; }

    internal OwnershipJournal Journal { get; }

    internal JournalWriterLease Writer { get; }

    internal AcknowledgedAllocationIntent Intent { get; }

    /// <summary>The writer baseline at the point the durable
    /// AllocationIntent was acknowledged; proof the intent precedes
    /// creation.</summary>
    internal long IntentSequence { get; }

    internal Guid ExecutionId { get; }

    internal Guid LeaseGeneration { get; }

    internal string BootId { get; }

    /// <summary>The context is usable only while the fence that proved the
    /// intent is still held. Allocation after the writer is released is
    /// refused.</summary>
    internal bool IsLive => Journal.IsEffectiveWriter(Writer);

    /// <summary>The only way to obtain a context. A journal-free call, a
    /// foreign lease, a released lease, or an unacknowledged intent all
    /// refuse.</summary>
    internal static bool TryCreate(
        int parentFd,
        OwnershipJournal? journal,
        JournalWriterLease? writer,
        OwnershipSnapshot? expectedIntent,
        Guid executionId,
        Guid leaseGeneration,
        string bootId,
        out ScopedAllocationContext? context,
        out string error)
    {
        context = null;
        error = string.Empty;
        if (parentFd < 0)
        {
            error = "no retained delegated parent";
            return false;
        }

        if (journal is null || writer is null)
        {
            error = "domain allocation requires a journal and a live writer lease";
            return false;
        }

        if (!ReferenceEquals(writer.Journal, journal))
        {
            error = "writer lease does not belong to the supplied journal";
            return false;
        }

        if (!writer.IsLive)
        {
            error = "writer lease is not live";
            return false;
        }

        if (expectedIntent is null)
        {
            error = "matching AllocationIntent is required";
            return false;
        }

        if (expectedIntent.ExecutionId != executionId
            || expectedIntent.LeaseGeneration != leaseGeneration
            || expectedIntent.BootId != bootId
            || expectedIntent.Stage != PreparationStage.AllocationIntent)
        {
            error = "expected AllocationIntent identity does not match this transaction";
            return false;
        }

        if (!journal.TryProveAllocationIntent(writer, expectedIntent, out var intentProof, out error))
        {
            return false;
        }

        context = new ScopedAllocationContext(
            parentFd, journal, writer, intentProof!, intentProof!.Sequence, executionId, leaseGeneration, bootId);
        return true;
    }
}
