using Gagamba.Execution;
using Xunit;

namespace Gagamba.Execution.Tests;

public sealed class Ar5OutputTests
{
    private static Task<CompletionResult> Done =>
        Task.FromResult<CompletionResult>(new CompletionResult.NaturalExit(0));

    [Fact]
    public async Task ConcurrentBinaryStreamsObeySeparateAndTotalBudgets()
    {
        byte[] stdout = Enumerable.Repeat((byte)0xFF, 9000).ToArray();
        byte[] stderr = Enumerable.Repeat((byte)0, 9000).ToArray();
        var session = new OutputCaptureSession(new MemoryStream(stdout), new MemoryStream(stderr),
            Done, new OutputCaptureOptions(100, 100, 150, RetainBytes: true));
        var result = await session.Completion.WaitAsync(TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);
        Assert.Equal(OutputCaptureStatus.Overflow, result.Status);
        Assert.Equal(9000, result.StdoutBytesSeen);
        Assert.Equal(9000, result.StderrBytesSeen);
        Assert.InRange(result.StdoutRetained.Length, 0, 100);
        Assert.InRange(result.StderrRetained.Length, 0, 100);
        Assert.True(result.StdoutRetained.Length + result.StderrRetained.Length <= 150);
        Assert.True(session.Overflowed.IsCompleted);
    }

    [Fact]
    public async Task DefaultOutputIsNotRetained()
    {
        var session = new OutputCaptureSession(new MemoryStream("secret"u8.ToArray()),
            new MemoryStream(), Done, new OutputCaptureOptions(10, 10, 20));
        var result = await session.Completion.WaitAsync(TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);
        Assert.Equal(OutputCaptureStatus.Complete, result.Status);
        Assert.Equal(6, result.StdoutBytesSeen);
        Assert.Empty(result.StdoutRetained.ToArray());
    }

    [Fact]
    public async Task HeldPipeEndsAfterFiniteGraceWithUnknownOutput()
    {
        var held = new HeldStream();
        var session = new OutputCaptureSession(held, new MemoryStream(), Done,
            new OutputCaptureOptions(10, 10, 20, PipeGrace: TimeSpan.FromMilliseconds(50)));
        var result = await session.Completion.WaitAsync(TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken);
        Assert.Equal(OutputCaptureStatus.DrainIncomplete, result.Status);
        Assert.True(held.WasDisposed);
    }

    private sealed class HeldStream : Stream
    {
        private readonly TaskCompletionSource _closed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool WasDisposed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        { await _closed.Task.WaitAsync(cancellationToken); return 0; }
        protected override void Dispose(bool disposing)
        { WasDisposed = true; _closed.TrySetResult(); base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
