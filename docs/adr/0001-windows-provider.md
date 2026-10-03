# ADR 0001: Windows provider — experimental API plus Gagamba supervisor

Status: accepted for continued prototyping (NOT production qualification).
Date: 2026-10-03. Evidence: `docs/evidence/windows-minimal-launch-GW-1B.md`,
`docs/evidence/windows-denial-tree-io-GW-1B.md`,
`docs/evidence/windows-pipes-supervisor-workloads-GW-1B.md`.

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
- `dotnet --info` launches but exits 1 silently in every grant configuration
  (ws-only through full closure), while `whoami.exe` runs fine with no
  grants at all — so System32 is implicitly covered and the dotnet failure
  is runtime compatibility (registry/capability/Low-IL friction), not path
  closure. Unresolved: `registryRead` capability variant, env tuning.

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
  residue, and the dotnet-compat answer on every claimed OS build.
