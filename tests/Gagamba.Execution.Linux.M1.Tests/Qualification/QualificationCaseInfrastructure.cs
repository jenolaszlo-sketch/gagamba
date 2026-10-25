// RC2A-02A qualification case infrastructure (test-only). Cases carry a fixed
// identity (case id, field id, mutation class, row class) and an Execute body
// that performs the complete acceptance/refusal assertions. Expected values are
// authored from the accepted contract; offsets come from the independent wire
// template's recorded byte positions, never from the production decoder.
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal abstract class QualificationCase
{
    protected QualificationCase(string caseId, string fieldId, string mutationClass, string caseClass, string description)
    {
        CaseId = caseId;
        FieldId = fieldId;
        MutationClass = mutationClass;
        CaseClass = caseClass;
        Description = description;
    }

    internal string CaseId { get; }
    internal string FieldId { get; }
    internal string MutationClass { get; }
    internal string CaseClass { get; }
    internal string Description { get; }

    internal abstract void Execute();
}

/// <summary>Valid semantic or boundary mutation built through the real model
/// constructors. Asserts independent reference-encoder equality, canonical byte
/// and digest change, decode success, complete graph projection and an explicit
/// domain observation of the changed value.</summary>
internal sealed class ValidMutationCase : QualificationCase
{
    private readonly Func<RestrictedManifest> baseline;
    private readonly Func<RestrictedManifest, RestrictedManifest> mutate;
    private readonly Action<RestrictedManifest, RestrictedManifestView> domainCheck;

    internal ValidMutationCase(
        string caseId,
        string fieldId,
        string mutationClass,
        string description,
        Func<RestrictedManifest> baseline,
        Func<RestrictedManifest, RestrictedManifest> mutate,
        Action<RestrictedManifest, RestrictedManifestView> domainCheck)
        : base(caseId, fieldId, mutationClass, "valid", description)
    {
        this.baseline = baseline;
        this.mutate = mutate;
        this.domainCheck = domainCheck;
    }

    internal override void Execute()
    {
        var before = baseline();
        var beforeBytes = QualificationModelTools.ReferenceEncode(before);
        var mutated = mutate(before);
        Assert.NotSame(before, mutated);

        var view = new RestrictedManifestView(mutated);
        var production = RestrictedManifestCodec.Encode(view);
        var reference = QualificationModelTools.ReferenceEncode(mutated);
        Assert.Equal(reference, production);
        Assert.False(beforeBytes.AsSpan().SequenceEqual(production), "A valid mutation must change the canonical bytes.");
        Assert.NotEqual(
            QualificationModelTools.ReferenceDigestHex(before),
            QualificationModelTools.ReferenceDigestHex(mutated));

        var decoded = RestrictedManifestCodec.Decode(production);
        Assert.Null(decoded.Error);
        Assert.Null(decoded.Offset);
        var decodedView = Assert.IsType<RestrictedManifestView>(decoded.Value);
        QualificationModelTools.AssertGraph(mutated, decodedView);
        Assert.Equal(production, RestrictedManifestCodec.Encode(decodedView));
        domainCheck(mutated, decodedView);
    }
}

/// <summary>Structurally malformed, noncanonical or forced-value wire input.
/// Asserts the exact refusal code and the exact construction-recorded offset,
/// and that the canonical positive counterpart decodes and re-encodes
/// identically.</summary>
internal sealed class WireRefusalCase : QualificationCase
{
    private readonly Func<QualificationWireBuild> buildRefusal;
    private readonly Func<QualificationWireBuild> buildCanonical;
    private readonly string offsetKey;
    private readonly int offsetDelta;

    internal WireRefusalCase(
        string caseId,
        string fieldId,
        string mutationClass,
        string caseClass,
        string description,
        ManifestDecodeError expectedError,
        Func<QualificationWireBuild> buildRefusal,
        string offsetKey,
        Func<QualificationWireBuild>? buildCanonical = null,
        int offsetDelta = 0)
        : base(caseId, fieldId, mutationClass, caseClass, description)
    {
        ExpectedError = expectedError;
        this.buildRefusal = buildRefusal;
        this.offsetKey = offsetKey;
        this.offsetDelta = offsetDelta;
        this.buildCanonical = buildCanonical ?? (() => QualificationWireTemplate.Golden().Build());
    }

    internal ManifestDecodeError ExpectedError { get; }

    internal override void Execute()
    {
        var built = buildRefusal();
        var expectedOffset = built.OffsetOf(offsetKey) + offsetDelta;
        Assert.InRange(expectedOffset, 0, built.Bytes.Length);
        var result = RestrictedManifestCodec.Decode(built.Bytes);
        Assert.Null(result.Value);
        Assert.Equal(ExpectedError, result.Error);
        Assert.Equal(expectedOffset, result.Offset);

        var canonical = buildCanonical();
        var accepted = RestrictedManifestCodec.Decode(canonical.Bytes);
        Assert.Null(accepted.Error);
        var view = Assert.IsType<RestrictedManifestView>(accepted.Value);
        Assert.Equal(canonical.Bytes, RestrictedManifestCodec.Encode(view));
    }
}

/// <summary>Explicit assertion body for family-level cases (sweeps, multi-input
/// catalogs) whose expected behavior is committed in the body.</summary>
internal sealed class ActionCase : QualificationCase
{
    private readonly Action action;

    internal ActionCase(string caseId, string fieldId, string mutationClass, string caseClass, string description, Action action)
        : base(caseId, fieldId, mutationClass, caseClass, description)
    {
        this.action = action;
    }

    internal override void Execute() => action();
}

/// <summary>Public API misuse (programming error) with the documented exception.</summary>
internal sealed class MisuseCase : QualificationCase
{
    private readonly Action action;

    internal MisuseCase(string caseId, string fieldId, string description, Action action)
        : base(caseId, fieldId, "misuse", "misuse", description)
    {
        this.action = action;
    }

    internal override void Execute() => Assert.Throws<ArgumentNullException>(action);
}

/// <summary>Composite-field traceability: the public view type's property
/// inventory must equal the committed field-map subfields, in field-map order.</summary>
internal sealed class CompositeTraceCase : QualificationCase
{
    private readonly Type viewType;
    private readonly string[] expectedProperties;

    internal CompositeTraceCase(string caseId, string fieldId, string description, Type viewType, string[] expectedProperties)
        : base(caseId, fieldId, "trace", "trace", description)
    {
        this.viewType = viewType;
        this.expectedProperties = expectedProperties;
    }

    internal override void Execute()
    {
        var actual = viewType.GetProperties().Select(p => p.Name).ToArray();
        Assert.Equal(expectedProperties, actual);
    }
}
