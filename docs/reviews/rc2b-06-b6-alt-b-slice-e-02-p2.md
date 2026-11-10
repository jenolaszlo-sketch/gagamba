# ALT-B Slice E-2 P2 — detached R/Wc/P structural and content-binding verification

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2`

Date: 2026-11-09/10 (Asia/Manila). Producer: DeepSeek. Scope: internal,
unprivileged, production-unreachable structural and content-binding verification
of supplied R/Wc/P bytes against a supplied K catalog. **Content-chain
verification is not operational approval verification** and nothing here
establishes reviewer, owner, or administrator authority, protected acquisition or
custody, catalog eligibility, positive validation, or protected execution.

Evidence package: [rc2b-06-b6-alt-b-slice-e-02-p2-evidence/](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/README.md).

## 1. Verified starting baseline

- Branch `main`, exactly one canonical worktree, tracked/index clean;
  HEAD `dfa570fcfa4b55176d6cc63ad1d8c8930d4a58eb`, tree
  `e90ec80e6b9ed7ab180ae0572841b79fea440a6d`.
- 44 historical untracked paths present with matching SHA-256 (diff=0).
- Recorded `origin/main` `53850aa158cf26f5a40fb9bd51c606a111188ef3`; local main
  26 ahead; no fetch/pull/push; ancestry verified for P2 entry owner decision,
  P2 entry assessment, P1 acceptance/Q1/implementation, E-2 entry, D1, P0, P4,
  E-1, F-01/F-02/F-03 (all `git merge-base --is-ancestor` exit 0).
- Binding identities: P2 owner decision blob `893b48a2…`, assessment
  `745dbc8c…`, P1 acceptance `f360932a…`, P1 verifier blob `fdff96b9…`, P0
  `db25949d…`, D1 `83fe8d26…`.

## 2. Binding owner authorization

`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-ENTRY-OWNER-01`,
**P2 ENTRY AUTHORIZED WITH QUALIFICATIONS — P2 ONLY** (owner decision §25).
Authorized: R/Wc/P structural and content-binding verification (owner §§6–9),
P1 reuse read-only (§13), one bounded package (§14), additive surfaces (§15),
non-authoritative output (§16), positive fixtures (§17), negative controls
(§18), production-unreachability evidence (§20), no E-1 consumer (§21), RELEASE
and conditional P4 conditions (§22). Excluded: everything in §§10–12 and §24.

## 3. Exact implementation files

New production files (only under the authorized directory):

| File | Blob |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Validation/DetachedEvidenceVerifier.cs` | `2b482a70…` |
| `src/Gagamba.Execution.Linux.M1/Validation/DetachedEvidenceContracts.cs` | `652486d4…` |

New test files (only under the authorized directory):

| File | Blob |
|---|---|
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/DetachedEvidenceFixtures.cs` | `72d5fdc5…` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/DetachedEvidenceTests.cs` | `ce058ede…` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/DetachedEvidenceNegativeTests.cs` | `600c40f2…` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Validation/DetachedEvidenceReachabilityTests.cs` | `f65e60c4…` |

No existing production, test, tooling, project, solution, package, or frozen
artifact file was modified. The only tracked-file changes are the current-state
updates in `docs/PROJECT-STATUS.md`, `docs/handoff.md`, `docs/work-queue.md`,
plus this report and the new evidence package.

## 4. Normative contract matrix

Pre-implementation reconstruction is recorded in
[contract-matrix.md](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/contract-matrix.md),
with per-attribute origin: adopted P0 record §§6, 11.1–11.3 and the accepted
external schema extractions; D1 §§5–7/9; P2 owner decision §§6–16. No field,
version, signature, timestamp, nonce, or authority metadata was invented.

## 5. R schema implementation

Exactly the 13 accepted fields; `conclusion = supported-for-bounded-validation`;
`catalogSha256 = Hk`; ten duplicated K values; schemaVersion 1; S2 identifier
grammar for `caseId`; pre/post qualification-pair rule; lower-case 64-hex
hashes; no reviewer identity, timestamp, or owner hash. `Hr` is recomputed from
the exact received bytes; a claimed identity is never trusted. Implementation and
diagnostic table: evidence
[schema-and-identity.md §1](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/schema-and-identity.md).

## 6. Wc schema implementation

Exactly the 11 accepted fields; `decision =
accept-exact-bounded-validation-catalog`; `catalogSha256 = Hk`;
`catalogReviewSha256 = Hr` (with K and R re-verified first); eight duplicated
values; no owner identity, signature, timestamp, or P hash;
`schemaVersion = 1`. Evidence §2.

## 7. P-content schema implementation

Exactly the 12 accepted fields; `accepted = true` (boolean) as a content
precondition only; `catalogSha256 = Hk`, `catalogReviewSha256 = Hr`,
`ownerCatalogAcceptanceSha256 = Hw` (with K, R, and Wc re-verified first);
eight duplicated values; **no self-hash field**; no administrator identity,
signature, nonce, or timestamp. No protected-root access, administrator
verification, custody, issuance, publication, durability, withdrawal, pre-B1
authorization, challenge/response, or invocation permit exists. Evidence §3.

## 8. Hk/Hr/Hw/Hp identity verification

- `Hk` from exact K bytes after accepted P1 `TryVerifyCatalog`; `Hr`/`Hw`/`Hp`
  from exact received canonical bytes via accepted `CanonicalJson.Hash`
  (no reserialization, no input-asserted identities).
- `Hp` is never demanded as an intrinsic P field; external `Hp` comparisons
  (fixture digests) are distinguished from intrinsic schema validation.
- Dependency direction `M → F → S → K → R → Wc → P` preserved; no new edges, no
  edge back into K/F/S. Evidence §4.

## 9. P1 read-only reuse evidence

Used exactly: `CanonicalJson.Parse/Hash/IsLowerHex`, `FrozenSchemaRules.IsValidId`,
`FrozenContractVerifier.TryVerifyCatalog`, plus reads of the returned
`CatalogContract`. Enforced by a compiled-token test
(`DetachedEvidenceCallsOnlyItselfConsumesTheCatalogContractAndUsesTheAuthorizedP1Surface`);
any other P1 call fails the suite. P1 sources unchanged (blob `fdff96b9…`
retained); no P1 refactor, F02 consolidation, canonicalization change, or K
contract change. Evidence
[reachability-and-release.md §2](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/reachability-and-release.md).

## 10. Cross-record binding checks

Vanguard re-validation (`K → R → Wc → P`) so failures name the offending
record's own rule; binding checks precede duplicated-value checks so
substitutions are reported as binding failures. Duplicated-value coverage spans
every K category at least once across hash-consistent semantic tests. Evidence
§§5 and fixture/negative documents.

## 11–12. Hand-constructed positive fixtures and exact digests

Two complete chains, built by a standalone generator before the P2 verifier
existed (platform SHA-256 over exact canonical bytes):
B2/pre (`Hr2 = 82e28e80…`, `Hw2 = a93b8cd4…`, `Hp2 = 9b1cc43b…`) and B3/post
(`Hr3 = 7d96bc2f…`, `Hw3 = 293e4af9…`, `Hp3 = 67c81e9e…`), bound to accepted P1
`K2`/`K3` (`Hk2 = 793aa2aa…`, `Hk3 = 255d017e…`). Exact bytes, digests, lengths,
blobs, and reproduction are in
[fixtures-and-digests.md](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/fixtures-and-digests.md);
regeneration was verified byte-identical, and the test suite re-derives every
digest with the platform primitive and `CanonicalJson.Hash`.

## 13. Structural negative-control matrix

Raw-byte controls for missing/extension fields, literals, versions, identifiers,
types, enums, hash format, empty strings, nulls, whitespace, multiple objects,
truncation, invalid/noncanonical escapes, noncanonical integers, non-ASCII bytes,
duplicate keys, key order, and oversize — each asserting the exact first refusal.
Full matrix:
[negative-control-matrix.md §1](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/negative-control-matrix.md).

## 14. Binding/substitution negative controls

Incorrect `Hk`/`Hr`/`Hw` in each record; substituted K/R/Wc/P across otherwise
valid chains; external `Hp` mismatch; qualification-pair disagreement. Expected
first refusals (`R catalogSha256 does not bind K`, `Wc …`, `P …`) verified; a
substituted R into Wc verification is refused by the vanguard R rule. Matrix §2.

## 15. Hash-consistent semantic-invalidity controls

`ChainRefusesHashConsistentReviewDisagreements` /
`…ReceiptDisagreements` / `…ContentDisagreements`: every affected hash is
recomputed (edit R ⇒ new `Hr` propagated; edit Wc ⇒ new `Hw` propagated), and
the first refusal is the intended `<record> <field> disagreement` — proving the
semantic/duplicated-value checks exist independently of hash freshness. Matrix
§3.

## 16. Untrusted-chain authority-boundary controls

- Complete valid chains are labeled **STRUCTURALLY VALID, OPERATIONALLY
  UNTRUSTED TEST CONTENT** and must succeed structurally while projecting no
  authority (`UntrustedChainIsStructurallyValidButCarriesNoAuthorityProjection`).
- Injected reviewer/owner/administrator identity, signature, or timestamp
  metadata is a field-set rejection, never authority evidence.
- `accepted=true` is verified as a literal only; `false`/non-boolean are refused;
  the literal is deliberately not projected so it cannot be misread as an
  approval flag.
- A valid untrusted P-shaped record is accepted structurally without any
  authority claim; structural rejection and authority-not-established remain
  distinct. Matrix §4.

## 17. Determinism and failure behavior

Repeated verification is deterministic with stable digests; inputs are
byte-unchanged; refusals return a null contract with an exact deterministic
diagnostic; no partial validation success, no swallowed exceptions, no hidden
I/O or environment dependency, no mutable global state, bounded parsing
(codec size/depth), no input mutation. Matrix §5.

## 18. Non-authoritative result design

`ReviewRecordContract`, `OwnerReceiptRecordContract`,
`AdminRecordContentContract`, `DetachedEvidenceBindingsContract` — internal
records of content values/identities only; no forbidden name tokens; no
promotion method, factory, public API, capability, or approval token; no
cross-namespace authority type anywhere in the P2 surface. Evidence
[schema-and-identity.md §6](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/schema-and-identity.md).

## 19. Production reachability

Debug and compiled-Release direct-call scans (with scanner positive control)
find zero production callers; no Validation type is exported; no production
signature consumes P2 results; P2 references only Validation + BCL types; all
four `TryVerify*` entries require ≥2 `byte[]` inputs. P1-Q1-F05 limitation
retained: direct-call absence is not an exhaustive transitive or
reflection-aware proof. Evidence
[reachability-and-release.md §1–2](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/reachability-and-release.md).

## 20. E-1 consumer exclusion

No E-1 production consumer and no reference to E-1, `Installed`, `Launch`,
`Lifecycle`, `Restricted`, `Execution`, `Bootstrap`, `Native`, `Preparation`, or
`Recovery` types from the P2 core; no protected-root or custody dependency; no
final-admission consumer. E-1 remains accepted with qualifications, untouched.

## 21. F02 authority separation

`FinalAcceptanceCodec` unchanged; no P2 reference to F02 or the qualification
store; no executable approval dependency, delegation, or shared-root
provisioning; the divergence-control obligation is preserved.

## 22. RELEASE verification

Release M1 built production-mode (0/4 hooks, 0/3 properties, 14/14 core types);
`eng/VerifyProductionAssembly --production` → **RESULT: PASS**, exit 0; Debug-as-
production → **RESULT: FAIL**, exit 1 with the unchanged forbidden lists. Raw
outputs preserved. No verifier rule weakened; no new `ForTest` seam; `eng/**`
unchanged.

## 23. P4 neutrality

**P4-NEUTRAL, CONDITIONAL.** Only new internal files under the authorized
directories; no protected-sink caller set, F01/F02 enforcement, S10/S11
domination, S28 isolation, first protected effect, downgrade refusal, root
authorization, E-1 reachability, or RELEASE isolation file modified; no new
production-reachable authority path; F-R1/O-R1, O-R2, O-R3, the
observation-to-effect residual, `NATIVE-PENDING`, and all 331 native skips
preserved. A protected-execution or production authority boundary change would
stop the work; none occurred. Conditional on independent confirmation in P2-Q1.

## 24. Executed build/test results

| Check | Result |
|---|---|
| Debug M1 build | succeeded, 0 warnings / 0 errors, instrumentation PASS |
| Test project build | succeeded, 0 warnings / 0 errors |
| P2 filter (`Validation.DetachedEvidence`) | **33 passed / 0 failed / 0 skipped** |
| P1+P2 Validation filter | **120 passed / 0 failed / 0 skipped** (87 + 33) |
| Full Windows M1 suite | **1,517 passed / 0 failed / 331 skipped / 1,848**, exit 0 (baseline 1,484/0/331/1,815; delta +33) |
| Release M1 build | succeeded, 0 warnings / 0 errors, production mode PASS |
| RELEASE verifier | **PASS**, exit 0 |
| Debug-as-production | **FAIL**, exit 1 (expected) |

Native skips are unqualified and are not passes. Exact commands, outputs, and
exit codes: [executed-results.txt](rc2b-06-b6-alt-b-slice-e-02-p2-evidence/executed-results.txt)
and `…-p2-evidence/executed/`.

## 25. Limitations and retained qualifications

- **Q1-F01–F07 retained** as ruled by the P2 owner decision §5; in particular
  Q1-F05 (direct-call-only reachability) and Q1-F06 (runtime-only exclusions:
  issuer authenticity, protected acquisition, host checks, freshness,
  execution).
- Fixtures are non-authoritative test bytes; structural validity of a supplied
  chain proves nothing about genuine issuance, human review, or custody.
- P2 builds are not claimed bit-identical to any prior build (Q1-F04).
- No native positive qualification; `NATIVE-PENDING` and 331 skips retained.
- No independent multi-reviewer or owner acceptance; this is producer evidence.

## 26. Producer disposition and recommended independent qualification

**IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION.**

Recommended next commission (not started): **`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2-Q1`**
— reconstruct R/Wc/P schemas independently, build independent fixtures and
`Hk/Hr/Hw/Hp`, challenge field sets/literals/duplicated values, construct
original substitution and hash-consistent attacks, verify structurally valid
untrusted chains, verify absence of issuance/V/W/A/protected-root/promotion,
verify production unreachability, RELEASE isolation, and conditional P4
neutrality, and preserve native limitations. A separate
**`…-P2-OWNER-ACCEPTANCE-01`** must follow supportive qualification.

No independent P2-Q1, owner acceptance, R/Wc/P issuance, protected approval,
positive validation, downstream implementation, activation, synchronization, or
push was performed. Work stops here.
