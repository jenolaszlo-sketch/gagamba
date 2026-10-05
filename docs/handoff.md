# Gagamba handoff

Updated 2026-10-05 for GR-0 (conformance + provider selection) on top of GM-2, GL-2, GW-2, GP-3.

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
- GW-1B-L5 git resolution: the staged-closure pattern is proven (staged `git --version` exits 0 in-sandbox) but every other git command dies `128` with `fatal: Unable to read current working directory: Permission denied` — ancestor dirs (`C:\`, profile) deny list access under AppContainer, confirmed by an elevated ProcMon capture. Fixed the harness pipe-drain gap (`RunPipedAsync` now drains when a child ran) that had hidden the fatal; trimmed the leg's bisect scaffolding. **Closed**: with authorized broad ro grants, ro `C:\` alone turns the full workload green (true HEAD + clean status); the profile tree is unbindable and poisons any spec containing it. Provider rule: ro drive-root grants, closures outside profile trees.
- GW-1B-L6 dotnet resolution: a new `L3-WORKLOAD-PIDPROBE` leg (manifest v6 = 20 IDs) stages a self-contained probe that exits 0 in-sandbox. Self-introspection works, but only `[System Process]` + self are visible; `GetProcessById(parent)` throws the verbatim dotnet `ArgumentException`. The CLI installer probe kills it, not the runtime — provider direction is self-contained managed closures.
- GW-1B-L7 runner topology (per spec): `L5-SANDBOX-PARENT` (manifest v7 = 21 IDs) proves nested creation, same-domain visibility, nested confinement, `--list-sdks`/`--list-runtimes` and offline `build` under a generic resident runner. The package-readability hunt closed on a broken fixture (missing `using`, host fails too); `dotnet test` hangs on TCP-loopback handshake denied by the offline profile (policy, not quirk). Model adopted: sandbox execution domain → resident runner → tool tree; environment is a granted resource.
- GW-1B-L8 grant-shape rule (`L1-GRANT-SHAPE`, manifest v8 = 22 IDs): multiple explicit grants bind; canonical forms bind identically; duplicates collapse + cross-kind conflicts reject in preparation; nested is inner-wins. Prior same-kind rejections do not reproduce as rules — INVALID_DATA is tuple-dependent, retry-fresh stands. Normative text in the security model.
- CI-1 workflow exists: `Gagamba.sln` (9 projects incl. `Gw1bLaunch.Tests`) + `.github/workflows/ci.yml` (verify/build/unit/fixtures/probe/spike-smoke); spike gate asserts run-to-completion + valid evidence, not aggregate Passed. Remote signal pending first push run.
- GQ-1 job ownership proven: `L5-JOB-OWNERSHIP` (manifest v9 = 23 IDs), all 8 acceptance phases green (terminate, close-kill, exit-code proof, crashed-supervisor cleanup, nesting incl. self-jobbed hosts, incompatible-assign handling). J8 denial path unit-covered (invalid-handle forcing). Contract: no descendant outlives the domain via parent exit or supervisor crash.
- GL-1A Linux lifecycle matrix green (`spikes/Gl1aLife`, shell+python only, no .NET/gcc): PG addressable but no owner-death/containment; PDEATHSIG direct coupling only (root-only vs cascade); cgroup v2 recursive kill but no auto owner-death; L14 watchdog composition works as constructed ownership. GP-2 contract extraction comes after GM-1A, never before.
- GM-1A macOS verdict (`spikes/Gm1aLife`, `macos-latest` arm64): PG works; launchd cleans same-PG on job death (M7) but escapees survive (M8); job outlives its client (M10); `bootout` cleans, `stop` doesn't (M9); watchdog+PG composes without containment (M11/M12). macOS primitive weakest of the three — GP-2 must be capabilities-based, never `IProcessJob`.
- GP-2 capabilities contract extracted (`src/Gagamba.Execution` + tests): six capabilities, Absent/Partial/Full × Native/Constructed/None, per-platform matrix with evidence, required/preferred negotiation, fail-closed, no `IsSandboxed` (mechanically enforced). Strict coding agent: Windows only. Owner-death: native Windows, composed elsewhere (never equated).
- GP-3 provider SPI defined (`IExecutionProvider` + opaque handles, 13 tests): negotiation → preparation → launch → lifecycle control → disposal, issuance validation, disposal rules.
- GW-2 Windows provider live (`src/Gagamba.Execution.Windows` + 10 tests): suspend-assign-resume, kill-on-close, exclusive env, single-use tokens, idempotent terminate, opaque handles; tree-kill/root-exit/dispose/env/foreign/probe coverage without PIDs. GP-3 survived contact unchanged. Next: GL-2/GM-2 providers, then conformance.
- GL-2 Linux provider live (`src/Gagamba.Execution.Linux` + 19 tests, green as root in WSL): `posix_spawn`+SETCGROUP atomic placement (`CLONE_INTO_CGROUP`), `cgroup.kill` terminate/dispose, `addchdir_np` workdir, exclusive env, single-use tokens, opaque handles. Two from-memory libc constants were wrong and failed SILENTLY (`O_DIRECTORY` 0x4000 vs real 0x10000; SETCGROUP 0x40 is USEVFORK, real bit 0x100). Since a wrong bit fails silently, first `Prepare` runs a live placement self-test (sacrificial sleeper must be BORN in `cgroup.procs`) and rejects otherwise. Evidence `docs/evidence/linux-provider-GL-2.md`.
- GM-2 macOS provider live (`src/Gagamba.Execution.MacOS` + 20 tests, green on `macos-latest`): unique launchd job per execution, bootstrap→kickstart→`print` readiness, `bootout` termination/cleanup (never `stop`), no KeepAlive/AbandonProcessGroup, WorkingDirectory, exclusive env, opaque handles, no PID use, escape observed-not-killed (setsid must come from a non-leader; launchd session-isolates job roots). Environment pressure point resolved by measurement: plist `EnvironmentVariables` gives granted-present + ambient-absent (launchd adds OS session vars — documented delta, GP-3 unchanged, no trampoline). Evidence `docs/evidence/macos-provider-GM-2.md`.
- GM-2 CI runner root cause: the Windows provider tree tests declared a 2-variable env (SYSTEMROOT/SYSTEMDRIVE) that works locally but NOT on `windows-latest`, where PowerShell 5.1 stalls during startup module analysis unless `PSModulePath` (or a module-cache path) is present — cmd.exe tolerates a bare block, PowerShell 5.1 does not. The provider was correct (it passes the spec exactly); the test spec was unrealistic. Fix: `TestEnv()` declares the system variables the PowerShell fixture genuinely needs. Diagnosed via a temporary `gw2-diag` workflow (now removed).
- GP-3 FROZEN. Three materially different providers (Windows Job Objects, Linux cgroup v2, macOS launchd+PG) exercised the SPI with zero signature changes; no extension without a downstream consumer proving a gap.
- GR-0 conformance + provider selection (`src/Gagamba.Conformance`, `src/Gagamba.Runtime`): one expected matrix + one runner over the frozen SPI (`tests/Gagamba.Conformance.Tests`, run per OS in CI); `ExecutionRuntime.Create()` selects the OS provider and offers requirement-driven launch. Environment guarantee restated portably (ambient not inherited; requested entries provided; platform-owned domain vars may additionally exist) without complicating GP-3. Matrix + semantic-probing doctrine in `docs/conformance.md`. Constructed owner-death cleanup stays out of native providers/runtime.
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

Continue **contracts + Windows provider done (GP-2/GP-3/GW-2)**:
next is GL-2/GM-2 providers against the same SPI, then GR-1 conformance. Decided, no further experiments needed: loopback as `offline+loopback`
profile capability, dotnet SCM probing restricted, msys ro drive-root
grant transitional, multi-grant prep semantics, kernel job-object
ownership, watchdog compositions constructed-not-kernel. Keep identities
disposable and profiles deleted; never modify the host opportunistically.

Before executing experiments that alter host ACLs/users/firewall or install a runtime, prepare the exact setup/cleanup implementation and inspect the task's authorization. No permission prompt is required merely to write code, read state or run safe fixtures. Keep privileged setup separate and attributable.

## Resume prompt

> Continue standalone Gagamba in C:\Users\Laszlos\source\repos\Gagamba. Read AGENTS.md, docs/handoff.md, docs/work-queue.md, docs/security-model.md and docs/fixture-protocol.md first. All three platforms evidenced: Windows job-object ownership (GQ-1), Linux lifecycle matrix (GL-1A), macOS lifecycle verdict (GM-1A: launchd cleans same-PG only, escape survives, watchdog+PG composes). Next is GP-2 capabilities-based contract extraction. Keep public APIs provisional, all restrictions fail-closed, and Hufu/Luban integration deferred. Do not mistake fixture self-tests, upstream examples or Docker's outer restrictions for Gagamba enforcement. Inspect the repository state and preserve the original proposal.

## Validation of this preparation

Documentation link resolution and original-proposal SHA-256 equality are checked by `eng/verify-preparation.ps1`. The script is read-only and can be rerun without backend setup. Use `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\eng\verify-preparation.ps1` from the repository root; see the [README invocation options](../README.md). Root resolution occurs in the script body for Windows PowerShell -File compatibility. It checks required prep artifacts and basic gate consistency; it does not validate sandbox behavior or prove the design secure.

Verifier regression checks passed on 2026-10-03: Windows PowerShell -File with default/explicit root, call-operator invocation, PowerShell 7 -File with default/explicit root, and missing/empty root rejection (seven cases). Default-root tests ran outside the repository. Documentation links and the original proposal hash still pass. These are preparation-tool checks only.

No backend, package, or CI conformance tests apply yet because no provider exists. The user created the remote and it is connected locally. GW-1B slice-4 code is uncommitted on top of `4b4ce8f`; commit it before further engine work so the source manifest pins exactly. CI activation, distro installation and package publication remain pending.
