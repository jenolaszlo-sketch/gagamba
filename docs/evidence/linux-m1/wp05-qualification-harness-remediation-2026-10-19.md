# WP05 Qualification Harness Remediation - 2026-10-19

## Disposition

- Harness remediation: **`WP05 QUALIFICATION HARNESS REMEDIATED - INDEPENDENT REQUALIFICATION REQUIRED`**
- Owner integration: **`WP05 OWNER INTEGRATION DEFERRED BY AUTHORITATIVE PLAN`**
- WP05 release: **NOT ACCEPTED**; no independent acceptance is claimed here.

This tranche changes test code, test scheduling metadata, a test entrypoint, and documentation only. It does not modify production behavior, the seccomp policy, the coordinator, the adapter, the release protocol, the G2 manifest, or the v3 schema.

## Repository binding

- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- Remediation branch: `remediation/wp05-qualification-harness`
- R1 test-oracle correction: `88cce9745d0dc2a21bc507080d08cf97e0f00f7e`
- R2 process-global isolation: `87a890762d02243d71cdceece78853ed77804930`
- R3 owner-integration audit: `ef11393912aebc34130162f0e4789630081bcd27`
- R4: this evidence and status commit
- Independent review preserved separately: `review/wp05-seccomp-kernel-requalification` at `5b6ab064ec0e329735aae969a3478577203d6616` (Q1 `16ca8bf`, Q2 `bf80d52`, Q3 `d672d07`, Q4 `c2c87a4`, Q5 `ba5320c`, Q6 `b88845b`, Q7 `5b6ab06`). The original Q1 assertion and its 137 failure evidence remain unchanged there.
- Working tree at test time: source tree `87a890762d02243d71cdceece78853ed77804930` (R2); no production source changes; only tests, test script, and docs after the base.

## Fact 2 oracle remediation

Original independent observation: the preserved Q1 probe asserted `forgedExit == 133 || forgedExit == 139`; the forced abort produced a SIGKILL-derived wrapper exit 137, and the probe failed at that final assertion. The independent review classified this as a qualification-probe expectation defect/permitted forced-stop race.

Contract used for the correction:

- `HeldAbortOutcome` exposes `Stopped`, `WrapperReaped`, `Error`, and `NoTargetInitializationMarkerAbsent`; it does not promise an exact wrapper exit code.
- `BubblewrapAdapter.AbortAsync` closes abort control and then calls `domain.StopAndAwaitEmpty` before reaping. The cgroup stop may SIGKILL the coordinator before its own `fail(33)` exit. `OwnerPreparationTransaction` consumes only `Stopped` and the marker.

New assertion (`Wp05ForgedAbortOracle.Validate`, test-only): it requires all of the following simultaneously:

1. The forged request decision is `Refused`, durable release is `NotCommitted`, delivery is `NotAttempted`, and no durable commit is reported.
2. The release capability remains live (no consumption).
3. The journal after abort is readable, remains `Prepared`, and contains no release or stop fact.
4. The abort reports `Stopped`, `WrapperReaped`, no error, and the fixture constructor marker absent.
5. The wrapper exit is 133 (the coordinator's own fail(33) exit) or 137 (this test's own `AbortAsync` cgroup stop, SIGKILL). 139 and every other value are rejected. The SIGKILL outcome is accepted only with the successful abort/reap/security context above; an unexplained exit is not accepted.

The original security assertions remain in the test, and the oracle strengthens the final check (it also rejects 139, which was the old seccomp SIGSEGV outcome). The corrected test passes; the original independent probe is untouched.

Negative oracle tests (`Wp05ForgedAbortOracleTests`): **12/12 passed** (2 contractual outcome rows, 10 rejection rows). They reject forged release acceptance, durable release/delivery facts, consumed capability, missing abort/cleanup evidence, journal release/stop facts, invalid stage/read state, absent marker evidence, and unexplained exit codes 0, 1, 130, 139, 143.

Q1 results with the corrected oracle:

| Run | Result |
|---|---|
| Fact 1 alone | 1/1 passed |
| Fact 2 alone | 1/1 passed |
| Fact 3 alone | 1/1 passed |
| Q1 together (run 1) | 3/3 passed |
| Q1 together (run 2) | 3/3 passed |
| Q1 in supported phase 1 (two runs) | passed both |

## Process-global reaping isolation

Inventory:

- `M1RecoveryScope` construction enables `PR_SET_CHILD_SUBREAPER` (process-global).
- `M1RecoveryScope.ReapAdoptedChildren` uses process-wide `waitpid(-1, WNOHANG)`.
- `M1RecoveryScope.AbortHeld` calls `ReapAdoptedChildren`.
- Test classes that own this process-global state: `NonRootQualificationTests` (direct `ReapAdoptedChildren` and `AbortHeld`), `OwnerDeathQualificationTests`, `IntegratedBarrierTests`, `DomainAuthorityTests` (scope creation), and `RecoveryHarness` (subreaper enablement). All five are tagged `[Trait("Category", "ProcessGlobalReaping")]`.
- Victims are unrelated child-spawning/pidfd tests, including the Q1 probes and `PidFdChildReaperTests`; they run in the regular phase.

Selected mechanism: **dedicated test-host processes**, implemented by `eng/linux-m1-qualified-tests.sh`:

1. The regular assembly runs in parallel with `--filter 'Category!=ProcessGlobalReaping'`.
2. Each tagged class runs in its own `dotnet test` invocation (a fresh test-host process). Within a class, xUnit runs tests sequentially; no other class shares the host.

This does not rely on a collection attribute alone. The process-global `waitpid(-1)` source cannot share a host with unrelated child tests, and the tagged classes do not share with each other. A bare parallel `dotnet test` of the full assembly remains unsupported; the unsupported control still reproduced the two interference failures (Q1 Fact 3 `waitid(P_PIDFD) errno 10` and `NonRootWp04RealOwnerLossAfterHeldAcceptanceReconciles` adopted-child timeout).

Supported command (documented in `docs/testing-and-ci.md`):

```bash
bash eng/linux-m1-qualified-tests.sh
```

The M1 assembly is not currently wired into the CI workflows; any future M1 CI job must use this profile. The script uses the project and Release configuration from the repository and does not set up host delegation; the documented non-root delegated environment must already exist.

Remaining limitations: new process-global tests must be tagged and added to the script's class list; the bare parallel full-assembly command is not supported; per-class isolation is class-granular.

## Regression results

Linux environment: Ubuntu 26.04.1 LTS, kernel `6.6.114.1-microsoft-standard-WSL2` x86_64, boot `bc5278f0-01e9-4a7c-9b25-a5e843a495ba`, glibc 2.43, .NET 10.0.112, GCC 15.2.0, Python 3.14.4; test uid/gid 1000, `CapEff=0`; delegated cgroup `/sys/fs/cgroup/gaga-delegated` (`memory`, `pids`); Bubblewrap 0.11.1 `523da3e7…`; workspace `/home/gaga/wp05-harness-qual`.

Supported profile (two independent runs):

| Phase | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Regular parallel (process-global classes excluded) | 462 | 0 | 0 |
| `NonRootQualificationTests` (dedicated host) | 31 | 0 | 0 |
| `OwnerDeathQualificationTests` (dedicated host) | 7 | 0 | 0 |
| `IntegratedBarrierTests` (dedicated host) | 1 | 0 | 14 |
| `DomainAuthorityTests` (dedicated host) | 2 | 0 | 19 |
| `RecoveryHarness` (dedicated host) | 3 | 0 | 2 |
| **Total** | **506** | **0** | **35** |

Both runs produced identical counts. The 541 total reflects the 529 prior assembly rows plus 12 new oracle regression rows. NonRoot repeated three additional fresh-process runs: **31/31 each**. Q1 together repeated: **3/3**. Focused WP05/WP04 selection: **83/83**. Freeze selection (`ManifestTests`, `ReleaseFrameTests`, `OwnershipV3`): **74/74**.

Unsupported control (recorded, not the supported profile): a bare parallel full assembly produced **504 passed / 2 failed / 35 skipped**: `Wp05ReleasePathIndependentQualificationTests.RealCoordinatorOverallExpiry...` (`waitid(P_PIDFD) errno 10`) and `NonRootQualificationTests.NonRootWp04RealOwnerLossAfterHeldAcceptanceReconciles` (adopted children did not become reapable). This confirms the isolation requirement; the supported profile passed both rows.

WP04 native HELD/abort: `NonRootQualificationTests` 31/31 includes `NonRootWp04HeldSetupReportsHeldWithoutTargetInitializationThenAbortsAndReconciles` (HELD, no target constructor marker, abort, cleanup, reconciliation).

Skips: 35 total, all pre-existing root-only/non-root-profile gates: 14 `IntegratedBarrierTests` rows requiring openat2 and root-writable `/sys/fs/cgroup`, 19 `DomainAuthorityTests` rows plus 2 `RecoveryHarness` rows requiring direct cgroup-root creation. No new skips; no required seccomp row was skipped.

Windows: full solution Release build **0 warnings / 0 errors**; M1 assembly on Windows **378 passed / 0 failed / 163 skipped** (541 total). Windows skips are the Linux-only/delegation rows; they are not Linux evidence.

Build distinction: the WSL copied-tree build succeeded with 0 errors and 8 SourceLink warnings because the copy has no `.git` metadata; these are environment/tooling warnings, not code warnings. The Windows in-repo solution build had 0 warnings and 0 errors.

## Owner integration audit

Read-only audit: `docs/reviews/gagamba-linux-m1-wp05-owner-integration-audit.md`.

Outcome A: **`WP05 OWNER INTEGRATION DEFERRED BY AUTHORITATIVE PLAN`**.

- Plan §16 WP05 boundary: “Live integrated path still cannot release”; WP05 uses qualification-only protocol tests and no target exec.
- Plan WP08: owner orchestration, dependencies WP01–WP07, with release still inhibited pending G3.
- Plan WP10: minimal owner integration wiring in `RestrictedExecutionOwner.cs` after WP09/G3, plus the one complete static-native transaction.
- ADR 0004 §21/§22 defer owner release wiring.
- Current `Owner/Program.cs` runs only `OwnerPreparationTransaction`; no release invocation. The Q1 probe's internal composition is intentional WP05 qualification, not a production gap.
- No production owner API/endpoint/orchestration/wiring/target execution was added.

## Production boundary and freeze verification

Production diff since `58cb1ba` is limited to test files, the test entrypoint, and docs; there are no changes under `src/`, `native/`, or `artifacts/`.

| Frozen item | Verified value |
|---|---|
| Corrected seccomp filter | 672 bytes / `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` |
| `exec-coordinator` | `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b` |
| WP05 qualification coordinator | `e20903cce6061ecc2b152f9e7b4ccb389edeee54047d96f1f3b66dd5195be0b9` |
| WP04 native fixture | `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305` |
| G2 canonical manifest | 873 bytes / `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3` (freeze suite passed) |
| WP05 RELEASE frame | 128 bytes / `90A07CDEF85B18B5B2A0360D7D01705CF7B9E4E9B2D52E7C56DA76A16201E59B` (freeze suite passed) |
| Durable schema | OwnershipSnapshot remains v3 (freeze suite passed) |
| Target execution | None; Q1 stops at the WP05 pre-exec boundary |

## Evidence files and commits

Committed under `docs/evidence/linux-m1/wp05-qualification-harness-remediation-artifacts/`:

- `environment.txt`, `artifacts.txt`
- `oracle.log`, `fact1.log`, `fact2.log`, `fact3.log`, `q1.log`, `q1-run2.log`
- `qualified-profile.log`, `qualified-profile-run2.log`
- `nonroot-rep-1.log`, `nonroot-rep-2.log`, `nonroot-rep-3.log`
- `focused.log`, `freeze.log`
- `unsupported-full-parallel.log` (control)
- `windows-build.log`, `windows-m1.log`
- `SHA256SUMS`

Commits: R1 `88cce9745d0dc2a21bc507080d08cf97e0f00f7e`, R2 `87a890762d02243d71cdceece78853ed77804930`, R3 `ef11393912aebc34130162f0e4789630081bcd27`, R4 (this evidence/status commit).

## Remaining work

1. Independent requalification of the harness changes (the implementer does not accept them).
2. WP05 release/stop race matrix.
3. Fresh-process crash matrix.
4. No-resend recovery qualification.
5. Trusted status-channel qualification.
6. Full WP05 independent acceptance.
7. Owner orchestration and release wiring in WP08/WP10 (deferred by plan).
8. Deferred wrong-audit-architecture generator issue (separate, unchanged).

No WP05 acceptance is claimed.
