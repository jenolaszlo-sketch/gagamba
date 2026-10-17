# WP05 Plan Binding and Schema Blocker

Date: 2026-10-17 (Asia/Manila)

Remediation slice: WP05 — single-use release and truthful target-status protocol
(plan binding only; no implementation or qualification executed).

Baseline source SHA: `07a4c69937d61889c442a283c27cdbc2773c8539`
Final source SHA: `07a4c69937d61889c442a283c27cdbc2773c8539` (production source
unchanged; this document is the only change)
Branch: `qualification/ar7-feasibility`
Host OS/version: Windows host; WSL2 Ubuntu reachable for a read-only prerequisite
check (not used for qualification)
Architecture: x86_64
Runtime/SDK version: .NET 10.0.112; gcc 15.2.0; Bubblewrap 0.11.1 (checked only)
Relevant native prerequisites: delegated cgroup v2 subtree
`/sys/fs/cgroup/gaga-delegated/host`; pinned Bubblewrap and seccomp filter
Qualification scope: WP05 plan binding and stop-rule analysis only
Explicit exclusions: no WP05 implementation, no release, no target execution, no
WP06–WP10 work, no WP04/G2 requalification

**Disposition: `WP05 SCHEMA CHANGE REQUIRED - RELEASE DURABILITY NOT REPRESENTABLE IN CURRENT SCHEMA`.**

## 1. Repository binding verified

| Item | Observation |
|---|---|
| Branch | `qualification/ar7-feasibility` |
| HEAD | `07a4c69937d61889c442a283c27cdbc2773c8539` |
| Clean tree | Yes, before this documentation change |
| WP04 independent acceptance commit | `2429101a7557c387e31ea47a8f3ccdd4685ca900` |
| WP04 production freeze | `bac516eeb640ce4ea415043a86579c20948793c2` |
| WP04 qualification/probe commit | `3873d2398b31763b4b1200338ea3947bcb1fc1e1` |
| WP04 qualification evidence/status | `8afca146cd09841c7a8f58b922cbdacd9398f41e` |
| WP04 disposition | `WP04 ACCEPTED - HELD SANDBOX QUALIFICATION COMPLETE` |

The commit chain from the frozen WP04 production source through the final
acceptance link is intact. WP04 was not reopened. No counterexample to an
accepted WP04 property was produced.

## 2. Authoritative WP05 contract

The repository plan is authoritative. WP05 is defined at
[`docs/plans/gagamba-linux-m1-implementation-plan.md`](../../plans/gagamba-linux-m1-implementation-plan.md)
lines 429–441:

- **Objective:** implement the exact release/stop winner and exec/root
  observation rules without enabling integrated target release.
- **Named components:** `Execution/ReleaseArbiter.cs`,
  `Execution/ReleaseDecision.cs`, `Launch/CoordinatorProtocol.cs`,
  `Launch/CoordinatorStatusReader.cs`; `native/linux-m1/exec-coordinator.c`,
  `protocol.h`; `Execution/ReleaseRaceTests.cs`, `Launch/TargetStatusTests.cs`,
  `Native/ReleaseProtocolTests.cs`; an explicit qualification-only coordinator
  build target in `eng/linux-m1/build-native.sh`.
- **Dependencies:** WP04/G2a; durable journal, private capability, bounded
  transport and monotonic clock contracts. Resources/capture are fake
  prerequisite proofs until WP06–WP09.
- **Invariants:** I02/I05/I06/I09; gate linearization after durable intent; one
  atomic nonblocking 128-byte write; no retries; source-bound exec-error and
  confirmation rules; separate direct-child/helper/target status.
- **Boundary:** the live integrated path still cannot release. Qualification
  uses fake transport/model tests and a separately hashed protocol-test
  coordinator that acknowledges then exits before forking the target. The real
  fixture target remains for WP10.
- **Completion/stop:** exactly one winner and at most one release write in every
  schedule; ambiguous delivery/result stays Unknown. Stop if the gate must be
  held over blocking I/O, release must be resent, exec is inferred from pipe EOF
  alone, or coordinator controls reach the target.
- **Review gate:** G2b before WP06.

## 3. Plan-binding answers

1. **What WP05 is:** the release/stop arbitration and target-status protocol
   slice described above; not target execution, resource installation, capture,
   owner orchestration or reconciliation.
2. **Release authority:** the per-execution owner process is the issuer and holds
   the sole release-pipe writer (plan line 126). The recovery scope never
   receives release authority and never calls release (plan lines 126, 215).
   The target receives no control descriptor; FD 4 is coordinator-only (plan
   line 231).
3. **Who may commit:** only the authoritative per-execution owner, through the
   single `ReleaseArbiter` gate, after validating the live capability and all
   Owned prerequisites. The caller's one-shot `Authorize(identity, digest)` is
   accepted only on the original inherited owner channel and cannot construct a
   capability or a release writer (plan lines 126, 209–212).
4. **Durable state transition:** durable `ReleaseIntent` with
   preparation/manifest identity and absolute deadlines is written and flushed
   outside the gate (plan lines 147, 209, 342). The linearization point is the
   in-gate `Owned → ReleaseCommitted` change plus capability consumption, after
   which one release frame is attempted (plan line 211). A crash after durable
   `ReleaseIntent` is conservatively `MayHaveExecuted` (plan lines 116, 164,
   213).
5. **What makes release single-use:** the `PreparedTransaction` capability is
   consumed inside the gate (plan line 211; `PreparationStore.TryConsume`), the
   coordinator permanently closes its release reader after accepting RELEASE
   (plan line 193), exactly one 128-byte write is attempted with no retry queue
   (plan lines 193, 211), and duplicate calls return the retained decision
   without another write (plan line 215).
6. **Owner dies before release:** without a durable `ReleaseIntent`, release is
   impossible; the domain is stopped and reconciled, the capability is lost with
   the owner generation, and the result is setup failure/`NotStarted`
   (plan lines 159–163). If the owner dies after the durable `ReleaseIntent`
   (including before the in-memory commit), recovery conservatively assumes
   `MayHaveExecuted`/`Unknown` and never resends (plan line 164).
7. **Owner dies after durable commitment but before release is observed:**
   `MayHaveExecuted`; preserve any durable trusted root result, otherwise root
   `Unknown`; kill the whole domain, close/drain channels, reconcile once, and
   never replay (plan lines 164–165, 213, 315).
8. **Result when execution cannot be excluded:** `ReleaseDisposition.MayHaveExecuted`
   with `ExecDisposition.Unknown`/`RootResultKind.Unknown` and
   `ExecutionOutcome.Unknown` as appropriate (plan lines 116–119, 315). It is
   never collapsed to success, refusal or confirmed-not-executed. Cleanup stays
   an independent field and cannot convert Unknown to success (plan lines 37,
   324).
9. **Deferred to WP06–WP10:** WP06 resource installation/readback; WP07 bounded
   capture and stop notification; WP08 owner orchestration, setup-inclusive
   deadlines and caller loss; WP09 reconciliation, receipts and G3; WP10 actual
   fixture target release/execution. WP05 uses fake resource/capture
   prerequisites and a no-target protocol build (plan lines 371, 433–437,
   443–511).
10. **Does WP05 execute the real fixture target?** No. Native target release is
    disabled in WP01–WP09. WP05 tests the RELEASE codec against a qualification
    coordinator that records acceptance and exits before any target fork; that
    binary cannot pass integrated artifact pinning. WP10 enables the fixture only
    after G3 (plan lines 371, 435, 437).

## 4. Existing release-related code discovered

| Existing artifact | State |
|---|---|
| `Execution/TransactionStateModel.cs` | WP01 pure model: `TryCommitRelease`, retained release decision, stop-before/after-commit handling, fake `ITransactionJournal` and `IReleaseTransport`. Not durable, not native, not a production release path. |
| `Preparation/PreparationStore.cs` | Single-use `TryConsume` with exact reference identity, issuer/generation/digest/expiry checks; `Invalidate` aborts. |
| `Preparation/PreparedTransaction.cs` | Private capability; only the store mints it; no serialization or clone route. |
| `Launch/HeldExecution.cs` | Holds the owner-side control writer but exposes only `CloseAbortControl`; no release write. |
| `Launch/IHeldLauncher.cs` | Prepare-held and abort only; no release operation. |
| `native/linux-m1/protocol.h` | Only `HELD=1` and `PROTOCOL_FAILED=6`; no `RELEASE` kind. |
| `native/linux-m1/exec-coordinator.c` | Explicitly release-disabled; after HELD it polls EOF and fails; no target fork. |
| `Launch/CoordinatorProtocol.cs` | Decodes only HELD and PROTOCOL_FAILED; requires the HELD flags `0x000f` including release-disabled. |
| `Launch/LaunchDescriptorMap.cs` / adapter | FD 4 is reserved as the release reader, but no release writer is exposed by the managed path. |
| `Recovery/OwnershipRecord.cs`, `Recovery/OwnershipJournal.cs` | Accepted durable journal/writer fencing, but no release state, release identity or release deadlines. |
| Qualification build | No WP05 qualification-only coordinator target exists in `eng/linux-m1/build-native.sh`. |

## 5. Schema blocker

The plan requires a durable `ReleaseIntent` containing the preparation/manifest
identity and absolute deadlines before entering the release gate (plan lines 147,
209, 342). The accepted durable ownership journal cannot represent it:

- `OwnershipSnapshot` (`Recovery/OwnershipRecord.cs:69–89`) carries
  `PreparationStage`, whose only values are `None`, `AllocationIntent`,
  `AllocationRegistered` and `Prepared` (`OwnershipRecord.cs:34–40`). There is no
  release-intent, committed, delivered or ambiguity state.
- The snapshot has no absolute-deadline fields. The only preparation record is
  `RecordedPreparation` (execution/issuer/generation/preparation IDs and the
  manifest digest), not the release deadlines required by the plan.
- The codec is `OwnershipSnapshotCodec.CurrentVersion = 2`
  (`OwnershipRecord.cs:95`). The stage is written and read as one byte
  (`OwnershipRecord.cs:122–128, 211`), and the decoder rejects every other
  version (`OwnershipRecord.cs:192–195`). Adding release state or deadlines
  changes the durable record shape and requires a versioned schema change.
- The accepted WP03 evidence describes exactly this version-2 field set and
  explicitly excludes release credentials/writer
  ([`wp03.md`](wp03.md) lines 141–149). The journal's mutation fence is the
  accepted G2 writer lease (`OwnershipJournal.cs:358–416`); release durability
  must be committed through that same fence to preserve G2 writer fencing.
- A separate second release journal would be parallel release state, contrary to
  the one-snapshot model (plan line 332), the existing-journal preference, and
  the prohibition on parallel release state. It would also fail to prove that
  release commitment is fenced by the accepted ownership writer.

The plan says a changed trust boundary, authority, state transition or wire
format requires review before implementation continues (plan line 101). WP05's
expected file list does not authorize a recovery-schema change, and the
authoritative stop rule for this condition is explicit: stop before migration
implementation and report the schema status. No migration, schema edit,
production code, test, coordinator build or qualification run was attempted.

## 6. Checks performed

| Check | Result |
|---|---|
| Repository/branch/HEAD/clean tree | Verified as in section 1 |
| Commit chain and WP04 preservation | Verified |
| Plan/source inspection | Read-only; release-related inventory in section 4 |
| WSL prerequisite reachability | Ubuntu reachable; boot ID `18785e6a-be5e-41e6-8f33-1084ede27841`; `/sys/fs/cgroup/gaga-delegated/host` present; Bubblewrap 0.11.1; gcc 15.2.0; .NET 10.0.112. Informational only; the check ran as root and was not a WP05 qualification. |
| WP05 builds/tests | Not run; the binding stop precedes implementation |
| Release / target execution | None |
| Cleanup | No execution resources were created; only read-only inspection |

## 7. Qualification boundary and unblock

This record does **not** claim any WP05 property: not release authority, not
single-use release, not release/stop linearity, not crash-boundary behavior, not
truthful target status, and not any WP04/G2 regression. It is a plan/schema
blocker, not a product-defect counterexample to WP04 or G2. WP06–WP09 and WP10
remain deferred; the wrong-audit-architecture seccomp generator decision remains
separate.

Unblocking requires a separately reviewed decision that either:

1. authorizes a versioned extension of the accepted durable ownership snapshot to
   represent `ReleaseIntent`, release commitment/delivery and the absolute
   deadlines, including recovery semantics, migration/compatibility rules and
   renewed G2 evidence; or
2. explicitly amends the WP05 dependency/scope to a different reviewed durable
   record model, with the same writer-fencing and crash-boundary evidence.

Until that review, WP05 implementation must not begin. Any later implementation
must then restart qualification on the amended plan and exact revision.
