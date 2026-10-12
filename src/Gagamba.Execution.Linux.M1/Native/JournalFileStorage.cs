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

    private readonly Dictionary<string, FileStream> _heldLocks = new(StringComparer.Ordinal);
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

    public bool Exists(string directory, string name) => File.Exists(Path.Combine(directory, name));

    public byte[]? ReadAll(string directory, string name)
    {
        try
        {
            return File.ReadAllBytes(Path.Combine(directory, name));
        }
        catch (Exception)
        {
            return null;
        }
    }

    public JournalIoResult TryAcquireExclusiveLock(string directory, string name)
    {
        if (_heldLocks.ContainsKey(name))
        {
            return JournalIoResult.Success;
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
            _heldLocks[name] = stream;
            return JournalIoResult.Success;
        }
        catch (Exception ex)
        {
            return JournalIoResult.Failure($"lock {name}: {ex.GetType().Name}");
        }
    }

    public JournalIoResult ReleaseExclusiveLock(string directory, string name)
    {
        if (!_heldLocks.TryGetValue(name, out var stream))
        {
            return JournalIoResult.Success;
        }

        Flock((int)stream.SafeFileHandle.DangerousGetHandle(), LOCK_UN);
        stream.Dispose();
        _heldLocks.Remove(name);
        return JournalIoResult.Success;
    }

    public bool IsLockHeld(string directory, string name)
    {
        if (_heldLocks.ContainsKey(name))
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
