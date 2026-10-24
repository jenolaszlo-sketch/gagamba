// RC-D02 projections over the existing immutable model. No public construction authority.
using System.Collections.Immutable;

namespace Gagamba.Execution.Restricted.Contracts;

public enum BudgetValueKind : byte { Absent = 0, Exact = 1, Unlimited = 2 }
public enum QualificationDisposition : byte { Mandatory = 0, PendingQualification = 1 }

/// <summary>Passive immutable inspection of RestrictedManifest; no enforcement or execution authority.</summary>
public sealed class RestrictedManifestView
{
    internal RestrictedManifest Model { get; }

    internal RestrictedManifestView(RestrictedManifest value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ManifestValidation.Validate(value);
        _ = ManifestCodec.Encode(value, RestrictedManifestCodec.MaxCanonicalBytes);
        Model = value;
        SchemaVersion = value.SchemaVersion;
        ImplementationId = value.ImplementationId;
        Authority = new AuthorityManifestView(value.Authority);
        Resources = new ResourceManifestView(value.Resources);
        Closure = new ClosureIdentityView(value.Closure);
        Invocation = new InvocationManifestView(value.Invocation);
        Channels = new ChannelManifestView(value.Channels);
    }

    public ushort SchemaVersion { get; }
    public string ImplementationId { get; }
    public AuthorityManifestView Authority { get; }
    public ResourceManifestView Resources { get; }
    public ClosureIdentityView Closure { get; }
    public InvocationManifestView Invocation { get; }
    public ChannelManifestView Channels { get; }
}

/// <summary>Passive immutable inspection of AuthorityManifest; no enforcement or execution authority.</summary>
public sealed class AuthorityManifestView
{
    internal AuthorityManifestView(AuthorityManifest value)
    {
        Namespaces = value.Namespaces;
        Capabilities = value.Capabilities;
        NoNewPrivs = value.NoNewPrivs;
        FilterSha256 = value.FilterSha256;
        FilterLength = value.FilterLength;
        ReadOnlyDestinations = value.ReadOnlyDestinations;
        WritableStores = value.WritableStores.Select(item => new WritableStoreView(item)).ToImmutableArray();
        NoAmbientEnvironment = value.NoAmbientEnvironment;
        NoIpcEndpoints = value.NoIpcEndpoints;
        NoExtraInheritedFds = value.NoExtraInheritedFds;
        MountPolicy = new ExecutableMountPolicyView(value.MountPolicy);
        NetworkQualification = (QualificationDisposition)value.NetworkQualification;
    }

    public ImmutableArray<string> Namespaces { get; }
    public ImmutableArray<string> Capabilities { get; }
    public bool NoNewPrivs { get; }
    public ImmutableArray<byte> FilterSha256 { get; }
    public ulong FilterLength { get; }
    public ImmutableArray<string> ReadOnlyDestinations { get; }
    public ImmutableArray<WritableStoreView> WritableStores { get; }
    public bool NoAmbientEnvironment { get; }
    public bool NoIpcEndpoints { get; }
    public bool NoExtraInheritedFds { get; }
    public ExecutableMountPolicyView MountPolicy { get; }
    public QualificationDisposition NetworkQualification { get; }
}

/// <summary>Passive immutable inspection of ResourceManifest; no enforcement or execution authority.</summary>
public sealed class ResourceManifestView
{
    internal ResourceManifestView(ResourceManifest value)
    {
        Memory = new ResourceBudgetView(value.Memory);
        Swap = new ResourceBudgetView(value.Swap);
        Tasks = new ResourceBudgetView(value.Tasks);
        FileDescriptors = new ResourceBudgetView(value.FileDescriptors);
        TmpBytes = new ResourceBudgetView(value.TmpBytes);
        ShmBytes = new ResourceBudgetView(value.ShmBytes);
        StdoutBytes = new ResourceBudgetView(value.StdoutBytes);
        StderrBytes = new ResourceBudgetView(value.StderrBytes);
        CaptureAggregateBytes = new ResourceBudgetView(value.CaptureAggregateBytes);
        SetupTimeoutNs = new ResourceBudgetView(value.SetupTimeoutNs);
        OverallTimeoutNs = new ResourceBudgetView(value.OverallTimeoutNs);
        StopAllowanceNs = new ResourceBudgetView(value.StopAllowanceNs);
        FinalDrainNs = new ResourceBudgetView(value.FinalDrainNs);
        InodeQualification = (QualificationDisposition)value.InodeQualification;
        CoreDumps = new CoreDumpPolicyView(value.CoreDumps);
        OwnerAllocation = new OwnerAllocationLimitsView(value.OwnerAllocation);
    }

    public ResourceBudgetView Memory { get; }
    public ResourceBudgetView Swap { get; }
    public ResourceBudgetView Tasks { get; }
    public ResourceBudgetView FileDescriptors { get; }
    public ResourceBudgetView TmpBytes { get; }
    public ResourceBudgetView ShmBytes { get; }
    public ResourceBudgetView StdoutBytes { get; }
    public ResourceBudgetView StderrBytes { get; }
    public ResourceBudgetView CaptureAggregateBytes { get; }
    public ResourceBudgetView SetupTimeoutNs { get; }
    public ResourceBudgetView OverallTimeoutNs { get; }
    public ResourceBudgetView StopAllowanceNs { get; }
    public ResourceBudgetView FinalDrainNs { get; }
    public QualificationDisposition InodeQualification { get; }
    public CoreDumpPolicyView CoreDumps { get; }
    public OwnerAllocationLimitsView OwnerAllocation { get; }
}

/// <summary>Passive immutable inspection of ResourceBudget; no enforcement or execution authority.</summary>
public sealed class ResourceBudgetView
{
    internal ResourceBudgetView(ResourceBudget value)
    {
        Kind = (BudgetValueKind)value.Kind;
        Value = value.Value;
        Unit = value.Unit;
        Qualification = (QualificationDisposition)value.Qualification;
    }

    public BudgetValueKind Kind { get; }
    public ulong Value { get; }
    public string Unit { get; }
    public QualificationDisposition Qualification { get; }
}

/// <summary>Passive immutable inspection of WritableStore; no enforcement or execution authority.</summary>
public sealed class WritableStoreView
{
    internal WritableStoreView(WritableStore value)
    {
        Path = value.Path;
        Bytes = value.Bytes;
        HasInodeBudget = value.HasInodeBudget;
        InodeBudget = value.InodeBudget;
    }

    public string Path { get; }
    public ulong Bytes { get; }
    public bool HasInodeBudget { get; }
    public ulong InodeBudget { get; }
}

/// <summary>Passive immutable inspection of ExecutableMountPolicy; no enforcement or execution authority.</summary>
public sealed class ExecutableMountPolicyView
{
    internal ExecutableMountPolicyView(ExecutableMountPolicy value)
    {
        ToolExecutable = value.ToolExecutable;
        ScratchNoexec = value.ScratchNoexec;
    }

    public bool ToolExecutable { get; }
    public bool ScratchNoexec { get; }
}

/// <summary>Passive immutable inspection of CoreDumpPolicy; no enforcement or execution authority.</summary>
public sealed class CoreDumpPolicyView
{
    internal CoreDumpPolicyView(CoreDumpPolicy value)
    {
        CoreDisabled = value.CoreDisabled;
        RuntimeDumpsDisabled = value.RuntimeDumpsDisabled;
        CorePatternObserved = value.CorePatternObserved;
    }

    public bool CoreDisabled { get; }
    public bool RuntimeDumpsDisabled { get; }
    public string? CorePatternObserved { get; }
}

/// <summary>Passive immutable inspection of OwnerAllocationLimits; no enforcement or execution authority.</summary>
public sealed class OwnerAllocationLimitsView
{
    internal OwnerAllocationLimitsView(OwnerAllocationLimits value)
    {
        StagingBytes = value.StagingBytes;
        StagingObjects = value.StagingObjects;
        ConfigBytes = value.ConfigBytes;
        CoordinatorStatusBytes = value.CoordinatorStatusBytes;
        HelperStatusBytes = value.HelperStatusBytes;
        JournalBytes = value.JournalBytes;
        ReceiptBytes = value.ReceiptBytes;
    }

    public ulong StagingBytes { get; }
    public uint StagingObjects { get; }
    public ulong ConfigBytes { get; }
    public ulong CoordinatorStatusBytes { get; }
    public ulong HelperStatusBytes { get; }
    public ulong JournalBytes { get; }
    public ulong ReceiptBytes { get; }
}

/// <summary>Passive immutable inspection of ClosureIdentity; no enforcement or execution authority.</summary>
public sealed class ClosureIdentityView
{
    internal ClosureIdentityView(ClosureIdentity value)
    {
        Objects = value.Objects.Select(item => new ClosureObjectView(item)).ToImmutableArray();
        HelperDependencies = value.HelperDependencies;
    }

    public ImmutableArray<ClosureObjectView> Objects { get; }
    public ImmutableArray<string> HelperDependencies { get; }
}

/// <summary>Passive immutable inspection of ClosureObject; no enforcement or execution authority.</summary>
public sealed class ClosureObjectView
{
    internal ClosureObjectView(ClosureObject value)
    {
        Role = value.Role;
        Sha256 = value.Sha256;
        Length = value.Length;
        Destination = value.Destination;
        BuildIdentity = value.BuildIdentity;
    }

    public string Role { get; }
    public ImmutableArray<byte> Sha256 { get; }
    public ulong Length { get; }
    public string Destination { get; }
    public string BuildIdentity { get; }
}

/// <summary>Passive immutable inspection of InvocationManifest; no enforcement or execution authority.</summary>
public sealed class InvocationManifestView
{
    internal InvocationManifestView(InvocationManifest value)
    {
        ToolPath = value.ToolPath;
        Mode = value.Mode;
        InputPath = value.InputPath;
        Argv = value.Argv;
        Environment = value.Environment;
        WorkDirectory = value.WorkDirectory;
    }

    public string ToolPath { get; }
    public string Mode { get; }
    public string InputPath { get; }
    public ImmutableArray<string> Argv { get; }
    public ImmutableArray<KeyValuePair<string, string>> Environment { get; }
    public string WorkDirectory { get; }
}

/// <summary>Passive immutable inspection of ChannelManifest; no enforcement or execution authority.</summary>
public sealed class ChannelManifestView
{
    internal ChannelManifestView(ChannelManifest value)
    {
        StdinEof = value.StdinEof;
        StdoutLimitBytes = value.StdoutLimitBytes;
        StderrLimitBytes = value.StderrLimitBytes;
        AggregateLimitBytes = value.AggregateLimitBytes;
    }

    public bool StdinEof { get; }
    public ulong StdoutLimitBytes { get; }
    public ulong StderrLimitBytes { get; }
    public ulong AggregateLimitBytes { get; }
}
