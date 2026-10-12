// WP02 copy-host seam: the engine below never touches pathnames, file
// descriptors or syscalls directly. LibcCopyHost implements the seam with
// the narrow WP02 interop; deterministic portable tests substitute a fake.
// Hosts and their handles must never be mixed.
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Preparation;

/// <summary>WP02-scoped filesystem seam. All operations are
/// descriptor-relative; the only pathname inputs are single-component
/// child names resolved under a retained directory descriptor, plus the
/// two harness-supplied bootstrap paths. Methods return a Linux errno
/// (0 on success) and never throw for expected failures.</summary>
internal interface ICopyHost
{
    int OpenDirectory(string path, out SafeHandle? handle);

    int OpenSubdirectory(SafeHandle parent, string name, out SafeHandle? handle);

    int StatHandle(SafeHandle handle, out FileIdentity identity);

    int OpenSource(SafeHandle root, string name, out SafeHandle? handle);

    int CreateStaging(SafeHandle parent, string name, out SafeHandle? handle);

    int MakeDirectory(SafeHandle parent, string name, uint mode);

    Stream OpenRead(SafeHandle handle);

    Stream OpenWrite(SafeHandle handle);

    int SetMode(SafeHandle handle, uint mode);

    int SyncFile(SafeHandle handle);

    int SyncDirectory(SafeHandle handle);

    int Unlink(SafeHandle parent, string name);
}

/// <summary>Production host: implements the seam with descriptor-relative
/// libc calls. Every method fails closed with Unsupported off Linux.</summary>
internal sealed class LibcCopyHost : ICopyHost
{
    internal static readonly LibcCopyHost Instance = new();

    private LibcCopyHost()
    {
    }

    private static int FdOf(SafeHandle handle) =>
        ((SafeFileHandle)handle).DangerousGetHandle().ToInt32();

    private static int ValidateBootstrapPath(string path)
    {
        if (path.Contains('\0'))
        {
            return PreparationInterop.EINVAL;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(path) > 4096)
        {
            return PreparationInterop.ENAMETOOLONG;
        }

        return 0;
    }

    public int OpenDirectory(string path, out SafeHandle? handle)
    {
        handle = null;
        if (!PreparationInterop.IsLinux)
        {
            return PreparationInterop.ENOSYS;
        }

        var invalid = ValidateBootstrapPath(path);
        if (invalid != 0)
        {
            return invalid;
        }

        var errno = PreparationInterop.OpenDirectory(path, out var fd);
        if (errno != 0)
        {
            return errno;
        }

        handle = new SafeFileHandle(new IntPtr(fd), ownsHandle: true);
        return 0;
    }

    public int OpenSubdirectory(SafeHandle parent, string name, out SafeHandle? handle)
    {
        handle = null;
        if (!PreparationInterop.IsLinux)
        {
            return PreparationInterop.ENOSYS;
        }

        var errno = PreparationInterop.OpenSubdirectory(FdOf(parent), name, out var fd);
        if (errno != 0)
        {
            return errno;
        }

        handle = new SafeFileHandle(new IntPtr(fd), ownsHandle: true);
        return 0;
    }

    public int StatHandle(SafeHandle handle, out FileIdentity identity)
    {
        identity = default;
        if (!PreparationInterop.IsLinux)
        {
            return PreparationInterop.ENOSYS;
        }

        return PreparationInterop.StatFd(FdOf(handle), out identity);
    }

    public int OpenSource(SafeHandle root, string name, out SafeHandle? handle)
    {
        handle = null;
        if (!PreparationInterop.IsLinux)
        {
            return PreparationInterop.ENOSYS;
        }

        var errno = PreparationInterop.OpenSource(
            FdOf(root), name, PreparationInterop.RequiredResolve, out var fd);
        if (errno != 0)
        {
            return errno;
        }

        handle = new SafeFileHandle(new IntPtr(fd), ownsHandle: true);
        return 0;
    }

    public int CreateStaging(SafeHandle parent, string name, out SafeHandle? handle)
    {
        handle = null;
        if (!PreparationInterop.IsLinux)
        {
            return PreparationInterop.ENOSYS;
        }

        var errno = PreparationInterop.CreateExclusive(FdOf(parent), name, out var fd);
        if (errno != 0)
        {
            return errno;
        }

        handle = new SafeFileHandle(new IntPtr(fd), ownsHandle: true);
        return 0;
    }

    public int MakeDirectory(SafeHandle parent, string name, uint mode) =>
        !PreparationInterop.IsLinux
            ? PreparationInterop.ENOSYS
            : PreparationInterop.MakeDirectory(FdOf(parent), name, mode);

    public Stream OpenRead(SafeHandle handle) =>
        new UnownedFdStream(FdOf(handle), FileAccess.Read);

    public Stream OpenWrite(SafeHandle handle) =>
        new UnownedFdStream(FdOf(handle), FileAccess.Write);

    public int SetMode(SafeHandle handle, uint mode) =>
        !PreparationInterop.IsLinux
            ? PreparationInterop.ENOSYS
            : PreparationInterop.SetMode(FdOf(handle), mode);

    public int SyncFile(SafeHandle handle) =>
        !PreparationInterop.IsLinux
            ? PreparationInterop.ENOSYS
            : PreparationInterop.Sync(FdOf(handle));

    public int SyncDirectory(SafeHandle handle) =>
        !PreparationInterop.IsLinux
            ? PreparationInterop.ENOSYS
            : PreparationInterop.Sync(FdOf(handle));

    public int Unlink(SafeHandle parent, string name) =>
        !PreparationInterop.IsLinux
            ? PreparationInterop.ENOSYS
            : PreparationInterop.Unlink(FdOf(parent), name);
}

/// <summary>Unbuffered stream over a descriptor the caller owns.
/// Disposal never touches the descriptor: ownership stays with the
/// SafeHandle holder, so closing a stream cannot invalidate retained
/// descriptors. (Framework file streams take ownership, which is wrong
/// for this engine.)</summary>
internal sealed class UnownedFdStream : Stream
{
    private readonly int _fd;
    private readonly FileAccess _access;

    public UnownedFdStream(int fd, FileAccess access)
    {
        _fd = fd;
        _access = access;
    }

    public override bool CanRead => _access != FileAccess.Write;

    public override bool CanWrite => _access != FileAccess.Read;

    public override bool CanSeek => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var errno = PreparationInterop.ReadChunk(_fd, buffer, offset, count, out var bytesRead);
        if (errno != 0)
        {
            throw new IOException($"Native read failed with errno {errno}.");
        }

        return bytesRead;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        var errno = PreparationInterop.WriteAll(_fd, buffer, offset, count);
        if (errno != 0)
        {
            throw new IOException($"Native write failed with errno {errno}.");
        }
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();
}
