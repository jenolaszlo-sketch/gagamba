# Gagamba handoff

Updated 2026-10-04 for GW-1B slice 4 (encoder, crash recovery, stability, workloads) plus a review pass, then GW-1B-L5 (git-getcwd resolution, harness pipe-drain fix), GW-1B-L6 (dotnet exit-1 mechanism) and GW-1B-L7 (resident-runner topology verdict per experiment spec). Start the next session in `C:\Users\Laszlos\source\repos\Gagamba`.

## User direction

Develop Gagamba alone as an independently useful .NET sandbox library. Windows, Linux and macOS are target platforms. Start locally on the user's Windows host; Linux may use WSL2/Docker and macOS may use GitHub CI. Hufu integration and replacing Luban remain deferred. The latest request prioritized preparation and handoff.

## Repository checkpoint

- Local Git repository is connected to `https://github.com/jenolaszlo-sketch/gagamba.git` as `origin`. Preparation baseline `7bdb28f690a4b7988876ec463b3e77254dc075fe` was committed and pushed on main, preserving initial license commit `fcc2fdc`. Local and remote main were verified to match before the verifier-entrypoint fix. The older staged-only report is superseded. Use git log, git status and git ls-remote origin refs/heads/main for the current checkpoint, since follow-up fixes advance it.
- Original proposal is preserved byte-for-byte under `docs/proposals/2026-10-03-original-proposal.md`; it is historical input, not the current implementation queue.
- Preparation delivered the security baseline, private fixture/evidence protocol, activity queue, implementation/testing plans and repository guidance.
- GP-1A committed as `208c2b1`, GP-1B as `c7003f8` (fixture Worker|Host|Harness|SelfTest incl. F3 lifecycle, `eng/fixture-selftest.ps1|.sh`, evidence summaries, raw JSON under ignored `artifacts/`). No backend provider, native helper, NuGet package, installed distro or CI workflow exists. No sandbox has been launched or qualified.
- GW-1A implemented (`f973a73`): read-only `spikes/Gw1aProbe` + `eng/probe.ps1|.sh`, per-kind evidence manifests, verdict EXPORT-PRESENT-SCHEMA-UNPINNED. The launch API was resolved but never invoked; no provider claimed.
- GW-1B slice 1 implemented (`195d16f`): pinned schema + `PIN.md`, verified spec compiler (`spikes/Gw1bLaunch`), first launches under disposable identities. Denial/descendants/races/workloads/ADR were open.
- GW-1B slice 2 implemented (`61edee2`): denial with positive controls, descendant effects + inherited denial, cancel race proven; tree-stop gap and file-stdio rejection characterized.
- GW-1B slice 3 implemented (`4b4ce8f`): pipes proven as transport, ToolHelp supervisor proven cross-boundary, whoami maps the base image, dotnet exits 1 silently in all grant configs. Provider ADR 0001 written.
- GW-1B slice 4 implemented and reviewed: byte-exact spec encoder vs flatc (vendored vectors), crash-recovery by recorded PID, launch-stability/retry, grant-shape and Git-tree findings, and a review fix pass (crash-leg ordering, supervisor cost/PID-reuse, fixture bugs, eng-script false-green). 14/19 legs green; 4 open gates. Evidence `docs/evidence/windows-slice4-GW-1B.md`.
- GW-1B-L5 git resolution: the staged-closure pattern is proven (staged `git --version` exits 0 in-sandbox) but every other git command dies `128` with `fatal: Unable to read current working directory: Permission denied` — ancestor dirs (`C:\`, profile) deny list access under AppContainer, confirmed by an elevated ProcMon capture. Fixed the harness pipe-drain gap (`RunPipedAsync` now drains when a child ran) that had hidden the fatal; trimmed the leg's bisect scaffolding.
- GW-1B-L6 dotnet resolution: a new `L3-WORKLOAD-PIDPROBE` leg (manifest v6 = 20 IDs) stages a self-contained probe that exits 0 in-sandbox. Self-introspection works, but only `[System Process]` + self are visible; `GetProcessById(parent)` throws the verbatim dotnet `ArgumentException`. The CLI installer probe kills it, not the runtime — provider direction is self-contained managed closures.
- GW-1B-L7 runner topology (per spec): `L5-SANDBOX-PARENT` (manifest v7 = 21 IDs) proves nested creation, same-domain visibility, nested confinement, `--list-sdks`/`--list-runtimes` and offline `build` under a generic resident runner. The package-readability hunt closed on a broken fixture (missing `using`, host fails too); `dotnet test` hangs on TCP-loopback handshake denied by the offline profile (policy, not quirk). Model adopted: sandbox execution domain → resident runner → tool tree; environment is a granted resource.
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

Unverified: Linux namespaces and seccomp, macOS runner execution, actual read/network denial, and developer-workload compatibility (the Windows `dotnet` runtime is specifically blocked; Git works only past a new ancestor list-access gate, see below).

Verified GW-1A/GW-1B 2026-10-04 (Windows host, spike-only; no provider qualified): `eng/probe.ps1` 7/7 mandatory (`windows-probe-20261004-040252-0c70af09.json`); `eng/launch-spike.ps1` 14/19 mandatory with 4 open engine gates and 1 gated NotRun (`windows-launch-20261004-040446-4d715686.json`); `eng/fixture-selftest.ps1` 20/20 harness + unit Passed (`fixture-selftest-20261004-040228-5f9560f3.json`). Open Windows gates: engine never stops descendants on root exit/kill; STARTUPINFO file-handle transport rejected (pipes work); `dotnet` CLI exits 1 on its installer parent-PID probe (self-contained managed closures proven green); staged Git runs but every command past `--version` dies on ancestor list-access (`C:\`, profile) during cwd resolution. See `docs/evidence/windows-slice4-GW-1B.md`.

## Exact next task

Continue **GW-1B (engine findings)**: (1) test ancestor-chain list grants vs
cwd confinement under a grantable subtree for the Git workload (GW-1B-L5),
and record whether the drive root can be granted narrowly; (2) decide the
test-execution loopback question (VSTest needs 127.0.0.1 TCP; `build` is
green, `test` hangs by policy) and the SCM-probing stance (`--info` tail
only — classify CLI-core supported vs host-inspection restricted);
(3) confirm the multi-grant-under-`%TEMP%` rule and document it as a
preparation-time rejection; (4) design launcher-crash recovery beyond the
supervisor (GQ-1 gate). Then GL-1A (separate Ubuntu WSL2) and GM-1A (macOS
probe). Keep identities disposable and profiles deleted; never modify the
host opportunistically.

Before executing experiments that alter host ACLs/users/firewall or install a runtime, prepare the exact setup/cleanup implementation and inspect the task's authorization. No permission prompt is required merely to write code, read state or run safe fixtures. Keep privileged setup separate and attributable.

## Resume prompt

> Continue standalone Gagamba in C:\Users\Laszlos\source\repos\Gagamba. Read AGENTS.md, docs/handoff.md, docs/work-queue.md, docs/security-model.md and docs/fixture-protocol.md first. GW-1B slice 4 is complete and reviewed; GW-1B-L5 resolved the git silent-128 (ancestor list-access gate, staged closures proven, harness drain fixed); GW-1B-L6 resolved the dotnet exit-1 (parent-PID query in the CLI installer probe, self-contained closures green). Remaining Windows gates: descendant stop on root exit, file-handle stdio, dotnet CLI host, and Git ancestor grants. Keep public APIs provisional, all restrictions fail-closed, Windows local testing first, Linux/macOS in scope, and Hufu/Luban integration deferred. Do not mistake fixture self-tests, upstream examples or Docker's outer restrictions for Gagamba enforcement. Inspect the repository state and preserve the original proposal.

## Validation of this preparation

Documentation link resolution and original-proposal SHA-256 equality are checked by `eng/verify-preparation.ps1`. The script is read-only and can be rerun without backend setup. Use `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify-preparation.ps1` from the repository root; see the [README invocation options](../README.md). Root resolution occurs in the script body for Windows PowerShell -File compatibility. It checks required prep artifacts and basic gate consistency; it does not validate sandbox behavior or prove the design secure.

Verifier regression checks passed on 2026-10-03: Windows PowerShell -File with default/explicit root, call-operator invocation, PowerShell 7 -File with default/explicit root, and missing/empty root rejection (seven cases). Default-root tests ran outside the repository. Documentation links and the original proposal hash still pass. These are preparation-tool checks only.

No backend, package, or CI conformance tests apply yet because no provider exists. The user created the remote and it is connected locally. GW-1B slice-4 code is uncommitted on top of `4b4ce8f`; commit it before further engine work so the source manifest pins exactly. CI activation, distro installation and package publication remain pending.
