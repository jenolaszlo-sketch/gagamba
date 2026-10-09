using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux;

// Child receives only dup2'd stdout/stderr. Parent read ends and original
// writers are CLOEXEC; addclosefrom(3) still closes all unrelated child FDs.
internal sealed class LinuxCapturePipes : IDisposable
{
    private int _outRead, _outWrite, _errRead, _errWrite;
    private LinuxCapturePipes(int[] stdout, int[] stderr)
    { _outRead = stdout[0]; _outWrite = stdout[1]; _errRead = stderr[0]; _errWrite = stderr[1]; }

    public static LinuxCapturePipes? Create(out string error)
    {
        error = "";
        int[] stdout = new int[2], stderr = new int[2];
        try
        {
            if (NativeMethods.pipe2(stdout, NativeMethods.O_CLOEXEC) != 0)
            { error = $"stdout pipe2 errno={Marshal.GetLastWin32Error()}"; return null; }
            if (NativeMethods.pipe2(stderr, NativeMethods.O_CLOEXEC) != 0)
            {
                int errno = Marshal.GetLastWin32Error();
                NativeMethods.close(stdout[0]); NativeMethods.close(stdout[1]);
                error = $"stderr pipe2 errno={errno}";
                return null;
            }
            var result = new LinuxCapturePipes(stdout, stderr);
            if (stdout.Concat(stderr).Any(fd => fd < 3))
            { result.Dispose(); error = "capture requires parent stdio descriptors 0-2 to be open"; return null; }
            return result;
        }
        catch (Exception ex)
        {
            foreach (int fd in stdout.Concat(stderr)) if (fd > 2) NativeMethods.close(fd);
            error = $"capture pipe prerequisite: {ex.GetType().Name}";
            return null;
        }
    }

    public int AddActions(IntPtr actions)
    {
        int rc = NativeMethods.posix_spawn_file_actions_adddup2(actions, _outWrite, 1);
        if (rc != 0) return rc;
        return NativeMethods.posix_spawn_file_actions_adddup2(actions, _errWrite, 2);
    }

    public void CloseWriters()
    {
        if (_outWrite >= 0) { NativeMethods.close(_outWrite); _outWrite = -1; }
        if (_errWrite >= 0) { NativeMethods.close(_errWrite); _errWrite = -1; }
    }

    public (Stream Stdout, Stream Stderr) TakeReaders()
    {
        var outHandle = new SafeFileHandle((IntPtr)_outRead, ownsHandle: true);
        var errHandle = new SafeFileHandle((IntPtr)_errRead, ownsHandle: true);
        _outRead = -1; _errRead = -1;
        try
        {
            var stdout = new FileStream(outHandle, FileAccess.Read, 4096, isAsync: false);
            try { return (stdout, new FileStream(errHandle, FileAccess.Read, 4096, isAsync: false)); }
            catch { stdout.Dispose(); throw; }
        }
        catch { outHandle.Dispose(); errHandle.Dispose(); throw; }
    }

    public void Dispose()
    {
        CloseWriters();
        if (_outRead >= 0) { NativeMethods.close(_outRead); _outRead = -1; }
        if (_errRead >= 0) { NativeMethods.close(_errRead); _errRead = -1; }
    }
}
