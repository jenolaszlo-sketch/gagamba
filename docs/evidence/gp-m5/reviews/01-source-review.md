# GP-M5 macOS capture source review (preliminary)

Date: 2026-10-16. Review scope: dirty working-tree source on branch `codex/gp-m5-macos-capture`, HEAD `5d73ca28cc62d7ad360355a15a5910438251262a`. The implementation and qualification harness reviewed here were uncommitted; therefore this report binds to the reviewed diff plus that HEAD, not to a clean source SHA. Baseline source and native FIFO-topology evidence remain recorded in [contract.md](../contract.md) and [baseline.md](../baseline.md). This is an independent source/lifecycle review, not native qualification. Native implementation run was pending at review time.

## Disposition

No confirmed source-level deadlock, descriptor-reuse race, unbounded output accumulation, or bypass of launchd dispatch was found in the reviewed path. The design starts both drains against pre-opened reader/keeper pairs before launchd bootstrap/kickstart, drains beyond retention limits, serializes `read(2)` against descriptor disposal, and reports incomplete output separately from lifecycle completion. This supports proceeding to the pending native run, subject to the qualification boundaries below. Do not mark GP-M5 native-qualified from this source review.

## Findings

| ID | Status | Finding |
| --- | --- | --- |
| R1 | Addressed in current diff; native run pending | Partial FIFO setup now has a deterministic native regression. It pre-creates the second endpoint as a regular-file sentinel, verifies the first FIFO is removed, verifies no captured marker was dispatched, and then reuses the preparation for a successful ordinary launch. This tests the pre-admission setup-failure invariant rather than only invalid option validation. |
| R2 | Source assessment passed; native run pending | Both stdout and stderr drains are created before launchd dispatch. Retention is bounded by the shared stream and aggregate budgets; excess bytes continue draining and signal overflow. The qualification harness includes finite simultaneous stdout/stderr pressure. Actual ARM64 execution remains required to establish the Darwin FIFO behavior and absence of deadlock. |
| R3 | Source assessment passed; native run pending | Reader handle use and disposal are guarded by the same lock, and async polling uses nonblocking reads. Lifecycle relay closes keeper writers after provider completion, while the capture session waits for EOF only for its configured finite grace. The native run must establish cleanup/quiescence and descriptor behavior under stop and root-exit cases. |
| R4 | Scope limitation | The descendant probes establish same-process-group behavior only. They do not establish cleanup of `setsid` escapees or hostile same-user interference. Preserve the recorded exclusion and do not describe this as general descendant containment. |
| R5 | Qualification pending | The report’s process-identity helper treats inability to query a PID as “not alive”; the challenge/response control provides an independent effect observation, but native evidence should retain both observations and must not use PID-query failure alone as cleanup proof. |

## Lifecycle and result-integrity review

- Capture options, invocation snapshot, environment validation, token identity, and endpoint creation precede token consumption and target release. Setup failure leaves the preparation in `_preparations`; the new test verifies ordinary launch reuse.
- The captured path continues to use the existing OwnedJob and launchd service. `StandardOutPath` and `StandardErrorPath` point to private FIFOs; ordinary launches retain `/dev/null`.
- On successful launch, capture lifecycle follows the provider’s completion task. On launch failure, the path attempts job cleanup and binds a failed lifecycle result so the keeper descriptors close and drain tasks can finish. Cleanup uncertainty is retained in `_failedJobs`; provider disposal owns capture cleanup and retry.
- `OutputCaptureSession` keeps lifecycle and output completion distinct. The orchestrator returns `ObservationFailure` for incomplete/read-failed output and does not report a successful combined result in those cases. Overflow remains explicit and requests termination while the job is live.
- The code and tests do not claim setsid escape resistance, filesystem confinement, hostile-workload security, owner-death cleanup, or resource quotas.

## Test and evidence boundary

The current native harness covers exact bytes and limits, per-stream overflow, simultaneous pressure, orchestration overflow, nonzero exit, deadline/caller cancellation, explicit stop with a same-PG child, observation-only waiter cancellation, invalid options, root-exit same-PG cleanup, and repeated capture/FD-growth diagnostics. The added setup test covers partial FIFO creation failure before dispatch and preparation reuse. The baseline archive remains immutable.

At review time, implementation-native execution, focused macOS suite counts, cross-platform regressions, and package-consumer qualification were not yet supplied. Portable tests or source inspection cannot establish launchd/FIFO qualification. Native failures, skips, setup errors, cleanup status, and source hashes must be included in the final evidence before the remediation is considered complete.

## Reviewer result

Preliminary source review: **no blocking source defect identified; native qualification pending**. Re-review is required against the exact final implementation revision and its native evidence, especially any changes to FIFO ownership, disposal retry, capture result aggregation, or the qualification oracles.
