# Independent WP05 V3 Durable Schema Final Acceptance Review

Date: 2026-10-17 (Asia/Manila)

Reviewer: Luna (independent final acceptance reviewer)

## Revision binding

| Role | Exact revision |
|---|---|
| Branch | `qualification/ar7-feasibility` |
| Accepted pre-implementation baseline | `84a6aac92647b1c09bd729fe6171d62b34063047` |
| Production under qualification | `c075841bcccc31e186719c9e81261285332fe8ac` |
| Schema/codec | `3ffb843f5382e914b7d51325644424f98a361c00` |
| Transitions/recovery | `959bac1fad1c3ec8a5a5834ceb94712461f60edb` |
| Observation idempotency/negative controls | `21a04a1d35d3eea4d81c388903e30bdc7c85b921` |
| Cleanup-state monotonicity | `e004bc4b12d20fbf674c2aef3f0adaed205b0bb8` |
| Implementation evidence/status | `fa093a9`, `a16c2c9`, `c922eda`, `c075841` |
| Independent qualification probes | `df7522e4c20f33a5e9879eb56a5744598a3a6642` |
| Evidence completion/composite record | `43e9c21231bc155bf55c713b9098c8c668557d39` |
| ADR blocker preservation | `c48b4cd193616c7a2777ac664457183002f845a1` |
| Original ADR 0004 | `c3cb16412cf136691280eb8f3fadedbe7dc451a9` |
| Initial partial review preservation | `fa75f31` |
| Clarified ADR 0004 | `a7738f07bcaacb86e356ab0f982262b53e26065d` |
| Complete ADR-review preservation | `46a1991b57edb410d53ac7fc0c6b43ba815a6792` |
| Clarification rereview | `f1bb62b` |
| ADR/WP05 acceptance status | `84a6aac92647b1c09bd729fe6171d62b34063047` |

The final review binds to the exact full SHAs above. The branch is linear from
the accepted baseline through the production revision and the qualification-only
commits.

## Qualification provenance

- **Luna** independently performed the principal source review, Linux non-root
  qualification, journal-fence/owner-protocol checks, five independent hostile
  probes, and the full Windows run, until execution quota was exhausted.
- **DeepSeek Pro** is the implementation author of the v3 tranche and later
  completed the remaining objective/mechanical evidence checks. DeepSeek Pro is
  explicitly the **evidence completer, not the independent final acceptor**.
- **Luna** independently audited the completion results and repeated selected
  checks before this acceptance. The completion document's mechanical checks are
  attributed to DeepSeek Pro; they are not presented as Luna's execution.

The composite record at
[gagamba-linux-m1-wp05-v3-schema-qualification-record.md](gagamba-linux-m1-wp05-v3-schema-qualification-record.md)
separates Part I (Luna qualification) from Part II (DeepSeek Pro evidence
completion). The completion document is
[wp05-v3-qualification-evidence-completion-2026-10-17.md](../evidence/linux-m1/wp05-v3-qualification-evidence-completion-2026-10-17.md).

## Scope

Accepted: the WP05 v3 durable-schema foundation only — `OwnershipSnapshot` v3,
durable enum encoding, v2 compatibility, v3 codec, lazy v2→v3 transition,
`ReleaseRecord`, `StopRecord`, `ExecObservation`, `RootObservation`, invariant
validation, monotonic full-snapshot transitions, stale-baseline refusal, existing
G2 writer fencing as applied to v3, recovery classification, and cleanup/result
independence.

Not accepted: release transport; runtime release/stop arbitration wiring;
coordinator `RELEASE`; post-release coordinator status; native post-release
branch; owner release orchestration; actual capability consumption; target
execution; integrated WP05 process-loss behavior; full WP05; WP10.

## Production freeze

The worktree was clean at `c075841` before qualification artifacts. The
qualification-only probe and evidence/composite/documentation commits after
`c075841` change no production file. `git diff c075841..HEAD` is limited to the
test probe and documentation. There is no post-`c075841` change to recovery
production behavior, lifecycle production behavior, native coordinator,
Bubblewrap integration, owner protocol, coordinator protocol, descriptor map,
release FD, seccomp, manifest wire format or target fixture.

## V2 compatibility

Version 2 remains accepted and unchanged in meaning: `Release=null`, `Stop=null`,
`Exec=null`, `Root=null`; `Stage`, execution identity, `BootId`, mount/domain
identity and allocations retain their v2 values. No v3 fact is inferred. The
ordinary WP04 pre-release constructors explicitly use `LegacyVersion`; the
encoded version-2 marker remains unchanged. Unsupported versions fail closed.

## V2 golden record

The representative v2 record golden SHA-256 is
`5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE`. It was
derived from `SHA256(Encode(v2Snapshot))` by both the implementation codec test
and the independent probe, not merely copied from a stored file. The same value
was independently recomputed during this audit.

## V3 schema

Version 3 adds only optional `ReleaseRecord`, `StopRecord`, `ExecObservation` and
`RootObservation` after the unchanged v2 fields. `RecordedPreparation` remains
the release identity. There is no release nonce, no release UUID, no second
journal, no second `BootId` and no persisted bearer release capability.

## Durable enum encoding

Explicit stable values are used throughout:

| Enum | Values |
|---|---|
| `ReleaseTag` | `0=Absent`, `1=Intent`, `2=Committed` |
| `DeliveryResult` | `0=NotAttempted`, `1=Delivered`, `2=WouldBlock`, `3=BrokenPipe`, `4=Partial`, `5=Unknown` |
| `ExecTag` | `0=NotAttempted`, `1=FailedBeforeExec`, `2=Confirmed`, `3=Unknown` |
| `ExecBasis` | `0=None`, `1=CoordinatorFrame`, `2=ExecutableObjectIdentity`, `3=ReviewedNormalExitRule` |
| `RootTag` | `0=NotObserved`, `1=Observed` |
| `RootKind` | `0=Unknown`, `1=NotStarted`, `2=Exited`, `3=Signalled` |
| `StopCause` | existing durable values `0–7` |

Unknown/reserved values fail closed. Compiler ordinal defaults are not relied on.

## ReleaseRecord

`Intent` carries `IntentAt`, `SetupDeadline`, `OverallDeadline`. `Committed`
preserves those fields and adds `CommittedAt` and `DeliveryResult`. Committed
cannot exist without intent data; Committed + NotAttempted is invalid; release
cannot regress, disappear, or change identity/deadlines.

## StopRecord and takeover deadline

`StopRecord` carries `StopAcceptedAt`, `StopCause`, `StopDeadline`, with
`StopDeadline = StopAcceptedAt + 5_000_000_000 ns`. The first durable stop is
immutable and later writes preserve it. Same-boot recovery takeover uses the
persisted deadline and cannot grant itself a new now+5s allowance. The one-second
final drain remains separate runtime behavior. Deadline expiry only limits
teardown and cannot authorize release or resend.

## Exec observation

Durable values: `NotAttempted`, `FailedBeforeExec(errno)`, `Confirmed(basis)`,
`Unknown`. Bases: `CoordinatorFrame`, `ExecutableObjectIdentity`,
`ReviewedNormalExitRule`. Observations require `ReleaseTag=Committed`;
`Intent + observation` is invalid; observations are single-assignment; identical
duplicates are idempotent no-ops; contradictory observations and fact
disappearance are refused.

## Root observation

Durable values: `NotObserved`, `Unknown`, `NotStarted`, `Exited(code)`,
`Signalled(signal, core)`. `Exited`/`Signalled` require `Exec=Confirmed`;
`NotStarted` requires `FailedBeforeExec`; root `Unknown` requires `Exec=Confirmed`
or `Exec=Unknown`. Invalid combinations fail closed.

## Observation authority

The coordinator is the sole target/reaper observation source; the owner validates
trusted coordinator status and persists validated evidence under its writer
lease. The owner is not itself the target/reaper oracle. Observations do not
derive authority from stdout/stderr, Bubblewrap JSON or caller-provided bytes.
They are evidence, not release authority.

## Delivery/observation compatibility

Production validation enforces that definite failed delivery (`WouldBlock`,
`BrokenPipe`, `Partial`) cannot coexist with `Exec=Confirmed`,
`FailedBeforeExec`, or a known root observation. `Unknown` delivery may coexist
with later validated trusted status where ADR 0004 permits it. This is enforced
in production validation, not merely tests. `Committed + Exec Confirmed + Root
Unknown` is explicitly accepted and classifies as release `MayHaveExecuted`,
root/outcome `Unknown`; it is never converted to NotStarted, success or target
failure.

## Partial/Delivered semantics

`Partial` is a defensive impossible-under-qualified-contract anomaly: never a
retry, never success, conservatively classified, and qualification-worthy if
observed. `Delivered` means only the full 128-byte owner pipe write returned
successfully; it does not imply coordinator read, exec success, root
observation, target initialization or execution.

## Lazy v2→v3 transition

The encoder emits v2 when no new fact exists and v3 on the first WP05 fact. The
transition preserves the complete snapshot identity, `BootId`, preparation state,
lease generation, allocation inventory and all existing facts, and adds no
implicit stop, observation or release intent. The independent probe verifies the
upgrade with structural preparation-digest comparison.

## Monotonic full-snapshot transitions

All v3 durable updates hold the live `JournalWriterLease`, start from the latest
acknowledged snapshot, validate baseline/lineage, preserve every existing
monotonic fact, add exactly the authorized release/stop/observation fact (or
cleanup-only progress), and use the exact next sequence. There are no independent
field writes, last-writer-wins merges or parallel release/status journals.

## Stale-baseline qualification

Stale updates cannot overwrite newer intent, committed release, delivery, stop,
exec/root observations or cleanup progress. Stale baseline, wrong sequence, dead
or foreign lease, changed lineage, version regression and fact disappearance all
fail closed. The implementation tests and the independent
`StaleSnapshotsCannotOverwriteAnyAdvancedDurableFact` probe pass.

## Writer fencing

The existing `JournalWriterLease`/`flock` fence is retained; no new fencing
mechanism was introduced. Linux `JournalFencingTests` pass 6/6, and the full
non-root Linux run executed the fence family without a skip. No G2 counterexample
was produced.

## Recovery classification

The pure classifier returns: no release → `NotCommitted` with appropriate
pre-exec state; Intent/Committed → `MayHaveExecuted`; FailedBeforeExec → root
`NotStarted` and failed establishment when complete trusted evidence supports it;
Confirmed + Root Unknown → `Unknown`; Confirmed + Exited/Signalled → known root
subject to separate domain-empty/capture/stop conditions. Cleanup is a separate
result dimension.

## Cleanup/result independence

Cleanup cannot erase release intent, committed release, delivery, stop, exec or
root facts. `CleanupConfirmed` may coexist with `MayHaveExecuted`; cleanup success
never rewrites execution history to `NotCommitted`. The production test and the
independent probe both pass.

## WP04 regression

Ordinary WP04 pre-release snapshots remain version 2 and use the explicit
`LegacyVersion`; HELD remains pre-release; abort remains legacy/v2; no
`ReleaseRecord`, WP05 `StopRecord`, exec or root observation is introduced. The
focused WP04/journal Windows selection passed 64/64; WSL owner protocol 21/21;
the full non-root Linux run executed the owner-protocol family without a skip.
No WP04 counterexample was reported or observed. The shared reconciliation
cleanup writer now uses latest-snapshot `WriteMutation`; cleanup decisions and
HELD/abort behavior are unchanged. Native WP04 HELD/owner-death tests were not
rerun because no native WP04 behavior changed.

## G2 focused renewed evidence

Focused renewed evidence exists for v3 codec honesty, writer fencing,
stale-baseline refusal, recovery classification, cleanup/result independence and
identity validation. G2 is not reopened wholesale; no counterexample was found.
Descriptor ownership, native protocol topology, capability locality, pidfd
topology and the canonical manifest remain unaffected.

## Canonical vector

`ManifestTests.CanonicalGoldenBytes` passed 1/1: 873 bytes, SHA-256
`82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`. The manifest
codec and wire format are unchanged.

## Native/protocol/seccomp freeze

The v3 work changed no native coordinator, Bubblewrap adapter, owner protocol,
coordinator protocol, descriptor map, release-FD behavior, manifest wire format or
target fixture. The pinned seccomp artifact decodes to 672 bytes with SHA-256
`4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc`; this is a
freeze check, not a seccomp requalification. The deferred
wrong-audit-architecture seccomp-generator decision remains deferred.

## Test totals

| Check | Result |
|---|---|
| Windows full Linux-M1 | 322 passed, 156 skipped, 0 failed / 478 |
| Non-root Linux full assembly (UID 1000) | 405 passed, 73 skipped, 0 failed / 478 |
| Independent hostile probes | 5/5 |
| Luna focused Linux non-root | v3 38/38, classification 11/11, fencing 6/6, owner protocol 21/21 |
| Focused WP04/journal Windows | 64/64 |
| Canonical manifest | 1/1; 873 bytes / `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3` |
| Full Release build | 0 warnings, 0 errors |
| V2 golden | `5701CD4E2545CBF7FC8382D11B2F66C19D8C7C4D14592F00547BE68331EAF7AE` |
| Seccomp freeze | 672 bytes / `4ee63c48010d74118f74ddec44b48acc16cabc3e7e5693e83d941f82bbb3a2bc` |

## Skip classification

The 156 Windows skips are pre-existing Linux-native/environment families
(`NonRootQualificationTests`, `NativeCopyInTests`, `CoordinatorMountVerifierTests`,
`DomainAuthorityTests`, `IntegratedBarrierTests`, `NativeStagingCleanupTests`,
`OwnerProtocolTests`, `OwnerDeathQualificationTests`, `JournalFencingTests`,
`SeccompFilterPinTests`, `PidFdChildReaperTests`, `RecoveryHarness`). No WP05-v3
test is skipped, and no required v3 property is hidden behind a skip. The
non-root Linux full run executed the v3, journal-fencing and owner-protocol
families; no such family was skipped there.

## Limitations

This acceptance does not prove actual release-pipe correctness, release/stop
runtime arbitration, actual single-use release-capability consumption,
coordinator `RELEASE` parsing/consumption, native post-release behavior,
post-release status transport, target initialization/execution, integrated
process-loss behavior after real release, full WP05 or WP10. The deferred
wrong-audit-architecture seccomp-generator decision remains deferred.

## Findings

No blocking product, ADR, G2 or WP04 defect. Non-blocking evidence-coverage notes
are recorded in the composite record: the independent v2→v3 probe's allocation
assertion uses an empty allocation inventory (allocation preservation is
structurally enforced by the full-snapshot transition and `IdentityEqual`), and
the shared reconciliation cleanup writer now uses the latest-snapshot
`WriteMutation` path without changing HELD/abort or cleanup decisions. Native
WP04 HELD/owner-death tests were not rerun because no native WP04 behavior
changed.

## Final disposition

**WP05 V3 SCHEMA ACCEPTED - RELEASE PROTOCOL IMPLEMENTATION MAY PROCEED**
