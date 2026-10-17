// WP05 trusted coordinator status reader. The coordinator is the sole target/
// reaper observation source; the owner validates the descriptor-free status
// sequence and persists only after durable ReleaseCommitted. No stdout,
// stderr or Bubblewrap JSON is accepted as target status.
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Recovery;

namespace Gagamba.Execution.Linux.M1.Launch;

internal interface IStatusFrameSource
{
    bool TryReadFrame(long deadlineNanoseconds, out byte[]? frame, out string error);
}

internal sealed record CoordinatorStatusObservation(
    ExecObservation? Exec,
    RootObservation? Root,
    bool ProtocolFailed,
    uint FailureCode);

internal sealed class CoordinatorStatusReader
{
    private enum State
    {
        AwaitingExec,
        AwaitingRoot,
        Terminal,
    }

    private readonly IStatusFrameSource _source;
    private readonly Guid _executionId;
    private readonly Guid _preparationId;
    private readonly byte[] _digest;
    private ulong _expectedSequence;
    private State _state = State.AwaitingExec;
    private ExecObservation? _exec;

    internal CoordinatorStatusReader(
        IStatusFrameSource source,
        Guid executionId,
        Guid preparationId,
        ReadOnlySpan<byte> manifestDigest,
        ulong initialSequence = 2)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _executionId = executionId;
        _preparationId = preparationId;
        _digest = manifestDigest.Length == 32
            ? manifestDigest.ToArray()
            : throw new ArgumentException("manifest digest must be 32 bytes", nameof(manifestDigest));
        _expectedSequence = initialSequence;
    }

    internal bool TryReadNext(
        long deadlineNanoseconds,
        out CoordinatorStatusObservation? observation,
        out string error)
    {
        observation = null;
        error = string.Empty;
        if (_state == State.Terminal)
        {
            error = "coordinator status sequence is already terminal";
            return false;
        }

        if (!_source.TryReadFrame(deadlineNanoseconds, out var frame, out error) || frame is null)
        {
            return false;
        }

        if (!CoordinatorProtocol.TryDecodeStatus(
                frame, _executionId, _preparationId, _digest, _expectedSequence,
                out var decoded, out error) || decoded is null)
        {
            return false;
        }

        var nextSequence = _expectedSequence + 1;
        if (nextSequence == 0)
        {
            error = "coordinator status sequence overflow";
            return false;
        }

        switch (decoded.Kind)
        {
            case CoordinatorStatusKind.ProtocolFailed:
            {
                var code = ReadU32(decoded.Payload);
                _state = State.Terminal;
                _expectedSequence = nextSequence;
                observation = new CoordinatorStatusObservation(null, null, true, code);
                return true;
            }
            case CoordinatorStatusKind.ExecFailed:
            {
                if (_state != State.AwaitingExec)
                {
                    error = "EXEC_FAILED is out of order";
                    return false;
                }

                var errno = ReadU32(decoded.Payload);
                if (errno is 0 or > int.MaxValue)
                {
                    error = "EXEC_FAILED errno is invalid";
                    return false;
                }

                _exec = new ExecObservation(ExecTag.FailedBeforeExec, checked((int)errno));
                _state = State.AwaitingRoot;
                _expectedSequence = nextSequence;
                observation = new CoordinatorStatusObservation(_exec, null, false, 0);
                return true;
            }
            case CoordinatorStatusKind.ExecConfirmed:
            {
                if (_state != State.AwaitingExec)
                {
                    error = "EXEC_CONFIRMED is out of order";
                    return false;
                }

                _exec = new ExecObservation(ExecTag.Confirmed, Basis: ExecBasis.CoordinatorFrame);
                _state = State.AwaitingRoot;
                _expectedSequence = nextSequence;
                observation = new CoordinatorStatusObservation(_exec, null, false, 0);
                return true;
            }
            case CoordinatorStatusKind.RootWait:
            {
                var raw = checked((int)ReadU32(decoded.Payload));
                if (_state == State.AwaitingExec)
                {
                    _state = State.Terminal;
                    _expectedSequence = nextSequence;
                    observation = new CoordinatorStatusObservation(
                        new ExecObservation(ExecTag.Unknown),
                        new RootObservation(RootKind.Unknown, RawWaitPresent: true, RawWaitStatus: raw),
                        false, 0);
                    return true;
                }

                if (_state != State.AwaitingRoot)
                {
                    error = "ROOT_WAIT is out of order";
                    return false;
                }

                RootObservation root;
                if (_exec?.Tag == ExecTag.FailedBeforeExec)
                {
                    root = new RootObservation(RootKind.NotStarted, RawWaitPresent: true, RawWaitStatus: raw);
                }
                else if (_exec?.Tag == ExecTag.Confirmed)
                {
                    if (!TryDecodeRootWait(raw, out root, out error))
                    {
                        return false;
                    }
                }
                else
                {
                    error = "ROOT_WAIT requires a prior exec observation";
                    return false;
                }

                _state = State.Terminal;
                _expectedSequence = nextSequence;
                observation = new CoordinatorStatusObservation(_exec, root, false, 0);
                return true;
            }
            default:
                error = "unsupported coordinator status kind";
                return false;
        }
    }

    internal static bool TryDecodeRootWait(int rawWaitStatus, out RootObservation root, out string error)
    {
        root = null!;
        error = string.Empty;
        var low = rawWaitStatus & 0x7f;
        if (low == 0)
        {
            root = new RootObservation(
                RootKind.Exited,
                ExitCode: (rawWaitStatus >> 8) & 0xff,
                RawWaitPresent: true,
                RawWaitStatus: rawWaitStatus);
            return true;
        }

        if (low is not (0x7f))
        {
            root = new RootObservation(
                RootKind.Signalled,
                Signal: low,
                CoreDumped: (rawWaitStatus & 0x80) != 0,
                RawWaitPresent: true,
                RawWaitStatus: rawWaitStatus);
            return true;
        }

        error = "raw wait status is not a terminal target result";
        return false;
    }

    private static uint ReadU32(byte[] payload)
    {
        if (payload.Length != 4)
        {
            throw new InvalidOperationException("status payload is not four bytes");
        }

        return System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(payload);
    }
}

internal sealed class FdStatusFrameSource : IStatusFrameSource
{
    private readonly int _fd;

    internal FdStatusFrameSource(int fd)
    {
        _fd = fd;
    }

    public bool TryReadFrame(long deadlineNanoseconds, out byte[]? frame, out string error)
    {
        frame = null;
        error = string.Empty;
        var header = new byte[CoordinatorProtocol.HeaderBytes];
        if (!TryReadExact(header, deadlineNanoseconds, out error))
        {
            return false;
        }

        var total = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(12));
        if (total < CoordinatorProtocol.HeaderBytes || total > CoordinatorProtocol.MaxFrameBytes)
        {
            error = "status frame length is outside the fixed bound";
            return false;
        }

        var payload = new byte[checked((int)total) - CoordinatorProtocol.HeaderBytes];
        if (payload.Length > 0 && !TryReadExact(payload, deadlineNanoseconds, out error))
        {
            return false;
        }

        frame = new byte[checked((int)total)];
        header.CopyTo(frame, 0);
        payload.CopyTo(frame, CoordinatorProtocol.HeaderBytes);
        return true;
    }

    private bool TryReadExact(byte[] target, long deadlineNanoseconds, out string error)
    {
        error = string.Empty;
        var offset = 0;
        while (offset < target.Length)
        {
            var remaining = deadlineNanoseconds - MonotonicNowNanoseconds();
            if (remaining <= 0)
            {
                error = "status deadline expired";
                return false;
            }

            var poll = new[] { new NativeLaunchInterop.PollFd { Fd = _fd, Events = 1 } };
            var rc = NativeLaunchInterop.Poll(poll, 1, (int)Math.Clamp(remaining / 1_000_000, 1, 250));
            if (rc < 0)
            {
                if (Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
                error = $"status poll errno {Marshal.GetLastWin32Error()}";
                return false;
            }

            if (rc == 0) continue;
            var scratch = new byte[target.Length - offset];
            var read = NativeLaunchInterop.Read(_fd, scratch, (nuint)scratch.Length);
            if (read < 0 && Marshal.GetLastWin32Error() == NativeLaunchInterop.EINTR) continue;
            if (read <= 0)
            {
                error = "status channel closed before a complete record";
                return false;
            }

            Buffer.BlockCopy(scratch, 0, target, offset, (int)read);
            offset += (int)read;
        }

        return true;
    }

    private static long MonotonicNowNanoseconds()
    {
        var timestamp = Stopwatch.GetTimestamp();
        return (timestamp / Stopwatch.Frequency * 1_000_000_000L)
            + (timestamp % Stopwatch.Frequency * 1_000_000_000L / Stopwatch.Frequency);
    }
}
