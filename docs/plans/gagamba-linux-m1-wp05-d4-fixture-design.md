# WP05 D4 Fixture Design and Feasibility - 2026-10-20

Status: D4.0 design and feasibility only. No production change, no release
qualification, no gate acceptance. This note binds the D4 fixture to R12 and
the independent Q3 required-work statements.

## 1. Bindings

- Product R12: `38f6981b2a60daab2df37c92913a6760c1bef131`,
  tree `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`.
- Preservation base: `7dd5ffa6c2ecaacc37022adb9a7b6c333695eef0`.
- Accepted harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f`.
- Plan review V3: `e4b0a798e944ddfb144901c63b0c444e7980533b`; required
  changes blob `73eb09af27c5dbc896739ebf48489862ec13e0a9`.
- D1-D3: `dbfecad...`, `dc4c372...`, `338a98a...`; independent review Q3
  `d0f2220...`, required D4-D6 document blob
  `1768ec2a98e69349ca46137f4194fb0d49233772`.
- New branch: `implementation/wp05-d4-release-owner-witness`.
- Production source tree is unchanged: `src` and production `native` diff
  against R12 is empty.

## 2. Feasibility matrix

| Capability | R12 production boundary | Existing/test-only seam | Proposed D4 fixture | Observation | Limitation |
|---|---|---|---|---|---|
| Original release owner | `ReleaseArbiter.RequestRelease` over `HeldExecution`/`ReleasePipeWrite` | Q3 `ProbeHarness` composition; internal `HeldExecution.TryWriteRelease`; test-only project can reference test assembly | Separate owner process constructs real journal, prepared capability, cgroup/held execution, then invokes arbiter | Owner PID/start time; prepared identity; journal sequence; held report; FD inventory | Owner is a test-only executable, not production owner/WP10; no target |
| Pre-intent crash | First intent write starts inside `OwnershipJournal.WriteTransition` | `IJournalStorage` seam, test-only wrapper around `JournalFileStorage` | Gate the first intent `CreateExclusive`/`WriteAll`; acknowledge and wait; supervisor SIGKILL | Journal has Prepared only; no intent; no FD4 witness | Wrapper is test-only; the gate is before/inside a real durable operation, not a production hook |
| During intent persistence / seal | `OwnershipJournal.Write`: seal creation, candidate writes/renames, seal unlink | Same `IJournalStorage` seam; existing D2 `FailAt("unlink", 2)` shape | Test-only gate/fault wrapper blocks at a named storage step; supervisor kills and inspects actual files | Actual seal/slot state; no invented commit | The exact crash point is the wrapper's step, not a kernel/ptrace breakpoint |
| After durable intent | `ReleaseArbiter` `TestBarrier("after-intent")` | Existing internal `TestBarrier` | Owner writes barrier ack and parks; supervisor SIGKILL | Durable ReleaseIntent; no FD4 | Barrier is an internal test hook, not production |
| After gate, before FD4 | `TestBarrier("before-consume")` / `"after-consume"` | Existing internal barriers | Owner parks; supervisor SIGKILL | Intent; capability consumed (after-consume); no FD4 | No target; write not attempted |
| After FD4 before committed | `TestBarrier("after-write")` | Real `HeldExecution.TryWriteRelease` wrapped by test-only witness transport | Owner parks after actual nonblocking FD4 call; supervisor SIGKILL | Witness pre-write/return; FD4 bytes not directly in supervisor; coordinator acceptance may be observed only if read before death | Native acceptance after owner death is unknown; do not infer it |
| After committed | `TestBarrier("after-committed")` | Real journal plus barrier | Owner parks; supervisor SIGKILL | Durable Committed; no resend | Acceptance receipt may precede/follow; preserve only observed facts |
| Stop generation | `ReleaseArbiter.RequestStop` -> `WriteMutation` | `TestBarrier("after-stop")`; `IJournalStorage` gate/fault | Owner requests stop normally and with a test-only failed durable write; supervisor kills; replacement recovers | Runtime acceptance vs durable StopRecord; deadline | Does not decide C2; failed stop is diagnostic |
| Recovery | `OwnershipJournal.TryAcquireWriter`, `TryRecoverCommit`, `ReleaseRecoveryClassifier` | Existing production recovery APIs; new replacement process | Fresh replacement acquires fence, resolves, classifies, reports; no capability/FD4 | Baseline sequence, recovered release/stop/classification | No production owner takeover; no target; C1-C4 unresolved |
| No-resend | Recovery plus FD4 writer | Supervisor-owned ledger + owner witness + replacement descriptor inventory | Replacement has no FD4; witness count remains one; coordinator acceptance count if available | At-most-once observed; unknown delivery preserved | Cannot claim universal exactly-once; supervisor may miss post-crash acceptance |
| CE-01 retention | Arbiter decision retention / durable intent | Existing concurrent probe + fresh owner | Crash after intent, replacement retries release against durable intent | Loser/refusal; one FD4 attempt | Fresh replacement must not be given capability; it cannot legitimately release; contract remains C4 |
| CE-03 stop window | Stop runtime vs durable; capability | Test-only faulted journal + fresh owner/replacement | Original owner accepts stop with failed durable write, dies; replacement observes no Stop and is allowed a diagnostic release only with newly minted test capability | Runtime stop true, durable stop false, capability live before death; replacement release is diagnostic | New authority is not a production takeover; no privilege-bypass claim; C2 unresolved |
| CE-04 concurrent duplicate | `ReleaseArbiter` gate and retained decision | Existing `after-intent` barrier + fresh owner | Owner starts deterministic two-caller release race; witness/FD4 count; optionally kill after intent | Winner/loser/later decisions; one FD4 attempt | Scheduling is controlled by barrier, not a general race proof |
| CE-02 ambiguity | Seal unlink failure after both slots | Test-only `IJournalStorage` fault wrapper | Owner receives ambiguous failure; supervisor kills; replacement `TryRecoverCommit` classifies Intent | Actual journal files; MayHaveExecuted recovery; no FD4 | Same C1/R09 unresolved; no production repair |

## 3. Process and authority geometry

```text
supervisor (test process)
  |-- creates temp journal dir, witness/control files, readiness/barrier files
  |-- starts original owner (fresh OS process)
  |     |-- owns journal writer flock (ownership.lock)
  |     |-- owns prepared capability and real HeldExecution
  |     |     |-- owns FD4 control writer
  |     |     `-- owns coordinator/wrapper/status descriptors
  |     |-- owns barrier/witness file descriptors only as needed
  |     `-- may be SIGKILLed at a named barrier
  |-- observes owner PID + /proc start identity, FD inventory, barrier ack
  |-- confirms death; confirms fence release
  |-- starts replacement (new OS process)
  |     |-- fresh TryAcquireWriter on same journal
  |     |-- Resolve/TryRecoverCommit and classification only
  |     `-- no prepared capability, no HeldExecution, no FD4 writer
  `-- bounds and cleans test-owned cgroup/process/temp state
```

The supervisor never holds the journal writer for release purposes or
constructs release authority. After owner death it may transiently acquire the
fence solely to prove the owner released it; that probe is not release
authority. The witness ledger is observer-only: it records pre-write intent,
the actual `HeldExecution.TryWriteRelease` return, and (where read before
death) coordinator stdout/exit. Missing observations remain unknown.

D5 correction notes: the owner's `Environment.ProcessId > 0` caller-liveness
prerequisite is a test-assembly-consistent surrogate, not a qualified
WP08/WP10 owner-liveness check. The `go.txt` storage gate accepts any writer
and has no uid/pid binding; it is fixture-only. The witness append uses one
bounded O_APPEND line per event; it remains test-only and a missing event is
never treated as proof that an effect did not occur.

## 4. Crash boundary plan

| ID | Gate | Expected classification |
|---|---|---|
| D4-C01 | First intent storage step before any durable Release | Prepared/no intent; no FD4 |
| D4-C02 | Seal/slot storage step during intent persistence | Actual seal/slot state; may be uncertain; no FD4 |
| D4-C03 | `after-intent` | Durable Intent; no FD4; replacement no release |
| D4-C04 | `after-consume` | Intent; capability consumed in dead owner; no FD4; replacement no release |
| D4-C05 | `after-write` | Intent; one FD4 attempt; committed may be absent; acceptance unknown; replacement no resend |
| D4-C06 | `after-committed` | Committed preserved; no second FD4 |
| D4-C07 | Stop `WriteAll` gate / `after-stop` | Runtime stop and/or durable StopRecord; no fabricated durable stop |
| D4-C08 | Two replacement contenders | One effective journal writer; no recovery release; loser refuses |
| D4-CE01 | Post-intent crash + replacement retry | Replacement cannot release over durable Intent |
| D4-CE02 | Ambiguous seal failure + death | `TryRecoverCommit` Intent; MayHaveExecuted; no FD4 |
| D4-CE03 | Failed durable stop + death | No durable Stop; replacement diagnostic release only with newly minted test authority |
| D4-CE04 | Two-caller owner race | One physical FD4 attempt; loser/later decision shape; no second acceptance |

## 5. Required negative controls

- Supervisors that retain the journal writer fail the fixture; owner must
  release it only by process death.
- Replacement launched before owner death must fail `TryAcquireWriter`.
- `/proc/<owner>/fd` must show the journal lock; supervisor FD inventory must
  not; owner FD4/held descriptors must not appear in replacement.
- PID reuse is rejected by `/proc/<pid>/stat` start time plus the spawned
  child handle, not PID inequality alone.
- Witness ledger count is not inferred from decision or journal state; a
  second attempted invocation would add a second pre-write record.
- Recovery records only what `ReadLatest`/`TryRecoverCommit` actually returns.
- Cleanup must show no test-owned process, domain, temp directory, writer
  lock, or witness writer remains.

## 6. Feasibility decision

**Supported without production changes**, subject to the supported Linux
environment and native artifacts being present. The owner uses the existing
R12 `ReleaseArbiter` and `HeldExecution` composition; test-only storage gates
and witness wrappers only observe or pause existing seams. The production
coordinator has no post-release receipt API, so coordinator acceptance is
observed from its stdout/exit only when the owner survives to read it; after a
kill it is `unknown`, never inferred. The three previously unavailable
Q3 release-path facts and D5 native status negatives remain outside D4.

Blocked/limited by design, not by a missing mandatory seam:

- No production target execution; no WP08/WP10 owner; no FD5 post-release.
- CE-03 replacement release is a newly minted test-only capability used as a
  diagnostic, not an authorized production takeover.
- C1-C4 remain unresolved; D4 does not decide or repair them.
- Missing native artifacts at runtime make the real HeldExecution/FD4 cases
  `HELD - REQUIRED ARTIFACT UNAVAILABLE`; they are not replaced with a
  synthetic transport claim.
