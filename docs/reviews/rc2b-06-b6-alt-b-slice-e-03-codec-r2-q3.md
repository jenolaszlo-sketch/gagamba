# Alternative B Slice E-3 codec R2 independent Q3

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-CODEC-R2-Q3`, completed 16 November 2026 (Asia/Manila).

**SUPPORTED WITH QUALIFICATIONS**

The exact R2 revision is suitable for a separate formal owner acceptance decision within the internal managed E-3 scope. Both Q2 blockers are closed on R2, all 17 Q1 cases remain resolved, and the additional controls found no new blocking defect. This qualification confers no operational approval or production activation.

## Revision and governing records

| Record | Commit or blob | Tree or SHA-256 |
|---|---|---|
| Original implementation | `34eef4c8e7d814e15cb1bfce27df032e75766f19` | `5b9bfa884b4cec86aab293b80ada1b7f73b58d11` |
| Q1 evidence import | `7713ec58cc7c292862c8c80d6b57dcacd3420b44` | `e7f0854d5365aef6533d6ba731eebadc8638bac0` |
| R1 implementation | `4a97b79e0060b001a7c1d30ae93bc66cedc52418` | `dfd9519b7799155a0fd6f99a8edad27618c38a6a` |
| R1 Q2 qualification | `4cb46a6cdbcc52b7129dbd88c4eba2e62ae73469` | `0975cc5fbd13c6342c219333029e82e3d02c94aa` |
| Reviewed R2 and starting canonical main | `8be61181e804b2dfafb0797eb54c92d2f234ff86` | `1c9307845858fb25ca0a9473d53d1fd1496018fc` |
| Adopted frozen v3-pre record | `f6a71562845d2cfe0aee5495864a509c5caabe7a` | `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e` |

The owner adoption, adopted annex, normative BJ/IE and bootstrap decisions and entry authorization match the object and hash ledger. The baseline had one canonical worktree, zero stashes, a clean index and tracked checkout, and all 44 historical untracked files matched their prior SHA-256 values. Verified 156 Q1, 100 Q2 and 64 R2 evidence hashes against canonical Git bytes and checkout files. Q1 and Q2 retain their original **REMEDIATION REQUIRED** dispositions for their reviewed predecessors. [Git and normative identities](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/raw/identities.json)

The entire non-documentation R2 delta consists of the bounded scalar comparison change in `ValidationPreCodec.cs` and one deterministic test file. Frozen schema, binary layout, legacy behavior, dispatch, transport, projects, dependencies and build policy remain outside the delta. The qualified source remains disconnected from production callers. [Exact source delta](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/raw/scope-diff.json)

## Closure and conformance

The exact 13 Q2 fixtures were executed on both Debug and Release R1 and R2 assemblies. R1 replay produced five malformed-string exceptions and eight exponent exceptions in each configuration. R2 returned `Expectation` with no decoded object for every fixture. Q3 verified the matching R1 and R2 input hashes against the original Q2 evidence. [13-case Q2 matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/q2-matrix.md)

The original Q1 replay independently reproduced all 17 original failures and all 17 corrected outcomes per configuration. Missing and contradictory policy projections, ambiguous decoded sessions and disposed projections refused. Replacement binary expectations could not change the originally bound digest. [17-case Q1 matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/q1-matrix.md)

Frozen §3 requires exact typed semantic projection equality. Entry §3 requires bounded refusal without a validated payload. The R2 string guard handles the deferred JSON string decoding failure locally. The numeric guard handles the specific exponent failure locally. Both guards preserve the existing equality routine for accepted values. The 56 new Q3 controls exercised mathematically equivalent finite numbers, values that differ below binary floating-point precision, extreme exponents, preserved case-sensitive string equality, malformed surrogate spellings, and distinct wire integer grammar. No coercion, rounding, clamping, normalization or fallback was observed. These controls supplement the repeated 2,383-case Q1 and 876-case Q2 suites. [Managed conformance matrix](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/conformance.md) · [Q3 control source](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/Q3Cases.cs)

## Independently measured gates

| Tier per configuration | Debug | Release |
|---|---|---|
| Original focused cases | 146 passed | 146 passed |
| R1 cases | 24 passed | 24 passed |
| R2 cases | 15 passed | 15 passed |
| Combined focused | 185 passed | 185 passed |
| Broader M1 | 1,678 passed, 331 native skips | 1,678 passed, 331 native skips |
| Unchanged Q1 replay on R2 | 2,383 matched | 2,383 matched |
| Unchanged Q2 supplemental replay on R2 | 876 matched | 876 matched |
| New Q3 controls | 56 matched | 56 matched |
| Production-mode verifier | Expected policy refusal, exit 1 | PASS, exit 0 |

The completed managed runs have zero failures and zero not-run cases. The VSTest attempt timed out before executing tests and is excluded. The xUnit v3 in-process runs used verified DLLs. Focused tests in each configuration used the corresponding actual M1 core. The broad Release project retains its existing Debug fixture dependency, while two existing tests inspect the actual corrected Release assembly. Build and test execution used SDK 10.0.401 and runtime 10.0.12 on Windows NT 10.0.26200.0 x64. Builds used cached packages with NuGet advisory lookup disabled, consistent with the prior recorded `NU1900` failure. All 51 production-build controls, nine preparation regressions and the final preparation validator passed. [Exact counts and artifact hashes](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/summary.json) · [Reproduction](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/reproduction.md)

The unchanged scanner inspected 36 physical DLL paths covering both configurations of M1, Owner, Recovery, InstallTool and actual dependency copies. It detected the expected managed test callers and found zero production inbound codec references or outbound repository-code calls from the codec. Production dependency copies match each inspected core. Static evidence covers the inspected shipping assemblies and statically resolvable references. [Compiled scan](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/raw/compiled-scan.jsonl)

**P4-neutral.** The exact source reverse-call inventory matches the original. The S10 and S11 parent paths retain first-effect admission dominance. S28 retains its sole test caller. F-R1/O-R1 and O-R2 remain re-audit triggers. O-R3 and the observation-to-effect residual remain open. No production activation or authority path was added. [P4 caller inventory](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/raw/p4.txt)

## Retained qualifications and handoff

The 331 native skips and `NATIVE-PENDING` remain unqualified. BB-02 remains open and isolated. E-1 and E-2 preserve their accepted boundaries, including E-2's 39 qualifications and 23 downstream obligations. E3-ENTRY-F01 stays closed. Composite Q-E1 and P0 external-package durability remain open. E-4 retains GAVA1 and cross-format handoff. E-6 retains runtime release correlation and atomic enforcement. Existing owner-review qualifications and any trigger-based re-audits remain in force. The earlier Q1 producer documentation placement observation remains non-blocking for the code qualification. This commission makes no claim about protected native execution or custody.

Recommend a separate E-3 owner acceptance decision for the exact R2 revision under these qualifications. The Q3 commit adds only this report and its evidence. [SHA-256 evidence manifest](rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence/SHA256SUMS.txt)
