# Gagamba RC2A-01 Independent Review Report

Reviewer role: independent implementation review (the next gate required by the RC2A-01 handoff). Review-only; no production code, tests, specifications, or evidence were modified.

---

## A. Git binding

| Item | Expected | Verified actual | Result |
|---|---|---|---|
| Worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-01` | present, `.git` file (linked worktree) | OK |
| Branch | `implementation/gagamba-rc2a-01` | same | OK |
| Final evidence commit | `68b01fbb53eefca9c8ca2f83ff50641521020a2a` | HEAD | OK |
| Final tree | `54f659d01937da29bc9a5f82566a8349af264664` | `HEAD^{tree}` | OK |
| Implementation commit | `781e9debb8df852d1d4d8ca98d92591e1609e193` | direct parent of evidence commit | OK |
| Working tree | clean | `git status --porcelain` empty at review start and end | OK |

Ancestry (verified with `git log --graph`): `d609c29` (RC-1 design) → `9a70533` (RC1-A owner acceptance) → `e93d642` (implementation) → `5a5ef7b` (evidence) → `781e9de` (clarification fix) → `68b01fb` (final evidence). The independent GLM design review `feb9d75` is preserved unmerged on sibling branch `review/gagamba-rc1-glm-independent-contract`, exactly as the owner instruction required ("The decision did not merge the review"). All 8 source blobs recorded in `clarification-source-binding.json` match `git rev-parse 781e9de:<path>` exactly; root/src/tests/native trees match.

## B. Scope inspected

**Specifications**: RC-1 design (`docs/design/restricted-consumer-contract-rc1.md`, RC-D01–D04/D06/D09, schema-1 grammar, §3 AE-4 profile, §4 invariants); RC1-A owner decision (`docs/evidence/restricted-consumer-rc1-owner-acceptance/owner-instruction.md`); GLM review commit `feb9d75` (R1-F01–F07); frozen API finalization `docs/design/restricted-consumer-contract-rc2a-01-api.md` (incl. owner-clarification paragraph).

**Production**: `RestrictedManifestView.cs` (265 l), `RestrictedManifestCodec.cs` (62 l), `RestrictedPreparationReference.cs` (45 l), `ManifestReader.cs` (221 l), `ManifestValidation.cs` (40 l), `RestrictedManifest.cs` (908 l, delta only). Full diffs `9a70533..e93d642`, `e93d642..781e9de`.

**Tests**: `RestrictedContractTests.cs` (37 cases), `RestrictedPublicProjectionTests.cs` (9 cases), `ManifestTests.cs`, `ReferenceEncoder.cs` (both verified unchanged from baseline).

**Evidence**: all 11 files under `docs/evidence/restricted-consumer-contract-rc2a-01/`, raw TRX/log artifacts under `artifacts/rc2a-01/`, `docs/handoff.md`, `docs/work-queue.md`, `docs/plans/restricted-consumer-contract-implementation.md`, root `README.md`.

## C. Contract compliance matrix

| # | Requirement (source) | Implementation location | Evidence | Assessment |
|---|---|---|---|---|
| C1 | Twelve sealed get-only manifest views mirroring every RC-D02 property (72 fields) | `RestrictedManifestView.cs:10-265` | field-map.md (72 rows verified against source); `EveryModelPropertySurvivesProjectionAndDecode` | **SUPPORTED** |
| C2 | Public enums `BudgetValueKind`/`QualificationDisposition`, byte-typed, exact numbers | `RestrictedManifestView.cs:6-7` | `EnumNumbersArePinned` (M1) | **SUPPORTED** |
| C3 | No public view constructors/builders/setters/factories; no second encodable model | internal ctors `RestrictedManifestView.cs:14,41,74,...` | `PublicSurfaceIsSealedReadOnlyAndPassive` (reflection sweep) | **SUPPORTED** |
| C4 | View equality = object identity; only reference has content equality | views have no `Equals` override; reference `IEquatable` at `RestrictedPreparationReference.cs:31-44` | `PassiveReferenceCopiesAndComparesContents` | **SUPPORTED** |
| C5 | Exactly the RC-D03 codec constants + 5 methods; closed 12-code refusal vocabulary in listed order | `RestrictedManifestCodec.cs:7-61` | api-inventory.md; public surface test | **SUPPORTED** |
| C6 | `Supports`: exact ordinal schema+ID; unsupported/empty/whitespace/case-variant → false; null → `ArgumentNullException` even at unsupported schema | `RestrictedManifestCodec.cs:30-35` | `SupportsIsExact` (8 cases); `NullArgumentsAreProgrammingErrors` (schemas 0/1/65535); my probe (trailing NUL, leading space, embedded space, max schema) | **SUPPORTED** |
| C7 | `Encode` validates then delegates to sole authoritative encoder; complete length >65,536 → `ArgumentException` (`manifest`), inclusive 65,536 succeeds, no partial buffer; oversized values can never become public views | `RestrictedManifestCodec.cs:45-50`; `RestrictedManifest.cs:699-711, 892-907` (CanonicalStream pre-growth checks); projection guard `RestrictedManifestView.cs:18` | `CompleteUtf8ByteCeilingIsInclusiveAndSymmetric` 65,536/65,537 (both directions: encode throw + projection throw + decode refusal); legacy unbounded encoder preserved as positive control | **SUPPORTED** |
| C8 | Decode: strict bounded reader; oversize/empty refused at offset 0 before parsing; checked cursor/counts vs minimum item size before allocation; unknown schema/implementation refused as soon as read | `RestrictedManifestCodec.cs:37-42`; `ManifestReader.cs:16-30, 32-40, 172-179, 207-213` | `DecodeRefusesOversizedInputBeforeParsing` (65,537 and 1,000,000); `UnknownSchemasRefuse`; my probe: 65,536 zero bytes → `UnsupportedSchema@0` | **SUPPORTED** |
| C9 | Canonical strictness: wire order enforced, never sorted into acceptance; duplicates, unsorted, malformed UTF-8 (overlong/surrogate/out-of-range), undefined enum/bool/presence tags, trailing data, forced-value contradictions refused at named offsets; no normalization | `ManifestReader.cs:44-61, 103-129, 147-160, 181-202`; re-encode equality defense `ManifestReader.cs:24-25` | `UnsortedAndDuplicateSetsAreNotNormalized`; `TagsAndForcedValuesRejectAtTheirField` (10 exact-offset cases); `StrictUtf8DoesNotRepairMalformedSequences`; all-873-prefix sweep; my independent 873-offset single-byte mutation sweep: **every structural/canonical byte mutation refused; only value regions (raw digests, u64 values, free string bytes) accepted, each as canonical re-encoding** | **SUPPORTED** |
| C10 | SHA-256 covers complete canonical bytes; constant-time compare; wrong length → false (clarified); all-zero ordinary digest; null manifest throws | `RestrictedManifestCodec.cs:52-61` | golden digest test; `WrongDigestLengthReturnsFalse` (0/31/33); `NonmatchingAndAllZeroDigestsAreNotWildcards`; my probes (lengths 0/31/33/64/65,536, mutated, all-zero) | **SUPPORTED** |
| C11 | Five-field passive reference: validating ctor (empty GUIDs / non-32-byte digest → `ArgumentException` naming argument), copied digest, content equality/hash, no temporal/control/lookup members | `RestrictedPreparationReference.cs` (whole file; 5 properties only) | `PassiveReferenceCopiesAndComparesContents`; property-name assertion in public surface test; my probes (31/33/empty-GUID throws) | **SUPPORTED** |
| C12 | Mode `work` descriptive only; no argv insertion; no mode allowlist or dispatch; profile fields (network/inode Pending, required denials, fixed paths `/tool` `/input` `/tmp`, stdin EOF, core disabled, capabilities empty, argv+env ≤ 16,384 UTF-8 bytes, empty env values allowed, empty argv items not) enforced on every route | `ManifestReader.cs:42-131`; model constructors `RestrictedManifest.cs` (unchanged invariants) | golden `work` case; `ArgumentEnvironmentBoundUsesUtf8Bytes` 16,384/16,385 (incl. wire-modified 16,385 refusal); `AllRepresentableDistinctionsRemainDistinct` | **SUPPORTED** |
| C13 | No authority: no Async/Release/Prepare/Start/Lookup members, no new friend assemblies, no new dependencies, no native/provider/runtime surface | whole delta (6 src files); `InternalsVisibleTo` pre-existing at baseline (verified `git show 9a70533:...`) | public-surface method-name sweep; empty metadata diff; grep of new files for delegates/events/Task/interop: only `Tasks` budget property | **SUPPORTED** |
| C14 | R1-F04: no temporal fields; limitation stated in consumer docs | `RestrictedPreparationReference.cs:5-7` XML; root `README.md:31-36` | docs reviewed | **SUPPORTED** |
| C15 | Exported vectors, exhaustive per-field independent mutations, Linux/macOS runs, package/clean-consumer/mixed-graph qualification | — | explicitly deferred in API doc §"Focused assertion inventory"; RC2A-02 NOT_STARTED | **NOT ASSESSED** (deferred by design, disclosure preserved) |

## D. Findings

No confirmed contract violations were found. All adversarial probes that failed were probe-side artifacts (miscomputed offsets; expectations contradicting the spec, e.g. decreasing sequence ≠ duplicate), re-interpreted and confirmed as correct implementation behavior.

**R2A-F01 — Low (recommendation, non-blocking).** The decode-path re-encode equality check (`ManifestReader.cs:24-25`, refusal `InvalidValue@0`) has no direct triggering test. It is unreachable from malformed wire given pre-construction order/tag/length checks (which is why no test can easily trigger it), so it is defense in depth only. Contract is not violated; the untested branch should be noted in RC2A-02 coverage planning. Violated contract: none; unsupported claim: none.

**R2A-F02 — Low (evidence hygiene, non-blocking).** `clarification-document-sha256.json` binds 8 post-clarification documents but omits `docs/design/restricted-consumer-contract-rc2a-01-api.md`, whose bytes changed in `781e9de`. The doc remains bound by its Git blob in the commit and was reviewed directly here; the original `document-sha256.json` binds only the pre-clarification version. Recommend rebinding in the RC2A-02 evidence set.

**R2A-F03 — Informational.** Producer-recorded local DLL hashes (e.g. `Gagamba.Execution.dll` `766f2bfc…`) did not reproduce byte-identically on my rebuild (same 137,728-byte size, different hash). The evidence explicitly scopes these as "not package or deployment qualification"; the reproduction criterion — test outcomes — did reproduce. No claim is invalidated.

**R2A-F04 — Informational.** `VerifySha256(null manifest, wrong-length digest)` throws `ArgumentNullException` (null check precedes length check, `RestrictedManifestCodec.cs:58-59`). Consistent with the API doc ("Null API arguments are programming errors"); only the null+32-byte combination is tested. Deterministic and documented; suggest an explicit assertion in RC2A-02.

**Test-coverage observations (recommendations for RC2A-02, non-blocking):** no explicit wire test for an empty argv item or a zero-length argv list (behavior is implemented via `Text()` default and model invariants, but unasserted); the wrong-length-digest contract is exercised at 3 representative lengths (implementation is total via `!= 32`).

**Unverified concerns:** none remaining. Every suspected issue was either reproduced as correct behavior or identified as a probe artifact.

## E. Test and evidence assessment

**Personally reproduced during this review (Windows 11, same worktree at `68b01fb`):**
- Release build of `Gagamba.Execution` with `-warnaserror`: **0 warnings / 0 errors**.
- Full `Gagamba.Execution.Tests` run: **57 passed / 0 failed / 0 skipped** (37 new contract cases + 20 existing pure regressions).
- M1 deterministic filter (ManifestTests | RestrictedPublicProjectionTests | PreparationIdentityTests): **38 passed / 0 failed / 0 skipped**. Total 95 — matches the claims exactly.
- Golden vector: independently decoded the committed hex — **873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`** — matches every recorded location (tests, README, validation JSON, RC-1 doc).
- Independent adversarial probe binary (temp dir, references the built DLL): golden round-trip/digest/re-encode; all wrong-length `VerifySha256` inputs false (incl. 64 and 65,536); matching true; mutated/all-zero false; `Supports` adversarial IDs false and null throwing at max schema; all 873 prefixes refuse with in-range offsets; full 873-offset single-byte mutation sweep proving canonical rigidity of all structural bytes; 65,537/1,000,000-byte inputs refused `InputTooLarge@0` before parsing; result mutual exclusivity on refusals; reference validation boundaries.
- Blob binding: all 8 recorded Git blobs and 4 tree IDs re-derived from commit objects — exact match.
- Hash chain: all recorded TRX/log SHA-256 values in `clarification-test-results.json` and `test-results.json` match the raw artifacts on disk (`clarification-baseline.trx` `5eb88b8e…`, `clarification-public.trx` `9a75b353…`, `clarification-internal.trx` `df38860c…`, logs, build log, `public-committed.trx` `3ef1fec0…`, `internal-committed.trx` `060c5e6e…`).
- LF-normalized document digests spot-checked (3/3 exact).

**Historical evidence inspected (not reproduced):** the baseline run demonstrating 3 wrong-length failures (old `ArgumentException` behavior, proving the tests detect the pre-clarification contract — a genuine test-first signal); earlier failed/abandoned runs honestly retained and excluded from counts; the 65,536/65,537 and 16,384/16,385 boundary cases (reproduced inside the 38-test run).

**What remains unproven (disclosed in the evidence):** Linux/macOS portable runs; exhaustive per-field mutation coverage; exported consumer vectors; package/pack/clean-consumer/mixed Hufu graph qualification; RC2A-02 independent tests-first qualification; all native/runtime confinement. The clarification runs executed pre-commit at HEAD `5a5ef7b` (candidly disclosed, source bytes bound); my post-commit reproduction closes that gap.

## F. Deferred obligation reconciliation

| Obligation | Status |
|---|---|
| R1-F01 | **Supported by producer test evidence and reproduced**: mandatory inclusive encode ceiling, shared-guard + projection-guard, 65,536/65,537 both directions, no partial output. Required remediation satisfied; RC2A-02 qualification still pending. |
| R1-F05 | **Supported**: wrong-length digest (0/31/33), `Supports` boundaries, oversized-encode assertions all present, committed, and passing. |
| R1-F02 | **Preserved OPEN for RC-2B** — no pre-mint stop latch exists (correctly absent; no facade types shipped). |
| R1-F03 | **Preserved OPEN for RC-2B** — no correlation retention/consumption decision implemented. |
| R1-F04 | **Preserved**: reference has no temporal fields; limitation stated in XML docs and root README. |
| R1-F06 | **Disclosure preserved**: Hufu AE-4 evidence at `Solo\hufu-ae4-implementation`, branch `implementation/ae4-integration-plan`, `37dcf333…`, HUFU-R recheck noted; `clarification-validation.json` records that worktree unchanged/clean. |
| R1-F07 | **Preserved**: verified-accurate disclosures retained; no fresh C/native/package qualification claimed anywhere in the current evidence. |
| RC-1 | **Not reopened**: design doc and owner decision unchanged since `9a70533` (verified empty diff); GLM review preserved unmerged on its own branch. |
| RC2A-02 | **Separate, NOT_STARTED** (plans doc, handoff, validation JSON all consistent). |
| Package qualification | **Not implied**: no pack; csproj/metadata untouched; DLL hashes explicitly disclaimed as non-qualification. |
| Hufu resumption / AE4-B01 | **Pending / OPEN** — stated in clarification §5, handoff, and work queue; no resumption authorization inferred. |

## G. Final recommendation

**ACCEPT WITH FOLLOW-UP**

Reasons: the implementation at `781e9de` conforms to the accepted RC-1 owner decision, the RC-D01–D04 contract, and the clarified API finalization; the 65,536-byte symmetric ceiling, strict canonical decoder, SHA-256 contract, and passive five-field reference are enforced in code, not merely documented; the evidence chain (blob bindings, artifact hashes, golden digest, 95 test outcomes) is internally consistent and was independently reproduced, including adversarial probes found no counterexample. No finding blocks acceptance.

Blocking conditions / follow-ups (none reopen RC-1, none silently close obligations):
1. RC2A-02 tests-first independent qualification (separately commissioned) — carries R2A-F01, R2A-F04, and the argv/argv-list coverage observations, plus exported vectors and exhaustive field-mutation coverage.
2. Package/clean-consumer/mixed-graph gates remain open; no pack authorized by this review.
3. R1-F02/F03 remain OPEN for RC-2B; R1-F06 recheck at HUFU-R.
4. AE4-B01 remains OPEN; Hufu resumption remains unauthorized.

**Review side effects (reported per constraints):** the worktree's gitignored `bin/`/`obj/` were rebuilt by my verification builds; all test results and the probe project were written to `C:\Windows\TEMP\opencode\` outside the repository; no tracked file was modified, no commit created, and `git status --porcelain` was verified clean at the end (HEAD `68b01fb`, tree `54f659d`).

*Leave acceptance to the owner.*