using System.Text;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

public sealed class FrozenContractTests
{
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    [Fact]
    public void B2ChainVerifiesWithIndependentlyComputedDigests()
    {
        Assert.True(FrozenContractVerifier.TryVerifyChain(
            Bytes(ValidationFixtures.M2), Bytes(ValidationFixtures.F2),
            Bytes(ValidationFixtures.S2), Bytes(ValidationFixtures.K2),
            out var chain, out var failure), failure);
        Assert.NotNull(chain);
        Assert.Equal(ValidationFixtures.Hm2, chain!.Hm);
        Assert.Equal(ValidationFixtures.Hf2, chain.Hf);
        Assert.Equal(ValidationFixtures.Hs2, chain.Hs);
        Assert.Equal(ValidationFixtures.Hk2, chain.Hk);
        Assert.Equal("case-2", chain.Catalog.CaseId);
        Assert.Equal("pre", chain.Catalog.Phase);
        Assert.Equal("b2-lifeline", chain.Catalog.Topology);
        Assert.False(chain.Catalog.AllowRelease);
        Assert.Null(chain.Catalog.QualificationId);
        Assert.Null(chain.Catalog.QualificationRecordSha256);
        Assert.Empty(chain.Manifest.Objects);
        Assert.Equal("b2-lifeline-v1", chain.Policy.PolicyKind);
        Assert.False(chain.Script.HasRelease);
        Assert.Equal(new[] { ValidationFixtures.A2_0, ValidationFixtures.A2_1, ValidationFixtures.A2_2,
            ValidationFixtures.A2_3 }, chain.Script.ActionHashes);
    }

    [Fact]
    public void B3ReleaseChainVerifiesWithIndependentlyComputedDigests()
    {
        Assert.True(FrozenContractVerifier.TryVerifyChain(
            Bytes(ValidationFixtures.M3), Bytes(ValidationFixtures.F3),
            Bytes(ValidationFixtures.S3), Bytes(ValidationFixtures.K3),
            out var chain, out var failure), failure);
        Assert.NotNull(chain);
        Assert.Equal(ValidationFixtures.Hm3, chain!.Hm);
        Assert.Equal(ValidationFixtures.Hf3, chain.Hf);
        Assert.Equal(ValidationFixtures.Hs3, chain.Hs);
        Assert.Equal(ValidationFixtures.Hk3, chain.Hk);
        Assert.Equal("case-3", chain.Catalog.CaseId);
        Assert.Equal("post", chain.Catalog.Phase);
        Assert.Equal("b3-integrated", chain.Catalog.Topology);
        Assert.True(chain.Catalog.AllowRelease);
        Assert.Equal("Q1", chain.Catalog.QualificationId);
        Assert.Equal(new string('f', 64), chain.Catalog.QualificationRecordSha256);
        Assert.Equal(new[] { "coordinator", "filter", "input", "tool" },
            chain.Manifest.Objects.Select(item => item.Role).ToArray());
        Assert.Equal(new[] { "0555", "0444", "0444", "0555" },
            chain.Manifest.Objects.Select(item => item.Mode).ToArray());
        Assert.Equal(new[] { "/coordinator", "fd:6", "/input", "/tool" },
            chain.Manifest.Objects.Select(item => item.Destination).ToArray());
        Assert.Equal("b3-static-fixture-v1", chain.Policy.PolicyKind);
        Assert.True(chain.Script.HasRelease);
        Assert.Equal(new[] { ValidationFixtures.A3_0, ValidationFixtures.A3_1, ValidationFixtures.A3_2,
            ValidationFixtures.A3_3, ValidationFixtures.A3_4, ValidationFixtures.A3_5 },
            chain.Script.ActionHashes);
    }

    [Fact]
    public void IndividualArtifactVerifiersAcceptEveryAcceptedFixture()
    {
        Assert.True(FrozenContractVerifier.TryVerifyManifest(Bytes(ValidationFixtures.M2), out var m2, out _));
        Assert.Equal("b2-lifeline", m2!.Topology);
        Assert.True(FrozenContractVerifier.TryVerifyManifest(Bytes(ValidationFixtures.M3), out var m3, out _));
        Assert.Equal(4, m3!.Objects.Count);
        Assert.True(FrozenContractVerifier.TryVerifyPolicy(Bytes(ValidationFixtures.F2), out var f2, out _));
        Assert.Equal(1000, f2!.Uid);
        Assert.True(FrozenContractVerifier.TryVerifyPolicy(Bytes(ValidationFixtures.F3), out var f3, out _));
        Assert.Equal(1000, f3!.Gid);
        Assert.True(FrozenContractVerifier.TryVerifyActionScript(Bytes(ValidationFixtures.S2), out var s2, out _));
        Assert.Equal(4, s2!.Actions.Count);
        Assert.True(FrozenContractVerifier.TryVerifyActionScript(Bytes(ValidationFixtures.S3), out var s3, out _));
        Assert.Equal(6, s3!.Actions.Count);
        Assert.True(FrozenContractVerifier.TryVerifyCatalog(Bytes(ValidationFixtures.K2), out var k2, out _));
        Assert.Equal("test", k2!.Scope);
        Assert.True(FrozenContractVerifier.TryVerifyCatalog(Bytes(ValidationFixtures.K3), out var k3, out _));
        Assert.Equal("linux-x64", k3!.Rid);
    }

    [Fact]
    public void RepeatedVerificationIsDeterministic()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.True(FrozenContractVerifier.TryVerifyChain(
                Bytes(ValidationFixtures.M3), Bytes(ValidationFixtures.F3),
                Bytes(ValidationFixtures.S3), Bytes(ValidationFixtures.K3),
                out var chain, out var failure), failure);
            Assert.Equal(ValidationFixtures.Hm3, chain!.Hm);
            Assert.Equal(ValidationFixtures.Hf3, chain.Hf);
            Assert.Equal(ValidationFixtures.Hs3, chain.Hs);
            Assert.Equal(ValidationFixtures.Hk3, chain.Hk);
            Assert.Equal(6, chain.Script.ActionHashes.Count);
        }
    }
}
