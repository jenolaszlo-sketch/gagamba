// WP02 narrow Linux interop: descriptor-relative acquisition, staging,
// identity stat and durability primitives. Every function returns a Linux
// errno (0 on success); expected failures never throw. Non-Linux callers
// receive Unsupported without invoking native code. This file must not
// grow into a general syscall library: only WP02's primitives live here.
using System.Runtime.InteropServices;

namespace Gagamba.Execution.Linux.M1.Preparation.Native;

/// <summary>How openat2 reached the kernel on this host.</summary>
internal enum Openat2SupportKind : byte
{
    Unsupported = 0,
    LibcSymbol = 1,
    RawSyscall = 2,
}

/// <summary>Identity fields captured from fstat: filesystem object
/// identity plus type/mode/link/size facts used for binding.</summary>
internal readonly record struct FileIdentity(
    ulong Device,
    ulong Inode,
    uint Mode,
    ulong LinkCount,
    long Size,
    long Blocks512);

internal static class PreparationInterop
{
    internal const int AT_FDCWD = -100;

    internal const int O_RDONLY = 0;
    internal const int O_WRONLY = 1;
    internal const int O_CREAT = 0x40;
    internal const int O_EXCL = 0x80;
    internal const int O_NOFOLLOW = 0x20000;
    internal const int O_CLOEXEC = 0x80000;
    internal const int O_DIRECTORY = 0x10000;

    internal const ulong RESOLVE_NO_XDEV = 0x01;
    internal const ulong RESOLVE_NO_MAGICLINKS = 0x02;
    internal const ulong RESOLVE_NO_SYMLINKS = 0x04;
    internal const ulong RESOLVE_BENEATH = 0x08;
    internal const ulong RESOLVE_CACHED = 0x20;
    internal const ulong RequiredResolve =
        RESOLVE_BENEATH | RESOLVE_NO_SYMLINKS | RESOLVE_NO_MAGICLINKS;

    // An invalid resolve bit for the availability probe: above every bit
    // the kernel defines, so a supporting kernel answers EINVAL while a
    // kernel without openat2 answers ENOSYS. Diagnostic only; every real
    // call still handles ENOSYS fail-closed at the call site.
    internal const ulong ProbeInvalidResolve = 1UL << 40;

    internal const int EPERM = 1;
    internal const int ENOENT = 2;
    internal const int EINTR = 4;
    internal const int EIO = 5;
    internal const int EBADF = 9;
    internal const int ENOMEM = 12;
    internal const int EACCES = 13;
    internal const int EFAULT = 14;
    internal const int EEXIST = 17;
    internal const int ENOTDIR = 20;
    internal const int EISDIR = 21;
    internal const int EINVAL = 22;
    internal const int EFBIG = 27;
    internal const int ENOSPC = 28;
    internal const int EROFS = 30;
    internal const int ENOSYS = 38;
    internal const int ELOOP = 40;
    internal const int ENAMETOOLONG = 36;
    internal const int EDQUOT = 122;

    internal const uint S_IFMT = 0xF000;
    internal const uint S_IFREG = 0x8000;
    internal const uint S_IFDIR = 0x4000;

    internal const uint Mode0600 = 0x180;
    internal const uint Mode0500 = 0x140;
    internal const uint Mode0400 = 0x100;
    internal const uint Mode0700 = 0x1C0;

    // Pinned openat2 syscall numbers for explicitly supported
    // architectures. Any other architecture fails closed.
    internal const long SysOpenat2X64 = 437;
    internal const long SysOpenat2Arm64 = 437;

    [StructLayout(LayoutKind.Sequential)]
    internal struct OpenHow
    {
        public ulong Flags;
        public ulong Mode;
        public ulong Resolve;
    }

    // glibc x86-64 struct stat (144 bytes, fully laid out so the kernel
    // never writes past the buffer).
    [StructLayout(LayoutKind.Sequential)]
    internal struct StatX64
    {
        public ulong StDev;
        public ulong StIno;
        public ulong StNlink;
        public uint StMode;
        public uint StUid;
        public uint StGid;
        public uint Pad0;
        public ulong StRdev;
        public long StSize;
        public long StBlksize;
        public long StBlocks;
        public long AtimSec;
        public long AtimNsec;
        public long MtimSec;
        public long MtimNsec;
        public long CtimSec;
        public long CtimNsec;
        public long Unused0;
        public long Unused1;
        public long Unused2;
    }

    // glibc AArch64 struct stat (128 bytes, fully laid out).
    [StructLayout(LayoutKind.Sequential)]
    internal struct StatArm64
    {
        public ulong StDev;
        public ulong StIno;
        public uint StMode;
        public uint StNlink;
        public uint StUid;
        public uint StGid;
        public ulong StRdev;
        public ulong Pad1;
        public long StSize;
        public int StBlksize;
        public int Pad2;
        public long StBlocks;
        public long AtimSec;
        public long AtimNsec;
        public long MtimSec;
        public long MtimNsec;
        public long CtimSec;
        public long CtimNsec;
        public long Unused0;
        public long Unused1;
    }

    [DllImport("libc", EntryPoint = "openat2", SetLastError = true)]
    private static extern int LibcOpenat2(int dirfd, string path, ref OpenHow how, IntPtr size);

    [DllImport("libc", EntryPoint = "syscall", SetLastError = true)]
    private static extern long LibcSyscall(long number, IntPtr arg1, IntPtr arg2, IntPtr arg3, IntPtr arg4);

    [DllImport("libc", EntryPoint = "openat", SetLastError = true)]
    private static extern int LibcOpenat(int dirfd, string path, int flags, uint mode);

    [DllImport("libc", EntryPoint = "mkdirat", SetLastError = true)]
    private static extern int LibcMkdirat(int dirfd, string path, uint mode);

    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static extern int LibcFstatX64(int fd, out StatX64 buf);

    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
    private static extern int LibcFstatArm64(int fd, out StatArm64 buf);

    [DllImport("libc", EntryPoint = "fchmod", SetLastError = true)]
    private static extern int LibcFchmod(int fd, uint mode);

    [DllImport("libc", EntryPoint = "unlinkat", SetLastError = true)]
    private static extern int LibcUnlinkat(int dirfd, string path, int flags);

    [DllImport("libc", EntryPoint = "fsync", SetLastError = true)]
    private static extern int LibcFsync(int fd);

    [DllImport("libc", EntryPoint = "read", SetLastError = true)]
    private static extern long LibcRead(int fd, IntPtr buffer, UIntPtr count);

    [DllImport("libc", EntryPoint = "write", SetLastError = true)]
    private static extern long LibcWrite(int fd, IntPtr buffer, UIntPtr count);

    internal static bool IsLinux => OperatingSystem.IsLinux();

    internal static bool IsSupportedArchitecture() =>
        RuntimeInformation.ProcessArchitecture is Architecture.X64 or Architecture.Arm64;

    internal static long SysOpenat2ForCurrentArchitecture() =>
        RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => SysOpenat2X64,
            Architecture.Arm64 => SysOpenat2Arm64,
            _ => -1,
        };

    /// <summary>Runtime openat2 availability. Never infers from kernel,
    /// glibc, or architecture strings: it issues the call and reads the
    /// kernel's answer. ENOSYS means Unsupported; any other errno proves
    /// the call reached a supporting kernel.</summary>
    internal static Openat2SupportKind ProbeOpenat2Support()
    {
        if (!IsLinux || !IsSupportedArchitecture())
        {
            return Openat2SupportKind.Unsupported;
        }

        var how = new OpenHow
        {
            Flags = O_RDONLY | O_CLOEXEC,
            Mode = 0,
            Resolve = ProbeInvalidResolve,
        };
        try
        {
            var fd = LibcOpenat2(AT_FDCWD, "/", ref how, (IntPtr)24);
            if (fd >= 0)
            {
                LibcClose(fd);
                return Openat2SupportKind.LibcSymbol;
            }

            return Marshal.GetLastWin32Error() == ENOSYS
                ? TryRawProbe()
                : Openat2SupportKind.LibcSymbol;
        }
        catch (EntryPointNotFoundException)
        {
            return TryRawProbe();
        }
        catch (DllNotFoundException)
        {
            return Openat2SupportKind.Unsupported;
        }
    }

    private static Openat2SupportKind TryRawProbe()
    {
        var number = SysOpenat2ForCurrentArchitecture();
        if (number < 0)
        {
            return Openat2SupportKind.Unsupported;
        }

        var pathBytes = System.Text.Encoding.UTF8.GetBytes("/\0");
        var how = new OpenHow
        {
            Flags = O_RDONLY | O_CLOEXEC,
            Mode = 0,
            Resolve = ProbeInvalidResolve,
        };
        var pathHandle = GCHandle.Alloc(pathBytes, GCHandleType.Pinned);
        var howHandle = GCHandle.Alloc(how, GCHandleType.Pinned);
        try
        {
            var result = LibcSyscall(
                number,
                (IntPtr)AT_FDCWD,
                pathHandle.AddrOfPinnedObject(),
                howHandle.AddrOfPinnedObject(),
                (IntPtr)24);
            // Raw syscalls return -errno directly, never via errno.
            if (result >= 0)
            {
                LibcClose((int)result);
                return Openat2SupportKind.RawSyscall;
            }

            return result == -ENOSYS
                ? Openat2SupportKind.Unsupported
                : Openat2SupportKind.RawSyscall;
        }
        catch (EntryPointNotFoundException)
        {
            return Openat2SupportKind.Unsupported;
        }
        catch (DllNotFoundException)
        {
            return Openat2SupportKind.Unsupported;
        }
        finally
        {
            pathHandle.Free();
            howHandle.Free();
        }
    }

    [DllImport("libc", EntryPoint = "close")]
    private static extern int LibcClose(int fd);

    /// <summary>Source acquisition: O_RDONLY, O_NOFOLLOW, O_CLOEXEC with
    /// the required resolve policy. No O_CREAT, no write, no truncation,
    /// no fallback: ENOSYS (or an unavailable path) is reported, never
    /// weakened.</summary>
    internal static int OpenSource(int dirfd, string name, ulong resolve, out int fd) =>
        OpenSourceWithResolve(dirfd, name, resolve, out fd);

    internal static int OpenSourceWithResolve(int dirfd, string name, ulong resolve, out int fd)
    {
        fd = -1;
        var how = new OpenHow
        {
            Flags = (ulong)(O_RDONLY | O_NOFOLLOW | O_CLOEXEC),
            Mode = 0,
            Resolve = resolve,
        };
        try
        {
            var result = LibcOpenat2(dirfd, name, ref how, (IntPtr)24);
            if (result < 0)
            {
                return Marshal.GetLastWin32Error();
            }

            fd = result;
            return 0;
        }
        catch (EntryPointNotFoundException)
        {
            return OpenSourceRaw(dirfd, name, resolve, out fd);
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    internal static int OpenSourceRaw(int dirfd, string name, ulong resolve, out int fd)
    {
        fd = -1;
        var number = SysOpenat2ForCurrentArchitecture();
        if (number < 0)
        {
            return ENOSYS;
        }

        var pathBytes = System.Text.Encoding.UTF8.GetBytes(name + "\0");
        var how = new OpenHow
        {
            Flags = (ulong)(O_RDONLY | O_NOFOLLOW | O_CLOEXEC),
            Mode = 0,
            Resolve = resolve,
        };
        var pathHandle = GCHandle.Alloc(pathBytes, GCHandleType.Pinned);
        var howHandle = GCHandle.Alloc(how, GCHandleType.Pinned);
        try
        {
            var result = LibcSyscall(
                number,
                (IntPtr)dirfd,
                pathHandle.AddrOfPinnedObject(),
                howHandle.AddrOfPinnedObject(),
                (IntPtr)24);
            if (result >= 0)
            {
                if (result > int.MaxValue)
                {
                    LibcClose((int)result);
                    return EINVAL;
                }

                fd = (int)result;
                return 0;
            }

            // Raw syscalls return -errno directly.
            return result >= -4095 ? -(int)result : EINVAL;
        }
        catch (EntryPointNotFoundException)
        {
            return ENOSYS;
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
        finally
        {
            pathHandle.Free();
            howHandle.Free();
        }
    }

    internal static int OpenDirectory(string path, out int fd)
    {
        fd = -1;
        try
        {
            var result = LibcOpenat(
                AT_FDCWD, path, O_RDONLY | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC, 0);
            if (result < 0)
            {
                return Marshal.GetLastWin32Error();
            }

            fd = result;
            return 0;
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    internal static int CreateExclusive(int dirfd, string name, out int fd)
    {
        fd = -1;
        try
        {
            var result = LibcOpenat(
                dirfd, name, O_WRONLY | O_CREAT | O_EXCL | O_CLOEXEC, Mode0600);
            if (result < 0)
            {
                return Marshal.GetLastWin32Error();
            }

            fd = result;
            return 0;
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    internal static int OpenSubdirectory(int dirfd, string name, out int fd)
    {
        fd = -1;
        try
        {
            var result = LibcOpenat(
                dirfd, name, O_RDONLY | O_DIRECTORY | O_NOFOLLOW | O_CLOEXEC, 0);
            if (result < 0)
            {
                return Marshal.GetLastWin32Error();
            }

            fd = result;
            return 0;
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    internal static int MakeDirectory(int dirfd, string name, uint mode)
    {
        try
        {
            return LibcMkdirat(dirfd, name, mode) == 0 ? 0 : Marshal.GetLastWin32Error();
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    internal static int StatFd(int fd, out FileIdentity identity)
    {
        identity = default;
        try
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                if (LibcFstatX64(fd, out var st) != 0)
                {
                    return Marshal.GetLastWin32Error();
                }

                identity = new FileIdentity(st.StDev, st.StIno, st.StMode, st.StNlink, st.StSize, st.StBlocks);
                return 0;
            }

            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                if (LibcFstatArm64(fd, out var st) != 0)
                {
                    return Marshal.GetLastWin32Error();
                }

                identity = new FileIdentity(st.StDev, st.StIno, st.StMode, st.StNlink, st.StSize, st.StBlocks);
                return 0;
            }

            return ENOSYS;
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    internal static bool IsRegularFile(uint mode) => (mode & S_IFMT) == S_IFREG;

    internal static bool IsDirectory(uint mode) => (mode & S_IFMT) == S_IFDIR;

    internal static int SetMode(int fd, uint mode)
    {
        try
        {
            return LibcFchmod(fd, mode) == 0 ? 0 : Marshal.GetLastWin32Error();
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    internal static int Unlink(int dirfd, string name)
    {
        try
        {
            return LibcUnlinkat(dirfd, name, 0) == 0 ? 0 : Marshal.GetLastWin32Error();
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    internal static int Sync(int fd)
    {
        try
        {
            int result;
            do
            {
                result = LibcFsync(fd);
            }
            while (result != 0 && Marshal.GetLastWin32Error() == EINTR);

            return result == 0 ? 0 : Marshal.GetLastWin32Error();
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
    }

    /// <summary>Unbuffered read into buffer[offset..]. Returns 0 and sets
    /// bytesRead on success (0 means end of file).</summary>
    internal static int ReadChunk(int fd, byte[] buffer, int offset, int count, out int bytesRead)
    {
        bytesRead = 0;
        var pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            while (true)
            {
                var result = LibcRead(
                    fd,
                    IntPtr.Add(pinned.AddrOfPinnedObject(), offset),
                    (UIntPtr)count);
                if (result < 0)
                {
                    var errno = Marshal.GetLastWin32Error();
                    if (errno == EINTR)
                    {
                        continue;
                    }

                    return errno;
                }

                bytesRead = (int)result;
                return 0;
            }
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
        finally
        {
            pinned.Free();
        }
    }

    /// <summary>Writes the full range unless an error occurs.</summary>
    internal static int WriteAll(int fd, byte[] buffer, int offset, int count)
    {
        var pinned = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var remaining = count;
            var position = offset;
            while (remaining > 0)
            {
                var result = LibcWrite(
                    fd,
                    IntPtr.Add(pinned.AddrOfPinnedObject(), position),
                    (UIntPtr)remaining);
                if (result < 0)
                {
                    var errno = Marshal.GetLastWin32Error();
                    if (errno == EINTR)
                    {
                        continue;
                    }

                    return errno;
                }

                if (result == 0)
                {
                    return EIO;
                }

                remaining -= (int)result;
                position += (int)result;
            }

            return 0;
        }
        catch (DllNotFoundException)
        {
            return ENOSYS;
        }
        finally
        {
            pinned.Free();
        }
    }
}
