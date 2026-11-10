using System.Security.Cryptography;
using System.Text;
using Gagamba.Execution.Linux.M1.Validation;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Validation;

/// <summary>Producer negative controls for the E-2-P2 detached-evidence core.
/// Structural refusals are byte-level and name the accepted frozen rule that
/// requires rejection; binding and semantic refusals recompute affected content
/// identities so the intended relationship rule -- not a stale hash -- is the
/// first observed refusal. Authority metadata injection is a structural
/// field-set rejection, never interpreted as authority evidence. These fixtures
/// are test-only, non-authoritative bytes.</summary>
public sealed class DetachedEvidenceNegativeTests
{
    private static readonly string Dead = new('d', 64);

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static string Edit(string source, params (string Find, string Replace)[] edits)
    {
        var text = source;
        foreach (var (find, replace) in edits)
        {
            Assert.Contains(find, text, StringComparison.Ordinal);
            text = text.Replace(find, replace, StringComparison.Ordinal);
        }

        return text;
    }

    private static byte[] Mutate(string source, params (string Find, string Replace)[] edits) =>
        Bytes(Edit(source, edits));

    private static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static void ReviewFailure(string expected, byte[] review, byte[] catalog)
    {
        Assert.False(DetachedEvidenceVerifier.TryVerifyReviewRecord(review, catalog, out var contract, out var failure));
        Assert.Null(contract);
        Assert.Equal(expected, failure);
    }

    private static void ReceiptFailure(string expected, byte[] receipt, byte[] catalog, byte[] review)
    {
        Assert.False(DetachedEvidenceVerifier.TryVerifyOwnerReceiptRecord(
            receipt, catalog, review, out var contract, out var failure));
        Assert.Null(contract);
        Assert.Equal(expected, failure);
    }

    private static void ContentFailure(string expected, byte[] admin, byte[] catalog, byte[] review, byte[] receipt)
    {
        Assert.False(DetachedEvidenceVerifier.TryVerifyAdminRecordContent(
            admin, catalog, review, receipt, out var contract, out var failure));
        Assert.Null(contract);
        Assert.Equal(expected, failure);
    }

    private static void ChainFailure(string expected, byte[] catalog, byte[] review, byte[] receipt, byte[] admin)
    {
        Assert.False(DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain(
            catalog, review, receipt, admin, out var bindings, out var failure));
        Assert.Null(bindings);
        Assert.Equal(expected, failure);
    }

    /// <summary>Recomputes Hr over edited review bytes and propagates the edited
    /// content identity through the receipt and P-content hash references so a
    /// semantic disagreement, not a stale hash, is the first refusal.</summary>
    private static (byte[] R, byte[] Wc, byte[] P) EditedReview(
        string reviewText, string receiptTemplate, string adminTemplate, string oldHr, string oldHw)
    {
        var hr = Sha(Bytes(reviewText));
        var receiptText = receiptTemplate.Replace(oldHr, hr, StringComparison.Ordinal);
        var hw = Sha(Bytes(receiptText));
        var adminText = adminTemplate.Replace(oldHr, hr, StringComparison.Ordinal)
            .Replace(oldHw, hw, StringComparison.Ordinal);
        return (Bytes(reviewText), Bytes(receiptText), Bytes(adminText));
    }

    /// <summary>Recomputes Hw over edited receipt bytes and propagates it into
    /// the P-content hash reference.</summary>
    private static (byte[] R, byte[] Wc, byte[] P) EditedReceipt(
        string receiptText, string reviewTemplate, string adminTemplate, string oldHw)
    {
        var hw = Sha(Bytes(receiptText));
        var adminText = adminTemplate.Replace(oldHw, hw, StringComparison.Ordinal);
        return (Bytes(reviewTemplate), Bytes(receiptText), Bytes(adminText));
    }

    private static (byte[] R, byte[] Wc, byte[] P) EditedContent(
        string adminText, string reviewTemplate, string receiptTemplate) =>
        (Bytes(reviewTemplate), Bytes(receiptTemplate), Bytes(adminText));

    [Fact]
    public void ReviewRefusesFieldSetViolations()
    {
        ReviewFailure("R field set", Mutate(DetachedEvidenceFixtures.R2,
            ("\"conclusion\":\"supported-for-bounded-validation\",", string.Empty)), Bytes(ValidationFixtures.K2));
        ReviewFailure("R field set", Mutate(DetachedEvidenceFixtures.R2,
                ("\"conclusion\":\"supported-for-bounded-validation\",",
                    "\"conclusion\":\"supported-for-bounded-validation\",\"extra\":1,")),
            Bytes(ValidationFixtures.K2));
    }

    [Fact]
    public void AuthorityMetadataInjectionIsRefusedAsAFieldSetViolation()
    {
        // Reviewer, owner, and administrator identity, signature, and timestamp
        // metadata is not part of any accepted frozen record. Injecting it is a
        // structural rejection, and P2 never interprets such metadata as
        // authority evidence.
        ReviewFailure("R field set", Mutate(DetachedEvidenceFixtures.R2,
                ("\"qualificationRecordSha256\":null,",
                    "\"qualificationRecordSha256\":null,\"reviewerId\":\"V1\",")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("R field set", Mutate(DetachedEvidenceFixtures.R2,
                ("\"qualificationRecordSha256\":null,",
                    "\"qualificationRecordSha256\":null,\"reviewedAt\":\"2026-11-10\",")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("R field set", Mutate(DetachedEvidenceFixtures.R2,
                ("\"schemaVersion\":1,", "\"schemaVersion\":1,\"signature\":\"" + Dead + "\",")),
            Bytes(ValidationFixtures.K2));
        ReceiptFailure("Wc field set", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"hostId\":\"" + new string('a', 64) + "\",",
                    "\"hostId\":\"" + new string('a', 64) + "\",\"ownerSignature\":\"" + Dead + "\",")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc field set", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"hostId\":\"" + new string('a', 64) + "\",",
                    "\"hostId\":\"" + new string('a', 64) + "\",\"ownerId\":\"W1\",")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ContentFailure("P field set", Mutate(DetachedEvidenceFixtures.P2,
                ("\"accepted\":true,", "\"accepted\":true,\"administratorId\":\"A1\",")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P field set", Mutate(DetachedEvidenceFixtures.P2,
                ("\"schemaVersion\":1,", "\"schemaVersion\":1,\"signature\":\"" + Dead + "\",")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
    }

    [Fact]
    public void ReviewRefusesSchemaAndIdentifierViolations()
    {
        ReviewFailure("R conclusion", Mutate(DetachedEvidenceFixtures.R2,
            ("\"conclusion\":\"supported-for-bounded-validation\"", "\"conclusion\":\"supported\"")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("schemaVersion integer", Mutate(DetachedEvidenceFixtures.R2,
            ("\"schemaVersion\":1", "\"schemaVersion\":2")), Bytes(ValidationFixtures.K2));
        ReviewFailure("schemaVersion integer", Mutate(DetachedEvidenceFixtures.R2,
            ("\"schemaVersion\":1", "\"schemaVersion\":0")), Bytes(ValidationFixtures.K2));
        ReviewFailure("schemaVersion integer", Mutate(DetachedEvidenceFixtures.R2,
            ("\"schemaVersion\":1", "\"schemaVersion\":\"1\"")), Bytes(ValidationFixtures.K2));
        ReviewFailure("R caseId", Mutate(DetachedEvidenceFixtures.R2,
            ("\"caseId\":\"case-2\"", "\"caseId\":\"case/2\"")), Bytes(ValidationFixtures.K2));
        ReviewFailure("caseId string", Mutate(DetachedEvidenceFixtures.R2,
            ("\"caseId\":\"case-2\"", "\"caseId\":null")), Bytes(ValidationFixtures.K2));
        ReviewFailure("R phase", Mutate(DetachedEvidenceFixtures.R2,
            ("\"phase\":\"pre\"", "\"phase\":\"pending\"")), Bytes(ValidationFixtures.K2));
        ReviewFailure("R topology", Mutate(DetachedEvidenceFixtures.R2,
            ("\"topology\":\"b2-lifeline\"", "\"topology\":\"b4\"")), Bytes(ValidationFixtures.K2));
        ReviewFailure("hostId hex", Mutate(DetachedEvidenceFixtures.R2,
                ("\"hostId\":\"" + new string('a', 64) + "\"",
                    "\"hostId\":\"" + new string('a', 63) + "\"")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("hostId hex", Mutate(DetachedEvidenceFixtures.R2,
            ("\"hostId\":\"" + new string('a', 64) + "\"", "\"hostId\":null")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("actionScriptSha256 hex", Mutate(DetachedEvidenceFixtures.R2,
                ("\"actionScriptSha256\":\"" + ValidationFixtures.Hs2 + "\"",
                    "\"actionScriptSha256\":\"" + ValidationFixtures.Hs2.ToUpperInvariant() + "\"")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("R qualificationId", Mutate(DetachedEvidenceFixtures.R3,
            ("\"qualificationId\":\"Q1\"", "\"qualificationId\":\"Q/1\"")), Bytes(ValidationFixtures.K3));
    }

    [Fact]
    public void ReviewRefusesQualificationAndBindingViolations()
    {
        ReviewFailure("R qualificationId disagreement", Mutate(DetachedEvidenceFixtures.R2,
            ("\"qualificationId\":null", "\"qualificationId\":\"Q1\"")), Bytes(ValidationFixtures.K2));
        ReviewFailure("R qualificationRecordSha256 disagreement", Mutate(DetachedEvidenceFixtures.R3,
                ("\"qualificationRecordSha256\":\"" + new string('f', 64) + "\"",
                    "\"qualificationRecordSha256\":null")),
            Bytes(ValidationFixtures.K3));
        ReviewFailure("R catalogSha256 does not bind K", Mutate(DetachedEvidenceFixtures.R2,
                ("\"catalogSha256\":\"" + ValidationFixtures.Hk2 + "\"", "\"catalogSha256\":\"" + Dead + "\"")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("R catalogSha256 does not bind K",
            Bytes(DetachedEvidenceFixtures.R2), Bytes(ValidationFixtures.K3));
    }

    [Fact]
    public void ReviewRefusesByteLevelViolations()
    {
        ReviewFailure("noncanonical CJSON-1 representation",
            Bytes(DetachedEvidenceFixtures.R2 + " "), Bytes(ValidationFixtures.K2));
        ReviewFailure("invalid CJSON-1",
            Bytes(DetachedEvidenceFixtures.R2 + "{}"), Bytes(ValidationFixtures.K2));
        var truncated = Bytes(DetachedEvidenceFixtures.R2);
        ReviewFailure("invalid CJSON-1", truncated[..^1], Bytes(ValidationFixtures.K2));
        ReviewFailure("CJSON-1 size",
            Bytes(DetachedEvidenceFixtures.R2 + new string('x', CanonicalJson.MaximumBytes)),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("CJSON-1 size", [], Bytes(ValidationFixtures.K2));
        var nonAscii = Bytes(DetachedEvidenceFixtures.R2);
        nonAscii[5] = 0xE9;
        ReviewFailure("CJSON-1 ASCII printable bytes", nonAscii, Bytes(ValidationFixtures.K2));
        ReviewFailure("CJSON-1 keys are not strictly ascending", Mutate(DetachedEvidenceFixtures.R2,
                ("\"caseId\":\"case-2\",", "\"caseId\":\"case-2\",\"caseId\":\"case-2\",")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("CJSON-1 keys are not strictly ascending", Mutate(DetachedEvidenceFixtures.R2,
                ("\"caseId\":\"case-2\",\"catalogSha256\":\"" + ValidationFixtures.Hk2 + "\",",
                    "\"catalogSha256\":\"" + ValidationFixtures.Hk2 + "\",\"caseId\":\"case-2\",")),
            Bytes(ValidationFixtures.K2));
        ReviewFailure("noncanonical CJSON-1 representation", Mutate(DetachedEvidenceFixtures.R2,
            ("\"caseId\":\"case-2\"", "\"caseId\":\"case\\u002d2\"")), Bytes(ValidationFixtures.K2));
        ReviewFailure("invalid CJSON-1", Mutate(DetachedEvidenceFixtures.R2,
            ("\"caseId\":\"case-2\"", "\"caseId\":\"case\\q2\"")), Bytes(ValidationFixtures.K2));
        ReviewFailure("CJSON-1 unsigned canonical integer", Mutate(DetachedEvidenceFixtures.R2,
            ("\"schemaVersion\":1", "\"schemaVersion\":1.0")), Bytes(ValidationFixtures.K2));
        ReviewFailure("CJSON-1 string", Mutate(DetachedEvidenceFixtures.R2,
            ("\"caseId\":\"case-2\"", "\"caseId\":\"\"")), Bytes(ValidationFixtures.K2));
    }

    [Fact]
    public void ReceiptRefusesFieldSetAndSchemaViolations()
    {
        ReceiptFailure("Wc field set", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"decision\":\"accept-exact-bounded-validation-catalog\",", string.Empty)),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc field set", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"decision\":\"accept-exact-bounded-validation-catalog\",",
                    "\"decision\":\"accept-exact-bounded-validation-catalog\",\"extra\":1,")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc decision", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"decision\":\"accept-exact-bounded-validation-catalog\"",
                    "\"decision\":\"accepted-exact-bounded-validation-catalog\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("schemaVersion integer", Mutate(DetachedEvidenceFixtures.Wc2,
            ("\"schemaVersion\":1", "\"schemaVersion\":2")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc caseId", Mutate(DetachedEvidenceFixtures.Wc2,
            ("\"caseId\":\"case-2\"", "\"caseId\":\"case/2\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc phase", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"phase\":\"pre\"", "\"phase\":\"pending\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc topology", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"topology\":\"b2-lifeline\"", "\"topology\":\"b4\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("hostId hex", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"hostId\":\"" + new string('a', 64) + "\"", "\"hostId\":null")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("artifactSetSha256 hex", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"artifactSetSha256\":\"" + new string('b', 64) + "\"",
                    "\"artifactSetSha256\":\"" + new string('b', 63) + "\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc qualificationId disagreement", Mutate(DetachedEvidenceFixtures.Wc3,
                ("\"qualificationId\":\"Q1\"", "\"qualificationId\":\"Q2\"")),
            Bytes(ValidationFixtures.K3), Bytes(DetachedEvidenceFixtures.R3));
        ReceiptFailure("Wc qualificationRecordSha256 disagreement", Mutate(DetachedEvidenceFixtures.Wc3,
                ("\"qualificationRecordSha256\":\"" + new string('f', 64) + "\"",
                    "\"qualificationRecordSha256\":\"" + new string('e', 64) + "\"")),
            Bytes(ValidationFixtures.K3), Bytes(DetachedEvidenceFixtures.R3));
    }

    [Fact]
    public void ReceiptRefusesBindingAndSubstitutionViolations()
    {
        ReceiptFailure("Wc catalogReviewSha256 does not bind R", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"catalogReviewSha256\":\"" + DetachedEvidenceFixtures.Hr2 + "\"",
                    "\"catalogReviewSha256\":\"" + Dead + "\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc catalogSha256 does not bind K", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"catalogSha256\":\"" + ValidationFixtures.Hk2 + "\"", "\"catalogSha256\":\"" + Dead + "\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("Wc catalogSha256 does not bind K",
            Bytes(DetachedEvidenceFixtures.Wc2), Bytes(ValidationFixtures.K3), Bytes(DetachedEvidenceFixtures.R3));
        // A substituted review is refused by the vanguard review re-validation,
        // which names the review's own K binding before any receipt rule.
        ReceiptFailure("R catalogSha256 does not bind K",
            Bytes(DetachedEvidenceFixtures.Wc3), Bytes(ValidationFixtures.K3), Bytes(DetachedEvidenceFixtures.R2));
    }

    [Fact]
    public void ReceiptRefusesByteLevelViolations()
    {
        ReceiptFailure("noncanonical CJSON-1 representation", Bytes(DetachedEvidenceFixtures.Wc2 + " "),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        var truncated = Bytes(DetachedEvidenceFixtures.Wc2);
        ReceiptFailure("invalid CJSON-1", truncated[..^1],
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
        ReceiptFailure("CJSON-1 keys are not strictly ascending", Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"caseId\":\"case-2\",", "\"caseId\":\"case-2\",\"caseId\":\"case-2\",")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2));
    }

    [Fact]
    public void ContentRefusesFieldSetAndSchemaViolations()
    {
        ContentFailure("P field set", Mutate(DetachedEvidenceFixtures.P2,
                ("\"accepted\":true,", string.Empty)),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P field set", Mutate(DetachedEvidenceFixtures.P2,
                ("\"accepted\":true,", "\"accepted\":true,\"approval\":true,")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P accepted is not true", Mutate(DetachedEvidenceFixtures.P2,
                ("\"accepted\":true", "\"accepted\":false")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("accepted boolean", Mutate(DetachedEvidenceFixtures.P2,
                ("\"accepted\":true", "\"accepted\":\"true\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("accepted boolean", Mutate(DetachedEvidenceFixtures.P2,
                ("\"accepted\":true", "\"accepted\":1")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("schemaVersion integer", Mutate(DetachedEvidenceFixtures.P2,
                ("\"schemaVersion\":1", "\"schemaVersion\":2")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P caseId", Mutate(DetachedEvidenceFixtures.P2,
                ("\"caseId\":\"case-2\"", "\"caseId\":\"case/2\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P phase", Mutate(DetachedEvidenceFixtures.P2,
                ("\"phase\":\"pre\"", "\"phase\":\"pending\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P topology", Mutate(DetachedEvidenceFixtures.P2,
                ("\"topology\":\"b2-lifeline\"", "\"topology\":\"b4\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("ownerCatalogAcceptanceSha256 hex", Mutate(DetachedEvidenceFixtures.P2,
                ("\"ownerCatalogAcceptanceSha256\":\"" + DetachedEvidenceFixtures.Hw2 + "\"",
                    "\"ownerCatalogAcceptanceSha256\":\"" + DetachedEvidenceFixtures.Hw2.ToUpperInvariant() + "\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("ownerCatalogAcceptanceSha256 hex", Mutate(DetachedEvidenceFixtures.P2,
                ("\"ownerCatalogAcceptanceSha256\":\"" + DetachedEvidenceFixtures.Hw2 + "\"",
                    "\"ownerCatalogAcceptanceSha256\":\"" + new string('a', 63) + "\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P qualificationId disagreement", Mutate(DetachedEvidenceFixtures.P3,
                ("\"qualificationId\":\"Q1\"", "\"qualificationId\":\"Q2\"")),
            Bytes(ValidationFixtures.K3), Bytes(DetachedEvidenceFixtures.R3), Bytes(DetachedEvidenceFixtures.Wc3));
        ContentFailure("P qualificationRecordSha256 disagreement", Mutate(DetachedEvidenceFixtures.P3,
                ("\"qualificationRecordSha256\":\"" + new string('f', 64) + "\"",
                    "\"qualificationRecordSha256\":\"" + new string('e', 64) + "\"")),
            Bytes(ValidationFixtures.K3), Bytes(DetachedEvidenceFixtures.R3), Bytes(DetachedEvidenceFixtures.Wc3));
    }

    [Fact]
    public void ContentRefusesBindingAndSubstitutionViolations()
    {
        ContentFailure("P catalogSha256 does not bind K", Mutate(DetachedEvidenceFixtures.P2,
                ("\"catalogSha256\":\"" + ValidationFixtures.Hk2 + "\"", "\"catalogSha256\":\"" + Dead + "\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P catalogReviewSha256 does not bind R", Mutate(DetachedEvidenceFixtures.P2,
                ("\"catalogReviewSha256\":\"" + DetachedEvidenceFixtures.Hr2 + "\"",
                    "\"catalogReviewSha256\":\"" + Dead + "\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P ownerCatalogAcceptanceSha256 does not bind Wc", Mutate(DetachedEvidenceFixtures.P2,
                ("\"ownerCatalogAcceptanceSha256\":\"" + DetachedEvidenceFixtures.Hw2 + "\"",
                    "\"ownerCatalogAcceptanceSha256\":\"" + Dead + "\"")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("P catalogSha256 does not bind K",
            Bytes(DetachedEvidenceFixtures.P2), Bytes(ValidationFixtures.K3),
            Bytes(DetachedEvidenceFixtures.R3), Bytes(DetachedEvidenceFixtures.Wc3));
    }

    [Fact]
    public void ContentRefusesByteLevelViolations()
    {
        ContentFailure("noncanonical CJSON-1 representation", Bytes(DetachedEvidenceFixtures.P2 + " "),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        var truncated = Bytes(DetachedEvidenceFixtures.P2);
        ContentFailure("invalid CJSON-1", truncated[..^1],
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
        ContentFailure("CJSON-1 keys are not strictly ascending", Mutate(DetachedEvidenceFixtures.P2,
                ("\"accepted\":true,", "\"accepted\":true,\"accepted\":true,")),
            Bytes(ValidationFixtures.K2), Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2));
    }

    [Fact]
    public void ChainRefusesBindingAndSubstitutionViolations()
    {
        ChainFailure("R catalogSha256 does not bind K", Bytes(ValidationFixtures.K2),
            Mutate(DetachedEvidenceFixtures.R2,
                ("\"catalogSha256\":\"" + ValidationFixtures.Hk2 + "\"", "\"catalogSha256\":\"" + Dead + "\"")),
            Bytes(DetachedEvidenceFixtures.Wc2), Bytes(DetachedEvidenceFixtures.P2));
        ChainFailure("Wc catalogSha256 does not bind K", Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2),
            Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"catalogSha256\":\"" + ValidationFixtures.Hk2 + "\"", "\"catalogSha256\":\"" + Dead + "\"")),
            Bytes(DetachedEvidenceFixtures.P2));
        ChainFailure("Wc catalogReviewSha256 does not bind R", Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2),
            Mutate(DetachedEvidenceFixtures.Wc2,
                ("\"catalogReviewSha256\":\"" + DetachedEvidenceFixtures.Hr2 + "\"",
                    "\"catalogReviewSha256\":\"" + Dead + "\"")),
            Bytes(DetachedEvidenceFixtures.P2));
        ChainFailure("P catalogSha256 does not bind K", Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2),
            Mutate(DetachedEvidenceFixtures.P2,
                ("\"catalogSha256\":\"" + ValidationFixtures.Hk2 + "\"", "\"catalogSha256\":\"" + Dead + "\"")));
        ChainFailure("P catalogReviewSha256 does not bind R", Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2),
            Mutate(DetachedEvidenceFixtures.P2,
                ("\"catalogReviewSha256\":\"" + DetachedEvidenceFixtures.Hr2 + "\"",
                    "\"catalogReviewSha256\":\"" + Dead + "\"")));
        ChainFailure("P ownerCatalogAcceptanceSha256 does not bind Wc", Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2),
            Mutate(DetachedEvidenceFixtures.P2,
                ("\"ownerCatalogAcceptanceSha256\":\"" + DetachedEvidenceFixtures.Hw2 + "\"",
                    "\"ownerCatalogAcceptanceSha256\":\"" + Dead + "\"")));
        // Substituted K, R, Wc, and P bytes across otherwise valid chains.
        ChainFailure("R catalogSha256 does not bind K", Bytes(ValidationFixtures.K3),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2), Bytes(DetachedEvidenceFixtures.P2));
        ChainFailure("R catalogSha256 does not bind K", Bytes(ValidationFixtures.K3),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc3), Bytes(DetachedEvidenceFixtures.P3));
        ChainFailure("Wc catalogSha256 does not bind K", Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc3), Bytes(DetachedEvidenceFixtures.P2));
        ChainFailure("P catalogSha256 does not bind K", Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2), Bytes(DetachedEvidenceFixtures.P3));
        // A substituted P does not carry the external expected content identity.
        Assert.NotEqual(DetachedEvidenceFixtures.Hp2, Sha(Bytes(DetachedEvidenceFixtures.P3)));
        Assert.NotEqual(DetachedEvidenceFixtures.P2, DetachedEvidenceFixtures.P3);
    }

    [Fact]
    public void ChainRefusesHashConsistentReviewDisagreements()
    {
        // Every affected hash is recomputed correctly; only the reviewed
        // relationship to K is violated.
        var caseId = EditedReview(Edit(DetachedEvidenceFixtures.R2,
                ("\"caseId\":\"case-2\"", "\"caseId\":\"case-9\"")),
            DetachedEvidenceFixtures.Wc2, DetachedEvidenceFixtures.P2,
            DetachedEvidenceFixtures.Hr2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("R caseId disagreement", Bytes(ValidationFixtures.K2), caseId.R, caseId.Wc, caseId.P);
        var phase = EditedReview(Edit(DetachedEvidenceFixtures.R2,
                ("\"phase\":\"pre\"", "\"phase\":\"post\"")),
            DetachedEvidenceFixtures.Wc2, DetachedEvidenceFixtures.P2,
            DetachedEvidenceFixtures.Hr2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("R phase disagreement", Bytes(ValidationFixtures.K2), phase.R, phase.Wc, phase.P);
        var topology = EditedReview(Edit(DetachedEvidenceFixtures.R2,
                ("\"topology\":\"b2-lifeline\"", "\"topology\":\"b3-integrated\"")),
            DetachedEvidenceFixtures.Wc2, DetachedEvidenceFixtures.P2,
            DetachedEvidenceFixtures.Hr2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("R topology disagreement", Bytes(ValidationFixtures.K2), topology.R, topology.Wc, topology.P);
        var host = EditedReview(Edit(DetachedEvidenceFixtures.R2,
                ("\"hostId\":\"" + new string('a', 64) + "\"", "\"hostId\":\"" + new string('e', 64) + "\"")),
            DetachedEvidenceFixtures.Wc2, DetachedEvidenceFixtures.P2,
            DetachedEvidenceFixtures.Hr2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("R hostId disagreement", Bytes(ValidationFixtures.K2), host.R, host.Wc, host.P);
        var artifact = EditedReview(Edit(DetachedEvidenceFixtures.R2,
                ("\"artifactSetSha256\":\"" + new string('b', 64) + "\"",
                    "\"artifactSetSha256\":\"" + new string('e', 64) + "\"")),
            DetachedEvidenceFixtures.Wc2, DetachedEvidenceFixtures.P2,
            DetachedEvidenceFixtures.Hr2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("R artifactSetSha256 disagreement", Bytes(ValidationFixtures.K2),
            artifact.R, artifact.Wc, artifact.P);
        var manifest = EditedReview(Edit(DetachedEvidenceFixtures.R2,
                ("\"fixtureManifestSha256\":\"" + ValidationFixtures.Hm2 + "\"",
                    "\"fixtureManifestSha256\":\"" + new string('c', 64) + "\"")),
            DetachedEvidenceFixtures.Wc2, DetachedEvidenceFixtures.P2,
            DetachedEvidenceFixtures.Hr2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("R fixtureManifestSha256 disagreement", Bytes(ValidationFixtures.K2),
            manifest.R, manifest.Wc, manifest.P);
        var policy = EditedReview(Edit(DetachedEvidenceFixtures.R2,
                ("\"fixturePolicySha256\":\"" + ValidationFixtures.Hf2 + "\"",
                    "\"fixturePolicySha256\":\"" + new string('c', 64) + "\"")),
            DetachedEvidenceFixtures.Wc2, DetachedEvidenceFixtures.P2,
            DetachedEvidenceFixtures.Hr2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("R fixturePolicySha256 disagreement", Bytes(ValidationFixtures.K2),
            policy.R, policy.Wc, policy.P);
        var action = EditedReview(Edit(DetachedEvidenceFixtures.R2,
                ("\"actionScriptSha256\":\"" + ValidationFixtures.Hs2 + "\"",
                    "\"actionScriptSha256\":\"" + new string('c', 64) + "\"")),
            DetachedEvidenceFixtures.Wc2, DetachedEvidenceFixtures.P2,
            DetachedEvidenceFixtures.Hr2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("R actionScriptSha256 disagreement", Bytes(ValidationFixtures.K2),
            action.R, action.Wc, action.P);
        var qualification = EditedReview(Edit(DetachedEvidenceFixtures.R3,
                ("\"qualificationId\":\"Q1\"", "\"qualificationId\":\"Q2\"")),
            DetachedEvidenceFixtures.Wc3, DetachedEvidenceFixtures.P3,
            DetachedEvidenceFixtures.Hr3, DetachedEvidenceFixtures.Hw3);
        ChainFailure("R qualificationId disagreement", Bytes(ValidationFixtures.K3),
            qualification.R, qualification.Wc, qualification.P);
        var record = EditedReview(Edit(DetachedEvidenceFixtures.R3,
                ("\"qualificationRecordSha256\":\"" + new string('f', 64) + "\"",
                    "\"qualificationRecordSha256\":\"" + new string('e', 64) + "\"")),
            DetachedEvidenceFixtures.Wc3, DetachedEvidenceFixtures.P3,
            DetachedEvidenceFixtures.Hr3, DetachedEvidenceFixtures.Hw3);
        ChainFailure("R qualificationRecordSha256 disagreement", Bytes(ValidationFixtures.K3),
            record.R, record.Wc, record.P);
    }

    [Fact]
    public void ChainRefusesHashConsistentReceiptDisagreements()
    {
        var caseId = EditedReceipt(Edit(DetachedEvidenceFixtures.Wc2,
                ("\"caseId\":\"case-2\"", "\"caseId\":\"case-9\"")),
            DetachedEvidenceFixtures.R2, DetachedEvidenceFixtures.P2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("Wc caseId disagreement", Bytes(ValidationFixtures.K2), caseId.R, caseId.Wc, caseId.P);
        var host = EditedReceipt(Edit(DetachedEvidenceFixtures.Wc2,
                ("\"hostId\":\"" + new string('a', 64) + "\"", "\"hostId\":\"" + new string('e', 64) + "\"")),
            DetachedEvidenceFixtures.R2, DetachedEvidenceFixtures.P2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("Wc hostId disagreement", Bytes(ValidationFixtures.K2), host.R, host.Wc, host.P);
        var phase = EditedReceipt(Edit(DetachedEvidenceFixtures.Wc2,
                ("\"phase\":\"pre\"", "\"phase\":\"post\"")),
            DetachedEvidenceFixtures.R2, DetachedEvidenceFixtures.P2, DetachedEvidenceFixtures.Hw2);
        ChainFailure("Wc phase disagreement", Bytes(ValidationFixtures.K2), phase.R, phase.Wc, phase.P);
        var record = EditedReceipt(Edit(DetachedEvidenceFixtures.Wc3,
                ("\"qualificationRecordSha256\":\"" + new string('f', 64) + "\"",
                    "\"qualificationRecordSha256\":\"" + new string('e', 64) + "\"")),
            DetachedEvidenceFixtures.R3, DetachedEvidenceFixtures.P3, DetachedEvidenceFixtures.Hw3);
        ChainFailure("Wc qualificationRecordSha256 disagreement", Bytes(ValidationFixtures.K3),
            record.R, record.Wc, record.P);
    }

    [Fact]
    public void ChainRefusesHashConsistentContentDisagreements()
    {
        var caseId = EditedContent(Edit(DetachedEvidenceFixtures.P2,
                ("\"caseId\":\"case-2\"", "\"caseId\":\"case-9\"")),
            DetachedEvidenceFixtures.R2, DetachedEvidenceFixtures.Wc2);
        ChainFailure("P caseId disagreement", Bytes(ValidationFixtures.K2), caseId.R, caseId.Wc, caseId.P);
        var phase = EditedContent(Edit(DetachedEvidenceFixtures.P2,
                ("\"phase\":\"pre\"", "\"phase\":\"post\"")),
            DetachedEvidenceFixtures.R2, DetachedEvidenceFixtures.Wc2);
        ChainFailure("P phase disagreement", Bytes(ValidationFixtures.K2), phase.R, phase.Wc, phase.P);
        var artifact = EditedContent(Edit(DetachedEvidenceFixtures.P2,
                ("\"artifactSetSha256\":\"" + new string('b', 64) + "\"",
                    "\"artifactSetSha256\":\"" + new string('e', 64) + "\"")),
            DetachedEvidenceFixtures.R2, DetachedEvidenceFixtures.Wc2);
        ChainFailure("P artifactSetSha256 disagreement", Bytes(ValidationFixtures.K2),
            artifact.R, artifact.Wc, artifact.P);
        var qualification = EditedContent(Edit(DetachedEvidenceFixtures.P3,
                ("\"qualificationId\":\"Q1\"", "\"qualificationId\":\"Q2\"")),
            DetachedEvidenceFixtures.R3, DetachedEvidenceFixtures.Wc3);
        ChainFailure("P qualificationId disagreement", Bytes(ValidationFixtures.K3),
            qualification.R, qualification.Wc, qualification.P);
    }

    [Fact]
    public void ValidUntrustedContentRecordIsAcceptedStructurallyWithoutAuthority()
    {
        // A structurally valid, untrusted P-shaped record: it satisfies the frozen
        // P-content contract, and that structural success is required here. It
        // establishes no administrator authority, protected acquisition, custody,
        // catalog approval or eligibility, or invocation permission.
        Assert.True(DetachedEvidenceVerifier.TryVerifyAdminRecordContent(
            Bytes(DetachedEvidenceFixtures.P2), Bytes(ValidationFixtures.K2),
            Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2),
            out var content, out var failure));
        Assert.Equal(string.Empty, failure);
        Assert.Equal("case-2", content!.CaseId);
        Assert.Equal(DetachedEvidenceFixtures.Hw2, content.OwnerCatalogAcceptanceSha256);
    }

    [Fact]
    public void RefusalMessagesAreDeterministicAndInputsAreNotMutated()
    {
        var admin = Mutate(DetachedEvidenceFixtures.P2,
            ("\"ownerCatalogAcceptanceSha256\":\"" + DetachedEvidenceFixtures.Hw2 + "\"",
                "\"ownerCatalogAcceptanceSha256\":\"" + Dead + "\""));
        var adminCopy = (byte[])admin.Clone();
        var catalog = Bytes(ValidationFixtures.K2);
        var catalogCopy = (byte[])catalog.Clone();
        for (var attempt = 0; attempt < 3; attempt++)
            ChainFailure("P ownerCatalogAcceptanceSha256 does not bind Wc", catalog,
                Bytes(DetachedEvidenceFixtures.R2), Bytes(DetachedEvidenceFixtures.Wc2), admin);
        Assert.Equal(adminCopy, admin);
        Assert.Equal(catalogCopy, catalog);
    }
}
