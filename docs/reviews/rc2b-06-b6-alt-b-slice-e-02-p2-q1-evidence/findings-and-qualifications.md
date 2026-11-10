# P2-Q1 — findings and retained qualifications

Every finding below is classified and bounded. No implementation defect,
authority-boundary violation, or RELEASE/P4 regression was demonstrated.

## Findings

### Q1-F01 — Direct-call-only reachability scope (BOUNDED LIMITATION, retained)

- **Accepted invariant:** owner decision §20 — P2 must remain
  production-unreachable, and direct-call absence is not a complete
  reflection-aware or transitive proof.
- **Affected scope:** `Validation/DetachedEvidenceVerifier.cs`,
  `Validation/DetachedEvidenceContracts.cs`.
- **Evidence:** reviewer scans `REACH-DBG-01..06`, `REACH-REL-01..06`,
  `P1R-01` (0 offenders in Debug and compiled RELEASE; no exported type; no
  consuming signature), plus the producer reachability tests re-executed.
- **Demonstrated defect:** none. The bound is the accepted evidence scope.
- **Required remediation:** none for P2. A first production consumer requires
  its own authorization and the F-02 first-consumer gate where applicable.

### Q1-F02 — Null `byte[]` argument escapes the refusal contract (BOUNDED OBSERVATION)

- **Accepted invariant:** commission §16 — no unexpected exception escapes
  where the accepted `TryVerify` contract requires refusal.
- **Affected source location:** `CanonicalJson.Parse` first statement
  (`bytes.Length`) reached from every P2 `TryVerify*` entry.
- **Reproduction:** reviewer probe `ROB-04`: pass `null` as the review argument
  to `TryVerifyReviewRecord`; a `NullReferenceException` escapes.
- **Expected behavior:** the contract covers `byte[]` inputs; `null` is not a
  byte-array value and no accepted document defines null-argument behavior.
- **Actual behavior:** `NullReferenceException`; identical behavior in the
  reused P1 entry `FrozenContractVerifier.TryVerifyCatalog` (`ROB-06`,
  accepted P1 behavior since `06006a7`).
- **Evidence:** `executed/probe-run.txt` (ROB-04, ROB-06).
- **Demonstrated defect:** no. Internal-only API; no production caller; no
  malformed-bytes path reaches it; the behavior is inherited from the accepted
  P1 baseline and is consistent across the Validation core.
- **Required remediation:** none during P2-Q1 (modifying P1/P2 is prohibited).
  If a future consumer ever exposes these entries more broadly, a bounded
  hardening decision (explicit null refusal) belongs to that consumer's
  authorization.

### Q1-F03 — Depth-8 codec constraint (BOUNDED IMPLEMENTATION QUALIFICATION, retained)

- **Accepted invariant:** P1-Q1-F01 lineage — depth 8 is an implementation
  constraint, not a CJSON-1 rule; R/Wc/P are depth-2 records.
- **Evidence:** MAL-19 (a syntactically deeper probe object is refused as
  `invalid CJSON-1`; no crash); constructed R/Wc/P sizes 595–777 bytes.
- **Demonstrated defect:** none; no accepted R/Wc/P record exceeds the bound.

### Q1-F04 — Non-bit-reproducible rebuilds (INFO, retained)

Reviewer Debug/Release builds pass all checks but their hashes differ from the
producer's recorded binaries. Accepted lineage: P1-Q1-F04 / P4-03 Q1-F02.
Source blobs remain the identity anchor.

### Q1-F05 — "Duplicated value" counting convention (INFO, clarification)

The owner/producer documents label Wc/P with "eight duplicated values" while
enumerating seven equal-to-K fields; R is labeled "ten". The reviewer verified
the implementation checks the complete reference set in every interpretation:
Hk/Hr/Hw/Hp references plus every equal-to-K field (R: 11 references; Wc: 9;
P: 10). See `schema-reconstruction.md` §5. No missing check; no defect.

### Q1-F06 — No accepted topology↔phase cross-rule (INFO, clarification)

No accepted E-2 normative source states that `phase=pre` implies
`topology=b2-lifeline` (or post↔b3) for K/R/Wc/P; the accepted rule is
cross-record equality with K. The implementation therefore does not enforce a
phase/topology cross-rule, correctly avoiding an invented rule. If a future
schema revision intends such a constraint, it requires a new accepted source;
P2 as authorized is correct.

## Retained qualifications (not findings; preserved from accepted decisions)

- P0 qualifications: external-package durability; historical `PENDING`
  captions; S2 grammar reference; single-reviewer provenance limits.
- D1 qualifications: procedural V independence; separate W/A; trusted-root
  threat-model limits; unprovisioned protected roots; deferred refusal
  encodings; no delegation/standing approval/cross-host reuse; F02 authority
  separation; unselected Mechanism A permits; `NATIVE-PENDING`.
- P1 qualifications: all seven Q1 findings as ruled in the P1 owner
  acceptance.
- E-1: accepted with qualifications; F-01/F-02/F-03 closures remain scoped;
  future-consumer fail-closed and first-consumer parity obligations retained.
- P4 refusal-only acceptance: F-R1/O-R1, O-R2, O-R3, observation-to-effect
  residual, `NATIVE-PENDING`, 331 native skips.
- Composite Q-E1 remains open for GAVA1/v3-pre/release correlation; E-2
  remains open beyond P2.
- No operational R/Wc/P approval exists or is established by this
  qualification. A structurally valid, correctly bound chain remains
  operationally untrusted content.
