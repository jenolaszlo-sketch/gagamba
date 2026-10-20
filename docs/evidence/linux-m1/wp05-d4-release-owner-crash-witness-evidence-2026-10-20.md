# WP05 D4 Fresh-Process Release Owner, FD4 Witnesses and No-Resend Diagnostics - 2026-10-20

Status: test-only D4 implementation and diagnostic evidence. Independent review
is required. No production remediation, target execution, owner integration,
D5 native-status qualification or release acceptance is claimed.

Disposition: `WP05 D4 FRESH-PROCESS RELEASE FIXTURES AND NO-RESEND DIAGNOSTICS
COMPLETE - INDEPENDENT REVIEW REQUIRED; RELEASE NOT QUALIFIED`.

## A. Repository binding

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`; isolated D4
  worktree `C:\Users\Laszlos\source\repos\gagamba-d4`.
- Branch: `implementation/wp05-d4-release-owner-witness`.
- Preservation base: `7dd5ffa6c2ecaacc37022adb9a7b6c333695eef0`.
- Product: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`.
- D4.0 `26c73db350ed2d1c08d0be06763430aa0124ecad` design/feasibility.
- D4.1 `acebcc83f541e1c4d4216578a2b9da45b2753b25` owner fixture.
- D4.2 `e36a9ea4c67c415d9bf7c913d217a32be8ddd0f4` supervisor/witness/crash
  probes.
- D4.2b `de1ee93fdfa9cc29cf82a79f6e6dac9bfea7d753` cross-process intent retry
  refusal; evidence source HEAD.
- Production diff R12..HEAD for `src` and production `native` is empty.
- Test-only changes: `Wp05D4OwnerHost.cs`, `Wp05D4SupervisorTests.cs`,
  `tests/Fixtures/Wp05D4Owner`, D4 design/evidence documents.
- Git status in the D4 worktree was clean before evidence generation; the Linux
  checkout was fetched from a bundle to exact `de1ee93` and was clean.

## B. Feasibility and seams

The design note `docs/plans/gagamba-linux-m1-wp05-d4-fixture-design.md` maps
each required boundary to the R12 implementation. The implemented seams are:

- `ReleaseArbiter.TestBarrier`: `after-intent`, `before-consume`,
  `after-consume`, `after-write`, `after-committed`, `after-stop`.
- Test-only `IJournalStorage` wrapper around the real `JournalFileStorage` for
  pre-intent, candidate-write, stop-write and seal-failure gates/faults. The
  wrapper does not replace the journal; it delegates to real file operations.
- Real `HeldExecution` and the production pre-exec coordinator for the actual
  FD4 path. No target is forked.
- Production `OwnershipJournal.TryAcquireWriter`, `Resolve`/`TryRecoverCommit`
  and `ReleaseRecoveryClassifier` for replacement recovery.
- Test-only `IReleaseWriteChannel` witness wrapping `HeldExecution`; separate
  test-only capability/interface transport for CE-01/CE-03 diagnostics.

Supported without production changes: all D4-C01..C08 boundaries and CE
variants below. Limitations are explicit: storage gates are test-only seam
points, not kernel breakpoints; coordinator acceptance after a kill is unknown
unless the owner survives to observe it; the production coordinator provides
no receipt ledger.

## C. Process supervisor

Architecture:

```text
supervisor (xUnit process)
  -> fresh Wp05D4Owner process
       real OwnershipJournal fence
       real PreparedTransaction
       real HeldExecution + production coordinator
       test-only witness/barrier/fault files
  -> SIGKILL verified against direct child + /proc start identity
  -> fresh replacement process
       only journal path (+ source for test-only capability diagnostic)
       fresh TryAcquireWriter / TryRecoverCommit / classifier
       no capability, HeldExecution, FD4, original lease or in-memory arbiter
```

Per-case owner/replacement PIDs and start identities are in `processes.txt`.
The supervisor records its own events and never acquires the journal writer.
Owner death is SIGKILL against the verified direct child; exit 137 is asserted.
The replacement runs only after the owner has exited and the fence has been
released. D4-12 started a replacement while the owner was live and received a
writer refusal.

Observed owner fields include `owner.ready` journal path, journal sequence,
control FD, child cgroup, identity and start identity. The supervisor checks the
owner lock FD, supervisor lock absence, owner control FD and replacement
descriptor/lease separation. Cleanup removes only the test-owned child cgroup
and temporary case tree; the delegated host parent and session anchor are
untouched.

## D. Witnesses

Two lanes are preserved:

- **Lane A - test-only interface witness.** `PreWriteWitnessAcknowledged`,
  `WriteReturned`, `TestTransportAttempt`, owner/supervisor/termination and
  recovery events are append-only records. A pre-write record is not delivery;
  a test-interface attempt is not FD4.
- **Lane B - real FD4/native path.** D4-1 and D4-9 observed one real
  `HeldExecution.TryWriteRelease` returning `Delivered`, production coordinator
  exit 0, and durable Committed. D4-3 observed one real write return and durable
  Intent, then the owner was killed before reading coordinator acceptance:
  acceptance is unknown.

No post-crash receipt is inferred. A write return is not coordinator acceptance.
D4-8 and D4-2b use test-only capabilities/interface transport and are labelled
diagnostic, not physical FD4 or production authority.

## E. Crash matrix

Full case records are in `compact/`; the distilled matrix is
`crash-matrix.txt`.

| ID | Case | Observation | Classification |
|---|---|---|---|
| D4-C01 | D4-10 | Gate before intent seal; Prepared/no Release; no FD4 | PASS - COMPONENT GATE |
| D4-C02 | D4-11 | Gate at candidate write after seal; actual journal uncertain; no FD4 | PASS - ACTUAL STATE PRESERVED |
| D4-C03 | D4-2 | SIGKILL after durable Intent; replacement Intent/MayHaveExecuted; no resend | PASS - REAL PROCESS |
| D4-C04 | D4-5 | SIGKILL after capability consume; Intent; no FD4 | PASS - REAL PROCESS |
| D4-C05 | D4-3 | SIGKILL after real FD4 return; Intent; one pre-write/write; acceptance unknown | PASS - REAL FD4 WRITE OBSERVED |
| D4-C06 | D4-4 | SIGKILL after Committed; replacement Committed; no second write | PASS - REAL PROCESS |
| D4-C07 | D4-7 / D4-8 / D4-13 | Durable stop / failed durable stop / in-flight stop write | PASS - REAL PROCESS + COMPONENT GATES |
| D4-C08 | D4-14 | Two recovery contenders; one writer, one refusal; no release | PASS - REAL PROCESS/LOCK |

No case produced a second pre-write witness, a second test-interface attempt, or
a second production coordinator exit 0.

## F. Counterexamples

- **CE-01 retention/retry:** D4-2b kills the owner after durable Intent and a
  fresh replacement with a newly minted test-only capability attempts release.
  It is `Refused(NotCommitted)` with 0 transport attempts; the durable Intent
  remains. D4-9 also shows the owner-process concurrent loser `Refused`, later
  caller `Duplicate`, and exactly one FD4.
- **CE-02 ambiguous persistence:** D4-6 injects a post-content seal-unlink
  failure in the owner. The owner reports `Refused(NotCommitted)`, 0 transport
  attempts. After SIGKILL, `ReadLatest` fails on the seal; replacement
  `TryRecoverCommit` recovers Intent, classifies `MayHaveExecuted`, and makes no
  release attempt.
- **CE-03 non-durable stop window:** D4-8 accepts a stop at runtime with a
  test-only failed durable write, kills the owner, and observes no durable stop.
  Recovery observes no stop; a replacement release is **diagnostic only**,
  using a newly minted test-only capability and interface transport with no
  original FD4. It is not a production takeover or privilege-bypass claim.
- **CE-04 concurrent duplicate:** D4-9 uses an owner-process two-caller
  interleaving. Winner `Released`, concurrent loser `Refused(NotCommitted)`,
  later caller `Duplicate`, one real FD4, production coordinator exit 0.

C1-C4 remain unresolved. No D4 result closes or selects any C1-C4 contract.

## G. Execution and regressions

Exact Linux environment: Ubuntu WSL2, kernel
`6.6.114.1-microsoft-standard-WSL2`, boot
`69d053fb-0fd9-4209-aa5b-be128424ed34`, uid 1000 `gaga`, delegated cgroup
`/gaga-delegated/host`, all capability sets zero, .NET `10.0.112`, GCC
`15.2.0`, Bubblewrap `0.11.1`, filter
`2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`.

- D4 supervisor run 1: **15 passed / 0 failed / 0 skipped**.
- D4 supervisor run 2: **15 passed / 0 failed / 0 skipped**.
- Focused regressions: **66 passed / 0 failed / 0 skipped**, covering the D1
  lease probe, D2 contract probes, release arbiter/status/reconciler classes,
  native release protocol, and the three previously unavailable
  `Wp05ReleasePathIndependentQualificationTests` as an incidental regression.
  Those three are not promoted to D5 qualification.
- Fixture build: 0 errors; warning-only SourceLink/no-remote output.
- No full assembly, D5 native negative suite, target execution or G3 acceptance
  was run.
- Cleanup evidence: delegated host cgroup has no test-owned child; no
  `Wp05D4Owner` or `exec-coordinator` process remains; no `/tmp/gagamba-d4-*`
  residue; test case trees were intentionally preserved under evidence.

## H. Evidence preservation

Report: this file.

Artifact directory:
`docs/evidence/linux-m1/wp05-d4-release-owner-crash-witness-artifacts/`.

It contains:

- exact D4 run logs (`d4-supervisor-run1.log`, `d4-supervisor-run2.log`) and
  focused regression log;
- `source-head.txt`, `environment.txt`, `fixture-build.log`, `processes.txt`;
- `compact/` case records: ready/barrier/result/witness, replacement results,
  actual journal slots present, source identities and case index;
- `coverage-map.txt`, `crash-matrix.txt`, `witness-semantics.txt`,
  `exclusions.txt`, `review-binding.txt`, cleanup and `SHA256SUMS`.

Test file SHA-256 values are recorded in `source-head.txt`. The artifact
manifest must be verified against committed blobs after the evidence commit;
the report intentionally does not claim a hash until that verification.

## I. Remaining work

1. Independent GLM/Beam review of D4 design, owner/supervisor, witnesses,
   crash cases and evidence.
2. D5 native FD5 negative statuses and the three artifact-dependent release-path
   facts as formal D5 rows.
3. D6 C1-C4 decisions and any separately authorized production remediation.
4. WP08 owner orchestration and WP10 target execution/production status.
5. No D4 result is a release gate acceptance.

## J. Frozen claims

- R12 production is unchanged; no production/native/eng source diff.
- Frozen Q3 harness and its original oracles are unchanged.
- D1-D3 commits/evidence are unchanged.
- No real target was executed.
- C1-C4 remain unresolved.
- No D5, G3 or WP05 release acceptance.
