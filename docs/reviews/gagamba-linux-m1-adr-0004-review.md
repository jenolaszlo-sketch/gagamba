# Independent ADR 0004 Review

## Revision binding

Repository review was bound to qualification/ar7-feasibility at c3cb16412cf136691280eb8f3fadedbe7dc451a9, whose parent is the WP05 blocker-preservation commit c48b4cd193616c7a2777ac664457183002f845a1. A read-only Git check confirmed the branch, HEAD, and clean working tree.
The ADR commit changes only the ADR and two status documents. The reviewed checkout contains no WP05 release implementation: the native coordinator remains release-disabled, and the managed path remains prepare-held/abort-only. WP04 and G2 are recorded as accepted; WP05 and WP10 remain unqualified.

## WP05 contract binding

The authoritative plan requires release/stop arbitration and truthful status reporting, while forbidding integrated target execution in WP05. It calls for fake resource/capture prerequisites, a no-target qualification coordinator, a release protocol/status reader, and a post-release native branch; WP10 owns actual fixture execution.
ADR 0004 preserves those execution boundaries. It does not authorize target execution. Its DeliveryResult=Delivered represents a successful full pipe write, consistent with the plan only if “ReleaseDelivered” means the owner observed that write return. It does not prove coordinator consumption.
There is a material completeness gap: the plan also requires durable ExecObserved and RootObserved results in the ownership snapshot so trusted observations can survive owner loss. ADR 0004 proposes no durable representation for those observations. Saying recovery can use “durable trusted evidence” does not identify where that evidence is encoded.

## Necessity of schema v3

The schema change is necessary. Version 2 has no release phase, release or stop deadlines, delivery result, or stop record; its decoder accepts only version 2. A parallel journal would evade the accepted writer fence and create competing durable state.

## Single-journal decision

Keeping release and stop evidence in the existing ownership journal is correct and is a major positive. It preserves one JournalWriterLease, one sequence, and one acknowledged snapshot lineage; recovery does not reconcile separate journals. The journal remains evidence, never release authority.

## v3 representation

The Intent/Committed tag and separate first-stop record are a workable compact base. SetupDeadline is needed for final release eligibility; OverallDeadline is needed for execution timeout and the release frame; the stop cause and deadline support bounded recovery. DeliveryResult narrows what the owner observed without claiming execution.
IntentAt and CommittedAt are mainly audit timestamps, not recovery decisions. In particular, CommittedAt cannot mean the exact time the observation became durable: the timestamp must be sampled before the record containing it is persisted. Define its sampling point, and do not use it to infer release/stop ordering. The durable schema also needs the plan-required trusted execution/root observations.

## Release identity

Reusing immutable RecordedPreparation is sufficient: execution, issuer, generation, preparation, and manifest digest identify the prepared transaction. A release record grants no authority; the single-use capability and writer are owner-local and are not reconstructed after owner death. A new nonce or UUID is not justified.

## ReleaseIntent safety boundary

The boundary is sound and intentionally conservative:
- Before an acknowledged intent, the live release path must not send; recovery may report NotCommitted.
- After intent, recovery reports MayHaveExecuted unless durable trusted observations narrow a result.
- Recovery never resends from intent-only state.
False-positive MayHaveExecuted is an acceptable cost for preventing duplicate release. The missing durable status representation matters because it prevents the promised trusted observations from narrowing that result after owner loss.

## Runtime versus durable linearization

The two boundaries are consistent. Runtime release wins at the in-gate Owned → ReleaseCommitted change with capability consumption; the earlier durable intent is the crash-safety boundary. Intent may exist without release winning. Recovery can safely treat it as MayHaveExecuted, and must not retry it.

## Release/stop arbitration

The gate correctly determines the runtime winner. A stop accepted before release commit forbids a write; a stop after commit cannot erase release history. With intent plus stop, recovery cannot infer the winner from timestamps or assume release was impossible.
A crash after the stop wins in memory but before its stop record is durable must still be classified from the last durable state. If that state contains intent, it remains MayHaveExecuted; if it has no intent, release remains impossible. The ADR needs to specify how competing out-of-gate journal writes preserve these semantics.

## Operation ordering

The proposed successful path matches the plan: validate, durably flush intent outside the gate, recheck stop/deadline/identity/prerequisites/capability inside the gate, consume the capability and make one nonblocking write, then persist the delivery result outside the gate.
The gate should not contain journal I/O. Keeping persistence outside it follows the plan and avoids blocking journal operations from delaying or serializing stop/release arbitration.

## Crash matrix

| Durable release | Durable stop | Runtime fact if knowable | Execution classification after owner loss | Recovery action |
|---|---|---|---|---|
| Neither | No | No durable release admission | NotCommitted; root NotStarted | Stop and identity-checked cleanup |
| Neither | Present (S0) | Stop won before durable intent | NotCommitted; root NotStarted | Stop and cleanup; no release |
| Intent (R1) | No | Gate may not have run, or may have committed | MayHaveExecuted; status Unknown absent durable observations | Kill/stop and clean; never resend |
| Intent (R1) | Present (S1) | Stop may have won before commit; durable order is insufficient | MayHaveExecuted; status Unknown | Preserve both records; no resend |
| Committed (R2) | No | Gate committed and one write was attempted | MayHaveExecuted; delivery per record, execution Unknown absent durable observations | Kill/stop and clean; never resend |
| Committed (R2) | Present (S2) | Release committed before later stop | MayHaveExecuted; preserve known durable observations | Stop and clean; preserve release history |

“Committed” without its required intent fields is not a valid distinct state: the Committed tag carries those fields. Truncated or inconsistent encodings must fail closed.

## DeliveryResult semantics

Delivered means the full 128-byte write returned successfully; it does not prove the coordinator read the frame, exec succeeded, or the target ran. WouldBlock, BrokenPipe, Partial, and Unknown likewise do not justify retry. The ADR preserves that distinction.

## Pipe atomicity

Linux documents PIPE_BUF as 4096 bytes. For an O_NONBLOCK pipe write no larger than PIPE_BUF, the write either transfers all requested bytes or fails with EAGAIN; therefore a 128-byte partial result is not expected for the specified transport. Partial may remain as a defensive anomaly, but should be defined as impossible under the qualified Linux pipe contract and handled conservatively without retry. pipe(7)

## No-resend invariant

The invariant is sound across owner restart, recovery, duplicate API calls, stale owner, and lost acknowledgement: the capability is consumed once, the coordinator closes its reader after accepting RELEASE, and recovery never reconstructs authority. This depends on implementation preserving the invariant through every retry path and journal update.

## Deadline/boot semantics

Version 3 inherits BootId from the v2 snapshot. The current implementation reads /proc/sys/kernel/random/boot_id, and recovery refuses cross-boot records before destructive adoption. Thus a v2-to-v3 extension need not add a second boot identifier.
CLOCK_MONOTONIC is system-wide, non-settable, and does not go backward, so wall-clock jumps cannot make an expired deadline appear valid. It is frequency-adjusted and pauses during system suspend; that is the existing plan’s chosen time base, not an ADR change. If the intended budget must include suspend time, the plan itself would need revision. clock_gettime(2)
The ADR should also tie StopDeadline to the plan’s five-second stop allowance and bounded final drain, rather than leave the bound implicit. Stop-cause values need a fixed durable encoding, and timestamp capture points should be stated. Release/stop winners must never be inferred by timestamp comparison.

## v2/v3 compatibility

Lazy v2→v3 encoding is preferable to rewriting historical records. Existing v2 meaning is preserved, and older v2 code rejects v3 rather than decoding it partially. The current journal resolver treats unsupported snapshots as uncertain, and writer acquisition fails closed rather than overwriting them.
Upgrade must occur under the same live writer fence and exact acknowledged baseline. The ADR’s “v2 Prepared becomes semantically equivalent v3” rule is sound if it copies the complete snapshot and initializes no inferred release evidence. Confirm the WP04 abort path does not begin emitting v3 stop records unintentionally; if it does, cover that behavior with a focused regression.

## Writer fencing

All v3 transitions should use the existing JournalWriterLease; the current journal enforces a live lease and exact next sequence. Recovery may write stop/cleanup only after pidfd-proven owner exit and fenced takeover; it never writes release.
The ADR says stop and release history is immutable, but it does not specify how concurrent intent, committed-result, and stop writes serialize or merge. Every writer must derive from the latest acknowledged snapshot under the fence, preserve the other transition’s fields, and fail closed on stale baselines. This needs to be made explicit before implementation.

## Record invariants

The ADR already covers important rules: release requires Prepared plus Preparation; Committed requires intent fields and a non-NotAttempted result; identity and deadlines are immutable; release cannot move backward or disappear; stop cause must be known; malformed or unsupported records fail closed.
Before implementation, specify the trusted status fields and their source/order validation, fixed on-disk stop-cause values, timestamp capture semantics, and the stop-deadline bound. Ensure persistence failures cannot permit release after a stop or erase a committed stop/release fact.

## Cleanup/result independence

Cleanup status is correctly independent of release disposition. Cleanup confirmation cannot convert MayHaveExecuted to NotCommitted; release and stop fields must survive domain/staging removal and owner/coordinator exit.
The ADR’s terminal-receipt copy rule preserves release, stop, and delivery results, but the omitted durable exec/root observations leave a gap in the plan’s requirement to preserve trusted status after owner loss and cleanup.

## Security analysis

The new fields introduce no reusable release authority. They add bounded evidence only. Identity reuse, single-use capability consumption, writer fencing, no-resend behavior, and refusal of cross-boot records fit the accepted trust model. The checksum detects corruption, not forgery; the ADR correctly retains the host-owned trusted-writer assumption.

## G2 impact

| Accepted area | Impact |
|---|---|
| Descriptor ownership, protocol framing/vector, recovery topology, capability locality, non-root pidfd topology | Unaffected by this schema proposal |
| Journal decoding/honesty, writer fencing, recovery classification, cleanup/result independence, identity validation | Focused regression and renewed evidence required |
| Accepted G2 native topology and unrelated qualification rows | No wholesale rerun unless a focused regression shows a dependency or counterexample |

A focused G2b review remains appropriate; the schema change does not by itself reopen all of G2.

## WP04 impact

No WP04 native or seccomp behavior changes are proposed. V2 records with no release/stop fields can remain v2, preserving accepted WP04 evidence. If WP04 abort/stop begins writing StopRecord, it will emit v3 and needs focused evidence for that behavior; the ADR should bound the change to WP05 semantics or state the regression requirement.

## Protocol/vector impact

The amendment adds no owner/coordinator wire fields or descriptor changes and does not alter the canonical 873-byte manifest vector. Future RELEASE/status protocol and native post-release changes remain separate implementation and qualification work; WP05 still must not execute the fixture target.

## Implementation authorization

The single-journal architecture is sound, but the ADR is not yet precise enough to authorize schema implementation. In particular, the missing durable execution/root observations conflict with the authoritative plan’s snapshot requirement. Adding those bounded observations within the same journal can resolve this without introducing another authority.
No source, tests, schema, documentation, or Git state were changed, and no tests were run.

## Findings

### Blocking

- None requiring replacement of the single-journal architecture.

### Required clarifications

- Specify durable, bounded ExecObserved/RootObserved (and any plan-required observation records) in the same snapshot, or obtain a reviewed authoritative-plan amendment. Terminal receipts alone do not satisfy recovery after owner loss before terminal publication.
- Define serialization/merge ordering for concurrent Intent, Committed, and Stop writes so monotonic facts cannot be lost and stale snapshots cannot overwrite a winning stop.
- Bind StopDeadline and timestamp meanings to the existing plan, and specify stable durable enum encodings.

### Non-blocking recommendations

- Keep Partial only as a defensive impossible-under-contract anomaly; treat it conservatively as unknown and never retry.
- Document that IntentAt/CommittedAt are audit timestamps and cannot decide a winner.
- Retain the explicit “delivered means owner write returned full length” wording throughout implementation and tests.

## Final disposition

ADR 0004 ACCEPTED WITH REQUIRED CLARIFICATIONS - UPDATE ADR BEFORE IMPLEMENTATION
