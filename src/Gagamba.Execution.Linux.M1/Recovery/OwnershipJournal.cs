// WP03 durable ownership journal (G2-04/G2-05): bounded two-generation
// protocol with a fenced writer lease.
//
// On-disk layout (all names bounded, all content ≤64 KiB):
//   gen.a / gen.b  — the two generation slots; exactly one holds the
//                    acknowledged generation after a completed write.
//   gen.tmp        — transient candidate staging; never acknowledged.
//   ownership.lock — flock writer fence (storage-level).
//
// Commit protocol for sequence N+1 (baseline N):
//   1. resolve: the acknowledged generation must be N (or none exists and
//      N is the fresh genesis 0);
//   2. free a slot (unlink higher-sequence debris if both slots present);
//   3. exclusive-create gen.tmp, complete write, fsync(file);
//   4. rename(gen.tmp → target), fsync(dir) — candidate established;
//   5. unlink(old slot), fsync(dir) — retirement.
// Success is reported only after step 5. Any earlier failure leaves the
// old acknowledged generation intact.
//
// Reader resolution (conservative; never invents durability):
//   - no slot file            → no acknowledged generation;
//   - one valid slot          → acknowledged (a lone slot is either the
//     untouched old generation or a fully established new one; torn
//     content can never reach a slot because renames happen only after a
//     complete fsynced write);
//   - both slots valid        → the lower sequence (the higher is
//     unconfirmed replacement debris);
//   - anything else           → explicit uncertainty/refusal.
//
// A failed write never advances in-memory or durable sequence. If steps 1–4
// complete but retirement (5) fails, the new generation is established but
// reported as failure; the reader rules above keep both outcomes safe.
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
        _journal.ReleaseWriterLock();
    }

    public void Dispose()
    {
        Release();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Bounded two-generation journal. Thread-affine; mutation
/// requires a live <see cref="JournalWriterLease"/> so flock fencing is
/// structural, not conventional.</summary>
internal sealed class OwnershipJournal
{
    internal const string SlotA = "gen.a";
    internal const string SlotB = "gen.b";
    internal const string TempName = "gen.tmp";
    internal const string DefaultLockName = "ownership.lock";

    private readonly IJournalStorage _storage;
    private readonly string _directory;
    private readonly string _lockName;
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

    /// <summary>Acquires exclusive writer authority: takes the storage
    /// lock, then resolves the last acknowledged durable snapshot under
    /// that fence and seeds the lease baseline from it.</summary>
    internal bool TryAcquireWriter(out JournalWriterLease? lease, out string error)
    {
        lease = null;
        error = string.Empty;
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
                _lastSequence = 0;
                return true;
            case ResolveKind.Acknowledged:
                lease = new JournalWriterLease(this, _directory, resolved.Snapshot!.Sequence);
                _lastSequence = resolved.Snapshot.Sequence;
                return true;
            default:
                _storage.ReleaseExclusiveLock(_directory, _lockName);
                error = resolved.Error;
                return false;
        }
    }

    internal void ReleaseWriterLock() =>
        _storage.ReleaseExclusiveLock(_directory, _lockName);

    internal JournalIoResult Write(JournalWriterLease? lease, OwnershipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (lease is null || !ReferenceEquals(lease.Journal, this) || !lease.IsLive)
        {
            return JournalIoResult.Failure("writer authority required: no live writer lease");
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

        // Free a slot: if both hold content, the higher sequence is
        // unconfirmed debris from an interrupted replacement.
        string? target = null;
        var hasA = _storage.Exists(_directory, SlotA);
        var hasB = _storage.Exists(_directory, SlotB);
        if (!hasA)
        {
            target = SlotA;
        }
        else if (!hasB)
        {
            target = SlotB;
        }
        else
        {
            // Both present: resolve() already determined the lower one is
            // acknowledged, so remove the higher-sequence debris.
            var readA = ReadSlot(SlotA);
            var readB = ReadSlot(SlotB);
            var debris = readA.Snapshot!.Sequence >= readB.Snapshot!.Sequence ? SlotA : SlotB;
            var removed = _storage.Unlink(_directory, debris);
            if (!removed.Ok)
            {
                return JournalIoResult.Failure($"cannot free a generation slot: {removed.Error}");
            }

            target = debris;
        }

        // Stage the candidate completely, then make it visible.
        _storage.Unlink(_directory, TempName);
        var created = _storage.CreateExclusive(_directory, TempName);
        if (!created.Ok)
        {
            return created;
        }

        var written = _storage.WriteAll(_directory, TempName, bytes);
        if (!written.Ok)
        {
            return written;
        }

        var syncedFile = _storage.FsyncFile(_directory, TempName);
        if (!syncedFile.Ok)
        {
            return syncedFile;
        }

        var visible = _storage.Rename(_directory, TempName, target);
        if (!visible.Ok)
        {
            return visible;
        }

        var syncedDir = _storage.FsyncDirectory(_directory);
        if (!syncedDir.Ok)
        {
            // Candidate bytes may be visible but durability is unconfirmed;
            // the old acknowledged generation is untouched.
            return syncedDir;
        }

        // Retire the old generation. Only now may the previous acknowledged
        // bytes disappear. A failure here still leaves a safely
        // interpretable state (reader rules), but is reported as failure so
        // no caller mistakes retirement for success.
        string? oldSlot = target == SlotA ? SlotB : SlotA;
        if (_storage.Exists(_directory, oldSlot))
        {
            var retired = _storage.Unlink(_directory, oldSlot);
            if (!retired.Ok)
            {
                return JournalIoResult.Failure($"old retirement incomplete: {retired.Error}");
            }

            var syncedRetire = _storage.FsyncDirectory(_directory);
            if (!syncedRetire.Ok)
            {
                return JournalIoResult.Failure($"old retirement unconfirmed: {syncedRetire.Error}");
            }
        }

        lease.AdvanceBaseline(expected);
        _lastSequence = expected;
        return JournalIoResult.Success;
    }

    /// <summary>Writes with the next monotonic sequence stamped.</summary>
    internal JournalIoResult WriteNext(JournalWriterLease? lease, OwnershipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (lease is null || !lease.IsLive)
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

    private Resolved Resolve()
    {
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
            if (a.Snapshot.Sequence == b.Snapshot.Sequence)
            {
                return new Resolved(ResolveKind.Uncertain, null, "duplicate sequence across generations");
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
