# AR-7 Separate-Gate R2 Independent Review


**Date:** 2026-10-10
**Review type:** Independent read-only R2 evidence review
**Reviewed range:** `e34016e..af88f82`
**Execution by reviewer:** None


**Final disposition:** `AR-7 EVIDENCE NOT READY FOR ARCHITECTURE DECISION`


---

The separate gate fixes the original initialization-order defect, but the final R2 pair does not close qualification. The new launch path omits the frozen seccomp filter. P07 remains RERUN REQUIRED, limited to the R2 positive/deadline pair.
I reviewed e34016e..af88f82, including source at 5315211, the previous independent reviews, and retained artifacts. I performed no probes, builds, tests, cleanup, or modifications.
Final R2 assessment
The blocking defect is in [spawn_sgate(), line 1222 (line 1222)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:1222):
argv = ctx.common + ['--ro-bind', ..., '/gate', '--', '/gate', ...]


Unlike the existing launch paths, this contains no --seccomp, and ctx.common does not supply it. Passing ctx.seccomp_fd through pass_fds preserves a descriptor; it does not install the filter. The recorded filter_sha256 establishes which bytes the manager constructed, not their application to /gate or /target.
Consequently, the positive demonstrates useful work under a changed composition. It cannot establish compatibility with the frozen candidate.
The structural phase repair itself is sound:
- Bubblewrap directly launches /gate.
- [p07gate.c (line 14)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07gate.c:14) is a separately compiled executable. It contains none of the target’s identity/input initialization and never opens /input.
- It has no pre-authorization fork, spawn, or target-exec path.
- Its sole target launch is execv("/target", ...), guarded by comparison with the expected nonce string.
- The manager’s sole nonce write requires both positive mode and decision A. The deadline case cannot take that branch.
Thus, for the inspected gate source and controlled channel, target initialization is structurally prevented while authorization is withheld. Missing output is only corroboration.
The gate is test coordination and workload preparation: it creates the observable release boundary, but adds no confinement, cgroup authority, or lifecycle enforcement. That distinction does not excuse the omitted existing filter.
Source binding and chronology
I independently recomputed all 14 recorded Python/C source hashes across seven new records; they match their claimed Git blobs. All seven committed JSON records are byte-identical to their retained artifact originals.
Both final records identify 5315211. On October 10, Manila time:
- Source committed: 20:36:21
- Positive started: 20:36:50
- Deadline started: 20:37:11
- Evidence committed as af88f82: 20:46:49
Every retained, dated attempt follows its claimed source commit. I inspected the requested revision chain, including the additional f14e6f5 change. The unavailable 39d1 attempt cannot independently establish execution chronology; its absence is honestly documented.
One binding limitation remains: [the runtime source-hash collection (line 2138)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:2138) uses glob('p07.*'), which excludes p07gate.c. Both final records contain the same compiled-gate hash, successful compilation, and source path, but no runtime gate-source hash. The current gate source matches the committed blob; that does not independently prove its historical compilation input. I did not rebuild the removed binaries.
The superseded attempts remain preserved: missing gate compilation, marker-key mismatches, and the watchdog failure are not promoted to successful phase proofs. Old af61…, the in-worker pair, and all previous cleanup entries are unchanged. The failed in-worker phase proof remains documented in the prior review and the new explanatory section, although stale acceptance wording survives earlier in P07.
Positive and deadline observations
The [positive record](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/sgate-r2pos-169dd00ba2714e97b92c7d44406fa64d.json) supports:
/gate held → external snapshot → A → nonce sent → AUTH_RECEIVED bytes=32 → GATE_AUTH_OK → target identity/input initialization → spinwork_sum 522240 → complete → rc 0
Before authorization, the snapshot contains the manager, two Bubblewrap processes, and /gate PID 1154041, starttime 152643316; no actual /target process appears. This is a valid authorization/initialization/useful-work control, subject to the composition and source-binding limitations above.
The [deadline record](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/sgate-r2dead-d1d52ed3047e487a971b27849ffde81a.json) preserves:
- Manager 1154139; Bubblewrap 1154141/1154142.
- Sleeping /gate 1154143, starttime 152645438, exact target-leaf membership and full gate command line.
- Namespace PID 2, corroborated by the committed NSpid resolver.
- Three target-leaf members and no actual /target process.
The runner issues target/cgroup.kill before sending decision K. The manager cannot authorize in deadline mode. This source ordering supports “authorization never sent” and “target never execs” without relying on absent markers.
The target domain becomes empty in approximately 0.000394 seconds; the manager survives the stop, no fixture-leaf strays remain, Bubblewrap returns −9, and the manager returns 0. This is an injected deadline decision at a held boundary, not qualification of a production deadline implementation.
Candidate integrity, ownership, and cleanup
Changes are confined to P07 instrumentation and evidence/documentation. No production src/ or eng/ changes, guardian, privileged service, VM, Hufu integration, or production launcher were introduced. No P02/P03/P04 or accepted R1/R3/R4/R5/R6 reruns appear in the retained new records. P05/P06/P08/P09 remain untouched.
Namespace flags, capability dropping, cgroup topology, limits, and ownership placement remain unchanged. Seccomp application does not.
Both final runs retain outer-domain populated 0, manager termination, and workspace/cgroup removal. The separate [cleanup record](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/independent-cleanup.json) confirms workspace/cgroup absence and host-shm cleanliness. These support removal of the contained gate/helpers/target and fixture resources; no surviving or unknown fixture-owned process is reported for either final run.
The superseded a7d8… record explicitly retains cleanup_confirmed:false and workspace:false. Its later filesystem cleanup is not independently established by the appended cleanup entries. Preserve that uncertainty separately from the final pair’s successful cleanup. Cleanup never recovers an Unknown execution result.
Previous findings
Finding	Final disposition	Evidence	Remaining qualification
M1	Partially resolved	Accepted R1/R3/R4/R5/R6 retained; separate-gate ordering demonstrated	R2 must run with the frozen seccomp composition
M2	Corrected claim retained	P03 placement exception unchanged	No P03 target-leaf lifecycle claim
M3	Corrected claim retained	P04 post-collection truncation qualification unchanged	No streaming-output enforcement
M4	Corrected claim retained	P03 coverage/oracle limitations retained	Implemented paths and tested endpoints only
M5	Corrections retained	Existing measurement and closure qualifications	Accounting, startup-budget, closure and core-sink limits remain
M6	Original phase-placement defect corrected; final closure pending	Separate executable and authorization-controlled exec path	Candidate mismatch and incomplete runtime gate-source binding prevent final R2 acceptance
M7	Prior resolution retained	Old provenance/cleanup records unchanged; final pair cleaned	Unknown results stay Unknown; R5 chronology qualification and new superseded-attempt cleanup uncertainty remain explicit
m1	Replacement-specific reconciliation required	Seccomp claims, gate-hash wording and historical acceptance prose conflict with evidence	Correct summaries without reopening accepted probe results




Probe dispositions
Probe	Final disposition
P02	VERDICT SUPPORTED
P03	VERDICT SUPPORTED WITH QUALIFICATION
P04	VERDICT SUPPORTED WITH QUALIFICATION
P07	RERUN REQUIRED




Remaining corrections
- Docs-only: Withdraw “seccomp unchanged” for these runs; distinguish compiled-gate hashes from runtime source hashes; explicitly mark the old in-worker acceptance claims as superseded; preserve a7d8… cleanup uncertainty and 39d1 unavailability.
- Interpretation: Retain the separate-gate ordering, authorization, useful-work and owned-stop observations under their actual composition. AUTH_RECEIVED occurs after line collection, so its absence is not independent proof that no partial bytes arrived.
- Genuine rerun requirement: Restore the existing frozen seccomp installation on the separate-gate launch path and repeat only the R2 positive/deadline pair. Bind the gate source alongside the Python/worker sources in those records. No broader experiment programme is required.
Aggregate non-claims
Architecture review must retain:
- No general Linux security, production-readiness, or production restricted-execution qualification.
- Tested WSL2, pinned-helper, UID/delegation and operation scope; LSM state unknown.
- No independent inode/metadata bound, complete syscall/IPC coverage, or intra-execution loopback prohibition.
- No bounded streaming-output enforcement, complete per-case memory attribution, measured swap usage, measured minimum .NET budget, or exhaustive host core-sink exclusion.
- Incomplete published-.NET closure qualification; P03 placement exception and documented P04 configuration differences.
- No general descendant-elimination guarantee from C7/C8; no recovery of Unknown results; existing R5 chronology and R6 PID-reuse qualifications.
- No Windows-native impossibility claim, Windows VM qualification, macOS restricted-execution qualification, or P05/P06/P08/P09 completion.
- No production pre-initialization release guarantee from this test gate.
The minimum blocking execution work is the same narrow R2 pair under the restored frozen filter, with complete source binding and corrected evidence descriptions.
AR-7 EVIDENCE NOT READY FOR ARCHITECTURE DECISION
