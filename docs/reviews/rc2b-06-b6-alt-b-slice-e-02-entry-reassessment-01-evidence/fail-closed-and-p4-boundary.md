# E-2 entry reassessment — fail-closed, E-1, P4, and RELEASE boundary

Date: 2026-11-09 (Asia/Manila)

## Fail-closed case applicability for E-2-P1

Statuses: **In P1** = the refusal invariant is exercised by the structural package at the data level; **Deferred** = belongs to later packages, the protected path, or native qualification; **N/A** = not reachable from the structural package.

| # | Failure class | P1 applicability | Required invariant in P1 |
|---|---|---|---|
| 1 | Missing catalog | In P1 (data level) | Absent input refuses; no default object |
| 2 | Unsupported catalog identity | In P1 (data level) | Unknown schema/version refuses; no search/`latest` semantics exist in P1 |
| 3 | Invalid governing schema | In P1 | Non-CJSON-1 or non-`schemaVersion`=1 refuses |
| 4 | Catalog/schema mismatch | In P1 (data level) | Field-set/type/bounds mismatch refuses |
| 5 | Broken `Hm/Hf/Hs` bindings | In P1 | Cross-artifact hash equality checks refuse on mismatch |
| 6 | Missing detached review | Deferred (E-2-P2, then protected path) | — |
| 7 | Missing owner acceptance | Deferred (E-2-P2) | — |
| 8 | Missing protected approval | Deferred (protected path; atomic Slice E) | — |
| 9 | Wrong approval target | Deferred (E-2-P2 structural; protected path authority) | — |
| 10 | Unauthorized issuer | Deferred (protected path authority; D1 no-delegation default) | — |
| 11 | Unsupported F02 authority reuse | N/A for P1 (no stores); policy is adopted (F02 separate) | — |
| 12 | Invalid or conflicting approval | Deferred (E-2-P2 / protected path) | — |
| 13 | Stale or substituted evidence | Deferred (protected path lifecycle); P1 covers snapshot substitution within one call | Immutable inputs; no mutation after decode |
| 14 | Incomplete chain | In P1 for K/M/F/S completeness (duplicate values, missing references) | Missing/duplicate/extra fields refuse |
| 15 | Wrong host | In P1 only as data equality (duplicated `hostId` equal across K/F/S); actual host binding deferred | Duplicated host values must equal K |
| 16 | Invalid delegation | N/A for P1 (no actors); D1 default is no delegation | — |
| 17 | Missing/unknown V/W/A role | N/A for P1 | — |
| 18 | Unavailable verifier | N/A for P1 (no filesystem/host dependency) | — |
| 19 | E-1 integrity failure | N/A for P1 (no E-1 consumer) | — |

No error codes or exception types are specified here. Refusal invariants are binding; encodings and observable negative controls are implementation deliverables of the authorized package.

## E-1 consumer classification

**NO E-1 PRODUCTION CONSUMER.** E-2-P1 has no design dependency on E-1: it consumes byte sequences and the accepted schema contracts only. No `PrepublicationIntegrityProjection` reference, no constructor, helper, or static use would be introduced. A future protected validation path (E-4/E-6) may consume E-1 integrity evidence, at which point **PRODUCTION CONSUMER REQUIRES SEPARATE AUTHORIZATION** and the mandatory F-02 first-consumer review plus F-01 fail-closed obligation apply.

Preserved: F-01 role-hash bindings and future-consumer obligation; F-02 Q1-F01–Q1-F06 and the first-consumer gate with Option A reassessment; F-03 Windows PowerShell 5.1 scope and tooling immutability.

## P4 and RELEASE classification

**P4-NEUTRAL (design classification, conditional on implementation evidence).** Rationale:

- No protected-sink caller or sink is added; no admission, lifecycle, authority, or RELEASE path is touched.
- F01/F02 enforcement, S10/S11 domination, S28 isolation, first protected effect, qualified-to-legacy downgrade refusal, and root authorization boundaries are unchanged by construction.
- The package cannot construct prepublication positive authority or a validation factory; no `OwnerAuthorityContext.Validation` interaction.
- RELEASE impact: none intended. Implementation must demonstrate no production caller, `internal` visibility, no RELEASE test seam, no public API; if it cannot, the classification becomes **P4 RE-AUDIT REQUIRED** before any exposure.

Preserved: F-R1/O-R1, O-R2, O-R3, observation-to-effect residual, `NATIVE-PENDING`, all 331 native skips (not passes), RELEASE test-hook isolation.

## Downstream separation

| Milestone | Relationship to E-2-P1 | Boundary E-2-P1 must respect |
|---|---|---|
| E-3 v3-pre codec | None during E-2-P1 | Do not implement or resolve v3-pre layouts; composite Q-E1 remains open |
| E-4 GAVA1 | None during E-2-P1 | Do not implement GAVA1; E-4 owns its authority mechanisms and P4 re-audit |
| E-5 launcher/custody | None during E-2-P1 | Do not implement launcher, custody, or privilege drop |
| E-6 atomic enablement / R-O-C ordering | None during E-2-P1 | Do not implement publication, locks, or ordering; E-6 owns them with its P4 re-audit |
| E-7 driver integration | None during E-2-P1 | Do not implement driver or dispatch |
| Slice N | None during E-2-P1 | No protected native behavior is claimed or exercised |
| Later E-2 packages | E-2-P1's non-authoritative outputs may be consumed only through separately authorized contracts | No approval, eligibility, capability, or validation authority may be inferred from P1 results |
