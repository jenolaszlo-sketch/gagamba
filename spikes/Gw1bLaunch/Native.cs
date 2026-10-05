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

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bInheritHandle;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe,
        ref SecurityAttributes lpPipeAttributes, uint nSize);

    [DllImport("kernel32.dll")]
    public static extern uint GetProcessId(IntPtr hProcess);

    // Plain Win32 process + Job Object API for GQ-1 ownership (not the
    // sandbox API): suspend-launch, assign-before-resume (no escape
    // window), kill-on-close, terminate. No quotas, no UI limits.
    public const uint CREATE_SUSPENDED = 0x4;
    public const int JobObjectExtendedLimitInformationClass = 9;
    public const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
    // Deliberately never set: breakaway would defeat tree ownership.
    public const uint JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x8;
    public const uint JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK = 0x1000;
    public const uint ERROR_ACCESS_DENIED = 5;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateProcessW(
        string? applicationName,
        System.Text.StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint ResumeThread(IntPtr hThread);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr CreateJobObjectW(IntPtr jobAttributes, string? name);

    [StructLayout(LayoutKind.Sequential)]
    internal struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetInformationJobObject(IntPtr hJob, int jobObjectInformationClass,
        ref JobObjectExtendedLimitInformation jobObjectInformation, uint cbJobObjectInformationLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool TerminateJobObject(IntPtr hJob, uint exitCode);

    // ToolHelp process enumeration for the supervisor sweep (no job handle is
    // returned by the launch API, so descendants are found via parent PIDs).
    public const uint TH32CS_SNAPPROCESS = 0x2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct ProcessEntry32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public UIntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    // Explicit Unicode: matches the CharSet.Auto (UTF-16 on Windows) struct
    // layout. The default ANSI import misreads names AND struct size.
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Process32First(IntPtr hSnapshot, ref ProcessEntry32 lppe);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool Process32Next(IntPtr hSnapshot, ref ProcessEntry32 lppe);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [StructLayout(LayoutKind.Sequential)]
    internal struct FileTime
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetProcessTimes(IntPtr hProcess,
        out FileTime lpCreationTime, out FileTime lpExitTime,
        out FileTime lpKernelTime, out FileTime lpUserTime);

    public const uint PROCESS_TERMINATE = 0x1;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    public const uint SYNCHRONIZE = 0x100000;

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
