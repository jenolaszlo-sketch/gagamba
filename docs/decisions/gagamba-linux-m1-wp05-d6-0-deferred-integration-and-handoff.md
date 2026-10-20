# WP05 D6.0 Deferred Integration and Handoff

Status: **PROPOSED - NOT APPROVED**

This file preserves the D5 independent review handoff, H1-H5, without implying
that any item is resolved. It is documentation-only.

## H1 - D5-O05 status persistence integration seam

- `ReleaseObservationBuffer` has zero production callers at the D5/D6 source.
- Component-level native FD5/buffer evidence is accepted only as component
  evidence.
- No production owner/status wiring, no real target-status persistence and no
  end-to-end trusted status claim exist.
- Product-surface consumers must remain fail-closed until integration.
- Future owner: WP08 production owner orchestration and WP10 target/status
  integration, subject to the authoritative plan and separate authorization.
- Completion requires actual production composition, native status provenance,
  durable-commit gating, owner/target evidence and independent qualification.
- D6.0 does not implement this.

## H2 - Descriptor destructive exclusions

Unclaimed D5 experiments:

- extra inherited FD5 writer delaying EOF;
- inherited helper writer on FD5;
- wrong-FD/deselect behavior and EOF/ownership consequences.

They remain unclaimed because they require reusable, disposable descriptor
topology and an explicit ownership/EOF decision; a destructive shared-host
experiment would be invalid. Smallest future test-only work package: a
second-writer/EOF case in a disposable process topology with documented FD5
ownership, followed by a wrong-FD case; no production changes. These affect
future integration/status claims, not the current C1-C4 decisions. They are
not silently counted as D5 passes.

## H3 - CR-1 optional raw termination witness

The bounded control-based SIGKILL standard is adopted:
direct-child targeting, audited bounded self-exit codes, journal fence release
and the independently supplied wait4/.NET controls. A native wait4/pidfd raw
status witness is an optional test-only enhancement, not a blocking D5
failure. Any addition must not reopen the frozen harness or introduce competing
reaping.

## H4 - C1-C4

C1-C4 are the proposed contracts in this D6.0 package. They remain unresolved
until independent GLM design review and explicit human authorization. D6.0 does
not approve, implement or close them.

## H5 - D5 documentation ordering

The D5 plan listed evidence commits before D5.1c; repository ancestry shows
D5.1c after them. The independent review classified this nonblocking. If D5
docs are next touched, annotate the ordering; do not rewrite historical D5
commits or evidence.

## Deferred integration

- WP08 owner orchestration.
- WP10 actual target execution and production post-release FD5 status.
- Full G3 acceptance.
- Portability/ARM/Alpine/BSD expansions.
- No target execution, production status integration or release acceptance is
  authorized by D6.0.

## Completion prerequisites

H1-H5 may be closed only with the future owner's source-bound implementation,
real native/target evidence where applicable, independent review and evidence
preservation. A green D4/D5 fixture rerun does not close H1/H2 or any C item.
