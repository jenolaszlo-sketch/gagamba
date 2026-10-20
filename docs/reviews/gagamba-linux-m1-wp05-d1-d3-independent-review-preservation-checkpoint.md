# WP05 D1-D3 Independent Diagnostic Review - Preservation Checkpoint - 2026-10-20

Role: independent review preservation engineer (GLM, OpenCode). This checkpoint
preserves, verbatim in substance, the independently reviewed and reproduced D1-D3
diagnostic state. It changes no production or test code, does not alter any
D1-D3 or Q1-Q3 commit, does not implement D4-D6, and does not accept any WP05
release gate.

Disposition: `WP05 D1-D3 DIAGNOSTIC FIXTURES REVIEWED AND INDEPENDENTLY
REPRODUCED - C1-C4 UNRESOLVED; C7/C9 PARTIAL; RELEASE NOT QUALIFIED`.

## A. Git binding

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`; remote
  `https://github.com/jenolaszlo-sketch/gagamba.git` (origin, public).
- Frozen production: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040` (unchanged throughout).
- Planning A3 `2c66eac1da24d0bb9ce706b618abfffdc2d5a437`; plan review V3
  `e4b0a798e944ddfb144901c63b0c444e7980533b` (blobs `f1a062e1e12895426dcfb8b7b0b13a2e6d933918`,
  `73eb09af27c5dbc896739ebf48489862ec13e0a9`, `0bbafa6b9725cce8a20858e1d64a8deec6134f15`);
  plan-review preservation `24e7498f1940c8e985d229a48b208fe4a0065b26`.
  Frozen harness Q3 `36a124d7dff98ecfaf956340767fe1768ad5b17f` /
  preservation `86eee0d9362fc2785c4d7997fec377b7a33c7767` are unaffected.
- Implementation branch `implementation/wp05-release-qualification-fixtures`,
  tip D3 `338a98af269e6b5839f3ccc2e0881f016153b01e` (tree
  `f05c48472cdf08b9c51acb2dc97cfed5cf7050f2`):
  - D1 `dbfecada5d1f6e6c7b852dda5f48b6971e4c650c` - fresh-process journal
    lease fixture.
  - D2 `dc4c372b180abef1b27038d94f49af90226fa548` - 13 contract diagnostic
    probes.
  - D3 `338a98af269e6b5839f3ccc2e0881f016153b01e` - evidence package, C8 plan
    note, handoff/work-queue updates.
  Ancestry `24e7498 -> D1 -> D2 -> D3` verified with `merge-base --is-ancestor`
  at preservation time; diffs `24e7498..D3` and `D2..D3` touch no `src/`,
  `native/`, `eng/` content; `D2..D3` also touches no `tests/` content, so the
  D2-executable test set is identical under D3.
- Review branch `review/wp05-d1-d3-fixture-evidence-review`, tip Q3
  `d0f2220d5e10abb97343d069a5ff43c22c72bfd7`:
  - Q1 `3e6bb51c86146f0dd88d0058f587140a9dd2164f` - fixture and source-oracle
    verification, test-source SHA binding, evidence-manifest audit, and an
    independent Linux rerun with committed artifacts
    (`docs/reviews/gagamba-linux-m1-wp05-d1d3-review-artifacts/`, manifest
    `SHA256SUMS`, script `reviewer-rerun.sh`).
  - Q2 `6baa78d16e7e75626ec9d94af79a3b0780391e35` - counterexample
    reconciliation.
  - Q3 `d0f2220d5e10abb97343d069a5ff43c22c72bfd7` - required D4-D6 work and
    the final review disposition.
  Ancestry `D3 -> Q1 -> Q2 -> Q3` verified; Q1 is based on exact D3;
  `338a98af..d0f2220d` adds only `docs/reviews/` documentation and evidence
  (13 files, 694 insertions) and touches no production, native or test code.
  Review document blobs at Q3:
  `3a8ef8ad9ce04efffc4337fbda3158dc96e7ddc8`
  (`gagamba-linux-m1-wp05-d1-d3-diagnostic-fixture-implementation-review.md`),
  `817d96e0f4656bf9d9106b7b4583704de5e0f9f7`
  (`gagamba-linux-m1-wp05-d1-d3-counterexample-evidence-reconciliation.md`),
  `1768ec2a98e69349ca46137f4194fb0d49233772`
  (`gagamba-linux-m1-wp05-d4-d6-required-qualification-work.md`).
- Preservation branch `preservation/wp05-d1-d3-independent-fixture-review`
  created on exact D3 `338a98af269e6b5839f3ccc2e0881f016153b01e` (not on Q3);
  worktree `C:\Users\Laszlos\source\repos\gagamba-preserve-d1d3`; single
  documentation-only preservation commit whose parent is D3.

## B. Independent reproduction

Producer vs reviewer (both on the supported Linux profile; producer evidence
`docs/evidence/linux-m1/wp05-release-fixtures-counterexample-probes-2026-10-20.md`
+ artifacts; reviewer artifacts in the Q1 directory above):

| Group | Producer | Independent rerun |
|---|---|---|
| Contract probes | 13 passed / 0 failed | 13 / 0 (104 ms) |
| C7 lease/process | 1 / 0 (7 s) | 1 / 0 (4 s) |
| Native release protocol | 4 / 0 | 4 / 0 (438 ms) |
| Focused existing classes | 49 passed / 0 failed / 3 skipped | 49 / 0 / 3 (same three skipped) |
| Fixture build | 0 errors / 10 warnings | 0 errors / 10 warnings |

Windows (producer, verification-only): contract probes 13/13; the C7 lease
probe is `Skipped: linux only`, never counted as passing.

Exact execution revision: the independent rerun ran on the Linux checkout
`/home/gaga/qualification/gagamba-r12` at exactly D2 `dc4c372b180abef1b27038d94f49af90226fa548`
(clean before and after; `reviewer-run-header.txt` / `reviewer-run-footer.txt`
record `clean=0`). D3 changed no executable test or production content (its
diff is evidence/documentation only), so the D2 executable test set equals the
D3 test set at the executable level.

Reviewer environment (from committed `reviewer-environment.txt`): Ubuntu WSL2,
kernel `6.6.114.1-microsoft-standard-WSL2`, boot
`69d053fb-0fd9-4209-aa5b-be128424ed34`, uid/gid 1000 (`gaga`),
`CapInh`/`CapPrm`/`CapEff`/`CapAmb` all zero, cgroup `0::/gaga-delegated/host`,
.NET `10.0.112`, GCC `15.2.0`, bubblewrap `0.11.1`, frozen seccomp filter
SHA-256 `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`.
Commands are exactly those in committed `reviewer-rerun.sh`: fixture build plus
four `dotnet test --filter` groups (contract probes, lease process probes,
`ReleaseProtocolTests`, five focused classes). All commands exited 0; the
script runs under `set -Eeuo pipefail` and its footer line proves each step
completed (per-command exit codes are not separately echoed). No temp-dir
residue remained; no target process was started; the checkout was clean at
both ends. This environment binds to this host; no broader host/kernel
portability claim is made.

Evidence integrity at preservation time (recomputed by this engineer):

- Producer artifact manifest: 23/23 entries verified byte-exact.
- Reviewer Q1 artifact manifest: 9/9 entries verified byte-exact.
- The five added test-source SHA-256 values in `source-head.txt` all match the
  committed files (`cd5cd00c...`, `ac6e3462...`, `15029e4d...`,
  `70155896...`, `a27e4c23...`).
- Producer and reviewer manifests remain distinct; no hash was rewritten; no
  line-ending normalization was applied.

## C. Findings (preserved state)

C1-C4 are production-symbol decisions and remain unresolved. The binding
C-ID -> R-ID mapping from `73eb09af27c5dbc896739ebf48489862ec13e0a9` is:
C1 = R09 ambiguous-intent contract + wedge stop mechanics (binds Q06; blocks
G3-A11/G3-B08); C2 = R08 stop-first capability invalidation + the CE-03
multi-instance window (binds Q05; blocks G3-A02/G3-A07); C3 = R27 persisted
stop-deadline consumption (binds Q04; blocks G3-A10); C4 = R02/R06 duplicate
semantics and the two idempotency contracts, covering the CE-01 retention
asymmetry and the CE-04 deterministic concurrent test (binds Q07; blocks
G3-A05). Diagnostic behavior observed by Q1/Q2 is reproduced evidence, not
approved remediation, and cannot close any of these.

Per-counterexample diagnostic status (Q2 reconciliation; all bounded):

- CE-01 (retention asymmetry: outside-gate refusals are not retained; retry
  semantics change with the failure point): observed at the concurrent loser's
  un-retained `Refused(NotCommitted)`; the sequential aspect remains anchored
  by the pre-existing arbiter tests. Reproduced, unresolved.
- CE-02 (ambiguous post-content seal failure -> false definite
  `NotCommitted`, un-persistable stop, no stop request): reproduced end-to-end
  with journal error shape, `NotCommitted`, zero transport attempts, no
  durable stop, and conservative
  recovery `Intent -> MayHaveExecuted`. Fidelity limit: same-process fake
  storage injection plus same-process recovery after fence release; no
  full fresh-process release crash/no-resend sequence is qualified.
- CE-03 (runtime-accepted stop with failed/absent durable evidence does not
  bind a second arbiter instance): reproduced with two arbiter instances in
  one process - durable stop blocks; failed durable-stop write does not, and
  the second instance releases exactly once. Fidelity limit: synthetic
  in-process composition; fresh-process variant unverified.
- CE-04 (concurrent duplicate: loser `Refused`, winner `Released`, later
  caller `Duplicate`, one physical write): reproduced deterministically via
  the `after-intent` barrier. Observation level: one write through the
  instrumented `IReleaseWriteChannel` boundary; this is not an independently
  established physical FD4 acceptance count.

CE-04 terminology reconciliation (recorded clarification; no historical
document amended):

- V3 register: CE-01 = retention asymmetry (sequential retry semantics);
  CE-04 = the concurrent duplicate three-caller shape. The register's CE-04
  hook line reads `C3/C4 (Q07 ...)`; the required-changes document - the
  canonical conversion - assigns Q07, CE-01's documentation and CE-04's test
  to C4 alone (C3 = R27/Q04). The `C3/` token on CE-04's hook is therefore an
  original text wobble, clarified by the canonical mapping above.
- D3's coverage map and Q1's verdict table label the one probe as covering
  `C4, CE-01/CE-04`. Substantively correct: the interleaving is CE-04's; the
  concurrent loser's non-retention instantiates CE-01's outside-gate
  asymmetry.
- Q2 section 2's heading names the concurrent scenario "CE-01"; the register
  reserves the concurrent interleaving for CE-04 while CE-01 names the
  asymmetry property that the loser instantiates. The Q2 findings and the Q3
  preservation summary therefore use "CE-04" (concurrent, parked-intent probe)
  for the same scenario Q2's section heading labels "CE-01". This is a
  labeling imprecision, not a change of findings; classification: minor
  documentation discrepancy, non-material to C4/Q07/G3-A05 traceability, not
  a qualification blocker. Authoritative mapping stands as written in this
  section A (CE-01 asymmetry, CE-04 concurrent) plus the C4 binding.

C5-C9 independently verified state:

- C5: implemented and independently passed (clock-unavailable stop, deadline
  overflow; runtime acceptance, no durable stop, release refused).
- C6: implemented and independently passed (ambiguous post-content seal
  injection distinct from definite write failure; self-validating injection).
- C7: lease/process separation reproduced (real `OwnershipJournal` fence,
  fresh owner SIGKILLed, replacement `TryAcquireWriter` afresh, `/proc` fd
  checks, PIDs differ, `sequence=` baselines). NOT complete: full
  release-owner SIGKILL-at-release-barrier supervisor, transport witness
  ledger, coordinator acceptance ledger, no-resend qualification.
- C8: review-dependency linearization in the execution plan; docs-only;
  `plan-c8.diff` matches the committed edit exactly.
- C9: buffer durable-commit gate, failed-flush non-persistence and synthetic
  reader negatives (wrong identity/sequence, EOF, malformed kind)
  independently passed; the existing native qualification-coordinator valid
  path passed 4/4. NOT complete: real FD5 negative frames and the
  held-execution descriptor topology; the production coordinator emits no
  post-release status.

Observation limitations preserved: no real target was executed anywhere in
this slice; no production post-release status path exists at R12; the three
`Unavailable` facts in `Wp05ReleasePathIndependentQualificationTests` were
never executed successfully (no generated native fixture / WP04 coordinator
artifacts in the checkout) and are not counted as passing by anyone.

## D. Review disposition

`WP05 D1-D3 DIAGNOSTIC FIXTURES REVIEWED AND INDEPENDENTLY REPRODUCED - C1-C4
UNRESOLVED; C7/C9 PARTIAL; RELEASE NOT QUALIFIED`

## E. Missing qualification

- Fresh-process release owner (SIGKILL at release barriers) - D4.
- Real FD4 transport/no-resend acceptance witness - D4.
- Fresh-process variants for CE-01/CE-03/CE-04 - D4.
- Native FD5 negative frames and held-execution descriptor topology - D5.
- Generation/staging required to run the three `Unavailable`
  `Wp05ReleasePathIndependentQualificationTests` - D5.
- C1-C4 contract decisions by their holders (no diagnostic closes them) - D6.
- Combined bounded evidence and final G3 gates - D6.
- Any production correction, if a C-decision requires it: separately
  authorized and evidenced slice.

## F. Next required work

`d0f2220d5e10abb97343d069a5ff43c22c72bfd7` -

`docs/reviews/gagamba-linux-m1-wp05-d4-d6-required-qualification-work.md`
(blob `1768ec2a98e69349ca46137f4194fb0d49233772`) is the smallest remaining
qualification scope: D4 fresh-process release-owner supervisor (real lease,
deterministic crash barriers, no inherited lease/writer, real FD4 witness,
coordinator acceptance observation), D5 native trusted-status negatives
(real coordinator frames, FD4/FD5 ownership and EOF behavior, descriptor
topology, generated native/calibration artifacts, then the three currently
`Unavailable` tests), D6 combined bounded evidence with explicit C1-C4
decisions. No G3 release gate is accepted.

## G. Preservation verification

- Branch/ref identities verified before commit:
  `implementation/wp05-release-qualification-fixtures` =
  `338a98af269e6b5839f3ccc2e0881f016153b01e`;
  `review/wp05-d1-d3-fixture-evidence-review` =
  `d0f2220d5e10abb97343d069a5ff43c22c72bfd7`;
  preservation branch parent = exact D3.
- `git fsck --connectivity-only` sound; dangling objects recorded without
  deletion: blobs `e72aa4396de52771fbdc8a35c71667a595294b59`,
  `ee0c580b97a576fcce653128f08b7ebd7ae51ae4`,
  `d2b34a88de388f94ec292f40aefc0e5052b61233`,
  `375cc62d5ed6dca7ff3a2cba0ec948439fcae98e`; commit
  `a2d0e647ad9d0a80daf77037fb1a9b27751aaac3` (pre-existing, unrelated).
- Production and native source diffs remain empty across
  `24e7498..D3` and `338a98af..Q3`.
- All producer and reviewer evidence documented above remains reachable by
  exact path from this checkpoint's commits.
- No G3 acceptance claim is introduced anywhere in this commit; C1-C4 remain
  unresolved; WP05 release remains NOT ACCEPTED.
- Remote publication: this task publishes exactly three refs afterwards -
  `implementation/wp05-release-qualification-fixtures`,
  `review/wp05-d1-d3-fixture-evidence-review` and
  `preservation/wp05-d1-d3-independent-fixture-review` - with exact-SHA
  verification against `https://github.com/jenolaszlo-sketch/gagamba.git`
  recorded in the preservation report; remote heads must match the SHAs above.
