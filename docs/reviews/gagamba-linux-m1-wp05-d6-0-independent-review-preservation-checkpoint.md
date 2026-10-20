# WP05 D6.0 Independent Contract Design Review - Preservation Checkpoint

Status: **PRESERVED - REVIEW ONLY; NO CONTRACT APPROVED**

Role: preservation engineer. This checkpoint preserves the independent GLM
D6.0 review and its required amendments. It does not revise the design, approve
any C1-C4 contract, authorize production remediation, accept a gate or claim
release qualification.

## A. Revision binding

### Frozen production and prior preservation

- R12: `38f6981b2a60daab2df37c92913a6760c1bef131`, tree
  `6854675d6b37a49d5302d6104103bdbfbc268040`.
- Previous baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`.
- Frozen harness Q3: `36a124d7dff98ecfaf956340767fe1768ad5b17f`.
- D4 preservation: `5c58b065c37a71c8f392a617f725822ffb64c171`.
- D5 preservation: `578b2084cfa09be76e73d0fb26e09f8d823f9ae6`.

### D6.0 producer chain

- Base: `578b2084cfa09be76e73d0fb26e09f8d823f9ae6`.
- D6.0-A: `726e081b1b9bac9037f323558a91e62d67a75163`.
- D6.0-B: `1e0f126b4617ba5b58f01258278a6dd78c19a63c`.
- D6.0-C / design HEAD: `23df1c566e7d8f072d924f374bf6891c726c03fe`.
- Branch: `analysis/wp05-d6-0-release-contract-decisions`.

Ancestry verified: D6.0-A parent = D5 preservation; D6.0-B parent = A;
D6.0-C parent = B. The producer chain adds nine documentation files only and
leaves R12 production/test/native/artifact sources unchanged. All C1-C4
decision records remain `PROPOSED - NOT APPROVED`.

The new preservation commit parent is the exact D6.0-C design HEAD
`23df1c566e7d8f072d924f374bf6891c726c03fe`; it is a documentation-only
commit adding this checkpoint, `docs/handoff.md` and `docs/work-queue.md`.
Its full commit SHA is recorded in the final preservation handoff.

### Independent D6.0 review chain

- Q1: `588ecf7f2a30e905397c6e3551b03c31aea7b6e0` - main review (844 lines).
- Q2: `29c48380b610fc96b0ee6e2dd8e0c0b22fe69423` - matrix (129 lines) and
  interaction register (261 lines), combined 390 lines.
- Q3: `2a235c391673258023d3b3058ad7bbb31cc20406` - required design changes
  and approval handoff (276 lines).
- Branch: `review/wp05-d6-0-independent-contract-design`.

Ancestry verified: Q1 parent = design HEAD; Q2 parent = Q1; Q3 parent = Q2.
The actual review chain is **three commits and four changed documentation
files**. The completion report's "four documentation commits" wording is a
count discrepancy only; no fourth commit exists and none was created. Review
diff is documentation-only and no producer document was rewritten.

### Review document blobs

| Document | Git blob |
|---|---|
| `docs/reviews/gagamba-linux-m1-wp05-d6-0-independent-contract-design-review.md` | `ef3c1cad62739699e29edfd7d6c5e01ccda14c4a` |
| `docs/reviews/gagamba-linux-m1-wp05-d6-0-contract-review-matrix.md` | `5352664618e733acc9286f183ad0cca40ad46227` |
| `docs/reviews/gagamba-linux-m1-wp05-d6-0-contract-interaction-register.md` | `e269351813a4e2af06afa7f5a08c44fe293ad336` |
| `docs/reviews/gagamba-linux-m1-wp05-d6-0-required-design-changes-and-approval-handoff.md` | `65392a0fc3221f570ceef884d87307fea854863d` |

### Original C1-C9 provenance (A-PKG-1)

- Original required-changes blob: `73eb09af27c5dbc896739ebf48489862ec13e0a9`.
- Published source branch/commit:
  `review/wp05-release-plan-adversarial-review` at
  `e4b0a798e944ddfb144901c63b0c444e7980533b`.
- This branch is **not an ancestor** of the D6.0 base. It is reachable through
  its published ref and the D5-lineage preservation checkpoint
  `c4022d8a45fb523008b7132ce83344b915fd2012`. It was not merged or
  cherry-picked into D6.0.

## B. Independent review outcome

The exact overall disposition from Q3 is:

> **Option B**: the D6.0 package's defect-to-contract mapping is
> independently confirmed against the R12 source; no structural defect was
> found in any proposal; the named amendments are required before any
> approval question is final; two human decisions are outstanding (C3
> policy choice; per-contract approvals); no gate is accepted; release
> qualification remains unclaimed and unqualified.

No structural rejection of the design package. No contract is approved. The
review-ready verdicts are preserved separately:

| Contract | Verdict | Required amendments |
|---|---|---|
| C1 / R09 | DESIGN SUPPORTED WITH REQUIRED AMENDMENTS | A-C1-1, A-C1-2, A-C1-3 |
| C2 / R08 | DESIGN SUPPORTED WITH REQUIRED AMENDMENTS | A-C2-1, A-C2-2, A-C2-3 |
| C3 / R27 | CONTRACT CHOICE REQUIRED | A-C3-1..A-C3-4 + human zero-wait policy choice |
| C4 / R02/R06 | DESIGN SUPPORTED FOR HUMAN DECISION | A-C4-1 documentation discipline |

## C. Required amendment register

The authoritative wording is Q3 §R1 (blob `65392a0f...`). This compact table
preserves every identifier; no correction is represented as implemented.

| ID | Contract | Required correction | Blocking? | Next action |
|---|---|---|---|---|
| A-C1-1 | C1 | State stop invocation as tail-of-call: after failing request path releases `_journalGate`, without holding `_gate` across journal I/O; add concurrent opposing-order test to prove no gate inversion. | Blocking for approval text | Fold into C1; test plan |
| A-C1-2 | C1 | Name the exact retained ambiguous decision vocabulary and verify exhaustiveness of every `ReleaseDecisionKind` switch; update C4-dependent rows. | Blocking for approval text | Fold into C1/C4 |
| A-C1-3 | C1 | State capability disposition in the ambiguous window: rely on C2 stop consumption and document transient live-while-wedged window, or close directly; human choice. Reviewer prefers C2 reliance. | Blocking for approval text | Fold into C1 |
| A-C2-1 | C2 | State consumption inside `_gate` acceptance blocks for normal, clock-unavailable and overflow paths; duplicate-stop re-entry does not re-consume; exactly-once at first acceptance. | Blocking for approval text | Fold into C2; invert `IsLive` tests |
| A-C2-2 | C2 | Preserve release-first semantics: failed `TryConsume=Consumed` pairs with already-consumed capability; stop stands; `ReleaseAlreadyCommitted` includes consume-before-transport. | Blocking for approval text | Fold into C2 |
| A-C2-3 | C2 | Include the CE-03 out-of-model declaration verbatim: multi-instance window outside accepted single-arbiter model, closed by capability contract not composition. | Blocking for approval text | Fold into C2 |
| A-C3-1 | C3 | Require an epoch-corroborated monotonic clock; durable record timestamps are QPC transaction-clock epoch; `OwnerDomainClock` is incompatible, `LaunchDomainClock` compatible; fail to quarantine/Unknown when epoch identity is not established; corroboration test. | Blocking for C3 contract | Fold into C3; clock test |
| A-C3-2 | C3 | Reconciler aggregation must map attempted-but-not-empty to Unknown/quarantine; current unconfirmed stop maps Failed to `ReconcileOutcome.Failed`; require tested aggregation amendment. | Blocking for C3 contract | Fold into C3; Reconciler test |
| A-C3-3 | C3 | Document no-StopRecord restart after runtime-accepted/unpersistable stop as cleanup-only allowance, never a fresh stop deadline; C2 prevents release authority return. | Blocking for C3 contract | Fold into C3; I6 test |
| A-C3-4 | C3 | Resolve conflict between "domain already empty: no unnecessary stop" and unconditional kill before population observation; either add pre-observation amendment or rewrite to "no unnecessary WAIT" and record kill. | Blocking for C3 contract | Human/producer choice |
| A-C4-1 | C4 | Bind exact R12 reason strings per refusal class and clarify snapshot-liveness/identity checks are inside `_journalGate` but semantically before durable intent; prevent outside/gate taxonomy confusion. | Non-blocking discipline | Fold into C4 |
| A-PKG-1 | Package | Cite publisher branch `review/wp05-release-plan-adversarial-review` by the original blob ID (`73eb09af...`) because it is not an ancestor. | Non-blocking | D6.1 text |
| A-PKG-2 | Package | Rephrase "production scope": `ReleaseArbiter` and `Reconciler` are uncomposed components with zero production call sites; adoption evidence is component-level until WP08 composition. | Non-blocking | D6.1 text |
| A-PKG-3 | Package | Add named acceptance row "ambiguous wedge then salvaged clock-unavailable stop decision"; both halves exist as D2 probes but no composed row. | Non-blocking | D6.1 test list |
| A-PKG-4 | Package | Evidence/gate matrix self-declaration (no gate accepted; diagnostics only) concurs with required-changes C8 and D5 H1-H5. | No change | Preserve |

The separate **human policy choice** is whether an expired deadline authorizes
kill plus one observation as the bounded urgent stop, or another bounded
interval. The reviewer does not answer it. C3 remains `CONTRACT CHOICE
REQUIRED`.

## D. Critical findings

### C1 runtime stop invocation and lock ordering

Q3 A-C1-1 identifies a real lock-order hazard in the proposed ambiguous path:
the failing release has released `_journalGate` at `ReleaseArbiter.cs:115` and
takes `_gate` at 118; `RequestStop` takes `_gate` at 230 then `_journalGate`
at 272. The file invariant forbids journal I/O while `_gate` is held. C# monitor
re-entrancy can hide an inverted nesting, so a concurrent opposing-order test
is required. The design is supported only with this amendment.

### C2 capability invalidation linearization

Q3 A-C2-1..3 confirm consumption is feasible with the store-local single-use
API, but require per-path placement inside `_gate`, no re-consume on duplicate
stop, preservation of release-first/`ReleaseAlreadyCommitted` semantics, and
the CE-03 out-of-model declaration. The earlier proposal's "TryConsume" call is
not automatically safe in every interleaving without those statements/tests.

### C3 clock-epoch mismatch

This is substantive, not documentation-only. Durable StopRecord timestamps use
the QPC-based transaction clock in `OwnerClock`
(`OwnerPreparationTransaction.cs:21-32`). `IDomainClock` has two divergent
production implementations: `OwnerDomainClock` uses `Stopwatch.StartNew()` with
a construction-moment epoch (incompatible with the persisted timestamps), while
`LaunchDomainClock` is QPC-based (compatible). Direct subtraction without
epoch corroboration can produce an invalid remaining allowance. C3 must require
epoch identity/corroboration and fail to quarantine/Unknown when it cannot be
established.

### C3 outcome-mapping gap

The proposed "attempted but not empty -> Unknown/quarantine" is not current
aggregation behavior. An unconfirmed stop is a `Failed` component mapping to
`ReconcileOutcome.Failed` (`Reconciler.cs:234-235,429-436`); Unknown/quarantine
arises from Unknown components (`:437-442`) or missing persistence (`:450-459`).
C3 requires a tested aggregation amendment distinct from operation failure.

### C3 zero-wait expiry policy

The reviewer confirms the mechanism (kill first, then one observation) is
mechanically supported by `StopAndAwaitEmpty`, but the **policy** belongs to the
human holder. No choice is made here. The meaning of a zero stop allowance is
not approved.

### C3 already-empty domain conflict

The producer's "domain already empty: no unnecessary stop" row conflicts with
today's unconditional kill when the DomainStop bit is unset
(`Reconciler.cs:217-248`). The design must either add an explicit
pre-observation amendment or rewrite the row to "no unnecessary WAIT" and
record the kill. Silence is not acceptable.

### C4 documented semantics

C4 is supported for human decision. A-C4-1 requires exact reason strings per
class and a clear distinction between lock placement and the semantic
outside/gate taxonomy. The minimal `Refused` + discoverable reason/metadata
form is accepted; a `ConcurrentRefusal` kind remains optional and unnecessary
for the current consumer.

### Component integration boundary

Q3/A-PKG-2 verified `ReleaseArbiter` and `Reconciler` have **zero production
call sites** in the reviewed R12 source. They are implemented components used
by test-only paths, not a completed production owner lifecycle. This is a
component-versus-integration boundary, not evidence that the components are
unused or defective. D6.0 may decide component contracts; production
composition and end-to-end qualification remain future work.

## E. Interaction findings

The full Q2 register is blob `e2693518...`. It covers all six pairs:

- C1 x C2: tail-of-call stop invocation and capability consumption; durable
  stop failure is harmless; wedge remains sealed until death/recovery.
- C1 x C3: durable StopRecord if it lands; otherwise no-record cleanup-only
  allowance; C3 clock applies only to reconciler path.
- C1 x C4: retained ambiguous decision; later Duplicate; C1 vocabulary must be
  fixed before final C4 text.
- C2 x C3: C2 closes release authority; C3 bounds cleanup; expiry kill/one
  observation is policy-gated.
- C2 x C4: stop-winning release consumes authority; release-first preserved.
- C3 x C4: independent by construction; C4 is live-owner vocabulary, recovery
  classification remains separate.

Scenarios I1-I8 and their counterexamples/acceptance conditions are preserved
in the Q2 register; no additional amendment beyond Q3 was found. No structural
pairwise defect was found.

## F. Remaining work and handoff

1. D6.1 documentation-only design revision folding A-C1, A-C2, A-C3, A-C4 and
   A-PKG items; C3 zero-wait and A-C3-4 remain human choices.
2. Focused independent re-review of the revised package.
3. Individual human decisions: C1, C2, C3 (policy + contract), C4.
4. Separately authorized production remediation, one tranche at a time.
5. Independent requalification and new source-bound evidence; no automatic
   transfer from R12/D4/D5.
6. H1 D5-O05 status integration remains open; H2 destructive descriptor
   exclusions unclaimed; H3 optional raw wait4/pidfd witness remains optional;
   H4 C1-C4 unresolved; H5 D5 plan-order note preserved.
7. WP08 owner orchestration, WP10 target/production status, full G3 and
   portability remain deferred.

No gate is accepted. C1-C4 are not approved. The producer's remediation
proposal is not authorized.

## G. Preservation verification

- D6.0 producer chain and review chain ancestry: verified as above.
- D6.0 producer diff from D5 preservation: nine documentation files only.
- Review diff from D6.0-C: four documentation files only; no producer document
  rewritten.
- Production `src`/`native` diff from R12: empty.
- Frozen harness unchanged.
- D4/D5 preservation refs unchanged.
- Review documents retrievable at Q3; producer documents retrievable at
  D6.0-C; original C1-C9 source reachable through its published review branch
  and not made ancestral.
- The preservation commit is documentation-only: this checkpoint plus handoff
  and work-queue updates.
- C1-C4 remain unapproved; C3 policy choice remains open; H1-H5 preserved.

Publication state and remote verification are recorded in the final handoff
report after exact-ref publication.
