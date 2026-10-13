# Gagamba Linux M1 G2-08 R6D rereview (preserved)

- review type: independent G2-08 (WP03) rereview, R6D partial check (before WP04)
- implementation under test: b52742c3fa6b776228a2959845bbd1b15c576884
- evidence-only commit at review time: 65102852e7150c9053b77dbe44457bb420f52aa3
- branch: qualification/ar7-feasibility
- disposition: G2-08 RESOLVED - R6D PARTIAL ACCEPTED; G2-03 REMAINS BLOCKING
- resolved: G2-08 (G2-01, G2-02, G2-04, G2-05, G2-06, G2-07 and G2-09 were already resolved)
- still blocking: G2-03, G2-10
- nonblocking: G2-11 (evidence correction/maintenance)
- reviewer observation (not a blocker): ScopedAllocationContext.TryCreate proves an acknowledged write exists, not that it is the matching AllocationIntent
- G2 overall remains NOT ACCEPTED and WP04 remains blocked. The body below is the report verbatim.

# Independent G2-08 Rereview

Review-only; no repository modification, no fixes, no WP04 work. Author evidence (§21) was read only after source inspection and independent probing, and only for comparison.

## Target binding

- Branch: `qualification/ar7-feasibility`; HEAD `65102852e7150c9053b77dbe44457bb420f52aa3` (evidence commit, docs-only: `wp03.md`, `handoff.md`, `work-queue.md`).
- Implementation under review: `b52742c3fa6b776228a2959845bbd1b15c576884` (8 files: `M1RecoveryScope.cs`, `CgroupInterop.cs`, new `ScopedAllocationContext.cs`, `SafeCopyIn.cs`, `Reconciler.cs`, 3 test files).
- `git diff b52742c HEAD -- src/ tests/` empty; tree clean before/after; no preserved review touched; no WP04 code.
- Reviewer host: Ubuntu WSL2 x86_64, kernel 6.6.114.1-microsoft-standard-WSL2, glibc 2.43, .NET 10.0.112. Non-root: uid/gid 1000 (`gaga`), pre-existing `gaga-delegated` parent, no provisioning. All reviewer builds/probes in `/tmp` from a blob-verified `git archive` export.

## G2-08 disposition

**Resolved.**

## Source assessment

The defect was exactly the `&& a.Confirmed` filter (`Reconciler.cs:270` old) dropping unconfirmed `StagingDirectory` components before aggregation, after which the empty set read as success. R6D removes the filter: every recorded `Staging`/`StagingDirectory` component participates. Final aggregation (lines 394–419) is truthful — any `Failed` → `Failed`, any `Unknown` → `QuarantinedUnknown`, `CleanupConfirmed` only when stop, empty, staging, and removal are all `Confirmed`. Child-before-container ordering holds: object debt (`sawFailure`/`sawMismatch`) skips the directory loop entirely, so unresolved objects keep their directory and object failure can never be hidden by directory/domain cleanup. No other filtering/grouping logic discards unresolved components (swept the file; the only other `Where` clauses select by kind, never by confirmation state). Missing cleaner with recorded allocations → `Unknown`; unconfirmed identity can never satisfy the identity-checked cleaner path (unconfirmed entries carry dev/ino 0, e.g. `M1RecoveryScope.cs:604,629`, which matches no real directory — the harness `RetainedStagingCleaner` returns `Mismatch` there, preserving replacements per G2-07).

## Exact old counterexample reproduction

Independent probe (real temp staging dir present, snapshot `[StagingDirectory Confirmed=false]`, no cleaner, live corroborating lease, real journal): outcome `QuarantinedUnknown` (not `CleanupConfirmed`), staging component `Unknown` (not `Confirmed`), directory still present, content untouched; repeat reconciliation returns the identical outcome with the directory still present. The old result is unreachable.

## Component aggregation matrix

All independently exercised against production code: unconfirmed dir + no cleaner → Unknown/debt; unconfirmed dir + mismatch cleaner → Unknown with the cleaner actually invoked once (previously filtered) and dir surviving; staged-object failure → `Failed` with the directory cleaner never invoked and dir surviving; staged-object mismatch → Unknown with directory untouched; domain Confirmed + staging Unknown → aggregate not Confirmed; staging Confirmed + removal Unknown → aggregate not Confirmed (per-component states stay truthful); full real end-to-end cleanup (real files/dirs, identity-checked removal) → `CleanupConfirmed`, and only then — with idempotent repeat confirmation.

## Child/container ordering

Structurally (object loop gates the directory loop) and behaviorally (dir-cleaner call count zero on object failure/mismatch) verified. Identity mismatch never authorizes name-only deletion (mismatch → preserve). Reconciliation repeats idempotently.

## Idempotence assessment

Repeat runs without new evidence return identical verdicts and leave the directory and its content untouched; with persisted journal progress, a completed cleanup re-confirms rather than degrading.

## Regression assessment

No accepted finding regressed (concrete samples, all green): G2-04/05 journal suites, G2-02/G2-09 domain authority (89/89 combined), G2-07 preparation/staging cleanup (107 passed, 1 env-fixture skip), R6B.1 F1/F2 tests. Full root suite: 333 passed, 17 skipped (16 non-root-gated + 1 fixture-binary-env skip explaining the 334/16 author count; same 350 total), 0 failed. Non-root delegated: 34/34 (`NonRootQualificationTests` 16 + `ReconcilerTests` 18). Independent probe: 23/23 root, 23/23 uid-1000.

## G2-03 structural observations

G2-03 remains blocking — separate-actor D1-D6 qualification absent (author-admitted; verified: the owner binary contains zero staging/copy code, and all death coverage is in-process barriers plus domain-only owner tests). Do not mark resolved. Structural improvements observed, all consistent with accepted boundaries: raw `AllocateDomain` removed (only `AllocateDomainUnderTransaction` + integrated flow remain; reflection test pins this); `TryCreateDomain` requires a live context; intent is written before creation in both flows; the staging-directory callback fires at creation before any object copy with retention/accounting failure aborting; callback exceptions cannot silently continue (`InvariantFailure`, barriers rethrow). One softness noted (observation, not a new blocker): `ScopedAllocationContext.TryCreate` proves *an* acknowledged write (`BaselineSequence > 0`), not that the acknowledged snapshot is an `AllocationIntent` for the given execution/generation — ordering currently holds by construction in both flows. The new `_retainedRecoveryLeases` double-hold is safe (`DomainLease.Dispose` idempotent; no G2-01 impact). No concrete G2-03 defect found.

## Independent execution results

Reviewer-run: 23/23 probe checks (root) + 23/23 (gaga); `ReconcilerTests` 18/18; full M1 333+17 root; 34/34 non-root; targeted boundary samples 89/89 + 107/108. Author evidence inspected only (§21 claims checked against source; counts differ by the one environmental fixture skip).

## Remaining global blockers

- G2-03 (separate-actor death qualification absent; structural 1A/1B accepted as progress, not acceptance)
- G2-10
- G2-11 maintenance
G2 NOT ACCEPTED. WP04 BLOCKED.

G2-08 RESOLVED - R6D PARTIAL ACCEPTED; G2-03 REMAINS BLOCKING
