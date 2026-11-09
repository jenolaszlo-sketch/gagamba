# ALT-B Slice E-2 P1-Q1 — independent qualification

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P1-Q1`

Date: 2026-11-09 (Asia/Manila). Reviewer: DeepSeek. Scope: independent qualification, reviewer-owned probes in external scratch, and evidence documentation on canonical `main`; no push. This is a single-reviewer independent review, not multi-model certification and not owner acceptance.

Reviewed implementation: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P1`, producer disposition `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.

Formal Q1 disposition: stated once in §19. Evidence package: [rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/README.md).

---

## 1. Verified starting baseline

| Item | Verified value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| HEAD / tree | `06006a7035ca3a7615189b867584b1b3955eeb7a` / `89330a5cf779ec35bb208e6bf9ec67bf0c12e87a` |
| Worktrees | exactly one canonical checkout |
| Tracked / index | clean |
| Historical untracked paths | 44, all present, per-file SHA-256 match, `diff_count=0` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (recorded only; not synchronized) |
| Local `main` position | 22 commits ahead; no push |
| P0 / D1 / entry-owner-02 blobs | `db25949d…` / `83fe8d26…` / `f5ab96c6…` (unchanged) |

Ancestry verified (`git merge-base --is-ancestor`, exit 0) for P1, entry-owner-02, reassessment, D1, P0 acceptance, E-2 entry decision, P4, E-1, F-01, F-02, and F-03 commits. No reset, clean, merge, rebase, prune, stash, history rewrite, branch, or additional worktree was used. All reviewer probes ran from external scratch (`%TEMP%\opencode\p1q1-probe`); no canonical file was modified.

## 2. Exact implementation revision reviewed

Source blobs: `CanonicalJson.cs` `276581c4…`; `FrozenSchemaRules.cs` `868ddbee…`; `FrozenContracts.cs` `3344069c…`; `FrozenContractVerifier.cs` `fdff96b9…`. Tests: six files under `tests/Gagamba.Execution.Linux.M1.Tests/Validation/`. The producer report blob is `8dfe6b8c…`. Reviewer rebuilds: Debug M1 (rebuilt) and Release M1 MVID `ec05b2be…` / SHA-256 `5B649DAE…`; the producer's Release was `19a06c60…` / `321F7131…`. The difference is the accepted non-bit-reproducible-rebuild qualification (P4 coverage acceptance §5; P4-03 Q1-F02); source blobs are the identity anchor and both builds pass the unchanged verifier. Full detail: [baseline-and-revision.md](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/baseline-and-revision.md).

## 3. Normative CJSON-1 reconstruction

The reviewer reconstructed each accepted rule from the adopted P0 record §6 (source: `final-acceptance-hash-contract.md` `49d566e1…`) before looking at behavior, then compared rule-by-rule with `CanonicalJson` and with independent byte vectors. Result: **no divergence from the accepted canonical-byte contract**. The full rule→implementation→control table is [cjson-reconstruction.md](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/cjson-reconstruction.md).

Limit analysis:

- **Size 65,536 bytes:** explicit accepted rule (P0 §6 rule 10); boundary independently reproduced (65,536 accepted; 65,537 rejected `CJSON-1 size`).
- **Depth 8:** **not** stated in the accepted contract. Measured behavior: root + 7 nested containers accepted, 8+ rejected (`invalid CJSON-1`), identical to the accepted F02 codec's `MaxDepth = 8`. The deepest accepted E-2 shape (F `sandbox.writableStores[]` objects) is depth 4. Classification: **bounded implementation constraint** (resource safety, F02-consistent), retained as a qualification; no accepted artifact shape is affected, and a future need for deeper nesting would require reconciliation rather than silent change.

## 4. Independent canonical-byte vectors

Thirty-two reviewer-constructed byte vectors plus explicit size-boundary probes and a 6–9 level depth boundary sweep were executed against `CanonicalJson` by reflection (no producer fixtures, no P1 encoder used to construct expected results). Highlights:

- Accepted as expected: minimal object, multi-key canonical, printable-character strings, `\"`/`\\` escapes, `0`/`2^63-1` integers, `null` value, arrays/nested objects, depth 3, 65,536-byte maximum.
- Rejected as expected: wrong key order and duplicates (`not strictly ascending`), empty string (`CJSON-1 string`), raw control bytes / non-ASCII / BOM / trailing newline (`ASCII printable`), leading/trailing space (`noncanonical CJSON-1 representation`), trailing data and truncation (`invalid CJSON-1`), leading zero, negative, decimal, exponent, int64 overflow (`unsigned canonical integer`), root array (`root object`), `\u` and `\/` escapes (`noncanonical`), empty input and oversize (`CJSON-1 size`).

All 34 vector checks passed; raw results in [reviewer-vectors.txt](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/reviewer-vectors.txt).

## 5. Encoder/decoder audit

- `Encode` validates the value tree before serializing and produces no repaired output; the verifier never uses `Encode` on untrusted input (only `ActionHash` over already-validated elements and tests).
- Round trip: for every accepted vector, `Encode(Parse(bytes))` byte-equals the input; rejection never silently normalizes.
- Determinism: repeated parse/verify calls return identical results, hashes, and failure strings; inputs were verified unmutated.
- No hidden mutable state: the production files contain only `static readonly` arrays and pure functions; independent IL inspection found no I/O, process, environment, network, or interop dependencies.
- Refusal paths are bounded: the mutation/truncation corpus produced no exception escapes through the `Try*` surface and no success-shaped result for invalid input.

## 6. Independent positive fixtures and digests

Three reviewer-owned chains were built from hand-written canonical templates (string composition only) — B2/pre/no-release, B3/no-release/pre, and B3/release/post — with digests computed by the platform SHA-256 primitive and re-verified by the probe; the P1 encoder was never the source of expected bytes. All three verified, and the verifier returned exactly the independently computed `Hm/Hf/Hs/Hk` values. Digests and lengths: [independent-chains.md](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/independent-chains.md).

Adversarial builder validation: an early reviewer-builder defect that failed to propagate recomputed hashes was detected by the chain verifier (`S fixturePolicySha256 does not bind F`) — confirming the binding checks operate on reviewer bytes rather than only on producer fixtures.

## 7. M/F/S/K schema assessment

The reviewer audited each verifier against P0 §§7–10 and exercised schema-specific controls:

- **M:** exact key set; `ValidId` case id; topology enum; B2 empty objects; B3 exactly four ordered roles with exact `path`/`mode`/`destination` and length bounds; aggregate ≤ 16,777,216 (rehash-consistent overflow refused); object extensions refused.
- **F:** exact 19-key set; policyKind↔topology; B2 hard-false/null fields; B3 exact `hostPaths` (path/device/inode/mode and uid/gid equality), enrollment `ValidId`s, fixed `copy`/`invocation`/`sandbox`/`resources`; canonical UUID `bootId` (uppercase refused); uid/gid bounds; `expiresLeadNs` ≤ session.
- **S:** exact key set; exactly one accepted template per topology; contiguous indexes; op literals; timeout bounds; no extensions; per-action `Ai` computed over the accepted domain string, 0x00 separator, and canonical action bytes.
- **K:** exact 22-key set; pre null pair vs post `ValidId`+hex qualification pair; 64/40-hex fields; text bounds; version; no review/owner/receipt/self-hash/approval fields.

Runtime-only rules were verified as intentionally deferred, not defective: installed coordinator/filter role equality, current-boot equality, "approved" uid/gid provisioning, host-path ancestry/pinning and cgroup semantics, effective sandbox behavior, and protected-store interactions. The owner decision assigns these outside P1.

## 8. Self-consistent semantic-invalidity tests

Nine hash-consistent adversarial variants (every affected digest recomputed) were rejected by the intended semantic rule, never by a hash mismatch: case-id, phase, topology, session-limit, timeout-beyond-session, release-template, authority-field injection, unsupported K version, and non-canonical `bootId`, plus M aggregate overflow and M object extension. Full table: [semantic-and-substitution-controls.md](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/semantic-and-substitution-controls.md).

## 9. Identity-substitution tests

Five substitution controls were detected at the correct binding: M replaced (F binding), S replaced (S binding), S changed with K stale (K `actionScriptSha256`), cross-chain K (K manifest binding), and F self-asserting a wrong `Hm` while downstream artifacts were rehashed (recomputed `Hm` wins). No identity is trusted from input metadata; every comparison recomputes the digest from received bytes after the canonical equality proof. A mutated-but-valid K byte is correctly treated as a new catalog object (different `Hk`), which is content addressing, not a bypass.

## 10. F02 compatibility matrix

Canonical record/selector accepted by both; five rejection categories (trailing space, BOM, duplicate field, `\u` escape, float version) rejected by both; `Hash` and `IsLowerHex` outputs equal; depth and empty-string behavior aligned. Both producer-claimed schema-scoped differences were independently confirmed: `null` (E-2 schemas require it; F02's schema forbids it) and unknown fields (accepted by the schema-free codec, rejected by every E-2 schema). No **NORMATIVE CJSON-1 DIVERGENCE** was found. `FinalAcceptanceCodec` was not modified and no shared-core refactor was attempted. Matrix: [f02-compatibility.md](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/f02-compatibility.md).

---

## 11. Failure-handling and resource-safety assessment

The reviewer audited the failure paths and ran a mutation/truncation corpus: 400 deterministic single-byte mutations (no exception escapes, no silent acceptances, all M/F/S mutations refused at their bindings), 11 K truncations, a 65,537-byte input, and deep-nesting probes. No unhandled malformed-input exception, partial-parse success, input mutation, reused state, integer overflow path, excessive allocation, unbounded nesting, incomplete UTF-8 acceptance, or hash-over-wrong-bytes defect was found. `Try*` wrappers catch exactly `InvalidDataException`; direct `Parse`/`Encode` throw by design. Detail: [failure-and-resource.md](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/failure-and-resource.md).

## 12. Non-authoritative output review

The five contract value types are internal data carriers with no approval, eligibility, invocation, capability, qualification, or execution semantics. The reviewer confirmed: no authority-shaped type or member names; no public or nested-public type; no parameter/return type referencing `OwnerAuthorityContext`, `LoadedInstallation`, `Grant`, or `Approval`; no R/Wc/P or approval records; no promotion method; `Try*` returns `false` + failure + null on any invariant failure. Internal visibility was not treated as sufficient on its own — compiled caller relationships were scanned independently (§13).

## 13. Production-reachability assessment

The reviewer implemented an independent IL operand scanner and scanned every method/constructor body of both configurations. Debug: 4,253 bodies scanned, 266 P1 targets, **0 direct caller hits**; reviewer-rebuilt Release: 4,100 bodies, **0 hits**; the producer-built Release was also scanned before rebuild with 0 hits. Scanner positive control detected the real intra-core call. No exported Validation type; zero forbidden dependency references. Scope: direct-call tokens only; transitive unreachability and hostile in-process reflection are not claimed (outside the accepted threat model). Detail: [reachability-release-p4.md](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/reachability-release-p4.md).

## 14. RELEASE verification

Reviewer-executed on the rebuilt Release assembly: `eng/VerifyProductionAssembly --production` → **RESULT: PASS**, exit 0, 0/4 hook types, 0/3 hook properties, 14/14 core types; Debug-as-production → **RESULT: FAIL**, exit 1 (expected). No verifier policy was modified and `eng/` is unchanged.

## 15. P4 impact

**P4-NEUTRAL** (independent classification). The complete implementation diff adds only the four authorized production files and six test files; no existing production source, test, tooling, project, or frozen artifact changed. No protected-sink caller, admission path, authority gate, first protected effect, qualified-to-legacy downgrade path, E-1 reachability, or RELEASE behavior changed. The full suite is green and the verifier passes. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING`, and all 331 native skips are preserved; skips are not passes.

## 16. Executed test results

| Action | Result |
|---|---|
| Reviewer probe (91 checks) | 91 passed / 0 failed |
| Debug M1 rebuild | 0 warnings / 0 errors |
| Release M1 rebuild | 0 warnings / 0 errors |
| Dedicated `Tests.Validation` filter | 87 passed / 0 failed / 0 skipped |
| Full Windows M1 suite | 1,484 passed / 0 failed / 331 skipped / 1,815 |
| RELEASE verifier / Debug-as-production | PASS exit 0 / expected FAIL exit 1 |

All producer counts were independently reproduced. Exact commands: [executed-results.txt](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/executed-results.txt).

## 17. Reviewer-owned negative-control matrix (summary)

Minimum categories required by the commission, all satisfied with reviewer-constructed inputs: eight original CJSON-1 malformed-byte controls (raw control, BOM, non-ASCII, escapes, numbers, duplicates, truncation, oversize); two original positive canonical vectors; three reviewer-owned positive chains; eleven rehashed semantic-invalidity controls; five identity-substitution controls; one authority-field injection (`"approved":true` into K, refused `K field set`); one unsupported schema/version control; one production-reachability and authority-construction control (independent scanner + surface scan, zero hits). Full per-check matrix with expected/actual: [reviewer-vectors.txt](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/reviewer-vectors.txt), [semantic-and-substitution-controls.md](rc2b-06-b6-alt-b-slice-e-02-p1-q1-evidence/semantic-and-substitution-controls.md).

## 18. Findings and qualifications

No normative implementation defect, schema-enforcement defect, incorrect binding, incomplete semantic validation, incorrect canonicalization, F02 divergence, authority-boundary violation, production-reachability regression, P4 regression, or RELEASE regression was found.

| ID | Severity | Finding | Classification | Impact |
|---|---|---|---|---|
| Q1-F01 | LOW / INFO | Depth bound 8 (root + 7 containers) is an implementation constraint not stated in P0 §6; it matches the accepted F02 codec and is far above every accepted E-2 artifact depth (≤ 4). | Bounded implementation limitation | Non-blocking; retained; reconciliation required only if a future schema needed deeper nesting |
| Q1-F02 | INFO | Codec-level empty-string rejection: consistent with F02 and with every E-2 schema; no field declares an empty-string permission. | Bounded implementation limitation | Non-blocking; retained interpretation |
| Q1-F03 | INFO | `null` and unknown-field differences from F02 are schema-scoped, not canonicalization divergences. | Confirmed producer claim | Non-blocking |
| Q1-F04 | INFO | Rebuilt binaries are not bit-reproducible (producer `321F7131…` vs reviewer `5B649DAE…`); both pass the unchanged verifier. | Accepted carried qualification (P4-03 Q1-F02) | Non-blocking; source blobs are the identity anchor |
| Q1-F05 | INFO | Reachability proof covers direct-call tokens only; reflection/dynamic invocation by hostile in-process code is outside the accepted threat model (E-1 precedent). | Evidence-scope limitation | Non-blocking; retained obligation for any future consumer |
| Q1-F06 | INFO | Runtime-only schema aspects (installed-role equality, current boot, approved identities, host pinning, sandbox behavior) are intentionally deferred. | Deferred downstream obligation | Non-blocking; owned by later milestones and Slice N |
| Q1-F07 | INFO | `IsAbsolutePosixPath` rejects `/` (no components); consistent with the accepted "no empty component / no trailing slash" wording; no accepted artifact uses `/`. | Observation | Non-blocking |

The 331 native skips remain unqualified; no protected native behavior is claimed.

## 19. Formal Q1 disposition

**SUPPORTED WITH QUALIFICATIONS.**

The implementation faithfully satisfies the authorized E-2-P1 scope: the accepted CJSON-1 canonical-byte contract, M/F/S/K structural and binding requirements, `Hm/Hf/Hs/Hk` identities, deterministic fail-closed rejection, and the non-authoritative, production-unreachable boundary are all independently verified with reviewer-owned vectors, chains, semantic-invalidity controls, substitution controls, and compiled reachability scans. The retained qualifications (Q1-F01 through Q1-F07) are bounded evidence limits and deferred downstream obligations that do not compromise any required P1 invariant. No remediation is required within the authorized scope.

Governance preserved: P0 accepted with qualifications; D1 adopted with qualifications (V/W/A separation, separate F02 authority, unprovisioned roots, deferred encodings, no delegation, no standing approval, no cross-host reuse, native pending); E-1 accepted with qualifications and production-unreachable; F-01/F-02/F-03 closed with qualifications; E-2 entry authorized with qualifications for P1 only; P4 refusal-only acceptance and `NATIVE-PENDING` preserved; no positive validation or protected execution is authorized.

## 20. Recommended next commission

**`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P1-OWNER-ACCEPTANCE-01`** — the formal owner acceptance decision for P1, deciding whether to accept the implementation at the reviewed revision with the retained qualifications, and explicitly leaving E-2-P2 and all later milestones unauthorized.

No remediation commission is recommended; no blocking defect exists. Neither the owner-acceptance commission nor any later work starts automatically. This Q1 review stops here: it does not remediate production code, issue owner acceptance, begin E-2-P2, construct validation authority, activate protected execution, synchronize, or push.


