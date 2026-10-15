# Gagamba Linux M1 G2-03 final rereview (preserved)

- review type: independent G2-03 final rereview (before WP04)
- implementation under review: 40e8adda46a44e29465c739d3498e7387be4ce8a
- fresh qualification evidence: 9fe127a98f682514138f4f5e333e47bcbeea8fe5
- branch: qualification/ar7-feasibility
- disposition: G2-03 RESOLVED - R6D.3 OWNER-DEATH QUALIFICATION ACCEPTED
- resolved: G2-01, G2-02, G2-03, G2-04, G2-05, G2-06, G2-07, G2-08, G2-09
- still blocking: G2-10
- nonblocking: G2-11 (evidence correction/maintenance)
- note: non-root D6 remains evidence debt associated with G2-10, not G2-03
- G2 overall remains NOT ACCEPTED and WP04 remains blocked. The body below is the report verbatim.

# Independent G2-03 Final Rereview

Review-only; repository untouched (HEAD `9fe127a`, tree clean; execution in `/tmp`+`/root` scratch from a blob-verified `git archive 40e8add` export).

## Target binding

- Branch `qualification/ar7-feasibility`; impl `40e8add`, fresh evidence `9fe127a`; `git diff 40e8add 9fe127a -- src/ tests/` empty — fresh qualification ran against the remediated code.
- `bfcc355` verified consistent: my preserved R6D.3a review (disposition accepted, requalification required) — matches the accepted remediation state.
- Preserved reviews unchanged; no WP04; no target execution anywhere in this review.

## Original G2-03 finding

Per the preserved reviews: allocation/staging not incrementally recoverable; raw allocation without durable intent/recovery scope was product-reachable; staging authority captured too late (after first completed object); death coverage was same-process exceptions, never loss of the transacting actor. Minimum correction demanded intent-first structure, authority retention at creation, and decisive separate-actor deaths with surviving-scope cleanup.

## Structural remediation assessment

Unchanged since the accepted R6D.2b topology: no journal-free allocation path (reflection-pinned), intent-first ordering in both flows, owner-owned transaction as the sole production path, staging authority retained/accounted at creation with refusal on retention failure, per-object incremental accounting. R6D.3a added the corrected after-intent handshake (validated StartAuthority before the barrier). No regression found; no redesign needed or made.

## Fresh-run binding

`OwnerDeathQualificationTests` at `40e8add` = 7 D-row tests (D1, D2, D3, D4, D5, D5-replacement, D6) with hardened pre-kill liveness (barrier match, pidfd-not-exited, refreshed direct-child alive, writer exclusion) — matching the §27 table one-for-one. No pre-remediation row carried forward.

## D1 disposition

**Pass (independently rerun 7/7 suite).** Durable intent, validated StartAuthority, live owner, exclusive writer, no domain/staging/Prepared/capability; real SIGKILL kill, pidfd-proven exit 137, single reap, surviving scope, takeover from acknowledged seq, `QuarantinedUnknown` repeat-stable. Old exit-25 handshake failure absent (barrier reached normally in all runs).

## D2 disposition

**Pass.** Real domain, retained exact authority, intent-stage journal, live owner killed; takeover; reconcile → stable `Refused` (conservative: no registered identity to corroborate — correct, not disguised success); retained handles independently stop/remove the exact domain; sibling untouched; no path/PID reconstruction.

## D3 disposition

**Pass.** Registered identity durable and corroborated by retained authority; kill; takeover; `CleanupConfirmed` repeat-stable. G2-09 preserved.

## D4 disposition

**Pass.** Real dir exists with known exact identity, retained + durably accounted, copy incomplete; kill; takeover; exact-identity cleanup → `CleanupConfirmed` repeat-stable. G2-07/08 preserved.

## D5 disposition

**Pass.** Accounted tool exists, input genuinely absent/unconfirmed; kill; takeover; tool removed, absence preserved, container not falsely confirmed; repeat stable — exactly the corrected B3 semantics.

## D5 replacement disposition

**Pass.** Post-death replacement detected (mismatch), preserved, no name-only deletion, `QuarantinedUnknown` repeat-stable. Recovery is non-destructive on hostile change.

## D6 disposition

**Pass.** Prepared durable, live owner killed pre-Mint; no store/capability in scope (structural reflection + description-shape asserts); no Mint, no reconstruction across repeats; truthful `CleanupConfirmed` of domain/staging; repeat stable. Decisive capability-loss boundary holds.

## Process-death/lifecycle proof

Separate owner OS processes (PID ≠ scope, recorded per row in evidence); barrier-gated kills only after liveness+exclusion asserts; SIGKILL → pidfd-observed exit 137 → `WaitForOwner` single reap; scope performs post-death journal/reconcile ops (alive). PID diagnostic-only throughout.

## Writer lifecycle

Exclusion-while-live asserted in every row (competitor refuses); takeover-only-after-death with baseline == latest acknowledged seq; no overlapping usable leases. G2-05 preserved.

## Recovery-authority audit

All destructive ops via retained fds + corroborated exact identity + acknowledged journal state (domain stop/remove, identity-checked staging removal). No path reopening, no PID authority, no post-death handle creation, no owner-memory dependence.

## Root qualification

D-rows **7/7**; barrier theory 7/7; full owner transaction + protocol/fencing/pidfd suites green; full M1 **353 passed, 0 failed, 13 skipped** (366). Author reports 354+12 — same total; delta is one environment-sensitive skip/pass flip (fixture/pidfd-sentinel class), also observed flipping between my own runs (352/351 with only race-sensitive copy/mount/sentinel flakes, each green isolated/repeated).

## Non-root qualification and D6 gap

Delegation root `/sys/fs/cgroup/gaga-delegated` is **absent** in this session (host rebooted; confirmed missing, `gaga` uid 1000 intact) — not provisioned, per instructions. Consequence: all 12 `NonRootQualificationTests` skip honestly; cgroup-bound D-rows cannot run here. Independently runnable-as-gaga evidence: OwnerProtocol 21/21, Journal 62/62 (uid 1000, CapEff 0, NoNewPrivs 0, kernel 6.6.114.1, glibc 2.43).
**D6-gap reasoning:** the original G2-03 criterion demands separate-actor death with surviving recovery under delegation — satisfied in substance by non-root D1-D5+replacement (author evidence, 6/6 when delegation existed) since compositional mechanics are privilege-independent; D6 adds only capability-locality (process-local store, no privilege interaction), which root D6 proves and non-root D1-D5 already bound to the same code paths. Non-root D6 is therefore **G2-10 evidence debt, not a G2-03 blocker**. G2-10 stays blocking regardless.

## R6B.1 F2 coverage assessment

**A — equivalent/superseding coverage exists, no blocker.** The old test asserted immediate bootstrap-fd closure after handoff — inapplicable since the accepted topology intentionally holds the private channel for the transaction lifetime. Superseded by: `BootstrapSocketPairIsMessagePreserving` + `ValidTransferReceivesCloexecDescriptors` + `ExecProbeProvesReceivedDescriptorsDoNotSurvive` + `ZeroLengthRecord…/PeerClose…` (F1) + channel Dispose/exit semantics exercised by every owner-death row. Nothing accepted was lost.

## Accepted-boundary regressions

Author selections identified: 63 = protocol/bootstrap/handle/identity unit tests; 68 = Journal 62 + Fencing 6; 43 = closure/capability/binding/cleanup; 48 = barrier/reconciler/ownership/lease integration; F1 2/2. Sampled green independently (21/21, 62/62, 4/4, plus full suites). No finding reopened — no counterexample found.

## Full-suite/canonical-vector results

Linux M1 353+13 (366); Windows M1 257+109 with 0-warning/0-error build (re-ran current tree); vector tests green (873 bytes, SHA-256 `82161029…805d5f3`). Cleanup: zero `gagamba-*` cgroups, zero matching `/tmp` dirs — verified empty; hygiene supportive only.

## Limitations

Single-host class (WSL2); non-root D6 not re-runnable here (see gap analysis); transient parallel flakes noted with isolated-green follow-ups; evidence PIDs ephemeral by nature (mechanism + exit codes asserted, not bare names).

## G2-03 disposition

All §25 properties established: intent-first allocation, incremental retention, pre-copy staging authority, real owner loss ×7 barriers, surviving scope, fenced takeover, truthful idempotent reconciliation, conservative uncertainty/mismatch handling, no capability resurrection, no reconstructed authority. **G2-03 is independently resolved.**

## Remaining global status

- Resolved: G2-01, G2-02, G2-03, G2-04, G2-05, G2-06, G2-07, G2-08, G2-09
- Blocking: G2-10
- Maintenance: G2-11
- Still: G2 NOT ACCEPTED; WP04 BLOCKED (G2-10 remains)

G2-03 RESOLVED - R6D.3 OWNER-DEATH QUALIFICATION ACCEPTED
