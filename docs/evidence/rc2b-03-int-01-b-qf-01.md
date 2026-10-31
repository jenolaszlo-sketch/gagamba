# INT-01-B-QF-01 qualification checkpoint: remediation required

Date: 2026-10-31 (Asia/Manila).
Slice: GAGAMBA-RC2B-03-INT-01-B-QF-01.
Baseline: `988ef8f31b9e56ed0bb0ac46846cf06d9244a14d`, tree
`d09fc4d61b12031349e10486cbbab8eda77c0124`.
Tested source: `51f615534b468fa3977afc867e5603abdf09bdae`, tree
`9f9b6adb604a4d63a44c048f05db8d701a362379`.
Branch: canonical main, jenolaszlo-sketch/gagamba.
Host: WSL2 Ubuntu 26.04.1 x86_64, kernel
`6.6.114.1-microsoft-standard-WSL2`, glibc 2.43, systemd 259.5;
UID/GID 1000, zero checked capabilities, cgroup v2 memory/pids delegation.
Runtime/SDK: WSL .NET 10.0.13 / 10.0.113; Windows managed regressions also run.
Scope: eight core regressions, four newly implemented integrated fault cases,
four historical cleanup dispositions, authentication/safety/release/journal
component regressions and Windows managed regressions.
Exclusions: twenty required additional cases remain unimplemented/unexecuted;
complete fault qualification, independent review, owner acceptance, native
Linux-host/macOS qualification, B4-B7, Hufu and production activation.

**Disposition: QUALIFICATION INCOMPLETE / REMEDIATION REQUIRED.**
This does not satisfy the complete 24-case request and is **not READY FOR
INDEPENDENT INT-01 SECURITY REVIEW**. There is no demonstrated environment
blocker. The outstanding work is implementation and native execution of twenty
required fault fixtures. **F-01 remains MAJOR/OPEN.** No acceptance is issued.

## 1. Baseline, ancestry and historical preservation

Verified exact linear ancestry, each successor with its predecessor as sole parent:

| Commit | Tree | Purpose |
|---|---|---|
| `026f2e8afe74b658cd6603efbcfc78e80277a16b` | `49148ab2b3167d4bea57cc55af599e9b5e94a962` | Original implementation evidence |
| `a80b622168ba4e1a5250f06ea81f36b0995cc374` | `6ff9c3b70520aa72b9cbed383240ad0acb546922` | ENV-01 runner and internal fixture |
| `988ef8f31b9e56ed0bb0ac46846cf06d9244a14d` | `d09fc4d61b12031349e10486cbbab8eda77c0124` | ENV-01 evidence |

Remote main was still 026f2e8. The explicitly requested ordinary
`git push origin main` advanced it to 988ef8f; a later live ls-remote confirmed
that exact receipt. No force push, amend, reset, rebase, branch or worktree was
used. Existing untracked work remains. The new qualification commits remain local.

All 99 files in ENV-01's sealed raw manifest were rehashed successfully; historical
manifest SHA-256 remains
`b20f10e51b6831339d1b78e9ee5ac82eacfa614d9988bcb74c05129da8843b24`.
Historical case records and [ENV-01 evidence](rc2b-03-int-01-env-01.md) were not
rewritten. New executions have a separate ignored root,
`artifacts/rc2b-int01-b-qf01/`, with distinct attempts and raw exit statuses.

Only `eng/int01-native-matrix.py` changes in tested source. Production source,
compiled test sources and native source remain identical to ENV-01:

| Tree | Identity |
|---|---|
| src | `2748fb78af6a2658ae15b34db5903f2e120b6755` |
| tests | `d1c8cf2f426d7994afef9818f48add8f95a01c89` |
| native | `85631d2f04512376621d3d09f51d67fd90f45335` |
| eng | `2ff4848a900c2ee5db13559fab90f7fba7bba925` |

Alternative B, [D01 authority](../decisions/rc2b-03-int-01-d01-owner-decision.md),
B1/B2/B3, RC-1, C1-C4, Option A and all prior qualifications remain unchanged.
No release authority or authenticated bearer is passed through recovery.

## 2. Four historical Unknown cleanup outcomes

The following describes old records at a80b622, then separately identifies new
observations at 51f6155. Missing evidence is not promoted by inference.

| Historical case | Retained authority and independent stop | Kernel kill / emptiness | Exit, reaping, fence | Staging / journal | Disposition |
|---|---|---|---|---|---|
| recovery-sigkill-held | Recovery killed; owner retains its native domain and safety monitor. No surviving independent recovery witness. | Old case has no corroborated kill-acceptance or emptiness record. | Owner exit via retained pidfd; independent reaping/fence unavailable. | Not confirmed. | Unknown is correct for that evidence. New rerun remains Unknown. |
| recovery-sigkill-after-release | Recovery killed after target outcome; owner retains native authority. | Prior target empty observation is not subsequent cleanup proof. Kill acceptance unobserved. | Owner exit observed; no surviving recovery reaper/fence witness. | Not confirmed. | Unknown retained; no successful-cleanup inference. |
| owner-sigkill-held | Recovery survives with native domain/staging FDs and requests OwnerExited stop. | Old snapshot NativeStop Unknown; separate DomainEmpty/DomainRemoved true. | OwnerExited, OwnerReaped, WriterFence true. | Old final snapshot flags true, but old runner leaves overall Unknown unconditionally. | Historical Unknown unchanged; new independent final record confirms cleanup. |
| owner-sigkill-after-release | Recovery survives and requests OwnerExited stop. | Old NativeStop KillWrittenEmptyObserved, but final cleanup snapshot sampled before DomainEmpty/removal fields completed. | Owner exit/reaping/fence true. | Old snapshot flags false; watchdog may terminate recovery before settlement. | Historical Unknown unchanged; new final record confirms cleanup. |

**QF-01-F01: harness observation defect, corrected.** The previous owner-kill
branch stopped polling at OwnerReaped && WriterFence, then its finally block could
SIGKILL recovery during asynchronous cleanup. It also never assigned Confirmed
for that branch, even if all cleanup flags were present. The correction preserves
the intermediate reaped/fenced snapshot, awaits recovery's independently written
`integrated-final.json` within the original existing twelve-second harness bound,
and assigns cleanup only from actual final flags. Failure to obtain the final
record now fails that case. It never manufactures a new recovery deadline.

Watchdog actions are explicitly recorded separately. No production change was
needed to observe both owner-death cases settle in the new executions. Recovery
death still has Unknown cleanup; harness termination, PID disappearance and outer
systemd cleanup do not replace a surviving reaper, writer fence or journal witness.
No source-level guarantee of recovery-initiated cleanup after recovery's own
SIGKILL is established by this evidence.

## 3. Environment and artifacts

Reuse of the ENV-01 launcher was verified as gaga. Every execution used a uniquely
named transient systemd user service with `Delegate=memory pids`,
`DelegateSubgroup=host`, `ExitType=cgroup`, `KillMode=control-group`,
`RuntimeMaxSec=120`, `TimeoutStopSec=10`, and unprivileged
`setpriv --inh-caps=-all --ambient-caps=-all --no-new-privs`.
Only the test-owned delegated unit parent/descendants were modified. No shared
units, global configuration, permissions, sudo, root execution or privileged
fallback were used. Every standalone/per-case preflight had 12/12 passing checks.

Final parent:
`/sys/fs/cgroup/user.slice/user-1000.slice/user@1000.service/app.slice/gagamba-int01-env01-qf01-final-02.service`.
Harness/controller/recovery/owner membership was that exact parent's `/host`.
Profiles, preflights, /proc status/cgroup/FD inventories, native domain population
and HELD journals/receipts are retained per case.

The unchanged ENV-01 protected runtime, native fixture, coordinator, seccomp,
owner and recovery binaries were reused; no new native build was necessary for
this Python harness change. Exact closure manifests were copied into the new
corpus; real implementation pin verification remains enabled in every execution.

| Identity | SHA-256 |
|---|---|
| Final runner bytes | `d203bf13822d833c41ac6f6d16f1696f1e11dea4befc384d57c88493461e1721` |
| Final profile bytes | `42f2b943474bd4378c5e3a4c27e9acbd488b6aa61c0ece4a66b1f67e264f81e3` |
| Controller DLL | `203db78890a3d12205604aab4b3577ab57164fe98b3e863523e00f7b468e648b` |
| Owner DLL | `23e67b90087127c5e4a78fdee5a0952bfcfdaa865c68e0e6d0aeffdb8ba390a1` |
| Recovery DLL | `a77cc7c7596c810f7775261242ce67db1753a7458302c162635ebbce5485bfec` |
| Fixture | `f07bd71128093a98c4dc2ead3e484c9f150990f26b192dc2bbd23c01aa37ad9a` |
| Coordinator | `ec3e31d3ffa8483aa31cbecef682b656d9fcd16c7a194d17bea9719071d3691e` |
| Seccomp | `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` |

Complete root-owned runtime/hostfxr inventory pins are unchanged and retained in
`artifact-identities.json` and the final profile. This is experimental source-bound
execution, not production installation attestation or binary reproducibility.

## 4. Eight-case core regression at final source

The unchanged default selection remains all eight core cases. Final execution
also selected each case explicitly to preserve separate stdout/stderr/exit status.
All actual controller/recovery/owner PIDs, owner PPid, retained pidfds, domain
paths and native control FDs are in each JSON, not guessed from numeric PIDs.

| Core case | Result | Cleanup | Physical RELEASE observation |
|---|---|---|---|
| positive-one-release | Passed | Confirmed | Exactly 1; Duplicate then still 1; ExecConfirmed, root exit 0 |
| stop-before-release | Passed | Confirmed | Later release not Released; no successful delivery asserted |
| controller-sigkill-held | Passed | Confirmed | No release request sent; independent recovery final retained |
| recovery-sigkill-held | Passed | Unknown | Later release not Released; physical count unobserved after owner loss |
| owner-sigkill-held | Passed | Confirmed in new run | Later release not Released; final recovery receipt awaited |
| controller-sigkill-after-release | Passed | Confirmed | 1 before fault; recovery final retained |
| recovery-sigkill-after-release | Passed | Unknown | 1 before fault; retained original release observation unchanged |
| owner-sigkill-after-release | Passed | Confirmed in new run | 1 before fault; retained original decision unchanged |

The old ENV-01 results remain producer evidence at their original source, not new
executions. New core attempts `core-recheck` and `final-implemented` each passed
8/8. After-release cases still inject after the finite target's synchronous
outcome; they do not qualify a populated workload release/death race.
NativeStop Unknown, DomainEmpty, OwnerReaped, WriterFence, staging removal and
journal finalization remain separate facts. The harness does not relabel
Unknown native-stop evidence from later successful domain removal.

## 5. All 24 additional required case dispositions

Expectations for the four new cases were written before their first execution
to `additional-first/expectations.json`. The actual integrated recovery-parented
topology, authenticated original session and genuine HELD are required by all
four before injecting faults. Core assertions remain intact.

| Additional case | Current outcome / required implementation |
|---|---|
| controller-before-preparation | NOT IMPLEMENTED / NOT RUN: kill original controller before prepare; prove no usable publication |
| controller-during-preparation | NOT IMPLEMENTED / NOT RUN: deterministic authenticated preparation barrier and original-controller loss |
| controller-release-race | NOT IMPLEMENTED / NOT RUN: owner-local arbitration barriers with actual controller loss |
| recovery-retention-death | NOT IMPLEMENTED / NOT RUN: kill recovery during native authority retention and prove no usable capability |
| recovery-release-race | NOT IMPLEMENTED / NOT RUN: actual recovery loss at release arbitration points |
| owner-before-retention | NOT IMPLEMENTED / NOT RUN: owner death before mandatory domain/staging custody ACKs |
| owner-mid-staging | NOT IMPLEMENTED / NOT RUN: partial copy with exact retained staging/domain identities |
| owner-after-intent | NOT IMPLEMENTED / NOT RUN: actual durable release Intent barrier and owner death |
| owner-ambiguous-delivery | NOT IMPLEMENTED / NOT RUN: actual transport uncertainty without manufactured certainty |
| missing-domain-ack | NOT IMPLEMENTED / NOT RUN: withhold mandatory native-domain retention acknowledgement |
| missing-staging-ack | NOT IMPLEMENTED / NOT RUN: withhold mandatory staging-retention acknowledgement |
| wrong-descriptor-role-count | NOT IMPLEMENTED / NOT RUN: malformed integrated native transfer rejected before publication |
| partial-scm-rights | NOT IMPLEMENTED / NOT RUN: incomplete actual integrated ancillary descriptor transfer |
| lost-retention-ack | NOT IMPLEMENTED / NOT RUN: lose an actual required retention ACK |
| blocked-observer-native-stop | NOT IMPLEMENTED / NOT RUN: block observation while proving independently actionable native stop |
| simultaneous-controller-recovery-death | PASSED at HELD: two exact pidfd SIGKILLs back-to-back, owner exit, kernel populated 0/empty procs. Cleanup Unknown. No atomic simultaneous-death claim. |
| integrated-stop-before-mint | NOT IMPLEMENTED / NOT RUN: actual pre-mint stop barrier and irreversible consumption |
| integrated-stop-after-intent-before-write | NOT IMPLEMENTED / NOT RUN: actual owner-local Intent/pre-write ordering |
| integrated-ambiguous-intent | NOT IMPLEMENTED / NOT RUN: genuine integrated journal commit ambiguity, preserve Option A |
| integrated-lost-release-ack | NOT IMPLEMENTED / NOT RUN: lose authenticated release reply after decision; no resend |
| integrated-original-deadline | PASSED: original setup cutoff expires, later release refused, durable journal Release absent, cleanup Confirmed. Scope is HELD setup deadline, not every deadline phase. |
| integrated-owner-live-takeover-refusal | PASSED for kernel writer exclusion: exact live owner before/after separate-open LOCK_EX|LOCK_NB contender; EWOULDBLOCK; recovery WriterFence false. Cleanup Confirmed. Does not inject a forged recovery takeover command. |
| integrated-post-exit-writer-fence | PASSED: actual owner SIGKILL; separate exit/reaping/fence observations; independently written final cleanup receipt. Cleanup Confirmed. |
| actual-recovery-release-impersonation | NOT IMPLEMENTED / NOT RUN: actual recovery process must attempt inadmissible direct release without receiving a release bearer |

Four added cases each passed in `additional-first`, `additional-final` and
`final-implemented`. The final combined selection is **12 passed / 0 failed**,
comprising eight core and four additional cases. It is not 32/32. No skipped or
unimplemented case is counted as a pass. `PENDING_CASES` in the runner still
preserves the original 24-name requirement inventory; its legacy stdout count
does not subtract individually executed cases. This table is the explicit
per-case disposition; complete qualification remains pending.

**QF-01-F02: mandatory integrated fault coverage incomplete, OPEN.** Existing
integrated profile/launch does not route a deterministic fault specification into
the mandatory retention, descriptor transfer, journal and direct release paths.
`IntegratedRecoveryHost` constructs OwnerPreparationRequest without TestBarrier;
existing non-integrated barrier tests and component fault fakes are not equivalent
to these twenty required native cases. Implement narrowly scoped test-only
injection/barrier plumbing through the trusted experimental bootstrap, reject
unsupported profiles, keep authentication and all required checks intact, then
execute each exact case and affected regressions. The missing implementation is
not an environment/permission blocker and does not require architecture selection.

## 6. Regression evidence and exact commands

Native runs use the ENV-01 systemd properties above, launching:

```text
/usr/bin/python3 /mnt/c/Users/Laszlos/source/repos/Solo/artifacts/int01-qf01/launch.py final-implemented implemented
```

The retained launcher performs and inspects preflight, then invokes the committed
runner with `--report <unique case directory> --case <exact case name>` for every
implemented case. Every per-case directory retains preflight, JSON, stdout,
stderr and an exit-status JSON. The launcher and collector are copied into the
ignored evidence corpus for reproduction. Exact environment/profile variables
are in final `launch-identity.json`; the controller remains the pinned ENV-01 DLL.

| Suite / command | Passed / failed / skipped | Evidence boundary |
|---|---|---|
| Native core default regression and final eight selections | 8/0/0 each | Actual integrated execution; two recovery-death cleanups Unknown |
| Four added native case selections | 4/0/0 each | Case-specific limits above; dual-death cleanup Unknown |
| WSL selected component regression, first attempt without primitive opt-in | 177/0/27 | Preserved; native primitive skips not qualification |
| Same WSL selection with GAGAMBA_INT01_NATIVE_PRIMITIVES=1 | 201/0/3 | Authentication, safety, C1-C4/Option A-related release, journal/fencing/reconciliation, bootstrap/B2/B3 components; not substitutes for twenty native faults |
| Windows full M1 Release --no-restore | 987/0/256 | Managed coverage; explicit Linux/native skips, not backend qualification |
| Windows Execution Release --no-restore | 68/0/0 | Managed regression |
| Windows Runtime Release --no-restore | 15/0/0 | Managed regression |

WSL command uses the retained source-identical M1 test assembly:

```text
dotnet test /home/gaga/gagamba-int01-env01-20261031/retained/bin/Gagamba.Execution.Linux.M1.Tests/release/Gagamba.Execution.Linux.M1.Tests.dll --filter "FullyQualifiedName~DirectAuthenticationTests|FullyQualifiedName~OwnerSafetyMonitorTests|FullyQualifiedName~ReleaseArbiterTests|FullyQualifiedName~ReleaseAdmissionTests|FullyQualifiedName~Journal|FullyQualifiedName~ReconcilerTests|FullyQualifiedName~OwnerBootstrapTests|FullyQualifiedName~RecoveryB2Tests|FullyQualifiedName~RestrictedControlB3Tests" --logger "trx;LogFileName=wsl-enabled.trx" --results-directory <raw-root>/wsl-enabled
```

The enabled run's three skips are
`D4_11_DuringIntentStorageGatePreservesActualJournalState`,
`D5_O03_JournalFailureIsNotReportedAsDurableObservation`, and
`ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport`; their additional
native prerequisite profiles were not supplied in this component invocation.
All raw TRX files, including skipped attempts, remain. No consolidated complete
B1/B2/B3 native regression or complete integrated fault qualification is claimed.

Windows commands: `dotnet test <project> -c Release --no-restore --logger
'trx;LogFileName=<suite>.trx' --results-directory <raw-root>/<suite>` for
`tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj`,
`tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj`, and
`tests/Gagamba.Runtime.Tests/Gagamba.Runtime.Tests.csproj`. These build as part
of test execution; raw logs preserve all build/test output. No production source
or compiled test source was changed; the Python runner was exercised natively.

## 7. Cleanup, raw retention and next remediation

Final read-only collection verifies zero remaining QF-01 test-unit cgroups and
zero experimental processes; the user-manager unit inventory has no matching
loaded unit. Test-owned outer units are collected by systemd. `case-workspaces.tar.gz`
retains actual stderr, independent recovery final records and journals for all
new runs. Private Linux workspaces/artifacts remain intentionally retained as
evidence. Outer teardown is not per-case native cleanup or independent reaping.

The new raw corpus has a SHA-256 manifest; its exact hash and file count are
returned with the delivery. Historical evidence remains sealed separately.
No production secrets, direct session secrets or authentication MACs were logged.
Required documentation preparation, whitespace, staged-scope, protected-tree and
historical-manifest checks are performed before the evidence commit.

Next work under the existing QF-01 authorization: implement and execute the
twenty explicitly unimplemented cases, strengthen any limited case evidence
required by independent review, complete remaining applicable native regressions,
and only then evaluate READY FOR INDEPENDENT INT-01 SECURITY REVIEW. This
checkpoint is partial implementation, not completion of the user's objective.
F-01 remains MAJOR/OPEN. No B4-B7, Hufu, VM/HCS exploration, acceptance or
production activation occurred.

Raw manifest receipt: 158 files; SHA-256 `248f50498a3e881ae8280193b04bc9faf7bc8195989e8feb30e9f22f2fa15a47`.
