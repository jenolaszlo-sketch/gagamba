// Native interop for the GW-1B minimal-launch staircase.
// Launch-API signatures follow Microsoft Learn createprocessinsandbox
// (2026-06-01); STARTUPINFO/PROCESS_INFORMATION are the standard Win32 shapes.
// Only the sandboxed-launch API plus wait/kill/cleanup primitives are declared.
using System.Runtime.InteropServices;
using System.Text;

namespace Gagamba.Spikes.Gw1bLaunch;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct StartupInfo
{
    public int cb;
    public string? lpReserved;
    public string? lpDesktop;
    public string? lpTitle;
    public int dwX;
    public int dwY;
    public int dwXSize;
    public int dwYSize;
    public int dwXCountChars;
    public int dwYCountChars;
    public int dwFillAttribute;
    public int dwFlags;
    public short wShowWindow;
    public short cbReserved2;
    public IntPtr lpReserved2;
    public IntPtr hStdInput;
    public IntPtr hStdOutput;
    public IntPtr hStdError;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ProcessInformation
{
    public IntPtr hProcess;
    public IntPtr hThread;
    public uint dwProcessId;
    public uint dwThreadId;
}

internal static class Native
{
    // BOOL Experimental_CreateProcessInSandbox(applicationName, commandLine,
    //   NULL, NULL, FALSE, creationFlags, environment, currentDirectory,
    //   startupInfo, identity, sandboxSpecification, sandboxSpecificationSize,
    //   processInformation). Reserved params stay NULL/FALSE per the contract.
    [DllImport("processmodel.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool Experimental_CreateProcessInSandbox(
        string? applicationName,
        [MarshalAs(UnmanagedType.LPWStr)] StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref StartupInfo startupInfo,
        string identity,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 11)] byte[] sandboxSpecification,
        uint sandboxSpecificationSize,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsProcessInJob(IntPtr hProcess, IntPtr hJob,
        [MarshalAs(UnmanagedType.Bool)] out bool result);

    // Marks a handle inheritable. The engine duplicates STARTUPINFO std handles
    // into the sandbox;/plain non-inheritable handles were rejected, so the
    // transport leg sets HANDLE_FLAG_INHERIT (0x1) explicitly.
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetHandleInformation(IntPtr hObject, uint dwMask, uint dwFlags);

    // Removes the per-user AppContainer profile the engine created for a
    // disposable test identity. userenv.dll hosts the profile APIs.
    // Returns an HRESULT (0 = S_OK).
    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    public static extern int DeleteAppContainerProfile(string pszAppContainerName);

    // Creates a per-user AppContainer profile (userenv). Diagnostic use only:
    // validates the Delete P/Invoke chain on a known profile.
    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    public static extern int CreateAppContainerProfile(string pszAppContainerName,
        string pszDisplayName, string pszDescription, IntPtr pCapabilities,
        uint dwCapabilityCount, out IntPtr ppSid);

    [DllImport("advapi32.dll")]
    public static extern IntPtr FreeSid(IntPtr pSid);

    public const uint INFINITE = 0xFFFFFFFF;
    public const uint WAIT_OBJECT_0 = 0;
    public const uint WAIT_TIMEOUT = 0x102;
    public const uint STILL_ACTIVE = 259;
}
