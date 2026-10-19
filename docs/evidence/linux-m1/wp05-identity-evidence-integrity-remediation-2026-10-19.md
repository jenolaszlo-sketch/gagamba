# WP05 Identity and Evidence Integrity Remediation

Date: 2026-10-19
Scope: bounded follow-up remediation of the R9 qualification-harness evidence and identity provenance

## Disposition

- Evidence integrity: **`WP05 R9 EVIDENCE INTEGRITY RECONCILED - HISTORICAL LIMITATIONS RECORDED`**
- Identity provenance: **`WP05 IDENTITY PROVENANCE REMEDIATED - INDEPENDENT REQUALIFICATION REQUIRED`**
- Overall: **`WP05 HARNESS FOLLOW-UP REMEDIATION COMPLETE - INDEPENDENT LINUX REQUALIFICATION REQUIRED`**
- SIGPIPE: implementation-time finding unchanged; independent Linux runtime verification remains pending for Luna.
- WP05 harness/release: **not accepted**; no independent acceptance is claimed.

No production behavior, seccomp policy/artifact, coordinator, adapter, release protocol, journal schema, owner entrypoint, or production reaper was changed.

## A. Repository binding

- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- R9 implementation/evidence HEAD: `aa1a7f54d28dc96a8367e2273999fbad7ec03634`
- New branch/worktree: `remediation/wp05-identity-evidence-integrity` in `C:\Users\Laszlos\source\repos\Gagamba`
- Independent review: branch `review/wp05-abort-oracle-final-requalification`; Q1 `bac2509b94510a522275fde6a640033f0fe6fbf4` (probes), Q2 `2ee412be5ed5f21f4320d3d08aab4f2ff1141987` (evidence), Q3 `d809ac969a84073d117d30b5a80437159741774c` (disposition); review worktree `C:\Users\Laszlos\source\repos\Solo\wp05-abort-oracle-final-requalification`.
- R10 evidence integrity: `5a30576f60ba24be8fe76fdf71702ca29d93b4fc`
- R11 identity correction: `c48e358451c8b0dbe7aace281f64a2eced399ed7`
- R12 evidence/handoff: this commit
- Final code/test revision: R11 `c48e358...`; evidence HEAD: R12.
- Production diff from `58cb1ba` under `src/`, `native/`, or `artifacts/`: empty. The tracked changes are qualification tests, test-only fixtures, the test runner, and documentation.

## B. Evidence integrity

The independent review's audit is reproduced exactly: **20 of 23 R9 manifest entries match the committed Git blob bytes; 3 do not** (`observer-observation.txt`, `sigpipe-observation.txt`, `source-head.txt`).

| File | Recorded SHA-256 | Committed-byte SHA-256 | Working-tree SHA-256 | Classification |
|---|---|---|---|---|
| `observer-observation.txt` | `904b3077edaafd9a9aeecaa499c1cf1d351d2a8b748c00022854866f304db26a` | `8880dc44de2d18c2c309a93651b8de3d06b67586c59a3857a0c5efbda0cb145d` | `904b3077edaafd9a9aeecaa499c1cf1d351d2a8b748c00022854866f304db26a` | B — manifest-generation over CRLF working-tree bytes before LF commit |
| `sigpipe-observation.txt` | `6b58a73aa968a91c0c546aac858e3e52541c12487ab1efaa0114cb44ac2814e8` | `9113ce3ff5ebf208013421b67b1ce072973a2ea3abc7a0f8477afd99c5cf45d4` | `6b58a73aa968a91c0c546aac858e3e52541c12487ab1efaa0114cb44ac2814e8` | B — same CRLF/LF manifest-generation cause |
| `source-head.txt` | `1de880bab5b6760af8214607025f8b7415a1cbf58df3061ed5d29e2ed1d44696` | `29f98abfefb0dc200adab86ef2dd64cc262d95da9f11a6aa474ad9a5fa4917f2` | `1de880bab5b6760af8214607025f8b7415a1cbf58df3061ed5d29e2ed1d44696` | B — same cause; content records R7, not R9 |

Full committed identities, sizes, line endings, Git blob IDs, normalized equality, and introducing commit (`3ba6fde...`, R8) are in `wp05-r9-evidence-integrity-reconciliation-2026-10-19.md` and `wp05-identity-evidence-integrity-remediation-artifacts/git-blob-provenance.txt`.

Revision attribution: `source-head.txt` records `source_head=6e534c893a84e56e80ca2d53306f57b408ff6c19` (R7), branch `remediation/wp05-abort-oracle-provenance`, and boot `83799c16...`. It is the execution record for the two R7 supported-profile runs, not an R9 execution. R9 only restored the exact hostile-probe bytes and updated documentation; no R9 full Linux supported-profile run occurred. The R7 counts are not relabeled as R9. The new R11 runs in section E are fresh and separately bound to R11.

Impact: the committed evidence content is intact and normalized-equal; no observation was altered. The old manifest's three entries are stale for committed-byte verification and are not treated as verified; the original R9 evidence directory and manifest were not changed. The earlier "23/23 verified" claim was based on working-tree bytes and is superseded. The new artifacts directory uses `* -text` and a new `SHA256SUMS` computed over exact committed bytes; this does not retroactively validate the three old entries.

## C. Identity provenance

Original finding: the Q1 integration constructed `AuthorizedStopEvidence.Identity` from `termination.Identity`, the observation under validation, so the later equality check was tautological at that call site.

Source-level chain after R11:

- Expected identity: `Wp05TerminationObserver.StartForReaper` derives the execution/generation/preparation UUIDs from the Q1 harness `PreparationIdentity`, obtains the production `PidFdChildReaper`'s retained kernel pidfd, and reads the wrapper PID from `/proc/self/fdinfo/<pidfd>` (`Pid:` line) at observer start. The expected identity is usable only with non-empty UUIDs and a valid pidfd/PID. It is not constructed from the termination result.
- Observation: `waitid(P_PIDFD, WEXITED|WNOWAIT)` supplies raw `si_code`/`si_status` and `si_pid`; the observer uses the same ownership pidfd and does not consume the status.
- Stop evidence: the actual `HeldAbortOutcome` (`Stopped`, `WrapperReaped`, error, marker) from the test's sole `AbortAsync` call on the primary lease.
- Oracle: requires the expected identity to be usable, requires expected identity == observed identity, and applies all forged-release/journal/capability/transport/cleanup/marker checks. A missing/unusable expected identity or any expected/observed mismatch fails closed.

| Identity element | Expected source | Observed source | Independent witness | Oracle comparison |
|---|---|---|---|---|
| Execution UUID | Q1 harness `PreparationIdentity`; observer constructor | Carried into the termination DTO from the expected identity | Not reported by `waitid`; expected-bound | Expected == observed; expected must be usable |
| Generation UUID | Same | Same | Not reported by `waitid`; expected-bound | Expected == observed; expected must be usable |
| Preparation UUID | Same | Same | Not reported by `waitid`; expected-bound | Expected == observed; expected must be usable |
| PidFd | Production reaper's retained kernel pidfd | Same pidfd used for `waitid(P_PIDFD)` | Ownership handle is the binding; same fd used for observation | Expected == observed; fd used for `waitid` |
| PID | `/proc/self/fdinfo/<pidfd>` (`Pid:`) at start | `waitid` `si_pid` | Yes — two kernel observations of the same pidfd/child | Expected == observed |
| Domain identity | Harness primary lease/execution; single `AbortAsync` | Product `Stopped`/`WrapperReaped` + identity | Product stop/reap result; sole authorized stop in the test | Stop fields and identity must match |
| SIGKILL sender | Not observable | `CLD_KILLED` / `SIGKILL` | No kernel sender witness | Accepted only with authorized abort/reap/identity; bounded causal limit |

The UUIDs are not two independent witnesses: they originate in the harness/reaper and the observed DTO carries the expected values. The load-bearing independent kernel binding is the owned pidfd plus the fdinfo PID versus `waitid` `si_pid`. The kernel proves signal termination, not the signal sender; the authorized-stop attribution is bounded to the test's sole `AbortAsync` call, the product stop/reap outcome, the matching ownership identity, and the isolated test host. The oracle does not claim stronger causality.

Correction and tests:

- New `ExpectedIdentity` observer property and kernel fdinfo PID read; `StartForPidFd` rejects an unusable expected identity.
- `Wp05TerminationObserverTests.ExpectedIdentityComesFromOwnedPidFdBeforeObservation` proves the expected PID/pidfd/UUIDs come from the owned reaper and harness before observation.
- Q1 Fact 2 now passes `terminationObserver.ExpectedIdentity` to `AuthorizedStopEvidence`; it no longer uses `termination.Identity`.
- The oracle rejects a missing/unusable expected identity; existing negative cases cover wrong execution/generation/preparation UUIDs, wrong pidfd/PID, unrelated/stale identity, normal exit 137, SIGKILL without stop proof, contradictory/unknown/dumped status, incomplete cleanup, forged acceptance, transport, journal, and capability misuse.
- The preserved bare-137 hostile probe remains byte-exact (1,810 bytes; Git blob `d427c19f...`) and still passes because its bare numeric input is rejected. The nine independent reviewer probes were imported byte-exact from Q1 `bac2509` and pass 9/9.

## D. Implementation changes (test-only)

- `Wp05TerminationObserver`: exposes `ExpectedIdentity`; reads the wrapper PID from `/proc/self/fdinfo/<pidfd>`; validates the expected identity; continues to use non-consuming `waitid(P_PIDFD, WNOWAIT)` and fail-closed Unknown handling.
- `Wp05ForgedAbortOracle`: rejects an unusable stop/expected identity before comparison.
- `Wp05ReleasePathIndependentQualificationTests`: Fact 2 uses the ownership-derived expected identity.
- `Wp05ForgedAbortOracleTests`: adds the missing/unusable expected-identity negative.
- `Wp05TerminationObserverTests`: adds the expected-identity-from-owned-pidfd control.
- `Wp05AbortOracleFinalReviewerProbeTests`: imported unchanged from reviewer Q1 for regression.
- No production files changed; all forged-release, journal, capability, transport, cleanup, and marker invariants remain load-bearing.

## E. Regression

Linux environment at exact R11: Ubuntu 26.04.1 LTS, kernel `6.6.114.1-microsoft-standard-WSL2` x86_64, boot `55be3081-6938-41e7-9b53-60119ea44076`, glibc 2.43, .NET 10.0.112, GCC 15.2.0; uid/gid 1000, `CapEff=0`; delegated cgroup `/sys/fs/cgroup/gaga-delegated` (`memory`, `pids`); Bubblewrap 0.11.1 `523da3e7...`; fresh workspace.

| Check | Result |
|---|---|
| Oracle + preserved hostile | 24/24 passed |
| Independent reviewer probes | 9/9 passed |
| Native observer controls (normal exit 137, SIGKILL, expected identity source) | 3/3 passed |
| Q1 Facts | 3/3 passed; Fact 2 1/1 |
| WP04 native `NonRootQualificationTests` | 31/31 passed |
| Focused | 83/83 passed |
| Freeze (manifest/RELEASE/v3) | 74/74 passed |
| Supported profile run 1 (R11) | phase 1 483/483; phase 2 48 passed / 35 skipped; 531 passed / 35 skipped / 566 rows; 0 failed |
| Supported profile run 2 (R11) | identical counts; 0 failed |
| Windows M1 assembly (R11) | 399 passed / 0 failed / 167 skipped / 566 |
| Full solution Release build (Windows) | 0 warnings / 0 errors |

Count reconciliation: R9 had 555 rows; R11 adds one oracle negative, nine preserved reviewer probes, and one native observer control = **566 rows** (Linux phase 1 483, phase 2 83 with 35 root-only skips). Windows has one additional Linux-only skip (the new observer test) and the nine reviewer + one oracle rows pass: 399 passed / 167 skipped / 566. Skips remain 14 `IntegratedBarrierTests`, 19 `DomainAuthorityTests`, and 2 `RecoveryHarness` root-only gates on Linux; no new skipped required row. The unsupported bare parallel assembly was not used as the acceptance profile. The R7 supported-profile runs remain historical R7 evidence and are not presented as R11 results.

## F. SIGPIPE boundary

The implementation-time finding is unchanged: `NativeLaunchInterop.TryIgnoreSigPipe` sets `signal(SIGPIPE, SIG_IGN)` without restoring; the .NET test host baseline and post-`WriteOnce` disposition were both SIG_IGN, the child inherited SIG_IGN, and the diagnostic restored the original disposition. The R11 Linux profile re-exercised the tagged `ProcessGlobalHostState` diagnostic in a dedicated process (1/1 passed). This remains implementation-time evidence; independent Linux runtime verification by Luna is still pending. No production signal-handling change was made or implied.

## G. Remaining qualification

1. Independent Luna requalification of exact R11/R12: native observer/identity chain, Q1/WP04, supported runner twice, process-host separation/cleanup, SIGPIPE runtime, and row accounting.
2. Independent stop-provenance verification (kernel signal sender is not observable; bounded attribution).
3. Linux test-host process separation and cleanup.
4. SIGPIPE independent runtime verification.
5. Two complete independent supported-profile runs (the R11 implementation runs are implementation-time evidence).
6. WP05 release/stop and crash qualification only after harness acceptance.
7. WP08 owner orchestration / WP10 owner wiring and target execution remain plan-deferred.
8. Deferred wrong-audit-architecture seccomp issue remains separate.

## Evidence and commits

- Reconciliation addendum: `docs/evidence/linux-m1/wp05-r9-evidence-integrity-reconciliation-2026-10-19.md`
- This evidence report: `docs/evidence/linux-m1/wp05-identity-evidence-integrity-remediation-2026-10-19.md`
- Artifacts: `docs/evidence/linux-m1/wp05-identity-evidence-integrity-remediation-artifacts/` (hash audit, Git blob provenance, source-head attribution, review provenance, original-probe identity, identity matrix, R11 Linux/Windows logs, environment, `SHA256SUMS`).
- Commits: R10 `5a30576...`, R11 `c48e358...`, R12 (this evidence/handoff commit).
- Original R9 evidence and manifest unchanged; independent Q1–Q3 commits/evidence unchanged.
- The new artifact manifest uses the exact committed-byte convention: the new directory has `* -text`, and the manifest is verified against committed blobs after this commit.

No harness or WP05 acceptance is claimed.
