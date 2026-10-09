# AR-5 — deadline, bounded output and execution evidence

- Date: 2026-10-09 (Asia/Manila)
- Remediation slice: AR-5
- Baseline source SHA: `5bbfd704b1e8e2284b2e0cf2e33772c2eb6aed33`
- Final tested source SHA: `d0c8bab0865c5c793ec673e981fa144c718b0b6e` (project-reference checkpoint `cb1d13e93e9139c9dd8aeae7ed034ee881fce467`, then source/test commit)
- Branch/tree at test: `main`, clean after source commit; evidence/handoff commit follows.
- Windows host: Windows 10.0.26200, x64, .NET SDK 10.0.401/runtime 10.0.12; Job Objects and extended startup handle-list APIs.
- Linux host: Ubuntu 26.04.1 LTS in WSL2, kernel 6.6.114.1-microsoft-standard-WSL2, x86-64, glibc 2.43, .NET SDK 10.0.112/runtime 10.0.12, cgroup v2, UID 0. The test creates `/sys/fs/cgroup/gagamba-ar5-tests` under the writable root cgroup and verifies `cgroup.kill`; `posix_spawn` SETCGROUP and `addclosefrom_np` are required.
- Qualification scope: Windows native Job Object capture/deadline, root WSL2 Linux cgroup capture/deadline, portable output and runtime tests, macOS fake launchd refusal/plist tests.
- Explicit exclusions: native macOS launchd capture/legacy `/dev/null` qualification, unprivileged Linux delegation for this slice, installed-package and release qualification, other OS versions/topologies and hostile-code isolation.

**Status:** Implemented and locally tested. Windows and the stated WSL2 Linux host are native-tested for the listed AR-5 legs. macOS capture is explicitly unsupported before dispatch, and native macOS qualification remains pending. No package or release qualification is claimed. This document supersedes the earlier AR-5 feasibility gate in this same path: the user chose Windows/Linux capture now and explicit macOS refusal.

## Claim and baseline-to-fix observation

| Probe / test | Baseline observation at `5bbfd70` | Change | Fixed observation at `d0c8bab` |
| --- | --- | --- | --- |
| `Ar5WindowsTests` native capture/overflow/stdin/deadline | Static source had no output-capable contract, child stdio pipes or orchestration helper. AR-5 tests could not compile against it. No uncontrolled flood was run on the baseline. | Extended startup with `PROC_THREAD_ATTRIBUTE_HANDLE_LIST`, two pipes and readable `NUL` stdin; suspend/assign/resume remains intact. | 5/5 focused native tests passed; 30/30 full provider tests passed. |
| `Ar5LinuxTests` native dual-stream flood and root-exit descendant deadline | Static source inherited trusted stdio and exposed no capture route. No uncontrolled flood was run on the baseline. | `pipe2(O_CLOEXEC)`, `dup2` actions before existing `addclosefrom_np(3)`, close parent writers, drain concurrently. | 2/2 focused native tests and 42/42 full provider tests passed; zero child directories remained under the AR-5 cgroup parent. |
| `Ar5MacTests` refusal and plist | Baseline plist named ordinary per-job stdout/stderr files with no size limit. | Captured launch refuses without consuming the token or kickstarting; legacy launchd stdout/stderr point to `/dev/null`. | 1/1 fake launchd regression passed; no native macOS result is inferred. |
| `Ar5OutputTests` and `Ar5OrchestratorTests` | No byte budgets, output status, stop cause or structured run receipt existed; these are new API tests rather than a historical executable failure. | Bounded two-stream drain, deadline/explicit/caller stop orchestration and redacted receipt. | 3/3 output tests and 9/9 runtime orchestration cases passed; full contract/runtime suites passed. |

The prior gate recorded source facts rather than a baseline flood. This slice does not relabel an absent baseline run as a measured failure. Tests added for the new public surface necessarily did not compile at the baseline; the baseline source/plist inspection is the reproducible observation.

## Resulting contract and ownership

The frozen `IExecutionProvider` methods remain compatible. Additive `IOutputCaptureProvider.LaunchCaptured` is opt-in, because a runtime helper cannot install or safely own native child stdio after the provider launches it. The concrete consumer need is a run/deadline/output flow without confusing cancellation of `WaitForCompletionAsync` with termination. An external `Process` launched for capture was rejected because it would bypass Job Object/cgroup ownership. `ExecutionOrchestrator.RunAsync` is the higher-level helper; existing callers can keep using the original SPI.

`OutputCaptureOptions` defines byte limits, not text/character limits: independent stdout and stderr budgets up to 16 MiB each, total up to 32 MiB, zero meaning overflow on the first byte. Both streams drain concurrently even after an overflow. Retention is disabled by default; explicit `RetainBytes=true` keeps only the bounded prefix within per-stream and total budgets. Overflow signals the orchestrator to call `Terminate`, then it observes provider completion within `StopGrace` (default five seconds, maximum one minute). Pipe EOF has a separate finite `PipeGrace` (default five seconds, maximum one minute). A missing EOF yields `DrainIncomplete`, never an indefinite wait or a claim of complete output. A failed terminal observation aborts reader ownership. Legacy Windows/Linux launches do not have an output budget unless capture is requested; legacy macOS output is discarded rather than written to hidden growing files. A macOS capture request is refused before target dispatch; a native launchd-compatible bounded stream topology still requires measurement.

The run receipt includes correlation ID, provider/platform, effective capability receipt, a normalized HMAC fingerprint, UTC start/completion and duration, distinct natural/nonzero/caller/deadline/explicit/overflow/launch/observation causes, root exit where known, bounded output status/counts and Confirmed/Failed/Unknown cleanup. The fingerprint key is process-local, so it supports comparison within one process lifetime but is not a cross-restart stable identifier. Arguments, environment values, raw provider diagnostics and output bytes are absent by default. Explicit `RetainBytes=true` returns bounded raw bytes to the caller. Provider reasons are converted to generic reason codes because they may contain sensitive invocation data.

At a deadline, caller cancellation, explicit stop or overflow, orchestration requests domain `Terminate` rather than cancelling an observation token. It then waits under an independent stop grace. A natural or terminated provider result proves cleanup only under the provider's lifecycle contract. Failed or timed-out observation stays Unknown. A failed pre-launch discard reports Failed. If capture setup fails after admission, the provider requests termination and retains ownership for disposal; the receipt does not claim cleanup. Existing provider-owned terminal state and observer-cancellation semantics from AR-1 through AR-3 remain the authority.

## Exact final checks and cleanup

| Check on `d0c8bab` | Result |
| --- | --- |
| Windows `dotnet build Gagamba.sln --no-restore -v:q` | Passed, 0 warnings/errors |
| Windows core contract suite | 20 passed, 0 skipped |
| Windows native provider suite | 30 passed, 0 skipped, including 5 AR-5 cases |
| Windows-host macOS parser/fake suite | 45 passed, 4 explicit native skips; fake tests do not qualify launchd |
| Windows runtime suite | 14 passed, 0 skipped, including 9 AR-5 cases |
| Windows conformance suite | 5 passed, 0 skipped; existing conformance oracles await AR-6 review |
| WSL2 root Linux provider suite | 42 passed, 0 skipped, including 2 AR-5 native cases |
| Root cgroup child directories after Linux suite | 0 under `/sys/fs/cgroup/gagamba-ar5-tests` |
| Native macOS | Unavailable, not run |
| Installed package/release | Not run; AR-6 scope |

Native tests use finite commands and outer `WaitAsync` deadlines; providers own Job Object/cgroup termination. The Windows deadline leg observes `StopRequestAccepted` and Confirmed cleanup; the Linux leg launches a child that holds inherited pipes after the root exits, then observes deadline termination, `Complete` pipe drain and Confirmed cleanup. The Linux cgroup parent was enumerated after the suite and contained no child directories. Windows provider completion establishes job-domain terminal state through its existing lifecycle implementation; this slice did not independently count Windows OS handles. macOS fake cleanup removes its private plist directory after disposal, but `/dev/null` routing is not native-qualified. Failed cleanup/observation is never promoted to success.

## Findings, review and limitations

AR-5 addresses F13's unbounded private launchd log path and the review's missing usable deadline/output/evidence flow. It does **not** claim host-wide output or disk quotas; only opt-in captured bytes are budgeted, and legacy macOS output is discarded. AR-1/AR-2/AR-3 ownership fixes remain prerequisites, not requalified by these new tests. No baseline F13 resource flood was performed, so the finding is addressed by source removal and regression/fake evidence with native macOS qualification pending.

Luna read-only review found: (1) captured Windows stdin was opened write-only; fixed to readable `NUL` and added native EOF case, (2) synchronous completion-observer exceptions could bypass stop/cleanup; fixed with bounded termination/re-observation and a fake-provider regression, and (3) partial Windows reader construction could leak handles; fixed staged disposal. Luna confirmed the Windows whitelist handle inheritance, Linux `dup2`/close ordering and macOS pre-dispatch refusal source paths. The reviewer could not run Git in this checkout; Sol inspected the diff and ran the final suites. Earlier Luna inspections identified the need for the whitelist on Windows and native launchd uncertainty; the latter is handled by explicit refusal, not an unmeasured FIFO claim.

AR-5 does not provide filesystem confinement, network confinement, identity isolation, arbitrary disk quotas, arbitrary CPU quotas or full hostile-code isolation. macOS same-identity jobs and the documented `setsid` escape remain outside a hostile-execution boundary. Windows/Linux native tests cover this host/configuration, not all deployments; macOS native behavior, installed packages and release qualification are pending. AR-6 must bind any qualification to its own exact source and package candidate; this AR-5 evidence does not automatically qualify later revisions.
