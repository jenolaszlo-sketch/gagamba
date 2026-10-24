# RC2A-01 implementation handoff

- Date: 2026-10-25 (Asia/Manila).
- Slice: passive restricted consumer contract implementation and focused implementation tests.
- Baseline: `9a705335701f0c1923bd9867a2612b8a93afa8cb`, tree `f6a7e141bf7f38c976655c0772b7ee817a7ce148`.
- Implementation: `e93d642e1a36af25c0e145d328bebadd1bba4838`, tree `68f922ea552fd81c60fa4b168834752d6668c56a`.
- Branch: `implementation/gagamba-rc2a-01`.
- Worktree: `C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-01`.
- Host: Windows 11 / NT 10.0.26200.0, AMD64; .NET SDK 10.0.401; Microsoft.NETCore.App 10.0.12.
- Framework: xUnit v3 3.2.0, VSTest adapter 3.1.4, Microsoft.NET.Test.Sdk 18.0.1; net10.0, Release, warnings as errors.
- Native prerequisites: none exercised. No target, native fixture, restricted owner or helper was started.
- Scope: source implementation and local Windows execution of pure value/codec tests only.
- Exclusions: RC2A-02 qualification, RC-2A acceptance, package/clean-consumer/mixed Hufu graph qualification, RC-2B, native/runtime qualification, release, pack, merge, push, tag or publication.

Disposition: **RC2A-01 IMPLEMENTED - INDEPENDENT REVIEW REQUIRED**.

## A. Git and authority binding

Verified actual objects and direct-parent relationships: RC-1 `d609c29d3048bc0fd91583068cb013fb906fdf90` is the direct parent of both the owner decision and the separate GLM review `feb9d7584c41712195d4b90754ed7a342ba74bb7`. The owner decision's exact root tree matches the commission. GLM review blob is `c2617370806f7ba0ac7bb4006fc92d3a880df2dc`. The decision did not merge the review. Both documents and RC-D01–D04/D09 tables/grammar, profile clarifications, work orders and existing evidence were read before source edits. Existing source/golden/reference tests were inspected.

The current user commission expressly authorizes RC2A-01 implementation and focused tests. The earlier plan's RC2A-01 finalization-only / RC2A-03 source sequencing is superseded for this bounded commission; it is not a conflict in field/wire semantics. The original RC-1 and owner acceptance remain unchanged. No material contract discrepancy or redesign was found.

Worktree creation (after the sandbox denied the shared Git lock; retried with approved escalation):

```powershell
git worktree add -b implementation/gagamba-rc2a-01 C:/Users/Laszlos/source/repos/Solo/gagamba-rc2a-01 9a705335701f0c1923bd9867a2612b8a93afa8cb
```

Initial owner and implementation checkouts were clean. The original owner checkout was rechecked clean at its unchanged commit. The canonical Gagamba checkout was observed on an unrelated review branch with existing untracked review/spike/TestResults artifacts; it was not edited. Initial restricted status access to that canonical checkout was denied; approved read-only access later supplied the observation. No unrelated files were staged or committed.

| Tree | Baseline | Implementation |
|---|---|---|
| src | `9dd0f055eaf48d48bda8683dee3f8dc534a06e5d` | `5781fccef79763be0be7ba8a82d1fce1143c6b74` |
| tests | `ebcb976a164b212090fbe0958fe4b0c0b243f794` | `a13d10e5bcdea33daa817d4fd4c3598d828d6106` |
| native | `85631d2f04512376621d3d09f51d67fd90f45335` | unchanged |

The implementation was clean during both final committed test runs. This evidence commit is its documentation-only immediate child; its exact final commit/tree and final clean-state verification are recorded in the external completion receipt/final report to avoid circular self-hashing. Source/test trees must remain identical to the implementation above. [Source binding](source-binding.json) includes exact Git blobs and SHA-256 of Git blob bytes for all eleven implementation files, document identities and the original commission attachment's byte hash. No fresh remote-state claim is made; pinned local commit identity/ancestry is authoritative here.

## B. Contract-to-code map

The [frozen concrete API](../../design/restricted-consumer-contract-rc2a-01-api.md) was written before focused tests/source implementation. It names failure types and null/length conventions left for RC2A-01 by RC1-A. [API declarations](api-inventory.md) and the exhaustive [72-property map](field-map.md) bind the inspection tree.

| Contract source | Implementation | Evidence |
|---|---|---|
| RC-D01/D02 | `Restricted/Contracts/RestrictedManifestView.cs`: twelve sealed get-only root/nested views, immutable collection/digest projections; two byte enums | `EveryModelPropertySurvivesProjectionAndDecode` compares the complete graph/property inventory with both direct and decoded views; public surface and mutation tests |
| RC-D03, RC1-A section 5 / R1-F01 | `RestrictedManifestCodec.cs`; existing `ManifestCodec` in `RestrictedManifest.cs` now accepts an internal optional bound and checks the one writer before growth/output | Golden bytes and independent ReferenceEncoder unchanged; exact full encoding size guards |
| RC-D03 validation | `ManifestReader.cs` and `ManifestValidation.cs`; existing model constructor invariants reused by all reader construction, additional strict string/enum checks shared with projection/encoding | Every golden truncation; malformed/order/tag/length/UTF-8/enum/forced-value and argv/env tests |
| RC-D03 digest/supports; RC1-A R1-F05 | Public codec exact ordinal `Supports`, SHA-256 of canonical encoding and `CryptographicOperations.FixedTimeEquals` | Required length/matching/nonmatching/all-zero and schema/ID boundary cases |
| RC-D04 | `RestrictedPreparationReference.cs`: four nonempty GUIDs plus copied 32 bytes; content equality/hash code | All GUID positions, digest lengths, copied arrays, equal/unequal instances; no temporal/control members |
| RC1-A Mode approval / RC-1 section 3 | Existing descriptive invocation string preserved; no argument insertion or executable mode selector | Golden `work` unchanged; synthetic internal invocation with exactly `[/tool]` remains passive data |
| R1-F04 | Shipped README and reference XML documentation | No deadline, expiry or liveness property; timing-source limitation explicit |

Only `RestrictedManifestCodec` is the public entry; its encoder delegates to the existing `ManifestCodec`. The reader performs final exact re-encoding as defense in depth after bounded parsing. It never sorts wire sets into acceptance. Internal constructors still canonically sort trusted construction enumerations. View construction and public Encode share strict validation and the same bounded writer. Legacy internal Encode without the optional cap remains compatible; it is used only as the deterministic oversized positive control in these focused tests. No second production encoder or public oversized-view factory exists.

## C. File inventory

| Changed files | Purpose |
|---|---|
| `src/Gagamba.Execution/Restricted/RestrictedManifest.cs` | Shared existing writer's optional inclusive byte bound; no grammar change |
| `src/Gagamba.Execution/Restricted/ManifestReader.cs` | Strict fixed-depth decoder, checked cursor/count/length bounds and typed refusal |
| `src/Gagamba.Execution/Restricted/ManifestValidation.cs` | Shared additional schema/strict UTF-8 validation |
| `src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestView.cs` | Complete immutable public inspection projections and enums |
| `src/Gagamba.Execution/Restricted/Contracts/RestrictedManifestCodec.cs` | Five-method public codec API plus closed result/error vocabulary |
| `src/Gagamba.Execution/Restricted/Contracts/RestrictedPreparationReference.cs` | Validating copied passive reference with content equality |
| `tests/Gagamba.Execution.Tests/Restricted/RestrictedContractTests.cs` | 37 new public-only cases, including preserved literal golden |
| `tests/Gagamba.Execution.Linux.M1.Tests/Contracts/RestrictedPublicProjectionTests.cs` | 9 new pure internal projection/boundary cases; existing friend identity, no new friend assembly |
| `README.md`; `docs/design/restricted-consumer-contract-rc2a-01-api.md` | Consumer timing/non-authority notes; exact finalized API/failure contract |
| This evidence directory | Handoff, property/API inventories, source/blob/host binding, exact run results/history and local binary hashes |
| `docs/handoff.md`, `docs/work-queue.md`, `docs/plans/restricted-consumer-contract-implementation.md` | Actual bounded completion and next-gate status; historical entries retained |

No project, package reference, native source, M1 production source, existing golden/ReferenceEncoder or Hufu file changed.

## D. Build and test evidence

Exact commands below ran from the implementation worktree. Existing project defaults already set `TreatWarningsAsErrors=true`. Successful test-host runs required approved execution outside the filesystem sandbox (the restricted test-host IPC connection timed out). They selected pure in-memory tests, not native workload qualification.

```powershell
dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release --no-restore -warnaserror
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=public-committed.trx' --results-directory artifacts/rc2a-01 --blame-hang-timeout 60s
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests' --logger 'trx;LogFileName=internal-committed.trx' --results-directory artifacts/rc2a-01 --blame-hang-timeout 60s
```

Each command's output was piped through `2>&1 | Tee-Object artifacts/rc2a-01/<run-name>.log`. Build: **0 warnings / 0 errors**. Final tests rebuilt against the exact clean implementation commit; the standalone build used the identical source tree before commit metadata was recorded.

| Final group | Passed | Failed | Skipped |
|---|---:|---:|---:|
| Execution assembly: 37 new public contract cases + 20 existing pure regressions | 57 | 0 | 0 |
| M1 deterministic filter: 19 existing ManifestTests + 9 new projection cases + 10 existing PreparationIdentityTests | 38 | 0 | 0 |
| Total executed against implementation commit | **95** | **0** | **0** |

[Machine-readable test evidence](test-results.json) preserves every final test name/outcome, TRX/log hashes, complete captured attempt logs and counters. [Build outputs](build-artifacts.json) contains exact source-bound local DLL hashes; these are not package or deployment qualification. Raw TRX and logs remain under ignored `artifacts/rc2a-01/`.

Earlier attempts are retained, not counted as successes:

1. First focused baseline `dotnet test ... -c Release --filter FullyQualifiedName~RestrictedContractTests --logger 'trx;LogFileName=baseline.trx' --results-directory artifacts/rc2a-01` stopped during restore with NU1900 because the vulnerability feed was inaccessible. No tests ran.
2. Retried baseline with `-p:NuGetAudit=false` used cached dependencies and failed compilation with CS0234/CS0246 for the absent public contract namespace/types (three diagnostic lines). This is missing-API evidence, not a runtime failure or RC2A-02 qualification. Audit was disabled only on restore commands; no dependency or persistent security setting changed. No vulnerability-audit result is claimed.
3. First implemented public run used `--no-restore`, filter `FullyQualifiedName~RestrictedContractTests`, log `public-first`; VSTest could not connect to testhost after its 90-second timeout and aborted. The first internal attempt (`-p:NuGetAudit=false`, original two-class filter) stalled in restore under the sandbox and was interrupted; no test result was produced. Exact abandoned test-host PID 32528 was later checked; no process remained and no termination was needed.
4. Approved public retry (`public-unsandboxed`) ran 24 then-existing cases: 23 passed / 1 failed. The failing duplicate fixture put `ipc` after `mount`, so the reader correctly reported the earlier order violation. The test was corrected to an adjacent duplicate `pid`; no production contract was relaxed.
5. Expanded public run (`public-final`): 57/0/0. Approved internal restore/test with `-p:NuGetAudit=false` (`internal-unsandboxed`): 38/0/0. Both committed final runs reproduced their counts.

`dotnet --info` initially reported SDK 10.0.401 then failed querying Service Control Manager under sandbox restrictions. `dotnet --version`, `--list-runtimes`, OS and architecture queries supplied the identities above. This is an environment observation, not a build/test failure. No full solution/native test run was attempted; the applicable assemblies/filter were selected to keep the commission passive.

## E. Boundary and negative evidence

| Invariant | Focused assertion and measured result |
|---|---|
| Preserved canonical identity | Existing literal is 873 bytes, SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`; exact re-encode and independent ReferenceEncoder agreement pass |
| Inclusive complete UTF-8 byte bound | A multibyte mode string yields exactly 65,536 bytes: projection, public Encode, Decode, full graph comparison and re-encode pass. Same grammar at 65,537: legacy internal control produces exact bytes, bounded shared Encode and view construction throw `ArgumentException` (`manifest`), Decode returns `InputTooLarge`/no view. No partial returned buffer or public bypass |
| Pre-parser refusal | 65,537-byte and 1,000,000-byte inputs return `InputTooLarge` at offset zero before parsing/allocation of reader values |
| Argv/env cap | Exactly 16,384 combined strict-UTF-8 bytes round-trip; 16,385 refuses construction and a directly modified wire representation refuses decoding |
| Digest | Expected lengths 0/31/33 throw `ArgumentException` (`expectedSha256`); matching 32 verifies; changed and all-zero 32 return false against nonzero golden; zero reference digest remains a present copied value |
| Supports | Exact schema 1/implementation true; schema 0/2/65535, unknown/empty/space/case-variant IDs false; null throws `ArgumentNullException` |
| Malformed/canonical rules | All 873 proper prefixes refuse without partial values; oversized u32 length/count, trailing data, malformed UTF-8 including overlong/surrogate/out-of-range, unknown schema/implementation, unsorted/duplicate sets, undefined enum/boolean/presence tags and forced-value contradictions refuse. Named tag/forced-value tests assert exact offsets |
| Representable distinctions | Null versus nonempty core pattern; absent/exact-zero/unlimited budget; pending tags; empty env value; duplicate argv; empty closure; inode-budget presence; NoNewPrivs=false; NUL/supplementary text survive structurally valid round trips. No new native profile rule is invented |
| Immutability and authority | Input/output byte mutation and caller arrays do not change views/digests; all exported contract classes sealed/get-only, no public view/result constructors, no asynchronous/release/prepare/start/lookup members; reference has exactly five properties |

## F. Authority and cleanup analysis

Only the existing Execution contract assembly changes. The new code uses immutable values, spans, UTF-8, binary primitives, hashing and the existing in-memory encoder; no process/native/provider/registration/dispatch API or new dependency is introduced. It cannot access `PreparationStore`, retained handles, release pipes, owner protocols or runtime selection. Decoding, digest verification and copying GUIDs create descriptions only. Matching content never reconstitutes the store's original capability object. Existing identity tests still refuse foreign issuer/generation, digest mismatch, expiry, abort and fabricated association.

Mode is descriptive data. The approved first profile's `work` mapping adds no argv item; no executable profile admission or mode allowlist is added. Network/inode remain PendingQualification, and decoded flags are no establishment evidence. No runtime-isolation or restricted-execution claim follows.

No workload-native cleanup is applicable because no target/owner/helper ran. Build/test infrastructure is ordinary local .NET execution; a final process inspection found no remaining RC2A test processes. No lifecycles, stop/retention/retry/disposal contracts, C1–C4 or C3 Policy A changed.

## G. GLM finding reconciliation

| Finding | RC2A-01 disposition |
|---|---|
| R1-F01 | Implemented and locally tested: mandatory encode refusal, inclusive complete-byte ceiling, no successful partial output; independent review required |
| R1-F02 | **OPEN, deferred to RC-2B**: no pre-mint stop latch/facade implemented |
| R1-F03 | **OPEN, deferred to RC-2B**: no correlation retention/consumption decision selected |
| R1-F04 | Qualification preserved: passive reference has no temporal fields; consumer timing sources documented in README/XML |
| R1-F05 | Required digest, Supports and oversized-encoding focused assertions pass; RC2A-02 independent qualification remains pending |
| R1-F06 | Evidence-location disclosure preserved: Hufu AE-4 evidence is `Solo\hufu-ae4-implementation`, branch `implementation/ae4-integration-plan`, `37dcf33362e8e745800a530a478fd406795c3e58`; recheck at HUFU-R, no Hufu edits |
| R1-F07 | Verified design-review disclosures retained, no corrective action; no fresh C/native/package qualification claimed |

## H. Limitations and unresolved qualification

No known material implementation blocker remains in the exercised slice. Producer-focused tests are not independent review or exhaustive hostile-codec qualification. Linux/macOS portable runs, exhaustive per-field independent mutations, all future exported vectors, package/nuspec/installed clean consumer, version graph and Hufu delta qualification remain unperformed. No packages were packed, installed or published. The independent ReferenceEncoder and existing golden were preserved, not rewritten.

No C1 independent acceptance, C3/C4 implementation, real owner/capture/recovery integration, production profile, native confinement, cleanup, G3/G4/G5 or release readiness is established. R1-F02/F03 remain open. AE4-B01 remains OPEN. No user authorization to resume Hufu is inferred.

## I. Required next gate

Independent implementation review of the exact source/test commit `e93d642e1a36af25c0e145d328bebadd1bba4838` and this handoff, including API/failure choices, strict reader bounds, immutable projection completeness and both writer guards. Then separately commission RC2A-02 tests-first qualification with its own independent coverage/evidence. Remaining local-package/clean-consumer and mixed graph gates require their own authorized work. Stop here; RC-2A final acceptance and Hufu resumption are not claimed.
