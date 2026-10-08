# Windows lifecycle AR-0/AR-1 evidence

Date: 2026-10-09 (Asia/Manila). Scope: Windows lifecycle and current-scope documentation only. Baseline source: `8dea7d86c8ee2f1c924100acdbc1bf5dc14b2a88` on `main`, with pre-existing uncommitted review-plan documentation. Host: Windows 11 Home Single Language, 64-bit, build 26200; .NET SDK 10.0.401. No package publication or installed-package qualification was performed.

## Baseline-to-fix regressions

The three tests below were added **before** production changes and run against the unchanged Windows provider. Each test uses a fresh GUID workspace/marker, finite child, bounded watchdog, and a controlled lifecycle barrier where needed.

| Probe / test | Baseline failure | Implementation change | Fixed observation |
| --- | --- | --- | --- |
| E1 `E1_WaitCallReturnsBeforeLiveRootFinishes` | The call occupied its caller for the live root; assertion `WaitForCompletionAsync occupied its calling thread` failed. | The execution owns one async observer/terminal task; each wait only observes it. | The call returned before the live child finished; termination produced `Terminated`. |
| E2 `E2_DisposalWinsBeforeLaunchRelease` | Launch returned `Started` after disposal closed admission. | A provider-visible launch admission persists through native work; disposal closes admission and joins it; resume/registration share the gate. | Launch returned `Failed`, and the fresh child marker remained absent. |
| E3 `E3_DisposeSettlesAnOutstandingWait` | The waiter only settled when the independent six-second watchdog cancelled it. | Disposal requests state-owned termination and joins its terminal task; observer retains SafeHandles until completion. | Disposal and waiter settled with `Terminated` before the watchdog. |

Additional native regressions: `PrepareRacingDisposalCannotRegisterAUsableJob`, `CancelledObserverPreservesNaturalOutcomeForLaterWait`, `TwoWaitersAndTerminateShareOneTerminalResult`, two `FailedAssignmentOrResumeCannotRunTarget` cases, and `CleanupUncertaintyRemainsOwnedAndDisposeCanRetry`. The existing Windows suite also covers root exit with a surviving child, assignment failure against a dead target, direct tree termination, dispose kill, and environment behavior. Repeated dispose and later/equivalent-handle terminal reads are asserted. All new tests have bounded child work and test-level cleanup; normal completion queries the actual job's active-process count before reporting terminal.

## Final local checks

| Check | Result |
| --- | --- |
| `dotnet build Gagamba.sln -c Release --no-restore --nologo` | Succeeded; zero warnings/errors |
| Windows provider tests | 23 passed, 0 failed, 0 skipped (14 existing + 9 new cases) |
| Execution contract tests | 14 passed, 0 failed, 0 skipped |
| Windows conformance tests | 5 passed, 0 failed, 0 skipped |

The native Windows tests ran on this host. Linux/macOS qualification, CI dispatch, package qualification, and exact-revision release gates were not part of AR-1. No test-owned process or job is known to remain after the passing suite: natural and terminated outcomes require root exit plus zero active job members, failed pre-release targets must signal before their process handle is discarded, and existing tree tests prove lock release. This is bounded local evidence, not a hostile-code security certification.

## Ownership and limits

The [invariant document](../windows-lifecycle-invariants.md) describes the provider gate, launch admission, execution state, termination/observer coordination, and weak completed-result retention. Observer cancellation leaves execution running. New calls after provider disposal still throw under the SPI; waits already obtained settle. Explicit termination after terminal is idempotent for a terminated result and does not rewrite a natural result.

AR-0 corrected README/current contract wording: the shipped providers manage process lifetimes, not filesystem, network, identity, credential, IPC, or broker authority. Windows `EscapeResistant=Full` is scoped to ordinary directly created descendants with breakaway disabled. F01 hostile-workload enforcement and a broader F02 capability/version decision remain for later work. Fresh marker/barrier/watchdog rules address F14 test-oracle risks for this slice; AR-6 qualification remains open.

The pre-assignment owner-crash interval remains: a suspended process created before successful job assignment is not yet protected by job owner-death semantics. Broker-mediated work may be created outside the job. If native termination of an unassigned suspended launch cannot be confirmed, the provider retains its process/job handles, reports cleanup uncertainty, and retries during disposal; it does not claim confirmed cleanup. Such an exceptional failure would require operator investigation. No AR-2 or later platform work was performed.

Luna performed an independent read-only review. It found an unconfirmed failed-launch cleanup path and reference-identity-only terminal lookup; both were corrected. It also noted that new waits after disposal cannot read terminal state. That is retained intentionally because the existing SPI requires methods to throw after disposal, and the invariant now limits later waits to a live provider. Its final pass found a cached faulted disposal task that prevented subsequent cleanup retry; the provider now permits a later disposal call to retry still-owned failed admissions. No other new race was identified in that pass.
