# WP05 Abort Oracle Provenance Remediation - 2026-10-19

## Disposition

- Oracle: **`WP05 ABORT ORACLE REMEDIATED - INDEPENDENT REQUALIFICATION REQUIRED`**
- SIGPIPE: **`WP05 SIGPIPE INVESTIGATION - NO QUALIFICATION-HOST INTERFERENCE DEMONSTRATED`**
- Owner integration: **`WP05 OWNER INTEGRATION DEFERRED BY AUTHORITATIVE PLAN`** (unchanged; see the separate audit)
- WP05 release: **NOT ACCEPTED**; no independent acceptance is claimed.

This tranche changes qualification tests, a test-only native observer, a qualification-only SIGPIPE probe, the test runner, and documentation. It does not change production behavior, seccomp policy or artifacts, the coordinator, the adapter, the release protocol, the journal schema, the owner entrypoint, or the production reaper.

## A. Repository binding

- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- Previous harness remediation HEAD (R4): `ecc80d1c24f583ad113c4932fc23d876d0ffbbb0`
- New branch/worktree: `remediation/wp05-abort-oracle-provenance` in `C:\Users\Laszlos\source\repos\Gagamba`
- Independent review branch: `review/wp05-qualification-harness-requalification`
- Independent commits: Q1 hostile probe `8a863c498658701c31ac88b5ab5a0d754949457b`, Q2 evidence `5c4c1a4b8cdda968406b5b2e267213b62c4eff26`, Q3 preservation `67e2d5b95ff2ff1a7d92fb2af27cb37bc0995bce`. They are reachable in the canonical repository and were not modified.
- Independent evidence/checkpoint (on the review branch): `docs/evidence/linux-m1/wp05-qualification-harness-independent-requalification-2026-10-19.md`, `docs/reviews/gagamba-linux-m1-wp05-harness-counterexample-preservation-checkpoint.md`.
- R5 import of the preserved probe: `042940e1a471063b69644682946aa9c6d63efd5f`
- R6 oracle/provenance correction: `fc4f53f5dd989c2a18c36c82e3535a82b36a6de8`
- R7 SIGPIPE investigation/isolation: `6e534c893a84e56e80ca2d53306f57b408ff6c19`
- R8 evidence/status: `3ba6fde64ea3d450f74da13a6d8ab90a36c25977`
- R9 byte-exact hostile-probe import correction: this commit (no semantic change; final probe bytes match Q1)
- Tested code/test revision: `6e534c893a84e56e80ca2d53306f57b408ff6c19` (full supported-profile runs); R9 restores the exact Q1 probe bytes with no semantic change, so the final code/test revision is R9
- Production diff (`src/`, `native/`, `artifacts/`): empty since `58cb1ba`. Tracked changes are tests, the qualification runner/fixture, and docs.

## B. Reproduced counterexample

The preserved hostile probe `Wp05ForgedAbortOracleHostileProbeTests.Exit137WithoutRawWaitOrStopProvenanceIsRejected` supplies a valid forged-release refusal, valid journal, live capability, no transport use, a stopped/reaped abort, and only a numeric `wrapperExitCode: 137`, with no raw wait classification or authorized-stop provenance. It expects rejection.

Before remediation the oracle returned `null` (accepted). The imported probe failed at `Assert.NotNull` (one failed test, one failed assertion); raw output is `hostile-probe-before.log`. The probe was imported from Q1 `8a863c4` at R5 and its assertion was not altered; R9 corrects the import encoding to the exact Q1 blob (see B.1).

Root cause: `src/Gagamba.Execution.Linux.M1/Native/AtomicCgroupSpawn.cs` `NativeLaunchInterop.TryWaitPidFd` decodes `waitid` as `CLD_EXITED -> si_status`, and `CLD_KILLED`/`CLD_DUMPED -> 128 + si_status`; `PidFdChildReaper` retains only the derived integer. The old oracle accepted 133 or 137 from that integer and had no `si_code`, `si_status`, wrapper identity, or stop-provenance input. A normal exit 137 and a SIGKILL-derived 137 were indistinguishable.

### B.1 Hostile-probe import provenance correction

R5 imported the Q1 probe through a PowerShell text redirect, which wrote UTF-16LE (3710 bytes) instead of the independent reviewer's blob. R9 restores the exact Q1 blob bytes (1810 bytes; Git blob `d427c19f7e0aaf635a24bc877b4eb511acbb89a2` = Q1 `8a863c4`). The test text, input, and substantive assertion are unchanged. The pre-fix failure log remains valid; the byte-exact probe passes at R9. The final import is byte-for-byte the preserved independent probe.
## C. Termination-evidence design

Test-only types (`Wp05TerminationEvidence.cs`):

- `OwnedWrapperIdentity(ExecutionId, GenerationId, PreparationId, PidFd, Pid)` — the execution/generation/preparation identity plus the kernel pidfd and observed child pid. It is usable only when all identities are non-empty and the pidfd/pid are valid.
- `ObservedTermination(Identity, Kind, RawCode, ExitStatus?, Signal?, Source, Error)` — distinguishes `Exited`, `Killed`, `Dumped`, and `Unknown`; carries raw `si_code`, exit status, and signal; `Source` must be `PidFdWaitIdWnowait`.
- `AuthorizedStopEvidence(Identity, Stopped, WrapperReaped, Error, NoTargetInitializationMarkerAbsent)` — built from the actual `HeldAbortOutcome`; no self-asserted `AuthorizedStop` flag.

Test-only observation (`Wp05TerminationObserver.cs`):

- Obtains the production reaper's retained pidfd (test-only reflection into the internal reaper field; the production reaper owns and eventually consumes the status).
- Polls with the existing `PidFd.TryExited`, then calls `waitid(P_PIDFD, WEXITED | WNOWAIT)` and parses the raw `siginfo` (`si_code`, `si_status`, pid). `WNOWAIT` does not consume the status, so the production `PidFdChildReaper.TryReap` still reaps it.
- If the status is consumed (ECHILD), times out, has no child record, or is otherwise inconsistent, it returns `Unknown`; the oracle fails closed.
- It records the observed pid in the identity; no PID is treated as authority and no new durable identity is introduced.

Oracle (`Wp05ForgedAbortOracle.Validate` structured overload):

- Retains every forged-release security check: `Refused`, durable `NotCommitted`, delivery `NotAttempted`, no durable commit, live capability, readable journal still `Prepared` with no Release or Stop fact.
- Requires the actual abort outcome: `Stopped`, `WrapperReaped`, no error, marker absent.
- Requires usable structured termination with the same identity as the stop evidence, `PidFdWaitIdWnowait` source, and consistent raw fields.
- Accepts a normal exit only when `RawCode=1`, `ExitStatus=133`, and no signal (documented coordinator self-exit).
- Accepts signal termination only when `RawCode=2`, `Signal=SIGKILL (9)`, and no exit status.
- Rejects normal exit 137, normal exits 0/1/130/139/143, SIGTERM/other signals, `CLD_DUMPED`, `Unknown`, missing/consumed wait status, contradictory raw fields, identity mismatches, wrong execution/generation/preparation/pidfd/pid, missing reaping/cleanup/marker, transport activity, forged acceptance, durable release facts, and capability consumption.
- The preserved hostile probe's 5-argument call remains but now always returns an insufficient-evidence rejection; bare numeric exit codes are never accepted.

The causal limit is explicit: a raw SIGKILL proves signal termination, not the actor. The accepted signal case is supported by the test's sole authorized `AbortAsync` call, the product `Stopped`/`WrapperReaped` result, the same execution/wrapper identity, and the isolated test context. An unrelated external SIGKILL could not be distinguished by kernel status alone; the oracle fails closed when provenance is missing and the qualification host is dedicated. This is the strongest claim the instrumentation supports.

## D. Oracle regressions and native controls

- Structured oracle tests: **23/23 passed** (22 structured positive/negative rows plus the preserved hostile probe). The original 12 oracle regressions were adapted to the structured contract; new rows cover normal exit 137, SIGKILL without stop provenance, wrong wrapper/execution/generation/preparation identity, wrong signal, contradictory termination, missing/consumed wait status, dumped termination, and the full forged-release/journal/capability/transport/marker matrix.
- Preserved hostile probe: before remediation **1 failed** because the oracle returned `null`; after remediation **1 passed** because the structured-evidence gate rejects the bare 137. Raw outputs: `hostile-probe-before.log`, `hostile-probe-after.log`.
- Native observer controls (`Wp05TerminationObserverTests`): **2/2 passed**. A process that exits 137 is observed as `Exited`/raw 1/status 137 with no signal, then reaped by `waitpid` as a normal exit 137. A process killed by SIGKILL is observed as `Killed`/raw 2/signal 9 with no exit status, then reaped as signal 9. Both observations use `WNOWAIT`; the final `waitpid` still succeeds, proving the status was preserved. Values: `observer-observation.txt`.
- Q1 Facts: **3/3 passed** (Fact 1, Fact 2, Fact 3), including repeated Fact 2 runs; the production coordinator still reaches the pre-exec boundary and still accepts no forged release. No target execution.

## E. SIGPIPE investigation

Source audit:

- `src/Gagamba.Execution.Linux.M1/Native/AtomicCgroupSpawn.cs:360-368`, `NativeLaunchInterop.TryIgnoreSigPipe`, calls `signal(SIGPIPE, SIG_IGN)` and captures but does not restore the previous disposition.
- `src/Gagamba.Execution.Linux.M1/Launch/ReleasePipeWrite.cs:20-33` invokes it from `WriteOnce`; `tests/Gagamba.Execution.Linux.M1.Tests/Native/ReleaseProtocolTests.cs:131-158` directly calls `WriteOnce` for delivered/broken-pipe/would-block cases in the regular phase.
- `signal` changes the process-wide disposition (not a per-thread mask); `SIG_IGN` is inherited across `exec`; a caught handler would reset on exec.

Disposable-host diagnostic (`Wp05SigpipeDispositionTests`, dedicated test host, restores the original disposition):

- Baseline `SIGPIPE` handler: `0x1` (SIG_IGN).
- After `ReleasePipeWrite.WriteOnce` on a closed pipe: `0x1` (SIG_IGN); the write returned `BrokenPipe`.
- Exec'd child probe: class 1 (SIG_IGN).
- Original disposition restored: verified.
- Raw observation: `sigpipe-observation.txt`; runner log: `sigpipe.log`.

Conclusion: the .NET qualification host already ignores SIGPIPE, so `TryIgnoreSigPipe` is idempotent and the untagged call produced no observable host/child change. The additional test is tagged `Category=ProcessGlobalHostState` and runs in a dedicated test-host process; the supported runner excludes both `ProcessGlobalReaping` and `ProcessGlobalHostState` from the regular phase and runs the tagged classes individually. `ReleaseProtocolTests` remains in the regular phase because the host baseline is already SIG_IGN and no interference was demonstrated. No production change was made. The redundancy of the production call on .NET is noted as a maintenance observation, not a defect.

Disposition: **`WP05 SIGPIPE INVESTIGATION - NO QUALIFICATION-HOST INTERFERENCE DEMONSTRATED`**.

## F. Regression

Environment at the exact-R7 supported runs: Ubuntu 26.04.1 LTS, kernel `6.6.114.1-microsoft-standard-WSL2` x86_64, boot `83799c16-80f7-4588-b29a-1dc9efe920fb`, glibc 2.43, .NET 10.0.112, GCC 15.2.0, Python 3.14.4; uid/gid 1000, `CapEff=0`; delegated cgroup `/sys/fs/cgroup/gaga-delegated` (`memory`, `pids`) with the test process in `gaga-delegated/host`; Bubblewrap 0.11.1 `523da3e7…`; artifacts rebuilt in a fresh workspace.

| Check | Result |
|---|---|
| Structured oracle + hostile probe | 23/23 passed |
| Native observer controls | 2/2 passed |
| SIGPIPE diagnostic | 1/1 passed; baseline/after SIG_IGN; child SIG_IGN |
| Q1 Facts (all three) | 3/3 passed |
| Q1 Fact 2 | 1/1 passed (plus repeated runs) |
| WP04 native `NonRootQualificationTests` | 31/31 passed |
| Focused WP05/WP04 | 83/83 passed |
| Freeze (`ManifestTests`, `ReleaseFrameTests`, `OwnershipV3`) | 74/74 passed |
| Supported profile, run 1 (HEAD R7) | phase 1 473/473; phase 2 47 passed / 35 skipped; total 555 rows; 0 failed |
| Supported profile, run 2 (HEAD R7) | identical counts; 0 failed |
| Unsupported bare parallel control | 517 passed / 3 failed / 35 skipped / 555; **not supported** |
| Windows M1 assembly (HEAD R7) | 389 passed / 0 failed / 166 skipped / 555 |
| Full solution Release build (Windows) | 0 warnings / 0 errors |

The supported-profile totals: 473 + 47 = **520 passed**, **35 skipped**, **0 failed**, **555 rows**, repeated identically twice at R7. The unsupported bare parallel control failed with `NonRootWp04CoordinatorExitBeforeHeldRefusesAndReconciles` (adopted-child reaping), Q1 Fact 3 (`waitid(P_PIDFD) errno 10`), and Q1 Fact 2 (observer/oracle interference); the supported profile passes all three. The pre-commit supported runs (identical content) also produced the same counts; their logs are retained as supplementary.

Skips: 35 total, all pre-existing root-only gates — 14 `IntegratedBarrierTests` (openat2/root cgroup), 19 `DomainAuthorityTests`, and 2 `RecoveryHarness`. The new observer, SIGPIPE, oracle, and hostile rows are not skipped. Windows skips are Linux-only rows (166) and are not Linux evidence.

Build distinction: the WSL copied-tree build had 0 errors and 8 SourceLink warnings because the copy lacks `.git`; the in-repo Windows build had 0 warnings/0 errors.

## G. Frozen boundary

- Corrected seccomp: 672 bytes, SHA-256 `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` (unchanged).
- `exec-coordinator` `5e55e92b…`, qualification coordinator `e20903cc…`, WP04 fixture `ee25a876…` (unchanged).
- G2 canonical manifest: 873 bytes / `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3` (freeze test passed).
- WP05 RELEASE frame: 128 bytes / `90A07CDEF85B18B5B2A0360D7D01705CF7B9E4E9B2D52E7C56DA76A16201E59B` (freeze test passed).
- `OwnershipSnapshot` remains v3; no schema v4 or second journal.
- No real target execution; no production owner integration.
- Production diff since `58cb1ba`: empty for `src/`, `native/`, and `artifacts/`. The only non-test production-adjacent addition is the qualification-only `tests/Fixtures/wp05-sigpipe-probe.c`, which is not a production artifact.

## H. Remaining qualification

1. Independent requalification by Luna of the corrected oracle, observer, hostile probe, and SIGPIPE result.
2. Linux test-host separation/cleanup must be independently qualified on a fresh host; the supported profile is green twice but the reviewer must confirm the runner and isolation.
3. Full WP05 release/stop arbitration matrix.
4. Fresh-process crash matrix.
5. No-resend recovery qualification.
6. Trusted status-channel qualification.
7. Full WP05 independent acceptance.
8. WP08 owner orchestration and WP10 minimal owner release wiring/target execution remain on their plan-owned schedule.
9. Deferred wrong-audit-architecture generator issue remains separate.

No WP05 acceptance is claimed.

## Evidence files and commits

Committed under `docs/evidence/linux-m1/wp05-abort-oracle-provenance-remediation-artifacts/`:

- `environment.txt`, `artifacts.txt`, `source-head.txt`
- `hostile-probe-before.log`, `hostile-probe-after.log`
- `oracle.log`, `oracle-byte-exact.log`, `observer.log`, `observer-observation.txt`, `sigpipe.log`, `sigpipe-observation.txt`
- `fact2.log`, `q1.log`, `wp04.log`, `focused.log`, `freeze.log`
- `qualified-profile-head-run1.log`, `qualified-profile-head-run2.log`
- `qualified-profile-run1.log`, `qualified-profile-run2.log` (pre-commit identical-content supplementary)
- `unsupported-full-parallel.log` (control)
- `windows-build.log`, `windows-m1.log`
- `SHA256SUMS`

Commits: R5 `042940e...`, R6 `fc4f53f...`, R7 `6e534c8...`, R8 (this evidence/status commit).


