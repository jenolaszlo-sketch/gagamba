// RC2A-02A qualification wire template (test-only). A raw, parameterized
// reconstruction of the schema-1 canonical grammar with byte-offset recording.
// It is used to (a) independently rebuild the committed 873-byte golden literal,
// (b) construct canonical and deliberately malformed qualification inputs with
// construction-derived offsets, and (c) supply the committed expected graph
// values for decode verification. It never calls production codec code.
using Gagamba.Execution.Restricted;

namespace Gagamba.Execution.Linux.M1.Tests.Qualification;

internal sealed class QualificationWireBudget
{
    internal BudgetValueKind Kind { get; set; } = BudgetValueKind.Exact;
    internal ulong Value { get; set; }
    internal string Unit { get; set; } = "bytes";
    internal QualificationDisposition Qualification { get; set; } = QualificationDisposition.Mandatory;
}

internal sealed class QualificationWireStore
{
    internal string Path { get; set; } = "/tmp";
    internal ulong Bytes { get; set; }
    internal bool HasInodeBudget { get; set; }
    internal ulong InodeBudget { get; set; }
}

internal sealed class QualificationWireClosureObject
{
    internal string Role { get; set; } = "tool";
    internal byte[] Sha256 { get; set; } = new byte[32];
    internal ulong Length { get; set; }
    internal string Destination { get; set; } = "/tool";
    internal string BuildIdentity { get; set; } = "identity";
}

internal sealed class QualificationWireEnvPair
{
    internal string Key { get; set; } = "K";
    internal string Value { get; set; } = "V";
}

internal sealed class QualificationWireTemplate
{
    internal static readonly string[] BudgetNames =
    {
        "Memory", "Swap", "Tasks", "FileDescriptors", "TmpBytes", "ShmBytes",
        "StdoutBytes", "StderrBytes", "CaptureAggregateBytes", "SetupTimeoutNs",
        "OverallTimeoutNs", "StopAllowanceNs", "FinalDrainNs",
    };

    internal ushort SchemaVersion { get; set; } = 1;
    internal string ImplementationId { get; set; } = RestrictedManifest.ImplementationIdentifier;
    internal string[] Namespaces { get; set; } = Array.Empty<string>();
    internal string[] Capabilities { get; set; } = Array.Empty<string>();
    internal bool NoNewPrivs { get; set; } = true;
    internal byte[] FilterSha256 { get; set; } = new byte[32];
    internal ulong FilterLength { get; set; }
    internal string[] ReadOnlyDestinations { get; set; } = Array.Empty<string>();
    internal QualificationWireStore[] Stores { get; set; } = Array.Empty<QualificationWireStore>();
    internal bool NoAmbientEnvironment { get; set; } = true;
    internal bool NoIpcEndpoints { get; set; } = true;
    internal bool NoExtraInheritedFds { get; set; } = true;
    internal bool ToolExecutable { get; set; } = true;
    internal bool ScratchNoexec { get; set; }
    internal QualificationDisposition NetworkQualification { get; set; } = QualificationDisposition.PendingQualification;
    internal Dictionary<string, QualificationWireBudget> Budgets { get; set; } = NewBudgets();
    internal QualificationDisposition InodeQualification { get; set; } = QualificationDisposition.PendingQualification;
    internal bool CoreDisabled { get; set; } = true;
    internal bool RuntimeDumpsDisabled { get; set; }
    internal string? CorePatternObserved { get; set; }
    internal ulong OwnerStagingBytes { get; set; }
    internal uint OwnerStagingObjects { get; set; }
    internal ulong OwnerConfigBytes { get; set; }
    internal ulong OwnerCoordinatorStatusBytes { get; set; }
    internal ulong OwnerHelperStatusBytes { get; set; }
    internal ulong OwnerJournalBytes { get; set; }
    internal ulong OwnerReceiptBytes { get; set; }
    internal QualificationWireClosureObject[] Objects { get; set; } = Array.Empty<QualificationWireClosureObject>();
    internal string[] HelperDependencies { get; set; } = Array.Empty<string>();
    internal string ToolPath { get; set; } = "/tool";
    internal string Mode { get; set; } = "work";
    internal string InputPath { get; set; } = "/input";
    internal string[] Argv { get; set; } = Array.Empty<string>();
    internal QualificationWireEnvPair[] Environment { get; set; } = Array.Empty<QualificationWireEnvPair>();
    internal string WorkDirectory { get; set; } = "/tmp";
    internal bool StdinEof { get; set; } = true;
    internal ulong StdoutLimitBytes { get; set; }
    internal ulong StderrLimitBytes { get; set; }
    internal ulong AggregateLimitBytes { get; set; }

    internal static Dictionary<string, QualificationWireBudget> NewBudgets() =>
        BudgetNames.ToDictionary(name => name, _ => new QualificationWireBudget(), StringComparer.Ordinal);

    internal static byte[] Sequence32(byte start)
    {
        var bytes = new byte[32];
        for (var i = 0; i < 32; i++)
        {
            bytes[i] = (byte)(start + i);
        }

        return bytes;
    }

    /// <summary>The committed sample-manifest values in canonical form. Build()
    /// over this instance must equal the committed 873-byte golden literal.</summary>
    internal static QualificationWireTemplate Golden()
    {
        var template = new QualificationWireTemplate
        {
            Namespaces = new[] { "ipc", "mount", "net", "pid", "user", "uts" },
            FilterSha256 = Sequence32(0x01),
            FilterLength = 312,
            ReadOnlyDestinations = new[] { "/coordinator", "/input", "/tool" },
            Stores = new[]
            {
                new QualificationWireStore { Path = "/dev/shm", Bytes = 1048576, HasInodeBudget = false, InodeBudget = 0 },
                new QualificationWireStore { Path = "/tmp", Bytes = 7340032, HasInodeBudget = false, InodeBudget = 0 },
            },
            CorePatternObserved = null,
            OwnerStagingBytes = 16777216,
            OwnerStagingObjects = 128,
            OwnerConfigBytes = 16384,
            OwnerCoordinatorStatusBytes = 16384,
            OwnerHelperStatusBytes = 16384,
            OwnerJournalBytes = 65536,
            OwnerReceiptBytes = 32768,
            Objects = new[]
            {
                new QualificationWireClosureObject { Role = "app", Sha256 = Sequence32(0x20), Length = 200, Destination = "/app", BuildIdentity = "dotnet-publish" },
                new QualificationWireClosureObject { Role = "input", Sha256 = Sequence32(0x30), Length = 300, Destination = "/input", BuildIdentity = "copyin" },
                new QualificationWireClosureObject { Role = "tool", Sha256 = Sequence32(0x10), Length = 100, Destination = "/tool", BuildIdentity = "gcc-static" },
            },
            HelperDependencies = new[] { "a-dep", "z-dep" },
            Argv = new[] { "--in", "/input" },
            Environment = new[]
            {
                new QualificationWireEnvPair { Key = "HOME", Value = "/tmp" },
                new QualificationWireEnvPair { Key = "LANG", Value = "C" },
            },
            StdoutLimitBytes = 65536,
            StderrLimitBytes = 65536,
            AggregateLimitBytes = 131072,
        };
        SetBudget(template, "Memory", BudgetValueKind.Exact, 134217728, "bytes");
        SetBudget(template, "Swap", BudgetValueKind.Exact, 0, "bytes");
        SetBudget(template, "Tasks", BudgetValueKind.Exact, 48, "tasks");
        SetBudget(template, "FileDescriptors", BudgetValueKind.Exact, 64, "fds");
        SetBudget(template, "TmpBytes", BudgetValueKind.Exact, 7340032, "bytes");
        SetBudget(template, "ShmBytes", BudgetValueKind.Exact, 1048576, "bytes");
        SetBudget(template, "StdoutBytes", BudgetValueKind.Exact, 65536, "bytes");
        SetBudget(template, "StderrBytes", BudgetValueKind.Exact, 65536, "bytes");
        SetBudget(template, "CaptureAggregateBytes", BudgetValueKind.Exact, 131072, "bytes");
        SetBudget(template, "SetupTimeoutNs", BudgetValueKind.Exact, 10000000000, "ns");
        SetBudget(template, "OverallTimeoutNs", BudgetValueKind.Exact, 30000000000, "ns");
        SetBudget(template, "StopAllowanceNs", BudgetValueKind.Exact, 5000000000, "ns");
        SetBudget(template, "FinalDrainNs", BudgetValueKind.Exact, 1000000000, "ns");
        return template;
    }

    internal static void SetBudget(QualificationWireTemplate template, string name, BudgetValueKind kind, ulong value, string unit)
    {
        template.Budgets[name] = new QualificationWireBudget
        {
            Kind = kind,
            Value = value,
            Unit = unit,
            Qualification = QualificationDisposition.Mandatory,
        };
    }

    internal QualificationWireBudget Budget(string name) => Budgets[name];

    /// <summary>Builds the complete wire image. When <paramref name="defectSlot"/>
    /// names a recorded slot, the defect action replaces that slot's canonical
    /// write and the defect start offset is recorded. Recorded offsets are
    /// construction-derived byte positions, never guesses.</summary>
    internal QualificationWireBuild Build(string? defectSlot = null, Action<QualificationWireWriter>? defect = null, bool recordRegions = false)
    {
        var writer = new QualificationWireWriter();
        var offsets = new Dictionary<string, int>(StringComparer.Ordinal);
        var regions = recordRegions ? new List<QualificationWireRegion>() : null;
        var defectOffset = -1;

        void Field(string slotId, bool contentEligible, Action<QualificationWireWriter> write, int structuralPrefix = 0)
        {
            var start = writer.Position;
            if (slotId == defectSlot)
            {
                defectOffset = start;
                defect!(writer);
            }
            else
            {
                write(writer);
            }

            var end = writer.Position;
            offsets[slotId] = start;
            if (regions is not null)
            {
                if (end > start && structuralPrefix > 0)
                {
                    regions.Add(new QualificationWireRegion(start, Math.Min(structuralPrefix, end - start), false, slotId));
                }

                var contentStart = Math.Min(start + structuralPrefix, end);
                if (end > contentStart)
                {
                    regions.Add(new QualificationWireRegion(contentStart, end - contentStart, contentEligible, slotId));
                }
            }
        }

        Field("schema", false, w => w.U16(SchemaVersion));
        Field("implementation", false, w => w.Text(ImplementationId), 4);

        Field("namespaces.count", false, w => w.U32((uint)Namespaces.Length));
        for (var i = 0; i < Namespaces.Length; i++)
        {
            var item = Namespaces[i];
            Field($"namespaces.item[{i}]", true, w => w.Text(item), 4);
        }

        Field("capabilities.count", false, w => w.U32((uint)Capabilities.Length));
        for (var i = 0; i < Capabilities.Length; i++)
        {
            var item = Capabilities[i];
            Field($"capabilities.item[{i}]", false, w => w.Text(item), 4);
        }

        Field("authority.noNewPrivs", true, w => w.Bool(NoNewPrivs));
        Field("authority.filterSha", true, w => w.Raw(FilterSha256));
        Field("authority.filterLength", true, w => w.U64(FilterLength));

        Field("readOnly.count", false, w => w.U32((uint)ReadOnlyDestinations.Length));
        for (var i = 0; i < ReadOnlyDestinations.Length; i++)
        {
            var item = ReadOnlyDestinations[i];
            Field($"readOnly.item[{i}]", true, w => w.Text(item), 4);
        }

        Field("stores.count", false, w => w.U32((uint)Stores.Length));
        for (var i = 0; i < Stores.Length; i++)
        {
            var store = Stores[i];
            Field($"stores.item[{i}].path", true, w => w.Text(store.Path), 4);
            Field($"stores.item[{i}].bytes", true, w => w.U64(store.Bytes));
            Field($"stores.item[{i}].hasInode", true, w => w.Bool(store.HasInodeBudget));
            Field($"stores.item[{i}].inode", true, w => w.U64(store.InodeBudget));
        }

        Field("authority.noAmbientEnvironment", false, w => w.Bool(NoAmbientEnvironment));
        Field("authority.noIpcEndpoints", false, w => w.Bool(NoIpcEndpoints));
        Field("authority.noExtraFds", false, w => w.Bool(NoExtraInheritedFds));
        Field("mount.toolExecutable", false, w => w.Bool(ToolExecutable));
        Field("mount.scratchNoexec", true, w => w.Bool(ScratchNoexec));
        Field("authority.networkQualification", false, w => w.U8((byte)NetworkQualification));

        foreach (var name in BudgetNames)
        {
            var budget = Budgets[name];
            Field($"budget.{name}.kind", true, w => w.U8((byte)budget.Kind));
            Field($"budget.{name}.value", true, w => w.U64(budget.Value));
            Field($"budget.{name}.unit", true, w => w.Text(budget.Unit), 4);
            Field($"budget.{name}.qualification", true, w => w.U8((byte)budget.Qualification));
        }

        Field("resources.inodeQualification", false, w => w.U8((byte)InodeQualification));
        Field("core.disabled", false, w => w.Bool(CoreDisabled));
        Field("core.runtimeDumpsDisabled", true, w => w.Bool(RuntimeDumpsDisabled));
        Field("core.patternPresent", false, w => w.Bool(CorePatternObserved is not null));
        if (CorePatternObserved is not null)
        {
            Field("core.pattern", true, w => w.Text(CorePatternObserved), 4);
        }

        Field("owner.stagingBytes", true, w => w.U64(OwnerStagingBytes));
        Field("owner.stagingObjects", true, w => w.U32(OwnerStagingObjects));
        Field("owner.configBytes", true, w => w.U64(OwnerConfigBytes));
        Field("owner.coordinatorStatusBytes", true, w => w.U64(OwnerCoordinatorStatusBytes));
        Field("owner.helperStatusBytes", true, w => w.U64(OwnerHelperStatusBytes));
        Field("owner.journalBytes", true, w => w.U64(OwnerJournalBytes));
        Field("owner.receiptBytes", true, w => w.U64(OwnerReceiptBytes));

        Field("objects.count", false, w => w.U32((uint)Objects.Length));
        for (var i = 0; i < Objects.Length; i++)
        {
            var item = Objects[i];
            Field($"objects.item[{i}].role", true, w => w.Text(item.Role), 4);
            Field($"objects.item[{i}].sha", true, w => w.Raw(item.Sha256));
            Field($"objects.item[{i}].length", true, w => w.U64(item.Length));
            Field($"objects.item[{i}].destination", true, w => w.Text(item.Destination), 4);
            Field($"objects.item[{i}].buildIdentity", true, w => w.Text(item.BuildIdentity), 4);
        }

        Field("helpers.count", false, w => w.U32((uint)HelperDependencies.Length));
        for (var i = 0; i < HelperDependencies.Length; i++)
        {
            var item = HelperDependencies[i];
            Field($"helpers.item[{i}]", true, w => w.Text(item), 4);
        }

        Field("invocation.toolPath", false, w => w.Text(ToolPath), 4);
        Field("invocation.mode", true, w => w.Text(Mode), 4);
        Field("invocation.inputPath", false, w => w.Text(InputPath), 4);

        Field("argv.count", false, w => w.U32((uint)Argv.Length));
        for (var i = 0; i < Argv.Length; i++)
        {
            var item = Argv[i];
            Field($"argv.item[{i}]", true, w => w.Text(item), 4);
        }

        Field("env.count", false, w => w.U32((uint)Environment.Length));
        for (var i = 0; i < Environment.Length; i++)
        {
            var pair = Environment[i];
            Field($"env.key[{i}]", true, w => w.Text(pair.Key), 4);
            Field($"env.value[{i}]", true, w => w.Text(pair.Value), 4);
        }

        Field("invocation.workDirectory", false, w => w.Text(WorkDirectory), 4);

        Field("channels.stdinEof", false, w => w.Bool(StdinEof));
        Field("channels.stdoutLimit", true, w => w.U64(StdoutLimitBytes));
        Field("channels.stderrLimit", true, w => w.U64(StderrLimitBytes));
        Field("channels.aggregateLimit", true, w => w.U64(AggregateLimitBytes));

        return new QualificationWireBuild
        {
            Bytes = writer.ToArray(),
            Offsets = QualificationHash.Freeze(offsets),
            DefectOffset = defectOffset,
            Description = defectSlot is null ? "canonical" : $"defect@{defectSlot}",
            Regions = regions ?? (IReadOnlyList<QualificationWireRegion>)Array.Empty<QualificationWireRegion>(),
        };
    }
}
