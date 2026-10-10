# AR-7 blocking feasibility — P01 counterexample and mandatory stop

Date: 2026-10-09. Production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`. **Restricted execution remains unimplemented and unqualified.**

Update 2026-10-10: P02/P03/P04 executed under the post-P01 decision, each with a narrow **SUPPORTED BY THIS PROBE** outcome; P07 then executed for the surviving Linux candidate only, also **SUPPORTED BY THIS PROBE** (18/18 legs; C7/C8 expectations corrected by the init-death cascade mechanism, recorded in evidence). See the probe dispositions and [P02](ar7-P02-linux-control-plane.md) / [P03](ar7-P03-transports-ipc.md) / [P04](ar7-P04-resource-storage.md) / [P07](ar7-P07-crash-ownership.md) evidence. The P01 blocking finding below is unchanged. Independent reviews remain pending.

P01 found an ungranted ARAP file read and a synthetic ARAP broker request from zero-capability LPAC. The broker created a child outside the candidate Job; independent ownership/effect oracles confirmed it. This blocks the tested unaugmented LPAC + Job composition. Under the user's section 24 hard-stop rule, the assignment stopped after cleanup and review. P03/P04/P07 are **NOT RUN**, not passed or unsupported by inference. P02 ran later under the separate post-P01 continuation decision; its outcome is recorded in the probe disposition and does not alter the P01 finding. No final AR-7 architecture is selected.

“Ungranted” here means absent from the proposed execution profile/manifest: the synthetic file and pipe DACLs deliberately grant the broad ARAP SID. That ambient class authority is the counterexample, not access to an object with no granting ACE.

## Repository state and source binding

| Field | Value |
| --- | --- |
| Repository/worktree | `C:\Users\Laszlos\source\repos\Gagamba`, branch worktree (Solo `ar7-feasibility-worktree` retired 2026-10-10 after a clean-status check; branch unaltered) |
| Branch | `qualification/ar7-feasibility` |
| Starting SHA | `8e156774fec28d807b557acae487791289369609` (documentation-only AR-7 review) |
| Last executed source SHA | `caadbee51d9e909b9aebfed5f6bce7581bcdab3d` (P02 follow-up; clean at release). P01 source `ee170724c14d8bd138cec1a461d86eb8bec76ec6` unchanged |
| P01 evidence checkpoint | `9b95dd58d4a62f4fee258110f58d9000b7e1d220` |
| P02 evidence checkpoint | `0a3197f89552c9fcc81523240838dcdf022866c0`; raw observations `ar7-feasibility/P02/` + `independent-cleanup.json` |
| P01B evidence checkpoint | Gate runs at `f41670a` (nonce `1390153c…`) and `7b45c20` (nonce `ba357230…`, one bounded SACL correction); raw observations `ar7-feasibility/P01B/` + `independent-cleanup.json`. No candidate ran; INCONCLUSIVE in this context |
| Ending documentation SHA | Resolve this evidence commit using `git log -1 --format=%H`; the delivery reports its actual SHA and final clean state. It follows, rather than retrospectively changing, the tested-source binding. |
| Frozen production branch/tag | Branch `qualification/ar6-native` resolves to `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`; tag `ar6-review-baseline-06c8ac7` is annotated and targets qualified source `7660f16dbb5b4b165e6a5279b131bf53c5bf6a7b` (ancestor of the branch tip; tag message names the seal commit). Neither ref moved |
| Production/release difference | No changes under `src`, `eng`, `.github` or package/build metadata relative to frozen baseline |
| Qualification | No probe/review revision inherits the AR-6 source seal; no merge, push, package publication or final architecture selection |

The frozen AR-6 [run 37902043866](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/37902043866) and retained archive/seal remain associated with `06c8ac7`. The retained ZIP is `C:\Users\Laszlos\source\repos\Solo\ar6-baselines\gagamba-ar6-review-baseline-06c8ac7.zip`, SHA-256 `9b548946ef7df529115352d53accf76f9fac10cfc1e6bbbf5cd450845e1ee2a4`. This assignment does not rewrite historical AR-6 evidence or claim a new seal.

Commits following the review start:

| SHA | Purpose |
| --- | --- |
| `450cb17eecb508bbf3bb1bb404a6ce5ceb6caced` | Bounded native P01 fixture, driver and design |
| `33e6dd7ae9fea8c27f6afb3d12e06cafc05d1ec5` | Preserve tool paths across compiler setup; no target released in prior tool failure |
| `574a52d9f0560a302fc3dbfa4988f030cd5aef4e` | Diagnose failing token information class explicitly |
| `34be8131e0d42b23ab9a49522dbce5e51fb1de96` | Retain public LPAC token query as unsupported/unknown |
| `ee170724c14d8bd138cec1a461d86eb8bec76ec6` | Supply synthetic profile environment; exact completed experiment source |
| `9b95dd58d4a62f4fee258110f58d9000b7e1d220` | Durable P01 observations, cleanup and read-only Luna review |
| `bea39161985b4f2f134eb612d85082a493fc5421` | Consolidated stop, four explicit not-run records, handoff and queue |
| `4d70b7969c47766b778727478c240abb661cbc13` | Post-P01 decision: eliminate LPAC + Job, resume P02 independently, permit one bounded P01B; no probes run in the decision |
| `2b22b89c34d8e6a95a2ad1f331b771aa40d74831` | Bounded same-UID P02 harness; run aborted before target release (address shadowing), cleaned; NOT RUN as isolation result |
| `c3a67eb0da3096ddbc277232f3ed7d7c29560a05` | Correct control-address shadowing; first executed P02 source |
| `caadbee51d9e909b9aebfed5f6bce7581bcdab3d` | Follow-up P02 variants (init-FD, clone matrix, actual signal oracle); last executed probe source |
| `0a3197f89552c9fcc81523240838dcdf022866c0` | Source-bound P02 observations and verified cleanup |
| P03 evidence checkpoint | Executed run `70edf2c2` at `229de9e`; setup observations `b70ec3f2`/`a0d51fae`/`522d7d8f` (no target released); raw observations `ar7-feasibility/P03/` + `independent-cleanup.json` |

## Probe disposition

| Probe | Result/host/candidate | Key control and outcome | Consequence/evidence |
| --- | --- | --- | --- |
| P01 | **BLOCKED BY THIS PROBE**; Windows 26200 x64, SDK 26100; non-elevated filtered administrator token; documented zero-capability LPAC + Job | Same unrestricted native program reached all eight brokers/read all seven sentinels. Candidate read input/wrote scratch, denied owner-only/AAP files, but read ARAP file and invoked global ARAP broker. External child PID 32372: outside candidate, inside outer Job, marker exists, exit 0. | **WINDOWS NATIVE PATH BLOCKED**, narrowly for this composition. [P01](ar7-P01-windows-lpac.md), [durable observations](ar7-feasibility/P01-observations.json) |
| P01B construction gate | **INCONCLUSIVE, unsupported in this execution context**; same host/caller as P01 (26200 x64, medium integrity, elevation 0, session 1); full-restriction token + distinct package SID + narrow owned DACLs all established | Fixture-owned window-station creation denied (`ERROR_ACCESS_DENIED`) before Jobs/target/authority tests; one bounded SACL correction changed nothing (hypothesis falsified). No candidate ever ran: not a composition verdict. Windows native track ends; no P01C. [P01B](ar7-P01B-windows-restricting-sid.md), [raw observations](ar7-feasibility/P01B/) |
| P02 | **SUPPORTED BY THIS PROBE**; Ubuntu 26.04.1 LTS on WSL2 (kernel 6.6.114.1, glibc 2.43), same host UID/GID 65534, pinned non-setuid Bubblewrap 0.11.1 + user/mount/PID/net/IPC/UTS namespaces + cap-drop ALL + cBPF filter, root-owned outer cgroup oracle | Gated same-UID positive control performed every permitted operation (cgroup create/write, ptrace, process-memory, pidfd, signal effect, both Unix endpoints with exact nonce). Restricted candidate did useful work (input read, scratch write, exit 0) while all tested control-plane interactions were denied (ENOENT/EPERM/ESRCH/ENOSYS), zero endpoint requests observed, limits unchanged, target domain empty after return. setns authorization stays INCONCLUSIVE. | Linux control-plane candidate survives for P03/P04/P07; harness frozen. Narrow scope only: no general sandbox claim. [P02](ar7-P02-linux-control-plane.md), [raw observations](ar7-feasibility/P02/) |
| P03 | **SUPPORTED BY THIS PROBE**; same host/candidate as P02 (WSL2 Ubuntu 26.04, UID 65534, frozen bwrap composition + filter `30bfaf6b…`); 10 synthetic endpoints × direct + io_uring paths | Gated positive completed every exchange on both paths with matching oracle entries. Restricted candidate did useful work while all direct `socket()` calls died EPERM and every io_uring-built channel died at connect/send on namespace/capability barriers (refused/unreachable/ENOENT/EACCES); zero oracle requests, limits unchanged, domain empty. Prepared stdio stayed functional. | Linux transport candidate survives for P04/P07. Narrow scope only. [P03](ar7-P03-transports-ipc.md), [raw observations](ar7-feasibility/P03/) |
| P04 | **SUPPORTED BY THIS PROBE**; same host/candidate as P02/P03 (explicit target 128 MiB/48 tasks); 16 native + 3 self-contained .NET cases, each gated | Below-budget cases completed with exact ceilings (scratch 8388608 B, fds 61, .NET hello ~27 MiB); above-budget cases OOM-killed (`oom_kill` + 128+signo rc) or `EAGAIN`-denied (45/48 tasks exact); zero counter anomalies, domain empty, cleanup verified. .NET startup minimum measured, profile unraised. Narrow scope only. [P04](ar7-P04-resource-storage.md), [raw observations](ar7-feasibility/P04/) |
| P07 | **SUPPORTED BY THIS PROBE**; same host/candidate as P02/P03/P04; 18 legs (positives, C1–C11 incl. wrapper-driven observer-death, deterministic + 5 racy cancels, 4 descendant/interference battery) | All legs green with no halt and no repair: caller/manager/outer/ns-init/target deaths all end domain-empty; cancel won ⇒ refused with zero spawns, cancel lost ⇒ owned + cleaned; no release after a won stop; interference denied (setns/mount EPERM); observer death audited via manifest with states Unknown. C7/C8 design expectations refuted by the PID-ns init-death cascade (instant-empty, strictly stronger). Root status kept separate from domain state throughout. | Linux crash/ownership candidate survives P07. Narrow scope only; independent review pending. [P07](ar7-P07-crash-ownership.md), [raw observations](ar7-feasibility/P07/) |

## Interim checkpoint at early stop (updated 2026-10-10 for P02, P01B, P03, P04, P07)

The planned checkpoint after P01 and P02 was not reached at the section-24 stop: section 24 stopped work earlier. That stop record is preserved above. Under the later post-P01 continuation decision, P02 ran independently: Windows unaugmented LPAC + Job stays **Blocked**; Linux control-plane is **Supported by this probe** within its narrow scope. P01B then ran once as the single permitted Windows composition: **Inconclusive** without a candidate (fixture-owned station creation denied in this execution context; one bounded correction spent; no P01C), so the Windows native track ends and Windows keeps refusing. P03 then ran for the surviving Linux candidate only: **Supported by this probe** within its narrow scope (all direct `socket()` EPERM; io_uring-built channels die on namespace/capability barriers; zero oracle requests; prepared stdio functional). P04 then ran for the same candidate: **Supported by this probe** within its narrow scope (below-budget native and .NET workloads complete with exact ceilings; above-budget cases OOM-killed or task-denied with matching counters; .NET startup minimum ~27 MiB measured, profile unraised). P07 then ran for the same candidate under the approved design plus three review clarifications: **Supported by this probe** within its narrow scope (18/18 legs; no released-and-unowned state, no post-stop release, no stop-survival; C7/C8 corrected by the init-death cascade; observer death audited Unknown-then-resolved). No platform alternative was implemented.

## Platform checkpoint 2026-10-10 (architectural split; P07 recorded)

| Candidate | Disposition | Next |
| --- | --- | --- |
| Linux rootless pinned helper/namespaces/cgroup | P02+P03+P04+P07 survived (narrow) | Deferred probes / independent review per spec; no production work |
| Windows unaugmented LPAC + Job | Blocked (P01) | Refusal; eliminated |
| Windows restricting-SID + LPAC | Inconclusive, unsupported in this context (P01B); no candidate ran | Native track ends; no P01C; refusal stands |
| Windows VM-backed | Pending (preferred fallback direction, not approved implementation) | Separate design decision |
| macOS restricted profile | Proposed refusal-only; no candidate qualified | Refusal |

| Platform/candidate | Identity | FS authority | Network | IPC | Control plane | Resource bounds | Crash ownership | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Windows ordinary AppContainer + Job comparison | Observed pass | Observed fail | Pending | Observed fail | Pending | Pending | Pending | Observed fail |
| Windows documented LPAC opt-out + Job | Observed pass* | Observed fail | Pending | Observed fail | Pending | Pending | Pending | Observed fail |
| Windows stronger native composition or VM | Pending | Pending | Pending | Pending | Pending | Pending | Pending | P01B restricting-SID+LPAC ended INCONCLUSIVE without a candidate (fixture setup denied in this context; no P01C); VM pending |
| Linux rootless pinned helper/namespaces/cgroup | Pending | Pending | Supported by P03 probe (narrow; prepared stdio only) | Pending | Supported by P02 probe (narrow) | Supported by P04 probe (narrow) | Supported by P07 probe (narrow; 18/18 legs, cascade-corrected C7/C8) | Candidate survives P02+P03+P04+P07; deferred probes + independent review pending |
| macOS restricted profile (proposed refusal-only; no candidate qualified) | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Unsupported |

*Identity means externally observed low-integrity, zero-capability AppContainer token plus successful documented opt-out creation and AAP behavioral distinction. Direct LPAC boolean query was unavailable. It does not prove a separately provisioned normal non-admin account. FS failure means ARAP read authority remained; tested owner-only file denial still succeeded. macOS property cells are Pending because no experiment occurred; Unsupported status means no qualified restricted candidate and proposed refusal, not measured OS inability.

## Architecture-eliminating finding and unresolved decisions

P01 eliminates **LPAC + Job alone** as sufficient enforcement of the proposed explicit closure/no-host-IPC contract on this host. The exact-package pipe control establishes restricted reachability; the ARAP request is ungranted candidate authority, not a named-pipe exception secretly added to the profile. The broker was deliberately synthetic and finite. The result does not establish a real-service exploit or eliminate every native Windows design.

Astra must decide the next scope before more experiments:

- Windows: a further reviewed native authority mechanism, a defensible narrower runtime baseline if compatible with the common contract, VM-backed isolation or refusal. Do not broaden capabilities or host ACLs during probes.
- Linux: P02 executed 2026-10-10 under the explicit continuation decision with a narrow SUPPORTED outcome; the P02 harness is now frozen unless a later probe genuinely invalidates one of its assumptions. Separate UID, Landlock, host provisioning and VM alternatives remain unmeasured.
- Helper/guardian topology: P07 established pre-release barrier/ownership (controller check→commit + pre-exec re-check), independently enforceable stop (root observer via owned outer cgroup, proven after manager/caller failure; observer death audited), truthful root-vs-domain status, and no target control-handle retention (interference denied). No guardian exists or was added; no helper is selected here.
- Privileges: P01 used per-user resources and no elevated token; normal standard-account deployment remains unqualified. No Windows service, root daemon, setuid launcher or persistent elevated helper was created.
- macOS: remains proposed refusal-only for the restricted profile. AR-6 legacy launchd lifecycle qualification does not create a restricted execution candidate.

P05/P06/P08/P09 were not run. No production seam, new provider, public contract, Hufu dependency, release workflow or packaging metadata changed.

## Cleanup and independent challenge

All final-run Jobs reached active count 0; all eight broker endpoints closed and returned not-found; the unique AppContainer mapping/AC folder/whole package directory were absent; listed fixture files/directories removed; observer found no surviving fixture executable or watchdog expiration. The preceding failed environment launch also left no package/workspace residue. Unknown token state and inaccessible `LOCAL` namespace legs remain explicitly unknown/inconclusive.

Luna read-only reviewed the documented recipe, pre-release safety and final P01 raw evidence. Accepted cleanup-gating and outer-membership fixes were applied before release. Its post-run review confirmed the counterexample and cautioned against real-service/all-native/normal-standard-account/LOCAL-denial claims; these limits are recorded. No missing decision-reversing variant was identified. Three not-run dispositions (P03/P04/P07) are not completed native probes and cannot acquire independent qualification from this review.

Final Luna documentation challenge accepted the immediate-stop precedence and exact-source/not-run/cleanup records. Its two editorial findings were accepted: distinguish profile-ungranted authority from deliberately broad object ACLs, and leave untested macOS property cells Pending with explicit proposed-refusal status. No variant was identified that would reverse the scoped global ARAP request result. Source/binary/driver hashes, eight ordinary endpoint controls, external broker ownership, empty Jobs, relative evidence links and unchanged production/release paths were checked before the final documentation commit.

**AR-7 FEASIBILITY UPDATE 2026-10-10 — P01 BLOCKS WINDOWS LPAC+JOB; P02+P03+P04+P07 SUPPORT LINUX CANDIDATE NARROWLY; P01B INCONCLUSIVE WITHOUT CANDIDATE; DEFERRED PROBES + INDEPENDENT REVIEW PENDING; NO PRODUCTION WORK**

Prior stop line preserved: AR-7 FEASIBILITY PROBES BLOCKED - USER DECISION REQUIRED (2026-10-09; superseded for P02/P01B scheduling only by the post-P01 decision and user continuation, not for the P01 finding).
