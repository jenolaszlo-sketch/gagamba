# RC2A-02A executed traceability matrix

Date: 2026-10-25 (Asia/Manila). Test source commit
`71a28afe2623516fb4176e6a4ce7988a3d70df06` (tree
`70b932148cbfa59b0e424229879eebd36df2d80c`). Authoritative runs:
`runs/windows-required-set.trx` (R3, 457/0/0) and
`runs/windows-public.trx` (R1, 67/0/0); the exact committed-plan internal
command is `runs/windows-internal.trx` (R2, 473/0/0 with 41 out-of-scope
Linux-native skips). Machine-readable matrix:
[`exports/mutation-coverage-matrix.json`](exports/mutation-coverage-matrix.json);
family counts: [`family-results.json`](family-results.json); boundary rows:
[`boundary-results.json`](boundary-results.json).

## Executed case families

| Family | Cases executed | Result | Evidence |
|---|---:|---|---|
| Qualification harness self-tests | 7 | 7 passed | `Q2A_Harness01..07` in R3 |
| Q2A-V canonical vectors | 27 (23 vectors + 4 set checks) | 27 passed | `Q2A_V_*` in R3; [`exports/vector-manifest.json`](exports/vector-manifest.json) |
| Q2A-V A5 refusal catalog | 79 (78 entries + 1 catalog check) | 79 passed | `Q2A_V_A5_*` in R3 |
| Q2A-F field mutations | 284 (279 cases over 72 fields + 5 matrix checks) | 284 passed | `Q2A_F_*` in R3; matrix export |
| Q2A-B boundary/sweeps | 20 (19 methods; the 16,383/16,384 theory has 2 rows) | 20 passed | `Q2A_B_*` in R3; boundary export |
| Q2A-E evidence exports | 2 | 2 passed | `Q2A_E_*` in R3 |
| Q2A-B public digest/identifier boundaries | 10 | 10 passed | `Rc2a02PublicBoundaryTests` in R1 |
| RC2A-01 contract regressions (internal) | 38 | 38 passed | R3 |
| Existing public regressions | 57 | 57 passed | R1 |
| **Required-set total** | **457** | **457 passed, 0 failed, 0 skipped** | R3 and identical R4 rerun |
| **Overall results relevant to this slice** | **524** | **524 passed, 0 failed, 0 skipped** | R1 + R3 |

The committed-plan internal filter additionally selects 41 pre-existing
Linux-native tests that skip on Windows; they are not RC2A-02A cases and are
disclosed in [findings](findings.md#q2a02a-f02--exact-plan-filter-selects-out-of-scope-linux-native-classes).

Q2A-F mutation-class distribution over the 279 cases: valid 51, boundary 70,
presence 41, tag 15, forced 12, order 6, duplicate 6, length 19, utf8 18,
cross 16, empty 14, supported-id 2, misuse 1, trace 8. Row classes: valid 162,
malformed 68, noncanonical 40, misuse 1, trace 8. All 72 committed field-map
entries are present in order; every applicable class has at least one case and
no inapplicable class has a fabricated case (`Q2A_F_MatrixAssignsEveryApplicableClassAndRejectsInapplicableOnes`).

## Plan §4 requirement mapping

| Requirement source | Obligation | Case family | Executed status |
|---|---|---|---|
| RC-D02 (field table) | Every manifest field exposed and mutation-mapped | Q2A-F1/F2 over 72 fields | **PASS** — 72/72 fields, 279 cases, applicability table with explicit non-applicable reasons |
| RC-D03 (canonical grammar) | Exact encode/decode identity; closed refusal vocabulary | Q2A-V3, Q2A-B1, Q2A-F4 | **PASS** — 23 vectors × three independent representations; 78-entry A5 catalog; exact code/offset assertions; 12-code vocabulary pinned |
| RC-D03/R1-F01 | Symmetric inclusive 65,536-byte ceiling; no partial output | Q2A-B1 | **PASS** — 65,535/65,536 accepted; 65,537 refuses bounded encode, projection and decode; 1,000,000 refuses pre-parse; legacy encoder used only as deterministic positive control |
| RC-D03/R1-F05 | Digest length rules, `Supports` boundaries, oversized encode refusal | Q2A-B4, Q2A-F3, Q2A-B1 | **PASS** — lengths 0/31/33/64/65,536 false; 32 matching true; changed/all-zero false; exact ordinal `Supports`; null-ID programming errors; ceiling cases |
| RC-D04 (passive reference) | Five fields, validation, content equality, no temporal/control members | Q2A-F2 (reference rows), Q2A-S | **DEFERRED — Q2A-S** is outside the RC2A-02A Windows authorization; unchanged obligation for a later authorized stage. RC2A-01 regressions remain green. |
| RC1-A §5 | Encode-side refusal evidence at both byte boundaries | Q2A-B1 | **PASS** — bounded writer guard + projection guard at 65,537; inclusive 65,536; no partial buffer |
| RC1-A §6 / R1-F05 | 31/33-byte digests, all-zero rule, `Supports` positive/unknown/null | Q2A-B4, Q2A-F3 | **PASS** — public and internal assertions |
| R1-F04 | No temporal fields; documentation preserved | Q2A-S negative surface check | **DEFERRED — Q2A-S** outside this authorization; no temporal surface change was made (src tree unchanged) |
| R2A-F01 (re-encode branch) | Reachability assessment; no artificial test if unreachable | Q2A-B5 assessment | **PASS** — [assessment](reencode-branch-assessment.md); branch exercised as pass-through on 114,378 accepted sweep decodes; zero `InvalidValue@0` |
| R2A-F02 (API doc digest) | Rebind clarified API document in evidence inventory | Q2A-E1 | **PASS** — [`document-sha256.json`](document-sha256.json) |
| R2A-F03 (DLL byte identity) | Keep test-versus-binary reproducibility distinct; no binary claim | Q2A-E4 rule | **PASS (disclosure retained)** — test outcomes only; no DLL/package identity claim |
| R2A-F04 (combined null+wrong-length; argv observations) | Explicit combined assertion; empty argv-item and zero-length argv-list cases | Q2A-B4, Q2A-B3 | **PASS** — `Q2A_B_20..23`, `Q2A_B_11..14` |
| RC-T01..T06, T15, T16 (2A subset) | Work-order 2A test obligations | Q2A-V, Q2A-F, Q2A-B | **PASS within authorized families** — execute/refusal/boundary evidence above; native portions of the T-set remain out of scope |
| RC-T12/T17 (packaging subset) | Not in RC2A-02 | — | **OUT OF SCOPE** (RC2A-04) |
| RC-T07..T11, T13 (native portions), T14, T18..T23 | Not in RC2A-02 | — | **OUT OF SCOPE** (RC-2B/native gates) |

## Additional plan obligations

| Plan obligation | Status |
|---|---|
| §3 A1 vector set (golden + listed representable distinctions) | **PASS** — 23 vectors cover empty/nonempty collections, empty argv/environment/value, absent/exact-zero/unlimited budgets, inode presence/absence, `NoNewPrivs=false`, both `RuntimeDumpsDisabled` values, core pattern absent/present (NUL + supplementary), non-ASCII and supplementary UTF-8, 16,384-byte argv/environment, 65,536-byte manifest, minimal manifest, duplicate argv, empty closure |
| §3 A2/A3/A4 reproducible generation and independent verification | **PASS** — committed hex equals independent raw-template build and `ReferenceEncoder` output; digests recomputed with BCL SHA-256; reruns reproduce bytes/digests; production decode of every vector matches the committed graph |
| §3 A5 named malformed/noncanonical catalog | **PASS** — 78 entries: truncation (873 prefixes), trailing data, oversized/empty input, inflated counts/lengths, reduced length, malformed UTF-8 (overlong/surrogate/out-of-range), unknown tags, unsorted sets, adjacent duplicates, empty non-allowed strings, forced contradictions, unsupported schema/implementation |
| §3 B1/B2/B3/B4 boundary and misuse cases | **PASS** — see Q2A-B rows |
| §3 B5 re-encode branch assessment | **PASS** — see assessment |
| §3 B6 truncation and single-byte sweeps | **PASS with corrected characterization** — 873-prefix refusal; 222,615-mutation sweep with canonical rigidity, immutable-region containment, survivor-profile evidence; see finding Q2A02A-F01 |
| §3 E1–E5 evidence hygiene | **PASS in this evidence set** — rebound document inventory, exact source identities, runner provenance, reproducibility distinction, failed attempts/limitations |
| §3 P1–P4 cross-platform portability (Q2A-P) | **NOT RUN** — not authorized in RC2A-02A; Windows only. No cross-platform claim is made. |
| §3 F1/F2 independent review exit | **PENDING** — independent review has not occurred; this is the next gate |
