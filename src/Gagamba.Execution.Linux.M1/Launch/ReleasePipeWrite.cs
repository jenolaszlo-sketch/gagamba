// WP05 release transport: exactly one nonblocking 128-byte pipe write. The
// owner writer is the only writer; the coordinator reader owns the other end.
// A short write is an invariant anomaly under the qualified PIPE_BUF contract.
using Gagamba.Execution.Linux.M1.Native;

namespace Gagamba.Execution.Linux.M1.Launch;

internal enum ReleasePipeWriteResult : byte
{
    Delivered = 0,
    WouldBlock = 1,
    BrokenPipe = 2,
    Partial = 3,
    Unknown = 4,
    AlreadyAttempted = 5,
}

internal static class ReleasePipeWrite
{
    internal static bool TryEnsureSignalHandling(out string error) =>
        NativeLaunchInterop.TryIgnoreSigPipe(out error);

    internal static ReleasePipeWriteResult WriteOnce(int fd, ReadOnlyMemory<byte> frame)
    {
        if (frame.Length != CoordinatorProtocol.ReleaseFrameBytes
            || NativeLaunchInterop.PIPE_BUF < CoordinatorProtocol.ReleaseFrameBytes)
        {
            return ReleasePipeWriteResult.Unknown;
        }

        if (!NativeLaunchInterop.TryIgnoreSigPipe(out _))
        {
            return ReleasePipeWriteResult.Unknown;
        }

        var bytes = frame.ToArray();
        var written = NativeLaunchInterop.WriteBytesOnce(fd, bytes);
        if (written == bytes.Length)
        {
            return ReleasePipeWriteResult.Delivered;
        }

        if (written < 0)
        {
            var errno = NativeLaunchInterop.LastErrno;
            return errno switch
            {
                NativeLaunchInterop.EAGAIN => ReleasePipeWriteResult.WouldBlock,
                NativeLaunchInterop.EPIPE => ReleasePipeWriteResult.BrokenPipe,
                _ => ReleasePipeWriteResult.Unknown,
            };
        }

        if (written == 0)
        {
            return ReleasePipeWriteResult.Unknown;
        }

        return ReleasePipeWriteResult.Partial;
    }
}
