# Frozen E-4 producer and independent qualification matrices

These requirements are fixed before implementation. Producer must stage builds and transient tests outside repository paths, use the existing managed build and test settings, and record tool/SDK/runtime, exact commands and exits, fixture identities, actual counts, logs and binary hashes. Compare candidate scope and state to the authorization commit. Before submitting for qualification, identify the exact final implementation commit/tree and reconcile all retained user files, source blobs and evidence hashes with the tested inputs. Required managed cases have zero failures, runner errors, undiscovered cases and skips.

## Producer verification

| ID and normative source | Execution and expected result | Required evidence |
|---|---|---|
| V01, G01–G02 | Debug and Release. Verify exact magic/version/size, every field offset, independent LE and GUID byte packing, declared/actual lengths, truncation at each field and payload boundary, empty/short and surplus input, payload bound and above bound, maximum declared u32 and wrong endian. Both exact allowed lengths pass in the correct context. | Fixture identity and actual versus expected per case. |
| V02, G03–G05 | Debug and Release. Exercise all six routes for types 4–7. Reject unsupported types including 1–3 and 8, wrong sender/recipient/type, unknown roles, first sequence other than 1, spent or unavailable pending state, substituted or empty IDs, missing/wrong-width/mismatching hashes, swapped payload hashes and ACK with surplus catalog bytes. All invalid inputs yield refusal without output. | Wire, context and outcome ledger with rule and case IDs. |
| V03, G06–G07 and G09 | Debug and Release. Test missing, invalid or contradictory supplied state, including synthetic wrong original association and invalid ancillary classifications. Check successful data ownership, default contexts and malformed inputs without unexpected exceptions or usable output. Mutate caller buffers and returned views after a check and verify retained values. Live peer/descriptor acquisition and channel lifetime stay later obligations. | Paired positive and negative tests, ownership and error results. |
| V04, G08–G10 and frozen E-3 §§3–5 | Debug and Release. Use E-3 R2 results for complete B2 and integrated fixtures with independently established inputs. Confirm session, execution, protocol lease and original capability digest match across formats. Test each comparison independently, including a wrong catalog, lease versus installation generation, and wrong/missing phase, input or expectation. Failed decode/comparison never yields a handoff. Bind the existing 32-byte release witness and confirm distinct unchanged meanings and no new release field, output authority or integration. | E-3 exact fixture and original identity bindings, E-4 bytes and outcomes, witness identity and static release-boundary evidence. |
| V05, E-3 entry §5 and Q4 | Debug and Release. Run new E-4 focused conformance and all 185 existing E-3 focused cases using like-configuration codec artifacts. Preserve the 146 original/24 R1/15 R2 case membership, original 17 Q1 and 13 Q2 outcomes incl. no output/exception for Q2, and legacy refusal/positive controls. Use the preserved reproducers when existing suite coverage alone leaves an outcome unverified. | Per-config filtered XML, class/case inventory, convergence with Q4 binding, any exact-probe exits and ledgers. |
| V06, Q4 broad baseline | Debug and Release. Run broader M1 with the documented test layout. Account for 1,678 inherited managed passes and the added tests; all 331 native skips per configuration retain their unqualified disposition. Any discrepancy must be explained against exact discovery and logs. | Full XML, skip identities/reasons and binary/layout SHA-256. |
| V07, build and preparation controls | Run all 51 current build-policy controls as defined, including the exercised Debug and Release cases. Preparation validation and its nine regression tests pass. Build M1, M1 Tests, Owner, Recovery and InstallTool in both configurations. | Ten exits, per-control expected/actual results, log and final preparation result. |
| V08, Release/Debug policy and metadata | Existing verifier in production mode returns Release PASS exit 0 and Debug expected policy refusal exit 1 with expected configuration/hooks diagnostics. Inspect both configurations across M1, Owner, Recovery, InstallTool and actual copied dependencies. Detect known E-3/E-4 test callers as positives. Find zero production inbound references to E-4 or new production references to E-3 and no prohibited outbound authority/transport calls. | Actual core fingerprints, metadata reachability commands and hits, per-file hashes, test positives and exact limitations. |
| V09, P4 owner §§3–7 | Exact candidate diff and reverse-call inventory. Prove no new production caller, fallback, authority/status promotion, or changed path to a protected effect. Preserve S10/S11 upstream domination, S28 test-only, O-R3 and the observation-to-effect residual and verify that code fits the exact allowlist. | Source/path and branch review, before/after caller and first-effect evidence, artifact scope, P4 determination. |

[GAVA1 requirements G01–G10](../rc2b-06-b6-alt-b-slice-e-04-entry-owner-01-evidence/gava1-contract.md) · [Q4 results and artifact procedure](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/reproduction.md) · [P4 refusal-only owner decision](../rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md)

### Command entry points and artifact selection

Run from canonical Gagamba. For example, with a fresh external `$run` directory selected for this commission, use the existing build procedure and control entry points. Save exact outputs and exit statuses.

```powershell
$repo = (Get-Location).Path
$q3 = Join-Path $repo 'docs/reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q3-evidence'
& "$q3/Build-Candidate.ps1" -Source $repo -Artifacts "$run/candidate" -Logs "$run/logs"
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = "$run/control-verifier"
$env:PYTHONDONTWRITEBYTECODE = '1'
pwsh -NoProfile -File eng/test-production-build.ps1 -EvidenceDirectory "$run/production-controls"
python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v
pwsh -NoProfile -File eng/verify-preparation.ps1
```

First verify paths and hash-bind actual Debug and Release test DLLs and cores per the Q4 layout. In the following commands, set `$focusedTests` to the selected configuration's matching test/codec layout, `$broadTests` to the broad layout preserving its existing Debug dependency and actual Release discovery path, `$config` to the current configuration name, `$verifierDll` to the unchanged compiled verifier and `$coreDll` to the selected actual core. Use the named in-process entry point if VSTest times out before running cases and preserve both records. Any `NuGetAudit=false` cached restore retains the earlier advisory limitation. Run the verifier on both actual configurations, including expected refusal on Debug.

```powershell
dotnet $focusedTests -noColor -class '*Gava1ContextCodecTests' -xml "$run/e4-focused-$config.xml"
dotnet $focusedTests -noColor -class '*ValidationPreCodecBootstrapTests' -class '*ValidationPreCodecBinaryTests' -class '*InstallationWireTests' -class '*ValidationPreCodecRemediationTests' -class '*ValidationPreCodecR2Tests' -xml "$run/e3-focused-$config.xml"
dotnet $broadTests -noColor -xml "$run/broad-$config.xml"
dotnet $verifierDll --production $coreDll
```

Adapt the existing metadata scanner as an external audit tool for both E-3 and E-4 type/call sets. Preserve the scanner inputs, commands and reproducible source or identity with the evidence. Keep transient DLLs and scratch artifacts out of the commit. Native and protected qualification require their own authorization. [Q4 procedure](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/reproduction.md) · [Known focused case inventory](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/raw/counts.json)

## Separate independent qualification and owner acceptance

| Gate | Required independent verification and evidence |
|---|---|
| I00, pre-execution independence | A fresh Codex task with no participation in E-4 implementation records actual task/session identity and provenance of the separation. Stop before executing qualification if it cannot be established. Preserve the claim's factual basis. |
| I01, exact candidate and authority | Verify implementation commit/tree, source and test scope, frozen source identities, evidence manifest, 44 preserved files and no non-authorized changes. Confirm the test oracle was independently packed from normative rules and comparison data. Build fresh and bind exact artifacts. |
| I02, conformance and ownership | Independently execute V01–V04 in Debug and Release. Construct independent positive and negative vectors, verify independently reported expected versus actual identities and include a disposable sensitivity control that detects missing required comparisons without modifying the canonical checkout. Document all case outcomes and raw bindings. |
| I03, regressions and build verification | Independently execute V05–V08, preserving actual counts, negative controls, runner and restore limits and unqualified native skips. Demonstrate matching Debug/Release artifact selection, scanner test positives and production isolation. |
| I04, exact-revision P4 | Independently execute V09 on the actual implementation revision, reconcile compiled reachability and all authority/first-effect boundaries, and preserve the E-6 and future-caller triggers. |
| I05, formal handoff | Supply a pinned independent disposition, raw checksum-verifiable evidence and retained qualifications. A separate owner verifies every required gate and adjudicates findings before E-4 acceptance. Producer results cannot supply reviewer independence. |
