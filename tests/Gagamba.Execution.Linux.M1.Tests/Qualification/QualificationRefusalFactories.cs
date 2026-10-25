// RC2A-02A refusal-case factories (test-only): reusable constructors for wire
// refusal cases. Each factory fixes one documented refusal rule; offsets come
// from the independent wire template's construction-derived byte positions.
// Row classes follow the plan's F5 labels: order/duplicate/forced/cross are
// noncanonical; tag/utf8/length/empty refusals are malformed.
using Gagamba.Execution.Restricted.Contracts;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal static class QualificationRefusalFactories
{
    internal static string RowClass(string mutationClass) => mutationClass switch
    {
        "valid" => "valid",
        "misuse" => "misuse",
        "trace" => "trace",
        "order" or "duplicate" or "forced" or "cross" => "noncanonical",
        _ => "malformed",
    };

    internal static WireRefusalCase Defect(
        string caseId,
        string fieldId,
        string mutationClass,
        string description,
        string defectSlot,
        Action<QualificationWireWriter> defect,
        ManifestDecodeError expected,
        string offsetKey = "defect",
        Func<QualificationWireTemplate>? template = null)
    {
        template ??= QualificationWireTemplate.Golden;
        return new WireRefusalCase(
            caseId, fieldId, mutationClass, RowClass(mutationClass), description, expected,
            () => template().Build(defectSlot, defect), offsetKey);
    }

    internal static WireRefusalCase Variant(
        string caseId,
        string fieldId,
        string mutationClass,
        string description,
        Func<QualificationWireTemplate> template,
        string offsetKey,
        ManifestDecodeError expected)
    {
        return new WireRefusalCase(
            caseId, fieldId, mutationClass, RowClass(mutationClass), description, expected,
            () => template().Build(), offsetKey);
    }

    internal static WireRefusalCase Tag(string caseId, string fieldId, string slot, byte tag, string description) =>
        Defect(caseId, fieldId, "tag", description, slot, w => w.Tag(tag), ManifestDecodeError.InvalidTag);

    internal static WireRefusalCase LengthInflation(string caseId, string fieldId, string slot, string description) =>
        Defect(caseId, fieldId, "length", description, slot, w => w.U32(uint.MaxValue),
            ManifestDecodeError.InvalidLengthOrCount);

    internal static WireRefusalCase MalformedUtf8(string caseId, string fieldId, string slot, string hex, string description) =>
        Defect(caseId, fieldId, "utf8", description, slot, w => w.TextRaw(Convert.FromHexString(hex)),
            ManifestDecodeError.InvalidUtf8);

    internal static WireRefusalCase EmptyText(string caseId, string fieldId, string slot, string description) =>
        Defect(caseId, fieldId, "empty", description, slot, w => w.Text(string.Empty),
            ManifestDecodeError.InvalidValue);

    internal static WireRefusalCase WrongFixedText(string caseId, string fieldId, string slot, string value, string description) =>
        Defect(caseId, fieldId, "forced", description, slot, w => w.Text(value),
            ManifestDecodeError.InvalidValue);

    internal static WireRefusalCase ForcedBoolFalse(string caseId, string fieldId, string slot, string description) =>
        Defect(caseId, fieldId, "forced", description, slot, w => w.Bool(false),
            ManifestDecodeError.InvalidValue);

    internal static WireRefusalCase Order(string caseId, string fieldId, string slot, string value, string description) =>
        Defect(caseId, fieldId, "order", description, slot, w => w.Text(value),
            ManifestDecodeError.NonCanonicalOrder);

    internal static WireRefusalCase Duplicate(string caseId, string fieldId, string slot, string value, string description) =>
        Defect(caseId, fieldId, "duplicate", description, slot, w => w.Text(value),
            ManifestDecodeError.Duplicate);
}
