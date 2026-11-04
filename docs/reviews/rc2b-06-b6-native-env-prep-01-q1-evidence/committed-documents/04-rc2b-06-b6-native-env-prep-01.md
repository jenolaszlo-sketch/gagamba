# B6 native environment preparation evidence

Date: 2026-11-04 (Asia/Manila)
Commission: GAGAMBA-RC2B-06-B6-NATIVE-ENV-PREP-01
Baseline source: `21f0aa7f3afd3a7dbfbc6ecae51d4025b13439b7`
Baseline tree: `75cbd04d009a84aee2d4da591da228a88f0a0cde`
Final production source: unchanged from baseline; documentation delivery identity is the commit containing this receipt (resolve `git log -1 --format='%H %T' -- docs/evidence/rc2b-06-b6-native-env-prep-01.md`). No self-referential final commit hash is embedded.
Branch/repository: canonical `main`, `C:\Users\Laszlos\source\repos\Gagamba`
Origin: `https://github.com/jenolaszlo-sketch/gagamba.git`
Host: Windows11 Home Single Language 10.0.26200 x64; WSL2 registration inventory only
Architecture: Intel x64, Core Ultra5 125H, 14 cores/18 logical processors
Runtime/SDK: no build/runtime execution in this commission; historical guest SDK10.0.114/runtime10.0.14 distinguished from current inspection
Relevant native prerequisites: protected root ancestry, exact runtime/native pins, WSL2 feature permission and operator delegation remain future qualification gates
Scope: read-only source/host inventory, compatibility research, planning documents, drafting QA and Git preservation validation
Exclusions: no provisioning/import, package install, Linux root or privileged commands, namespace/seccomp/cgroup probe, build/test workload, installer/publication, production code, activation, S4/B7, branch/worktree creation/removal or push

**NATIVE ENVIRONMENT PLAN READY WITH QUALIFICATIONS.** One fresh disposable Ubuntu26.04 amd64 WSL2 image is the recommended architecture. Exact image/package bytes, real feature/permission/delegation probes, HD-5 configuration proof/decision, qualification publication sequencing and a reviewed installed-profile driver remain explicit execution gates. This is preparation evidence, not native-qualified or owner-accepted implementation evidence.

## Baseline and accepted-source audit

Expected commit/tree matched exactly; tracked working tree and index were clean (`git diff --quiet`, `git diff --cached --quiet`: both0). Branch main and origin matched commission. Cached HEAD...origin/main divergence0/0; live `git ls-remote origin refs/heads/main` independently returned `21f0aa7f3afd3a7dbfbc6ecae51d4025b13439b7`. No fetch/reset/rebase/stash/clean occurred. Git reads outside the Solo sandbox were approved read-only tool executions, not administrator elevation or Linux privilege.

Read canonical AGENTS, current status/handoff/queue, B6 design/qualification plan, specification/wire/S1/S2/F02/B2-amendment owner records, S3 producer evidence, installer/Python/trusted verifier, runtime/loaded-role/helper/cgroup/spawn sources and actual fixture/driver entry points. [Source input hashes](rc2b-06-b6-native-env-prep-01/source-inputs.json) identify checked working-file bytes; baseline Git commit is authoritative for committed-byte identity (Windows line endings may differ). Existing historical records were not rewritten.

S1 and S2 remain accepted with qualifications; S3 is implemented with qualifications, not owner-accepted by this commission. Accepted B2 amendment is direct authenticated original-owner evidence, no recovery forwarding/type13 and no qualified delegated-parent B2. Runtime owner-only one physical RELEASE and recovery stop-only authority are unchanged. F02 governs separate administrative publication approval and receipt-selected installed runtime record, without a new revocation policy.

Historical S2 archive `docs/evidence/rc2b-06-b6-impl-01-s2/candidate-payload.tar.gz` was rehashed: `0f4533b77daf2ae3c9d3a4487bc59d6be5be422393049f1fed5a3c0e54f1486a`, matching reported identity. Its set digest `31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133` is historical only. No S3 candidate was built or hashed; no future digest is declared.

GitHub connector returned 11 jobs for [reported CI run41792647967](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/41792647967): 10 completed/success, one launch-spike job skipped. The Ubuntu B2 lifeline step succeeded; Windows equivalent was skipped as platform-dependent. This corroborates reported CI job status only: the tool result did not establish run-level head SHA, and general CI is not installed/native qualification. [Saved job snapshot](rc2b-06-b6-native-env-prep-01/ci-jobs.json).

## Preserved independent Q1 history

Read/reconciled the six external packages under `C:\Users\Laszlos\source\repos\Solo`: initial `gagamba-s3-q1-review`, `gagamba-s3-q1-review-cont-01`, gap-closure01/02/03 and `gagamba-s3-q1-final-nonroot-01`. [Top-level report/manifest hashes](rc2b-06-b6-native-env-prep-01/review-inputs.json) preserve their identities without copying or editing originals. This preparation hashes reviewed reports/manifests; it does not repeat all archived native tests or assert independent revalidation of every raw archive member.

The initial audit was incomplete; continuation reconciled final Git blob versus CRLF export and added independent codec/kernel observations; gap packages added queued rights, EAGAIN, injected EINTR, paused syscall loss and replay observations. Latest final-nonroot disposition is **S3-Q1 INCONCLUSIVE — ENVIRONMENTAL QUALIFICATION ONLY**. It reports no demonstrated production defect. Its Q-E1/Q-E2/Q-E3 are preserved in the matrix: installed provider/R/O and selectors/loader; delegated legacy B2 and integrated HELD/C3; trusted-launcher and fresh loaded-owner premise.

Captured36 sent by R after O peer authentication is refused through actual record credentials. A deliberately altered synthetic original O can send identical prior36 successfully in PN because that fixture skips production loaded verification;36 has no session field and byte novelty is not the accepted proof. Actual protected owner's37/session and loaded checks remain mandatory. Dedicated4096-byte ancillary MSG_CTRUNC branch is unforced;253 rights+credentials+timestamp fits1096 bytes and254 fails EINVAL. Another rights parser's truncation pass is not this reader's pass. Untraced single samples are distinct from ptrace/injected syscall timing and from durable C3/domain cleanup.

## Read-only host assessment

`wsl --version` returned WSL2.7.3.0, kernel package6.6.114.1-1 and Windows10.0.26200.9711. Approved `wsl --list --verbose`/`--list --running --quiet` showed Ubuntu **Stopped**, version2, and docker-desktop **Running**, version2. Existing Ubuntu was not started, nor was docker entered. Therefore Linux kernel/filesystem/libc/runtime/current package sources were not freshly read; source-bound historical observations remain explicitly historical.

Historical Linux observations: kernel6.6.114.1-microsoft-standard-WSL2, glibc2.43, uid/gid1000, CapEff/CapPrm zero, `/`0:0 mode0777, `/opt/gagamba` absent, cgroup root0:0 mode0555/nonwritable and no delegated parent. Correct verifier refusal is retained; environmental absence is not a production defect. No chmod/remount/config/delegation change was attempted.

CIM read-only inventory: total visible memory16,275,772KiB, free2,276,728KiB at sampling (~15.52/~2.17GiB). Filesystem free C:21,310,644,224 bytes; D:507,861,516,288. These point to D: for disposable storage and a memory headroom gate; they do not measure future guest build peak. User-profile `.wslconfig` was absent. WSL listing/CIM first failed inside sandbox, then permitted read-only retries succeeded. No automatic approval rejection remains unresolved.

Compatibility research checked primary Microsoft WSL import/config/security documentation, Ubuntu libc/bwrap/dotnet package metadata, .NET10.0.114 release record, kernel cgroup documentation, systemd delegation and WSL kernel config. Links and limits appear beside claims in the plan. Exact .deb/bwrap/compiler/static-lib bytes and actual libc symbol exports remain unfrozen. Upstream version metadata is availability evidence only.

## Deliverables and drafting review

| Deliverable | Result / limitation |
|---|---|
| [Environment architecture and inventories](../plans/rc2b-06-b6-native-env-prep-01.md) | A/B/C comparison, exact source/code pins, trusted-path predicates, dependency/kernel matrix, cgroup authority, resources, retention and N0–N7 gates |
| [Native matrix](../plans/rc2b-06-b6-native-qualification-matrix.md) | T01–T25/T20a-b-c/T25-A-B, V01–V13, all28 S3 obligations, latest Q1 deltas, B2/B3 chronology, positive/mutation/evidence/verdict/cleanup and explicit driver/host blockers |
| [Future privileged runbook](../plans/rc2b-06-b6-native-provisioning-runbook.md) | Plan-only P01–P10 named actor/target/preconditions/evidence/retry/refusal/rollback; actual flags/source semantics; no executed provisioning |
| Independent-review handoff | Source-bound questions and frozen-input approval boundary in plan/runbook; external review and owner decisions still required before execution |

User requested Luna where possible. GPT-6 Luna performed read-only native-requirement assessment and qualification matrix drafting, including later external Q1 reports. Primary agent checked code/flags/contracts, corrected inherited matrix ambiguities (real B2 has no delegated parent/HELD, historical System.IO.Hashing is not an observed current defect, same-session echo byte novelty is not unconditional, HD-5 is not automatically enabled) and integrated the artifacts. Luna's follow-up drafting QA is internal assistance, **not** the commission's required independent approval or qualification authority. No reasoning transcript or self-generated owner acceptance is evidence.

Internal Luna QA identified three material planning issues: unnecessary explicit thread/subtree write grants, ICU being required by the current default path, and missing concrete supervisor-leaf placement. Accepted corrections remove additional thread/controller-reconfiguration grants, require current-path ICU, and gate systemd254+ DelegateSubgroup or a reviewed alternative placement mechanism. The suggestion to eliminate all cgroup.procs write permission was not accepted: kernel cgroup atomic-clone code still checks target/common-ancestor cgroup.procs write permissions even when userspace performs no migration write. The revised plan records this kernel-dependent authority and requires a bounded probe. No runtime/source change resulted.

## Validation and preservation

Validation is documentation/source consistency and preservation, not runtime testing. Check new local Markdown references, accepted ID coverage, exact script parser flags, qualifier grammar, JSON snapshots and changed-path scope; use `git diff --check` before documentation delivery. Production tests were deliberately not rerun for this documentation-only slice.

The [44-file starting hash inventory](rc2b-06-b6-native-env-prep-01/untracked-before.json) and [74-worktree starting registration inventory](rc2b-06-b6-native-env-prep-01/worktrees-before.txt) are the before controls. Delivery validation compares every original path/length/SHA-256 and every registration identity after line-ending and terminal-blank-line normalization. Only documentation deliverables and new evidence inputs may be added; status/handoff/queue updates must describe planning, not native success. [Validation receipt](rc2b-06-b6-native-env-prep-01/validation.json) records final comparisons and affected scope. Delivery commit/tree is reported in the final handoff; no push.

No live qualification execution was allocated, so application cleanup is **Not applicable: no test processes/domains created**. This is distinct from the historical native tests' bounded stop/reap results and from future domain cleanup. Planning files and source/reviewer materials are intentionally retained; no environment or historical generation/worktree was removed.
