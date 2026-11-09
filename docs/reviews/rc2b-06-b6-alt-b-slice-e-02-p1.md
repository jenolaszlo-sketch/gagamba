# ALT-B Slice E-2 P1 — frozen-schema contract core implementation

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P1`

Date: 2026-11-09 (Asia/Manila). Producer: DeepSeek. Scope: bounded internal implementation and implementation evidence on canonical `main`; no push. This is a single-model producer record; it is **not** independent qualification, owner acceptance, or a native result.

Binding authorization: [E-2 entry owner decision 02](../decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-02.md), disposition **E-2 ENTRY AUTHORIZED WITH QUALIFICATIONS — E-2-P1 ONLY**; authorized future implementation scope §§7–14, surfaces §11, prohibited behavior §§10, 13, 18.

Producer disposition: **IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION** (§20).

---

## 1. Verified starting baseline

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `2dab45aa4f4be6b0f713a35b4d50bdfa6fc925be` |
| Starting tree | `b458be30925bfc71b7b96fe40b6966126d0bcf74` |
| Worktrees | exactly one canonical checkout |
| Tracked / index state before edits | clean |
| Historical untracked paths | 44, all present, per-file SHA-256 match, `diff_count=0` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 21 commits ahead; no push |
| P0 / D1 / entry-owner-02 blobs | `db25949d…` / `83fe8d26…` / `f5ab96c6…` (unchanged) |
| Network / package / cloud | none used |

All listed acceptance commits are ancestral (`git merge-base --is-ancestor`, exit 0). No reset, clean, merge, rebase, prune, stash, history rewrite, branch, or additional worktree was used.

---

## 2. Binding owner authorization

The owner decision authorizes exactly one bounded package and fixes its boundary: CJSON-1 encode/decode, strict schema validation, M/F/S structural checks, K structural checks, exact `Hm/Hf/Hs` bindings, deterministic identity verification, fail-closed rejection, non-authoritative results, regression tests, reachability/surface assertions, and reproducible evidence. It prohibits approval-chain behavior, V/W/A actors, R/Wc/P creation or verification, protected roots/custody, publication/withdrawal, live challenge/response, capability, delegation, standing approval, cross-host reuse, E-1 consumption, public APIs, and any authority factory. Implementation surfaces are limited to new files under `src/Gagamba.Execution.Linux.M1/Validation/` and `tests/Gagamba.Execution.Linux.M1.Tests/Validation/`, with all existing files read-only (a shared-extraction route would have required a separate owner amendment).

---

## 3. Exact implementation files

Production (new; no existing file changed):

| File | Bytes | SHA-256 |
|---|---|---|
| `src/Gagamba.Execution.Linux.M1/Validation/CanonicalJson.cs` | 5,045 | `fa1ec8452ed42edcda0a47398a56b67debb8ea44cfe61d58a910c3e8f4108147` |
| `src/Gagamba.Execution.Linux.M1/Validation/FrozenSchemaRules.cs` | 1,771 | `435fff7e8bfe1f9492f3caa72c77c986e92547707ad07b157acd1d06c0d886ba` |
| `src/Gagamba.Execution.Linux.M1/Validation/FrozenContracts.cs` | 2,257 | `8dae7db2d3f9dd0f701418aaece00f26d54b3ff9ad3f10e5a15f7596ba91de9c` |
| `src/Gagamba.Execution.Linux.M1/Validation/FrozenContractVerifier.cs` | 27,752 | `3f357b8df37f7d1f8009e9642aa82675f02e8797a87f94e921c7ab4b48430344` |

Tests (new):

| File | Bytes | SHA-256 |
|---|---|---|
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/ValidationFixtures.cs` | 9,445 | `55233358386d24e469eeff5466078754da9a921117731b618770e2273f958764` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/CanonicalJsonTests.cs` | 6,361 | `be10edd7bc197c8177c264dc0030d4e1dc0d8b3a834d2f45e32857e13f5de5b6` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/FrozenContractTests.cs` | 5,340 | `55de9d0dca2c1a662f05811cc22060723575ed0740571c92f9710f16aa3422b1` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/FrozenContractNegativeTests.cs` | 16,044 | `7742fb85d31a643fb1edada2b519df762a83a24b51694893118aec65bfd0b4b8` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/Cjson1F02ParityTests.cs` | 8,272 | `7c7672309e23cc2e02ff61378a279ab80031e56bd2d79a2e2e818d70a2f3b2d2` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/ValidationReachabilityTests.cs` | 11,653 | `07a718d7195f1c3eaf1f4a5d56a22cd3bd4add9d6d9e796f154cce1195c87f3b` |

Evidence: `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p1-evidence/` (README, contract matrix, codec/parity analysis, positive fixtures and digests, negative-control matrix, reachability/RELEASE findings, executed results). Unchanged by this package: `FinalAcceptanceCodec`, installed verifiers, E-1 projection, authority constructors/factories, `eng/**`, project/solution files, package references, and all existing tests.

---

## 4. Normative specification references

- Adopted P0 record `docs/evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md` §§6–10 (CJSON-1 and K/M/F/S/R/Wc/P), itself extracted from the F02-accepted external `final-acceptance-hash-contract.md` (`49d566e1…`) and `case-policy-and-action-contract.md` (`667e6118…`).
- S2 `ValidId` grammar: `docs/reviews/rc2b-06-b6-impl-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md` §1.
- D1 approval model and boundaries (approval omitted by design): `docs/decisions/rc2b-06-b6-alt-b-slice-e-02-d1.md`.
- Owner authorization and prohibitions: `docs/decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-02.md`.
- F02 codec parity reference: `src/Gagamba.Execution.Linux.M1/Installed/FinalAcceptance.cs` (read-only).

---

## 5. CJSON-1 implementation contract

`CanonicalJson` implements the accepted rules exactly: size 1..65,536 bytes; all bytes 0x20–0x7E; one root object parsed with a depth bound of 8; strictly ascending ASCII keys; printable non-empty strings; unsigned canonical integers via raw-text equality and `TryGetInt64`; booleans; `null` permitted structurally; and a parse→canonical-serialize byte-equality proof using `Utf8JsonWriter` with relaxed escaping. `Encode` validates then serializes without repair; `Hash` is SHA-256 over the exact bytes as 64 lower-case hex; `IsLowerHex` matches the accepted convention. Codec-level decisions (depth bound, `null` permission, empty-string rejection, schema-level unknown-field handling) are documented with their accepted basis in [cjson1-contract.md](rc2b-06-b6-alt-b-slice-e-02-p1-evidence/cjson1-contract.md).

---

## 6. F02 codec parity assessment

`FinalAcceptanceCodec` exposes no canonical serializer, so parity was measured behaviorally plus exact round-trip identity of accepted bytes. Findings: canonical F02 record/selector accepted by both; 13 noncanonical categories rejected by both; `Hash`/`IsLowerHex` outputs equal on samples; depth bound identical (8); empty strings rejected by both. Documented schema-scoped differences: `null` values (E-2 schemas require them; F02's schema forbids them) and unknown fields (codec-level acceptance vs schema-level field sets). `FinalAcceptanceCodec` was not modified; the standalone core carries parity tests and a **divergence-control obligation**: any future change to either canonical implementation requires a parity reassessment before reliance.

---

## 7. M/F/S/K schema implementation

- **M:** exact field set; `schemaVersion`=1; `ValidId` case id; topology enum; B2 exactly empty objects; B3 exactly four ordered roles with exact `path`/`mode`/`destination`/length bounds and aggregate bound.
- **F:** exact 19-key set; `policyKind`↔`topology`; B2 hard-false/null rules; B3 exact `hostPaths` (path/device/inode/mode/uid/gid, uid/gid equal F), enrollment `ValidId`s, fixed `copy`/`invocation`/`sandbox`/`resources` constants; canonical UUID `bootId`; uid/gid nonzero ≤2^31-1; `expiresLeadNs` ≤ `maximumSessionNs`.
- **S:** exact 7-key set; exactly one of the three accepted templates per topology; contiguous indexes; allowed op literals; timeout bounds; no extension fields; per-action `Ai` hashes.
- **K:** exact 22-key set; pre null-pair or post `ValidId` + 64-hex qualification pair; 64/40-hex fields; text bounds; `maximumSessionNs` 1..600000000000; no review/owner/receipt/self-hash/approval fields.

Runtime-only checks that P1 deliberately does **not** perform (they are not data-contract properties): installed coordinator/filter role equality, current-boot equality for `bootId`, "approved" provisioning status of uid/gid, host-path ancestry/pinning and cgroup semantics, effective sandbox behavior, and protected store interactions. These remain downstream obligations.

---

## 8. Hm/Hf/Hs/Hk computation and verification

All identities are SHA-256 over the exact artifact bytes after the CJSON-1 equality proof: `Hm = SHA256(M)`, `Hf = SHA256(F)`, `Hs = SHA256(S)`, `Hk = SHA256(K)`. The chain verifier recomputes each hash from the received bytes and enforces: `F/S/K` bind `Hm`; `S/K` bind `Hf`; `K` binds `Hs`; K/F/S/M case, phase, and topology agree; F/K `hostId` agree; `allowRelease` agrees across F/K/template; F/K session limits agree; S timeouts are within the F maximum. Per-action hashes use `SHA256("gagamba-validation-action-v1" ∥ 0x00 ∥ CJSON1(action))`. No identity asserted only by input metadata is trusted; every comparison uses a locally recomputed value.

---

## 9. Independently constructed positive fixtures

No committed accepted K/M/F/S fixture set exists, so fixtures were constructed from the accepted rules: `fixtures/generate-fixtures.ps1` builds canonical JSON templates by hand (no encoder, no library serializer), chaining `M → F → S → K`, and computes digests with the platform primitive; the generated exact bytes and digest manifest are committed under `docs/reviews/rc2b-06-b6-alt-b-slice-e-02-p1-evidence/fixtures/`. Tests hard-code byte-identical fixture strings and the digest vectors; expected values are never produced by calling the code under test. The independent reviewer can reconstruct everything from the script and the normative sections without trusting the P1 codec.

---

## 10. Exact fixture bytes and expected digests

| Fixture | SHA-256 | Bytes |
|---|---|---|
| `b2-chain/m.json` | `50d4e8cb9fc5c382c007cef7ab804dd95394849724066862eba6bd804f15eeec` | 75 |
| `b2-chain/f.json` | `05a8fb8555a389a702a49a80344c69512a000136ec74a894af0ce16e5b3cd70e` | 524 |
| `b2-chain/s.json` | `86225f4a7b3df026973fd7c9351859cb7bba1a16492653e6a61a4fba401c6226` | 468 |
| `b2-chain/k.json` | `793aa2aa8975140d05e39d334bcc5f18bf2c1c78c4b2ca1106d0d52329b487c0` | 1083 |
| `b3-chain/m.json` | `bfe1c53f2005fb9871decdf099051116e094858919840d8930b3aeccfd17079b` | 727 |
| `b3-chain/f.json` | `093ce53d520cfd73e46bf891d0ace3cb3a17cefeeefd10a54a1699be82fac919` | 2226 |
| `b3-chain/s.json` | `8896b2cd0065c26a26034a502bbcfe31a1986483dba9d6808b0717c15e3d8d33` | 571 |
| `b3-chain/k.json` | `255d017e6ec54d29442e4336d8ecfe4258e0647bb9054f044df73dba67174151` | 1147 |

All ten per-action `Ai` vectors are in `fixtures/digests.txt`. Fixture digests were re-verified with `Get-FileHash` after generation (8/8 match). The fixtures are test-only and non-authoritative.

---

## 11. Negative-control matrix

Implemented deterministic controls cover: missing/wrong-typed fields; invalid identifiers; unknown versions/enums; malformed and noncanonical CJSON-1 (20 codec categories); duplicate keys; prohibited extensions; truncated input; incorrect `Hm/Hf/Hs` and cross-chain substitution; conflicting duplicated values (with rebuilt hashes so the targeted rule is reached); M B2/B3 violations; F B2/B3 host-path/fixed-object/resource violations; S template/index/op/timeout violations; K pre/post qualification and value violations; unauthorized authority-field injection; release-template disagreement; timeout-beyond-session; oversize input; and F02 parity categories. The full mapping is [negative-control-matrix.md](rc2b-06-b6-alt-b-slice-e-02-p1-evidence/negative-control-matrix.md). No condition was invented that the normative contract permits.

---

## 12. Determinism evidence

The core is pure over byte arrays: no filesystem, process, network, environment, or static mutable state (verified by IL dependency scan). Tests assert repeated verification returns identical results and identical failure strings, and that inputs are not mutated. Byte output is exact canonical form (`Encode(parse(x)) == x` for accepted inputs). Parsing and allocation are bounded by the 65,536-byte size limit and depth 8. Failures are deterministic `InvalidDataException` paths surfaced as `(false, failure, null)`.

---

## 13. Non-authoritative output contract

Result types (`FixtureManifestContract`, `FixturePolicyContract`, `ActionScriptContract`, `CatalogContract`, `FrozenContractChain`, and the object/action records) are internal data-contract values only. They expose no approval, eligibility, invocation, capability, qualification, or execution semantics; there is no promotion method, factory, public constructor surface, or authority-shaped name (checked by test). `TryVerifyXxx` returns `false` with a failure description and a null result on any invariant failure — never a success-shaped object with warnings, never silent repair.

---

## 14. Production-reachability inspection

Debug assembly: 0 direct call/ldftn tokens from other M1 types into the E-2-P1 namespace across 4,519 method bodies; positive control proves the scanner detects intra-core calls. Compiled source-Release assembly (loaded in a collectible `AssemblyLoadContext`): 0 direct call tokens; no exported Validation type. No public API in either configuration; no forbidden filesystem/process/environment/network dependency references. Full detail and limitations: [reachability-and-release.md](rc2b-06-b6-alt-b-slice-e-02-p1-evidence/reachability-and-release.md). The scan proves direct-call absence, not general transitive unreachability.

---

## 15. E-1 consumer exclusion

The package contains no reference to `PrepublicationIntegrityProjection`, `InstalledFileSystem`, or any E-1 type; classification remains **NO E-1 PRODUCTION CONSUMER**. F-01 role-hash bindings and the fail-closed future-consumer obligation, F-02 Q1-F01–Q1-F06 and the first-consumer review, and F-03's Windows PowerShell 5.1 scope and tooling immutability are preserved. No verification tooling was modified.

---

## 16. P4-neutrality evidence

Only new internal files were added; no existing production file, protected-sink caller, admission/lifecycle path, or RELEASE behavior changed. The RELEASE verifier passes with its accepted rules (no policy change). The owner decision's **conditional P4-NEUTRAL** classification is supported at this revision by source scope, direct-call absence, and verifier results; independent P1-Q1 must confirm. Preserved: F-R1/O-R1, O-R2, O-R3, observation-to-effect residual, `NATIVE-PENDING`, and all 331 native skips (not passes).

---

## 17. RELEASE verification

`eng/VerifyProductionAssembly --production` on the Release M1 assembly: RESULT: PASS, exit 0, 0/4 hook types, 0/3 hook properties, 14/14 core types (MVID `19a06c60…`, SHA-256 `321F7131…`). The expected Debug-as-production control: RESULT: FAIL, exit 1, naming the accepted forbidden surfaces. No verifier policy was weakened; `eng/` is unchanged.

---

## 18. Executed test results

| Command | Result |
|---|---|
| Debug M1 build | succeeded, 0 warnings / 0 errors; instrumentation hook PASS |
| Debug test-project build | succeeded, 0 warnings / 0 errors |
| Dedicated filter `FullyQualifiedName~Tests.Validation` | **87 passed / 0 failed / 0 skipped** (83 E-2-P1 tests + 4 pre-existing authority tests matching the filter) |
| Full Windows M1 suite | **1,484 passed / 0 failed / 331 skipped / 1,815 total** (historical F-02 baseline 1,401 passed; delta +83 new tests) |
| Release M1 build | succeeded, 0 warnings / 0 errors |
| RELEASE verifier / Debug-as-production | PASS exit 0 / expected FAIL exit 1 |
| Fixture digest re-verification | 8/8 match |

The 331 native skips are unqualified and are not counted as native passes. Exact commands and outputs: [executed-results.txt](rc2b-06-b6-alt-b-slice-e-02-p1-evidence/executed-results.txt).

---

## 19. Known coverage limitations

1. Reachability is proven for direct-call tokens only; no general transitive analysis is claimed.
2. Depth-8 parsing is an F02-consistent bounded-parsing decision, documented in the codec analysis; the accepted schemas nest below it.
3. Codec-level `null` acceptance and schema-level unknown-field rejection are documented schema-scoped differences from the F02 codec (required by the E-2 schemas).
4. Runtime-only schema aspects (installed-role equality, current boot, approved identities, host-path pinning/cgroup and effective sandbox semantics) are intentionally out of P1 and remain downstream.
5. No protected-filesystem or native behavior is exercised; no approval, eligibility, invocation, or execution authority exists; native qualification remains `NATIVE-PENDING`.
6. This is a single-model producer record; independent P1-Q1 has not run.

No stop condition from the owner decision was triggered: no ambiguity requiring reconciliation was hit, no existing file needed modification, no public API, authority factory, production caller, E-1 consumer, protected mechanism, approval verification, or P4-relevant change appeared, and RELEASE verification passed without weakening any policy.

---

## 20. Producer disposition and recommended independent qualification

**IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION.**

Recommended next commission (not started automatically): **`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P1-Q1`**, exercising the owner decision §15 checklist: independent rule reconstruction, codec inspection, F02 comparison, original positive byte fixtures with independently computed digests, original malformed negatives, duplicate/canonicalization challenges, binding and substitution challenges, non-authoritative output inspection, unauthorized authority-construction attempts, absence of R/Wc/P approval code, Debug and RELEASE reachability, E-1 consumer absence, RELEASE isolation, P4 neutrality, and explicit native-pending limitations.

P1 owner acceptance remains a separate mandatory decision after successful Q1. E-2-P2, E-3 through E-7, Slice N, catalog approvals, positive validation, and protected execution remain unauthorized.
