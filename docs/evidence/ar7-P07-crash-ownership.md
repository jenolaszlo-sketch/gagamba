# AR-7 P07 — Crash, root status and release ownership

Date: 2026-10-10. Slice: architecture-blocking feasibility probes. **Conclusion: SUPPORTED BY THIS PROBE** (Linux rootless candidate only; crash/ownership/release scope only).

## Source binding and environment

- Frozen production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, `qualification/ar6-native`, `ar6-review-baseline-06c8ac7`; unchanged. No `src/`, `eng/`, packaging, or production changes in any P07 commit.
- Verdict-run harness: commit `b8861bf` (`spikes: P07 fix str-in-bytes, branch-aware C5, cascade-corrected C7/C8`), tree clean at release. Harness bytes bound to the run: `p07.py` sha256 `90ae2ef7b8badfd10ad3fc02090b7c63cd86323043ddc690669f8647944ad70a`, `p07.c` sha256 `694a7a3855133cb83b05bdd8a79a09d77867c085c32cf1c75e205fce9aca77b4` (both equal `git show b8861bf:` bytes). Compiled static worker sha256 `107954d4debddd378e940cd266317f74a1e3d14d90332158db8a2cce39085b47` (`gcc -static -O2`, rc 0, no warnings recorded).
- C11 wrapper-run harness: commit `826438d` (wrapper observes release via target leaf; suite path byte-identical to `b8861bf`).
- Candidate composition (unchanged from frozen P02/P03/P04): pinned non-setuid Bubblewrap `0.11.1`, sha256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6`, mode `0755` (pin + non-setuid asserted in-harness; run refuses otherwise), same namespaces/cap-drop-ALL/`no_new_privs`/cBPF bytes, same cgroup topology (outer 256 MiB/swap 0/64 tasks; delegated pass-through; manager/target/positive + sibling 64 MiB/16; target explicit 128 MiB/48).
- Native host (measured, not inventory): Linux kernel `6.6.114.1-microsoft-standard-WSL2`, x86_64, yama ptrace_scope `1`, `user.max_user_namespaces` 30743. Verdict nonce `70cf6e33690c4bd1b24ebec4824d2b08`; wrapper nonce `2bb5adb292294908bc0f75ea20b7bf07`.
- Prior P07 commits on this branch (history, not verdict): `3a00c4c` (first harness, unexecuted at commit), `dd90280` (trailer reachability, NSpid resolution, hold bound), review clarifications `81ff611`. Two pre-qualification runs exposed harness bugs only (unreachable `complete` trailer; host-PID lookup of namespace-local PIDs; `hold` killed by `alarm(12)`; `str in bytes`; C5 fault landing post-setup; C7/C8 expectations contradicting the init-death cascade; wrapper relay gap) — fixed, re-run, superseded. No candidate change was ever made to pass a leg; all superseded runs reported cleanup-confirmed and left no residue.
- Scope: Linux only. Windows is out of scope for P07 (P01B stands). Exclusions: every guarantee beyond crash/ownership/release semantics, the P04 inode-ceiling limit (carries forward), production implementation, deferred probes P05/P06/P08/P09, and independent review (still pending; model-agnostic gate before final acceptance, not a verdict input).

## Question and mechanism

Can the surviving setup topology guarantee that failures during setup, release, execution, cancellation, and teardown never produce a target that is both released and no longer lifecycle-owned — while preserving truthful distinction between target-root completion, wrapper/helper completion, and loss of observation?

Executed design: `docs/reviews/2026-10-10-ar7-p07-design.md` as approved with the three review clarifications (controller-owned check→commit barrier with pre-exec re-check; observer as enforcer+oracle with authority proof and C11; owned-survival vs unowned-failure semantics). Ordering authority was the manager state machine; monotonic timestamps were single-domain evidence only (no mtime comparison anywhere); stops used `cgroup.kill` + `populated` polling only; fault-injection kills addressed freshly-validated PIDs (NSpid-resolved with cgroup+starttime+cmdline, unique match required) and are the test itself, never a stop mechanism.

## Positive controls, crash matrix, and external oracle

Positives (direct worker, positive leaf, GO immediate): sleeper, pipeholder, exit17 (rc 17), daemonizer, forker-5, reexec (second generation observed), interfere — 7/7 gates true. The interfere positive and restricted legs show identical denial shapes (host cgroup unreachable, `setns`/`mount` denied), so the restricted denials below are not sandbox-specific artifacts.

| Case | Result |
| --- | --- |
| positives | SUPPORTED-leg (7/7 gates) |
| C1 caller dies pre-setup | SUPPORTED-leg (vacuous; no `c1` workspace residue) |
| C2 caller dies mid-provisioning (SIGKILL self, signo 9) | SUPPORTED-leg (observer audit: manifest entries `c2-f1`,`c2-f2`, residue `[]`) |
| C3 manager dies pre-release (signo 9) | SUPPORTED-leg (target domain empty, zero bwrap delta) |
| C4 manager dies post-release (signo 9, RELEASED marker read) | SUPPORTED-leg (observer killed via outer `cgroup.kill`, outer empty; root status UNKNOWN, never inferred) |
| C5 bwrap-outer dies at earliest post-spawn instant | SUPPORTED-leg, pre-READY branch (`fault_landed_pre_ready` true, wrapper rc -9, drained via owned pipe, empty in 0.00025 s; no target ever passed the commit barrier) |
| C6 ns-init killed (leaf scan: outer host-PID + exactly one other bwrap; SIGKILL ns-init) | SUPPORTED-leg (chain collapsed, wrapper rc 137, domain empty in 0.00017 s) |
| C7 root exits 0 while child sleeps 8 | SUPPORTED-leg, corrected (see mechanism note; empty in 0.00016 s, no survivor) |
| C8 pipe-holder 5 s | SUPPORTED-leg, corrected (output complete, rc 0 preserved, pipe closed at root exit, domain empty; elapsed 0.0017 s recorded as cascade evidence) |
| C9 deadline during setup (hang victim, 4 s manager watchdog) | SUPPORTED-leg (watchdog fired, domain empty) |
| C10 validated target root killed (resolved host-PID) | SUPPORTED-leg (chain collapsed, wrapper 137, empty in 0.00028 s; root_result `SIGKILL-injected-against-validated-PID`, known by construction) |
| cancel_det (flag pre-created) | SUPPORTED-leg (`CANCELLED-BEFORE-RELEASE`, zero spawns — bwrap delta 0 corroborates — domain empty) |
| cancel_race 5/5 | SUPPORTED-leg (all `lost-race-owned-cleaned`, domains emptied; single-domain monotonic triples recorded as evidence, never as ordering authority; no release after an observed cancellation) |
| bat_daemon (double-fork + setsid) | SUPPORTED-leg (reparented grandchild stayed in target leaf, killed via domain, empty) |
| bat_fork (40 children) | SUPPORTED-leg (complete, empty) |
| bat_reexec (`execve` self) | SUPPORTED-leg (second generation marker, confinement inherited, empty) |
| bat_interfere | SUPPORTED-leg (rc 0 + complete + empty; denials: `setns` EPERM after successful ns-fd open, `mount` EPERM after mkdir, host cgroup unreachable; same-shape denials as the positive) |
| C11 suite leg (hold marker + rc 0) | SUPPORTED-leg (owned release; stop verified by suite-teardown domain kill) |
| C11 wrapper (observer SIGKILLed mid-teardown, rc -9) | SUPPORTED-leg (release independently observed via target leaf; manifest read; outer emptied via kill; states stayed Unknown, never inferred; all identities removed) |

Mechanism note (C7/C8 correction, evidence-grade): the design anticipated post-root-exit survival, but the PID-namespace init-death cascade (design Section 2) kills every remaining member the instant ns-init reaps the root — c7 empty in ~0.00016 s with an 8 s sleeper outstanding, c8 pipe closed in ~0.0017 s against a 5 s hold intent. The mechanism refutes the expectation and proves something strictly stronger: post-exit survival is unconstructible in this topology. The verdicts above rest on the corrected expectations (known root result + complete output + immediate empty + no survivor), recorded here rather than as a design rewrite.

Oracle notes: the 50 ms population sampler corroborates (7 bwrap samples with cgroup/starttime/cmdline bindings; NSpid chains confirming ns-PID 1/2 placement) but sub-100 ms processes can be missed (c8 delta 0 despite a proven release) — deltas corroborate, the state machine governs. No release after a won stop occurred in any round; no released-and-unowned workload, no whole-domain-stop survival, no owner-death defeat, no stale-identity kill occurred.

## Cleanup

Suite run: outer `cgroup.kill`, reaped, `populated 0` (`cleanup_events: populated 0 / frozen 0`), known leaves removed bottom-up (no unknown descendants — preserved-and-abort path never triggered), workspace files removed by exact name list, `/dev/shm` swept clean of `p07-*`. `cleanup_absence`: outer/workspace/manager gone, shm clean; `cleanup_confirmed: true`; no setup or cleanup error. Wrapper run: observer killed mid-teardown as designed; wrapper audit emptied the outer domain via kill, removed all leaves, workspace, and manifest; `outer_gone` + `workspace_gone` true. Post-run host sweep confirmed no `gagamba-ar7-P07-*` cgroup, workspace, shm, or process residue. Raw transcripts and binaries remain under ignored `artifacts/ar7/P07/`; committed records are `docs/evidence/ar7-feasibility/P07/70cf6e33690c4bd1b24ebec4824d2b08.json` (sha256 `fa2db62c601683e38ad3cd507509e4a6a70108d2031910514dbf6fe34d6bc6db`), `c11-wrapper-2bb5adb292294908bc0f75ea20b7bf07.json` (sha256 `c24afcbaae24dceef8cd157be27f01fdd9852410f16ab9dacaf2477d6d73942a`), and `independent-cleanup.json`.

## Stop reason and architecture consequence

18/18 legs SUPPORTED with no halt and no repair; the Linux track advances. No released-but-unowned target, no release after a won stop, no survival past a required whole-domain stop, no target-preserved control authority, no meaningless root status, no dishonest cleanup, no stale-identity action was observed. The C7/C8 cascade correction strengthens (not weakens) the containment claim and is preserved above as a reviewable finding. Ownership remains enforceable by the root observer through the externally owned cgroup after manager/caller failure (proven in C2/C3/C4 and re-proven in every cleanup); observer failure itself is a tested Unknown-then-audited path (C11), not an assumption and not a new guardian.

This file was a NOT-RUN stub dated 2026-10-09 (P01 hard-stop era) retained as history; the record above supersedes only its P07 disposition. P01/P01B counterexamples and the P04 inode-ceiling limit are untouched.

## Luna review and non-claims

Independent review is pending (model-agnostic gate before final acceptance); this verdict is the probe's own evidence, not the independent qualification. No final architecture selection, production/public API/release change, privileged persistent helper, publication, or merge occurred. No Windows claim, no inode-ceiling claim, no P05/P06/P08/P09 claim is made here. See [consolidated disposition](restricted-execution-AR-7-feasibility.md).
