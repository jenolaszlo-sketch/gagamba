# RC2A-02 tests-first qualification specification

Date: 2026-10-25 (Asia/Manila). Current status: **RC2A-02A ACCEPTED WITH
QUALIFICATIONS; RC2A-02B ACCEPTED WITH QUALIFICATIONS; Q2A-S / RC-D04 OPEN,
ASSIGNED TO PLANNED RC2A-02C; FULL RC2A-02 NOT YET ACCEPTED** — see §12 for the
current owner disposition, §11 for RC2A-02B acceptance, §9 for the original
deferral and §10 for the portability execution record. Earlier execution
records remain historical. This document is planning only; it records no new
test execution and changes no production or test source.

Governing inputs, all verified in the canonical Gagamba repository:

| Input | Exact identity |
|---|---|
| RC-1 restricted consumer contract design | `d609c29d3048bc0fd91583068cb013fb906fdf90` |
| Independent GLM RC-1 design review | `feb9d7584c41712195d4b90754ed7a342ba74bb7` |
| RC1-A owner acceptance record | `9a705335701f0c1923bd9867a2612b8a93afa8cb` |
| Accepted RC2A-01 implementation ancestry tips | `e93d642e1a36af25c0e145d328bebadd1bba4838`, `5a5ef7bdde0708411cd875d0b0700556199134c1`, `781e9debb8df852d1d4d8ca98d92591e1609e193` |
| Accepted RC2A-01 evidence/contract tip (the acceptance object) | `68b01fbb53eefca9c8ca2f83ff50641521020a2a` |
| Accepted RC2A-01 root tree | `54f659d01937da29bc9a5f82566a8349af264664` |
| Source tree at the accepted commit | `b8c167e3d7340a562221591bdc2b6df7adb372d1` |
| Tests tree at the accepted commit | `ceaa7aca8121a1acef5300e074a1f6bc73c0a781` |
| Unchanged native tree | `85631d2f04512376621d3d09f51d67fd90f45335` |
| Frozen API contract document blob | `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` (`docs/design/restricted-consumer-contract-rc2a-01-api.md`) |
| Independent GLM RC2A-01 implementation review (preserved verbatim, unmerged) | commit `8e41babfc96afa2e617a53b63e50903d54001194`, tree `fd30ef218b2ff5d1422d2e3abddd59c50e11aead`, document blob `d329fcc49d289696bf1c4163f8f346c56115ba8c`, byte SHA-256 `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0` (16,888 bytes) at `docs/reviews/restricted-consumer-rc2a-01-glm-independent.md` on `review/gagamba-rc2a-01-glm-independent`; disposition **ACCEPT WITH FOLLOW-UP**; dispositions of R2A-F01–R2A-F04 are recorded in the [owner acceptance record](../decisions/restricted-consumer-rc2a-01-owner-acceptance.md) |

RC2A-01 is **owner-accepted with follow-up**; qualification of the implementation
is not complete. RC2A-02 is the separately commissioned tests-first stage that
qualifies it. Focused RC2A-01 producer tests (95 passing cases) are implementation
evidence only and are not counted toward RC2A-02 exit.

## 1. Purpose and boundaries

RC2A-02 qualifies the accepted passive value/codec implementation exactly as it
stands at the accepted commit: independent test authoring before execution,
canonical consumer vectors with reproducible generation and independent
verification, exhaustive per-field mutation coverage, boundary and misuse
assertions, and reproducible cross-platform run definitions with honest evidence
hygiene. It changes no production source. Test source changes are confined to new
test files and, where strictly necessary, additions to existing deterministic test
classes; the existing golden bytes, `ReferenceEncoder` oracle and all committed
assertions of RC2A-01 are preserved unchanged.

RC2A-02 is not: RC2A-03 component qualification sign-off, RC2A-04 packaging and
clean-consumer qualification, RC-2B work, native or production qualification,
Hufu resumption, or publication. Completion of RC2A-02 does not by itself qualify
any package.

## 2. Entry criteria

1. The [owner acceptance record](../decisions/restricted-consumer-rc2a-01-owner-acceptance.md)
   is committed with the accepted RC2A-01 commit `68b01fbb53eefca9c8ca2f83ff50641521020a2a`
   (tree `54f659d01937da29bc9a5f82566a8349af264664`) as a direct parent, and the
   independent review is preserved unchanged at its bound commit and blob.
2. A separate, explicit execution authorization for RC2A-02 exists from the owner.
   This specification is not that authorization.
3. The accepted API contract (`docs/design/restricted-consumer-contract-rc2a-01-api.md`)
   and the review findings' follow-up obligations are bound in the current SHA-256
   document inventory (see section 7, E1).
4. A dedicated qualification branch and worktree are created directly from the
   accepted commit; `git status` is clean at start; src/tests/native trees match
   the identities in the table above.
5. The R2A follow-up dispositions recorded by the owner acceptance are reflected
   in this specification; any follow-up that changes test obligations is folded
   into the case inventory before execution begins.

## 3. Test design

Case identifiers: `Q2A-V-*` (vectors), `Q2A-F-*` (field mutations), `Q2A-B-*`
(boundaries), `Q2A-S-*` (public surface/misuse), `Q2A-P-*` (portability runs).
All vector and refusal expectations are fixed in the specification and test
source before execution (tests-first); execution records actual outcomes against
them. Where an expectation in this document proves wrong during execution, the
test is not silently adjusted: the discrepancy is recorded as a finding, the
specification row is corrected by a documentation commit, and the correction is
visible in the traceability matrix.

### A. Independent consumer vectors (Q2A-V)

A1. **Vector set.** Export the canonical vector set as committed data:

- The preserved literal golden vector: 873 bytes, SHA-256
  `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`, bytes
  unchanged from RC2A-01.
- Newly authored vectors covering each representable distinction listed in
  RC2A-01 evidence section E ("Representable distinctions") and each valid
  mutation family of section B: at minimum empty and nonempty namespace lists,
  empty argv list, empty environment value, absent/exact-zero/unlimited budgets,
  inode-budget present and absent, `NoNewPrivs=false`, `RuntimeDumpsDisabled`
  both values, present/absent core pattern, supplementary-plane and non-ASCII
  UTF-8 strings, maximum 16,384-byte combined argv/environment payload, maximum
  65,536-byte complete encoding, minimal valid manifest.
- Each vector records: purpose, generator procedure, expected canonical bytes
  (committed hex), expected SHA-256, expected decode graph summary (every
  property value), and expected `Supports` result.

A2. **Reproducible generation.** Each vector is generated by a committed
deterministic procedure (script or documented byte-construction recipe) that
takes no environment input. Running the generator twice on one machine and on
different machines must produce identical bytes. The generation output is
compared byte-for-byte against the committed hex; any difference is a failure of
the vector, not a new vector.

A3. **Independent verification.** The existing test-only `ReferenceEncoder`
remains the independent encoding oracle and is never modified to call
`ManifestCodec`. An independent decode-side verifier (test-only, no production
code reuse beyond the public contract) decodes each vector and asserts the
expected graph. Encode/decode agreement is checked in both directions:
production decode of generated bytes, independent encode of the expected graph,
and byte equality. Vectors are exercised through the public API only
(`RestrictedManifestCodec`, views, passive reference).

A4. **Byte-for-byte reproducibility.** Committed vector bytes, digests and
independent-encoder outputs are preserved as evidence; reruns must reproduce
them exactly. Digest recomputation uses `SHA256` over complete canonical bytes
only.

A5. **Malformed and noncanonical rejection vectors.** A named catalog of
rejection vectors with expected `ManifestDecodeError` code and offset, covering
at least: every truncation prefix of the golden (873 cases, expected
`Truncated` at end-of-input offsets, offset range assertion), trailing data
(`TrailingData`), oversized input 65,537 and 1,000,000 bytes (`InputTooLarge`
at 0), empty input (`EmptyInput` at 0), inflated/reduced counts and lengths
(`InvalidLengthOrCount`), malformed UTF-8 including overlong, surrogate and
out-of-range sequences (`InvalidUtf8`), unknown tags (`InvalidTag`), unsorted
sets (`NonCanonicalOrder`), adjacent-duplicate entries in namespaces, writable
store paths, closure roles and environment keys (`Duplicate`), empty non-allowed
strings and empty environment keys (`InvalidValue`), forced-value contradictions
(ambient denials false, tool path not `/tool`, capabilities nonempty, stdin EOF
false, network/inode qualification `Mandatory`, inode-budget absent with nonzero
value, budget kind non-Exact with nonzero value) (`InvalidValue`), unknown
schema (0/2/65535, `UnsupportedSchema` at 0) and unknown/whitespace/case-variant
implementation IDs (`UnsupportedImplementation` at 2). Every catalog entry has a
canonical positive counterpart.

### B. Exhaustive field mutation coverage (Q2A-F)

F1. **Field inventory.** The checklist is the committed 72-field
[field-to-property map](../evidence/restricted-consumer-contract-rc2a-01/field-map.md)
(blobs at the accepted commit). Every one of the 72 fields appears in the
mutation matrix with at least one case; no field is covered only incidentally.

F2. **Mutation classes per field.** For each field, the matrix records which of
the following apply and fixes expected outcomes:

- **Valid semantic mutation**: change the value within schema-1 rules; expected
  result is different canonical bytes, different digest, decode success, and the
  changed value visible in the decoded view.
- **Boundary values**: minimum, maximum, and one-past/one-under where
  representable (`u16`/`u32`/`u64` scalar edges, `ushort.MaxValue` schema,
  `ulong.MaxValue` budget, string length edges).
- **Presence**: null/absent versus present (core pattern; inode budget; budget
  kinds Absent/Exact/Unlimited), with the documented distinctions asserted.
- **Ordering**: correct canonical order accepted; swapped pairs refused
  (`NonCanonicalOrder` at the second item's start offset) for namespaces,
  read-only destinations, writable stores, closure objects, helper dependencies
  and environment keys. Argv order is preserved, never sorted: permuted argv is
  a valid distinct encoding with a different digest.
- **Lengths**: oversized declared length/count versus remaining bytes
  (`InvalidLengthOrCount` at the field start); exact remaining accepted.
- **Encoding**: malformed UTF-8 refused (`InvalidUtf8`); strict re-encoding
  identity asserted.
- **Forced fields**: the forced set (schema 1, implementation ID, `/tool`,
  `/input`, `/tmp`, capabilities empty, the three ambient denials true, tool
  executable true, core disabled true, stdin EOF true, network and inode
  PendingQualification) must refuse contradiction with `InvalidValue` at the
  field's start offset.
- **Duplicate entries**: adjacent duplicates in each ordered set refused with
  `Duplicate` at the second item's start offset; duplicate argv explicitly
  retained as valid.

F3. **Schema and implementation identifiers.** `Supports` and decode both cover:
schema 1/implementation exact match (positive); schema 0, 2, 65535; empty,
whitespace, case-variant, trailing-NUL and unknown implementation IDs; null
implementation ID throws `ArgumentNullException` (programming-error convention,
including at unsupported schema values). Decode refuses before dependent
allocation; `Supports` never throws for non-null inputs.

F4. **Refusal code and offset assertions.** Every refusal case asserts the exact
`ManifestDecodeError` code and the exact zero-based offset where specified in
this document; where the offset is data-dependent it asserts the documented
offset rule (offending field start; end-of-input for truncation; reader position
after the relevant fields for cross-field model invariants; zero for
empty/oversized input) plus range membership. The catalog doubles as the
regression net for any future decoder change.

F5. **Valid versus malformed versus noncanonical.** Each matrix row labels its
class: `valid` (accepted, digest changes), `malformed` (structural refusal),
`noncanonical` (well-formed but refused: order/duplicate/forced), or
`misuse` (public API programming error). No malformed or noncanonical input may
be accepted; no valid mutation may be refused.

### C. Boundary testing (Q2A-B)

B1. **65,536/65,537 manifest boundaries.** A structurally valid manifest whose
complete canonical encoding is exactly 65,536 bytes: view construction,
`Encode`, `Decode`, full graph comparison and re-encode all succeed. The same
grammar at 65,537 bytes: bounded `Encode` and view construction throw
`ArgumentException` (parameter `manifest`), `Decode` returns `InputTooLarge` at
offset 0, and no partial buffer or view is produced. The legacy internal
encoder without the cap may be used as the deterministic oversized positive
control in deterministic internal tests only; no public oversized-view factory
exists. Pre-parse refusal is additionally proven with a 1,000,000-byte input
(`InputTooLarge`, offset 0) and an empty input (`EmptyInput`, offset 0).

B2. **16,384/16,385 argv/environment boundaries.** Combined strict-UTF-8 byte
count of argv and environment (keys and values) of exactly 16,384 bytes is
accepted and round-trips; 16,385 bytes refuses view construction and, when
crafted directly on the wire, refuses decode. The boundary is measured over
UTF-8 bytes including multibyte characters, not characters.

B3. **Empty argv item and zero-length argv list.** An argv list containing an
empty string item refuses (constructor `ArgumentException`, surfaced by decode
as `InvalidValue` at the item's length-prefix offset). A zero-length argv list
(count 0) is valid, round-trips, and encodes as a zero count. The empty-item
versus empty-list distinction is asserted explicitly. Both cases carry the
independent review's coverage observation (accepted under R2A-F04's
follow-through): the behavior is implemented but was unasserted at RC2A-01.

B4. **Null-manifest, wrong-length digest and their combination.** Null manifest
arguments to `Encode`, `ComputeSha256` and `VerifySha256` throw
`ArgumentNullException`. `VerifySha256` returns false for expected digest
lengths 0, 31, 33, 64 and 65,536; a matching 32-byte digest returns true; a
changed 32-byte digest and an all-zero 32-byte digest return false against the
nonzero golden; all-zero remains a present value, not an absence or wildcard.
`ComputeSha256` equals SHA-256 of the complete canonical encoding for every
vector. Per R2A-F04, an explicit combined assertion is required:
`VerifySha256(null manifest, wrong-length digest)` throws
`ArgumentNullException` — the null-check-before-length-check ordering is pinned
by test, not left implicit.

B5. **Defensive re-encode equality branch assessment.** The reader performs a
final exact re-encode comparison after bounded parsing
(`ManifestReader.Decode`, re-encode equality check immediately before returning
a view). R2A-F01 (ACCEPT, FOLLOW-UP) requires this assessment and permits the
branch to remain without a direct triggering test **if** it is demonstrably
unreachable under the accepted input model; no artificial test is required:

- Reachability analysis: through the public `Decode` boundary, every locally
  validated accepted input re-encodes identically because the reader enforces
  canonical set order with the same comparer the writer uses, strict UTF-8
  guarantees byte-exact string round trips, and model constructor invariants
  subsume the remaining canonicality conditions. The branch is therefore
  expected to be unreachable as an independently triggerable failure path while
  executing on every accepted decode. The assessment, its conclusion and its
  justification are committed as qualification evidence; only if the analysis
  demonstrates a reachable divergence case must a triggering test be authored.
- The branch is exercised implicitly as a pass-through assertion by every
  positive vector and every accepted mutation case; the qualification records
  this as its exercise mode. Deliberate fault injection would require
  production changes and is out of scope for RC2A-02.
- Any crafted input that reaches the branch with unequal re-encoding is a
  production defect: report it, preserve the bytes, and stop (section 6).

B6. **Truncation and single-byte mutation sweeps.** Every proper prefix of the
golden refuses without partial values; the sweep asserts refusal, error
presence and offset range for all 873 lengths. A single-byte flip sweep over
the golden asserts that no mutation outside the two raw 32-byte digest fields
is accepted; mutations inside digest fields are valid present values, and the
sweep's survivor set is compared against the committed digest-field offset
ranges as evidence.

### D. Cross-platform portability (Q2A-P)

P1. **Reproducible run definitions.** For each platform, the qualification
records exact commands, exact test filters, SDK/runtime versions, results
directory and artifact names, so any later runner can reproduce the run:

```powershell
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=<platform>-public.trx' --results-directory artifacts/rc2a-02/<platform> --blame-hang-timeout 60s
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Qualification' --logger 'trx;LogFileName=<platform>-internal.trx' --results-directory artifacts/rc2a-02/<platform> --blame-hang-timeout 60s
```

plus the platform's Release build of `src/Gagamba.Execution` with warnings as
errors. Native fixtures, restricted owners, helpers and any workload remain
unstarted; filters select pure tests only.

P2. **Platform set and honesty rules.** Windows remains the primary evidence
platform and is re-run on the qualification branch. Reproducible Linux and macOS
qualification runs are defined and executed natively when hosts are available.
WSL2 results are always labeled `wsl2` and never reported as native Linux
results. macOS results are labeled with the exact host and architecture. A
platform that is not run is recorded as "not run" with the reason; it is neither
a failure nor a pass, and no cross-platform success is implied until each
declared native run has actually executed.

P3. **Separate evidence.** Per-platform artifacts, TRX files, logs and
`SHA256SUMS` are preserved separately under `artifacts/rc2a-02/<platform>/` with
a per-platform summary in the evidence document. Platform-specific failures are
not averaged away: each platform's result stands alone.

P4. **Portability scope.** These are pure managed-code runs of the existing
net10.0 assemblies. They do not qualify native confinement, cgroups, sandboxing,
or any platform-specific provider, and they do not disturb the existing
Linux/macOS provider evidence. Package-level platform bundles remain RC2A-04
work.

### E. Evidence hygiene (Q2A-E)

E1. **Rebound document inventory (R2A-F02 follow-through).** At qualification
start, the current SHA-256 document inventory is refreshed so the clarified API
contract `docs/design/restricted-consumer-contract-rc2a-01-api.md` — omitted
from the post-clarification inventory per R2A-F02 — plus the preserved
independent review, the owner acceptance record, this specification and the
current plan/queue/handoff are all bound by LF-normalized SHA-256 of their exact
bytes, following the `restricted-consumer-contract-rc1` evidence convention.

E2. **Exact source identities.** Every run records the exact commit and root
tree it executed against (which must be the accepted commit and tree; test-only
changes on the qualification branch are committed tests-first and bound per
run), plus src/tests/native sub-tree identities.

E3. **Runner and result provenance.** Each run captures: OS name/version and
architecture, .NET SDK and runtime versions, machine identifier, exact command
lines with exit codes, TRX file, captured log, and SHA-256 of each artifact.
Independent-runner provenance (a rerun by a different operator or review agent
from a clean checkout) is distinguished from producer runs and is required for
exit (section 5).

E4. **Test reproducibility versus binary reproducibility.** Evidence states
which claim is being made: deterministic test outcomes (counts, vectors,
refusals) are reproducible across runs and platforms as specified; local DLL
byte hashes are environment-bound observations and are not binary
reproducibility, package identity, or deployment qualification. This
distinction is already owner-accepted in R2A-F03 (ACCEPT AS DISCLOSED): the
reviewer's rebuild of the producer's DLLs matched test outcomes but not byte
hashes, and no binary-reproducibility or deployment-qualification claim is
accepted in either direction. Package-level artifact-set hashing remains
RC2A-04.

E5. **Failed attempts and limitations.** Failed, hung or abandoned runs are
preserved with their logs and are never recounted as passes. Every known
limitation (host availability, WSL2 versus native labeling, skipped case
justifications) is stated in the evidence document.

### F. Independent review exit (Q2A-R)

R1. Exit requires an independent reviewer (GLM or an equivalently independent
agent) who, from a clean checkout of the exact qualification commit: re-executes
the declared commands on at least the primary platform; recomputes vector bytes
and digests; verifies per-field matrix completeness against the 72-field
inventory; verifies refusal code/offset assertions against the decoder source;
and reviews the evidence document, traceability matrix and failed-attempt
register.

R2. The reviewer's disposition, exact binding and any findings are preserved as
a separate review artifact; follow-ups are dispositioned by the owner without
rewriting the review. RC2A-02 is not complete until that review accepts the
evidence.

## 4. Traceability matrix

Requirement sources map to case families; the executed matrix commits one row
per case with its actual outcome and artifact. Status below is the pre-execution
state.

| Requirement source | Obligation | Case family | Status |
|---|---|---|---|
| RC-D02 (field table) | Every manifest field exposed and mutation-mapped | Q2A-F1/F2 over 72 fields | PENDING |
| RC-D03 (canonical grammar) | Exact encode/decode identity; closed refusal vocabulary | Q2A-V3, Q2A-B1, Q2A-F4 | PENDING |
| RC-D03/R1-F01 | Symmetric inclusive 65,536-byte ceiling; no partial output | Q2A-B1 | PENDING |
| RC-D03/R1-F05 | Digest length rules, Supports boundaries, oversized encode refusal | Q2A-B4, Q2A-F3, Q2A-B1 | PENDING |
| RC-D04 (passive reference) | Five fields, validation, content equality, no temporal/control members | Q2A-F2 (reference rows), Q2A-S | PENDING |
| RC1-A §5 | Encode-side refusal evidence at both byte boundaries | Q2A-B1 | PENDING |
| RC1-A §6 / R1-F05 | 31/33-byte digests, all-zero rule, Supports positive/unknown/null | Q2A-B4, Q2A-F3 | PENDING |
| R1-F04 | No temporal fields; documentation preserved | Q2A-S negative surface check | PENDING |
| R2A-F01 (re-encode branch, ACCEPT FOLLOW-UP) | Carry reachability assessment; no artificial test if unreachable | Q2A-B5 assessment | PENDING |
| R2A-F02 (API doc digest, ACCEPT FOLLOW-UP) | Rebind clarified API document in evidence inventory | Q2A-E1 | PENDING |
| R2A-F03 (DLL byte identity, ACCEPT AS DISCLOSED) | Keep test-versus-binary reproducibility distinct; no binary claim | Q2A-E4 rule | PENDING (standing rule) |
| R2A-F04 (combined null+wrong-length; argv observations, ACCEPT FOLLOW-UP) | Explicit combined assertion; empty argv-item and zero-length argv-list cases | Q2A-B4, Q2A-B3 | PENDING |
| RC-T01..T06, T15, T16 (2A subset) | Work-order 2A test obligations | Q2A-V, Q2A-F, Q2A-B | PENDING |
| RC-T12/T17 (packaging subset) | Not in RC2A-02; remain RC2A-04 | — | OUT OF SCOPE |
| RC-T07..T11, T13 (native portions), T14, T18..T23 | Not in RC2A-02; remain RC-2B/native gates | — | OUT OF SCOPE |

## 5. Exit criteria

All of the following are required; partial completion is recorded as such:

1. Every Q2A case in the executed traceability matrix is executed on the
   primary platform against the exact accepted source (plus committed tests-first
   test commits), passes, and none is skipped without a recorded justification
   that this specification permits.
2. The vector set is byte-for-byte reproducible (generation rerun identical),
   independently verified against `ReferenceEncoder`/independent decoder outputs,
   and its committed digests match.
3. The mutation matrix covers all 72 fields with the required classes, and every
   refusal case asserts the exact specified code and offset.
4. Both byte boundaries (65,536/65,537; 16,384/16,385), the empty-argv-item and
   zero-length-argv-list cases, the null-manifest and wrong-length digest cases,
   the truncation sweep and the single-byte mutation sweep are executed with
   committed evidence.
5. The defensive re-encode branch assessment (B5) is recorded with its
   reachability conclusion and any attempts made to trigger it.
6. Windows run completed; each declared additional platform run is either
   completed natively with separate evidence or recorded as not run with the
   reason; no cross-platform claim exceeds what ran.
7. Evidence hygiene artifacts (E1–E5) are committed: rebound document
   inventory, per-run identities, provenance, and the failed-attempt register.
8. An independent reviewer has re-executed and accepted the evidence per
   section 3.F, with the disposition preserved.

Completion of RC2A-02 establishes qualification of the passive value/codec
implementation only. It does not complete RC2A-04 packaging/clean-consumer
qualification, does not qualify any native or production capability, does not
close AE4-B01, and does not authorize Hufu resumption or publication.

## 6. Exclusions, defects and stop conditions

Explicit exclusions, per the owner acceptance: RC-1 stays closed; the accepted
RC2A-01 public contract is not changed; RC-2B obligations R1-F02/R1-F03 stay
open and unimplemented; AE4-B01 stays open; Hufu stays dormant; no package is
built, packed or published; no native sandbox qualification is claimed; RC2A-02
completion is not package qualification.

Production defects discovered during qualification (for example, a refusal
missing, an offset misreported, a noncanonical input accepted, a digest
mismatch): stop, preserve the failing input and full output, report the defect
with a minimal reproduction, and propose a separate bounded remediation slice
with its own tests-first authorization. Qualification scope is not silently
expanded to fix defects, and an accepted contract behavior is never adjusted to
make a test pass.

Additional stop conditions: any need to touch production source; any need for a
public oversized-view factory, friend assembly or reflection; schema or
implementation identifier drift; golden byte or digest change; missing host
availability that would tempt a substituted fake environment; unresolved
execution authorization for any declared run.

## 7. Execution outline (for the future authorized runner)

1. Re-verify entry criteria; create the qualification branch/worktree directly
   from `68b01fbb53eefca9c8ca2f83ff50641521020a2a`; record clean state.
2. Refresh the document SHA-256 inventory (E1); author vectors and the full
   test source tests-first; commit tests with the baseline-red evidence where
   applicable (new cases must be shown to fail against a stub or to assert
   pre-existing behavior deliberately; no production change).
3. Execute P1 run definitions per platform in the order Windows, then declared
   additional platforms; capture E3 provenance per run; commit results.
4. Complete the executed traceability matrix and evidence document; record
   failed attempts and limitations (E5).
5. Stop; hand off for independent review (3.F) and owner disposition of any
   findings. No further work is authorized by passing alone.

## 8. RC2A-02A execution correction (2026-10-25)

The RC2A-02A Windows execution discovered that the literal Section 3 B6
characterization ("no mutation outside the two raw 32-byte digest fields is
accepted") does not match the accepted decoder. Observed: of 222,615 single-byte
mutations over the 873-byte golden (each byte position set to each of its 255
alternative values), 114,378 are accepted. Accepted mutations include raw digest
bytes, other scalar values, free-text content, free boolean/enum tag bytes, and
length-prefix bytes that re-segment the same byte string into a different, fully
canonical manifest (for example golden byte 157 set to 0x3B). Every accepted
mutation re-encodes byte-for-byte, so no noncanonical image was accepted.

Corrected row (effective for RC2A-02A and later executions):

> A single-byte flip sweep over the golden asserts **canonical rigidity**: for
> each mutation, either the decoder refuses with a code and an in-range exact
> offset, or it accepts the mutation and Encode(Decode(x)) equals x
> byte-for-byte. The survivor set is classified against the committed
> immutable-region map (schema, implementation ID, fixed path strings,
> forced-true booleans, pending-only qualification tags), which must contain no
> survivor; the complete survivor profile (positions, slots, accepted value
> counts) is preserved as evidence. The claim that only digest-field mutations
> are accepted is withdrawn.

This correction was found during execution and is recorded per the Section 3 rule
that a wrong expectation is not silently adjusted. It changes no production
behavior, does not relax the decoder contract, and affects no other row. Evidence:
`docs/evidence/restricted-consumer-contract-rc2a-02a-windows/findings.md`
(Q2A02A-F01), `exports/flip-sweep-survivors.json` and the preserved first failing
run under `runs/failed-attempts/`.

## 9. Execution status and owner acceptance (2026-10-25)

**RC2A-02A (Windows managed-code contract qualification): ACCEPTED WITH
QUALIFICATIONS.**

- Owner decision:
  [RC2A-02A owner acceptance](../decisions/restricted-consumer-contract-rc2a-02a-owner-acceptance.md).
- Qualified implementation: test source
  `71a28afe2623516fb4176e6a4ce7988a3d70df06` (tree
  `70b932148cbfa59b0e424229879eebd36df2d80c`); evidence
  `c1a01761f8d58136a381ad87269635c6c14ad093` (documentation-only). Production
  `src` and `native` subtrees remain the accepted RC2A-01 identities.
- Independent review `RC2A-02A-GLM-Q1` at
  `7b6344551c2d4b42832df39822eaf4b8a1c01495` (preserved on
  `review/gagamba-rc2a-02a-glm-q1`, unmerged): verdict **SUPPORTED WITH
  QUALIFICATIONS**, no confirmed production or qualification defect.
- P3 dispositions: F02 (native-filter wording) **ACCEPT, DOCUMENTATION
  FOLLOW-UP**; F03 (two large vectors digest-bound only) **ACCEPT AS
  DISCLOSED**; F04 (Q2A-S / RC-D04 deferred) **ACCEPT, DEFERRED
  QUALIFICATION**. Details and provenance are in the owner decision §6.
- Q2A02A-F01 is accepted as a **correct plan correction** (§8 above): the
  corrected invariant is canonical rigidity plus immutable-region containment;
  canonical representation is production-enforced and distinct from authority or
  sandbox enforcement.

**Q2A-S / RC-D04: OPEN.** The public surface/misuse family (Q2A-S) and the
RC-D04 passive-reference rows plus the R1-F04 negative temporal-surface check
were not executed in RC2A-02A; the executed traceability matrix in the RC2A-02A
evidence discloses the deferral. The plan's execution sections (A vectors, B
mutations, C boundaries, D portability, E evidence, F review) do not assign
Q2A-S to RC2A-02B, which is portability. Q2A-S / RC-D04 therefore remains an
**OPEN obligation of full RC2A-02 acceptance**, to be assigned by the owner to a
future qualification stage. It must be completed, or receive a separate explicit
owner disposition, before full RC2A-02 acceptance.

**RC2A-02B (portability qualification): NOT STARTED — REQUIRES SEPARATE OWNER
AUTHORIZATION.**

**Full RC2A-02: NOT YET ACCEPTED.**

### 9.1 RC2A-02B entry-condition checklist (preparation only; not an authorization)

1. A separate, explicit owner authorization for RC2A-02B exists. This checklist
   and this document are not that authorization.
2. A dedicated branch/worktree is created from the accepted RC2A-02A evidence
   tip `c1a01761f8d58136a381ad87269635c6c14ad093` (or a later accepted tip
   designated by the owner), with the start `src`/`tests`/`native` identities
   recorded and the worktree clean.
3. The Q2A-S / RC-D04 disposition is confirmed for this stage, either by an
   explicit owner assignment or by a separate deferred-stage decision; it is
   not silently absorbed.
4. Host availability is established per declared platform. Windows is the
   primary platform; Linux/macOS runs require real native hosts, and any WSL2
   run is labeled `wsl2` and never reported as native Linux (§3 P2).
5. The §3 P1 run definitions are executed with full provenance capture: exact
   command, exit code, TRX, log, artifact SHA-256, OS name/version,
   architecture, .NET SDK/runtime, test-framework versions.
6. Every declared platform produces separate evidence under
   `artifacts/rc2a-02/<platform>/` with a per-platform summary (§3 P3); a
   platform not run is recorded as "not run" with the reason and is neither a
   pass nor a failure.
7. §3 P4 scope boundaries are maintained: pure managed-code portability only;
   no native confinement, cgroup, sandbox, package or deployment claim.
8. The specific RC2A-02A-qualified assertions are re-executed on each
   declared platform, not merely inferred from the Windows run.
9. After execution, the §3 F1/F2 independent-review exit is performed for the
   RC2A-02B evidence, and its disposition is preserved without rewriting.

## 10. RC2A-02B portability execution record (2026-10-25)

Status: **RC2A-02B IMPLEMENTED / QUALIFIED LOCALLY (WINDOWS + WSL2) / NATIVE
LINUX AND macOS NOT RUN / READY FOR INDEPENDENT REVIEW.**

- Baseline: `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` (tree
  `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e`), the owner-acceptance tip and
  direct child of the accepted qualification evidence `c1a01761`; tested
  implementation `71a28afe2623516fb4176e6a4ce7988a3d70df06` unchanged.
- Executed (pure managed-code value/codec tests only):
  - **Windows 11 native** (AMD64; SDK 10.0.401, runtime 10.0.12): Execution
    Release build 0 warnings/0 errors; public suite 67/0/0; precise required set
    457/0/0 and an identical rerun; the literal committed-plan filter 473/0/41
    (the same three pre-existing Linux-native skip classes as RC2A-02A).
  - **WSL2 Ubuntu 26.04.1**, always labeled `wsl2` and never native Linux (SDK
    10.0.113, runtime 10.0.13): Execution Release build 0 warnings/0 errors;
    public suite 67/0/0; precise required set 457/0/0 and an identical rerun.
    The Windows and WSL2 test-name/outcome multisets are identical (67 and 457).
  - **Native Linux: NOT RUN** (no native host accessible). **macOS: NOT RUN**
    (no host). Per §3 P2 these are neither pass nor failure, and no
    cross-platform success is implied beyond the executed environments.
- Evidence:
  [RC2A-02B portability qualification](../evidence/restricted-consumer-contract-rc2a-02b-portability/README.md)
  (matrix, `results.json`, source binding, raw per-platform logs/TRX/exit files,
  `SHA256SUMS`, document digests and reviewer handoff).
- Execution-scope decision: the precise `Tests.Qualification.` filter is used
  for qualification on every platform; the literal `~Qualification` filter is
  executed on Windows only, because it selects pre-existing Linux-native
  classes (Q2A02A-F02) that §3 P1 ("filters select pure tests only") and §3 P4
  exclude. This follows the owner-accepted F02 disposition and does not change
  the accepted specification.
- **Q2A-S / RC-D04: NOT IN SCOPE; remains OPEN** (RC2A02A-GLM-F04) for full
  RC2A-02 acceptance.
- Independent review of RC2A-02B: **PENDING**. No RC2A-02B owner acceptance is
  recorded; full RC2A-02 remains **NOT YET ACCEPTED**; RC2A-03/04 and all other
  gates remain unchanged.

## 11. RC2A-02B owner acceptance (2026-10-25)

**RC2A-02B ACCEPTED WITH QUALIFICATIONS. Full RC2A-02 remains NOT YET
ACCEPTED.** This section supersedes the earlier “review pending / no owner
acceptance” status in §10; that entry is retained as the qualification-time
record. No qualification suite was rerun for this documentation-only decision.

The accepted bounded scope is Windows 11 native and WSL2 Ubuntu managed-code
value/codec qualification. On each platform, the public suite passed 67/67 and
the precise required suite passed 457/457 with no failures or skips; each
required-suite rerun had an identical test-name/outcome multiset. The Windows
and WSL2 public and required test-name/outcome multisets match. WSL2 is not
native Linux. The Windows broad-filter run is provenance evidence only: 473
passed, 0 failed, 41 skipped; the precise `Tests.Qualification.` filter remains
the qualification selection mechanism.

The decision is based on these exact histories:

| Role | Branch | Commit | Tree | Parent |
|---|---|---|---|---|
| RC2A-02A owner-accepted starting baseline | `decisions/gagamba-rc2a-02a-owner-acceptance` | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | `8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e` | `c1a01761f8d58136a381ad87269635c6c14ad093` |
| RC2A-02B qualification | `qualification/gagamba-rc2a-02b` | `dd16baec48663d40cd29504d98e57e30dd0855df` | `96e86aac2d284637805809b54da72e76bf46f1fc` | `5b4292d0fefc576664df61692dfad5cef6362879` |
| Q1 independent review, preserved unmerged | `review/gagamba-rc2a-02b-q1` | `5bc396c2e5619906ae202afe0a4244dfd6024d9e` | `8ebbccb7b3787ce8e57bc74bbc93a8c1edb9e650` | `dd16baec48663d40cd29504d98e57e30dd0855df` |
| R1 correction | `remediation/gagamba-rc2a-02b-r1` | `12d914d319b1df3eedafc5439bc547dacb578651` | `35cc347f8e436bd1fec9997cb50ede81e561767c` | `dd16baec48663d40cd29504d98e57e30dd0855df` |
| R1 final evidence tip | `remediation/gagamba-rc2a-02b-r1` | `1cd22b9b109ddba4673b11487bceea62d2e92117` | `2f2bae947dee368ae073dd3fe7877380ca8854ff` | `12d914d319b1df3eedafc5439bc547dacb578651` |
| Q2 independent verification, preserved unmerged | `review/gagamba-rc2a-02b-q2` | `60de1f2d34a7df337a426501cb3954fdd3216a6b` | `c556b0ad7bad69e5b7ca0815f9a1a17d36e51407` | `1cd22b9b109ddba4673b11487bceea62d2e92117` |

The qualification evidence `dd16baec` binds the executed source to
`e5fff9e8` (tree `8c04e9e2`); the unchanged source, test and native subtrees are
`b8c167e3d7340a562221591bdc2b6df7adb372d1`,
`5c3268286575cb55891fae9288627657f2d30ddf`, and
`85631d2f04512376621d3d09f51d67fd90f45335`, respectively. Q2 independently
verified 29/29 `results.json` artifact references, 43/43 `SHA256SUMS` entries,
and 9/9 applicable document digest bindings. The raw qualification evidence,
counts and outcomes were unchanged by R1; source provenance and evidence
integrity hold.

### Q1 dispositions

- **R1-01 — stale Windows artifact hashes: RESOLVED.** R1 corrected all 14
  identified digests from committed bytes and checked the full artifact set;
  Q2 independently verified all 29 references and all 43 manifest entries.
- **R1-02 — inaccurate broad-filter explanation: RESOLVED.** README and related
  evidence documentation now reflect the raw Windows TRX: five additional
  classes beyond the precise qualification set, two managed-code classes
  passing 16 tests total and three Linux-native classes skipped (41 total);
  aggregate 473/0/41. The precise filter remains authoritative for
  qualification.
- **Authorization-provenance note: PRESERVED AS DISCLOSED.** Q1’s audit-trail
  note remains visible in its report and the R1/Q2 disposition; this decision
  does not rewrite either review or treat the note as a test result.

Q1 (`5bc396c2`) and Q2 (`60de1f2d`) remain separate, unchanged review histories;
neither is merged or copied into this owner-decision branch. Q2’s verdict is
**VERIFIED WITH QUALIFICATIONS** and recommends this bounded owner acceptance.

### Qualifications, exclusions and remaining gates

- Native Linux: **NOT RUN**. macOS: **NOT RUN**. The plan permits recording
  unavailable native hosts as not run; it does not require provisioning them
  for this bounded acceptance. WSL2 is not native Linux.
- Native confinement and sandbox behavior, package and clean-consumer
  compatibility, mixed dependency graphs, binary reproducibility, and Hufu
  integration are **NOT ESTABLISHED** by this stage.
- **Q2A-S / RC-D04 remains OPEN.** Per §9 and the prior owner decision, this
  public-surface/misuse and passive-reference obligation must be completed or
  receive a separate explicit owner disposition before full RC2A-02 acceptance.
  Its stage is not silently assigned to RC2A-02B.
- RC2A-03 and RC2A-04 retain their separate pending qualification gates. The
  RC2A-04 package/clean-consumer work, RC-2B obligations R1-F02/F03, R1-F06
  HUFU-R recheck, AE4-B01, Hufu resumption, native sandbox qualification and
  publication retain their existing status and authorization boundaries.

This decision accepts **RC2A-02B only**. It does not accept full RC2A-02,
authorize RC2A-03 or later milestones, close any listed obligation, or expand
the supported claim beyond Windows and WSL2 managed-code value/codec scope.

## 12. Q2A-S / RC-D04 owner disposition (2026-10-25)

Status: **OPEN / NOT QUALIFIED; OWNER ASSIGNED TO PLANNED RC2A-02C; EXECUTION
NOT STARTED.**

The owner’s exact scope analysis, rationale, evidence assessment and acceptance
criteria are recorded in the [Q2A-S / RC-D04 disposition](../decisions/gagamba-q2as-rcd04-owner-disposition.md).
The source requirement is RC-1 §RC-D04 at design commit
`d609c29d3048bc0fd91583068cb013fb906fdf90`; the residual RC2A-02 rows are
Q2A-F2 reference coverage, Q2A-S public-surface/misuse and R1-F04’s negative
temporal-surface/documentation check. The assigned slice covers the public
five-field passive reference only; RC-2B’s live provider/artifact/deadline and
release authority remains outside it.

The RC2A-02A traceability matrix deferred these rows outside that authorization.
Existing focused tests/probes and the Windows/WSL2 public-suite passes are
supporting evidence, not a retroactive Q2A-S disposition. RC2A-02C is a planned
focused qualification and review step; no tests or implementation are changed
or claimed by this owner-disposition commit.

RC2A-02C must complete and receive focused independent review before full
RC2A-02 acceptance and before RC2A-03 qualification/acceptance relies on the
passive reference. This assignment does not mark Q2A-S complete, accept full
RC2A-02, or start RC2A-03.
