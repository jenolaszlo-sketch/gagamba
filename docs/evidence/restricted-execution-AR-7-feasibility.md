# AR-7 blocking feasibility — P01 counterexample and mandatory stop

Date: 2026-10-09. Production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`. **Restricted execution remains unimplemented and unqualified.**

P01 found an ungranted ARAP file read and a synthetic ARAP broker request from zero-capability LPAC. The broker created a child outside the candidate Job; independent ownership/effect oracles confirmed it. This blocks the tested unaugmented LPAC + Job composition. Under the user's section 24 hard-stop rule, the assignment stopped after cleanup and review. P02/P03/P04/P07 are **NOT RUN**, not passed or unsupported by inference. No final AR-7 architecture is selected.

“Ungranted” here means absent from the proposed execution profile/manifest: the synthetic file and pipe DACLs deliberately grant the broad ARAP SID. That ambient class authority is the counterexample, not access to an object with no granting ACE.

## Repository state and source binding

| Field | Value |
| --- | --- |
| Repository/worktree | `C:\Users\Laszlos\source\repos\Solo\ar7-feasibility-worktree`, linked to `C:\Users\Laszlos\source\repos\Gagamba` |
| Branch | `qualification/ar7-feasibility` |
| Starting SHA | `8e156774fec28d807b557acae487791289369609` (documentation-only AR-7 review) |
| Last executed source SHA | `ee170724c14d8bd138cec1a461d86eb8bec76ec6`; clean at release |
| P01 evidence checkpoint | `9b95dd58d4a62f4fee258110f58d9000b7e1d220` |
| Ending documentation SHA | Resolve this evidence commit using `git log -1 --format=%H`; the delivery reports its actual SHA and final clean state. It follows, rather than retrospectively changing, the tested-source binding. |
| Frozen production branch/tag | Both resolve to `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`; never moved |
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
| Following documentation commit | Consolidated stop, four explicit not-run records, handoff and queue; inspect Git log for its immutable identity |

## Probe disposition

| Probe | Result/host/candidate | Key control and outcome | Consequence/evidence |
| --- | --- | --- | --- |
| P01 | **BLOCKED BY THIS PROBE**; Windows 26200 x64, SDK 26100; non-elevated filtered administrator token; documented zero-capability LPAC + Job | Same unrestricted native program reached all eight brokers/read all seven sentinels. Candidate read input/wrote scratch, denied owner-only/AAP files, but read ARAP file and invoked global ARAP broker. External child PID 32372: outside candidate, inside outer Job, marker exists, exit 0. | **WINDOWS NATIVE PATH BLOCKED**, narrowly for this composition. [P01](ar7-P01-windows-lpac.md), [durable observations](ar7-feasibility/P01-observations.json) |
| P02 | **NOT RUN**; no new Linux host/helper/UID context measured | No control or hostile attempt; P01 mandatory stop occurred first | Linux feasibility pending, not failed. [P02](ar7-P02-linux-control-plane.md) |
| P03 | **NOT RUN** for all candidates | P01 is already a host-IPC counterexample, but no full transport matrix or surviving Linux candidate was established | Network authority pending; unaugmented Windows candidate does not advance. [P03](ar7-P03-transports-ipc.md) |
| P04 | **NOT RUN** for all candidates | No quotas, all-writable-storage accounting or .NET budget controls exercised | Resource/storage viability pending; no silent 128 MiB budget increase. [P04](ar7-P04-resource-storage.md) |
| P07 | **NOT RUN** for all topologies | P01 normal completion/Job association is not a crash/root-status test | Atomic release/crash/death-detection/root-versus-wrapper status pending. [P07](ar7-P07-crash-ownership.md) |

## Interim checkpoint at early stop

The planned checkpoint after P01 and P02 was not reached: section 24 stopped work earlier. Windows unaugmented LPAC + Job is **Blocked**; Linux is **Inconclusive** because it was not run, not because of a measured kernel/helper failure. No candidate was advanced to P03/P04. No platform alternative was implemented.

| Platform/candidate | Identity | FS authority | Network | IPC | Control plane | Resource bounds | Crash ownership | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Windows ordinary AppContainer + Job comparison | Observed pass | Observed fail | Pending | Observed fail | Pending | Pending | Pending | Observed fail |
| Windows documented LPAC opt-out + Job | Observed pass* | Observed fail | Pending | Observed fail | Pending | Pending | Pending | Observed fail |
| Windows stronger native composition or VM | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| Linux rootless pinned helper/namespaces/cgroup | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Pending |
| macOS restricted profile (proposed refusal-only; no candidate qualified) | Pending | Pending | Pending | Pending | Pending | Pending | Pending | Unsupported |

*Identity means externally observed low-integrity, zero-capability AppContainer token plus successful documented opt-out creation and AAP behavioral distinction. Direct LPAC boolean query was unavailable. It does not prove a separately provisioned normal non-admin account. FS failure means ARAP read authority remained; tested owner-only file denial still succeeded. macOS property cells are Pending because no experiment occurred; Unsupported status means no qualified restricted candidate and proposed refusal, not measured OS inability.

## Architecture-eliminating finding and unresolved decisions

P01 eliminates **LPAC + Job alone** as sufficient enforcement of the proposed explicit closure/no-host-IPC contract on this host. The exact-package pipe control establishes restricted reachability; the ARAP request is ungranted candidate authority, not a named-pipe exception secretly added to the profile. The broker was deliberately synthetic and finite. The result does not establish a real-service exploit or eliminate every native Windows design.

Astra must decide the next scope before more experiments:

- Windows: a further reviewed native authority mechanism, a defensible narrower runtime baseline if compatible with the common contract, VM-backed isolation or refusal. Do not broaden capabilities or host ACLs during probes.
- Linux: resume P02 only under an explicit continuation decision; Bubblewrap versus bespoke launcher, same-UID resistance, separate UID, Landlock, host provisioning and VM alternatives remain unmeasured.
- Helper/guardian topology: P07 must establish pre-release deadline/ownership, independently enforceable stop, root status and resistance to target control-handle retention. No helper is selected here.
- Privileges: P01 used per-user resources and no elevated token; normal standard-account deployment remains unqualified. No Windows service, root daemon, setuid launcher or persistent elevated helper was created.
- macOS: remains proposed refusal-only for the restricted profile. AR-6 legacy launchd lifecycle qualification does not create a restricted execution candidate.

P05/P06/P08/P09 were not run. No production seam, new provider, public contract, Hufu dependency, release workflow or packaging metadata changed.

## Cleanup and independent challenge

All final-run Jobs reached active count 0; all eight broker endpoints closed and returned not-found; the unique AppContainer mapping/AC folder/whole package directory were absent; listed fixture files/directories removed; observer found no surviving fixture executable or watchdog expiration. The preceding failed environment launch also left no package/workspace residue. Unknown token state and inaccessible `LOCAL` namespace legs remain explicitly unknown/inconclusive.

Luna read-only reviewed the documented recipe, pre-release safety and final P01 raw evidence. Accepted cleanup-gating and outer-membership fixes were applied before release. Its post-run review confirmed the counterexample and cautioned against real-service/all-native/normal-standard-account/LOCAL-denial claims; these limits are recorded. No missing decision-reversing variant was identified. Four not-run dispositions are not completed native probes and cannot acquire independent qualification from this review.

Final Luna documentation challenge accepted the immediate-stop precedence and exact-source/not-run/cleanup records. Its two editorial findings were accepted: distinguish profile-ungranted authority from deliberately broad object ACLs, and leave untested macOS property cells Pending with explicit proposed-refusal status. No variant was identified that would reverse the scoped global ARAP request result. Source/binary/driver hashes, eight ordinary endpoint controls, external broker ownership, empty Jobs, relative evidence links and unchanged production/release paths were checked before the final documentation commit.

**AR-7 FEASIBILITY PROBES BLOCKED - USER DECISION REQUIRED**
