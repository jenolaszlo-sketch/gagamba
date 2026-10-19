# Gagamba Linux M1 — WP05 Owner Integration Audit

Date: 2026-10-19
Type: read-only plan-binding audit; no production implementation
Outcome: **`WP05 OWNER INTEGRATION DEFERRED BY AUTHORITATIVE PLAN`**

## Binding

- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- Authoritative plan: `docs/plans/gagamba-linux-m1-implementation-plan.md`
- Schema amendment: `docs/adr/0004-wp05-durable-release-schema-amendment.md`
- Production owner entry point: `src/Gagamba.Execution.Linux.M1.Owner/Program.cs`
- Independent qualification review (preserved separately): `review/wp05-seccomp-kernel-requalification` at `5b6ab064ec0e329735aae969a3478577203d6616`

## Authoritative plan findings

- Plan §16, WP05 implementation boundary (line 435): **“Live integrated path still cannot release.”** WP05 is explicitly a no-target, component/protocol tranche tested with a qualification-only coordinator; real post-release target exec is deferred to WP10.
- Plan §8.1/§8.2 define the release coordinator wire contract and the `ReleaseArbiter` release/stop linearization. They do not assign production-owner release invocation to WP05.
- Plan WP08 (line 473): **“Owner orchestration, setup-inclusive deadlines and caller loss”** wires preparation/held setup/capture/stop into one execution-owned lifetime, **“with release still inhibited pending G3.”** Dependencies: WP01–WP07.
- Plan WP10 (line 502): **“One complete internal static-native transaction.”** It contains the **“minimal owner integration wiring in `RestrictedExecutionOwner.cs`”** and enables the fixed fixture only after WP09/G3.
- ADR 0004 §21 says the first schema tranche must not automatically include owner release wiring; §22 explicitly defers production implementation of `ReleaseArbiter`, release transport, coordinator post-release branch, and owner wiring.

## Current implementation evidence

- `Owner/Program.cs` validates its private bootstrap channel and runs `OwnerPreparationTransaction.Run(...)`; it contains no release/arbiter invocation.
- `OwnerPreparationTransaction` implements preparation, HELD setup and abort; it does not call `ReleaseArbiter` or write a release.
- The WP05 release components (`ReleaseArbiter`, `ReleaseDecision`, `CoordinatorProtocol`, `CoordinatorStatusReader`) and the native RELEASE branch exist and are tested, including the preserved Q1 probe that composes them through internal APIs. That composition is intentional WP05 qualification, not the production owner path.
- The production owner has no `Release`/`release` reference (source inspection).

## Ownership matrix

| Responsibility | Required by WP05? | Current implementation | Evidence | Owning WP |
|---|---|---|---|---|
| `ReleaseArbiter` | Yes (WP05 §8.2) | `src/Gagamba.Execution.Linux.M1/Execution/ReleaseArbiter.cs`; tested | `ReleaseArbiterTests`, Q1 Facts | **WP05** |
| Durable `ReleaseIntent` | Yes (WP05 §8.2; ADR 0004) | Arbiter intent write; v3 journal snapshot | `ReleaseArbiterTests`, `OwnershipV3*` tests | **WP05** |
| Release capability consumption | Yes | Single-use capability consumed inside the gate | `ReleaseArbiterTests`, Q1 Fact 1 | **WP05** |
| Production owner invokes release | **No, not WP05** | `Owner/Program.cs` and `OwnerPreparationTransaction` contain no release invocation | Source inspection; plan §16 WP05 boundary | **WP08** (owner lifetime; release still inhibited pending G3) and **WP10** (minimal owner integration wiring after WP09/G3) |
| Coordinator accepts RELEASE | Yes | `native/linux-m1/exec-coordinator.c`, qualification coordinator, `ReleaseProtocolTests` | Native release receipt tests | **WP05** |
| Trusted status persistence | Yes for WP05 trusted status/observations; terminal receipt is later | `CoordinatorStatusReader`, v3 observations | `CoordinatorStatusReaderTests`, v3 tests | **WP05** (facts); **WP09** (terminal receipt/retirement) |
| Resource/capture prerequisites | No; faked in WP05 | `ReleasePrerequisites` fake booleans | Arbiter/Q1 tests | **WP06** (resources), **WP07** (capture) |
| Target fork/exec | Later, WP10 | Not implemented; Q1 stops at pre-exec | Q1 Fact 1, plan §16/§8.1 | **WP10** |

## Conclusion

The absence of direct production-owner release wiring is **not a WP05 gap**. The authoritative plan deliberately keeps WP05's integrated path unable to release and assigns owner orchestration to WP08 (release still inhibited) and the minimal owner release integration to WP10 after WP09/G3. No production change is required or made in this tranche.

Outcome A: **`WP05 OWNER INTEGRATION DEFERRED BY AUTHORITATIVE PLAN`**

Future work package and entry criteria:

- **WP08** — owner orchestration; entry criteria WP01–WP07; release remains inhibited pending G3.
- **WP10** — minimal owner integration wiring in `RestrictedExecutionOwner.cs` and the one complete static-native transaction; entry criteria WP09/G3, exact reviewed native artifacts and a supported delegated host.

## Non-scope

No production owner API, release endpoint, managed orchestration, capture/resource integration, authority protocol, schema v4, second journal, or target execution was added or reviewed here.
