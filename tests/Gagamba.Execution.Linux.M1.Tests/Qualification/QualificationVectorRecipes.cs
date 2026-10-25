// RC2A-02A Q2A-V vector recipes (test-only). Each vector fixes its purpose, its
// committed deterministic construction recipe, the model materialization used by
// the independent ReferenceEncoder, and the independent raw-template values used
// for decode-side graph verification. Expected canonical bytes and SHA-256
// digests are committed literals in QualificationVectorBytes (authored from the
// independent reference encoder and re-derivable by the committed tests).
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal sealed record QualificationVector(
    string Id,
    string Purpose,
    string Recipe,
    bool ExpectedSupports,
    Func<RestrictedManifest> Materialize,
    Func<QualificationWireTemplate> Expected);

internal static class QualificationVectorRecipes
{
    internal const int MaxManifestTotalBytes = 65536;
    internal const int MaxManifestPaddingBytes = MaxManifestTotalBytes - 869; // golden length minus its 4 mode-content bytes
    internal const int MaxArgEnvTotalBytes = 16384;

    internal static RestrictedManifest Minimal()
    {
        return new RestrictedManifest(
            1,
            RestrictedManifest.ImplementationIdentifier,
            new AuthorityManifest(
                Array.Empty<string>(), Array.Empty<string>(), true, new byte[32], 0,
                Array.Empty<string>(), Array.Empty<WritableStore>(), true, true, true,
                new ExecutableMountPolicy(true, false), QualificationDisposition.PendingQualification),
            new ResourceManifest(
                Exact(0), Exact(0), Exact(0), Exact(0), Exact(0), Exact(0), Exact(0),
                Exact(0), Exact(0), Exact(0), Exact(0), Exact(0), Exact(0),
                QualificationDisposition.PendingQualification,
                new CoreDumpPolicy(true, false, null),
                new OwnerAllocationLimits(0, 0, 0, 0, 0, 0, 0)),
            new ClosureIdentity(Array.Empty<ClosureObject>(), Array.Empty<string>()),
            new InvocationManifest("/tool", "work", "/input", Array.Empty<string>(),
                Array.Empty<KeyValuePair<string, string>>(), "/tmp"),
            new ChannelManifest(true, 0, 0, 0));
    }

    internal static ResourceBudget Exact(ulong value, string unit = "bytes") =>
        new(BudgetValueKind.Exact, value, unit, QualificationDisposition.Mandatory);

    private static RestrictedManifest Sample() => QualificationModelTools.Baseline();

    private static QualificationWireTemplate Template() => QualificationWireTemplate.Golden();

    private static QualificationWireTemplate MinimalTemplate() => new();

    internal static readonly QualificationVector[] All =
    {
        new(
            "Q2A-V-01-golden",
            "Preserved 873-byte literal golden vector, byte-for-byte unchanged from RC2A-01",
            "SampleManifest() exactly as committed at the accepted commit; literal hex preserved",
            true,
            Sample,
            Template),
        new(
            "Q2A-V-02-minimal",
            "Minimal valid manifest: every collection empty, all budgets Exact zero, all optional values absent",
            "Constructor-recipe minimal manifest with empty collections and zero owner allocation",
            true,
            Minimal,
            MinimalTemplate),
        new(
            "Q2A-V-03-empty-namespaces",
            "Empty namespace list on the otherwise full sample graph",
            "SampleManifest with Authority.Namespaces = []",
            true,
            () => QualificationModelTools.WithAuthority(Sample(), "Namespaces", Array.Empty<string>()),
            () =>
            {
                var t = Template();
                t.Namespaces = Array.Empty<string>();
                return t;
            }),
        new(
            "Q2A-V-04-nonascii-namespaces",
            "Non-ASCII namespaces sorted by unsigned UTF-8 byte order (a < café < z < é)",
            "SampleManifest with Authority.Namespaces = [a, café, z, é]",
            true,
            () => QualificationModelTools.WithAuthority(Sample(), "Namespaces", new[] { "a", "caf\u00E9", "z", "\u00E9" }),
            () =>
            {
                var t = Template();
                t.Namespaces = new[] { "a", "caf\u00E9", "z", "\u00E9" };
                return t;
            }),
        new(
            "Q2A-V-05-empty-argv",
            "Zero-length argv list encodes as a zero count and round-trips",
            "SampleManifest with Invocation.Argv = []",
            true,
            () => QualificationModelTools.WithInvocation(Sample(), "Argv", Array.Empty<string>()),
            () =>
            {
                var t = Template();
                t.Argv = Array.Empty<string>();
                return t;
            }),
        new(
            "Q2A-V-06-empty-environment",
            "Empty environment list encodes as a zero count and round-trips",
            "SampleManifest with Invocation.Environment = []",
            true,
            () => QualificationModelTools.WithInvocation(Sample(), "Environment", Array.Empty<KeyValuePair<string, string>>()),
            () =>
            {
                var t = Template();
                t.Environment = Array.Empty<QualificationWireEnvPair>();
                return t;
            }),
        new(
            "Q2A-V-07-empty-environment-value",
            "Present environment key with an empty value is distinct from an absent key",
            "SampleManifest with Invocation.Environment = [(K, ''), (LANG, C)]",
            true,
            () => QualificationModelTools.WithInvocation(Sample(), "Environment", new[]
            {
                new KeyValuePair<string, string>("K", string.Empty),
                new KeyValuePair<string, string>("LANG", "C"),
            }),
            () =>
            {
                var t = Template();
                t.Environment = new[]
                {
                    new QualificationWireEnvPair { Key = "K", Value = string.Empty },
                    new QualificationWireEnvPair { Key = "LANG", Value = "C" },
                };
                return t;
            }),
        new(
            "Q2A-V-08-budget-absent",
            "Memory budget Absent carries a canonical zero value and stays distinct from zero and unlimited",
            "SampleManifest with Resources.Memory = Absent(0, bytes, Mandatory)",
            true,
            () => QualificationModelTools.WithBudget(Sample(), "Memory",
                new ResourceBudget(BudgetValueKind.Absent, 0, "bytes", QualificationDisposition.Mandatory)),
            () =>
            {
                var t = Template();
                QualificationWireTemplate.SetBudget(t, "Memory", BudgetValueKind.Absent, 0, "bytes");
                return t;
            }),
        new(
            "Q2A-V-09-budget-exact-zero",
            "Memory budget Exact zero is distinct from Absent and Unlimited",
            "SampleManifest with Resources.Memory = Exact(0, bytes, Mandatory)",
            true,
            () => QualificationModelTools.WithBudget(Sample(), "Memory", Exact(0)),
            () =>
            {
                var t = Template();
                QualificationWireTemplate.SetBudget(t, "Memory", BudgetValueKind.Exact, 0, "bytes");
                return t;
            }),
        new(
            "Q2A-V-10-budget-unlimited",
            "Memory budget Unlimited carries a canonical zero value",
            "SampleManifest with Resources.Memory = Unlimited(0, bytes, Mandatory)",
            true,
            () => QualificationModelTools.WithBudget(Sample(), "Memory",
                new ResourceBudget(BudgetValueKind.Unlimited, 0, "bytes", QualificationDisposition.Mandatory)),
            () =>
            {
                var t = Template();
                QualificationWireTemplate.SetBudget(t, "Memory", BudgetValueKind.Unlimited, 0, "bytes");
                return t;
            }),
        new(
            "Q2A-V-11-inode-budget-present",
            "Writable store with an inode budget present (hasInode true, nonzero budget)",
            "SampleManifest with Authority.WritableStores = [/tmp, 7340032, hasInode true, 1024]",
            true,
            () => QualificationModelTools.WithAuthority(Sample(), "WritableStores", new[]
            {
                new WritableStore("/tmp", 7340032, true, 1024),
            }),
            () =>
            {
                var t = Template();
                t.Stores = new[] { new QualificationWireStore { Path = "/tmp", Bytes = 7340032, HasInodeBudget = true, InodeBudget = 1024 } };
                return t;
            }),
        new(
            "Q2A-V-12-inode-budget-absent",
            "Writable store with the inode budget absent (hasInode false, canonical zero)",
            "SampleManifest with Authority.WritableStores = [/tmp, 7340032, hasInode false, 0]",
            true,
            () => QualificationModelTools.WithAuthority(Sample(), "WritableStores", new[]
            {
                new WritableStore("/tmp", 7340032, false, 0),
            }),
            () =>
            {
                var t = Template();
                t.Stores = new[] { new QualificationWireStore { Path = "/tmp", Bytes = 7340032, HasInodeBudget = false, InodeBudget = 0 } };
                return t;
            }),
        new(
            "Q2A-V-13-nonewprivs-false",
            "NoNewPrivs=false is a representable decoded value (not a forced field)",
            "SampleManifest with Authority.NoNewPrivs = false",
            true,
            () => QualificationModelTools.WithAuthority(Sample(), "NoNewPrivs", false),
            () =>
            {
                var t = Template();
                t.NoNewPrivs = false;
                return t;
            }),
        new(
            "Q2A-V-14-runtime-dumps-disabled",
            "RuntimeDumpsDisabled=true is a representable decoded value",
            "SampleManifest with Resources.CoreDumps.RuntimeDumpsDisabled = true",
            true,
            () => QualificationModelTools.WithResources(Sample(), "CoreDumps", new CoreDumpPolicy(true, true, null)),
            () =>
            {
                var t = Template();
                t.RuntimeDumpsDisabled = true;
                return t;
            }),
        new(
            "Q2A-V-15-core-pattern-present",
            "Observed core pattern present with a non-ASCII printable value and dumps still disabled",
            "SampleManifest with Resources.CoreDumps = (true, false, '|/tmp/core.%e')",
            true,
            () => QualificationModelTools.WithResources(Sample(), "CoreDumps", new CoreDumpPolicy(true, false, "|/tmp/core.%e")),
            () =>
            {
                var t = Template();
                t.RuntimeDumpsDisabled = false;
                t.CorePatternObserved = "|/tmp/core.%e";
                return t;
            }),
        new(
            "Q2A-V-16-core-pattern-nul-supplementary",
            "Core pattern containing NUL and a supplementary-plane character survives structurally valid round trips",
            "SampleManifest with Resources.CoreDumps = (true, false, 'core\\u0000\\U00010000')",
            true,
            () => QualificationModelTools.WithResources(Sample(), "CoreDumps", new CoreDumpPolicy(true, false, "core\u0000\U00010000")),
            () =>
            {
                var t = Template();
                t.CorePatternObserved = "core\u0000\U00010000";
                return t;
            }),
        new(
            "Q2A-V-17-supplementary-plane",
            "Supplementary-plane strings in namespaces, helper dependencies and environment keys",
            "SampleManifest with namespaces/helpers/env using U+E000 and U+10000 in canonical byte order",
            true,
            () =>
            {
                var model = QualificationModelTools.WithAuthority(Sample(), "Namespaces", new[] { "\uE000", "\U00010000" });
                model = QualificationModelTools.WithClosure(model, "HelperDependencies", new[] { "\uE000", "\U00010000" });
                model = QualificationModelTools.WithInvocation(model, "Environment",
                    new[] { new KeyValuePair<string, string>("\U00010000", "\U00010000") });
                return model;
            },
            () =>
            {
                var t = Template();
                t.Namespaces = new[] { "\uE000", "\U00010000" };
                t.HelperDependencies = new[] { "\uE000", "\U00010000" };
                t.Environment = new[] { new QualificationWireEnvPair { Key = "\U00010000", Value = "\U00010000" } };
                return t;
            }),
        new(
            "Q2A-V-18-nonascii-utf8",
            "Two- and three-byte UTF-8 characters in mode and environment keys/values",
            "SampleManifest with Mode = café and environment [clé, ß]",
            true,
            () =>
            {
                var model = QualificationModelTools.WithInvocation(Sample(), "Mode", "caf\u00E9");
                model = QualificationModelTools.WithInvocation(model, "Environment", new[]
                {
                    new KeyValuePair<string, string>("cl\u00E9", "valeur-\u00E9"),
                    new KeyValuePair<string, string>("\u00DF", "m\u00fcnchen"),
                });
                return model;
            },
            () =>
            {
                var t = Template();
                t.Mode = "caf\u00E9";
                t.Environment = new[]
                {
                    new QualificationWireEnvPair { Key = "cl\u00E9", Value = "valeur-\u00E9" },
                    new QualificationWireEnvPair { Key = "\u00DF", Value = "m\u00fcnchen" },
                };
                return t;
            }),
        new(
            "Q2A-V-19-max-argenv-16384",
            "Maximum accepted combined argv/environment payload: exactly 16,384 strict-UTF-8 bytes",
            "SampleManifest with Argv = ['a' * 16383] and Environment = [(K, '')]: 16383 + 1 + 0 bytes",
            true,
            () => QualificationModelTools.Replace(Sample(), "Invocation", new InvocationManifest(
                "/tool", "work", "/input",
                new[] { new string('a', MaxArgEnvTotalBytes - 1) },
                new[] { new KeyValuePair<string, string>("K", string.Empty) },
                "/tmp")),
            () =>
            {
                var t = Template();
                t.Argv = new[] { new string('a', MaxArgEnvTotalBytes - 1) };
                t.Environment = new[] { new QualificationWireEnvPair { Key = "K", Value = string.Empty } };
                return t;
            }),
        new(
            "Q2A-V-20-max-manifest-65536",
            "Maximum accepted complete canonical encoding: exactly 65,536 bytes via padded mode",
            "SampleManifest with Mode = 'a' * 64,667 so the complete encoding is exactly 65,536 bytes",
            true,
            () => QualificationModelTools.Model(mode: new string('a', MaxManifestPaddingBytes)),
            () =>
            {
                var t = Template();
                t.Mode = new string('a', MaxManifestPaddingBytes);
                return t;
            }),
        new(
            "Q2A-V-21-duplicate-argv",
            "Duplicate argv entries are explicitly retained as a valid distinct encoding",
            "SampleManifest with Invocation.Argv = [x, x]",
            true,
            () => QualificationModelTools.WithInvocation(Sample(), "Argv", new[] { "x", "x" }),
            () =>
            {
                var t = Template();
                t.Argv = new[] { "x", "x" };
                return t;
            }),
        new(
            "Q2A-V-22-empty-closure",
            "Empty closure object list and empty helper dependency list",
            "SampleManifest with Closure.Objects = [] and Closure.HelperDependencies = []",
            true,
            () =>
            {
                var model = QualificationModelTools.WithClosure(Sample(), "Objects", Array.Empty<ClosureObject>());
                return QualificationModelTools.WithClosure(model, "HelperDependencies", Array.Empty<string>());
            },
            () =>
            {
                var t = Template();
                t.Objects = Array.Empty<QualificationWireClosureObject>();
                t.HelperDependencies = Array.Empty<string>();
                return t;
            }),
        new(
            "Q2A-V-23-inode-present-and-absent",
            "One store with an inode budget present and one with it absent in the same authority inventory",
            "SampleManifest with stores [/dev/shm true 512] and [/tmp false 0]",
            true,
            () => QualificationModelTools.WithAuthority(Sample(), "WritableStores", new[]
            {
                new WritableStore("/dev/shm", 1048576, true, 512),
                new WritableStore("/tmp", 7340032, false, 0),
            }),
            () =>
            {
                var t = Template();
                t.Stores = new[]
                {
                    new QualificationWireStore { Path = "/dev/shm", Bytes = 1048576, HasInodeBudget = true, InodeBudget = 512 },
                    new QualificationWireStore { Path = "/tmp", Bytes = 7340032, HasInodeBudget = false, InodeBudget = 0 },
                };
                return t;
            }),
    };
}
