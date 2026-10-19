# WP05 Release Fixtures and Counterexample Probes - 2026-10-20

Status: implementation and diagnostic evidence only; no release qualification,
no production remediation, no target execution and no gate acceptance.

Disposition: `WP05 RELEASE FIXTURES AND DIAGNOSTIC PROBES IMPLEMENTED - C1-C4
UNRESOLVED; C7/C9 PARTIAL; NO RELEASE QUALIFICATION ESTABLISHED`.

## 1. Scope and bindings

This slice implements the test-only fixture work authorized by the independent
adversarial plan review and runs counterexample-first diagnostic probes against
the exact R12 product source. It does not decide C1-C4, does not modify the
product, and does not execute a real target.

- Product: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Product baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`.
- Planning base: `24e7498f1940c8e985d229a48b208fe4a0065b26`.
- Test-only commits: `dbfecada5d1f6e6c7b852dda5f48b6971e4c650c` (fresh-process
  lease fixture) and `dc4c372b180abef1b27038d94f49af90226fa548` (contract probes).
  Evidence is bound to `dc4c372`; the Linux checkout was fetched to that exact
  commit from a bundle based on R12.
- Review: `e4b0a798e944ddfb144901c63b0c444e7980533b`; required changes,
  adversarial review and counterexample-register blobs are in `review-binding.txt`.
  Disposition `PLAN ANALYSIS ACCEPTED WITH REQUIRED CHANGES`.
- Frozen harness Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f` is unchanged and
  not reopened.

`source-head.txt` records the exact branch/HEAD/tree, R12 base, empty
`src`/`native` product diff, and SHA-256 of all five added test files.

## 2. Implemented work

### 2.1 Contract probes - `Wp05ReleaseContractProbes.cs`

Thirteen in-process, test-only facts use the real R12 `ReleaseArbiter`,
`OwnershipJournal`, `ReleaseObservationBuffer`, `CoordinatorStatusReader` and
`Reconciler` with fake storage/clock/domain/transport only where the existing
test seams require it. No production behavior was changed.

- C5: stop accepted with an unavailable monotonic clock and with deadline
  overflow; both record runtime acceptance and no durable stop, and release is
  refused. The probe also records that the capability remains live.
- C6: ambiguous post-content seal-release failure is distinguished from a
  definite write failure. The ambiguous case remains `NotCommitted` at the
  runtime decision, requests no stop, and recovery later classifies the durable
  intent as `MayHaveExecuted`; the definite case leaves no release.
- C1 diagnostic (CE-02): the ambiguous case reproduces the component-level
  false-definite decision/no-stop wedge. This is a probe, not an accepted
  decision or a production execution.
- C2 diagnostic (CE-03): stop-first leaves the capability live; a durable stop
  blocks a second arbiter, while a failed durable-stop write does not, and the
  second arbiter can release once. The accepted single-arbiter composition does
  not expose this; C2 remains unresolved.
- C3 diagnostic: `Reconciler` uses its caller-supplied allowance after the
  persisted stop deadline has expired; the persisted deadline has no consumer.
- C4 diagnostic (CE-01/CE-04): the concurrent duplicate loser receives
  `Refused(NotCommitted)` while the winner releases exactly once; a later caller
  receives `Duplicate`. This is the deterministic three-caller shape and remains
  a contract decision.
- C9 partial: buffer flush before `MarkDurableCommitted` does not persist;
  committed-write failure creates no observation; synthetic reader frames with
  wrong identity/sequence, EOF, or malformed kind are refused.

### 2.2 Fresh-process lease fixture - C7

`tests/Fixtures/Wp05LeaseFixture` is a non-packable test-only console project.
It references the test assembly only; the production build and public runtime
cannot select it. `Wp05LeaseFixtureHost` opens the real `OwnershipJournal` on a
temporary directory. The owner acquires the journal writer fence and holds it;
the supervisor starts a separate replacement after SIGKILL. The replacement must
call `TryAcquireWriter` afresh and seed its own baseline. No lease, capability
or writer is passed to the replacement.

`lease-trace.txt` shows: owner lock FD present, supervisor lock FD absent,
owner killed, replacement exit 0 with a new PID and sequence. The xUnit probe
additionally asserts the owner FD and supervisor absence and checks that the
replacement PID differs.

This is a lease/process-separation fixture, not the full G3-C release-owner
SIGKILL-at-release-barrier supervisor. That missing release supervisor, witness
ledger and coordinator acceptance ledger remain required.

### 2.3 C8 dependency linearization

`plan-c8.diff` records the plan edit: the review binding and C1-C4 acceptance
block are explicit in prerequisites and the expected sequence. C8 is docs-only;
no runtime test applies.

## 3. Executed results

### 3.1 Linux supported profile

Executed as uid 1000 `gaga` via the root-only operator launcher in
`/sys/fs/cgroup/gaga-delegated/host`; zero inheritable/permitted/effective/
ambient capabilities; no target fork. Exact commands are in `commands.txt`.

| Run | Result |
|---|---|
| Fixture build | 0 errors, 10 warnings (repository has no remote / no SourceLink); `fixture-build.log` |
| `Wp05ReleaseContractProbes` | **13 passed, 0 failed, 0 skipped**; `contract-probes.log` |
| `Wp05LeaseProcessProbes` | **1 passed, 0 failed, 0 skipped** (7 s); `lease-process.log` |
| `Native.ReleaseProtocolTests` (valid qualification-coordinator status) | **4 passed, 0 failed, 0 skipped**; `native-release-protocol.log` |
| Five existing focused classes | **49 passed, 0 failed, 3 skipped**; `focused-existing.log` |

The three skips are `Wp05ReleasePathIndependentQualificationTests` and are
classified `Unavailable`: the checkout lacks the generated Linux native fixture
and WP04 coordinator artifacts. They are not counted as passed and cannot
support a release claim. The other focused classes (`ReleaseArbiterTests`,
`CoordinatorStatusReaderTests`, `Native.ReleaseProtocolTests`, `ReconcilerTests`)
ran and passed.

### 3.2 Windows implementation-time check

| Run | Result |
|---|---|
| `Wp05ReleaseContractProbes` | **13 passed, 0 failed, 0 skipped**; `contract-probes-windows.log` |
| `Wp05LeaseProcessProbes` | **0 passed, 0 failed, 1 skipped** (`linux only`); `lease-process-windows.log` |

Windows is not Linux qualification evidence. The C7 skip is expected and is
not counted as a pass.

### 3.3 Environment identity

`environment.txt`: Ubuntu WSL2 kernel `6.6.114.1-microsoft-standard-WSL2`,
boot `69d053fb-0fd9-4209-aa5b-be128424ed34`, uid 1000 `gaga`, all capability
sets zero, cgroup `/gaga-delegated/host`, .NET `10.0.112`, GCC `15.2.0`,
Bubblewrap `0.11.1`, frozen filter SHA-256
`2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`.
The source checkout was `/home/gaga/qualification/gagamba-r12`.

## 4. Cleanup

`cleanup.txt`, `cgroup-host.procs`, `lease-temp-residue.txt`,
`fixture-processes.txt` and `testhost-processes.txt` were captured after the
runs. The delegated host procs file is empty, no `gagamba-wp05-lease-*` temp
directory remains, no fixture process remains, and no target exists. The test
hosts and manual owner/replacement exited. The cgroup/session anchor itself was
retained for the reviewer; no cleanup of the operator environment is claimed
here.

## 5. Explicit exclusions

- No C1-C4 resolution. No production remediation, no `src`/`native` diff, no
  owner release wiring (WP08/WP10).
- No real target execution, no production post-release status, no full G3/WP05
  acceptance, no G3-A/B/D/E acceptance tag.
- C7 full release-owner crash/no-resend and C9 qualification-coordinator
  negative/descriptor evidence are incomplete. C9's current negatives are
  synthetic source/buffer cases; the native valid-status path is the existing
  test-only qualification coordinator.
- The three unavailable focused facts, any skipped case, and the Windows-only
  result are not release evidence.
- No full M1 assembly, repeated supported-profile run, macOS/Windows native run,
  or independent reviewer execution was performed in this slice.
- The review documents are bound by commit/blob, not merged into this branch.

## 6. Evidence inventory

Compacted logs and text are in
`docs/evidence/linux-m1/wp05-release-fixtures-counterexample-probes-artifacts/`.
`SHA256SUMS` binds the artifact bytes. The main test files are bound by
`source-head.txt`; the product source is bound by R12 and the empty product diff.

Next required step: independent GLM/Beam review of this slice and the C1-C4
decisions, then completion of the C7 release-owner supervisor and C9 native
status negatives before any gate acceptance.
