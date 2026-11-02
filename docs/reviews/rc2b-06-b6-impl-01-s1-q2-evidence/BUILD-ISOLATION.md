# BUILD-ISOLATION.md — Variant propagation, output isolation, build-order results

All probes executed by the reviewer on reviewer-materialized source copies inside the isolated review directory. Producer's 51-probe matrix was not copied; the mandatory failure classes were covered with fresh, reviewer-authored cases (plus independent mutations).

## 1. Build matrix results

| Probe | Command shape | Exit | Diagnostics / gates | Evidence |
|---|---|---|---|---|
| Clean Release (solution) | `dotnet build Gagamba.sln -c Release` (fresh in-place tree) | 0 | 3× production gate PASS (0/4+0/3), 2× instrumented gate PASS (4/4+3/3) | b1-solution-release-after.log |
| Clean Debug (solution) | `dotnet build Gagamba.sln -c Debug` | 0 | instrumented M1 gate PASS | e1-solution-debug.log |
| Release after Debug (solution) | Release immediately after Debug | 0 | production + instrumented gates PASS | e2-release-after-debug.log |
| Incremental Release (M1) | repeat `-c Release` in-place | 0 | gate PASS; hash stable `0FAFB4F7…` | e4 |
| Incremental Debug/Test (M1) | `-c Test` in isolated root | 0 | instrumented gate PASS | e9 |
| Full solution Release (parallel, fresh root) | `-c Release -m --artifacts-path …` | 0 | gates PASS | e5 |
| Direct project Release (M1/Owner/Recovery) | fresh artifacts roots | 0 | closure gates PASS | e3, b3, b4 |
| Direct Debug owner with stale production M1 in output | pre-copy Release M1.dll into Owner bin\Debug, then `-c Debug` | 0 | instrumented gate PASS; **stale production binary repaired** to Debug hash `8DA0A01C…` | e8 |
| D6.4 fixture configuration | `dotnet test D6_4.Tests -c Debug` / `-c Release` | 0/0 | 3/3 both; fixture identity + isolated d6-4-variant roots verified | f-d64-debug, f-d64-release-runner |
| Controlled contradictory-property build | 14+ cases (FINDING-002-REVIEW §3) | 1 | intended GAGAMBA001–008 refusals | c02–c20, c-g5/6/7 |
| Controlled stale-output reuse | corrupt/restore cycles; no-build; publish input | 1/0 | refused or repaired; publish refuses unsafe input | c12–c17b |
| Parallel isolated builds (Release + Test concurrently) | two `dotnet build` processes, separate artifacts roots | 0/0 | Release gate PASS; Test-config binary inspected instrumented PASS | e9-* |
| Same-output-root overlapping builds | — | — | **not claimed safe by the implementation**; not probed as a supported path (declared boundary) | — |

## 2. Variant identity and propagation

| Artifact | Config | SHA-256 (first 16) |
|---|---|---|
| M1 (in-place Release) | Release | `0FAFB4F7AA4CDCDC` |
| M1 (Debug) | Debug | `8DA0A01CE0620EA0` |
| M1 (Test, fresh root) | Test | `A651F219966FD3E7` |
| Owner (Release / Debug) | | `B1D48349B931E480` / `7D3CDE0995AA8717` |
| Recovery (Release) | Release | `C0794CEE6A04917C` |
| D6.4 TestFixture | Debug | `ECBD3385584AD53C` |
| M1.Tests\bin\Release closure copy | (Debug M1) | `8DA0A01CE0620EA0` — identical to M1 Debug |

- Distinct intermediate and output paths per configuration (standard `bin/<config>`, `obj/<config>`; D6.4 `d6-4-variant` roots enforced by GAGAMBA008).
- No test binary substituted for production anywhere; no production binary substituted into instrumented closures (the one induced case, e8, was repaired and gate-verified).
- No unexpected promotion of rejected artifacts: refusal cases leave prior bytes untouched; repair cases pass the gate.
- Unsupported combinations refuse: `Unclassified` configuration (GAGAMBA001), artifacts-path remapping for D6.4 (GAGAMBA008, declared), fixture carrier contradictions (GAGAMBA004/005/006/007).

## 3. Cross-root hash variance (documented environmental difference)

| M1 Release build root | SHA-256 |
|---|---|
| In-place (canonical-style paths) | `0FAFB4F7AA4CDCDCF1B7D040CF45ADB99F583BF7A1D1D54FBBDD9ABF993E2CF4` |
| Fresh artifacts root e3 | `22804E14AF6BDEE25E205EEB960365EBFA2193B19CA4C0236343D4699B270322` |
| Parallel fresh root e5 | `E6335C11DB6BB6888ECE185CC2260696C79F296E6DFBE99044976CED1BAA0BE5` |
| Parallel fresh root e9 | `7AFA96DB8266975D01D83F2E41594EF607082A9B402B1D03D62D3F2BCA3F06CB` |

Within an unchanged build root, repeated and incremental builds are hash-stable; across isolated output roots hashes differ (path-derived inputs such as SourceLink/PDB references flow into the PE). The reviewed implementation claims stability only within unchanged roots and explicitly disclaims cross-root byte-reproducibility; this review confirms exactly that behavior and records the variance as expected.

## 4. Gate E outcome

**Supported.** The producer's 51 build/binary probe claims are corroborated in their material claims for every mandatory failure class exercised here (clean/incremental/order/parallel/contradictory/stale/publish/fixture). Producer claims not independently repeated remain qualified: same-root concurrent overlapping builds (declared unsupported), CLI solution-override variants (equivalent cases covered via c10/e5), and the full producer matrix text (its `results.json` was not re-executed line-by-line; classes were re-derived independently instead).
