# Gagamba Linux M1 WP05 D6.8 — GLM Independent C2 Review Preservation Checkpoint

Preservation engineer: DeepSeek V4.1 (documentation, publication and independent
remote verification only; no production change)
Scope: WP05 D6.8 C2 / R08 only. C1, C3 and C4 remain human-approved contracts
without authorized implementation. This checkpoint preserves the D6.8
implementation and GLM's independent Option A review; it is not Linux runtime
qualification, G3 acceptance, implementation authorization for any other
tranche, or release authorization.

## 1. Exact D6.7 approved base

| Item | Verified value |
|---|---|
| D6.7 human-decision preservation | `5993989e148b2e305a410c582eeaa1321bb60637` |
| Base tree | `8b0fed7cd9262c909883b7d002b6644918752892` |
| Base `src/` tree | `406f52da43b62920128151b2f5706c32d45b6db7` — verified identical to frozen R12 `src/` |

The D6.7 approval record is
`docs/decisions/gagamba-linux-m1-wp05-d6-7-human-contract-approvals.md`
(C2 "Stop-first capability invalidation (R08)" = `APPROVED`; overall status
`HUMAN CONTRACT APPROVED - IMPLEMENTATION NOT AUTHORIZED`). The approved C2
contract text is `docs/decisions/wp05-d6-0-c2-stop-capability.md` with the
D6.1 A-C2-1/A-C2-2/A-C2-3 revision.

## 2. D6.8 producer commit chain

Linear chain, each commit the immediate parent of the next (verified via
`git log --format="%H %P"`):

```text
5993989e  docs(wp05): preserve D6.7 GLM Option A human decision integrity review
    |
9bafb411  test(wp05): add C2 stop-capability regression coverage
    |
9992e721  fix(wp05): consume release capability on accepted stop
    |
665b18fd  docs(wp05): record C2 capability invalidation evidence
```

| Commit | Full SHA | Subject |
|---|---|---|
| D6.8.1 tests-first | `9bafb411035567e007d105587247a9b3712b7bf1` | `test(wp05): add C2 stop-capability regression coverage` |
| D6.8.2 production | `9992e7211f60d9a9a70ec4a6e4bdf907790eef19` | `fix(wp05): consume release capability on accepted stop` |
| D6.8.3 evidence (producer HEAD) | `665b18fdca15f8ed30ae4ca93b490f29bf41a181` | `docs(wp05): record C2 capability invalidation evidence` |

No merge, amend, rebase or cherry-pick was performed on this chain.

## 3. Producer source and tree identities

| Item | Verified value |
|---|---|
| Producer branch | `implementation/wp05-d6-8-c2-stop-capability` |
| Producer HEAD | `665b18fdca15f8ed30ae4ca93b490f29bf41a181` |
| Final producer tree | `ad2df42c80df1ffc6dc52673030de3790761723d` |
| Production commit tree | `366f52b4dbd9f0099227b5df3c38d87cb94855a3` |
| Implementation `src/` tree | `84cfabb8f82e3ac4061776cc9a9a8c4d5354a126` |
| `ReleaseArbiter.cs` base blob | `9f8533fd745c04b060c97c80c68da3724643922a` |
| `ReleaseArbiter.cs` implementation blob | `dca9ea1d6feea9fdb752de7d3e90f80431b85c58` |
| Frozen R12 production | `38f6981b2a60daab2df37c92913a6760c1bef131`, tree `6854675d6b37a49d5302d6104103bdbfbc268040` — unchanged |
| Frozen WP05 harness Q3 | `36a124d7dff98ecfaf956340767fe1768ad5b17f`, tree `f764722997130f06c8a62c45a4230805aa538ea5` — unchanged |

All identities were verified directly with `git rev-parse` before preservation;
R12 and Q3 were not altered.

## 4. GLM independent review identity

| Item | Verified value |
|---|---|
| Review branch | `review/wp05-d6-8-glm-independent-c2` |
| Review commit | `959f2d3162bdfbecac2723f50acae8459b2fa310` (`review(wp05): record D6.8 GLM independent C2 implementation review`) |
| Verified immediate parent | `665b18fdca15f8ed30ae4ca93b490f29bf41a181` |
| Review document | `docs/reviews/gagamba-linux-m1-wp05-d6-8-glm-independent-c2-implementation-review.md` |
| Review-document blob | `dee4ca9244f5a8c5613152b9a3a9a90a96294ed2` |
| Review commit scope | exactly one review document, +457/-0 |

## 5. Exact Option A disposition (verbatim)

`WP05 D6.8 GLM INDEPENDENT C2 IMPLEMENTATION REVIEW COMPLETE - OPTION A: C2 AUTHORITY INVARIANT SUPPORTED BY SOURCE AND DETERMINISTIC TESTS; NO BLOCKING FINDINGS; LINUX RUNTIME QUALIFICATION OUTSTANDING; C1 C3 C4 IMPLEMENTATION NOT AUTHORIZED; RELEASE NOT QUALIFIED`

The review reports: correct first-stop capability consumption under `_gate`;
safe `TryConsume` state transitions for every reachable store state; no new
journal I/O under `_gate`; no lock inversion; shared-capability closure across
arbiter instances; failed durable stop cannot permit a second release through
the consumed capability; release-first behavior preserved; no unauthorized
C1/C3/C4 changes; zero blocking findings.

## 6. C2 source implementation summary

Single production file changed: `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs`
(+17/-3, blob `9f8533f…` -> `dca9ea1…`).

- `RequestStop` calls the new private `ConsumeStopCapability()` under `_gate`
  immediately after the duplicate-stop early return and before all three
  first-acceptance branches (clock unavailable, deadline overflow, valid
  deadline).
- `_stopAccepted = true` was consolidated to that same point; the three branch
  decisions, deadlines and reasons are unchanged from the baselined R12
  behavior.
- Consumption uses the existing issuing-store single-use
  `PreparedTransaction.TryConsume`; it is independent of `_journalGate` and of
  the later durable StopRecord write, which remains after the gate and is
  unchanged.
- `ConsumeStopCapability()` treats `TryConsume == true` as irreversible
  consumption; treats refusals that leave the capability non-live
  (`Consumed`/`Aborted`/`Expired`/`Foreign`, disposed) as "stop still accepted,
  no reactivation"; and throws a fail-closed `InvalidOperationException` only
  if a refusal would leave the capability still live — a state the GLM review
  found unreachable for store-issued capabilities under a monotonic clock and
  safe on unwind (no partial state, no journal state touched).

The GLM review independently confirmed the linearization point, the authority
invariant (`PreparedTransaction.IsLive == false` after accepted first stop in
every required condition), at-most-one-physical-RELEASE, lock ordering
(arbiter `_gate` -> store `_gate`; no `_journalGate` under `_gate`), and
release-first/duplicate-stop preservation by source inspection plus
deterministic tests.

## 7. Independently reproduced results (producer evidence)

GLM's independent reproduction, Windows-hosted, .NET SDK 10.0.401, net10.0
Release (same profile as the producer's recorded environment):

| Evidence | Reported (producer) | Independently reproduced (GLM) |
|---|---|---|
| Baseline focused tests | 29 passed | 29 passed |
| Tests-first red phase | 41 total / 30 passed / 11 failed | 41 total / 30 passed / 11 failed (exact failure list matched; `C2_T03` and the overflow probe passed red as the producer honestly disclosed) |
| Green focused tests | 41 passed | 41 passed |
| Affected profile (`Category!=ProcessGlobalReaping&Category!=ProcessGlobalHostState`) | 423 passed / 86 skipped / 0 failed | 423 passed / 86 skipped / 0 failed |
| Whole test assembly | 424 passed / 185 skipped / 0 failed | arithmetic verified from per-class runs: 509 + 100 process-global-class tests = 609 total; 423 + 1 = 424 passed; 86 + 99 = 185 skipped; 0 failed |
| Solution build | 0 warnings / 0 errors | 0 warnings / 0 errors |
| Independent GLM review | Option A, zero blockers | Option A (this record) |

These results are attributable to the recorded Windows-hosted execution
environment only. Linux-only classes (including the updated `D5_CR3b`
expectation in `Wp05D4SupervisorTests.cs`) skipped on that host; they were not
executed and are not qualified. The frozen producer evidence document
`docs/evidence/linux-m1/wp05-d6-8-c2-capability-invalidation.md` was not
revised during preservation.

## 8. Verified production/test diff scope

Full producer-series diff `5993989..665b18f` contains exactly five files
(+794/-14), and nothing else:

| File | Change |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs` | +17/-3 (production commit only) |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05D68C2StopCapabilityTests.cs` | +432 (new, C2-T01..T12) |
| `tests/Gagamba.Execution.Linux.M1.Tests/Execution/Wp05ReleaseContractProbes.cs` | +16/-10 (four R12 counterexample probes converted to C2 regressions) |
| `tests/Gagamba.Execution.Linux.M1.Tests/D4/Wp05D4SupervisorTests.cs` | +1/-1 (`capability_live` True -> False in `D5_CR3b`, Linux-only) |
| `docs/evidence/linux-m1/wp05-d6-8-c2-capability-invalidation.md` | +328 (new) |

Verified absent: C1 ambiguous persistence, C3 durable-deadline/Policy A/typed
stop outcomes, C4 duplicate/refusal redesign, Owner orchestration, WP08/WP10
integration, journal schema changes, native process supervision changes, new
release authority, packaging/CI/release configuration. The historical unsafe
R12 probe behavior remains recoverable at the base commit and is no longer
asserted as desired by the active suite. None of these five files was modified
in the preservation commit.

## 9. Findings F-01 and F-02 — both explicitly OUTSTANDING

Neither finding is remediated by D6.8 preservation, and neither may be read as
closed. The producer evidence document and the GLM review remain unchanged.

- **F-01 — overflow-path test discrimination gap (non-blocking, OPEN).**
  `C2_T03` and the converted overflow probe set the clock to
  `long.MaxValue - 2_000_000_000L` while the harness capability expires at
  `1_000_000_000`, so the capability is already expired at the stop and
  `IsLive == false` holds via store expiry, not via C2 consumption. The
  producer disclosed this honestly. GLM determined that a genuinely
  live-capability overflow case **is** constructible with existing facilities
  (mint with an expiry near `long.MaxValue`, then advance the clock to
  `long.MaxValue - 2_000_000_000L`) and classified the gap non-blocking
  because the shared unconditional consume call site is red-proven on the
  other branches. No such test exists yet; adding it is follow-up work.
- **F-02 — duplicate-stop evidence wording (non-blocking, OPEN).**
  The evidence document's claim that the `C2_T05` counting-clock delta proves
  "no `TryConsume`/`IsLive` read" is an overstatement: `TryConsume` returns
  `Consumed` and `TryLookup` short-circuits on consumed entries before their
  clock reads, so the count cannot detect a refused second consume. The GLM
  review confirmed the exactly-once property holds by source branch ordering
  (duplicate early return precedes consumption; no other reachable consume call
  site in `RequestStop`). The evidence wording remains uncorrected.

## 10. Linux qualification limitations

- All executed evidence is Windows-hosted. Linux-only classes — including the
  updated `D5_CR3b` C2 expectation — skipped and are unqualified.
- Native Linux runtime qualification of the C2 behavior remains outstanding
  with its own approved plan; no Linux runtime execution is claimed by D6.8
  preservation or by the GLM review.
- Production Owner integration (WP08/WP10) remains deferred; C2 evidence is at
  the `ReleaseArbiter` component seam only (`C2_T11` is component-level).
- Windows-hosted unit-test success does not establish native Linux
  qualification.

## 11. Release and implementation authorization boundaries

D6.8 preservation authorizes nothing beyond recording the reviewed checkpoint.
It does not authorize: C1 ambiguous-persistence implementation; C3 deadline or
Policy A runtime implementation; C4 duplicate/concurrency implementation;
WP08/WP10 integration; G3 acceptance; journal schema changes; production
deployment; NuGet publication; release qualification. The D6.7 C1–C4 human
contract approvals remain valid, but implementation is separately authorized
per tranche. C2 source behavior is independently supported by the GLM review;
native Linux runtime qualification remains pending; WP05 release remains NOT
QUALIFIED.

## 12. Sibling review and preservation ancestry

```text
5993989e  D6.7 preservation
    |
9bafb411  C2 regression tests
    |
9992e721  C2 production correction
    |
665b18fd  C2 implementation evidence (producer HEAD)
    |
    +---- 959f2d31  GLM independent review        (review/wp05-d6-8-glm-independent-c2)
    |
    +---- (this)    D6.8 preservation             (preservation/wp05-d6-8-glm-independent-c2-review)
```

The review and preservation are separate sibling commits with the same
immediate parent `665b18fd...`; neither is an ancestor of the other. This
preservation commit is a documentation-only child of the producer HEAD, not
based on the review. Its full SHA is recorded in the handoff and work-queue
entries at commit time and is re-verified from a fresh clone after publication.

## 13. Next recommended work

1. **D6.8-F01 — live-capability overflow regression** (focused follow-up, not a
   production defect): construct a genuine live capability with a near-maximum
   clock; prove it is live before stop acceptance and consumed afterward, with
   no fabricated durable StopRecord.
2. **D6.8-F02 — duplicate-stop evidence precision** (focused follow-up):
   correct the counting-clock claim, preserve the source-level exactly-once
   evidence, and strengthen test observability only if later qualification
   requires it.
3. **Linux-native C2 qualification** under the approved plan.
4. **C1 — Ambiguous ReleaseIntent (R09)**: the next proposed implementation
   tranche per the approved dependency order; requires its own separate
   explicit authorization before any production work begins.

## Disposition

`WP05 D6.8 C2 IMPLEMENTATION AND INDEPENDENT REVIEW PRESERVED - GLM OPTION A; ZERO BLOCKING FINDINGS; F-01 AND F-02 TRACKED; LINUX RUNTIME QUALIFICATION OUTSTANDING; C1 C3 C4 IMPLEMENTATION NOT AUTHORIZED; RELEASE NOT QUALIFIED`
