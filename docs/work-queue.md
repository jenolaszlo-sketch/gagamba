# Current work queue

Updated 2026-10-04. Status definitions: Complete = stated artifact exists; Ready = dependencies sufficient to start; Held = named prerequisite missing; Pending = later delivery. Complete design work never implies completed security qualification.

| ID | Status | Next action / exit evidence |
| --- | --- | --- |
| PREP-1 | Complete | Standalone repo, original proposal preserved, feasibility/design and implementation/testing plans |
| PREP-2 | Complete | Local WSL/Docker inventory and three-platform test environment plan |
| GP-0 | Complete — design baseline only | [offline-process-v1](security-model.md) defines permissions, assumptions and lifetime; empirical qualification remains open |
| GP-1A | Complete 2026-10-03 | F1/F2 host/worker, protocol validation, positive controls, watchdog and evidence validator implemented; Release `eng/fixture-selftest.ps1` 16/16 harness + 13/13 unit Passed, cleanup Confirmed — see [GP-1A evidence](evidence/fixture-selftest-GP-1A.md). Proves fixtures only, not backend enforcement |
| GP-1B | Complete 2026-10-03 | Deterministic child/grandchild, early exit, barrier and orphan-stop with ownership records and survivor sweep; Release 20/20 harness + 15/15 unit Passed — see [GP-1B evidence](evidence/fixture-selftest-GP-1B.md). Unsandboxed host stop only; kernel-backed ownership arrives with backends |
| GW-1A | Complete 2026-10-03 | Availability verdict EXPORT-PRESENT-SCHEMA-UNPINNED: all three Experimental_* exports resolve on 25H2, PE table cross-check agrees, in-job=TRUE noted, schema UNPINNED with MIT sources identified, launch never invoked — see [GW-1A evidence](evidence/windows-availability-GW-1A.md). No provider claim |
| GW-1B-L1 | Complete 2026-10-03 | Schema pinned (mxc v0.8.0), verified spec compiler, first launches: bare shape rejected fail-closed, appcontainer exit 7 + job, rw-grant marker effect, profile lifecycle proven — see [GW-1B launch evidence](evidence/windows-minimal-launch-GW-1B.md) |
| GW-1B-L2 | Complete 2026-10-03 — evidence with 2 blocking gaps | Denial (ro + ungranted) with positive controls, descendant effects + inherited denial, cancel race: all proven. Tree-stop gap (kill AND natural exit leave survivors) and stdio-redirect rejection characterized — see [GW-1B slice-2 evidence](evidence/windows-denial-tree-io-GW-1B.md) |
| GW-1B-L3 | Complete 2026-10-03 — evidence with open gates | Pipes proven (exact capture), ToolHelp supervisor proven cross-boundary (found/killed, root survived), whoami maps base image; dotnet exits 1 silently in all grant configs (compat, not closure) — see [GW-1B slice-3 evidence](evidence/windows-pipes-supervisor-workloads-GW-1B.md) and [provider ADR](adr/0001-windows-provider.md) |
| GW-1B-L4 | Complete 2026-10-04 — 14/19 legs green, 4 gates open + 1 NotRun | Encoder byte-exact vs flatc; crash-recovery by recorded PID and launch-stability/retry proven; grant-shape and Git-tree grant findings recorded; remaining red: tree-stop, file-handle stdio, dotnet runtime, Git install grant — see [GW-1B slice-4 evidence](evidence/windows-slice4-GW-1B.md) |
| GW-1B-L5 | Complete 2026-10-04 — ro C:\ unblocks git | L4 leg ancestor variants: ro `C:\` alone turns the full workload green (true HEAD + clean status); ro profile is unbindable (`ERROR_INVALID_DATA`, poisons any spec containing it); full chain unnecessary. Provider rule: ro drive-root grants for msys cwd resolution, closures outside profile trees — see [GW-1B slice-4 evidence](evidence/windows-slice4-GW-1B.md) |
| GW-1B-L6 | Complete 2026-10-04 — dotnet exit-1 mechanism identified | New `L3-WORKLOAD-PIDPROBE` leg (manifest v6 = 20 IDs): self-contained managed probe exits 0 in-sandbox; self-introspection works but only `[System Process]` + self are visible, and `GetProcessById(parent)` throws the verbatim dotnet `ArgumentException`. The CLI installer probe kills it, not the runtime. Provider direction: self-contained closures — see [GW-1B slice-4 evidence](evidence/windows-slice4-GW-1B.md) |
| GW-1B-L7 | Complete 2026-10-04 — runner topology verdict | `L5-SANDBOX-PARENT` (manifest v7 = 21 IDs): P1-P4 + P6-P8 green — nested creation, same-domain visibility, nested confinement, `--list-sdks`/`--list-runtimes`, offline `build` all work under a resident runner. P5 fails on SCM service probing (named dotnet dependency, not concerning). P9: the package-readability hunt ended in a broken fixture (missing `using`, fails on host too); with it fixed, `build` passes and `test` hangs on TCP-loopback handshake (127.0.0.1:6279) denied by the offline profile — a policy decision, not a quirk. Model: sandbox → resident runner → tool tree — see [sandbox-parent evidence](evidence/windows-sandbox-parent-GW-1B.md) |
| GW-1B-L8 | Complete 2026-10-05 — %TEMP% multi-grant rule | `L1-GRANT-SHAPE` (manifest v8 = 22 IDs): multiple explicit grants bind (same-kind and mixed, `%TEMP%` and `C:\temp`); canonical forms bind identically; duplicates collapse + cross-kind conflicts reject in preparation (host asserts); nested is inner-wins. Prior S0/S0b rejections do not reproduce as rules — INVALID_DATA is tuple-dependent (stable per path string, falsified as length cap), retry-fresh stands. Normative text in security model grant-preparation section |
| GW-1B | In progress — engine findings | Remaining: descendant stop on root exit (GQ-1: job objects), STARTUPINFO/file-handle transport, dotnet CLI host (self-contained contracted), test loopback (policy). Closed: git ancestor grants, dotnet PID mechanism, runner topology, offline build |
| GL-1A | Held on separate WSL distro (Windows probe done) | Prepare Ubuntu WSL2, namespace/seccomp inventory and equivalent minimal Linux probe |
| GM-1A | Held on workflow/runner setup (GP-1A done) | Remote exists; manual bounded macOS capability probe, ARM64 first and Intel separately |
| CI-1 | Ready next — real-workflow test | Remote exists; add real build/unit workflow for fixtures + probe, no placeholder success jobs. Tests whether the sandbox serves a real workflow; may show which restrictions matter |
| POL-1 | Decided — loopback as profile capability | `offline`: no network. `offline+loopback`: localhost only, requested explicitly (e.g. `dotnet test`). Least privilege intact; no weakening of `offline` |
| POL-2 | Decided — dotnet SCM probing restricted | Host-service inspection (`--info` tail) is host-inspection restricted; CLI-core (`--list-sdks/runtimes`, build) supported. No model bends for diagnostics |
| PROV-1 | Noted — msys ro drive-root grant transitional | Compatibility concession for msys cwd resolution, recorded as adapter quirk; not the intended filesystem model, must not become precedent |
| GQ-1 | Direction set — job objects | Kernel-backed descendant ownership is the contract: Job Objects (`KILL_ON_JOB_CLOSE`), supervisor as the Gagamba abstraction above. Covers natural root exit and supervisor crash; verify no breakaway/nested-job cases |
| GP-2 | Held on all backend spike evidence | Review public contracts and provider decisions |
| GP-3 / GW-2 / GL-2 / GM-2 | Pending | Managed core and actual providers after reviewed contracts |
| GQ-1 / GR-1 | Pending | Full conformance, independent review and isolated package qualification |
| Hufu/Luban integration | Deferred | Outside current project scope |

The GP-1A/B and GW-1A/B slices refine the broader milestones in the [implementation plan](implementation-plan.md); they allow availability/minimal-launch research before full adversarial fixture completion. They do not relax backend acceptance criteria.

## Decisions made for implementation preparation

- Use offline-process-v1 as the baseline, including denial of loopback rather than implicitly allowing private networking.
- Use explicit ReadOnly/ReadWrite directory semantics; deny write-only and conflicting nested grants in v1.
- Begin in disposable workspaces with controlled root ancestry; target-created escape attempts remain in scope.
- .NET 10 is the initial fixture/spike target, not a promise that every eventual package targets only .NET 10.
- Keep public APIs and package structure unfrozen until backend evidence is available.

## Open feasibility gates

1. Windows API schema/source availability, working I/O transport and owned-unit crash behavior.
2. Exact platform runtime resource baselines; no host-secret or host-control-service exposure.
3. macOS robust owned-tree termination, including escaping process groups, plus API maintenance/distribution strategy.
4. Linux syscall/namespace policy, host interoperability restrictions and deployment prerequisites.
5. Availability of repeatable Windows 11 CI; Windows Server compilation does not qualify the client API.

Record evidence and status changes here as work progresses. Do not rerun completed inventory solely to rediscover the same environment; refresh it when provisioning or observed state changes.

## Pre-coding gap disposition

- Commit/push prerequisite is satisfied by preparation baseline `7bdb28f` on local and remote main. A clean `git status` has no staged `A` entries; inspect the exact checkout if another session still reports the initial commit.
- The verifier supports Windows PowerShell `-File` by computing its default root in the body. README documents process-scoped execution-policy options. Explicit empty roots reject rather than selecting another repository.
- `tests/Fixtures/` (fixture host/worker/harness/self-test), `spikes/Gw1aProbe`, `spikes/Gw1bLaunch`, `eng/probe.ps1|.sh`, `eng/launch-spike.ps1|.sh` and `eng/fixture-selftest.ps1|.sh` now exist with working behavior. No solution file, `.github/workflows/`, or `src/Gagamba.*` projects exist yet; create each with working behavior at its milestone, never as empty scaffolding.
- The five feasibility gates above remain open and do not block fixture self-tests. Gate 1 (Windows API/schema/IO/lifetime) now has partial evidence: availability, schema pin, launch, denial, pipes and supervisor sweep pass; descendant stop, file-handle transport and dotnet CLI remain open. The Git install-tree grant is bypassed via staged closures; the open Git gate is ancestor list-access for cwd resolution (GW-1B-L5). Managed runtime itself is proven via self-contained closures (GW-1B-L6); only the CLI host probe fails. The gates still block their respective backend and release claims.
- Before GL-1A execution, install and initialize a separate Ubuntu WSL2 distribution, confirm version 2 and Linux filesystem placement, and record setup evidence. Docker's internal distribution is not the development environment. This installation has not occurred.
- Before GM-1A launcher/conformance work, manually run a bounded macOS runner availability probe and retain its image/architecture/tool report. That probe is the initial part of the macOS research gate, not proof of sandbox enforcement. No macOS workflow or probe run exists yet.
