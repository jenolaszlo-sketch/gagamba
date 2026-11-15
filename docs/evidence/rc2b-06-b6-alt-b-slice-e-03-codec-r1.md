# Alternative B Slice E-3 codec R1 bounded remediation

Commission `RC2B-06-B6-ALT-B-SLICE-E-03-CODEC-R1`, 15 November 2026 (Asia/Manila), canonical Gagamba `main`.

**E-3 CODEC R1 IMPLEMENTED — READY FOR INDEPENDENT Q2**

Producer verification corrected all 17 original Q1 counterexamples in Debug and Release. The original Q1 report retains its **REMEDIATION REQUIRED** disposition for the reviewed predecessor. [Original Q1 report](../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-q1.md) · [Original Q1 checksums](../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/SHA256SUMS.txt)

## Source identities and coordination

| Identity | Commit | Tree |
|---|---|---|
| Independent Q1 reviewed source | `34eef4c8e7d814e15cb1bfce27df032e75766f19` | `5b9bfa884b4cec86aab293b80ada1b7f73b58d11` |
| Canonical start of R1 commission | `e8099e37c1243e81f173bf0248307cf7d2fe3875` | `a51ed41ee2cba8205a930749f63244c5491898c5` |
| Separate byte-exact Q1 import, R1 implementation parent | `7713ec58cc7c292862c8c80d6b57dcacd3420b44` | `e7f0854d5365aef6533d6ba731eebadc8638bac0` |
| Tested implementation and tests staged on that parent | Three exact blobs in the source ledger | `7d535411183bf5e1ad73bb2e04ad1cea79b1d84a` |
| Final R1 implementation and evidence revision | The commit introducing this report | That commit's tree, retrievable with the commands in the reproduction notes |

Canonical preflight found one worktree, zero stashes, a clean tracked state, 44 retained historical untracked files and 0 ahead/0 behind the locally recorded `origin/main`. Git ancestry leads to the exact Q1 revision. Commit `18c95f6cdf082f87bd6113141c7f967abfc82042` updates preparation link validation to ignore fenced code and tests the validator. Commit `e8099e37c1243e81f173bf0248307cf7d2fe3875` consolidates historical documents and scopes raw-byte attributes to their imported paths. Both retained the original E-3 codec and test blobs.

The owner confirmed branch cleanup was complete and verified, the original Q1 review had concluded, and byte-exact import was authorized. The import staged and hashed all 156 items listed in the Q1 manifest. The two narrowly scoped Git attributes preserve the evidence and report through Windows checkout conversion. The import and implementation have separate commits. The external reviewer workspace remains available. [Preflight](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/canonical-preflight.json) · [Q1 import receipt](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/q1-import.json) · [Tested source identities](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/raw/source-identities.json)

## Root causes and bounded corrections

| Q1 finding | Root cause and R1 correction | Verified outcome |
|---|---|---|
| CODEC-Q1-F01 | The comparator treated wire constants as sufficient and skipped four source basenames, `TotalBudgetBytes` and `MaxObjects`. The projection now must separately provide each field. The decoder compares the values semantically and still validates the original wire constants through the schema. | Each of the six omissions gives `Expectation`. All six contradictory values of the declared JSON kind give `Comparison`. A wrong kind gives `Expectation`. Matching projections pass. |
| CODEC-Q1-F02 | `TryGetProperty` silently selected the last matching decoded name in the projection. Each consumed object now registers required names with ordinal decoded equality and refuses ambiguity. | Both conflicting session reproducers and a nested profile duplicate give `Expectation`. A single valid escaped session member continues to pass. |
| CODEC-Q1-F03 | An early `JsonElement.ValueKind` read dereferenced a disposed backing document outside the projection comparison. Projection inspection now contains a narrow `ObjectDisposedException` boundary. The property-name decoder handles its own invalid name error. | Both disposed originals give `Expectation` with no decoded value. Wire shape and comparison failures retain distinct diagnostics. |
| CODEC-Q1-F04 | `ReadOnlyMemory<byte>` kept caller-owned backing storage. Each digest snapshots the 32 input bytes and the existing memory-returning properties give callers a copy. Wrong-width inputs map to an absent expectation and retain the existing binary refusal. | The replacement digest gives `Comparison`. The original still passes after mutation. Controls cover all six input arrays and all six returned memory views. |
| CODEC-Q1-E01 | The producer's earlier documentation placement differed from entry §3. The original evidence and finding are preserved. This remediation evidence is under `docs/evidence/`. | Non-blocking documentation observation retained for owner disposition. |

The 17-row matrix identifies the exact original ledger case, exact before and after outcomes, and the corresponding xUnit evidence in both configurations. Regression tests exercise supplied projections separately from wire bytes. The unchanged Q1 probe also replays the original synthetic inputs and expectations with the exact original counterexample identifiers and wire hashes. [Closure matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/closure-matrix.md)

## Managed and build verification

| Check | Debug | Release |
|---|---|---|
| Projects built | M1, Owner, Recovery, InstallTool and M1 Tests, 5/5 | Same 5/5 |
| Original focused cases | 146 passed, 0 failed, 0 skipped | 146 passed, 0 failed, 0 skipped |
| R1 regressions against original DLL | 24 total, 3 passed, 21 expected failures, 0 skipped | Same |
| R1 regressions against corrected DLL | 24 passed, 0 failed, 0 skipped | 24 passed, 0 failed, 0 skipped |
| Original Q1 probe against original DLL | 2,383 assertions, 2,366 matched, 17 reproduced counterexamples | Same |
| Original Q1 probe against corrected DLL | 2,383 assertions, 2,383 matched, 0 counterexamples | Same |
| Unfiltered M1 managed suite | 1,994 total, 1,663 passed, 0 failed, 331 native skips | Same |
| Existing production verifier | Expected policy refusal, exit 1 | PASS, exit 0 |

Focused and R1 test tiers used their configuration's actual core M1 DLL. The broader Release test project retains its required Debug core instrumentation, and both unfiltered configurations also successfully load the actual Release assembly for two existing reachability controls. The broad total increased by the 24 R1 cases, with zero additional skips. The producer replay verifies the earlier independent observations. It does not constitute independent Q2. [Machine-readable counts](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/verification-summary.json) · [Actual test input DLL hashes](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/raw/test-bindings.txt) · [Reproduction](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/reproduction.md)

Verification ran on Windows NT 10.0.26200.0 x64 with SDK 10.0.401 and runtime 10.0.12. Builds used cached packages with NuGet advisory lookup disabled after the original qualification's advisory-source failure. VSTest timed out before running tests. The evidence counts come from the same test DLLs run via the xUnit v3 in-process runner. The 331 native cases remain unqualified.

## Production isolation, P4 and preserved boundaries

The unchanged Q1 metadata scanner inspected 36 DLL paths, including actual production dependency copies, and found zero production callers, codec type references, or codec outbound repository calls. It found known bootstrap and binary callers in both managed test DLLs. The standalone corrected M1 SHA-256 values are `b799c3f0e9bf3339f26bae6072af1334993c45044c338fbdb18b14014933bc0e` (Debug) and `7044539fca015f4c8cfda4fdee84b9856a98a9619dbe61a4876929a410d22d39` (Release). The Release verifier reported 14/14 required types. Debug produced the expected configuration and qualification-hook diagnostics. [Compiled scan](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/raw/compiled-scan.jsonl) · [Release verifier](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/raw/verifier-release.log) · [Debug verifier](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/raw/verifier-debug.log)

P4 stays neutral for this change. The before/after reverse-call graph is identical, including the three S10 and three S11 callers and their existing first-effect admission dominance. S28 has its single test caller. F-R1/O-R1 and O-R2 retain their future re-audit triggers. O-R3 and the observation-to-effect residual remain open. Metadata inspection covers the built shipping graph and does not establish a claim about arbitrary external reflection. [Source call inventory](rc2b-06-b6-alt-b-slice-e-03-codec-r1-evidence/raw/p4-source.txt)

The only implementation delta consists of two codec files and one focused test file. Legacy code, protocol assets, native backends, authority and transport paths, project/build configuration and adopted evidence are identical to the import baseline. The preparation verifier and its 9 tests passed. The production-build validator passed all 51 controls. Its local publish controls generated only disposable build output. BB-02 remains isolated and open. E-1/E-2, E3-ENTRY-F01, composite Q-E1, P0 external durability, the native skips and all existing E-2 qualifications and downstream obligations retain their existing disposition. E-4, E-6, production activation, protected execution, independent Q2, and owner acceptance remain outside this verification.
