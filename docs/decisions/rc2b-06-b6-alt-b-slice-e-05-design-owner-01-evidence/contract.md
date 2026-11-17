# D3 — frozen managed interface and exact file boundary

## Scope and ownership

All new types are `internal`, in `Gagamba.Execution.Linux.M1.Bootstrap`, owned by existing project `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj`. They exist in both ordinary Debug and Release builds, have no live caller and perform only bounded in-memory comparisons. No public API, new friend assembly, generic verifier callback, service registration, static initializer with effects, native import, file/socket/clock/process access, authority conversion or serialization is permitted. This is a model of supplied evidence, not a verified-factory interface. Tests may create synthetic operands in both configurations because those operands cannot carry authority.

The exact callable surface is frozen below as **future design notation**, not source created by this commission:

```csharp
internal sealed class ValidationCustodyModel : IDisposable
{
    internal static ValidationCustodyRefusal TryCreate(
        ValidationCustodyExpectations? expectations,
        ValidationCustodyEvidence? evidence, long nowNs, bool stopObserved,
        out ValidationCustodyModel? model);
    internal ValidationCustodyRefusal Advance(
        ValidationCustodyEvent next, ValidationCustodyEvidence? evidence,
        long nowNs, bool stopObserved,
        out ValidationCustodyModelSnapshot? snapshot);
    public void Dispose(); // interface member on an internal, non-authoritative class
}
```

No other construction or restore method. Constructor private. `TryCreate` returns a model only on `None`; `Advance` returns a snapshot only on `None`. Each successful snapshot exposes `State` and a defensive copy of `Binding` only, with a constant diagnostic string `EvidenceKind = "supplied-structural-model"`. No `Accepted`, `Authorized`, `Qualified`, `CanExecute`, live handle, approval flag, action capability, or conversion to an existing installation/authority result. A snapshot cannot advance or recreate its model. The class owns only managed values and a private synchronization gate; `Dispose` closes the model, never a native resource.

## Data types and exact fields

Use sealed classes with get-only properties and constructor snapshots for reference containers; value-only records/structs for scalar tuples. All names in this table are fixed new internal types unless explicitly identified as existing. No extensible dictionary, arbitrary field bag or delegate.

| Type | Exact fields / validation |
|---|---|
| `ValidationCustodyBinding` | `ValidationPreContext Pre` (existing type; Version=3, Topology=B2 or Integrated); `Gava1Ids Ids` (existing; three nonempty GUIDs); nonempty `Guid Host`, `Guid Boot`, `Guid Selection`, `Guid LauncherOriginal`, `Guid ControllerOriginal`; byte[32] `LauncherExecutable`, `LauncherClosure`, `ControllerExecutable`, `ControllerClosure`, `DriverExecutable`, `DriverClosure`, `ArtifactSet`, `Catalog`, `Manifest`, `Policy`, `Script`, `Review`, `OwnerReceipt`, `Approval`, `Capability`; string `CaseId`; `long AdmitBeforeNs`, `long SessionEndNs`; nullable strings `QualificationId`, `QualificationRecordSha256`. |
| `ValidationCustodyExpectations` | One non-null `ValidationCustodyBinding Binding`. This is an independently retained expected operand, never read from the suspect evidence inside the model. |
| `ValidationCustodyEvidence` | One non-null `ValidationCustodyBinding Binding`; `ValidationCustodyFact ProtectedInputs`, `Provenance`, `ApprovalCurrent`, `OriginalAssociation`, `LiveOriginals`, `FreshAuthorization`, `IssuanceCommitted`, `OfferValidated`, `PermanentDrop`, `ConsumptionCommitted`, `CleanupConfirmed`. These are supplied premise classifications only. |
| `ValidationCustodyFact : byte` | `Unknown=0`, `Consistent=1`, `Contradictory=2`. Other values invalid. `Consistent` asserts only what a test supplied, never actual provenance. |
| `ValidationCustodyEvent : byte` | `ObserveIssue=1`, `ObserveOffer=2`, `ObserveDrop=3`, `ObserveConsume=4`, `Complete=5`. Invalid values refuse. No action dispatch, Release or rebind event. |
| `ValidationCustodyState : byte` | `PendingModel=1`, `IssuedModel=2`, `OfferedModel=3`, `DroppedModel=4`, `ConsumedModel=5`, `Closed=6`. No live authority state. |
| `ValidationCustodyRefusal : byte` | `None=0`, `Context=1`, `Comparison=2`, `Evidence=3`, `Transition=4`, `Stopped=5`, `Expired=6`. No wire serialization or reinterpretation as existing protocol codes. |
| `ValidationCustodyModelSnapshot` | `ValidationCustodyState State`, copied `ValidationCustodyBinding Binding`, constant `EvidenceKind` above. Only model creates it. Successful Complete returns Closed snapshot; any refusal returns null. |

The original-process GUIDs are **synthetic association labels**, not process IDs/pidfds or a proposed native identity format. Selection is likewise a model label; ArtifactSet remains the independent artifact-set digest. Session/Execution/Lease retain the exact E-3/E-4 meanings. No preparation identity is fabricated: it is created downstream and release correlation remains E-6. CaseId uses the inspected frozen S2/P0 grammar: 1–128 ASCII characters, each alphanumeric, dot, underscore or hyphen; no leading or trailing dot (thus neither dot nor double-dot). Use ordinal equality. This exactly preserves `FrozenSchemaRules.IsValidId` semantics without calling the E-2 verifier. All 15 digest fields are exactly 32 bytes, compared byte-for-byte. Host/Boot/Selection and both original labels must be nonempty; L and C labels must differ. No cross-field GUID inequality beyond that rule is invented. QualificationId and QualificationRecordSha256 are both null for this pre-only model. Post, legacy/v2 and unknown topology refuse Context without touching accepted codecs.

Host, Boot and Selection GUIDs are also explicitly synthetic association labels in this model. In particular, Host is not a replacement for the actual protected host identifier's frozen byte grammar. Test adapters retain a separate mapping to their independently built host/boot/selection operands; there is no production adapter or serialization of these labels. A later native handoff must use the actual protected host/boot/selection representations, not import this model Binding.

Time domain is supplied monotonic nanoseconds on the bound synthetic boot, signed Int64 with `0 <= nowNs < AdmitBeforeNs <= SessionEndNs` at creation. Values are compared, never added or extended. ObserveIssue/Offer/Drop/Consume must occur before AdmitBeforeNs as well as SessionEndNs; Complete before SessionEndNs. Each call's nowNs must be at least the last accepted observation. This models the applicable tighter deadline supplied by the test; it does not compute the real issuance/session/C3 bounds or read a clock. Live adapters must select the minimum accepted bound and enforce actual original deadlines independently. No synthetic duration asserts that the native 60-second issuance bound has been checked.

## Input origin and ownership

| Input | Initial E-5 producer | Future independent source / owner (not implemented here) |
|---|---|---|
| Expected L executable/closure | Test-owned reviewed expectation vector, created separately from evidence | A's fixed protected installation/review and actual L verification; never K's nonexistent L field |
| Expected C/D executable and closure | Independent test vector; K/C/D cross-check in tests | Protected catalog C/D closure plus independently approved package inventory; L/C measure actual originals; E-7 measures D |
| Hk/Hm/Hf/Hs and Hr/Hw/Hp | Independent fixture bytes and locally computed SHA-256 outside model | Each actor's protected acquisition; A's actual V/W provenance; unchanged E-2 verifies contents only |
| Host/boot/selection, IDs and capability | Separate retained test originals; test-only `Gava1Originals` / E-3 results may populate the suspect operand | Protected host/selection and C-generated IDs, retained original L/C and capability, never D-supplied expectations |
| Case, phase/topology, qualification pair | Independently selected pre witness | Exact protected K and retained selection; no invented topology/phase cross-rule beyond pre-only scope |
| Premise facts | Synthetic witness classified independently for each category | Later native/read/administrative observations owned as in D2; real values cannot be established by setting this enum |
| nowNs / stopObserved | Deterministic test scheduler | Original monotonic deadline and retained stop monitor at the live integration gate |

Every byte array is copied on intake and on return. No writable alias via `ReadOnlyMemory`, cast, record copy or nested Binding may mutate retained data. Strings are bounded/immutable. Constructors store invalid digest lengths as empty arrays (or otherwise bounded invalid state) so `TryCreate`/`Advance` can refuse Context, without copying unbounded buffers; overlong CaseId becomes invalid without retention. No caller callback is invoked under the model gate. Inputs must not be concurrently mutated during their own constructor copy; once constructed, they are immutable. Tests exercise mutation after construction and after output retrieval.

## Permitted dependencies

Production model may reference only ordinary BCL value/array/locking facilities and existing value types `ValidationPreContext`, `ValidationPreTopology`, `Gava1Ids`. It **must not call** E-2 verifiers, E-3 decoder/binary validator, E-4 `Decode` or `MatchPreBootstrap`, E-1 projection or any live component. This preserves the accepted test-only composition boundary and introduces no new production codec consumer. New model-to-value-type metadata references must be separately enumerated by the compiled scan; they are allowed and do not justify ignoring callable codec references.

Tests alone may call unchanged `FrozenContractVerifier.TryVerifyChain`, `DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain`, `ValidationPreCodec.DecodeBootstrap`, `ValidationPreCodecBinary`'s existing validation surface, `Gava1ContextCodec.Decode` and `MatchPreBootstrap` with independently retained expectations. Preserve their native refusal enums/out-null behavior and P1/P2 failure diagnostics. Model tests adapt successful parsed scalar/digest values into the evidence operand only; expected values stay independently held. Failed codec output is never mapped to `Consistent`. Null byte arrays are checked before invoking accepted E-2 code; no inherited codec is fixed in this package.

## Exact proposed file inventory

The following are the **entire** future initial implementation allowlist. All are additions; no existing file modification is proposed.

| Path | Role / project |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationCustodyModelData.cs` | All new enums, immutable operands, snapshot; existing M1 project/Bootstrap namespace |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationCustodyModel.cs` | Private construction, ordered comparison, state/lifetime and IDisposable; same ownership |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelTests.cs` | Positive/refusal/independent-binding cases; existing M1.Tests project, Bootstrap test namespace |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelLifecycleTests.cs` | Ordering, terminal/stop/deadline/disposal/concurrency/defensive ownership cases |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelCompositionTests.cs` | Both accepted E-3/E-4 test-only handoffs, E-2 supplied-chain controls, source/compiled reachability assertions |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelVectors.cs` | Test-only separately constructed expected/suspect operands, no production fixture or native handle |

All four test files use namespace `Gagamba.Execution.Linux.M1.Tests.Bootstrap`. Tests use the existing M1.Tests friend declaration in `Execution/TransactionStateModel.cs`; no Visibility/IVT/project/package/build-policy edit is allowed. If existing access cannot support these tests, stop for a bounded scope revision instead of widening access. Before implementation authorization, confirm all six paths are absent and the namespace/project compile globs still apply. Future native project/package and C/D reservations in D1 are explicitly **not** in this allowlist.

All existing E-1–E-4 files, `QualifiedLauncher`, B2Controller, Recovery/Owner entry points, `Installed/**`, `Validation/**`, `Lifecycle/**`, `Restricted/**`, `Launch/**`, `eng/**`, `native/**`, projects/solution, Directory.Build files, CI, providers and runtime activation stay unchanged. Prohibit all call paths to `PrepublicationIntegrityProjection.Verify`, `LoadedInstallation`, `OwnerAuthorityContext`, admission factories, protected readers, spawn/FD/native transport, journal/staging, writer, `HeldReleasePhase`, `ReleaseArbiter`, driver dispatch or `RestrictedExecutionRuntime.Create`. No proposed source or test file is created by this decision.
