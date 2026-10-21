# WP05 D6.2 Human Approval Addendum

Status: **PROPOSED - NOT APPROVED**
Human decisions: **PENDING HUMAN**
C3 policy: **C3 ZERO-WAIT POLICY - PENDING HUMAN**

This addendum carries the individual human questions after the D6.2 precision
amendments. It contains no prefilled approval, no selected C3 policy and no
implementation authorization. GLM review is advisory and cannot answer these
questions on the human holder's behalf. C2 and C4 remain independently
supported for human decision; C1 and C3 require focused verification of the
D6.2 amendments first.

## C1 - Ambiguous ReleaseIntent (D6.2-R3/R4)

**Question:** After focused D6.2 verification, approve C1 as the authoritative
ambiguous-release contract, including conditional retention, the stop-first
and ambiguity-first ordering rules, the stop-won `Refused(MayHaveExecuted)`
classification, and the corrected test-only fixture provenance?

- Current status: **PENDING HUMAN**.
- Review status: D6.1 DESIGN SUPPORTED WITH REQUIRED AMENDMENTS; D6.2 focused
  verification pending.
- Required prerequisites: focused GLM verification of C1 D6.2-R3/R4; C2 remains
  a prerequisite; no production implementation is authorized by this answer.
- Consequences of approval: C1 becomes the authoritative documentation
  contract; implementation may be proposed separately, subject to per-tranche
  authorization and later source-bound qualification. No production behavior
  changes and no gate closes.
- Consequences of rejection: C1 remains unapproved; the release gate stays
  closed; the D6.2 text remains a proposal.
- Open boundaries: journal ambiguity remains separate from the returned
  runtime result; no second Intent/FD4; recovery remains conservative; C2
  consumption is load-bearing.

## C2 - Stop capability consumption

**Question:** Approve C2 as written, with the D6.2 cross-reference only:
irreversible single-use capability consumption at first stop acceptance,
including normal, clock-unavailable and overflow paths, independent of
StopRecord persistence?

- Current status: **PENDING HUMAN**.
- Review status: DESIGN SUPPORTED FOR HUMAN DECISION (D6.1); D6.2 does not
  reopen C2.
- Required prerequisites: none beyond human decision; implementation tests
  remain per-tranche.
- Consequences: stop-first remains terminal for the capability; failed durable
  stop cannot reopen release; release-first history is preserved; no schema
  change.
- Consequences of rejection: C2 remains unapproved; any C1 implementation
  would lack authoritative capability closure; the release gate stays closed.
- Open boundaries: CE-03 multi-instance remains out of model and closed by the
  capability contract, not composition.

## C3(a) - Expired-deadline policy

**Question:** Does an expired durable stop deadline authorize Policy A
(identity-checked kill plus one immediate population observation with no
renewed waiting allowance), or does it require Policy B (a separately
specified bounded cleanup/observation interval with its own authority and
upper bound)?

- Current status: **C3 ZERO-WAIT POLICY - PENDING HUMAN**.
- Review status: Policy A is technically supportable; GLM did not select it.
  D6.2 R1/R2 are designed to support either policy.
- Required prerequisites: the human policy answer precedes C3 contract
  approval.
- Consequences: neither policy may reset or extend the stored five-second
  deadline; unconfirmed emptiness remains Unknown/quarantine; no release
  authority is recreated.
- Consequences of no selection: C3 contract approval remains gated; no
  production remediation is authorized.
- Open boundaries: Policy A zero-timeout behavior still requires later runtime
  qualification; Policy B requires a new explicit bound/authority.

## C3(b) - Deadline/reconciliation contract

**Question:** Subject to the policy answer, approve C3 as amended by D6.2-R1
and R2: typed stop/aggregate outcome discrimination, explicit
`JournalFinalization` uncertainty, sealed trusted clock provenance, fail-closed
epoch/frequency checks, and no deadline reset?

- Current status: **PENDING HUMAN** (contract + policy).
- Review status: D6.1 DESIGN SUPPORTED WITH REQUIRED AMENDMENTS; D6.2 focused
  verification pending.
- Required prerequisites: focused GLM verification of R1/R2; human C3 policy
  answer.
- Consequences: confirmed empty cleanup remains subject to durable
  finalization; persistence-only failure becomes Unknown/quarantine; operation
  failure remains Failed; incompatible clocks cannot fabricate remaining time.
- Consequences of rejection: C3 remains unapproved; the release gate stays
  closed; no C3 implementation is authorized.
- Open boundaries: no schema change; `OwnerDomainClock` remains incompatible
  and dead code; no OS-wide clock qualification is claimed.

## C4 - Duplicate/concurrent release semantics

**Question:** Approve C4 as written, with the D6.2 precision note: sequential
`Duplicate` of a stop-won refusal, unchanged retention asymmetry, no new
`ConcurrentRefusal`, no waiting protocol and one physical RELEASE?

- Current status: **PENDING HUMAN**.
- Review status: DESIGN SUPPORTED FOR HUMAN DECISION (D6.1); D6.2 does not
  reopen C4.
- Required prerequisites: none beyond human decision; exact-string tests remain
  per-tranche.
- Consequences: `Refused` remains non-proof of no prior release; `Duplicate`
  remains non-proof of Committed; one physical FD4 is preserved.
- Consequences of rejection: C4 remains unapproved; observability expectations
  would lack an authoritative contract; the release gate stays closed.
- Open boundaries: sealed-journal retry-futility note is a nonblocking
  clarification; no public contract change.

## Implementation authorization

**Question:** Should any specific D6 remediation tranche be authorized after
the individual contract decisions and focused independent review?

- Current status: **PENDING HUMAN**; no blanket authorization.
- Order remains a proposal: C2 -> C1 -> C4 -> C3, with C3 movable only by
  separate human authorization.
- Every tranche requires its own approved contract, tests-first implementation,
  independent review, new source/evidence binding and rollback criteria.
- No production remediation, target execution or release qualification is
  authorized by this addendum.

## Summary

| Item | Status |
|---|---|
| C1 contract | PENDING HUMAN; focused D6.2 verification first |
| C2 contract | PENDING HUMAN; review-supported, unchanged |
| C3 expired-deadline policy | C3 ZERO-WAIT POLICY - PENDING HUMAN |
| C3 contract | PENDING HUMAN; focused D6.2 verification and policy first |
| C4 contract | PENDING HUMAN; review-supported, unchanged |
| Implementation authorization | PENDING HUMAN; per tranche, no blanket |

No answer in this addendum is an approval.
