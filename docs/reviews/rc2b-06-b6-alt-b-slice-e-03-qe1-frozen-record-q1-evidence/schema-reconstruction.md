# Eleven closed bootstrap objects and member comparison

The reviewer transcribed names, types and relationships from the
[bootstrap-base owner decision](../../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01.md)
and its [annex](../../decisions/rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md).
The scratch inventory builds common roots once and constructs the two
role maps independently. The probe compares the manually transcribed
inventory with all annex rows, then checks that candidate §1 incorporates
the exact annex blob at the immutable owner commit. The schema file
at Q1 HEAD matches that same owner object. Candidate §3 carries the
owner's operative cross-field rules through the closing expectation paragraph
and the strict JSON rules agree on inspection.

| Graph branch | Closed objects |
|---|---|
| B2 root | B2 (16), B2InstallationIdentity → PreIdentity (16), Integrated and DelegatedParent literal null |
| Integrated root | Integrated (14), Integrated → IntegratedBody (8) → InstallationIdentity (PreIdentity), Controller (5), Runtime (7), Profile (19). Profile.IntegratedRecovery is literal null. |
| Both roots | RecoveryPin (2) → RecoveryHashes (5) and OwnerPin (2) → OwnerHashes (5) |
| Unique inventories | 11, 99 required rows. Shared inventories count once. |

B2 requires its two non-null root members. Integrated requires their
absence, even for null. Qualification nulls belong to the common selected
identity. The closed Profile additionally forbids QualificationFault and
QualifiedInstallation. No spelling alias adds a second key. Decode names
ordinally and case-sensitively before applying the closed sets. Every
listed row must appear once. H and F identify historical or F2 semantics,
O names new owner-selected rules, and BJ names the prior owner choices.
The table below preserves those source assignments. Scalar names resolve
to the annex's domain table. Case and length requirements apply to
decoded strings except BJ-03, which binds literal value-token bytes.

| Object | Member | Type | Presence | Nullability | v3-pre | Source | Candidate match |
|---|---|---|---|---|---|---|---|
| B2 | `Version` | V3 | Once | No | B2 | BB §4 (H) | Exact annex |
| B2 | `Execution` | Guid | Once | No | B2 | BB §4 (O) | Exact annex |
| B2 | `Generation` | Guid | Once | No | B2 | BB §4 (O) | Exact annex |
| B2 | `Session` | Guid | Once | No | B2 | BB §4 (H+O) | Exact annex |
| B2 | `ControllerPid` | Pid | Once | No | B2 | BB §4 (O) | Exact annex |
| B2 | `ControllerUid` | Principal | Once | No | B2 | BB §4 (O) | Exact annex |
| B2 | `Secret` | Secret32 | Once | No | B2 | BB §4 (H+O) | Exact annex |
| B2 | `Recovery` | RecoveryPin | Once | No | B2 | BB §4 (O) | Exact annex |
| B2 | `Owner` | OwnerPin | Once | No | B2 | BB §4 (O) | Exact annex |
| B2 | `EvidenceDirectory` | Path | Once | No | B2 | BB §4 (O) | Exact annex |
| B2 | `DelegatedParent` | Null | Once | Only null | B2 | BB §4 (H) | Exact annex |
| B2 | `Integrated` | Null | Once | Only null | B2 | BB §4 (H) | Exact annex |
| B2 | `B2InstallationIdentity` | PreIdentity | Once | No | B2 | BB §4 (H+BJ) | Exact annex |
| B2 | `B2DeadlineNs` | I64+ | Once | No | B2 | BB §4 (H) | Exact annex |
| B2 | `AuthorityMode` | PreMode | Once | No | B2 | BB §4 (H) | Exact annex |
| B2 | `AuthorityHash` | CapabilityHex | Once | No | B2 | BB §4 (BJ) | Exact annex |
| Integrated | `Version` | V3 | Once | No | I | BB §4 (H) | Exact annex |
| Integrated | `Execution` | Guid | Once | No | I | BB §4 (O) | Exact annex |
| Integrated | `Generation` | Guid | Once | No | I | BB §4 (O) | Exact annex |
| Integrated | `Session` | Guid | Once | No | I | BB §4 (H+O) | Exact annex |
| Integrated | `ControllerPid` | Pid | Once | No | I | BB §4 (O) | Exact annex |
| Integrated | `ControllerUid` | Principal | Once | No | I | BB §4 (O) | Exact annex |
| Integrated | `Secret` | Secret32 | Once | No | I | BB §4 (H+O) | Exact annex |
| Integrated | `Recovery` | RecoveryPin | Once | No | I | BB §4 (O) | Exact annex |
| Integrated | `Owner` | OwnerPin | Once | No | I | BB §4 (O) | Exact annex |
| Integrated | `EvidenceDirectory` | Path | Once | No | I | BB §4 (O) | Exact annex |
| Integrated | `DelegatedParent` | Path | Once | No | I | BB §4 (F+O) | Exact annex |
| Integrated | `Integrated` | IntegratedBody | Once | No | I | BB §4 (H+O) | Exact annex |
| Integrated | `AuthorityMode` | PreMode | Once | No | I | BB §4 (H) | Exact annex |
| Integrated | `AuthorityHash` | CapabilityHex | Once | No | I | BB §4 (BJ) | Exact annex |
| IntegratedBody | `Profile` | Profile | Once | No | I | BB §4 (F+O) | Exact annex |
| IntegratedBody | `Issuer` | Guid | Once | No | I | BB §4 (F+O) | Exact annex |
| IntegratedBody | `StoreGeneration` | Guid | Once | No | I | BB §4 (F+O) | Exact annex |
| IntegratedBody | `AcceptedAtNs` | I64+ | Once | No | I | BB §4 (O) | Exact annex |
| IntegratedBody | `ExpiresAtNs` | I64+ | Once | No | I | BB §4 (O) | Exact annex |
| IntegratedBody | `Controller` | Controller | Once | No | I | BB §4 (O) | Exact annex |
| IntegratedBody | `Runtime` | Runtime | Once | No | I | BB §4 (H+O) | Exact annex |
| IntegratedBody | `InstallationIdentity` | PreIdentity | Once | No | I | BB §4 (H+BJ) | Exact annex |
| RecoveryPin | `Directory` | Path | Once | No | Both | BB §4 (O) | Exact annex |
| RecoveryPin | `Sha256` | RecoveryHashes | Once | No | Both | BB §4 (O) | Exact annex |
| OwnerPin | `Directory` | Path | Once | No | Both | BB §4 (O) | Exact annex |
| OwnerPin | `Sha256` | OwnerHashes | Once | No | Both | BB §4 (O) | Exact annex |
| RecoveryHashes | `Gagamba.Execution.Linux.M1.Recovery.dll` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| RecoveryHashes | `Gagamba.Execution.Linux.M1.Recovery.deps.json` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| RecoveryHashes | `Gagamba.Execution.Linux.M1.Recovery.runtimeconfig.json` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| RecoveryHashes | `Gagamba.Execution.Linux.M1.dll` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| RecoveryHashes | `Gagamba.Execution.dll` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| OwnerHashes | `Gagamba.Execution.Linux.M1.Owner.dll` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| OwnerHashes | `Gagamba.Execution.Linux.M1.Owner.deps.json` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| OwnerHashes | `Gagamba.Execution.Linux.M1.Owner.runtimeconfig.json` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| OwnerHashes | `Gagamba.Execution.Linux.M1.dll` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| OwnerHashes | `Gagamba.Execution.dll` | Sha | Once | No | Both | BB §4 (O) | Exact annex |
| PreIdentity | `ArtifactSetSha256` | Sha | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `OwnerClosureSha256` | Sha | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `RecoveryClosureSha256` | Sha | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `CoordinatorSha256` | Sha | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `SeccompFilterSha256` | Sha | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `RuntimeInventorySha256` | Sha | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `Generation` | Sha | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `ManifestSchema` | One | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `OwnerProtocol` | One | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `CoordinatorProtocol` | One | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `Bootstrap` | One | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `ProfileId` | ProfileId | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `ProfileVersion` | ProfileVersion | Once | No | Both | BB §4 (H) | Exact annex |
| PreIdentity | `Qualified` | False | Once | No | Both | BB §4 (BJ) | Exact annex |
| PreIdentity | `qualificationId` | Null | Once | Only null | Both | BB §4 (BJ) | Exact annex |
| PreIdentity | `qualificationRecordSha256` | Null | Once | Only null | Both | BB §4 (BJ) | Exact annex |
| Controller | `Pid` | Pid | Once | No | I | BB §4 (O) | Exact annex |
| Controller | `Uid` | Principal | Once | No | I | BB §4 (O) | Exact annex |
| Controller | `Gid` | Principal | Once | No | I | BB §4 (F+O) | Exact annex |
| Controller | `PidNamespace` | PidNs | Once | No | I | BB §4 (O) | Exact annex |
| Controller | `UserNamespace` | UserNs | Once | No | I | BB §4 (O) | Exact annex |
| Runtime | `HostPath` | Path | Once | No | I | BB §4 (H+O) | Exact annex |
| Runtime | `HostSha256` | Sha | Once | No | I | BB §4 (O) | Exact annex |
| Runtime | `RuntimeDirectory` | Path | Once | No | I | BB §4 (H+O) | Exact annex |
| Runtime | `RuntimeInventorySha256` | UpperSha | Once | No | I | BB §4 (O) | Exact annex |
| Runtime | `HostFxrDirectory` | Path | Once | No | I | BB §4 (H+O) | Exact annex |
| Runtime | `HostFxrInventorySha256` | UpperSha | Once | No | I | BB §4 (O) | Exact annex |
| Runtime | `RuntimeVersion` | RuntimeVersion | Once | No | I | BB §4 (O) | Exact annex |
| Profile | `OwnerDllPath` | Path | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `DelegatedParentPath` | Path | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `SourceRootPath` | Path | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `StagingParentPath` | Path | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `JournalDirectory` | Path | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `OwnerDllSha256` | Sha | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `ToolSourceName` | Tool | Once | No | I | BB §4 (F) | Exact annex |
| Profile | `InputSourceName` | Input | Once | No | I | BB §4 (F) | Exact annex |
| Profile | `CoordinatorSourceName` | Coordinator | Once | No | I | BB §4 (F) | Exact annex |
| Profile | `FilterSourceName` | Filter | Once | No | I | BB §4 (F) | Exact annex |
| Profile | `ToolSha256` | Sha | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `InputSha256` | Sha | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `CoordinatorSha256` | Sha | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `ToolEnrollmentId` | ValidId | Once | No | I | BB §4 (F) | Exact annex |
| Profile | `InputEnrollmentId` | ValidId | Once | No | I | BB §4 (F) | Exact annex |
| Profile | `TotalBudgetBytes` | Budget | Once | No | I | BB §4 (F) | Exact annex |
| Profile | `MaxObjects` | Four | Once | No | I | BB §4 (F) | Exact annex |
| Profile | `ExpiresLeadNanoseconds` | Lead | Once | No | I | BB §4 (F+O) | Exact annex |
| Profile | `IntegratedRecovery` | Null | Once | Only null | I | BB §4 (O) | Exact annex |


### Scalar families assessed

- Canonical integer tokens with inclusive version/constant/domain limits.
  No Boolean-to-number coercion, float or exponent. UID, GID and PID
  are positive signed-31-bit values. Deadline/time tokens use positive i64.
  Namespace inode decimals use positive u64. Runtime patch uses canonical
  nonnegative signed-31-bit decimal.
- Nonzero lowercase unhyphenated hashes where required, field-specific
  uppercase runtime inventory strings, raw byte equality where a comparison
  crosses case conventions. BJ-03 imposes its separate exact lowercase
  literal grammar and BJ-04 compares all 32 bytes.
- Canonical nonempty lowercase hyphenated GUID text, existing .NET GUID
  order in binary frames, canonical standard padded Base64 for 32 secret
  octets, and bounded ValidId and ASCII POSIX path grammar.
- Fixed profile/version, fixed F2 basenames, budget and count, bounded
  expiry lead, B2-only member prohibition, and integrated structural
  equalities. Filesystem facts, time and custody are verified by callers
  at their separate gates.

Classification across the 99 rows is exact candidate match by
incorporation, with zero missing/extra rows, type, presence, nullability or
version discrepancies. This maps a complete static inventory. The
bounded vector matrix exercises representative refusals and full positive
coverage rather than every mutation of every row.
