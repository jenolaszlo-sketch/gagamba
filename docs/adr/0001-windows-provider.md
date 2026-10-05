# ADR 0001: Windows provider — experimental API plus Gagamba supervisor

Status: accepted for continued prototyping (NOT production qualification).
Date: 2026-10-03; workload-visibility limitation added 2026-10-04. Evidence: `docs/evidence/windows-minimal-launch-GW-1B.md`,
`docs/evidence/windows-denial-tree-io-GW-1B.md`,
`docs/evidence/windows-pipes-supervisor-workloads-GW-1B.md`,
`docs/evidence/windows-slice4-GW-1B.md`.

## Context

Gagamba needs a Windows enforcement mechanism for `offline-process-v1`
(AppContainer isolation, explicit directory grants, denied network, owned
process tree, bounded I/O). Candidates: the experimental
`processmodel.dll` launch API, a hand-rolled restricted-token /
dedicated-user implementation (a la the OpenAI Codex design), or an external
runtime dependency.

## Evidence (all measured on 25H2 build 26200, processmodel 10.0.26100.9444)

Proven with the `Gw1bLaunch` staircase under disposable identities:

- Launch works: `Experimental_CreateProcessInSandbox` + `...AsUserInSandbox`
  resolve; AppContainer launches run with requested exit codes in ~50 ms.
- Fail-closed shapes: `app_container=false` rejected (`ERROR_NOT_SUPPORTED`);
  malformed specs rejected (`ERROR_INVALID_DATA`).
- Filesystem: ro read allowed / ro write denied; ungranted reads denied —
  each with unsandboxed positive controls. Descendants inherit restrictions.
- I/O: anonymous pipes transport stdout/stderr exactly; file handles and
  NUL-stdin configurations are rejected. File-effect oracles work throughout.
- Lifetime: root exit codes observed; cancel-by-terminate works (exit 99).
- Cleanup: per-user profiles removed (registry-Moniker proof), workspaces
  deleted, no residue in any run.

Gaps that shape the design:

- The engine does NOT stop descendants on root kill OR natural root exit
  (both variants measured). Tree ownership must live in Gagamba.
- `dotnet --info` launches but exits 1 in every grant configuration
  (ws-only through full closure), while `whoami.exe` runs fine with no
  grants at all. Resolved by GW-1B-L6 (see Workload limitation below): the
  CLI installer probe queries an outside PID; the runtime itself is healthy
  and staged self-contained closures exit 0. The `registryRead` capability
  variant was tried (slice 4) and did not change the outcome; that was
  expected in hindsight — the failure was never registry access.
- Some host directory trees cannot be granted at all: `C:\Program Files\Git`
  (and its subdirectories, and `C:\Program Files` itself) returns
  `ERROR_INVALID_DATA` deterministically, while `dotnet`, `Common Files`,
  `Windows`, and copies of Git's contents under `C:\temp` are accepted. The
  provider must validate grant roots at preparation and reject/fallback rather
  than assume a directory is bindable. Update (GW-1B-L5): the user-profile
  tree (`C:\Users\Laszlos`) is likewise unbindable and poisons any spec
  containing it; the drive root `C:\` itself binds fine read-only, and that
  single grant unblocks msys cwd resolution for the full Git workload.
- Some host directory trees cannot be granted at all: `C:\Program Files\Git`
  (and its subdirectories, and `C:\Program Files` itself) returns
  `ERROR_INVALID_DATA` deterministically, while `dotnet`, `Common Files`,
  `Windows`, and copies of Git's contents under `C:\temp` are accepted. The
  provider must validate grant roots at preparation and reject/fallback rather
  than assume a directory is bindable.
- Process-wide grant-shape fragility: two grants of the same kind under
  `%TEMP%` are rejected while the same shape under `C:\temp` is accepted
  (foreign ACEs on the `%TEMP%` tree are suspected). Preparation should
  validate the actual grant set and surface a typed rejection.
- Grant binding has a currently unexplained non-deterministic or
  content/state-dependent failure mode: grant totals 115/121/124 bind
  while 118 rejects and one 121-char workspace failed 6/6 across runs
  (same length, different names, opposite verdicts) — falsified as a
  length cap, not yet called random; a deterministic variable may remain
  unidentified. The spike retries with fresh identity+workspace as a
  defensive workaround, never as path validation.

## Execution domain (GW-1B-L7)

Gagamba does not sandbox a process; it creates a **sandbox execution
domain**:

```text
Sandbox Execution Domain
│
├── root process
│   ├── child
│   │   ├── grandchild
│   │   └── grandchild
│   └── child
│
├── filesystem grants
├── environment
├── network policy
└── resource limits
```

Processes within the domain have enough normal OS semantics to function
together (nested creation, parent/child discovery, inherited
confinement); processes outside it remain isolated. Authority propagates
downward, bounded by the parent: child authority ⊆ parent authority.
Hufu may attenuate further, but a child never gains authority by being
spawned — the provider gives the OS-level half (proven: nested children
are confined), the supervisor owns the rest.

Environment is a granted resource, not an inheritance: caller env does
not cross the boundary, so the resident runner constructs child env
explicitly (`--env`). The Hufu-facing direction is an explicit
`SandboxExecutionRequest { Executable, Arguments, WorkingDirectory,
Environment, Grants }` where allow/deny is per-variable (PATH yes,
API keys no).

## Workload limitation: the sandbox hides host objects (git + dotnet, same rule)

Both workload failures resolve to one principle, binding until disproven:
**inside the sandbox, host objects outside the grants are invisible, and
programs that resolve them die.** The binaries run; their environment
probes fail. Never re-investigate these as path-closure or capability
issues — check visibility first.

- Git (GW-1B-L5): every command except `--version` resolves the cwd by
  walking ancestors. `C:\` and `C:\Users\Laszlos` deny list-directory under
  AppContainer (ProcMon `CreateFile → ACCESS DENIED`), msys `getcwd()`
  returns `EACCES`, git dies with `fatal: Unable to read current working
  directory: Permission denied`, exit `128`. Nine black-box discriminators
  (empty repos, `--git-dir` forms, temp/`HOME`/config/ownership variants,
  tracing) all failed identically before the capture named it.
- dotnet CLI (GW-1B-L6): the runtime starts, but `InstallerBase` queries an
  outside PID in its static constructor. Only `[System Process]` + self are
  visible (`GetProcesses`/ToolHelp count = 2 vs ~405); `GetProcessById`
  on the parent throws `ArgumentException: Process with an Id ... is not
  running`, exit 1. A staged self-contained probe exits 0 with all
  self-introspection green — same sandbox, same grants.

Design consequences of the rule:

- Stage dependency closures into grantable roots; never grant installs.
- Prefer self-contained managed closures over CLI hosts for sandboxed work.
- Workloads must not resolve host objects: no ancestor walks above the
  grants, no outside-PID queries, no host-path/profile lookups. Treat any
  "not found"-shaped death of an otherwise-launchable binary as a
  visibility failure first.
- Open: avoiding the CLI host probe for framework-dependent launch; the
  git half is closed (ro drive-root grant), with profile trees documented
  unbindable.

## Decision

Use the experimental API **supplemented by a Gagamba supervisor**, i.e. the
mechanism is API + ToolHelp parent-PID sweep with PID+start-time identity
(proven cross-boundary: found=1, killed=1, root survived, no surviving
writer). Reject silent fallback: unsupported shapes, failed setups, and
missing mechanisms reject before target dispatch.

- Scope: Windows 11 25H2+ where the exports resolve. Other builds are
  Unsupported, never downgraded.
- The supervisor owns stop: root-wait, descendant sweep, terminate, verify.
  Kernel-backed ownership beyond that (surviving OUR crash) is unproven and
  stays an open GQ-1 gate.
- I/O uses pipes and file effects. STARTUPINFO file redirection is out.
- Profile lifecycle timing varies (present-then-deleted vs never-materialized
  with no residue either way); the residue check after every run is mandatory
  and stays in the conformance path.

## Alternatives and fallback

- Restricted-token / dedicated-user reimplementation: deferred fallback if the
  supervisor cannot reach the GQ bar or the dotnet-compat question forces
  capabilities outside the offline profile. Its install/elevation/ACL costs
  were the reason to try the API first; they stand if we come back.
- External runtime dependency (e.g. MXC binaries): rejected as a production
  dependency. MXC remains a research source (schema, tiers, os-support matrix)
  and its MIT artifacts may inform — never silently extend — our specs.

## Consequences

- Public APIs stay provisional until GL-1A/GM-1A evidence exists.
- In-job hosts (like current CI shells) need nesting review per run.
- Conformance must re-prove: descendant stop, pipe transport, profile
  residue, and the workload-visibility answers (CLI-host vs self-contained,
  ancestor grants) on every claimed OS build.
- Execution model (GW-1B-L7): host → sandbox → resident runner → tool →
  descendants. The sandbox is the unit of isolation; nested processes form
  a mutually-visible domain (parent/child/self) while outside PIDs stay
  sealed. The runner is generic (exe + args + env); caller env does not
  cross the boundary, so the runner applies child env itself.
- Network profiles: `offline` means no network including loopback;
  `offline+loopback` adds localhost-only, requested explicitly per
  workload (e.g. test runners). Least privilege intact.
- The msys read-only drive-root grant is a transitional compatibility
  concession (adapter quirk), not the intended filesystem model.
- GQ-1 contract direction: kernel-backed descendant ownership via Job
  Objects (`KILL_ON_JOB_CLOSE`), supervisor as the abstraction above;
  breakaway/nested-job cases still to verify.
