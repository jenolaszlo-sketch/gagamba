# ALT-B Slice E-2 P2 — independent qualification (Q1)

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-Q1`
Date: 2026-11-11 (Asia/Manila). Reviewer: independent qualification context
(DeepSeek Flash Max), separate from the producer session. Scope: independent
qualification and documentation only.

Reviewed implementation: **`9b80969ce766b6d8939775710aa77d080c3ff674`**
(tree `fc77bc1d673f787adae2d332141f1dd5f854e883`).

**Formal disposition (stated once, §21): SUPPORTED WITH QUALIFICATIONS.**

Evidence package:
[rc2b-06-b6-alt-b-slice-e-02-p2-q1-evidence/](rc2b-06-b6-alt-b-slice-e-02-p2-q1-evidence/README.md).

---

## 1. Commission and method

The reviewer independently qualified the P2 implementation against its binding
owner authorization (`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-ENTRY-OWNER-01`)
and the adopted frozen contracts, using only reviewer-owned executable probes
in external scratch, reviewer-constructed fixtures, independent rebuilds, and
independent IL scans. Producer tests were re-executed but not relied on as the
evidence basis; producer fixtures were hashed but never used as reviewer
inputs. No production or test source file was modified. The central acceptance
requirement — **structural verification must never become operational approval
verification** — was tested with a fully canonical, correctly bound,
operationally untrusted chain.

## 2. Verified baseline

Branch `main`; exactly one canonical worktree; tracked/index clean; HEAD/tree
exactly as expected; 44 historical untracked paths present with matching
SHA-256 (`diff_count=0`) against the canonical manifest; recorded
`origin/main` `53850aa…` with local `main` 27 commits ahead; no fetch/push/
reset/clean/merge/rebase/prune/stash. Ancestry verified (exit 0) for the P2
implementation, P2 entry owner decision, P2 entry assessment, P1 acceptance and
Q1, P1 implementation, E-2 entry owner-02, reassessment, D1, P0 acceptance,
E-2 reconciliation, P4 acceptance, and E-1 acceptance. Full values:
[baseline-and-revision.md](rc2b-06-b6-alt-b-slice-e-02-p2-q1-evidence/baseline-and-revision.md).

## 3. Implementation identity

| Object | Blob |
|---|---|
| `Validation/DetachedEvidenceVerifier.cs` | `2b482a7068b3eae4603a93d79454e9c69ce7b6d3` |
| `Validation/DetachedEvidenceContracts.cs` | `652486d4ab26fec6a975e876e59a473b66bc331c` |

Both match the producer report. The reviewed commit adds exactly the two
authorized production files and four authorized test files; every other
addition is report/evidence; only `docs/PROJECT-STATUS.md`, `docs/handoff.md`
and `docs/work-queue.md` were modified. No existing production, test, tooling,
project, solution, package, or frozen artifact changed.

## 4. Independent normative reconstruction

Reconstructed in full from the adopted P0 record §6 (CJSON-1), §11.1–11.3
(R/Wc/P), §11.5 (acyclic graph) and the P2 owner decision §§6–16, before
consulting implementation behavior (details:
[schema-reconstruction.md](rc2b-06-b6-alt-b-slice-e-02-p2-q1-evidence/schema-reconstruction.md)).

- **R** — 13 fields; `conclusion = supported-for-bounded-validation`; `Hk`
  reference; ten equal-to-K values; qualification pair inherited from K
  (null pre / exact pair post); no reviewer identity, signature, or timestamp;
  `Hr = SHA256(CJSON1(R))`.
- **Wc** — 11 fields; `decision =
  accept-exact-bounded-validation-catalog`; `Hk` and `Hr` references; seven
  equal-to-K fields (the "eight duplicated" label counts `catalogSha256` with
  them); no owner identity, signature, timestamp, or P hash;
  `Hw = SHA256(CJSON1(Wc))`.
- **P** — 12 fields; `accepted = true`; `Hk`/`Hr`/`Hw` references; seven
  equal-to-K fields (same counting convention); **no self-hash**; no
  administrator identity, signature, nonce, or timestamp;
  `Hp = SHA256(CJSON1(P))`.

Every producer claim was checked against this reconstruction. The complete
enumerated check set was verified by code inspection and by hash-consistent
mutation: R (Hk + 10 equalities), Wc (Hk + Hr + 7 equalities), P (Hk + Hr + Hw
+ 7 equalities). No rule was invented by the implementation, and no accepted
rule was omitted. Clarifications recorded: the "duplicated value" count words
(§[findings](rc2b-06-b6-alt-b-slice-e-02-p2-q1-evidence/findings-and-qualifications.md)
Q1-F05) and the absence of an accepted topology↔phase cross-rule (Q1-F06).

## 5. CJSON-1 and canonical-byte verification

The accepted P1 codec is reused unchanged (blob-verified). Reviewer-owned
vectors (21 malformed controls) confirm: ASCII-only with no BOM/whitespace;
exactly one object; strictly ascending keys with duplicates rejected; escapes
must be literal-form (round-trip equality); integers must be canonical;
empty strings refused; size limit enforced; trailing/truncated/oversize input
refused; **hashes are computed over the exact received bytes with no
reserialization**, and no malformed input can succeed through re-encoding (a
noncanonical form that parses still fails the byte-identity round trip). The
depth-8 codec bound remains the accepted P1-Q1-F01 implementation constraint;
no R/Wc/P record approaches it (constructed records are 534–777 bytes, depth 2)
and it is not treated as a CJSON-1 rule.

## 6. Reviewer-owned positive fixtures and digests

Two complete chains built by the reviewer directly from the schema (never via
the P2 encoder, verifier, or producer fixtures):

| Chain | Hk | Hr | Hw | Hp |
|---|---|---|---|---|
| B2/pre | `ee039fad…` | `18a2a09c…` | `3de6ed37…` | `3c425e51…` |
| B3/post | `4ae4a492…` | `aa1e4311…` | `5fade074…` | `3829599a…` |

Exact bytes (8 files), lengths, full digests, and labels are committed under
[reviewer-fixtures/](rc2b-06-b6-alt-b-slice-e-02-p2-q1-evidence/reviewer-fixtures/README.md);
the reviewer computed every digest independently with the platform SHA-256
primitive, and the P2 chain entry returned exactly those values (`POS-01..05`).
The producer's six fixture files independently hash to their recorded digests
(6/6), and the producer counts were reproduced exactly.

## 7. Binding, substitution, and semantic-invalidity results

- **Hash-consistent semantic invalidity (primary adversarial objective):** 23
  reviewer controls. Each starts from valid canonical content, edits one field
  or cross-record relationship, correctly recomputes **all** affected digests,
  preserves canonical representation, and is refused by the *intended* named
  invariant (`R/Wc/P <field> disagreement`, or the literal rules `R
  conclusion`, `Wc decision`, `P accepted is not true`) — never by a stale
  hash. All ten equal-to-K categories are exercised at least once, across all
  three records.
- **Identity/substitution:** R/Wc/P each bound to a different K; Wc bound to a
  different R; P bound to a different R and a different Wc; cross-case
  substitution of otherwise valid records; substituted P across chains; a
  modified record with stale downstream references (the record's own rule
  fires first — deterministic vanguard ordering); and an incorrect external
  expected Hp (content-identity comparison only; the verifier has no
  expected-digest input). Every case refused with the exact first failing
  invariant (`SUB-01..11`).
- **No digest is ever trusted because the input asserts it:** every reference
  is compared against a value recomputed from the exact received bytes.

## 8. Structurally valid but operationally untrusted evidence (authority boundary)

A fully canonical, correctly bound K/R/Wc/P chain (both B2/pre and B3/post)
with the exact literals and `accepted=true` verifies structurally — **and P2
establishes nothing about** reviewer/owner/administrator existence or
independence, a real review or acceptance, protected acquisition or custody,
catalog approval or eligibility, invocation authorization, positive
validation, or protected execution (`AUTH-01`, `AUTH-04`). The `accepted=true`
literal is verified but deliberately never projected: the P content contract
exposes exactly the 11 accepted content properties, with **no `Accepted` or
equivalent authority flag** (`AUTH-02`). A valid untrusted chain is neither
rejected for missing issuer authenticity (explicitly outside P2) nor promoted
into a trusted approval.

## 9. Authority-construction audit

The five P2 types (verifier + four result records) contain no R/Wc/P issuance,
V/W/A actor, reviewer/administrator designation, protected-root provisioning,
approval store, trusted acquisition, approval factory, authority promotion,
invocation permit, positive-validation factory, protected-execution call, or
public authority-bearing API. Verified by: declared-member enumeration (141
members; no forbidden name token; `AUTH-03`/`AUTH-06`), signature-type scan
(`REACH-*-06`), compiled call-token resolution (`P1R-01`), and the exact result
property surface (`AUTH-02`). `internal` visibility was not used as the
argument. Reviewer/owner/administrator identity, timestamp, nonce, signature,
and approval-claim injections placed at canonical ascending positions are
refused as exact-field-set violations (`AUTH-05a–h`).

## 10. Approved P1 reuse

Compiled call tokens show the P2 surface calls exactly: `CanonicalJson.Parse/
Hash/IsLowerHex`, `FrozenSchemaRules.IsValidId`, `FrozenContractVerifier.
TryVerifyCatalog`, plus reads of the returned `CatalogContract` values that are
compared (10 getters). No other P1 interface, no `FinalAcceptanceCodec`, no F02
store, no authority or E-1 type. The four P1 files match their P1-accepted
blobs; `FinalAcceptance.cs` has no commit after its F02 implementation
(`831c980`); `eng/**` has no commit after `52037fa` (F-03). P1's structural and
non-authoritative contracts are preserved; no F02 approval or trust-store
authority is reused.

## 11. Fail-closed and robustness findings

23 malformed-byte vectors, wrong types/versions/literals, identifier and enum
violations, missing/extension fields, duplicate keys, truncation, cascade/
conflict cases: every refusal returns `false`, a null result contract, and one
exact deterministic diagnostic, with no escaped exception. Inputs are not
mutated on success or refusal; repeated calls are deterministic with stable
digests; no partial validation success; no hidden mutable state; no
filesystem, environment, process, or network dependency; no input-driven
reserialization. One bounded observation (Q1-F02): a `null` `byte[]` argument
escapes as `NullReferenceException`; null is outside the accepted byte-array
domain, the behavior is identical to the accepted P1 entry, and there is no
production path to it. No defect was demonstrated.

## 12. Production reachability (Debug and compiled RELEASE)

Reviewer IL scans over every method body of every non-Validation type in both
the Debug and the freshly built Production-mode Release assemblies: **zero**
direct-call tokens to any of the 154 P2 member tokens; **zero** type-token or
`ldtoken` references to the P2 types; **zero** string literals naming
`DetachedEvidence`; no exported Validation type; no production signature
consuming a P2 result; scanner positive control detected a known intra-core
call. Producer reachability tests reproduced within 33/33 and the full suite.
**Retained scope limitation (P1-Q1-F05 lineage): zero direct-call tokens do not
constitute a complete transitive or reflection-aware unreachability proof; no
stronger claim is made.**

## 13. E-1 consumer exclusion and F02 separation

No E-1 production consumer: the P2 core references no E-1/`Installed`/
`Launch`/`Lifecycle`/`Restricted`/`Execution`/`Bootstrap`/`Native`/
`Preparation`/`Recovery` type; no E-1 file changed; no production path reaches
P2. F02 remains separate: `FinalAcceptanceCodec` unchanged, no reference to the
qualification store or the final-admission chain, no shared authority, no new
trust anchor.

## 14. RELEASE verification

Unchanged `eng/VerifyProductionAssembly`, applied to independently rebuilt
assemblies: RELEASE production **RESULT: PASS, exit 0** (0/4 hook types, 0/3
hook properties, 14/14 core types); Debug-as-production **RESULT: FAIL, exit
1** with the unchanged forbidden lists. No verifier rule weakened; no new
`ForTest` seam; no new public authority API; no protected acquisition or
approval path. Builds are non-bit-reproducible against the producer hashes
(retained Q1-F04-lineage qualification); source blobs are the identity anchor.

## 15. P4 classification

**P4-NEUTRAL** (Q1-confirmed for the reviewed revision, under the P2 owner
decision §22 conditional classification). Only the two authorized internal
files were added; no protected-sink caller, F01/F02 enforcement, S10/S11
domination, S28 isolation, first protected effect, qualified-to-legacy
downgrade refusal, root authorization, E-1 reachability, or RELEASE isolation
file changed; no production authority path consumes P2. Retained without
change: F-R1/O-R1, O-R2, O-R3, the accepted observation-to-effect residual,
`NATIVE-PENDING`, and all 331 native skips. The classification does not extend
to any future consumer or downstream milestone.

## 16. Independent test/build/regression results

| Check | Reviewer result |
|---|---|
| Debug M1 build | succeeded, 0 warnings / 0 errors |
| Test project build | succeeded, 0 warnings / 0 errors |
| Release M1 build | succeeded, 0 warnings / 0 errors, production mode PASS |
| P2 filter (`Validation.DetachedEvidence`) | **33 / 0 / 0** (33 total) |
| Validation filter | **120 / 0 / 0** (120 total) |
| Full Windows M1 | **1,517 passed / 0 failed / 331 skipped / 1,848**, exit 0 |
| RELEASE verifier | PASS, exit 0 |
| Debug-as-production | FAIL, exit 1 (expected) |
| Reviewer probe | **97 / 97 checks, 0 failures**, exit 0 |

The 331 native skips are unqualified and are not passes; no native positive
qualification is claimed. Producer-reported baselines are all reproduced
exactly except rebuild binary hashes (accepted). Raw logs:
[executed-results.md](rc2b-06-b6-alt-b-slice-e-02-p2-q1-evidence/executed-results.md).

## 17. Findings and qualifications

No normative schema defect, no canonicalization defect, no incorrect content
identity, no broken binding, no incomplete semantic check, no fail-closed
defect, no authority-boundary violation, no production reachability
regression, no RELEASE regression, and no P4 regression was demonstrated.

Retained/recorded (full text:
[findings-and-qualifications.md](rc2b-06-b6-alt-b-slice-e-02-p2-q1-evidence/findings-and-qualifications.md)):

- **Q1-F01 (bounded, retained):** direct-call/type-token/string reachability
  evidence only; not a complete transitive or reflection-aware proof.
- **Q1-F02 (bounded observation):** null `byte[]` argument escapes the
  byte-array domain refusal contract; identical to the accepted P1 behavior; no
  production path; no remediation during Q1.
- **Q1-F03 (retained):** depth-8 codec constraint as an implementation bound,
  far above every R/Wc/P record; not a CJSON-1 rule.
- **Q1-F04 (retained):** rebuilds are not bit-reproducible; accepted lineage.
- **Q1-F05, Q1-F06 (INFO):** "duplicated value" counting convention; absence
  of an accepted topology↔phase cross-rule (no invented rule).
- Retained prior qualifications: P0, D1, all seven P1 findings, E-1/F-01/F-02/
  F-03 obligations, P4 refusal-only qualifications, composite Q-E1 open items,
  `NATIVE-PENDING`, and the 331 native skips.
- **No operational R/Wc/P approval exists or is established.** Structural
  success of a supplied chain is not catalog approval, eligibility, capability,
  or protected-execution authority.

## 18. Formal Q1 disposition

**SUPPORTED WITH QUALIFICATIONS.**

The required P2 contracts are independently verified: exact R/Wc/P schema
structure and content, canonical-byte enforcement over exact received bytes,
correct Hk/Hr/Hw/Hp identities, every accepted binding and duplicated-value
relationship, deterministic refusal of hash-consistent semantic violations,
non-authoritative result semantics with no authority projection, production
unreachability within the demonstrated (direct-call/type-token/string) scope,
RELEASE compatibility, E-1/F02 separation, and P4 neutrality. The
qualifications are the explicitly bounded limitations above (reachability
scope, null-argument observation, depth-8 codec bound, non-bit-reproducible
rebuilds) plus the preserved prior qualifications; none compromises a required
invariant of the authorized scope.

## 19. Recommended next commission

**`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-OWNER-ACCEPTANCE-01`** — the owner
must separately determine whether P2 is formally accepted at this exact
revision and rule on the retained Q1 qualifications. Not started automatically.
E-3 through E-7, Slice N, any production consumer, catalog approval, protected
roots, live invocation, positive validation, and protected execution remain
unauthorized.

## 20. Preservation and stop

One canonical worktree; branch `main`; the 44 historical untracked paths remain
hash-identical; no fetch/pull/push, no branch or additional worktree, no reset/
clean/merge/rebase/prune/stash/history rewrite. This commission committed only
this report, the reviewer evidence package, and the necessary current-state
documentation blocks. No remediation, owner acceptance, downstream
implementation, operational approval, positive validation, activation,
synchronization, or push occurred.
