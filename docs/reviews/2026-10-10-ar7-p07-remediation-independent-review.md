# AR-7 P07 Remediation Independent Re-Review


**Date:** 2026-10-10
**Review type:** Independent read-only remediation audit
**Reviewed range:** `f71ee8a..804e1c1`
**Execution by reviewer:** None


**Final disposition:** `AR-7 EVIDENCE NOT READY FOR ARCHITECTURE DECISION`


---

The remediation closes most P07 evidence gaps, but does not close all original blockers. R2 still fails to establish that the deadline occurred before target initialization. Several documentation corrections also remain incomplete.
I reviewed f71ee8a..804e1c15c0752ace5282da3ebcebe22ac16d000f read-only. No probes, builds, tests, cleanup, or modifications were performed.
Original finding dispositions
Finding	Disposition	Evidence	Remaining qualification
M1 — P07 fault-matrix mismatch	PARTIALLY RESOLVED	R1a/R1b now kill the outer provisioner; R3/R4 demonstrate live stops; R5 establishes execution before observer loss; R6 exercises identity guards.	R2 does not prove the required pre-initialization phase.
M2 — P03 placement overclaim	SUPERSEDED BY CORRECTED CLAIM	[P03 explicitly withdraws target-leaf placement and emptiness claims (line 24)](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-P03-transports-ipc.md:24), retaining outer-domain observations.	Blanket “same composition/topology” language elsewhere needs the P03 exception.
M3 — P04 output-bound overclaim	SUPERSEDED BY CORRECTED CLAIM	[P04 now describes post-collection evidence-prefix truncation (line 70)](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-P04-resource-storage.md:70).	No bounded streaming collection or overflow behavior was demonstrated.
M4 — P03 coverage and attribution	SUPERSEDED BY CORRECTED CLAIM	P03 identifies missing variants, category-level gates, snapshot limitations, negative CQE interpretation, and raw/ping/loopback qualifications.	Support remains limited to implemented attempts against tested host endpoints.
M5 — P04 measurement overclaims	PARTIALLY RESOLVED	Cumulative counters, inherited FD limits, native-only mount inventory, incomplete .NET binding, and startup observation are acknowledged.	Current summaries still call startup a measured minimum; exhaustive core-sink absence remains overstated.
M6 — Identity, ordering, observation	PARTIALLY RESOLVED	Fresh identity guards and corrected C6/C10 runs are supported; continuous-timeline language is withdrawn.	Pre-Bubblewrap commit, target execution, and post-initialization GO remain conflated in places. R2 ordering remains unproven.
M7 — Cleanup and provenance	RESOLVED	Fixture-path disposition, expanded cleanup records, preserved superseded artifacts, corrected teardown, and R5 source binding address the original defects.	Cleanup confirmation does not recover execution results. R5 chronology has the documentary limitation described below.
m1 — Aggregate reconciliation	PARTIALLY RESOLVED	Baseline/tag, historical NOT-RUN scope, qualified P03/P04 table entries, inode limitation, errno, timings, and Unknown-result wording were corrected.	Stale source, measured-minimum, and all-legs-supported statements remain.




P07 disposition: RERUN REQUIRED
Only the pre-target setup-deadline transition requires a further targeted run.
Remediation leg	Independent assessment
R1a	Supported. The outer provisioner dies before manager spawn. Its incremental manifest survives, listed resources exist, domain population is empty, and removal is recorded. This covers the injected provisioning checkpoint.
R1b	Supported. The outer caller dies after a validated target execution marker; manager and target remain owned in their leaves until independent outer-domain cleanup.
R2	Insufficient phase proof. It demonstrates an early helper kill, drained pipes, and an empty domain, but not that target initialization never began.
R3	Supported. A descendant retains its PID/starttime across observations and is freshly checked alive immediately before target/cgroup.kill; the manager survives and no strays remain.
R4	Supported. Eight streamed spawn markers are followed by actual target-leaf growth from 11 to 14 members. Output reaches spawned 11, below the configured 30, before domain termination.
R5	Supported for the narrow observer-loss claim. The manager persists RELEASED-EXECUTING only after reading the target’s RELEASED marker. Observer death leaves execution results Unknown; later cleanup is separately established.
R6	Supported for fresh identity validation. Absent and mismatched identities are refused; matching SIG0 and guarded C6/C10 actions are recorded. This does not establish atomic immunity to every PID-reuse race.




The decisive R2 defect is in [the deadline implementation (line 803)](C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:803). It stops the outer Popen child, scans processes, then kills that child. The retained external snapshot does show the outer Bubblewrap process in state T, which corroborates its stop. However:
- Stopping that process does not establish that every already-created helper or target process is held.
- The scan result is reduced to a Boolean; it does not preserve a complete pre-decision process-state proof.
- A target can begin execution after the scan and be killed before its first output.
- target_bytes_in_pipe checks for READY or identity, rather than measuring zero output bytes.
- Withholding GO cannot establish no initialization: [the worker initializes and reads input before GO (line 47)](C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.c:47).
Thus [the `af61…` record](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/af61e28e18204ac198bacfd3a78bcbfb.json) supports cleanup after an early fault, but its “target never executed” conclusion exceeds the instrumentation. This is the same required transition left open by the original review, not a request for broader coverage.
The retained manager-death, helper-failure, cascade, ownership, and root-versus-domain observations remain useful. No new evidence demonstrates released-but-unowned execution or survival after the exercised domain stops. That absence does not close R2.
Source, chronology, and historical integrity
I independently recomputed all available P07 source-hash bindings: 28 hashes across 14 records matched their claimed Git blobs. All 16 retained raw files with corresponding ignored artifact originals were byte-identical to those originals.
Source commit	Chronology evidence
896613c — 07:11:14 UTC	af61… starts 07:11:22; guarded 8b69… starts 07:11:48.
1cceab4 — 07:14:32 UTC	Superseded 7aef… starts 07:14:37.
f9d19c5 — 07:15:47 UTC	Final R3, R4, R1a, and R1b start between 07:15:52 and 07:16:24.
c628f3c — 07:19:30 UTC	R5’s exact Python/C hashes match. Its wrapper record lacks UTC start/release time; the original file’s 07:19:37 modification time corroborates chronology but is not an independent release timestamp.
804e1c1 — 07:25:33 UTC	Documentation/evidence checkpoint following the source revisions and dated runs.




Accordingly, source-before-execution chronology is supported for every dated remediation run. R5’s exact source binding is verified, but its committed wrapper artifact alone cannot independently establish wall-clock commit-before-release ordering. Preserve that qualification; it does not require repeating an otherwise demonstrated transition.
The original review and previously committed raw run artifacts were unchanged. independent-cleanup.json was extended without changing its old entries. The 7aef… record is preserved with cleanup_confirmed:false and resource-absence failures, explicitly superseded by the clean 3474… run. The /tmp/mnt-p07 record now states ownership, empty-directory verification, removal, and subsequent absence.
Candidate integrity and previous probe dispositions
The remediation changes only p07.py, p07.c, and evidence/documentation. No production src/ or eng/ changes, new confinement mechanism, guardian, privileged service, VM, Hufu integration, or production provider were introduced. P05/P06/P08/P09 remain untouched.
The P07 helper pin, namespace/capability/seccomp composition, mounts, placement path, and resource limits remain unchanged. Workload changes add bounded holdchild and trickle modes and fixture-directory removal. Harness changes add fault coordination, identity checks, observation, provenance, and teardown. The surviving root runner exercises existing cgroup authority; its role remains test observer/enforcer.
P03’s historical manager-leaf placement remains an explicit exception to blanket claims of identical topology.
Previous independent dispositions remain:
- P02: VERDICT SUPPORTED
- P03: VERDICT SUPPORTED WITH QUALIFICATION
- P04: VERDICT SUPPORTED WITH QUALIFICATION
No P02/P03/P04 rerun is evidenced by this remediation, and none is required by these findings.
Remaining corrections
- Docs-only: Remove the measured-minimum claims in [the aggregate (line 60)](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/restricted-execution-AR-7-feasibility.md:60) and [work queue (line 7)](C:/Users/Laszlos/source/repos/Gagamba/docs/work-queue.md:7); scope the stale “Last executed source SHA”; reconcile [P07’s all-supported conclusion (line 75)](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-P07-crash-ownership.md:75) with its superseded-leg classification. Preserve the P03 placement exception and explicitly exclude exhaustive host core-sink qualification.
- Interpretation: Describe the cancellation check as preceding Bubblewrap execution, target initialization as preceding GO, and recorded release timestamps according to their actual collection point. Retain deterministic pre-spawn refusal and five observed lost races without claiming a demonstrated setup-cancellation winner. First-seen snapshots are not continuous timelines or uniformly starttime-bearing records.
- Genuine rerun requirement: Replace only R2 with a phase-proven pre-target setup-deadline case. The evidence must establish a barrier that prevents target initialization before the deadline decision, followed by ownership and cleanup observations. Preserve the present R2 artifact as an early-fault observation.
The resulting qualifications must continue to exclude general Linux security, production readiness or restricted execution, independent inode/metadata bounds, complete syscall/IPC coverage, streaming output enforcement, a measured minimum .NET budget, exhaustive core-sink coverage, Windows-native impossibility, Windows VM qualification, macOS restricted execution, and completion of P05/P06/P08/P09.
Minimum blocking work: close R2’s required phase-proof gap and reconcile the remaining evidence claims. No broader probe campaign or candidate hardening is required by this review.
AR-7 EVIDENCE NOT READY FOR ARCHITECTURE DECISION
