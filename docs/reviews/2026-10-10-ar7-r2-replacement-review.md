# AR-7 R2 Replacement Independent Review


**Date:** 2026-10-10
**Review type:** Independent read-only remediation audit
**Reviewed range:** `9b7d6eb..a554d8e`
**Execution by reviewer:** None


**Final disposition:** `AR-7 EVIDENCE NOT READY FOR ARCHITECTURE DECISION`


---

Replacement R2 does not close the final execution blocker. The committed gate prevents a second exec into spinwork, but it runs after the target binary has already initialized, printed identity, and opened/read its input. P07 remains RERUN REQUIRED, limited to R2.I reviewed the committed source and evidence through a554d8ed2650e339ec9aa16f57281fec6e07fe63. I performed no probes, builds, tests, cleanup operations, or modifications.
Replacement R2 assessment
The decisive defect is established by source, rather than inferred from missing output:
1. Bubblewrap launches -- /target gate …; it does not launch a separate gate executable. [Launch code (line 1123)](C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:1123)
2. Before dispatching on mode, /target executes its common initialization: stdout setup, alarm, argument processing, identity output, and opening/reading /input. [Initialization (line 50)](C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.c:50)
3. Only afterward does the gate branch print PRETARGET_GATE_REACHED and wait for authorization. Valid authorization permits execv("/target", …) with mode spinwork. [Gate (line 169)](C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.c:169)
Thus the demonstrated sequence is:
target exec → common initialization/input access → gate → authorization → second target exec → spinwork
The required sequence places the gate before the first target initialization. Calling the initial invocation “gate” does not undo its execution of the same initialization identified by the previous review.
The retained announced_identity also comes from that pre-gate initialization. spawn_gate() consumes the initial stdout into a local buffer, retains parsed identity, and subsequently records only the post-gate output tail. An empty deadline stdout_tail therefore does not mean initialization produced no output.
Source binding and chronology: supported.
Checkpoint	Recorded time, October 10, Manila
bd96d5f committed	17:41:06
First positive 1b93… started	17:41:13
bca5690 committed	17:41:54
Final positive 7878… started	17:41:59
Deadline b6e3… started	17:42:25
Evidence/docs a554d8e committed	17:43:52




I recomputed all six source-hash bindings across the three replacement records; each matches its claimed Git blobs. Their recorded source_sha values, populated from runtime --source-sha, agree. Both final records are byte-identical to their retained artifact originals. The superseded positive is JSON-equivalent to its original. Old R2 af61… is unchanged.
a554d8e changes evidence/documentation only. The records contain matching final worker hashes; I did not rebuild the removed binaries.
The first positive is preserved correctly as unsuccessful. Its precise failure was stdin EOF, go_read rc=0, exit 126, with timeout:false; the report’s “deadlocked” description is inaccurate.
Candidate integrity: preserved, with a documentation discrepancy.
Since 9b7d6eb, changes are confined to p07.c, p07.py, and evidence/documentation. No P02/P03/P04 or accepted R1/R3/R4/R5/R6 reruns appear in the retained record. Prior raw artifacts remain unchanged; existing independent-cleanup entries are preserved. Production src/, eng/, confinement policy, ownership topology, and deferred P05/P06/P08/P09 remain untouched. No guardian, privileged service, VM, Hufu integration, or production launcher was added.
The expected extra gate --ro-bind does not exist. COMMON is unchanged and binds the worker at /target and input at /input. The gate is a new mode in that worker. It adds test coordination, not confinement or stop authority, but its placement is too late for R2’s required phase. [Mount/launch configuration (line 86)](C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:86)
Positive control: valid for authorization followed by re-exec and useful work.
The [final positive](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/r2-r2pos-787891a6a2a643e591a1a62c2705b8ee.json) establishes:
- External gate identity: PID 1132072, starttime 151959374, target-leaf membership, and namespace PID 2 resolution.
- Authorization only after the held-gate snapshot.
- AUTH_RECEIVED bytes=32 → GATE_AUTH_OK → re-exec identity/input output → spinwork_sum 522240 → complete → candidate rc 0.
- Domain emptiness and cleanup.
The checksum matches the bounded page-touch workload. Release genuinely enables execution, but the control does not establish that all target initialization follows authorization.
Deadline, authorization, and ownership: supported for the narrower held-worker transition.
The [deadline record](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/r2-r2dead-b6e3f259db0c4f9e9d69eac3aab7c399.json) preserves:
- Manager PID 1132181.
- Bubblewrap PIDs 1132183 and 1132184.
- Gate PID 1132185, starttime 151962007, sleeping state, /target gate … cmdline containing the full nonce, and exact target cgroup membership.
- Namespace PID 2 identity, corroborated by the committed resolver.
- Three target-leaf members and no additional target process in the snapshot.
The manager blocks awaiting the runner’s decision. The deadline branch kills the target domain before sending K; the manager’s authorization branch requires both positive mode and A. This structurally supports no nonce authorization and no second exec. Missing AUTH_RECEIVED/GATE_AUTH_OK corroborates that conclusion but is not its sole basis.
The report overstates the echo: AUTH_RECEIVED is printed after line collection, not upon every received byte. Its absence alone cannot prove that no partial bytes arrived.
The existing target/cgroup.kill empties the domain in approximately 0.000537 seconds. The manager survives, the fixture-leaf stray list is empty, Bubblewrap returns -9, and the manager returns 0. This demonstrates an injected stop after the held-state proof, not a measured production deadline implementation. The worker was already initialized at that point.
Identity and cleanup: retained within their established scope.
R6’s starttime/domain/role checks and absent/mismatched identity refusals are unchanged. Its prior qualification remains: these checks do not establish atomic immunity to every PID-reuse race.
Both final runs record outer populated 0, removed cgroups/workspaces, and manager termination. The separate [cleanup record](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/independent-cleanup.json) confirms workspace/cgroup absence and host-shm cleanliness. Domain emptiness covers the contained helpers, gate, target, and descendants; the deadline additionally records no remaining fixture-leaf processes. No surviving or unknown fixture-owned process is reported. These are retained cleanup observations, not a new live host sweep. Cleanup does not repair the phase defect or recover any previously Unknown execution result.
Previous finding dispositions
Finding	Final disposition	Evidence	Remaining qualification
M1	Partially resolved	Accepted R1/R3/R4/R5/R6 retained	R2 still follows target initialization
M2	Corrected claim retained	P03 explicitly records manager-leaf placement	No target-leaf lifecycle claim
M3	Corrected claim retained	P04 describes post-collection truncation	No streaming-output enforcement
M4	Corrected claim retained	P03 coverage/oracle limitations retained	Implemented paths and tested endpoints only
M5	Documentation/interpretation corrections completed	9b7d6eb; current P04 and aggregate qualifications	Accounting, closure-binding, startup-budget and core-sink limits remain
M6	Partially resolved	R6 preserved; snapshots and original GO terminology corrected	Replacement R2 again places the claimed boundary after initialization
M7	Resolved within accepted scope	Prior cleanup/provenance repairs preserved; replacement cleanup retained	Unknown results stay Unknown; R5 chronology qualification survives
m1	Prior reconciliation retained; replacement-specific corrections needed	New source/raw evidence contradicts gate-bind and phase descriptions	Correct the new summaries without reopening accepted probe results




Probe dispositions
Probe	Final disposition
P02	VERDICT SUPPORTED
P03	VERDICT SUPPORTED WITH QUALIFICATION
P04	VERDICT SUPPORTED WITH QUALIFICATION
P07	RERUN REQUIRED




Remaining corrections
- Docs-only: Correct the nonexistent separate gate bind, the first positive’s EOF failure, stale replacement-source summaries, and claims that replacement R2 closed the blocker.
- Interpretation: Preserve these runs as authorization-controlled re-exec/useful-work and held-worker domain-stop observations. Withdraw “target never initialized” and absence-of-echo-as-independent-proof claims.
- Genuine rerun requirement: Only R2 remains blocking. Its positive/deadline pair must demonstrate a boundary before target initialization, including the common identity/input preamble. No broader experiments or reruns of accepted legs are required.
Aggregate non-claims
The architecture review must retain:
- No general Linux security, production readiness, or production restricted-execution qualification.
- Scope limited to the tested WSL2 host, pinned helper, UID/delegation arrangement, and operations; LSM state remains unknown.
- No independent inode/metadata bound, complete syscall/IPC coverage, or prohibition of intra-execution loopback.
- No bounded streaming-output enforcement, complete per-case memory attribution, measured swap usage, measured minimum .NET budget, exhaustive core-sink coverage, or complete published-.NET closure binding/mount qualification.
- P03’s placement exception; P04’s documented configuration differences; no blanket identical-composition claim.
- No general descendant-elimination guarantee from C7/C8; no recovery of C11/R5 Unknown results; R5’s missing independent release timestamp remains qualified.
- No Windows-native impossibility, Windows VM qualification, macOS restricted execution, or P05/P06/P08/P09 completion.
- No production pre-initialization release guarantee from this replacement R2.
The minimum blocking work remains the same narrow R2 transition: prove stop wins before target initialization, with a working positive control and preserved ownership/cleanup evidence.
AR-7 EVIDENCE NOT READY FOR ARCHITECTURE DECISION
