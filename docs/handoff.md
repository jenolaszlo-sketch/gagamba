# Gagamba handoff

Updated 2026-10-03 for GP-1A completion. Start the next session in `C:\Users\Laszlos\source\repos\Gagamba`.

## User direction

Develop Gagamba alone as an independently useful .NET sandbox library. Windows, Linux and macOS are target platforms. Start locally on the user's Windows host; Linux may use WSL2/Docker and macOS may use GitHub CI. Hufu integration and replacing Luban remain deferred. The latest request prioritized preparation and handoff.

## Repository checkpoint

- Local Git repository is connected to `https://github.com/jenolaszlo-sketch/gagamba.git` as `origin`. Preparation baseline `7bdb28f690a4b7988876ec463b3e77254dc075fe` was committed and pushed on main, preserving initial license commit `fcc2fdc`. Local and remote main were verified to match before the verifier-entrypoint fix. The older staged-only report is superseded. Use git log, git status and git ls-remote origin refs/heads/main for the current checkpoint, since follow-up fixes advance it.
- Original proposal is preserved byte-for-byte under `docs/proposals/2026-10-03-original-proposal.md`; it is historical input, not the current implementation queue.
- Preparation delivered the security baseline, private fixture/evidence protocol, activity queue, implementation/testing plans and repository guidance.
- GP-1A implemented (uncommitted working tree on top of `8eee0fd`): `tests/Fixtures/Gagamba.Fixture.Worker|Host|Harness|SelfTest`, `eng/fixture-selftest.ps1|.sh`, evidence summary `docs/evidence/fixture-selftest-GP-1A.md`, raw JSON under ignored `artifacts/`. No backend provider, native helper, NuGet package, installed distro or CI workflow exists. No sandbox has been launched or qualified.
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

Verified GP-1A 2026-10-03 (fixtures only, not enforcement): Release `eng/fixture-selftest.ps1` — 13/13 unit + 16/16 harness Passed, aggregate Passed, cleanup Confirmed. Run `20261003-154524-5ec79312` on Windows 10.0.26200 win-x64 NTFS, standard-user, .NET 10.0.12, source commit `8eee0fd` dirty with SHA-256 manifest. Bounds shown: 16 KiB protocol, 64 KiB stdin, 1 MiB output caps, 3 s hang kill, workspace escape rejected. See `docs/evidence/fixture-selftest-GP-1A.md`.

## Exact next task

Implement **GP-1B next** (F3: deterministic child/grandchild, early root exit, barrier and ownership checks plus worker self-tests). **GW-1A availability/minimal-launch probing** (Windows export/schema, minimal launch, captured I/O) may proceed in parallel; its containment/lifetime qualification requires GP-1B. Do not claim any provider, package, or cross-platform conformance yet.

Before executing experiments that alter host ACLs/users/firewall or install a runtime, prepare the exact setup/cleanup implementation and inspect the task's authorization. No permission prompt is required merely to write code, read state or run safe fixtures. Keep privileged setup separate and attributable.

## Resume prompt

> Continue standalone Gagamba in C:\Users\Laszlos\source\repos\Gagamba. Read AGENTS.md, docs/handoff.md, docs/work-queue.md, docs/security-model.md and docs/fixture-protocol.md first. GP-1A is complete with passing Release fixture self-tests (16/16 harness + 13/13 unit, cleanup Confirmed; fixtures only, no backend qualified). Implement GP-1B next: deterministic child/grandchild fixtures, early-exit/barrier/ownership checks and self-tests. GW-1A availability probing may proceed in parallel but claims no provider. Keep public APIs provisional, all restrictions fail-closed, Windows local testing first, Linux/macOS in scope, and Hufu/Luban integration deferred. Do not mistake fixture self-tests, upstream examples or Docker's outer restrictions for Gagamba enforcement. Inspect the repository state and preserve the original proposal.

## Validation of this preparation

Documentation link resolution and original-proposal SHA-256 equality are checked by `eng/verify-preparation.ps1`. The script is read-only and can be rerun without backend setup. Use `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify-preparation.ps1` from the repository root; see the [README invocation options](../README.md). Root resolution occurs in the script body for Windows PowerShell -File compatibility. It checks required prep artifacts and basic gate consistency; it does not validate sandbox behavior or prove the design secure.

Verifier regression checks passed on 2026-10-03: Windows PowerShell -File with default/explicit root, call-operator invocation, PowerShell 7 -File with default/explicit root, and missing/empty root rejection (seven cases). Default-root tests ran outside the repository. Documentation links and the original proposal hash still pass. These are preparation-tool checks only.

No backend, package, or CI conformance tests apply yet because no provider exists. The user created the remote and it is connected locally. GP-1A code is uncommitted; commit it before backend spikes so the source manifest (commit + dirty flag + SHA-256) pins exactly. CI activation, distro installation and package publication remain pending.
