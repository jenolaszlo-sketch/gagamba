# WP05 D4 Independent Crash/Witness Review — Canonical Preservation Checkpoint — 2026-10-20

Status: documentation-only preservation of the completed independent GLM review
of the WP05 D4 tranche. This checkpoint preserves the reviewer's Option B
disposition and evidence verbatim in binding form; it implements no correction,
runs no test, changes no production or test source, modifies no Q1–Q5 commit,
and accepts no gate. The authoritative review documents live on the review
branch `review/wp05-d4-crash-witness-qualification` (Q1–Q5 untouched) and are
referenced, not duplicated, by this checkpoint.

## A. Revision binding

- Product/frozen: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`. Independent harness review
  `36a124d7dff98ecfaf956340767fe1768ad5b17f`
  (`WP05 HARNESS QUALIFIED (bounded)`) remains frozen and unchanged by D4 or
  this preservation.
- Prior preservation/D4 base: `7dd5ffa6c2ecaacc37022adb9a7b6c333695eef0`
  (branch `preservation/wp05-d1-d3-independent-fixture-review`).
- D4 implementation chain on
  `implementation/wp05-d4-release-owner-witness` (exact tip = manifest
  verification commit):
  - D4.0 design/feasibility `26c73db350ed2d1c08d0be06763430aa0124ecad`
  - D4.1 real release-owner fixture `acebcc83f541e1c4d4216578a2b9da45b2753b25`
  - D4.2 supervisor + FD4 witness + crash probes
    `e36a9ea4c67c415d9bf7c913d217a32be8ddd0f4`
  - D4.2b cross-process Intent retry refusal (evidence source HEAD)
    `de1ee93fdfa9cc29cf82a79f6e6dac9bfea7d753`
  - Evidence package `cebee05c63abab342d5643ee5186596d244675ae`
  - Manifest verification (exact D4 final HEAD)
    `8254cf89f091eb5baf305d125e00cd2bf61a1ca1` (tree
    `42b8b8069edf08e11e7b67af09a133a45e809b27`).
- Independent review chain on `review/wp05-d4-crash-witness-qualification`
  (exact tip = Q5):
  - Q1 `0734ed8a80d6d6bf3f4ad315dbf22ea708d8b94c` — main review + rerun
    evidence (39 files)
  - Q2 `6caf52ea42c673a53c28fa32779c2eb1d1285c8f` — crash-witness evidence
    matrix and termination-oracle reconciliation
  - Q3 `bc332370f2c651f64fc1ac6f27293a68677a7e63` — required corrections and
    D5 handoff, including the CR register
  - Q4 `adbd93045cc9c3319fc08bb1675f3af664bf1acd` — verification corrections
    (source SHA wording, terminology, environment binding)
  - Q5 `310484a5e82d4a81b0909e07281511cb7bc4c315` — canonical handoff and
    work-queue review status (tree
    `7d7a6bedf33ea8410a2db69dc144cdef7fb2dc55`).
  - Verified ancestry: Q1's parent is exact D4 final
    `8254cf89f091eb5baf305d125e00cd2bf61a1ca1`; Q2←Q1, Q3←Q2, Q4←Q3, Q5←Q4
    (single-parent chain, `rev-list --count 8254cf89..310484a5` = 5). Q1–Q5
    are preserved unchanged; no amend/rebase/squash/cherry-pick was performed.
- This preservation branch: `preservation/wp05-d4-independent-crash-witness-review`,
  commit parent = exact D4 final `8254cf89f091eb5baf305d125e00cd2bf61a1ca1`
  (not based on Q5; reviewer commits are not cherry-picked).
- Review-document blob identities at Q5 (for exact cross-reference):
  - Main review
    `docs/reviews/gagamba-linux-m1-wp05-d4-independent-crash-witness-review.md`
    blob `d512b75f8a2b398cff0f3ad8a18d0684e7423d15`
  - Evidence matrix
    `docs/reviews/gagamba-linux-m1-wp05-d4-crash-witness-evidence-matrix.md`
    blob `c963d5b935d88305460882b96dff5307e9ff1411`
  - Required corrections + D5 handoff
    `docs/reviews/gagamba-linux-m1-wp05-d4-required-corrections-and-d5-handoff.md`
    blob `4b5f112801b63867ef6b63604d5e2121a70f9a11`.
- Reviewer rerun artifact set:
  `docs/reviews/gagamba-linux-m1-wp05-d4-review-artifacts/` (committed on the
  review branch; includes run logs/summaries, per-case `myruns/`, SIGKILL
  controls, `manifest-verify.txt`, `final-binding.txt`, `environment-review.txt`,
  `leftover-procs-after.txt`).

## B. Independent result

Disposition preserved verbatim (Option B — evidence supported with
qualifications):

`WP05 D4 FRESH-PROCESS RELEASE FIXTURES AND NO-RESEND DIAGNOSTICS REVIEWED AND INDEPENDENTLY REPRODUCED - EVIDENCE SUPPORTED WITH QUALIFICATIONS; CORRECTIONS AND D5 ITEMS REQUIRED - C1-C4 UNRESOLVED; RELEASE NOT QUALIFIED`

## C. Native reproduction (verified against committed logs)

- Producer (D4 evidence, exact R12 production state, commit `cebee05..8254cf89`):
  D4 supervisor **15 passed / 0 failed / 0 skipped** twice
  (`d4-supervisor-run1.log`, `d4-supervisor-run2.log`); focused regressions
  **66 passed / 0 failed / 0 skipped** (`regressions.log`); evidence
  manifest **133/133 files OK / 0 failed** (reviewer re-verification of the
  committed artifact tree at `8254cf89...` via `git archive` + `sha256sum -c`,
  `manifest-verify.txt`); report SHA
  `53c76c575fdf74c9543614bb629a39257b9255c81b2d963102aeb3157d79fa58` re-hashed
  from committed bytes (`final-binding.txt`) and matching the producer's
  `report-sha256.txt`. Reviewer-bind artifact `report-hash.txt` preserves a
  first-invocation `sha256sum` directory error verbatim; the successful report
  hash binding is `final-binding.txt` — declared scope, preserved as-is.
- Reviewer (independent reruns on the established Linux qualification topology,
  frozen
  launcher `/root/wp05/run-as-gaga-in-host.sh`, runtime-only, no new probes):
  run1 **15/0/0** (`drr-run1-cg.summary`, elapsed 11541 ms, xunit 7 s),
  run2 **15/0/0** (`drr-run2-cg.summary`, 11320 ms, xunit 7 s), focused
  regressions **66/0/0 with zero skips** (`drr-regressions-cg.summary`,
  10864 ms, xunit 6 s). All counts reproduce the producer's exactly. No separate
  reviewer `SHA256SUMS` self-manifest is part of the committed set; the
  reviewer's binding scope is the producer-manifest re-verification plus the
  source-blob hashes in `final-binding.txt` (declared scope; no additional
  checksum requirement is invented here).
- Execution environment, recorded from committed evidence as the values in
  force at run/review time (not asserted to still be current):
  Linux/WSL2, kernel `6.6.114.1-microsoft-standard-WSL2`, boot
  `69d053fb-0fd9-4209-aa5b-be128424ed34`, non-root `gaga` (uid 1000), CapInh/
  CapPrm/CapEff/CapAmb = `0000000000000000`, cgroup
  `0::/gaga-delegated/host` under delegated parent
  `/sys/fs/cgroup/gaga-delegated`, .NET 10.0.112, gcc 15.2.0, Bubblewrap 0.11.1,
  frozen native identities: fixture `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305`,
  exec-coordinator `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b`,
  seccomp filter `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`,
  wp05-qualification-coordinator `e20903cce6061ecc2b152f9e7b4ccb389edeee54047d96f1f3b66dd5195be0b9`,
  and frozen Bubblewrap pin `523da3e7` (as recorded in the review).
  Reruns were executed at exact `de1ee93fdfa9cc29cf82a79f6e6dac9bfea7d753`
  (tests tree identical to `8254cf89...`; tests-tree diff empty). No
  cross-host, cross-kernel, or cross-distribution claims are made.
- Cleanup: all D4 cases' test-owned cgroups removed, empty cgroup host procs,
  no coordinator/test-owner processes, no temp residue (producer `cleanup.txt`/
  `test-owner-processes.txt`/`coordinator-processes.txt`/`temp-residue.txt`);
  reviewer `leftover-procs-after.txt` shows none. No target was ever executed.

## D. Bounded observations preserved

- **Real owner termination and the SIGKILL oracle.** The fixture kills its
  direct-child .NET owner with `Process.Kill(entireProcessTree:false)` and
  asserts `ExitCode == 137`. The independent review verified from upstream
  .NET 10 sources that `Kill()` maps to `kill(pid, SIGKILL)` and that a signal
  death's `ExitCode` encodes `128 + signal`, and added its own kernel/BCL
  controls: a `wait4` probe on the frozen profile reports
  `signaled=1 termsig=9` (`wait4probe.txt`); a SIGTERM-trapping child is still
  killed and reports `137` via `.Kill()` (`dnet-kill-control.txt`); a child
  that voluntarily exits 137 also reports `137`
  (`dnet-self-exit-control.txt`); a directly-SIGTERM-targeted child exits 143
  (`term-control.txt`). The accepted **measured** claim is therefore:
  kernel-anchored SIGKILL delivery with a bounded, audited public exit-code
  surface — NOT "exit 137 always proves SIGKILL", and NOT an invalid fixture.
  Every individual D4 owner termination was not separately re-classified with
  raw `waitid`/`wait4` in the fixture itself; that classification gap is
  preserved as CR-1.
- **Process and journal-fence separation.** The original owner holds the real
  `OwnershipJournal` writer fence; the supervisor never owns the journal
  writer (it transiently probes the fence to prove owner death — wording
  limitation recorded as CR-2); the owner is killed at controlled boundaries
  (`after-intent`, `after-consume`, `after-write`, `after-committed`, plus
  test-only storage gates at pre-intent/persistence seams); a replacement
  process starts independently and fresh-acquires the writer; two recovery
  contenders are exercised (D4-14); no replacement ever receives the original
  capability, `HeldExecution`, or FD4 writer; test-owned cleanup completed.
  These are fixture-level observations, not production WP08 owner-orchestration
  acceptance.
- **FD4 witness lanes.** Lane A = test-only transport-interface observations
  (`TestTransportAttempt` via `D4RecordingTransport`; Lane-B-style reporting
  through the arbiter-lite `replace-release`/`retry-release` diagnostic with
  a newly minted test-only capability in D4-8/D4-2b); Lane B = real
  `HeldExecution.TryWriteRelease` with the production pre-exec coordinator
  (`D4WitnessingTransport`), recording `PreWriteWitnessAcknowledged`,
  `WriteReturned`, and `CoordinatorExitObserved` after real
  `WrapperReaper.TryReap`. Distinction preserved: a pre-write witness is not
  proof of physical delivery; a successful write is not automatically proof
  of native acceptance; coordinator exit 0 is interpreted only within the
  native coordinator's contract; durable `Committed` is evidence, not a
  delivery authorization.
- **Post-write crash uncertainty.** D4-3/D4-4 report one real FD4 write with
  durable Intent, original-owner death, no durable Committed, and acceptance
  remaining **unknown**. This ambiguity is preserved exactly: no confirmed
  execution, no confirmed non-execution, no permission to resend.
- **No-resend scope.** The independent reruns reproduced no second observed
  pre-write witness, no second test-transport invocation, and no second
  production-coordinator exit 0 across all D4 cases, matching the producer's
  compact index. This is an instrumentation-scoped at-most-once observation
  over executed fixtures, not a universal exactly-once release guarantee, and
  does not infer the presence or absence of a physical effect from a final
  journal record alone.

## E. Remaining correction register (preserved as written)

Source: full detail in the committed correction document (blob
`4b5f112801b63867ef6b63604d5e2121a70f9a11`, review branch), bounded by the
main review's §G.2 (blob `d512b75f...`). Not shortened; register is intact as
actually present. Note: the correction document explicitly states **none of
the CR items blocks the existing bounded D4 diagnostic evidence**; the
register is non-contiguous as written — CR-10 exists only in the main
review's §G.2 with the "no change required" classification, and the final
numbered register (CR-1..CR-11 plus CR-3b, with CR-10 meta-presence only in the
main review) is preserved exactly as committed, not renumbered.

| ID | Finding (as written) | Severity/owner class | Blocks D4 evidence? | Blocks future qualification? | Owner/next package |
|---|---|---|---|---|---|
| CR-1 | SIGKILL oracle standard: public `ExitCode == 137` cannot classify `WIFSIGNALED` directly; accepted as measured with reviewer controls, but D5/D6 must bind the kill oracle to `wait4/waitid` status or a pidfd teardown/oracle, or commit the control artifacts as a durable standard note | Standard hardening; native/test-harness owner (D5+) | No | Yes (for formal D5/D6 termination claims) | D5 native status; D6 acceptance boundary |
| CR-2 | Supervisor class-header wording: "supervisor never acquires an owner journal lease" conflicts with the transient `AssertJournalFenceReleased` probe and the supervisor's coordinator-exit observer; reword | Documentation text fix | No | No (wording must not imply scope) | Next test-only docs pass |
| CR-3 | Fresh-process CE-04 second-owner case (two independent owner processes each minting their own release capability racing `RequestRelease` against the same journal) is NOT implemented; only in-owner (D4-9) and writer-fence contention (D4-14) exist | Required missing case; D5+ | No | Yes | D5 fixture work |
| CR-3b | Stop-machinery re-inheritance: an explicit case extending the D1 lease stop/signal machinery so a lease acquired after a REFUSED stop generation is shown as free of re-inherited stop state | Required missing case; D5+ | No | Yes | D5 fixture work (with CR-3) |
| CR-4 | Trivially-true `Environment.ProcessId > 0` prerequisite stub on the owner; disclose in the design doc / replace or re-derive from a real owner path when a production owner exists | Design-doc disclosure | No | At WP08/WP10 boundary | Design doc next edit; WP08/WP10 |
| CR-5 | Storage gate `go.txt` has no writer identity (uid/pid) binding; keep fixture-only at test scope; production-context gate designs must bind writer identity | Documentation boundary | No | Yes, for any future production-context gate | D5/D6 design notes |
| CR-6 | D4-7 does not verify the persisted `StopDeadlineNanoseconds` policy value against the allowed stop window (it only mirrors the journal value) | Test hardening | No | No | D5 assertion tightening |
| CR-7 | Concurrent witness-appends by owner/replacement/supervisor are racy (lines well-formed in both runs); consider serializing per-case appends behind a per-case lock or `flock` | Test hardening | No | No | D5 hardening |
| CR-8 | xunit.v3 parallelizes facts within one class even at "one collection": the D4 profile disclosed this explicitly (facts started within ~3 ms of each other in a probe); the Q3 harness discipline should either document tolerance for parallel fan-out within the `Category=ProcessGlobalReaping` suite or serialize the class via a shared `[Collection]` ordering rule; record in `docs/testing-and-ci.md` | Documentation + runner protocol | No | No | Q3 harness discipline update |
| CR-9 | The pid-reuse assertion compares the recorded `owner.ready` identity only by PID prefix, not by the full `pid:starttime` start-time digits; strengthen to compare the full identity (or explicitly document that identity-vs-prefix is sufficient under a recorded model) | Test hardening | No | No | D5 hardening |
| CR-10 | (located only in the main review §G.2 item 10, absent from the correction document — register is non-contiguous as written) — "no `UtimeStable`/`waitpid` code eval on SIGKILL vs dotnet's combo": the consumer expectations preserve crash state while `WaitForExitAsync` reaps the child — .NET's targeted reaping is the single writer of exit state; `WaitForExitAsync` blocks the supervisor thread only after `Kill`, reducing PID-reuse exposure. Acceptable as-is; no change required. | None / informational (no change required) | No | Covered by CR-1's scope | None |
| CR-11 | `OwnerSession.MirrorRegistered` writes `OwnershipSnapshotCodec.LegacyVersion` (2) rather than `CurrentVersion` (3), diverging from the frozen `review-binding.txt` v3 claim; align or explicitly document | Fidelity nit | No | No | Documentation-only or tiny fixture edit |

## F. Remaining engineering work (not authorized by this preservation)

- D5 (from the reviewer's committed handoff document §3):
  1. Real FD5 negative-status qualification on the real qualification
     coordinator (exec_failed, exec_confirmed, protocol_failed,
     exec_confirmed_root, root_unconfirmed, 20/21/22 mapping, invalid
     identity/sequence negatives). MUST NOT be derived from D4's incidental
     regression coverage.
  2. Held-execution descriptor topology: production-consistent FD4/FD5
     descriptor-allocation review.
  3. Qualification-coordinator status provenance: what is a private status
     in release steps vs. what a target's namespace says.
  4. Wrong-identity and wrong-sequence status cases on the native side.
  5. Native EOF and malformed-frame negative behavior.
  6. Formal treatment of the three previously-unavailable
     `Wp05ReleasePathIndependentQualificationTests`: artifacts now exist and
     they ran incidentally in the 66/0/0 regressions, but running green inside
     a filter is NOT their formal native qualification; conversion requires
     explicit D5 evidence rows.
  7. The CR-1 termination-classification standard work and, if D5/D6 require
     strengthened oracle claims, the CR-1/CR-7/CR-8/CR-9 hardening bundle.
  8. If D5's fixture scope includes CE coverage, CR-3 and CR-3b are the
     two named missing cases.
- D6: exact C1–C4 decisions (C1/R09 ambiguous ReleaseIntent and stop
  mechanics; C2/R08 stop-first capability and multi-instance window;
  C3/R27 persisted stop-deadline consumption; C4/R02–R06 concurrent
  duplicate and idempotency semantics, including CE-01/CE-04 diagnostics);
  separately authorized production remediation; combined independent
  evidence; final bounded WP05 release disposition against all open gate
  requirements.
- Deferred and out of scope: WP08 production owner orchestration, WP10
  actual target execution, production post-release FD5 status, full G3
  acceptance, cross-platform/portability qualification.

## G. Acceptance exclusions (unchanged by this preservation)

- Not a full WP05 release qualification; not a G3 gate acceptance; not a
  release-protocol acceptance instrument over continued OpenCode review.
- No real target was executed (the D4 fixture does not fork a target).
- No production remediation, no production source change, no frozen-harness
  change by the D4 slice or by this preservation.
- No D5 native-status acceptance.
- No upgrade of the no-resend evidence to a universal exactly-once
  guarantee; post-write acceptance remains unknown in the affected cases.
- No C-item is reopened or closed by reproduced probes; C1–C4 remain
  unresolved exactly as recorded.

## H. Preservation verification (facts recorded by this document)

- Git ancestry: D4 chain linear from `7dd5ffa...` (verified by
  `merge-base --is-ancestor` for each edge); Q1–Q5 chain linear with Q1's
  parent the exact D4 final HEAD (verified per-edge); preservation commit's
  parent = exact D4 final HEAD; review reference points at exact Q5
  `310484a5e82d4a81b0909e07281511cb7bc4c315` before and after preservation.
- Root repository connectivity: `git fsck --no-progress` ran clean; recorded
  pre-existing unrelated dangling objects (not touched, not deleted): dangling
  commit `a2d0e647ad9d0a80daf77037fb1a9b27751aaac3`; dangling blobs
  `ee0c580b97a576fcce653128f08b7ebd7ae51ae4`, `375cc62d5ed6dca7ff3a2cba0ec948439fcae98e`,
  `e72aa4396de52771fbdc8a35c71667a595294b59`, `d2b34a88de388f94ec292f40aefc0e5052b61233`.
- Evidence integrity: the four load-bearing D4 test/fixture source blobs match
  the producer's `source-head.txt` (verified via `final-binding.txt`); the
  producer evidence manifest re-verifies 133/133; the report self-hash holds
  against committed bytes.
- Production diff vs R12 is empty for `src native artifacts` (verified for
  both `8254cf89...` and `310484a5...`); the frozen harness is unchanged
  relative to D4's base (`7dd5ffa...`: empty `eng` diff). The R12→D4 span
  contains `eng/linux-m1-independent-readiness.sh`, introduced pre-D1/D3 in
  the independent-readiness chain at
  `f0d8cf81efa08a281635d7ca97e2c10ec26de29d` — pre-existing infrastructure,
  not a D4 change.
- Review additions are confined to documentation: the Q1–Q5 span adds only
  the three review documents and the reviewer artifact files plus the handoff/
  work-queue review entries (no production, test, or evidence-file rewrite).
- Remote publication status (recorded at commit time): the three refs
  (`implementation/wp05-d4-release-owner-witness`,
  `review/wp05-d4-crash-witness-qualification`,
  `preservation/wp05-d4-independent-crash-witness-review`) are scheduled for
  publication to the authorized remote
  `https://github.com/jenolaszlo-sketch/gagamba.git`, followed by an
  independent fetch/verification pass (commits, ancestry, connectivity,
  selected document retrieval); the actual push and remote-verification
  outcomes are recorded in the preservation engineer's delivery report, which
  is the truth record for this item — this checkpoint is not rewritten to
  match it.
