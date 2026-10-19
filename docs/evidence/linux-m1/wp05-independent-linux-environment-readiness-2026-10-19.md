# WP05 Independent Linux Environment and Source Readiness

Date: 2026-10-19
Scope: producer-side source/environment preparation for an independent Luna Linux qualification of exact WP05 R12; no acceptance

## Disposition

- Source access: **`WP05 R12 INDEPENDENT SOURCE ACCESS PREPARED - REVIEWER ACCESS UNVERIFIED`**
- Producer Linux environment: **`WP05 LINUX PRODUCER PREFLIGHT PASSED - TRANSIENT DELEGATION CLEANED`**
- Overall: **`WP05 INDEPENDENT QUALIFICATION INFRASTRUCTURE PARTIALLY READY - INDEPENDENT REVIEWER ACCESS UNVERIFIED`**
- WP05 harness/release: **not accepted**; no independent qualification is claimed.
- Existing dispositions are unchanged: seccomp `WP05 SECCOMP POLICY ACCEPTED - CORRECTED FILTER QUALIFIED`; harness follow-up `WP05 HARNESS FOLLOW-UP REMEDIATION COMPLETE - INDEPENDENT LINUX REQUALIFICATION REQUIRED`; SIGPIPE implementation-time A with runtime verification pending.

No production behavior, seccomp policy/artifact, coordinator, adapter, release protocol, journal schema, owner entrypoint, or production reaper was changed. The only source change in this slice is the test-tooling preflight script and its testing documentation.

## A. Repository and source binding

- Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`
- Infrastructure branch: `infrastructure/wp05-independent-linux-readiness`, created from R12.
- Exact qualification revision: `38f6981b2a60daab2df37c92913a6760c1bef131` (R12).
- R12 tree: `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`.
- I1 source access: `6792dc98fa577d23e073f5090862c52fc0f1ecf7`.
- I2 preflight/tooling: `f0d8cf81efa08a281635d7ca97e2c10ec26de29d`.
- I3 evidence/handoff: this commit.
- Final readiness bundle identity: `bundle-identity.txt` in the I3b follow-up.

The Q4 blocker branch was fetched into canonical as `review/wp05-harness-linux-final-acceptance` from the existing Solo checkout. The exact Q4 commit is `71d5a7e0af5141d74110eacc06ba478ab75a81ac`; its parent is `fa7aaed1ab40ab86892cf3c89a85bc7e070cfd4c`, whose parent is Q3 `d809ac969a84073d117d30b5a80437159741774c`. Q3 is an ancestor of Q4; R12 is not an ancestor of Q4; Q3 is not an ancestor of R12. The Q1-Q4 lineage and the R10-R12 lineage share R9 `aa1a7f5...`; no merge, rebase, amend, or ancestry rewrite was performed. The blocked Q4 branch remains historical and is not the R12 qualification source.

Source bundle (outside the repository):
- `C:\Users\Laszlos\source\repos\Solo\wp05-r12-source.bundle`
- 2,052,989 bytes; SHA-256 `38951b891be5673366c33c855610c7cd08b841353efbe108e78b41b9ad589eea`
- Six refs: R12 plus `review/wp05-release-seccomp-counterexample`, `review/wp05-seccomp-kernel-requalification`, `review/wp05-qualification-harness-requalification`, `review/wp05-abort-oracle-final-requalification`, and `review/wp05-harness-linux-final-acceptance`.
- `git bundle verify` reports complete history and `okay`; restore in a disposable empty repository produced tree `6854675d...` exactly; `git fsck` exited 0. Details: `source-bundle-procedure.txt`.
- The bundle does not include the infrastructure branch/script/evidence. The final readiness bundle is created after I3 and recorded separately.

New independent checkout:
- `C:\Users\Laszlos\source\repos\Solo\wp05-r12-linux-qualification`
- Branch `review/wp05-harness-r12-linux-qualification`; HEAD R12; clean; empty diff from R12.
- Q1-Q4 and earlier review history are preserved under `refs/remotes/source/*`.
- Creation evidence: `reviewer-checkout.txt`.

## B. Producer environment and preflight

The preflight `eng/linux-m1-independent-readiness.sh` checks environment/source readiness only. It does not build, run the qualification suite, execute a target, or claim acceptance. It checks:

- Linux x86-64 and the recorded distribution/kernel.
- Non-root uid/gid; `CapInh`, `CapPrm`, `CapEff`, and `CapAmb` zero; records `CapBnd` and `NoNewPrivs`.
- cgroup v2 unified; delegated `memory` and `pids` in both `cgroup.controllers` and `cgroup.subtree_control`; `cgroup.kill`; writable delegated parent and current host leaf; a temporary empty child cgroup with `memory.max`/`pids.max`, removed immediately.
- Bubblewrap 0.11.1, exact SHA-256 `523da3e7...`, non-setuid; .NET 10 SDK, GCC, Python, Git, libc, and required utilities.
- Exact R12 object, R12 ancestry, `src`/`native`/`tests` equal to R12, and no tracked modifications.
- Frozen seccomp generator/checked-in hex consistency and regenerated size 672 / SHA-256 `2aa87901...`; ignored WP04 artifact pin if present.
- Writable workspace and report path.
- It always prints `REVIEWER_ACCESS=UNVERIFIED` and `INDEPENDENT_ACCEPTANCE=NOT_CLAIMED`.

The delegated cgroup was absent on the producer boot `00ddd357-8337-4ff3-b9f0-5b990cd9111d`. For the producer check only, a fresh transient tree was created at `/sys/fs/cgroup/gaga-delegated`, delegated `memory pids`, chowned to gaga, with a `host` leaf; the preflight ran as uid/gid 1000 in that leaf. Exact setup, placement, and cleanup: `setup-cleanup.txt`. The tree was removed after the run; no keepalive or reusable privileged asset remains.

Observed producer environment at the passing runs:
- Ubuntu 26.04.1 LTS; kernel `6.6.114.1-microsoft-standard-WSL2 x86_64`; glibc 2.43; Git 2.53.0; .NET 10.0.112; GCC 15.2.0; Python 3.14.4.
- uid/gid 1000; `CapInh=CapPrm=CapEff=CapAmb=0`; `CapBnd=000001ffffffffff`; `NoNewPrivs=0`.
- Bubblewrap `/usr/bin/bwrap`, 0.11.1, mode 755, `523da3e7...`.
- Source present at `/mnt/c/Users/Laszlos/source/repos/Gagamba`; run1 HEAD I1, run2 HEAD I2; both contain exact R12 and pass the production-surface comparison.
- Host cgroup after the run: `populated 0`, `frozen 0`; parent removed.

Results:
- Run 1: PASS, 57 passed, 0 failed. Report: `readiness-producer-run1.txt`.
- Run 2: PASS, 57 passed, 0 failed. Report: `readiness-producer-run2.txt` (also `readiness-producer.txt`).
- Negative control before I2: a checkout with uncommitted `docs/testing-and-ci.md` changes failed 56/1 at `source.clean_tracked`; report `readiness-producer-dirty-source.txt`. The committed I2 checkout then passed. This is source-sensitivity evidence, not a host failure.
- During preparation the first provisional run exposed and corrected a preflight bug: `/proc/self/cgroup` returns the namespace-relative `/gaga-delegated/host`, which was initially treated as a filesystem path. I2 resolves it to `/sys/fs/cgroup/gaga-delegated/host`; the passing reports show both raw and resolved paths. No product/host change was made.

The `/mnt/c` checkout produced Git CRLF normalization warnings while comparing existing working-tree files; `source.production_surfaces` passed against exact R12 and no tracked content changed. A reviewer should prefer a Linux-native copy for execution performance.

## C. Independent reviewer access

No Luna run was performed in this turn. The prepared checkout and bundle prove source preparation only. Prior independent attempts recorded:
- WSL launch failure `Wsl/E_ACCESSDENIED`;
- inability to access the canonical checkout under the reviewer filesystem permissions;
- absence of R12 objects in the reviewer checkout at that time.

The producer context's WSL, ACL, and delegated-cgroup access are not transferable. The canonical ACL currently grants Laszlos FullControl, inherited CodexSandboxUsers ReadAndExecute+Synchronize, and SYSTEM/Administrators FullControl; Luna's actual execution identity and access remain unknown. No host ACL was widened.

Required external action: Luna must run the preflight in its own context from the prepared checkout (or restored final bundle), as a non-root uid in an existing operator-delegated cgroup v2 memory/pids host leaf, using exact R12 and the script's `--report`. If WSL, checkout, Git objects, or delegation remain unavailable, the reviewer must record the exact command, identity, error, and failure class. The producer pass cannot substitute. Details: `access-blockers.txt`.

## D. Tooling and documentation changes

- Added `eng/linux-m1-independent-readiness.sh` (I2): fail-closed environment/source preflight; no build/test/target; always marks reviewer access unverified; cleans its temporary child cgroup and workspace.
- Updated `docs/testing-and-ci.md` (I2) with the invocation, checks, transient-delegation and cleanup rule, and the producer/reviewer separation.
- Added this evidence document and artifacts (I3).
- Production diff from `58cb1ba` under `src/`, `native/`, or `artifacts/`: empty. No test suite was run in this slice.

## E. Evidence integrity and limits

- `SHA256SUMS` is computed over the committed artifact bytes and verified after the I3 commit; the verification is recorded in the I3b hash audit. The artifacts directory uses `-text`.
- The producer reports contain Git CRLF warnings from `/mnt/c`; those warnings are representation-only and are not failures. The source check compares exact R12 production/test surfaces.
- The transient cgroup was producer-created, not a persistent operator delegation. It was removed; the final state has no Gagamba cgroup, process, workspace, or keepalive in the delegated tree.
- The producer run is not independent qualification, not WP05 acceptance, and not evidence that Luna can access the source or host.
- Seccomp, harness, identity, SIGPIPE, release/stop, crash, no-resend, trusted-status, and owner-integration statuses are unchanged. WP05 remains not accepted; owner integration remains deferred to WP08/WP10; the wrong-architecture seccomp issue remains separate.

## F. Artifacts

`docs/evidence/linux-m1/wp05-independent-linux-environment-readiness-artifacts/`:

- `repository-refs.txt` - refs, lineage, Q4 fetch, checkout, bundle identity.
- `source-bundle-procedure.txt` - bundle creation, restore verification, tree match, fsck.
- `reviewer-checkout.txt` - standalone checkout creation and preserved history refs.
- `commands.txt` - exact producer commands.
- `environment-producer.txt` - host/toolchain/identity/cgroup environment.
- `setup-cleanup.txt` - transient delegation setup, placement, cleanup.
- `access-blockers.txt` - prior Luna blockers and required external action.
- `readiness-producer-run1.txt`, `readiness-producer-run2.txt`, `readiness-producer.txt` - passing 57/0 reports.
- `readiness-producer-dirty-source.txt` - dirty-source negative control.
- `SHA256SUMS` - committed-byte manifest; I3b hash audit and final bundle identity follow.

## G. Required next action

Luna must independently restore/access exact R12, run the preflight in its own non-root delegated Linux context, and report the exact result. Only then can source/Linux reviewer readiness be accepted. The subsequent WP05 work remains independent requalification of exact R12: native observer/identity/stop provenance, process-host separation and cleanup, SIGPIPE runtime, supported-profile repeatability, release/stop, fresh-process crash, no-resend, trusted status, and full acceptance. Owner integration remains WP08/WP10; WP05 and the harness are not accepted.
