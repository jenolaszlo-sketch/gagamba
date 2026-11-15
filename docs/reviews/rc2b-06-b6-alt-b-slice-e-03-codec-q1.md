# Alternative B Slice E-3 v3-pre codec independent Q1

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-CODEC-Q1`  
15 November 2026 (Asia/Manila)

**REMEDIATION REQUIRED. The exact implementation is not ready for owner acceptance.**

The principal blocker is an integrated bootstrap comparison gap. The decoder accepts structurally valid wire claims when the separately supplied policy projection omits or contradicts any of four source basenames, the total budget, or the object count. The frozen record expressly requires agreement on those fields. Both builds also accept conflicting decoded session members in the supplied projection. Separate controls expose an unavailable-projection exception and mutable stored binary digest expectations. The results reproduce in Debug and Release with synthetic data and no production caller.

## Exact revision and authority

| Binding | Verified value |
|---|---|
| Entry authorization | `1c7fb672055cc3322eb1633d37e5e84d764cb66d`, tree `cd3b29e8e5ad8790e0795418408b8d871e920f58` |
| Reviewed detached implementation | `34eef4c8e7d814e15cb1bfce27df032e75766f19`, tree `5b9bfa884b4cec86aab293b80ada1b7f73b58d11` |
| Later canonical revision supplied in the commission | `e8099e37c1243e81f173bf0248307cf7d2fe3875`, tree `a51ed41ee2cba8205a930749f63244c5491898c5` |
| Adopted record | `docs/evidence/rc2b-06-b6-alt-b-slice-e-03-v3-pre-frozen-record-candidate-01.md`, blob `f6a71562845d2cfe0aee5495864a509c5caabe7a`, 21,225 raw Git bytes |
| Adopted record SHA-256 | `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e` |
| Owner adoption and prior independent Q1 | Adoption `d2fea61b7db323319ee59033c8b1b12ded1ffe7d`. Frozen-record Q1 `5f8b92e4874378bb365fcec80d794f964aa6f821`, evidence tree `1cb5ac2c2b467cbfb1c0d4ac85682fb979ffe961`. |
| Incorporated owner decisions | BJ/IE `f3639dc619dbfee39859afb7da553e2167d9b83d`, bootstrap `93fe2c9155a21b12fd3486b9da743260897d508e`, normative schema `a7248152d09e19d8f3775db397f340949fa9ee57`. |
| Entry and P4 decisions | Entry `028a7c1858b5e7b506deff9d2d8d263afe3344ac`, surfaces and gates evidence tree `ebd4a064e4cbad696f60898940cc2c0f8f4ea6e9`, P4 coverage acceptance `31ae1b08cb4663a6a9d9de657fd972fccb731cf2`. |

All six authorized production and test files were added at the reviewed revision and match their commissioned Git blobs. The preparation correction modifies the Markdown link checker and its tests. Historical consolidation adds historical documents and confines its attribute rules to historical evidence paths. Both descendant revisions retain all six blobs and the authority inputs. There were no unauthorized source, project, configuration, dependency, transport or activation changes in the implementation. The producer evidence lives under `docs/reviews/` where entry §3 names `docs/evidence/`. This is a documentation placement variance for the owner to disposition. [Git identities and exact changed paths](rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/git-identities.json)

The oracle was reconstructed from the adopted record, the incorporated schema and owner decisions. The probe constructs its own synthetically bound claims and projections directly from separate outputs of a common fixture-data model. It parses the 99 owner schema rows to drive member-by-member tests and packs binary payloads by explicit offsets. The producer implementation and test fixtures were inspected and run separately. [Traceability matrix](rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/traceability.md) · [Probe source](rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/probe/Program.cs)

## Results

| Gate | Debug | Release |
|---|---|---|
| Build M1, Owner, Recovery, InstallTool and M1 Tests | 5/5 exit 0 | 5/5 exit 0 |
| Focused producer suite | 122 codec + 24 legacy passed, 0 failures, 0 skips | 122 codec + 24 legacy passed, 0 failures, 0 skips |
| Unfiltered M1 managed regression | 1,639 passed, 0 failures, 331 native skips | 1,639 passed, 0 failures, 331 native skips |
| Independent assertion outcomes against that configuration's core M1 | 2,366 matched, 17 counterexamples, exit 1 | 2,366 matched, 17 counterexamples, exit 1 |
| Compiled isolation across relevant production DLLs and dependency copies | Zero inbound references, scanner detected known test callers | Zero inbound references, scanner detected known test callers |
| Existing verifier in production mode on core M1 | Expected policy FAIL, exit 1 | PASS, exit 0 |

The independent checks covered all 11 closed JSON objects, all 99 unique required definitions, and all 46 B2 and 83 integrated member occurrences. Both complete forms passed. The three Q1-E01 controls all passed in each form. They accepted a correctly escaped member name, refused a duplicate decoded name and refused an escaped digest value. The digest result depends on all 32 independently supplied bytes. Structural equality conveys no authenticated provenance, authorization, operational approval or capability custody.

Binary checks covered both roles and legal echo lengths, status-zero offsets, the fixed byte-counted tail, B2-only 37, and inherited 10/12. The probe distinguished well-formed peer refusal from invalid layout, unavailable or wrong context and structural success. The channel, direction and predecessor proof remain supplied by the future caller. `ReleaseRequest` is excluded. Legacy bootstrap and binary controls passed, including existing qualified positives, qualified-to-v1 refusal and the unqualified v1 positive. The separate BB-02 observation was reproduced through the legacy decoder and serializer, with isolation of the strict codec intact. [Individual assertion accounting and environment limitations](rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/managed-accounting.md) · [Reproduction and evidence index](rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/reproduction.md)

## Findings

| Finding | Severity and gate | Observed behavior and normative basis | Required remediation or retained qualification |
|---|---|---|---|
| CODEC-Q1-F01 | High, blocking | All 12 permutations of missing or contradictory expected values for `ToolSourceName`, `InputSourceName`, `CoordinatorSourceName`, `FilterSourceName`, `TotalBudgetBytes` and `MaxObjects` produced structural success. Frozen §3 relationship 6 expressly includes those fields, and the paragraph following relationship 7 requires exact typed agreement and refusal on missing required comparison data. `ValidationPreCodec.cs:313–316` skips them. | Compare all six wire claims to the independent supplied projection as typed members while retaining the wire constants. Add missing and conflicting expectation controls. |
| CODEC-Q1-F02 | Medium, blocking | Both forms accepted a supplied projection with conflicting `"\u0053ession"` and `"Session"` members because `TryGetProperty` chooses the later value. Wire duplicate controls passed. Frozen §3 relationship 2 requires agreement with the original session and a single unambiguous typed expectation. `ValidationPreCodec.cs:306–325` permits the ambiguous input. | Refuse ambiguity in supplied required members by decoded ordinal identity at each consumed object, or use an equivalently unambiguous projection representation. |
| CODEC-Q1-F03 | Medium, blocking | Disposing the document backing an otherwise valid projection before the call causes `ObjectDisposedException` for both forms at `ValidationPreCodec.cs:27`. Entry §3 requires bounded refusal without a validated payload. Frozen §3 requires refusal when comparison data is unavailable. | Bind an owned projection with an explicit lifetime, or convert unavailable projections into bounded refusals. Exercise a lifetime control at the chosen boundary. |
| CODEC-Q1-F04 | Medium, blocking | An installed-artifact digest stored in binary expectations changed after construction when the caller reused the original array. A message 10 carrying the replacement digest then produced structural success. Frozen §4 requires the original expected artifact digest under independently supplied context. `ValidationPreCodecData.cs:48–55` retains caller-backed memory. `ValidationPreCodecBinary.cs:86` compares its current contents. | Bind immutable digest values when creating the binary expectations or enforce an equivalent ownership boundary. Add controls for post-construction mutation of supplied digest storage. |
| CODEC-Q1-E01 | Low, non-blocking documentation variance | Producer report and evidence were committed under `docs/reviews/`. Entry §3 names `docs/evidence/`. Git confirms the variance is confined to documentation. This finding is separate from the earlier frozen-record Q1-E01 parser controls. | Have the owner disposition the placement when reconciling the commission record. |

These findings concern the structural comparison and refusal interface. No production authority or protected-effect path was observed. [Exact counterexamples and retained limits](rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/findings.md)

## P4 and preservation

**P4-neutral for this exact revision.** The production reverse-call inventory reproduces three callers each for S10 and S11, with the installed route dominated by the existing recovery admission gates. S28 remains test-only. No new production route to admission, custody, launch, release or another protected effect was introduced. F-R1/O-R1 and O-R2 retain their caller-change re-audit triggers. O-R3 refusal-reason typing and the observation-to-effect residual remain open. The codec findings do not alter that narrowly scoped P4 result. Any remediation candidate needs a fresh exact-revision delta, compiled isolation and P4 trigger check. E-4 and E-6 retain their separate re-audits. [P4 and reachability assessment](rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/reachability-and-p4.md)

E-1 and E-2 boundaries remain accepted, with E-2's 39 retained qualifications and 23 downstream obligations intact. E3-ENTRY-F01 stays closed. Composite Q-E1, BB-02 and P0 external-package durability remain open. The first E-1 production consumer remains unassigned. E-4 retains GAVA1 and the cross-format handoff. E-6 retains runtime `ReleaseRequest` recomputation and atomic enforcement. Protected readers and exact runtime case policy retain their existing atomic-set and Slice N gates. `NATIVE-PENDING` and all 331 inherited skips remain unqualified. The supplied green GitHub CI result was context for the commission. Live GitHub status and external historical Git ancestry remain independently unverified.

## Handoff

Commission a bounded E-3 codec remediation for the four findings, then an independent Q2 against that exact candidate before owner acceptance. Keep remediation within the codec, its focused tests and review evidence. The isolated checkout remains detached at the reviewed revision. This report and its evidence are staged outside canonical Gagamba for handoff, pending confirmed cleanup and coordination before any import. [Evidence SHA-256 manifest](rc2b-06-b6-alt-b-slice-e-03-codec-q1-evidence/SHA256SUMS.txt)
