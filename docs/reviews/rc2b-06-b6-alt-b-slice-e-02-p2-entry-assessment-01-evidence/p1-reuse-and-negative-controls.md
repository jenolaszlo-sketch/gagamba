# P2 entry assessment — P1 reuse, surfaces, and negative controls

Date: 2026-11-09 (Asia/Manila)

## P1 reuse inventory

| P1 component (accepted, read-only) | P2 use |
|---|---|
| `CanonicalJson.TryParse` / `Encode` / `Hash` / `IsLowerHex` | CJSON-1 validation, canonical bytes, exact-byte SHA-256, hex checks |
| `FrozenSchemaRules.IsValidId` | `caseId` / post-phase `qualificationId` grammar |
| `FrozenContractVerifier.TryVerifyCatalog` | verified K contract + recomputed `Hk` before R/Wc/P binding checks |
| P1 `Try*` result pattern | internal non-authoritative R/Wc/P contract and binding results |

No P1 modification, no new canonicalization logic, no new public API, no project-file change. P1's divergence-control obligation (parity reassessment on any canonical-behavior change) carries forward unchanged.

## Q1 finding applicability

| Finding | P2 effect |
|---|---|
| Q1-F01 depth 8 | none (R/Wc/P depth ≤ 2) |
| Q1-F02 empty strings | compatible (no field permits one) |
| Q1-F03 null/unknown fields | compatible (same qualification pair rule; exact field sets) |
| Q1-F04 non-bit-identical rebuild | carries forward (source blobs remain the anchor) |
| Q1-F05 direct-call-only reachability | P2 must produce its own Debug/Release scans and positive control |
| Q1-F06 deferred runtime checks | defines P2 exclusions (issuer/custody/freshness/live) |
| Q1-F07 `/` path | not applicable (no path fields) |

## Proposed surfaces

New internal files under `src/Gagamba.Execution.Linux.M1/Validation/` (for example `DetachedEvidenceContracts.cs` and `DetachedEvidenceVerifier.cs`; final names must avoid approval/trust/authorization/eligibility/admission/execution tokens) and tests under `tests/Gagamba.Execution.Linux.M1.Tests/Validation/` (positive chains, raw-byte negatives, adversarial boundary controls, Debug/Release reachability). All existing files remain unchanged.

## Negative-control strategy (candidate matrix)

| Case | Rule | Refusal | Owner | Raw bytes | Trusted acquisition | Native |
|---|---|---|---|---|---|---|
| Malformed/noncanonical CJSON-1 | P0 §6 | codec rule | P2 | Yes | No | No |
| Wrong schema/version | P0 §11 | field set / version | P2 | Yes | No | No |
| Missing/typed/extension fields | P0 §11 | exact-set/type rules | P2 | Yes | No | No |
| Invalid identity/literal/enum/`accepted` | P0 §11, `ValidId` | specific rule | P2 | Yes | No | No |
| Duplicate/truncated/oversized | P0 §6 | codec rules | P2 | Yes | No | No |
| Incorrect `Hk`/`Hr`/`Hw`/`Hp` | P0 §11 | binding mismatch | P2 | Yes | No | No |
| Substituted K/R/Wc/P; stale references | P0 §11 | binding/duplication | P2 | Yes | No | No |
| Conflicting valid evidence (case/phase/topology/host/artifact/qualification) | P0 §11 | cross-record equality | P2 | Yes | No | No |
| Unauthorized authority/issuer field | P0 §11 exact sets | field-set refusal | P2 | Yes | No | No |
| Untrusted but structurally valid P-shaped input | accepted design | **no structural refusal**; content-only result, no authority claim | P2 semantics + E-5/E-6 | Yes | No | No |
| Promotion to approval/eligibility | non-authoritative contract | no API exists; absence asserted | P2 | Yes | No | No |
| Real issuer authenticity/custody/order/freshness/live invocation | D1/E-5/E-6 | deferred | Downstream | — | Yes | Yes for native custody |

## Adversarial self-consistent cases

- R valid and bound to K without V authority → structural success; no review/approval claim.
- Wc valid and bound without W acceptance → structural success; no human-acceptance claim.
- P valid and bound but supplied from an untrusted location → structural success; no custody/eligibility/invocation claim.
- Cross-version references → refused by the differing binding.
- Hash-consistent role/field violation → refused by the structural rule.
- Forged administrative identity field → refused as an unknown/extension field.

P2 does not prove issuer genuineness (it has no trusted source) and never describes structural success as approval.
