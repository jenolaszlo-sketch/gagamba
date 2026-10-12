// WP03 recovery-host subreaper support. The dedicated harness host sets
// PR_SET_CHILD_SUBREAPER and reads it back before creating any child, so
// it can reap adopted descendants after owner death. Refuse if the
// operation is unavailable: no weaker fallback is accepted.
using System.Runtime.InteropServices;

namespace Gagamba.Execution.Linux.M1.Native;

internal static class RecoverySubreaper
{
    private const int PR_SET_CHILD_SUBREAPER = 36;
    private const int PR_GET_CHILD_SUBREAPER = 37;

    [DllImport("libc", EntryPoint = "prctl", SetLastError = true)]
    private static extern int Prctl(int option, ulong arg2, ulong arg3, ulong arg4, ulong arg5);

    internal static bool IsLinux => OperatingSystem.IsLinux();

    /// <summary>Enables and verifies child-subreaper on this process.
    /// Fails closed when unavailable.</summary>
    internal static bool TryEnableAndVerify(out string error)
    {
        error = string.Empty;
        if (!IsLinux)
        {
            error = "Unsupported: not Linux";
            return false;
        }

        try
        {
            if (Prctl(PR_SET_CHILD_SUBREAPER, 1, 0, 0, 0) != 0)
            {
                error = "PR_SET_CHILD_SUBREAPER failed";
                return false;
            }

            // prctl does not directly return the value; read it through a
            // pointed-to int. Use a small managed buffer.
            var box = new int[1];
            var handle = GCHandle.Alloc(box, GCHandleType.Pinned);
            try
            {
                var rc = Prctl(PR_GET_CHILD_SUBREAPER, (ulong)handle.AddrOfPinnedObject(), 0, 0, 0);
                if (rc != 0)
                {
                    error = "PR_GET_CHILD_SUBREAPER failed";
                    return false;
                }
            }
            finally
            {
                handle.Free();
            }

            if (box[0] != 1)
            {
                error = "subreaper readback was not 1";
                return false;
            }

            return true;
        }
        catch (DllNotFoundException)
        {
            error = "Unsupported: prctl unavailable";
            return false;
        }
    }
}
