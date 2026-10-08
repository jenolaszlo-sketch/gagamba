# Gagamba Remediation Evidence Requirement

Apply this requirement to **AR-2 through AR-7** and to any future security-sensitive Gagamba remediation.

Every completed remediation slice must produce a durable evidence document under:

```text
docs/evidence/
```

The evidence document is part of the deliverable.

A slice is not complete merely because its implementation and tests are green.

---

# 1. Purpose

Evidence documents record:

```text
claim
→ baseline observation
→ test/probe
→ implementation change
→ fixed observation
→ qualification boundary
```

They are intended to make future security and architecture claims independently reviewable.

Do not write evidence documents as progress summaries or release notes.

They should answer:

> What did we actually establish, on what source revision, on what host, using what test, and what remains unproved?

---

# 2. Required header

Every evidence document must begin with:

```text
Title
Date
Remediation slice
Baseline source SHA
Final source SHA
Branch
Host OS/version
Architecture
Runtime/SDK version
Relevant native prerequisites
Qualification scope
Explicit exclusions
```

For Linux include, where relevant:

- kernel
- libc
- cgroup version
- privilege/UID context
- delegation configuration

For macOS include, where relevant:

- macOS version
- architecture
- launchd domain
- helper location/version assumptions

Do not generalize evidence from one host configuration to unsupported configurations.

---

# 3. Baseline-to-fix evidence

For every finding with a reproducible defect, provide:

| Probe / test | Baseline failure | Implementation change | Fixed observation |
| --- | --- | --- | --- |

Whenever practical:

1. add the regression first
2. run it against the unchanged baseline
3. preserve the observed failure
4. implement the repair
5. rerun the same regression

The evidence must make clear when baseline reproduction was impossible and why.

Never invent a baseline failure merely to complete the table.

---

# 4. Test construction requirements

Security/lifecycle regressions should use, as applicable:

- fresh GUID/nonced workspaces
- fresh marker files
- explicit lifecycle barriers
- finite child workloads
- bounded waits
- independent watchdogs
- independent cleanup paths
- positive controls
- negative controls
- actual native-domain observation where available

Avoid relying on:

- arbitrary sleeps as the primary synchronization mechanism
- stale heartbeat files
- mtime alone
- absence of output
- lack of a hang
- process PID disappearance alone
- successful API return without independent effect verification

A passing test must establish the intended property, not merely exercise the code path.

---

# 5. Final local checks

Include an exact table such as:

| Check | Result |
| --- | --- |
| Solution build | ... |
| Provider tests | ... |
| Contract tests | ... |
| Conformance tests | ... |
| Slice-specific regressions | ... |

Record:

- passed
- failed
- skipped
- unsupported

Do not treat a skip as qualification.

Where relevant, distinguish:

```text
portable test passed
native test passed
native test unavailable
host unsupported
qualification incomplete
```

---

# 6. Cleanup evidence

Security-sensitive execution tests must report cleanup status.

State how the test established:

- root termination
- descendant termination
- domain emptiness
- handle/resource release
- temporary filesystem cleanup
- cgroup/job/launchd cleanup
- test-owned helper cleanup

If cleanup cannot be confirmed, record:

```text
Cleanup status: Unknown / Failed
```

Do not convert observation failure into successful cleanup.

If the implementation intentionally retains ownership for later retry, document that behavior.

---

# 7. Ownership and semantic changes

For slices that change lifecycle behavior, record the resulting invariants.

Examples:

- admission ownership
- execution ownership
- completion ownership
- termination ownership
- observer cancellation semantics
- disposal semantics
- retry semantics
- cleanup uncertainty
- terminal-result retention

For platform-specific work, identify what is inherited from the shared lifecycle model and what remains platform-specific.

---

# 8. Findings addressed

List every review finding touched by the slice.

For each use one of:

```text
Fixed
Partially addressed
Evidence improved
Documentation corrected
Deferred
Still open
```

Do not mark a finding fixed merely because documentation now acknowledges it.

Example:

```text
F08 — Fixed for qualified Linux environment.
F10 — Partially addressed; musl remains unsupported and unqualified.
F01 — Still open; hostile-workload boundary not implemented.
```

---

# 9. Qualification boundary

Every evidence document must explicitly state what its results do **not** establish.

Examples:

- root WSL testing is not unprivileged Linux qualification
- parser tests are not native macOS qualification
- process ownership is not filesystem confinement
- a disposable workspace is not a filesystem sandbox
- blocked direct descendants do not prove broker isolation
- lifecycle completion does not prove network restriction
- local tests do not prove installed NuGet package behavior
- source tests do not prove release qualification

Prefer narrow accurate claims.

---

# 10. Platform-specific evidence

## AR-2 Linux

Evidence should include at least:

- lifecycle parity with AR-1 invariants
- E8 cancellation/retry preservation
- E9 descriptor inheritance before/after
- cleanup failure behavior
- cgroup-empty observation
- native prerequisite classification
- synchronized placement qualification
- E10 migration result
- privilege/delegation context

Separate root/WSL evidence from unprivileged delegated Linux evidence.

---

## AR-3 macOS

Evidence should include at least:

- helper deadline behavior
- concurrent stdout/stderr draining
- oversized-output handling
- Running/Terminal/NotFound/ObservationFailed distinctions
- failed bootout behavior
- unknown/malformed state handling
- native same-process-group lifecycle evidence
- setsid limitation

Clearly distinguish parser/fake-helper tests from native launchd qualification.

---

## AR-4 contracts and invocation

Evidence should include:

- mutation-before/after behavior
- immutable capability data
- invocation snapshot behavior
- argv round trips
- Windows serialization cases
- malformed-input cleanup
- foreign-instance token behavior
- compatibility results

If public API is additive, include a source-compatibility test.

---

## AR-5 execution usability/evidence

Evidence should include:

- deadline semantics
- user cancellation versus deadline
- bounded stdout/stderr
- overflow behavior
- root-exit/descendant-open-pipe behavior
- cleanup-failure result
- redaction behavior
- normalized invocation fingerprint behavior

Do not claim resource quotas that were not implemented.

---

## AR-6 qualification

Evidence should include:

- exact source SHA
- package candidate SHA/version
- Windows qualification
- qualified Linux qualification
- native macOS qualification
- skipped/unsupported counts
- installed-package consumer tests
- stale/missing evidence rejection
- release-gate rejection tests

The release gate itself must be tested negatively.

---

## AR-7 restricted execution

Evidence requirements are stricter.

For every claimed restriction include both:

```text
positive control
negative control
```

Examples:

### Filesystem

Positive:
authorized file is accessible.

Negative:
unauthorized file is inaccessible.

Also test:

- absolute path
- `..`
- symlink
- junction
- hardlink where applicable
- mount/alias behavior

### Network

Positive:
authorized endpoint is reachable.

Negative:
unauthorized endpoint is blocked.

Consider separately:

- loopback
- LAN
- internet
- DNS
- metadata endpoints

### IPC

Test representative local channels appropriate to the platform.

### Credentials

Use synthetic credential sentinels only.

Never access real secrets for qualification.

---

# 11. Model-review evidence

If Sol, Luna, Muse Spark, Astra, or another reviewer is used, record only useful engineering facts:

```text
Model/reviewer
Assigned scope
Material findings
Accepted findings
Rejected findings
Resulting changes
```

Do not include hidden reasoning or conversational transcripts.

Independent review is evidence about the implementation process, not proof of correctness by itself.

---

# 12. Evidence status vocabulary

Use these terms consistently:

### Implemented

Code exists.

### Locally tested

Relevant tests passed in the current development environment.

### Native-qualified

The actual platform mechanism passed the required qualification tests on a documented compatible host.

### Package-qualified

A package built from the exact qualified source was installed and tested independently from the source tree.

### Release-qualified

All mandatory supported-platform and package qualification requirements for that exact source revision are satisfied.

Do not collapse these levels into “done.”

---

# 13. Source binding

Every evidence artifact must identify the exact source revision it proves.

If the working tree was dirty, record that.

Evidence from one SHA must not silently qualify another SHA.

If later commits affect security-sensitive code covered by previous evidence, determine whether requalification is required.

---

# 14. Evidence retention

Keep concise durable evidence under:

```text
docs/evidence/
```

Raw logs, TRX files, temporary binaries, large probe output, and disposable artifacts may remain under ignored:

```text
artifacts/
```

The durable document should contain enough information to reproduce the important checks without preserving every raw byte.

Archived one-off probes may be retained when they establish behavior not yet promoted into regression tests.

---

# 15. Completion rule

A remediation slice is complete only when:

```text
implementation
+ regression tests
+ native qualification where required
+ cleanup evidence
+ documented limitations
+ durable evidence artifact
```

are all present.

If native qualification is unavailable, the slice may be recorded as:

```text
IMPLEMENTED — NATIVE QUALIFICATION PENDING
```

but not as fully qualified.

---

# 16. Guiding principle

Do not ask:

> Did the model fix it?

Ask:

> What independently inspectable evidence now supports the claim?

The evidence document is the answer.