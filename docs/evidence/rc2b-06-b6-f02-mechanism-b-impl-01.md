# B6-F02 Mechanism B bounded implementation 01 — evidence and Q1 handoff

**2026-11-05 · GAGAMBA-RC2B-06-B6-F02-MECHANISM-B-IMPL-01 · canonical `main`.**

**IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT Q1.**

Commission: `GAGAMBA-RC2B-06-B6-F02-MECHANISM-B-IMPL-01`

Owner authority: [Mechanism B owner acceptance](../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md)

Authorized plan: [B6-F02 Mechanism B bounded implementation 01](../plans/rc2b-06-b6-f02-mechanism-b-impl-01.md)

Q1 handoff: [Independent Q1 questions](../reviews/rc2b-06-b6-f02-mechanism-b-impl-01-q1-handoff.md)

## 1. Source binding

| Item | Commit | Tree |
|---|---|---|
| Canonical baseline | `504b6d06ac88591072b11f428ae88cade390c075` | `e3f028dc32c48935fb4abdc6a553a2641c960c53` |
| Owner acceptance, plan and bulletins | `4aab87761078a92d05cd08aa1998cbe2c4f6aaf1` | `582e80115f01d7adec6c7d771a875beb315901c5` |
| Production and portable tests | `831c980e517cff48885aec24283d91a2b41e0860` | `66bda80f7aec5f92a4ddcac309f305bf62568846` |
| Evidence, Q1 handoff and final bulletins | this commit | recorded in the final handoff |

The remote `origin/main` remained `21f0aa7f3afd3a7dbfbc6ecae51d4025b13439b7`. The owner-acceptance, implementation and evidence commits are local only and were not pushed.

## 2. Committed file hashes

SHA-256 of the exact committed files at `831c980e517cff48885aec24283d91a2b41e0860`:

| File | SHA-256 |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Installed/FinalAcceptance.cs` | `5f54b4e30b37bbe586c10457021e9d8a3658662eb07f2d06d19d62060215c180` |
| `src/Gagamba.Execution.Linux.M1/Installed/FinalAdmissionVerifier.cs` | `c494312dbbc589cd8bb31624164725eb8eda3a7c1eb9bddd6baed5362a0ae4c36` |
| `src/Gagamba.Execution.Linux.M1/Installed/AdmissionAuthority.cs` | `d331344a5449ed027cd2da8e307b3cd12f1b8058a5a731e5a41403aa92191297` |
| `src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.cs` | `dc9ac2d06689adc6a3ecb7f5a9e6bd6b895d5be1ef4e9d25046dcf420fc82aed` |
| `src/Gagamba.Execution.Linux.M1/Installed/InstalledMetadata.cs` | `c09165e2e6439eaa7b4d6e2ca7f69e3eadad2c4d6d3220a902b114d6eaf4d8f1` |
| `src/Gagamba.Execution.Linux.M1/Installed/LoadedInstallation.cs` | `f2a1fa61e45ae11ff639de515beb2708762c84f1d3d9af772d3beab9f57df373` |
| `src/Gagamba.Execution.Linux.M1/Restricted/B3Qualification.cs` | `621cb288e0cc661197671db8d99cfbbfd5dd5abcc3a959da5cc69f05b9f3eeee` |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedB3Client.cs` | `9dad0f6396146500c248a163ee565a251bb0703803fbb131acec5b780db11b3b` |
| `tests/Gagamba.Execution.Linux.M1.Tests/Installed/FinalAcceptanceTests.cs` | `3b76fe7ef18ca21ca28cbbb2cd6a9f15fe148c7493cfad7f64872a1e928c3fb6` |

The commit changed nine files: eight production files and one test file. No project, dependency, packaging, install-tool or publication file changed.

## 3. Implementation boundary

### 3.1 Strict final-acceptance codec

`FinalAcceptance.cs` implements:

- the exact 24-field final record, schema version 1, with `acceptance` fixed to `final-native-accepted` and `buildConfiguration` fixed to `Production`;
- the exact 7-field selector/approval, schema version 1, with `accepted` required to be the JSON literal `true`;
- host identity as exactly 64 lowercase hexadecimal ASCII characters followed by exactly one LF;
- ASCII printable bytes only, one root object, strict ascending keys, no duplicate/unknown/missing keys, canonical reserialization equality, and a 65,536-byte maximum;
- unsigned canonical integers only, bounded strings and limitations, and exact UTC timestamp syntax;
- no filesystem access, directory search, fallback, `latest` selection, caller-supplied expected hash, or self-asserted acceptance.

`FinalAdmissionEvaluation.Evaluate` recomputes the final-record SHA-256 from the exact record bytes and binds the selector, record and retained S2 expectation. It returns only descriptive binding data; it does not create admission authority.

### 3.2 Protected-store verifier and private grant

`FinalAdmissionVerifier.cs` reads only:

- `/etc/gagamba/host-id`;
- `/opt/gagamba/final-admission-selectors/<artifactSetSha256>.json`;
- `/opt/gagamba/final-admissions/<finalRecordSha256>.json`.

It does not contain or read `/etc/gagamba/accepted-final-admissions`; a source-tree search found no occurrence of that path. The selector filename is derived from the retained S2 artifact-set digest, and the record filename from the protected selector's final-record hash. No caller path or expected hash is accepted.

`FinalAdmissionVerifier.Grant` has a private constructor. Its only factory is `Grant.VerifyInstalled`, which performs the full verification. The outer `FinalAdmissionVerifier.VerifyInstalled` forwards to that factory and is the only path exposed to `LoadedInstallation.VerifyFinalAdmission`. A bare binding, Boolean, selector, or validation result cannot construct a grant.

Verification order:

1. require a `Qualified` S2 selection with manifest, identity, qualification ID and raw qualification-record hash;
2. open the three protected directories with descriptor-relative trusted reads;
3. read and strictly parse the 65-byte host identity;
4. read and strict-parse the selector and check exact S2 binding;
5. read and strict-parse the record and evaluate all bindings;
6. call `InstalledFileSystem.VerifyActive()` again and require the original S2 identity, qualification ID, raw record hash, source commit and source tree to be unchanged;
7. reread all three files and require exact bytes and snapshots to be unchanged;
8. reopen all three directories and require directory snapshots to be unchanged;
9. return a grant holding the fresh S2 result.

### 3.3 Fail-closed admission separation

`AdmissionAuthority.cs` adds:

- `IsFinalAdmissionEligible`, which requires a non-null grant and an exact binding match;
- `IsFinalAdmissionBindingEligible`, a pure test/binding predicate that is never authority;
- `IsProductionAdmissionAuthorized`, which additionally requires an activation object;
- `ProductionActivationAuthorization`, an unreachable type with a private constructor and no factory in this slice.

A portable test reflects over the type and confirms every constructor is private and no parameterless static factory returning the type exists.

### 3.4 S2 raw-record plumbing

`InstalledResult` now retains `QualificationRecordSha256` from the receipt-selected S2 record. `InstalledFileSystem.VerifyActive` returns it only on the trusted qualified path. `LoadedInstallation.Revalidate` now also compares that raw hash. `FinalAdmissionExpectation.FromSelection` takes the artifact digest, qualification ID and raw record hash from the retained S2 selection and never from a caller.

### 3.5 Pre-effect admission gates

- `B3Qualification.TryCreate` refuses when `QualifiedInstallation=true` and `IntegratedRecovery` is null, before the legacy non-integrated path can be used. The final-admission gate runs after existing source/closure checks and before `B3PreparationOwner` construction, which creates the journal directory.
- `IntegratedB3Client.Prepare` verifies final admission before `RecoveryConnection.Start` can spawn recovery or owner processes.
- `RestrictedExecutionRuntime.Create()` still returns `UnavailableRestrictedExecutionProvider`. No public activation factory, RELEASE writer, release-arbiter change, recovery stop-only change, C3 change, or Unknown-policy change was introduced.

### 3.6 Protected-directory reader

`InstalledFileSystem.OpenProtectedDirectory` reuses the existing absolute-path, trusted-ancestor, no-follow, metadata, capability, sparse-file and stability primitives. It requires each exact leaf directory to be root-owned `0755` and each ancestor to be root-owned and not group/other writable. `ReadFile` rejects non-single-component names and reads a `0444`, single-link regular file with a bounded size. A future host must provision `/etc/gagamba`, `/opt/gagamba/final-admission-selectors` and `/opt/gagamba/final-admissions` accordingly.

## 4. Verification executed

Environment: Windows x64, canonical repository `C:\Users\Laszlos\source\repos\Gagamba`, .NET SDK 10.0.401, Debug configuration. Commands were run from the repository root on 2026-11-05.

```powershell
dotnet build tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --nologo
dotnet test tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --nologo --filter "FullyQualifiedName~FinalAcceptanceTests"
dotnet test tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --nologo
```

Observed results:

| Check | Result |
|---|---|
| Debug build | **0 warnings, 0 errors**; `Build succeeded` |
| Compilation-identity validation | **PASS** for provider and owner; instrumented test-build mode; 4/4 hook types, 3/3 hook properties, 12/12 core production types |
| Focused `FinalAcceptanceTests` | **69 passed, 0 failed, 0 skipped, 69 total** |
| Full M1 runner | **1257 passed, 331 skipped, 0 failed, 1588 total** |

The build output identified the instrumented Debug M1 assembly as SHA-256 `70B4427489D388825DC6560310C484273754807E4BC72DFC9407BF86ECA27C91`, MVID `14ec54b6-aeeb-4e67-a764-322107b61e35`. This is a Debug test-build identity, not an installed production binary qualification.

**The 331 skips are not passes.** They are Linux/native tests that did not execute on Windows. No protected-store positive, installed acceptance record, host identity, privileged installation, native loader closure, or production activation was created or observed.

### 4.1 Focused test coverage

The 69 focused cases comprise:

| Area | Cases |
|---|---:|
| Canonical record/selector binding | 1 |
| Empty and maximum limitations | 1 |
| Noncanonical CJSON refusal: trailing space/newline, BOM, non-ASCII, duplicate key, multiple objects, escapes, negative/float/exponent/leading-zero integers | 12 |
| Malformed record schema: unknown/missing/unsorted fields, wrong literals, host, IDs, time, limitation bounds, hash case/length, null host | 16 |
| Oversize and non-object roots | 1 |
| Malformed selector: false/non-Boolean acceptance, wrong schema, unknown/missing fields, bad ID, hash case/length | 8 |
| Host identity exact-encoding refusals | 9 |
| Accepted exact host encoding | 1 |
| Final binding mismatch dimensions | 17 |
| S2 `Qualified` alone is not final or production admission | 1 |
| Final binding does not satisfy a different S2 selection | 1 |
| Production activation has no factory | 1 |
| **Total** | **69** |

The test file is in the M1 test project. The production project references only `Gagamba.Execution` (`dotnet list ... reference`); it does not reference the test project.

## 5. N/S dispositions for this bounded slice

Rows N01-N21 and N29 are reported because they intersect final acceptance or its immediate admission boundary. N22-N28 and N30-N38 are untouched by this slice and remain specifications; no row below is claimed as a native pass unless a protected-store procedure actually ran, which it did not.

| Row | Disposition in this slice |
|---|---|
| N01 | **Implemented in source, portable negative; native pending.** The verifier uses the fixed selector path and cannot use a record as its own expected hash or fall back to an absent selector. No protected-store positive or missing-selector native run was executed. |
| N02 | **Implemented in source; native pending.** No runtime API takes a caller path or expected hash. Selector and record names are derived from retained S2 bytes. Source inspection, not a native caller-injection run. |
| N03 | **Portable coverage.** Recomputed record hash must equal the protected selector hash; the `record-hash` mismatch test passes. Native substituted protected bytes are pending. |
| N04 | **Portable coverage for runtime schema.** The 24-field record, canonical reserialization, duplicate/unknown/missing/type/literal/time/bounds/hash tests pass. Publication-time gate-results/evidence completeness is not implemented (publisher non-goal). |
| N05-N14 | **Not in this slice.** Full Mechanism B live session, replay, concurrency, crash cutpoints, process substitution and descriptor custody are not implemented or exercised. No disposition is claimed beyond the existing accepted S2 behavior. |
| N15 | **Partial boundary only.** The verifier revalidates the retained S2 selection after reading final bytes. The full B generation/HELD/active-generation behavior is not implemented; Q10/S01 remain open. |
| N16 | **Portable coverage.** Artifact digest plus qualification ID plus raw qualification-record hash are bound across selector, record and retained S2; selector/record ID and raw-hash mismatch cases pass. Native record reassociation is pending. |
| N17-N19 | **Not in this slice.** Final/F02 publication interruption, selector visibility, withdrawal and quarantine are publisher/administrator concerns; no publisher or withdrawal path exists here. Runtime absence/noncanonical data fails closed by construction, but the publication procedures were not executed. |
| N20 | **Partial foundation only.** Two pre-effect gates and an unreachable activation type exist; the complete production sink map and all legacy bypass candidates are not audited (S09 open). Pure separation predicates pass; no native call-graph positive was run. |
| N21 | **Preserved by non-modification; not exercised.** No RELEASE writer, release-arbiter, owner/recovery authority, C3 or Unknown-policy source changed. Native attempt-count and substitute-actor procedures were not executed. |
| N29 | **Implemented in source; native pending.** Protected reads reuse descriptor-relative no-follow, metadata, single-link, `0444` and stability checks and reject path traversal components. No symlink/hardlink/FIFO/device/changed-inode native mutation was run in this commission. |
| N22-N28, N30-N38 | **Specifications only, outside this bounded slice.** They remain future qualification obligations and are not implemented, waived, or counted as passes. |
| S01 (Q10) | **OPEN NON-WAIVER — NOT IMPLEMENTED.** The pre-RELEASE active-generation reread and refusal before a new `ReleaseRequest` remain mandatory for a later commission. No source change claims this behavior. |
| S02 | **Not implemented.** No final/F02 publication or approval-withdrawal path exists in this slice. |
| S03 | **Not implemented.** No block/quarantine or publication storage-error path exists in this slice. |
| S04 | **Not implemented.** No catalog approval publication chain exists in this slice. |
| S05 | **Not implemented.** No session capability or release correlation tuple exists in this slice. |
| S06 | **Pending native qualification.** No native host environment was provisioned. Missing non-Linux/native primitives fail closed as unavailable, but the required individual native mechanism controls were not executed. |
| S07 | **Not implemented.** Protocol and concurrency expansion is outside this slice. |
| S08 | **Not implemented; native pending.** No privileged execution path was added, and no native descriptor/custody/privilege-drop procedure was run. |
| S09 | **Not implemented.** Only the two named gates are covered; the full construction/allocation/release sink map remains future work. |

## 6. Preservation proof

The baseline preservation inventory was rechecked before evidence creation:

- `git status --porcelain=v1 -uall` reported **44** historical untracked paths and **zero** tracked modifications.
- `git worktree list --porcelain` reported **74** worktrees.
- No historical untracked path was deleted, renamed, cleaned, or staged.
- `git diff --check` reported no whitespace errors.
- No branch, linked worktree, reset, rebase, stash, clean, prune, force operation, or push was performed.

The same 44-path and 74-worktree counts are rechecked after the evidence/Q1/status commit and recorded by the final `git status` and `git worktree list` output.

## 7. Qualifications and open obligations

1. **No native protected-store positive.** `/etc/gagamba`, `/opt/gagamba/final-admission-selectors` and `/opt/gagamba/final-admissions` were not provisioned. No root-owned host identity, selector, record or approval exists. N01-N04, N16, N20, N21, N29 and S06/S08 remain unexecuted at native level.
2. **`platformInventorySha256` mapping.** The record field `platformInventorySha256` maps to the existing S2 manifest field `platform.runtimeInventorySha256`. This is an implementation mapping to review, not a second independent platform inventory.
3. **False-`QualifiedInstallation` legacy path.** `QualifiedInstallation=true` now refuses a null `IntegratedRecovery` instead of permitting the legacy non-integrated path. Existing tests use `false`, so legacy behavior is unchanged; the new refusal needs independent review as an intentional fail-closed gate.
4. **Q10 open.** The accepted pre-RELEASE active-generation reread/refusal is not implemented. It must not be silently waived or treated as satisfied by the final-verifier S2 revalidation.
5. **Publisher and full sink map absent.** No final-record publisher, gate-results validator, approval publication, withdrawal, quarantine, lock protocol, or complete production admission sink enumeration is implemented. The two gates are foundation, not full coverage.
6. **No production activation.** `RestrictedExecutionRuntime.Create()` remains unavailable; `ProductionActivationAuthorization` has no factory. Final admission is eligibility only.
7. **No out-of-scope work.** Full session-bound capability, GAVA purposes, actor/descriptor binding, installed-profile driver, S4/B7, protected-store provisioning, privileged installation, native loader closure, packages and push remain out of scope.
8. **Skipped native tests are not passes.** The Windows full-suite result is a portable regression result only.

## 8. Disposition

**IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT Q1.**

The bounded source implementation is committed on canonical `main`. Independent Q1 must verify the source binding, schema/codec strictness, grant construction, admission separation, gate placement, preservation inventory, and the truthfulness of every pending-native and open-Q10 statement above.
