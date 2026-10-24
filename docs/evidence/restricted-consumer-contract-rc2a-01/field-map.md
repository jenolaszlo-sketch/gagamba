# RC-D02 field-to-property inventory

Source: accepted `RestrictedManifest.cs`; public projection file: `src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestView.cs`. Wire order remains the existing `ManifestCodec.Write*` methods and RC-D03 grammar.

| Internal field | Public field | Public type |
|---|---|---|
| `RestrictedManifest.SchemaVersion` | `RestrictedManifestView.SchemaVersion` | `ushort` |
| `RestrictedManifest.ImplementationId` | `RestrictedManifestView.ImplementationId` | `string` |
| `RestrictedManifest.Authority` | `RestrictedManifestView.Authority` | `AuthorityManifestView` |
| `RestrictedManifest.Resources` | `RestrictedManifestView.Resources` | `ResourceManifestView` |
| `RestrictedManifest.Closure` | `RestrictedManifestView.Closure` | `ClosureIdentityView` |
| `RestrictedManifest.Invocation` | `RestrictedManifestView.Invocation` | `InvocationManifestView` |
| `RestrictedManifest.Channels` | `RestrictedManifestView.Channels` | `ChannelManifestView` |
| `AuthorityManifest.Namespaces` | `AuthorityManifestView.Namespaces` | `ImmutableArray<string>` |
| `AuthorityManifest.Capabilities` | `AuthorityManifestView.Capabilities` | `ImmutableArray<string>` |
| `AuthorityManifest.NoNewPrivs` | `AuthorityManifestView.NoNewPrivs` | `bool` |
| `AuthorityManifest.FilterSha256` | `AuthorityManifestView.FilterSha256` | `ImmutableArray<byte>` |
| `AuthorityManifest.FilterLength` | `AuthorityManifestView.FilterLength` | `ulong` |
| `AuthorityManifest.ReadOnlyDestinations` | `AuthorityManifestView.ReadOnlyDestinations` | `ImmutableArray<string>` |
| `AuthorityManifest.WritableStores` | `AuthorityManifestView.WritableStores` | `ImmutableArray<WritableStoreView>` |
| `AuthorityManifest.NoAmbientEnvironment` | `AuthorityManifestView.NoAmbientEnvironment` | `bool` |
| `AuthorityManifest.NoIpcEndpoints` | `AuthorityManifestView.NoIpcEndpoints` | `bool` |
| `AuthorityManifest.NoExtraInheritedFds` | `AuthorityManifestView.NoExtraInheritedFds` | `bool` |
| `AuthorityManifest.MountPolicy` | `AuthorityManifestView.MountPolicy` | `ExecutableMountPolicyView` |
| `AuthorityManifest.NetworkQualification` | `AuthorityManifestView.NetworkQualification` | `QualificationDisposition` |
| `ResourceManifest.Memory` | `ResourceManifestView.Memory` | `ResourceBudgetView` |
| `ResourceManifest.Swap` | `ResourceManifestView.Swap` | `ResourceBudgetView` |
| `ResourceManifest.Tasks` | `ResourceManifestView.Tasks` | `ResourceBudgetView` |
| `ResourceManifest.FileDescriptors` | `ResourceManifestView.FileDescriptors` | `ResourceBudgetView` |
| `ResourceManifest.TmpBytes` | `ResourceManifestView.TmpBytes` | `ResourceBudgetView` |
| `ResourceManifest.ShmBytes` | `ResourceManifestView.ShmBytes` | `ResourceBudgetView` |
| `ResourceManifest.StdoutBytes` | `ResourceManifestView.StdoutBytes` | `ResourceBudgetView` |
| `ResourceManifest.StderrBytes` | `ResourceManifestView.StderrBytes` | `ResourceBudgetView` |
| `ResourceManifest.CaptureAggregateBytes` | `ResourceManifestView.CaptureAggregateBytes` | `ResourceBudgetView` |
| `ResourceManifest.SetupTimeoutNs` | `ResourceManifestView.SetupTimeoutNs` | `ResourceBudgetView` |
| `ResourceManifest.OverallTimeoutNs` | `ResourceManifestView.OverallTimeoutNs` | `ResourceBudgetView` |
| `ResourceManifest.StopAllowanceNs` | `ResourceManifestView.StopAllowanceNs` | `ResourceBudgetView` |
| `ResourceManifest.FinalDrainNs` | `ResourceManifestView.FinalDrainNs` | `ResourceBudgetView` |
| `ResourceManifest.InodeQualification` | `ResourceManifestView.InodeQualification` | `QualificationDisposition` |
| `ResourceManifest.CoreDumps` | `ResourceManifestView.CoreDumps` | `CoreDumpPolicyView` |
| `ResourceManifest.OwnerAllocation` | `ResourceManifestView.OwnerAllocation` | `OwnerAllocationLimitsView` |
| `ResourceBudget.Kind` | `ResourceBudgetView.Kind` | `BudgetValueKind` |
| `ResourceBudget.Value` | `ResourceBudgetView.Value` | `ulong` |
| `ResourceBudget.Unit` | `ResourceBudgetView.Unit` | `string` |
| `ResourceBudget.Qualification` | `ResourceBudgetView.Qualification` | `QualificationDisposition` |
| `WritableStore.Path` | `WritableStoreView.Path` | `string` |
| `WritableStore.Bytes` | `WritableStoreView.Bytes` | `ulong` |
| `WritableStore.HasInodeBudget` | `WritableStoreView.HasInodeBudget` | `bool` |
| `WritableStore.InodeBudget` | `WritableStoreView.InodeBudget` | `ulong` |
| `ExecutableMountPolicy.ToolExecutable` | `ExecutableMountPolicyView.ToolExecutable` | `bool` |
| `ExecutableMountPolicy.ScratchNoexec` | `ExecutableMountPolicyView.ScratchNoexec` | `bool` |
| `CoreDumpPolicy.CoreDisabled` | `CoreDumpPolicyView.CoreDisabled` | `bool` |
| `CoreDumpPolicy.RuntimeDumpsDisabled` | `CoreDumpPolicyView.RuntimeDumpsDisabled` | `bool` |
| `CoreDumpPolicy.CorePatternObserved` | `CoreDumpPolicyView.CorePatternObserved` | `string?` |
| `OwnerAllocationLimits.StagingBytes` | `OwnerAllocationLimitsView.StagingBytes` | `ulong` |
| `OwnerAllocationLimits.StagingObjects` | `OwnerAllocationLimitsView.StagingObjects` | `uint` |
| `OwnerAllocationLimits.ConfigBytes` | `OwnerAllocationLimitsView.ConfigBytes` | `ulong` |
| `OwnerAllocationLimits.CoordinatorStatusBytes` | `OwnerAllocationLimitsView.CoordinatorStatusBytes` | `ulong` |
| `OwnerAllocationLimits.HelperStatusBytes` | `OwnerAllocationLimitsView.HelperStatusBytes` | `ulong` |
| `OwnerAllocationLimits.JournalBytes` | `OwnerAllocationLimitsView.JournalBytes` | `ulong` |
| `OwnerAllocationLimits.ReceiptBytes` | `OwnerAllocationLimitsView.ReceiptBytes` | `ulong` |
| `ClosureIdentity.Objects` | `ClosureIdentityView.Objects` | `ImmutableArray<ClosureObjectView>` |
| `ClosureIdentity.HelperDependencies` | `ClosureIdentityView.HelperDependencies` | `ImmutableArray<string>` |
| `ClosureObject.Role` | `ClosureObjectView.Role` | `string` |
| `ClosureObject.Sha256` | `ClosureObjectView.Sha256` | `ImmutableArray<byte>` |
| `ClosureObject.Length` | `ClosureObjectView.Length` | `ulong` |
| `ClosureObject.Destination` | `ClosureObjectView.Destination` | `string` |
| `ClosureObject.BuildIdentity` | `ClosureObjectView.BuildIdentity` | `string` |
| `InvocationManifest.ToolPath` | `InvocationManifestView.ToolPath` | `string` |
| `InvocationManifest.Mode` | `InvocationManifestView.Mode` | `string` |
| `InvocationManifest.InputPath` | `InvocationManifestView.InputPath` | `string` |
| `InvocationManifest.Argv` | `InvocationManifestView.Argv` | `ImmutableArray<string>` |
| `InvocationManifest.Environment` | `InvocationManifestView.Environment` | `ImmutableArray<KeyValuePair<string, string>>` |
| `InvocationManifest.WorkDirectory` | `InvocationManifestView.WorkDirectory` | `string` |
| `ChannelManifest.StdinEof` | `ChannelManifestView.StdinEof` | `bool` |
| `ChannelManifest.StdoutLimitBytes` | `ChannelManifestView.StdoutLimitBytes` | `ulong` |
| `ChannelManifest.StderrLimitBytes` | `ChannelManifestView.StderrLimitBytes` | `ulong` |
| `ChannelManifest.AggregateLimitBytes` | `ChannelManifestView.AggregateLimitBytes` | `ulong` |
