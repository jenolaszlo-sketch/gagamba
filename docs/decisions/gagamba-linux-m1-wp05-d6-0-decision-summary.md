# WP05 D6.0 Executive Decision Summary

Status: **PROPOSED - NOT APPROVED**

## C1 - Ambiguous ReleaseIntent (R09)

Problem: R12 can report definite `NotCommitted` when both journal slots may
already hold Intent and the seal unlink failed, and it requests no stop.

Recommendation: explicit ambiguous-persistence decision with
`MayHaveExecuted`, no FD4, bounded runtime stop, best-effort durable stop,
retained ambiguous result and conservative recovery.

Why: restores truthful durability semantics without weakening no-resend.
Change scope: medium (runtime decision API plus `RequestRelease`/stop path; no
schema). Main risk: retaining/reporting ambiguity correctly and defining the
owner/domain stop boundary. Alternatives: minimal uncertainty field; documented
current semantics (both rejected for clarity/R09 compliance).

Approval question: **Approve an explicit C1 ambiguous decision with bounded
runtime stop and retained result?**

## C2 - Stop-first capability invalidation (R08)

Problem: a runtime stop does not invalidate the capability; failed durable
stop plus a second arbiter can reuse it.

Recommendation: consume/invalidate the capability at every runtime stop
acceptance, independent of journal persistence.

Why: uses the existing single-use authority, closes the bounded multi-instance
window, no schema. Change scope: small (`RequestStop` plus capability consume).
Main risk: stop after a release that already consumed the capability must not
rewrite release history. Alternatives: first-refusal invalidation; single-
arbiter-only documentation (rejected as insufficient).

Approval question: **Approve irreversible capability invalidation on stop
acceptance, including failed/clock-unavailable stop paths?**

## C3 - Persisted stop deadline (R27)

Problem: the immutable StopRecord deadline is not consumed; `Reconciler` uses
a caller-supplied five-second allowance and can effectively reset it.

Recommendation: `Reconciler` computes remaining time from the persisted
absolute deadline; expired => zero-wait urgent stop; zero/unverifiable =>
quarantine/Unknown; no fresh five seconds.

Why: enforces the existing durable policy without a schema change. Change
scope: small/medium (`Reconciler` plus deterministic clock tests). Main risk:
zero/invalid deadlines must fail closed without unsafe destructive authority.
Alternatives: caller-provenance (unenforceable); new authority object
(unnecessary).

Approval question: **Approve persisted-deadline consumption with zero-wait
urgent stop on expiry and quarantine on unverifiable deadlines?**

## C4 - Duplicate/concurrent release semantics (R02/R06)

Problem: sequential duplicates are `Duplicate`, but a concurrent loser can be
`Refused` while the winner releases; retention differs by failure point.

Recommendation: explicit concurrent-refusal contract; later calls Duplicate;
document outside-gate vs gate retention; no waiting; one physical write.

Why: matches observed R12 behavior, avoids deadlock/wait state, preserves
safety. Change scope: small (retention/documentation; no schema). Main risk:
consumers must not treat `Refused` as definite no-release or `Duplicate` as
proof of Committed. Alternatives: linearizable wait-based result; new
in-progress state (rejected as larger/unnecessary).

Approval question: **Approve explicit concurrent-refusal semantics and the
documented retention asymmetry?**

## Combined

The proposals compose without a journal schema change: C2 closes runtime
authority, C1 uses it for ambiguous persistence, C4 documents retention,
C3 enforces the stored deadline. No gate is accepted by this summary. Human
approval is required individually; GLM review is advisory and separate from
authorization.
