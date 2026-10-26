# Gagamba RC2A-02C-Q2 — Independent Qualification Verification

**Review ID:** RC2A-02C-Q2
**Scope:** Q2A-S / RC-D04 passive managed-code reference and R1-F04 documentation boundary
**Role:** Independent reviewer
**Mode:** Read-only review; focused tests permitted

## Verdict

**SUPPORTED WITH QUALIFICATIONS.** The bounded RC2A-02C claims are supported by the reviewed source/API, the runtime public-surface inventory, independent evidence recalculation, and a reproduced Windows build and focused test run. No contract or test defect was found in the scoped reference behavior.

One nonblocking evidence-document discrepancy remains: `docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/validation.md:74` reports **39/39** package checksums, while the committed `SHA256SUMS` contains 40 entries. I independently verified all **40/40** entries and their committed blobs. This does not indicate a bad digest or altered evidence; correct the count and regenerate the applicable document and package manifests before owner disposition.

The first restricted test invocation timed out after 90 seconds waiting for VSTest to connect to its testhost. VSTest reported one test assembly matched, the run aborted with exit 1, and the TRX contains zero test results. The log, diagnostics, TRX, and exit file are preserved and are not treated as product failures. A subsequent run with normal VSTest process communication passed all three focused tests.

## Git binding and reviewed trees

The review worktree was created from the requested final evidence commit. Exact identities were resolved in the repository:

| Role | Commit | Tree | Parent |
|---|---|---|---|
| RC2A-02A owner acceptance | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | — | — |
| RC2A-02B qualification | `dd16baec48663d40cd29504d98e57e30dd0855df` | — | — |
| RC2A-02B owner acceptance | `dc02951902f11031f8b0d225d2afa2cd537296a3` | — | — |
| Q1 independent review | `5bc396c2e5619906ae202afe0a4244dfd6024d9e` | — | — |
| Q2 independent verification | `60de1f2d34a7df337a426501cb3954fdd3216a6b` | — | — |
| Q2A-S owner disposition / RC2C authorization baseline | `91612b9a42827ffdd76891819670623fa43daeed` | `5f9ec8a9d01627ee3ed0a245ca54642ae1413ea4` | `dc02951902f11031f8b0d225d2afa2cd537296a3` |
| Pre-execution matrix | `66a66bf56508b23b5730827a012ba730b702b72b` | `9e25e4b66d64276030b8d2d670f33989ad408ac6` | `91612b9a42827ffdd76891819670623fa43daeed` |
| Tests-first assertions | `90895ee0865caeeacce2570dce836f25e1e5279f` | `024e1f448dbf91ee7afe3fe07663368ec482a4fa` | `66a66bf56508b23b5730827a012ba730b702b72b` |
| Inventory completion | `e1be8579bcaca806071e88dcdd389fd2a054384b` | `d7d40c3e917c5a56c21cd6eddb96789928d25047` | `90895ee0865caeeacce2570dce836f25e1e5279f` |
| Final tested code | `c4837b0b434fc75ebc3e08d278bdc09c3965a181` | `d47eb6173ecf7b4de442f0f9c165f3ef9e24c88d` | `e1be8579bcaca806071e88dcdd389fd2a054384b` |
| Final evidence/documentation tip reviewed | `62b75f5866d0c339816712baeb04baa386b06f85` | `66f996e1cf973e48487a073b07f1e78d9139c911` | `c4837b0b434fc75ebc3e08d278bdc09c3965a181` |

The matrix is an ancestor of the tests-first, inventory, and tested commits. The reviewed documentation commit is a direct child of the final tested code commit. The final tested `src`, `tests`, and `native` trees are respectively `b8c167e3d7340a562221591bdc2b6df7adb372d1`, `9c906b349b0bf0cffdee331fb5700f9b7828c199`, and `85631d2f04512376621d3d09f51d67fd90f45335`. At the owner baseline, `src` and `native` match those identities; the test tree was `5c3268286575cb55891fae9288627657f2d30ddf`.

`git diff 91612b9a42827ffdd76891819670623fa43daeed c4837b0b434fc75ebc3e08d278bdc09c3965a181 -- src native` is empty. The only code change from that baseline is `tests/Gagamba.Execution.Tests/Restricted/RestrictedContractTests.cs`. No source, test, or native changes occur between `c4837b0` and `62b75f5`. The original qualification worktree at `C:\Users\Laszlos\source\repos\Solo\rc2a-02c` was clean when checked. Q1 and Q2 refs resolve to the supplied commits and are not ancestors of the review branch; neither was merged or changed.

## Independent requirement-to-evidence assessment

I read the pre-execution matrix and compared each row with RC-1 §RC-D04 at `d609c29d3048bc0fd91583068cb013fb906fdf90`, the frozen RC2A-01 API, the owner disposition, source, tests, and run evidence.

| Requirement | Evidence examined | Independent assessment |
|---|---|---|
| Q2A-S-01 — four GUID fields and 32-byte digest | Final reflection inventory; valid constructor case in `PassiveReferenceRejectsInvalidInputsByParameterName` | Pass. The runtime inventory reports the four `Guid` properties and `ImmutableArray<byte>` digest; a 32-byte input constructs. |
| Q2A-S-02 — reject empty IDs and wrong digest lengths with parameter names | `RestrictedContractTests.cs:227-260`; final TRX | Pass. Each ID is made empty independently; lengths 0, 31, and 33 are rejected as `manifestSha256`; the null-array-to-empty-span case is rejected with the same parameter; length 32 succeeds. |
| Q2A-S-03 — copy mutable input | `RestrictedContractTests.cs:199-213`; final TRX | Pass. The source array is mutated after construction and the reference retains the original bytes. |
| Q2A-S-04 — immutable digest surface | Runtime inventory; `RestrictedContractTests.cs:210-213`; final TRX | Pass. The public type is `ImmutableArray<byte>`; mutating a separately materialized `ToArray()` result does not change subsequent reads. |
| Q2A-S-05 — content equality | `RestrictedContractTests.cs:200-205`; final TRX | Pass. Equal values are built from separately allocated digest arrays; generic equality and object equality both succeed. |
| Q2A-S-06 — equal values have equal hash codes | `RestrictedContractTests.cs:202-203`; final TRX | Pass. Equal independently constructed values have equal hash codes. |
| Q2A-S-07 — all fields participate in equality | `RestrictedContractTests.cs:206-223`; final TRX | Pass. Each GUID is changed independently and a digest byte is changed; each changed value compares unequal. |
| Q2A-S-08 — approved passive public surface | Runtime inventory, exact reflection assertions at `RestrictedContractTests.cs:264-373`, frozen API | Pass for the reviewed tree. The 18 exported types match the frozen contract’s 12 view types, two enums, codec, decode result, decode error, and passive reference. For the reference, the test asserts the exact constructor, five get-only property types, direct interface and declared method set; it rejects declared operators, fields, events, and nested types. Inherited `Object` methods are recorded separately. I manually compared the full emitted type set with the frozen API; the test does not hard-code the complete 18-name list. |
| Q2A-S-09 — no temporal authority | Reference inventory, source XML, documentation review | Pass. No temporal property or method is present; source documentation denies expiry, liveness, establishment, and execution permission. |
| Q2A-S-10 — no provider/release/control authority | Exact reference declared-member inventory and source | Pass within the passive type boundary. No control member is exposed. This does not prove downstream runtime enforcement. |
| Q2A-S-11 — no live provenance implied by construction | Constructor test; RC-1 §RC-D04; source XML | Pass. Caller-supplied nonempty identifiers and a valid-length digest construct. The contract explicitly permits copying evidence values and does not grant live-operation authority. |
| Q2A-S-12 — no digest recomputation or attestation requirement | Constructor implementation at `RestrictedPreparationReference.cs:10-22`; frozen API and owner disposition | Pass. The constructor validates length and copies bytes only. The zero-filled 32-byte value is accepted; no recomputation, provenance, or freshness requirement was added. |
| Q2A-S-13 — R1-F04 timing boundary | `documentation-review.md`, RC-1 §RC-D04, and shipped XML at `RestrictedPreparationReference.cs:5-7` | Pass. D4a timing remains explicit enrolled/synthetic data; later D4b timing requires an actual provider-origin prepared execution. The reference does not imply either. |

The assertions exercise public construction, equality, hash, and returned values. Reflection is used to inspect the observable public API; tests do not reach into private constructor state or invoke provider preparation. No provider-origin operation, release, retained-state access, or other excluded capability is invoked.

The test file also contains existing codec tests for golden encode/decode, digest verification, exact `Supports` matching, malformed wire, truncation, and oversize refusal. Those tests are not part of the three-test RC2A-02C filter and are not claimed as RC2A-02C results. Separate existing RC2A-02A/02B tests and evidence cover codec size, digest, and support boundaries; this review did not rerun those suites because they are outside the RC2A-02C passive-reference slice.

## Independent Windows execution

Environment recorded in `rc2a-02c-q2-evidence/windows/environment.txt`: `LAPTOP-KBD8D8JG`, AMD64, PowerShell 7.6.5, .NET SDK 10.0.401, Windows registry `ProductName=Windows 10 Home Single Language`, `DisplayVersion=25H2`, build `26200.9711`.

Build command reproduced at the reviewed code tree:

```powershell
dotnet build tests\Gagamba.Execution.Tests\Gagamba.Execution.Tests.csproj --configuration Release --verbosity minimal --nologo -p:NuGetAudit=false
```

Result: exit 0, **0 warnings / 0 errors**. The build log is preserved at [`build.log`](rc2a-02c-q2-evidence/windows/build.log).

Focused test command reproduced with normal testhost communication; output was directed to the separate review-evidence directory so the original qualification artifacts were not modified:

```powershell
$env:RC2A02C_EVIDENCE_DIRECTORY = 'docs\reviews\rc2a-02c-q2-evidence\windows\elevated'
$filter = 'FullyQualifiedName~RestrictedContractTests.PassiveReferenceCopiesAndComparesContents|FullyQualifiedName~RestrictedContractTests.PassiveReferenceRejectsInvalidInputsByParameterName|FullyQualifiedName~RestrictedContractTests.PublicSurfaceIsSealedReadOnlyAndPassive'
dotnet test tests\Gagamba.Execution.Tests\Gagamba.Execution.Tests.csproj --configuration Release --no-build --no-restore --filter $filter --logger 'trx;LogFileName=rc2a-02c-q2-independent.trx' --results-directory docs\reviews\rc2a-02c-q2-evidence\windows\elevated --verbosity minimal --diag docs\reviews\rc2a-02c-q2-evidence\windows\elevated\vstest-diag.log
```

Result: exit 0, **3 passed / 0 failed / 0 skipped**. The independently generated TRX has the same three test names and outcomes as the original final TRX; the independently generated surface inventory has the same 18 exported types and same reference declared surface. See the [review TRX](rc2a-02c-q2-evidence/windows/elevated/rc2a-02c-q2-independent.trx), [test log](rc2a-02c-q2-evidence/windows/elevated/test.log), and [review inventory](rc2a-02c-q2-evidence/windows/elevated/public-surface-inventory.json).

The default restricted invocation is preserved at [`windows/test.log`](rc2a-02c-q2-evidence/windows/test.log), with [`test.exit`](rc2a-02c-q2-evidence/windows/test.exit), the zero-result [`TRX`](rc2a-02c-q2-evidence/windows/rc2a-02c-q2-independent.trx), and VSTest diagnostics. It timed out after 90 seconds waiting for testhost communication, returned exit 1, and executed zero tests. The successful normal-communication retry is separately recorded under `windows/elevated/`.

## Evidence-integrity checks

I independently recalculated from the reviewed checkout and compared the Git blob identities:

- `SHA256SUMS`: **40/40** hashes match the files; each committed blob also matches the raw captured file bytes.
- `document-sha256.json`: **16/16** LF-normalized byte bindings match.
- Evidence package Markdown links: **14/14** relative links resolve.
- Source binding: `executionCommit=c4837b0b434fc75ebc3e08d278bdc09c3965a181`, tree `d47eb6173ecf7b4de442f0f9c165f3ef9e24c88d`; source/test/native trees and ancestry match the committed code.
- Original versus independently reproduced TRX: **3/3** test-name/outcome pairs match.

The stale `39/39` statement in `validation.md:74` is the only confirmed documentation inconsistency. The manifest itself contains 40 entries, all valid; the additional package file is `.gitattributes`. No raw qualification artifact is missing or changed.

## Scope exclusions

This is Windows-only qualification of the passive managed-code value and R1-F04 documentation boundary. It does not establish provider-origin preparation, deadline handling, release behavior, retained-state authorization, native confinement, package/distribution readiness, reproducibility, Hufu integration, native Linux/macOS behavior, or full RC2A-02 acceptance. RC2A-02C remains locally qualified only; Q2A-S / RC-D04 requires its separate owner disposition. No RC2A-03 work is authorized or started.

## Re-verification (2026-10-26)

A second independent execution reproduced the focused build and test run on the same host and SDK, with results written to [`rc2a-02c-q2-evidence/windows/rerun-20261026/`](rc2a-02c-q2-evidence/windows/rerun-20261026/test.log):

- Build: exit 0, 0 warnings / 0 errors ([`build.log`](rc2a-02c-q2-evidence/windows/rerun-20261026/build.log), [`build.exit`](rc2a-02c-q2-evidence/windows/rerun-20261026/build.exit) = 0).
- Focused tests: exit 0, **3 passed / 0 failed / 0 skipped** ([`rc2a-02c-q2-rerun.trx`](rc2a-02c-q2-evidence/windows/rerun-20261026/rc2a-02c-q2-rerun.trx)); the three test names and outcomes match the committed final qualification TRX.
- The rerun [`public-surface-inventory.json`](rc2a-02c-q2-evidence/windows/rerun-20261026/public-surface-inventory.json) is byte-identical to both the committed qualification final inventory and this review's earlier independent inventory.

The re-verification also independently confirmed: all 40 `SHA256SUMS` digests against the checkout; all 16 `document-sha256.json` LF-normalized bindings; 14/14 relative evidence links; the pre-execution matrix diff (status/evidence-path updates only, requirements unchanged); commit chronology (matrix `66a66bf` at 21:25:59 precedes every test commit, and the final TRX run time falls between `c4837b0` and `62b75f5`); the RC-1 blob `4b300b01` and frozen-API blob `2d6b9d12` identities (the API blob is unchanged since the RC2A-02A acceptance `e5fff9e8`); and that Q1/Q2 review commits are not ancestors of the qualification or review branches. No new finding arose.

## Findings and recommendation

**Q2C-Q2-F01 — Low, nonblocking documentation inconsistency**
**Location:** `docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/validation.md:74`
**Evidence:** The text says 39/39 package checksums; the manifest has 40 lines, and independent SHA-256 plus committed-blob checks pass 40/40.
**Impact:** The validation summary understates the number checked. It does not invalidate the underlying checksums, document bindings, test run, or code binding.
**Disposition:** Correct the count to 40/40 in a separate documentation-only evidence update and regenerate the bound document digest and package manifest. No production or test change and no qualification rerun are indicated.

No contract or test defect, missing evidence for the scoped passive-reference claims, or unsupported portability/security claim was found. Recommend that the owner record a separate, explicitly bounded RC2A-02C disposition after the count correction. This review does not issue that decision, close full RC2A-02, or close unrelated obligations.