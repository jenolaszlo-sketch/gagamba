# Gagamba Linux M1 R6D.2 topology review (preserved)

- review type: independent R6D.2 topology review (before WP04)
- implementation under review: b3cbc979b022452dc528b8fbd84c7d1ad4470390
- docs/plan-only HEAD at review time: f6c91764176e898d63b9fd02adde86c8f87ec8f4
- branch: qualification/ar7-feasibility
- disposition: R6D.2 TOPOLOGY NOT ACCEPTED - CORRECTION REQUIRED
- blocker: B1 (wait-free reply peek in the protocol driver)
- test-side bug: B2 (`NonRootMissingPidfdRefusesBeforeOwnerTransaction` barrier value)
- G2 overall remains NOT ACCEPTED and WP04 remains blocked. The body below is the report verbatim.

# Independent R6D.2 Topology Review

Review-only; no modifications, fixes, or WP04 work. Author evidence (wp03 §22) was read after source inspection and independent execution, and only for comparison.

## Target binding

- Branch `qualification/ar7-feasibility`; HEAD `f6c91764176e898d63b9fd02adde86c8f87ec8f4` (docs/plan-only: `wp03.md`, `handoff.md`, `implementation-plan.md`, `work-queue.md`).
- Implementation under review: `b3cbc979b022452dc528b8fbd84c7d1ad4470390` (20 files; +2896/−2157).
- `git diff b3cbc97 HEAD -- src/ tests/` empty; tree clean before/after; no WP04 source added (no Bubblewrap/target/coordinator/FD-map); no target executed anywhere in this review.

## Topology disposition

**Not accepted for qualification** — one concrete driver blocker (plus one test-side bug). The underlying owner-owned topology is otherwise structurally sound in every dimension checked, and the blocker has a narrow, located correction.

## Before/after process ownership

Before (R6D): scope ran `AllocatePreparation`/`AllocateDomainUnderTransaction` locally — transaction authority lived recovery-side. After (R6D.2): those scope methods are deleted (reflection-pinned by test); the single authoritative production path is `OwnerPreparationTransaction.Run` in the owner process. The scope only writes the initial intent, releases the fence, spawns the owner, and retains/acknowledges recovery authority through the protocol. Verified by call-site sweep: the sole `TryCreateDomain` production caller is the owner transaction.

## Transaction path

Owner-side, in order: Start/intent+identity match → acquire exclusive writer → boot check → `TryProveAllocationIntent` → WriterReady → receive StartAuthority (2 fds, identity-checked incl. staging-parent dev/ino) → context → real domain creation → send 7-fd recovery set → await retention ack → durable AllocationRegistered → registration ack → staging transfer/retain/account/proceed handshake → SafeCopyIn with retained staging-parent authority → staged descriptions → manifest authorization (scope-built, identity-bound) → durable Prepared → description ack → after-prepared-before-mint barrier → owner-local Mint → Completed (description only) → wait for scope lifetime (EOF ends owner). The scope cross-checks durable journal state against retained authority at registration, staging, and Prepared (`IsExpectedRegistered`, `HasDurableStagingDirectory`, `MatchesPrepared`) and aborts on any mismatch/timeout.

## ScopedAllocationContext provenance

The R6D softness is closed: `TryCreate` now requires the exact expected intent plus `TryProveAllocationIntent`, which demands this journal's effective live writer and field-equality of the acknowledged snapshot (execution, lease generation, boot, AllocationIntent stage, domain name, writer kind, cleanup, staging-parent identity, full allocation list, no Preparation). Wrong execution/generation, unrelated records, stale/foreign writers, and missing intents all refuse. `IsLive` is now fence-backed (`Journal.IsEffectiveWriter`).

## Writer lifecycle

Scope writes intent → disposes its writer → spawns owner → owner acquires → owner mutates → scope cannot acquire while live → owner death releases the real fence → scope takes over post-exit-proof from acknowledged state. Verified live: exclusion errors while live (twice), kill, exit proof, takeover acquiring with baseline continuing from the acknowledged sequence. No two-usable-lease interval by construction plus flock. G2-05 preserved.

## Domain authority transfer

Owner creates the real domain (live-verified: real cgroup child, ino opening, mount 356) and sends an independently-duplicated 7-fd recovery set; scope validates via the accepted `TryValidateDomainHandles`, retains dups, and acks; the owner advances only after the ack, and stops on failure/timeout/invalid transfer. Handle sets are independently owned/disposable (G2-01/09 intact). Send-without-ack is refused by protocol order.

## Staging authority transfer

Owner creates → captures identity → transfers fd+dev/ino → scope stats/compares/retains → acks → owner durably accounts → scope verifies durable accounting → MayProceed → only then object copy. Verified live, including "no copy before proceed". Malformed/wrong-identity/unretained transfers throw or fail closed before copy; no path fallback (retained-handle dup or refusal); duplicates refused (`stageDirectoryAuthorized` guard + sequencer).

## Capability locality

Store created and Mint executed owner-side only; scope receives `OwnerPreparedDescription` (5 ID/digest fields, no handles) and staged descriptions (role/digest/length/identity/names, no handles). Manifest travels scope→owner as JSON values. G2-06 preserved structurally; no capability crosses, mirrors, or resurrects scope-side.

## Owner protocol

19 message types, magic `M1OP`, version 1, per-frame exec/gen binding, monotonic per-direction sequencing with exact-type+sequence acceptance (duplicates, out-of-order, stale, wrong-identity, oversized all refuse; bounds: 60 KiB payload, header+payload frames). Transfers reuse the exact-count `FdPassing` path with close-on-any-rejection. Sends use `MSG_NOSIGNAL`; receives use `MSG_CMSG_CLOEXEC`. G2-02 intact: `SOCK_SEQPACKET` unchanged, overlong/CTRUNC/extra-descriptor refusals and F1 zero-length cleanup present in both transfer and control receives. The channel is a longer-lived private inherited socketpair bounded by the explicit protocol (no pathname, no reconnect, CLOEXEC parent end, EOF close semantics both directions, dies with the transaction; cannot reach WP04 — no target exists).

## pidfd/lifecycle

Owner is a direct child; pidfd acquired immediately with CLOEXEC check (missing → terminate+refuse); retained per-owner; used for exit observation and single reaping; domain stop stays cgroup-based. Scope Dispose closes channels (owner EOF-exits), waits boundedly, terminates only on failure. D1-D6 kill/reap mechanics are structurally possible.

## D1-D6 barrier readiness

| Barrier | Owner state | Durable state | Recovery authority already retained | Honest kill point? |
|---|---|---|---|---|
| after-intent | writer held, intent proven | AllocationIntent ack'd | none yet (intent only) | yes — takeover from intent |
| after-domain-retained | domain created, recovery set sent | intent | validated domain dups | yes |
| after-registered | registered written | AllocationRegistered + identity | domain dups | yes |
| after-staging-accounted | dir accounted | staging identity confirmed | domain dups + staging dir fd | yes (decisive D4 shape) |
| mid-staging | 1 object accounted | partial objects | as above | yes (D5 shape) |
| after-copy | copy complete, not Prepared | copy complete | as above | yes |
| after-prepared-before-mint | Prepared ack'd, store absent | Prepared | all | yes — Mint owner-local, scope holds description data only (D6: kill here cannot create or resurrect any scope capability) |

All barriers sit in the real owner path with `TestBarrier` allowlisted in the spec codec. Corresponding real-death tests exist (`RealOwnerDeathAtPreparationBarrierLeavesRecoverableBoundary`, 7 barriers) but cannot pass yet — see blocker.

## Plan/ADR consistency

One paragraph of plan §10.1 rewritten to describe the implemented owner-owned flow (scope records intent, releases writer, sends request+descriptors; owner proves intent, creates domain/preparation, transfers recovery sets; scope retains/acks; writer takeover only after pidfd-proven exit; owner alone holds primary lease + store). ADR 0003 untouched. The change reconciles wording with the approved topology actually built; it states no WP04 path is implemented. Legitimate clarification, not an architecture change.

## Linux-native evidence

Pristine `git archive` export, root: protocol unit tests 6/6; full M1 317 passed + 10 failed + 10 skipped. All 10 failures are owner-process transaction tests failing identically at the first exchange — the blocker below, not environmental. Independent full-transaction probe (patient peer, production code both sides): 24/24 PASS — domain creation, both authority splits with acks, registration/staging durable matches, ordering, writer exclusion, kill, takeover baseline continuation.

## Non-root evidence

uid/gid 1000 (`gaga`), all caps zero, NoNewPrivs 0, kernel 6.6.114.1-microsoft-standard-WSL2, `gaga-delegated` (cpu memory pids) unprovisioned. Full topology probe: 24/24 PASS (real domain ino=327705, both splits, ordering, exclusion, kill, takeover). Repo `NonRootQualificationTests`: 2/9 — 6 fail on the same peek blocker, 1 on the test bug below; preflight/missing-delegation pass.

## Accepted-boundary regressions

112/112 sampled green (Journal, Fencing, DomainAuthority incl. R6B.1 F1/F2, Reconciler, Protocol); G2-07 preparation tests green in the full suite. No accepted finding regressed. (One removed G2-01 domain-sentinel test variant noted; pidfd-number-reuse sentinel retained; disposals idempotent — observation only.)

## Findings

**B1 (blocker) — wait-free reply peek makes the protocol driver unable to complete any transaction.** `M1RecoveryScope.ReadNextRecordLength` (`M1RecoveryScope.cs:716`, called at lines 355, 433, 511, 602) issues `FIONREAD` with no wait immediately after sending. The owner needs ~100 ms (measured) to answer; the peek reads 0 and fails with "owner sent a record outside the protocol frame bound", aborting the transaction. Reproduced: all 10 owner-process tests fail identically; diagnostic shows the owner alive with Start→WriterReady→StartAuthority accepted and the 7-fd reply arriving 100 ms later, received cleanly with a patient wait. Consequence: no owner transaction can complete, so no barrier is reachable and D1-D6 cannot be honestly executed. Minimum correction: bounded wait for readability (reuse the 30 s transition-timeout semantics, e.g. the existing poll path) before the `FIONREAD` peek at all four sites; keep the bound and length checks unchanged. Nothing else in the topology needs to change — the patient-peer probe proves the remainder works.

**B2 (test-side bug) — `NonRootMissingPidfdRefusesBeforeOwnerTransaction` passes `"none"` as `TestBarrier`, which the new spec codec allowlist rejects** (`NonRootQualificationTests.cs:283`), so it fails with "Unknown owner test barrier" instead of reaching pidfd refusal. Minimum correction: pass null for no-barrier (the suite's convention), or explicitly map `"none"`→null in the codec.

**Observations (not blockers):** `WithholdStagingRetentionAckForTest` is a test-only knob inside the production flow (fail-safe direction — it withholds an ack); the removed domain-lease fd-reuse sentinel variant narrows G2-01 sample coverage (pidfd variant retained).

## Remaining global status

- G2-03 remains blocking pending actual D1-D6 kill/recovery qualification (structural 1A/1B + owner-owned topology are progress, not acceptance)
- G2-10 remains blocking; G2-11 maintenance; G2 NOT ACCEPTED; WP04 BLOCKED.

R6D.2 TOPOLOGY NOT ACCEPTED - CORRECTION REQUIRED