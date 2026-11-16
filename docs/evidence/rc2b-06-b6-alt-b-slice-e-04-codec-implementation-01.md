# Alternative B Slice E-4 codec implementation

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-04-CODEC-IMPLEMENTATION-01`, 16 November 2026 (Asia/Manila).

**IMPLEMENTED, READY FOR INDEPENDENT QUALIFICATION**

The internal managed decoder accepts only INIT, CONTEXT-ACK, DIRECT-CONTEXT and DIRECT-ACK (types 4–7) in their exact record and supplied context. It compares original IDs and digests, snapshots comparison values and publishes structural data only after success. The structural handoff compares an E-3 pre-bootstrap projection, a checked E-4 context and independently supplied originals. E-3 R2 remains unchanged. Only the test project composes both codecs. [Authorization](../decisions/rc2b-06-b6-alt-b-slice-e-04-entry-auth-01.md) · [Source/test inventory and traceability](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/traceability.md)

The canonical implementation base was commit `db44b7e50c6ce449fdb6b596ad52744cf10443c0`, tree `f6e37e07f99b9b29ab7ba23d3e5004ce4ba20b8b`. Pinned E-3 R2 is `8be61181e804b2dfafb0797eb54c92d2f234ff86`. The GAVA1 excerpt is blob `23d6ed7d95e4dd532de7e7f3a0f3a6de665b50a1`, SHA-256 `e6e0730c735c5984c97a46ed1c205131c5ffff6427de87bcb731f00110d568c4`. The adopted E-3 record is blob `f6a71562845d2cfe0aee5495864a509c5caabe7a`. The commit introducing this report binds the four tested source SHA-256 values in the annex. [Identity and counts](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/identity-and-results.json)

| Verification | Debug | Release |
|---|---:|---:|
| E-4 focused, xUnit v3 and VSTest | 11 passed on each runner | 11 passed on each runner |
| E-3 focused | 185 passed | 185 passed |
| E-3 conformance replay | 2,383 matched | 2,383 matched |
| Exact Q2 projection regression | 13 bounded `Expectation` refusals | 13 bounded `Expectation` refusals |
| Q2 and Q3 supplements | 876 and 56 passed | 876 and 56 passed |
| Broader M1 | 1,689 passed, 331 native skips | 1,689 passed, 331 native skips |
| Existing production verifier | Expected policy refusal, exit 1 | PASS, exit 0 |

All 17 historical Q1 cases reproduced on the preserved predecessor and matched the accepted outcome on the candidate. All 13 exact Q2 cases returned `Expectation` without decoded data or unexpected exceptions. The broad suite gained the 11 new managed test cases per configuration and retained the identical native skip set. All ten project builds, all 51 build-policy controls, preparation validation and all nine preparation regressions passed. [Managed evidence](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/e4-focused-debug.xml) · [Broader Release XML](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/broad-release.xml) · [Build policy](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/production-controls.json) · [Preparation](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/preparation.log)

The primary recorded matrix used the same xUnit in-process layout as Q4. VSTest 18.7.0 also passed the final E-4 focused set in both configurations. The inherited testhost timeout did not recur for those runs. Broad Release preserves the existing Debug fixture dependency and inspects the actual Release core for reachability. Focused suites bind the corresponding Debug or Release core. Cached restore used `NuGetAudit=false`. [Runtime and SDK](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/environment.txt) · [DLL identities](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/all-bindings.json)

The compiled audit inspected 36 paths for each codec family, detected known calls in both test DLLs, and found zero production references and zero outbound repository calls outside the codec family. Owner, Recovery and InstallTool loaded identical corresponding core copies. P4 is neutral for this revision. The existing installed S10/S11 graph and S28 test-only status are preserved. The two new internal files create no authority or effect path. Scope of the metadata claim is the inspected assemblies and statically resolvable references. [E-4 metadata scan](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/compiled-scan-e4.jsonl) · [E-3 metadata scan](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/compiled-scan.jsonl) · [P4 caller inventory](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/raw/p4-inventory.txt)

BB-02, composite Q-E1, P0 external evidence durability and the first production E-1 consumer remain open. P4 remains refusal-only. `NATIVE-PENDING` and all 331 native skips per configuration remain unqualified. Keep the 39 E-2 qualifications and 23 downstream obligations, F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual and separate production integration, release enforcement and E-6 gates. No native, live transport or protected validation was exercised as E-4 qualification. [Carried-forward ledger](../decisions/rc2b-06-b6-alt-b-slice-e-04-entry-auth-01-evidence/prerequisites-and-obligations.md)

Commission independent qualification in a fresh Codex task uninvolved in implementation. Establish reviewer independence before execution, bind this exact revision and re-audit P4. Separate owner acceptance follows. From the evidence directory check the committed files with `sha256sum --check SHA256SUMS.txt`. Checkout conversions can be verified against Git blob bytes. [Frozen verification matrix](../decisions/rc2b-06-b6-alt-b-slice-e-04-entry-auth-01-evidence/verification-matrices.md) · [SHA-256 manifest](rc2b-06-b6-alt-b-slice-e-04-codec-implementation-01-evidence/SHA256SUMS.txt)
