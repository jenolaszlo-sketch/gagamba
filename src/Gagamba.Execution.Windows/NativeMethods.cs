// Win32 interop owned by the Windows provider. Shapes proven in the
// GW-1B/GQ-1 spike; duplicated here so src never depends on spike code.
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Windows;

internal static class NativeMethods
{
    public const uint CREATE_SUSPENDED = 0x4;
    public const uint EXTENDED_STARTUPINFO_PRESENT = 0x80000;
    public const uint STARTF_USESTDHANDLES = 0x100;
    public const uint HANDLE_FLAG_INHERIT = 0x1;
    public const int PROC_THREAD_ATTRIBUTE_HANDLE_LIST = 0x20002;
    public const uint GENERIC_READ = 0x80000000;
    public const uint FILE_SHARE_READ = 0x1;
    public const uint OPEN_EXISTING = 3;
    // Mandatory whenever lpEnvironment is a caller-built Unicode block;
    // without it CreateProcess rejects the call with ERROR_INVALID_DATA-ish
    // ERROR_INVALID_PARAMETER (measured 0x57), even for byte-perfect blocks.
    public const uint CREATE_UNICODE_ENVIRONMENT = 0x400;
    public const int JobObjectExtendedLimitInformationClass = 9;
    public const int JobObjectBasicAccountingInformationClass = 1;
    public const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;
    public const uint ERROR_ACCESS_DENIED = 5;
    public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    public const uint SYNCHRONIZE = 0x00100000;
    public const uint WAIT_OBJECT_0 = 0;
    public const uint WAIT_TIMEOUT = 258;
    public const uint WAIT_FAILED = 0xFFFFFFFF;
    public const uint STILL_ACTIVE = 259;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct StartupInfo
    {
        public int cb;
        public string? Reserved;
        public string? Desktop;
        public string? Title;
        public uint X;
        public uint Y;
        public uint XSize;
        public uint YSize;
        public uint XCountChars;
        public uint YCountChars;
        public uint FillAttribute;
        public uint Flags;
        public short ShowWindow;
        public short Reserved2;
        public IntPtr Reserved3;
        public IntPtr StdInput;
        public IntPtr StdOutput;
        public IntPtr StdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessInformation
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint dwProcessId;
        public uint dwThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StartupInfoEx
    {
        public StartupInfo StartupInfo;
        public IntPtr AttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityAttributes
    {
        public int Length;
        public IntPtr SecurityDescriptor;
        [MarshalAs(UnmanagedType.Bool)] public bool InheritHandle;
    }

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

    [StructLayout(LayoutKind.Sequential)]
    internal struct JobObjectBasicAccountingInformation
    {
        public long TotalUserTime;
        public long TotalKernelTime;
        public long ThisPeriodTotalUserTime;
        public long ThisPeriodTotalKernelTime;
        public uint TotalPageFaultCount;
        public uint TotalProcesses;
        public uint ActiveProcesses;
        public uint TotalTerminatedProcesses;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateProcessW(
        string? applicationName,
        StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateProcessW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreateProcessWExtended(
        string? applicationName, StringBuilder commandLine, IntPtr processAttributes,
        IntPtr threadAttributes, [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags, IntPtr environment, string? currentDirectory,
        ref StartupInfoEx startupInfo, out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CreatePipe(out IntPtr readPipe, out IntPtr writePipe,
        ref SecurityAttributes pipeAttributes, uint size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetHandleInformation(IntPtr handle, uint mask, uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool InitializeProcThreadAttributeList(IntPtr list, int count,
        uint flags, ref IntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UpdateProcThreadAttribute(IntPtr list, uint flags,
        IntPtr attribute, IntPtr value, IntPtr size, IntPtr previous, IntPtr returnSize);

    [DllImport("kernel32.dll")]
    internal static extern void DeleteProcThreadAttributeList(IntPtr list);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr CreateFileW(string name, uint access, uint share,
        ref SecurityAttributes attributes, uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint ResumeThread(SafeWaitHandle hThread);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TerminateProcess(SafeWaitHandle hProcess, uint uExitCode);

    [DllImport("kernel32.dll")]
    internal static extern uint GetProcessId(SafeWaitHandle hProcess);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr CreateJobObjectW(IntPtr jobAttributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetInformationJobObject(SafeWaitHandle hJob, int jobObjectInformationClass,
        ref JobObjectExtendedLimitInformation jobObjectInformation, uint cbJobObjectInformationLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool QueryInformationJobObject(SafeWaitHandle hJob, int jobObjectInformationClass,
        ref JobObjectBasicAccountingInformation jobObjectInformation, uint cbJobObjectInformationLength, IntPtr lpReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AssignProcessToJobObject(SafeWaitHandle hJob, SafeWaitHandle hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AssignProcessToJobObject(SafeWaitHandle hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool TerminateJobObject(SafeWaitHandle hJob, uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetExitCodeProcess(SafeWaitHandle hProcess, out uint lpExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint WaitForSingleObject(SafeWaitHandle hHandle, uint dwMilliseconds);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);
}
