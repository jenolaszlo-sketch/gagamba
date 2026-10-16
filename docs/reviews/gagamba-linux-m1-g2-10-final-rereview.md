# Gagamba Linux M1 G2-10 final rereview (preserved)

- review type: independent G2-10 final rereview (before WP04)
- implementation under review: 40e8adda46a44e29465c739d3498e7387be4ce8a
- qualification/evidence commit at review time: 127c8e63fea2b0a19b91ac790a0713236eb42ab5
- branch: qualification/ar7-feasibility
- disposition: G2-10 RESOLVED - UNPRIVILEGED PIDFD QUALIFICATION ACCEPTED
- resolved: G2-01, G2-02, G2-03, G2-04, G2-05, G2-06, G2-07, G2-08, G2-09, G2-10
- nonblocking: G2-11 (evidence correction/maintenance)
- note: the final non-root D1-D6 matrix plus D5 replacement executed successfully; non-root D6 closed the explicit outstanding G2-10 evidence debt; no G2-10 remediation remains
- G2 overall remains NOT ACCEPTED and WP04 remains blocked. The body below is the report verbatim.

# Independent G2-10 Final Rereview

Review-only; repository untouched (HEAD `127c8e63`, tree clean; execution in `/tmp`+`/root` scratch from a blob-verified `git archive 40e8add` export).

## Target binding

- Branch `qualification/ar7-feasibility`; impl `40e8add`, evidence `127c8e63`; `git diff 40e8adda 127c8e63 -- src/ tests/` empty — qualification ran against the accepted code with zero prod/test delta.

## Original G2-10 finding

Per preserved reviews: the selected unprivileged pidfd-backed recovery topology was unqualified — inventory/delegation/pidfd/exit-takeover/orphan-reap were proven (R5), but staging-death coverage was same-process exceptions and separate owner cases never executed integrated staging. Required: actual separate staging-actor death with surviving non-root recovery at decisive barriers; every allocated object recoverable under the surviving-scope model. Later tranches supplied the owner-owned transaction (R6D.2 topology) and the root D1-D6 matrix (G2-03); this tranche had to prove the full matrix unprivileged, including the explicit non-root D6 debt.

## Environment / privilege assessment

- Host: Ubuntu WSL2, kernel 6.6.114.1, x86_64, glibc 2.43, .NET 10.0.112. Boot ID stable across runs.
- Qualification ran as uid/gid 1000 (`gaga`), CapInh/Prm/Eff/Amb all zero, NoNewPrivs 0, cgroup `0::/init.scope` for the harness (owner likewise uid 1000 per `/proc`).
- Delegated parent `gaga-delegated`, `gaga:gaga` mode 755, subtree `memory pids` (no cpu), pre-existing `host` leaf; root subtree `cpu memory pids` untouched. No sudo/provisioning/ACL/controller changes by me (verified before/after states identical).
- The `dotnet vstest` invocations themselves ran under `wsl -u gaga`, so the qualification process tree (including spawned owners) is unprivileged by construction; cgroup writes succeeding under the gaga-owned parent corroborate it.

## Delegation boundary

Bounded as required: memory+pids only, no general `/sys/fs/cgroup` write, no CPU widening, no host mutation, no keepalive interference (no keepalive present in this session; none needed).

## Fresh non-root matrix

Independently rerun as uid 1000: `OwnerDeathQualificationTests` **7/7** (D1, D2, D3, D4, D5, D5-replacement, D6 — including the previously missing D6), `NonRootQualificationTests` **12/12**, vector **1/1**. Author's 7/7 + 12/12 confirmed.

## D1 disposition

Pass. Intent durable, StartAuthority validated, live owner, exclusive writer, nothing created; SIGKILL kill, pidfd exit proof, takeover from seq 1, `QuarantinedUnknown` repeat-stable. Old exit-25 handshake failure absent.

## D2 disposition

Pass. Real domain, retained exact authority, intent-stage journal; kill; takeover; stable `Refused` (conservative — no registered identity exists, correct); retained handles stop/remove the exact domain; sibling untouched; no path/PID reconstruction.

## D3 disposition

Pass. Registered identity durable and corroborated; kill; takeover; `CleanupConfirmed` repeat-stable. G2-09 preserved.

## D4 disposition

Pass. Real dir with exact identity, retained + accounted, copy incomplete; kill; takeover; exact cleanup → `CleanupConfirmed` repeat-stable. G2-07/08 preserved.

## D5 disposition

Pass. Accounted tool exists, input genuinely absent; kill; takeover; tool removed, absence preserved, no false container confirmation; repeat stable.

## D5 replacement disposition

Pass. Post-death replacement → mismatch detected, preserved, no name-only deletion, `QuarantinedUnknown` repeat-stable. Recovery non-destructive.

## D6 disposition

**Pass — the explicit debt is closed.** Ran (not skipped) as uid 1000: Prepared durable, live owner pre-kill, no Mint/store/capability in scope, real kill with pidfd/exit/reap proof, surviving scope, post-death takeover, no Mint or reconstruction across repeats, truthful `CleanupConfirmed` repeat-stable.

## pidfd/lifecycle proof

pidfd acquired live with CLOEXEC, not-exited pre-kill, exit-observed post-kill, single reap; exercised in every D-row (test asserts) plus my independent 5 s hold (10 samples: alive + pidfd-not-exited + excluded throughout, then kill 137). PID diagnostic-only; authority cgroup-FD based throughout.

## Writer lifecycle

Exclusion-while-live asserted in every row (competitor refuses); takeover-only-after-death with baseline == latest acknowledged seq; no overlapping usable leases. G2-05 preserved.

## Recovery-authority audit

All destructive ops via retained fds + corroborated identity + acknowledged journal state. No path reopening, no PID authority, no post-death handle creation, no owner-memory dependence.

## Protocol / descriptor proof

Exercised end-to-end by every D-row (real channel, transfers, acks, barriers): SEQPACKET, bounded one-record receives, type dispatch, exec/gen binding, sequencing, exact counts/roles, CLOEXEC, close-on-reject, TRUNC/CTRUNC paths, queued-record handling, 64 B collision case. No G2-02 weakening (framing suites green in full runs).

## Capability-locality proof

Store created + Mint executed owner-side only (structural, unchanged); scope receives descriptions; D6 asserts no store/capability pre- and post-death with repeat. G2-06 preserved.

## Host non-modification

Delegated parent present, `gaga:gaga` 755, `memory pids` subtree before and after; my one probe-created temp parent removed by its owner; only permanent empty `host` leaf remains.

## Cleanup

Zero `gagamba-*` cgroups; zero matching `/tmp` dirs (one probe temp needed manual `rmdir` after the killed owner's handles released — best-effort cleanup can lag process death; verdict-neutral). No live owners, no unreaped children observed.

## Regression / vector results

Root full M1 353+13 with 0 failures on the deciding run (author: 354+12; same 366 total — bucket flip of environment-sensitive skips); two transient parallel flakes seen across runs (copy/mount/sentinel-race class), each green isolated/repeated — unrelated to this delta. Windows M1 257+109, build 0/0; vector 873 B / `82161029…805d5f3` (incl. as uid 1000). Selections identified: 63 protocol/identity, 68 journal/fencing, 43 closure/cleanup, 48 barrier/reconciler/lease, F1 2/2.

## Limitations

Single-host class (WSL2); reboot cleared prior delegation mid-history (re-provisioned by operator before this session); transient parallel flakes documented; evidence PIDs ephemeral by nature (mechanisms + exit codes asserted, not bare names).

## G2-10 disposition

All §27 questions answer yes: finding reconstructed accurately; qualification genuinely unprivileged; all rows + replacement executed (D6 actually ran); pidfd correct as lifecycle-only evidence; authority retained and sufficient; fencing exact; no PID/path reconstruction; capability locality preserved; delegation bounded and external; cleanup complete; no counterexample found.

## Remaining global status

- G2-01 through G2-10 resolved; G2-11 maintenance only. Still: G2 NOT ACCEPTED; WP04 BLOCKED (whole-G2 review + G2-11 pending).

G2-10 RESOLVED - UNPRIVILEGED PIDFD QUALIFICATION ACCEPTED
