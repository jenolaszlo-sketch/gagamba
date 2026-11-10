using System.Text.Json;

namespace Gagamba.Execution.Linux.M1.Validation;

/// <summary>Deterministic structural and content-binding verification for supplied
/// detached review (R), owner receipt (Wc), and protected-approval content (P)
/// bytes relative to a supplied catalog (K). Reuses the accepted P1 contract core
/// read-only. Results are content-consistency values only: successful verification
/// does not establish reviewer, owner, or administrator authority, protected
/// acquisition or custody, catalog approval or eligibility, invocation
/// authorization, or protected execution, and no result can be promoted into any
/// authority type.</summary>
internal static class DetachedEvidenceVerifier
{
    private const string ReviewConclusion = "supported-for-bounded-validation";
    private const string ReceiptDecision = "accept-exact-bounded-validation-catalog";

    private static readonly string[] Phases = ["pre", "post"];
    private static readonly string[] Topologies = ["b2-lifeline", "b3-integrated"];

    private static readonly string[] ReviewFields =
    [
        "actionScriptSha256", "artifactSetSha256", "caseId", "catalogSha256", "conclusion",
        "fixtureManifestSha256", "fixturePolicySha256", "hostId", "phase", "qualificationId",
        "qualificationRecordSha256", "schemaVersion", "topology",
    ];

    private static readonly string[] ReceiptFields =
    [
        "artifactSetSha256", "caseId", "catalogReviewSha256", "catalogSha256", "decision",
        "hostId", "phase", "qualificationId", "qualificationRecordSha256", "schemaVersion", "topology",
    ];

    private static readonly string[] AdminContentFields =
    [
        "accepted", "artifactSetSha256", "caseId", "catalogReviewSha256", "catalogSha256",
        "hostId", "ownerCatalogAcceptanceSha256", "phase", "qualificationId",
        "qualificationRecordSha256", "schemaVersion", "topology",
    ];

    internal static bool TryVerifyReviewRecord(
        byte[] reviewBytes,
        byte[] catalogBytes,
        out ReviewRecordContract? review,
        out string failure)
    {
        try
        {
            review = VerifyReviewRecord(reviewBytes, catalogBytes);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            review = null;
            failure = exception.Message;
            return false;
        }
    }

    internal static bool TryVerifyOwnerReceiptRecord(
        byte[] receiptBytes,
        byte[] catalogBytes,
        byte[] reviewBytes,
        out OwnerReceiptRecordContract? receipt,
        out string failure)
    {
        try
        {
            receipt = VerifyOwnerReceiptRecord(receiptBytes, catalogBytes, reviewBytes);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            receipt = null;
            failure = exception.Message;
            return false;
        }
    }

    internal static bool TryVerifyAdminRecordContent(
        byte[] adminBytes,
        byte[] catalogBytes,
        byte[] reviewBytes,
        byte[] receiptBytes,
        out AdminRecordContentContract? adminContent,
        out string failure)
    {
        try
        {
            adminContent = VerifyAdminRecordContent(adminBytes, catalogBytes, reviewBytes, receiptBytes);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            adminContent = null;
            failure = exception.Message;
            return false;
        }
    }

    internal static bool TryVerifyDetachedEvidenceChain(
        byte[] catalogBytes,
        byte[] reviewBytes,
        byte[] receiptBytes,
        byte[] adminBytes,
        out DetachedEvidenceBindingsContract? bindings,
        out string failure)
    {
        try
        {
            var catalog = VerifyCatalog(catalogBytes, out var hk);
            var review = VerifyReviewRecord(reviewBytes, catalogBytes);
            var receipt = VerifyOwnerReceiptRecord(receiptBytes, catalogBytes, reviewBytes);
            var adminContent = VerifyAdminRecordContent(adminBytes, catalogBytes, reviewBytes, receiptBytes);
            var hr = CanonicalJson.Hash(reviewBytes);
            var hw = CanonicalJson.Hash(receiptBytes);
            var hp = CanonicalJson.Hash(adminBytes);

            bindings = new DetachedEvidenceBindingsContract(catalog, review, receipt, adminContent, hk, hr, hw, hp);
            failure = string.Empty;
            return true;
        }
        catch (InvalidDataException exception)
        {
            bindings = null;
            failure = exception.Message;
            return false;
        }
    }

    private static ReviewRecordContract VerifyReviewRecord(byte[] reviewBytes, byte[] catalogBytes)
    {
        var catalog = VerifyCatalog(catalogBytes, out var hk);
        var root = CanonicalJson.Parse(reviewBytes);
        RequireFields(root, "R", ReviewFields);
        Long(root, "schemaVersion", 1, 1);
        var actionScriptSha256 = Hex(root, "actionScriptSha256");
        var artifactSetSha256 = Hex(root, "artifactSetSha256");
        var caseId = ValidId(root, "caseId", "R caseId");
        var catalogSha256 = Hex(root, "catalogSha256");
        var conclusion = Exact(root, "conclusion", ReviewConclusion, "R conclusion");
        var fixtureManifestSha256 = Hex(root, "fixtureManifestSha256");
        var fixturePolicySha256 = Hex(root, "fixturePolicySha256");
        var hostId = Hex(root, "hostId");
        var phase = Choice(root, "phase", Phases, "R phase");
        var qualificationId = NullableValidId(root, "qualificationId", "R qualificationId");
        var qualificationRecordSha256 = NullableHex(root, "qualificationRecordSha256",
            "R qualificationRecordSha256");
        var topology = Choice(root, "topology", Topologies, "R topology");

        Require(catalogSha256 == hk, "R catalogSha256 does not bind K");
        Require(caseId == catalog.CaseId, "R caseId disagreement");
        Require(phase == catalog.Phase, "R phase disagreement");
        Require(topology == catalog.Topology, "R topology disagreement");
        Require(hostId == catalog.HostId, "R hostId disagreement");
        Require(artifactSetSha256 == catalog.ArtifactSetSha256, "R artifactSetSha256 disagreement");
        Require(qualificationId == catalog.QualificationId, "R qualificationId disagreement");
        Require(qualificationRecordSha256 == catalog.QualificationRecordSha256,
            "R qualificationRecordSha256 disagreement");
        Require(fixtureManifestSha256 == catalog.FixtureManifestSha256,
            "R fixtureManifestSha256 disagreement");
        Require(fixturePolicySha256 == catalog.FixturePolicySha256, "R fixturePolicySha256 disagreement");
        Require(actionScriptSha256 == catalog.ActionScriptSha256, "R actionScriptSha256 disagreement");

        return new ReviewRecordContract(1, actionScriptSha256, artifactSetSha256, caseId, catalogSha256,
            conclusion, fixtureManifestSha256, fixturePolicySha256, hostId, phase, qualificationId,
            qualificationRecordSha256, topology);
    }

    private static OwnerReceiptRecordContract VerifyOwnerReceiptRecord(
        byte[] receiptBytes, byte[] catalogBytes, byte[] reviewBytes)
    {
        var catalog = VerifyCatalog(catalogBytes, out var hk);
        _ = VerifyReviewRecord(reviewBytes, catalogBytes);
        var hr = CanonicalJson.Hash(reviewBytes);
        var root = CanonicalJson.Parse(receiptBytes);
        RequireFields(root, "Wc", ReceiptFields);
        Long(root, "schemaVersion", 1, 1);
        var artifactSetSha256 = Hex(root, "artifactSetSha256");
        var caseId = ValidId(root, "caseId", "Wc caseId");
        var catalogReviewSha256 = Hex(root, "catalogReviewSha256");
        var catalogSha256 = Hex(root, "catalogSha256");
        var decision = Exact(root, "decision", ReceiptDecision, "Wc decision");
        var hostId = Hex(root, "hostId");
        var phase = Choice(root, "phase", Phases, "Wc phase");
        var qualificationId = NullableValidId(root, "qualificationId", "Wc qualificationId");
        var qualificationRecordSha256 = NullableHex(root, "qualificationRecordSha256",
            "Wc qualificationRecordSha256");
        var topology = Choice(root, "topology", Topologies, "Wc topology");

        Require(catalogSha256 == hk, "Wc catalogSha256 does not bind K");
        Require(catalogReviewSha256 == hr, "Wc catalogReviewSha256 does not bind R");
        Require(caseId == catalog.CaseId, "Wc caseId disagreement");
        Require(phase == catalog.Phase, "Wc phase disagreement");
        Require(topology == catalog.Topology, "Wc topology disagreement");
        Require(hostId == catalog.HostId, "Wc hostId disagreement");
        Require(artifactSetSha256 == catalog.ArtifactSetSha256, "Wc artifactSetSha256 disagreement");
        Require(qualificationId == catalog.QualificationId, "Wc qualificationId disagreement");
        Require(qualificationRecordSha256 == catalog.QualificationRecordSha256,
            "Wc qualificationRecordSha256 disagreement");

        return new OwnerReceiptRecordContract(1, artifactSetSha256, caseId, catalogReviewSha256,
            catalogSha256, decision, hostId, phase, qualificationId, qualificationRecordSha256, topology);
    }

    private static AdminRecordContentContract VerifyAdminRecordContent(
        byte[] adminBytes, byte[] catalogBytes, byte[] reviewBytes, byte[] receiptBytes)
    {
        var catalog = VerifyCatalog(catalogBytes, out var hk);
        _ = VerifyReviewRecord(reviewBytes, catalogBytes);
        _ = VerifyOwnerReceiptRecord(receiptBytes, catalogBytes, reviewBytes);
        var hr = CanonicalJson.Hash(reviewBytes);
        var hw = CanonicalJson.Hash(receiptBytes);
        var root = CanonicalJson.Parse(adminBytes);
        RequireFields(root, "P", AdminContentFields);
        Long(root, "schemaVersion", 1, 1);
        Require(Boolean(root, "accepted"), "P accepted is not true");
        var artifactSetSha256 = Hex(root, "artifactSetSha256");
        var caseId = ValidId(root, "caseId", "P caseId");
        var catalogReviewSha256 = Hex(root, "catalogReviewSha256");
        var catalogSha256 = Hex(root, "catalogSha256");
        var hostId = Hex(root, "hostId");
        var ownerCatalogAcceptanceSha256 = Hex(root, "ownerCatalogAcceptanceSha256");
        var phase = Choice(root, "phase", Phases, "P phase");
        var qualificationId = NullableValidId(root, "qualificationId", "P qualificationId");
        var qualificationRecordSha256 = NullableHex(root, "qualificationRecordSha256",
            "P qualificationRecordSha256");
        var topology = Choice(root, "topology", Topologies, "P topology");

        Require(catalogSha256 == hk, "P catalogSha256 does not bind K");
        Require(catalogReviewSha256 == hr, "P catalogReviewSha256 does not bind R");
        Require(ownerCatalogAcceptanceSha256 == hw, "P ownerCatalogAcceptanceSha256 does not bind Wc");
        Require(caseId == catalog.CaseId, "P caseId disagreement");
        Require(phase == catalog.Phase, "P phase disagreement");
        Require(topology == catalog.Topology, "P topology disagreement");
        Require(hostId == catalog.HostId, "P hostId disagreement");
        Require(artifactSetSha256 == catalog.ArtifactSetSha256, "P artifactSetSha256 disagreement");
        Require(qualificationId == catalog.QualificationId, "P qualificationId disagreement");
        Require(qualificationRecordSha256 == catalog.QualificationRecordSha256,
            "P qualificationRecordSha256 disagreement");

        return new AdminRecordContentContract(1, artifactSetSha256, caseId, catalogReviewSha256,
            catalogSha256, hostId, ownerCatalogAcceptanceSha256, phase, qualificationId,
            qualificationRecordSha256, topology);
    }

    private static CatalogContract VerifyCatalog(byte[] catalogBytes, out string hk)
    {
        if (!FrozenContractVerifier.TryVerifyCatalog(catalogBytes, out var catalog, out var failure))
            throw new InvalidDataException(failure);
        hk = CanonicalJson.Hash(catalogBytes);
        return catalog!;
    }

    private static void RequireFields(JsonElement element, string what, string[] expected)
    {
        Require(element.EnumerateObject().Select(property => property.Name).SequenceEqual(expected),
            $"{what} field set");
    }

    private static string Text(JsonElement parent, string key)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String, $"{key} string");
        return value.GetString()!;
    }

    private static string Exact(JsonElement parent, string key, string expected, string what)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String && value.GetString() == expected, what);
        return expected;
    }

    private static string Choice(JsonElement parent, string key, string[] allowed, string what)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String && allowed.Contains(value.GetString()!), what);
        return value.GetString()!;
    }

    private static string Hex(JsonElement parent, string key)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.String, $"{key} hex");
        var text = value.GetString()!;
        Require(CanonicalJson.IsLowerHex(text, 64), $"{key} hex");
        return text;
    }

    private static string? NullableValidId(JsonElement parent, string key, string what)
    {
        var value = parent.GetProperty(key);
        if (value.ValueKind == JsonValueKind.Null) return null;
        Require(value.ValueKind == JsonValueKind.String, $"{key} string");
        var text = value.GetString()!;
        Require(FrozenSchemaRules.IsValidId(text), what);
        return text;
    }

    private static string? NullableHex(JsonElement parent, string key, string what)
    {
        var value = parent.GetProperty(key);
        if (value.ValueKind == JsonValueKind.Null) return null;
        Require(value.ValueKind == JsonValueKind.String, $"{key} hex");
        var text = value.GetString()!;
        Require(CanonicalJson.IsLowerHex(text, 64), what);
        return text;
    }

    private static long Long(JsonElement parent, string key, long minimum, long maximum)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind == JsonValueKind.Number, $"{key} integer");
        Require(value.TryGetInt64(out var number), $"{key} integer");
        Require(number >= minimum && number <= maximum, $"{key} integer");
        return number;
    }

    private static bool Boolean(JsonElement parent, string key)
    {
        var value = parent.GetProperty(key);
        Require(value.ValueKind is JsonValueKind.True or JsonValueKind.False, $"{key} boolean");
        return value.ValueKind == JsonValueKind.True;
    }

    private static string ValidId(JsonElement parent, string key, string what)
    {
        var value = Text(parent, key);
        Require(FrozenSchemaRules.IsValidId(value), what);
        return value;
    }

    private static void Require(bool condition, string diagnostic)
    {
        if (!condition) throw new InvalidDataException(diagnostic);
    }
}
