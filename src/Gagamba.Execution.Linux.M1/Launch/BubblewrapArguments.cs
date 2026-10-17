namespace Gagamba.Execution.Linux.M1.Launch;

internal static class BubblewrapArguments
{
    internal static IReadOnlyList<string> Build() =>
    [
        "--unshare-user",
        "--unshare-pid",
        "--unshare-net",
        "--unshare-ipc",
        "--unshare-uts",
        "--uid", "0",
        "--gid", "0",
        "--disable-userns",
        "--assert-userns-disabled",
        "--cap-drop", "ALL",
        "--die-with-parent",
        "--new-session",
        "--clearenv",
        "--setenv", "HOME", "/tmp",
        "--setenv", "TMPDIR", "/tmp",
        "--setenv", "LANG", "C",
        "--dir", "/dev",
        "--dir", "/run",
        "--dir", "/sys",
        "--dir", "/home",
        "--ro-bind-fd", "7", "/tool",
        "--ro-bind-fd", "8", "/input",
        "--ro-bind-fd", "9", "/coordinator",
        "--proc", "/proc",
        "--remount-ro", "/proc",
        "--size", "1048576",
        "--tmpfs", "/dev/shm",
        "--size", "7340032",
        "--tmpfs", "/tmp",
        "--chdir", "/tmp",
        "--remount-ro", "/",
        "--seccomp", "6",
        "--json-status-fd", "10",
        "--", "/coordinator",
    ];
}
