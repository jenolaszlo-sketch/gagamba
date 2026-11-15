# Managed and assertion accounting

| Execution | Debug | Release |
|---|---|---|
| M1, Owner, Recovery, InstallTool and M1 Tests builds | 5 exit 0 | 5 exit 0 |
| Focused codec bootstrap | 64 pass | 64 pass |
| Focused codec binary | 58 pass | 58 pass |
| Legacy InstallationWireTests | 24 pass | 24 pass |
| Focused totals | 146 pass, 0 fail, 0 skip, 0 not run | 146 pass, 0 fail, 0 skip, 0 not run |
| Broad M1 test assembly | 1,970 cases, 1,639 pass, 0 fail, 331 native skip, 0 not run | 1,970 cases, 1,639 pass, 0 fail, 331 native skip, 0 not run |
| Independent probe assertions against the actual configuration's M1 | 2,383 assertions, 2,366 matched, 17 counterexamples, exit 1 | Same observations, exit 1 |

The Release M1 test assembly intentionally references instrumented Debug M1 via AdditionalProperties in the unchanged csproj. Its copied DLL hash matches Debug. The independent Release probe has direct DLL references to the actual Release M1 build. The broad managed runs each load the actual Release build for the two existing compiled-exclusion controls. [Exact candidate and dependency copies](test-binary-bindings.json)

Assertion accounting below expands the Assert calls visible in each focused method body and its explicit test helpers, using the reported execution roster and loop bounds. This is a source count applied to actual completed cases. It does not count internal assertions inside xUnit implementations or unasserted positive setup calls.

| Focused class or control family | Cases | Assert calls per focused run |
|---|---:|---:|
| Codec bootstrap complete witnesses | 2 | 12 |
| All required/wrong-kind occurrences, including restoring a valid witness per topology | 2 | 526 |
| B2-only root presence | 2 | 16 |
| Three Q1-E01 tests | 3 | 14 |
| Qualification state, both topologies for each input | 8 | 48 |
| AuthorityHash other lexical forms, independent comparison, all-zero control | 12 | 54 |
| Whole-byte bounds and invalid UTF-8/trailing JSON | 4 | 23 |
| Scalar domain plus allowed whitespace/order/escape | 13 | 41 |
| Relationships, projection comparators, and version tokens | 15 | 45 |
| Context selection and unchanged legacy bootstrap decoder | 3 | 13 |
| **Codec bootstrap subtotal** | **64** | **792** |
| Codec binary fixed positives and malformed echoes | 19 | 37 |
| Binary field mutation and profile expectations | 30 | 60 |
| Confirmation predecessor/mismatch/refusal paths and B2 separation | 7 | 34 |
| Legacy binary separation and release exclusion | 2 | 10 |
| **Codec binary subtotal** | **58** | **141** |
| Legacy role projections, golden bytes and generic owner channels | 4 | 27 |
| Legacy echo single-byte loops and expectation mutations | 3 | 204 |
| Legacy response pairing, binding and confirmation | 8 | 64 |
| Legacy directions/context, invalid identities, bootstrap and Ready | 9 | 39 |
| **Legacy subtotal** | **24** | **334** |
| **Focused total** | **146** | **1,267** |

The two per-member producer cases executed 258 individual decoder refusal controls, plus two restored valid witnesses. The producer bootstrap class called the v3 decoder 351 times through Verify and the binary class called the v3 validator 116 times. The independent ledger counts each asserted outcome once and also checks structural result publication on every bootstrap invocation. Of the 17 counterexample observations per configuration, 12 correspond to F01, two each to F02 and F03, and one to F04. Diverse independent binary outcomes include 10 true peer-refusal controls covering the five inherited reasons under both applicable topologies.

The first NuGet attempt failed at advisory-source lookup. The completed builds used cached packages with NuGetAudit=false. A local clone remote also caused SourceLink warnings. The ordinary VSTest path could not establish its testhost connection and produced aborted TRX files. The successful fallback ran the same binaries through their xUnit v3 in-process runner and produced xUnit XML. An initial broad run from sibling artifacts skipped two repository-layout discovery tests as well as the 331 native rows. Re-running from a disposable mirror with the exact solution marker and Release library in the two tests' hard-coded layout passed both controls. There are 331 skips in each final broad result. [Focused Debug XML](focused-direct-debug.xml) · [Focused Release XML](focused-direct-release.xml) · [Broad Debug XML](managed-M1-layout-debug.xml) · [Broad Release XML](managed-M1-layout-release.xml) · [Build and VSTest exit ledger](build-and-test-exit.txt) · [Direct runner exits](direct-exit.txt)
