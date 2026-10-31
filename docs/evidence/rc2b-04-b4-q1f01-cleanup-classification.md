# B4Q1-F01 Cleanup Classification Correction

Date: 2026-10-31
Remediation slice: GAGAMBA-B4Q1-F01-R1
Baseline source SHA: `37a90871fa820cf34f663355fb0d7cc918a08e16` (tree `f51740e4082dd0706f759318a88ecf83eda70bf4`)
Final source SHA: `4516c24e01fbea7cc40c44b1e7c3c78109b5b438` (tree `ce1fd2f72e6bb32745bdf348184c4f87728d718a`); this document is its documentation-only child commit on `main`
Branch: `main`
Host OS/version: Windows 11 (NT 10.0.26200.0) hosting WSL2 Ubuntu 26.04.1 LTS
Architecture: x86_64
Runtime/SDK version: .NET SDK `10.0.113`, runtime `10.0.13`; Python `3.14.8` (Windows deterministic tests) and Ubuntu `python3` 3.14 (native harness)
Relevant native prerequisites: cgroup v2 host; transient systemd **user** unit with `Delegate=memory pids`, `DelegateSubgroup=host`, `ExitType=cgroup`, `KillMode=control-group`, `TimeoutStopSec=10`; UID/GID 1000 (`gaga`); `CapInh`/`CapPrm`/`CapEff`/`CapAmb` all zero; `NoNewPrivs=1`; kernel `6.6.114.1-microsoft-standard-WSL2`; glibc `2.43-2ubuntu2.4`; boot id `b87db9d9-f88e-4bb2-8b65-d85879947625`
Qualification scope: cleanup-classification correction in the native INT-01 and B4 qualification harnesses; full 32-case INT-01 matrix (two runs), full 13-case B4 matrix, 275-case WSL2 component regression batch, and a deterministic offline classification regression
Explicit exclusions: no production source, durable-journal format, lifecycle or release/recovery authority change; WSL2 is not native Linux; no native Linux host, macOS, VM/HCS, B5 sealed-C3, B6 attestation, B7 lifecycle qualification, Hufu, packaging or production activation; no historical producer/reviewer/decision artifact was modified

---

## 1. Finding and required correction

B4Q1-F01 (MEDIUM, reporting/classification) established that the INT-01
qualification harness promoted `missing-staging-ack` to **Confirmed** while the
durable journal verdict was `OwnershipCleanupState.Unknown = 4`.

The sequence that produced the false promotion is:

1. The recovery host reconciles the missing staging ACK and durably persists
   `Cleanup = Unknown` (`Reconciler` truthfully aggregates the unestablished
   staging component).
2. After reconciliation, a bounded physical fallback removes the exact retained
   empty staging directory and records `StagingRemoved = true` in the
   recovery-final snapshot.
3. The harness classified cleanup from the final snapshot flags alone
   (five flags for the fault/B4 matrices; three flags for the INT-01
   controller/owner-kill paths), so all-true flags became **Confirmed** even
   though the authoritative durable record said Unknown.

A successfully persisted Unknown verdict is **not** Confirmed cleanup. The
owner decision requires the harness classification to be corrected before any
future stage treats its aggregate Confirmed count as authoritative
([owner acceptance §2](../decisions/rc2b-04-b4-owner-acceptance.md)).

## 2. Corrected classification contract

The durable journal verdict is authoritative. `IntegratedRecoveryHost` writes
`integrated-journal-final.json` from the final journal record after
reconciliation; its `Cleanup` field is the production-decoded
`OwnershipCleanupState` (`0 NotStarted`, `1 InProgress`, `2 Confirmed`,
`3 Failed`, `4 Unknown`). The live owner dispose path reads the same field
through the controller's `journal-status` reply (`{Ok, Snapshot.Cleanup}`).

The shared classifier (`eng/int01_cleanup.py`) reports **Confirmed** only when:

- the durable verdict is exactly `2` (`OwnershipCleanupState.Confirmed`), and
- every required final physical/reaping/fencing flag is literally `true`
  (`OwnerReaped`, `WriterFence`, `DomainEmpty`, `StagingRemoved`,
  `JournalFinalized`) for recovery-final records, or the live dispose
  observation is Confirmed for the dispose path.

Missing, malformed, out-of-contract or non-integer verdict evidence is treated
as unknown and can never produce Confirmed. Every record now carries
`CleanupEvidence` with `DurableVerdict`, `FinalFlags`, `PhysicalCleanupObserved`,
`PhysicalCleanupImproved` and the resulting `State`; physical improvements stay
visible without being promoted.

## 3. Classification sites corrected

| Site | Baseline rule | Corrected rule |
|---|---|---|
| `eng/int01-native-matrix.py` controller-kill paths | `DomainEmpty && StagingRemoved && JournalFinalized` | durable `2` + all five final flags |
| `eng/int01-native-matrix.py` owner-kill path | same three flags | durable `2` + all five final flags |
| `eng/int01-native-matrix.py` live dispose path | `Cleanup.State == 1` only | `Cleanup.State == 1` **and** `journal-status` durable `2` |
| `eng/int01-fault-matrix.py` recovery-final path | five flags | durable `2` + five flags |
| `eng/b4-native-matrix.py` recovery-final path | five flags | durable `2` + five flags |

The existing hard assertions are preserved: owner reaping and writer fencing
are still asserted for the kill paths, `Cleanup.State == 1` is still asserted
for the dispose path, recovery-loss still must be Unknown, and no assertion was
removed or weakened. The correction is entirely in the qualification harness;
production source and the journal contract are untouched.

## 4. Baseline-to-fix evidence

Native single-case runs used the same profile, private build, host and
unprivileged delegation; the baseline runs executed at `37a9087` before the
harness edit, the fixed runs executed the byte-identical committed runner
files (SHA-256 bound in each `identity.json`).

| Probe / test | Baseline failure | Implementation change | Fixed observation |
|---|---|---|---|
| `missing-staging-ack` native case | `Cleanup=Confirmed`; recovery-final flags all true; durable journal `Cleanup=4`; `DomainRemoved=true` | require durable `2` + final flags | `Cleanup=Unknown`; same five flags and physical removal visible; `DurableVerdict=4`; `PhysicalCleanupImproved=true` |
| `missing-domain-ack` native case | `Cleanup=Unknown`; `DomainEmpty/DomainRemoved=true`, `StagingRemoved/JournalFinalized=false`; durable journal `Cleanup=0` | same | `Cleanup=Unknown`; durable `0`; physical domain removal remains visible |
| `test_persisted_unknown_with_every_flag_is_not_confirmed` (offline) | legacy five-flag expression returns `Confirmed` for the B4Q1-F01 fixture | shared classifier | returns `Unknown` with `DurableVerdict=4` and `PhysicalCleanupImproved=true` |
| Full INT-01 matrix | producer record tally 21 Confirmed / 11 Unknown | all five sites | **20 Confirmed / 12 Unknown**, exactly one flip (see §5) |
| Full B4 matrix | producer record tally 12 Confirmed / 1 Unknown | B4 site | **12 / 1**, no classification flips |

Baseline single-case raw records:
`baseline-staging-ack/cases/missing-staging-ack.json` SHA-256
`276ed5fda92379483e218a69c08b7f1d118fd404ad666e70d4fc1f06e58f4f10`;
`baseline-domain-ack/cases/missing-domain-ack.json` SHA-256
`ee1cbb9e9947668122d97820eff0e79267d782a937948d6086e422c1d8bf3bb3`.
Fixed single-case raw records:
`fixed-staging-ack/cases/missing-staging-ack.json` SHA-256
`673165e9a2b1ced9b3dee78b144e9223398002cd0084c5336152e9a8b09d741d`;
`fixed-domain-ack/cases/missing-domain-ack.json` SHA-256
`c9cde32db5172b3910ca8ab7799540c49d4fe35ff57cec510e843cac194e5d30`.

## 5. Full INT-01 matrix (32 cases, WSL2)

Command: `python3 eng/int01-native-matrix.py --full-matrix --report <dir>` inside
the transient unprivileged user unit. Both runs reported
**32 passed / 0 failed / 32 requested** with 12/12 preflight checks.

Run 1 raw manifest: `fixed-full` (SHA-256 manifest digest
`28414c042dffffedae9d201e381e30a8828df863010be34b4c64f39ede30a842`).
Identical rerun: `fixed-full-repeat` (manifest digest
`a6cd3412e96fc84d3bc22ddcf0d98be6a6ccf853f079bf7377e05a29f76d3f43`).
Both runs produced the same per-case classifications and tallies:
**20 Confirmed / 12 Unknown**.

| Case | Status | Cleanup | Durable verdict | R/W/D/S/J flags | Physical improved |
|---|---|---|---|---|---|
| `actual-recovery-release-impersonation` | Passed | **Confirmed** | 2 | TTTTT | False |
| `blocked-observer-native-stop` | Passed | **Confirmed** | 2 | TTTTT | False |
| `controller-before-preparation` | Passed | **Unknown** | - | - | - |
| `controller-during-preparation` | Passed | **Confirmed** | 2 | TTTTT | False |
| `controller-release-race` | Passed | **Confirmed** | 2 | TTTTT | False |
| `controller-sigkill-after-release` | Passed | **Confirmed** | 2 | TTTTT | False |
| `controller-sigkill-held` | Passed | **Confirmed** | 2 | TTTTT | False |
| `integrated-ambiguous-intent` | Passed | **Unknown** | - | - | - |
| `integrated-lost-release-ack` | Passed | **Confirmed** | 2 | TTTTT | False |
| `integrated-original-deadline` | Passed | **Confirmed** | 2 | - | - |
| `integrated-owner-live-takeover-refusal` | Passed | **Confirmed** | 2 | - | - |
| `integrated-post-exit-writer-fence` | Passed | **Confirmed** | 2 | TTTTT | False |
| `integrated-stop-after-intent-before-write` | Passed | **Confirmed** | 2 | TTTTT | False |
| `integrated-stop-before-mint` | Passed | **Confirmed** | 2 | TTTTT | False |
| `lost-retention-ack` | Passed | **Confirmed** | 2 | TTTTT | False |
| `missing-domain-ack` | Passed | **Unknown** | 0 | TTTFF | True |
| `missing-staging-ack` | Passed | **Unknown** | 4 | TTTTT | True |
| `owner-after-intent` | Passed | **Confirmed** | 2 | TTTTT | False |
| `owner-ambiguous-delivery` | Passed | **Confirmed** | 2 | TTTTT | False |
| `owner-before-retention` | Passed | **Unknown** | 0 | TTFFF | False |
| `owner-mid-staging` | Passed | **Confirmed** | 2 | TTTTT | False |
| `owner-sigkill-after-release` | Passed | **Confirmed** | 2 | TTTTT | False |
| `owner-sigkill-held` | Passed | **Confirmed** | 2 | TTTTT | False |
| `partial-scm-rights` | Passed | **Unknown** | 0 | TTFFF | False |
| `positive-one-release` | Passed | **Confirmed** | 2 | - | - |
| `recovery-release-race` | Passed | **Unknown** | - | - | - |
| `recovery-retention-death` | Passed | **Unknown** | - | - | - |
| `recovery-sigkill-after-release` | Passed | **Unknown** | - | - | - |
| `recovery-sigkill-held` | Passed | **Unknown** | - | - | - |
| `simultaneous-controller-recovery-death` | Passed | **Unknown** | - | - | - |
| `stop-before-release` | Passed | **Confirmed** | 2 | - | - |
| `wrong-descriptor-role-count` | Passed | **Unknown** | 0 | TTFFF | False |

Flags are `OwnerReaped/WriterFence/DomainEmpty/StagingRemoved/JournalFinalized`;
`-` means the case has no recovery-final record (recovery loss, ambiguous
intent, no preparation) or used the live dispose path, where the durable verdict
comes from `journal-status`. The dispose-path Confirmed cases
(`positive-one-release`, `stop-before-release`, `integrated-original-deadline`,
`integrated-owner-live-takeover-refusal`) therefore have `DurableVerdict=2` and
`LiveCleanupConfirmed=true`.

Baseline comparison against the preserved producer record
(`artifacts/rc2b-04-b4-impl-01/int01-final/cases`): exactly one classification
flip, `missing-staging-ack` `Confirmed → Unknown`; all other 31 classifications
unchanged; baseline tally 21/11, corrected tally 20/12 in both runs.

## 6. B4 matrix (13 cases, WSL2)

Command: `python3 eng/b4-native-matrix.py --native <private-native> --report <dir>`.
Result: **13/13 passed**, cleanup **12 Confirmed / 1 Unknown**
(`recovery-loss-with-blocked-observer` remains Unknown). Compared with the
producer record (`artifacts/rc2b-04-b4-impl-01/native-final/cases`): zero
classification flips. Raw manifest digest:
`fc986bf4110f864627897cef0a76f6a60615297d7b8e139abedc1a4a46d23d89`.

## 7. Component regressions (WSL2)

Command: `dotnet test <Linux M1 tests dll>` with the B4/cleanup/ownership class
filter used by the producer (`ProtectedCaptureTests`, `RetainedDirectoryCleanupTests`,
`OwnerSafetyDisposalTests`, `OwnerSafetyMonitorTests`, `ReleaseArbiterTests`,
`ReleaseAdmissionTests`, `OwnerProtocolTests`, `Journal`, `ReconcilerTests`,
`OwnerBootstrapTests`, `RecoveryB2Tests`, `RecoveryConnectionDisposalTests`,
`RestrictedControl`, `OwnerHeldWireCodecTests`).

Result: **275 passed / 0 failed / 0 skipped**. The B4-relevant subset is
**12/12** (`ProtectedCaptureTests` 8, `RetainedDirectoryCleanupTests` 3,
`OwnerSafetyDisposalTests` 1). Raw manifest digest:
`75dd671764c011ce85d321dd0fe41a116047056e6ce6ab41861f4a5e5b82ab97`.
One earlier iteration used the mounted `/mnt/c` checkout as
`GAGAMBA_REPOSITORY_ROOT` and failed the `D4_11` fixture build on DrvFs file
timestamps; it was rerun with the producer's private source copy
(`/home/gaga/gagamba-b4-20261031/source`) and passed. That was an
environment-path error, not a source or harness defect.

## 8. Deterministic offline regression

`eng/tests/test_int01_cleanup.py` (stdlib only) — **13 tests, all pass on
Windows Python 3.14.8**; the same module is imported by the native harnesses.
Coverage includes: durable Confirmed + all flags → Confirmed; **durable Unknown
+ all five flags → Unknown with physical improvement recorded** (the exact
B4Q1-F01 shape, with the legacy rule asserted to have returned Confirmed);
missing/malformed/non-object/out-of-range verdict evidence → Unknown; each of
the five flags individually required; non-boolean truthy flag not promoted;
absent record → Unknown; `missing-domain-ack` shape → Unknown with physical
removal visible; dispose live-state and durable verdict both required;
`journal-status` requires `Ok` and a Snapshot; and a source guard that the three
runners use the shared classifier and no longer contain the legacy flag-only
rule.

The whole `eng/tests` suite (32 tests, including the pre-existing qualification
gate tests) passes: `Ran 32 tests ... OK`.

## 9. Cleanup and host audit

After the full matrix runs, repeat run, B4 matrix and component run, the host
audit found **zero** remaining `gagamba*` user units, **zero** live
qualification/controller processes and **zero** `gagamba*` cgroups under
`/sys/fs/cgroup/user.slice`. Test-owned workspaces are intentionally retained
for review under the private qualification root, as in the producer run.
Watchdog actions remain separate from observed recovery/owner cleanup and were
not promoted into any classification. No test-owned service unit, process or
cgroup remained.

## 10. Preservation and unchanged dispositions

The historical producer evidence, its 281-file manifest, the independent Q1
review, the owner decision and all earlier checkpoints are unchanged. This slice
adds a new evidence document and current-status entries only.

- B4Q1-F01 — **Fixed** within the qualification-harness scope by this slice.
- B4Q1-F02 (check-then-remove residual race), B4Q1-F03 (drain-thread robustness)
  and B4Q1-F04 (gated store-failure hook) — **unchanged, still carried** as
  accepted bounded residuals.
- R1-F02 — **still globally OPEN**; not affected by this harness correction.
- C1–C4, Option A, INT-01 acceptance and B4 acceptance — **preserved**.
- No release/recovery authority, no new journal semantics and no production
  activation were introduced.

## 11. Source binding and raw artifacts

Tested runner files and SHA-256 (identical in the committed tree and in every
run's `identity.json`):

```text
408071bd881d9a4ceae77cb161777183936791ae2120dccac8ba47df5856911a  eng/int01_cleanup.py
586f78dfa07035c3e7b9021202829d336b22de8ae689bfea89383e6a1afa8310  eng/int01-native-matrix.py
e6b0a697eda4db33768a3cfabcf727bc812efbfd9cdbb5ac33db1df57b29bed7  eng/int01-fault-matrix.py
9656e284c66a878a2b711c3a3dff107cf6278f20bd30b85a110c2b2c7c7cc285  eng/b4-native-matrix.py
f55d289a394e4313689351280ca44c5bcad56f4c5d67d8ef0195350f2ccfa3b4  eng/tests/test_int01_cleanup.py
```

The native runs executed from the baseline checkout with the corrected runner
files present but uncommitted; each run's `identity.json` records `Head=37a9087`
and the exact runner SHA-256 values above, which are byte-identical to
`360413b`. Raw reports (case JSON, profiles, identity, logs, TRX) are retained
under `artifacts/rc2b-04-b4-q1f01/`; the raw directory is intentionally not
committed. Per-directory manifest digests are recorded in §4–§7 and in the raw
`/tmp/*.manifest` files produced during the run.

## 12. Findings disposition

**B4Q1-F01 — Fixed for the qualification harness; historical producer tally
preserved unchanged.** The corrected INT-01 tally is **20 Confirmed / 12
Unknown**, with `missing-staging-ack` and `missing-domain-ack` both Unknown and
their physical cleanup improvements explicitly visible. Future stages may use
the harness aggregate Confirmed count only under this durable-verdict contract.

## 13. Qualification boundary

This report establishes only the corrected classification behavior of the
native qualification harness on the documented WSL2 Ubuntu 26.04.1 host, the
unchanged INT-01/B4 security assertions, and the deterministic classifier
contract. It does **not** establish native-Linux-host or macOS behavior, VM/HCS
confinement, B5 sealed-C3 durability/deadline reconciliation, B6
installed-artifact attestation, B7 comprehensive lifecycle qualification, Hufu
integration, package qualification, production readiness or release authority.
WSL2 is not native Linux. Historical evidence from another host/SHA is not
silently qualified by this run.
