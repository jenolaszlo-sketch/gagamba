using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation.Native;

namespace Gagamba.Execution.Linux.M1.Native;

internal static class NativeLaunchInterop
{
    internal const int O_RDONLY = 0;
    internal const int O_WRONLY = 1;
    internal const int O_ACCMODE = 3;
    internal const int O_CLOEXEC = 0x80000;
    internal const int O_NOFOLLOW = 0x20000;
    internal const int O_NONBLOCK = 0x800;
    internal const int F_DUPFD_CLOEXEC = 1030;
    internal const int F_GETFD = 1;
    internal const int F_GETFL = 3;
    internal const int F_SETFL = 4;
    internal const int FD_CLOEXEC = 1;
    internal const int EAGAIN = 11;
    internal const int EPIPE = 32;
    internal const int SIGPIPE = 13;
    internal const int POSIX_SPAWN_SETCGROUP = 0x100;
    internal const int WNOHANG = 1;
    internal const int EINTR = 4;
    internal const int SIGCHLD = 17;
    private const int PidFdWaitId = 3;
    private const int WaitExited = 4;
    private const int ChildExited = 1;
    private const int ChildKilled = 2;
    private const int ChildDumped = 3;
    internal const int MFD_CLOEXEC = 1;
    internal const int MFD_ALLOW_SEALING = 2;
    internal const int F_ADD_SEALS = 1033;
    internal const int F_GET_SEALS = 1034;
    internal const int F_SEAL_SEAL = 1;
    internal const int F_SEAL_SHRINK = 2;
    internal const int F_SEAL_GROW = 4;
    internal const int F_SEAL_WRITE = 8;
    internal const int PIPE_BUF = 4096;
    private const uint S_IFMT = 0xF000;
    private const uint S_IFREG = 0x8000;
    private const uint S_IFIFO = 0x1000;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PollFd
    {
        public int Fd;
        public short Events;
        public short Revents;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StatX64
    {
        public ulong Dev;
        public ulong Ino;
        public ulong Nlink;
        public uint Mode;
        public uint Uid;
        public uint Gid;
        public uint Pad;
        public ulong Rdev;
        public long Size;
        public long Blksize;
        public long Blocks;
        public long AtimSec, AtimNsec, MtimSec, MtimNsec, CtimSec, CtimNsec;
        public long Unused0, Unused1, Unused2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StatArm64
    {
        public ulong Dev, Ino;
        public uint Mode, Nlink, Uid, Gid;
        public ulong Rdev, Pad;
        public long Size;
        public int Blksize, Pad2;
        public long Blocks, AtimSec, AtimNsec, MtimSec, MtimNsec, CtimSec, CtimNsec, Unused0, Unused1;
    }

    [StructLayout(LayoutKind.Explicit, Size = 128)]
    private struct ChildSignalInfo
    {
        [FieldOffset(0)] internal int Signal;
        [FieldOffset(8)] internal int Code;
        [FieldOffset(16)] internal int Pid;
        [FieldOffset(20)] internal uint Uid;
        [FieldOffset(24)] internal int Status;
    }

    [DllImport("libc", EntryPoint = "gnu_get_libc_version")]
    private static extern IntPtr GnuGetLibcVersion();
    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    internal static extern int Open(string path, int flags);
    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    internal static extern int Close(int fd);
    [DllImport("libc", EntryPoint = "fcntl", SetLastError = true)]
    internal static extern int Fcntl(int fd, int command, int argument);
    [DllImport("libc", EntryPoint = "pipe2", SetLastError = true)]
    private static extern int Pipe2([Out] int[] fds, int flags);
    [DllImport("libc", EntryPoint = "memfd_create", SetLastError = true)]
    private static extern int MemfdCreate(string name, uint flags);
    [DllImport("libc", EntryPoint = "write", SetLastError = true)]
    private static extern nint Write(int fd, byte[] buffer, nuint count);
    [DllImport("libc", EntryPoint = "signal", SetLastError = true)]
    private static extern IntPtr Signal(int signal, IntPtr handler);
    [DllImport("libc", EntryPoint = "read", SetLastError = true)]
    internal static extern nint Read(int fd, byte[] buffer, nuint count);
    [DllImport("libc", EntryPoint = "pread", SetLastError = true)]
    internal static extern nint ReadAt(int fd, byte[] buffer, nuint count, long offset);
    [DllImport("libc", EntryPoint = "lseek", SetLastError = true)]
    private static extern long Lseek(int fd, long offset, int whence);
    [DllImport("libc", EntryPoint = "poll", SetLastError = true)]
    internal static extern int Poll([In, Out] PollFd[] fds, nuint count, int timeout);
    [DllImport("libc", EntryPoint = "posix_spawn", SetLastError = true)]
    private static extern int PosixSpawn(
        out int pid, string path, IntPtr fileActions, IntPtr attributes,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string?[] argv,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string?[] envp);
    [DllImport("libc", EntryPoint = "posix_spawnattr_init")]
    private static extern int SpawnAttrInit(IntPtr attributes);
    [DllImport("libc", EntryPoint = "posix_spawnattr_destroy")]
    private static extern int SpawnAttrDestroy(IntPtr attributes);
    [DllImport("libc", EntryPoint = "posix_spawnattr_setflags")]
    private static extern int SpawnAttrSetFlags(IntPtr attributes, short flags);
    [DllImport("libc", EntryPoint = "posix_spawnattr_setcgroup_np")]
    private static extern int SpawnAttrSetCgroup(IntPtr attributes, int cgroupFd);
    [DllImport("libc", EntryPoint = "posix_spawn_file_actions_init")]
    private static extern int FileActionsInit(IntPtr actions);
    [DllImport("libc", EntryPoint = "posix_spawn_file_actions_destroy")]
    private static extern int FileActionsDestroy(IntPtr actions);
    [DllImport("libc", EntryPoint = "posix_spawn_file_actions_adddup2")]
    private static extern int FileActionsDup2(IntPtr actions, int fd, int newFd);
    [DllImport("libc", EntryPoint = "posix_spawn_file_actions_addclosefrom_np")]
    private static extern int FileActionsCloseFrom(IntPtr actions, int first);
    [DllImport("libc", EntryPoint = "waitpid", SetLastError = true)]
    internal static extern int WaitPid(int pid, out int status, int options);
    [DllImport("libc", EntryPoint = "waitid", SetLastError = true)]
    private static extern int WaitId(int idType, uint id, IntPtr info, int options);
    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long Syscall(long number, long arg1, long arg2, long arg3, long arg4);
    [DllImport("libc", EntryPoint = "fgetxattr", SetLastError = true)]
    private static extern nint FGetXattr(int fd, string name, byte[] value, nuint size);
    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static extern int FStatX64(int fd, out StatX64 stat);
    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static extern int FStatArm64(int fd, out StatArm64 stat);

    internal static bool TryWaitPidFd(int pidfd, out int exitCode, out string error)
    {
        exitCode = -1;
        error = string.Empty;
        if (!OperatingSystem.IsLinux() || pidfd < 0)
        {
            error = "invalid Linux pidfd wait request";
            return false;
        }

        var info = Marshal.AllocHGlobal(128);
        try
        {
            Marshal.Copy(new byte[128], 0, info, 128);
            int result;
            do
            {
                result = WaitId(PidFdWaitId, (uint)pidfd, info, WaitExited);
            }
            while (result != 0 && Marshal.GetLastPInvokeError() == EINTR);

            if (result != 0)
            {
                error = $"waitid(P_PIDFD) errno {Marshal.GetLastPInvokeError()}";
                return false;
            }

            var child = Marshal.PtrToStructure<ChildSignalInfo>(info);
            if (child.Signal != SIGCHLD || child.Pid <= 0)
            {
                error = "waitid(P_PIDFD) returned no child exit record";
                return false;
            }

            exitCode = child.Code switch
            {
                ChildExited => child.Status,
                ChildKilled or ChildDumped => 128 + child.Status,
                _ => -1,
            };
            if (exitCode < 0)
            {
                error = $"waitid(P_PIDFD) returned unexpected child state {child.Code}";
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            error = $"Unsupported pidfd wait primitive: {ex.GetType().Name}";
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(info);
        }
    }

    internal static string LibcVersion
    {
        get
        {
            try { return Marshal.PtrToStringAnsi(GnuGetLibcVersion()) ?? string.Empty; }
            catch { return string.Empty; }
        }
    }

    internal static bool HasExpectedTypeAndAccess(int slot, int fd, out string error)
    {
        error = string.Empty;
        var flags = Fcntl(fd, F_GETFL, 0);
        if (flags < 0)
        {
            error = $"FD {slot} access flags unavailable";
            return false;
        }
        var expectedAccess = slot is 0 or 3 or 4 or 6 or 7 or 8 or 9 or 11 ? O_RDONLY : O_WRONLY;
        var expectedType = slot is 0 or 1 or 2 or 4 or 5 or 10 ? S_IFIFO : S_IFREG;
        if ((flags & O_ACCMODE) != expectedAccess
            || PreparationInterop.StatFd(fd, out var stat) != 0
            || (stat.Mode & S_IFMT) != expectedType)
        {
            error = $"FD {slot} has the wrong file type or access mode";
            return false;
        }
        return true;
    }

    internal static bool TryOpen(string path, out int fd, out string error)
    {
        fd = -1;
        error = string.Empty;
        if (!OperatingSystem.IsLinux()) { error = "Unsupported: Linux required"; return false; }
        fd = Open(path, O_RDONLY | O_CLOEXEC | O_NOFOLLOW);
        if (fd >= 0) return true;
        error = $"open errno {Marshal.GetLastWin32Error()}";
        return false;
    }

    internal static bool TryStatOwnerMode(int fd, out uint uid, out uint gid, out uint mode)
    {
        uid = gid = mode = 0;
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            if (FStatX64(fd, out var st) != 0) return false;
            uid = st.Uid; gid = st.Gid; mode = st.Mode;
            return true;
        }
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            if (FStatArm64(fd, out var st) != 0) return false;
            uid = st.Uid; gid = st.Gid; mode = st.Mode;
            return true;
        }
        return false;
    }

    internal static bool TryStatIdentity(int fd, out ulong device, out ulong inode, out uint mode)
    {
        device = inode = 0;
        mode = 0;
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            if (FStatX64(fd, out var st) != 0) return false;
            device = st.Dev; inode = st.Ino; mode = st.Mode;
            return true;
        }
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            if (FStatArm64(fd, out var st) != 0) return false;
            device = st.Dev; inode = st.Ino; mode = st.Mode;
            return true;
        }
        return false;
    }

    internal static bool TryGetFileLength(int fd, out long length)
    {
        length = -1;
        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            if (FStatX64(fd, out var st) != 0) return false;
            length = st.Size;
            return true;
        }
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            if (FStatArm64(fd, out var st) != 0) return false;
            length = st.Size;
            return true;
        }
        return false;
    }

    internal static bool HasFileCapabilityXattr(int fd, out string error)
    {
        error = string.Empty;
        var value = new byte[64];
        var length = FGetXattr(fd, "security.capability", value, (nuint)value.Length);
        if (length >= 0)
        {
            if (length == 0) return false;
            error = "Bubblewrap executable carries security.capability";
            return true;
        }
        var errno = Marshal.GetLastWin32Error();
        if (errno == 61) return false; // ENODATA
        error = $"security.capability inspection errno {errno}";
        return true;
    }

    internal static bool TryCreatePipe(out Microsoft.Win32.SafeHandles.SafeFileHandle? read,
        out Microsoft.Win32.SafeHandles.SafeFileHandle? write, out string error)
    {
        read = write = null;
        error = string.Empty;
        var fds = new int[2];
        if (Pipe2(fds, O_CLOEXEC) != 0)
        {
            error = $"pipe2 errno {Marshal.GetLastWin32Error()}";
            return false;
        }
        read = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(fds[0]), ownsHandle: true);
        write = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(fds[1]), ownsHandle: true);
        return true;
    }

    internal static bool TrySetNonBlocking(int fd, out string error)
    {
        error = string.Empty;
        var flags = Fcntl(fd, F_GETFL, 0);
        if (flags < 0)
        {
            error = $"F_GETFL errno {Marshal.GetLastWin32Error()}";
            return false;
        }

        if (Fcntl(fd, F_SETFL, flags | O_NONBLOCK) < 0)
        {
            error = $"F_SETFL errno {Marshal.GetLastWin32Error()}";
            return false;
        }

        return true;
    }

    internal static bool TryIgnoreSigPipe(out string error)
    {
        error = string.Empty;
        var previous = Signal(SIGPIPE, new IntPtr(1));
        if (previous == new IntPtr(-1))
        {
            error = $"signal(SIGPIPE) errno {Marshal.GetLastWin32Error()}";
            return false;
        }

        return true;
    }

    internal static nint WriteBytesOnce(int fd, byte[] buffer)
    {
        var written = Write(fd, buffer, (nuint)buffer.Length);
        return written < 0 && Marshal.GetLastWin32Error() == EINTR ? -2 : written;
    }

    internal static int LastErrno => Marshal.GetLastWin32Error();

    internal static bool TryCreateSealedMemfd(string name, ReadOnlySpan<byte> contents,
        out Microsoft.Win32.SafeHandles.SafeFileHandle? handle, out string error)
    {
        handle = null;
        error = string.Empty;
        var fd = MemfdCreate(name, MFD_CLOEXEC | MFD_ALLOW_SEALING);
        if (fd < 0) { error = $"memfd_create errno {Marshal.GetLastWin32Error()}"; return false; }
        var owned = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(fd), ownsHandle: true);
        try
        {
            var bytes = contents.ToArray();
            var offset = 0;
            while (offset < bytes.Length)
            {
                var written = Write(fd, bytes.AsSpan(offset).ToArray(), (nuint)(bytes.Length - offset));
                if (written < 0 && Marshal.GetLastWin32Error() == EINTR) continue;
                if (written <= 0)
                {
                    error = $"memfd write failed errno {Marshal.GetLastWin32Error()}";
                    owned.Dispose();
                    return false;
                }
                offset += checked((int)written);
            }

            if (Lseek(fd, 0, 0) != 0
                || Fcntl(fd, F_ADD_SEALS, F_SEAL_WRITE | F_SEAL_GROW | F_SEAL_SHRINK | F_SEAL_SEAL) < 0)
            {
                error = $"memfd seal failed errno {Marshal.GetLastWin32Error()}";
                owned.Dispose();
                return false;
            }
            var seals = Fcntl(fd, F_GET_SEALS, 0);
            if ((seals & (F_SEAL_WRITE | F_SEAL_GROW | F_SEAL_SHRINK | F_SEAL_SEAL))
                != (F_SEAL_WRITE | F_SEAL_GROW | F_SEAL_SHRINK | F_SEAL_SEAL))
            {
                error = "memfd seals did not read back";
                owned.Dispose();
                return false;
            }
            if (!TryStatIdentity(fd, out var device, out var inode, out _))
            {
                error = "sealed memfd object identity unavailable";
                owned.Dispose();
                return false;
            }
            var readFd = Open($"/proc/self/fd/{fd}", O_RDONLY | O_CLOEXEC);
            if (readFd < 0 || !TryStatIdentity(readFd, out var readDevice, out var readInode, out _)
                || readDevice != device || readInode != inode || (Fcntl(readFd, F_GET_SEALS, 0) & F_SEAL_WRITE) == 0)
            {
                if (readFd >= 0) Close(readFd);
                error = "sealed memfd could not be reopened read-only as the same object";
                owned.Dispose();
                return false;
            }
            owned.Dispose();
            handle = new Microsoft.Win32.SafeHandles.SafeFileHandle(new IntPtr(readFd), ownsHandle: true);
            return true;
        }
        catch
        {
            owned.Dispose();
            throw;
        }
    }

    internal static bool TrySpawn(
        DomainLease lease,
        LaunchDescriptorMap map,
        IReadOnlyList<string> bwrapArguments,
        out int pid,
        out string error)
    {
        pid = -1;
        error = string.Empty;
        if (!OperatingSystem.IsLinux())
        {
            error = "Unsupported: Linux required";
            return false;
        }
        if (!Version.TryParse(LibcVersion, out var version) || version < new Version(2, 43))
        {
            error = $"Unsupported libc version {LibcVersion}; atomic setcgroup spawn requires glibc 2.43+";
            return false;
        }
        if (!PreparationInterop.HasCloexec(map[LaunchDescriptorRole.PinnedBubblewrap]))
        {
            error = "pinned Bubblewrap FD is not CLOEXEC";
            return false;
        }

        var cgroupFd = lease.DuplicateDomainDirectoryFd();
        if (cgroupFd < 0 || !PreparationInterop.HasCloexec(cgroupFd))
        {
            if (cgroupFd >= 0) Close(cgroupFd);
            error = "retained cgroup directory duplication failed or lacked CLOEXEC";
            return false;
        }

        IntPtr attr = IntPtr.Zero, actions = IntPtr.Zero;
        bool attrReady = false, actionsReady = false;
        var temporaries = new List<int>(12);
        try
        {
            attr = Marshal.AllocHGlobal(1024);
            int rc = SpawnAttrInit(attr);
            if (rc != 0) { error = $"posix_spawnattr_init errno {rc}"; return false; }
            attrReady = true;
            rc = SpawnAttrSetFlags(attr, POSIX_SPAWN_SETCGROUP);
            if (rc != 0) { error = $"posix_spawnattr_setflags errno {rc}"; return false; }
            rc = SpawnAttrSetCgroup(attr, cgroupFd);
            if (rc != 0) { error = $"posix_spawnattr_setcgroup_np errno {rc}"; return false; }

            actions = Marshal.AllocHGlobal(1024);
            rc = FileActionsInit(actions);
            if (rc != 0) { error = $"posix_spawn_file_actions_init errno {rc}"; return false; }
            actionsReady = true;
            foreach (var descriptor in map.Entries)
            {
                var temporary = Fcntl(descriptor.FileDescriptor, F_DUPFD_CLOEXEC, 64);
                if (temporary < 0) { error = $"FD {descriptor.Slot} source move errno {Marshal.GetLastWin32Error()}"; return false; }
                temporaries.Add(temporary);
                rc = FileActionsDup2(actions, temporary, descriptor.Slot);
                if (rc != 0) { error = $"posix_spawn dup2 action for FD {descriptor.Slot} errno {rc}"; return false; }
            }

            rc = FileActionsCloseFrom(actions, 12);
            if (rc != 0) { error = $"posix_spawn closefrom action errno {rc}"; return false; }

            var argv = new string?[bwrapArguments.Count + 2];
            argv[0] = HelperInstallation.PinnedPath;
            for (var i = 0; i < bwrapArguments.Count; i++) argv[i + 1] = bwrapArguments[i];
            argv[^1] = null;
            var envp = new string?[] { "LANG=C", null };
            rc = PosixSpawnInternal(out var child, "/proc/self/fd/11", actions, attr, argv, envp);
            if (rc != 0)
            {
                error = $"atomic Bubblewrap spawn refused errno {rc}";
                return false;
            }
            pid = child;
            return true;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            error = $"Unsupported atomic spawn primitive: {ex.GetType().Name}";
            return false;
        }
        finally
        {
            foreach (var fd in temporaries) Close(fd);
            if (actionsReady) FileActionsDestroy(actions);
            if (actions != IntPtr.Zero) Marshal.FreeHGlobal(actions);
            if (attrReady) SpawnAttrDestroy(attr);
            if (attr != IntPtr.Zero) Marshal.FreeHGlobal(attr);
            Close(cgroupFd);
        }
    }

    private static int PosixSpawnInternal(out int pid, string path, IntPtr actions, IntPtr attr,
        string?[] argv, string?[] envp) => PosixSpawn(out pid, path, actions, attr, argv, envp);
}
