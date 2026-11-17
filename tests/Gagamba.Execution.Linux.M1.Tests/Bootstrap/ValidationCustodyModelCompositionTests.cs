using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Bootstrap;
using Gagamba.Execution.Linux.M1.Tests.Validation;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;
using static Gagamba.Execution.Linux.M1.Tests.Bootstrap.ValidationCustodyModelVectors;

namespace Gagamba.Execution.Linux.M1.Tests.Bootstrap;

public sealed class ValidationCustodyModelCompositionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void R16_AcceptedHandoffFeedsOnlyObservedValues(int topology)
    {
        var pre = new ValidationPreContext(3, (ValidationPreTopology)topology);
        var payload = ValidationPreCodecVectors.Payload(topology);
        var capability = ValidationPreCodecVectors.CapabilityDigest;
        var projection = ValidationPreCodecVectors.Projection(topology);
        var ids = Gava1ContextVectors.BootstrapIds;
        var independentCatalog = Gava1ContextVectors.Catalog;
        var expected = new Inputs(topology) { Ids = ids };
        expected.Digests[7] = independentCatalog;
        expected.Digests[14] = capability;

        Assert.Equal(ValidationPreRefusal.None,
            ValidationPreCodec.DecodeBootstrap(payload, pre, capability, projection, out var data));
        Assert.NotNull(data);
        var bootstrap = new Gava1PreBootstrap(data.Fields.GetProperty("Version").GetUInt16(),
            (Gava1PreTopology)data.Topology, data.Fields.GetProperty("AuthorityMode").GetString(),
            new(data.Fields.GetProperty("Session").GetGuid(), data.Fields.GetProperty("Execution").GetGuid(),
                data.Fields.GetProperty("Generation").GetGuid()),
            Convert.FromHexString(data.Fields.GetProperty("AuthorityHash").GetString()!));
        byte message = topology == 1 ? (byte)4 : (byte)6;
        byte recipient = topology == 1 ? (byte)3 : (byte)4;
        var frame = Gava1ContextVectors.Pack(message, 2, ids, capability, independentCatalog);
        var expectedRoute = Gava1ContextVectors.Expect(message, 2, recipient,
            new Gava1Originals(ids, capability, independentCatalog));
        Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.Decode(frame, expectedRoute, out var context));
        Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.MatchPreBootstrap(bootstrap, context,
            (Gava1PreTopology)topology, new(ids, capability, independentCatalog), out var handoff));
        Assert.NotNull(handoff);

        var release = Gava1ContextVectors.ReleaseWitness;
        Assert.Equal(32, release.Length);
        Assert.Equal(ValidationPreRefusal.Comparison,
            ValidationPreCodec.DecodeBootstrap(payload, pre, release, projection, out var failure));
        Assert.Null(failure);
        Assert.Equal(Gava1Refusal.Comparison, Gava1ContextCodec.MatchPreBootstrap(bootstrap with { Capability = release },
            context, (Gava1PreTopology)topology, new(ids, capability, independentCatalog), out var refused));
        Assert.Null(refused);
        Assert.Equal(Gava1Refusal.Comparison, Gava1ContextCodec.MatchPreBootstrap(bootstrap,
            context, (Gava1PreTopology)topology, new(ids, capability, release), out refused));
        Assert.Null(refused);

        var identity = topology == 1 ? data.Fields.GetProperty("B2InstallationIdentity")
            : data.Fields.GetProperty("Integrated").GetProperty("InstallationIdentity");
        var generation = Convert.FromHexString(identity.GetProperty("Generation").GetString()!);
        Assert.Equal(32, generation.Length);
        Assert.Equal(Gava1Refusal.Comparison, Gava1ContextCodec.MatchPreBootstrap(
            bootstrap with { Ids = ids with { Lease = new Guid(generation.AsSpan(0, 16)) } }, context,
            (Gava1PreTopology)topology, new(ids, capability, independentCatalog), out refused));
        Assert.Null(refused);

        Array.Clear(frame);
        Array.Clear(handoff.CatalogDigest);
        Array.Clear(handoff.CapabilityDigest);
        var observed = AdaptHandoff(topology, handoff);
        Assert.Equal(ValidationCustodyRefusal.None, ValidationCustodyModel.TryCreate(Expected(expected),
            Observed(inputs: observed), 0, false, out var model));
        Assert.NotNull(model);
        using (model)
        {
            var snapshot = Accept(model, 1, 1, Observed(1, observed));
            Assert.Equal(ids, snapshot.Binding.Ids);
            Assert.Equal(independentCatalog, snapshot.Binding.Catalog);
            Assert.Equal(capability, snapshot.Binding.Capability);
        }

        foreach (string field in new[] { "catalog", "capability", "IDs" })
        {
            var suspectIds = field == "IDs" ? ids with { Execution = Label(99) } : ids;
            var suspectCatalog = field == "catalog" ? release : independentCatalog;
            var suspectCapability = field == "capability" ? release : capability;
            var suspectOriginals = new Gava1Originals(suspectIds, suspectCapability, suspectCatalog);
            var suspectFrame = Gava1ContextVectors.Pack(message, 2, suspectIds, suspectCapability, suspectCatalog);
            Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.Decode(suspectFrame,
                Gava1ContextVectors.Expect(message, 2, recipient, suspectOriginals), out var suspectContext));
            var suspectBoot = bootstrap with { Ids = suspectIds, Capability = suspectCapability };
            Assert.Equal(Gava1Refusal.None, Gava1ContextCodec.MatchPreBootstrap(suspectBoot, suspectContext,
                (Gava1PreTopology)topology, new(suspectIds, suspectCapability, suspectCatalog), out var suspectHandoff));
            Assert.NotNull(suspectHandoff);
            CreateRefuses($"R16 agreeing suspect {field}", ValidationCustodyRefusal.Comparison, Expected(expected),
                Observed(inputs: AdaptHandoff(topology, suspectHandoff)));
        }
    }

    [Theory]
    [InlineData(1, "W01")]
    [InlineData(2, "IE-V03")]
    public void R16_AcceptedBinaryBoundaryRetainsOriginalRefusals(int topology, string packet)
    {
        var context = new ValidationPreBinaryContext(new(3, (ValidationPreTopology)topology),
            ValidationPreMessage.RecoveryEcho);
        var expected = ValidationPreCodecVectors.BinaryExpectations();
        var body = ValidationPreCodecVectors.Binary(packet);
        Assert.Equal(ValidationPreRefusal.None, ValidationPreCodecBinary.ValidatePayload(body, context, expected));
        Assert.Equal(ValidationPreRefusal.Expectation, ValidationPreCodecBinary.ValidatePayload(body, context, null));
        Assert.Equal(ValidationPreRefusal.Size, ValidationPreCodecBinary.ValidatePayload(body.AsSpan(1), context, expected));
        body[0] ^= 0x80;
        Assert.Equal(ValidationPreRefusal.Comparison, ValidationPreCodecBinary.ValidatePayload(body, context, expected));
    }

    [Fact]
    public void R04_R17_ReboundChainsRemainSubjectToIndependentCatalogAndApproval()
    {
        var expected = new Inputs { CaseId = "case-2", Host = Label(17) };
        expected.Digests[3] = Convert.FromHexString(new string('d', 64));
        expected.Digests[5] = Convert.FromHexString(new string('e', 64));
        expected.Digests[6] = Convert.FromHexString(new string('b', 64));
        expected.Digests[7] = Hash(ValidationFixtures.K2);
        expected.Digests[8] = Hash(ValidationFixtures.M2);
        expected.Digests[9] = Hash(ValidationFixtures.F2);
        expected.Digests[10] = Hash(ValidationFixtures.S2);
        expected.Digests[11] = Hash(DetachedEvidenceFixtures.R2);
        expected.Digests[12] = Hash(DetachedEvidenceFixtures.Wc2);
        expected.Digests[13] = Hash(DetachedEvidenceFixtures.P2);

        var accepted = ObserveChain(Bytes(ValidationFixtures.M2), Bytes(ValidationFixtures.F2),
            Bytes(ValidationFixtures.S2), Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2),
            Bytes(DetachedEvidenceFixtures.Wc2), Bytes(DetachedEvidenceFixtures.P2), out var failure);
        Assert.Equal("", failure);
        Assert.NotNull(accepted);
        Assert.Equal(ValidationCustodyRefusal.None, ValidationCustodyModel.TryCreate(Expected(expected),
            accepted, 0, false, out var model));
        Assert.NotNull(model);
        model.Dispose();

        var changedK = ValidationFixtures.K2.Replace(new string('0', 40), new string('1', 40), StringComparison.Ordinal);
        var changedHk = HexHash(changedK);
        var changedR = DetachedEvidenceFixtures.R2.Replace(ValidationFixtures.Hk2, changedHk, StringComparison.Ordinal);
        var changedHr = HexHash(changedR);
        var changedW = DetachedEvidenceFixtures.Wc2.Replace(ValidationFixtures.Hk2, changedHk, StringComparison.Ordinal)
            .Replace(DetachedEvidenceFixtures.Hr2, changedHr, StringComparison.Ordinal);
        var changedP = DetachedEvidenceFixtures.P2.Replace(ValidationFixtures.Hk2, changedHk, StringComparison.Ordinal)
            .Replace(DetachedEvidenceFixtures.Hr2, changedHr, StringComparison.Ordinal)
            .Replace(DetachedEvidenceFixtures.Hw2, HexHash(changedW), StringComparison.Ordinal);
        var rebound = ObserveChain(Bytes(ValidationFixtures.M2), Bytes(ValidationFixtures.F2),
            Bytes(ValidationFixtures.S2), Bytes(changedK), Bytes(changedR), Bytes(changedW), Bytes(changedP), out failure);
        Assert.Equal("", failure);
        Assert.NotNull(rebound);
        Assert.NotEqual(expected.Digests[7], rebound.Binding.Catalog);
        Assert.NotEqual(expected.Digests[13], rebound.Binding.Approval);
        CreateRefuses("R04/R17 hash-consistent rebound chain", ValidationCustodyRefusal.Comparison,
            Expected(expected), rebound);

        var wrongApproval = new Inputs { CaseId = expected.CaseId, Host = expected.Host };
        Array.Copy(expected.Digests, wrongApproval.Digests, expected.Digests.Length);
        wrongApproval.Digests[13] = Hash("independently held wrong approval");
        CreateRefuses("R04/R17 wrong external Hp", ValidationCustodyRefusal.Comparison, Expected(wrongApproval), accepted);
    }

    [Fact]
    public void R17_InvalidChainsRetainFailureAndCannotFeedTheModel()
    {
        byte[]?[] operands = [Bytes(ValidationFixtures.M2), Bytes(ValidationFixtures.F2), Bytes(ValidationFixtures.S2),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2),
            Bytes(DetachedEvidenceFixtures.P2)];
        for (int i = 0; i < operands.Length; i++)
        {
            var missing = operands.ToArray();
            missing[i] = null;
            Assert.Null(ObserveChain(missing[0], missing[1], missing[2], missing[3], missing[4], missing[5], missing[6],
                out var missingFailure));
            Assert.Equal("missing supplied chain operand", missingFailure);
        }

        Assert.Null(ObserveChain(operands[0], operands[1], operands[2], operands[3], operands[4], operands[5],
            Bytes(DetachedEvidenceFixtures.P2.Replace("\"case-2\"", "\"case-9\"", StringComparison.Ordinal)), out var failure));
        Assert.Equal("P caseId disagreement", failure);
        Assert.Null(ObserveChain(operands[0], operands[1], operands[2], Bytes(ValidationFixtures.K3),
            operands[4], operands[5], operands[6], out failure));
        Assert.Equal("K fixtureManifestSha256 does not bind M", failure);
    }

    [Fact]
    public void R18_CoreModelTypesAreInternalAndTestsCarryTheExpectedReferences()
    {
        using var coreFile = File.OpenRead(typeof(ValidationCustodyModel).Assembly.Location);
        using var core = new PEReader(coreFile);
        var metadata = core.GetMetadataReader();
        var modelTypes = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
            .Where(type => metadata.GetString(type.Name).StartsWith("ValidationCustody", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(9, modelTypes.Length);
        Assert.All(modelTypes, type => Assert.Equal(TypeAttributes.NotPublic, type.Attributes & TypeAttributes.VisibilityMask));
        Assert.All(modelTypes.SelectMany(type => type.GetMethods()), method =>
            Assert.False(metadata.GetMethodDefinition(method).Attributes.HasFlag(MethodAttributes.PinvokeImpl)));

        using var testFile = File.OpenRead(typeof(ValidationCustodyModelCompositionTests).Assembly.Location);
        using var tests = new PEReader(testFile);
        var testMetadata = tests.GetMetadataReader();
        var references = testMetadata.MemberReferences.Select(testMetadata.GetMemberReference)
            .Where(member => member.Parent.Kind == HandleKind.TypeReference &&
                testMetadata.GetString(testMetadata.GetTypeReference((TypeReferenceHandle)member.Parent).Name) ==
                "ValidationCustodyModel").Select(member => testMetadata.GetString(member.Name)).ToArray();
        Assert.Contains("TryCreate", references);
        Assert.Contains("Advance", references);
    }

    private static Inputs AdaptHandoff(int topology, Gava1Originals handoff)
    {
        var observation = new Inputs(topology) { Ids = handoff.Ids };
        observation.Digests[7] = handoff.CatalogDigest;
        observation.Digests[14] = handoff.CapabilityDigest;
        return observation;
    }

    private static ValidationCustodyEvidence? ObserveChain(byte[]? m, byte[]? f, byte[]? s, byte[]? k,
        byte[]? r, byte[]? w, byte[]? p, out string failure)
    {
        if (m is null || f is null || s is null || k is null || r is null || w is null || p is null)
        {
            failure = "missing supplied chain operand";
            return null;
        }
        if (!FrozenContractVerifier.TryVerifyChain(m, f, s, k, out var chain, out failure) ||
            !DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain(k, r, w, p, out var detached, out failure))
        {
            return null;
        }

        Assert.NotNull(chain);
        Assert.NotNull(detached);
        Assert.Equal(new string('a', 64), chain.Catalog.HostId);
        var observation = new Inputs { CaseId = chain.Catalog.CaseId, Host = Label(17) };
        observation.Digests[3] = Convert.FromHexString(chain.Catalog.ControllerClosureSha256);
        observation.Digests[5] = Convert.FromHexString(chain.Catalog.DriverClosureSha256);
        observation.Digests[6] = Convert.FromHexString(chain.Catalog.ArtifactSetSha256);
        observation.Digests[7] = Convert.FromHexString(chain.Hk);
        observation.Digests[8] = Convert.FromHexString(chain.Hm);
        observation.Digests[9] = Convert.FromHexString(chain.Hf);
        observation.Digests[10] = Convert.FromHexString(chain.Hs);
        observation.Digests[11] = Convert.FromHexString(detached.Hr);
        observation.Digests[12] = Convert.FromHexString(detached.Hw);
        observation.Digests[13] = Convert.FromHexString(detached.Hp);
        return Observed(inputs: observation);
    }

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);
    private static byte[] Hash(string text) => SHA256.HashData(Bytes(text));
    private static string HexHash(string text) => Convert.ToHexStringLower(Hash(text));
}
