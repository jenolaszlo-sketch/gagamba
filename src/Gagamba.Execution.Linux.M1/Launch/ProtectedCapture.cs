using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Control;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.Linux.M1.Launch;

internal sealed record CapturedStream(ulong Seen, byte[] Bytes, bool Eof, bool Overflow, bool Failed)
{
    internal bool Complete => Eof && !Overflow && !Failed;
    internal byte[]? Digest => Eof && Bytes.Length > 0 ? SHA256.HashData(Bytes) : null;
}
internal sealed record ProtectedCaptureSnapshot(CapturedStream Stdout, CapturedStream Stderr, bool Expired)
{
    internal bool Complete => Stdout.Complete && Stderr.Complete && !Expired;
}

// Private owner memory is the fixed custody. Nothing is mounted into the target, written
// to an ambient path, or queued to an observer. Only checked, copied slices leave custody.
internal sealed class ProtectedCapture : IDisposable
{
    internal const int StreamCeiling = 65536, AggregateCeiling = 131072, ReadCeiling = 16384;
    private sealed class State(int limit)
    {
        internal readonly byte[] Buffer = new byte[limit];
        internal int Stored;
        internal ulong Seen;
        internal ulong QuietEpoch;
        internal bool Eof, Overflow, Failed;
    }
    private readonly object _gate = new();
    private readonly State _stdout, _stderr;
    private readonly int _aggregate;
    private readonly Action<StopCauseKind> _stop;
    private readonly Action<bool>? _beforeStore;
    private readonly Thread _outThread, _errThread;
    private int _stored, _fault, _closed;
    private long _cutoff;
    private bool _expired;
    internal ProtectedCapture(SafeFileHandle stdout, SafeFileHandle stderr, ChannelManifest limits,
        long overallDeadline, Action<StopCauseKind> stop, Action<bool>? beforeStore = null)
    {
        if (limits.StdoutLimitBytes > StreamCeiling || limits.StderrLimitBytes > StreamCeiling
            || limits.AggregateLimitBytes > AggregateCeiling) throw new ArgumentException("Capture quota exceeds fixed custody");
        if (!NativeLaunchInterop.TrySetNonBlocking(stdout.DangerousGetHandle().ToInt32(), out _)
            || !NativeLaunchInterop.TrySetNonBlocking(stderr.DangerousGetHandle().ToInt32(), out _))
            throw new IOException("Capture nonblocking setup refused");
        _stdout = new((int)limits.StdoutLimitBytes); _stderr = new((int)limits.StderrLimitBytes);
        _aggregate = (int)limits.AggregateLimitBytes; _cutoff = overallDeadline;
        _stop = stop; _beforeStore = beforeStore;
        _outThread = Worker(stdout, _stdout, true); _errThread = Worker(stderr, _stderr, false);
        _outThread.Start(); _errThread.Start();
    }
    internal bool Healthy => Volatile.Read(ref _fault) == 0 && Volatile.Read(ref _closed) == 0 && !Volatile.Read(ref _expired);
    internal bool SettleSetup(long deadline)
    {
        ulong stdoutEpoch, stderrEpoch;
        lock (_gate) { stdoutEpoch = _stdout.QuietEpoch; stderrEpoch = _stderr.QuietEpoch; }
        while (Healthy && OwnerSafetyMonitor.Now() < deadline)
        {
            lock (_gate)
                if ((_stdout.Eof || _stdout.QuietEpoch > stdoutEpoch) && (_stderr.Eof || _stderr.QuietEpoch > stderrEpoch)) return Healthy;
            Thread.Sleep(1);
        }
        return false;
    }
    // First stop/final-drain bound wins. Repeated observations never grant more time.
    internal void BoundDrain(long cutoff)
    {
        lock (_gate) _cutoff = Math.Min(_cutoff, cutoff);
    }
    private Thread Worker(SafeFileHandle fd, State state, bool stdout) => new(() => Drain(fd, state, stdout))
        { IsBackground = true, Name = stdout ? "gagamba-stdout" : "gagamba-stderr" };
    private void Drain(SafeFileHandle handle, State state, bool stdout)
    {
        var buffer = new byte[4096];
        var fd = handle.DangerousGetHandle().ToInt32();
        try
        {
            while (Volatile.Read(ref _closed) == 0)
            {
                lock (_gate)
                    if (OwnerSafetyMonitor.Now() >= _cutoff) { _expired = true; handle.Dispose(); return; }
                var poll = new[] { new NativeLaunchInterop.PollFd { Fd = fd, Events = 1 } };
                var ready = NativeLaunchInterop.Poll(poll, 1, 10);
                if (ready < 0)
                {
                    if (Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
                    throw new IOException("Capture poll failed");
                }
                if (ready == 0) { lock (_gate) state.QuietEpoch++; continue; }
                var count = NativeLaunchInterop.Read(fd, buffer, (nuint)buffer.Length);
                if (count < 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error is NativeLaunchInterop.EINTR or NativeLaunchInterop.EAGAIN) continue;
                    throw new IOException("Capture read failed");
                }
                if (count == 0) { lock (_gate) state.Eof = true; return; }
                var overflow = false;
                lock (_gate)
                {
                    // Saturating counters remain observations, never storage reservations.
                    state.Seen = Math.Min(ulong.MaxValue / 2, state.Seen + (ulong)count);
                    var room = Math.Min(state.Buffer.Length - state.Stored, _aggregate - _stored);
                    var retain = Math.Min(room, (int)count);
                    overflow = retain < count;
                    state.Overflow |= overflow;
                    _beforeStore?.Invoke(stdout); // test-only storage-failure injection
                    Buffer.BlockCopy(buffer, 0, state.Buffer, state.Stored, retain);
                    state.Stored += retain; _stored += retain;
                }
                if (overflow) Fault(StopCauseKind.OutputOverflow);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            lock (_gate) state.Failed = true;
            Fault(StopCauseKind.OutputReadFailure);
        }
    }
    private void Fault(StopCauseKind cause)
    {
        BoundDrain(OwnerSafetyMonitor.Now() + 6_000_000_000L);
        if (Interlocked.Exchange(ref _fault, 1) == 0) _stop(cause);
    }
    internal ProtectedCaptureSnapshot Snapshot()
    {
        lock (_gate)
        {
            CapturedStream Copy(State s) => new(s.Seen, s.Buffer.AsSpan(0, s.Stored).ToArray(), s.Eof, s.Overflow, s.Failed);
            return new(Copy(_stdout), Copy(_stderr), _expired);
        }
    }
    internal byte[] Read(bool stdout, ulong offset, int count)
    {
        if (count is < 0 or > ReadCeiling || offset > StreamCeiling) throw new ArgumentOutOfRangeException(nameof(count));
        lock (_gate)
        {
            var state = stdout ? _stdout : _stderr;
            if (offset >= (ulong)state.Stored) return [];
            return state.Buffer.AsSpan((int)offset, Math.Min(count, state.Stored - (int)offset)).ToArray();
        }
    }
    internal ProtectedCaptureSnapshot Finish()
    {
        BoundDrain(OwnerSafetyMonitor.Now() + 1_000_000_000L);
        _outThread.Join(); _errThread.Join();
        return Snapshot();
    }
    public void Dispose()
    {
        Interlocked.Exchange(ref _closed, 1);
        _outThread.Join(); _errThread.Join();
    }
}
