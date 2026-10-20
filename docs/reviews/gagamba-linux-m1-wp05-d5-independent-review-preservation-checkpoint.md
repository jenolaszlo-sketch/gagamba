# WP05 D5 Independent Native-Status Review — Canonical Preservation Checkpoint — 2026-10-20

Status: documentation-only preservation of the completed independent GLM review
of the WP05 D5 tranche. This checkpoint preserves the reviewer's Option B
disposition, the independent reproduction evidence, and the bounded limitation
register verbatim in binding form; it implements no correction, runs no test,
changes no production or test source, modifies no D5 or review commit, and
accepts no gate. The authoritative review documents live on the review branch
`review/wp05-d5-native-status-qualification` (the three review commits
untouched) and are referenced, not duplicated, by this checkpoint.

## A. Revision binding

- Product/frozen: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`. Independent harness review
  `36a124d7dff98ecfaf956340767fe1768ad5b17f`
  (`WP05 HARNESS QUALIFIED (bounded)`) remains frozen and unchanged by D5 or
  this preservation.
- Prior preservation/D5 base:
  `5c58b065c37a71c8f392a617f725822ffb64c171`
  (branch `preservation/wp05-d4-independent-crash-witness-review`).
- D5 implementation chain on
  `implementation/wp05-d5-native-status-qualification`, resolved from actual
  repository ancestry (12 commits, linear, D5.0's parent is the exact D4
  preservation commit):
  - D5.0 plan/correction inventory
    `c3cc27e4ba5c51f3e8debd73588d0c7d79f8fd79`
  - D5.1 fixture corrections + CR-3/CR-3b cross-process probes
    `ed49ed5a0388d7ab3b1a973a3cced71b80bacc87`
  - D5.2 native FD5 probes
    `dbef38a64eed1bbc9f436b49f88bdbae472d3049`
  - D5.3 persistence/cleanup
    `fa028c22612745e2cc4017ff7bb2b9766c896930`
  - D5.1b strengthened two-owner contention
    `30a8eb90491c94a0ed3fa9a87130bdc55be1c187`
  - Evidence 1 `fb10cd53066fe451a12294c64fd61ef371c86604`
  - Manifest verification `bf1644d38c4528a4c6a63585ebaf1da52e6d13c4`
  - Descriptor exclusions `5d5d6b29a8701d7815f621a07396e8d187d2fee8`
  - Report byte binding `8bca3ff7ba9ebc70e255be7dbc0f36606041b7c3`
  - D5.1c cross-owner fence refusal (final test-code state)
    `a990982904d51062ef9449deceb89cd81f434948`
  - Evidence refresh `bd4a4f26c8797cc9052470a5359b6f6702fbfbc`
  - Final binding (exact D5 HEAD)
    `a4fca00f3b2bd3f2b627d7044516d0ef8124b259` (tree
    `e8a3e82a38f5b5dce544a97af912216279b36cbc`).
  - Recorded non-blocking documentation discrepancy (preserved, not silently
    fixed): the D5 execution plan's chain table lists the evidence commits as
    preceding D5.1c; actual ancestry has D5.1c after them. Binding is
    unaffected because all runs and the final binding were taken from the
    final test-code state; the reviewer's D6 handoff records this as a
    documentation-only correction item.
  - Source identity: runs were bound at run-source HEAD
    `a990982904d51062ef9449deceb89cd81f434948` (tree
    `6b335cff794749566898e0a3a81abf0d47218fbc`); `git diff a990982..a4fca00 --
    tests src native artifacts eng` is empty.
- Independent review chain on
  `review/wp05-d5-native-status-qualification` (three commits, linear;
  Q1's parent is the exact D5 final HEAD; Q1–Q3 preserved unchanged, no
  amend/rebase/cherry-pick):
  - Q1 `0e096199d04ed4f54e1825865519c4a96b93440b` — independent native-status
    review + 23-file reviewer artifact set
  - Q2 `7fea172e278d69a92f04152884df93916471ef89` — evidence matrix
  - Q3 `87cced6143fb3337829fab48a3c012b06c06508c` — corrections closure and
    D6 handoff (review tip).
  - Review-document blob identities at Q3 (for exact cross-reference):
    - Main review
      `docs/reviews/gagamba-linux-m1-wp05-d5-independent-native-status-review.md`
      blob `505a0bd3aa02aa1b3b9ddc7e6d70ed70bd0a45e7`
    - Evidence matrix
      `docs/reviews/gagamba-linux-m1-wp05-d5-native-status-evidence-matrix.md`
      blob `d96e6582e900399d205d8ae83b8f837cd6ba00e7`
    - Corrections closure + D6 handoff
      `docs/reviews/gagamba-linux-m1-wp05-d5-required-corrections-and-d6-handoff.md`
      blob `8886bd65fe7bafdf40bbf43b3189491122519603`
    - Reviewer artifacts: `docs/reviews/gagamba-linux-m1-wp05-d5-review-artifacts/`
      (23 committed files: rerun logs for the four suites plus the three
      formal rows, focused-regression log, environment snapshot, independent
      256-entry manifest verification, FD5 topology samples, CR-3/CR-3b
      fresh-run case artifacts, reviewer SHA-256 list).
- This preservation branch:
  `preservation/wp05-d5-independent-native-status-review`, commit parent =
  exact D5 final `a4fca00f3b2bd3f2b627d7044516d0ef8124b259` (not based on
  Q3; reviewer commits are not cherry-picked).

## B. Independent result

Disposition preserved verbatim (Option B — evidence supported with
qualifications; released-path gate NOT QUALIFIED remains the standing record):

`WP05 D5 INDEPENDENT REVIEW COMPLETE - NATIVE STATUS EVIDENCE SUPPORTED WITH QUALIFICATIONS; REQUIRED WORK REMAINS; RELEASE NOT QUALIFIED`

## C. Independent reproduction (verified against committed logs and reruns)

- Producer (D5 evidence, exact `a990982...` run source): D4+CR supervisor
  **17/17 twice**, native FD5 **32/32 twice**, focused regressions **66/0/0**,
  three formerly-unavailable release-path methods **1/1 each** as formal
  rows; qualification artifact manifest **256/256**; D5 coordinator binary
  identity `16d30538df254c04082fc56bdbfe9b2c992eae5861dfc278682e99d957aa17e3`
  (source `tests/Fixtures/wp05-qualification-coordinator.c`
  `16da6f3fd672a793549aea802bad70afed092247351fa3179d9fa39ff0b5fea3`,
  build script
  `eng/linux-m1/build-d5-status.sh`
  `cd3a01a8a9e5f081b87737d643e7113db843ceaa60dcfa730e41c29c47ae1bee`); report
  self-hash `733b772e2093c069a8e3fd7649d3b05795d31b716bd08e5f0550b767ec9382b1`
  bound to committed bytes. Frozen native identities verified unchanged:
  fixture `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305`,
  WP04 coordinator
  `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b`,
  seccomp filter
  `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`; the
  historical `e20903cce6061ecc2b152f9e7b4ccb389edeee54047d96f1f3b66dd5195be0b9`
  qualification coordinator was not relabeled.
- Reviewer (independent reruns on the established WSL2 delegation topology,
  frozen launcher `/root/wp05/run-as-gaga-in-host.sh`, runtime-only, no new
  probes, checkout hash-verified via bundle at exact `a4fca00...`): supervisor
  **17/17 twice** (`drr-run1-cg.log`, `drr-run2-cg.log`), native **32/32
  twice** (`nat-run1-cg.log`, `nat-run2-cg.log`), focused regressions
  **66/66** (`regressions-cg.log`), RQ-1/RQ-2/RQ-3 **1/1 each**
  (`rp1/rp2/rp3-review.log`); independent artifact-manifest verification
  **256/256** via `git archive` + `sha256sum -c` (`manifest-verify.txt`);
  report hash re-validated byte-exact on a Linux checkout (a Windows-side
  CRLF checkout hashes differently — line-ending noise, recorded as such in
  the review). FD5 descriptor topology reproduced twice by the reviewer
  (same-inode relation; `pipe:[238552]` and `pipe:[255061]` vs the
  producer's `pipe:[209719]`); the 96-byte exec frame is byte-identical
  between producer and reviewer records; 48/50 run-1 frame files match, the
  remaining two are per-run pid/fd variables as documented.
- Execution environment (same boot epoch for producer and reviewer runs, as
  recorded at run/review time): Linux/WSL2, kernel
  `6.6.114.1-microsoft-standard-WSL2`, boot
  `69d053fb-0fd9-4209-aa5b-be128424ed34`, non-root `gaga` (uid 1000), all
  capability sets zero, cgroup `0::/gaga-delegated/host`, .NET 10.0.112,
  gcc 15.2.0, Bubblewrap 0.11.1. No cross-host, cross-kernel, or
  cross-distribution claims are made.
- Cleanup: reviewer post-suite checks show no coordinator processes, no
  test-owner residue, no temp residue; delegated cgroup procs contained only
  the invocation shell chain. No target was ever executed.

## D. Bounded findings preserved (verbatim scope from the committed review)

- **Native FD5 (CE-04+RQ-6 supplement): PASS at component level.** All 32
  probes (15 direct rows + one 17-row theory) reproduce the producer's
  counts; exit families 20 (in-stream EOF after 64-byte header), 21 (RELEASE
  bytes plus at least one extra byte), 22 (invalid frame) are asserted
  against the native RELEASE paths with fixture emitters at source lines
  150/160/167 of the D5 fixture; the RELEASE branch semantics are unchanged
  from base — D5 gained coverage only.
- **CR-3 (fresh-process release coercion): PASS at the achievable boundary.**
  Retried fresh-lease release, exactly-one-FD4-write witness, and no
  additional rescued writer outside journal truth are exercised; the
  "simultaneous two-acquired-writer race" case remains unrepresentable in
  R12 (single-writer fencing by contract) and is preserved as a documented
  contract boundary, not an unresolved requirement by this slice.
- **CR-3b (release after durable stop): PASS as a standalone fresh-lease
  boundary case.** Durable stop committed by a dead owner; release refused
  `NotAttempted/0 transports`; the replacement owner rebuilds `stop=true`
  with the exact durable deadline echo from journal truth; measured stop
  deadline arithmetic is exactly +5_000_000_000 ns. The "acquired=false"
  acquisition variant remains out of R12's contract scope (documented
  limitation, preserved as written).
- **D5-O05: disposition C — deferred integration boundary.** The buffer-level
  encoding safety of `ReleaseObservationBuffer` (mark-gated flush, refusal of
  a differing second offer, all-or-none flush) is qualified at component
  level; the production release-observer wiring is NOT exercised — the buffer
  has zero production callers at the D5 HEAD — and the end-to-end claim is
  out of any test-only package's scope. The D5-O05 HOLD is preserved; product
  consumers keep failing closed; no evidence exists that activates them.
- **Exit-20/21/22 formal row conversion and RQ-1/2/3:** the three formerly
  unavailable release-path methods run as formal rows under the D5 chain
  (1/1 each, classic first-party paths, no target execution).
- **Descriptor-exclusion honesty:** the committed `exclusions.txt` records
  unclaimed behaviors (destructive descriptor cases: inherited extra writer,
  wrong-FD deselector, incorrect inherited helper writer; EINTR fault
  injection not claimed). They are preserved exactly as unclaimed.
- **CR-1:** resolved at the bounded control-based termination standard
  (`termination-oracle.txt`); the optional wait4/pidfd late-stage witness was
  not strengthened and remains a future-test-surface item only.

## E. Remaining work register (preserved from the committed D6 handoff; none authorized by this preservation)

- H1 — D5-O05 integration seam: production owner/status wiring needed before
  the end-to-end durable-observation claim; component evidence retained;
  consumers remain fail-closed.
- H2 — descriptor destructive-exclusion cases: unclaimed; smallest safe
  future experiment is the test-only second-writer case (simultaneous
  same-pipe writers while the production reader validates the first frame),
  requiring an authorized fixture-scope package with a documented EOF
  decision; production untouched.
- H3 — CR-1 optional wait4/pidfd raw-status witness: unclaimed enhancement;
  new test-only surface only; no frozen-harness reopening.
- H4 — C1–C4: unchanged, unresolved by design; outside WP05 scope.
- H5 — documentation pass: annotate the D5 plan's chain-ordering discrepancy
  (A above) whenever those docs are next touched; no urgency, no re-bind
  required.
- D6: exact C1–C4 decisions; separately authorized production remediation;
  combined independent evidence; final bounded WP05 release disposition
  against all open gate requirements. Then WP08/WP10 remain deferred.
- **Release status: NOT QUALIFIED. No gate is accepted by D5, the review, or
  this preservation.**

## F. Acceptance exclusions (unchanged by this preservation)

- Not a full WP05 release qualification; not a G3 gate acceptance.
- No real target was executed by D5, the review, or this preservation.
- No production remediation, no production source change, no frozen-harness
  change, no D5 fixture modification by this preservation. Production diffs
  against the D4-preservation base are empty for `src native artifacts`
  (re-verified for both `a4fca00...` and the review tip).
- No D5-O05 end-to-end claim, no descriptor destructive-negative claim, no
  release execution claim is accepted or created.
- No C-item is reopened or closed; C1–C4 remain unresolved exactly as
  recorded.

## G. Preservation verification (facts recorded by this document)

- Git ancestry: D5 chain linear from
  `5c58b065c37a71c8f392a617f725822ffb64c171` (parent-of-D5.0 exact); review
  chain linear with Q1's parent exactly `a4fca00f3b2bd3f2b627d7044516d0ef8124b259`;
  preservation commit's parent = exact D5 final HEAD; the three review
  commits are referenced (not cherry-picked) and remain unchanged on their
  own branch (`review/wp05-d5-native-status-qualification`).
- Root repository connectivity: `git fsck --connectivity-only` shows only
  pre-existing unrelated dangling objects (not touched, not deleted): dangling
  commit `a2d0e647ad9d0a80daf77037fb1a9b27751aaac3`; dangling blobs
  `ee0c580b97a576fcce653128f08b7ebd7ae51ae4`,
  `375cc62d5ed6dca7ff3a2cba0ec948439fcae98e`,
  `e72aa4396de52771fbdc8a35c71667a595294b59`,
  `d2b34a88de388f94ec292f40aefc0e5052b61233`.
- Evidence integrity: test-source SHA-256 block from the producer's
  `source-head.txt` re-verified byte-identically by the reviewer (six files:
  `Wp05D4OwnerHost.cs` `4a5b96baf8c8d99e43231d4ef8c9a1226d2e2c07452a562a8d0e6d2a1db7a031`,
  `Wp05D4SupervisorTests.cs` `279e3fc48de1955818c45ef584b0eb04b47fd1d132f39ffddc151aae1e7545eb`,
  `Wp05D5NativeStatusTests.cs` `0b68a8ab8560bfa25f594bd68bc94f8c65db34e6c6b4c38c39742396e82119b7`,
  `wp05-qualification-coordinator.c` `16da6f3fd672a793549aea802bad70afed092247351fa3179d9fa39ff0b5fea3`,
  `build-d5-status.sh` `cd3a01a8a9e5f081b87737d643e7113db843ceaa60dcfa730e41c29c47ae1bee`,
  plan `d581d4d7ec5d0931a7bd5ba2a6bccf50256b2eee606697f0ff92db1799767684`);
  256-entry artifact manifest re-verified by the reviewer and again at
  preservation time; D5 binary identity re-verified by the reviewer's
  independent rebuild.
- Production diff vs the D4-preservation base is empty for
  `src native artifacts` (verified at `a4fca00...`); `eng/` gained only
  `eng/linux-m1/build-d5-status.sh`; the frozen harness is unchanged.
- D5 additions are test-only plus evidence/docs (`tests/Fixtures/wp05-qualification-coordinator.c`
  +136; `Wp05D4OwnerHost.cs` +99; `Wp05D4SupervisorTests.cs` +82; new
  `Wp05D5NativeStatusTests.cs` +562; plan and evidence/docs commits; one
  work-queue accounting update inside the D5 chain).
- Review additions are confined to documentation: the Q1–Q3 span adds only
  the three review documents, the 23-file reviewer artifact set, and the
  reviewer artifact index (no production, test, or evidence-file rewrite).
- Remote publication status (recorded at commit time): the three refs
  (`implementation/wp05-d5-native-status-qualification`,
  `review/wp05-d5-native-status-qualification`,
  `preservation/wp05-d5-independent-native-status-review`) are scheduled for
  publication to the authorized remote
  `https://github.com/jenolaszlo-sketch/gagamba.git`, followed by an
  independent fetch/verification pass (commits, ancestry, connectivity,
  selected document retrieval); the actual push and remote-verification
  outcomes are recorded in the preservation engineer's delivery report,
  which is the truth record for this item — this checkpoint is not rewritten
  to match it.
