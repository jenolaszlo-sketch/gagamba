using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

/// <summary>Positive producer controls for the E-2-P2 detached review (R), owner
/// receipt (Wc), and protected-approval content (P) verification core. The
/// complete chains used here are, by explicit label, STRUCTURALLY VALID,
/// OPERATIONALLY UNTRUSTED TEST CONTENT: successful verification establishes
/// frozen-schema conformance and content-binding consistency only, and never
/// reviewer, owner, or administrator authority, protected acquisition or custody,
/// catalog approval or eligibility, invocation authorization, or protected
/// execution. All fixtures are test-only bytes, not genuine issued records.</summary>
public sealed class DetachedEvidenceTests
{
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    [Fact]
    public void B2DetachedChainVerifiesWithIndependentlyComputedDigests()
    {
        Assert.True(DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain(
            Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2),
            Bytes(DetachedEvidenceFixtures.Wc2),
            Bytes(DetachedEvidenceFixtures.P2),
            out var bindings, out var failure), failure);
        Assert.NotNull(bindings);
        Assert.Equal(ValidationFixtures.Hk2, bindings!.Hk);
        Assert.Equal(DetachedEvidenceFixtures.Hr2, bindings.Hr);
        Assert.Equal(DetachedEvidenceFixtures.Hw2, bindings.Hw);
        Assert.Equal(DetachedEvidenceFixtures.Hp2, bindings.Hp);
        Assert.Equal("case-2", bindings.Review.CaseId);
        Assert.Equal("pre", bindings.Review.Phase);
        Assert.Equal("b2-lifeline", bindings.Review.Topology);
        Assert.Equal("supported-for-bounded-validation", bindings.Review.Conclusion);
        Assert.Null(bindings.Review.QualificationId);
        Assert.Equal(ValidationFixtures.Hk2, bindings.Review.CatalogSha256);
        Assert.Equal("case-2", bindings.Receipt.CaseId);
        Assert.Equal("accept-exact-bounded-validation-catalog", bindings.Receipt.Decision);
        Assert.Equal(DetachedEvidenceFixtures.Hr2, bindings.Receipt.CatalogReviewSha256);
        Assert.Equal("case-2", bindings.AdminContent.CaseId);
        Assert.Equal(DetachedEvidenceFixtures.Hw2, bindings.AdminContent.OwnerCatalogAcceptanceSha256);
        Assert.Equal("pre", bindings.AdminContent.Phase);
        Assert.Equal("b2-lifeline", bindings.Catalog.Topology);
        Assert.False(bindings.Catalog.AllowRelease);
    }

    [Fact]
    public void B3DetachedChainVerifiesWithIndependentlyComputedDigests()
    {
        Assert.True(DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain(
            Bytes(ValidationFixtures.K3),
            Bytes(DetachedEvidenceFixtures.R3),
            Bytes(DetachedEvidenceFixtures.Wc3),
            Bytes(DetachedEvidenceFixtures.P3),
            out var bindings, out var failure), failure);
        Assert.NotNull(bindings);
        Assert.Equal(ValidationFixtures.Hk3, bindings!.Hk);
        Assert.Equal(DetachedEvidenceFixtures.Hr3, bindings.Hr);
        Assert.Equal(DetachedEvidenceFixtures.Hw3, bindings.Hw);
        Assert.Equal(DetachedEvidenceFixtures.Hp3, bindings.Hp);
        Assert.Equal("case-3", bindings.Review.CaseId);
        Assert.Equal("post", bindings.Review.Phase);
        Assert.Equal("b3-integrated", bindings.Review.Topology);
        Assert.Equal("supported-for-bounded-validation", bindings.Review.Conclusion);
        Assert.Equal("Q1", bindings.Review.QualificationId);
        Assert.Equal(new string('f', 64), bindings.Review.QualificationRecordSha256);
        Assert.Equal("accept-exact-bounded-validation-catalog", bindings.Receipt.Decision);
        Assert.Equal("Q1", bindings.Receipt.QualificationId);
        Assert.Equal(DetachedEvidenceFixtures.Hr3, bindings.Receipt.CatalogReviewSha256);
        Assert.Equal("Q1", bindings.AdminContent.QualificationId);
        Assert.Equal(DetachedEvidenceFixtures.Hw3, bindings.AdminContent.OwnerCatalogAcceptanceSha256);
        Assert.True(bindings.Catalog.AllowRelease);
    }

    [Fact]
    public void IndividualDetachedVerifiersAcceptEveryFixture()
    {
        Assert.True(DetachedEvidenceVerifier.TryVerifyReviewRecord(
            Bytes(DetachedEvidenceFixtures.R2), Bytes(ValidationFixtures.K2), out var r2, out var r2Failure));
        Assert.Equal(string.Empty, r2Failure);
        Assert.Equal("supported-for-bounded-validation", r2!.Conclusion);
        Assert.Null(r2.QualificationId);
        Assert.True(DetachedEvidenceVerifier.TryVerifyReviewRecord(
            Bytes(DetachedEvidenceFixtures.R3), Bytes(ValidationFixtures.K3), out var r3, out _));
        Assert.Equal("Q1", r3!.QualificationId);
        Assert.Equal(new string('f', 64), r3.QualificationRecordSha256);
        Assert.True(DetachedEvidenceVerifier.TryVerifyOwnerReceiptRecord(
            Bytes(DetachedEvidenceFixtures.Wc2), Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2), out var wc2, out var wc2Failure));
        Assert.Equal(string.Empty, wc2Failure);
        Assert.Equal(DetachedEvidenceFixtures.Hr2, wc2!.CatalogReviewSha256);
        Assert.Equal("accept-exact-bounded-validation-catalog", wc2.Decision);
        Assert.True(DetachedEvidenceVerifier.TryVerifyOwnerReceiptRecord(
            Bytes(DetachedEvidenceFixtures.Wc3), Bytes(ValidationFixtures.K3),
            Bytes(DetachedEvidenceFixtures.R3), out var wc3, out _));
        Assert.Equal(DetachedEvidenceFixtures.Hr3, wc3!.CatalogReviewSha256);
        Assert.True(DetachedEvidenceVerifier.TryVerifyAdminRecordContent(
            Bytes(DetachedEvidenceFixtures.P2), Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2),
            out var p2, out var p2Failure));
        Assert.Equal(string.Empty, p2Failure);
        Assert.Equal(DetachedEvidenceFixtures.Hw2, p2!.OwnerCatalogAcceptanceSha256);
        Assert.Null(p2.QualificationId);
        Assert.True(DetachedEvidenceVerifier.TryVerifyAdminRecordContent(
            Bytes(DetachedEvidenceFixtures.P3), Bytes(ValidationFixtures.K3),
            Bytes(DetachedEvidenceFixtures.R3), Bytes(DetachedEvidenceFixtures.Wc3),
            out var p3, out _));
        Assert.Equal("Q1", p3!.QualificationId);
    }

    [Fact]
    public void ContentIdentitiesMatchIndependentPlatformDigests()
    {
        // The expected digest constants were produced before the P2 verifier
        // existed, by the standalone generator
        // (docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p2-evidence/fixtures/
        // generate-fixtures.ps1) using the platform SHA-256 primitive over the
        // exact fixture bytes. This test re-derives them with the same primitive
        // and with the accepted P1 canonical hash as two further witnesses.
        Assert.Equal(DetachedEvidenceFixtures.Hr2, Sha(Bytes(DetachedEvidenceFixtures.R2)));
        Assert.Equal(DetachedEvidenceFixtures.Hw2, Sha(Bytes(DetachedEvidenceFixtures.Wc2)));
        Assert.Equal(DetachedEvidenceFixtures.Hp2, Sha(Bytes(DetachedEvidenceFixtures.P2)));
        Assert.Equal(DetachedEvidenceFixtures.Hr3, Sha(Bytes(DetachedEvidenceFixtures.R3)));
        Assert.Equal(DetachedEvidenceFixtures.Hw3, Sha(Bytes(DetachedEvidenceFixtures.Wc3)));
        Assert.Equal(DetachedEvidenceFixtures.Hp3, Sha(Bytes(DetachedEvidenceFixtures.P3)));
        Assert.Equal(ValidationFixtures.Hk2, Sha(Bytes(ValidationFixtures.K2)));
        Assert.Equal(ValidationFixtures.Hk3, Sha(Bytes(ValidationFixtures.K3)));
        Assert.Equal(DetachedEvidenceFixtures.Hr2, CanonicalJson.Hash(Bytes(DetachedEvidenceFixtures.R2)));
        Assert.Equal(DetachedEvidenceFixtures.Hw2, CanonicalJson.Hash(Bytes(DetachedEvidenceFixtures.Wc2)));
        Assert.Equal(DetachedEvidenceFixtures.Hp2, CanonicalJson.Hash(Bytes(DetachedEvidenceFixtures.P2)));
        Assert.Equal(DetachedEvidenceFixtures.Hr3, CanonicalJson.Hash(Bytes(DetachedEvidenceFixtures.R3)));
        Assert.Equal(DetachedEvidenceFixtures.Hw3, CanonicalJson.Hash(Bytes(DetachedEvidenceFixtures.Wc3)));
        Assert.Equal(DetachedEvidenceFixtures.Hp3, CanonicalJson.Hash(Bytes(DetachedEvidenceFixtures.P3)));
    }

    [Fact]
    public void RepeatedVerificationIsDeterministicAndDoesNotMutateInputs()
    {
        var catalog = Bytes(ValidationFixtures.K3);
        var review = Bytes(DetachedEvidenceFixtures.R3);
        var receipt = Bytes(DetachedEvidenceFixtures.Wc3);
        var admin = Bytes(DetachedEvidenceFixtures.P3);
        var catalogCopy = (byte[])catalog.Clone();
        var reviewCopy = (byte[])review.Clone();
        var receiptCopy = (byte[])receipt.Clone();
        var adminCopy = (byte[])admin.Clone();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            Assert.True(DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain(
                catalog, review, receipt, admin, out var bindings, out var failure), failure);
            Assert.Equal(ValidationFixtures.Hk3, bindings!.Hk);
            Assert.Equal(DetachedEvidenceFixtures.Hr3, bindings.Hr);
            Assert.Equal(DetachedEvidenceFixtures.Hw3, bindings.Hw);
            Assert.Equal(DetachedEvidenceFixtures.Hp3, bindings.Hp);
        }

        Assert.Equal(catalogCopy, catalog);
        Assert.Equal(reviewCopy, review);
        Assert.Equal(receiptCopy, receipt);
        Assert.Equal(adminCopy, admin);
    }

    [Fact]
    public void UntrustedChainIsStructurallyValidButCarriesNoAuthorityProjection()
    {
        // STRUCTURALLY VALID, OPERATIONALLY UNTRUSTED TEST CONTENT.
        // Every accepted structural and content-binding rule is satisfied: K binds
        // R binds Wc binds P, all duplicated values agree, and all content
        // identities are correct. The success below must not be read -- and the
        // P2 result surface must not project -- reviewer authority, owner
        // authority, administrator authority, protected acquisition or custody,
        // catalog approval or eligibility, invocation permission, positive
        // validation authority, or protected execution.
        Assert.True(DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain(
            Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2),
            Bytes(DetachedEvidenceFixtures.Wc2),
            Bytes(DetachedEvidenceFixtures.P2),
            out var bindings, out var failure), failure);
        Assert.NotNull(bindings);
        Assert.Equal("supported-for-bounded-validation", bindings!.Review.Conclusion);
        Assert.Equal("accept-exact-bounded-validation-catalog", bindings.Receipt.Decision);
        Assert.Contains("\"accepted\":true", DetachedEvidenceFixtures.P2, StringComparison.Ordinal);
        // The required accepted=true literal is verified by the P-content rules
        // but is deliberately not projected as a result property, so no P2 value
        // can be misread as an approval flag.
        Assert.DoesNotContain(typeof(AdminRecordContentContract).GetProperties(),
            property => property.Name == "Accepted");
        // Result contracts expose content values only; no property of any P2
        // result type is an authority-bearing type or a promotion method.
        foreach (var type in new[]
                 {
                     typeof(ReviewRecordContract), typeof(OwnerReceiptRecordContract),
                     typeof(AdminRecordContentContract), typeof(DetachedEvidenceBindingsContract),
                 })
        {
            foreach (var property in type.GetProperties())
            {
                var propertyNamespace = property.PropertyType.Namespace;
                var isGagambaType = propertyNamespace is not null &&
                    (propertyNamespace == "Gagamba" ||
                     propertyNamespace.StartsWith("Gagamba.", StringComparison.Ordinal));
                Assert.False(isGagambaType &&
                             propertyNamespace != "Gagamba.Execution.Linux.M1.Validation",
                    $"{type.Name}.{property.Name} exposes {property.PropertyType.FullName}");
            }
        }
    }
}
