# WP05 D4 Required Corrections and D5 Handoff - 2026-10-20

Status: independent reviewer statement derived from the D4 crash-witness
review (`docs/reviews/gagamba-linux-m1-wp05-d4-independent-crash-witness-review.md`)
and the rerun artifact set (`docs/reviews/gagamba-linux-m1-wp05-d4-review-artifacts/`).
This document does not authorize or implement any correction itself, and does
not accept any gate. It binds D4's measured state to the required-work
baseline blob `1768ec2a98e69349ca46137f4194fb0d49233772`.

Review disposition (repeat): `WP05 D4 FRESH-PROCESS RELEASE FIXTURES AND
NO-RESEND DIAGNOSTICS REVIEWED AND INDEPENDENTLY REPRODUCED - EVIDENCE
SUPPORTED WITH QUALIFICATIONS; CORRECTIONS AND D5 ITEMS REQUIRED - C1-C4
UNRESOLVED; RELEASE NOT QUALIFIED`.

## 1. D4→D6 required-work coverage ledger

| Required-work item (Q3 baseline) | D4 state | Residual |
|---|---|---|
| D4 item 1: release-owner SIGKILL-at-boundary fixtures with real journal fence and real FD4 witness | Implemented: D4-10..D4-14, CE cases; barriers/faults at real journal/arbiter seams; real `HeldExecution`+pre-exec coordinator; no target | Partially limited to component gates for pre-intent/persistence boundaries (disclosed; not a kernel breakpoint) |
| D4 item 2: transport witness ledger | Implemented: lane A (`D4RecordingTransport`) + lane B (`D4WitnessingTransport` around real `HeldExecution.TryWriteRelease`), append-only, per-case | None beyond CR-7 hardening note |
| D4 item 3: fresh-process CE-01/02/03/04 second-owner variants | CE-01 via D4-2b (fresh replacement + retry refusal); CE-02 via D4-6 (fresh replacement recovery); CE-03 via D4-8 (fresh replacement diagnostic release); **CE-04 fresh second-owner race NOT implemented** (only in-owner D4-9 and writer contention D4-14) | CR-3 — D5+ work item |
| D4 item 4: stop-refused/signal machinery per lease retained across intermediate leases must not re-inherit (extend D1 pattern) | Partially visible: D4-12/D4-14 fresh writer exclusion; D4-8 replacement release only possible after failed stop is absent (no stop machinery re-inherited); no dedicated new case over the D1 lease pattern's remaining fenced state | CR-3b — D5+ explicit case per the "D1 pattern extension" |
| D5 item 1: real FD5 status negatives on the qualification coordinator | Out of D4 scope (producer says so; review confirms no FD5 claims) | unchanged handoff |
| D5 item 2: three `Wp05ReleasePathIndependentQualificationTests` conversion | **Artifacts now exist and the three tests ran and passed in both the producer's and my focused regression runs (66/0/0 with zero skip)** — but no D5 QUALIFICATION claim may be timed from the D4 lineage; conversion to formal D5 rows remains required | unchanged handoff; artifact presence alone does not make the conversion |
| D6 items | untouched | unchanged |

## 2. Corrections (no silent test fixes; none block the D4 diagnostic evidence)

All corrections are documentation/hardening items; the reviewed D4 state is
honest and none of these alters a measured result:

- **CR-1 (SIGKILL oracle standard).** The fixture asserts
  `ExitCode == 137` after `Process.Kill(entireProcessTree:false)`. Verified:
  .NET 10 `Kill()` maps to `kill(pid, SIGKILL)`,
  `ProcessExitStatus.ExitCode` encodes `128 + signal` for signal deaths, and
  the reviewer's controls (`wait4probe.txt`, `dnet-kill-control.txt`,
  `dnet-self-exit-control.txt`, `term-control.txt`) bind the runtime truth to
  this kernel profile. Residual: public `ExitCode` cannot distinguish a
  voluntary `exit 137`. Required for formal D5/D6 reuse: bind the kill oracle
  to `wait4/waitid`-level status (e.g., test-only native reaper publishing the
  waited status) or a pidfd kill path, OR record this bounded reliance as an
  explicit standard note inside the review evidence set — the fallback of
  relying on a reviewer-produced control artifact is only valid when the
  control set is committed to the same durable evidence root (as this review
  does under `docs/reviews/gagamba-linux-m1-wp05-d4-review-artifacts/controls/`).
- **CR-2 (class-header wording).** `Wp05D4SupervisorTests.cs` says
  "supervisor ... never acquires ... an owner journal lease" while
  `AssertJournalFenceReleased` transiently acquires the fence in the
  supervisor process to prove release. Reword to "never holds the journal
  writer for release purposes; transiently probes the fence to prove owner
  death". Text-only fix.
- **CR-3 (fresh-process CE-04 variant + D4 item 4 extension).** Add a D5+
  case: two independent owner processes each composing their own capability
  (minted per-process from the same staged preparation as the current owner)
  racing `RequestRelease` against the same journal, with the
  per-lease stop-refused/signal machinery extension of the D1 pattern; the
  expected shape is exactly one FD4 attempt and one `Refused`/`Duplicate`
  outcome pair, mirroring D4-9 but across processes.
- **CR-3b (stop-refused re-inheritance).** Add an explicit D5+ case extending
  the D1 lease fixture stop/signal machinery so that a lease acquired after a
  REFUSED stop generation is evidence of no re-inherited stop state (the D4
  suite only demonstrates the failed-stop replacement-release path).
- **CR-4 (surrogate prerequisite).** The owner's `ReleasePrerequisites`
  contains `Environment.ProcessId > 0` (always true on Linux). Disclose in
  the D4.0 design doc that the "caller liveness" prerequisite is a
  test-assembly-consistent surrogate; when a production owner exists
  (WP08/WP10) this stub must be replaced or re-derived from the real owner
  path.
- **CR-5 (gate writer identity).** The storage gate waits for any writer of
  `go.txt` with no uid/pid binding. Keep as a fixture-only control; if the
  same gate shape survives into a production-context review, require writer
  identity binding.
- **CR-6 (stop deadline policy).** D4-7 does not verify that the persisted
  `StopDeadlineNanoseconds` equals `now + StopConfirmationAllowanceNanoseconds`
  (it only mirrors and reports). A small assertion tightening is suggested.
- **CR-7 (witness append races).** Owner + replacement + supervisor append to
  one witness file concurrently. Lines stayed well-formed in both producer
  and reviewer runs; consider serializing per-case appends behind a
  `flock` on the witness file for D5 hardening.
- **CR-8 (xunit within-class parallelism).** The 15 facts of the D4 class
  run concurrently under xunit.v3 default settings; the Q3 harness
  discipline should either state the expected tolerance for
  `Category=ProcessGlobalReaping` suites (as D4 demonstrates) or force
  serialization. Record in `docs/testing-and-ci.md` as the runner protocol
  for the category.
- **CR-9 (owner start-time comparison).** Strengthen the pid-reuse assert to
  compare the exact `pid:starttime` identity against `owner.ready` (the
  assertion currently only checks the PID prefix).
- **CR-11 (legacy snapshot version).** `OwnerSession.MirrorRegistered` writes
  `OwnershipSnapshotCodec.LegacyVersion` (2); either intentionally document
  the legacy mirror or switch to `CurrentVersion` (3) to match the frozen
  v3 claim in `review-binding.txt`.

## 3. D5 handoff (unchanged obligations, sharpened)

1. **Real FD5 status negatives** against the real qualification coordinator
   (`exec_failed`, `exec_confirmed`, `protocol_failed`,
   `exec_confirmed_root`, `root_unconfirmed`, 20/21/22 mapping, invalid
   identity/sequence negatives) remain unfinished and MUST NOT be counted
   from D4's incidental regression coverage.
2. **Three formerly-`Unavailable` release-path facts.** Artifacts exist
   (`wp05-qualification-coordinator`
   `e20903cce6061ecc2b152f9e7b4ccb389edeee54047d96f1f3b66dd5195be0b9`,
   WP04 fixture/coordinator/filter all frozen and unchanged), and both the
   producer's and this reviewer's regression runs show 66/0/0 with the three
   tests executing. The D5 conversion (formal D5 rows in the qualification
   matrix + evidence binding) remains REQUIRED; running green inside a
   filter is not the conversion itself.
3. **C1-C4 decisions (D6)** remain the holding gate for release
   qualification; D4/D5 evidence cannot substitute for the C1-C4 holders'
   decisions.
4. **No production remediation may ride on D4/D5 evidence** — any production
   source change must be its own evidence slice.

## 4. Bounds preserved in this review

- No adapter, authorization-engine, workflow or other third-party dependency
  was introduced by the D4 slice or the review.
- The review did not modify R12 sources (verified: `git diff R12 de1ee93 -- src`
  empty; `git diff de1ee93 8254cf8 -- tests/docs` only covers the D4
  evidence commits; the branch tip's `tests` tree is identical to the D4.2b
  evidence source HEAD).
- Reviewer rerun artifacts are preserved in
  `docs/reviews/gagamba-linux-m1-wp05-d4-review-artifacts/` and refer only to
  disposable fixture-owned state; no real target was executed at any point.
