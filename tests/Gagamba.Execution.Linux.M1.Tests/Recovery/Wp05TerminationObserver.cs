// Test-only wrapper termination observer. It peeks at the owned wrapper's
// wait status with waitid(P_PIDFD, WNOWAIT) so the production reaper can still
// consume the status. It never closes the reaper's pidfd and never claims that
// a SIGKILL was caused by the test stop; the oracle combines this raw
// observation with the actual HeldAbortOutcome and identity binding.
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Restricted;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

internal sealed class Wp05TerminationObserver : IDisposable
{
    private const int PidFdIdType = 3;
    private const int WaitExited = 4;
    private const int WaitNoWait = 0x01000000;
    private const int Eintr = 4;
    private const int SigChld = 17;

    [StructLayout(LayoutKind.Explicit, Size = 128)]
    private struct ChildSignalInfo
    {
        [FieldOffset(0)] public int Signal;
        [FieldOffset(8)] public int Code;
        [FieldOffset(16)] public int Pid;
        [FieldOffset(20)] public uint Uid;
        [FieldOffset(24)] public int Status;
    }

    [DllImport("libc", EntryPoint = "waitid", SetLastError = true)]
    private static extern int WaitId(int idType, uint id, IntPtr info, int options);

    private readonly int _pidFd;
    private readonly OwnedWrapperIdentity _identity;
    private readonly Thread _thread;
    private readonly object _gate = new();
    private ObservedTermination? _result;
    private volatile bool _disposed;

    private Wp05TerminationObserver(int pidFd, OwnedWrapperIdentity identity)
    {
        _pidFd = pidFd;
        _identity = identity;
        _thread = new Thread(Run) { IsBackground = true, Name = "wp05-termination-observer" };
        _thread.Start();
    }

    internal OwnedWrapperIdentity ExpectedIdentity => _identity;

    internal static Wp05TerminationObserver StartForReaper(
        PidFdChildReaper reaper, PreparationIdentity identity)
    {
        var field = typeof(PidFdChildReaper).GetField(
            "_pidFd", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingFieldException(nameof(PidFdChildReaper), "_pidFd");
        var handle = field.GetValue(reaper) as SafeFileHandle
            ?? throw new InvalidOperationException("wrapper reaper has no live pidfd");
        var pidFd = handle.DangerousGetHandle().ToInt32();
        var pid = ReadPidFromPidFd(pidFd);
        var owned = new OwnedWrapperIdentity(
            identity.ExecutionId, identity.GenerationId, identity.PreparationId, pidFd, pid);
        return new Wp05TerminationObserver(pidFd, owned);
    }

    internal static Wp05TerminationObserver StartForPidFd(
        int pidFd, OwnedWrapperIdentity identity)
    {
        if (pidFd < 0) throw new ArgumentOutOfRangeException(nameof(pidFd));
        if (!identity.IsUsable)
            throw new ArgumentException("a usable expected wrapper identity is required", nameof(identity));
        return new Wp05TerminationObserver(pidFd, identity);
    }

    internal ObservedTermination WaitForResult(int timeoutMilliseconds)
    {
        if (_thread.Join(timeoutMilliseconds))
        {
            lock (_gate)
            {
                return _result ?? Unknown("observer completed without a result");
            }
        }

        return Unknown("termination observation timed out");
    }

    private static int ReadPidFromPidFd(int pidFd)
    {
        foreach (var line in File.ReadLines($"/proc/self/fdinfo/{pidFd}"))
        {
            if (!line.StartsWith("Pid:", StringComparison.Ordinal)) continue;
            if (int.TryParse(line.AsSpan(4).Trim(), out var pid) && pid > 0) return pid;
            break;
        }

        throw new InvalidOperationException(
            $"pidfd {pidFd} did not expose a usable kernel Pid in fdinfo");
    }

    private void Run()
    {
        try
        {
            var deadline = Environment.TickCount64 + 15_000;
            while (!_disposed && Environment.TickCount64 < deadline)
            {
                if (!PidFd.TryExited(_pidFd, 25, out var exited, out var pollError))
                {
                    Store(Unknown($"pidfd poll failed: {pollError}"));
                    return;
                }

                if (!exited) continue;

                var info = Marshal.AllocHGlobal(128);
                try
                {
                    Marshal.Copy(new byte[128], 0, info, 128);
                    int result;
                    do
                    {
                        result = WaitId(PidFdIdType, (uint)_pidFd, info, WaitExited | WaitNoWait);
                    }
                    while (result != 0 && Marshal.GetLastPInvokeError() == Eintr);

                    if (result != 0)
                    {
                        Store(Unknown($"waitid(P_PIDFD,WNOWAIT) errno {Marshal.GetLastPInvokeError()}"));
                        return;
                    }

                    var child = Marshal.PtrToStructure<ChildSignalInfo>(info);
                    if (child.Signal != SigChld || child.Pid <= 0)
                    {
                        Store(Unknown("waitid(P_PIDFD,WNOWAIT) returned no child record"));
                        return;
                    }

                    var kind = child.Code switch
                    {
                        1 => ObservedTerminationKind.Exited,
                        2 => ObservedTerminationKind.Killed,
                        3 => ObservedTerminationKind.Dumped,
                        _ => ObservedTerminationKind.Unknown,
                    };
                    Store(new ObservedTermination(
                        _identity with { Pid = child.Pid },
                        kind,
                        child.Code,
                        kind == ObservedTerminationKind.Exited ? child.Status : null,
                        kind is ObservedTerminationKind.Killed or ObservedTerminationKind.Dumped
                            ? child.Status
                            : null,
                        TerminationObservationSource.PidFdWaitIdWnowait,
                        string.Empty));
                    return;
                }
                finally
                {
                    Marshal.FreeHGlobal(info);
                }
            }

            Store(Unknown("timed out waiting for wrapper termination"));
        }
        catch (Exception ex)
        {
            Store(Unknown($"termination observation failed: {ex.Message}"));
        }
    }

    private void Store(ObservedTermination result)
    {
        lock (_gate)
        {
            _result ??= result;
        }
    }

    private ObservedTermination Unknown(string error) =>
        new(_identity, ObservedTerminationKind.Unknown, 0, null, null,
            TerminationObservationSource.None, error);

    public void Dispose()
    {
        _disposed = true;
        _thread.Join(1000);
    }
}

