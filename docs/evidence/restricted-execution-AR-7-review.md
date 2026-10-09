# AR-7 restricted-execution architecture review evidence

- Date: 2026-10-09 (Asia/Manila).
- Remediation slice: AR-7 architecture review only; original F01 remains open.
- Baseline source SHA: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`.
- Final implementation source SHA: not applicable; no implementation was changed. All production source inspected is the frozen baseline above. A documentation commit does not inherit its release seal.
- Frozen branch/tag: `qualification/ar6-native` / `ar6-review-baseline-06c8ac7`.
- Review branch: `review/ar7-architecture`, separate worktree; clean baseline before review documents were added.
- Review host: Microsoft Windows 10.0.26200, X64; PowerShell 7.6.5; .NET SDK 10.0.401. No target runtime execution was performed for AR-7.
- Relevant native prerequisites: not established for AR-7. No LPAC, namespace/helper, Landlock, resource-limit, privileged-broker or native macOS experiment was run.
- Qualification scope: source/document inspection, primary OS documentation research, review-artifact consistency and frozen-baseline integrity checks only.
- Explicit exclusions: AR-7 enforcement, native hostile-code qualification, package qualification, release qualification, runtime compatibility, privileged setup, Hufu implementation, merge and publication.

**Status: architecture review delivered; feasibility and implementation remain held.** This is not `IMPLEMENTED — NATIVE QUALIFICATION PENDING`: no AR-7 implementation exists. Planned probes are not reported as tests that passed.

## Deliverables and source binding

- [Architecture review](../reviews/2026-10-09-ar7-security-architecture.md): A–F, J–K, ten AR7-Fxx findings, platform alternatives, exact profile, decision log and final summary.
- [Probe and Sol handoff](../reviews/2026-10-09-ar7-probes-and-handoff.md): G–I, eleven classified experiments, seven conditional slices, twenty-five qualification rows and future security-manifest requirements.
- [Source inventory](restricted-execution-AR-7-review/source-inventory.json): SHA-256 of baseline production source plus selected inspected tests/documents, source SHA, frozen archive identity and method. Hash inventory establishes which bytes were available, not that every line was independently audited or tested.

Repository guidance, current handoff/queue, original independent review, remediation plan, AR-1 through AR-6 evidence, lifecycle/security documents, public invocation/capability shapes, provider launch/resource/capture paths, orchestration and selected regression/conformance tests were inspected. Historical experiments were used only through their documented evidence; historical probe binaries were not executed. No sibling implementation was reviewed or changed.

Reproducible source anchors at the reviewed SHA:

| Source | Review seam |
| --- | --- |
| [Provider.cs](../../src/Gagamba.Execution/Provider.cs) / [Platforms.cs](../../src/Gagamba.Execution/Platforms.cs) | `ProcessStartSpec.TrySnapshot`, lifecycle-only Prepare/Launch contract and advertised capability bounds |
| [WindowsExecutionProvider.cs](../../src/Gagamba.Execution.Windows/WindowsExecutionProvider.cs) / [OwnedJob.cs](../../src/Gagamba.Execution.Windows/OwnedJob.cs) | `LaunchInner` create/assign/release ordering and `OwnedJob.Create` kill-on-close flags |
| [WindowsCapturePipes.cs](../../src/Gagamba.Execution.Windows/WindowsCapturePipes.cs) | Readable NUL stdin and explicit three-handle inheritance list |
| [LinuxExecutionProvider.cs](../../src/Gagamba.Execution.Linux/LinuxExecutionProvider.cs) / [OwnedCgroup.cs](../../src/Gagamba.Execution.Linux/OwnedCgroup.cs) | `LaunchInner`, atomic cgroup placement and host-path controller access |
| [LinuxCapturePipes.cs](../../src/Gagamba.Execution.Linux/LinuxCapturePipes.cs) | `AddActions` installs descriptors 1/2 only; descriptor 0 is not replaced |
| [MacOsExecutionProvider.cs](../../src/Gagamba.Execution.MacOS/MacOsExecutionProvider.cs) / [OwnedJob.cs](../../src/Gagamba.Execution.MacOS/OwnedJob.cs) | Capture refusal, user-domain job and private plist |
| [ExecutionOrchestrator.cs](../../src/Gagamba.Runtime/ExecutionOrchestrator.cs) | Setup/launch before deadline wait, `RunReceipt` and process-local fingerprint |
| [Linux lifecycle regressions](../../tests/Gagamba.Execution.Linux.Tests/LifecycleRegressions.cs) | E10 root sibling migration and failure/reaping controls |
| [ConformanceMatrix.cs](../../src/Gagamba.Conformance/ConformanceMatrix.cs) / [broken provider controls](../../tests/Gagamba.Conformance.Tests/BrokenProviderTests.cs) | Lifecycle-only legs, macOS observed escape and false-launch rejection |

These links resolve to files in the review worktree; its unchanged source matches the frozen Git blobs in the inventory.

## Review observations, recommendations and outstanding evidence

| Claim / concern | Baseline observation and method | Implementation change | Fixed observation / qualification |
| --- | --- | --- | --- |
| Production is lifecycle-only | Static source: ordinary Windows process + Job; Linux cgroup spawn; macOS user launchd; no restriction manifest or filesystem/network enforcement | None | No restricted claim; AR7-F01 open; require P01–P09 |
| Windows needs identity/IPC boundary | Static ordinary token launch; Microsoft documents ambient AppContainer ACL grants and WMI Job exception | None | Candidate LPAC composition unproved; AR7-F02; P01/P03 |
| Linux needs inaccessible control hierarchy | AR-2 measured root E10 migration is historical; current same-UID cgroup path operations/delegation alone do not establish hostile denial | None | No new migration probe; AR7-F03; P02 |
| Preparation is not file-identity binding | Static `ProcessStartSpec.TrySnapshot` copies values/checks cwd, not stable executable/runtime objects | None | AR7-F04; P05/P06 |
| Capture is not all inherited-authority control | Static Linux `AddActions` replaces stdout/stderr only, leaving stdin inherited; Windows captured path uses readable NUL and a three-handle allowlist | None | Restricted path must explicitly own stdin and all helper channels; AR7-F05/F10; P03/P05/Q06 |
| Native resource bounds differ | Static no required process/memory/disk setup; primary docs distinguish Job charge, cgroup task counting and controller prerequisites | None | AR7-F06; storage/task/runtime viability P04 pending |
| Crash ownership needs new composition proof | Linux owner-death Absent; macOS setsid escape in AR-6; Windows create-before-assign source interval | None | No new crash probe; AR7-F07; P07 |
| macOS cannot inherit a stronger claim | Existing capture refusal and observed PG escape; Apple public App Sandbox is a separate possible product architecture | None | Initial proposed profile refuses; AR7-F08; P10 required to reopen |
| Historical requirements conflict | Static security-model overlap rules and root-exit expectations differ from each other/current lifecycle | None | New versioned copied-closure proposal; AR7-F09; user decisions remain pending |
| Deadline/receipt setup seam is incomplete for restricted use | Static deadline task installed after synchronous launch; receipt holds lifecycle strings/process-local fingerprint; setup FD transport must fit Linux close-from | None | Proposed pre-release gate and immutable authority record; AR7-F10; P05/P07/P09 |

These are architectural gaps or conditions to prove. This review does not reopen repaired AR-1–AR-6 findings merely because stronger AR-7 requirements need additional mechanisms.

## Exact checks and limits

1. Verified frozen branch and annotated tag resolve to `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`; checked original checkout remains clean.
2. Recomputed SHA-256 of retained `gagamba-ar6-review-baseline-06c8ac7.zip`: `9b548946ef7df529115352d53accf76f9fac10cfc1e6bbbf5cd450845e1ee2a4`, matching its sidecar. Read the retained seal: `sourceSha` is the same full SHA; `sourceTreeClean=true`; version `0.1.0-preview.4`. No frozen archive bytes were changed.
3. Confirmed review changes are documentation-only in the isolated review worktree; production source, tests, workflows, package metadata, original proposal and all prior evidence are unchanged against the frozen source.
4. Checked relative Markdown links, unique finding/probe/qualification/slice identifiers, cross-references, required deliverable headings, 55-file source inventory and final decision wording. The checker confirmed ten findings, eleven planned probes, twenty-five planned qualification rows, seven proposed slices and seven changed documentation files; `git diff --check` passed. These are artifact checks, not security tests.
5. No build or unit/native test was rerun because no executable source changed and such tests would not establish the proposed restrictions. AR-7 native tests: **0 run**. AR-7 installed-package tests: **0 run**. Every P/Q item remains planned.

Frozen qualification is [run 37902043866](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/37902043866). The existing AR-6 evidence document's `7660f16` results remain an earlier exact-source record; the final `06c8ac7` candidate has its own retained seal. This review neither rebases those records nor carries their qualification into the review documentation commit.

## Cleanup and review process

No sandbox target, listener, synthetic broker, cgroup, mount, Job, service, task, identity/profile or firewall rule was created by this review. There are no newly launched AR-7 workloads to clean up. The isolated documentation worktree and its files are intentional deliverables, not discarded probe resources. Native cleanup is **not exercised**, not “Confirmed” for a proposed sandbox.

Reviewer: Codex (GPT-6 agent in this session), source and architecture review. No second model/subagent or external human independently reviewed AR-7. The user's specification title names Astra; this artifact does not misattribute the work to that model. Earlier Luna review evidence belongs to its original slices and is not re-used as AR-7 review evidence.

Material review adjustments: made Windows complete writable-state accounting a blocking decision; separated Linux task/process ceilings; preserved the inherited-stdin gap as an explicit requirement; treated helper status/setup descriptors as an integration question; distinguished Apple child inheritance from packaging entitlements; retained pre-release/macOS refusal and original F01 open. No finding was closed through documentation alone.

## Qualification boundary and next gate

The proposed `restricted-offline-closure-v1` is not implemented or qualified on any platform. Primary OS references support choosing experiments, not passing the security matrix. New production work waits for blocking feasibility evidence and the review's material user decisions. Do not change Sol's execution architecture on the frozen branch. No new privileged service or VM deployment is approved here.

AR-7 REQUIRES FEASIBILITY PROBES BEFORE ARCHITECTURE DECISION
