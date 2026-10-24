# RC-1 independent GLM contract review

Date: 2026-10-25 (Asia/Manila). Reviewer: GLM (independent contract-design
review assignment). Review target:
[RC-1 restricted consumer contract](../../design/restricted-consumer-contract-rc1.md).

**Independent disposition: RC-1 ACCEPTED WITH QUALIFICATIONS.**

This is a source-grounded design review only. It is not implementation
authorization, not an executed test result, not a package qualification and not
native/runtime qualification. RC-1 design acceptance does not close Hufu
AE4-B01; that blocker remains open until an independently accepted RC-2A
implementation is consumable at an exact version.

## A. Exact Git binding

All identities below were re-resolved from the working tree during review;
none are copied from the producer's documents alone.

| Item | Identity | Verified |
|---|---|---|
| Design commit | `d609c29d3048bc0fd91583068cb013fb906fdf90` | Yes: `git rev-parse HEAD` in the dedicated RC-1 worktree |
| Design tree | `d09ee7d079e44ecb645e7173bc99e11979ffee39` | Yes: `git rev-parse "HEAD^{tree}"` |
| Parent / selected restricted baseline | `629a08d2ee82a9ac084d87ed73ad8d584661ea28` | Yes: `git rev-parse HEAD^` |
| Unchanged src tree | `9dd0f055eaf48d48bda8683dee3f8dc534a06e5d` | Yes: `git rev-parse HEAD:src` |
| Unchanged tests tree | `ebcb976a164b212090fbe0958fe4b0c0b243f794` | Yes: `git rev-parse HEAD:tests` |
| Unchanged native tree | `85631d2f04512376621d3d09f51d67fd90f45335` | Yes: `git rev-parse HEAD:native` |
| Review branch | `review/gagamba-rc1-glm-independent-contract` | Created from the exact design commit in a new worktree |
| Review worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc1-glm-review` | Clean before and after this review commit |
| RC-1 design worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc1-contract` | Inspected at the exact commit; working tree clean |
| Completion receipt | `Solo\artifacts\gagamba-rc1-tools\completion-receipt.json` | Claims checked individually (see K.4) |

Consumer-side identities verified during review:

| Item | Identity | Verified |
|---|---|---|
| Hufu AE-4 implementation evidence | `37dcf33362e8e745800a530a478fd406795c3e58` (worktree `Solo\hufu-ae4-implementation`, branch `implementation/ae4-integration-plan`, clean) | Yes: `git rev-parse HEAD`; src tree `2a9a9c60b833d6281f6e8528f767a7750c4411f4` and tests tree `955ca47e1d327f262dc516264fbe4bc0ac78429e` match the handoff |
| Hufu earlier AE-4 design | `236948509ee2e608e2e9a8461fabc379e037b65b` (worktree `Solo\hufu-ae4-design`) | Yes |
| Hufu canonical main | `0de6f1b667d07c5b6dfe1506d2511896c9e47b03` | Yes: ancestor of `37dcf333`, not a later tip; the AE-4 line is the newest evidence |
| D6.7 human C1–C4 approvals | `01063c164391e0676ae1f6087d2c0865f7692511` (worktree `Gagamba-d6-7`) | Yes: approval document read; matches RC-D06 (see C.6) |

Local checkout observations recorded for honesty: the canonical
`C:\Users\Laszlos\source\repos\Gagamba` checkout is on an unrelated review
branch with untracked test artifacts; it was not used for this review. The
canonical `Penghou.Hufu` checkout at main `0de6f1b` does not contain the AE-4
documents; they exist on the dedicated branch in the Solo clone worktrees.
These are environment facts, not discrepancies with the reviewed material.

## B. Review method and evidence boundary

Performed: read-only Git identity resolution; full reading of the three
required RC-1 documents plus evidence files; source reading of every named
implementation file at the exact commit; reading of the D6.7 approvals and the
Hufu AE-4 design, stage register, entry-check (AE4-B01) and A01–A03
amendments; and three deterministic static re-computations using an independent
Python 3.14 script (no Gagamba code executed):

1. SHA-256 of the 873-byte literal golden vector recomputed from the test
   source text: matches `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`.
2. The RC-D03 byte grammar applied mechanically to the golden hex: consumes
   exactly 873 bytes with zero remainder and reproduces every value asserted by
   `CanonicalGoldenBytes`/`SampleManifest` (see C.2).
3. SHA-256 of the LF-normalized bytes of five RC-1 documents recomputed and
   compared with `document-sha256.json`: all match (design doc
   `164496f886bdc324816bf963a1ad3398c9bb8e702bee6d97dc1e687d6e7d15fe`, plan,
   handoff, validation.json, remote-heads.txt).

Not performed: `dotnet build/test/pack`, any native workload, any package
restore/publication, any Gagamba or Hufu source modification. All RC-T01–T23
cases remain designed, not executed. "Test verified" below always means the
existing test source was inspected and found to assert what the design claims,
never that it was run by this reviewer.

## C. Contract correctness verification

### C.1 Manifest model completeness (RC-D02)

The D02 field table was compared field-by-field against
`src/Gagamba.Execution/Restricted/RestrictedManifest.cs` at the exact commit:
seven root fields; authority subtree; writable store; mount policy; the 13
budgets in the listed order; inode qualification; core dumps; owner
allocation; closure; closure object; invocation; channels. Every property,
type (`u16/u32/u64`, digest, string, lists) and nesting level in the table has
a matching get-only property in the source model, and the source model has no
execution-relevant field omitted from the table. Verified constructor
semantics match D02's "Current" claims exactly:

- `QualificationDisposition` is `Mandatory=0` / `PendingQualification=1` and
  `BudgetValueKind` is `Absent=0/Exact=1/Unlimited=2`
  (RestrictedManifest.cs:17, :25; pinned by `EnumNumbersArePinned`).
- Constructors enforce: empty capabilities; the three ambient denials; tool
  executable; network/inode qualification Pending; fixed paths `/tool`,
  `/input`, `/tmp`; stdin EOF; core disabled; present core pattern nonempty;
  absent/unlimited budgets carry zero; argv/env combined UTF-8 at most 16,384
  bytes (RestrictedManifest.cs:217).
- Constructors accept values needing later profile validation: `NoNewPrivs=false`,
  empty inventories and unspecified enum casts in `ResourceBudget` kind and
  qualification (e.g. `(BudgetValueKind)7` is constructible and encodable).
  D02's statement is accurate; "constructor success is not restricted-profile
  acceptance" is a correct reading of this source.
- Empty environment values are allowed; empty argv items are not
  (InvocationManifest, RestrictedManifest.cs:253–296). D02/D03 preserve this
  distinction.

The root carries only `SchemaVersion` + `ImplementationId`
(`linux-m1-native-transaction`); no package version, build hash, preparation
UUID or readback is representable in schema 1 (RestrictedManifest.cs:532).
`EstablishedManifest` provides Absent/Observed/Unknown with private
construction and no promotion route; D02's establishment separation is
faithful to the source.

### C.2 Canonical codec and byte grammar (RC-D03)

- `ManifestCodec` is the sole production encoder (RestrictedManifest.cs:628);
  no decoder exists in the class, and no other production encoder exists in
  the tree.
- The normative D03 grammar was mechanically parsed against the literal golden
  hex in `tests/Gagamba.Execution.Linux.M1.Tests/Contracts/ManifestTests.cs:553`:
  the parse consumes exactly 873 bytes with zero trailing remainder and
  reproduces every value of `SampleManifest()` — including sorted namespaces,
  empty capabilities, `noNewPrivs=1`, filter digest/length, sorted read-only
  destinations, sorted stores, the three denials, mount-policy bytes, network
  qualification byte, all 13 budgets in order, inode byte, core-dump bytes
  with absent core pattern, the seven owner-allocation integers, sorted closure
  by role, sorted helper dependencies, invocation order (argv preserved,
  environment sorted by key), and the four channel values. The grammar is
  complete and encoder-exact; SHA-256 of these bytes equals the recorded
  digest.
- No magic, BOM, terminator, padding or digest suffix appears in the encoding;
  the manifest contains no UUID bytes (`GuidToRfc4122Bytes` is protocol-side
  only).
- Ordered sets use `Utf8BytewiseComparer` (RestrictedManifest.cs:579):
  unsigned UTF-8 byte order, distinct from UTF-16 ordinal order, proven by the
  existing `SupplementaryUnicodeSortsByUtf8Bytes` and
  `ReferenceEncoderMatchesProductionOnPermutedNonAscii` tests. Argv order is
  preserved and duplicates allowed; set duplicates are rejected at
  construction; duplicate stores/paths/roles/keys/namespaces are covered by
  existing tests.
- `OwnerManifestWire` (JSON) is internal, separate transport, and
  `FromWire` rebuilds through the validated constructors with digest binding
  before use (`OwnerManifestWire.cs:57`); it is not and does not become the
  public binary decoder. `ReferenceEncoder.cs` is a test-only independent
  oracle that never calls `ManifestCodec`; the design correctly keeps it out
  of production and forbids Hufu consumption of it.
- Immutability: get-only properties, defensive copies to immutable snapshots,
  and `MutatingSourceArraysCannotAlterDigest` / mutation of captured arrays
  proven ineffective at the existing boundary (see E.2 for the public-view
  obligations this imposes on RC-2A).

The D03 validation layers (size ceiling before allocation; checked cursor with
count-vs-remaining checks; rejection of unknown versions/enums/tags/UTF-8/
truncation/trailing/duplicates/unsorted sets; application of existing model
invariants; final re-encode byte equality) are implementable over exactly the
invariants verified above. No missing invariant was found. The strict reader
"newly rejects undefined enum values that some internal constructors can
currently carry" is an accurate statement about the source.

### C.3 Preparation identity and capability (RC-D04)

- `PreparationIdentity`: four non-empty GUIDs plus a 32-byte digest
  (RestrictedManifest.cs:324). Verified.
- `PreparedTransaction` has a private constructor, minted only by the nested
  `PreparationStore`, consumed only through the store, with no clone or
  deserialize route (PreparedTransaction.cs:16).
- `PreparationStore.Mint` validates issuer/generation against the store,
  digest against the manifest encoding, future expiry, ID uniqueness and
  exclusive closure transfer before minting (PreparationStore.cs:70–124).
- `TryConsume` requires the minted object's reference
  (`ReferenceEquals(entry.Capability, candidate)`) plus issuer, generation,
  digest re-verification, expiry and unused state (PreparationStore.cs:167–214).
  A matching GUID/digest in a fabricated object is therefore insufficient,
  exactly as D04 requires; existing negative tests cover foreign issuer,
  foreign generation, duplicate consume, aborted, expired and fabricated
  identifier (`PreparationIdentityTests.cs:50,62,97,107,117,127`).
- D04's passive `RestrictedPreparationReference` (public validating ctor, no
  lookup/release/adoption/renewal) cannot reach any of these paths; the store
  is internal to M1 and not exposed by RC-2A. Deadlines/expiry are correctly
  carried only by provider-origin objects (see R1-F04).

### C.4 Release arbitration and C1–C4 fidelity (RC-D06)

`ReleaseArbiter` (ReleaseArbiter.cs:13) was read against the D6.7 approved
contracts and matches:

- C1: `AmbiguousAfterContent` → first-retainer order under the gate
  (retained decision first → Duplicate; stop accepted → consume capability and
  retain `Refused(MayHaveExecuted)`; otherwise retain
  `AmbiguousPersistence/MayHaveExecuted` with `Delivery.NotAttempted` and
  `LiveExecutionExcluded=true`), tail stop issued after the gate and journal
  lock are released, ambiguous stop limitations recorded without rewriting the
  retained decision (ReleaseArbiter.cs:116–178).
- C2: `RequestStop` consumes the capability at first runtime acceptance on
  every path (including clock-unavailable and deadline-overflow), retains
  first cause/deadline, is idempotent on duplicates and never restores
  authority on failed durable persistence (ReleaseArbiter.cs:293–364).
- C4: retained-release Duplicate; concurrent/pre-gate refusals; one physical
  RELEASE via single consumption; no waiting protocol.
- Release request preconditions: identity equality, digest re-verification
  against the live capability, setup strictly before overall, both deadlines
  in the future, prerequisites satisfied, `IsLive` capability, and the
  acknowledged journal snapshot's recorded preparation identity must match
  (ReleaseArbiter.cs:64–79, :421–429) — matching D04/D05's claim that no
  identifier-based route exists around the live-object association.
- D06's disclaimer about `Reconciler` is source-accurate: it passes the
  caller-supplied `_stopAllowance` to `StopAndAwaitEmpty`
  (Reconciler.cs:124,129,231), so it is not the approved durable-deadline C3
  implementation; the existing probe test
  `ReconcilerUsesCallerAllowanceAfterPersistedStopDeadlineExpired`
  (Wp05ReleaseContractProbes.cs:280) demonstrates exactly this. Likewise the
  current `OwnerClock` is a plain Stopwatch wrapper, not the sealed,
  corroborated C3 production clock; RC-1 correctly defers clock selection to
  C3's implementation gate.

D06's requirement table reproduces the D6.7 approvals (including Policy A's
"identity-checked kill plus one immediate population observation, no renewed
wait") without alteration, and RC-2A changes none of it.

### C.5 Owner machinery (RC-D05 "Current")

`OwnerPreparationTransaction` (OwnerPreparationTransaction.cs:19) and
`OwnerManifestAuthorizationCodec` are internal; the owner validates identity
against its own bootstrap (`ExecutionId`/`IssuerId`/`StoreGenerationId`),
proves durable allocation intent before staging, transfers authority by FD
passing with identity checks, records `Prepared` durably, and mints the
capability in the owner process only. Nothing in this path is a public
authorization hook for Hufu, matching D05's claim and the AE-4 entry check.
`M1RecoveryScope` is an in-process scope retaining parent/staging/domain
handles, subreaper and owner pidfds (M1RecoveryScope.cs:72); the design's
treatment of the standalone surviving recovery process as new RC-2B
deployment work (B2) is honest and correct.

### C.6 Lifecycle separation (RC-D08) and packaging (RC-D09)

- Public `IExecutionProvider` (Provider.cs:177) exposes only
  describe/prepare/discard/launch/terminate/wait; `ExecutionRuntime` selects
  Windows/Linux/macOS lifecycle providers by OS and refuses otherwise
  (ExecutionRuntime.cs:39,62–71). No restricted admission exists in the public
  surface; D08's separation and default-disabled selection are implementable
  without touching this contract.
- `Gagamba.Execution` is packable net10.0; `Gagamba.Runtime` references
  Execution plus the three lifecycle provider projects; M1 and Owner are
  `IsPackable=false`; `Directory.Build.props` pins `0.1.0-preview.4`,
  warnings-as-errors and SourceLink as a private build dependency — all
  verified, matching D09's "Current" claims.
- Hufu `Penghou.Hufu.Sandbox.csproj` is net10.0, non-packable and pins
  `Gagamba.Runtime [0.1.0-preview.4]` — verified at `37dcf333`, matching
  D01/RC-1 §1.

## D. Security and authority review

- **Digest is identity evidence, not permission.** Verified in source: no
  public path from a digest or reference to `TryConsume`/`TryLookup`;
  consumption requires the minted object plus issuer/generation/expiry/unused
  state, and the arbiter re-checks identity, digest, deadlines, prerequisites
  and the journal snapshot. RC-2A adds no route around this.
- **Decoding cannot mint or reconstruct a live preparation.** `PreparedTransaction`
  has a private constructor and `Mint` requires an exclusively transferred
  `PreparedClosure` (PreparedClosure ownership via `TryTransferToStore`). The
  public codec/reference exposes no capability surface; RC-T07/T09 negatives
  enforce this at the public boundary.
- **No substitution of manifest/issuer/generation.** Digest is re-verified at
  mint, at consume and inside the arbiter; the owner rejects authorization
  whose identity does not match its transaction; the RC-2B release request is
  required to pass the exact prepared view object created by the live
  operation, checked by local object association and complete retained
  identity (D04/D05). The owner wire exchange binds digests before use.
- **No release-capability exposure in RC-2A.** The tranche is
  inspect/encode/decode/digest/supports plus a passive reference; the release
  arbiter, journal, store, FDs, release pipe/frame, writer leases, owner
  sockets, paths, barriers and `ReleasePrerequisites` remain private forever
  at this boundary, per D02/D04, and RC-T11 asserts the negative surface.
- **Ordinary lifecycle providers cannot satisfy restricted selection.** D08
  keeps `IRestrictedExecutionProvider` separate, default disabled, no
  `IExecutionProvider` inheritance, no Launch adapter, no fallback; refusal
  before target initialization; Windows/macOS can parse values but cannot
  advertise the profile. Source-grounded and consistent with the inspected
  runtime selection.
- **Unsupported versions/platforms refuse explicitly.** Decoder error
  taxonomy is closed (`UnsupportedSchema`, `UnsupportedImplementation`, …);
  unknown versions get no best-effort decode, field drop or implicit
  migration. D03's compatibility rule (a patch may fix rejection bugs but
  cannot redefine accepted schema-1 bytes) protects existing golden identity.
- **No Hufu dependency enters Gagamba.** Verified: RC-1 changed documentation
  only (15 paths, all under `docs/`); RC-2A file scope adds no Hufu reference
  and explicitly rejects reflection/friend access/copied codecs.
- **No premature owner-facade promise.** RC-2A ships no control interface,
  provider registration, bootstrap, observers or dummy implementations;
  `gagamba-restricted-values-v1` is explicitly distinct from the later
  `gagamba-restricted-control-v1`, preventing the values identity from ever
  satisfying live provider selection.

Counterexample search (forged identities, malformed encodings, version
confusion, stale preparations, capability exposure) found no design-level
contradiction with the source model; the residual items are recorded as
findings in G.

## E. Hufu compatibility (AE4-D4a)

Verified against the AE4-B01 entry check, the AE-4 stage register §4
(D4a prerequisites: accepted P-API value/codec artifact at exact
source/version) and the A02 complete manifest acceptance table:

- Every field the A02 predicate enumerates (namespaces, capabilities,
  no_new_privs, filter hash/length, read-only/writable destinations, mount
  policy, ambient denials, network/inode qualification, closure objects and
  helper dependencies, invocation including Mode and argv, all budgets and
  units, owner allocations, core/runtime dump flags and observed core
  pattern, channels) is exposed by the D02 view with exact types. The host
  can match the full predicate without reflection, friend access or copied
  codecs.
- RC-1 §3's `ae4-linux-fixed-fixture-v1` parameter set matches the Hufu A02/M1
  envelope exactly (verified value-by-value, including canonical environment
  key order HOME < LANG < TMPDIR, and the explicit `RuntimeDumpsDisabled=false`
  non-claim).
- **Mode mapping.** Hufu A02 requires `Invocation.Mode` to be explicitly
  selected by the Gagamba-owner contract acceptance, not guessed from a
  non-empty-string constructor. RC-1 §3 selects `"work"` and requires RC-1
  acceptance to include the mapping. The fixture source
  (`tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c:16,22,28`) confirms the
  described behavior: default mode prints fixed useful-work text; `--fail` and
  `--ctor-marker` select the other modes; the constructor marker is written by
  the constructor function; the fixture does not read input. The mapping is
  descriptive-only, adds no argv item, and is consistent with Hufu G11.
- Pin parameters (`ProviderBuild`, `ClosureObjects`, `HelperDependencies`,
  `ReadOnlyDestinations`, `FilterSha256`, `FilterLength`,
  `CorePatternObserved` + qualified host condition, `HostQualificationId`)
  satisfy A02's "required enrollment parameters, not wildcards" rule;
  RC-1 correctly states these cannot truthfully be filled with native artifact
  hashes today and supplies source-bound synthetic vectors for D4a with the
  distinction explicit.
- `contractId = "gagamba-restricted-values-v1"`, exact accepted Execution
  NuGet version, and `artifactSetSha256` as SHA-256 of the exact retained
  artifact-inventory file bytes are workable values for the Hufu binding's
  `provider` object; the RC-2A work order (step 6) produces that inventory
  consistently.
- `preparedEvidenceSha256` stays synthetic in D4a; the real
  provider-origin establishment receipt is correctly deferred to RC-2B
  B1/B3. D4a must not and per this design cannot manufacture establishment.
- The dependency-graph risk (Hufu pins `Gagamba.Runtime [0.1.0-preview.4]`
  which transitively pulls `Gagamba.Execution 0.1.0-preview.4`) is honestly
  handled: RC-1 requires Sol to validate the mixed graph before D4a, treats
  incompatibility as an unresolved gate rather than a muted warning, and
  stops for amendment if needed.

Conclusion: RC-2A, once implemented, independently accepted and consumable at
an exact version, satisfies the value/codec portion of AE4-B01 for
deterministic no-launch D4a. D4a does not require RC-2B (Hufu's own stage
register makes the facade a D4b prerequisite); conversely D4b cannot proceed
without the facade, and RC-1 says so.

## F. Test inventory coverage (RC-T01–T23)

Verified: the plan defines RC-T01–T14 plus nine additional cases (T15–T23),
each with class (V/I/N) and tranche. The RC-2A subset (V/I/package) is
explicitly separated from native/2B portions; the plan warns the reviewer to
reject claims that whole integration cases passed based on the subsets.

Existing tests the design relies on were inspected and exist with the claimed
assertions: golden byte/digest identity; digest stability across equivalent
constructions; source-array mutation isolation; collection permutation byte
identity; supplementary-plane and non-ASCII UTF-8 ordering; duplicate
namespace/role/key/store rejection; absent/zero/unlimited distinctness;
invalid digest/identity/schema/implementation/denial/path/channel rejections;
lone-surrogate strictness; RFC 4122 GUID vector; enum value pinning;
independent `ReferenceEncoder` agreement; preparation store negatives
(foreign issuer/generation, duplicate consume, aborted, expired, fabricated
identifier); C1/C2 arbiter suites; the reconciler-allowance probe.

Coverage observations (non-blocking):

- RC-T05 (truncation at every byte boundary, tag/length corruption, invalid
  UTF-8, unsorted sets) plus RC-T15 (65,536/65,537 boundary, 16,384 argv/env
  boundary, count-vs-remaining checks) give the hostile-decoder boundary
  meaningful negative coverage; canonical counterparts act as positive
  controls. Sufficient.
- RC-T02/T03/T16 together give an exhaustive per-field mutation and
  field-to-property/field-to-wire inventory; T03's forced-field rejection list
  is derivable from the constructor set verified in C.1 (version,
  implementation ID, fixed paths, the three ambient denials, tool-executable,
  network/inode pending tags, empty capabilities, stdin EOF, core disabled),
  while `NoNewPrivs=false` and budget-kind/value variations are correctly
  treated as representable semantic changes with different bytes rather than
  structural rejections.
- Redundancy: RC-T06 overlaps RC-T05 on duplicate/unsorted inputs, but their
  scopes differ (malformed-byte refusal vs semantic distinctness of
  null/present, empty/nonempty, absent/zero/unlimited); retaining both is
  justified, not redundant.
- Gaps (see R1-F05): the matrix does not explicitly name `VerifySha256`
  wrong-length digest refusal (31/33 bytes, non-all-zero absence rule),
  `Supports` boundary arguments, or an encode-side oversized-view case.
- Assertions that cannot be established without native execution are properly
  excluded from RC-2A acceptance (N-class portions of T07–T13 and all of
  T14/T18–T23).

## G. Findings

All findings are non-blocking for RC-2A. No Blocking or Required-amendment
finding was identified: no source-level contradiction, missing invariant,
authority leak or unsupported assumption was found in the proposed contract.

| ID | Severity (classification) | Evidence / exact location | Violated requirement or unsupported assumption | Counterexample and impact | Minimum required correction | Owning component | Blocks |
|---|---|---|---|---|---|---|---|
| R1-F01 | Qualified limitation (specification gap; must be fixed in the RC-2A commission) | RC-1 D03 validation layer 1; `RestrictedManifestCodec` sketch (D03); RC-T15 | The 65,536-byte ceiling is specified for `Decode` transport input; `Encode(RestrictedManifestView)` behavior for a view whose canonical bytes exceed 65,536 is not stated (refusal result/exception unnamed). Internally constructible manifests can exceed 65,536 bytes because collection counts are not constructor-bounded, so Encode needs an explicit answer. | A consumer builds a valid internal-equivalent view with >65,536 canonical bytes; `Decode` refuses such bytes by design while `Encode` semantics are undefined → RC-2A could ship an asymmetric codec or, worse, encode bytes its own decoder refuses. Impact is limited (AE-4 profile is ~873 bytes; owner configuration is stricter) but the commission must not leave it implicit. | The RC-2A commission (RC2A-01) must state that public `Encode` also refuses views producing >65,536 canonical bytes (refusal result or exception, named and tested), preserving `Decode(Encode(v))` totality for accepted views, and add the encode-side boundary case beside RC-T15. | RC-2A commission (RC2A-01) | Blocks nothing if fixed at commission; RC-2A implementation must not start before it is fixed in the commission text |
| R1-F02 | Deferred (RC-2B semantic specification) | D05 "Stop can be submitted before/during preparation… Once stop wins before any capability exists, preparation must not mint a releasable one"; plan B1/B3; RC-T18 | New facade obligation beyond the current store/arbiter (which only see a minted capability). Not a contradiction: it adds an admission-stage obligation without altering arbiter/capability semantics, and it is listed as a Hufu semantic delta. | RC-2B implemented naively could mint a releasable capability while a provider-local stop is pending, which the design forbids but existing code cannot express. | The RC-2B commission must specify the pre-mint stop latch as in-memory operation state with its own observable fact (D07 stop-submission axis) and test it (RC-T18 "queued stop before mint"). | RC-2B (B1/B3) | RC-2B only |
| R1-F03 | Deferred (RC-2B semantic ambiguity) | D05 approval correlation: "Before retention, a mismatch refuses admission without a second attempt" | Ambiguous whether a later correctly-correlated call on the same operation may proceed after a pre-retention mismatch, and whether the mismatch refusal itself is retained. C1–C4 are preserved either way; the facade result algebra needs one chosen reading. | Two hosts with different correlations call the same operation before any retention; unclear whether the second (matching) call can still release. | The RC-2B B1 commission must pin the pre-retention mismatch rule (recommend: refuse that call without retaining a decision and without consuming anything; document whether a subsequent matching call is admissible) and test it (RC-T23 subcase). | RC-2B (B1) | RC-2B only |
| R1-F04 | Qualified limitation (explicit in the design; record for the consumer) | D04 passive `RestrictedPreparationReference` (five fields, no expiry/deadline); Hufu A02 `preparation` object carries deadlines separately | A consumer could read temporal validity into the passive reference. The design already scopes deadlines to provider-origin objects and synthetic D4a values. | Hufu D4a binding built from a passive reference only would have no basis for expiry facts and could over- or under-assert the A02 `acceptedAtNs/expiresAtNs/setup/overall` fields. | None in RC-2A. Hufu must take deadlines from its enrolled/synthetic data (D4a) and later only from `PreparedRestrictedExecution` (D4b); the limitation must remain stated in consumer-facing package docs. | RC-2A docs + Hufu delta review | Blocks nothing; qualification to record |
| R1-F05 | Informational (test inventory additions, non-blocking) | Plan §4 matrix; RC-T05/T15 | No explicitly named assertions for: `VerifySha256` with 31/33-byte digests and the all-zero-absence rule; `Supports` argument boundaries; encode-side oversized view (ties to R1-F01). | An RC-2A implementation could accept wrong-length digests in `VerifySha256` (violating "requires exactly 32 bytes") without failing a named case. | Add these negative assertions to the RC-2A commission's accepted subset (folding into T05/T15 is fine). | RC-2A commission | Blocks nothing |
| R1-F06 | Informational (evidence environment) | Hufu AE-4 documents exist at `Solo\hufu-ae4-implementation` (branch `implementation/ae4-integration-plan`, commit `37dcf333`), which is ahead of canonical Hufu main `0de6f1b` (an ancestor); canonical `Penghou.Hufu` checkout does not contain them | None. The RC-1 producer's citations are accurate; only the physical location differs from a naive expectation. | A future reviewer looking only at `Penghou.Hufu` main would not find the AE-4 documents and might wrongly conclude the consumer evidence is missing. | Record the exact worktree/branch binding (this review does; HUFU-R recheck at RC-2A acceptance). | Process/evidence hygiene | Blocks nothing |
| R1-F07 | Informational (verified-accurate disclosures, no action) | RC-1 §1 capability inventory; D06/D09 "Current" statements; evidence `input-and-vector-check.json` | None found. Every "Current" claim was checked against source and is accurate: no decoder exists; `ReleasePrerequisites` is a documented WP05 fake; the `Reconciler` passes a caller allowance (not C3); the C1 independent review is absent from the inspected inventories; M1/Owner are non-packable; `OwnerClock` is not the sealed corroborated C3 clock; the golden is 873 bytes with the recorded digest; document hashes match. | — | None. These stay as explicit gates (C1-R, C3-I, C4-I, G3–G5). | — | Blocks nothing |

Design-flaw vs out-of-scope distinction: R1-F02/R1-F03 are RC-2B design
specification debt, explicitly within the staged scope, not defects in the
accepted RC-2A contract. Native qualification, production registration,
package publication and Hufu resumption remain out of RC-1's scope as stated.

## H. Implementation-stage review

### RC-2A readiness

The work order is sufficient for a commissionable tranche: exact proposed
source files (new `Restricted/Contracts/` projections, `RestrictedManifestView`,
`RestrictedManifestCodec`, `RestrictedPreparationReference`, new
`ManifestReader.cs`, plus targeted public tests, with the M1 contract tests and
ReferenceEncoder oracle preserved); minimal public API changes in a small
namespace; canonical codec reuse (the wrapper delegates to the existing
encoder, one shared validation path for decoded and internally produced views);
deterministic positive and negative tests first with baseline recording;
public-API surface checks; a local, uniquely versioned
`0.1.0-rc2a.<commit>` package; clean isolated-cache consumer verification
without internals; and explicit stop conditions (schema drift, mutable
aliasing, codec duplication, hidden launch, unresolved Hufu graph
compatibility). There is no dependency on unfinished native runtime
integration. The file scope forbids friend assemblies for consumers, new
packages/TFMs, Linux M1 edits, and publication. Boundaries are clear and match
the C-section verification above. With R1-F01/R1-F05 folded into the
commission, RC-2A is ready to be commissioned once RC1-A owner acceptance and
an explicit implementation assignment exist.

### RC-2B readiness (design obligations only)

The later facade design preserves: single-use release authority via the
existing capability/arbiter (D04/D05); C1 ambiguous-intent semantics including
first-retainer order and tail-stop outside locks; C2 stop-capability
consumption at first runtime acceptance; approved C3 Policy A with original
sealed deadlines and no renewed wait (the current `Reconciler` allowance
correctly not advertised as C3); C4 duplicate/concurrent semantics without a
ConcurrentRefusal kind; capture and cleanup independence; evidence-only
recovery with no release on reconnect and no record-based adoption (D05
process table; matches `M1RecoveryScope`'s actual retained-authority model);
and restricted-profile refusal without lifecycle fallback (D08). The new
deployment obligations (independently surviving unprivileged recovery process,
authenticated controller lifeline, stop-only reconnect, session secret
handling) are correctly marked as new work requiring their own RC-2B security
review and native tests — the design does not claim the current in-process
scope is already that service. RC-2B must not be implemented as part of RC-2A;
nothing in the value contract is unsafe without it, because no public value
can reach live authority.

## I. Handoff question dispositions (J.1–J.10)

1. **D4a field sufficiency without RC-2B:** Yes — verified against the Hufu
   A02 acceptance table (E); passive reference construction plus
   Gagamba-owned vectors are sufficient for synthetic D4a matching, with the
   synthetic/actual distinction the design already mandates.
2. **Projection/grammar/reader canonical identity:** Verified — the D03
   grammar parses the existing golden exactly (C.2); null/empty/tag/order and
   UTF-8 ordering semantics match the model; the new input ceiling is a new
   transport bound, correctly not claimed for the internal encoder; encode
   side needs R1-F01's clarification.
3. **One canonical encoder/validator; private JSON separation:** Verified —
   `ManifestCodec` sole production encoder; `OwnerManifestWire` is internal,
   JSON-only, validates through the same constructors and binds digests; no
   second manifest model or public encoder exists in the proposal.
4. **IDs/digests/factories/correlation/reconnect reaching native release:**
   No — source-verified (C.3/C.4/D); RC-2B's operation-object binding is the
   only route, with local-object-association checks in addition to identity.
5. **C1–C4/C3 Policy A preserved:** Yes — D06 matches D6.7 verbatim in
   substance; arbiter source implements the C1/C2/C4 rules; the Reconciler
   disclaimer is source-accurate; queued-vs-accepted stop is explicitly
   distinguished (R1-F02 defers its precise 2B specification).
6. **Lifecycle/Windows/macOS accidentally satisfying restricted selection:** No
   — D08's separate, default-disabled interface with refusal before setup,
   plus RC-T11 negative surface tests; pending production qualifications keep
   `restricted-offline-closure-v1` refused.
7. **Package/TFM/source/native identity sufficiency:** Yes — unique local
   version, recorded hashes, isolated clean-consumer restore, mixed Hufu graph
   validation with explicit stop; Execution's nuspec has no runtime
   dependencies (SourceLink is build-only), which the design correctly tells
   reviewers not to assume but which the source confirms.
8. **RC-2A implementable/testable without native facade or result API:** Yes —
   V/I/package subsets are self-contained; deferred native portions are
   explicitly listed and excluded from RC-2A exit.
9. **Later process/handle/lifeline/reconnect/capture contract vs current
   owner architecture:** Consistent — the design treats the surviving recovery
   process, lifeline authentication and stop-only reconnect as new
   deployment/protocol work (B2), not a rename of `M1RecoveryScope`; no claim
   that a standalone recovery executable exists today.
10. **Meaningful negatives/positive controls; uncertainty without OS
    enforcement claims:** Yes — per-case positive controls, mutation
    inventories, and the plan's own warning against counting subsets as whole
    integration; pure tests claim codec/validation facts only.

## J. Acceptance decision

**RC-1 ACCEPTED WITH QUALIFICATIONS.**

Stated separately:

- **Contract design acceptance:** RC-1 ACCEPTED WITH QUALIFICATIONS
  (R1-F01 to be resolved in the RC-2A commission text; R1-F04 recorded as a
  consumer-facing limitation; R1-F02/R1-F03 deferred to RC-2B).
- **RC-2A implementation readiness:** READY — subject to (a) RC1-A owner
  acceptance (which must explicitly include the Mode=`work` mapping, the
  public surface, the 65,536-byte codec ceiling including the R1-F01
  encode-side refusal rule, and the facade qualifications), (b) a separate
  implementation assignment, and (c) RC2A-01 revalidation that the parent and
  schema source are unchanged.
- **RC-2B implementation readiness:** NOT READY — requires its own commission,
  accepted C1–C4 implementations as applicable, real owner/readiness work and
  the deployment/security review its own text demands.
- **Hufu AE4-D4a resumption readiness:** NOT READY YET — AE4-B01 remains OPEN.
  D4a may be requested only after RC-2A is implemented, independently
  accepted, and consumable at an exact pinned version, and after Sol's
  dependency-graph and semantic-delta review. The value/codec design basis is
  now sufficient.
- **Runtime/security qualification status:** NOT QUALIFIED — unchanged. No
  native workload was run, no package published, no production profile
  enabled; C1 independent review, C3/C4 implementation and G3–G5 remain
  outstanding gates.

## K. Evidence appendix

1. Inspected RC-1 documents: design, implementation plan, review handoff,
   evidence README, validation.json, document-sha256.json,
   input-and-vector-check.json, local-refs.txt, worktrees.txt,
   remote-heads.txt, commission.md.
2. Inspected Gagamba source at `d609c29`:
   `src/Gagamba.Execution/Restricted/RestrictedManifest.cs` (model +
   `ManifestCodec`), `src/Gagamba.Execution/Restricted/RestrictedTransaction.cs`,
   `src/Gagamba.Execution.Linux.M1/Preparation/PreparedTransaction.cs`,
   `Preparation/PreparationStore.cs`, `Execution/ReleaseArbiter.cs`,
   `Execution/ReleaseDecision.cs`, `Lifecycle/OwnerPreparationTransaction.cs`,
   `Lifecycle/OwnerManifestWire.cs`, `Lifecycle/M1RecoveryScope.cs` (partial),
   `Recovery/Reconciler.cs` (partial), `src/Gagamba.Execution/Provider.cs`,
   `src/Gagamba.Runtime/ExecutionRuntime.cs`, `src/Gagamba.Runtime/Gagamba.Runtime.csproj`,
   `src/Gagamba.Execution/Gagamba.Execution.csproj`,
   `src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj`,
   `Directory.Build.props`, `.gitattributes`, and
   `tests/Fixtures/Gagamba.LinuxM1.Native/fixture.c`.
3. Inspected tests: `tests/Gagamba.Execution.Linux.M1.Tests/Contracts/ManifestTests.cs`
   (full), `Contracts/ReferenceEncoder.cs` (partial), plus negative-case
   inventory across `PreparationIdentityTests.cs`, `StateModelTests.cs`,
   `Execution/ReleaseArbiterTests.cs`, `Execution/Wp05D68C2StopCapabilityTests.cs`,
   `Execution/Wp05D69C1AmbiguousIntentTests.cs`,
   `Execution/Wp05ReleaseContractProbes.cs`,
   `Preparation/PublicationTests.cs`, `Preparation/ClosureOwnershipTests.cs`.
4. Receipt verification: every completion-receipt claim checked here was
   reproduced (commit/tree/parent, unchanged trees, document hash count,
   worktree cleanliness, Hufu identity; disposition string matches this
   review's input).
5. Inspected Hufu documents at `37dcf333`: `docs/ae4-gagamba-restricted-execution-integration-design.md`,
   `docs/ae4-gagamba-restricted-execution-stage-register.md`,
   `docs/ae4-implementation-plan.md` (referenced),
   `docs/ae4-implementation-contract-amendments.md` (A01–A03),
   `docs/reviews/ae4-implementation-entry-check.md` (AE4-B01),
   `src/Penghou.Hufu.Sandbox/Penghou.Hufu.Sandbox.csproj`; earlier design at
   `236948509` per the handoff.
6. Inspected Gagamba approvals: `docs/decisions/gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md`
   and the four C-contract decision documents at `01063c1` (worktree
   `Gagamba-d6-7`).
7. Commands: `git status --short --branch`, `git rev-parse`, `git log`,
   `git ls-tree`, `git worktree list`, `git branch -a --contains`, file reads
   and pattern searches over the exact worktrees, plus the Python
   re-computations described in B. No file outside this review document and
   its branch was created or modified; no build, test, pack, restore, push,
   merge, tag or publication was performed.

**End of independent review.**
