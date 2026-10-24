# Restricted consumer contract implementation work orders

Date: 2026-10-24. Governing proposal:
[RC-1](../design/restricted-consumer-contract-rc1.md).
Consumer dependency: Hufu AE4-B01. **Planning only; neither RC-2A nor RC-2B is
authorized by this document.** No code or test was changed or executed for RC-1.

## 1. Progress and gates

Update this table with exact commit, evidence path, reviewer and disposition as
work happens. DONE means the named bounded deliverable, not native qualification.
Preserve failed attempts and historical entries. A blocked dependency is not a
test failure or permission to replace a real provider with a fake.

| ID | Deliverable / owner | Status at RC-1 | Completion evidence / dependency |
|---|---|---|---|
| RC1-01 | Verify current Gagamba/Hufu inventory; Astra | DONE | Source manifest, remote/worktree inventories; selected 629a08d / 37dcf33 |
| RC1-02 | D01–D09 architecture and consumer delta; Astra | DONE, PROPOSED | RC-1 contract; no independent acceptance |
| RC1-03 | Test matrix, staged work orders and evidence; Astra | DONE, DOCUMENTATION ONLY | This plan and review handoff |
| RC1-R | Independent contract review; GLM | PENDING | Exact RC-1 commit/tree; Accept / Accept with qualifications / Amend / Reject |
| RC1-A | Owner accepts contract and dispositions | PENDING | Explicit record, including Mode, public surface, codec ceiling and facade qualifications |
| RC2A-01 | Revalidate accepted parent/scope and pin API inventory | NOT_STARTED | RC1-R + RC1-A + separate implementation assignment |
| RC2A-02 | Tests-first pure contract negatives/vectors | NOT_STARTED | RC2A-01 |
| RC2A-03 | Shared validation/reader, public views and passive reference | NOT_STARTED | RC2A-02 baseline evidence |
| RC2A-04 | Focused regressions, local package, clean consumer | NOT_STARTED | RC2A-03; exact local artifact/source identity |
| RC2A-R | Independent implementation/package review | PENDING | RC2A-04; no native qualification inferred |
| HUFU-R | Sol reassesses AE4-B01 and requests D4a authorization | BLOCKED | Accepted RC2A source/package, Hufu design/delta decisions and compatible dependency graph |
| C1-R | Independent C1 implementation review | PENDING AT INVENTORY | Separate Gagamba workstream; producer evidence is not acceptance |
| C4-I | Approved C4 implementation and review | PENDING | Separate commission; C1/C2 prerequisites |
| C3-I | Approved C3 Policy A implementation and review | PENDING | Separate commission; approved clock/deadline/recovery rules |
| M1-G3 | Real WP06–WP09 resources/capture/owner/recovery composition | PENDING | Accepted relevant component work; G3 permits only WP10 internal fixture |
| M1-G4G5 | WP10 transaction, WP11 faults/G4, WP12/G5 | PENDING | Exact bounded internal scope, not consumer or production approval |
| RC2B-01 | Accepted facade/deployment commission | NOT_STARTED | Relevant accepted C1–C4, actual owner/runtime readiness, explicit scope/gates |
| RC2B-02 | Real control, process/lifeline/capture/recovery implementation | NOT_STARTED | RC2B-01; target release inhibited until applicable gate |
| RC2B-03 | Qualified consumer artifact and native fault evidence | NOT_STARTED | RC2B-02; G3/G4/G5 and separately authorized exact consumer fixture |
| RC2B-R | Independent facade/consumer acceptance | PENDING | Exact deployment/host/source/native binaries and required native cases |
| HUFU-D4B | Real Hufu restricted adapter | BLOCKED | Accepted RC2B plus Hufu-specific authorization and prerequisites |

C2 reviewed WSL2 component evidence is already preserved, not a new item to
reimplement. H-01 runner DLL selection, H-02 fixture-build hang/root cause and
N-01 evidence-index issue remain separately tracked in existing records. Native
distribution breadth and production offline/network/inode/runtime/core/package
gaps remain open. There is no dependency from RC-2A onto native G3/G4/G5.

## 2. RC-2A — smallest public value/codec tranche

### Entry contract

Require the exact accepted RC-1 commit, GLM report and resolved mandatory
findings, owner acceptance and an explicit implementation assignment. Recheck
source/worktrees/remotes; if C1/C3/C4 or schema source changed, bind a new baseline
and review the delta. Work in an isolated branch. Preserve existing evidence.

The commission must name Execution's passive API, schema 1, 65,536-byte public
codec ceiling, inspection-only construction, passive preparation reference and
the accepted test subset. It must not silently commission facade interface code.

### File scope

| Path | Allowed change in RC-2A |
|---|---|
| `src/Gagamba.Execution/Restricted/RestrictedManifest.cs` | Factor/reuse canonical validation and the existing encoder without changing valid schema-1 bytes or release semantics; keep internal types internal |
| `src/Gagamba.Execution/Restricted/ManifestReader.cs` (new) | Checked bounded strict reader sharing canonical validation; no native references |
| `src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestView.cs` (new) | Complete sealed get-only root/nested projections, immutable byte/list snapshots |
| `src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestCodec.cs` (new) | Public supports/decode/encode/digest entry, typed refusal and stable limits |
| `src/Gagamba.Execution/Restricted/Contracts/RestrictedPreparationReference.cs` (new) | Validated passive GUID/digest correlation with content equality |
| `tests/Gagamba.Execution.Tests/Restricted/` (new) | Tests using only public API, golden vector data, malformed inputs, API surface/immutability checks |
| `tests/Gagamba.Execution.Linux.M1.Tests/Contracts/ManifestTests.cs`, `ReferenceEncoder.cs` | Preserve independent oracle; add deterministic field/order/strict-validation coverage where internal construction is required |
| Existing M1 preparation identity/closure tests | Focused deterministic assertions only if needed to verify view mapping does not alter authority; no native case broadening |
| `docs/evidence/restricted-consumer-contract-rc2a/` (future) | Exact commands, baseline failures, source/test/API/package hashes, coverage, exclusions and review handoff |
| Public contract/package docs and current queue/handoff | Actual status and consumer instructions |

Existing project files may include useful test data or local package verification
only if named in the RC-2A commission. No new production project/package, TFM,
friend assembly for a consumer, Hufu dependency, runtime registration, Linux M1
source edit or package publication. Pack candidate version via command properties;
do not republish or overwrite preview.4. Stop if necessary work exceeds scope.

### Sequence and checks

1. Inventory the accepted public API and unchanged binary schema. Read the actual
   test conventions/project files. Record source/test trees and SDK. Select
   portable test commands excluding native fixtures before running anything.
2. Write tests first for exposed values, decoder malformed inputs and negative
   misuse. Record whether each baseline fails compilation (missing public API)
   or behavior (reader/validation absent); do not call all failures runtime bugs.
   Preserve existing golden bytes and independently encoded expected values.
3. Implement passive projections and reference, one shared validator and strict
   reader; the wrapper delegates encoding to existing `ManifestCodec`. Enforce
   length/count bounds before allocation. Do not return partially parsed values.
4. Run focused public tests and exact existing deterministic ManifestTests /
   preparation tests relevant to the change. Add per-field mutation cases and
   malformed byte mutation coverage. Check public surface for control handles,
   setters/factories, tests or native dependencies. A single pure DTO mapper test
   is not proof of a native preparation binding.
5. Build the affected net10.0 library/test projects in Release with warnings as
   errors; build the solution only if accepted repo checks require it. Do not run
   a whole test assembly that implicitly starts native workloads. Record exact
   filter, discovered cases, passed/failed/skipped counts and every required skip.
6. Commit the implementation source/test slice; locally pack that exact commit
   into a unique version `0.1.0-rc2a.<full implementation commit>` with repository
   commit metadata. Preserve package bytes/hash, nuspec and assembly references.
   Retain a bounded artifact inventory file listing exact package/DLL hashes and
   source identity; record SHA-256 of its exact bytes as the artifact-set reference.
   Evidence may be a subsequent docs commit pointing back to the exact code commit.
7. Run a temporary clean consumer outside the source tree using only exact local
   `Gagamba.Execution` NuGet reference and an isolated cache/config. Restore solely
   from the prepared local dependency feed. Fail if external feed access, source
   project references, friend identity, reflection or sibling fallback is needed.
   Decode/encode/verify all published vectors and refuse malformed input.
8. Validate the planned Hufu graph against current Runtime preview dependencies
   using a separate temporary consumer: no Hufu edits. Compare direct Execution
   override versus a separately commissioned coherent candidate set. Treat NuGet
   downgrade/assembly incompatibility as an unresolved gate, not a warning to mute.
9. Record actual results and all deferred tests; stop for independent review.
   Do not start Hufu or RC-2B merely because the tests are green.

Illustrative command families for the **future** implementer (not run in RC-1):
`dotnet test <public-tests> -c Release --filter <exact-portable-filter>`;
`dotnet test <M1-tests> -c Release --filter <exact-deterministic-contract-filter>`;
`dotnet pack src/Gagamba.Execution/Gagamba.Execution.csproj -c Release
-p:PackageVersion=<unique-version> -p:RepositoryCommit=<exact-source>`;
`dotnet restore <clean-consumer> --configfile <isolated-config> --packages <empty-cache>`;
`dotnet run --project <clean-consumer> --no-restore`. Record actual shell-safe
commands and versions; placeholders are not evidence of execution.

### Exit criteria and stop conditions

All required RC-2A subcases in section 4 pass with positive controls; required
cases are not skipped; no valid golden byte/digest changes; public values cannot
modify their source/internal model; one canonical encoder; bounded strict reader;
exact local package works from a clean consumer without internal/native access.
Scope has no native activation or C1–C4 behavior change. Independent reviewer
accepts exact source/API/package and qualifications. The Hufu graph/delta result
is explicit even if it requires an additional bounded follow-up.

Stop on schema/identity drift, need for unsupported metadata/defaults, mutable
aliasing, reliance on internals in the consumer, codec duplication, hidden launch,
unexpected package dependency or unresolved compatibility. Do not repair a
Gagamba runtime defect opportunistically. Preserve the failing case and return
to its owner workstream.

## 3. RC-2B — later restricted control facade

### Entry and sequencing

Requires a separate explicit assignment accepting D04–D09 control semantics,
exact public interface/deployment design and the actual current owner architecture.
The assignment must identify which prerequisite gates allow implementation and
which permit target release. Accepted relevant C1–C4 implementations, real
resources/readbacks/capture/owner/recovery and production clock support cannot be
replaced by WP05 fake prerequisites. RC-2A package acceptance is necessary but
does not satisfy any native gate.

G3 permits WP10's internal fixed fixture only. WP10, WP11/G4 and WP12/G5 remain
required before the separately authorized consumer milestone. A Gagamba consumer
commission must name exact experimental profile, artifacts, deployment and host;
production offline registration remains refused until its own gaps are closed.

### Bounded work packages for its future commission

| ID | Scope and intended files | Acceptance / stop boundary |
|---|---|---|
| B1 | Public facade, provider-origin prepared/result values, bounded profile/source inputs; Execution contract + Runtime selection | Separate interface; no lifecycle fallback; installed artifact/profile checks; no authority from parsed values |
| B2 | Dedicated recovery bootstrap/deployment, private controller lifeline/authentication, owner/scope links; M1 lifecycle + a separately justified bootstrap project | Native process topology outlives controller; same-UID trust limits; protocol/secret review; refuse unavailable independence |
| B3 | Real owner preparation/release mapping; M1 OwnerPreparationTransaction/owner protocol/arbiter composition | Original identity/deadlines, actual establishment, no target initialization before release, C1–C4 retained unchanged; no fake readiness |
| B4 | Independent stop/observer/capture paths, fixed protected custody; capture/owner/facade | Stop works during setup and blocked observers; exact C2 acknowledgement; bounded per-stream/aggregate storage, no target-driven trusted status |
| B5 | Inspection-only lookup, retained-authority reconciliation, C3 clock/deadline mapping; recovery + journal | No release on reconnect, no record-only adoption, no new waiting allowance; separate cleanup and journal uncertainty |
| B6 | Local deployment/native artifact qualification; provider/owner/bootstrap packaging | Exact RID/protocol/binary closure; reject test coordinators and missing versions; no publication |
| B7 | Deterministic and native full-lifecycle fault evidence, independent review | Every mandatory case executed on declared host, cleanup proved or Unknown preserved; source-bound producer and independent evidence |

B2 is an architectural/deployment change, not a rename of the current in-process
`M1RecoveryScope`. Resolve actual executable/package layout and protocol before
coding; the RC-1 sketch is a constraint, not permission to improvise a daemon.
Any security-relevant change to accepted RC-1 semantics needs a delta review.

Stop and record an integration finding if controller loss cannot reliably reach
the surviving holder, scope/owner lifetimes contradict the design, native
artifacts differ from accepted pins, capture/control backpressure affects safety,
result mappings erase uncertainty, or C3/C4 behavior is unavailable. Do not
work around awkward integration in Hufu. Fix Gagamba under a separate authorized
slice and revalidate affected gates.

## 4. Acceptance test inventory (designed, not run)

Kinds: **V** public values only, portable; **I** Gagamba internal deterministic
test with no native target; **N** later native/real-owner qualification. A case
can have separate subcases in more than one class. All cases below are PENDING;
RC-1 review accepts their design, not their outcome. Do not count an I model as N.

| ID | Stimulus, positive control and required result | Class and tranche |
|---|---|---|
| RC-T01 | Decode the preserved literal golden and each new documented vector; assert every scalar/nested/list value, encode exact original bytes, verify expected SHA-256. Retain independently authored ReferenceEncoder agreement | V + I, 2A |
| RC-T02 | Build equivalent internal accepted manifests with permutations of every set, fresh backing arrays and supplementary-plane strings; project/decode through public codec; identical bytes/digest. Reordered argv is a negative counterexample | I + V output, 2A; no public builder required |
| RC-T03 | Change each serialized field independently while remaining structurally valid; assert different bytes and test digest, with an explicit per-field inventory. For fixed/forced fields (version/implementation/path/denial/pending tags), assert rejection instead. Nonmanifest IDs/metadata do not change manifest digest but must fail their separate association checks | I + V, 2A; metadata/live association I/N in 2B |
| RC-T04 | Schema 0, 2 and 65535; unknown implementation; valid v1 positive control. Typed refusal, no partial view/migration or native work | V, 2A |
| RC-T05 | Truncate at every byte boundary; append bytes; corrupt bool/presence/enum tags; oversized/overflowing lengths/counts; invalid UTF-8; unsorted sets. Every malformed input refuses without large allocation or normalization; canonical counterpart succeeds | V, 2A |
| RC-T06 | Null core pattern versus present nonempty; empty env value versus empty key; absent/exact-zero/unlimited budgets; absent inode budget zero; duplicate role/path/key/set entry refusal; duplicate argv retained; UTF-8 byte order distinct from UTF-16; input enumeration order normalized only before encoding | V + I, 2A |
| RC-T07 | Mutate all input/output byte arrays and caller collections after decode/projection; values/digest unchanged; no setters/public capability constructors. Later attempt mutation/substitution after real preparation; owner still holds original copied objects and view | V + I in 2A; N in 2B |
| RC-T08 | Pair a changed manifest with original preparation reference: pure digest mismatch; internal store mint/bind negative and existing positive control. Later pass another operation's view, same tuple forged data or replaced source objects; release refuses with no target initialization | V + I in 2A; I + N in 2B |
| RC-T09 | Construct equal passive IDs/hash; API offers no release/lookup-to-capability. Existing store tests reject fabricated/foreign/expired/consumed association. Later restart/reconnect/deserialize evidence cannot release; lost release ACK causes inspection only, never resend | V + I in 2A; I + N in 2B |
| RC-T10 | Parse Mandatory/Pending correctly; no decoded object can claim establishment. Actual provider mismatch/pending mandatory production qualification refuses; a separate approved experimental fixture preserves pending flags instead of promoting them | V in 2A; I + N in 2B |
| RC-T11 | 2A public/package API inventory contains no launch/control adapter. Later an ordinary IExecutionProvider cannot satisfy restricted selection; unsupported Windows/macOS and Linux mechanism/profile failures refuse before initialization; positive qualified Linux fixture only under its gate | V in 2A (negative surface); I + N in 2B |
| RC-T12 | Clean exact-version NuGet consumer decodes/re-encodes/digests using public API only. Isolated cache/local feed; no source references, friend identity, reflection or fixture packages. Validate planned mixed Runtime dependency graph separately | V + packaging, 2A |
| RC-T13 | RC-2A schema/implementation mismatch refuses without needing native files. Later swap one native/managed artifact hash, role, RID, build/protocol version, dependency or test-only coordinator; matching accepted closure succeeds, mismatch refuses before preparation/target | V subset in 2A; I + N required for full case in 2B |
| RC-T14 | Preserve independent C1 ambiguity+NotAttempted delivery, duplicate without durable commit, concurrent Refused with winner, root/helper differences, nonzero target exit, capture truncation, absent stop time, populated/unknown domain and cleanup/finalization uncertainty. Tagged payloads reject contradictions; native receipts reproduce the mappings | I (existing vocabulary baseline may be inspected) + public result V + N, 2B; public result types are not in 2A |
| RC-T15 | Exact 65,536-byte accepted structurally valid input vs 65,537 refusal; exact 16,384 argv/env UTF-8 limit vs overflow; u32 count with tiny remaining buffer; checked cursor arithmetic. Bounded allocations and no stack recursion | V + I, 2A; justifies hostile decoder boundary |
| RC-T16 | Exhaustive field-to-public-property and field-to-wire assertion map; data-only construction cannot issue provider-origin prepared/established objects; no new execution field omitted from digest or matcher inventory | V + I, 2A; provider-origin native association in 2B |
| RC-T17 | Local package DLL/nuspec/API inventory and unique version match exact source; required dependency closure restores without external/source fallback; no M1 native load on Windows parsing. Wrong package/version/mixed assembly graph fails qualification | V + packaging, 2A; justifies real consumer gap |
| RC-T18 | Both release/stop orders, queued stop before mint, clock unavailable/overflow, failed StopRecord persistence, repeated stop, C1 first-retainer cuts. One physical attempt maximum; accepted stop never restores capability; stop ACK stages remain distinct | I + N, 2B; exact C1/C2/C4 suites remain prerequisites |
| RC-T19 | Original C3 deadline remaining/expired/incompatible epoch/frequency/boot/no valid StopRecord; Policy A identity-checked kill plus one immediate observation, no new wait; separate journal failure/uncertainty; repeated reconcile cannot extend | I + N, 2B and separate C3 workstream |
| RC-T20 | Controller dies while Held/running/host evidence call hung; scope survives and signals stop. Owner dies with scope retained; scope dies with owner alive; all holders lost; reconnect authentication and no-release checks. Record actual stop/emptiness/cleanup or Unknown, never disk-only adoption | I + N, 2B; justifies Hufu A01/A03 obligations |
| RC-T21 | Test/private coordinator substitution, helper closure mismatch, missing capture/readback and unsupported deployment all refuse. Golden codec fixture is rejected as executable enrollment. Production offline profile stays disabled despite experimental acceptance | I + N, 2B |
| RC-T22 | Slow/absent observers, full status/capture buffers, aggregate overflow, read failure, final-drain expiry and capture-store failure. Independent safety stop remains callable; no unbounded queue, raw handle/path leak or disclosure claim | I + N, 2B |
| RC-T23 | Lost prepare/release/stop/dispose acknowledgements, changed approval correlation, cancelled waits and concurrent repeated calls. A changed correlation after retention keeps Duplicate/original facts plus a binding-mismatch diagnostic. No replay, deadline reset, terminal rewrite or cleanup-on-dispose fiction | I + N, 2B |

RC-2A acceptance covers its designated V/I/package subcases only. RC-T07–T13
have later live/selection/native portions that remain pending. RC-T14 and
RC-T18–T23 are not RC-2A exit tests. Reviewer must inspect the case-to-assertion
map and reject any claim that the whole integration passed based on these subsets.

## 5. Evidence and independent review

For each implementation tranche retain: exact parent/branch/code/test trees,
source diff, tests-first baseline observations, actual commands/SDK/platform,
required case coverage, failed runs, artifact hashes, package contents, native
host/prerequisites when applicable, cleanup and residual uncertainty. Pure tests
record native cleanup as not applicable because no workload was started.

Producer validation and independent review are separate commits/artifacts with
exact source binding. New source changes invalidate affected earlier evidence.
GLM's current assignment is RC-1 review only; do not present this producer's
document checks as independent acceptance or commission an implementation through
the review prompt.

## 6. Hufu resumption decision

AE4-B01 remains **OPEN** after RC-1. After RC-2A independent acceptance, give Sol:

- Accepted RC-1 and RC-2A source/API/version and resolved review findings.
- Actual package bytes/hash, dependency graph, canonical vectors and strict
  validation/immutability evidence from the clean consumer.
- Accepted Mode/profile parameter mapping and every Hufu semantic delta from RC-1.
- Exact test subset and explicit absence of a live provider/native qualification.

Sol then reassesses the contract part of AE4-B01 and requests authorization for
AE4-D4a under Hufu's own design/owner gates. It is not an automatic resume. Hufu
uses actual ExecuteProcess/ReadFile semantics and its own separate binding codec.
No native provider calls belong in D4a. D4b still requires accepted RC-2B,
relevant C1–C4, real owner/capture/recovery behavior, exact consumer fixture gates
and separate Hufu authorization. Public NuGet publication is a separate decision.
