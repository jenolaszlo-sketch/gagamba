# Current work queue

Updated 2026-10-03. Status definitions: Complete = stated artifact exists; Ready = dependencies sufficient to start; Held = named prerequisite missing; Pending = later delivery. Complete design work never implies completed security qualification.

| ID | Status | Next action / exit evidence |
| --- | --- | --- |
| PREP-1 | Complete | Standalone repo, original proposal preserved, feasibility/design and implementation/testing plans |
| PREP-2 | Complete | Local WSL/Docker inventory and three-platform test environment plan |
| GP-0 | Complete — design baseline only | [offline-process-v1](security-model.md) defines permissions, assumptions and lifetime; empirical qualification remains open |
| GP-1A | Ready — next | Implement F1/F2 host/worker, protocol validation, positive controls, watchdog and evidence validator from [fixture contract](fixture-protocol.md) |
| GP-1B | Held on GP-1A | Add deterministic child/grandchild, early exit, barrier and ownership checks; pass worker self-tests |
| GW-1A | Held on GP-1A | Probe local Windows export/schema, minimal launch and captured I/O; no production-provider claim |
| GW-1B | Held on GP-1B and GW-1A | Real denial, descendants, launch/cancel/crash and controlled workload evidence; provider ADR |
| GL-1A | Held on first local Windows probe and separate WSL distro | Prepare Ubuntu WSL2, namespace/seccomp inventory and equivalent minimal Linux probe |
| GM-1A | Held on GP-1A and workflow/runner setup | Remote exists; manual bounded macOS capability probe, ARM64 first and Intel separately |
| CI-1 | Held on GP-1A | Remote exists; add real build/unit workflow when code is available, no placeholder success jobs |
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
- No `tests/Fixtures/`, solution, `eng/verify.ps1`, `eng/probe.ps1`, `eng/conformance.ps1` or `.github/workflows/` is expected yet. Create each with working behavior at its milestone. No empty `src/Gagamba.*` projects.
- The five feasibility gates above remain open and do not block GP-1A fixture self-tests. They block their respective backend and release claims.
- Before GL-1A execution, install and initialize a separate Ubuntu WSL2 distribution, confirm version 2 and Linux filesystem placement, and record setup evidence. Docker's internal distribution is not the development environment. This installation has not occurred.
- Before GM-1A launcher/conformance work, manually run a bounded macOS runner availability probe and retain its image/architecture/tool report. That probe is the initial part of the macOS research gate, not proof of sandbox enforcement. No macOS workflow or probe run exists yet.
