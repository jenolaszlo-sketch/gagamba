// RC2A-02A Q2A-B public digest, identifier and misuse boundaries (test-only).
// Exercises the public contract only: null-manifest programming errors, wrong
// digest lengths including the R2A-F04 combined null+wrong-length ordering,
// matching/nonmatching/all-zero digests, exact Supports behavior, and an
// independent BCL SHA-256 recomputation of the preserved golden bytes.
using System.Security.Cryptography;
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Tests.Restricted;

public sealed class Rc2a02PublicBoundaryTests
{
    private const string GoldenDigest = "82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3";

    private static byte[] Golden() => Convert.FromHexString(RestrictedContractTests.GoldenHex);

    private static RestrictedManifestView View() =>
        Assert.IsType<RestrictedManifestView>(RestrictedManifestCodec.Decode(Golden()).Value);

    [Fact]
    public void Q2A_B_20_NullManifestArgumentsAreProgrammingErrors()
    {
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.Encode(null!));
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.ComputeSha256(null!));
        Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.VerifySha256(null!, new byte[32]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    [InlineData(33)]
    [InlineData(64)]
    [InlineData(65536)]
    public void Q2A_B_21_WrongDigestLengthsReturnFalse(int length) =>
        Assert.False(RestrictedManifestCodec.VerifySha256(View(), new byte[length]));

    [Fact]
    public void Q2A_B_22_MatchingDigestIsTrueAndChangedOrAllZeroDigestIsFalse()
    {
        var digest = Convert.FromHexString(GoldenDigest);
        Assert.True(RestrictedManifestCodec.VerifySha256(View(), digest));
        var changed = digest.ToArray();
        changed[0] ^= 1;
        Assert.False(RestrictedManifestCodec.VerifySha256(View(), changed));
        Assert.False(RestrictedManifestCodec.VerifySha256(View(), new byte[32]));
    }

    [Fact]
    public void Q2A_B_23_CombinedNullManifestAndWrongLengthDigestThrowsArgumentNullException()
    {
        // R2A-F04: the null-manifest check precedes the digest length check, so
        // the combined input is a programming error, not a false verification.
        Assert.Throws<ArgumentNullException>(
            () => RestrictedManifestCodec.VerifySha256(null!, new byte[31]));
        Assert.Throws<ArgumentNullException>(
            () => RestrictedManifestCodec.VerifySha256(null!, new byte[65536]));
    }

    [Fact]
    public void Q2A_B_24_SupportsIsExactForEveryDocumentedIdentifierVariant()
    {
        const string supported = "linux-m1-native-transaction";
        Assert.True(RestrictedManifestCodec.Supports(1, supported));
        Assert.False(RestrictedManifestCodec.Supports(0, supported));
        Assert.False(RestrictedManifestCodec.Supports(2, supported));
        Assert.False(RestrictedManifestCodec.Supports(65535, supported));
        Assert.False(RestrictedManifestCodec.Supports(1, "unknown"));
        Assert.False(RestrictedManifestCodec.Supports(1, string.Empty));
        Assert.False(RestrictedManifestCodec.Supports(1, " "));
        Assert.False(RestrictedManifestCodec.Supports(1, " linux-m1-native-transaction"));
        Assert.False(RestrictedManifestCodec.Supports(1, "linux-m1-native-transaction "));
        Assert.False(RestrictedManifestCodec.Supports(1, "Linux-m1-native-transaction"));
        Assert.False(RestrictedManifestCodec.Supports(1, supported + "\0"));
        foreach (var schema in new ushort[] { 0, 1, 2, 65535 })
        {
            Assert.Throws<ArgumentNullException>(() => RestrictedManifestCodec.Supports(schema, null!));
        }
    }

    [Fact]
    public void Q2A_B_25_GoldenDigestIsTheIndependentSha256OfTheCommittedBytes()
    {
        var golden = Golden();
        Assert.Equal(873, golden.Length);
        Assert.Equal(
            GoldenDigest,
            Convert.ToHexString(SHA256.HashData(golden)).ToLowerInvariant());
        var view = View();
        Assert.Equal(SHA256.HashData(golden), RestrictedManifestCodec.ComputeSha256(view).ToArray());
        Assert.True(RestrictedManifestCodec.VerifySha256(view, Convert.FromHexString(GoldenDigest)));
    }
}
