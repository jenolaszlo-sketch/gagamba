# WP05 D4 Crash-Witness Evidence Matrix - Reviewer Rerun - 2026-10-20

Status: independent reviewer artifact. This matrix binds the D4 crash
boundaries and no-resend claims to (1) my own rerun of the exact fixture
sources and (2) kernel/BCL SIGKILL controls. It does not change any producer
artifact and does not accept a gate.

Reviewer environment: same Linux family profile as the producer (Ubuntu,
kernel `6.6.114.1-microsoft-standard-WSL2`, boot
`69d053fb-0fd9-4209-aa5b-be128424ed34`, uid 1000 `gaga`, GAGAMBA-delegated
cgroup parent `/sys/fs/cgroup/gaga-delegated` with `memory pids` subtree
control, .NET `10.0.112`, GCC `15.2.0`, Bubblewrap `0.11.1`), launched by the
frozen `/root/wp05/run-as-gaga-in-host.sh` (cgroup probe + setpriv uid 1000 +
caps none); kernel/boot identity committed as
`environment-review.txt` (captured on the same boot as the reruns). Native artifact identities matched the frozen set:

- fixture `ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305`
- exec coordinator `5e55e92b4228013e88a9fb34ac4674c5b9a485e831db7ff6d86f02df7c68c73b`
- seccomp filter `2aa879019138c4d4066557213ce0037de70430aac3a05008eec5c8abd999d108`
- wp05 qualification coordinator `e20903cce6061ecc2b152f9e7b4ccb389edeee54047d96f1f3b66dd5195be0b9`.

Bound source HEAD for execution: `de1ee93fdfa9cc29cf82a79f6e6dac9bfea7d753`
(evidence source HEAD; `tests` tree diff `de1ee93..8254cf8` empty).

## 1. Rerun counts

| Run | Commands | Producer claim | Reviewer run | Wall |
|---|---|---|---|---|
| D4 supervisor run 1 | filter `FullyQualifiedName~Wp05D4SupervisorTests`, GAGAMBA_DELEGATED_CGROUP via launcher | 15/0/0 (xunit 7 s) | **15 passed / 0 failed / 0 skipped** (xunit 7 s) | 11.5 s |
| D4 supervisor run 2 | same, fresh `D4_EVIDENCE` dir | 15/0/0 | **15/0/0** | 11.3 s |
| Focused regressions | producer's filter (66) | 66/0/0 | **66 passed / 0 failed / 0 skipped** (no skips) | 10.9 s |

The `Duration: 7 s` line printed by xunit under VSTest reflects only the
runner's internal clock. The fixture's 15 facts run in parallel xunit
collections (class timing showed ~90 ms start spread on my rerun), which
explains sub-shell-clock durations being less than wall time. No count is
affected.

## 2. Crash-boundary ↔ evidence mapping (reviewer-confirmed)

| Boundary | Case | What the kernel/test actually exhibit | no-resend property | Reviewer classification |
|---|---|---|---|---|
| pre-intent (`create:gen.seal` gate) | D4-10 | owner parked mid journal-create; killed; journal persisted no Release; replacement recovers, does not attempt release | no FD4 witness before kill; none after | PASS - COMPONENT GATE |
| intent persistence (`write:gen.tmp` gate) | D4-11 | owner parked mid candidate write; killed; `ReadLatest` fails with seal-ambiguity; replacement recovery resolves the seal explicitly (`RecoverySealConfirmed=recovered`) and does not attempt release | no FD4; no second write | PASS - ACTUAL STATE PRESERVED |
| `after-intent` | D4-2 | durable ReleaseIntent; killed after acknowledged intent; replacement recovers Intent / `MayHaveExecuted`, no attempt | 0 pre-write witnesses, 0 transports | PASS - REAL PROCESS |
| `after-consume` | D4-5 | consumed capability in dead owner; no FD4 | 0 pre-write witnesses | PASS - REAL PROCESS |
| `after-write` | D4-3 | one real FD4 write (`PreWriteWitnessAcknowledged` + `WriteReturned=Delivered`), owner killed before reaping acceptance; Intent stays | no second write; acceptance recorded `unknown`, not inferred | PASS - REAL FD4 WRITE OBSERVED; ACCEPTANCE UNKNOWN |
| `after-committed` | D4-4 | durable Committed persists; replacement recovers Committed; no second write | 1 write total | PASS - REAL PROCESS |
| stop durable / failed / in-flight | D4-7/D4-8/D4-13 | durable StopRecord; failed durable stop (runtime accepted only); in-flight stop write killed; all three routed into recovery with no fabricated stop/release; D4-8's replacement release is a LABELLED diagnostic via new test-only capability + Lane-A test transport | 0 FD4 in owner lane; Lane-A is separately countable | PASS - REAL PROCESS + COMPONENT GATES |
| two recovery contenders | D4-14 | first contender holds writer (barrier), second refuses with real acquired-fence denial, no release from either | no second writer; no release | PASS - REAL PROCESS/LOCK |
| CE-01 retry refusal | D4-2b | replacement with newly minted test-only capability re-tries release over durable Intent: R12 arbiter refuses (`Refused(NotCommitted)`), 0 transport attempts; Intent preserved | 0 test lanes, 0 FD4 | PASS - DIAGNOSTIC CONTRACT |
| CE-02 ambiguous intent | D4-6 | injected seal-unlink failure; owner decision `Refused(NotCommitted)`/0 attempts; killed; replacement recovers with explicit seal handling, `MayHaveExecuted`, no attempt | 0 FD4 | PASS - COMPONENT FAULT + REAL RECOVERY |
| CE-03 failed durable stop | D4-8 | runtime accepted stop; durable write fails; owner killed; replacement release is diagnostic Lane-A only (1 `TestTransportAttempt`, 0 `PreWriteWitnessAcknowledged`) | no original-FD4 change | PASS - DIAGNOSTIC ONLY |
| CE-04 duplicate | D4-9 | winner Released with 1 real FD4 attempt and coordinator exit 0; concurrent loser `Refused`; later caller `Duplicate` | exactly one FD4 attempt | PASS - REAL NATIVE |

No case produced a second `PreWriteWitnessAcknowledged` or second
`TestTransportAttempt`, and no case observed a second production-coordinator
exit 0 (verified identically in the producer's `compact/case-index.txt` and
my own `case-index-run1.txt`).

## 3. SIGKILL oracle controls (reviewer-added, non-fixture)

Executed on the same profile (uid 1000 `gaga`, zero caps, via the frozen
launcher). Artifacts in `controls/`:

| Control | Setup | Observation | File |
|---|---|---|---|
| kernel wait4 | C: fork + child parks + parent `kill(2, SIGKILL)` + `wait4` | `signaled=1 termsig=9` | `controls/wait4probe.txt` |
| shell-level SIGTERM contrast | `sleep`-ing bash killed with SIGTERM then `wait`ed | exit 143 (correct non-SIGKILL mapping) | `controls/term-control.txt` |
| .NET default .Kill() | child traps SIGTERM (`trap "" TERM`) then blocks; .NET 10.0.112 harness calls `Process.Kill(entireProcessTree:false)` | child killed despite TERM trap, `ExitCode==137` | `controls/dnet-kill-control.txt` |
| .NET self-exit ambiguity | child calls `bash -c 'exit 137'` | `ExitCode==137` (indistinguishable from SIGKILL on `ExitCode` alone) | `controls/dnet-self-exit-control.txt` |
| runtime sources | .NET runtime `Process.Unix.cs` and `ProcessExitStatus.cs` upstream read | `Kill()` = `SIGKILL`; signal-death ExitCode = 128 + signal; real signal stored in `ProcessExitStatus.Signal` | (this document) |

Contract of the fixture's kill oracle: the D4 owner exits only 0, 1 or 3
normally and never forwards 137, and the kill is issued against an audited
direct child whose death is additionally corroborated by journal-flock
fence-release failure. The D4-kill assertion is therefore an accepted
measured signal-death proof on this profile. Its public-surface limitation
(the inability to classify `WIFSIGNALED` directly from `Process.ExitCode`)
is recorded as review correction CR-1 — a D5+ bound on the new standard.

## 4. Per-case artifacts from my rerun

Under `myruns/`: each case's `result.txt`, `owner.ready`, `barrier.txt`,
`witness.txt`, replacement result files, and the journal slot list. Their
event-count signatures match the producer's `compact/case-index.txt`
(`prewrite`, `test_transport`, `write_returned`, `coord_exit` columns
identical within my runs).

## 5. Cleanup

- No `gagamba-d4-*` residual child processes at cleanup (`leftover-procs-after.txt`).
- All cgroup domains created by the cases were removed by their own
  `D4Case.Dispose`/`CleanupCgroup` discipline (verified with populating-0 waits
  in the case containers; the reviewer's runs declared the cases exited with
  standard exit code 0 and no leftover `bwrap` in `ps`).

## 6. Independent verification of the producer's artifact manifest

`manifest-verify.txt`: extracted `8254cf89f091eb5baf305d125e00cd2bf61a1ca1:docs/evidence/linux-m1/wp05-d4-release-owner-crash-witness-artifacts`
via `git archive` and ran `sha256sum -c SHA256SUMS`:
**133 entries / 133 OK / 0 failures**. The developer report's own self-hash
`53c76c575fdf74c9543614bb629a39257b9255c81b2d963102aeb3157d79fa58`
revalidates through `report-hash.txt` and `final-binding.txt`.
