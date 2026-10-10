# AR-7 P04 — Linux resource and writable-storage bounds

Date: 2026-10-10. Slice: defensive feasibility only. **Conclusion: SUPPORTED BY THIS PROBE**, limited to this exact host, frozen composition, and tested cases.

## Source binding and environment

| Field | Recorded value |
| --- | --- |
| Production baseline | `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`; frozen branch/tag unchanged; no `src`/`eng`/packaging drift |
| Design | [P04 design](../reviews/2026-10-10-ar7-p04-design.md) (reviewed + frozen-composition clarifications) |
| Exact executed probe revision | `e2cb05c8d0d153080a7da5f14e9cef04f7fe442d`, clean at release |
| Prior setup observations (no verdict) | `dc0673a7` (cross-scope placement), `a041d77e` (FSIZE-vs-CoreCLR, hidden binary), `a82a5e34` (bind-source hidden), `2961113b` (memfd offset, threads regex), `0ff2eb8a` (dropped UID handover), `4d15efdf` (sibling double-create), `eb7ecf24` (wrong-UID positives + residue, since removed) |
| Nonce (verdict run) | `74346e3119e64a3599ccbdda8eaf2f7e`; corroborating runs `ac0d735d`, `23ae461d` (16/16 native legs each), `cb148ffe` (19/19 legs, one residue file) |
| Host | Ubuntu 26.04.1 LTS on WSL2, x86_64, kernel `6.6.114.1-microsoft-standard-WSL2`, glibc 2.43, GCC 15.2.0, Python 3.14.4, .NET SDK 10.0.401 (host publish only) |
| Helper | Bubblewrap `0.11.1`, SHA-256 `523da3e7…`, mode `0755` non-setuid (pin-gated) |
| Filter | Frozen P02/P03 cBPF bytes, SHA-256 `30bfaf6b…` (identical) |
| Candidate identity | Host UID/GID 65534, zero caps, `NoNewPrivs:1`, `Seccomp:2/filters:1` (externally sampled) |
| .NET staging | Self-contained `linux-x64` folder publish, binary SHA-256 `2c7bd57e…`; loader closure (8 libs incl. `ld-linux`, each hashed) ro-bound at absolute paths |

Probe source is [p04.c](../../spikes/Ar7Feasibility/p04.c) with [driver](../../spikes/Ar7Feasibility/p04.py) and [workload](../../spikes/Ar7Feasibility/dotnet/Program.cs) ([project](../../spikes/Ar7Feasibility/dotnet/P04Workload.csproj)). Durable raw record: [P04 observations](ar7-feasibility/P04/74346e3119e64a3599ccbdda8eaf2f7e.json) plus [independent cleanup](ar7-feasibility/P04/independent-cleanup.json). Raw transcripts/binaries stay under ignored `artifacts/ar7/P04/`.

## Question and composition

Can this candidate bound resource consumption and writable state honestly
enough for `restricted-offline-closure-v1` while still running useful native
and self-contained .NET workloads? The isolation mechanism (namespaces,
cap-drop, filter, NNP) matches P02; the 128 MiB/48-task
bounds moved from inherited to explicit target-leaf limits (identical
effective values; the delegated parent passes through outer's 256 MiB/64).
Configuration exception (independent review §6): P03 historically executed
in the manager leaf rather than the target leaf, and P04 expanded workload
mounts/environment and removed the inherited file-size limit for .NET —
relevant configuration differences, so no blanket identical-placement claim
is made across probes even though the confinement mechanisms are comparable.
Workload closure binds (`/target`, `/input`, `/app` + loader chain) and
`HOME=/tmp` are workload preparation, not enforcement — stated here so they
are never cited as boundary evidence.

## Positive controls (per-case gates, all held before each release)

Native cases ran in the outer-bounded `positive` leaf; .NET cases likewise.
Every case's positive completed with exact markers (touched checksums,
created counts, byte counts, echo, exit codes/signals, stdio-only FDs)
before its candidate was released. Any gate failure skips only that case's
candidate (`INCONCLUSIVE`, never a pass).

## Restricted results (19/19 SUPPORTED-legs, no halt, no anomalies)

Memory: native `mem 64` completed (RSS 66 MiB, peak attributed to
verified-touched pages); `mem 192` was OOM-killed (`oom_kill` +1, exit
137 = bwrap's 128+SIGKILL convention, target peak clamped at exactly
134217728). Requested-vs-touched is explicit everywhere: untouched mappings
are non-evidence under overcommit.

Tasks (Linux tasks, never relabeled as processes): 8/40 threads created
exactly; 60-thread attempt created **45** with `pids.max` event firing —
exact enforcement arithmetic (2 bwrap scaffolding + 1 main + 45 = 48 = the
limit, then `EAGAIN`).

Storage: scratch stopped at exactly **8388608 bytes / 8 files** with
`ENOSPC`; fd-consume stopped at exactly **61** (`EMFILE` under the shared
inherited `RLIMIT_NOFILE=64` table minus stdio — this demonstrates that
inherited limit, not an additional namespace restriction); SysV SHM (16 MiB
lifecycle: created, touched, detached and removed with host-invisible
afterwards observed; later `memory.peak` values are cumulative across the
reused leaf and do not independently attribute SHM/slab charging) and
`/dev/shm` (1 MiB, host-invisible afterwards) observed within those bounds;
`oom_score_adj` lowering denied `EACCES` (measured on both modes, not the
earlier `EPERM` wording — corrected against the committed record)
while raising succeeds; null sinks bounded; ro-closure write-open denied
(`EACCES`, measured on both modes — not the guessed `EROFS`, corrected in
evidence); empty `/sys`+`/run` are writable private tmpfs (created+removed
by the probe, memcg-bounded, destroyed at teardown — same class as `/tmp`).

Output/crash: 1 MiB stdout + 64 KiB stderr delivered with exit code 17
intact. Correction (independent review M3): the 32,768/8,192 character
truncations are evidence-prefix limits applied after full `communicate()`
collection, not bounded streaming capture — no stream bound or overflow
handling is demonstrated; finite collection with exit-code preservation is.
Forced `abort()` delivered SIGABRT
(rc 134 = 128+SIGABRT convention) with no core artifact in any surviving
location (`RLIMIT_CORE 0`) and an empty domain. This covers only the
observed tested crash behavior; exhaustive absence of every possible host
core-dump/core-sink path is explicitly not established.

.NET (self-contained, invariant globalization, workstation GC, no heap
knobs): `hello` started with ~27 MiB working set and 7–8 tasks — a
successful startup observation (correction: not a measured minimum memory
budget), returned as architecture input; the
128 MiB profile was not raised. Artifact binding digests cover the apphost
and loader libraries; no digest binding is retained for `P04Workload.dll`
and the complete published closure (correction, independent review M5). `alloc64` completed (~100 MiB).
`alloc160` (positive: ~200 MiB) was OOM-killed (`oom_kill` +1, rc 137).
Host provides 2 GiB swap, yet every over-budget case OOM-killed with
`swap.max=0` set — no swap escape (per-case `swap.current` sampling was
not instrumented; stated as a limit, with the OOM behavior as the
substantive proof).

Mount inventory: the candidate's `/proc/self/mountinfo` was cross-cut
against the expected set (`/`, `/proc`, `/dev` + six device nodes, 8 MiB
`/tmp`, empty `/sys`+`/run`, ro `/target`+`/input`+`/app`+loader paths);
zero unknown writable mounts. Correction (independent review M5): this is
the native-configuration inventory — it does not contain `/app` or the
.NET loader mounts, so complete writable-closure qualification is not
established. Writable locations are classified
bounded (tmpfs, shm, pipes), visible-denied (`EROFS`/`EACCES`), or
invisible (`ENOENT` — never presented as denial).

### Inode/metadata ceiling: reconciliation (from the committed record only)

Verdict for an *independently bounded* inode/metadata ceiling: **not
configured** — and therefore enforcement unobserved. The 8 MiB scratch
tmpfs was created with a byte `size=` only; no separate `nr_inodes=` was
ever configured, so the kernel default (host-RAM scale, millions) applied
implicitly. The `inode-fill` case stopped at the 20000-file harness safety
cap without `ENOSPC` — the cap prevented reaching either the byte ceiling
or any inode limit, so no inode-denial was observed. What *was*
configured + observed: the byte ceiling (exactly 8388608 B to `ENOSPC`)
and the memcg ceiling (dentry/slab consumption stays inside the 128 MiB
accounting — no OOM across 20000 files). Those bound total consumption but
are not an inode-specific claim. The independent inode ceiling remains an
**unresolved P04 qualification limit for the final architecture decision**;
it is preserved here as a limit, not converted into success. No further
test was run for this question and the candidate was not changed.

## Review, consequence, and non-claims

Independent review completed 2026-10-10 ([review](../reviews/2026-10-10-ar7-independent-evidence-review.md)): P04 VERDICT SUPPORTED WITH QUALIFICATION — touched-memory OOM evidence, task accounting, exact 8,388,608-byte scratch exhaustion, inherited FD ceiling, and useful native/.NET execution stand; independent inode bounds, bounded capture, complete charging measurements, and complete writable-closure qualification are not established. The narrowings above record that disposition; no P04 rerun was performed. Checklist
for the future reviewer: per-case gate completeness; OOM attribution
(event + 128+signo convention); thread arithmetic including bwrap
scaffolding; byte-exact storage ceilings; the unmeasured inode ceiling
(20000 loop cap hit, no `ENOSPC` — an explicit limit, not a claim);
fd-count exactness; shm privateness proof; mount-inventory closure;
FSIZE-inheritance narrative below; cleanup independence; any variant that
would reverse a leg.

Setup findings preserved (not verdicts): cross-scope cgroup placement is
denied even to root-then-setuid flows (all placement stays in one delegated
subtree); `RLIMIT_FSIZE` breaks CoreCLR startup instantly, and an inherited
manager-side 1 MiB limit poisoned every .NET leg until removed (per-kind
limits may only lower, never raise, unprivileged); memfd offsets are shared
across `pass_fds` dups (rewind per launch); bwrap reports fatal signals as
128+signo. No candidate repair was performed or is authorized on failure;
no production code changed.

This candidate survives P04 for P07 on this host. It does not establish
general Linux sandbox security, complete profile qualification, crash/
ownership semantics (P07), other kernels/distros/LSM states, GC
determinism beyond the measured runs, or production readiness.

## Historical 2026-10-09 stop snapshot (superseded scheduling only; preserved verbatim below)

# AR-7 P04 — Resources and complete writable storage

Date: 2026-10-09. Slice: architecture-blocking feasibility probes. **Conclusion: NOT RUN.**

## Source binding and environment

- Frozen production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, `qualification/ar6-native`, `ar6-review-baseline-06c8ac7`; unchanged.
- Review/start revision: `8e156774fec28d807b557acae487791289369609`.
- Probe branch: `qualification/ar7-feasibility`. No P04 harness exists and there is **no tested P04 probe commit or helper binary/hash**.
- Last executed probe source: P01 `ee170724c14d8bd138cec1a461d86eb8bec76ec6`, clean at release. P01 evidence checkpoint: `9b95dd58d4a62f4fee258110f58d9000b7e1d220`, clean before these documentation-only records. Neither revision qualifies P04.
- Available caller host: Windows `10.0.26200.0`, x64, PowerShell `7.6.5`, MSVC `14.51.36231`, Windows SDK `10.0.26100.0`, .NET SDK `10.0.401`. This is inventory from P01, not a P04 native run.
- P04 native OS/kernel/libc, runtime, UID/token, privileges, provisioning, prerequisites and helper: **not measured**. No administrator/root setup was performed for P04.
- Scope: honest not-run record after mandatory P01 hard stop. Exclusions: every P04 guarantee and any inherited AR-6 security qualification.

## Question and mechanism

Can a surviving candidate enforce finite useful resources and bound every meaningful writable location without an unreviewed privileged service?

Planned candidate: Surviving Windows/Linux candidates only. No finite scratch-store provisioning, profile/registry storage accounting, Job memory/process limits, memory/pids/swap/tmpfs/inode/FD controls, above-budget stress or self-contained .NET workload was run. The P01 Job's kill-on-close and fixed synthetic workload bounds are not P04 limits.

## Positive control, restricted result and external oracle

None executed. There is no positive control or restricted result for P04; no absent endpoint, prior lifecycle test, successful setup or outer restriction is counted as denial. [P01](ar7-P01-windows-lpac.md) records the actual counterexample and external oracle. Do not relabel it as a passed P04.

## Cleanup

No P04 workloads, listeners, profiles, mounts, cgroups, services or workspaces were created; there is no P04 residue to remove. P01 cleanup was independently verified before this stop record. This is not a P04 cleanup test.

## Stop reason and architecture consequence

The user specification section 24 requires immediate stop when a Windows/Linux candidate contradicts a central Astra assumption. P01's ungranted global ARAP endpoint created a host-authority child outside the candidate Job, contradicting the tested LPAC + Job closure/IPC composition. No architecture patch or additional platform experiment followed. The normal sequential backlog does not override that explicit hard stop.

All resource/storage cells remain Pending. Windows writable profile/registry state and Linux task-versus-process accounting need future evidence. The proposed 128 MiB budget has not been measured or raised.

## Luna review and non-claims

Luna reviewed the completed P01 run read-only and confirmed the counterexample, positive controls, cleanup and narrow interpretation. There is no executed P04 to review; this not-run disposition is not independent native qualification. The consolidated hold/status records receive a final read-only documentation challenge. No final architecture selection, production/public API/release change, Hufu dependency, privileged persistent helper, publication or merge occurred. See [consolidated disposition](restricted-execution-AR-7-feasibility.md).
