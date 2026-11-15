# Evidence matrix — WP05 D5 independent verification review

Companion to `gagamba-linux-m1-wp05-d5-independent-native-status-review.md` (same review branch, base = exact D5 HEAD `a4fca00f3b2bd3f2b627d7044516d0ef8124b259`). This matrix maps every WP05-D5 planned task/surface to (a) the producer-committed evidence, (b) this review's independent verification method, and (c) the reviewer's own artifacts committed alongside (`docs/reviews/gagamba-linux-m1-wp05-d5-review-artifacts/`).

Reviewer execution identity: kernel `6.6.114.1-microsoft-standard-WSL2`, boot `69d053fb-0fd9-4209-aa5b-be128424ed34`, uid/gid `1000:1000`, delegated cgroup `0::/gaga-delegated/host`, dotnet `10.0.112`, gcc `15.2.0`, bwrap `0.11.1` (details: `env-preflight.txt`). Checkout under test: clean detach of `a4fca00f3b2bd3f2b627d7044516d0ef8124b259` (fetched via hash-verified bundle).

## A. Qualification sources and binding

| # | Aspect | Producer evidence (committed at D5 chain) | Reviewer independent verification | Result |
|---|---|---|---|---|
| A1 | Run-source identity | `source-head.txt`: branch, `head=a990982904d51062ef9449deceb89cd81f434948`, tree `6b335cff794749566898e0a3a81abf0d47218fbc`, `base_r12`/`tree_r12`, 6-line test-source SHA block, `product_diff_src_native` empty | Recomputed all six SHA-256 lines byte-identically from checkout; verified chain (12 commits) and that `git diff a990982..a4fca00 -- tests src native artifacts eng` is empty | VERIFIED |
| A2 | Final binding point | Commit `a4fca00f3b2bd3f2b627d7044516d0ef8124b259` | `git rev-parse` equals; worktree detached exactly; `git status` clean after build | VERIFIED |
| A3 | Report byte identity | `report-sha256.txt` = `733b772e2093c069a8e3fd7649d3b05795d31b716bd08e5f0550b767ec9382b1` | `sha256sum` of committed report blob on a Linux checkout — byte-exact match | VERIFIED |
| A4 | Artifact-manifest integrity | `SHA256SUMS` (256 entries) + producer's verification pass | Independent `git archive a4fca00…` of artifact dir + `sha256sum -c` → 256/256 OK, 0 failures (log: `manifest-verify.txt`) | VERIFIED |
| A5 | Coordinator binary identity | `environment.txt` lists `16d30538df254c04082fc56bdbfe9b2c992eae5861dfc278682e99d957aa17e3` | Independent rebuild via `eng/linux-m1/build-d5-status.sh` on the same host family → bit-exact SHA-256 | VERIFIED |
| A6 | Historical artifact boundaries | Fixture `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305`, WP04 coordinator `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b`, seccomp filter `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108` | All three identities verified present and unchanged under `artifacts/linux-m1`; no replacement of sealed positions | VERIFIED |
| A7 | Environment identity | `environment.txt` (kernel, boot, caps zero, gaga uid, cgroup family, dotnet/gcc/bwrap versions) | Live re-snapshot matches all fields (same boot epoch; log: `env-preflight.txt`) | VERIFIED |

## B. Native FD5 surfaces

| # | Aspect | Producer evidence | Reviewer independent verification | Result |
|---|---|---|---|---|
| B1 | 32-probe native suite (15 facts + 17-row theory) | `test-results.txt`, committed run-1/run-2 frames under `native-status-run1/-run2` | Rerun twice (rc=0, 32/32 each; logs `nat-run1-cg.log`, `nat-run2-cg.log`); method count reconciled against source (15 direct + 17 MemberData rows) | VERIFIED |
| B2 | Exit 21 against native RELEASE paths | Report §F (exit-code family; method `D5_ReleaseValidationExits20And21And22`) | Source-audited fixture emitters (lines 150/160/167 at the D5 16-bit header boundary; codes 20/21/22); tested assertion semantics 20 = first-64-bytes-only, 21 = RELEASE + extra byte, 22 = invalid frame (digest byte 56); rerun green ×2 | VERIFIED |
| B3 | FD5 descriptor topology | `native-status-run1/run2/descriptor-topology.txt` + `descriptor_hold.txt` (producer sample `pipe:[209719]`) | Two reviewer samples: `pipe:[238552]` (ns1) and `pipe:[255061]` (ns2), same-inode relation in each (`descriptor-topology-ns1.txt`, `descriptor-topology-ns2.txt`, `descriptor_hold.txt`); producer+reviewer = 3 independent samples | VERIFIED |
| B4 | Raw frame byte fidelity | committed `exec_confirmed.txt` (96-byte GGM1-CTL frame, magic `47474D3143544C46`, digest `00..1F`) | Reviewer frames byte-identical (`native-status/exec_confirmed.txt`); 48/50 run-1 files identical to producer, 2 run-variable files expected (pid/fd links) | VERIFIED |
| B5 | Build-script legitimacy | `eng/linux-m1/build-d5-status.sh` (SHA `cd3a01a8a9e5f081b87737d643e7113db843ceaa60dcfa730e41c29c47ae1bee`) | Source audit: no extraction step, no flag mutation, no post-build rewrite; only-rebuild path from the committed fixture; same-binary result | VERIFIED |
| B6 | Negative-oracle fidelity | theory rows assert refused frames (`TryReadNext()==false`); no skip filters; no weakened asserts | Source audit of all 15 + 17 test bodies; production `CoordinatorStatusReader` gate anchors; bypass of validation would flip tests | VERIFIED |

## C. Cross-process qualification (CR-3 / CR-3b)

| # | Aspect | Producer evidence | Reviewer independent verification | Result |
|---|---|---|---|---|
| C1 | CR-3 main flow (retry-release + strict one-FD4-write witness) | compact case `gagamba-d4-D5-CR3-a482c41f42ea48e09117873bc0fe288c` (owner: Released/Delivered/durable=True/journal_seq=5/transport_attempts=1/exit=0; pre-write witness bytes=128) | Reviewer rerun case `gagamba-d4-D5-CR3-7ac1558035ba4149a7536997e1dbffd9`: same invariants (`Released/Delivered/True/5/1/0`), exactly-one pre-write witness (artifacts `cr3-owner-result.txt`, `cr3-witness.txt`) | VERIFIED |
| C2 | CR-3 no-writer refusal (B-owner) | committed `replacement-release-nowriter-677da240ebf340b19deb14b1c9aed9d3.txt` (Refused/NotCommitted/NotAttempted/0 transports/journal unchanged at 3) | Reviewer artifact `cr3-nowriter.txt`: identical refusal block, journal_seq=3 unchanged, `writer=not-acquired` | VERIFIED |
| C3 | CR-3 fence refusal (acquisition side) | committed `replacement-recover-b745fa8d4a324288bd61bbfb4a0013f0.txt` | Reviewer artifact `cr3-fence-refusal.txt` (exit 3, `RecoveryWriterRefused` ×1 in `cr3-witness.txt`) | VERIFIED |
| C4 | CR-3 post-release retry refusal | committed `replacement-retry-release-f93b1e0c3d0a4de9a98e084ddc0ede65.txt` (baseline=5; replacement refused NotAttempted/0) | Reviewer artifact `cr3-retry.txt`: writer=acquired, baseline=5, replacement refused with journal unchanged | VERIFIED |
| C5 | CR-3b stop-then-release (durable stop committed, then refused while alive) | compact case `gagamba-d4-D5-CR3b-179704d22f164801b3a0ea5c8f94f6c8` (stop_accepted=True, durable=True, exact +5_000_000_000 ns deadline delta; release refused NotAttempted) | Reviewer rerun case `…-D5-CR3b-a26b55c1fdd84bceba9d93e6c11fce64`: identical semantics incl. exact deadline arithmetic (`70416466646801` → `70421466646801`), release refused (artifacts `cr3b-owner-result.txt`, `cr3b-witness.txt`) | VERIFIED |
| C6 | CR-3b replacement owner rebuilds state from journal truth | committed `replacement-retry-release-75ce0cc4d1cc49fdb36ef291292de583.txt` (writer=acquired; `stop=true`; durable deadline echoed) | Reviewer artifact `cr3b-retry.txt`: `writer=acquired`, `stop=true`, `stop_deadline=70421466646801` echoed from durable record, replacement refused NotAttempted/0 — same pattern, fresh lease, new pids | VERIFIED |
| C7 | Dead-owner termination (bounded standard; SIGKILL observed) | committed artifacts record coordinator/owner exit semantics incl. `exit=137` for the killed owner | termination-oracle documented (wait-for-dead + bounded SIGKILL); reviewer rerun reproduces the post-kill replacement behavior above (C6) | VERIFIED (bounded standard; no wait4/pidfd witness — as documented, CR-1) |

## D. Durable release observation (O01..O05)

| # | Aspect | Producer evidence | Reviewer independent verification | Result |
|---|---|---|---|---|
| D1 | O01 journal-truth (fresh reader views only journal files) | committed case frames (`O02`-family rows) | Reviewer rerun ×2: journal-view rows red-on-bypass (fixture-owned state not consulted) | VERIFIED |
| D2 | O03 journal-backed release class | committed case frames | Reviewer rerun ×2; classifications consistent with journal seq/durable flags | VERIFIED |
| D3 | O04 excluded-execution classification | committed case frames (`NotStarted`/`NotCommitted` boundaries) | Reviewer rerun ×2 (artifacts `cr3b-retry.txt` classification block) | VERIFIED |
| D4 | O04 note (reader-level only) | report documents the reader-refusal extent of the test | Source audit confirms production buffer-level protection (differing second offer refused) exists in `ReleaseObservationBuffer.cs` but the D5 test exercises the reader path; documented, not claimed otherwise | DOCUMENTED |
| D5 | D5-O05 buffer-level component claim | report §O05 basis | Code audit of `ReleaseObservationBuffer` (mark-gated flush; differing-second-offer refusal; all-or-none flush) + rerun ×2 | VERIFIED (component level) |
| D6 | O05 end-to-end product-surface claim | not claimed by producer (HOLD row) | grep at D5 HEAD: zero production callers of `ReleaseObservationBuffer` → integration seam not exercised; ruling C (deferred) in main review §10 | HOLD CONFIRMED |

## E. Formal release-path rows (re-evidenced under the D5 tree)

| # | Method | Producer | Reviewer | Result |
|---|---|---|---|---|
| E1 | `HeldProductionCoordinatorAcceptsSingleArbitratedReleaseAndReachesPreExecBoundary` | 1/1 (0.147 s), `release-path-1.log` | 1/1 (0.164 s), log `rp1-review.log` | VERIFIED |
| E2 | `ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport` | 1/1 (0.148 s), `release-path-2.log` | 1/1 (0.140 s), log `rp2-review.log` | VERIFIED |
| E3 | `RealCoordinatorOverallExpiryWritesProtocolFailed30AndTheGateRefusesExpiredIntent` | 1/1 (6 s), `release-path-formal.txt` | 1/1 (6 s), log `rp3-review.log` | VERIFIED |
| E4 | Focused regression roster (66 official group) | 66/66 (`regressions.log`) | 66/66 (log `regressions-cg.log`) | VERIFIED |
| E5 | No test bypass/drop | — all three methods exist at the D5 base and are diff-empty | verified; no filter change; no target execution | VERIFIED |

## F. Process and registry

| # | Aspect | Producer evidence | Reviewer independent verification | Result |
|---|---|---|---|---|
| F1 | Production immutability | (no claim) | `git diff parent..a4fca00 -- src native artifacts` empty; `eng/` gained only the build script | VERIFIED |
| F2 | D4 suite not weakened | (part of D5.1) | All D4 case rails intact; additions only (§ main review 4.3) | VERIFIED |
| F3 | Exclusion file honesty | `exclusions.txt` (committed at `5d5d6b2…`): D5-O05 HELD + descriptor destructive cases + EINTR fault-injection not claimed | Reviewed contents against actual test scope; no claim of what was not run | VERIFIED |
| F4 | Cleanup obligations | (no claim) | Reviewer post-suite checks: no coordinator processes, no owner residue, no `/tmp/gagamba-*` residue; delegated cgroup procs only shell chain | VERIFIED |
| F5 | Ordering mismatch (plan vs ancestry) | plan table ordering | Ancestry-walk shows D5.1c after evidence commits; binding unaffected (§ main review 2) | DOCUMENTED (non-blocking) |

## G. Reviewer artifact index (committed under `docs/reviews/gagamba-linux-m1-wp05-d5-review-artifacts/`)

Files (per `SHA256SUMS-reviewer.txt` — 22 entries):

- `drr-run1-cg.log`, `drr-run2-cg.log` — supervisor suite ×2 (17/17 each)
- `nat-run1-cg.log`, `nat-run2-cg.log` — native suite ×2 (32/32 each)
- `rp1-review.log`, `rp2-review.log`, `rp3-review.log` — RQ-1/2/3 individually
- `regressions-cg.log` — 66/66 focused regression run
- `env-preflight.txt` — reviewer environment snapshot
- `manifest-verify.txt` — independent 256-entry manifest verification (`git archive` + `sha256sum -c`)
- `descriptor-topology-ns1.txt`, `descriptor-topology-ns2.txt`, `descriptor_hold.txt`, `native-status/exec_confirmed.txt` — FD5 topology + frame samples
- `cr3-owner-result.txt`, `cr3-witness.txt`, `cr3-nowriter.txt`, `cr3-fence-refusal.txt`, `cr3-retry.txt` — reviewer CR-3 case artifacts
- `cr3b-owner-result.txt`, `cr3b-witness.txt`, `cr3b-retry.txt` — reviewer CR-3b case artifacts
- `SHA256SUMS-reviewer.txt` — SHA-256 of all of the above

Every artifact above was produced on the WSL2 host under the delegated cgroup, from the reviewer's own clean checkout of `a4fca00f3b2bd3f2b627d7044516d0ef8124b259`, and committed discard-free into this review branch.
