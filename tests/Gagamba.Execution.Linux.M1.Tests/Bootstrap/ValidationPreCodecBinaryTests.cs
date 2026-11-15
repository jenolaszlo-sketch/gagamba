using Gagamba.Execution.Linux.M1.Bootstrap;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class ValidationPreCodecBinaryTests
{
    private static ValidationPreRefusal Check(byte[] bytes, int topology, int message,
        bool previous = false, ushort version = 3, ValidationPreBinaryExpectations? expectation = null) =>
        ValidationPreCodecBinary.ValidatePayload(bytes,
            new ValidationPreBinaryContext(new ValidationPreContext(version, (ValidationPreTopology)topology),
                (ValidationPreMessage)message, previous),
            expectation ?? ValidationPreCodecVectors.BinaryExpectations());

    [Theory]
    [InlineData("W01", 1, 9, 76)]
    [InlineData("W02", 1, 36, 76)]
    [InlineData("IE-V01", 2, 9, 76)]
    [InlineData("W02", 2, 36, 76)]
    [InlineData("IE-V03", 2, 9, 105)]
    [InlineData("IE-V02", 2, 36, 105)]
    [InlineData("W03", 1, 37, 246)]
    public void ExactFrozenEchoesAndExpectation(string vector, int topology, int message, int length)
    {
        var bytes = ValidationPreCodecVectors.Binary(vector);
        Assert.Equal(length, bytes.Length);
        Assert.Equal(0, bytes[message == 37 ? 216 : 71]);
        Assert.Equal(ValidationPreRefusal.None, Check(bytes, topology, message));
        if (length == 105)
        {
            Assert.Equal(26, bytes[76]);
            Assert.Equal(1, bytes[103]);
        }
    }

    [Theory]
    [InlineData("W06", 1, 9)]
    [InlineData("W07", 1, 9)]
    [InlineData("W08", 1, 9)]
    [InlineData("W09", 1, 9)]
    [InlineData("IE-V04", 2, 36)]
    [InlineData("IE-V05", 2, 9)]
    [InlineData("IE-V06", 2, 36)]
    [InlineData("IE-V07", 2, 9)]
    [InlineData("IE-V08", 2, 36)]
    [InlineData("IE-V09", 2, 9)]
    [InlineData("IE-V11", 2, 9)]
    [InlineData("IE-V12", 2, 36)]
    public void FrozenMalformedEchoesRefuse(string vector, int topology, int message) =>
        Assert.NotEqual(ValidationPreRefusal.None, Check(ValidationPreCodecVectors.Binary(vector), topology, message));

    [Theory]
    [InlineData(0, 9)]
    [InlineData(32, 9)]
    [InlineData(64, 9)]
    [InlineData(66, 9)]
    [InlineData(68, 9)]
    [InlineData(70, 9)]
    [InlineData(71, 9)]
    [InlineData(72, 9)]
    [InlineData(76, 9)]
    [InlineData(83, 9)]
    [InlineData(103, 9)]
    [InlineData(104, 9)]
    [InlineData(0, 37)]
    [InlineData(16, 37)]
    [InlineData(48, 37)]
    [InlineData(80, 37)]
    [InlineData(112, 37)]
    [InlineData(144, 37)]
    [InlineData(176, 37)]
    [InlineData(208, 37)]
    [InlineData(210, 37)]
    [InlineData(212, 37)]
    [InlineData(214, 37)]
    [InlineData(216, 37)]
    [InlineData(217, 37)]
    [InlineData(218, 37)]
    [InlineData(244, 37)]
    [InlineData(245, 37)]
    public void EveryBinaryFieldHasAnIndependentMutation(int offset, int message)
    {
        var topo = message == 9 ? 2 : 1;
        var bytes = ValidationPreCodecVectors.Binary(message == 9 ? "IE-V03" : "W03");
        Assert.Equal(ValidationPreRefusal.None, Check(bytes, topo, message));
        bytes[offset] ^= 0x80;
        Assert.NotEqual(ValidationPreRefusal.None, Check(bytes, topo, message));
    }

    [Theory]
    [InlineData("IE-V01", 9)]
    [InlineData("IE-V02", 36)]
    public void IntegratedBothLengthsRequireTheExactOriginalProfile(string vector, int message)
    {
        var bytes = ValidationPreCodecVectors.Binary(vector);
        Assert.Equal(ValidationPreRefusal.Expectation, Check(bytes, 2, message,
            expectation: ValidationPreCodecVectors.BinaryExpectations(null)));
        Assert.Equal(ValidationPreRefusal.Expectation, Check(bytes, 2, message,
            expectation: ValidationPreCodecVectors.BinaryExpectations("Other")));
    }

    [Theory]
    [InlineData("W04", 1, 10)]
    [InlineData("W04", 2, 10)]
    [InlineData("W05", 1, 12)]
    public void ConfirmationsRequireExactOriginalFieldsAndMatchingPredecessor(string vector, int topology, int message)
    {
        var bytes = ValidationPreCodecVectors.Binary(vector);
        Assert.Equal(message == 10 ? 36 : 48, bytes.Length);
        Assert.Equal(ValidationPreRefusal.None, Check(bytes, topology, message, previous: true));
        Assert.Equal(ValidationPreRefusal.Context, Check(bytes, topology, message));
        Assert.Equal(ValidationPreRefusal.Size, Check(bytes[..^1], topology, message, previous: true));
        Assert.Equal(ValidationPreRefusal.Size, Check([.. bytes, 0], topology, message, previous: true));
        Assert.Equal(ValidationPreRefusal.Context, Check(bytes, topology, message, previous: true, version: 2));
    }

    [Theory]
    [InlineData("W10", 1, 10)]
    [InlineData("W11", 1, 12)]
    public void ConfirmationDigestMismatchRefuses(string vector, int topology, int message) =>
        Assert.Equal(ValidationPreRefusal.Comparison, Check(ValidationPreCodecVectors.Binary(vector),
            topology, message, previous: true));

    [Fact]
    public void ConfirmationRefusalCodesRetainTheirHistoricalMeanings()
    {
        var refusal = ValidationPreCodecVectors.Binary("W13");
        for (byte reason = 1; reason <= 5; reason++)
        {
            refusal[1] = reason;
            Assert.Equal(ValidationPreRefusal.PeerRefusal, Check(refusal, 1, 10, previous: true));
        }
        refusal[1] = 6;
        Assert.Equal(ValidationPreRefusal.Layout, Check(refusal, 1, 10, previous: true));
        var malformed = ValidationPreCodecVectors.Binary("W14");
        Assert.Equal(ValidationPreRefusal.Layout, Check(malformed, 1, 10, previous: true));
        var success = ValidationPreCodecVectors.Binary("W04");
        success[2] = 1;
        Assert.Equal(ValidationPreRefusal.Layout, Check(success, 1, 10, previous: true));
    }

    [Fact]
    public void B2SessionAndMessageContextDoNotTransferToIntegrated()
    {
        var confirmation = ValidationPreCodecVectors.Binary("W12");
        Assert.Equal(ValidationPreRefusal.Context, Check(confirmation, 2, 12, previous: true));
        confirmation[0] ^= 1;
        Assert.Equal(ValidationPreRefusal.Comparison, Check(confirmation, 1, 12, previous: true));
        var owner = ValidationPreCodecVectors.Binary("W03");
        Assert.Equal(ValidationPreRefusal.Context, Check(owner, 2, 37));
        Assert.Equal(ValidationPreRefusal.Size, Check(owner[..216], 1, 37));
        Assert.Equal(ValidationPreRefusal.Size, Check([..owner, 0], 1, 37));
        Assert.Equal(ValidationPreRefusal.Context, Check(owner, 1, 37, version: 1));
    }

    [Fact]
    public void LegacyBinaryValidationAndPreValidationRemainSeparate()
    {
        var echo = ValidationPreCodecVectors.Binary("W01");
        Assert.False(InstallationWire.EchoShape(echo, 0));
        echo[71] = 1;
        Assert.True(InstallationWire.EchoShape(echo, 0));
        Assert.Equal(ValidationPreRefusal.Layout, Check(echo, 1, 9));
        Assert.Equal(ValidationPreRefusal.Context, Check(echo, 1, 9, version: 2));
        var originalExpectation = ValidationPreCodecVectors.BinaryExpectations();
        var owner = ValidationPreCodecVectors.Binary("W03");
        Assert.Throws<InvalidDataException>(() => InstallationWire.ReadExpectation(owner, originalExpectation.Session));
        owner[216] = 1;
        Assert.NotNull(InstallationWire.ReadExpectation(owner, originalExpectation.Session));
        Assert.Equal(ValidationPreRefusal.Layout, Check(owner, 1, 37));
    }

    [Fact]
    public void CodecHasNoReleasePayloadOperationAndRejectsMissingExpectations()
    {
        var payload = ValidationPreCodecVectors.Binary("W01");
        Assert.Equal(ValidationPreRefusal.Context, Check(new byte[32], 2, (int)Gagamba.Execution.Linux.M1.Native.OwnerProtocolMessageType.ReleaseRequest));
        var context = new ValidationPreBinaryContext(new(3, ValidationPreTopology.B2), ValidationPreMessage.RecoveryEcho);
        Assert.Equal(ValidationPreRefusal.Expectation, ValidationPreCodecBinary.ValidatePayload(payload, context, null));
        Assert.Equal(ValidationPreRefusal.Expectation, ValidationPreCodecBinary.ValidatePayload(payload, context,
            new ValidationPreBinaryExpectations(new byte[31])));
    }
}
