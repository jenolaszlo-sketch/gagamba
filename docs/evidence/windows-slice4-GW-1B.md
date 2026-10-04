# GW-1B slice 4 evidence: encoder, crash recovery, stability, workloads

Date: 2026-10-04. Raw report: `artifacts/windows-launch-20261004-040446-4d715686.json`
(ignored raw dir; this file is the retained summary). Extends slices 1–3
(`windows-minimal-launch-GW-1B.md`, `windows-denial-tree-io-GW-1B.md`,
`windows-pipes-supervisor-workloads-GW-1B.md`). Manifest v5 = 19 mandatory IDs.

Result: **14/19 mandatory Passed**, 4 Failed (open engine gates) and 1 NotRun
(`L4-WORKLOAD-BUILD`, gated on the dotnet runtime finding). The aggregate is
correctly `Failed`; the red legs are findings, not harness faults.

- Entrypoint: `eng/launch-spike.ps1` (Release). Spike: `spikes/Gw1bLaunch`.
- Provider direction and tradeoffs: [ADR 0001](../adr/0001-windows-provider.md).

## Proven this slice

- **Spec encoder byte-exact vs official `flatc` 25.12.19** on all supported
  shapes (bare, app, one rw grant, two ro grants, capabilities). Reference
  vectors are vendored at `spikes/Gw1bLaunch/testdata/flatc-*.bin`
  (`SOURCES.md`); L1-SPEC-BUILD asserts byte equality and rejects the negative
  cases (wrong version; corrupt file id). A fail-closed local check also
  rejects grants/capabilities when `app_container=false` before dispatch.
- **L4-CRASH-RECOVERY**: the launcher closes every handle at release (no
  wait/terminate/query retained) and recovery uses only the recorded PID. The
  sleeper is verified alive before the sweep (engine does not auto-stop), the
  supervisor kills it, and the root is killed by PID. Passes deterministically.
- **L4-RETRY-PROOF (launch stability)**: the same single-grant shape launches
  4/4 across fresh identities and workspaces; a transient `ERROR_INVALID_DATA`
  is detected and retried on fresh inputs. No fallback to unconfined execution.
- **L1-CLEANUP**: 16/16 legs reaped, per-user profiles removed or never
  materialized (registry-Moniker proof), workspaces deleted.

## Open gates (red, by design)

- **L2-TREE-STOP — engine does not stop descendants.** After the root is killed
  and (separately) after the root exits naturally, the `start /b` sleeper still
  completes. Compensated by the proven supervisor sweep, not closed. Kernel-
  backed ownership beyond the supervisor remains unproven.
- **L2-STDIO-REDIRECT — file-handle transport rejected.** `STARTF_USESTDHANDLES`
  with file handles (single, inheritable, and all-three) returns
  `ERROR_INVALID_DATA`. Anonymous **pipes** work (L3-PIPE-STDIO) and are the
  supported capture mechanism.
- **L3-WORKLOAD-DOTNET — runtime compatibility, not path closure.**
  `whoami.exe` runs with workspace-only grants, so the base image covers
  System32. But `dotnet --info` launches (`api=True`) and exits 1 with empty
  stdout **and** stderr in every configuration tried: workspace-only,
  `+dotnetDir`, full closure, `registryRead`, and
  `+dotnetDir+system32+registryRead`. The `registryRead` capability encodes
  byte-exact and the engine accepts it, but does not change the outcome. This
  is Low-integrity/AppContainer runtime friction or a missing capability; it
  also gates `L4-WORKLOAD-BUILD` (NotRun).
- **L4-WORKLOAD-GIT — the Git installation tree cannot be granted.** Granting
  `C:\Program Files\Git` (and each of its subdirectories), or even
  `C:\Program Files` itself, returns `ERROR_INVALID_DATA`, deterministically
  across runs and processes. `C:\Program Files\dotnet`,
  `C:\Program Files\Common Files`, `C:\Windows`, and a copy of Git's contents
  placed under `C:\temp` are all accepted. No reparse points are present in the
  Git tree. The mechanism is unknown; the practical consequence is that the
  system Git install is currently unbindable, so the Git workload cannot run
  under the sandbox without copying a Git runtime into a grantable root.

## Grant-shape and error findings

- **Multi-grant under `%TEMP%`.** Two grants of the **same kind** under
  `%TEMP%` (two ro, or two rw) are rejected with `ERROR_INVALID_DATA`
  (`S0`/`S0b`), while the equivalent shape under `C:\temp` is accepted
  (`flatc-ro2`). A mixed rw+ro pair under `%TEMP%` is accepted
  (`L2-READONLY-DENIAL`). The `%TEMP%` tree carries foreign and unknown-SID
  ACEs (`CodexSandboxUsers`), absent from `C:\temp`; suspect ACL/BFS merge,
  mechanism unconfirmed. Workload legs now collapse redundant nested grants.
- **Intermittent rejection.** A single-grant temp launch occasionally returned
  `ERROR_INVALID_DATA` once and then succeeded on a fresh identity+workspace;
  the stability leg exercises and documents this. The provider must treat it as
  transient and retry, never degrade.
- **`ERROR_ALREADY_EXISTS` (0xB7)** was observed when a manual run re-used an
  identity that still had a live profile. Fresh identity per attempt is
  mandatory; cleanup is idempotent.

## Review fixes applied alongside this slice

- Crash-recovery leg ordering bug: it opened the barrier (letting the sleeper
  finish) before sweeping, so the sweep always found nothing. Reordered and
  re-based on recorded-PID recovery only.
- `Supervisor`: replaced a full per-process creation-time scan of every
  process with a single cheap ToolHelp snapshot; creation time is now read from
  the terminate handle (PID-reuse safe) instead of a stale snapshot; diagnostic
  table no longer truncates the focus PID.
- `SpecBuilder`: removed dead code; added the fail-closed `app_container`
  precondition; canonical byte-exact layout retained.
- Fixtures: `FixtureWorkspace.DisposeAndReport` no longer short-circuits to
  `Confirmed`; `TreeRunner` kill re-checks PID start time and tolerates
  mid-write node records; `FixtureRunner` drains streams before classifying
  output overflow and kills the child on caller cancellation; the worker
  resolves its relaunch entry (apphost `.exe` or `dotnet .dll`) and writes node
  records atomically; `FixtureProtocol` rejects wrong-typed fields cleanly;
  `Evidence.TryGit` drains both pipes under a timeout.
- `eng/probe.sh` and `eng/fixture-selftest.sh` now fail closed on a non-Passed
  aggregate / wrong evidence kind, without a `python3` dependency.

## Git silent-128 resolved: ancestor list-access + harness drain gap

An elevated ProcMon capture of the staged-Git workload leg (captured by the
user, exported offline, analyzed from the CSV) closed the silent-`128`
mystery; a rerun with fixed pipe draining confirmed the mechanism:

- Every git command except `--version` resolves the cwd by walking ancestors.
  Under AppContainer, opening `C:\` (and `C:\Users\Laszlos`) as a directory
  returns `ACCESS DENIED` (`CreateFile`, `Read Data/List Directory`).
- msys reports `EACCES` from `getcwd()` and git dies before any repo/config
  work: `fatal: Unable to read current working directory: Permission denied`
  (67 bytes on the pipe), exit `128`. This explains why `config --list`,
  empty repos, `GIT_CONFIG_NOSYSTEM`, `HOME` redirection and
  `safe.directory=*` all failed identically, and why `GIT_TRACE` stayed
  silent (death precedes trace init). `--version` never touches the cwd.
- The message never reached evidence because `RunPipedAsync` skipped draining
  both pipes whenever the exit code mismatched the expectation. Fixed to
  always drain when a child ran (only API/spec rejections skip); the rerun
  leg reports the fatal for both `rev-parse` and `status`. Bisect scaffolding
  (config/empty-repo/strace probes, env overrides) was removed from the leg
  after serving its purpose.
- Adjacent: msys `strace` starts (`--version` exits 0) but dies with
  `0xC0000005` writing zero bytes the moment it traces any in-sandbox target;
  in-sandbox debug/spawn primitives are unusable, so black-box tracing ends
  here. Event logs (`Application` WER, `AppModel-Runtime/Admin`) show only
  normal container lifecycle; no crash records.
- Provider consequence: Git workloads need either list-access grants on the
  full ancestor chain of the cwd (including the drive root, currently
  untested) or a cwd confined under a fully grantable subtree. Open as
  GW-1B-L5; the staged-closure pattern (copy under `C:\temp`) itself is
  proven: the staged `git --version` exits 0 in-sandbox.

## GW-1B-L5 closed: read-only C:\ grant unblocks the Git workload

With explicit authorization for broad read-only grants, the L4 leg now
tries ancestor variants (fresh ws + repo copy each): ro `C:\`, ro profile
(`C:\Users\Laszlos`), both, and the full chain (gated on narrower
failure). Result:

- ro `C:\` alone: **full workload green** — `rev-parse HEAD` returns the
  true HEAD, `status --porcelain` exits 0 with empty stderr. L4 passes.
- ro profile: engine rejects the whole spec with `ERROR_INVALID_DATA`
  (all three launches, `deleted-never-materialized`) — the user-profile
  tree is unbindable, same family as the `C:\Program Files\Git` finding.
  Any spec containing it fails wholesale (root+profile too).
- Full chain unnecessary (skipped by gating).
- The drive root itself binds fine; only profile subtrees refuse.

Provider rule: msys/cygwin workloads get ro grants on the drive root for
cwd resolution (read-only, no writes); user-profile subtrees cannot be
granted at all, so closures must live outside them. No confinement
alternative needed for git.
- Side effect of the drain fix: the dotnet leg now reports its first real
  error instead of `err=''`: `System.TypeInitializationException` in
  `Microsoft.DotNet.Cli.Installer.Windows.InstallerBase` caused by
  `Process.GetProcessById` failing ("Process with an Id ... is not
  running"). New L3 lead, not yet investigated.

## Dotnet exit-1 mechanism: parent-PID query (L3-WORKLOAD-PIDPROBE)

A new mandatory leg (`L3-WORKLOAD-PIDPROBE`, manifest v6 = 20 IDs) stages a
self-contained probe (`spikes/PidProbe`) and asks which BCL introspection
works in-sandbox:

- Managed code runs: the staged probe exits 0 in-sandbox (grants rw=[ws],
  ro=[staged]); runtime startup needs nothing beyond the staged closure.
- `GetCurrentProcess`, `GetProcessById(self)` and `MainModule` all succeed.
- The visible process set is exactly two: `[System Process]` and self
  (`GetProcesses`/ToolHelp count=2 vs ~405 unsandboxed).
- The parent (medium-IL runner outside) is invisible:
  `GetProcessById(parent)` throws `ArgumentException: Process with an Id of
  41272 is not running` — verbatim the dotnet failure shape.
- Mechanism: the dotnet CLI installer probe queries an outside PID at
  startup and dies in its static constructor; the runtime itself is healthy.
- Provider consequence: prefer self-contained managed closures over the CLI
  host for sandboxed workloads; the `dotnet` CLI stays red. Open follow-up:
  framework-dependent launch without the CLI host probe.
