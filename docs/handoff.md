# Gagamba handoff

Prepared 2026-10-03. Start the next session in `C:\Users\Laszlos\source\repos\Gagamba`.

## User direction

Develop Gagamba alone as an independently useful .NET sandbox library. Windows, Linux and macOS are target platforms. Start locally on the user's Windows host; Linux may use WSL2/Docker and macOS may use GitHub CI. Hufu integration and replacing Luban remain deferred. The latest request prioritized preparation and handoff.

## Repository checkpoint

- Local Git repository is connected to `https://github.com/jenolaszlo-sketch/gagamba.git` as `origin`. Remote `main` starts at `fcc2fdc6860482308b2eea34d39e91d169d90cda` (Initial commit, LICENSE only). Local `main` adopts that history and preserves the license. This handoff accompanies the preparation commit on main; use git log, git status and origin/main to identify the exact current checkpoint before making changes.
- Original proposal is preserved byte-for-byte under `docs/proposals/2026-10-03-original-proposal.md`; it is historical input, not the current implementation queue.
- Preparation delivered the security baseline, private fixture/evidence protocol, activity queue, implementation/testing plans and repository guidance.
- No runtime source, native helper, worker, .NET project, NuGet package, installed distro or CI workflow exists. No sandbox has been launched or qualified.
- Current shell chat may still be rooted in Solo. Use the Gagamba path explicitly; Solo's planning staging files are not the source of truth. Prefer opening the next coding session directly in Gagamba so its workspace permissions match the work.

## Read in this order

1. [Current work queue](work-queue.md) — actual ready/held statuses.
2. [Security model](security-model.md) — normative offline-process-v1 baseline for the spike.
3. [Fixture contract](fixture-protocol.md) — first coding slice and evidence semantics.
4. [Implementation plan](implementation-plan.md) and [testing/CI plan](testing-and-ci.md).
5. [Test environments](test-environments.md), then detailed [research plan](research-plan.md) as needed.

The work queue refines the broader milestone order. The security model refines the draft design. Original proposal examples do not override either. If experiments disprove a chosen mechanism, document the finding and select another mechanism; do not silently broaden permissions.

## Verified versus unverified

Observed earlier on this machine: .NET SDK 10.0.401, Windows registry build 26200.9457/display 25H2, and processmodel.dll version 10.0.26100.9444. The registry's legacy product label differed; this does not qualify the API.

WSL default version is 2; its only listed distribution was running `docker-desktop`. Docker Desktop 4.75.0 and engine 29.5.2 reported Linux amd64 kernel 6.6.114.1-microsoft-standard-WSL2. There is no separately listed Ubuntu distribution. Read-only inventory initially failed under the assistant wrapper, then succeeded with an approved external read; this was not a broken Docker/WSL installation.

Unverified: Windows export/schema and sandbox launch, Linux namespaces and seccomp, macOS runner execution, reliable termination on every backend, actual read/network denial, and developer-workload compatibility. Upstream documentation and upstream CI configuration are research sources, not our test evidence.

## Exact next task

Implement **GP-1A only as the first coherent coding slice**:

1. Inspect repository status and root guidance, preserve existing docs, and use .NET 10 for the test tooling.
2. Create the minimal fixture host/worker and `eng` entrypoint described in fixture-protocol.md. Avoid empty public package projects.
3. Implement bounded read/write/exit/stdin/stdout/stderr operations, explicit Ready/Continue sequencing, malformed-input rejection, time limits, synthetic sentinels, independent cleanup and positive controls.
4. Implement the versioned report collector/validator and a FixtureSelfTest evidence record from actual runs. Enforce required IDs and no empty passing reports.
5. Run local Release self-tests, record exact source identity including dirty-tree input hashes where there is no commit, and update GP-1A status with concrete evidence.
6. Continue with GP-1B or GW-1A as appropriate. Windows availability/minimal-launch probes can follow GP-1A; actual process-tree claims require GP-1B and backend tests.

Before executing experiments that alter host ACLs/users/firewall or install a runtime, prepare the exact setup/cleanup implementation and inspect the task's authorization. No permission prompt is required merely to write code, read state or run safe fixtures. Keep privileged setup separate and attributable.

## Resume prompt

> Continue standalone Gagamba in C:\Users\Laszlos\source\repos\Gagamba. Read AGENTS.md, docs/handoff.md, docs/work-queue.md, docs/security-model.md and docs/fixture-protocol.md first. Preparation is complete through the GP-0 design baseline; no backend, worker or CI is implemented or qualified. Start GP-1A: a minimal .NET 10 controlled fixture host/worker, bounded I/O, reliable positive controls, watchdog/cleanup and strict versioned evidence validation. Use synthetic disposable fixtures and record actual Release self-test evidence. Then update the queue and handoff. Keep public APIs provisional, all restrictions fail-closed, Windows local testing first, Linux/macOS in scope, and Hufu/Luban integration deferred. Do not mistake fixture self-tests, upstream examples or Docker's outer restrictions for Gagamba enforcement. Inspect the repository state and preserve the original proposal.

## Validation of this preparation

Documentation link resolution and original-proposal SHA-256 equality are checked by `eng/verify-preparation.ps1`. The script is read-only and can be rerun without backend setup. It checks required prep artifacts and basic gate consistency; it does not validate sandbox behavior or prove the design secure.

No build or runtime tests apply yet because no implementation exists. The user created the remote and it is connected locally. CI activation, distro installation and package publication remain pending.
