# AR-7 Final Independent Evidence Review


**Date:** 2026-10-10
**Review type:** Final independent read-only AR-7 evidence review
**Reviewed range:** `8cb9500..95199bf`
**Execution by reviewer:** None


**Final disposition:** `AR-7 EVIDENCE READY FOR ARCHITECTURE DECISION`


---

The frozen-composition R2 closes the final execution blocker. Both prior defects are RESOLVED: the final pair installs the frozen seccomp filter, and runtime source binding now includes p07gate.c. P07 is supported within its declared scope.
I reviewed 8cb9500..95199bf, including committed source at edd7894 and retained artifacts. I performed no probes, builds, tests, cleanup, or modifications.
Seccomp/composition assessment
The evidence establishes installation, rather than merely construction or descriptor inheritance:
- [The committed launch path (line 1222)](C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:1222) supplies --seccomp with ctx.secc(). Both final records retain --seccomp 3.
- The filter-generation function is identical to the previously accepted P07 source. Both records report the same previously recorded filter hash: 30bfaf6b62030de8b105e1ac8de43f2a9884b2bd5de20e98aef81dce426c1aad.
- The runner independently reads /proc/<pid>/status before its decision:
Run	Held gate PID	Starttime	Seccomp	Filters
Positive ca9f1aea…	1162557	152901235	2	1
Deadline ab19f84e…	1162654	152903224	2	1




The positive subsequently records gate seccomp=2 filters=1, followed by target exec and target seccomp=2 filters=1. The target observation is a source-bound self-report; the held-gate observations are external. Together with the unchanged filter construction and explicit installation path, these establish execution under the frozen filter and its retention across target exec. No weakening or replacement appears in the changes.
Composition defect: RESOLVED.
Changes remain confined to P07 instrumentation, source binding, and evidence/documentation. Namespace flags, capability dropping, cgroup topology, ownership, and limits are unchanged. No production src/ or eng/ changes, guardian, privileged service, VM, Hufu integration, production launcher, or deferred probe was introduced. The retained new executions are R2 attempts only; accepted P02/P03/P04 and P07 R1/R3/R4/R5/R6 were not rerun.
The separate gate remains test coordination. It adds no confinement or lifecycle authority.
Source-binding assessment
Both final records explicitly bind all three relevant files. I independently recomputed their hashes against edd7894; all six comparisons match:
Source	Matching SHA-256 prefix
p07.py	6220365d52a6faa7…
p07.c	7f3167f3251846a7…
p07gate.c	f5aa4302d7fd8838…




Both records also contain successful compilation and the same compiled-gate hash, a0caa3100603d8ed…. That corroborates binary consistency; it is distinct from the verified source hashes.
Chronology is supported on October 10, Manila time:
Event	Time
edd7894 committed	21:19:42
Final positive started	21:19:49
Final deadline started	21:20:09
Evidence 95199bf committed	21:24:56




Both final JSON files are byte-identical to their retained artifact originals. No code changed between edd7894 and 95199bf.
The committed superseded 58b91f96… record remains inconclusive. Two additional failed positives, bfac805b… and 5e536d28…, remain in ignored artifacts; their recorded source hashes match, both report cleanup confirmed, and neither supplies the final verdict. Previously committed raw records and existing cleanup entries remain unchanged.
Source-binding defect: RESOLVED.
R2 assessment
The [final positive](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/sgate2-r2pos-ca9f1aea39bd43708504dccd1b80cf8a.json) establishes:
filtered /gate held → external snapshot → authorization → gate accepts nonce → target exec → target initialization → useful work → rc 0
The pre-decision snapshot contains the manager, two Bubblewrap processes, and /gate, with no actual /target process. After authorization, the retained output shows:
AUTH_RECEIVED bytes=32
gate seccomp=2 filters=1
GATE_AUTH_OK exec_target
identity ...
input_read rc=32 errno=0
spinwork_sum 522240
target seccomp=2 filters=1
...
complete


The candidate exits 0, and its domain becomes empty. Useful bounded target execution therefore succeeds under the restored frozen filter.
The [final deadline](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/sgate2-r2dead-ab19f84ebc96464d9ecd3971cb70c400.json) establishes the same held, filtered gate before the decision, including PID, starttime, command line, target-cgroup membership, and namespace-PID resolution. No actual /target exists.
The causal proof remains structural:
- /gate shares none of the target’s initialization and does not access /input.
- Its sole target-exec path follows the nonce comparison.
- The manager sends the nonce only for positive mode and decision A.
- The deadline runner issues target/cgroup.kill before sending K; this branch never authorizes target execution.
Thus target initialization cannot begin before authorization, and authorization is never sent in this deadline run. Missing output merely corroborates that chain.
The target domain empties in approximately 0.000295 seconds. The manager survives the stop, the stray list is empty, Bubblewrap returns −9, and the manager returns 0. Existing cgroup authority retains lifecycle ownership throughout.
Both final runs record outer-domain populated 0, removal of the manager, workspace, and cgroups. The separate [cleanup record](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/independent-cleanup.json) confirms workspace/cgroup absence and host-shm cleanliness. No surviving fixture workload is reported.
The deadline’s gate_seccomp_ok:false and target_seccomp_ok:false reflect absent post-authorization self-report strings. They do not contradict the external gate observation; the target never executes.
This closes the R2-dependent remaining portions of M1/M6 without reopening accepted findings.
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
P07	VERDICT SUPPORTED




Remaining corrections
- Docs-only, nonblocking: Current P07 explicitly reclassifies the no-seccomp pair and withdraws the in-worker phase claim, but nearby historical paragraphs still contain contradictory “unchanged seccomp,” acceptance, and echo-attestation wording. Mark those paragraphs consistently as superseded. Carry the preserved a7d8… cleanup uncertainty into the current summary, and index the two additional failed positives retained in artifacts. Update pending-review status when preserving this review.
- Interpretation: 39d1 remains unavailable; a7d8… remains cleanup-uncertain in its unchanged record and prior review. Neither is rehabilitated by the final pair. The latest text correctly treats absent AUTH_RECEIVED as corroboration only. The earlier missing self-report anomaly remains undiagnosed, but does not undermine the final external seccomp observations.
- Genuine rerun requirement: None. No additional execution is needed for this narrow review.
Aggregate non-claims
Architecture review must retain:
- No general Linux-security, production-readiness, or production restricted-execution claim.
- Tested WSL2, pinned-helper, UID/delegation and operation scope; LSM state unknown.
- No independent inode/metadata bound, complete syscall/IPC coverage, or intra-execution loopback prohibition.
- No bounded streaming-output enforcement, measured minimum .NET budget, or exhaustive host core-sink exclusion; published-.NET closure qualification remains incomplete.
- P03 placement exception; existing resource-accounting qualifications.
- No general descendant-elimination guarantee from C7/C8, recovery of Unknown results, or removal of the R5 chronology and R6 PID-reuse qualifications.
- No Windows-native impossibility, Windows VM qualification, macOS restricted-execution qualification, or P05/P06/P08/P09 completion.
- No claim that this test gate is a production pre-initialization release mechanism.
The evidence is sufficient for architecture decision within those boundaries. The remaining editorial corrections do not require another probe or block that decision.
AR-7 EVIDENCE READY FOR ARCHITECTURE DECISION
