# Qualification inventory

Full 10-column matrix: report §21. Identifiers are preserved from the
accepting decisions. Count: **39 retained items** (5 P0, 11 D1, 7 P1-Q1,
6 P2-Q1, 3 E-1, 4 P4, 1 native, 1 release-reachability, 1 composite Q-E1).

## P0 — frozen-schema provenance (5)

| ID | Disposition | Blocks E-2 | Responsible gate |
|---|---|---|---|
| P0-Q1 external durability | Accepted limitation | No | Owner/repository preservation; no numbered milestone |
| P0-Q2 historical captions | Accepted; do not rewrite | No | P0 record custodian |
| P0-Q3 `ValidId` grammar | Resolved reference | No | Committed reference |
| P0 §11 item 4 extraction completeness | Accepted limitation | No | Future reviewer/additive correction |
| P0 §11 item 5 single-reviewer recording | Accepted limitation | No | Owner governance |

## D1 — approval-chain governance (11)

| ID | Disposition | Blocks E-2 | Responsible gate |
|---|---|---|---|
| D1-1 procedural V independence | Accepted limitation | No | D1-governed administration |
| D1-2 trusted-root bounds | Accepted permanent non-claim | No | Owner (threat-model change only) |
| D1-3 unprovisioned roots/chain unimplemented | Implementation prerequisite | No | E-5/E-6 + Slice N |
| D1-4 refusal encodings unspecified | Implementation prerequisite | No | E-5/E-6 |
| D1-5 designation/process | Implementation prerequisite | No | D1-governed administration |
| D1-6 no delegation | Accepted limitation | No | New owner decision if changed |
| D1-7 Mechanism A unselected | Downstream deferred | No | Separate owner decision |
| D1-8 native unqualified | `NATIVE-PENDING` | No | Slice N |
| D1-9 P0 extraction inherited | Accepted limitation | No | P0 custodian |
| D1-10 single-reviewer recording | Accepted limitation | No | Owner governance |
| D1-11 E-4/E-6 P4 re-audits | Downstream deferred | No | E-4/E-6 |

## P1-Q1 (7)

| ID | Disposition | Blocks E-2 | Responsible gate |
|---|---|---|---|
| F01 depth-8 bound | Bounded implementation qualification | No | Future schema decision |
| F02 empty-string rejection | Informational | No | Future schema decision |
| F03 null/unknown-field differences | Schema-scoped differences | No | Future divergence review |
| F04 non-bit-reproducible rebuilds | Non-blocking qualification | No | Any binary-identity claim |
| F05 direct-call-only reachability | Reachability qualification | No | First production consumer |
| F06 deferred runtime-only checks | Explicitly deferred | No | E-2-P2/E-4/E-5/E-6/Slice N as itemized |
| F07 `/` path observation | Informational | No | Existing runtime host-path checks |

## P2-Q1 (6)

| ID | Disposition | Blocks E-2 | Responsible gate |
|---|---|---|---|
| F01 reachability scope | Retained limitation | No | First production consumer |
| F02 null `byte[]` | Bounded input-domain qualification | No | Future consumer (fail-closed handling) |
| F03 depth-8 | Retains P1-Q1-F01 | No | Future schema decision |
| F04 non-bit-reproducible rebuilds | Non-blocking qualification | No | Any binary-identity claim |
| F05 counting convention | Documented clarification | No | Future schema wording change |
| F06 no topology↔phase cross-rule | Accepted clarification | No | New specification decision |

P2-Q1-F03 explicitly retains P1-Q1-F01 rather than creating a second
normative rule. P2-Q1 identifiers are not merged with P1-Q1 identifiers.

## E-1, P4, native, RELEASE, composite Q-E1 (10)

| ID | Disposition | Blocks E-2 | Responsible gate |
|---|---|---|---|
| E1-F-01 role-hash/runtime bindings | Closed with qualifications | No | First E-1 consumer decision |
| E1-F-02 first-consumer gate/Option A | Closed with qualifications | No | First E-1 consumer decision |
| E1-F-03 PowerShell 5.1/7 | Closed with qualifications | No | Future PS7 qualification |
| P4-F-R1/O-R1 | Preserved residual | No | E-4/E-6 or caller's milestone |
| P4-O-R2 | Preserved residual | No | Future caller audit |
| P4-O-R3 | Open residual | No | Future authorization/consumer gate |
| P4 observation-to-effect | Accepted residual | No | E-6 + Slice N |
| NATIVE-PENDING | Pending | No | Slice N; E-5/E-6/E-7 |
| RELEASE-REACH | Retained limitation | No | First consumer/new dynamic path |
| Q-E1-COMPOSITE | Open, outside E-2 | No | E-3/E-4 |

## Promotion rules

No qualification may be described as closed by the E-2 closure decision. A
future gate that resolves one must record its own evidence and owner ruling.
Incorrect promotion risks: skipped native tests treated as passes; governance
adoption treated as operational approval; finite probes treated as exhaustive
proof; E-2 P4 neutrality reused for E-4/E-6 without a fresh audit.
