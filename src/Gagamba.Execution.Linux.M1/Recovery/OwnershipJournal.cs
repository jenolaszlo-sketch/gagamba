// WP03 durable ownership journal. One bounded versioned snapshot per
// execution, written through exclusive temp create, complete write, file
// fsync, atomic rename and directory fsync. The previous complete
// snapshot is retained until replacement durability succeeds. Writer
// exclusivity is a storage-level lock whose native form (flock) is
// released by the kernel on process death, which is what makes a
// takeover fenced rather than a concurrent-writer race.
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

    bool Exists(string directory, string name);

    byte[]? ReadAll(string directory, string name);

    JournalIoResult TryAcquireExclusiveLock(string directory, string name);

    JournalIoResult ReleaseExclusiveLock(string directory, string name);

    bool IsLockHeld(string directory, string name);
}

internal readonly record struct JournalReadResult(bool Ok, OwnershipSnapshot? Snapshot, string Error)
{
    internal static JournalReadResult Missing { get; } = new(false, null, "no durable snapshot exists");
}

/// <summary>Bounded single-snapshot journal. Thread-affine; one writer at
/// a time is enforced by the caller through the writer lock, not by this
/// type's internal state.</summary>
internal sealed class OwnershipJournal
{
    internal const string DefaultSnapshotName = "ownership.snapshot";
    internal const string DefaultLockName = "ownership.lock";

    private readonly IJournalStorage _storage;
    private readonly string _directory;
    private readonly string _snapshotName;
    private readonly string _lockName;
    private long _lastSequence;

    internal OwnershipJournal(
        IJournalStorage storage,
        string directory,
        string snapshotName = DefaultSnapshotName,
        string lockName = DefaultLockName)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _directory = directory;
        _snapshotName = snapshotName;
        _lockName = lockName;
    }

    internal long LastSequence => _lastSequence;

    internal JournalIoResult TryAcquireExclusiveWriter() =>
        _storage.TryAcquireExclusiveLock(_directory, _lockName);

    internal JournalIoResult ReleaseExclusiveWriter() =>
        _storage.ReleaseExclusiveLock(_directory, _lockName);

    internal bool IsWriterHeld() => _storage.IsLockHeld(_directory, _lockName);

    /// <summary>Durably writes one snapshot. On any step failure the prior
    /// complete snapshot (if any) remains intact and readable.</summary>
    internal JournalIoResult Write(OwnershipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        byte[] bytes;
        try
        {
            bytes = OwnershipSnapshotCodec.Encode(snapshot);
        }
        catch (ArgumentException ex)
        {
            return JournalIoResult.Failure($"refused: {ex.Message}");
        }

        var tempName = _snapshotName + ".tmp";
        var create = _storage.CreateExclusive(_directory, tempName);
        if (!create.Ok)
        {
            return create;
        }

        var write = _storage.WriteAll(_directory, tempName, bytes);
        if (!write.Ok)
        {
            return write;
        }

        var fsyncFile = _storage.FsyncFile(_directory, tempName);
        if (!fsyncFile.Ok)
        {
            return fsyncFile;
        }

        var rename = _storage.Rename(_directory, tempName, _snapshotName);
        if (!rename.Ok)
        {
            return rename;
        }

        var fsyncDir = _storage.FsyncDirectory(_directory);
        if (!fsyncDir.Ok)
        {
            return fsyncDir;
        }

        _lastSequence = snapshot.Sequence;
        return JournalIoResult.Success;
    }

    /// <summary>Writes with the next monotonic sequence stamped onto the
    /// supplied snapshot.</summary>
    internal JournalIoResult WriteNext(OwnershipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return Write(snapshot with { Sequence = _lastSequence + 1 });
    }

    internal JournalReadResult ReadLatest()
    {
        if (!_storage.Exists(_directory, _snapshotName))
        {
            return JournalReadResult.Missing;
        }

        var bytes = _storage.ReadAll(_directory, _snapshotName);
        if (bytes is null)
        {
            return new JournalReadResult(false, null, "snapshot unreadable");
        }

        if (!OwnershipSnapshotCodec.TryDecode(bytes, out var snapshot, out var error))
        {
            return new JournalReadResult(false, null, error);
        }

        _lastSequence = snapshot!.Sequence;
        return new JournalReadResult(true, snapshot, string.Empty);
    }
}
