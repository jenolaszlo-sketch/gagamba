// WP05 trusted coordinator status codec, sequence and observation-ordering
// tests. The coordinator is the sole target/reaper source; no stdout or target
// is involved.
using System.Buffers.Binary;
using Gagamba.Execution.Linux.M1.Execution;
using Gagamba.Execution.Linux.M1.Launch;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Linux.M1.Tests.Recovery;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Launch;

public sealed class CoordinatorStatusReaderTests
{
    private static readonly Guid Execution = new("aaaaaaaa-0000-0000-0000-00000000001a");
    private static readonly Guid Issuer = new("bbbbbbbb-0000-0000-0000-00000000001b");
    private static readonly Guid Generation = new("cccccccc-0000-0000-0000-00000000001c");
    private static readonly Guid Preparation = new("dddddddd-0000-0000-0000-00000000001d");
    private static readonly byte[] Digest = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray();

    private sealed class QueueStatusFrameSource : IStatusFrameSource
    {
        private readonly Queue<byte[]> _frames = new();

        public void Add(byte[] frame) => _frames.Enqueue(frame);

        public bool TryReadFrame(long deadlineNanoseconds, out byte[]? frame, out string error)
        {
            if (_frames.Count == 0)
            {
                frame = null;
                error = "queue empty";
                return false;
            }

            frame = _frames.Dequeue();
            error = string.Empty;
            return true;
        }
    }

    private static byte[] StatusFrame(
        CoordinatorStatusKind kind, ulong sequence, uint value = 0)
    {
        var payload = kind == CoordinatorStatusKind.ExecConfirmed ? 0 : 4;
        var frame = new byte[CoordinatorProtocol.HeaderBytes + payload];
        System.Text.Encoding.ASCII.GetBytes("GGM1CTL\0").CopyTo(frame, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(10), (ushort)kind);
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(12), (uint)frame.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(frame.AsSpan(16), sequence);
        CoordinatorProtocol.GuidToRfcBytes(Execution).CopyTo(frame, 24);
        CoordinatorProtocol.GuidToRfcBytes(Preparation).CopyTo(frame, 40);
        Digest.CopyTo(frame, 56);
        if (payload != 0)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(96), value);
        }

        return frame;
    }

    private static CoordinatorStatusReader Reader(QueueStatusFrameSource source) =>
        new(source, Execution, Preparation, Digest);

    [Fact]
    public void ConfirmedThenExitedRootIsReadInOrder()
    {
        var source = new QueueStatusFrameSource();
        source.Add(StatusFrame(CoordinatorStatusKind.ExecConfirmed, 2));
        source.Add(StatusFrame(CoordinatorStatusKind.RootWait, 3, 0));
        var reader = Reader(source);

        Assert.True(reader.TryReadNext(1_000_000, out var exec, out var error), error);
        Assert.NotNull(exec);
        Assert.Equal(ExecTag.Confirmed, exec!.Exec!.Tag);
        Assert.Null(exec.Root);

        Assert.True(reader.TryReadNext(1_000_000, out var root, out error), error);
        Assert.NotNull(root);
        Assert.Equal(RootKind.Exited, root!.Root!.Kind);
        Assert.Equal(0, root.Root.ExitCode);
        Assert.False(reader.TryReadNext(1_000_000, out _, out _));
    }

    [Fact]
    public void FailedExecThenRootIsNotStarted()
    {
        var source = new QueueStatusFrameSource();
        source.Add(StatusFrame(CoordinatorStatusKind.ExecFailed, 2, 2));
        source.Add(StatusFrame(CoordinatorStatusKind.RootWait, 3, 7));
        var reader = Reader(source);

        Assert.True(reader.TryReadNext(1_000_000, out var exec, out var error), error);
        Assert.Equal(ExecTag.FailedBeforeExec, exec!.Exec!.Tag);
        Assert.True(reader.TryReadNext(1_000_000, out var root, out error), error);
        Assert.Equal(RootKind.NotStarted, root!.Root!.Kind);
        Assert.True(root.Root.RawWaitPresent);
    }

    [Fact]
    public void UnconfirmedRootWaitIsDiagnosticUnknown()
    {
        var source = new QueueStatusFrameSource();
        source.Add(StatusFrame(CoordinatorStatusKind.RootWait, 2, 15));
        var reader = Reader(source);

        Assert.True(reader.TryReadNext(1_000_000, out var observation, out var error), error);
        Assert.Equal(ExecTag.Unknown, observation!.Exec!.Tag);
        Assert.Equal(RootKind.Unknown, observation.Root!.Kind);
        Assert.True(observation.Root.RawWaitPresent);
    }

    [Fact]
    public void SignalledRootDecodesCoreAndSignal()
    {
        var source = new QueueStatusFrameSource();
        source.Add(StatusFrame(CoordinatorStatusKind.ExecConfirmed, 2));
        source.Add(StatusFrame(CoordinatorStatusKind.RootWait, 3, 15 | 0x80));
        var reader = Reader(source);

        Assert.True(reader.TryReadNext(1_000_000, out _, out var error), error);
        Assert.True(reader.TryReadNext(1_000_000, out var root, out error), error);
        Assert.Equal(RootKind.Signalled, root!.Root!.Kind);
        Assert.Equal(15, root.Root.Signal);
        Assert.True(root.Root.CoreDumped);
    }

    [Fact]
    public void ProtocolFailedTerminatesSequence()
    {
        var source = new QueueStatusFrameSource();
        source.Add(StatusFrame(CoordinatorStatusKind.ProtocolFailed, 2, 42));
        var reader = Reader(source);
        Assert.True(reader.TryReadNext(1_000_000, out var observation, out var error), error);
        Assert.True(observation!.ProtocolFailed);
        Assert.Equal(42u, observation.FailureCode);
        Assert.False(reader.TryReadNext(1_000_000, out _, out _));
    }

    [Fact]
    public void OutOfOrderAndDuplicateStatusRefused()
    {
        var source = new QueueStatusFrameSource();
        source.Add(StatusFrame(CoordinatorStatusKind.ExecConfirmed, 3));
        var reader = Reader(source);
        Assert.False(reader.TryReadNext(1_000_000, out _, out _));

        var duplicates = new QueueStatusFrameSource();
        duplicates.Add(StatusFrame(CoordinatorStatusKind.ExecConfirmed, 2));
        duplicates.Add(StatusFrame(CoordinatorStatusKind.ExecConfirmed, 3));
        var duplicateReader = Reader(duplicates);
        Assert.True(duplicateReader.TryReadNext(1_000_000, out _, out var error), error);
        Assert.False(duplicateReader.TryReadNext(1_000_000, out _, out _));
    }

    [Fact]
    public void MalformedStatusFramesFailClosed()
    {
        foreach (var mutate in new Action<byte[]>[]
        {
            frame => frame[0] ^= 1,
            frame => frame[8] ^= 1,
            frame => frame[10] ^= 1,
            frame => frame[16] ^= 1,
            frame => frame[24] ^= 1,
            frame => frame[40] ^= 1,
            frame => frame[56] ^= 1,
            frame => frame[88] ^= 1,
        })
        {
            var frame = StatusFrame(CoordinatorStatusKind.ExecConfirmed, 2);
            mutate(frame);
            var source = new QueueStatusFrameSource();
            source.Add(frame);
            Assert.False(Reader(source).TryReadNext(1_000_000, out _, out _));
        }

        var truncated = StatusFrame(CoordinatorStatusKind.ExecFailed, 2, 1)[..99];
        var truncatedSource = new QueueStatusFrameSource();
        truncatedSource.Add(truncated);
        Assert.False(Reader(truncatedSource).TryReadNext(1_000_000, out _, out _));

        var extra = StatusFrame(CoordinatorStatusKind.ExecConfirmed, 2).Concat(new byte[] { 0 }).ToArray();
        var extraSource = new QueueStatusFrameSource();
        extraSource.Add(extra);
        Assert.False(Reader(extraSource).TryReadNext(1_000_000, out _, out _));
    }

    [Fact]
    public void BufferDoesNotPersistBeforeCommittedAndPreservesOrderAfter()
    {
        var (journal, lease, latest) = Seed(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseCommitted()));
        using (lease)
        {
            var buffer = new ReleaseObservationBuffer();
            Assert.True(buffer.OfferExec(RecoveryTestSupport.ExecConfirmed()));
            Assert.True(buffer.OfferRoot(RecoveryTestSupport.RootExited()));
            Assert.True(buffer.TryFlush(journal, lease, out var error), error);
            Assert.Null(journal.ReadLatest().Snapshot!.Exec);

            buffer.MarkDurableCommitted();
            Assert.True(buffer.TryFlush(journal, lease, out error), error);
            var read = journal.ReadLatest().Snapshot!;
            Assert.NotNull(read.Exec);
            Assert.NotNull(read.Root);
            Assert.Equal(ReleaseTag.Committed, read.Release!.Tag);
        }
    }

    [Fact]
    public void BufferRefusesObservationAgainstIntent()
    {
        var (journal, lease, latest) = Seed(RecoveryTestSupport.V3Snapshot(
            release: RecoveryTestSupport.ReleaseIntent()));
        using (lease)
        {
            var buffer = new ReleaseObservationBuffer();
            Assert.True(buffer.OfferExec(RecoveryTestSupport.ExecConfirmed()));
            buffer.MarkDurableCommitted();
            Assert.False(buffer.TryFlush(journal, lease, out _));
            Assert.Null(journal.ReadLatest().Snapshot!.Exec);
            Assert.Equal(ReleaseTag.Intent, journal.ReadLatest().Snapshot!.Release!.Tag);
        }
    }

    [Fact]
    public void BufferRejectsRootBeforeExecAndContradictions()
    {
        var buffer = new ReleaseObservationBuffer();
        Assert.False(buffer.OfferRoot(RecoveryTestSupport.RootExited()));
        Assert.True(buffer.OfferExec(RecoveryTestSupport.ExecConfirmed()));
        Assert.True(buffer.OfferExec(RecoveryTestSupport.ExecConfirmed()));
        Assert.False(buffer.OfferExec(RecoveryTestSupport.ExecUnknown()));
        Assert.True(buffer.OfferRoot(RecoveryTestSupport.RootExited()));
        Assert.True(buffer.OfferRoot(RecoveryTestSupport.RootExited()));
        Assert.False(buffer.OfferRoot(RecoveryTestSupport.RootUnknown()));
    }

    [Fact]
    public void RawWaitDecodeRejectsNonTerminalStatus()
    {
        Assert.False(CoordinatorStatusReader.TryDecodeRootWait(0x7f, out _, out _));
        Assert.True(CoordinatorStatusReader.TryDecodeRootWait(7 << 8, out var exited, out _));
        Assert.Equal(RootKind.Exited, exited.Kind);
        Assert.Equal(7, exited.ExitCode);
        Assert.True(CoordinatorStatusReader.TryDecodeRootWait(9, out var signalled, out _));
        Assert.Equal(RootKind.Signalled, signalled.Kind);
        Assert.Equal(9, signalled.Signal);
    }

    private static (OwnershipJournal Journal, JournalWriterLease Lease, OwnershipSnapshot Latest) Seed(
        OwnershipSnapshot snapshot)
    {
        var journal = new OwnershipJournal(new FakeJournalStorage(), "/status");
        Assert.True(journal.TryAcquireWriter(out var lease, out var error), error);
        Assert.True(journal.WriteNext(lease!, snapshot).Ok);
        var latest = journal.ReadLatest();
        Assert.True(latest.Ok, latest.Error);
        return (journal, lease!, latest.Snapshot!);
    }
}
