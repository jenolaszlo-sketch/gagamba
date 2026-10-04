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
