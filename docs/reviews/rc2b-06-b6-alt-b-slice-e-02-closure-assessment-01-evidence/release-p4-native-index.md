# RELEASE, P4, and native evidence index

This index points to accepted evidence. It does not copy raw outputs.

## RELEASE verification

| Check | Result | Source |
|---|---|---|
| RELEASE production verifier (independently rebuilt assembly) | **PASS**, exit 0; 0/4 forbidden hook types; 0/3 forbidden hook properties; 14/14 required core types | P2 acceptance §19; P2-Q1 evidence `executed/release-verifier.txt` |
| Debug-as-production control | **FAIL**, exit 1 (expected); unchanged forbidden lists | P2 acceptance §19; `executed/debug-as-production-verifier.txt` |
| Verifier policy | Unchanged; `eng/**` last changed at `52037fa` | P2 acceptance §19; P1 acceptance §2 |
| Exported authority API | None | P1 acceptance §13; P2 acceptance §17 |
| Production test hooks | None new; no `ForTest` seam added | P2 acceptance §19 |
| P1/P2 production authority consumer | None identified | P1 acceptance §13; P2 acceptance §17 |
| E-1 production consumer | None | P2 acceptance §18 |
| Protected execution path created by E-2 | None | P2 acceptance §26 |

Retained limitation: direct-call/type-token/selected-string absence is not an
exhaustive transitive or dynamic-invocation proof (P1-Q1-F05; P2-Q1-F01).

## P4

| Item | Status | Source |
|---|---|---|
| P2 classification | **P4-NEUTRAL** for the reviewed revision only | P2 acceptance §20 |
| P1 classification | P4-NEUTRAL for the accepted revision | P1 acceptance §15 |
| P0/D1 | Documentation-only; no production/test change | P0 acceptance §15; D1 §15 |
| Protected-effect boundary | Unchanged; no new operational path | Report §17 |
| F-R1/O-R1 | Preserved residual; new caller requires fresh audit | P4 coverage acceptance §5 |
| O-R2 | S28 test-only; `internal` is not authority | P4 coverage acceptance §5 |
| O-R3 | Refusal-reason typing race open | P4 coverage acceptance §5 |
| Observation-to-effect residual | Accepted residual | P4 coverage acceptance §5 |
| E-4/E-6 P4 re-audits | Required at those milestones | D1 §13 item 11; P4 coverage acceptance §7 |

## Native

| Item | Value | Source |
|---|---|---|
| Windows M1 full suite | 1,517 passed / 0 failed / 331 skipped / 1,848 total, exit 0 | P2 acceptance §21 |
| P1-era full suite | 1,484 passed / 0 failed / 331 skipped | P1 acceptance §5 |
| Native status | `NATIVE-PENDING` | P1/P2 acceptances |
| 331 skips | Unqualified; not passes, not coverage, not protected positives | P2 acceptance §21, §23 |
| Linux protected native qualification | Not claimed | P2 acceptance §23 |
| macOS protected native qualification | Not claimed | P2 acceptance §23 |
| Positive protected execution | Not claimed or authorized | P2 acceptance §23, §26 |
| Slice N | Separate authorized milestone; no completion claimed | Slice E entry owner §9; plan §3.6 |

## CI

| Item | Value | Treatment |
|---|---|---|
| Run `42933880787` | 10 executed jobs green; one Windows launch-spike job skipped by design; no publish | Operator-reported only |
| `origin/main` at assessment | `a30d180…`; divergence `0 0` against local tracking ref | Locally verified; no fetch |
| Release publication | None inferred | Publishing is tag-triggered |

CI success is not protected native qualification and is not a release
publication.
