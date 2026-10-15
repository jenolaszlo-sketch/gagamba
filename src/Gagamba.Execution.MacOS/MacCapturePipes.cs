using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Gagamba.Execution.MacOS;

// launchd opens private FIFO paths; it remains the only target launcher. Parent
// descriptors never cross exec. Open readers/keepers and start BOTH drains
// before kickstart. Open descriptors survive OwnedJob's confirmed unlink.
internal sealed class MacCapturePipes
{
    private readonly FifoReader _stdout;
    private readonly FifoReader _stderr;
    private readonly TaskCompletionSource<CompletionResult> _lifecycle =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _abort =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task<CompletionResult>? _providerCompletion;
    private Task? _relay;

    private MacCapturePipes(FifoReader stdout, FifoReader stderr, OutputCaptureOptions options)
    {
        _stdout = stdout;
        _stderr = stderr;
        Session = new OutputCaptureSession(stdout, stderr, _lifecycle.Task, options);
        CleanupCompletion = CloseAfterCaptureAsync();
    }

    public string StdoutPath => _stdout.Path;
    public string StderrPath => _stderr.Path;
    public OutputCaptureSession Session { get; }
    public Task CleanupCompletion { get; }
    public bool LifecycleCompleted => _providerCompletion?.IsCompleted == true;

    public static MacCapturePipes Create(string directory, OutputCaptureOptions options)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("native macOS FIFO capture required");
        FifoReader? stdout = null, stderr = null;
        try
        {
            stdout = FifoReader.Create(System.IO.Path.Combine(directory, "stdout.capture.fifo"));
            stderr = FifoReader.Create(System.IO.Path.Combine(directory, "stderr.capture.fifo"));
            return new MacCapturePipes(stdout, stderr, options);
        }
        catch
        {
            stdout?.RemoveUnlaunched();
            stderr?.RemoveUnlaunched();
            throw;
        }
    }

    // Called exactly once by the admitting launch, including its failed-launch
    // cleanup path. No pipe-grace clock starts while launchd is still running.
    public void BindCompletion(Task<CompletionResult> completion)
    {
        if (_providerCompletion is not null) throw new InvalidOperationException("capture already bound");
        _providerCompletion = completion;
        _relay = RelayCompletionAsync(completion);
    }

    private async Task RelayCompletionAsync(Task<CompletionResult> completion)
    {
        CompletionResult result;
        try
        {
            Task winner = await Task.WhenAny(completion, _abort.Task).ConfigureAwait(false);
            result = winner == completion ? await completion.ConfigureAwait(false)
                : new CompletionResult.Failed(new[] { "capture aborted during unresolved lifecycle cleanup" });
        }
        catch (Exception ex)
        { result = new CompletionResult.Failed(new[] { $"capture lifecycle fault: {ex.GetType().Name}" }); }
        _stdout.CloseKeeper();
        _stderr.CloseKeeper();
        _lifecycle.TrySetResult(result);
    }

    public void Abort()
    {
        // This only ends capture ownership. It is never proof that the launchd
        // job stopped; provider disposal retains its existing cleanup failures.
        _abort.TrySetResult();
        Session.Abort();
        _stdout.Dispose();
        _stderr.Dispose();
    }

    private async Task CloseAfterCaptureAsync()
    {
        try { await Session.Completion.ConfigureAwait(false); }
        finally
        {
            _stdout.Dispose();
            _stderr.Dispose();
            await Task.WhenAll(_stdout.Quiesced, _stderr.Quiesced).ConfigureAwait(false);
            _stdout.ReleaseCancellationSource();
            _stderr.ReleaseCancellationSource();
            if (_relay is not null) await _relay.ConfigureAwait(false);
        }
    }

    private sealed class FifoReader : Stream
    {
        private readonly object _gate = new();
        private readonly SafeFileHandle _reader;
        private readonly SafeFileHandle _keeper;
        private readonly byte[] _buffer = new byte[8192];
        private readonly CancellationTokenSource _closed = new();
        private readonly TaskCompletionSource _quiesced =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _disposed, _closeIssued;
        private int _activeRead;

        private FifoReader(string path, SafeFileHandle reader, SafeFileHandle keeper)
        { Path = path; _reader = reader; _keeper = keeper; }

        public string Path { get; }
        public Task Quiesced => _quiesced.Task;

        public static FifoReader Create(string path)
        {
            bool created = false;
            SafeFileHandle? reader = null, keeper = null;
            try
            {
                if (Native.mkfifo(path, 0x180 /* 0600 */) != 0)
                    throw new IOException($"mkfifo failed: errno={Marshal.GetLastPInvokeError()}");
                created = true;
                reader = Open(path, Native.O_RDONLY);
                keeper = Open(path, Native.O_WRONLY);
                return new FifoReader(path, reader, keeper);
            }
            catch
            {
                reader?.Dispose(); keeper?.Dispose();
                if (created) File.Delete(path);
                throw;
            }
        }

        private static SafeFileHandle Open(string path, int access)
        {
            int fd = Native.open(path, access | Native.O_NONBLOCK | Native.O_CLOEXEC);
            if (fd < 0) throw new IOException($"FIFO open failed: errno={Marshal.GetLastPInvokeError()}");
            var handle = new SafeFileHandle((IntPtr)fd, ownsHandle: true);
            if (fd < 3)
            {
                handle.Dispose();
                throw new IOException("capture requires parent stdio descriptors 0-2 to be open");
            }
            return handle;
        }

        public void CloseKeeper() { lock (_gate) _keeper.Dispose(); }

        public void RemoveUnlaunched()
        {
            Dispose();
            _closed.Dispose();
            File.Delete(Path);
        }

        public void ReleaseCancellationSource() => _closed.Dispose();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            CancellationToken closed;
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_activeRead != 0) throw new InvalidOperationException("concurrent FIFO reads unsupported");
                _activeRead = 1;
                closed = _closed.Token;
            }
            using var linked = cancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(closed, cancellationToken) : null;
            CancellationToken stop = linked?.Token ?? closed;
            try
            {
                if (buffer.Length == 0) return 0;
                while (true)
                {
                    stop.ThrowIfCancellationRequested();
                    long count;
                    int error;
                    lock (_gate)
                    {
                        ObjectDisposedException.ThrowIf(_disposed, this);
                        // read is nonblocking. The lock prevents FD reuse by
                        // Dispose while this syscall uses the owning handle.
                        count = Native.read(_reader.DangerousGetHandle(), _buffer,
                            (nuint)Math.Min(buffer.Length, _buffer.Length)).ToInt64();
                        error = count < 0 ? Marshal.GetLastPInvokeError() : 0;
                        if (count > 0) _buffer.AsMemory(0, (int)count).CopyTo(buffer);
                    }
                    if (count >= 0) return (int)count;
                    if (error == Native.EINTR) continue;
                    if (error != Native.EAGAIN) throw new IOException($"FIFO read failed: errno={error}");
                    await Task.Delay(5, stop).ConfigureAwait(false);
                }
            }
            finally
            {
                lock (_gate)
                {
                    _activeRead = 0;
                    if (_disposed && _closeIssued) _quiesced.TrySetResult();
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                _reader.Dispose(); _keeper.Dispose();
            }
            _closed.Cancel();
            lock (_gate)
            {
                _closeIssued = true;
                if (_activeRead == 0) _quiesced.TrySetResult();
            }
            base.Dispose(disposing);
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static class Native
    {
        // Darwin ABI, not Linux flags. Apple xnu bsd/sys/fcntl.h and errno.h.
        internal const int O_RDONLY = 0, O_WRONLY = 1, O_NONBLOCK = 0x4, O_CLOEXEC = 0x01000000;
        internal const int EINTR = 4, EAGAIN = 35;
        [DllImport("libc", SetLastError = true)]
        internal static extern int mkfifo([MarshalAs(UnmanagedType.LPUTF8Str)] string path, uint mode);
        [DllImport("libc", SetLastError = true)]
        internal static extern int open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);
        [DllImport("libc", SetLastError = true)]
        internal static extern IntPtr read(IntPtr fd, [Out] byte[] buffer, nuint count);
    }
}
