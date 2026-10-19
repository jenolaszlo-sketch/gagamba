# Gagamba Linux M1 â€” WP05 Harness Qualification Preservation Checkpoint

Date: 2026-10-19
Status: canonical preservation record; not a replacement for the independent review

## A. Implementation binding

- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- R11 code/test: `c48e358451c8b0dbe7aace281f64a2eced399ed7`
- R12 implementation/evidence: `38f6981b2a60daab2df37c92913a6760c1bef131`
- R12 tree: `6854675d6b37a49d5302d6104103bdbfbc268040`
- Production surface diff baseline -> R12 and R12 -> Q3: empty.

## B. Independent review

Canonical review reference: `review/wp05-opencode-harness-final-qualification`

| Checkpoint | Full SHA | Parent |
| --- | --- | --- |
| Q0 | `1e64875ce30f10c5c834188c330b6aae1fbac0ae` | R12 |
| Q1 | `bf453ea6efd827b7ce965f3f5bfddb4db11af0d4` | Q0 |
| Q2 | `21e53ad787c6c9b891aaab9c1996f4627659dcdf` | Q1 |
| Q3 | `36a124d7dff98ecfaf956340767fe1768ad5b17f` | Q2 |

The canonical reference resolves to Q3. Each checkpoint parent is the preceding
checkpoint; R12 and the production baseline are ancestors of Q3; no intervening
implementation commit or rewritten ancestry exists. `git fsck --no-dangling`
completed with no errors. The reviewer branch remains unmerged; its test file
remains reviewer-only.

Final disposition preserved without expansion:

**`WP05 HARNESS QUALIFIED (bounded)`**

## C. Evidence verification

Independent canonical audit of committed Git blobs:

- Historical R9 manifest: 20 matching / 3 mismatching entries; the exact causes
  reproduce as one CR before the final LF in `observer-observation.txt` and
  `source-head.txt`, and all-lines CRLF in `sigpipe-observation.txt`.
- R11 remediation manifest: 28/28.
- Qualification-harness remediation manifest: 18/18.
- Q3 review manifest: 17/17; it covers every review artifact except itself.
- Q1 probe source SHA-256: `df21e395...`, matching the reviewer identity record.
- Q2 run 1 and run 2 are distinct logs, each with eight phases, zero failures,
  537 passed / 35 skipped (489 phase-1 passed/0 skipped; phase-2 48 passed/35
  skipped). Skip classes: IntegratedBarrier 14, DomainAuthority 19,
  RecoveryHarness 2, supported by the first-hand root-only EPERM probe.
- Q3 release-path focused: 3/3, Test Run Successful.
- Q0 readiness: 57/0, READINESS=PASS.
- Frozen identities: seccomp 672 B / `2aa87901...`; Bubblewrap `523da3e7...`;
  G2 manifest 873 B / `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`; RELEASE 128 B / `90A07CDEF85B18B5B2A0360D7D01705CF7B9E4E9B2D52E7C56DA76A16201E59B`;
  OwnershipSnapshot v3. No real target was executed.

Recorded evidence limitations (not counterexamples):

1. **Q1 repetition logs:** only the fifth focused-run log is committed. The six
   probes also ran in both Q2 full-profile runs (489 includes the six reviewer
   probes), but the other four focused-run logs are not independently auditable.
   The committed probe behavior and the bounded acceptance do not depend on the
   unlogged repetitions.
2. **Q1 residue header:** the Q1 log's host-leaf `cgroup.procs` values are
   consistent with the audit shell PIDs (the Q2 residue artifact explicitly
   labels that context). No probe child, coordinator/bwrap/testhost, zombie or
   child cgroup residue was observed.
3. **P3c:** `0f29bb54921d11306f14fad1288851efd349c26a` exists canonically as the
   post-P3b docs-only attestation, but was absent from the reviewer bundle/worktree
   (bundle tip P3b) and was used by no qualification fact. The reviewer's Q0
   record is correct for its accessible sources. This is an infrastructure
   provenance limitation outside the load-bearing review and is not a harness
   counterexample.

The exact CRLF explanation is confirmed independently; the Q1 probe source,
Q2 accounting, Q3 manifest and production-surface assertions are supported by
committed evidence. `WP05 INDEPENDENT HARNESS EVIDENCE VERIFIED AND PRESERVED`
is recorded with the Q1 log-coverage limitation above.

## D. Accepted boundary

Qualified: the WP05 qualification harness and lane infrastructure, including the
test oracle, expected wrapper identity provenance, native pidfd observations,
exit-code versus signal distinction, `WNOWAIT` non-consumption, fail-closed
observer behavior, forged-release negative controls, process-global test
isolation, WP04 supporting regressions, SIGPIPE qualification-host behavior, and
supported-profile accounting, on the recorded non-root delegated Linux profile.

Explicitly not qualified:

- Full WP05 release implementation acceptance.
- Release/stop race qualification.
- Durable release intent and no-resend crash recovery.
- Fresh-process crash checkpoint matrix.
- Trusted native coordinator status qualification.
- Actual WP10 target workload execution.
- The remaining 35 root-only auxiliary rows.
- Cross-host, cross-kernel or multi-distribution portability.
- The Windows lane (not rerun; R11 Windows results remain historical producer
  evidence).
- Any meaning of P3c beyond the infrastructure-provenance limitation above.

**WP05 release remains NOT ACCEPTED.**

## E. Repository preservation

- Review reference imported into canonical Gagamba at exact Q3; no force-update,
  merge, rebase or cherry-pick.
- R12 implementation and all prior review references unchanged.
- Canonical work-queue and handoff pending edits preserved and committed in this
  preservation branch; the review evidence is referenced through the review
  branch rather than a broken relative link.
- Preservation artifacts and this checkpoint are committed together. The
  original reviewer evidence and manifests were not modified.
- Unrelated untracked experiments and historical test-results directories remain
  untouched. No production source or reviewer probe was changed.

## F. Environment lifecycle

- Reviewer evidence reports boot `69d053fb-0fd9-4209-aa5b-be128424ed34`, gaga
  uid 1000, CapEff 0, `/sys/fs/cgroup/gaga-delegated/host` placement.
- Current session remains available: session anchor PID 576 (`sleep infinity`,
  `/init.scope`) is alive; host cgroup is empty; parent/host are `gaga:gaga 755`;
  the delegated controllers are `memory pids`.
- No cleanup was executed. Operator authorization for termination/removal was
  not established, and the environment may be needed for the next WP05 release
  qualification. Cleanup responsibility remains with the operator; the
  documented cleanup must stop the anchor and remove the empty host/parent only
  after the review session ends. Cleanup completion is independent of the
  preservation validity.

## G. Next milestone

Independent WP05 release/stop arbitration, durable ReleaseIntent/no-resend
recovery, fresh-process crash qualification, and trusted native status-channel
qualification using the already qualified harness. WP08 owner orchestration and
WP10 owner wiring/target execution remain deferred; the 35 root-only auxiliary
rows and cross-host portability remain pending. No future task is marked complete
by this checkpoint.


## Final preservation status

**WP05 INDEPENDENT HARNESS QUALIFICATION PRESERVED - BOUNDED ACCEPTANCE FROZEN**

**WP05 RELEASE NOT ACCEPTED.**
