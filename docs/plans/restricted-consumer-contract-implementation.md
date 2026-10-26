# Restricted consumer contract implementation work orders

Date: 2026-10-24; owner-acceptance update 2026-10-25; RC2A-B acceptance update
2026-10-25; RC2A-02A acceptance update 2026-10-25; RC2A-02B owner acceptance
update 2026-10-25; RC2A-02C (Q2A-S / RC-D04) owner acceptance update
2026-10-26; RC2A-02 full-stage owner acceptance update 2026-10-26
(Asia/Manila). Governing design: [RC-1](../design/restricted-consumer-contract-rc1.md) at
`d609c29d3048bc0fd91583068cb013fb906fdf90`, independently accepted with
qualifications by GLM at `feb9d7584c41712195d4b90754ed7a342ba74bb7`, followed by
[RC1-A owner acceptance](../decisions/restricted-consumer-rc1-owner-acceptance.md).
Consumer dependency: Hufu AE4-B01. **Planning only; RC-2B, package qualification
and Hufu resumption are not authorized by this document, RC1-A or RC2A-B.**

## Current Q2A-S / RC-D04 status — ACCEPTED WITH QUALIFICATIONS via RC2A-02C (2026-10-26); RC2A-02 full stage ACCEPTED WITH QUALIFICATIONS

**Full-stage owner decision recorded 2026-10-26: [RC2A-02 is ACCEPTED WITH QUALIFICATIONS](../decisions/restricted-consumer-contract-rc2a-02-full-stage-owner-acceptance.md)** on branch `decisions/gagamba-rc2a-02-full-stage-acceptance`, based on the three accepted substage dispositions and the independent full-stage assessment **RC2A-02-FSA-01** (`assessment/gagamba-rc2a-02-full-stage` at `058a150130c5f112c7e9b8189bba8842cf0cec52`, verdict READY FOR OWNER ACCEPTANCE). All eight qualification-plan §5 exit criteria are dispositioned; the E2 (F03) and E6 (native Linux/macOS NOT RUN) qualifications are carried forward with the R2A-F03 standing rule, the 02B audit-trail note, the F01 erratum, and the passive-reference boundary. A consolidated cross-substage run was not performed and is not a mandatory RC2A-02 exit criterion. Acceptance establishes qualification of the passive value/codec implementation only; RC2A-03/04, RC-2B (R1-F02/F03 OPEN), AE4-B01, native sandbox, packaging/publication, Hufu, and end-to-end execution remain open or unauthorized, and no subsequent stage is authorized by this decision.

The RC2A-02C substage record below preserves the substage-level acceptance
history.

RC2A-02C ran on `qualification/gagamba-rc2a-02c-q2as-rcd04`: the pre-execution
matrix is commit `66a66bf56508b23b5730827a012ba730b702b72b`; final tested
commit `c4837b0b434fc75ebc3e08d278bdc09c3965a181` has `src` tree
`b8c167e3d7340a562221591bdc2b6df7adb372d1`, tests-only `tests` tree
`9c906b349b0bf0cffdee331fb5700f9b7828c199`, and unchanged `native` tree
`85631d2f04512376621d3d09f51d67fd90f45335`; the evidence tip is
`62b75f5866d0c339816712baeb04baa386b06f85` (tree
`66f996e1cf973e48487a073b07f1e78d9139c911`). The focused Windows Release
build passed with 0 warnings/errors and the three mapped tests passed 3/3,
0 failed, 0 skipped. The [evidence package](../evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/README.md)
contains the raw results, complete public-surface inventory, source binding,
documentation review, integrity manifest and reviewer handoff.

The independent review on `review/gagamba-rc2a-02c-q2` at
`9534581c34d55c1219e90ddcd290900583098388` with re-verification
`f8b692e04941462ea592d9653960191cafb937cc` issued **SUPPORTED WITH
QUALIFICATIONS**: 13/13 matrix rows independently supported, the build and
focused run reproduced twice, and 40/40 package digests, 16/16 document
bindings, 14/14 relative links and the source bindings independently
confirmed. The owner decision
[restricted-consumer-contract-rc2a-02c-owner-acceptance.md](../decisions/restricted-consumer-contract-rc2a-02c-owner-acceptance.md)
records **Q2A-S / RC-D04 ACCEPTED WITH QUALIFICATIONS** on
`decisions/gagamba-rc2a-02c-owner-acceptance`. Its single nonblocking P3
finding, Q2C-Q2-F01 (`validation.md` understated the package checksum count
as 39/39; the committed manifest has 40 verified entries), is corrected by
erratum in the decision without modifying the immutable evidence package.

The acceptance covers only the passive five-field reference contract and
R1-F04 documentation boundary on Windows. Full RC2A-02 remains **NOT
ACCEPTED**; this disposition does not start RC2A-03 or qualify RC-2B,
provider, deadline, release, native, packaging, reproducibility, Hufu or
end-to-end behavior. No next scope is authorized by this decision.

## RC2A-02B status — owner accepted with qualifications (2026-10-25)

**RC2A-02B ACCEPTED WITH QUALIFICATIONS** for Windows 11 native and WSL2
Ubuntu managed-code value/codec scope. The [owner decision](../decisions/restricted-consumer-contract-rc2a-02b-owner-acceptance.md)
is based on qualification `dd16baec48663d40cd29504d98e57e30dd0855df`, Q1
`5bc396c2e5619906ae202afe0a4244dfd6024d9e`, R1 final
`1cd22b9b109ddba4673b11487bceea62d2e92117`, and Q2
`60de1f2d34a7df337a426501cb3954fdd3216a6b`. Each platform retains public
67/0/0 and precise required 457/0/0 results; required reruns are identical and
Windows/WSL2 test-name/outcome multisets match. Q2 verified the two Q1 findings
resolved and the evidence bindings. Native Linux/macOS remain not run; Q2A-S /
RC-D04 remains open; full RC2A-02 remains unaccepted. The older status entries
below preserve the state at their recorded checkpoints and are superseded by
this decision.

## RC2A-02C owner assignment checkpoint — 2026-10-25

The owner’s [Q2A-S / RC-D04 disposition](../decisions/gagamba-q2as-rcd04-owner-disposition.md)
assigned the deferred passive-reference/public-surface qualification to a
focused **RC2A-02C** evidence stage. At that assignment checkpoint, it was
**OPEN / NOT QUALIFIED; execution had not started**. Its bounded scope was the
RC-D04 five-field public passive value, Q2A-S surface/misuse checks and R1-F04
temporal/documentation boundary; RC-2B live provider, artifact, deadline and
release checks remain separate. The execution result is recorded in the
current status above. Independent review and separate owner disposition remain
required before the obligation can close or full RC2A-02 can be accepted.

RC2A-B update: RC2A-01 is **owner-accepted with follow-up** at
`68b01fbb53eefca9c8ca2f83ff50641521020a2a` (tree
`54f659d01937da29bc9a5f82566a8349af264664`) after the independent GLM
implementation review (preserved verbatim at
`8e41babfc96afa2e617a53b63e50903d54001194`, **ACCEPT WITH FOLLOW-UP**; review
personally reproduced the 95 passing tests, build and golden digest with no
confirmed contract violation and no blocking finding). R2A-F01/F02/F04 are
carried into the [RC2A-02 qualification
specification](restricted-consumer-contract-rc2a-02-qualification.md);
R2A-F03 is accepted as disclosed. RC2A-02 remains **PLANNED, NOT STARTED** and
requires separate execution authorization. [Owner decision](../decisions/restricted-consumer-rc2a-01-owner-acceptance.md).

RC2A-02A update: the Windows managed-code consumer-contract qualification is
**ACCEPTED WITH QUALIFICATIONS** at tested source
`71a28afe2623516fb4176e6a4ce7988a3d70df06` (tree
`70b932148cbfa59b0e424229879eebd36df2d80c`) with documentation-only evidence at
`c1a01761f8d58136a381ad87269635c6c14ad093` (tree
`8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19`), based on the independent GLM review
`RC2A-02A-GLM-Q1` preserved on `review/gagamba-rc2a-02a-glm-q1` at
`7b6344551c2d4b42832df39822eaf4b8a1c01495` (verdict **SUPPORTED WITH
QUALIFICATIONS**; no confirmed production or qualification defect; P3 items
F02/F03/F04 dispositioned in the [owner decision](../decisions/restricted-consumer-contract-rc2a-02a-owner-acceptance.md)).
**RC2A-02B portability is NOT STARTED and requires separate owner
authorization; full RC2A-02 is NOT YET ACCEPTED; Q2A-S / RC-D04 remains OPEN**
as an obligation of full RC2A-02 acceptance (stage assignment by the owner; not
silently assigned to RC2A-02B). Package/clean-consumer/mixed-graph gates
(RC2A-04), native qualification, RC-2B (R1-F02/F03 OPEN), AE4-B01 and Hufu
resumption remain unauthorized and unqualified.

RC2A-02B update: the portability stage is **IMPLEMENTED / QUALIFIED LOCALLY
(WINDOWS + WSL2) / NATIVE LINUX AND macOS NOT RUN / READY FOR INDEPENDENT
REVIEW** at baseline `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` (tree
`8c04e9e2f0379f7b9a3ef280c3e20e5b0ec44f2e`), executing the unchanged tested
implementation `71a28afe2623516fb4176e6a4ce7988a3d70df06`. Windows native and
WSL2 Ubuntu both reproduce the public suite 67/0/0 and the precise required set
457/0/0 with identical test-name/outcome multisets; the Windows literal
committed-plan filter is 473/0/41 (pre-existing Linux-native skips). Native
Linux and macOS were not run (no hosts) and no cross-platform success beyond the
executed environments is claimed. Evidence:
[RC2A-02B portability qualification](../evidence/restricted-consumer-contract-rc2a-02b-portability/README.md).
Q2A-S / RC-D04 remains OPEN; independent review of RC2A-02B is PENDING; no
RC2A-02B owner acceptance is recorded and full RC2A-02 remains NOT YET ACCEPTED.
RC2A-03/04, native qualification, packaging/publication, RC-2B (R1-F02/F03
OPEN), R1-F06 HUFU-R recheck, AE4-B01 and Hufu resumption remain NOT AUTHORIZED
/ NOT QUALIFIED.

## 1. Progress and gates

Current RC2A-01 source after explicit user clarification is
`781e9debb8df852d1d4d8ca98d92591e1609e193`; see
[current evidence](../evidence/restricted-consumer-contract-rc2a-01/clarification-2026-10-25.md).
The frozen rules are oversized Encode => ArgumentException, wrong-length digest
=> false, and Supports(null) => ArgumentNullException with unsupported non-null
values => false. Fresh focused coverage passes 57 public and 38 deterministic
internal cases; build has 0 warnings/errors. Golden/encoder bytes and RC-1 are
preserved. These are RC2A-01 implementation results, not RC2A-02 qualification.

RC2A-01 update 2026-10-25: the subsequent user commission explicitly authorizes passive source implementation and focused tests in RC2A-01, superseding the earlier finalization-only/source sequencing below for that bounded work. Source `e93d642e1a36af25c0e145d328bebadd1bba4838`; [handoff](../evidence/restricted-consumer-contract-rc2a-01/README.md), [API finalization](../design/restricted-consumer-contract-rc2a-01-api.md). Independent implementation review is required next; RC2A-02 remains a separately commissioned qualification stage. No pack, native work or Hufu resumption is authorized. Historical work-order details remain the broader RC-2A qualification requirements.

Update this table with exact commit, evidence path, reviewer and disposition as
work happens. DONE means the named bounded deliverable, not native qualification.
Preserve failed attempts and historical entries. A blocked dependency is not a
test failure or permission to replace a real provider with a fake.

| ID | Deliverable / owner | Status as of RC1-A | Completion evidence / dependency |
|---|---|---|---|
| RC1-01 | Verify current Gagamba/Hufu inventory; Astra | DONE | Source manifest, remote/worktree inventories; selected 629a08d / 37dcf33 |
| RC1-02 | D01–D09 architecture and consumer delta; Astra | ACCEPTED WITH QUALIFICATIONS | RC-1 `d609c29`; GLM `feb9d75`; RC1-A owner record |
| RC1-03 | Test matrix, staged work orders and evidence; Astra | DONE, DOCUMENTATION ONLY | This plan and review handoff |
| RC1-R | Independent contract review; GLM | COMPLETE — ACCEPTED WITH QUALIFICATIONS | `feb9d7584c41712195d4b90754ed7a342ba74bb7`; original findings preserved separately |
| RC1-A | Owner accepts contract and dispositions | APPROVED AND RECORDED | [Owner decision](../decisions/restricted-consumer-rc1-owner-acceptance.md): Mode=`work`, passive public surface, encode/decode ceiling and all seven findings |
| RC2A-01 | Revalidate/finalize API; commissioned passive implementation and focused tests | **ACCEPTED WITH FOLLOW-UP (RC2A-B)** | HEAD `68b01fbb53eefca9c8ca2f83ff50641521020a2a`, tree `54f659d01937da29bc9a5f82566a8349af264664`; [owner decision](../decisions/restricted-consumer-rc2a-01-owner-acceptance.md); implementation only — qualification not complete |
| RC2A-R | Independent implementation/package review | **IMPLEMENTATION REVIEW COMPLETE — ACCEPT WITH FOLLOW-UP**; package review pending | GLM review preserved verbatim at `8e41babfc96afa2e617a53b63e50903d54001194` (blob `d329fcc49d289696bf1c4163f8f346c56115ba8c`, SHA-256 `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0`); R2A-F01–F04 dispositioned in the owner decision; package review remains RC2A-04 work |
| RC2A-02 | Separately commissioned tests-first qualification | **RC2A-02A AND 02B ACCEPTED WITH QUALIFICATIONS; RC2A-02C QUALIFIED LOCALLY / READY FOR INDEPENDENT REVIEW; Q2A-S / RC-D04 OPEN; FULL RC2A-02 NOT ACCEPTED** | [RC2A-02A owner decision](../decisions/restricted-consumer-contract-rc2a-02a-owner-acceptance.md); [RC2A-02B owner decision](../decisions/restricted-consumer-contract-rc2a-02b-owner-acceptance.md); [RC2A-02C evidence](../evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/README.md); [Q2A-S / RC-D04 owner disposition](../decisions/gagamba-q2as-rcd04-owner-disposition.md). The 02C local evidence awaits focused independent review and owner disposition. |
| RC2A-03 | Shared validation/reader, public views and passive reference | SOURCE IMPLEMENTED UNDER RC2A-01; stage not started; qualification pending; **Q2A-S / RC-D04 RC2A-02C evidence is a pre-acceptance gate** | `e93d642`; no separate qualification acceptance inferred; do not begin under this disposition |
| RC2A-04 | Focused regressions, local package, clean consumer | Focused RC2A-01 regressions pass; PACKAGE/CONSUMER NOT_STARTED | No pack authorized by current commission; remaining qualification gates pending |
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

RC1-A preserves GLM's original identifiers: R1-F01 is an accepted mandatory
encode-side correction; R1-F05 adds required tests; R1-F04 is the passive
reference's no-temporal-fields qualification; R1-F06 records Hufu's evidence
location; R1-F07 requires no corrective action. R1-F02/R1-F03 remain **OPEN,
DEFERRED TO RC-2B**. The decision's reconciliation table is authoritative for
owner dispositions; none of these statuses means implementation tests passed.

## 2. RC-2A — smallest public value/codec tranche

### Entry contract

Require RC-1 `d609c29d3048bc0fd91583068cb013fb906fdf90`, the separate GLM report
`feb9d7584c41712195d4b90754ed7a342ba74bb7`, the RC1-A decision and an explicit
implementation assignment. RC1-A accepts the design and mandatory clarification;
it does not commission code. Recheck
source/worktrees/remotes; if C1/C3/C4 or schema source changed, bind a new baseline
and review the delta. Work in an isolated branch. Preserve existing evidence.

The commission must name Execution's passive API, schema 1, **65,536-byte public
encoding and decoding ceiling**, inspection-only construction, passive preparation
reference and the accepted test subset. Mode=`work` is explicitly owner-approved
for the first consumer, with existing source semantics and no additional modes.
It must not silently commission facade interface code.

RC2A-01 must freeze the exact named refusal/result/exception behavior of public
Encode for oversized views (R1-F01), wrong-length VerifySha256 input and Supports
boundary arguments (R1-F05) before RC2A-02. Encoding whose complete canonical
bytes exceed 65,536 must refuse without a truncated/partial result or grammar
change. Preserve accepted in-bound bytes and existing validation/refusal rules.
Retain R1-F04 in consumer-facing package docs: passive references have no temporal
fields; D4a uses explicit enrolled/synthetic timing and D4b later uses actual
provider-origin timing. No expiry or liveness can be inferred from the reference.

The RC2A-01 exit record must bind the revalidated source/schema, public API,
concrete refusal behavior, required test-to-assertion map and deferred RC-2B/native
exclusions. If any mandatory detail is unresolved, RC2A-02 remains held. The
smallest next commission may authorize RC2A-01 then tests-first RC2A-02; this
planning update starts neither.

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
   Include R1-F05: expected SHA-256 lengths 0/31/33 refuse, matching/nonmatching
   32-byte controls, all-zero 32 bytes treated as present, Supports positive and
   boundary arguments, and exact-limit/oversized Encode cases (T04/T05/T15).
3. Implement passive projections and reference, one shared validator and strict
   reader; the wrapper delegates encoding to existing `ManifestCodec`. Enforce
   length/count bounds before allocation. Do not return partially parsed values
   or a partial/truncated encoding. Enforce the complete encode-side size guard
   as well as decode-side input size (R1-F01), without a competing encoder.
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
Encoding and decoding at 65,536 bytes obey the accepted canonical round trip;
65,537 bytes refuse without partial output. The named R1-F05 digest and Supports
cases pass under the API behavior frozen in RC2A-01. R1-F04 remains documented;
no temporal or control authority is added to the passive reference.
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

RC1-A leaves R1-F02 and R1-F03 **OPEN, DEFERRED TO RC-2B**. B1/B3 must specify
the observable pre-mint queued-stop latch and prevent a pending stop from yielding
a releasable capability (T18). B1 must explicitly decide pre-retention correlation
mismatch retention/consumption and subsequent matching-call admissibility (T23).
This plan records GLM's questions without silently choosing its recommendation
or implementing either behavior in RC-2A. Existing C1–C4 decisions remain settled.

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
| RC-T04 | Schema 0, 2 and 65535; unknown implementation; valid v1 positive control. Typed decode refusal, no partial view/migration or native work. R1-F05: Supports exact v1/implementation positive control, unsupported schema and unknown/empty/whitespace/case-variant implementation IDs; null follows the accepted programming-error convention. Freeze exact assertions in RC2A-01 | V, 2A |
| RC-T05 | Truncate at every byte boundary; append bytes; corrupt bool/presence/enum tags; oversized/overflowing lengths/counts; invalid UTF-8; unsorted sets. Every malformed input refuses without large allocation or normalization; canonical counterpart succeeds. R1-F05: VerifySha256 rejects expected lengths 0/31/33; matching 32-byte digest verifies, nonmatching 32-byte digest does not; all-zero 32 bytes is present, not absence/wildcard. No RC-2B correlation API added | V, 2A |
| RC-T06 | Null core pattern versus present nonempty; empty env value versus empty key; absent/exact-zero/unlimited budgets; absent inode budget zero; duplicate role/path/key/set entry refusal; duplicate argv retained; UTF-8 byte order distinct from UTF-16; input enumeration order normalized only before encoding | V + I, 2A |
| RC-T07 | Mutate all input/output byte arrays and caller collections after decode/projection; values/digest unchanged; no setters/public capability constructors. Later attempt mutation/substitution after real preparation; owner still holds original copied objects and view | V + I in 2A; N in 2B |
| RC-T08 | Pair a changed manifest with original preparation reference: pure digest mismatch; internal store mint/bind negative and existing positive control. Later pass another operation's view, same tuple forged data or replaced source objects; release refuses with no target initialization | V + I in 2A; I + N in 2B |
| RC-T09 | Construct equal passive IDs/hash; API offers no release/lookup-to-capability. Existing store tests reject fabricated/foreign/expired/consumed association. Later restart/reconnect/deserialize evidence cannot release; lost release ACK causes inspection only, never resend | V + I in 2A; I + N in 2B |
| RC-T10 | Parse Mandatory/Pending correctly; no decoded object can claim establishment. Actual provider mismatch/pending mandatory production qualification refuses; a separate approved experimental fixture preserves pending flags instead of promoting them | V in 2A; I + N in 2B |
| RC-T11 | 2A public/package API inventory contains no launch/control adapter. Later an ordinary IExecutionProvider cannot satisfy restricted selection; unsupported Windows/macOS and Linux mechanism/profile failures refuse before initialization; positive qualified Linux fixture only under its gate | V in 2A (negative surface); I + N in 2B |
| RC-T12 | Clean exact-version NuGet consumer decodes/re-encodes/digests using public API only. Isolated cache/local feed; no source references, friend identity, reflection or fixture packages. Validate planned mixed Runtime dependency graph separately | V + packaging, 2A |
| RC-T13 | RC-2A schema/implementation mismatch refuses without needing native files. Later swap one native/managed artifact hash, role, RID, build/protocol version, dependency or test-only coordinator; matching accepted closure succeeds, mismatch refuses before preparation/target | V subset in 2A; I + N required for full case in 2B |
| RC-T14 | Preserve independent C1 ambiguity+NotAttempted delivery, duplicate without durable commit, concurrent Refused with winner, root/helper differences, nonzero target exit, capture truncation, absent stop time, populated/unknown domain and cleanup/finalization uncertainty. Tagged payloads reject contradictions; native receipts reproduce the mappings | I (existing vocabulary baseline may be inspected) + public result V + N, 2B; public result types are not in 2A |
| RC-T15 | R1-F01/R1-F05: structurally valid exact-65,536-byte encode/decode positive round trip; 65,537 bytes refuse for decode and complete encode, with named failure and no truncated/partial result or grammar change. Use an internal deterministic projection seam to test the encode guard, not a public oversized-view factory. Also exact 16,384 argv/env UTF-8 limit vs overflow, u32 count with tiny remaining buffer, checked cursor arithmetic, bounded allocations and no stack recursion | V + I, 2A; hostile codec boundaries |
| RC-T16 | Exhaustive field-to-public-property and field-to-wire assertion map; data-only construction cannot issue provider-origin prepared/established objects; no new execution field omitted from digest or matcher inventory | V + I, 2A; provider-origin native association in 2B |
| RC-T17 | Local package DLL/nuspec/API inventory and unique version match exact source; required dependency closure restores without external/source fallback; no M1 native load on Windows parsing. Wrong package/version/mixed assembly graph fails qualification | V + packaging, 2A; justifies real consumer gap |
| RC-T18 | Both release/stop orders, queued stop before mint, clock unavailable/overflow, failed StopRecord persistence, repeated stop, C1 first-retainer cuts. R1-F02 remains open: RC-2B must specify/test observable pre-mint stop state and no releasable mint after stop wins. One physical attempt maximum; accepted stop never restores capability; stop ACK stages remain distinct | I + N, 2B; exact C1/C2/C4 suites remain prerequisites |
| RC-T19 | Original C3 deadline remaining/expired/incompatible epoch/frequency/boot/no valid StopRecord; Policy A identity-checked kill plus one immediate observation, no new wait; separate journal failure/uncertainty; repeated reconcile cannot extend | I + N, 2B and separate C3 workstream |
| RC-T20 | Controller dies while Held/running/host evidence call hung; scope survives and signals stop. Owner dies with scope retained; scope dies with owner alive; all holders lost; reconnect authentication and no-release checks. Record actual stop/emptiness/cleanup or Unknown, never disk-only adoption | I + N, 2B; justifies Hufu A01/A03 obligations |
| RC-T21 | Test/private coordinator substitution, helper closure mismatch, missing capture/readback and unsupported deployment all refuse. Golden codec fixture is rejected as executable enrollment. Production offline profile stays disabled despite experimental acceptance | I + N, 2B |
| RC-T22 | Slow/absent observers, full status/capture buffers, aggregate overflow, read failure, final-drain expiry and capture-store failure. Independent safety stop remains callable; no unbounded queue, raw handle/path leak or disclosure claim | I + N, 2B |
| RC-T23 | Lost prepare/release/stop/dispose acknowledgements, changed approval correlation, cancelled waits and concurrent repeated calls. A changed correlation after retention keeps Duplicate/original facts plus a binding-mismatch diagnostic. R1-F03 remains open: RC-2B must decide/test whether pre-retention mismatch is retained/consuming and whether a later matching call can proceed. No replay, deadline reset, terminal rewrite or cleanup-on-dispose fiction | I + N, 2B |

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
GLM's RC-1 design review is complete at `feb9d7584c41712195d4b90754ed7a342ba74bb7`;
RC1-A records owner acceptance with qualifications. Neither is an independent
implementation review. The separately commissioned RC-2A work must receive its
own source/package-bound GLM review. Do not present documentation checks as code
acceptance or commission implementation through a review prompt.

## 6. Hufu resumption decision

AE4-B01 remains **OPEN** after RC-1 and RC1-A. After RC-2A independent acceptance, give Sol:

- Accepted RC-1 and RC-2A source/API/version and resolved review findings.
- Actual package bytes/hash, dependency graph, canonical vectors and strict
  validation/immutability evidence from the clean consumer.
- Accepted Mode/profile parameter mapping and every Hufu semantic delta from RC-1.
- Exact test subset and explicit absence of a live provider/native qualification.

Carry R1-F04's temporal-source limitation and R1-F06's evidence location:
`C:\Users\Laszlos\source\repos\Solo\hufu-ae4-implementation`, branch
`implementation/ae4-integration-plan`, at
`37dcf33362e8e745800a530a478fd406795c3e58` when reviewed. Recheck the exact
consumer revision at HUFU-R; absence of these documents on canonical Hufu main
does not invalidate the dedicated branch's evidence.

Sol then reassesses the contract part of AE4-B01 and requests authorization for
AE4-D4a under Hufu's own design/owner gates. It is not an automatic resume. Hufu
uses actual ExecuteProcess/ReadFile semantics and its own separate binding codec.
No native provider calls belong in D4a. D4b still requires accepted RC-2B,
relevant C1–C4, real owner/capture/recovery behavior, exact consumer fixture gates
and separate Hufu authorization. Public NuGet publication is a separate decision.
