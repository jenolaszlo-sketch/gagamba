// RC2A-02A Q2A-V A5 named refusal catalog (test-only). Each entry fixes a
// documented refusal rule with an expected code and construction-derived exact
// offset, and every entry carries a canonical positive counterpart (asserted by
// WireRefusalCase or explicitly in ActionCase bodies).
using Gagamba.Execution.Restricted.Contracts;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal static class QualificationRefusalCatalog
{
    internal static readonly QualificationCase[] Cases = Build();

    private static QualificationCase[] Build()
    {
        var cases = new List<QualificationCase>();

        cases.Add(new ActionCase(
            "Q2A-V-A5-00-closed-vocabulary", "RestrictedManifest", "misuse", "malformed",
            "The refusal vocabulary is closed and ordered: twelve codes starting at zero",
            () =>
            {
                Assert.Equal(new[]
                {
                    "EmptyInput", "InputTooLarge", "UnsupportedSchema", "UnsupportedImplementation",
                    "Truncated", "InvalidLengthOrCount", "InvalidUtf8", "InvalidTag", "InvalidValue",
                    "NonCanonicalOrder", "Duplicate", "TrailingData",
                }, Enum.GetNames<ManifestDecodeError>());
                Assert.Equal(0, (int)ManifestDecodeError.EmptyInput);
                Assert.Equal(11, (int)ManifestDecodeError.TrailingData);
            }));

        cases.Add(new ActionCase(
            "Q2A-V-A5-01-truncation-prefixes", "RestrictedManifest", "length", "malformed",
            "Every proper prefix of the golden refuses without a partial view; Truncated offset equals end of available input",
            () =>
            {
                var golden = QualificationModelTools.GoldenBytes();
                for (var length = 0; length < golden.Length; length++)
                {
                    var result = RestrictedManifestCodec.Decode(golden.AsSpan(0, length));
                    Assert.Null(result.Value);
                    Assert.NotNull(result.Error);
                    Assert.NotNull(result.Offset);
                    Assert.InRange(result.Offset!.Value, 0, length);
                    if (length == 0)
                    {
                        Assert.Equal(ManifestDecodeError.EmptyInput, result.Error);
                        Assert.Equal(0, result.Offset);
                    }
                    else if (result.Error == ManifestDecodeError.Truncated)
                    {
                        Assert.Equal(length, result.Offset);
                    }
                }

                var full = RestrictedManifestCodec.Decode(golden);
                Assert.Null(full.Error);
                Assert.NotNull(full.Value);
            }));

        cases.Add(new ActionCase(
            "Q2A-V-A5-02-trailing-data", "RestrictedManifest", "length", "malformed",
            "One trailing byte after the golden manifest refuses TrailingData at the reader position after the manifest",
            () =>
            {
                var golden = QualificationModelTools.GoldenBytes();
                var withTrailing = golden.Concat(new byte[] { 0x00 }).ToArray();
                var result = RestrictedManifestCodec.Decode(withTrailing);
                Assert.Null(result.Value);
                Assert.Equal(ManifestDecodeError.TrailingData, result.Error);
                Assert.Equal(golden.Length, result.Offset);
                Assert.Null(RestrictedManifestCodec.Decode(golden).Error);
            }));

        cases.Add(new ActionCase(
            "Q2A-V-A5-03-oversized-65537", "RestrictedManifest", "length", "malformed",
            "65,537 bytes refuse InputTooLarge at offset zero before parsing",
            () => AssertSizeRefusal(65537)));

        cases.Add(new ActionCase(
            "Q2A-V-A5-04-oversized-1000000", "RestrictedManifest", "length", "malformed",
            "1,000,000 bytes refuse InputTooLarge at offset zero before parsing",
            () => AssertSizeRefusal(1000000)));

        cases.Add(new ActionCase(
            "Q2A-V-A5-05-empty-input", "RestrictedManifest", "length", "malformed",
            "Empty input refuses EmptyInput at offset zero",
            () =>
            {
                var result = RestrictedManifestCodec.Decode(ReadOnlySpan<byte>.Empty);
                Assert.Null(result.Value);
                Assert.Equal(ManifestDecodeError.EmptyInput, result.Error);
                Assert.Equal(0, result.Offset);
                Assert.Null(RestrictedManifestCodec.Decode(QualificationModelTools.GoldenBytes()).Error);
            }));

        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-06-inflated-namespace-count", "AuthorityManifest.Namespaces", "namespaces.count",
            "Inflated namespace count refuses InvalidLengthOrCount at the count start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-07-inflated-capabilities-count", "AuthorityManifest.Capabilities", "capabilities.count",
            "Inflated capability count refuses InvalidLengthOrCount at the count start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-08-inflated-readonly-count", "AuthorityManifest.ReadOnlyDestinations", "readOnly.count",
            "Inflated read-only destination count refuses InvalidLengthOrCount at the count start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-09-inflated-stores-count", "AuthorityManifest.WritableStores", "stores.count",
            "Inflated writable-store count refuses InvalidLengthOrCount at the count start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-10-inflated-objects-count", "ClosureIdentity.Objects", "objects.count",
            "Inflated closure-object count refuses InvalidLengthOrCount at the count start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-11-inflated-helpers-count", "ClosureIdentity.HelperDependencies", "helpers.count",
            "Inflated helper-dependency count refuses InvalidLengthOrCount at the count start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-12-inflated-argv-count", "InvocationManifest.Argv", "argv.count",
            "Inflated argv count refuses InvalidLengthOrCount at the count start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-13-inflated-env-count", "InvocationManifest.Environment", "env.count",
            "Inflated environment count refuses InvalidLengthOrCount at the count start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-14-inflated-implementation-length", "RestrictedManifest.ImplementationId", "implementation",
            "Inflated implementation-ID length refuses InvalidLengthOrCount at the field start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-15-inflated-mode-length", "InvocationManifest.Mode", "invocation.mode",
            "Inflated mode length refuses InvalidLengthOrCount at the field start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-16-inflated-unit-length", "ResourceBudget.Unit", "budget.Memory.unit",
            "Inflated budget unit length refuses InvalidLengthOrCount at the field start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-17-inflated-role-length", "ClosureObject.Role", "objects.item[0].role",
            "Inflated closure role length refuses InvalidLengthOrCount at the field start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-18-inflated-store-path-length", "WritableStore.Path", "stores.item[0].path",
            "Inflated writable-store path length refuses InvalidLengthOrCount at the field start"));
        cases.Add(QualificationRefusalFactories.LengthInflation(
            "Q2A-V-A5-19-inflated-env-key-length", "InvocationManifest.Environment", "env.key[0]",
            "Inflated environment key length refuses InvalidLengthOrCount at the field start"));

        cases.Add(new WireRefusalCase(
            "Q2A-V-A5-20-reduced-mode-length", "InvocationManifest.Mode", "length", "malformed",
            "A declared mode length shorter than its content shifts parsing and refuses InvalidLengthOrCount at the shifted input-path length",
            ManifestDecodeError.InvalidLengthOrCount,
            () => QualificationWireTemplate.Golden().Build("invocation.mode", w =>
            {
                w.U32(3);
                w.Raw(QualificationUtf8.Strict.GetBytes("work"));
            }),
            "invocation.inputPath",
            offsetDelta: -1));

        cases.Add(QualificationRefusalFactories.MalformedUtf8(
            "Q2A-V-A5-21-overlong-utf8", "InvocationManifest.Mode", "invocation.mode", "c0af",
            "Overlong UTF-8 mode encoding refuses InvalidUtf8 at the field start"));
        cases.Add(QualificationRefusalFactories.MalformedUtf8(
            "Q2A-V-A5-22-surrogate-utf8", "InvocationManifest.Mode", "invocation.mode", "eda080",
            "UTF-8-encoded surrogate mode byte sequence refuses InvalidUtf8 at the field start"));
        cases.Add(QualificationRefusalFactories.MalformedUtf8(
            "Q2A-V-A5-23-out-of-range-utf8", "InvocationManifest.Mode", "invocation.mode", "f4908080",
            "Beyond-Unicode UTF-8 mode byte sequence refuses InvalidUtf8 at the field start"));
        cases.Add(QualificationRefusalFactories.MalformedUtf8(
            "Q2A-V-A5-24-malformed-utf8-implementation", "RestrictedManifest.ImplementationId", "implementation", "ff",
            "Malformed implementation bytes refuse InvalidUtf8 at the field start"));
        cases.Add(QualificationRefusalFactories.MalformedUtf8(
            "Q2A-V-A5-25-malformed-utf8-namespace", "AuthorityManifest.Namespaces", "namespaces.item[0]", "c0af",
            "Malformed namespace bytes refuse InvalidUtf8 at the item start"));
        cases.Add(QualificationRefusalFactories.MalformedUtf8(
            "Q2A-V-A5-26-malformed-utf8-env-key", "InvocationManifest.Environment", "env.key[0]", "eda080",
            "Malformed environment key bytes refuse InvalidUtf8 at the key start"));

        cases.Add(QualificationRefusalFactories.Tag(
            "Q2A-V-A5-27-unknown-tag-no-new-privs", "AuthorityManifest.NoNewPrivs", "authority.noNewPrivs", 2,
            "Boolean tag above one refuses InvalidTag at the field start"));
        cases.Add(QualificationRefusalFactories.Tag(
            "Q2A-V-A5-28-unknown-tag-scratch-noexec", "ExecutableMountPolicy.ScratchNoexec", "mount.scratchNoexec", 2,
            "Boolean tag above one refuses InvalidTag at the field start"));
        cases.Add(QualificationRefusalFactories.Tag(
            "Q2A-V-A5-29-unknown-tag-budget-kind", "ResourceBudget.Kind", "budget.Memory.kind", 3,
            "Budget kind tag above two refuses InvalidTag at the budget start"));
        cases.Add(QualificationRefusalFactories.Tag(
            "Q2A-V-A5-30-unknown-tag-budget-qualification", "ResourceBudget.Qualification", "budget.Memory.qualification", 2,
            "Budget qualification tag above one refuses InvalidTag at the field start"));
        cases.Add(QualificationRefusalFactories.Tag(
            "Q2A-V-A5-31-unknown-tag-network-qualification", "AuthorityManifest.NetworkQualification", "authority.networkQualification", 2,
            "Network qualification tag above one refuses InvalidTag at the field start"));
        cases.Add(QualificationRefusalFactories.Tag(
            "Q2A-V-A5-32-unknown-tag-inode-qualification", "ResourceManifest.InodeQualification", "resources.inodeQualification", 2,
            "Inode qualification tag above one refuses InvalidTag at the field start"));
        cases.Add(QualificationRefusalFactories.Tag(
            "Q2A-V-A5-33-unknown-tag-core-pattern-presence", "CoreDumpPolicy.CorePatternObserved", "core.patternPresent", 2,
            "Core-pattern presence tag above one refuses InvalidTag at the presence byte"));

        cases.Add(QualificationRefusalFactories.Order(
            "Q2A-V-A5-34-unsorted-namespaces", "AuthorityManifest.Namespaces", "namespaces.item[1]", "aaa",
            "Out-of-order namespaces refuse NonCanonicalOrder at the second item start"));
        cases.Add(QualificationRefusalFactories.Order(
            "Q2A-V-A5-35-unsorted-readonly", "AuthorityManifest.ReadOnlyDestinations", "readOnly.item[1]", "/aaa",
            "Out-of-order read-only destinations refuse NonCanonicalOrder at the second item start"));
        cases.Add(QualificationRefusalFactories.Order(
            "Q2A-V-A5-36-unsorted-store-paths", "AuthorityManifest.WritableStores", "stores.item[1].path", "/aaa",
            "Out-of-order writable-store paths refuse NonCanonicalOrder at the second item start"));
        cases.Add(QualificationRefusalFactories.Order(
            "Q2A-V-A5-37-unsorted-closure-roles", "ClosureIdentity.Objects", "objects.item[1].role", "aaa",
            "Out-of-order closure roles refuse NonCanonicalOrder at the second item start"));
        cases.Add(QualificationRefusalFactories.Order(
            "Q2A-V-A5-38-unsorted-helper-deps", "ClosureIdentity.HelperDependencies", "helpers.item[1]", "a",
            "Out-of-order helper dependencies refuse NonCanonicalOrder at the second item start"));
        cases.Add(QualificationRefusalFactories.Order(
            "Q2A-V-A5-39-unsorted-env-keys", "InvocationManifest.Environment", "env.key[1]", "AAA",
            "Out-of-order environment keys refuse NonCanonicalOrder at the second key start"));

        cases.Add(QualificationRefusalFactories.Duplicate(
            "Q2A-V-A5-40-duplicate-namespaces", "AuthorityManifest.Namespaces", "namespaces.item[1]", "ipc",
            "Adjacent duplicate namespaces refuse Duplicate at the second item start"));
        cases.Add(QualificationRefusalFactories.Duplicate(
            "Q2A-V-A5-41-duplicate-readonly", "AuthorityManifest.ReadOnlyDestinations", "readOnly.item[1]", "/coordinator",
            "Adjacent duplicate read-only destinations refuse Duplicate at the second item start"));
        cases.Add(QualificationRefusalFactories.Duplicate(
            "Q2A-V-A5-42-duplicate-store-paths", "AuthorityManifest.WritableStores", "stores.item[1].path", "/dev/shm",
            "Adjacent duplicate writable-store paths refuse Duplicate at the second item start"));
        cases.Add(QualificationRefusalFactories.Duplicate(
            "Q2A-V-A5-43-duplicate-closure-roles", "ClosureIdentity.Objects", "objects.item[1].role", "app",
            "Adjacent duplicate closure roles refuse Duplicate at the second item start"));
        cases.Add(QualificationRefusalFactories.Duplicate(
            "Q2A-V-A5-44-duplicate-helper-deps", "ClosureIdentity.HelperDependencies", "helpers.item[1]", "a-dep",
            "Adjacent duplicate helper dependencies refuse Duplicate at the second item start"));
        cases.Add(QualificationRefusalFactories.Duplicate(
            "Q2A-V-A5-45-duplicate-env-keys", "InvocationManifest.Environment", "env.key[1]", "HOME",
            "Adjacent duplicate environment keys refuse Duplicate at the second key start"));

        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-46-empty-mode", "InvocationManifest.Mode", "invocation.mode",
            "Empty mode refuses InvalidValue at the field start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-47-empty-unit", "ResourceBudget.Unit", "budget.Memory.unit",
            "Empty budget unit refuses InvalidValue at the field start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-48-empty-store-path", "WritableStore.Path", "stores.item[0].path",
            "Empty writable-store path refuses InvalidValue at the item start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-49-empty-closure-role", "ClosureObject.Role", "objects.item[0].role",
            "Empty closure role refuses InvalidValue at the item start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-50-empty-closure-destination", "ClosureObject.Destination", "objects.item[0].destination",
            "Empty closure destination refuses InvalidValue at the item start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-51-empty-build-identity", "ClosureObject.BuildIdentity", "objects.item[0].buildIdentity",
            "Empty build identity refuses InvalidValue at the item start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-52-empty-env-key", "InvocationManifest.Environment", "env.key[0]",
            "Empty environment key refuses InvalidValue at the key start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-53-empty-namespace", "AuthorityManifest.Namespaces", "namespaces.item[0]",
            "Empty namespace item refuses InvalidValue at the item start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-54-empty-readonly", "AuthorityManifest.ReadOnlyDestinations", "readOnly.item[0]",
            "Empty read-only destination refuses InvalidValue at the item start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-55-empty-argv-item", "InvocationManifest.Argv", "argv.item[0]",
            "Empty argv item refuses InvalidValue at the item start"));
        cases.Add(QualificationRefusalFactories.EmptyText(
            "Q2A-V-A5-56-empty-helper-dep", "ClosureIdentity.HelperDependencies", "helpers.item[0]",
            "Empty helper dependency refuses InvalidValue at the item start"));

        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-57-forced-no-ambient-environment", "AuthorityManifest.NoAmbientEnvironment", "forced",
            "Ambient environment denial false refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.NoAmbientEnvironment = false),
            "authority.noAmbientEnvironment", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-58-forced-no-ipc-endpoints", "AuthorityManifest.NoIpcEndpoints", "forced",
            "IPC endpoint denial false refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.NoIpcEndpoints = false),
            "authority.noIpcEndpoints", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-59-forced-no-extra-inherited-fds", "AuthorityManifest.NoExtraInheritedFds", "forced",
            "Extra inherited FD denial false refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.NoExtraInheritedFds = false),
            "authority.noExtraFds", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-60-forced-tool-path", "InvocationManifest.ToolPath", "forced",
            "Tool path other than /tool refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.ToolPath = "/wrong"),
            "invocation.toolPath", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-61-forced-input-path", "InvocationManifest.InputPath", "forced",
            "Input path other than /input refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.InputPath = "/wrong"),
            "invocation.inputPath", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-62-forced-work-directory", "InvocationManifest.WorkDirectory", "forced",
            "Work directory other than /tmp refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.WorkDirectory = "/wrong"),
            "invocation.workDirectory", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-63-forced-nonempty-capabilities", "AuthorityManifest.Capabilities", "forced",
            "Nonempty capability set refuses InvalidValue at the capability count start",
            () => VariantTemplate(t => t.Capabilities = new[] { "cap" }),
            "capabilities.count", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-64-forced-stdin-eof", "ChannelManifest.StdinEof", "forced",
            "Stdin not closed refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.StdinEof = false),
            "channels.stdinEof", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-65-forced-network-mandatory", "AuthorityManifest.NetworkQualification", "forced",
            "Network Mandatory qualification refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.NetworkQualification = Gagamba.Execution.Restricted.QualificationDisposition.Mandatory),
            "authority.networkQualification", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-66-forced-inode-mandatory", "ResourceManifest.InodeQualification", "forced",
            "Inode Mandatory qualification refuses InvalidValue at the field start",
            () => VariantTemplate(t => t.InodeQualification = Gagamba.Execution.Restricted.QualificationDisposition.Mandatory),
            "resources.inodeQualification", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-67-forced-inode-budget-absent-nonzero", "WritableStore.InodeBudget", "cross",
            "Inode budget absent with a nonzero value refuses InvalidValue at the inode budget start",
            () => VariantTemplate(t => t.Stores = new[] { new QualificationWireStore { Path = "/tmp", Bytes = 1, HasInodeBudget = false, InodeBudget = 5 } }),
            "stores.item[0].inode", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-68-forced-budget-absent-nonzero", "ResourceBudget.Kind", "cross",
            "Absent budget with a nonzero value refuses InvalidValue at the budget start",
            () => VariantTemplate(t => QualificationWireTemplate.SetBudget(t, "Memory", Gagamba.Execution.Restricted.BudgetValueKind.Absent, 5, "bytes")),
            "budget.Memory.kind", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-69-forced-budget-unlimited-nonzero", "ResourceBudget.Kind", "cross",
            "Unlimited budget with a nonzero value refuses InvalidValue at the budget start",
            () => VariantTemplate(t => QualificationWireTemplate.SetBudget(t, "Memory", Gagamba.Execution.Restricted.BudgetValueKind.Unlimited, 5, "bytes")),
            "budget.Memory.kind", ManifestDecodeError.InvalidValue));

        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-70-unsupported-schema-0", "RestrictedManifest.SchemaVersion", "supported-id",
            "Schema 0 refuses UnsupportedSchema at offset zero",
            () => VariantTemplate(t => t.SchemaVersion = 0), "schema", ManifestDecodeError.UnsupportedSchema));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-71-unsupported-schema-2", "RestrictedManifest.SchemaVersion", "supported-id",
            "Schema 2 refuses UnsupportedSchema at offset zero",
            () => VariantTemplate(t => t.SchemaVersion = 2), "schema", ManifestDecodeError.UnsupportedSchema));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-72-unsupported-schema-65535", "RestrictedManifest.SchemaVersion", "supported-id",
            "Schema 65,535 refuses UnsupportedSchema at offset zero",
            () => VariantTemplate(t => t.SchemaVersion = 65535), "schema", ManifestDecodeError.UnsupportedSchema));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-73-unsupported-implementation-empty", "RestrictedManifest.ImplementationId", "supported-id",
            "Empty implementation ID refuses InvalidValue at the field start (empty non-allowed string)",
            () => VariantTemplate(t => t.ImplementationId = string.Empty), "implementation", ManifestDecodeError.InvalidValue));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-74-unsupported-implementation-whitespace", "RestrictedManifest.ImplementationId", "supported-id",
            "Whitespace implementation ID refuses UnsupportedImplementation at offset two",
            () => VariantTemplate(t => t.ImplementationId = " "), "implementation", ManifestDecodeError.UnsupportedImplementation));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-75-unsupported-implementation-case", "RestrictedManifest.ImplementationId", "supported-id",
            "Case-variant implementation ID refuses UnsupportedImplementation at offset two",
            () => VariantTemplate(t => t.ImplementationId = "Linux-m1-native-transaction"), "implementation", ManifestDecodeError.UnsupportedImplementation));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-76-unsupported-implementation-trailing-nul", "RestrictedManifest.ImplementationId", "supported-id",
            "Trailing-NUL implementation ID refuses UnsupportedImplementation at offset two",
            () => VariantTemplate(t => t.ImplementationId = Gagamba.Execution.Restricted.RestrictedManifest.ImplementationIdentifier + "\0"), "implementation", ManifestDecodeError.UnsupportedImplementation));
        cases.Add(QualificationRefusalFactories.Variant(
            "Q2A-V-A5-77-unsupported-implementation-unknown", "RestrictedManifest.ImplementationId", "supported-id",
            "Unknown implementation ID refuses UnsupportedImplementation at offset two",
            () => VariantTemplate(t => t.ImplementationId = "other-profile"), "implementation", ManifestDecodeError.UnsupportedImplementation));

        return cases.ToArray();
    }

    private static QualificationWireTemplate VariantTemplate(Action<QualificationWireTemplate> mutate)
    {
        var template = QualificationWireTemplate.Golden();
        mutate(template);
        return template;
    }

    private static void AssertSizeRefusal(int size)
    {
        var result = RestrictedManifestCodec.Decode(new byte[size]);
        Assert.Null(result.Value);
        Assert.Equal(ManifestDecodeError.InputTooLarge, result.Error);
        Assert.Equal(0, result.Offset);

        // Canonical positive counterpart: the 65,536-byte boundary vector is accepted.
        var boundary = Convert.FromHexString(QualificationVectorBytes.ById["Q2A-V-20-max-manifest-65536"].Hex);
        Assert.Equal(65536, boundary.Length);
        var accepted = RestrictedManifestCodec.Decode(boundary);
        Assert.Null(accepted.Error);
        Assert.Equal(boundary, RestrictedManifestCodec.Encode(
            Assert.IsType<RestrictedManifestView>(accepted.Value)));
    }
}
