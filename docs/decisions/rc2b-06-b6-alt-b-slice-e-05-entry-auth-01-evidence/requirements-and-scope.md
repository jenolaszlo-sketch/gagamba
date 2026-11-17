# Requirements and implementation scope

All 24 frozen rows remain binding according to their original scope. The reassessment's five Ready and nineteen Qualified dispositions carry forward, with zero blocked. The row-level tests, expected observables and remaining conditions are incorporated by reference. [Frozen requirements](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/requirements.md) · [Readiness matrix](../rc2b-06-b6-alt-b-slice-e-05-entry-owner-02-evidence/requirements-readiness.md)

The location names below identify the corresponding `ValidationCustodyModel` filename suffixes in the [exact allowlist](bindings-and-allowlist.md#exact-six-file-additive-allowlist). Qualification evidence and execution of existing checks grant no additional implementation paths.

| ID | Frozen subject | Implementation or verification location | Entry disposition |
|---|---|---|---|
| E5-R01 | Fixed launcher identity and closure | Model for opaque bindings, with the artifact gate downstream | Qualified |
| E5-R02 | Pre-only operand shape | Data, Model, Tests, Vectors | Ready |
| E5-R03 | Independent executable and closure comparisons | Data, Model, Tests, Vectors | Qualified |
| E5-R04 | Independent artifact and approval-chain comparisons | Data, Model, Tests, CompositionTests, Vectors | Qualified |
| E5-R05 | Original association and protocol IDs | Data, Model, Tests, Vectors | Qualified |
| E5-R06 | Stage-specific evidence facts | Model, Tests, LifecycleTests, Vectors | Qualified |
| E5-R07 | One-way lifecycle | Model, LifecycleTests, Vectors | Qualified |
| E5-R08 | OFFER, drop and consumption | Model, LifecycleTests, Vectors | Qualified |
| E5-R09 | Deadlines and monotonic observation | Data, Model, LifecycleTests | Qualified |
| E5-R10 | Observed stop | Model, LifecycleTests | Qualified |
| E5-R11 | Ordered refusals and null output | Model, Tests, LifecycleTests | Ready |
| E5-R12 | Terminal behavior and disposal | Model, Data, LifecycleTests | Ready |
| E5-R13 | Defensive copying | Data, Model, LifecycleTests, Vectors | Ready |
| E5-R14 | Concurrency and linearization | Model, LifecycleTests | Ready |
| E5-R15 | Withdrawal boundary | Model, LifecycleTests, Vectors | Qualified |
| E5-R16 | E-3/E-4 test-only handoffs | CompositionTests, Vectors | Qualified |
| E5-R17 | E-2 supplied-chain controls | CompositionTests, Vectors | Qualified |
| E5-R18 | Six-file containment and compiled isolation | All six paths and qualification evidence | Qualified |
| E5-R19 | Managed regressions and native skip identities | Managed verification evidence | Qualified |
| E5-R20 | Build, preparation and separation policy | Existing build and preparation gates | Qualified |
| E5-R21 | Exact-revision P4 audit | Exact-revision P4 evidence | Qualified |
| E5-R22 | Later native custody and transfer | Initial scope evidence, future native E-5/E-6 and Slice N | Qualified, downstream only |
| E5-R23 | E-6/E-7 atomic integration and dispatch | Initial scope evidence, E-6/E-7 and Slice N downstream | Qualified, downstream only |
| E5-R24 | Exceptional failure and cleanup | Model, LifecycleTests and producer/reviewer source review | Qualified |

The model uses the frozen internal type, private constructor and callable surface, pre-only scope, immutable independent inputs and supplied observation time/stop. `TryCreate` follows Context, Stopped, Expired, Comparison and Evidence order. `Advance` checks Transition first, then the same sequence. Refusal returns null. A failed `Advance` closes and clears retained state. `Dispose` is idempotent and successful snapshots remain passive structural diagnostics. Apply the exact time, lifecycle facts, concurrency, ownership and exception semantics in the design. [Contract](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/contract.md) · [Lifecycle](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/lifecycle.md)

E5-R22 reserves real protected custody and transfer evidence for later native E-5/E-6 and Slice N. E5-R23 preserves E-6 atomic enablement, E-7 driver dispatch and closure, and Slice N qualification before exposure. The bounded producer records their continued allocation and demonstrates that the managed scope grants no live authority. [Frozen handoffs](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/responsibilities.md#frozen-cross-slice-handoffs)
