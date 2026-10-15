using Xunit;

namespace Gagamba.Execution.MacOS.Tests;

public sealed class CaptureJoinTests
{
    [Fact]
    public async Task Mac_capture_abort_joins_both_drain_tasks_before_completion()
    {
        using var stdout = new DelayedReader();
        using var stderr = new DelayedReader();
        var lifecycle = new TaskCompletionSource<CompletionResult>();
        var session = new OutputCaptureSession(stdout, stderr, lifecycle.Task,
            new OutputCaptureOptions(64, 64, 128), joinReadersOnCompletion: true);
        try
        {
            session.Abort();
            await Task.WhenAll(stdout.Disposed.Task, stderr.Disposed.Task)
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(session.Completion.IsCompleted);
            stdout.Release.TrySetResult();
            await stdout.ReadFinished.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.False(session.Completion.IsCompleted);
            stderr.Release.TrySetResult();
            var result = await session.Completion.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(OutputCaptureStatus.DrainIncomplete, result.Status);
            Assert.True(stdout.ReadFinished.Task.IsCompleted);
            Assert.True(stderr.ReadFinished.Task.IsCompleted);
        }
        finally
        {
            stdout.Release.TrySetResult();
            stderr.Release.TrySetResult();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
        }
    }

    private sealed class DelayedReader : Stream
    {
        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReadFinished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            try { await Release.Task; throw new IOException("controlled delayed read failure"); }
            finally { ReadFinished.TrySetResult(); }
        }
        protected override void Dispose(bool disposing) { Disposed.TrySetResult(); base.Dispose(disposing); }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
