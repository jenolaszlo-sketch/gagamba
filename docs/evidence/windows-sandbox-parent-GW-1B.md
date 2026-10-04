# GW-1B sandbox-resident runner topology (L5-SANDBOX-PARENT)

Date: 2026-10-04. Spike: `spikes/SandboxRunner` (generic resident
launcher) + `spikes/PidProbe` (`inspect` mode) + `L5-SANDBOX-PARENT` leg in
`spikes/Gw1bLaunch` (manifest v7 = 21 IDs). Per-phase JSON evidence is saved
next to each report as `sandbox-parent-{runId}.json` (ignored `artifacts/`
or `--out` dir); this file is the durable record.

## Question

The dotnet CLI dies querying its outside parent
(`InstallerBase.GetProcessById` → `ArgumentException`). If a sandbox
process launches dotnet and same-sandbox processes can see each other,
does the CLI proceed? No dotnet special-casing: the runner is generic
(exe + args + `--env`, `$RUNNER_PID`/`$HOST_PID` substitution only).

## Result classification (from the spec): L4 passes, L5 fails differently

Parent visibility is fixed; other dotnet dependencies remain. Topology
adopted with named follow-ups; no isolation weakened.

## Phases (all with grants rw=[ws], ro=[staged]; dotnet phases add ro=[dotnet])

- P1-viability pass: staged self-contained runner starts in-sandbox,
  reports own/parent PID, exits 0.
- P2-native-child pass: nested `cmd /c exit 42` — child PID visible while
  running, exit code observed. Nested creation works.
- P2b-nested-confinement pass: nested `type` of ungranted
  `AGENTS.md` → `Access is denied.`, exit 1. Nested children ARE confined.
- P3-child-sees-parent pass (the discriminator): staged child resolves the
  sandbox parent by PID, direct and enumerated, name `SandboxRunner`.
- P4-host-hidden pass: the leg PID (also the runner's ToolHelp ppid, so
  the hidden target was the grandparent) is invisible to the nested child
  (direct + enumerated false). Model: an engine-created process anchors a
  mutually-visible nested domain; outside PIDs stay sealed (numbers may
  leak via ToolHelp, opens fail).
- P5-dotnet-info FAIL (differently): full `--info` table prints — the
  original `InstallerBase` death is GONE — then exit 1 on
  `ServiceController`/`WindowsUpdateAgent` MSI workload probing:
  `Cannot open Service Control Manager ... Access is denied.` New first
  failure, mechanism named.
- P6-list-sdks pass, P7-list-runtimes pass (exit 0, no SCM touch).
- P8-build pass: host-restored dependency-free `mini`, in-sandbox
  `dotnet build --no-restore` → `Build succeeded. 0 Errors`. Offline
  compile works under the runner with ws-local
  `DOTNET_CLI_HOME`/`TMP`/`TEMP`/`APPDATA`/`NUGET_PACKAGES` (applied by the
  runner itself — caller env does NOT cross the boundary, proven by nulls
  in every `runnerEnv`).
- P9-test: RESOLVED in three parts. (1) The CS0246 trail was a broken
  fixture, not a sandbox finding: the hand-written `minitest` lacked
  `using Microsoft.VisualStudio.TestTools.UnitTesting;` and fails
  identically on the host (verified). The hunt it caused still proved the
  full stack innocent: package files byte-identical via stream+mmap+hash,
  evaluation resolves 370 refs, CSC invoked correctly (binlog forensics),
  servers irrelevant, no path-length caps, no MOTW/ACL/reparse anomalies,
  direct CSC works against sandbox-built refs (P8D4), even sandbox-copied
  package bytes work as refs (P8D5). (2) The one genuine anomaly found
  along the way — `cmd /c dir` denied on package dirs while runner-lists
  pass — reproduces on known-good dirs too: `cmd/dir` is broken
  in-sandbox generally (volume probe on `C:\`, same ancestor rule as
  git), the package dir was never special. (3) With the fixture fixed,
  `dotnet build` of minitest passes in-sandbox but `dotnet test` hangs:
  vstest.diag shows `SocketServer.Start: Listening on endpoint :
  127.0.0.1:6279` then `WaitForRequestHandlerConnection` (90s) — the
  console↔testhost handshake needs TCP loopback, which offline-process-v1
  denies by design. Our 60s bound fires first (orphan names recorded in
  the leg). Verdict: test execution is blocked by network policy, not by
  a provider quirk; unblocking it is a loopback-exception decision, not a
  bug fix.

## Side findings (provider constants)

- Grant-shape fragility is NOT a length cap: grant totals 115/121/124 bind
  while 118 rejects and a 121-char ws failed 6/6 across runs (same length,
  different names, opposite verdicts). Treat `ERROR_INVALID_DATA` on grants
  as retry-with-fresh-workspace, never as path validation. Short ws
  prefixes (`gw1b-sb-`) used for margin, not as a fix.
- Caller env is not inherited across the boundary (engine supplies
  per-identity `AC\Temp` for TMP/TEMP, nulls elsewhere). The runner's
  `--env` is the mechanism for child env.

## Architectural consequence (adopted)

Sandbox Process Tree Visibility: processes within the same sandbox
execution domain may discover and inspect the minimum process metadata
required for parent/child coordination, while processes outside that
domain remain inaccessible. Gagamba's execution model becomes host →
sandbox → resident runner → tool → descendants, with the sandbox (not each
process) as the unit of isolation.
