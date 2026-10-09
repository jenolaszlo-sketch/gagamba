using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Windows;

/// <summary>Only the three listed standard handles can cross CreateProcess.
/// Parent read handles are non-inheritable and child writes are closed as soon
/// as the suspended root has been created.</summary>
internal sealed class WindowsCapturePipes : IDisposable
{
    private SafeFileHandle? _stdoutRead;
    private SafeFileHandle? _stderrRead;
    private SafeFileHandle? _stdoutWrite;
    private SafeFileHandle? _stderrWrite;
    private SafeFileHandle? _stdin;
    private IntPtr _attributes;
    private IntPtr _handleList;
    private bool _initialized;

    private WindowsCapturePipes() { }

    internal static WindowsCapturePipes Create()
    {
        var pipes = new WindowsCapturePipes();
        try { pipes.Initialize(); return pipes; }
        catch { pipes.Dispose(); throw; }
    }

    private static (SafeFileHandle Read, SafeFileHandle Write) Pipe()
    {
        var security = new NativeMethods.SecurityAttributes
        { Length = Marshal.SizeOf<NativeMethods.SecurityAttributes>(), InheritHandle = true };
        if (!NativeMethods.CreatePipe(out IntPtr read, out IntPtr write, ref security, 0))
            throw new InvalidOperationException($"CreatePipe error 0x{Marshal.GetLastWin32Error():X}");
        var reader = new SafeFileHandle(read, true);
        var writer = new SafeFileHandle(write, true);
        if (!NativeMethods.SetHandleInformation(read, NativeMethods.HANDLE_FLAG_INHERIT, 0))
        {
            int error = Marshal.GetLastWin32Error();
            reader.Dispose(); writer.Dispose();
            throw new InvalidOperationException($"SetHandleInformation error 0x{error:X}");
        }
        return (reader, writer);
    }

    private void Initialize()
    {
        (_stdoutRead, _stdoutWrite) = Pipe();
        (_stderrRead, _stderrWrite) = Pipe();
        var security = new NativeMethods.SecurityAttributes
        { Length = Marshal.SizeOf<NativeMethods.SecurityAttributes>(), InheritHandle = true };
        IntPtr nul = NativeMethods.CreateFileW("NUL", NativeMethods.GENERIC_READ,
            NativeMethods.FILE_SHARE_READ, ref security, NativeMethods.OPEN_EXISTING, 0, IntPtr.Zero);
        if (nul == new IntPtr(-1))
            throw new InvalidOperationException($"open NUL error 0x{Marshal.GetLastWin32Error():X}");
        _stdin = new SafeFileHandle(nul, true);
        IntPtr size = IntPtr.Zero;
        NativeMethods.InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
        if (size == IntPtr.Zero) throw new InvalidOperationException("attribute list size unavailable");
        _attributes = Marshal.AllocHGlobal(size);
        if (!NativeMethods.InitializeProcThreadAttributeList(_attributes, 1, 0, ref size))
            throw new InvalidOperationException($"attribute list initialization error 0x{Marshal.GetLastWin32Error():X}");
        _initialized = true;
        _handleList = Marshal.AllocHGlobal(3 * IntPtr.Size);
        Marshal.WriteIntPtr(_handleList, 0, _stdin.DangerousGetHandle());
        Marshal.WriteIntPtr(_handleList, IntPtr.Size, _stdoutWrite.DangerousGetHandle());
        Marshal.WriteIntPtr(_handleList, 2 * IntPtr.Size, _stderrWrite.DangerousGetHandle());
        if (!NativeMethods.UpdateProcThreadAttribute(_attributes, 0,
            NativeMethods.PROC_THREAD_ATTRIBUTE_HANDLE_LIST, _handleList,
            new IntPtr(3 * IntPtr.Size), IntPtr.Zero, IntPtr.Zero))
            throw new InvalidOperationException($"handle list update error 0x{Marshal.GetLastWin32Error():X}");
    }

    internal NativeMethods.StartupInfoEx StartupInfo => new()
    {
        StartupInfo = new NativeMethods.StartupInfo
        {
            cb = Marshal.SizeOf<NativeMethods.StartupInfoEx>(),
            Flags = NativeMethods.STARTF_USESTDHANDLES,
            StdInput = _stdin!.DangerousGetHandle(),
            StdOutput = _stdoutWrite!.DangerousGetHandle(),
            StdError = _stderrWrite!.DangerousGetHandle()
        },
        AttributeList = _attributes
    };

    internal void CloseChildEnds()
    {
        _stdin?.Dispose(); _stdin = null;
        _stdoutWrite?.Dispose(); _stdoutWrite = null;
        _stderrWrite?.Dispose(); _stderrWrite = null;
    }

    internal (Stream Stdout, Stream Stderr) TakeReaders()
    {
        var stdout = _stdoutRead ?? throw new InvalidOperationException("stdout reader unavailable");
        var stderr = _stderrRead ?? throw new InvalidOperationException("stderr reader unavailable");
        _stdoutRead = null; _stderrRead = null;
        try
        {
            var output = new FileStream(stdout, FileAccess.Read, 4096, false);
            try { return (output, new FileStream(stderr, FileAccess.Read, 4096, false)); }
            catch { output.Dispose(); throw; }
        }
        catch { stdout.Dispose(); stderr.Dispose(); throw; }
    }

    public void Dispose()
    {
        CloseChildEnds();
        _stdoutRead?.Dispose(); _stderrRead?.Dispose();
        if (_initialized) NativeMethods.DeleteProcThreadAttributeList(_attributes);
        if (_attributes != IntPtr.Zero) Marshal.FreeHGlobal(_attributes);
        if (_handleList != IntPtr.Zero) Marshal.FreeHGlobal(_handleList);
        _attributes = IntPtr.Zero; _handleList = IntPtr.Zero;
        _initialized = false;
    }
}
