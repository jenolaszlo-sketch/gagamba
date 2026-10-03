# Gagamba

Standalone .NET execution sandbox library — architecture and feasibility work.

Status: preparation complete through the GP-0 design baseline, 2026-10-03. No sandbox implementation or qualified security boundary exists yet. Start with the [handoff](docs/handoff.md) and [current queue](docs/work-queue.md).

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

The [implementation plan](docs/implementation-plan.md) is the canonical work queue, from policy definition and local Windows probes through Linux/macOS backends and package qualification. The [testing and CI plan](docs/testing-and-ci.md) defines conformance cases, runner coverage, required checks and release gates. The [security model](docs/security-model.md) completes GP-0 as a design baseline. The [fixture contract](docs/fixture-protocol.md) prepares GP-1A; runtime implementation and enforcement evidence remain pending.

## Reading order

1. [Feasibility review](docs/feasibility-review.md): verdict, corrections, and implementation risks.
2. [Standalone design draft](docs/design.md): proposed semantics and boundary.
3. [Research plan](docs/research-plan.md): Windows, Linux, and macOS experiments and acceptance gates.
4. [Decision log](docs/decisions.md): selected direction, recommendations, and open questions.
5. [Sources](docs/sources.md): primary references and evidence limits.
6. [Original proposal](docs/proposals/2026-10-03-original-proposal.md): preserved input, including deferred integration ideas.

The original proposal is historical input. Its integration roadmap and illustrative APIs are not implementation instructions. Current working scope is this standalone design and research plan.

## Next work

Implement GP-1A: the controlled fixture host/worker, positive controls, bounded I/O, watchdog and evidence validator. Then begin the local Windows mechanism probe. See the [test environment plan](docs/test-environments.md) for WSL2, Docker, and macOS CI. Do not create empty production packages solely to match the original directory diagram.

This repository contains planning/preparation documents and a read-only verification script. It is connected to [GitHub](https://github.com/jenolaszlo-sketch/gagamba); the initial license is preserved. No sandbox code, packages, workflows or host security changes exist yet. Run `./eng/verify-preparation.ps1` to check preparation artifacts.
