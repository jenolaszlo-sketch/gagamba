// RC2A-02A Q2A-F 72-field mutation matrix (test-only). The field inventory is
// the committed RC2A-01 field-to-property map, in order. Each field declares its
// kind; the applicability table fixes which mutation classes apply, and the
// generated cases carry fixed identities and expected outcomes. Offsets for wire
// refusals come from the independent template's construction-recorded byte
// positions. No field is covered only incidentally: every applicable class has
// at least one case, and non-applicable classes are recorded with a reason.
using Gagamba.Execution.Restricted;
using Gagamba.Execution.Restricted.Contracts;
using InternalBudgetKind = Gagamba.Execution.Restricted.BudgetValueKind;
using InternalQualification = Gagamba.Execution.Restricted.QualificationDisposition;
using Xunit;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal sealed record QualificationBudgetSpec(InternalBudgetKind Kind, ulong Value);

internal sealed record QualificationBudgetShape(byte Kind, ulong Value);

internal sealed record QualificationModelVariant(
    string IdSuffix,
    Func<RestrictedManifest, RestrictedManifest> Mutate,
    bool RequireChange);

internal sealed record QualificationFieldRow(
    int Index,
    string FieldId,
    string Kind,
    string Slot = "",
    string CountSlot = "",
    string BudgetName = "",
    QualificationModelVariant[]? ModelVariants = null,
    Action<RestrictedManifest, RestrictedManifestView>? DomainCheck = null,
    string? OrderValue = null,
    string? DuplicateValue = null,
    string[]? Subfields = null,
    Type? ViewType = null);

internal static class QualificationFieldMatrix
{
    internal static readonly string[] MutationClasses =
    {
        "valid", "boundary", "presence", "tag", "forced", "order", "duplicate",
        "length", "utf8", "cross", "empty", "supported-id", "misuse", "trace",
    };

    private static readonly Dictionary<string, string[]> Applicability = new(StringComparer.Ordinal)
    {
        ["schema"] = new[] { "supported-id" },
        ["implementation-id"] = new[] { "supported-id", "utf8", "length", "misuse" },
        ["composite"] = new[] { "trace" },
        ["bool-free"] = new[] { "valid", "tag" },
        ["bool-forced"] = new[] { "tag", "forced" },
        ["enum-free"] = new[] { "valid", "tag" },
        ["enum-pending"] = new[] { "tag", "forced" },
        ["budget-kind"] = new[] { "valid", "tag", "cross" },
        ["u64"] = new[] { "valid", "boundary" },
        ["u32"] = new[] { "valid", "boundary" },
        ["bytes32"] = new[] { "valid" },
        ["text-free"] = new[] { "valid", "utf8", "length", "empty" },
        ["text-fixed"] = new[] { "forced", "utf8", "length" },
        ["text-optional"] = new[] { "valid", "presence", "tag", "utf8", "length", "empty" },
        ["collection-ordered"] = new[] { "valid", "boundary", "order", "duplicate", "length", "utf8", "empty" },
        ["collection-empty"] = new[] { "length", "forced" },
        ["argv"] = new[] { "valid", "boundary", "length", "utf8", "empty" },
        ["env"] = new[] { "valid", "boundary", "order", "duplicate", "length", "utf8", "empty" },
        ["budget"] = new[] { "valid", "boundary", "presence", "cross" },
        ["store-inode"] = new[] { "valid", "boundary", "cross" },
    };

    private static readonly Dictionary<string, string> NonApplicableReasons = new(StringComparer.Ordinal)
    {
        ["schema:tag"] = "Schema version is a u16 field, not a one-byte tag.",
        ["schema:valid"] = "Only schema 1 is accepted; there is no valid alternative schema.",
        ["schema:forced"] = "Schema refusal is UnsupportedSchema, recorded under supported-id.",
        ["implementation-id:forced"] = "Implementation refusal is UnsupportedImplementation/InvalidValue, recorded under supported-id.",
        ["composite:valid"] = "Composite value; valid mutations are covered by its subfield rows.",
        ["composite:tag"] = "Composite value; tag mutations are covered by its subfield rows.",
        ["collection-empty:valid"] = "The capability set is forced empty; no nonempty value is representable.",
        ["collection-empty:order"] = "Ordering is not applicable to a forced-empty set.",
        ["collection-empty:duplicate"] = "Duplicates are not applicable to a forced-empty set.",
        ["collection-empty:utf8"] = "Item encoding is not reachable because any item refuses.",
        ["collection-empty:empty"] = "An empty item would first refuse as a nonempty-set violation.",
        ["budget-kind:forced"] = "Budget kind has no forced single value; cross-field rules are recorded under cross.",
        ["text-fixed:valid"] = "Fixed path fields admit exactly one value; alternatives are forced refusals.",
        ["text-optional:boundary"] = "Pattern text has no numeric boundary; absence/presence and length classes apply.",
        ["argv:order"] = "Argv order is significant data, not a canonical ordering constraint.",
        ["argv:duplicate"] = "Duplicate argv entries are explicitly valid; recorded under valid.",
        ["store-inode:tag"] = "Inode budget is a u64 value, not a tag.",
        ["bytes32:length"] = "Fixed 32-byte fields have no length prefix; short input is the truncation family (B6).",
        ["bytes32:utf8"] = "Raw digest bytes are not UTF-8 text.",
        ["u64:utf8"] = "Numeric fields are not UTF-8 text.",
        ["u32:utf8"] = "Numeric fields are not UTF-8 text.",
    };

    internal static readonly QualificationFieldRow[] Fields = BuildFields();

    internal static readonly QualificationCase[] Cases = GenerateCases();

    internal static readonly Dictionary<string, QualificationCase> ByCaseId =
        Cases.ToDictionary(item => item.CaseId, StringComparer.Ordinal);

    internal static string[] ApplicableClasses(string kind) =>
        Applicability.TryGetValue(kind, out var classes)
            ? classes
            : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown field kind.");

    internal static bool IsApplicable(string kind, string mutationClass) =>
        ApplicableClasses(kind).Contains(mutationClass, StringComparer.Ordinal);

    internal static string NotApplicableReason(string kind, string mutationClass)
    {
        if (IsApplicable(kind, mutationClass))
        {
            throw new InvalidOperationException($"{kind}:{mutationClass} is applicable.");
        }

        return NonApplicableReasons.TryGetValue($"{kind}:{mutationClass}", out var reason)
            ? reason
            : $"The {mutationClass} mutation class does not apply to a {kind} field.";
    }

    private static QualificationFieldRow[] BuildFields()
    {
        var fields = new List<QualificationFieldRow>
        {
            new(1, "RestrictedManifest.SchemaVersion", "schema"),
            new(2, "RestrictedManifest.ImplementationId", "implementation-id", Slot: "implementation"),
            new(3, "RestrictedManifest.Authority", "composite", ViewType: typeof(AuthorityManifestView), Subfields: new[]
            {
                "Namespaces", "Capabilities", "NoNewPrivs", "FilterSha256", "FilterLength",
                "ReadOnlyDestinations", "WritableStores", "NoAmbientEnvironment", "NoIpcEndpoints",
                "NoExtraInheritedFds", "MountPolicy", "NetworkQualification",
            }),
            new(4, "RestrictedManifest.Resources", "composite", ViewType: typeof(ResourceManifestView), Subfields: new[]
            {
                "Memory", "Swap", "Tasks", "FileDescriptors", "TmpBytes", "ShmBytes", "StdoutBytes",
                "StderrBytes", "CaptureAggregateBytes", "SetupTimeoutNs", "OverallTimeoutNs",
                "StopAllowanceNs", "FinalDrainNs", "InodeQualification", "CoreDumps", "OwnerAllocation",
            }),
            new(5, "RestrictedManifest.Closure", "composite", ViewType: typeof(ClosureIdentityView), Subfields: new[]
            {
                "Objects", "HelperDependencies",
            }),
            new(6, "RestrictedManifest.Invocation", "composite", ViewType: typeof(InvocationManifestView), Subfields: new[]
            {
                "ToolPath", "Mode", "InputPath", "Argv", "Environment", "WorkDirectory",
            }),
            new(7, "RestrictedManifest.Channels", "composite", ViewType: typeof(ChannelManifestView), Subfields: new[]
            {
                "StdinEof", "StdoutLimitBytes", "StderrLimitBytes", "AggregateLimitBytes",
            }),
            new(8, "AuthorityManifest.Namespaces", "collection-ordered",
                Slot: "namespaces.item[1]", CountSlot: "namespaces.count",
                ModelVariants: new[]
                {
                    new QualificationModelVariant("valid", (m) => QualificationModelTools.WithAuthority(m, "Namespaces", new[] { "alpha", "zeta" }), true),
                    new QualificationModelVariant("boundary-empty", (m) => QualificationModelTools.WithAuthority(m, "Namespaces", Array.Empty<string>()), false),
                    new QualificationModelVariant("boundary-multi", (m) => QualificationModelTools.WithAuthority(m, "Namespaces", new[] { "a", "b", "c" }), false),
                },
                DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                    m.Authority.Namespaces.ToArray(), v.Authority.Namespaces.ToArray()),
                OrderValue: "aaa", DuplicateValue: "ipc"),
            new(9, "AuthorityManifest.Capabilities", "collection-empty", Slot: "capabilities.count"),
            new(10, "AuthorityManifest.NoNewPrivs", "bool-free", Slot: "authority.noNewPrivs",
                ModelVariants: new[]
                {
                    new QualificationModelVariant("valid", (m) => QualificationModelTools.WithAuthority(m, "NoNewPrivs", false), true),
                },
                DomainCheck: (m, v) => Assert.Equal(m.Authority.NoNewPrivs, v.Authority.NoNewPrivs)),
            new(11, "AuthorityManifest.FilterSha256", "bytes32",
                ModelVariants: new[]
                {
                    new QualificationModelVariant("valid", (m) => QualificationModelTools.WithAuthority(m, "FilterSha256", QualificationModelTools.Diffusion32(0x77)), true),
                },
                DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                    m.Authority.FilterSha256.ToArray(), v.Authority.FilterSha256.ToArray())),
            new(12, "AuthorityManifest.FilterLength", "u64", Slot: "authority.filterLength",
                ModelVariants: new[]
                {
                    new QualificationModelVariant("valid", (m) => QualificationModelTools.WithAuthority(m, "FilterLength", 4096ul), true),
                    new QualificationModelVariant("boundary-min", (m) => QualificationModelTools.WithAuthority(m, "FilterLength", 0ul), false),
                    new QualificationModelVariant("boundary-max", (m) => QualificationModelTools.WithAuthority(m, "FilterLength", ulong.MaxValue), false),
                },
                DomainCheck: (m, v) => Assert.Equal(m.Authority.FilterLength, v.Authority.FilterLength)),
            new(13, "AuthorityManifest.ReadOnlyDestinations", "collection-ordered",
                Slot: "readOnly.item[1]", CountSlot: "readOnly.count",
                ModelVariants: new[]
                {
                    new QualificationModelVariant("valid", (m) => QualificationModelTools.WithAuthority(m, "ReadOnlyDestinations", new[] { "/a", "/z" }), true),
                    new QualificationModelVariant("boundary-empty", (m) => QualificationModelTools.WithAuthority(m, "ReadOnlyDestinations", Array.Empty<string>()), false),
                    new QualificationModelVariant("boundary-multi", (m) => QualificationModelTools.WithAuthority(m, "ReadOnlyDestinations", new[] { "/a", "/b", "/c" }), false),
                },
                DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                    m.Authority.ReadOnlyDestinations.ToArray(), v.Authority.ReadOnlyDestinations.ToArray()),
                OrderValue: "/aaa", DuplicateValue: "/coordinator"),
            new(14, "AuthorityManifest.WritableStores", "collection-ordered",
                Slot: "stores.item[1].path", CountSlot: "stores.count",
                ModelVariants: new[]
                {
                    new QualificationModelVariant("valid", (m) => QualificationModelTools.WithAuthority(m, "WritableStores", new[]
                    {
                        new WritableStore("/data", 1, true, 2),
                        new WritableStore("/tmp", 7340032, false, 0),
                    }), true),
                    new QualificationModelVariant("boundary-empty", (m) => QualificationModelTools.WithAuthority(m, "WritableStores", Array.Empty<WritableStore>()), false),
                    new QualificationModelVariant("boundary-multi", (m) => QualificationModelTools.WithAuthority(m, "WritableStores", new[]
                    {
                        new WritableStore("/a", 1, false, 0),
                        new WritableStore("/b", 2, false, 0),
                        new WritableStore("/c", 3, true, 4),
                    }), false),
                },
                DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                    m.Authority.WritableStores.ToArray(), v.Authority.WritableStores.ToArray()),
                OrderValue: "/aaa", DuplicateValue: "/dev/shm"),
            new(15, "AuthorityManifest.NoAmbientEnvironment", "bool-forced", Slot: "authority.noAmbientEnvironment"),
            new(16, "AuthorityManifest.NoIpcEndpoints", "bool-forced", Slot: "authority.noIpcEndpoints"),
            new(17, "AuthorityManifest.NoExtraInheritedFds", "bool-forced", Slot: "authority.noExtraFds"),
            new(18, "AuthorityManifest.MountPolicy", "composite",
                ViewType: typeof(ExecutableMountPolicyView), Subfields: new[] { "ToolExecutable", "ScratchNoexec" }),
            new(19, "AuthorityManifest.NetworkQualification", "enum-pending", Slot: "authority.networkQualification"),
        };

        var budgetIndex = 20;
        foreach (var name in QualificationWireTemplate.BudgetNames)
        {
            fields.Add(BudgetRow(budgetIndex, name));
            budgetIndex++;
        }

        fields.Add(new(33, "ResourceManifest.InodeQualification", "enum-pending", Slot: "resources.inodeQualification"));
        fields.Add(new(34, "ResourceManifest.CoreDumps", "composite",
            ViewType: typeof(CoreDumpPolicyView),
            Subfields: new[] { "CoreDisabled", "RuntimeDumpsDisabled", "CorePatternObserved" }));
        fields.Add(new(35, "ResourceManifest.OwnerAllocation", "composite",
            ViewType: typeof(OwnerAllocationLimitsView), Subfields: new[]
            {
                "StagingBytes", "StagingObjects", "ConfigBytes", "CoordinatorStatusBytes",
                "HelperStatusBytes", "JournalBytes", "ReceiptBytes",
            }));

        fields.Add(new(36, "ResourceBudget.Kind", "budget-kind", Slot: "budget.Memory.kind",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid-absent", (m) => QualificationModelTools.WithBudget(m, "Memory",
                    new ResourceBudget(InternalBudgetKind.Absent, 0, "bytes", InternalQualification.Mandatory)), true),
                new QualificationModelVariant("valid-unlimited", (m) => QualificationModelTools.WithBudget(m, "Memory",
                    new ResourceBudget(InternalBudgetKind.Unlimited, 0, "bytes", InternalQualification.Mandatory)), true),
            },
            DomainCheck: (m, v) => Assert.Equal((byte)m.Resources.Memory.Kind, (byte)v.Resources.Memory.Kind)));
        fields.Add(new(37, "ResourceBudget.Value", "u64", Slot: "budget.Memory.value",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithBudget(m, "Memory",
                    new ResourceBudget(InternalBudgetKind.Exact, 4096, "bytes", InternalQualification.Mandatory)), true),
                new QualificationModelVariant("boundary-min", (m) => QualificationModelTools.WithBudget(m, "Memory",
                    new ResourceBudget(InternalBudgetKind.Exact, 0, "bytes", InternalQualification.Mandatory)), false),
                new QualificationModelVariant("boundary-max", (m) => QualificationModelTools.WithBudget(m, "Memory",
                    new ResourceBudget(InternalBudgetKind.Exact, ulong.MaxValue, "bytes", InternalQualification.Mandatory)), false),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Resources.Memory.Value, v.Resources.Memory.Value)));
        fields.Add(new(38, "ResourceBudget.Unit", "text-free", Slot: "budget.Memory.unit",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithBudget(m, "Memory",
                    new ResourceBudget(InternalBudgetKind.Exact, 134217728, "words", InternalQualification.Mandatory)), true),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Resources.Memory.Unit, v.Resources.Memory.Unit)));
        fields.Add(new(39, "ResourceBudget.Qualification", "enum-free", Slot: "budget.Memory.qualification",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithBudget(m, "Memory",
                    new ResourceBudget(InternalBudgetKind.Exact, 134217728, "bytes", InternalQualification.PendingQualification)), true),
            },
            DomainCheck: (m, v) => Assert.Equal(
                (byte)m.Resources.Memory.Qualification, (byte)v.Resources.Memory.Qualification)));
        fields.Add(new(40, "WritableStore.Path", "text-free", Slot: "stores.item[0].path",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetStore(m,
                    store => new WritableStore("/data", store.Bytes, store.HasInodeBudget, store.InodeBudget)), true),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Authority.WritableStores[0].Path, v.Authority.WritableStores[0].Path)));
        fields.Add(new(41, "WritableStore.Bytes", "u64", Slot: "stores.item[0].bytes",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetStore(m,
                    store => new WritableStore(store.Path, 42, store.HasInodeBudget, store.InodeBudget)), true),
                new QualificationModelVariant("boundary-min", (m) => SetStore(m,
                    store => new WritableStore(store.Path, 0, store.HasInodeBudget, store.InodeBudget)), false),
                new QualificationModelVariant("boundary-max", (m) => SetStore(m,
                    store => new WritableStore(store.Path, ulong.MaxValue, store.HasInodeBudget, store.InodeBudget)), false),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Authority.WritableStores[0].Bytes, v.Authority.WritableStores[0].Bytes)));
        fields.Add(new(42, "WritableStore.HasInodeBudget", "bool-free", Slot: "stores.item[0].hasInode",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetStore(m,
                    store => new WritableStore(store.Path, store.Bytes, true, store.InodeBudget)), true),
            },
            DomainCheck: (m, v) => Assert.Equal(
                m.Authority.WritableStores[0].HasInodeBudget, v.Authority.WritableStores[0].HasInodeBudget)));
        fields.Add(new(43, "WritableStore.InodeBudget", "store-inode", Slot: "stores.item[0].inode",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetStore(m,
                    store => new WritableStore(store.Path, store.Bytes, true, 1024)), true),
                new QualificationModelVariant("boundary-min", (m) => SetStore(m,
                    store => new WritableStore(store.Path, store.Bytes, true, 0)), false),
                new QualificationModelVariant("boundary-max", (m) => SetStore(m,
                    store => new WritableStore(store.Path, store.Bytes, true, ulong.MaxValue)), false),
            },
            DomainCheck: (m, v) => Assert.Equal(
                m.Authority.WritableStores[0].InodeBudget, v.Authority.WritableStores[0].InodeBudget)));
        fields.Add(new(44, "ExecutableMountPolicy.ToolExecutable", "bool-forced", Slot: "mount.toolExecutable"));
        fields.Add(new(45, "ExecutableMountPolicy.ScratchNoexec", "bool-free", Slot: "mount.scratchNoexec",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithAuthority(m, "MountPolicy",
                    new ExecutableMountPolicy(true, true)), true),
            },
            DomainCheck: (m, v) => Assert.Equal(
                m.Authority.MountPolicy.ScratchNoexec, v.Authority.MountPolicy.ScratchNoexec)));
        fields.Add(new(46, "CoreDumpPolicy.CoreDisabled", "bool-forced", Slot: "core.disabled"));
        fields.Add(new(47, "CoreDumpPolicy.RuntimeDumpsDisabled", "bool-free", Slot: "core.runtimeDumpsDisabled",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithResources(m, "CoreDumps",
                    new CoreDumpPolicy(true, true, null)), true),
            },
            DomainCheck: (m, v) => Assert.Equal(
                m.Resources.CoreDumps.RuntimeDumpsDisabled, v.Resources.CoreDumps.RuntimeDumpsDisabled)));
        fields.Add(new(48, "CoreDumpPolicy.CorePatternObserved", "text-optional", Slot: "core.pattern",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetPattern(m, "core.%p"), true),
                new QualificationModelVariant("presence-absent", (m) => SetPattern(m, null), false),
                new QualificationModelVariant("presence-present", (m) => SetPattern(m, "core.%e"), true),
            },
            DomainCheck: (m, v) => Assert.Equal(
                m.Resources.CoreDumps.CorePatternObserved, v.Resources.CoreDumps.CorePatternObserved)));

        fields.Add(OwnerRow(49, "StagingBytes", true));
        fields.Add(OwnerRow(50, "StagingObjects", false));
        fields.Add(OwnerRow(51, "ConfigBytes", true));
        fields.Add(OwnerRow(52, "CoordinatorStatusBytes", true));
        fields.Add(OwnerRow(53, "HelperStatusBytes", true));
        fields.Add(OwnerRow(54, "JournalBytes", true));
        fields.Add(OwnerRow(55, "ReceiptBytes", true));

        fields.Add(new(56, "ClosureIdentity.Objects", "collection-ordered",
            Slot: "objects.item[1].role", CountSlot: "objects.count",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithClosure(m, "Objects", new[]
                {
                    new ClosureObject("app", QualificationModelTools.Bytes32(0x20), 200, "/app", "dotnet-publish"),
                    new ClosureObject("cache", QualificationModelTools.Bytes32(0x21), 1, "/cache", "cache-id"),
                    new ClosureObject("input", QualificationModelTools.Bytes32(0x30), 300, "/input", "copyin"),
                    new ClosureObject("tool", QualificationModelTools.Bytes32(0x10), 100, "/tool", "gcc-static"),
                }), true),
                new QualificationModelVariant("boundary-empty", (m) => QualificationModelTools.WithClosure(m, "Objects", Array.Empty<ClosureObject>()), false),
                new QualificationModelVariant("boundary-single", (m) => QualificationModelTools.WithClosure(m, "Objects", new[]
                {
                    new ClosureObject("tool", QualificationModelTools.Bytes32(0x10), 100, "/tool", "gcc-static"),
                }), false),
            },
            DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                m.Closure.Objects.ToArray(), v.Closure.Objects.ToArray()),
            OrderValue: "aaa", DuplicateValue: "app"));
        fields.Add(new(57, "ClosureIdentity.HelperDependencies", "collection-ordered",
            Slot: "helpers.item[1]", CountSlot: "helpers.count",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithClosure(m, "HelperDependencies", new[] { "dep-a", "dep-z" }), true),
                new QualificationModelVariant("boundary-empty", (m) => QualificationModelTools.WithClosure(m, "HelperDependencies", Array.Empty<string>()), false),
                new QualificationModelVariant("boundary-single", (m) => QualificationModelTools.WithClosure(m, "HelperDependencies", new[] { "dep" }), false),
            },
            DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                m.Closure.HelperDependencies.ToArray(), v.Closure.HelperDependencies.ToArray()),
            OrderValue: "a", DuplicateValue: "a-dep"));
        fields.Add(new(58, "ClosureObject.Role", "text-free", Slot: "objects.item[0].role",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetObject(m,
                    item => new ClosureObject("app2", item.Sha256.ToArray(), item.Length, item.Destination, item.BuildIdentity)), true),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Closure.Objects[0].Role, v.Closure.Objects[0].Role)));
        fields.Add(new(59, "ClosureObject.Sha256", "bytes32",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetObject(m,
                    item => new ClosureObject(item.Role, QualificationModelTools.Diffusion32(0x55), item.Length, item.Destination, item.BuildIdentity)), true),
            },
            DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                m.Closure.Objects[0].Sha256.ToArray(), v.Closure.Objects[0].Sha256.ToArray())));
        fields.Add(new(60, "ClosureObject.Length", "u64", Slot: "objects.item[0].length",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetObject(m,
                    item => new ClosureObject(item.Role, item.Sha256.ToArray(), 42, item.Destination, item.BuildIdentity)), true),
                new QualificationModelVariant("boundary-min", (m) => SetObject(m,
                    item => new ClosureObject(item.Role, item.Sha256.ToArray(), 0, item.Destination, item.BuildIdentity)), false),
                new QualificationModelVariant("boundary-max", (m) => SetObject(m,
                    item => new ClosureObject(item.Role, item.Sha256.ToArray(), ulong.MaxValue, item.Destination, item.BuildIdentity)), false),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Closure.Objects[0].Length, v.Closure.Objects[0].Length)));
        fields.Add(new(61, "ClosureObject.Destination", "text-free", Slot: "objects.item[0].destination",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetObject(m,
                    item => new ClosureObject(item.Role, item.Sha256.ToArray(), item.Length, "/app2", item.BuildIdentity)), true),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Closure.Objects[0].Destination, v.Closure.Objects[0].Destination)));
        fields.Add(new(62, "ClosureObject.BuildIdentity", "text-free", Slot: "objects.item[0].buildIdentity",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => SetObject(m,
                    item => new ClosureObject(item.Role, item.Sha256.ToArray(), item.Length, item.Destination, "dotnet-publish-2")), true),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Closure.Objects[0].BuildIdentity, v.Closure.Objects[0].BuildIdentity)));
        fields.Add(new(63, "InvocationManifest.ToolPath", "text-fixed", Slot: "invocation.toolPath"));
        fields.Add(new(64, "InvocationManifest.Mode", "text-free", Slot: "invocation.mode",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithInvocation(m, "Mode", "other"), true),
            },
            DomainCheck: (m, v) => Assert.Equal(m.Invocation.Mode, v.Invocation.Mode)));
        fields.Add(new(65, "InvocationManifest.InputPath", "text-fixed", Slot: "invocation.inputPath"));
        fields.Add(new(66, "InvocationManifest.Argv", "argv", Slot: "argv.item[1]", CountSlot: "argv.count",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithInvocation(m, "Argv", new[] { "/input", "--in" }), true),
                new QualificationModelVariant("boundary-empty", (m) => QualificationModelTools.WithInvocation(m, "Argv", Array.Empty<string>()), false),
                new QualificationModelVariant("boundary-multi", (m) => QualificationModelTools.WithInvocation(m, "Argv", new[] { "a", "b", "c" }), false),
            },
            DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                m.Invocation.Argv.ToArray(), v.Invocation.Argv.ToArray())));
        fields.Add(new(67, "InvocationManifest.Environment", "env", Slot: "env.key[1]", CountSlot: "env.count",
            ModelVariants: new[]
            {
                new QualificationModelVariant("valid", (m) => QualificationModelTools.WithInvocation(m, "Environment", new[]
                {
                    new KeyValuePair<string, string>("K", string.Empty),
                    new KeyValuePair<string, string>("LANG", "C"),
                }), true),
                new QualificationModelVariant("boundary-empty", (m) => QualificationModelTools.WithInvocation(m, "Environment",
                    Array.Empty<KeyValuePair<string, string>>()), false),
                new QualificationModelVariant("boundary-multi", (m) => QualificationModelTools.WithInvocation(m, "Environment", new[]
                {
                    new KeyValuePair<string, string>("A", "1"),
                    new KeyValuePair<string, string>("B", "2"),
                    new KeyValuePair<string, string>("C", "3"),
                }), false),
            },
            DomainCheck: (m, v) => QualificationModelTools.AssertGraph(
                m.Invocation.Environment.ToArray(), v.Invocation.Environment.ToArray()),
            OrderValue: "AAA", DuplicateValue: "HOME"));
        fields.Add(new(68, "InvocationManifest.WorkDirectory", "text-fixed", Slot: "invocation.workDirectory"));
        fields.Add(new(69, "ChannelManifest.StdinEof", "bool-forced", Slot: "channels.stdinEof"));
        fields.Add(ChannelRow(70, "StdoutLimitBytes", "channels.stdoutLimit", 1));
        fields.Add(ChannelRow(71, "StderrLimitBytes", "channels.stderrLimit", 2));
        fields.Add(ChannelRow(72, "AggregateLimitBytes", "channels.aggregateLimit", 3));

        return fields.ToArray();
    }

    private static QualificationFieldRow BudgetRow(int index, string name) => new(
        index,
        $"ResourceManifest.{name}",
        "budget",
        Slot: $"budget.{name}.kind",
        BudgetName: name);

    private static QualificationFieldRow OwnerRow(int index, string property, bool isU64) => new(
        index,
        $"OwnerAllocationLimits.{property}",
        isU64 ? "u64" : "u32",
        Slot: $"owner.{char.ToLowerInvariant(property[0])}{property[1..]}",
        ModelVariants: isU64
            ? new[]
            {
                new QualificationModelVariant("valid", (m) => SetOwner(m, property, 42ul), true),
                new QualificationModelVariant("boundary-min", (m) => SetOwner(m, property, 0ul), false),
                new QualificationModelVariant("boundary-max", (m) => SetOwner(m, property, ulong.MaxValue), false),
            }
            : new[]
            {
                new QualificationModelVariant("valid", (m) => SetOwner(m, property, 42u), true),
                new QualificationModelVariant("boundary-min", (m) => SetOwner(m, property, 0u), false),
                new QualificationModelVariant("boundary-max", (m) => SetOwner(m, property, uint.MaxValue), false),
            },
        DomainCheck: (m, v) => Assert.Equal(
            m.Resources.OwnerAllocation.GetType().GetProperty(property)!.GetValue(m.Resources.OwnerAllocation),
            v.Resources.OwnerAllocation.GetType().GetProperty(property)!.GetValue(v.Resources.OwnerAllocation)));

    private static QualificationFieldRow ChannelRow(int index, string property, string slot, ulong valid) => new(
        index,
        $"ChannelManifest.{property}",
        "u64",
        Slot: slot,
        ModelVariants: new[]
        {
            new QualificationModelVariant("valid", (m) => QualificationModelTools.WithChannels(m, property, valid), true),
            new QualificationModelVariant("boundary-min", (m) => QualificationModelTools.WithChannels(m, property, 0ul), false),
            new QualificationModelVariant("boundary-max", (m) => QualificationModelTools.WithChannels(m, property, ulong.MaxValue), false),
        },
        DomainCheck: (m, v) => Assert.Equal(
            (ulong)m.Channels.GetType().GetProperty(property)!.GetValue(m.Channels)!,
            (ulong)v.Channels.GetType().GetProperty(property)!.GetValue(v.Channels)!));

    private static ResourceBudget BudgetOf(RestrictedManifest model, string name) => name switch
    {
        "Memory" => model.Resources.Memory,
        "Swap" => model.Resources.Swap,
        "Tasks" => model.Resources.Tasks,
        "FileDescriptors" => model.Resources.FileDescriptors,
        "TmpBytes" => model.Resources.TmpBytes,
        "ShmBytes" => model.Resources.ShmBytes,
        "StdoutBytes" => model.Resources.StdoutBytes,
        "StderrBytes" => model.Resources.StderrBytes,
        "CaptureAggregateBytes" => model.Resources.CaptureAggregateBytes,
        "SetupTimeoutNs" => model.Resources.SetupTimeoutNs,
        "OverallTimeoutNs" => model.Resources.OverallTimeoutNs,
        "StopAllowanceNs" => model.Resources.StopAllowanceNs,
        "FinalDrainNs" => model.Resources.FinalDrainNs,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown budget."),
    };

    private static RestrictedManifest SetStore(RestrictedManifest model, Func<WritableStore, WritableStore> mutate)
    {
        var stores = model.Authority.WritableStores.ToArray();
        stores[0] = mutate(stores[0]);
        return QualificationModelTools.WithAuthority(model, "WritableStores", stores);
    }

    private static RestrictedManifest SetObject(RestrictedManifest model, Func<ClosureObject, ClosureObject> mutate)
    {
        var objects = model.Closure.Objects.ToArray();
        objects[0] = mutate(objects[0]);
        return QualificationModelTools.WithClosure(model, "Objects", objects);
    }

    private static RestrictedManifest SetOwner(RestrictedManifest model, string property, object value) =>
        QualificationModelTools.WithResources(model, "OwnerAllocation",
            QualificationModelTools.Replace(model.Resources.OwnerAllocation, property, value));

    private static RestrictedManifest SetPattern(RestrictedManifest model, string? pattern) =>
        QualificationModelTools.WithResources(model, "CoreDumps",
            new CoreDumpPolicy(model.Resources.CoreDumps.CoreDisabled,
                model.Resources.CoreDumps.RuntimeDumpsDisabled, pattern));

    private static QualificationCase[] GenerateCases()
    {
        var cases = new List<QualificationCase>();
        foreach (var field in Fields)
        {
            GenerateFieldCases(cases, field);
        }

        return cases.ToArray();
    }

    private static void GenerateFieldCases(List<QualificationCase> cases, QualificationFieldRow field)
    {
        var number = field.Index;
        string Id(string suffix) => $"Q2A-F-{number:00}-{suffix}";
        var baseline = new Func<RestrictedManifest>(QualificationModelTools.Baseline);

        if (field.ModelVariants is { Length: > 0 })
        {
            foreach (var variant in field.ModelVariants)
            {
                var mutationClass = variant.IdSuffix.StartsWith("boundary", StringComparison.Ordinal)
                    ? "boundary"
                    : variant.IdSuffix.StartsWith("presence", StringComparison.Ordinal) ? "presence" : "valid";
                cases.Add(new VariantMutationCase(
                    Id(variant.IdSuffix), field.FieldId, mutationClass,
                    $"{mutationClass} mutation '{variant.IdSuffix}' is accepted, re-encodes identically and is visible in the decoded view",
                    baseline, variant.Mutate, variant.RequireChange,
                    field.DomainCheck ?? ((_, _) => { })));
            }
        }

        switch (field.Kind)
        {
            case "schema":
                cases.Add(new ActionCase(
                    Id("supported-id"), field.FieldId, "supported-id", "malformed",
                    "Schema 1 is accepted; schemas 0, 2 and 65,535 refuse UnsupportedSchema at offset zero",
                    () =>
                    {
                        var golden = QualificationModelTools.GoldenBytes();
                        Assert.True(RestrictedManifestCodec.Supports(1, RestrictedManifest.ImplementationIdentifier));
                        Assert.Null(RestrictedManifestCodec.Decode(golden).Error);
                        foreach (var schema in new ushort[] { 0, 2, 65535 })
                        {
                            var template = QualificationWireTemplate.Golden();
                            template.SchemaVersion = schema;
                            var built = template.Build();
                            var result = RestrictedManifestCodec.Decode(built.Bytes);
                            Assert.Null(result.Value);
                            Assert.Equal(ManifestDecodeError.UnsupportedSchema, result.Error);
                            Assert.Equal(built.Offsets["schema"], result.Offset);
                            Assert.Equal(0, result.Offset);
                        }
                    }));
                break;
            case "implementation-id":
                cases.Add(new ActionCase(
                    Id("supported-id"), field.FieldId, "supported-id", "malformed",
                    "Exact implementation ID accepted; whitespace, case-variant, trailing-NUL and unknown IDs refuse UnsupportedImplementation at offset two; empty refuses InvalidValue at offset two",
                    () =>
                    {
                        var golden = QualificationModelTools.GoldenBytes();
                        Assert.True(RestrictedManifestCodec.Supports(1, RestrictedManifest.ImplementationIdentifier));
                        Assert.Null(RestrictedManifestCodec.Decode(golden).Error);
                        foreach (var (value, code) in new[]
                        {
                            (string.Empty, ManifestDecodeError.InvalidValue),
                            (" ", ManifestDecodeError.UnsupportedImplementation),
                            ("Linux-m1-native-transaction", ManifestDecodeError.UnsupportedImplementation),
                            (RestrictedManifest.ImplementationIdentifier + "\0", ManifestDecodeError.UnsupportedImplementation),
                            ("other-profile", ManifestDecodeError.UnsupportedImplementation),
                        })
                        {
                            var template = QualificationWireTemplate.Golden();
                            template.ImplementationId = value;
                            var built = template.Build();
                            var result = RestrictedManifestCodec.Decode(built.Bytes);
                            Assert.Null(result.Value);
                            Assert.Equal(code, result.Error);
                            Assert.Equal(2, built.Offsets["implementation"]);
                            Assert.Equal(built.Offsets["implementation"], result.Offset);
                        }
                    }));
                cases.Add(QualificationRefusalFactories.MalformedUtf8(
                    Id("utf8"), field.FieldId, "implementation", "c0af",
                    "Malformed implementation bytes refuse InvalidUtf8 at offset two"));
                cases.Add(QualificationRefusalFactories.LengthInflation(
                    Id("length"), field.FieldId, "implementation",
                    "Inflated implementation length refuses InvalidLengthOrCount at offset two"));
                cases.Add(new MisuseCase(
                    Id("misuse"), field.FieldId,
                    "Supports throws ArgumentNullException for a null implementation ID at supported and unsupported schemas",
                    () =>
                    {
                        _ = RestrictedManifestCodec.Supports(1, null!);
                        _ = RestrictedManifestCodec.Supports(0, null!);
                        _ = RestrictedManifestCodec.Supports(65535, null!);
                        _ = RestrictedManifestCodec.Supports(2, null!);
                    }));
                break;
            case "composite":
                cases.Add(new CompositeTraceCase(
                    Id("trace"), field.FieldId,
                    $"Public view property inventory for {field.FieldId} equals the committed field-map subfields",
                    field.ViewType!, field.Subfields!));
                break;
            case "bool-free":
                cases.Add(QualificationRefusalFactories.Tag(
                    Id("tag"), field.FieldId, field.Slot, 2,
                    "Boolean tag above one refuses InvalidTag at the field start"));
                break;
            case "bool-forced":
                cases.Add(QualificationRefusalFactories.Tag(
                    Id("tag"), field.FieldId, field.Slot, 2,
                    "Boolean tag above one refuses InvalidTag at the field start"));
                cases.Add(QualificationRefusalFactories.ForcedBoolFalse(
                    Id("forced"), field.FieldId, field.Slot,
                    "Forced-true value false refuses InvalidValue at the field start"));
                break;
            case "enum-free":
                cases.Add(QualificationRefusalFactories.Tag(
                    Id("tag"), field.FieldId, field.Slot, 2,
                    "Qualification tag above one refuses InvalidTag at the field start"));
                break;
            case "enum-pending":
                cases.Add(QualificationRefusalFactories.Tag(
                    Id("tag"), field.FieldId, field.Slot, 2,
                    "Qualification tag above one refuses InvalidTag at the field start"));
                cases.Add(QualificationRefusalFactories.ForcedBoolFalse(
                    Id("forced"), field.FieldId, field.Slot,
                    "Mandatory (non-pending) qualification refuses InvalidValue at the field start"));
                break;
            case "budget-kind":
                cases.Add(QualificationRefusalFactories.Tag(
                    Id("tag"), field.FieldId, field.Slot, 3,
                    "Budget kind tag above two refuses InvalidTag at the budget start"));
                cases.Add(QualificationRefusalFactories.Variant(
                    Id("cross"), field.FieldId, "cross",
                    "Absent budget kind with a nonzero value refuses InvalidValue at the budget start",
                    () =>
                    {
                        var template = QualificationWireTemplate.Golden();
                        QualificationWireTemplate.SetBudget(template, "Memory", InternalBudgetKind.Absent, 5, "bytes");
                        return template;
                    },
                    "budget.Memory.kind", ManifestDecodeError.InvalidValue));
                cases.Add(QualificationRefusalFactories.Variant(
                    Id("cross-unlimited"), field.FieldId, "cross",
                    "Unlimited budget kind with a nonzero value refuses InvalidValue at the budget start",
                    () =>
                    {
                        var template = QualificationWireTemplate.Golden();
                        QualificationWireTemplate.SetBudget(template, "Memory", InternalBudgetKind.Unlimited, 5, "bytes");
                        return template;
                    },
                    "budget.Memory.kind", ManifestDecodeError.InvalidValue));
                break;
            case "u64":
            case "u32":
            case "bytes32":
                break; // covered by ModelVariants
            case "text-free":
                cases.Add(QualificationRefusalFactories.MalformedUtf8(
                    Id("utf8"), field.FieldId, field.Slot, "c0af",
                    "Malformed UTF-8 refuses InvalidUtf8 at the field start"));
                cases.Add(QualificationRefusalFactories.LengthInflation(
                    Id("length"), field.FieldId, field.Slot,
                    "Inflated declared length refuses InvalidLengthOrCount at the field start"));
                cases.Add(QualificationRefusalFactories.EmptyText(
                    Id("empty"), field.FieldId, field.Slot,
                    "Empty non-allowed text refuses InvalidValue at the field start"));
                break;
            case "text-fixed":
                cases.Add(QualificationRefusalFactories.WrongFixedText(
                    Id("forced"), field.FieldId, field.Slot, "/wrong",
                    "Wrong fixed path value refuses InvalidValue at the field start"));
                cases.Add(QualificationRefusalFactories.MalformedUtf8(
                    Id("utf8"), field.FieldId, field.Slot, "c0af",
                    "Malformed UTF-8 refuses InvalidUtf8 at the field start"));
                cases.Add(QualificationRefusalFactories.LengthInflation(
                    Id("length"), field.FieldId, field.Slot,
                    "Inflated declared length refuses InvalidLengthOrCount at the field start"));
                break;
            case "text-optional":
                cases.Add(QualificationRefusalFactories.Tag(
                    Id("tag"), field.FieldId, "core.patternPresent", 2,
                    "Core-pattern presence tag above one refuses InvalidTag at the presence byte"));
                cases.Add(QualificationRefusalFactories.MalformedUtf8(
                    Id("utf8"), field.FieldId, field.Slot, "c0af",
                    "Malformed core-pattern UTF-8 refuses InvalidUtf8 at the field start",
                    PatternTemplate));
                cases.Add(QualificationRefusalFactories.LengthInflation(
                    Id("length"), field.FieldId, field.Slot,
                    "Inflated core-pattern length refuses InvalidLengthOrCount at the field start",
                    PatternTemplate));
                cases.Add(QualificationRefusalFactories.EmptyText(
                    Id("empty"), field.FieldId, field.Slot,
                    "Empty observed core pattern refuses InvalidValue at the field start",
                    PatternTemplate));
                break;
            case "collection-ordered":
                cases.Add(QualificationRefusalFactories.Order(
                    Id("order"), field.FieldId, field.Slot, field.OrderValue!,
                    "Out-of-order set entry refuses NonCanonicalOrder at the second item start"));
                cases.Add(QualificationRefusalFactories.Duplicate(
                    Id("duplicate"), field.FieldId, field.Slot, field.DuplicateValue!,
                    "Adjacent duplicate set entry refuses Duplicate at the second item start"));
                cases.Add(QualificationRefusalFactories.LengthInflation(
                    Id("length"), field.FieldId, field.CountSlot,
                    "Inflated count refuses InvalidLengthOrCount at the count start"));
                cases.Add(QualificationRefusalFactories.MalformedUtf8(
                    Id("utf8"), field.FieldId, field.Slot, "c0af",
                    "Malformed UTF-8 in a set item refuses InvalidUtf8 at the item start"));
                cases.Add(QualificationRefusalFactories.EmptyText(
                    Id("empty"), field.FieldId, field.Slot,
                    "Empty set item refuses InvalidValue at the item start"));
                break;
            case "collection-empty":
                cases.Add(QualificationRefusalFactories.LengthInflation(
                    Id("length"), field.FieldId, field.Slot,
                    "Inflated capability count refuses InvalidLengthOrCount at the count start"));
                cases.Add(QualificationRefusalFactories.Variant(
                    Id("forced"), field.FieldId, "forced",
                    "Any capability entry refuses InvalidValue at the capability count start",
                    () =>
                    {
                        var template = QualificationWireTemplate.Golden();
                        template.Capabilities = new[] { "cap" };
                        return template;
                    },
                    "capabilities.count", ManifestDecodeError.InvalidValue));
                break;
            case "argv":
                cases.Add(QualificationRefusalFactories.LengthInflation(
                    Id("length"), field.FieldId, field.CountSlot,
                    "Inflated argv count refuses InvalidLengthOrCount at the count start"));
                cases.Add(QualificationRefusalFactories.MalformedUtf8(
                    Id("utf8"), field.FieldId, field.Slot, "c0af",
                    "Malformed UTF-8 in an argv item refuses InvalidUtf8 at the item start"));
                cases.Add(QualificationRefusalFactories.EmptyText(
                    Id("empty"), field.FieldId, field.Slot,
                    "Empty argv item refuses InvalidValue at the item start"));
                break;
            case "env":
                cases.Add(QualificationRefusalFactories.Order(
                    Id("order"), field.FieldId, field.Slot, field.OrderValue!,
                    "Out-of-order environment key refuses NonCanonicalOrder at the second key start"));
                cases.Add(QualificationRefusalFactories.Duplicate(
                    Id("duplicate"), field.FieldId, field.Slot, field.DuplicateValue!,
                    "Adjacent duplicate environment key refuses Duplicate at the second key start"));
                cases.Add(QualificationRefusalFactories.LengthInflation(
                    Id("length"), field.FieldId, field.CountSlot,
                    "Inflated environment count refuses InvalidLengthOrCount at the count start"));
                cases.Add(QualificationRefusalFactories.MalformedUtf8(
                    Id("utf8"), field.FieldId, field.Slot, "c0af",
                    "Malformed UTF-8 in an environment key refuses InvalidUtf8 at the key start"));
                cases.Add(QualificationRefusalFactories.EmptyText(
                    Id("empty"), field.FieldId, field.Slot,
                    "Empty environment key refuses InvalidValue at the key start"));
                break;
            case "budget":
                AddBudgetCases(cases, field, Id);
                break;
            case "store-inode":
                cases.Add(QualificationRefusalFactories.Variant(
                    Id("cross"), field.FieldId, "cross",
                    "Inode budget absent with a nonzero value refuses InvalidValue at the inode budget start",
                    () =>
                    {
                        var template = QualificationWireTemplate.Golden();
                        template.Stores = new[]
                        {
                            new QualificationWireStore { Path = "/tmp", Bytes = 1, HasInodeBudget = false, InodeBudget = 5 },
                        };
                        return template;
                    },
                    "stores.item[0].inode", ManifestDecodeError.InvalidValue));
                break;
        }
    }

    private static void AddBudgetCases(
        List<QualificationCase> cases,
        QualificationFieldRow field,
        Func<string, string> id)
    {
        var name = field.BudgetName;
        var baseline = new Func<RestrictedManifest>(QualificationModelTools.Baseline);
        void Add(string suffix, InternalBudgetKind kind, ulong value, bool requireChange)
        {
            cases.Add(new VariantMutationCase(
                id(suffix), field.FieldId,
                suffix.StartsWith("boundary", StringComparison.Ordinal)
                    ? "boundary"
                    : suffix.StartsWith("presence", StringComparison.Ordinal) ? "presence" : "valid",
                $"{suffix} budget {name} is accepted, re-encodes identically and is visible in the decoded view",
                baseline,
                model => QualificationModelTools.WithBudget(model, name,
                    new ResourceBudget(kind, value, "bytes", InternalQualification.Mandatory)),
                requireChange,
                (model, view) =>
                {
                    var expected = BudgetOf(model, name);
                    var actual = QualificationModelTools.ViewBudget(view.Resources, name);
                    Assert.Equal((byte)expected.Kind, (byte)actual.Kind);
                    Assert.Equal(expected.Value, actual.Value);
                }));
        }

        Add("presence-absent", InternalBudgetKind.Absent, 0, false);
        Add("presence-exact-zero", InternalBudgetKind.Exact, 0, false);
        Add("presence-unlimited", InternalBudgetKind.Unlimited, 0, false);
        Add("valid", InternalBudgetKind.Exact, 4096, true);
        Add("boundary-min", InternalBudgetKind.Exact, 0, false);
        Add("boundary-max", InternalBudgetKind.Exact, ulong.MaxValue, false);

        cases.Add(QualificationRefusalFactories.Variant(
            id("cross"), field.FieldId, "cross",
            $"Budget {name} with a non-Exact kind and nonzero value refuses InvalidValue at the budget start",
            () =>
            {
                var template = QualificationWireTemplate.Golden();
                QualificationWireTemplate.SetBudget(template, name, InternalBudgetKind.Absent, 5, template.Budget(name).Unit);
                return template;
            },
            $"budget.{name}.kind", ManifestDecodeError.InvalidValue));
    }

    private static QualificationWireTemplate PatternTemplate()
    {
        var template = QualificationWireTemplate.Golden();
        template.CorePatternObserved = "core.%p";
        return template;
    }
}
