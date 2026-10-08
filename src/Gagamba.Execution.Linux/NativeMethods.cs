// Linux libc interop owned by the Linux provider. All calls go through
// errno-returning functions (posix_spawn returns the error number
// directly); nothing here throws for expected failures.
using System.Runtime.InteropServices;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Gagamba.Execution.Linux.Tests")]

namespace Gagamba.Execution.Linux;

internal static class NativeMethods
{
    // Spawn flag that makes glibc pass the setcgroup_np fd to clone3 as
    // CLONE_INTO_CGROUP (child born inside the cgroup: atomic placement).
    // Value determined BEHAVIORALLY on glibc 2.43/x86-64 (strace shows
    // clone3 ... CLONE_INTO_CGROUP ... cgroup=FD only with this bit; the
    // from-memory 0x40 is POSIX_SPAWN_USEVFORK and silently disables
    // placement). VerifyAtomicPlacement in the provider re-proves this on
    // every new machine instead of trusting the constant.
    public const short POSIX_SPAWN_SETCGROUP = 0x100;
    public const int O_RDONLY = 0;
    public const int O_DIRECTORY = 0x10000; // 00200000 oct on x86-64; verified vs os.O_DIRECTORY
    public const int O_CLOEXEC = 0x80000;
    public const int EOPNOTSUPP = 95;
    public const int ENOENT = 2;
    public const int ESRCH = 3;
    public const int EACCES = 13;
    public const int ENOSYS = 38;
    public const int WNOHANG = 1;
    public const int ECHILD = 10;
    public const int EINTR = 4;

    [DllImport("libc")]
    internal static extern IntPtr gnu_get_libc_version();

    [DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int posix_spawn(
        out int pid,
        string path,
        IntPtr fileActions,
        IntPtr attrp,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string?[] argv,
        [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string?[] envp);

    [DllImport("libc")]
    internal static extern int posix_spawnattr_init(IntPtr attr);

    [DllImport("libc")]
    internal static extern int posix_spawnattr_destroy(IntPtr attr);

    [DllImport("libc")]
    internal static extern int posix_spawnattr_setflags(IntPtr attr, short flags);

    [DllImport("libc")]
    internal static extern int posix_spawnattr_setcgroup_np(IntPtr attr, int fd);

    [DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi)]
    internal static extern int open(string pathname, int flags);

    [DllImport("libc", SetLastError = true)]
    internal static extern int close(int fd);

    [DllImport("libc")]
    internal static extern int posix_spawn_file_actions_init(IntPtr actions);

    [DllImport("libc")]
    internal static extern int posix_spawn_file_actions_destroy(IntPtr actions);

    [DllImport("libc", CharSet = CharSet.Ansi)]
    internal static extern int posix_spawn_file_actions_addchdir_np(IntPtr actions, string path);

    [DllImport("libc")]
    internal static extern int posix_spawn_file_actions_addclosefrom_np(IntPtr actions, int from);

    [DllImport("libc", SetLastError = true)]
    internal static extern int kill(int pid, int sig);

    [DllImport("libc", SetLastError = true)]
    internal static extern int waitpid(int pid, out int status, int options);

    /// <summary>Split a command line into argv (whitespace, quotes, backslash).</summary>
    internal static string[] SplitArguments(string commandLine)
    {
        var args = new List<string>();
        var cur = new StringBuilder();
        bool inSingle = false, inDouble = false, have = false;
        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];
            if (c == '\\' && i + 1 < commandLine.Length && !inSingle)
            {
                cur.Append(commandLine[i + 1]);
                have = true;
                i++;
            }
            else if (c == '\'' && !inDouble) { inSingle = !inSingle; have = true; }
            else if (c == '"' && !inSingle) { inDouble = !inDouble; have = true; }
            else if (char.IsWhiteSpace(c) && !inSingle && !inDouble)
            {
                if (have) { args.Add(cur.ToString()); cur.Clear(); have = false; }
            }
            else { cur.Append(c); have = true; }
        }
        if (have) args.Add(cur.ToString());
        return args.ToArray();
    }
}
