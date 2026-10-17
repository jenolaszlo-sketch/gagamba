using System.Buffers.Binary;
using System.Text;
using Gagamba.Execution.Linux.M1.Native;
using Gagamba.Execution.Linux.M1.Lifecycle;
using Gagamba.Execution.Linux.M1.Preparation;
using Gagamba.Execution.Linux.M1.Preparation.Native;
using Gagamba.Execution.Linux.M1.Recovery;
using Gagamba.Execution.Restricted;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Recovery;

public sealed class OwnerProtocolTests
{
    private static readonly Guid Execution = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Lease = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void QueuedWriterReadyAndAfterIntentBarrierAreReceivedAsTwoSequencedRecords()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("requires Linux SOCK_SEQPACKET");
        }

        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var ownerFd, out var scopeFd, out var socketError), socketError);
        using var owner = new OwnerProtocolChannel(ownerFd, Execution, Lease, ownsSocket: true);
        using var scope = new OwnerProtocolChannel(scopeFd, Execution, Lease, ownsSocket: true);

        Assert.True(owner.TrySend(OwnerProtocolMessageType.WriterReady, ReadOnlySpan<byte>.Empty, out var writerError), writerError);
        Assert.True(owner.TrySend(OwnerProtocolMessageType.BarrierReached, Encoding.UTF8.GetBytes("after-intent"), out var barrierError), barrierError);

        Assert.True(scope.TryReceive(OwnerProtocolMessageType.WriterReady, out var writerReady, out var receiveWriterError), receiveWriterError);
        Assert.Equal((uint)1, writerReady!.Sequence);
        Assert.True(scope.TryReceive(OwnerProtocolMessageType.BarrierReached, out var barrier, out var receiveBarrierError), receiveBarrierError);
        Assert.Equal((uint)2, barrier!.Sequence);
        Assert.Equal("after-intent", Encoding.UTF8.GetString(barrier.Payload));
    }

    [Fact]
    public void AfterIntentBarrierFollowsValidatedStartAuthorityAndCanBeReleased()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("requires Linux SOCK_SEQPACKET and SCM_RIGHTS");
        }

        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var ownerFd, out var scopeFd, out var socketError), socketError);
        using var owner = new OwnerProtocolChannel(ownerFd, Execution, Lease, ownsSocket: true);
        using var scope = new OwnerProtocolChannel(scopeFd, Execution, Lease, ownsSocket: true);
        var parentPath = Path.Combine(Path.GetTempPath(), "gagamba-m1-owner-parent-" + Guid.NewGuid().ToString("N"));
        var stagingPath = Path.Combine(Path.GetTempPath(), "gagamba-m1-owner-staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parentPath);
        Directory.CreateDirectory(stagingPath);
        var parentFd = -1;
        var stagingFd = -1;
        int[] receivedFds = Array.Empty<int>();
        try
        {
            Assert.Equal(0, PreparationInterop.OpenDirectory(parentPath, out parentFd));
            Assert.Equal(0, PreparationInterop.OpenDirectory(stagingPath, out stagingFd));

            Assert.True(owner.TrySend(OwnerProtocolMessageType.WriterReady, ReadOnlySpan<byte>.Empty, out var readyError), readyError);
            Assert.True(scope.TryReceive(OwnerProtocolMessageType.WriterReady, out var ready, out var receiveReadyError), receiveReadyError);
            Assert.Equal((uint)1, ready!.Sequence);

            Assert.True(scope.TrySendTransfer(
                OwnerProtocolMessageType.StartAuthority, new byte[] { 0, 1 },
                new[] { parentFd, stagingFd }, out var authorityError), authorityError);
            Assert.True(owner.TryReceiveTransfer(
                OwnerProtocolMessageType.StartAuthority, expectedDescriptorCount: 2, expectedPayloadBytes: 2,
                out var authority, out receivedFds, out var receiveAuthorityError), receiveAuthorityError);
            Assert.Equal((uint)1, authority!.Sequence);
            Assert.Equal(new byte[] { 0, 1 }, authority.Payload);
            Assert.Equal(2, receivedFds.Length);
            Assert.All(receivedFds, descriptor =>
                Assert.True(PreparationInterop.HasCloexec(descriptor), "received authority descriptors must be CLOEXEC"));

            Assert.True(owner.TrySend(OwnerProtocolMessageType.BarrierReached,
                Encoding.UTF8.GetBytes("after-intent"), out var barrierSendError), barrierSendError);
            Assert.True(scope.TryReceiveTransferOrControl(
                OwnerProtocolMessageType.DomainRecoveryAuthority, DomainHandleTransfer.ExactDescriptorCount, 1,
                out var barrier, out var unexpectedFds, out var receiveBarrierError), receiveBarrierError);
            Assert.Empty(unexpectedFds);
            Assert.Equal(OwnerProtocolMessageType.BarrierReached, barrier!.Type);
            Assert.Equal((uint)2, barrier.Sequence);
            Assert.Equal("after-intent", Encoding.UTF8.GetString(barrier.Payload));

            Assert.True(scope.TrySend(OwnerProtocolMessageType.Continue, ReadOnlySpan<byte>.Empty, out var continueError), continueError);
            Assert.True(owner.TryReceive(OwnerProtocolMessageType.Continue, out var continuation, out var receiveContinueError), receiveContinueError);
            Assert.Equal((uint)2, continuation!.Sequence);
        }
        finally
        {
            foreach (var descriptor in receivedFds)
            {
                PreparationInterop.CloseFd(descriptor);
            }

            if (parentFd >= 0) PreparationInterop.CloseFd(parentFd);
            if (stagingFd >= 0) PreparationInterop.CloseFd(stagingFd);
            Directory.Delete(parentPath);
            Directory.Delete(stagingPath);
        }
    }

    [Fact]
    public void AfterRegisteredBarrierAndStagingAuthorityFramesShareThePinnedLength()
    {
        var barrier = OwnerProtocol.Encode(new OwnerProtocolMessage(
            OwnerProtocolMessageType.BarrierReached, Execution, Lease, 1, Encoding.UTF8.GetBytes("after-registered")));
        var stagingAuthority = OwnerProtocol.Encode(new OwnerProtocolMessage(
            OwnerProtocolMessageType.StagingAuthority, Execution, Lease, 1, new byte[16]));

        Assert.Equal(64, barrier.Length);
        Assert.Equal(barrier.Length, stagingAuthority.Length);
    }

    [Fact]
    public void AfterRegisteredBarrierDispatchThenAcceptsTheFollowingStagingTransfer()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("requires Linux SOCK_SEQPACKET and SCM_RIGHTS");
        }

        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var ownerFd, out var scopeFd, out var socketError), socketError);
        using var owner = new OwnerProtocolChannel(ownerFd, Execution, Lease, ownsSocket: true);
        using var scope = new OwnerProtocolChannel(scopeFd, Execution, Lease, ownsSocket: true);
        var stagingPath = Path.Combine(Path.GetTempPath(), "gagamba-m1-staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingPath);
        var stagingFd = -1;
        int[] receivedFds = Array.Empty<int>();
        try
        {
            Assert.Equal(0, PreparationInterop.OpenDirectory(stagingPath, out stagingFd));
            Assert.Equal(0, PreparationInterop.StatFd(stagingFd, out var stagingStat));
            var payload = new byte[16];
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(0, 8), stagingStat.Device);
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(8, 8), stagingStat.Inode);

            var barrierFrame = OwnerProtocol.Encode(new OwnerProtocolMessage(
                OwnerProtocolMessageType.BarrierReached, Execution, Lease, 1,
                Encoding.UTF8.GetBytes("after-registered")));
            var transferFrame = OwnerProtocol.Encode(new OwnerProtocolMessage(
                OwnerProtocolMessageType.StagingAuthority, Execution, Lease, 1, payload));
            Assert.Equal(64, barrierFrame.Length);
            Assert.Equal(barrierFrame.Length, transferFrame.Length);

            Assert.True(owner.TrySend(OwnerProtocolMessageType.BarrierReached,
                Encoding.UTF8.GetBytes("after-registered"), out var barrierSendError), barrierSendError);
            Assert.True(scope.TryReceiveTransferOrControl(
                OwnerProtocolMessageType.StagingAuthority, 1, 16,
                out var barrierMessage, out var barrierFds, out var barrierReceiveError), barrierReceiveError);
            Assert.Equal(OwnerProtocolMessageType.BarrierReached, barrierMessage!.Type);
            Assert.Equal((uint)1, barrierMessage.Sequence);
            Assert.Empty(barrierFds);
            Assert.Equal("after-registered", Encoding.UTF8.GetString(barrierMessage.Payload));

            Assert.True(scope.TrySend(OwnerProtocolMessageType.Continue, ReadOnlySpan<byte>.Empty, out var continueError), continueError);
            Assert.True(owner.TryReceive(OwnerProtocolMessageType.Continue, out _, out var receiveContinueError), receiveContinueError);
            Assert.True(owner.TrySendTransfer(OwnerProtocolMessageType.StagingAuthority, payload,
                new[] { stagingFd }, out var transferSendError), transferSendError);

            Assert.True(scope.TryReceiveTransfer(OwnerProtocolMessageType.StagingAuthority, 1, 16,
                out var stagingMessage, out receivedFds, out var transferReceiveError), transferReceiveError);
            Assert.Equal((uint)2, stagingMessage!.Sequence);
            Assert.Equal(payload, stagingMessage.Payload);
            Assert.Single(receivedFds);
            Assert.True(PreparationInterop.HasCloexec(receivedFds[0]));
            Assert.Equal(0, PreparationInterop.StatFd(receivedFds[0], out var receivedStat));
            Assert.Equal(stagingStat.Device, receivedStat.Device);
            Assert.Equal(stagingStat.Inode, receivedStat.Inode);
        }
        finally
        {
            foreach (var fd in receivedFds)
            {
                PreparationInterop.CloseFd(fd);
            }

            if (stagingFd >= 0)
            {
                PreparationInterop.CloseFd(stagingFd);
            }

            Directory.Delete(stagingPath);
        }
    }

    [Theory]
    [InlineData("after-intent", "DomainRecoveryAuthority", true, 7, 1)]
    [InlineData("after-domain-retained", "DomainRegistered", false, 0, 0)]
    [InlineData("after-registered", "StagingAuthority", true, 1, 16)]
    [InlineData("after-staging-accounted", "StagedObjects", false, 0, 0)]
    [InlineData("mid-staging", "StagedObjects", false, 0, 0)]
    [InlineData("after-copy", "StagedObjects", false, 0, 0)]
    [InlineData("after-prepared-before-mint", "Completed", false, 0, 0)]
    public void EveryOwnerBarrierIsAcceptedOnlyInItsExplicitNextState(
        string barrier,
        string nextStateName,
        bool transferState,
        int transferDescriptors,
        int transferPayloadBytes)
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("requires Linux SOCK_SEQPACKET");
        }

        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var ownerFd, out var scopeFd, out var socketError), socketError);
        using var owner = new OwnerProtocolChannel(ownerFd, Execution, Lease, ownsSocket: true);
        using var scope = new OwnerProtocolChannel(scopeFd, Execution, Lease, ownsSocket: true);
        var nextState = Enum.Parse<OwnerProtocolMessageType>(nextStateName);
        Assert.True(owner.TrySend(OwnerProtocolMessageType.BarrierReached, Encoding.UTF8.GetBytes(barrier), out var sendError), sendError);

        OwnerProtocolMessage? received;
        int[] descriptors;
        string receiveError;
        if (transferState)
        {
            Assert.True(scope.TryReceiveTransferOrControl(nextState, transferDescriptors, transferPayloadBytes,
                out received, out descriptors, out receiveError), receiveError);
        }
        else
        {
            Assert.True(scope.TryReceiveAllowed(out received, out receiveError,
                nextState, OwnerProtocolMessageType.BarrierReached, OwnerProtocolMessageType.Aborted));
            descriptors = Array.Empty<int>();
        }

        Assert.Equal(OwnerProtocolMessageType.BarrierReached, received!.Type);
        Assert.Equal((uint)1, received.Sequence);
        Assert.Equal(Execution, received.ExecutionId);
        Assert.Equal(Lease, received.LeaseGeneration);
        Assert.Equal(barrier, Encoding.UTF8.GetString(received.Payload));
        Assert.Empty(descriptors);
    }

    [Fact]
    public void WrongTypeAtStagingTransferStateClosesDescriptorsWithoutAdvancingSequence()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("requires Linux SOCK_SEQPACKET and SCM_RIGHTS");
        }

        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var senderFd, out var receiverFd, out var socketError), socketError);
        using var receiver = new OwnerProtocolChannel(receiverFd, Execution, Lease, ownsSocket: true);
        var stagingPath = Path.Combine(Path.GetTempPath(), "gagamba-m1-staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingPath);
        var sourceFd = -1;
        var expectedOpenCount = 0;
        int[] acceptedFds = Array.Empty<int>();
        try
        {
            Assert.Equal(0, PreparationInterop.OpenDirectory(stagingPath, out sourceFd));
            expectedOpenCount = CountFdsTargeting(stagingPath);
            var wrongType = OwnerProtocol.Encode(new OwnerProtocolMessage(
                OwnerProtocolMessageType.StagedObjects, Execution, Lease, 1, new byte[16]));
            Assert.Equal(64, wrongType.Length);
            Assert.True(FdPassing.TrySendTransfer(senderFd, new[] { sourceFd }, wrongType, out var sendError), sendError);

            Assert.False(receiver.TryReceiveTransfer(OwnerProtocolMessageType.StagingAuthority, 1, 16,
                out var rejectedMessage, out var rejectedFds, out var rejectError));
            Assert.Null(rejectedMessage);
            Assert.Empty(rejectedFds);
            Assert.Contains("unexpected owner transfer type", rejectError, StringComparison.Ordinal);
            Assert.Equal(expectedOpenCount, CountFdsTargeting(stagingPath));

            var statPayload = new byte[16];
            Assert.Equal(0, PreparationInterop.StatFd(sourceFd, out var sourceStat));
            BinaryPrimitives.WriteUInt64LittleEndian(statPayload.AsSpan(0, 8), sourceStat.Device);
            BinaryPrimitives.WriteUInt64LittleEndian(statPayload.AsSpan(8, 8), sourceStat.Inode);
            var correctType = OwnerProtocol.Encode(new OwnerProtocolMessage(
                OwnerProtocolMessageType.StagingAuthority, Execution, Lease, 1, statPayload));
            Assert.True(FdPassing.TrySendTransfer(senderFd, new[] { sourceFd }, correctType, out sendError), sendError);

            Assert.True(receiver.TryReceiveTransfer(OwnerProtocolMessageType.StagingAuthority, 1, 16,
                out var acceptedMessage, out acceptedFds, out var acceptError), acceptError);
            Assert.Equal((uint)1, acceptedMessage!.Sequence);
            Assert.Single(acceptedFds);
        }
        finally
        {
            foreach (var fd in acceptedFds)
            {
                PreparationInterop.CloseFd(fd);
            }

            if (sourceFd >= 0)
            {
                PreparationInterop.CloseFd(sourceFd);
            }

            PreparationInterop.CloseFd(senderFd);
            Directory.Delete(stagingPath);
        }
    }

    [Theory]
    [InlineData("WriterReady", false)]
    [InlineData("BarrierReached", true)]
    public void DescriptorBearingControlOrBarrierIsRefusedWithoutAdvancingSequence(
        string typeName,
        bool transferState)
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("requires Linux SOCK_SEQPACKET and SCM_RIGHTS");
        }

        var type = Enum.Parse<OwnerProtocolMessageType>(typeName);
        var payload = type == OwnerProtocolMessageType.BarrierReached
            ? Encoding.UTF8.GetBytes("after-registered")
            : Array.Empty<byte>();
        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var senderFd, out var receiverFd, out var socketError), socketError);
        using var receiver = new OwnerProtocolChannel(receiverFd, Execution, Lease, ownsSocket: true);
        var stagingPath = Path.Combine(Path.GetTempPath(), "gagamba-m1-staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingPath);
        var sourceFd = -1;
        try
        {
            Assert.Equal(0, PreparationInterop.OpenDirectory(stagingPath, out sourceFd));
            var baseline = CountFdsTargeting(stagingPath);
            var frame = OwnerProtocol.Encode(new OwnerProtocolMessage(type, Execution, Lease, 1, payload));
            Assert.True(FdPassing.TrySendTransfer(senderFd, new[] { sourceFd }, frame, out var sendError), sendError);

            OwnerProtocolMessage? rejected;
            int[] descriptors;
            string rejectError;
            if (transferState)
            {
                Assert.False(receiver.TryReceiveTransferOrControl(OwnerProtocolMessageType.StagingAuthority, 1, 16,
                    out rejected, out descriptors, out rejectError));
            }
            else
            {
                Assert.False(receiver.TryReceiveAllowed(out rejected, out rejectError,
                    OwnerProtocolMessageType.WriterReady,
                    OwnerProtocolMessageType.BarrierReached,
                    OwnerProtocolMessageType.Aborted));
                descriptors = Array.Empty<int>();
            }

            Assert.Null(rejected);
            Assert.Empty(descriptors);
            Assert.Contains("descriptor", rejectError, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(baseline, CountFdsTargeting(stagingPath));

            Assert.True(FdPassing.TrySendMessage(senderFd,
                OwnerProtocol.Encode(new OwnerProtocolMessage(type, Execution, Lease, 1, payload)), out sendError), sendError);
            OwnerProtocolMessage? accepted;
            string acceptError;
            if (transferState)
            {
                Assert.True(receiver.TryReceiveTransferOrControl(OwnerProtocolMessageType.StagingAuthority, 1, 16,
                    out accepted, out descriptors, out acceptError), acceptError);
                Assert.Empty(descriptors);
            }
            else
            {
                Assert.True(receiver.TryReceive(OwnerProtocolMessageType.WriterReady,
                    out accepted, out acceptError), acceptError);
            }

            Assert.Equal((uint)1, accepted!.Sequence);
        }
        finally
        {
            if (sourceFd >= 0)
            {
                PreparationInterop.CloseFd(sourceFd);
            }

            PreparationInterop.CloseFd(senderFd);
            Directory.Delete(stagingPath);
        }
    }

    [Fact]
    public void StagingTransferWithoutDescriptorsDoesNotAdvanceSequence()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("requires Linux SOCK_SEQPACKET and SCM_RIGHTS");
        }

        Assert.True(FdPassing.TryCreateInheritableSocketPair(out var senderFd, out var receiverFd, out var socketError), socketError);
        using var receiver = new OwnerProtocolChannel(receiverFd, Execution, Lease, ownsSocket: true);
        var stagingPath = Path.Combine(Path.GetTempPath(), "gagamba-m1-staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingPath);
        var sourceFd = -1;
        int[] acceptedFds = Array.Empty<int>();
        try
        {
            Assert.Equal(0, PreparationInterop.OpenDirectory(stagingPath, out sourceFd));
            Assert.Equal(0, PreparationInterop.StatFd(sourceFd, out var stat));
            var payload = new byte[16];
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(0, 8), stat.Device);
            BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(8, 8), stat.Inode);
            var missingFds = OwnerProtocol.Encode(new OwnerProtocolMessage(
                OwnerProtocolMessageType.StagingAuthority, Execution, Lease, 1, payload));
            Assert.True(FdPassing.TrySendMessage(senderFd, missingFds, out var sendError), sendError);

            Assert.False(receiver.TryReceiveTransfer(OwnerProtocolMessageType.StagingAuthority, 1, 16,
                out var rejected, out var rejectedFds, out var rejectError));
            Assert.Null(rejected);
            Assert.Empty(rejectedFds);
            Assert.Contains("descriptor count", rejectError, StringComparison.OrdinalIgnoreCase);

            Assert.True(FdPassing.TrySendTransfer(senderFd, new[] { sourceFd }, missingFds, out sendError), sendError);
            Assert.True(receiver.TryReceiveTransfer(OwnerProtocolMessageType.StagingAuthority, 1, 16,
                out var accepted, out acceptedFds, out var acceptError), acceptError);
            Assert.Equal((uint)1, accepted!.Sequence);
            Assert.Single(acceptedFds);
        }
        finally
        {
            foreach (var fd in acceptedFds)
            {
                PreparationInterop.CloseFd(fd);
            }

            if (sourceFd >= 0)
            {
                PreparationInterop.CloseFd(sourceFd);
            }

            PreparationInterop.CloseFd(senderFd);
            Directory.Delete(stagingPath);
        }
    }

    private static int CountFdsTargeting(string path)
    {
        var count = 0;
        foreach (var entry in Directory.EnumerateFileSystemEntries("/proc/self/fd"))
        {
            try
            {
                if (File.ResolveLinkTarget(entry, returnFinalTarget: true)?.FullName == path)
                {
                    count++;
                }
            }
            catch
            {
            }
        }

        return count;
    }

    [Fact]
    public void FrameRoundTripsBoundedPayloadAndIdentity()
    {
        var sequencer = new OwnerProtocolSequencer(Execution, Lease);
        var encoded = sequencer.Create(OwnerProtocolMessageType.Start, new byte[] { 1, 2, 3 });
        var receiver = new OwnerProtocolSequencer(Execution, Lease);

        Assert.True(receiver.TryAccept(encoded, OwnerProtocolMessageType.Start, out var message, out var error), error);
        Assert.Equal((uint)1, message!.Sequence);
        Assert.Equal(new byte[] { 1, 2, 3 }, message.Payload);
    }

    [Fact]
    public void WrongIdentityTypeSequenceAndLengthAreRejected()
    {
        var sender = new OwnerProtocolSequencer(Execution, Lease);
        var first = sender.Create(OwnerProtocolMessageType.Start);

        Assert.False(OwnerProtocol.TryDecode(first, Guid.NewGuid(), Lease, out _, out _));
        Assert.False(OwnerProtocol.TryDecode(first, Execution, Guid.NewGuid(), out _, out _));
        var wrongTypeReceiver = new OwnerProtocolSequencer(Execution, Lease);
        Assert.False(wrongTypeReceiver.TryAccept(first, OwnerProtocolMessageType.WriterReady, out _, out _));

        var receiver = new OwnerProtocolSequencer(Execution, Lease);
        Assert.True(receiver.TryAccept(first, OwnerProtocolMessageType.Start, out _, out _));
        Assert.False(receiver.TryAccept(first, OwnerProtocolMessageType.Start, out _, out _));
        Assert.False(OwnerProtocol.TryDecode(first.AsSpan(0, first.Length - 1), Execution, Lease, out _, out _));
    }

    [Fact]
    public void OversizedPayloadCannotBeEncoded()
    {
        var sender = new OwnerProtocolSequencer(Execution, Lease);
        Assert.Throws<ArgumentException>(() => sender.Create(
            OwnerProtocolMessageType.Start,
            new byte[OwnerProtocol.MaxPayloadBytes + 1]));
    }

    [Fact]
    public void PreparationSpecificationRoundTripsAndBuildsMatchingIntent()
    {
        var specification = new OwnerPreparationSpecification(
            Path.GetFullPath("journal"), "domain-a", Execution, Lease, "boot-a", Guid.NewGuid(), Guid.NewGuid(),
            Path.GetFullPath("source"), Path.GetFullPath("staging"), 12, 34, "prep-a",
            new[] { new CopyObjectPlan("tool", "tool.bin", 4096, null) }, 8192, 8,
            Array.Empty<KeyValuePair<string, string>>(), 100_000_000);

        var encoded = OwnerPreparationSpecificationCodec.Encode(specification);
        Assert.True(OwnerPreparationSpecificationCodec.TryDecode(encoded, out var decoded, out var error), error);
        Assert.Equal(encoded, OwnerPreparationSpecificationCodec.Encode(decoded!));
        var intent = OwnerPreparationSpecificationCodec.BuildIntent(decoded!);
        Assert.Equal(PreparationStage.AllocationIntent, intent.Stage);
        Assert.Equal((ulong)12, intent.StagingParentDevice);
        Assert.Equal((ulong)34, intent.StagingParentInode);
        Assert.Equal(3, intent.Allocations.Count);
        Assert.Equal("tool-000.staged", intent.Allocations[2].Name);
    }

    [Fact]
    public void PreparationSpecificationRejectsRelativePathsAndUnknownBarriers()
    {
        var invalid = new OwnerPreparationSpecification(
            "relative-journal", "domain-a", Execution, Lease, "boot-a", Guid.NewGuid(), Guid.NewGuid(),
            Path.GetFullPath("source"), Path.GetFullPath("staging"), 12, 34, "prep-a",
            new[] { new CopyObjectPlan("tool", "tool.bin", 4096, null) }, 8192, 8,
            Array.Empty<KeyValuePair<string, string>>(), 100_000_000, "unlisted");

        Assert.Throws<ArgumentException>(() => OwnerPreparationSpecificationCodec.Encode(invalid));
    }

    [Fact]
    public void ManifestAuthorizationRoundTripsAndBindsItsDigest()
    {
        var issuer = Guid.NewGuid();
        var generation = Guid.NewGuid();
        var preparation = Guid.NewGuid();
        var manifest = Gagamba.Execution.Linux.M1.Tests.Contracts.ManifestTests.SampleManifest();
        var identity = new PreparationIdentity(
            Execution, issuer, generation, preparation, ManifestCodec.ComputeDigestBytes(manifest));
        var authorization = new OwnerManifestAuthorization(manifest, identity);

        var encoded = OwnerPreparationSpecificationCodec.EncodeAuthorization(authorization);
        Assert.True(OwnerPreparationSpecificationCodec.TryDecodeAuthorization(
            encoded, out var decoded, out var error), error);
        Assert.Equal(ManifestCodec.ComputeDigestHex(manifest),
            ManifestCodec.ComputeDigestHex(decoded!.Manifest));
        Assert.Equal(preparation, decoded.Identity.PreparationId);
    }
}
