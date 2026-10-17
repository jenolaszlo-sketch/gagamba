using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Gagamba.Execution.Linux.M1.Native;

namespace Gagamba.Execution.Linux.M1.Launch;

/// <summary>Opens only the plan-pinned Bubblewrap installation and verifies
/// the opened object before it can enter the atomic spawn descriptor map.
/// The pathname is never used after opening and no PATH lookup exists.</summary>
internal static class HelperInstallation
{
    internal const string PinnedPath = "/usr/bin/bwrap";
    internal const string PinnedVersion = "0.11.1";
    internal const string PinnedSha256 = "523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6";

    internal static bool TryOpenPinnedBubblewrap(
        out SafeFileHandle? executable,
        out string error)
    {
        executable = null;
        error = string.Empty;
        if (!OperatingSystem.IsLinux())
        {
            error = "Bubblewrap is available only on the pinned Linux platform";
            return false;
        }

        if (!NativeLaunchInterop.TryOpen(PinnedPath, out var fd, out error)) return false;
        var handle = new SafeFileHandle(new IntPtr(fd), ownsHandle: true);
        try
        {
            if (!NativeLaunchInterop.TryStatOwnerMode(fd, out var uid, out var gid, out var mode)
                || uid != 0 || gid != 0 || (mode & 0xF000) != 0x8000 || (mode & 0x0FFF) != 0x01ED)
            {
                error = "opened Bubblewrap object is not root-owned regular mode 0755";
                return DisposeFailure(handle, out executable);
            }

            if (NativeLaunchInterop.HasFileCapabilityXattr(fd, out error))
            {
                if (error.Length == 0) error = "opened Bubblewrap object has file capabilities";
                return DisposeFailure(handle, out executable);
            }

            if (!TryHashOpenedObject(fd, out var digest, out error)
                || !string.Equals(Convert.ToHexString(digest).ToLowerInvariant(), PinnedSha256, StringComparison.Ordinal))
            {
                if (error.Length == 0) error = "opened Bubblewrap SHA-256 does not match the pinned 0.11.1 object";
                return DisposeFailure(handle, out executable);
            }

            if (!NativeLaunchInterop.TryStatIdentity(fd, out _, out _, out _)
                || !Gagamba.Execution.Linux.M1.Preparation.Native.PreparationInterop.HasCloexec(fd))
            {
                error = "opened Bubblewrap object identity or CLOEXEC state is unavailable";
                return DisposeFailure(handle, out executable);
            }

            executable = handle;
            return true;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    private static bool TryHashOpenedObject(int fd, out byte[] digest, out string error)
    {
        digest = Array.Empty<byte>();
        error = string.Empty;
        if (!NativeLaunchInterop.TryStatIdentity(fd, out _, out _, out var mode)
            || (mode & 0xF000) != 0x8000)
        {
            error = "opened Bubblewrap object is not a regular file";
            return false;
        }

        if (!NativeLaunchInterop.TryGetFileLength(fd, out var length) || length is <= 0 or > 64 * 1024 * 1024)
        {
            error = "opened Bubblewrap object length is outside the pinning bound";
            return false;
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[32 * 1024];
        long offset = 0;
        while (offset < length)
        {
            var want = (int)Math.Min(buffer.Length, length - offset);
            var read = NativeLaunchInterop.ReadAt(fd, buffer, (nuint)want, offset);
            if (read <= 0)
            {
                error = $"opened Bubblewrap object read failed errno {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}";
                return false;
            }

            hash.AppendData(buffer, 0, checked((int)read));
            offset += (long)read;
        }

        digest = hash.GetHashAndReset();
        return true;
    }

    private static bool DisposeFailure(SafeFileHandle handle, out SafeFileHandle? executable)
    {
        handle.Dispose();
        executable = null;
        return false;
    }
}
