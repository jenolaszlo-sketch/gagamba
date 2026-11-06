# Alternative B P3-R1 — independent Q2 handoff

Date: 2026-11-06 (Asia/Manila)
Producer: OpenAI Codex; implementation, regression and evidence preparation only

**P3-R1 REMEDIATED — READY FOR INDEPENDENT Q2**

- Source/test commit: `18a788da2f2f50625468f90feb144b9b9a193af2`.
- Source/test tree: `06933471948165a1532880be42cc1961f9fa6519`.
- Baseline: `982a14373e25ed9cde50533e5d6e4fcb30094185`, tree `ef5e88ff7e5870aff6678bdff74854dd57975b40`.
- Repository: `C:\Users\Laszlos\source\repos\Gagamba`, canonical `main`, one worktree.
- Durable evidence: [P3-R1 remediation](../evidence/rc2b-06-b6-alt-b-p3-r1.md).
- External package: `C:\Users\Laszlos\source\repos\Solo\gagamba-alt-b-p3-r1-codex`.
- Final delivery HEAD/tree: external `final-identity.json`; the subsequent evidence commit changes documentation only.
- Governing decision: [Alternative B owner authority](../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md).

## Finding and repair

P3-Q1 F1 (MAJOR) rejected every matching qualified HELD context: the installed/qualified
first condition was false, so the `else if (authority is not Legacy)` rejected Qualified.
The interrupted producer patch was saved before edits, reviewed as correct, continued
verbatim and committed with actual constructor regressions. The installed request still
requires Qualified; the remaining branch now accepts only Qualified or Legacy. The guard
is preserved. No arbiter, gate, factory, writer, C3, P1/P2, protocol or journal behavior changed.

Only two source/test paths change:

- `src/Gagamba.Execution.Linux.M1/Lifecycle/HeldReleasePhase.cs`.
- `tests/Gagamba.Execution.Linux.M1.Tests/Lifecycle/HeldReleasePhaseConstructorTests.cs`.

## Producer observations to reproduce independently

| Check | Result |
| --- | --- |
| Exact new positive regression against preserved Q1 Debug binary | Original guard exception; 0 passed / 1 failed, expected negative control |
| Baseline constructor matrix, Debug + Release | Both qualified shapes reject; legacy succeeds; missing/incompatible/validation controls refuse |
| New real-constructor/wiring tests | 13 passed / 0 failed / 0 skipped |
| Existing P3 | 27 passed / 0 failed / 0 skipped |
| Q10 + P3 | 20 passed / 0 failed / 0 skipped |
| Relevant P1/P2/lifecycle | 196 passed / 0 failed / 20 skipped |
| Full Windows M1 | 1,335 passed / 0 failed / 331 skipped; 1,666 total |
| Fixed constructor matrix | 8/8 Debug and 8/8 Release, qualified callback present and legacy callback absent |
| Debug test/dependency build | 0 warnings / 0 errors |
| Release M1 / Owner / Recovery | Each 0 warnings / 0 errors |
| Release production verifier | PASS |
| Debug as production | FAIL as expected; all three forbidden authority factories detected |

Final runs identify the clean source/test commit in `commands.json`; earlier dirty-tree
runs are preserved under `precommit-validation`. `run_checks.py` retains exact commands
and filters. `source-identities.json` binds working bytes and committed source blobs;
`SHA256SUMS.txt` binds the raw package except itself. All 2,255 Q1 manifest entries were
verified before reliance. Both historical review packages remain untouched.

Host: Windows 10.0.26200 x64; SDK 10.0.401, runtime 10.0.12, MSBuild 18.9.11.
Release M1 SHA-256 `F6DEB97331F81E655043FDBFDFD7F5A74076D0DC1B7A2D4B26A6706DABE31222`,
MVID `fb18ed6a-5b73-48ea-9544-fc9edd7612cc`.
Debug M1 SHA-256 `8F6C368D6816659F08D08E7E5AD55C83C901BF848CE0B7295481B6AE7650E4B2`,
MVID `c4ef5172-bc41-4eed-8c7d-f4c6f4f3fbc2`.

## Review questions and fixture boundaries

1. Does a real P1-retained matching Qualified context now construct HELD in both compilation flavors, retaining the original context and installing the instance method that calls `OwnerReleaseGenerationGate.Verify`?
2. Can omission of wire installation identity ever switch that Qualified context onto an ungated path? Producer observes it remains gated; the upstream request/context mismatch rejection is unchanged.
3. Do absent authority, installed/Legacy mismatch and reflected Validation refuse before arbiter/safety binding? Does validation still have only a private constructor and no usable factory or positive authority?
4. Does the exact new positive regression fail against original code and pass against the repaired code, rather than merely asserting an IL reference or an expected exception?
5. Does the constructor-created arbiter execute the real installed callback on generation refusal, with zero physical attempts, closed capability, retained Intent, no runtime/durable RELEASE commit and SetupFault stop? Does a duplicate avoid reverify/retry/update?
6. Do stop during successful verification and original deadline expiry still win, and does legacy success/duplicate retain one-attempt behavior?
7. Are all production files outside the guard unchanged, including P1/P2 admission, arbiter synchronization, original C3, controller Q10 and sole physical RELEASE writer?
8. Do production exclusion and the Debug negative control remain effective, without adding a production test/caller injection route?

The fixtures invoke the real constructor and retain its actual arbiter. Golden loaded
installation selections and safety/descriptor inputs are synthetic; native trust is not
established. The real-callback refusal test uses the existing Debug-only missing-loaded-
installation shape and leaves the callback intact. Arbiter clock/prerequisite/transport
fields are replaced by reflection in tests only. Stop/deadline rows separately substitute
a bounded successful callback to exercise winner precedence. These substitutions are
explicit and add no production seam. Matrix reflection fixtures similarly avoid OS work.

## Required qualifications and preservation

All **331 native skips remain unqualified**. Protected fixed-root positives, live generation
switch while HELD, actual qualified native RELEASE, withdrawal/process loss, native
stop/reaping/reconciliation, partial initialization cleanup and relevant native cutpoints
remain pending. The accepted post-last-stable-observation residual, including before
verifier return, is unchanged; it does not waive stop/C3 synchronization obligations.

No native provisioning, production activation, P4, Slice E or Slice N was performed.
No owner acceptance is requested by this producer package. The interrupted
`Solo\gagamba-alt-b-p3-q2\STOPPED.md` is **not independent qualification** and was not reused
as passing evidence. This commission performs no Q2 execution; a separately commissioned
reviewer must use a fresh package and preserve the historical Q1/Q2 packages.

`final-preservation.json` records clean tracked/index state, the same 44 historical
untracked paths and SHA-256 values, and one canonical main worktree. No branch, linked
worktree, reset, restore, clean, prune, rebase or push occurred. Original unfinished work
was preserved and committed, not discarded.
