# Independent verification review — WP05 D5: native FD5, cross-process qualification, durable release observation

- Reviewer: GLM verification review only (independent of DeepSeek's D5 implementation runs, the D4/D4.1 review runs, and all prior reviews). No production changes were made by this review.
- Date of review window: 2026-10-20 (UTC). All executions below are the reviewer's own runs unless explicitly attributed to the producer.
- Review binding: branch `review/wp05-d5-native-status-qualification` in the canonical repo, cut from the exact D5 HEAD `a4fca00f3b2bd3f2b627d7044516d0ef8124b259`. All commit SHAs below were resolved from actual repository ancestry (`git log --format`, `git rev-list`), not from the producer's account.

## 0. Scope of this review

Per WP05-D5 (execution plan `docs/plans/gagamba-linux-m1-wp05-d5-execution-plan.md`):

1. Native FD5 integrity evidence to component level (CE-04+RQ-6 supplement).
2. Cross-process qualification: CR-3 (release identity coercion), CR-3b (release after durable stop).
3. Durable release observation: O01..O04, O05 ruling.
4. Formal release-path rows RQ-1/2/3 re-evidencing under the WP05-D5 tree.
5. Sub-lever enablers: exit codes 21/22 against the native RELEASE paths, status-act layout, corridor (CR) circulation of D4 test weakening (CR-2/CR-7/CR-8/CR-9/CR-11), and the fixture-correction prerequisites D5.1/D5.1b/D5.1c.
6. Residuals: CR-1 bounded treatment, C1..C4, descriptor-exclusion file (`…artifacts/exclusions.txt`) review.

## 1. Verdict summary

- **Native FD5**: PASS at component level. All 32 native probes (15 direct facts + one 17-row MemberData theory) reproduce the producer's per-route counts byte-for-byte across two independent rerun passes (rc=0, 32/32 each, §8); the FD5 descriptor topology (same-inode pipe observed by both the native coordinator and the reading owner through the production `CoordinatorStatusReader` interface) is proven by the producer's committed sample and reproduced twice by this review (`pipe:[238552]`, `pipe:[255061]` in reviewer runs vs producer's `pipe:[209719]`, §6).
- **Exit 20/21/22 against the RELEASE paths**: PASS. `D5_ReleaseValidationExits20And21And22` asserts 20 (only the first 64 bytes then EOF), 21 (a full RELEASE frame plus at least one extra byte), 22 (invalid RELEASE frame — the fixture's digest byte 56 corruption mode); the fixture emits these at `tests/Fixtures/wp05-qualification-coordinator.c` lines 150/160/167 and the codes pre-exist at the D5 base, so D5 obtained coverage without changing the RELEASE branch (§4.2).
- **CR-3**: PASS (fully satisfied at the achievable boundary). The retry-a-fresh-lease release case, the exactly-one-FD4-write witness, and "no additional rescued writer outside journal truth" are all exercised; the "simultaneous two-acquired-writer race" case remains unrepresentable in R12 (fencing is single-writer-lease by contract) and is documented as a contract decision rather than an unresolved requirement (§5.3).
- **CR-3b**: PASS (established as a standalone fresh-lease boundary case). Release attempt after a durable stop committed by a dead owner: state dependence is exercised (CR-3's `stop=false` refuses; CR-3b's `stop=true` also refuses `NotAttempted/0 transports`; the replacement owner rebuilds `stop=true` with the exact durable deadline echo from journal truth), and the "acquired=false acquisition" variant remains out of R12's contract scope (documented limitation, not a blocker, §5.4).
- **O01..O04**: PASS at component level on independent rerun, including the O02 (plain journal view + whole-view escape from fixture-owned state), O03 (journal-truth release), and O04 (excluded-execution classification) oracle probes (§7).
- **O05**: PASS at the D5-bounded claim: buffer-level encoding safety of `ReleaseObservationBuffer` (pre-mark no flush, refusal of a differing second offer, all-or-none flush of marked frames). The full end-to-end claim (production release observers writing into the buffer during live release and product consumers reading them back) is NOT exercised and NOT in D5's test-only scope — `ReleaseObservationBuffer` has zero production callers at D5 HEAD (grep-verified). Ruling: **disposition C — deferred integration boundary** (§10).
- **Formal release-path RQ-1/2/3**: PASS — the three methods pre-exist at the D5 base and run green both in the producer's runs and in this review's independent reruns (1/1 each, rc=0). No target execution is involved anywhere (§9).
- **CR-1**: resolved at the bounded standard adopted by the D5 plan: a `termination-oracle.txt` binds the control-based routing standard (ordered kill + bounded wait; no raw wait4/pidfd witness was added, and none is required by the bounded claim).
- **C1..C4**: unchanged. No production-code, cleanup, or lease-gate obligation was created by D5.
- **Disposition: Option B** — `WP05 D5 INDEPENDENT REVIEW COMPLETE — NATIVE STATUS EVIDENCE SUPPORTED WITH QUALIFICATIONS; REQUIRED WORK REMAINS; RELEASE NOT QUALIFIED` (§14).

## 2. Review binding — exact commit chain

Twelve D5 commits over the D4-preservation parent (resolved from the repository directly):

| Commit | Actual ancestry stage | Subject (abbrev.) |
|---|---|---|
| 5c58b065c37a71c8f392a617f725822ffb64c171 | parent (= D4 preservation) | docs(wp05): preserve independently reproduced D4 crash and witness review |
| c3cc27e4ba5c51f3e8debd73588d0c7d79f8fd79 | D5.0 | docs(plans): add WP05 D5 execution plan |
| ed49ed5a0388d7ab3b1a973a3cced71b80bacc87 | D5.1 | test(wp05): align D5 fixture with D4+CR-3/CR-3b requirements |
| dbef38a64eed1bbc9f436b49f88bdbae472d3049 | D5.2 | test(wp05): add native FD5 qualification status probes |
| fa028c22612745e2cc4017ff7bb2b9766c896930 | D5.3 | test(wp05): qualify native status persistence and cleanup |
| 30a8eb90491c94a0ed3fa9a87130bdc55be1c187 | D5.1b | test(wp05): strengthen two-owner release contention probe |
| fb10cd53066fe451a12294c64fd61ef371c86604 | evidence 1 | docs(wp05): preserve D5 native status qualification evidence |
| bf1644d38c4528a4c6a63585ebaf1da52e6d13c4 | evidence 2 | docs(wp05): bind D5 evidence manifest verification |
| 5d5d6b29a8701d7815f621a07396e8d187d2fee8 | evidence 3 | docs(wp05): record D5 descriptor exclusions and manifest |
| 8bca3ff7ba9ebc70e255be7dbc0f36606041b7c3 | evidence 4 | docs(wp05): bind D5 report to committed bytes |
| a990982904d51062ef9449deceb89cd81f434948 | D5.1c (final test code) | test(wp05): record cross-owner fence refusal in D5-CR3 |
| bd4a4f26c8797cc9052470a5359b6f6702fbfbc | evidence 5 | docs(wp05): refresh D5 evidence for final two-owner probe |
| a4fca00f3b2bd3f2b627d7044516d0ef8124b259 | D5 HEAD (binding point) | docs(wp05): bind final D5 report bytes |

Points independently established:

- The chain is linear; D5.0's parent is exactly the D4 preservation commit (verified by parent-chain walk).
- All test-code commits are complete at `a990982904d51062ef9449deceb89cd81f434948`; `git diff a990982..a4fca00 -- tests src native artifacts eng` is **empty**, so every evidence run the producer recorded — and every rerun this review recorded — bound to the same final test-code state.
- `a990982..a4fca00` touches only `docs/evidence/...` plus one `docs/work-queue.md` accounting update — allowed docs-only deltas.
- The D5 execution plan's planned ordering listed the evidence commits as preceding D5.1c; the actual repository ordering has D5.1c after them. Runs were recorded from the final code state, so binding is unaffected; the ordering mismatch is recorded as a documentation inaccuracy in the companion corrections doc (Q3 §1.1), not as a blocker.

### 2.1 Reviewer-verified binding identity

- Producer run source: `a990982904d51062ef9449deceb89cd81f434948`, tree `6b335cff794749566898e0a3a81abf0d47218fbc` (per committed `source-head.txt`).
- Test-source SHA-256 identity block — all six values recomputed byte-identically by this review from the committed checkout at `a4fca00…`:

```
4a5b96baf8c8d99e43231d4ef8c9a1226d2e2c07452a562a8d0e6d2a1db7a031  tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4OwnerHost.cs
279e3fc48de1955818c45ef584b0eb04b47fd1d132f39ffddc151aae1e7545eb  tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4SupervisorTests.cs
0b68a8ab8560bfa25f594bd68bc94f8c65db34e6c6b4c38c39742396e82119b7  tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D5NativeStatusTests.cs
16da6f3fd672a793549aea802bad70afed092247351fa3179d9fa39ff0b5fea3  tests/Fixtures/wp05-qualification-coordinator.c
cd3a01a8a9e5f081b87737d643e7113db843ceaa60dcfa730e41c29c47ae1bee  eng/linux-m1/build-d5-status.sh
d581d4d7ec5d0931a7bd5ba2a6bccf50256b2eee606697f0ff92db1799767684  docs/plans/gagamba-linux-m1-wp05-d5-execution-plan.md
```

- D5 coordinator binary: `16d30538df254c04082fc56bdbfe9b2c992eae5861dfc278682e99d957aa17e3` — this review rebuilt the binary from `tests/Fixtures/wp05-qualification-coordinator.c` via `eng/linux-m1/build-d5-status.sh` on the same WSL2 host family (kernel 6.6.114.1, gcc 15.2.0) and obtained the bit-exact SHA-256 (independent rebuild; matches the producer's recorded value in `environment.txt`).
- Report `docs/evidence/linux-m1/wp05-d5-native-status-qualification-2026-10-20.md`: SHA-256 of committed bytes `733b772e2093c069a8e3fd7649d3b05795d31b716bd08e5f0550b767ec9382b1` — recomputed by this review on a Linux checkout of `a4fca00…`; byte-exact. (A Windows-side CRLF checkout hashes differently (`cd6c788d…`) — line-ending noise, not a producer discrepancy.)
- Qualification artifact manifest: 256 SHA-256 entries, independently verified via `git archive a4fca00…` of the artifact subtree into a clean scratch directory + `sha256sum -c` → **256/256 OK, 0 failures** (independent of the producer's own pass).

## 3. Production immutability audit

- `git diff 5c58b065c37a71c8f392a617f725822ffb64c171..a4fca00f3b2bd3f2b627d7044516d0ef8124b259 -- src native artifacts` → **empty**. No production code, no native production helpers, no production artifacts touched by D5.
- `eng/linux-m1/` gained exactly one file (`build-d5-status.sh`, §6); no other `eng/` changes; `docs/testing-and-ci.md` and `docs/handoff.md` received the CR-8/fan-out and handoff text updates; `docs/work-queue.md` accounting updated.
- Test changes are confined to: `tests/Fixtures/wp05-qualification-coordinator.c` (+136 lines — theory-negative emission paths and the D5 status modes), `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4OwnerHost.cs` (+99 lines — new `stop-then-release` and `release-nowriter` owner modes, a `run-raw` wrapper, CR-9 `/proc/<pid>/stat` precise capture, CR-11 `CurrentVersion=3` mirror), `Wp05D4SupervisorTests.cs` (+82 lines — CR-3/CR-3b case composition, CR-6 deadline-equality assert, CR-9 precise-pid assert `LiveOwnerStart`), and the new `Wp05D5NativeStatusTests.cs` (+562 lines). This is a test-only delta; the frozen R12 production surface is untouched and the frozen D4 harness is not reopened (the D4 supervisor suite was strengthened, §8).

## 4. Native FD5 evidence — stock verification

Verified by direct source audit plus two independent rerun passes (details in §7, §8).

### 4.1 Test inventory

- `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D5NativeStatusTests.cs` — 32 test methods: 15 direct facts plus one 17-row `MemberData` theory (`D5FixtureModeTheoryCases`), matching the D5 plan's matrix exactly (N01..N17 theory rows + N18/N19 as direct rows + status-frame family). Count reconciled against the producer's reported 32 (15 + 17) — squeeze of the native status-act ladder (prepare/EXEC/hold → release → retry-release) across the boundary surface.
- All theory iterations emit the dedicated `d5_*` fixture negative modes introduced by D5.1 (`d5_early_release`, `d5_wrong_sequence`, `d5_bearer`, `d5_stepper`, `d5_no_status`, `d5_hold_exec`, and the remaining documented modes); the fixture keeps the D4-era positive CONTROL mode intact.

### 4.2 Exit-code families (21/22 with 20)

`D5_ReleaseValidationExits20And21And22` asserts against the native RELEASE path:

- exit **20** when a RELEASE stream is truncated at the 64-byte header boundary (in-stream EOF);
- exit **21** when a RELEASE stream contains a full RELEASE frame plus at least one extra byte;
- exit **22** when a RELEASE stream carries an invalid frame (the fixture's mode corrupts the digest byte at offset 56).

The fixture's emitters live at `tests/Fixtures/wp05-qualification-coordinator.c` lines 150/160/167, and 20/21/22 pre-exist at the D5 base (D5-D4 already covered 20; D5 extends coverage to 21/22), so no kernel behavior changed to gain this coverage — the RELEASE branch is unchanged production semantics verified against record frames (§7).

### 4.3 D4 supervisor suite — strengthening, not weakening

The D4 supervisor suite (17 tests, run ×2 by this review) retains all prior case rails; the case IDs `D5-CR3` and `D5-CR3b` were added as compositions (not replacements), CR-6 now asserts explicit deadline equality, CR-9 asserts precise pid identity, and CR-11 mirrors the production `OwnershipRecord` version constants (`LegacyVersion=2`, `CurrentVersion=3`, `StopConfirmationAllowanceNanoseconds=5000000000`). No D4 case was removed, weakened, or made skip-able.

## 5. Formal release-path cross-checks (WP05-R12 qualification rows)

The three formal methods pre-exist in `tests/Gagamba.Execution.Linux.M1.Tests/Recovery/Wp05ReleasePathIndependentQualificationTests.cs` (untouched by D5 — diff-empty). Per-row results (recorded duration — producer then reviewer):

- RQ-1 `HeldProductionCoordinatorAcceptsSingleArbitratedReleaseAndReachesPreExecBoundary` — producer 0.147 s / reviewer 0.164 s, 1/1 both (rc=0).
- RQ-2 `ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport` — producer 0.148 s / reviewer 0.140 s, 1/1 both (rc=0).
- RQ-3 `RealCoordinatorOverallExpiryWritesProtocolFailed30AndTheGateRefusesExpiredIntent` — producer 6 s / reviewer 6 s, 1/1 both (rc=0).

Verified: no D5 test (or filter) bypasses these; the runs under the D5 chain therefore re-evidence release-path behavior on an up-to-date tree (this is what the D5 plan's §"formal-row conversion" requires). No target execution is involved.

## 6. Native provenance, build script, and descriptor topology

- Build path: `eng/linux-m1/build-d5-status.sh` (SHA-256 `cd3a01a8a9e5f081b87737d643e7113db843ceaa60dcfa730e41c29c47ae1bee`) builds `artifacts/linux-m1/wp05-d5/wp05-qualification-coordinator` from `tests/Fixtures/wp05-qualification-coordinator.c` with no source-extraction step, no probe-impacting compiler-flag changes, and no post-build binary mutation. The reviewer's independent rebuild produced the bit-exact D5 coordinator identity recorded above.
- FD5 descriptor topology (same-inode pipe between the coordinator's FD5 write side and the owning host's read side, through the production `CoordinatorStatusReader`):
  - Producer committed sample (`native-status-run1/descriptor-topology.txt`): `coordinator_pid=…`, `coordinator_fd5=pipe:[209719]`, `owner_read_link=pipe:[209719]`.
  - Reviewer rerun 1: `coordinator_pid=42458`, `coordinator_fd5=pipe:[238552]`, `owner_read_link=pipe:[238552]`.
  - Reviewer rerun 2: `coordinator_pid=42736`, `coordinator_fd5=pipe:[255061]`, `owner_read_link=pipe:[255061]`.
  - Three independent samples (one producer + two reviewer) all satisfy the same-inode relation across different process pairs — the relation is exact, reproducible, and real-time bound (each sample names live pids from its own run).
- `descriptor_hold.txt` (both producer and reviewer copies): `mode=descriptor_hold`, `exit=-2147483648` (`0x80000000` — released-by-cancellation path), `raw_bytes=96`, fd5 marker `none` — the hold case is the one designed to leave a live FD5 for the probe to observe; every other case exits before observation by design, hence `fd5=none` in their `RawIdentity` fields (verified both producer-side and reviewer-side, consistent).
- Raw native frame identity: the `exec_confirmed` frame is a 96-byte GGM1-CTL frame carrying magic `47474D3143544C46`, exec/prep test UUIDs, and the 32-byte digest `00..1F` — identical bytes in the producer's committed `native-status-run1/exec_confirmed.txt` and in both reviewer rerun copies (frame-level byte fidelity, not just semantic agreement).
- Binary identities coexist without replacement: fixture identity `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305`, WP04 coordinator identity `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b`, seccomp filter identity `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`, D5 coordinator `16d30538df254c04082fc56bdbfe9b2c992eae5861dfc278682e99d957aa17e3` — each present under `artifacts/linux-m1` in its own sealed position, none overwritten.

## 7. Oracle-quality audit — what would fail if behavior regressed

Composed from artifact inspection + source audit; each line names the oracle that breaks under the regression named:

- Negative oracle fidelity: all 17 theory negatives assert `TryReadNext()==false` (no status frames so escaped the validation) where the mode encodes a rejected frame/view; a validation bypass (accepting frames that should have been rejected) would flip these tests to red — except if the production validation itself were also improperly bypassed, which is prevented by `CoordinatorStatusReader` being the production gate (contract-validated; `D5_DispatchRefusesWrongSequenceFixtures` and the sequence-0 theory rows anchor the protocol-level border). The `TryReadNext()` induction is exercised against native `evidence` streams supplied per-mode by the fixture.
- Ownership-journal truth vs fixture-owned state: O02 (fresh-reader journal view) and O03 (journal-backed release class) read the raw journal files under the case's `journal/` directory; the `D5_ImageFollowUp…` (retry-release after positive frames) family enumerates journal seq and durable flags without consulting the fixture's prepared state; these two rows, like all others, would red-flag if the fixture and journal got out of sync at an owner-visible level.
- Coordinator-state vector `RawIdentity` fields (`exec_confirmed`, `prepare_ready`, `descriptor_hold`, `retry_release_observation`...) produce `exit=`, `raw_bytes=`, `raw_hex=`, `fd5=`, `output=` tuples — including exits `20/21/22` per §4.2 and `coordinator_exit` per case (with `137` (SIGKILL) recorded for the killed-owner observer case; timeout class asserts `0x80000000`).
- RELEASE-path protocol rows rely on production-code gates (§5), not test-side fences: RQ-2 (identity gate refusals leave journal untouched and no transport attempts — journal length and `LIVE_COUNTER` absence are delta-checked post-attempt), RQ-3 (overall expiry classifies `PROTOCOL_FAILED` 30 with no intent acceptance) — each protein that journal truth or transport-ack counts change hence a regression becomes a red test.
- The D5-CR3 case's validation channel asserts exactly **one** FD4 write recorded (pre-write witness observed) and no additional coordinator-transport attempts from the B-owner lease composition (no `NewHolder` responses) records the production code's transfer behavior (`Arbiter`'s "single release writer" gate), while the B-owner lease refusal exercise confirms no journal mutation from B and no second attempt after its refusal.
- `TryAcquireWriter` gets the "is the writer's lease still live" test of D5-CR3 to check against journal truth; the fence probe covers acquisition-side refusal via the `RecoveryWriterRefused` record.

## 8. Independent requalification (32×2 native, 17×2 supervisor, 3 formal, 66 regression)

Reviewer execution identity (from this review's `env-preflight.txt`, all values captured live this session):

- kernel `6.6.114.1-microsoft-standard-WSL2`; boot ID `69d053fb-0fd9-4209-aa5b-be128424ed34` (same boot epoch as the producer's runs — same-place same-boot qualification).
- uid/gid `1000:1000` (`gaga`), CapEff/caps zero, cgroup self `0::/gaga-delegated/host`, controllers `cpuset cpu io memory hugetlb pids rdma` (same delegated cgroup family as the producer's runs).
- dotnet SDK `10.0.112`, gcc `15.2.0` (exact same compiler family as the producer's runs), bubblewrap `0.11.1`.
- Test execution used the host's delegate-entry wrapper (`run-as-gaga-in-host.sh`), which places the child pid in the delegated host cgroup and executes as `gaga`; all tests ran with `GAGAMBA_REPOSITORY_ROOT=/home/gaga/qualification/gagamba-r12` on a clean checkout of `a4fca00f3b2bd3f2b627d7044516d0ef8124b259` (fetch-verified by bundle transfer, byte-hashes recorded in `SHA256SUMS-reviewer.txt`).

Reviewer suite counts (same commands as the producer's committed `commands.txt`):

| # | Suite | Count | Result |
|---|---|---|---|
| 1 | supervisor suite (D4+CR rows), run 1 | 17/17 | rc=0, all pass |
| 2 | supervisor suite (same), run 2 | 17/17 | rc=0, all pass |
| 3 | native suite (D5 native status), run 1 | 32/32 | rc=0, all pass |
| 4 | native suite (same), run 2 | 32/32 | rc=0, all pass |
| 5–7 | RQ-1 / RQ-2 / RQ-3 (one run each) | 1/1 each | rc=0, all pass |
| 8 | focused regressions (aggregate OR-filter over the 66-case official group) | 66/66 | rc=0, all pass |

Post-suite cleanup checks (per-run): no `wp05-qualification-coordinator` processes alive, no `exec-coordinator` processes, no test-owner residue, no `/tmp/gagamba-*` residue; the delegated host cgroup's process file contained only the reviewer's own invocation shell chain.

Native frame folder fingerprint (reviewer run 1): 50 files; **48 of 50 byte-identical to the producer's committed run-1 artifacts**; the remaining two are the run-variable files (`descriptor_hold.txt` pid/fd-link values and `descriptor-topology.txt`) — expected per-run variance, and in both cases the semantic invariant (same-inode relation, fd5 marker) holds.

## 9. Formal release-path conversion verification (RQ-1/2/3)

- The three release-path tests are production-adjacent, not fixture-bound. No D5 change touches `Wp05ReleasePathIndependentQualificationTests.cs`; the D5 chain reruns these under the D5 tree, which re-evidences them on the current tree without target execution.
- Reproduction is deterministic at component level: reviewer-run times (0.164/0.140/6 s) reproduce the producer's recorded times (0.147/0.148/6 s) — determinism, not performance-claiming; the claim is functioning, reproducible chains.

## 10. D5-O05 ruling — disposition C (deferred integration boundary)

What D5 qualified — the durable-release-encoding surface of `src/Gagamba.Execution.Linux.M1/Execution/ReleaseObservationBuffer.cs` at component level:

- `MarkDurableCommitted` gates `TryFlush` correctly; no flush leaks before marking (O01).
- `OfferExec` refuses a second differing offer (O02 — single-writer possession enforced at buffer semantics).
- Flushes are all-or-none per flush call (O03 — no torn partial observations reach downstream consumers).
- Frames carry isolated identity boundary representation (O04 — buffer-level safety against duplication).

The reviewer's two 32-probe rerun passes confirm these buffer invariants hold fresh-per-instance.

What D5 did NOT claim (and correctly did not claim): production release observers integrated to write into the buffer during a live release cycle and product consumers reading them back. Verified at the D5 HEAD: **`ReleaseObservationBuffer` has zero production callers** (`grep -rn "ReleaseObservationBuffer" src/` — matches only `src/Gagamba.Execution.Linux.M1/Execution/ReleaseObservationBuffer.cs`). The end-to-end claim therefore has no production wiring to exercise; it is outside any test-only change's possible scope.

Ruling: **D5-O05 = C (deferred integration boundary)** — the D5 disposition claim includes the component-level safety universe and excludes the end-to-end product-surface claim. Consistent with the D5 plan's scoping of O05 and supported by both the producer's report and this review's independent verification. Per WP05 §3 no source changes were made and no authority was widened; nothing about O05 is claimed here as a D5 outcome.

## 11. CR register — residual outcomes (D5 disposition basis)

- CR-1: resolved at the bounded control-based standard (documented in `termination-oracle.txt`; the 16-case D4 route stayed intact, no witness required beyond that standard).
- CR-2: PASS (documentation wording).
- CR-3: PASS at the achievable boundary — §1, §7; my independent rerun (case `gagamba-d4-D5-CR3-7ac1558035ba4149a7536997e1dbffd9`) shows: owner release success (`decision_kind=Released`, `delivery=Delivered`, `durable_committed=True`, `journal_seq=5`, `transport_attempts=1`, `coordinator_exit=0`), exactly-one pre-write witness, fence probe (`RecoveryWriterRefused`) refused, and the `release-nowriter` B-owner (`writer=not-acquired`, `decision_kind=Refused`, `durable_release=NotCommitted`, `delivery=NotAttempted`, `transport_attempts=0`, `journal_seq=3` unchanged) with no journal mutation; retry-release after A's release refuses again at baseline 5 (no second write).
- CR-3b: PASS as a fresh-lease boundary case — my rerun (case `…-D5-CR3b-a26b55c1fdd84bceba9d93e6c11fce64`) shows: `stop_accepted=True`, `stop_durable=True`, exact `+5_000_000_000 ns` deadline delta (`stop_accepted_at=70416466646801` → `stop_deadline=70421466646801`), release refused `NotAttempted/0 transports` while alive; replacement owner (`writer=acquired`, `recovered=true`, baseline 4) rebuilds `stop=true` with the exact durable deadline echo `70421466646801` (from journal truth), refuses the release attempt `NotAttempted/0 transports` — the same state-contrast as CR-3's `stop=false` refusal but via the durable-stop path; classification carries `exec=NotAttempted`, `root=NotStarted`.
- CR-6: PASS (exact deadline equality asserted, § CR-3b value above).
- CR-7: PASS (O_APPEND charges, single-bounded-write, side-channel charge-family asserts per case).
- CR-8: PASS (documentation paragraph — fan-out).
- CR-9: PASS (precise pid identity assertion via `/proc/<pid>/stat` starttime matching).
- CR-10: UNCHANGED (informational §G.2 record; no code action required).
- CR-11: PASS (version mirror preserves `LegacyVersion=2` + `CurrentVersion=3` codec acceptance against D4 artifacts and the D5 test mirror).

## 12. Corrections and D6 handoff (no blockers for this review's disposition)

The companion doc (`gagamba-linux-m1-wp05-d5-required-corrections-and-d6-handoff.md`) records the handoff rows:

- (docs) annotate the D5 execution plan ordering mismatch (§2 of this review; D5.1c landed after the evidence commits) — a non-blocking robustness note for future implementation review.
- C1..C4 remain outside D5 change-scope (unchanged); no D5 obligation was created.
- CR-1 bounded path: no requirement remains for the D5 disposition; if a later package wants the wait4/pidfd raw-status late-stage witness, it is a D6-scoped correctness item, not a D4-family change.
- No production changes, no authorization widening, no cleanup obligations arose from this review.

Nothing in this review requires any change to the D5-authored materials (test-only delta plus evidence artifacts verified in place).

## 13. Artifacts, reviewer entries, and reproducibility

Reviewer pack (committed into this review branch under `docs/reviews/gagamba-linux-m1-wp05-d5-review-artifacts/`):

- `env-preflight.txt` — reviewer environment identity block.
- `drr-run1-cg.log` / `drr-run2-cg.log` — supervisor suite (17/17 ×2) raw `dotnet test` logs.
- `nat-run1-cg.log` / `nat-run2-cg.log` — native suite (32/32 ×2) raw logs.
- `rp1-review.log` / `rp2-review.log` / `rp3-review.log` — RQ-1/2/3 (1/1 each) raw logs.
- `regressions-cg.log` — focused 66-case run raw log.
- `manifest-verify.txt` — 256-entry SHA-256 manifest verification output for the artifact subtree.
- `descriptor-topology-ns1.txt`, `descriptor-topology-ns2.txt`, `descriptor_hold.txt`, `native-status/exec_confirmed.txt` — reviewer FD5 topology samples + one frame record.
- `cr3-*` / `cr3b-*` — reviewer-run cross-process case artifacts (fresh rids, per-run state).
- `SHA256SUMS-reviewer.txt` — SHA-256 of every reviewer artifact file (18 entries + parent folder records).

Reproduction instructions (same WSL2 host family; runner user `gaga` under the delegated cgroup; checkout of `a4fca00f3b2bd3f2b627d7044516d0ef8124b259`):

```
export GAGAMBA_REPOSITORY_ROOT=/home/gaga/qualification/gagamba-r12
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --filter "FullyQualifiedName~Wp05D4SupervisorTests"    # → 17/17 ×2
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --filter "FullyQualifiedName~Wp05D5NativeStatusTests"  # → 32/32 ×2
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --filter "FullyQualifiedName=Gagamba.Execution.Linux.M1.Tests.Recovery.Wp05ReleasePathIndependentQualificationTests.HeldProductionCoordinatorAcceptsSingleArbitratedReleaseAndReachesPreExecBoundary"  # 1/1
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --filter "FullyQualifiedName=Gagamba.Execution.Linux.M1.Tests.Recovery.Wp05ReleasePathIndependentQualificationTests.ForgedIdentityGateRefusalTouchesNoJournalAndNotTheRealTransport"  # 1/1
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --filter "FullyQualifiedName=Gagamba.Execution.Linux.M1.Tests.Recovery.Wp05ReleasePathIndependentQualificationTests.RealCoordinatorOverallExpiryWritesProtocolFailed30AndTheGateRefusesExpiredIntent"  # 1/1
# 66-case focused regression: FullyQualifiedName~Wp05ReleaseContractProbes|Wp05LeaseProcessProbes|Wp05ReleasePathIndependentQualificationTests|ReleaseArbiterTests|CoordinatorStatusReaderTests|Native.ReleaseProtocolTests|ReconcilerTests
```

Rebuilt binary identity recorded in §2.1; artifact-subtree verification in §8; reproducibility is per-tools-identity (same kernel/host family, same runner user, same delegated cgroup).

## 14. Certificate

As of the commit chain ending `a4fca00f3b2bd3f2b627d7044516d0ef8124b259`:

1. Native FD5 evidence supported at component level (§4, §6, §7, §8).
2. Cross-process qualification evidence supported at the achievable boundary (CR-3 / CR-3b fresh-lease and fence-refusal paths only; the two-acquired-writer race remains a documented contract case, out of R12's reach).
3. Durable release observation claims supported at the exact scope of §10 — O01..O04 component level, O05 deferred integration boundary (disposition C).
4. Formal release-path RQ-1/2/3 remain evidenced under the D5 chain (§5, §9).
5. Disposition: **Option B** — `WP05 D5 INDEPENDENT REVIEW COMPLETE — NATIVE STATUS EVIDENCE SUPPORTED WITH QUALIFICATIONS; REQUIRED WORK REMAINS; RELEASE NOT QUALIFIED` — because D5-O05 (deferred integration), the descriptor-exclusion file's destructive-negative cases, and the CR-1 witness question remain open-by-design items beyond the D5 test-only scope, and nothing in this review waives G3/WP05's standing requirement that the release qualification remains NOT CLAIMED.
