# AR-7 Independent Evidence Review


**Date:** 2026-10-10  
**Review type:** Independent read-only evidence audit  
**Reviewed branch:** `qualification/ar7-feasibility`  
**Reviewed tip:** `5a79effc6d4aaf29990cbd989a685c96a245c266`  
**Execution:** No probes, builds, tests, cleanup operations, or repository modifications were performed by the reviewer.


**Final disposition:** `AR-7 EVIDENCE NOT READY FOR ARCHITECTURE DECISION`


---

1. Overall assessment
The committed evidence contains useful, reproducible observations, but the chain is not internally coherent enough to accept all four recorded verdicts unchanged. P02 stands. P03 and P04 support narrower conclusions with qualifications. P07 does not yet substantiate its claimed completed crash/setup/ownership matrix.
This is an evidence assessment, not a rejection of the Linux candidate or an architecture decision.
Read-only verification established:
- Clean qualification/ar7-feasibility checkout at 5a79effc6d4aaf29990cbd989a685c96a245c266.
- Both qualification/ar6-native and the peeled ar6-review-baseline-06c8ac7 tag resolve to 06c8ac7eff6b968804a5cdd6c651f1a0c6845d56.
- No changes from that baseline under production src, eng, .github, or the inspected build/package metadata.
- All 74 recorded Linux harness-source hash comparisons match their claimed Git commits. Corresponding commit timestamps precede the run records. This corroborates source binding; historical clean-at-release statements remain recorded assertions.
- P02/P03/P04 harness files remain unchanged after their respective verdict-run revisions. The b8861bf → 826438d P07 change affects the C11 wrapper, leaving the suite path unchanged.
I ran no probes, builds, tests, or cleanup operations and modified nothing.
2. Findings by severity
Critical
None established by this review. The discrepancies below do not demonstrate a sandbox escape.
Major
M1 — P07’s “18/18” result does not represent execution of the advertised fault matrix.
At b8861bf:
- C1 returns normally without killing a caller. Its explicitly vacuous result cannot qualify caller-death behavior.
- C2 kills the manager after creating two files. It does not kill the outer provisioning caller or exercise recovery from an independently persisted, incrementally populated manifest.
- C9 executes /target hang, which already runs target initialization and reads input, then kills its Bubblewrap parent after four seconds. This is a post-exec timeout/cascade observation, not a deadline firing during helper setup.
- bat_daemon observes natural root exit and subsequent emptiness; it does not issue the claimed domain kill against a live reparented descendant.
- bat_fork creates and reaps 40 children normally. The designed mid-spawn kill is absent.
C4 does provide useful evidence: the target domain remained populated after manager death and the external observer emptied it using cgroup.kill. That does not fill the other missing transitions.
References: [P07 driver: C1/C2 (line 365)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:365), [C9 (line 501)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:501), [battery (line 605)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:605), [worker (line 92)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.c:92), [committed P07 observations (line 146)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/70cf6e33690c4bd1b24ebec4824d2b08.json:146).
M2 — P03 did not retain P02’s target-leaf placement.
enter_target() is defined but never passed to the launch function; both launches use preexec_fn=bounds. External samples place Bubblewrap, target PID 1038623, and its child in delegated/manager, not delegated/target.
Consequently, the reported empty target leaf is vacuous for this run. The outer cgroup still contains the fixture and supports final cleanup. Network/mount isolation observations remain useful, but “byte-identical cgroup composition” and target-leaf lifecycle corroboration are incorrect.
References, executed revision 229de9e: [launch wiring (line 278)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p03.py:278), [unused placement function (line 314)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p03.py:314), [external target membership (line 93)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P03/70edf2c2242e4aea905e61854775b4a9.json:93).
M3 — P04 did not enforce the claimed output capture caps.
launch() calls communicate() to collect the entire output, then truncates the retained strings to 32,768 stdout characters and 8,192 stderr characters. These are evidence-prefix limits, not bounded streaming capture.
The big-output case demonstrates finite output collection and preservation of exit 17. It does not demonstrate enforcement of 32 KiB stream bounds or overflow handling. The distinction matters because the report credits a resource boundary that this harness does not implement.
References, executed revision e2cb05c: [capture implementation (line 136)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p04.py:136), [big-output record (line 776)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P04/74346e3119e64a3599ccbdda8eaf2f7e.json:776), [claimed cap (line 64)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-P04-resource-storage.md:64).
Moderate
M4 — P03 overstates transport coverage and gate strength.
The 12-design-entry/10-oracle-category reconciliation is reasonable: loopback inspection and prepared stdio have different observations. It does not establish every designed variant ran.
- Agent framing has a direct path only.
- DNS-over-TCP has a direct path only.
- The designed io_uring netlink/bring-up variant is absent.
- The positive gate checks any successful receipt per enabled endpoint category, not every direct/ring variant.
- Positive listener events are reduced to booleans; candidate events are snapshotted before listener shutdown/join. Thus the retained artifact is weaker than a complete phase-tagged oracle transcript.
The worker transcript nevertheless shows successful controls for the implemented ring paths and meaningful candidate failures. Interpret ring failures using negative CQE results—-111, -101, -2, -13—rather than the stale printed errno=1.
SOCK_RAW also fails in the ordinary control, so it is not candidate-specific denial evidence. Ping’s EACCES does not uniquely isolate a capability mechanism. Refusal of connections to absent namespace-local listeners does not prove that intra-execution loopback communication is impossible.
References: [P03 coverage claim (line 34)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-P03-transports-ipc.md:34), [gate/oracle handling (line 294)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p03.py:294), [worker paths (line 294)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p03.c:294), [agent path (line 392)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p03.c:392).
M5 — Several P04 measurements are presented as stronger accounting or isolation evidence than recorded.
The anonymous-memory OOM and task-limit evidence is persuasive. Other statements need narrowing:
- memory.peak is cumulative across the reused leaf and remains 134217728 after the first OOM. Later values do not independently demonstrate SysV SHM, /dev/shm, or inode/slab charging.
- The recorded mount inventory is the native configuration. It does not contain /app or the .NET loader mounts.
- The read-only input write-open fails with EACCES in both modes. Together with recorded ro mount flags, this supports an observed read-only configuration, but the failed positive cannot isolate mount enforcement as the cause.
- FD exhaustion at 61 additional descriptors occurs in both modes under the shared RLIMIT_NOFILE=64. This demonstrates that inherited limit, not an additional namespace restriction.
- Approximately 26.8 MB working set is a successful startup observation, not a measured minimum memory budget.
- Swap-use sampling is explicitly absent. Forced abort and fixture cleanup do not establish exhaustive absence of every possible host core-dump sink.
- The .NET artifact binding hashes the apphost and loader libraries, but does not retain a digest binding for P04Workload.dll and the complete published closure.
The explicit “inode ceiling not configured” reconciliation is correct.
References: [counter implementation (line 75)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p04.py:75), [published executable binding (line 154)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p04.py:154), [SHM observations (line 623)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P04/74346e3119e64a3599ccbdda8eaf2f7e.json:623), [mount inventory (line 716)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P04/74346e3119e64a3599ccbdda8eaf2f7e.json:716), [inode qualification (line 86)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-P04-resource-storage.md:86).
M6 — P07 identity, ordering, and observation claims exceed its instrumentation.
Starttimes are recorded, but not compared again before fault injection. C10 resolves a PID, sleeps 0.5 seconds, then signals that numeric PID. The general validate_pid() helper is unused. This does not establish the claimed per-action identity revalidation or stale-PID exclusion.
The observer records first-seen process snapshots, not the designed continuous timestamped population timeline.
Deterministic cancellation genuinely refuses before spawning. All five racy cancellations occur after the recorded release time; none exercises cancellation winning during setup. The preexec check precedes Bubblewrap execution, while the later GO barrier follows target initialization. Those distinct points must not be described interchangeably as target release.
No observed cancellation-winner escape or wrong-PID kill is established; the problem is unsupported coverage and assurance.
References: [identity collection (line 108)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:108), [C10 signal (line 518)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:518), [release handling (line 227)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:227), [race observations (line 570)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/70cf6e33690c4bd1b24ebec4824d2b08.json:570), [observer implementation (line 687)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:687).
M7 — P07’s cleanup/provenance chain is incomplete beyond its named final domains.
The unrestricted interference worker attempts to create the fixed host directory /tmp/mnt-p07; no corresponding removal appears in the harness. The final positive reports EEXIST. The committed independent cleanup checks omit this path. Complete fixture cleanup therefore is not established, although named final workspaces and cgroups are recorded absent.
Only the final suite, C11 wrapper, and independent cleanup records are committed for P07. The superseded runs’ cleanup claims cannot be independently audited from their missing committed records.
C11 correctly preserves execution result Unknown while later confirming domain cleanup. However, its trigger observes a nonempty target leaf, which can contain setup processes before target execution; it does not independently prove the advertised release/teardown phase. Its standalone record also lacks source SHA/hash fields, unlike the suite record.
References: [fixed host path (line 131)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.c:131), [positive `EEXIST` (line 136)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/70cf6e33690c4bd1b24ebec4824d2b08.json:136), [C11 trigger at `826438d` (line 856)](/C:/Users/Laszlos/source/repos/Gagamba/spikes/Ar7Feasibility/p07.py:856), [C11 artifact](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/c11-wrapper-2bb5adb292294908bc0f75ea20b7bf07.json), [cleanup artifact](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P07/independent-cleanup.json).
Minor
m1 — Aggregate and summary reconciliation remains unfinished.
The aggregate gives the wrong AR-6 tag target, names P02 as the last executed source, retains current-looking “P03/P04/P07 NOT RUN” statements, and uses ambiguous “Unknown-then-resolved” wording. Its P04 row omits the inode limitation.
Additional corrections:
- P03 run d1a141f0… actually executed successfully before the raw-ring addition; it is not another launch failure.
- P04 oom_score_adj lowering records EACCES, not the report’s EPERM.
- P07 C4 reads the first identity line, not RELEASED.
- P07’s interference positives and negatives are not identical: the positive successfully migrates between delegated leaves; its host namespace open fails before setns.
- C7/C8’s reported timings do not match the final artifact, although both remain consistent with rapid disappearance in this topology.
References: [aggregate (line 18)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/restricted-execution-AR-7-feasibility.md:18), [earlier executed P03 record](C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-feasibility/P03/d1a141f04b83418ca921690cd8ac8460.json), [P07 summary (line 25)](/C:/Users/Laszlos/source/repos/Gagamba/docs/evidence/ar7-P07-crash-ownership.md:25).
Informational
P02 preserves its failed setns positive as inconclusive and distinguishes visibility exclusion from authorization denial. P04’s failed cleanup records remain false, with subsequent cleanup recorded separately. These are appropriate evidence practices.
Windows dispositions remain appropriately narrow: P01 eliminates the tested unaugmented composition; P01B never constructs a target and remains inconclusive in that context. No P01C or qualified VM/macOS candidate is established.
3. Probe dispositions
Probe	Disposition	Reason
P02	VERDICT SUPPORTED	Same-UID positive operations, useful restricted execution, external membership/status observations, unchanged controls, signal-effect oracle, and cleanup support the stated host-specific result. setns authorization remains inconclusive; ENOENT/ESRCH remain visibility results.
P03	VERDICT SUPPORTED WITH QUALIFICATION	Implemented direct/ring transport attempts support isolation from the tested host endpoints. Exclude unexecuted variants, intra-execution loopback guarantees, target-leaf placement/emptiness claims, and an assertion of complete dual-path oracle coverage.
P04	VERDICT SUPPORTED WITH QUALIFICATION	Touched-memory OOM evidence, task accounting, exact 8,388,608-byte scratch exhaustion, inherited FD ceiling, and useful native/.NET execution support resource feasibility. Independent inode bounds, bounded capture, complete charging measurements, and complete writable-closure qualification are not established.
P07	RERUN REQUIRED	Several required setup/caller/active-stop transitions were replaced with different observations, and identity/phase instrumentation does not support the recorded complete verdict. Existing successful lifecycle observations should be retained; this is a harness/evidence gap, not a demonstrated candidate failure.
4. Required corrections
Docs-only corrections
Correct the aggregate refs, execution chronology, current-versus-historical status, errno values, timings, C4 marker description, and interference comparison. Carry the inode limitation into the aggregate. Keep C11 execution-result uncertainty explicitly separate from cleanup confirmation.
Evidence interpretation corrections
Remove unsupported claims of:
- Identical P03 cgroup placement.
- Complete P03 direct/ring coverage and prohibited namespace-local networking.
- P04 streaming capture bounds, per-case SHM/metadata charging, measured startup minimum, and complete .NET mount inventory.
- P07 completed caller/setup/descendant-stop coverage, per-action identity revalidation, and continuous population timelines.
Recover and bind existing superseded P07 and published-.NET evidence if available. Establish the disposition of /tmp/mnt-p07; absent retained evidence, preserve cleanup uncertainty rather than declaring it clean.
Genuine rerun requirement
To restore the existing P07 completed-matrix verdict, rerun only the missing or mis-targeted required cases: the specified caller/provisioning failure, actual pre-target setup deadline, live-descendant stop scenarios, and correctly phase-observed observer loss, with the declared identity safeguards.
No blanket rerun of P02/P03/P04 is required for their narrowed dispositions. No rerun is requested merely to find an inode limit or add interesting variants.
5. Aggregate non-claims
AR-7 still does not establish:
- General Linux security, production readiness, or qualification beyond this WSL2 host, pinned helper, UID/delegation setup, and tested operations; LSM state remains unknown.
- Complete syscall/IPC coverage, kernel setns authorization denial, or prohibition of intra-execution IP loopback.
- An independently configured inode/metadata ceiling.
- Bounded streaming output, complete memory-charging attribution, measured swap usage, exhaustive core-dump exclusion, or a minimum .NET budget.
- General root-exit descendant elimination. C7/C8 remain observations of this topology and host.
- Recovery of C11’s execution result. It remains Unknown.
- Windows native impossibility: only the original LPAC + Job candidate is eliminated. P01B remains untested as a composition; no P01C exists.
- Windows VM qualification or macOS restricted execution; VM remains pending and macOS refusal-only.
- P05 immutable preparation/release, P06 filesystem closure/export, P08 runtime baseline, P09 security manifest/package gating, production restricted execution, Hufu integration, a VM provider, or a privileged service.
6. Candidate integrity
The central namespace/capability/seccomp composition remained materially consistent, but the complete execution composition did not remain frozen as claimed.
The pinned Bubblewrap binary and filter remained consistent. No Landlock, separate host UID, privileged service, or additional confinement mechanism was introduced.
However, P03 changed actual cgroup placement accidentally. P04 moved limits from the shared delegated parent to the target leaf, expanded workload mounts/environment, and removed the inherited file-size limit for .NET. Those changes are relevant configuration differences even where justified and documented.
The narrower transport and resource observations remain comparable with these distinctions. A blanket “byte-identical candidate across all four probes” statement is unsupported.
7. Architecture-review readiness
The evidence is useful for architecture discussion, but the current completed-feasibility checkpoint is premature.
Minimum blocking work is to reconcile the material overclaims, resolve or explicitly retain the identified provenance/cleanup gaps, and complete the targeted P07 evidence needed for its existing verdict. No production implementation or candidate redesign is warranted by this review.
AR-7 EVIDENCE NOT READY FOR ARCHITECTURE DECISION
