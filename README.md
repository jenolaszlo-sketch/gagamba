# Gagamba

Standalone .NET execution sandbox library — architecture and feasibility work.

Status: research/prototyping, updated 2026-10-04. The GP-0 design baseline, GP-1A/GP-1B fixtures, GW-1A availability probe and GW-1B Windows launch slices 1–4 are implemented as **spikes and test tooling**. No production provider, package, workflow or qualified security boundary exists yet; several GW-1B legs are deliberately red pending engine findings. Start with the [handoff](docs/handoff.md) and [current queue](docs/work-queue.md).

Gagamba accepts an explicit execution policy and uses a supported backend to confine a process and its descendants. It is independently useful and has no Penghou, Hufu, Luban, workflow, authorization-engine, or agent dependency.

## Current direction

- Windows, Linux, and macOS are target platforms. Start testing on the local Windows host, use WSL2 for early Linux work, and investigate GitHub-hosted macOS CI. Each platform needs independent evidence.
- Develop and qualify Gagamba alone. Any Hufu integration is deferred.
- Reject unsupported restrictions and backend failures before target execution. Never fall back to unrestricted execution.
- Start with one execution and its process tree per sandbox, immutable policy, explicit filesystem access, and denied network.
- Treat endpoint mediation, persistent sessions, live changes and OpenShell as later extensions. macOS is in scope, with backend selection subject to research.
- Prove the platform mechanisms before freezing a package family or public API.

Standalone scope, three-platform scope and local-first testing are user-selected. The offline-process-v1 baseline is now defined for implementation preparation; backend choices and public APIs remain open.

## Implementation and qualification

The [implementation plan](docs/implementation-plan.md) is the canonical work queue, from policy definition and local Windows probes through Linux/macOS backends and package qualification. The [testing and CI plan](docs/testing-and-ci.md) defines conformance cases, runner coverage, required checks and release gates. The [security model](docs/security-model.md) is the offline-process-v1 design baseline. The [fixture contract](docs/fixture-protocol.md) defines the private test tooling. The Windows provider direction and its open findings are recorded in [ADR 0001](docs/adr/0001-windows-provider.md) and the [evidence summaries](docs/evidence/).

## Reading order

1. [Feasibility review](docs/feasibility-review.md): verdict, corrections, and implementation risks.
2. [Standalone design draft](docs/design.md): proposed semantics and boundary.
3. [Research plan](docs/research-plan.md): Windows, Linux, and macOS experiments and acceptance gates.
4. [Decision log](docs/decisions.md): selected direction, recommendations, and open questions.
5. [Sources](docs/sources.md): primary references and evidence limits.
6. [Original proposal](docs/proposals/2026-10-03-original-proposal.md): preserved input, including deferred integration ideas.

The original proposal is historical input. Its integration roadmap and illustrative APIs are not implementation instructions. Current working scope is this standalone design and research plan.

## Next work

GP-1A/GP-1B fixture self-tests and the GW-1A probe are green. GW-1B is mid-slice: denial, descendants, cancel races, pipes and supervisor sweep are demonstrated, but four legs remain red pending engine findings — descendant stop on root exit, STARTUPINFO/file-handle transport, `dotnet` runtime compatibility, and granting the Git installation tree. See the [current queue](docs/work-queue.md), the [slice-4 evidence](docs/evidence/windows-slice4-GW-1B.md) and [ADR 0001](docs/adr/0001-windows-provider.md). Next: resolve or narrow those findings, then proceed to GL-1A (separate Ubuntu WSL2) and GM-1A (macOS probe). See the [test environment plan](docs/test-environments.md).

This repository contains planning documents, private test tooling (`tests/Fixtures/`), feasibility spikes (`spikes/`), local entrypoints (`eng/`) and a read-only verification script. It is connected to [GitHub](https://github.com/jenolaszlo-sketch/gagamba); the initial license is preserved. No packages, CI workflows or host security changes exist yet.

## Verify preparation on Windows

From the repository root, use a fresh Windows PowerShell process:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify-preparation.ps1
```

For PowerShell 7, substitute `pwsh` for `powershell.exe`. From another working directory, provide the script's full path; its repository root is resolved from the script location. An explicit `-RepositoryRoot` override is also supported.

Alternatively, in an existing PowerShell session:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
& .\eng\verify-preparation.ps1
```

These options apply only to the invoked process/session, without changing CurrentUser or LocalMachine policy. Managed policy can take precedence; do not change machine policy to run this check. The verifier reads files only and does not establish sandbox or CI qualification.

## Local entrypoints

All build Release output and write a versioned JSON evidence report under ignored `artifacts/`; each exits non-zero unless its aggregate is `Passed`. Windows PowerShell examples (add `-Configuration Debug` to change config):

| Entrypoint | Purpose |
| --- | --- |
| `eng/verify-preparation.ps1` | Read-only check of preparation artifacts, doc links and the preserved proposal hash |
| `eng/fixture-selftest.ps1` | Build + unit-test + run the GP-1A/B fixture harness (self-test only; no sandbox) |
| `eng/probe.ps1` | GW-1A Windows availability probe (export/schema inventory; does not launch) |
| `eng/launch-spike.ps1` | GW-1B launch staircase (experimental Windows API; disposable identities, deleted afterwards) |

Bash equivalents (`eng/*.sh`) mirror these on Linux/macOS. `eng/launch-spike.sh` intentionally refuses off-Windows. `eng/probe.sh` runs cross-platform (Windows-only legs report `Unsupported`).

