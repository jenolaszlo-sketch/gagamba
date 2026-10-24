# RC2A-01 declared public API inventory

Bound source: `e93d642e1a36af25c0e145d328bebadd1bba4838`. Declaration lines only; complete signatures and XML documentation are in the named source files. No runtime facade types are exported.

## RestrictedManifestCodec.cs

```csharp
public enum ManifestDecodeError
public sealed class ManifestDecodeResult
public RestrictedManifestView? Value { get; }
public ManifestDecodeError? Error { get; }
public int? Offset { get; }
public static class RestrictedManifestCodec
public const ushort SupportedSchemaVersion = 1;
public const int MaxCanonicalBytes = 65536;
public static bool Supports(ushort schemaVersion, string implementationId)
public static ManifestDecodeResult Decode(ReadOnlySpan<byte> canonicalBytes)
public static byte[] Encode(RestrictedManifestView manifest)
public static ImmutableArray<byte> ComputeSha256(RestrictedManifestView manifest) =>
public static bool VerifySha256(RestrictedManifestView manifest, ReadOnlySpan<byte> expectedSha256)
```

## RestrictedManifestView.cs

```csharp
public enum BudgetValueKind : byte { Absent = 0, Exact = 1, Unlimited = 2 }
public enum QualificationDisposition : byte { Mandatory = 0, PendingQualification = 1 }
public sealed class RestrictedManifestView
public ushort SchemaVersion { get; }
public string ImplementationId { get; }
public AuthorityManifestView Authority { get; }
public ResourceManifestView Resources { get; }
public ClosureIdentityView Closure { get; }
public InvocationManifestView Invocation { get; }
public ChannelManifestView Channels { get; }
public sealed class AuthorityManifestView
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
public sealed class ResourceManifestView
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
public sealed class ResourceBudgetView
public BudgetValueKind Kind { get; }
public ulong Value { get; }
public string Unit { get; }
public QualificationDisposition Qualification { get; }
public sealed class WritableStoreView
public string Path { get; }
public ulong Bytes { get; }
public bool HasInodeBudget { get; }
public ulong InodeBudget { get; }
public sealed class ExecutableMountPolicyView
public bool ToolExecutable { get; }
public bool ScratchNoexec { get; }
public sealed class CoreDumpPolicyView
public bool CoreDisabled { get; }
public bool RuntimeDumpsDisabled { get; }
public string? CorePatternObserved { get; }
public sealed class OwnerAllocationLimitsView
public ulong StagingBytes { get; }
public uint StagingObjects { get; }
public ulong ConfigBytes { get; }
public ulong CoordinatorStatusBytes { get; }
public ulong HelperStatusBytes { get; }
public ulong JournalBytes { get; }
public ulong ReceiptBytes { get; }
public sealed class ClosureIdentityView
public ImmutableArray<ClosureObjectView> Objects { get; }
public ImmutableArray<string> HelperDependencies { get; }
public sealed class ClosureObjectView
public string Role { get; }
public ImmutableArray<byte> Sha256 { get; }
public ulong Length { get; }
public string Destination { get; }
public string BuildIdentity { get; }
public sealed class InvocationManifestView
public string ToolPath { get; }
public string Mode { get; }
public string InputPath { get; }
public ImmutableArray<string> Argv { get; }
public ImmutableArray<KeyValuePair<string, string>> Environment { get; }
public string WorkDirectory { get; }
public sealed class ChannelManifestView
public bool StdinEof { get; }
public ulong StdoutLimitBytes { get; }
public ulong StderrLimitBytes { get; }
public ulong AggregateLimitBytes { get; }
```

## RestrictedPreparationReference.cs

```csharp
public sealed class RestrictedPreparationReference : IEquatable<RestrictedPreparationReference>
public RestrictedPreparationReference(Guid executionId, Guid issuerId, Guid generationId,
Guid preparationId, ReadOnlySpan<byte> manifestSha256)
public Guid ExecutionId { get; }
public Guid IssuerId { get; }
public Guid GenerationId { get; }
public Guid PreparationId { get; }
public ImmutableArray<byte> ManifestSha256 { get; }
public bool Equals(RestrictedPreparationReference? other) => other is not null &&
public override bool Equals(object? obj) => obj is RestrictedPreparationReference other && Equals(other);
public override int GetHashCode()
```
