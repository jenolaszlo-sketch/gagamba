// WP03 native durable snapshot storage for the ownership journal. Each
// durability step is a discrete operation so callers can reason about and
// fault-inject it. Writer exclusivity uses flock: the kernel releases the
// lock when the holder dies, which is what makes recovery takeover a
// fence rather than a concurrent-writer race. Only trusted-host file IO
// runs here; no path is attacker-controlled.
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Native;

internal sealed class JournalFileStorage : IJournalStorage, IDisposable
{
    private const int LOCK_EX = 2;
    private const int LOCK_NB = 4;
    private const int LOCK_UN = 8;
    private const int EWOULDBLOCK = 11;

    [DllImport("libc", EntryPoint = "flock", SetLastError = true)]
    private static extern int Flock(int fd, int operation);

    // Lock ownership is keyed to the actual lock identity
    // (directory + name): one entry per real exclusive fence held by this
    // storage instance. A second acquisition of a held fence refuses instead
    // of manufacturing another independently disposable handle over the
    // same kernel lock. Cross-process and cross-object exclusion inside one
    // process additionally rests on flock itself (a separate open of a
    // locked file denies LOCK_EX|LOCK_NB with EWOULDBLOCK, verified on the
    // native host), so no second usable fence can exist while one is held.
    private readonly Dictionary<(string Directory, string Name), FileStream> _heldLocks = new();
    private bool _disposed;

    public JournalIoResult CreateExclusive(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        try
        {
            if (File.Exists(path))
            {
                return JournalIoResult.Failure($"EEXIST: {name}");
            }

            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            return JournalIoResult.Success;
        }
        catch (IOException ex)
        {
            return JournalIoResult.Failure($"create {name}: {ex.GetType().Name}");
        }
    }

    public JournalIoResult WriteAll(string directory, string name, byte[] content)
    {
        try
        {
            using var stream = new FileStream(
                Path.Combine(directory, name), FileMode.Open, FileAccess.Write, FileShare.None);
            stream.Write(content, 0, content.Length);
            return JournalIoResult.Success;
        }
        catch (Exception ex)
        {
            return JournalIoResult.Failure($"write {name}: {ex.GetType().Name}");
        }
    }

    public JournalIoResult FsyncFile(string directory, string name)
    {
        try
        {
            using var stream = new FileStream(
                Path.Combine(directory, name), FileMode.Open, FileAccess.Write, FileShare.None);
            stream.Flush(flushToDisk: true);
            return JournalIoResult.Success;
        }
        catch (Exception ex)
        {
            return JournalIoResult.Failure($"fsync file {name}: {ex.GetType().Name}");
        }
    }

    public JournalIoResult Rename(string directory, string sourceName, string targetName)
    {
        try
        {
            File.Move(
                Path.Combine(directory, sourceName),
                Path.Combine(directory, targetName),
                overwrite: true);
            return JournalIoResult.Success;
        }
        catch (Exception ex)
        {
            return JournalIoResult.Failure($"rename {sourceName}->{targetName}: {ex.GetType().Name}");
        }
    }

    public JournalIoResult FsyncDirectory(string directory)
    {
        var errno = PreparationInterop.OpenDirectory(directory, out var fd);
        if (errno != 0)
        {
            return JournalIoResult.Failure($"fsync dir open errno {errno}");
        }

        try
        {
            var syncErrno = PreparationInterop.Sync(fd);
            return syncErrno == 0 ? JournalIoResult.Success : JournalIoResult.Failure($"fsync dir errno {syncErrno}");
        }
        finally
        {
            PreparationInterop.CloseFd(fd);
        }
    }

    public JournalIoResult Unlink(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        try
        {
            if (!File.Exists(path))
            {
                return JournalIoResult.Success;
            }

            File.Delete(path);
            return JournalIoResult.Success;
        }
        catch (Exception ex)
        {
            return JournalIoResult.Failure($"unlink {name}: {ex.GetType().Name}");
        }
    }

    public bool Exists(string directory, string name) => File.Exists(Path.Combine(directory, name));

    public byte[]? ReadAll(string directory, string name)
    {
        var path = Path.Combine(directory, name);
        try
        {
            // Inspect length through trusted metadata before allocating or
            // reading proportionally to a corrupt length.
            var length = new FileInfo(path).Length;
            if (length < 0 || length > OwnershipSnapshotCodec.MaxSnapshotBytes)
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            return bytes.Length > OwnershipSnapshotCodec.MaxSnapshotBytes ? null : bytes;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public JournalIoResult TryAcquireExclusiveLock(string directory, string name)
    {
        var key = (directory, name);
        if (_heldLocks.ContainsKey(key))
        {
            return JournalIoResult.Failure($"lock {name} already held by this storage: one active fence only");
        }

        var path = Path.Combine(directory, name);
        try
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            var fd = (int)stream.SafeFileHandle.DangerousGetHandle();
            if (Flock(fd, LOCK_EX | LOCK_NB) != 0)
            {
                stream.Dispose();
                return JournalIoResult.Failure($"lock held by another writer (errno {Marshal.GetLastWin32Error()})");
            }

            // Keep the stream alive for the lock's lifetime; the kernel
            // releases the flock automatically when this process dies.
            _heldLocks[key] = stream;
            return JournalIoResult.Success;
        }
        catch (Exception ex)
        {
            return JournalIoResult.Failure($"lock {name}: {ex.GetType().Name}");
        }
    }

    public JournalIoResult ReleaseExclusiveLock(string directory, string name)
    {
        if (!_heldLocks.TryGetValue((directory, name), out var stream))
        {
            return JournalIoResult.Success;
        }

        Flock((int)stream.SafeFileHandle.DangerousGetHandle(), LOCK_UN);
        stream.Dispose();
        _heldLocks.Remove((directory, name));
        return JournalIoResult.Success;
    }

    public bool IsLockHeld(string directory, string name)
    {
        if (_heldLocks.ContainsKey((directory, name)))
        {
            return true;
        }

        var path = Path.Combine(directory, name);
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            var fd = (int)stream.SafeFileHandle.DangerousGetHandle();
            var rc = Flock(fd, LOCK_EX | LOCK_NB);
            if (rc == 0)
            {
                Flock(fd, LOCK_UN);
                return false;
            }

            return Marshal.GetLastWin32Error() == EWOULDBLOCK;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var stream in _heldLocks.Values)
        {
            Flock((int)stream.SafeFileHandle.DangerousGetHandle(), LOCK_UN);
            stream.Dispose();
        }

        _heldLocks.Clear();
        GC.SuppressFinalize(this);
    }
}
