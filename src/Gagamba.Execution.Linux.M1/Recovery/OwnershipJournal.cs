// WP03 durable ownership journal (G2-04/G2-05): bounded two-generation
// protocol with a fenced writer lease and a deletion-sealed acknowledgement.
//
// On-disk layout (all names bounded, all content ≤64 KiB):
//   gen.a / gen.b  — the two generation slots; a completed write leaves
//                    the same acknowledged generation in both.
//   gen.tmp        — transient candidate staging; never acknowledged.
//   gen.seal       — commit-in-flight marker (presence only); absent in
//                    every clean state, present while a commit's durability
//                    is unconfirmed.
//   ownership.lock — flock writer fence (storage-level).
//
// Acknowledgement contract: sequence N+1 becomes acknowledged exactly when
// the whole commit below completes. A successful Write return happens only
// after the final step. Any failure returns failure without advancing any
// in-memory or durable sequence.
//
// Commit protocol for sequence N+1 (baseline N):
//   0. best-effort unlink(gen.tmp) scratch cleanup (ignored; non-acking);
//   1. exclusive-create gen.seal, fsync(file), fsync(dir) — ambiguity is
//      now explicit: until step 4 the reader reports uncertainty;
//   2. exclusive-create gen.tmp, complete write, fsync(file),
//      rename(gen.tmp → gen.b), fsync(dir) — candidate staged;
//   3. exclusive-create gen.tmp, complete write, fsync(file),
//      rename(gen.tmp → gen.a), fsync(dir) — both slots hold N+1;
//   4. unlink(gen.seal) — the ambiguity override is removed.
// Old generations are never unlinked: each slot is overwritten in place by
// an atomic rename, so no retirement step can orphan the acknowledged
// generation the way the old retire-after-publish protocol did.
//
// Acknowledgement model: acknowledgement is a content property — both
// slots holding the generation, every rename preceded by a complete fsynced
// write and followed by a directory sync. The seal never acknowledges
// anything; its presence only overrides content reading with explicit
// uncertainty while a commit is in flight. Removing the seal therefore
// needs no durability confirmation of its own: a torn removal leaves a
// stale override (uncertainty, recoverable via TryRecoverCommit), never a
// false acknowledgement. Symmetrically, no confirmation failure of any
// content step can promote: every such failure leaves the seal present
// (uncertainty) or, via best-effort rollback, the old baseline visibly
// intact. A failed write reopens as the old acknowledged generation or as
// explicit uncertainty — never as the failed candidate.
//
// Crash note: a crash after a reported success but before the seal removal
// persists surfaces as uncertainty (stale seal over complete content),
// which TryRecoverCommit completes explicitly. Success is still honest:
// all content the recovery algorithm needs is durable before success is
// reported; only the ambiguity override may linger.
//
// Reader resolution (conservative; never invents durability):
//   - gen.seal present        → explicit uncertainty (commit in flight;
//     explicit recovery via TryRecoverCommit is required);
//   - no slot file           → no acknowledged generation;
//   - one valid slot         → acknowledged;
//   - both slots valid+equal → acknowledged;
//   - both slots valid, unequal → the lower sequence (the higher is an
//     uncommitted candidate: acknowledgement requires both-equal, so an
//     unequal pair can never hold an acknowledged higher generation);
//   - anything else          → explicit uncertainty/refusal.
//
// Failure rollback: on any pre-acknowledgement failure the seal is removed
// best-effort, except when no acknowledged baseline exists (genesis) or the
// slots already hold a complete newer generation; those stay sealed and
// therefore uncertain. Crash between failure and rollback likewise leaves
// the seal and reports uncertainty. Both outcomes satisfy the contract.
namespace Gagamba.Execution.Linux.M1.Recovery;

internal readonly record struct JournalIoResult(bool Ok, string Error)
{
    internal static JournalIoResult Success { get; } = new(true, string.Empty);

    internal static JournalIoResult Failure(string error) => new(false, error);
}

/// <summary>Discrete durable-storage seam so each durability step can be
/// fault-injected and so tests never require a real filesystem.</summary>
internal interface IJournalStorage
{
    JournalIoResult CreateExclusive(string directory, string name);

    JournalIoResult WriteAll(string directory, string name, byte[] content);

    JournalIoResult FsyncFile(string directory, string name);

    JournalIoResult Rename(string directory, string sourceName, string targetName);

    JournalIoResult FsyncDirectory(string directory);

    JournalIoResult Unlink(string directory, string name);

    bool Exists(string directory, string name);

    /// <summary>Reads a complete file. Implementations must refuse files
    /// exceeding <see cref="OwnershipSnapshotCodec.MaxSnapshotBytes"/>
    /// before allocating/reading them fully.</summary>
    byte[]? ReadAll(string directory, string name);

    JournalIoResult TryAcquireExclusiveLock(string directory, string name);

    JournalIoResult ReleaseExclusiveLock(string directory, string name);

    bool IsLockHeld(string directory, string name);
}

internal readonly record struct JournalReadResult(bool Ok, OwnershipSnapshot? Snapshot, string Error)
{
    internal static JournalReadResult Missing { get; } = new(false, null, "no durable snapshot exists");

    internal static JournalReadResult Uncertain(string error) => new(false, null, error);
}

/// <summary>Exclusive journal-writer ownership. Created only by
/// <see cref="OwnershipJournal.TryAcquireWriter"/> after the storage lock
/// is held; it cannot be forged from a PID, path or sequence number.
/// Disposal relinquishes writer authority exactly once.</summary>
internal sealed class JournalWriterLease : IDisposable
{
    private readonly OwnershipJournal _journal;
    private bool _released;

    internal JournalWriterLease(OwnershipJournal journal, string directory, long baselineSequence)
    {
        _journal = journal;
        Directory = directory;
        BaselineSequence = baselineSequence;
    }

    internal string Directory { get; }

    internal OwnershipJournal Journal => _journal;

    internal long BaselineSequence { get; private set; }

    internal bool IsLive => !_released;

    internal void AdvanceBaseline(long sequence) => BaselineSequence = sequence;

    internal void Release()
    {
        if (_released)
        {
            return;
        }

        _released = true;
        _journal.ReleaseWriter(this);
    }

    public void Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Unforgeable proof that one exact AllocationIntent is the
/// acknowledged baseline under a live writer lease. It is produced only by
/// OwnershipJournal after reading the committed state while that lease's
/// storage fence is held.</summary>
internal sealed class AcknowledgedAllocationIntent
{
    internal AcknowledgedAllocationIntent(
        OwnershipJournal journal,
        JournalWriterLease writer,
        long sequence,
        OwnershipSnapshot snapshot)
    {
        Journal = journal;
        Writer = writer;
        Sequence = sequence;
        Snapshot = snapshot;
    }

    internal OwnershipJournal Journal { get; }

    internal JournalWriterLease Writer { get; }

    internal long Sequence { get; }

    internal OwnershipSnapshot Snapshot { get; }
}

/// <summary>Bounded two-generation journal. Thread-affine; mutation
/// requires a live <see cref="JournalWriterLease"/> so flock fencing is
/// structural, not conventional.</summary>
internal sealed class OwnershipJournal
{
    internal const string SlotA = "gen.a";
    internal const string SlotB = "gen.b";
    internal const string TempName = "gen.tmp";
    internal const string SealName = "gen.seal";
    internal const string DefaultLockName = "ownership.lock";

    private readonly IJournalStorage _storage;
    private readonly string _directory;
    private readonly string _lockName;
    private readonly object _gate = new();
    private JournalWriterLease? _activeLease;
    private long _lastSequence;

    internal OwnershipJournal(
        IJournalStorage storage,
        string directory,
        string lockName = DefaultLockName)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _directory = directory;
        _lockName = lockName;
    }

    internal long LastSequence => _lastSequence;

    /// <summary>Acquires exclusive writer authority: refuses while this
    /// journal already owns a live lease (one active writer capability per
    /// actual fence), takes the storage lock, then resolves the last
    /// acknowledged durable snapshot under that fence and seeds the lease
    /// baseline from it. Ambiguous durable state refuses with explicit
    /// uncertainty; it is never resolved by choosing the highest visible
    /// sequence (see <see cref="TryRecoverCommit"/>).</summary>
    internal bool TryAcquireWriter(out JournalWriterLease? lease, out string error)
    {
        lease = null;
        error = string.Empty;
        lock (_gate)
        {
            if (_activeLease is not null && _activeLease.IsLive)
            {
                error = "writer already active on this journal: dispose the live lease first";
                return false;
            }

            _activeLease = null;
        }

        var acquired = _storage.TryAcquireExclusiveLock(_directory, _lockName);
        if (!acquired.Ok)
        {
            error = acquired.Error;
            return false;
        }

        var resolved = Resolve();
        switch (resolved.Kind)
        {
            case ResolveKind.None:
                lease = new JournalWriterLease(this, _directory, 0);
                break;
            case ResolveKind.Acknowledged:
                lease = new JournalWriterLease(this, _directory, resolved.Snapshot!.Sequence);
                break;
            default:
                _storage.ReleaseExclusiveLock(_directory, _lockName);
                error = resolved.Error;
                return false;
        }

        lock (_gate)
        {
            _activeLease = lease;
        }

        _lastSequence = lease.BaselineSequence;
        return true;
    }

    /// <summary>Verifies that the current acknowledged journal baseline is
    /// the exact supplied AllocationIntent for this transaction, then
    /// returns a proof bound to this journal and its live fenced writer.
    /// A positive sequence by itself is never allocation authority.</summary>
    internal bool TryProveAllocationIntent(
        JournalWriterLease? lease,
        OwnershipSnapshot expectedIntent,
        out AcknowledgedAllocationIntent? proof,
        out string error)
    {
        proof = null;
        error = string.Empty;
        ArgumentNullException.ThrowIfNull(expectedIntent);
        if (lease is null || !ReferenceEquals(lease.Journal, this) || !IsEffectiveWriter(lease))
        {
            error = "allocation intent proof requires this journal's effective live writer";
            return false;
        }

        var resolved = Resolve();
        if (resolved.Kind != ResolveKind.Acknowledged || resolved.Snapshot is null)
        {
            error = resolved.Error.Length == 0 ? "allocation intent baseline is not acknowledged" : resolved.Error;
            return false;
        }

        var actual = resolved.Snapshot;
        if (actual.Sequence != lease.BaselineSequence
            || actual.Stage != PreparationStage.AllocationIntent
            || expectedIntent.Stage != PreparationStage.AllocationIntent
            || actual.ExecutionId != expectedIntent.ExecutionId
            || actual.LeaseGeneration != expectedIntent.LeaseGeneration
            || actual.BootId != expectedIntent.BootId
            || actual.DomainName != expectedIntent.DomainName
            || actual.Writer != expectedIntent.Writer
            || actual.Cleanup != expectedIntent.Cleanup
            || actual.StagingParentDevice != expectedIntent.StagingParentDevice
            || actual.StagingParentInode != expectedIntent.StagingParentInode
            || actual.Preparation is not null
            || actual.Allocations.Count != expectedIntent.Allocations.Count
            || actual.Allocations.Where((item, index) => !AllocationIntentEntryMatches(item, expectedIntent.Allocations[index])).Any())
        {
            error = "acknowledged journal baseline is not the matching AllocationIntent";
            return false;
        }

        proof = new AcknowledgedAllocationIntent(this, lease, actual.Sequence, actual);
        return true;
    }

    private static bool AllocationIntentEntryMatches(AllocationEntry actual, AllocationEntry expected) =>
        actual.Kind == expected.Kind
        && actual.Name == expected.Name
        && actual.Length == expected.Length
        && actual.Device == expected.Device
        && actual.Inode == expected.Inode
        && actual.Confirmed == expected.Confirmed
        && actual.Digest.AsSpan().SequenceEqual(expected.Digest);

    /// <summary>Relinquishes the fence for a released lease. Only the
    /// currently active lease can clear writer authority; anything else is
    /// a harmless no-op on the tracking side (the storage release itself is
    /// still issued so accounting cannot wedge).</summary>
    internal void ReleaseWriter(JournalWriterLease lease)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_activeLease, lease))
            {
                _activeLease = null;
            }
        }

        _storage.ReleaseExclusiveLock(_directory, _lockName);
    }

    /// <summary>True only while the given lease is this journal's active
    /// writer and the real exclusive fence is still held.</summary>
    internal bool IsEffectiveWriter(JournalWriterLease lease)
    {
        lock (_gate)
        {
            if (!ReferenceEquals(_activeLease, lease) || !lease.IsLive)
            {
                return false;
            }
        }

        return _storage.IsLockHeld(_directory, _lockName);
    }

    internal JournalIoResult Write(JournalWriterLease? lease, OwnershipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (lease is null || !ReferenceEquals(lease.Journal, this) || !lease.IsLive)
        {
            return JournalIoResult.Failure("writer authority required: no live writer lease");
        }

        if (!IsEffectiveWriter(lease))
        {
            return JournalIoResult.Failure("writer authority required: lease is not the active fence holder");
        }

        long expected;
        try
        {
            checked
            {
                expected = lease.BaselineSequence + 1;
            }
        }
        catch (OverflowException)
        {
            return JournalIoResult.Failure("sequence overflow");
        }

        if (snapshot.Sequence != expected)
        {
            return JournalIoResult.Failure($"sequence must advance by exactly one (expected {expected}, got {snapshot.Sequence})");
        }

        byte[] bytes;
        try
        {
            bytes = OwnershipSnapshotCodec.Encode(snapshot);
        }
        catch (ArgumentException ex)
        {
            return JournalIoResult.Failure($"refused: {ex.Message}");
        }

        // Re-resolve under the fence: the disk must still show our baseline.
        var resolved = Resolve();
        if (resolved.Kind == ResolveKind.Uncertain)
        {
            return JournalIoResult.Failure($"lineage uncertain: {resolved.Error}");
        }

        if (resolved.Kind == ResolveKind.None)
        {
            if (lease.BaselineSequence != 0)
            {
                return JournalIoResult.Failure("lineage changed under the writer fence");
            }
        }
        else if (resolved.Snapshot!.Sequence != lease.BaselineSequence)
        {
            return JournalIoResult.Failure("lineage changed under the writer fence");
        }

        // Best-effort scratch cleanup; ignored because it is not part of
        // the acknowledgement (a leftover only fails the run below).
        _storage.Unlink(_directory, TempName);

        // Open the acknowledgement barrier: from here until the seal is
        // removed, the durable state is explicitly ambiguous.
        var sealed_ = _storage.CreateExclusive(_directory, SealName);
        if (!sealed_.Ok)
        {
            return JournalIoResult.Failure($"seal creation failed, commit already in flight: {sealed_.Error}");
        }

        var sealedFile = _storage.FsyncFile(_directory, SealName);
        if (!sealedFile.Ok)
        {
            return FailSealed(lease, $"seal file sync failed: {sealedFile.Error}");
        }

        var sealedDir = _storage.FsyncDirectory(_directory);
        if (!sealedDir.Ok)
        {
            return FailSealed(lease, $"seal directory sync failed: {sealedDir.Error}");
        }

        // Stage the candidate into SlotB (atomic rename of a complete,
        // fsynced temp file), then mirror it into SlotA. Both slots hold
        // the new generation only after this phase; no acknowledged bytes
        // are ever unlinked.
        foreach (var target in new[] { SlotB, SlotA })
        {
            var created = _storage.CreateExclusive(_directory, TempName);
            if (!created.Ok)
            {
                return FailSealed(lease, $"candidate temp creation failed: {created.Error}");
            }

            var written = _storage.WriteAll(_directory, TempName, bytes);
            if (!written.Ok)
            {
                return FailSealed(lease, $"candidate write failed: {written.Error}");
            }

            var syncedFile = _storage.FsyncFile(_directory, TempName);
            if (!syncedFile.Ok)
            {
                return FailSealed(lease, $"candidate file sync failed: {syncedFile.Error}");
            }

            var visible = _storage.Rename(_directory, TempName, target);
            if (!visible.Ok)
            {
                return FailSealed(lease, $"candidate rename failed: {visible.Error}");
            }

            var syncedDir = _storage.FsyncDirectory(_directory);
            if (!syncedDir.Ok)
            {
                return FailSealed(lease, $"candidate directory sync failed: {syncedDir.Error}");
            }
        }

        // Remove the ambiguity override. Both slots already hold the fully
        // durable new generation, so this deletion acknowledges nothing and
        // needs no confirmation of its own: if the syscall itself fails the
        // seal stays and the commit reads uncertain; once the syscall
        // succeeds the new sequence is acknowledged and success is
        // reported. (A crash before the removal persists may surface a
        // stale seal; TryRecoverCommit completes that explicitly.)
        var unsealed = _storage.Unlink(_directory, SealName);
        if (!unsealed.Ok)
        {
            return JournalIoResult.Failure($"seal release failed, commit ambiguous: {unsealed.Error}");
        }

        lease.AdvanceBaseline(expected);
        _lastSequence = expected;
        return JournalIoResult.Success;
    }

    /// <summary>Reports a pre-acknowledgement commit failure. The seal is
    /// removed best-effort exactly when doing so provably restores the
    /// acknowledged baseline: the baseline generation must still decode
    /// from at least one slot. Otherwise (genesis with no old generation,
    /// unreadable slots, or a complete newer generation that only the seal
    /// deletion could confirm) the seal stays and the reader reports
    /// explicit uncertainty.</summary>
    private JournalIoResult FailSealed(JournalWriterLease lease, string error)
    {
        if (lease.BaselineSequence != 0)
        {
            var a = ReadSlot(SlotA);
            var b = ReadSlot(SlotB);
            if (a.Snapshot?.Sequence == lease.BaselineSequence
                || b.Snapshot?.Sequence == lease.BaselineSequence)
            {
                _storage.Unlink(_directory, SealName);
                _storage.FsyncDirectory(_directory);
            }
        }

        return JournalIoResult.Failure(error);
    }

    /// <summary>Writes with the next monotonic sequence stamped.</summary>
    internal JournalIoResult WriteNext(JournalWriterLease? lease, OwnershipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (lease is null || !ReferenceEquals(lease.Journal, this) || !lease.IsLive)
        {
            return JournalIoResult.Failure("writer authority required: no live writer lease");
        }

        long next;
        try
        {
            checked
            {
                next = lease.BaselineSequence + 1;
            }
        }
        catch (OverflowException)
        {
            return JournalIoResult.Failure("sequence overflow");
        }

        return Write(lease, snapshot with { Sequence = next });
    }

    internal JournalReadResult ReadLatest()
    {
        var resolved = Resolve();
        switch (resolved.Kind)
        {
            case ResolveKind.None:
                return JournalReadResult.Missing;
            case ResolveKind.Acknowledged:
                _lastSequence = resolved.Snapshot!.Sequence;
                return new JournalReadResult(true, resolved.Snapshot, string.Empty);
            default:
                return JournalReadResult.Uncertain(resolved.Error);
        }
    }

    private enum ResolveKind
    {
        None,
        Acknowledged,
        Uncertain,
    }

    private readonly record struct Resolved(ResolveKind Kind, OwnershipSnapshot? Snapshot, string Error);

    /// <summary>Explicit recovery from an ambiguous commit seal. Takes the
    /// exclusive fence (refusing while a live writer holds it), then:
    /// both slots decodable → removes the seal, returning the acknowledged
    /// generation (both-equal: the staged commit was complete; unequal: the
    /// lower generation, demoting the uncommitted higher one, which the
    /// next commit deterministically overwrites); no slots → removes the
    /// seal, returning nothing (genesis never staged). Lone or
    /// uninterpretable slots refuse: the seal stays and the ambiguity must
    /// be resolved by an operator, never by choosing the highest visible
    /// sequence. Recovery never deletes generation content, only the seal.
    /// Returns false when there is nothing ambiguous to recover.</summary>
    internal bool TryRecoverCommit(out OwnershipSnapshot? committed, out string error)
    {
        committed = null;
        error = string.Empty;
        var acquired = _storage.TryAcquireExclusiveLock(_directory, _lockName);
        if (!acquired.Ok)
        {
            error = $"recovery refused, fence held: {acquired.Error}";
            return false;
        }

        try
        {
            if (!_storage.Exists(_directory, SealName))
            {
                error = "no ambiguous commit to recover";
                return false;
            }

            // Best-effort scratch cleanup; ignored (non-acknowledging).
            _storage.Unlink(_directory, TempName);

            var a = ReadSlot(SlotA);
            var b = ReadSlot(SlotB);
            if (!a.Present && !b.Present)
            {
                return FinishRecovery(null, out committed, out error);
            }

            if (a.Present && a.Snapshot is not null && b.Present && b.Snapshot is not null)
            {
                var acknowledged = a.Snapshot.Sequence <= b.Snapshot.Sequence ? a.Snapshot : b.Snapshot;
                return FinishRecovery(acknowledged, out committed, out error);
            }

            error = "uninterpretable sealed state: operator resolution required";
            return false;
        }
        finally
        {
            _storage.ReleaseExclusiveLock(_directory, _lockName);
        }
    }

    private bool FinishRecovery(OwnershipSnapshot? acknowledged, out OwnershipSnapshot? committed, out string error)
    {
        committed = null;
        error = string.Empty;
        // Override removal needs no confirmation (see the commit protocol:
        // a torn removal only leaves a stale, recoverable override).
        var unsealed = _storage.Unlink(_directory, SealName);
        if (!unsealed.Ok)
        {
            error = $"recovery seal release failed: {unsealed.Error}";
            return false;
        }

        committed = acknowledged;
        return true;
    }

    private Resolved Resolve()
    {
        // A present seal means a commit's durability is unconfirmed. The
        // sealed content may be complete, torn, or absent; no rule below may
        // promote it. Uncertainty is the only honest reading.
        if (_storage.Exists(_directory, SealName))
        {
            return new Resolved(ResolveKind.Uncertain, null, "commit seal present: durability ambiguous, explicit recovery required");
        }

        var a = ReadSlot(SlotA);
        var b = ReadSlot(SlotB);

        if (!a.Present && !b.Present)
        {
            return new Resolved(ResolveKind.None, null, string.Empty);
        }

        if (a.Present && a.Snapshot is not null && !b.Present)
        {
            return new Resolved(ResolveKind.Acknowledged, a.Snapshot, string.Empty);
        }

        if (b.Present && b.Snapshot is not null && !a.Present)
        {
            return new Resolved(ResolveKind.Acknowledged, b.Snapshot, string.Empty);
        }

        if (a.Present && a.Snapshot is not null && b.Present && b.Snapshot is not null)
        {
            // A completed commit leaves the same generation in both slots,
            // so both-equal is the normal acknowledged steady state. An
            // unequal pair can never hold an acknowledged higher generation
            // (acknowledgement requires both-equal), so the lower one is
            // the last acknowledged generation and the higher an
            // uncommitted candidate.
            if (a.Snapshot.Sequence == b.Snapshot.Sequence)
            {
                return new Resolved(ResolveKind.Acknowledged, a.Snapshot, string.Empty);
            }

            var older = a.Snapshot.Sequence < b.Snapshot.Sequence ? a.Snapshot : b.Snapshot;
            return new Resolved(ResolveKind.Acknowledged, older, string.Empty);
        }

        return new Resolved(ResolveKind.Uncertain, null, "no interpretable acknowledged generation");
    }

    private readonly record struct SlotRead(bool Present, OwnershipSnapshot? Snapshot);

    private SlotRead ReadSlot(string name)
    {
        if (!_storage.Exists(_directory, name))
        {
            return new SlotRead(false, null);
        }

        var bytes = _storage.ReadAll(_directory, name);
        if (bytes is null || bytes.Length == 0 || bytes.Length > OwnershipSnapshotCodec.MaxSnapshotBytes)
        {
            return new SlotRead(true, null);
        }

        if (!OwnershipSnapshotCodec.TryDecode(bytes, out var snapshot, out _))
        {
            return new SlotRead(true, null);
        }

        return new SlotRead(true, snapshot);
    }
}
