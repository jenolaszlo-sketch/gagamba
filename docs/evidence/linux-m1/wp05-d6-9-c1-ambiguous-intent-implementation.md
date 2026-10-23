# WP05 D6.9 C1 Ambiguous ReleaseIntent Persistence — Implementation Evidence

Date: 2026-10-23
Producer: Luna
Scope: C1 / R09 implementation and regression evidence only
Independent reviewer: GLM (required; not yet performed)

Status: **WP05 D6.9 C1 IMPLEMENTATION COMPLETE - TYPED AMBIGUOUS RELEASEINTENT PERSISTENCE; FIRST-RETAINER ORDERING AND LOCK-SAFE TAIL STOP IMPLEMENTED; DETERMINISTIC REGRESSIONS PASS; INDEPENDENT GLM SOURCE REVIEW REQUIRED; C3 C4 IMPLEMENTATION NOT AUTHORIZED; RELEASE NOT QUALIFIED**

This is producer implementation evidence. It is not an independent review, Linux qualification, G3 acceptance or WP05 release acceptance. C2 evidence and review documents remain unchanged.

## 1. Git binding

| Item | Identity |
|---|---|
| Repository | C:\Users\Laszlos\source\repos\Gagamba |
| Worktree | C:\Users\Laszlos\source\repos\Gagamba-d6-9-c1 |
| Origin | https://github.com/jenolaszlo-sketch/gagamba.git |
| Required base / D6.8.5 preservation | 6a60d2fc490acee18aa161b847a34343144c5b26 |
| Branch | implementation/wp05-d6-9-c1-ambiguous-intent |
| Tests-first commit | b1825deb898fa6c2390e344d1e477c8cf0a206a3 |
| Production commit | 6fcd8b5f52c7083bfba8a7139264641fc0d8da0f |
| Tests-first parent | 6a60d2fc490acee18aa161b847a34343144c5b26 |
| Production parent | b1825deb898fa6c2390e344d1e477c8cf0a206a3 |
| Base src/ tree | 84cfabb8f82e3ac4061776cc9a9a8c4d5354a126 |
| Base tests/ tree | e03515f4d1ffb2171ae911a5b1da894b6f553d65 |
| Final code src/ tree | 9dd0f055eaf48d48bda8683dee3f8dc534a06e5d |
| Final code tests/ tree | ebcb976a164b212090fbe0958fe4b0c0b243f794 |

The required third commit has subject docs(wp05): record C1 ambiguous persistence implementation evidence; it is created as a direct child of 6fcd8b5f52c7083bfba8a7139264641fc0d8da0f and contains this report plus the handoff and work-queue updates. Its full object name is the local branch tip reported in the final handoff. A Git commit cannot contain its own object name without changing the committed content and therefore its hash; no amend or fourth commit is used.

The implementation was made from the specified preservation base in an isolated worktree. The canonical checkout and the existing producer, review and preservation worktrees were not modified. C2 producer evidence and GLM review files were not changed.

## 2. Contract traceability

| Approved obligation | Implementation and evidence |
|---|---|
| C1-A — typed ambiguity at the final seal-removal boundary | JournalFailureKind.AmbiguousAfterContent is returned only after both candidate slots have been written, file-synced and directory-synced, when final gen.seal removal fails for a candidate carrying ReleaseTag.Intent. C1-T02/T03 and NonAmbiguousFailuresDoNotReceivePostContentClassification assert the distinction. |
| C1-B — explicit uncertain release decision | ReleaseDecisionKind.AmbiguousPersistence = 4 preserves existing enum values. The retained result is MayHaveExecuted, NotAttempted, DurableCommitted=false, LiveExecutionExcluded=true; its reason identifies ambiguous journal persistence. C1-T04/T05/T06 and the corrected historical probe assert these fields. |
| C1-C — first retainer wins | After _journalGate is released, _gate checks existing retained release first, accepted stop second, otherwise retains ambiguity. C1-T04 forces ambiguity-first; C1-T07 forces stop-first and verifies the exact stop-won reason; C1-T06 verifies reachable duplicate-entry behavior. |
| C1-D — stop is a lock-safe tail action | The ambiguity barrier is after _journalGate release; retention is under _gate; the retention barrier and RequestStop(CallerStop) run after _gate is released. C1-T07 demonstrates stop can acquire the runtime gate at the opposing barrier; C1-T15 asserts after-ambiguous-intent, after-ambiguous-retention, after-stop ordering. No journal I/O or RequestStop was added under _gate. |
| C1-E — preserve C2 stop capability and conservative persistence | Tail stop uses existing RequestStop, which consumes the shared capability at first acceptance. C1-T05/T07/T10 cover failed durable stop persistence and no authority restoration; C1-T08/T09 cover unavailable clock and deadline overflow without fabricated deadlines or records. |
| C1-F — recover conservatively and never resend | C1-T11 releases the original writer, acquires a fresh journal writer through TryRecoverCommit, and observes ReleaseTag.Intent classified MayHaveExecuted. C1-T12 verifies a fresh arbiter cannot write another Intent and the transport remains unused. Isolated Linux D4_6 additionally proves owner-process death, replacement recovery and zero release attempts. |

The approved D6.7/D6.2 behavior is implemented without C3/C4 redesign or production Owner integration. Case C's after-intent already-retained state is not fabricated: the reachable entry-duplicate path is tested in C1-T06, and the defensive retained-first branch after ambiguity is verified by source inspection.

## 3. Source changes and journal classification

Production changes are limited to the three authorized files:

- src/Gagamba.Execution.Linux.M1/Recovery/OwnershipJournal.cs: adds JournalFailureKind and a typed result. Ordinary errors remain Unclassified; no error-string parsing is used. The final seal unlink branch returns AmbiguousAfterContent only for a candidate Intent. The writer baseline and LastSequence advance only after successful unseal. No journal schema or format changed. Existing FailSealed, Resolve and recovery protocol remain in place.
- src/Gagamba.Execution.Linux.M1/Execution/ReleaseDecision.cs: appends the internal enum value AmbiguousPersistence = 4; values 0–3 are unchanged.
- src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs: captures the typed write result outside _journalGate, conditionally retains ambiguity or the exact stop-won refusal under _gate, and requests the C2 stop after releasing _gate. No retry loop, nested lock, new durable record or transport path was added.

Test changes are limited to the new Wp05D69C1AmbiguousIntentTests.cs, the historical Wp05ReleaseContractProbes.cs, and the D4 owner/supervisor test fixture. The D4 result projection exposes the ambiguous decision fields needed by its existing real-process probe. No production Owner, native, serialization, public API or C3/C4 files changed.

The fault-injection case fails the final seal unlink after the candidate Intent has been mirrored to both generation slots. C1-T02 asserts the write result is failed and typed AmbiguousAfterContent, the writer baseline and journal last-sequence remain at the previous acknowledged sequence, and the seal remains. C1-T03 decodes both slots and asserts equal candidate sequence and ReleaseTag.Intent; the seal remains and ReadLatest returns uncertainty. C1-T01 preserves definite pre-Intent write failure as Refused(NotCommitted) with no Intent, no stop and no transport attempt.

Other failure kinds are not promoted to this C1 ambiguity classification. Where the pre-existing journal protocol retains a seal, reads remain uncertain and recovery remains conservative; this evidence does not claim those states are safe to resend.

## 4. First-retainer, stop and recovery observations

The deterministic suite contains C1-T01 through C1-T15 plus a non-ambiguous-classification check (16 tests total).

- **Ambiguity-first:** C1-T04 pauses at after-ambiguous-retention, then requests a competing stop while the release caller is outside _gate. Stop is accepted, the capability is consumed, and the original result remains AmbiguousPersistence.
- **Stop-first:** C1-T07 pauses at after-ambiguous-intent, which is after _journalGate release and before retention. The stop is accepted first. The release then retains Refused(MayHaveExecuted, NotAttempted, DurableCommitted=false, LiveExecutionExcluded=true) with reason exactly release gate refused: stop won during durable intent. The test verifies later release returns a duplicate of that refusal.
- **Reachable duplicate:** C1-T06 issues another release after an ambiguity has been retained; it returns Duplicate with the original classification fields and reason, without another Intent or transport write. C1-T13 holds the first caller at the post-journal barrier while a second caller attempts release; both slots remain at sequence 2 and FD4 attempts remain zero.
- **Tail stop / C2:** ambiguity-first uses existing RequestStop(CallerStop) after retention. When the sealed journal prevents durable stop acknowledgement, the retained ambiguity receives a truthful tail-stop limitation and is not replaced. C1-T05/T07/T10 verify capability invalidation and failed-stop authority cannot reactivate release. C1-T08/T09 verify accepted stop with unavailable clock or arithmetic overflow has zero deadline and no durable StopRecord.
- **Unchanged release-first behavior:** C1-T14 observes one delivered transport write and a committed record on the normal success path.
- **Fresh-writer recovery:** C1-T11 proves a second writer is blocked while the original writer fence is live, then disposes the original writer and calls TryRecoverCommit from a fresh OwnershipJournal. The recovered snapshot has ReleaseTag.Intent, and ReleaseRecoveryClassifier returns MayHaveExecuted. C1-T12 then acquires fresh writer authority, attempts a release through a replacement arbiter, and verifies the journal remains Intent and transport attempts stay zero.
- **Real-process D4_6:** in a separate Linux test host, the test-only owner is killed after ambiguous persistence; replacement recovery reports Intent / MayHaveExecuted, release_attempted=false, zero transport attempts and zero pre-write acknowledgement witnesses. The D4 fixture asserts cgroup cleanup.

Concurrency tests use ManualResetEventSlim, bounded WaitAsync/watchdogs (5 seconds) and deterministic barrier callbacks; no sleep is used as race control. C1-T15 observes the lock-safe barrier order. The already-retained-after-intent defensive case is a source-level argument because that history is unreachable under the current single-arbiter ordering; no test seam manufactures it.

## 5. Red/green and regression results

### Windows

Environment: Windows, .NET SDK 10.0.401.

The final targeted command was dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-restore --nologo --logger 'console;verbosity=minimal' --filter 'FullyQualifiedName~Wp05D69C1AmbiguousIntentTests|FullyQualifiedName~Wp05D68C2StopCapabilityTests|FullyQualifiedName~ReleaseArbiterTests|FullyQualifiedName~Wp05ReleaseContractProbes|FullyQualifiedName~JournalTests|FullyQualifiedName~OwnershipV3TransitionTests|FullyQualifiedName~OwnershipV3CodecTests|FullyQualifiedName~Wp05ReleasePathIndependentQualificationTests'.

- Initial test compilation exposed xUnit analyzer errors xUnit1031 and xUnit1051 (blocking task waits / missing cancellation tokens); no tests ran in that attempt. The tests were corrected before the red runtime phase.
- Tests-first red phase against unchanged C2 production: **12 failed, 5 passed, 1 skipped, 18 total**. Captured red assertions included expected AmbiguousPersistence versus actual Refused, expected JournalFailureKind.AmbiguousAfterContent versus actual null (the result had no typed field), bounded waits for post-journal/retention barriers that old code never emitted, and missing automatic-tail-stop/capability outcomes.
- First production green attempt: **8 failed, 9 passed, 1 skipped, 18 total** because the retained reason did not contain the required phrase ambiguous journal persistence. This was corrected and retained in the production history.
- Focused rerun after that correction: **17 passed, 0 failed, 1 skipped, 18 total**.
- Existing regression groups before the final tail-stop assertion: **142 passed, 0 failed, 3 skipped, 145 total**.
- Final targeted C1/C2/arbiter/probe/journal/recovery/codec/release-path run after all production and test changes: **158 passed, 0 failed, 3 skipped, 161 total**. The three skips are Linux-specific release-path cases.
- dotnet build Gagamba.sln -c Release --nologo: succeeded, **0 warnings, 0 errors**.
- D4_6 is Linux-only and was not executed on Windows.

### WSL2 Linux execution

Environment: WSL2 Ubuntu 26.04.1, kernel 6.6.114.1-microsoft-standard-WSL2, x86_64, .NET SDK 10.0.113; qualification process uid=1000(gaga) in /gaga-delegated/host, with CapInh, CapPrm, CapEff and CapAmb all zero. The source was a task-local Linux extraction of git archive HEAD at production commit 6fcd8b5f52c7083bfba8a7139264641fc0d8da0f; this avoided sharing Windows-generated obj outputs.

The first attempt against the Windows-mounted worktree failed before tests: MSBuild MSB3374 could not update a Windows-generated Release .Up2Date timestamp (Access denied). The first Linux-local profile attempt then built successfully but stopped in its regular phase because the clean archive did not contain ignored native fixtures: four SeccompFilterPinTests failed with missing artifacts/linux-m1/wp04/seccomp-filter.bin; **518 passed, 4 failed, 4 skipped, total 526**. No process-global phase ran in that attempt.

The repository's bash eng/linux-m1/build-native.sh generated the existing fixtures; no source was changed. Hashes:

| Fixture | SHA-256 |
|---|---|
| tests/Fixtures/Gagamba.LinuxM1.Native/out/fixture | ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305 |
| artifacts/linux-m1/wp04/exec-coordinator | 5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b |
| artifacts/linux-m1/wp04/seccomp-filter.bin | 2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108 |
| artifacts/linux-m1/wp05/wp05-qualification-coordinator | 16d30538df254c04082fc56bdbfe9b2c992eae5861dfc278682e99d957aa17e3 |
| Release M1 owner DLL used by the profile | c805a38f315e3acaabee9314aa70849e6ec6de47cd1a96e901202c72d6ffb723 |

The supported process-isolated runner, with the documented Release owner override and existing /root/wp05/run-as-gaga-in-host.sh launcher, then completed. Exact invocation:

wsl.exe -d Ubuntu -u root -- /root/wp05/run-as-gaga-in-host.sh /usr/bin/env GAGAMBA_OWNER_DLL=/home/gaga/qualification/gagamba-d6-9-c1-impl/src/Gagamba.Execution.Linux.M1.Owner/bin/Release/net10.0/Gagamba.Execution.Linux.M1.Owner.dll CONFIGURATION=Release bash /home/gaga/qualification/gagamba-d6-9-c1-impl/eng/linux-m1-qualified-tests.sh

| Phase | Passed | Skipped | Failed | Total |
|---|---:|---:|---:|---:|
| Regular parallel phase (process-global classes excluded) | 526 | 0 | 0 | 526 |
| NonRootQualificationTests dedicated host | 31 | 0 | 0 | 31 |
| OwnerDeathQualificationTests dedicated host | 7 | 0 | 0 | 7 |
| IntegratedBarrierTests dedicated host | 1 | 14 | 0 | 15 |
| DomainAuthorityTests dedicated host | 2 | 19 | 0 | 21 |
| RecoveryHarness dedicated host | 3 | 2 | 0 | 5 |
| Wp05TerminationObserverTests dedicated host | 3 | 0 | 0 | 3 |
| Wp05SigpipeDispositionTests dedicated host | 1 | 0 | 0 | 1 |
| **Aggregate** | **574** | **35** | **0** | **609** |

The profile output included SourceLink/repository-location warnings because the extracted source has no .git directory; compilation succeeded. It did not include Wp05D4SupervisorTests in its process-global class list, so D4_6 was run separately in a dedicated non-root host after prebuilding its existing test-only owner fixture.

The C1-only filter was verified with --list-tests inside the Linux shell. Through the existing gaga launcher, the shell command was cd /home/gaga/qualification/gagamba-d6-9-c1-impl && dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-build --no-restore --nologo --logger "console;verbosity=minimal" --filter "FullyQualifiedName~Wp05D69C1AmbiguousIntentTests": **16 passed, 0 failed, 0 skipped**. Isolated D4_6_AmbiguousSealFailureRecoversIntentAndDoesNotResend used the same command with filter "FullyQualifiedName~D4_6_AmbiguousSealFailureRecoversIntentAndDoesNotResend": **1 passed, 0 failed, 0 skipped**. The D4 owner fixture binding is tests/Fixtures/Wp05D4Owner/Wp05D4Owner.csproj SHA-256 70155896bb8c2db4d34ecebe92a15eb4858a9682c1e82cfbbf96376ff29c347c, Program.cs SHA-256 b2223942f67b94b5fec8054d6ed1a66066ceb9d9e63b27cba8c5162ddf9f9dcf, built Release DLL SHA-256 63894aaa02e602fb13c08e4a35d77008fdd44e0ee5cbaa15b3ab069de4cd0753.

One additional direct-filter invocation did not apply the intended C1 selection across the Windows-to-WSL command boundary and selected unrelated process-global tests. It showed waitid(P_PIDFD) errno 10 and adopted-child reap timeouts, was interrupted after the broad selection was recognized, and has no final test count. It is not counted as a C1 result. A subsequent --list-tests check inside bash -lc showed exactly the 16 C1 tests, and the corrected focused run passed. Post-run inspection found the delegated host cgroup empty (populated 0, frozen 0), no child cgroups and no remaining gaga test/coordinator processes.

These are WSL2 execution results for this source snapshot, not C1 Linux qualification. No native Linux distribution, G3 acceptance or full WP05 release acceptance is claimed.

## 6. Enum consumer audit and scope

ReleaseDecisionKind remains internal. A production-only search found construction/duplicate-handling references in ReleaseArbiter; there is no exhaustive production switch, serialization or status projection requiring a wider change. Test-only D4 projection now reports the new decision and capability fields. Recovery remains based on durable ReleaseTag.Intent and continues to classify it MayHaveExecuted.

No C3/C4 behavior, journal format, production Owner integration, WP08/WP10, native backend, publication or package behavior was changed. The commit diff stays within the three authorized production files and directly affected test/evidence files. git diff --check is required after this document is added and is recorded with the evidence commit.

## 7. GLM independent review handoff

Producer tip to review: 6fcd8b5f52c7083bfba8a7139264641fc0d8da0f on implementation/wp05-d6-9-c1-ambiguous-intent. Please independently examine:

1. Whether ambiguity is typed at the correct journal operation boundary.
2. Whether any definite failure is incorrectly classified as ambiguous.
3. Whether any true post-content Intent ambiguity can still become NotCommitted.
4. Whether the first-retainer rule is exact.
5. Whether stop-first preserves the exact refusal reason.
6. Whether a duplicate can overwrite retained classification.
7. Whether any ambiguous path can reach FD4 or persist ReleaseCommitted.
8. Whether a second caller can write another ReleaseIntent.
9. Whether tail stop is invoked only after both gates are released.
10. Whether every accepted stop path consumes the C2 capability.
11. Whether deterministic barriers force the opposing interleavings they claim.
12. Whether recovery preserves MayHaveExecuted.
13. Whether owner-death and no-resend claims have real evidence.
14. Whether C3 and C4 remain unchanged.
15. Whether enum consumers handle uncertainty safely.

GLM has not reviewed this implementation. Native-distro qualification, independent GLM review, G3, Owner integration and WP05 release acceptance remain required or outstanding; no release-qualified claim is made.