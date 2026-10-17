# Independent ADR 0004 Review — Supplied Disposition and Required Clarifications

Date preserved: 2026-10-17 (Asia/Manila)

Revision binding: ADR commit `c3cb16412cf136691280eb8f3fadedbe7dc451a9`,
branch `qualification/ar7-feasibility`.

Disposition: **ADR 0004 ACCEPTED WITH REQUIRED CLARIFICATIONS - UPDATE ADR BEFORE IMPLEMENTATION**

Preservation note: the complete reviewer body was not included in the review
record supplied for this authoring turn. The disposition and the required
clarifications below are preserved as supplied. No reviewer text has been
inferred or fabricated. The verbatim reviewer body remains pending supply; this
file must not be altered after preservation.

## Accepted architecture

The review accepted the fundamental architecture:

- `OwnershipSnapshot` version 3;
- the existing ownership journal;
- the existing `JournalWriterLease`;
- no second release journal;
- `RecordedPreparation` as release identity;
- durable `ReleaseIntent` as the conservative crash-safety boundary;
- runtime release winner inside the arbitration gate;
- no resend after durable intent;
- no durable bearer release authority;
- lazy v2→v3 compatibility;
- WP04 and G2 not reopened wholesale.

These are not to be redesigned without a concrete contradiction.

## Required clarification 1: durable trusted status observations

The authoritative WP05 plan requires durable `ExecObserved` and `RootObserved`
(or exact repository-native equivalents). ADR 0004 lacked their durable
representation. The v3 schema must let trusted coordinator/status observations
survive owner death, recovery, domain cleanup, staging cleanup and terminal
receipt publication delay, in the same ownership snapshot/journal. A second
status journal is forbidden.

The amended ADR must define the exact observation model (values, meaning,
trusted source, identity binding, sequence/order validation, writer, first
appearance, monotonicity, update rules, malformed/contradictory handling),
observation authority (evidence only), execution classification, and observation
timing/owner-loss behavior.

## Required clarification 2: journal serialization/merge semantics

The amended ADR must define exactly how `ReleaseIntent`, `ReleaseCommitted`,
`StopRecord`, `ExecObserved`, `RootObserved` and later cleanup/terminal facts
coexist. Every write must hold the valid `JournalWriterLease`, start from the
latest acknowledged snapshot, preserve all existing monotonic facts, add only the
authorized transition, use the exact next sequence, and fail closed on stale
baseline/conflict. Updates are full monotonic snapshot transitions, not
independent last-writer-wins field writes.

The no-lost-fact rule, intent/stop serialization cases, committed/stop
serialization, and observation merge semantics must be explicit.

## Required clarification 3: StopDeadline, timestamps, encodings and boundaries

The amended ADR must bind `StopDeadline` precisely to the plan (five-second stop
allowance, bounded final drain, CLOCK_MONOTONIC nanoseconds, same-boot rule,
expiry behavior). Timestamps are audit/diagnostic, sampled before persistence,
and never determine release/stop winner ordering. All new durable enums require
explicit stable numeric encodings with reserved values and fail-closed decoding.

The amended ADR must also incorporate:

- partial delivery as an impossible-under-contract anomaly;
- `Delivered` meaning only the owner's full 128-byte pipe write success;
- v3 inheriting the existing `BootId` with same-boot deadline semantics;
- the WP04 v3 boundary (preferred: ordinary WP04 abort remains v2);
- the updated recovery matrix including release, stop, exec, root, delivery and
  cleanup;
- updated invariants;
- the updated G2 impact classification;
- the bounded first implementation tranche;
- the no-implementation boundary for this tranche.

## Final status

ADR 0004 CLARIFICATIONS REQUIRED - UPDATE ADR BEFORE IMPLEMENTATION
