using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class ValidationPreCodecRemediationTests
{
    [Theory]
    [InlineData("ToolSourceName")]
    [InlineData("InputSourceName")]
    [InlineData("CoordinatorSourceName")]
    [InlineData("FilterSourceName")]
    [InlineData("TotalBudgetBytes")]
    [InlineData("MaxObjects")]
    public void IntegratedRefusesMissingPolicyExpectation(string name)
    {
        var projection = ExpectedProjection(2);
        Assert.True(projection["Integrated"]!["Profile"]!.AsObject().Remove(name));
        AssertBootstrap(2, Element(projection), ValidationPreRefusal.Expectation);
    }

    [Theory]
    [InlineData("ToolSourceName", "\"different-basename\"")]
    [InlineData("InputSourceName", "\"different-basename\"")]
    [InlineData("CoordinatorSourceName", "\"different-basename\"")]
    [InlineData("FilterSourceName", "\"different-basename\"")]
    [InlineData("TotalBudgetBytes", "16777215")]
    [InlineData("MaxObjects", "5")]
    public void IntegratedRefusesContradictoryPolicyExpectation(string name, string contradiction)
    {
        var projection = ExpectedProjection(2);
        projection["Integrated"]!["Profile"]![name] = JsonNode.Parse(contradiction);
        AssertBootstrap(2, Element(projection), ValidationPreRefusal.Comparison);
    }

    [Fact]
    public void IntegratedRefusesWrongKindPolicyExpectation()
    {
        var projection = ExpectedProjection(2);
        projection["Integrated"]!["Profile"]!["MaxObjects"] = "4";
        AssertBootstrap(2, Element(projection), ValidationPreRefusal.Expectation);
    }

    [Fact]
    public void IntegratedAcceptsMatchingPolicyExpectation()
    {
        var projection = ExpectedProjection(2);
        var profile = projection["Integrated"]!["Profile"]!;
        profile["ToolSourceName"] = "tool";
        profile["InputSourceName"] = "input";
        profile["CoordinatorSourceName"] = "coordinator";
        profile["FilterSourceName"] = "filter";
        profile["TotalBudgetBytes"] = 16_777_216;
        profile["MaxObjects"] = 4;
        AssertBootstrap(2, Element(projection), ValidationPreRefusal.None);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void RefusesConflictingDecodedSessionExpectation(int topology)
    {
        var json = ExpectedProjection(topology).ToJsonString();
        using var projection = JsonDocument.Parse(
            "{\"\\u0053ession\":\"7abb4591-d3c0-4012-a398-114477880063\"," + json[1..]);
        AssertBootstrap(topology, projection.RootElement, ValidationPreRefusal.Expectation);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void AcceptsOneEscapedSessionExpectation(int topology)
    {
        var json = ExpectedProjection(topology).ToJsonString().Replace("\"Session\"", "\"\\u0053ession\"", StringComparison.Ordinal);
        using var projection = JsonDocument.Parse(json);
        AssertBootstrap(topology, projection.RootElement, ValidationPreRefusal.None);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void RefusesDisposedProjection(int topology)
    {
        var document = JsonDocument.Parse(ExpectedProjection(topology).ToJsonString());
        var element = document.RootElement;
        document.Dispose();
        AssertBootstrap(topology, element, ValidationPreRefusal.Expectation);
    }

    [Fact]
    public void RefusesAmbiguousNestedPolicyExpectation()
    {
        var json = ExpectedProjection(2).ToJsonString()
            .Replace("\"MaxObjects\":4", "\"\\u004daxObjects\":5,\"MaxObjects\":4", StringComparison.Ordinal);
        using var projection = JsonDocument.Parse(json);
        AssertBootstrap(2, projection.RootElement, ValidationPreRefusal.Expectation);
    }

    [Fact]
    public void ConstructorBindsOriginalArtifactDigest()
    {
        var message = ValidationPreCodecVectors.Binary("W04");
        var callerDigest = TestDigest(0x11);
        var expected = new ValidationPreBinaryExpectations(callerDigest);
        var context = new ValidationPreBinaryContext(new(3, ValidationPreTopology.B2),
            ValidationPreMessage.RecoveryEchoResponse, MatchingPredecessor: true);
        Assert.Equal(ValidationPreRefusal.None, ValidationPreCodecBinary.ValidatePayload(message, context, expected));

        callerDigest[0] ^= 0x80;
        var replacement = message[..4].Concat(callerDigest).ToArray();
        Assert.Equal(ValidationPreRefusal.Comparison, ValidationPreCodecBinary.ValidatePayload(replacement, context, expected));
        Assert.Equal(ValidationPreRefusal.None, ValidationPreCodecBinary.ValidatePayload(message, context, expected));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CopyingOrRetrievingAnyDigestCannotTransferOwnership(bool mutateRetrievedMemory)
    {
        var message = ValidationPreCodecVectors.Binary("W03");
        var artifact = TestDigest(0x11);
        var owner = TestDigest(0x22);
        var recovery = TestDigest(0x33);
        var coordinator = TestDigest(0x44);
        var filter = TestDigest(0x55);
        var runtime = TestDigest(0x66);
        var expected = new ValidationPreBinaryExpectations(artifact)
        {
            Session = new Guid("ffeeddcc-bbaa-9988-7766-554433221100"),
            ProfileId = "ae4-linux-fixed-fixture-v1",
            ProfileVersion = "1",
            OwnerClosureSha256 = owner,
            RecoveryClosureSha256 = recovery,
            CoordinatorSha256 = coordinator,
            SeccompFilterSha256 = filter,
            RuntimeInventorySha256 = runtime
        };
        var context = new ValidationPreBinaryContext(new(3, ValidationPreTopology.B2),
            ValidationPreMessage.B2OwnerExpectation);
        Assert.Equal(ValidationPreRefusal.None, ValidationPreCodecBinary.ValidatePayload(message, context, expected));

        ReadOnlyMemory<byte>[] references = mutateRetrievedMemory
            ? [expected.ArtifactSetSha256, expected.OwnerClosureSha256, expected.RecoveryClosureSha256,
                expected.CoordinatorSha256, expected.SeccompFilterSha256, expected.RuntimeInventorySha256]
            : [artifact, owner, recovery, coordinator, filter, runtime];
        for (var index = 0; index < references.Length; index++)
        {
            Assert.True(MemoryMarshal.TryGetArray(references[index], out var segment));
            segment.Array![segment.Offset] ^= 0x80;
            var replacement = (byte[])message.Clone();
            replacement[16 + index * 32] ^= 0x80;
            Assert.Equal(ValidationPreRefusal.Comparison, ValidationPreCodecBinary.ValidatePayload(replacement, context, expected));
            Assert.Equal(ValidationPreRefusal.None, ValidationPreCodecBinary.ValidatePayload(message, context, expected));
        }
    }

    private static JsonObject ExpectedProjection(int topology) =>
        JsonNode.Parse(ValidationPreCodecVectors.Projection(topology).GetRawText())!.AsObject();

    private static JsonElement Element(JsonObject projection) => JsonSerializer.SerializeToElement(projection);

    private static byte[] TestDigest(byte value) => Enumerable.Repeat(value, 32).ToArray();

    private static void AssertBootstrap(int topology, JsonElement projection, ValidationPreRefusal expected)
    {
        var result = ValidationPreCodec.DecodeBootstrap(ValidationPreCodecVectors.Payload(topology),
            new(3, (ValidationPreTopology)topology), ValidationPreCodecVectors.CapabilityDigest, projection, out var data);
        Assert.Equal(expected, result);
        if (expected == ValidationPreRefusal.None)
            Assert.NotNull(data);
        else
            Assert.Null(data);
    }
}
