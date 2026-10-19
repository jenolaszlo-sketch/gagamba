# WP05 OpenCode Linux Environment Preparation

Date: 2026-10-19
Scope: producer-side Linux-native source and non-root qualification-environment preparation for independent OpenCode review; no WP05 qualification or acceptance

## Disposition

- Source: **`WP05 LINUX-NATIVE R12 SOURCE PREPARED`**
- Cgroup environment: **`WP05 NONROOT QUALIFICATION DELEGATION PREPARED`**
- Infrastructure provenance: **`WP05 READINESS INFRASTRUCTURE PROVENANCE RECONCILED`**
- Overall: **`WP05 OPENCODE QUALIFICATION ENVIRONMENT PREPARED - INDEPENDENT REVIEWER VERIFICATION REQUIRED`**
- WP05 harness/release remains **not accepted**. No independent reviewer access is claimed.

No production source, test fixture, seccomp policy/artifact identity, coordinator, adapter, release protocol, journal schema, owner entrypoint, or production reaper was changed. The preparation changes are documentation/evidence and Linux-host/session state outside the repository.

## A. Repository and Linux-native source

- Canonical repository: `C:/Users/Laszlos/source/repos/Gagamba`
- Continuation branch: `infrastructure/wp05-independent-linux-readiness-2`, created from I3b.
- P1 source preparation: `d9955dd0820938d245241c6b24469b33785fcf46`
- P2 session provisioning/guidance: `f8397bb5e2ab91bdd3449d3f72441307c437e7c6`
- P3 evidence/handoff: this commit.
- Frozen target: R12 `38f6981b2a60daab2df37c92913a6760c1bef131`; tree `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`.
- Linux-native checkout: `/home/gaga/qualification/gagamba-r12`; owner `gaga:gaga` (uid/gid 1000); fresh `git init` from bundles, not a copy of Windows `.git` or Windows ownership.
- Branch: `review/wp05-opencode-linux-qualification` at R12; tracked status clean; ignored `artifacts/` contains only the regenerated filter.
- Normal `git` operations as `gaga` succeeded with no ownership warning and no `safe.directory` configuration. R11 and the production baseline are ancestors; `git fsck --no-progress` exited 0.

## B. Source transfer and history

Original bundle (preserved unchanged):
- `C:/Users/Laszlos/source/repos/Solo/wp05-r12-readiness.bundle`
- 2,081,455 bytes; SHA-256 `0e9b5486035b558acb746c61e07c6934ff1a023dba8142809953c21a75d76bf3`
- Seven refs: infrastructure I3 `7909522...`, R12, and five earlier review refs.
- Verified in a disposable empty repository: complete history, all refs fetched, R12 tree equal, fsck 0.

Supplemental bundle (I3b and the OpenCode review):
- `C:/Users/Laszlos/source/repos/Solo/wp05-r12-readiness-supplement.bundle`
- 1,953,932 bytes; SHA-256 `19b524a1ef57d2491fc1a2a8e2355599d59b9166af15811b2d94cd1294870e0f`
- Three refs: I3b `5c04d32...`, R12, and `review/wp05-opencode-linux-readiness` `e6f8604...`.
- Verified complete; fsck 0. The original bundle was not modified.

The Linux-native checkout preserves `refs/remotes/original/*` and `refs/remotes/supp/*`, including the OpenCode reviewer evidence commit and all historical review branches. The reviewer commit is documentation-only, parent R12, and was not merged or rewritten. Raw source verification: `linux-native-verification.log`; curated: `linux-native-checkout.txt`.

## C. Cgroup delegation and session

Independent review finding: boot `8020a863-085b-48e3-87e4-b31983a64ef6`, user gaga, current cgroup `/init.scope`, and `/sys/fs/cgroup/gaga-delegated` absent. The producer observed the same reboot/session behavior.

The minimal tree was provisioned using the previously documented procedure, via the local WSL root operator context:
- `mkdir /sys/fs/cgroup/gaga-delegated`
- enable `memory` and `pids` in `cgroup.subtree_control`
- create `host`
- chown parent/host and relevant `cgroup.procs`/`cgroup.subtree_control` files to `gaga:gaga`
- mode 755 on parent/host; no host security setting, ACL, firewall, package, systemd service, setuid helper, or persistent privileged daemon changed.

Current session (boot `69d053fb-0fd9-4209-aa5b-be128424ed34`):
- parent `/sys/fs/cgroup/gaga-delegated`, 755 gaga:gaga; controllers `cpu memory pids`; subtree `memory pids`
- host `/sys/fs/cgroup/gaga-delegated/host`, 755 gaga:gaga; controllers `memory pids`
- host `cgroup.procs` 644 gaga:gaga; parent/host `cgroup.procs` empty
- host `cgroup.events`: `populated 0`, `frozen 0`; `cgroup.kill` present
- preflight created a disposable child, wrote `memory.max`/`pids.max` to `max`, and removed it; no controller assumption was made from directory writes alone.

Placement boundary:
- gaga cannot self-migrate from `/init.scope`: writing its PID to the writable host `cgroup.procs` returned `Permission denied`. This failure is preserved in `preflight-placement-failure.log` and `preflight-placement-failure.txt`.
- The authorized placement launcher is root-owned and root-only: `/root/wp05/run-as-gaga-in-host.sh` (mode 700, root:root). It writes its own PID to the host leaf, then execs `setpriv --reuid=1000 --regid=1000 --clear-groups`; the qualification/test process is gaga with zero effective capabilities. Root is never the test process. The earlier gaga-writable root-executed launcher was removed after finding this privilege-boundary issue.
- A root-only provisioning runbook script is `/root/wp05/provision-session-cgroup.sh` (mode 700); the normal reviewer launcher is the only routine root step.

Session lifetime:
- An empty delegated tree disappeared when the WSL distro lifecycle reset between invocations. A minimal gaga-owned session anchor, `sleep infinity` PID 576 in `/init.scope`, keeps the WSL session/cgroup tree alive. It is outside the qualification host leaf, has no capabilities, is not a target/test process, and is not a privileged daemon.
- The tree and anchor are intentionally retained for the independent review session. Owner: gaga review session; lifecycle owner: root operator. Cleanup commands are in `reviewer-handoff.txt` and must be run after the review (or a reboot removes the tree/anchor). No permanent keepalive is claimed.

## D. Infrastructure provenance and I3b

I3b:
- Full SHA `5c04d324894a84b17a5d40f7546eab9a65d0eb4c`
- Parent I3 `79095223c2675934bec0ce52fe21718f28d3f471`
- Contents: `bundle-identity.txt`, `hash-audit.txt`, work queue/handoff updates only.
- No executable infrastructure change; the readiness script at I3b is Git blob `93c4dec426372c2cd89be05f3e617ed20f3b24eb`, identical to I3.

The original seven-ref bundle was created at I3 before I3b existed. Its infrastructure ref is I3, so the reviewer's "I3b unknown" result is explained; the bundle was not corrupt. The supplemental bundle supplies I3b and the OpenCode review commit. Selected script: I3b/I3, blob `93c4dec4...`; extracted SHA-256 `09a1c6b0...`; staged byte-exact into R12 by the wrapper and removed after execution. Full reconciliation: `infrastructure-reconciliation.txt`.

Check-count reconciliation:
- Reviewer: 39 PASS + 4 FAIL = 43; failures `cgroup.placement`, `delegated.exists`, `delegated.placement.writable`, `source.repository`.
- Guarded blocks skip 14 dependent checks (six delegation/controller, four probe, four source), giving 57 potential.
- With no ignored filter artifact: 56/0; after regenerating it from the checked-in generator: 57/0. The script did not change; I3b contains no script change. The initial reviewer result was the two blockers plus skipped dependents, not a different script version.

## E. Preflight

Selected script and exact invocation are in `preflight-summary.txt` and `reviewer-handoff.txt`. The final two runs used the root-only launcher and produced:
- `root-launcher-run1`: 57 PASS / 0 FAIL, exit 0
- `root-launcher-run2`: 57 PASS / 0 FAIL, exit 0

Earlier preserved results:
- gaga self-placement attempt: exit 1, permission denied (placement log)
- two runs before the ignored filter artifact: 56/0 each
- two complete runs after artifact regeneration with the earlier launcher: 57/0 each

Each passing run verified non-root identity and zero CapInh/CapPrm/CapEff/CapAmb, cgroup v2, delegated memory/pids, host placement, disposable child create/control/remove, Bubblewrap `523da3e7...` non-setuid, .NET/GCC/Python/Git, exact R12 HEAD/tree and production surfaces, clean tracked source, regenerated 672-byte filter `2aa87901...`, and writable workspace/report. `REVIEWER_ACCESS=UNVERIFIED` and `INDEPENDENT_ACCEPTANCE=NOT_CLAIMED` were emitted by the script. Probe cgroups and `/tmp` workspaces were removed; host leaf remained empty; session tree and anchor were retained.

## F. Independent reviewer handoff

Entry point: `C:/Users/Laszlos/source/repos/Solo/wp05-r12-linux-qualification` is the Windows review checkout; the prepared Linux-native review checkout is `/home/gaga/qualification/gagamba-r12`.

Commands, root operator step, expected identity, lifetime, reboot/reestablishment and cleanup are in `reviewer-handoff.txt`. The reviewer must:
1. verify exact R12 and clean source as gaga without `safe.directory`;
2. use the root-only operator launcher to enter the delegated host, then run the readiness script as gaga;
3. preserve report/log and cgroup/source observations;
4. leave tracked source unchanged and make reviewer-only commits on a separate branch.

GLM/Beam/OpenCode access has not yet been tested for this prepared session. The producer runs are not independent verification. The final bundle is created after this evidence commit; its identity is recorded in the follow-up bundle attestation.

## G. Frozen implementation and boundaries

- Production diff from `58cb1ba` under `src/`, `native/`, `artifacts/`: empty.
- Seccomp: 672 bytes, SHA-256 `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`; regenerated from the checked-in generator.
- Bubblewrap: 0.11.1, SHA-256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6`.
- G2 manifest: 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`.
- RELEASE frame: 128 bytes, SHA-256 `90A07CDEF85B18B5B2A0360D7D01705CF7B9E4E9B2D52E7C56DA76A16201E59B`.
- OwnershipSnapshot schema version 3.
- No WP05 release, termination-observer, SIGPIPE, crash, no-resend, full Linux M1, target, or acceptance test was run. No seccomp requalification was performed.

## H. Evidence

Evidence report: `docs/evidence/linux-m1/wp05-opencode-linux-environment-preparation-2026-10-19.md`
Artifacts: `docs/evidence/linux-m1/wp05-opencode-linux-environment-preparation-artifacts/`
- source: `linux-native-checkout.txt`, `linux-native-verification.log`
- cgroup/session: `cgroup-session.txt`, `cgroup-provisioning.log`, `session-verification.txt`
- failure: `preflight-placement-failure.log`, `preflight-placement-failure.txt`
- preflight: `preflight-summary.txt`, run logs/reports (56/0, 57/0, final root-launcher runs)
- provenance: `infrastructure-reconciliation.txt`, `filter-identity.txt`
- handoff: `reviewer-handoff.txt`
- `SHA256SUMS` is computed over committed artifact bytes and verified after the evidence commit; the hash audit and final bundle attestation follow.

Tracker updates: `docs/work-queue.md`, `docs/handoff.md`.

## I. Remaining limitations

- The authorized local WSL root context is required for session placement and initial cgroup provisioning; gaga cannot self-place. This is an operator boundary, not a root test. The reviewer must perform/obtain that operator step in its own context.
- The WSL session anchor is required for cgroup persistence between invocations; it is retained for this review. A reboot removes the tree/anchor; the runbook must be followed.
- Independent reviewer access, delegation, source verification and the preflight have not been executed by GLM/Beam through OpenCode. That is the next task and remains required.
- WP05 harness/release qualification remains pending under a separate specification; this preparation does not accept the sandbox.
