# Closed member inventory and completeness audit

Normative appendix to the [owner decision](../rc2b-06-b6-alt-b-slice-e-03-qe1-bootstrap-base-owner-01.md). The scalar and
cross-field definitions in this appendix and in the decision apply together.
This schema applies only to the two v3-pre bootstrap forms. It specifies a
structural data value. Bound installation, protected metadata and process
identities are established by a separate future caller.

**Every listed member is required exactly once.** Only `Null` permits null.
Omission, unknown members, duplicates and type/value violations refuse.
There are no optional members, implicit defaults or arrays. Member names use
ordinal, case-sensitive equality of decoded JSON names. This includes escaped
names when checking duplicate keys. In integrated, both B2-only root members
are prohibited even at null. B2 requires both and has literal null
`DelegatedParent` and `Integrated`. No other member from the opposite topology
may be merged into either root. All objects use closed sets including the two
role hash maps. The tables and scalar definitions close the inventory without
requiring code inspection.

## Authority key

- **H** Historical accepted requirement inherited where applicable. Exact JSON lexical choices in the scalar table are owner-selected wherever the historical source does not specify them.
- **F** Historical F2 meaning, value or constraint inherited. Placement and JSON spelling for the bootstrap projection are owner-selected where the source describes a mapping rather than an exact JSON member.
- **O** New v3 owner binding. Current production shape informs the inventory, but its serializer and defaults carry no normative authority.
- **BJ** Prior owner BJ-01–04, preserved.

An entry with mixed authority combines a historical domain with a newly
selected explicit representation or location. A table's tests are coverage
witnesses for all required members, plus the targeted negatives where shown.
Full positives P-B2 and P-I exercise every required member. Missing, null,
wrong kind, duplicate and unknown behavior applies to every object even
where a group uses a representative mutation.

## Scalar domains

All regular expressions are anchored to the entire **decoded** text, ordinal
and ASCII. Numeric ranges include endpoints. Integer tokens use decimal
`0` or a nonzero digit followed by ASCII digits, with no exponent, fraction,
minus, plus or leading zero. Reject overflow rather than rounding or
coercion. Whole-message parsing uses the strict UTF-8 single-object rule in
the decision. The sole additional literal token-interior constraint is the
prior BJ-03 hash rule.

| Type | Exact domain | Authority |
|---|---|---|
| V3, One, Four, Budget | JSON integer token exactly `3`, `1`, `4`, `16777216` respectively | F1 and prior owner for version, SPEC §6.2.2 for identity constants, F2 §3 and line 94 for profile budget and count |
| I64+, Lead | JSON integer 1–9223372036854775807 for I64+; Lead 1–120000000000. External session and deadline limits still apply | B2 §2; F2 §3. O binds integrated time tokens to I64+ |
| Pid, Principal | JSON integer 1–2147483647. Pid is a process ID; Principal is a non-root UID or GID | F1 original actor premise, F2 §3 approved principals; O binds projection widths |
| Guid | JSON string `[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}`, excluding all zero | O binds textual encoding for original nonempty identifiers. No new GUID version/variant restriction. This choice does not change existing .NET binary GUID order |
| Secret32 | JSON string of RFC 4648 standard Base64, exactly 44 ASCII characters including one final `=`, representing exactly 32 octets. Require the standard alphabet and canonical zero pad bits (decode then standard encoding must equal the decoded string). No spaces, URL alphabet or missing padding. Retained per-session association data, with no authority projected by the codec | Original B2 32-byte secret; O binds the textual encoding |
| Path | Decoded ASCII POSIX path, 2–1024 bytes, exactly one initial slash, one or more slash-separated components of `[!-~]` excluding slash and backslash inside components, and no empty, `.` or `..` component or trailing slash. No rewriting, resolving, or filesystem lookup. Additional bindings in the decision | F2 §3 for protected host path shape; O applies one deterministic lexical rule to all bootstrap paths; path existence, legitimacy and protection stay external |
| Sha | JSON string of exactly 64 lowercase ASCII hex characters, at least one nonzero. Identity/closure digest semantics from its member | B2 §2 and SPEC identity fields. O binds lowercase for non-identity legacy projection fields selected here |
| UpperSha | JSON string of exactly 64 uppercase ASCII hex characters, at least one nonzero. Decode to raw digest when comparing with an identity projection | O binds the two runtime-inventory spellings to the installed runtime representation observed in LoadedInstallation. This rule does not apply to the identity object or AuthorityHash |
| CapabilityHex | Prior BJ-03. Required JSON string with exactly 64 **literal** lowercase ASCII hex characters in its token interior, no escapes/aliases. Compare the decoded 32 bytes as in BJ-04. No further value restriction | Prior owner BJ-03/04 |
| PidNs, UserNs | JSON strings of `pid:[N]` and `user:[N]` respectively. N has canonical positive decimal digits and fits 1–18446744073709551615. This is a structural syntax; actual namespace correspondence is separately verified | O binds deterministic reference spelling informed by DirectPeerIdentity |
| RuntimeVersion | JSON string `10.0.N` with N a canonical decimal integer 0–2147483647. No prerelease, extra component or alternate spelling | O binds the existing installed S2 runtime shape documented by InstalledManifest and uses the original verified runtime version |
| ValidId | Case-sensitive JSON string of 1–128 ASCII `[A-Za-z0-9._-]` without leading or trailing dot | F2 §3, accepted P0 ValidId reference. No mandatory P1/P2 helper reuse |
| ProfileId, ProfileVersion | JSON strings exactly `ae4-linux-fixed-fixture-v1` and `1` | SPEC §6.2.2 |
| PreMode | JSON string exactly `validation-pre` | F1 line 46 |
| Tool, Input, Coordinator, Filter | JSON strings exactly `tool`, `input`, `coordinator`, `filter` respectively | F2 §3 and line 94 |
| False, Null | JSON Boolean false or literal JSON null, respectively. Each applies only to its designated members | BJ-01/02, inherited B2 shape and O Profile.IntegratedRecovery rule |
| Other names in the Type column | Non-null JSON objects defined by the matching closed inventory below | Authority per table/member |

The synthetic material in witnesses is ordinary data. No application should
derive a protected path, capability, secret or context from a test witness.

## B2

Source and design basis. B2 §2, original B2 evidence §private bootstrap, wire Q1 §9.1 and RecoveryProtocol:129–177. Identity grammar from SPEC §6.2.2 and BJ.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `Version` | V3 | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2; N03/N06/N11 representative |
| `Execution` | Guid | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2; N03/N06/N11 representative |
| `Generation` | Guid | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2; N03/N06/N11 representative |
| `Session` | Guid | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-B2; N03/N06/N11 representative |
| `ControllerPid` | Pid | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2; N03/N06/N11 representative |
| `ControllerUid` | Principal | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2; N03/N06/N11 representative |
| `Secret` | Secret32 | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-B2; N03/N06/N11 representative |
| `Recovery` | RecoveryPin | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2; N03/N06/N11 representative |
| `Owner` | OwnerPin | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2; N03/N06/N11 representative |
| `EvidenceDirectory` | Path | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2; N03/N06/N11 representative |
| `DelegatedParent` | Null | Yes | Only null | No | Wrong type/value, absent or duplicate | H | P-B2; N03/N06/N11 representative |
| `Integrated` | Null | Yes | Only null | No | Wrong type/value, absent or duplicate | H | P-B2; N03/N06/N11 representative |
| `B2InstallationIdentity` | PreIdentity | Yes | No | No | Wrong type/value, absent or duplicate | H+BJ | P-B2; N03/N06/N11 representative; N01 |
| `B2DeadlineNs` | I64+ | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2; N03/N06/N11 representative; N02/N21 |
| `AuthorityMode` | PreMode | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2; N03/N06/N11 representative |
| `AuthorityHash` | CapabilityHex | Yes | No | No | Wrong type/value, absent or duplicate | BJ | P-B2; N03/N06/N11 representative; N10/N18 |

## Integrated

Source and design basis. SPEC §6.2.2, WIRE §D.2, B2 §2 and F1 line 46. Base transport shape from wire Q1 §9.1 and RecoveryProtocol:129–177. No B2-only top-level members.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `Version` | V3 | Yes | No | No | Wrong type/value, absent or duplicate | H | P-I |
| `Execution` | Guid | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `Generation` | Guid | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `Session` | Guid | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-I |
| `ControllerPid` | Pid | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `ControllerUid` | Principal | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `Secret` | Secret32 | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-I |
| `Recovery` | RecoveryPin | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `Owner` | OwnerPin | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `EvidenceDirectory` | Path | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `DelegatedParent` | Path | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `Integrated` | IntegratedBody | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-I |
| `AuthorityMode` | PreMode | Yes | No | No | Wrong type/value, absent or duplicate | H | P-I |
| `AuthorityHash` | CapabilityHex | Yes | No | No | Wrong type/value, absent or duplicate | BJ | P-I; N10/N18 |

## IntegratedBody

Source and design basis. SPEC §6.2.2, wire Q1 §9.3, IntegratedRecoveryHost:13–37 and F2 line 94.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `Profile` | Profile | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `Issuer` | Guid | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `StoreGeneration` | Guid | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `AcceptedAtNs` | I64+ | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `ExpiresAtNs` | I64+ | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `Controller` | Controller | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I; N05 |
| `Runtime` | Runtime | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-I |
| `InstallationIdentity` | PreIdentity | Yes | No | No | Wrong type/value, absent or duplicate | H+BJ | P-I |

## RecoveryPin

Source and design basis. B2 §2, RecoveryProtocol:105–125 and LoadedInstallation:37–42.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `Directory` | Path | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Sha256` | RecoveryHashes | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |

## OwnerPin

Source and design basis. B2 §2, RecoveryProtocol:105–125 and LoadedInstallation:37–42.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `Directory` | Path | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Sha256` | OwnerHashes | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |

## RecoveryHashes

Source and design basis. RecoveryProtocol:109–114 and LoadedInstallation:37–42, role-specific fixed closure.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `Gagamba.Execution.Linux.M1.Recovery.dll` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Gagamba.Execution.Linux.M1.Recovery.deps.json` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Gagamba.Execution.Linux.M1.Recovery.runtimeconfig.json` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Gagamba.Execution.Linux.M1.dll` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Gagamba.Execution.dll` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |

## OwnerHashes

Source and design basis. RecoveryProtocol:109–114 and LoadedInstallation:37–42, role-specific fixed closure.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `Gagamba.Execution.Linux.M1.Owner.dll` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Gagamba.Execution.Linux.M1.Owner.deps.json` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Gagamba.Execution.Linux.M1.Owner.runtimeconfig.json` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Gagamba.Execution.Linux.M1.dll` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |
| `Gagamba.Execution.dll` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-B2, P-I |

## PreIdentity

Source and design basis. SPEC §§6.1 and 6.2.2, B2 §2, F1 lines 44–48, prior normative owner BJ-01 and BJ-02.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `ArtifactSetSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `OwnerClosureSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `RecoveryClosureSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `CoordinatorSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `SeccompFilterSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `RuntimeInventorySha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I; N19 (Runtime only) |
| `Generation` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `ManifestSchema` | One | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `OwnerProtocol` | One | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `CoordinatorProtocol` | One | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `Bootstrap` | One | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `ProfileId` | ProfileId | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `ProfileVersion` | ProfileVersion | Yes | No | No | Wrong type/value, absent or duplicate | H | P-B2, P-I |
| `Qualified` | False | Yes | No | No | Wrong type/value, absent or duplicate | BJ | P-B2, P-I; N09 |
| `qualificationId` | Null | Yes | Only null | No | Wrong type/value, absent or duplicate | BJ | P-B2, P-I; N07 |
| `qualificationRecordSha256` | Null | Yes | Only null | No | Wrong type/value, absent or duplicate | BJ | P-B2, P-I; N08 |

## Controller

Source and design basis. F1 actor identity constraints, F2 §3 and DirectPeerIdentity:11–22, IntegratedRecoveryHost:31–37.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `Pid` | Pid | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I; N16 |
| `Uid` | Principal | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `Gid` | Principal | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `PidNamespace` | PidNs | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `UserNamespace` | UserNs | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |

## Runtime

Source and design basis. SPEC §6.2.2, ExperimentalRuntimePin:12–29, LoadedInstallation:43–50 and AB lines 21/42.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `HostPath` | Path | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-I |
| `HostSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I; N04 |
| `RuntimeDirectory` | Path | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-I |
| `RuntimeInventorySha256` | UpperSha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I; N19 (Runtime only) |
| `HostFxrDirectory` | Path | Yes | No | No | Wrong type/value, absent or duplicate | H+O | P-I |
| `HostFxrInventorySha256` | UpperSha | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |
| `RuntimeVersion` | RuntimeVersion | Yes | No | No | Wrong type/value, absent or duplicate | O | P-I |

## Profile

Source and design basis. F2 §3 and line 94, B3Qualification:35–57, IntegratedB3Client:63–66, IntegratedRecoveryHost:31–44.

| Member | Type | Required | Null accepted | Omission accepted | Refusal | Authority | Vector coverage |
|---|---|---|---|---|---|---|---|
| `OwnerDllPath` | Path | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `DelegatedParentPath` | Path | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `SourceRootPath` | Path | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `StagingParentPath` | Path | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `JournalDirectory` | Path | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `OwnerDllSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `ToolSourceName` | Tool | Yes | No | No | Wrong type/value, absent or duplicate | F | P-I |
| `InputSourceName` | Input | Yes | No | No | Wrong type/value, absent or duplicate | F | P-I |
| `CoordinatorSourceName` | Coordinator | Yes | No | No | Wrong type/value, absent or duplicate | F | P-I |
| `FilterSourceName` | Filter | Yes | No | No | Wrong type/value, absent or duplicate | F | P-I |
| `ToolSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `InputSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `CoordinatorSha256` | Sha | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `ToolEnrollmentId` | ValidId | Yes | No | No | Wrong type/value, absent or duplicate | F | P-I |
| `InputEnrollmentId` | ValidId | Yes | No | No | Wrong type/value, absent or duplicate | F | P-I |
| `TotalBudgetBytes` | Budget | Yes | No | No | Wrong type/value, absent or duplicate | F | P-I |
| `MaxObjects` | Four | Yes | No | No | Wrong type/value, absent or duplicate | F | P-I |
| `ExpiresLeadNanoseconds` | Lead | Yes | No | No | Wrong type/value, absent or duplicate | F+O | P-I |
| `IntegratedRecovery` | Null | Yes | Only null | No | Wrong type/value, absent or duplicate | O | P-I |

Production scope exclusions are explicit. Integrated root forbids
`B2InstallationIdentity` and `B2DeadlineNs`. Profile forbids
`QualificationFault`, `QualifiedInstallation` and all other unlisted
names. `IntegratedRecovery` is required literal null: it is the observed
null recursion cut, adopted here for v3, and does not open a reachable
second integrated object. Root identity placement cannot be replaced by
a context member or shadowed in a second location. Runtime and role hash
objects carry no extra authority or digest fields.


## Explicit prohibitions

| Object | Member | Required | Null accepted | Omission accepted | Authority | Witness |
|---|---|---|---|---|---|---|
| Integrated root | `B2InstallationIdentity` | No, prohibited | No | Yes, required | B2 §2, F1, BB-01 | P-I/N01 |
| Integrated root | `B2DeadlineNs` | No, prohibited | No | Yes, required | B2 §2, F1, BB-01 | P-I/N02 |
| Profile | `QualificationFault` | No, prohibited | No | Yes, required | O, production separation | P-I/N17 |
| Profile | `QualifiedInstallation` | No, prohibited | No | Yes, required | O, production projection | P-I |
