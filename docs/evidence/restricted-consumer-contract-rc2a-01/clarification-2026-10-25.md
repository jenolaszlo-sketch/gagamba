# RC2A-01 owner clarification and current implementation evidence

Date: 2026-10-25 (Asia/Manila). Scope: passive contract implementation and focused
implementation tests. Host: Windows 11, AMD64, .NET SDK 10.0.401; native
prerequisites not exercised. No target, owner or helper workload was started.

Disposition: **RC2A-01 IMPLEMENTED - INDEPENDENT REVIEW REQUIRED**.

## 1. Git binding and starting-state observation

The supplied commission binds owner decision
`9a705335701f0c1923bd9867a2612b8a93afa8cb`, tree
`f6a7e141bf7f38c976655c0772b7ee817a7ce148`, design
`d609c29d3048bc0fd91583068cb013fb906fdf90` and independent design review
`feb9d7584c41712195d4b90754ed7a342ba74bb7`. Those identities and their direct
parent relationships were rechecked. The owner worktree remains unchanged/clean.

The prescribed worktree/branch already existed when this execution resumed:
`C:\Users\Laszlos\source\repos\Solo\gagamba-rc2a-01`, branch
`implementation/gagamba-rc2a-01`, clean at
`5a5ef7bdde0708411cd875d0b0700556199134c1`. Attempting the requested creation
reported that the branch already existed; no branch/worktree was reset. Inspection
found implementation `e93d642e1a36af25c0e145d328bebadd1bba4838`, directly based on
the accepted owner decision, followed by its documentation child `5a5ef7b`.
The existing isolated worktree was reused and both commits preserved.

The current clarification implementation is
`781e9debb8df852d1d4d8ca98d92591e1609e193`, directly based on `5a5ef7b`:

| Object | Identity |
|---|---|
| Current implementation root tree | `65db7ed171ba25b82b9ab21aae9196964d4f27bc` |
| Current src tree | `b8c167e3d7340a562221591bdc2b6df7adb372d1` |
| Current tests tree | `ceaa7aca8121a1acef5300e074a1f6bc73c0a781` |
| Unchanged native tree | `85631d2f04512376621d3d09f51d67fd90f45335` |

The evidence commit is a documentation-only child of that implementation. Final
commit/tree and clean-state checks are in the external completion receipt/user
report, avoiding self-referential hashes. The
[source and authority binding](clarification-source-binding.json) records exact
Git blobs/hashes and the current commission attachment hash.

## 2. Owner direction and frozen behavior

During this execution, the user explicitly selected the proposed refusal rules
with **“Use these refusal rules”**, then instructed:

> 1. Record these behaviors in the RC2A-01 contract documentation.
> 2. Ensure deterministic tests cover all three.
> 3. Preserve the existing golden vector and canonical encoding.
> 4. Continue the assigned work without reopening RC-1.

The selected rules are now recorded in the
[API finalization](../../design/restricted-consumer-contract-rc2a-01-api.md):

| Rule | Current behavior | Deterministic coverage |
|---|---|---|
| Oversized Encode | Complete encoding above 65,536 bytes throws `ArgumentException`; no partial buffer | `CompleteUtf8ByteCeilingIsInclusiveAndSymmetric`, 65,536/65,537 cases; shared bounded encoder and projection guard |
| Wrong-length digest | `VerifySha256` returns false for every digest length other than 32 | `WrongDigestLengthReturnsFalse`, lengths 0/31/33, plus matching/nonmatching/all-zero 32-byte controls |
| Supports | Null implementation ID throws `ArgumentNullException`; unsupported non-null schema/ID returns false | `SupportsIsExact` cases and `NullArgumentsAreProgrammingErrors` at schemas 0/1/65535 |

The wrong-length result supersedes the original implementation's exception
choice. Null manifest arguments remain programming errors. No fields, canonical
bytes, decode rules, new APIs or execution authority were added. The existing
twelve immutable views, five-field passive reference, strict decoder, single
encoder and Mode=`work` semantics remain implemented. No temporal fields were
added to the reference. Complete public API/property inventory remains in the
original [API inventory](api-inventory.md) and [field map](field-map.md); signatures
are unchanged by this clarification, and current source hashes are rebound.

## 3. Changed source and preservation checks

Only `RestrictedManifestCodec.cs` changes production behavior: the existing
wrong-length exception becomes false, with matching XML documentation. Public
tests change the three corresponding assertions and add null-ID checks for
supported/maximum schema values. The API document records the explicit owner
selection. Original RC-1, owner decision and GLM review remain unchanged.

`RestrictedManifest.cs` (the authoritative encoder), reader, model projections,
reference, native code, M1 production code, project/package metadata, original
`ManifestTests.cs` and `ReferenceEncoder.cs` are unchanged from the preceding
implementation. The public golden literal is unchanged even though its test
file has new assertions. Exact preservation is checked against the original
literal at the owner baseline: 873 bytes, SHA-256
`82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`.

## 4. Actual build and test results

Runs used the same source/test file bytes committed in `781e9de`. They ran just
before that commit with HEAD at `5a5ef7b`; this is not represented as a clean
post-commit build or independent qualification. Source hashes bind the tested
bytes, and local DLL hashes are retained without claiming package provenance.

| Run | Result | Evidence |
|---|---|---|
| New digest expectations against old production implementation | 3 failed / 0 passed / 0 skipped, each throwing the old `ArgumentException` | `clarification-baseline.trx` and log |
| Release Execution build, warnings as errors | Success, 0 warnings / 0 errors | `clarification-build.log` |
| Full Execution test assembly | 57 passed / 0 failed / 0 skipped | `clarification-public.trx` and log |
| Selected deterministic M1 manifest/projection/identity classes | 38 passed / 0 failed / 0 skipped | `clarification-internal.trx` and log |

Exact commands, run times, individual outcomes, raw log text, TRX/log hashes and
local DLL hashes are preserved in [test evidence](clarification-test-results.json).
Raw files remain under ignored `artifacts/rc2a-01/`. Existing historical failed
runs remain in the original evidence; none is relabeled as a pass. Commands:

```powershell
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~WrongDigestLengthReturnsFalse' --logger 'trx;LogFileName=clarification-baseline.trx' --results-directory artifacts/rc2a-01 --blame-hang-timeout 60s
dotnet build src/Gagamba.Execution/Gagamba.Execution.csproj -c Release --no-restore -warnaserror
dotnet test tests/Gagamba.Execution.Tests/Gagamba.Execution.Tests.csproj -c Release --no-restore --logger 'trx;LogFileName=clarification-public.trx' --results-directory artifacts/rc2a-01 --blame-hang-timeout 60s
dotnet test tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests' --logger 'trx;LogFileName=clarification-internal.trx' --results-directory artifacts/rc2a-01 --blame-hang-timeout 60s
```

Each command used `2>&1 | Tee-Object -FilePath artifacts/rc2a-01/<run-name>.log`
and preserved the actual command exit code. Test-host execution used approved
sandbox escalation for its IPC, as required by the previous implementation's
recorded environment; no restore/network/package operation was performed.

The 95 passing cases include original golden/reference-encoder equivalence,
strict parser negatives, both complete-byte size boundaries, immutability,
preparation-reference checks and public absence of execution control. These are
focused RC2A-01 implementation tests, not the separately commissioned RC2A-02
tests-first qualification or independent acceptance.

The first evidence-collection pass failed reading UTF-8 typographic quotes with
the Windows default CP1252 codec. The authoring script was corrected to specify
UTF-8 and rerun; no production or test source change was required. This is a
tooling observation, separate from the actual build/test results above.

## 5. GLM dispositions, remaining limits and next action

| Finding | Current disposition |
|---|---|
| R1-F01 | Mandatory inclusive encode/decode ceiling preserved and focused boundary tests pass; independent review required |
| R1-F02 | **OPEN, deferred to RC-2B** |
| R1-F03 | **OPEN, deferred to RC-2B** |
| R1-F04 | Passive reference has no temporal fields; original README/XML consumer qualification preserved |
| R1-F05 | Digest false-result, Supports and oversized-encode expectations are documented and deterministic tests pass |
| R1-F06 | Hufu AE-4 evidence remains at `Solo\hufu-ae4-implementation`, branch `implementation/ae4-integration-plan`, `37dcf33362e8e745800a530a478fd406795c3e58`; no Hufu edits |
| R1-F07 | Verified design-review disclosures remain preserved; no corrective or native qualification claim |

Code review of the delta found only passive digest comparison behavior; matching
bytes/GUIDs still cannot reach a store, release capability, native handle or
provider selection. Original C1–C4 and C3 Policy A are untouched. No native workload
cleanup applies. Builds include M1/Owner dependencies but the selected tests do
not start them. Windows/macOS/Linux confinement is not established here.

Independent GLM review must now bind `781e9de` plus this evidence and the original
implementation ancestry; review the full passive implementation, not just the
one-line follow-up. Local NuGet/clean-consumer/mixed Hufu graph qualification,
RC2A-02, real owner/facade/native gates and production readiness remain pending.
AE4-B01 remains OPEN; Hufu resumption is not authorized. No pack, push, merge,
tag or publication occurred. Stop at the evidence commit for independent review.
