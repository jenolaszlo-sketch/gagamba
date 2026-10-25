# RC2A-02A Windows qualification — independent GLM review (Q1)

Review ID: `RC2A-02A-GLM-Q1` · Reviewer: GLM (independent) · Date: 2026-10-25 (Asia/Manila)
Review type: independent adversarial contract and evidence verification
Production remediation: **not performed and not authorized**
Review evidence: [`restricted-consumer-contract-rc2a-02a-glm-q1-evidence/`](restricted-consumer-contract-rc2a-02a-glm-q1-evidence/)
Probe source (REVIEW-ONLY, not in the solution): [`review/GlmQ1Probes/`](../../review/GlmQ1Probes/)

**Verdict (§M): SUPPORTED WITH QUALIFICATIONS.**

---

## A. Git binding

| Item | Identity (independently verified) |
|---|---|
| Canonical repository metadata | `C:\Users\Laszlos\source\repos\Gagamba\.git` (all worktrees under `C:\Users\Laszlos\source\repos\Solo\...` per repo convention) |
| Owner-acceptance baseline commit | `49ebbfec4abc7e5e1da2bd492f3ff4e43e14ca4e`, tree `6e94bee4c39801dfa256c158b300b066c0f2f217` ✓ |
| Tested implementation commit | `71a28afe2623516fb4176e6a4ce7988a3d70df06`, tree `70b932148cbfa59b0e424229879eebd36df2d80c` ✓ |
| Committed evidence commit | `c1a01761f8d58136a381ad87269635c6c14ad093`, tree `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19`, parent `71a28afe` ✓ |
| Documentation-only nature of the evidence commit | Verified: `git diff --name-status 71a28afe..c1a01761` touches only `docs/**` (evidence set + plan correction + handoff/queue); `src`, `tests`, `native` subtree identities are byte-identical between the two commits ✓ |
| Production source subtree at test source | `src` tree `b8c167e3d7340a562221591bdc2b6df7adb372d1` = accepted RC2A-01 identity ✓ |
| Tests subtree at test source | `tests` tree `5c3268286575cb55891fae9288627657f2d30ddf` ✓ |
| Native subtree at test source | `native` tree `85631d2f04512376621d3d09f51d67fd90f45335` = unchanged ✓ |
| Test-source commit chain | `bd553ec` → `18aa0db` → `49b5eb4` → `71a28af` (linear, on top of `49ebbfe`) ✓ |
| Preserved RC2A-01 independent review | blob `d329fcc49d289696bf1c4163f8f346c56115ba8c`, 16,888 bytes, byte SHA-256 `77d2404c61dbf1af76e8448b966f03482186d95fff5bf6a676a32fe6c7d8e4d0` — recomputed from `git cat-file blob` ✓ |

Review worktree: `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02a-glm-q1-review`,
branch `review/gagamba-rc2a-02a-glm-q1`, created with `git worktree add` from
exactly `c1a01761f8d58136a381ad87269635c6c14ad093`. The qualification branch,
owner-acceptance branch and canonical checkout were left untouched; nothing was
merged, rebased, reset, amended or pushed.

## B. Contract inventory (actual production guarantees examined)

Production sources inspected before accepting the harness's interpretation:
`src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestCodec.cs`,
`RestrictedManifestView.cs`, `RestrictedPreparationReference.cs`,
`src/Gagamba.Execution/Restricted/ManifestReader.cs`,
`ManifestValidation.cs`, `RestrictedManifest.cs`.

- Public surface: `RestrictedManifestCodec` (constants `SupportedSchemaVersion=1`,
  `MaxCanonicalBytes=65536`; `Supports`, `Decode`, `Encode`, `ComputeSha256`,
  `VerifySha256`), sealed `ManifestDecodeResult` (internal construction), the
  closed 12-code `ManifestDecodeError` enum starting at 0, immutable passive
  `*View` types with no public constructors, and the passive
  `RestrictedPreparationReference` with content equality.
- Decoder: pre-parse size/empty checks (`InputTooLarge@0` >65,536; `EmptyInput@0`),
  sequential fixed-grammar reads, remaining-byte capacity checks before
  allocation, strict UTF-8 (`InvalidUtf8`), tag range checks (`InvalidTag`),
  bytewise ordering/duplicates for sorted sets (`NonCanonicalOrder`/`Duplicate`),
  forced-value and cross-field checks, `TrailingData` after full consumption, and
  a final exact re-encode comparison refusing `InvalidValue@0` on divergence
  (`ManifestReader.Decode`). Accepted output is canonical by construction.
- Model constructors are fail-closed (empty capability set, forced ambient
  denials, forced-true tool/core/stdin, pending-only network/inode
  qualifications, budget kind/value consistency, store inode consistency,
  16 KiB combined argv/environment UTF-8-byte bound, fixed paths).
- Encode path: shared validating boundary; bounded canonical stream with
  inclusive 65,536 ceiling, `ArgumentException("manifest")`, no partial output.
- Digests: SHA-256 over complete canonical bytes only; wrong-length expected
  digest returns false; null-manifest throws `ArgumentNullException` before the
  length check (R2A-F04); all-zero is an ordinary present value.

## C. Canonical vectors

The 23 vectors are checked in the qualification through three representations
(committed literal hex, test-only raw wire template, independent
`ReferenceEncoder`) plus production decode compared property-by-property. This
review examined all three oracles for shared logic:

- `QualificationWireTemplate`/`QualificationWireWriter` (test-only): own byte
  writer; shares **no encoding logic** with production. It references the
  internal `BudgetValueKind`/`QualificationDisposition` enum values and the
  implementation-identifier constant — contract data (the API contract fixes the
  public enums' numbers), not shared logic.
- `ReferenceEncoder` (RC2A-01, preserved): reimplements the layout from the plan
  appendix with its own primitives and its own bytewise comparer; never calls
  `ManifestCodec`.
- Production `ManifestCodec`: the third path.

Independence finding: no oracle shares critical encoding logic with production,
and the oracles do not share logic with each other. A same-wrong-assumption
failure would additionally have to survive this review's own fourth
implementation (below).

**Review-local independent reproduction.** The probe host
(`review/GlmQ1Probes/`, review-only) contains an independent schema-1 parser and
an independent byte writer written from the committed API contract text only:

- `AR-10-GOLDEN`: the review-local writer reproduces the committed 873-byte
  golden literal from the documented graph values alone.
- `AR-10`: it also reproduces the committed 475-byte `Q2A-V-02-minimal` vector;
  its BCL SHA-256 matches the committed digest, and the bytes decode through the
  production decoder with the expected graph.
- The committed golden literal's SHA-256 was recomputed with BCL
  `SHA256.HashData`: `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`
  over 873 bytes ✓ (`IP-06`).
- All 21 committed hex literals present in `vector-manifest.json` were re-hashed
  independently; all 21 digests match. The two large vectors (`Q2A-V-19`,
  `Q2A-V-20`) are digest-bound only in the manifest (hex omitted for size);
  see finding RC2A02A-GLM-F03.
- The golden's wire offsets were re-derived with the review-local parser and
  agree with the qualification template's construction-derived offsets
  (implementation@2, namespaces.count@33, capabilities.count@82,
  noNewPrivs@86, filterSha256@87, readOnly.item[2]@157, mode@767).

Required counterexample classes are all present in the vector set (empty and
nonempty collections; absent vs present optionals; zero vs unlimited budgets;
empty argv item vs empty list vs empty environment value; non-ASCII and
supplementary-plane UTF-8; exact 16,384 and 65,536 boundaries; both dump-policy
values; `NoNewPrivs=false`; multiple closure shapes) — each verified by the
executed vector theory (27 cases reproduced in this review's R3 run).

## D. Refusal catalog

The 78-entry A5 catalog plus 873-prefix truncation sweep were reproduced inside
this review's R3 execution (79/79 A5 cases, 20/20 boundary cases). Independent
spot reproduction with review-derived expectations (raw bytes preserved in the
probe output; production decoder executed directly; governing rule stated):

| Probe | Input (raw bytes preserved) | Expected (contract-derived) | Actual | Rule |
|---|---|---|---|---|
| B-truncation | golden prefix 412 bytes | `InvalidLengthOrCount`@408 | ✓ | prefix ends inside a declared length that no longer fits the remaining bytes; documented field-start rule |
| B-trailing | golden + `0x00` | `TrailingData`@873 | ✓ | reader position after the manifest |
| B-inflated-ns-count | namespaces.count := 0xFFFFFFFF | `InvalidLengthOrCount`@33 | ✓ | count checked against remaining bytes before allocation |
| B-empty-mode | mode length := 0 | `InvalidValue`@767 | ✓ | empty non-allowed string |
| B-overlong-utf8 | mode := C0 AF | `InvalidUtf8`@767 | ✓ | strict UTF-8 |
| B-duplicate-namespace | namespaces item[2] := "pid" | `Duplicate`@60 | ✓ | adjacent duplicate at the second item start |
| B-unsorted-namespace | namespaces item[0] := "zpc" | `NonCanonicalOrder`@44 | ✓ | bytewise order violation at the second item start |
| B-nonempty-capabilities | consistent one-item capability variant | `InvalidValue`@82 | ✓ | capability set forced empty (wire-level check at the count) |
| B-forced-noIpc | byte 225 := 0x00 | `InvalidValue`@225 | ✓ | forced-true boolean |
| B-absent-nonzero | Memory budget Absent, value ≠ 0 | `InvalidValue`@230 | ✓ | budget kind/value consistency |
| B-inode-absent-nonzero | store[1] hasInode=0, inodes=5 | `InvalidValue`@216 | ✓ | store inode consistency |
| B-unsupported-schema | schema := 2 | `UnsupportedSchema`@0 | ✓ | refused as soon as read |
| B-unsupported-implementation | impl := " inux-…" | `UnsupportedImplementation`@2 | ✓ | exact ordinal identity |
| B-unknown-tag | byte 86 := 0x02 | `InvalidTag`@86 | ✓ | boolean tag range |

Precedence (AR-06): schema-before-implementation, implementation-before-later
counts, tag-before-value, `InvalidLengthOrCount` (declared length vs remaining)
before `Truncated`, size check before all parsing — all as documented. Offset
ambiguities discovered during probe authoring (see probe history in the
evidence directory) were **review-side errors** (inconsistent byte edits), not
decoder defects; in every case production's classification followed the
documented rule. No malformed input was observed to be accepted.

## E. Field mutation matrix

Reconciled independently against the committed 72-field RC2A-01 field map
(`docs/evidence/restricted-consumer-contract-rc2a-01/field-map.md`):

- The committed field-map has exactly 72 rows; the executed matrix
  (`exports/mutation-coverage-matrix.json`) declares 72 fields, 279 cases, and
  the per-class distribution matches the traceability matrix exactly
  (valid 51, boundary 70, presence 41, tag 15, forced 12, order 6, duplicate 6,
  length 19, utf8 18, cross 16, empty 14, supported-id 2, misuse 1, trace 8;
  row classes: valid 162, malformed 68, noncanonical 40, misuse 1, trace 8).
- Every field has ≥1 case (0 fields with 0 cases); case IDs are unique and
  field-index-stable; composite trace cases bind the shipped view property
  inventories to the committed subfield lists (verified in source
  `Q2A_F_*` self-checks, reproduced in R3).
- The matrix self-tests reject fabricated class coverage and orphan fields
  (`Q2A_F_MatrixAssignsEveryApplicableClassAndRejectsInapplicableOnes`,
  `Q2A_F_CompositeSubfieldsMatchTheCommittedFieldMapAndShippedViews`).
- Coverage integrity: valid/boundary/presence cases go through the real
  validating constructors, assert independent-reference equality, canonical
  byte/digest change (where required), decode success, full graph projection
  and an explicit domain check; refusal cases assert exact code + exact
  construction-derived offset plus a canonical positive counterpart. The
  harness self-tests prove the machinery detects injected wrong code/offset and
  stale expectations (non-vacuity).
- Nested composites (budgets, stores, closure objects, environment pairs) are
  counted consistently with the field map's row model (composite rows + subfield
  rows); no relevant field is omitted.

## F. Mutation sweep and the Q2A02A-F01 correction

**Disposition: correct qualification-plan correction.** Independent evidence:

1. This review **fully reproduced the complete single-byte sweep** with its own
   driver (probe `AR-11`, review-local parser-derived immutable map):
   873 × 255 = 222,615 mutations; **114,378 accepted / 108,237 refused**; refusal
   histogram identical to the committed evidence on all nine codes
   (UnsupportedSchema 510, UnsupportedImplementation 3,684, Truncated 138,
   InvalidLengthOrCount 49,211, InvalidUtf8 34,042, InvalidTag 10,700,
   InvalidValue 7,605, NonCanonicalOrder 2,345, Duplicate 2); **576 survivor
   positions** with per-position accepted-value counts matching the committed
   profile exactly; **zero `InvalidValue@0`**.
2. Every one of the 114,378 accepted mutations re-encoded byte-for-byte to
   itself. Canonical rigidity is not merely a test observation: the decoder's
   final exact re-encode comparison (`ManifestReader.Decode`) enforces it in
   production, so "accepted ⇒ canonical for the parsed graph" is a structural
   property of the shipped decoder.
3. No survivor falls inside this review's independently derived immutable
   regions (schema, implementation ID, fixed paths, forced-true booleans,
   pending-only qualification tags).
4. `AR-02` (required re-segmentation counterexample): golden byte 157 → `0x3B`
   (first byte of the `readOnly.item[2]` length prefix, 5 → 59) is **accepted**;
   the review-local parser independently confirms the resulting graph — the
   third read-only destination becomes a 59-byte string absorbing store-section
   bytes, the store count re-reads as 0 (both writable stores disappear), and
   the remaining tail parses canonically. Production re-encodes byte-exactly;
   all mandatory fields (13 budgets, channels, closure, invocation) remain
   present; the 16 KiB argv/environment bound still holds in the decoded view.
   The result is a **different, valid, canonical manifest representing different
   authority** (no writable stores) — a canonical-data property, not a decoding
   defect. Canonical decoding, manifest validity, authority authorization and
   runtime sandbox enforcement remain distinct layers; only the first two are
   in scope here, and both hold.
5. `AR-03` (nested omission): the fixed-order grammar has no omittable nested
   value; an inflated item count that would shift parsing refuses
   (`InvalidLengthOrCount@224` at the offending third-store length prefix).
   An "omitted" store list (count 0) is a representable alternative, not an
   omission.
6. The plan's literal B6 row ("no mutation outside the two digest fields is
   accepted") is demonstrably false, and the corrected row (canonical rigidity +
   immutable-region containment + preserved survivor profile) is exactly the
   invariant the shipped decoder maintains. The failed first run is preserved
   (`runs/failed-attempts/b-first-sweep-failure.trx`: 19 passed, 1 failed —
   `Q2A_B_18_SingleByteFlipSweepEnforcesCanonicalRigidity`) and is not recounted
   as a pass. The correction was committed as documentation per the plan's own
   rule. No security-significant ambiguity remains: acceptance of a
   re-segmented manifest is canonical acceptance of different data, and the
   sweep was never claimed to establish authorization or sandbox enforcement.

## G. Boundaries and digests

All reproduced in R3/R1 plus targeted probes:

- Manifest size: 65,535/65,536 accepted with full graph + re-encode
  (`Q2A_B_01/02`); 65,537 refused symmetrically by bounded encode, view
  projection (`ArgumentException("manifest")`) and decode (`InputTooLarge@0`)
  (`Q2A_B_03`); 1,000,000 refused pre-parse (`Q2A_B_04`). Probe `AR-08`:
  65,537-byte input with an immediately invalid schema still refuses
  `InputTooLarge@0` (size precedes parsing); a 65,536-byte input is parsed
  (ceiling inclusive); empty input refuses `EmptyInput@0`.
- argv/environment: 16,383/16,384 accepted (inclusive); 16,385 refuses
  construction and, on a directly crafted wire, refuses `InvalidValue` at the
  invocation end (`Q2A_B_07..09`); multibyte shape proves UTF-8-byte counting,
  not characters (`Q2A_B_10`, é × 8,191 + 2-byte key = exactly 16,384).
- Empty distinctions: empty argv item refuses (`InvalidValue`/constructor);
  zero-length argv list is valid and encodes a zero count; empty environment
  value is a valid present value; absent environment entry is distinct
  (`Q2A_B_11..14`, vectors V-05/V-06/V-07).
- Digests: null manifest throws `ArgumentNullException` (also combined with
  wrong-length, R2A-F04, `Q2A_B_20/23`); wrong lengths 0/31/33/64/65,536 return
  false; matching true; changed and all-zero false against the nonzero golden;
  all-zero filter digest decodes as an ordinary present value; digest is opaque
  data — a structurally broken input with an all-zero digest refuses for the
  structural reason (probe `AR-07`); `ComputeSha256` equals BCL SHA-256 of the
  canonical bytes for every probe case.
- `Supports`: exact ordinal on both arguments; whitespace/case/trailing-NUL/
  unknown all false; null ID throws even at unsupported schemas (`Q2A_B_24`,
  probe `AR-09`).
- Truncation: all 873 proper prefixes refuse without a partial view;
  `Truncated` offsets equal end-of-input when that code fires; no
  `InvalidValue` observed (`Q2A_B_17`, A5-01; reproduced in R3).

## H. Public API and misuse

- Documented public API matches production behavior: exact method set, closed
  refusal vocabulary, `ManifestDecodeResult` with internal construction, view
  types with no public constructors (probe `AR-09-NO-PUBLIC-CTOR`: 0 public
  constructors across the view type), passive reference validation/equality.
- Public callers cannot bypass mandatory validation: views can only be obtained
  from `Decode` (which validates and re-encodes) or internal construction that
  runs the same validation; `Encode` runs the shared validator and the bounded
  writer; no test-only seam exists in production (no hooks; the re-encode branch
  is production code assessed, not modified).
- Exceptions and refusal results are not conflated: decode refusals are results
  with code+offset; programming errors (null arguments, empty GUIDs, wrong
  digest length in the *reference*) are exceptions; `VerifySha256` wrong-length
  is a `false` result, exactly as clarified.
- Previously recorded R2A-F01..F04 dispositions verified against code and
  evidence: R2A-F01 assessment exists and its reasoning was independently
  re-derived here (pass-through on every accepted decode; failure path never
  observed across 222,615 sweep decodes + all vectors/catalog cases; no
  production hook); R2A-F02 API document rebound by LF-normalized SHA-256
  (recomputed ✓); R2A-F03 binary-identity non-claim retained; R2A-F04 combined
  null+wrong-length assertion present (`Q2A_B_23`). No contradiction found.

## I. Evidence integrity and reproducibility

- `SHA256SUMS`: all 29 entries recomputed — **29/29 match**.
- `source-binding.json`: all 18 added test files re-verified for exact Git blob
  identity and byte SHA-256 — **18/18 match**.
- `document-sha256.json`: all 20 documents re-hashed over LF-normalized bytes
  (CRLF→LF) — **20/20 match**; the preserved RC2A-01 review blob re-hashed from
  `git cat-file` — **matches** `77d2404c…d4d0` over 16,888 bytes.
- Raw artifacts support the summaries: TRX outcome counts recomputed by this
  review — public 67/0/0, internal 514 total (473 passed, 41 NotExecuted),
  required set 457/0/0, rerun 457/0/0; the preserved failed attempt is 19
  passed / 1 failed (`Q2A_B_18…`), not recounted.
- The 41 R2 skips are pre-existing Linux-native classes (NonRoot 31,
  OwnerDeath 7, Wp05ReleasePath 3); none is an RC2A-02A case. See finding
  RC2A02A-GLM-F02 for a wording imprecision in the finding text.
- Regenerated-vs-original distinction: export files embed `sourceCommit`; the
  flip-sweep export records a `durationMs` observation and is disclosed as the
  one byte-level rerun difference (semantic content deterministic). This
  review's rerun reproduced all deterministic exports byte-for-byte in content
  (counts, histogram, survivor profile) and the vector/matrix exports are
  derived from committed constants.
- No report claims execution after the evidence commit; the evidence commit is
  documentation-only with tested source bound to `71a28afe` (verified in §A).

## J. Adversarial probes (new counterexamples)

Probe host: `review/GlmQ1Probes/` (REVIEW-ONLY console project; references only
`Gagamba.Execution`; not added to `Gagamba.sln`). Full preserved output:
[`restricted-consumer-contract-rc2a-02a-glm-q1-evidence/probe-output.txt`](restricted-consumer-contract-rc2a-02a-glm-q1-evidence/probe-output.txt)
— **103/103 checks pass** (including 7 probe-authoring corrections preserved in
history; none required production change).

| Probe | Challenged invariant | Result |
|---|---|---|
| AR-01/AR-02 | Accepted length-prefix re-segmentation is canonical, validated, fully structured | Held (independently parsed + production cross-checked) |
| AR-03 | A nested value cannot be omitted via length mutation | Held (grammar has no omittable nested value; inflation refuses) |
| AR-04 | Absent / Exact-0 / Unlimited are distinct, canonical-zero, visible | Held (three distinct encodings; kinds visible) |
| AR-05 | Malformed UTF-8 crossing a length boundary cannot enter the model | Held (`InvalidUtf8` at field start) |
| AR-06 | Documented refusal precedence (schema→impl→tag→length→truncation) | Held (5 precedence cases) |
| AR-07 | Digest is opaque data; structural validation independent of digest content | Held |
| AR-08 | Size refusal precedes parsing; ceiling inclusive | Held (4 cases) |
| AR-09 | Public surface cannot bypass validation; misuse is stable | Held (11 checks) |
| AR-10 | Committed vectors reproducible without the committed generators | Held (review-local writer reproduces golden + minimal vector + digests) |
| AR-11 | Complete 222,615-mutation sweep invariants | Held (exact counts, histogram, survivor profile, canonical rigidity ×114,378, zero InvalidValue@0, immutable containment) |

## K. Findings

**RC2A02A-GLM-F01 — Q2A02A-F01 sweep-correction assessment: correct plan
correction (NO ISSUE FOUND). Severity: n/a (assessment).**
The plan's literal B6 row was wrong and is now corrected by documentation; the
corrected invariant is production-enforced, not just tested. Independent full
sweep reproduction (§F) supports every number. The accepted re-segmentation
mutations represent different-but-valid authority descriptions; they do not
weaken the decoder contract, and no claim of authorization/sandbox invariance
was made or needed. Remaining uncertainty: none within the reviewed corpus;
the assessment is empirical + source-level, not a formal proof for arbitrary
future inputs (disclosed in `reencode-branch-assessment.md`).

**RC2A02A-GLM-F02 — Skip-class wording imprecision (TEST EXPECTATION ERROR,
P3, non-blocking).** `findings.md` Q2A02A-F02 says the committed internal filter
"also matches five pre-existing Linux-native classes", which is true, but on
Windows only three of them contribute the 41 skips; the other two
(`CoordinatorProtocolQualificationTests` 11 tests, `Wp05V3IndependentQualificationTests`
5 tests) are pure tests that executed and passed in R2 (473 = 457 + 16). The
reported counts (473/0/41, 514 total) are accurate; only the implication that
the five listed classes are the skip set is loose. No impact on R3/R4 or on any
RC2A-02A claim. Remediation direction: one-sentence clarification in a future
documentation commit; not required for acceptance.

**RC2A02A-GLM-F03 — Two vectors are digest-bound only in the committed manifest
(EVIDENCE LIMITATION, P3, non-blocking).** `vector-manifest.json` omits the hex
of `Q2A-V-19-max-argenv-16384` (16.6 KB) and `Q2A-V-20-max-manifest-65536`
(64 KB) for size; their byte-level binding rests on the committed recipes, the
wire template, and the reproduced test runs. This review independently verified
the other 21 vectors' hex+digest byte-for-byte and reproduced the full suite
that re-derives the two large vectors (all green). Impact: negligible; the
digests plus reproducible recipes remain strong bindings. Remediation direction:
none required; an optional review-side large-vector byte export could be added
in a later evidence refresh if desired.

**RC2A02A-GLM-F04 — Plan §4 RC-D04/Q2A-S row deferred (EVIDENCE LIMITATION, P3,
non-blocking).** The dedicated public-surface/misuse family (Q2A-S) and
dedicated passive-reference rows were not executed in RC2A-02A; the executed
traceability matrix discloses the deferral as outside the assignment's
authorized families, and coverage of the passive reference currently rests on
the RC2A-01 regressions (PreparationIdentityTests, 10 cases) plus the
implementation-id misuse case. This review's `AR-09` probe exercised the
reference validation/equality and found no defect. The obligation remains open
for a later authorized stage; it does not undermine the Windows consumer-contract
claims that were made.

**RC2A02A-GLM-F05 — No confirmed production defect (NO ISSUE FOUND).**
Across source inspection, the reproduced 457+67-case suites, 103 probe checks,
a complete independent 222,615-mutation sweep and 78-entry catalog spot
reproduction, no accepted malformed input, no wrong refusal code or offset, and
no validation bypass was observed.

Probe-authoring history (preserved, not hidden): the first probe revision
failed 7 checks; all 7 were review-side errors (hand-computed offsets,
byte-edits producing inconsistent inputs, one hex transcription typo). The
corrected probes are committed; the production behavior in each disputed case
followed the documented rule.

## L. Qualification scope (supported claims vs exclusions)

**Supported by this review (Windows, managed-code, consumer contract):**
canonical schema-1 encode/decode identity over the 23-vector set; exact refusal
codes/offsets over the 78-entry catalog + 873-prefix sweep; 72/72 field
mutation coverage with exact code/offset assertions; 65,536/65,537 and
16,384/16,385 boundaries with UTF-8-byte counting; empty/absent/presence
distinctions; digest and identifier behavior including R2A-F04's combined case;
canonical-rigidity and immutable-region containment over the complete
single-byte mutation space; evidence integrity (hashes, bindings, raw
artifacts, failed-attempt preservation); deterministic reruns.

**Expressly excluded (not re-verified, not claimed):** Linux/macOS/WSL2
qualification; native sandbox isolation; host-broker authority isolation;
binary reproducibility or package identity (R2A-F03); NuGet cleanliness;
clean-consumer or mixed-dependency integration; Hufu AE4-B01 readiness;
RC2A-02B completion; RC2A-03/RC2A-04 readiness; production sandbox security on
any platform; Q2A-S/RC-D04 dedicated surface family (deferred, see F04).
Passing this review does not unblock Hufu.

## M. Final verdict

**SUPPORTED WITH QUALIFICATIONS.** The committed RC2A-02A Windows claims are
independently supported within the reviewed scope, including the corrected
mutation-sweep characterization (Q2A02A-F01), with three non-blocking P3
qualifications recorded (F02 wording, F03 large-vector binding mode, F04
deferred Q2A-S family). No confirmed production or qualification defect was
found. A clean test run alone was not the basis: every claim was re-derived
from the contract, an independent parser/writer, and adversarial execution.

## N. Owner decision requirements

Before authorizing RC2A-02B, the owner should:

1. Accept this review and its P3 qualifications (or direct clarifications).
2. Confirm the Q2A02A-F01 plan correction (canonical rigidity + immutable-region
   containment) as the standing B6 row.
3. Decide whether the deferred Q2A-S/RC-D04 family (RC2A02A-GLM-F04) executes in
   RC2A-02B or in a separate slice.
4. Optionally direct the one-sentence Q2A02A-F02 clarification (RC2A02A-GLM-F02).
5. Note explicitly that RC2A-02B remains a separate authorization and that this
   review does not unblock Hufu.

---

### Review execution record

- Review branch/worktree: `review/gagamba-rc2a-02a-glm-q1` at
  `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-02a-glm-q1-review`, created
  from exactly `c1a01761f8d58136a381ad87269635c6c14ad093`.
- Executed commands (this review, Windows 11 NT 10.0.26200.0, AMD64,
  .NET SDK 10.0.401, runtime Microsoft.NETCore.App 10.0.12, xunit.v3 3.2.0 /
  xunit.runner.visualstudio 3.1.4 / Microsoft.NET.Test.Sdk 18.0.1, net10.0):
  - `dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release --no-restore -warnaserror`
    (after a fresh `dotnet restore`; 0 warnings / 0 errors)
  - `dotnet test tests/Gagamba.Execution.Tests/... -c Release` → **67/67/0**
  - `dotnet test tests/Gagamba.Execution.Linux.M1.Tests/... --filter
    'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.'`
    → **457/457/0** (twice; identical name/outcome multisets)
  - same with the committed `~Qualification` filter → **473 passed / 41 skipped
    (514 total)**, skips = NonRoot 31 + OwnerDeath 7 + Wp05ReleasePath 3
  - `dotnet run -c Release --project review/GlmQ1Probes/GlmQ1Probes.csproj`
    → **103/103 probe checks pass** (exit 0)
  - hash recomputation of `SHA256SUMS` (29/29), `source-binding.json` (18/18),
    `document-sha256.json` (20/20), preserved review blob (match)
- Probe output: `restricted-consumer-contract-rc2a-02a-glm-q1-evidence/probe-output.txt`
  (SHA-256 `164C40BB1BF61BF6DA19468205395A9CF702FC98F1B82DA1744F002F9D1E29E3`).
- This review's required-set TRX SHA-256 (regenerable; not committed):
  required `2b4a0790be1a60c6844e586b9512ec010d98f6e4c902a9f89a31e3cf63fac085`,
  rerun `7d1e9295737931a5beb3748bcf78595958222c76e97d7159e5ca68bc65582bab`,
  internal `e94fdeaa79f26023b8c4451f7ba4c262143d24549dbfe3e8b902c1f72dae4679`.
- Verification not performed: full rerun of R2's native-class tests on Linux
  (out of scope); binary reproducibility (excluded by R2A-F03 rule); the two
  large vectors' hex was verified via digest + suite reproduction rather than
  byte-level export (F03).
