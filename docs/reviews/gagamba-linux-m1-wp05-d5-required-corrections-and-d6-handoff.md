# WP05-D5 review — corrections closure and D6 handoff

Companion review deliverable (same branch as the main review and evidence matrix; base = exact D5 HEAD `a4fca00f3b2bd3f2b627d7044516d0ef8124b259`). Downstream implementers should read this together with the main review (`gagamba-linux-m1-wp05-d5-independent-native-status-review.md`) and the D4-review handoff (`gagamba-linux-m1-wp05-d4-required-corrections-and-d5-handoff.md`, preserved in D4 review history).

## 1. Corrections-stage closure

All corrections registered against WP05-D5 are closed at the D5 chain, each with independent verification (main review §§2–11, artifacts committed with this review). Nothing found during this review required changing the D5-authored materials; the audit below records per-item closure status.

| # | Correction | Location / composition (D5 12-commit chain) | Closing evidence (reviewer-verified) |
|---|---|---|---|
| 1.1 | Fixture prerequisite corrections (D5.1) | `ed49ed5a0388d7ab3b1a973a3cced71b80bacc87` — fixture `d5_`-mode emitters, CR-11 version-source mirror | Test-source SHA-256 verified; suite reruns green ×2 ×2 |
| 1.2 | Native FD5 probes (D5.2/D5.3) | `dbef38a64eed1bbc9f436b49f88bdbae472d3049`, `fa028c22612745e2cc4017ff7bb2b9766c896930` | 32/32 ×2 (logs `nat-run1-cg.log`, `nat-run2-cg.log`); exit-20/21/22 emitters verified |
| 1.3 | Two-owner contention probe (D5.1b) | `30a8eb90491c94a0ed3fa9a87130bdc55be1c187` | 17/17 ×2; CR-3 refresh case `…-D5-CR3-…-a482c41f42ea48e09117873bc0fe288c` bound to final code state |
| 1.4 | Durable stop/release (CR-3b case) | `ed49ed5a…` + `30a8eb9…` composition | stop-then-release owner mode exercised; exact +5_000_000_000 ns deadline arithmetic verified on reviewer run |
| 1.5 | Cross-owner fence refusal (D5.1c) | `a990982904d51062ef9449deceb89cd81f434948` | `RecoveryWriterRefused` path exercised (exit 3; reviewer artifacts `cr3-fence-refusal.txt`, `cr3-witness.txt`); diff-empty for `tests src native artifacts eng` after this commit |
| 1.6 | Evidence byte preservation + manifest verification (E1–E5) | `fb10cd53066fe451a12294c64fd61ef371c86604`, `bf1644d38c4528a4c6a63585ebaf1da52e6d13c4`, `5d5d6b29a8701d7815f621a07396e8d187d2fee8`, `8bca3ff7ba9ebc70e255be7dbc0f36606041b7c3`, `bd4a4f26c8797cc9052470a5359b6f6702fbfbc`, `a4fca00f3b2bd3f2b627d7044516d0ef8124b259` | 256-entry manifest verified independently (`git archive` + `sha256sum -c` → 256/256; log `manifest-verify.txt`); report SHA verified on committed bytes (`733b772e…`) |
| 1.7 | Exit 21/22 coverage (relative to native RELEASE paths) | fixture `d5_*` emitters at the D5 chain + test `D5_ReleaseValidationExits20And21And22` | verified at source (lines 150/160/167, `tests/Fixtures/wp05-qualification-coordinator.c`) and by rerun; kernel RELEASE branch unchanged (base exit semantics) |
| 1.8 | CR-1 bounded closure | `termination-oracle.txt` (committed evidence) | bounded control-based standard adopted; witness item handed to D6 (below) without blocking this disposition |
| 1.9 | Exclusion honesty | `exclusions.txt` (D5-O05 HOLD + destructive descriptor cases + EINTR fault-injection not claimed) | reviewed against actual test scope; no claim of what was not run |

### 1.1 Non-blocking documentation note

The D5 execution plan's chain table listed three evidence commits as preceding D5.1c; repository ancestry shows D5.1c (`a990982…`) after them. Runs were bound to the final test-code state and the manifest/report binding is to the final chain, so no evidential impact. Recommend a future docs pass annotate the plan (not required for this review's disposition).

## 2. D6 handoff rows

Each row names exactly what remains outside the D5 disposition and what a future work package must supply. None of these rows blocks the D5 Option B certificate; all are scoped to keep the release qualification unclaimed until supported.

- **(H1 — D5-O05 integration seam)** Production release observers are not wired to write `ReleaseObservationBuffer` entries during a live release cycle; the buffer has zero production callers at the D5 HEAD. Component-level safety stays qualified; the end-to-end claim requires production owner/status wiring outside any test-only work package. Until supported, the release flag's product-surface readers must continue to fail closed — no evidence exists that activates them.
- **(H2 — descriptor destructive-exclusion cases)** The exclusion file records unclaimed behaviors: an inherited extra writer delaying EOF semantics for the reader, an inherited helper writing on FD5 from a different process, wrong-FD/deselect complexities. None were exercised by D5 and none were claimed. Smallest safe future experiment remains the test-only second-writer case (simultaneous same-pipe writers while the production reader validates the first frame); it requires a work package authorizing fixture/mode additions plus a documented EOF decision, and it must not touch production.
- **(H3 — CR-1 optional witness)** The bounded control-based termination standard is adopted. An optional `wait4/pidfd` (raw-status) witness would harden late-stage attribution; it is an unclaimed enhancement, not a qualification requirement, and must land only as a new test-only surface (no frozen-harness reopening).
- **(H4 — C1..C4)** Unchanged boundaries; unresolved by design; outside WP05 and outside this review's change scope.
- **(H5 — documentation pass)** Annotate the D5 plan's ordering deviation (§1.1) whenever those docs are next touched; no urgency, no re-bind required.

## 3. Reviewer closing notes

- No production changes were made by D5 or by this review; `src/`, `native/`, and `artifacts/` diffs against the D4-preservation parent are empty.
- No authority was widened: D5 is a test-and-evidence-only package plus docs. All D5 surfaces exercise production paths through their normal entry points (fencing, journal, FD4/FD5 descriptors via the coordinator binary, delegated cgroup).
- No target execution was performed or authorized by D5 or by this verification review; the release qualification remains **NOT QUALIFIED**.
- Reviewer artifacts (this branch, `…artifacts/` directory): test logs ×4 suites, RQ logs ×3, regression log, environment snapshot, manifest verification log, FD5 topology samples, CR-3/CR-3b fresh-run case files, and the reviewer SHA-256 digest list over the same.
