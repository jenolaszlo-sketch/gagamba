# Independent ADR 0004 Clarification Rereview

Reviewer: DeepSeek (independent rereview)
Date: 2026-10-17 (Asia/Manila)
Scope: narrow rereview of the clarified ADR only. No source, test, schema,
migration, native, release or target work was performed.

## Revision binding

| Item | Revision |
|---|---|
| Branch | `qualification/ar7-feasibility` |
| Original ADR 0004 | `c3cb16412cf136691280eb8f3fadedbe7dc451a9` |
| Preceding blocker preservation | `c48b4cd193616c7a2777ac664457183002f845a1` |
| Partial review preservation | `fa75f31` |
| Complete review preservation | `46a1991b57edb410d53ac7fc0c6b43ba815a6792` |
| Clarified ADR 0004 | `a7738f07bcaacb86e356ab0f982262b53e26065d` |
| Working tree at review | clean |
| WP04 / G2 | accepted, unchanged |
| WP05 / WP10 | unqualified; no release, schema implementation, migration, coordinator RELEASE or target execution |

The clarified diff touches only documentation: ADR 0004, `docs/handoff.md`,
`docs/work-queue.md`, plus the earlier review-preservation file. There is no
source, test, native, migration or coordinator implementation in the diff.

Stage A correction: the complete Luna review is now preserved at
`46a1991b57edb410d53ac7fc0c6b43ba815a6792`, replacing the partial
`fa75f31` content without rewriting that historical commit. The earlier
partial-preservation note in this rereview is superseded. The complete review
contains exactly the three required clarifications evaluated below and no
additional blocking finding; the rereview disposition is unchanged.

Post-preservation verification: the complete review confirms the existing
architecture acceptance, the three required clarifications, the
`Delivered`/`Partial` semantics, the no-resend rule, the WP04 v2 boundary, the
G2 focused-evidence split, and the requirement that a focused G2b review remain
appropriate. The clarified ADR resolves all three required clarifications. No
new architecture defect is introduced by the completed review preservation.

## Original review requirements

Luna accepted the single-journal v3 architecture, the existing writer fence,
`RecordedPreparation` as release identity, durable `ReleaseIntent` as the
conservative safety boundary, the runtime gate winner, no resend, no bearer
authority, lazy v2/v3 compatibility, and the WP04/G2 boundaries. Luna required
three clarifications:

1. durable bounded `ExecObserved`/`RootObserved` in the same ownership snapshot;
2. serialization/merge ordering so concurrent intent, committed, stop and
   observation writes cannot lose monotonic facts or let stale snapshots
   overwrite newer facts;
3. precise `StopDeadline`, timestamp and stable durable-enum semantics.

## Durable exec/root observations

The clarified ADR now defines:

- `ExecTag`: `NotAttempted`, `FailedBeforeExec(errno)`, `Confirmed(basis)`,
  `Unknown`.
- `ExecBasis`: `CoordinatorFrame`, `ExecutableObjectIdentity`,
  `ReviewedNormalExitRule`.
- `RootTag`: `NotObserved`, `Observed`.
- `RootKind`: `Unknown`, `NotStarted`, `Exited(code)`,
  `Signalled(signal, core)`.

The required invariants are present:

- `Exited`/`Signalled` require `Exec=Confirmed`.
- `NotStarted` requires `FailedBeforeExec`.
- Root `Unknown` requires `Exec=Confirmed` or `Exec=Unknown`.
- Exec/root observations require `ReleaseTag=Committed`.
- `ReleaseTag=Intent` plus any observation is invalid.
- Observations are single-assignment/monotonic; contradictions fail closed.

This satisfies the plan's durable `ExecObserved`/`RootObserved` requirement. The
model mirrors the plan's `ExecDisposition` and `RootResult` vocabulary and
distinguishes a recorded root `Unknown` (with optional diagnostic raw wait) from
no root observation.

Non-blocking completeness note: the recovery matrix should also show
`Committed + Exec Confirmed + Root Unknown`; its classification is the same as
the existing `Committed + Exec Confirmed + root not observed` row (`Exec
Confirmed`, root `Unknown`, outcome `Unknown`, no resend). The schema already
permits the combination and the result is unambiguous.

## Observation trust and authority

The ADR binds observations to the trusted coordinator status pipe (FD 5), the
coordinator's validated status sequence, and the sole target/reaper relationship.
It requires magic/version/kind/length/sequence, execution/preparation/digest
identity and plan-order validation. It excludes stdout/stderr, Bubblewrap JSON,
caller bytes and untrusted target assertions.

Observations are evidence only: they cannot authorize release, recreate a
capability, authorize retry or override writer fencing. Only the owner persists
them under the live lease; recovery reads durable observations and never invents
them. A crash after observing but before persisting loses the observation, and
only durable evidence narrows recovery truth.

Non-blocking precision note: the phrase "the owner reads ... the target/reaper
relationship" should be implemented as the owner validating the trusted
coordinator's sole-reaper/executable-object report, not as the owner becoming a
target reaper. The plan gives the coordinator the target-reaper role; the owner's
own reaper is the wrapper/helper reaper. The implementation tranche should make
that ownership explicit in code and tests, but the ADR's trusted-source intent is
sound and does not create a new authority path.

## Recovery classification

The clarified ADR keeps `ReleaseDisposition.MayHaveExecuted` after durable
`Intent`/`Committed` after owner loss. Observations narrow execution/root/outcome
fields, not the release disposition. `FailedBeforeExec` can establish root
`NotStarted`; `Confirmed` plus `Exited`/`Signalled` establishes the known root
and the plan's `Succeeded`/`TargetFailed`/`Stopped`/`Unknown` conditions; raw
wait alone remains diagnostic when protocol evidence is insufficient; cleanup
`Confirmed` never overwrites execution history. The matrix is truthful and
field-separated.

## Journal serialization and merge

The ADR now requires one live `JournalWriterLease`, the latest acknowledged
snapshot as baseline, preservation of every existing monotonic fact, exactly the
next sequence, stale-baseline/lineage failure, full-snapshot transitions and no
last-writer-wins field merging. Intent/stop A/B/C and committed/stop cases are
explicit; observation writes preserve release/stop facts and later cleanup/stop
writes preserve observations. This resolves Luna's out-of-gate persistence
concern within the accepted trusted-writer model. The ADR honestly records the
residual: the journal is not append-only, so a deliberately faulty trusted
writer could clear a fact; that is a product defect outside the stale-writer
model.

## StopDeadline and timestamps

`StopDeadline = StopAcceptedAt + 5_000_000_000 ns` is the plan's five-second stop
confirmation allowance. The final drain is a separate one-second runtime bound
after domain-empty or stop-allowance exhaustion, whichever comes first; it is not
the `StopDeadline` and is not persisted as a second deadline. Expiry only bounds
teardown and never authorizes release or resend.

`IntentAt`, `CommittedAt` and `StopAcceptedAt` are sampled before persistence and
are audit/diagnostic only. They cannot determine the release/stop winner. No
separate observation timestamp is added; the journal sequence and audit trace
carry ordering.

Non-blocking precision note: the `StopDeadline` formula says "first in-gate stop
acceptance" while the field table also permits the recovery writer. Implement the
formula as the first authoritative stop acceptance (owner gate or recovery
takeover); the five-second allowance applies in both cases. This is a wording
precision, not a change to the accepted deadline semantics.

## Stable durable encodings, partial delivery, Delivered, boot

Explicit numeric encodings and reserved ranges are present for `ReleaseTag`,
`DeliveryResult`, `StopCause`, `ExecTag`, `ExecBasis`, `RootTag` and `RootKind`;
unknown values fail closed and compiler ordinals are not used. `Partial` is a
defensive impossible-under-contract anomaly for the 128-byte write within
`PIPE_BUF`, never retried, conservatively classified and qualification-worthy.
`Delivered` means only full 128-byte owner pipe-write success, not coordinator
consumption, exec, root, initialization or execution. v3 inherits the existing
`BootId`; same-boot deadlines only, cross-boot records exercise no release
authority, and no second boot identifier is added.

Non-blocking implementation invariant: the trusted-source model implies that a
definite failed delivery (`WouldBlock`, `BrokenPipe`, `Partial`) cannot coexist
with `Confirmed`/`FailedBeforeExec` or a known root. `Unknown` may coexist with a
validated coordinator status because the status itself proves frame receipt. The
ADR's general "contradictory combinations fail closed" and trusted-source rules
cover this; the first implementation tranche should enumerate the exact
combination in its invariant validation and tests.

## WP04, G2 and first implementation tranche

WP04's ordinary HELD/abort remains v2; `StopRecord` is introduced by the WP05
arbiter path, not silently by the existing WP04 abort. If a later implementation
routes WP04 abort through the WP05 arbiter and emits v3, focused WP04 regression
evidence is required. G2 keeps the correct split: focused renewed evidence for v3
encoding/decoding, journal honesty, writer fencing, stale-baseline refusal,
recovery classification, cleanup/result independence and identity validation;
descriptor ownership, native protocol topology, capability locality, pidfd
topology and the 873-byte canonical vector remain unaffected. G2 is not reopened
wholesale.

The first implementation tranche is correctly bounded to the v3 data model,
explicit enums, codec/v2 compatibility, invariant validation, monotonic snapshot
helpers, release/stop/exec/root records and focused journal/writer-fence tests. It
excludes release transport, coordinator RELEASE, native post-release code, owner
release orchestration, target execution and WP05 qualification.

## New contradictions

No new blocking architecture contradiction was found. The clarification does not
create a bearer release authority, does not let observations authorize release,
does not let recovery invent observations, and does not reinterpret v2 release
semantics. The non-blocking notes above are precision/completeness items for the
implementation tranche, not reasons to reopen the accepted architecture.

## Final disposition

**ADR 0004 ACCEPTED - WP05 V3 SCHEMA IMPLEMENTATION MAY PROCEED**
