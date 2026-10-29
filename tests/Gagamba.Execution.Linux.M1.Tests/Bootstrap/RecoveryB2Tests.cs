using System.Buffers.Binary;
using System.Security.Cryptography;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Native;
using Xunit;
namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;
public sealed class RecoveryB2Tests
{
    private static readonly Guid Execution = Guid.NewGuid();
    private static readonly Guid Generation = Guid.NewGuid();
    private static RecoveryConfiguration Config() => new(1, Execution, Generation, Guid.NewGuid(), 123, 1000,
        RandomNumberGenerator.GetBytes(32), new("/installed/recovery", []), new("/installed/owner", []), "/private/evidence");
    [Theory]
    [InlineData(null, "4")][InlineData("3", null)][InlineData("3", "3")][InlineData("0", "4")]
    [InlineData("-1", "4")][InlineData("x", "4")][InlineData("4", "2")]
    public void MissingDuplicateOrInvalidDescriptorRoleRefuses(string? a, string? b) =>
        Assert.Throws<InvalidDataException>(() => RecoveryConfiguration.DescriptorRoles(a, b));
    [Fact] public void DistinctDescriptorRolesAreRequired() => Assert.Equal((3, 4), RecoveryConfiguration.DescriptorRoles("3", "4"));
    [Fact] public void UnsupportedBootstrapVersionRefuses() => Assert.Throws<InvalidDataException>(() => (Config() with { Version = 2 }).Validate());
    [Fact] public void EmptyExecutionRefuses() => Assert.Throws<InvalidDataException>(() => (Config() with { Execution = Guid.Empty }).Validate());
    [Fact] public void EmptyGenerationRefuses() => Assert.Throws<InvalidDataException>(() => (Config() with { Generation = Guid.Empty }).Validate());
    [Fact] public void PrivilegedControllerRefuses() => Assert.Throws<InvalidDataException>(() => (Config() with { ControllerUid = 0 }).Validate());
    [Fact] public void InvalidSecretRefuses() => Assert.Throws<InvalidDataException>(() => (Config() with { Secret = new byte[31] }).Validate());
    [Fact] public void UnknownBootstrapFieldsAndExcessiveSizeRefuse()
    {
        Assert.Throws<InvalidDataException>(() => RecoveryConfiguration.Decode(new byte[8193]));
        var json = System.Text.Json.JsonSerializer.Serialize(Config());
        var unknown = System.Text.Encoding.UTF8.GetBytes(json[..^1] + ",\"ReleaseToken\":\"forbidden\"}");
        Assert.Throws<System.Text.Json.JsonException>(() => RecoveryConfiguration.Decode(unknown));
    }
    [Fact] public void MissingOwnerInstallationRefuses() => Assert.Throws<InvalidDataException>(() => (Config() with { Owner = null! }).Validate());
    [Theory][InlineData(0)][InlineData(1)][InlineData(47)][InlineData(561)]
    public void MalformedAndOversizedFramesRefuse(int size) => Assert.False(new RecoveryWire(Execution, Generation).Accept(new byte[size], out _, out _));
    [Fact] public void UnsupportedWireVersionRefuses()
    {
        var bytes = new RecoveryWire(Execution, Generation).Create(RecoveryMessage.Stop);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4), 2);
        Assert.False(new RecoveryWire(Execution, Generation).Accept(bytes, out _, out _));
    }
    [Theory][InlineData(true)][InlineData(false)]
    public void WrongExecutionOrGenerationRefuses(bool execution)
    {
        var frame = new RecoveryWire(execution ? Guid.NewGuid() : Execution, execution ? Generation : Guid.NewGuid()).Create(RecoveryMessage.Stop);
        Assert.False(new RecoveryWire(Execution, Generation).Accept(frame, out _, out _));
    }
    [Fact] public void DuplicateFrameAndReorderedFrameRefuse()
    {
        var send = new RecoveryWire(Execution, Generation); var receive = new RecoveryWire(Execution, Generation);
        var first = send.Create(RecoveryMessage.Stop); var second = send.Create(RecoveryMessage.Stop);
        Assert.False(receive.Accept(second, out _, out _)); Assert.True(receive.Accept(first, out _, out _));
        Assert.False(receive.Accept(first, out _, out _)); Assert.True(receive.Accept(second, out _, out _));
    }
    [Fact] public void InvalidPeerPidUidAndRootRefuse()
    {
        var peer = new RecoveryInterop.Peer { Pid = 123, Uid = 1000, Gid = 1000 };
        Assert.True(RecoveryInterop.MatchesPeer(peer, 123, 1000));
        Assert.False(RecoveryInterop.MatchesPeer(peer, 124, 1000)); Assert.False(RecoveryInterop.MatchesPeer(peer, 123, 1001));
        Assert.False(RecoveryInterop.MatchesPeer(peer with { Uid = 0 }, 123, 0));
    }
    [Fact] public void SessionCredentialIsSingleUse()
    {
        var secret = RandomNumberGenerator.GetBytes(32); var session = Guid.NewGuid();
        using var association = new RecoveryAssociation(session, secret);
        var auth = new byte[48]; session.TryWriteBytes(auth); secret.CopyTo(auth, 16);
        var wrong = auth.ToArray(); wrong[16] ^= 1; Assert.False(association.Authenticate(wrong));
        var other = auth.ToArray(); Guid.NewGuid().TryWriteBytes(other); Assert.False(association.Authenticate(other));
        Assert.True(association.Authenticate(auth)); Assert.False(association.Authenticate(auth));
    }
    [Fact] public void RepeatedLossStopAndLostAcknowledgementPreserveDeadline()
    {
        var obligation = new RecoveryObligation();
        Assert.True(obligation.Request("ControllerExited", 100)); var deadline = obligation.Deadline;
        Assert.False(obligation.Request("OwnerExited", 500)); Assert.False(obligation.Request("Duplicate", long.MaxValue));
        Assert.Equal(deadline, obligation.Deadline); Assert.Equal(1, obligation.Count);
        Assert.Equal("ControllerExited", obligation.Cause); Assert.False(obligation.OwnerAcknowledged);
        obligation.OwnerAcknowledged = true; Assert.Equal(deadline, obligation.Deadline); Assert.False(obligation.ReleaseAuthority);
    }
    [Fact] public void NoReleaseMessageOrCapabilityTransferExists()
    {
        Assert.DoesNotContain(Enum.GetNames<RecoveryMessage>(), name => name.Contains("Release") || name.Contains("Prepare"));
        Assert.False(new RecoveryObligation().ReleaseAuthority);
        Assert.Throws<ArgumentException>(() => new RecoveryWire(Execution, Generation).Create((RecoveryMessage)99));
        Assert.Throws<ArgumentException>(() => new RecoveryWire(Execution, Generation).Create(RecoveryMessage.Inspect, new byte[513]));
    }
    [Fact] public void PassiveRecordCannotReconstructInstallationAuthority() =>
        Assert.Throws<InvalidDataException>(() => new PinnedExecutable("/passive/record", []).Verify("Gagamba.Execution.Linux.M1.Owner"));
    [Fact] public void ForeignOwnerAndMissingRetainedPidfdCannotCloseLifeline()
    {
        using var scope = new Gagamba.Execution.Linux.M1.Lifecycle.M1RecoveryScope();
        using var unrelated = System.Diagnostics.Process.GetCurrentProcess();
        Assert.False(scope.CloseOwnerLifeline(unrelated));
    }
    [Fact] public void OwnerB2ControlShapesCannotCarryAuthority()
    {
        foreach (var type in new[] { OwnerProtocolMessageType.B2LifelineReady, OwnerProtocolMessageType.B2SafetyStop, OwnerProtocolMessageType.B2StopObserved })
        {
            Assert.False(OwnerProtocol.IsTransferType(type));
            Assert.True(OwnerProtocol.HasValidControlPayloadShape(new(type, Execution, Generation, 1, [])));
            Assert.False(OwnerProtocol.HasValidControlPayloadShape(new(type, Execution, Generation, 1, [1])));
        }
    }
}
