# B6-F02 Mechanism B bounded implementation 01

Date: 2026-11-05 (Asia/Manila)

Commission: `GAGAMBA-RC2B-06-B6-F02-MECHANISM-B-IMPL-01`

Owner authority: [Mechanism B owner acceptance](../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md)

Canonical baseline: commit `504b6d06ac88591072b11f428ae88cade390c075`, tree `e3f028dc32c48935fb4abdc6a553a2641c960c53`

Status: **IMPLEMENTATION PLAN AUTHORIZED BY THE OWNER DECISION; NO PRODUCTION ACTIVATION**

## 1. Objective

Implement the bounded first slice of the accepted Mechanism B foundation:

- protect final-acceptance records against caller selection and self-assertion;
- bind final admission to one retained successful S2 selection and the local protected host identity;
- keep S2 `Qualified`, final admission, and production activation separate;
- refuse final-admission-dependent operations before their first side effect when the protected final stores are absent, malformed, changed, or inconsistent;
- leave the existing owner-only RELEASE authority, recovery stop-only behavior, original C3 stopping, and the unavailable production factory untouched.

## 2. In scope

### 2.1 Codec and schema

Add an internal strict CJSON-1 final-acceptance codec in `src/Gagamba.Execution.Linux.M1/Installed/FinalAcceptance.cs`:

- exact 24-field final record schema, version 1;
- exact 7-field selector/approval schema, version 1, with `accepted` required to be the literal `true`;
- host identity exactly 64 lowercase hexadecimal characters plus one LF;
- ASCII printable bytes only, one object, sorted keys, no duplicate, unknown, or missing keys;
- canonical reserialization equality;
- 65,536-byte maximum;
- no directory search, fallback, `latest`, caller-supplied expected final hash, or self-asserted acceptance;
- exact `limitations` bounds and accepted literal values for `acceptance` and `buildConfiguration`.

### 2.2 Pure binding evaluation

Add a pure evaluation that:

- computes the final-record SHA-256 from the exact protected record bytes;
- requires the selector to name that exact hash;
- binds the selector and record to the retained S2 artifact-set digest, qualification ID, raw qualification-record hash, profile/version/RID/scope, source commit/tree, platform inventory, and owner decision;
- binds the record host ID to the protected local host ID;
- returns only descriptive binding data, never authority.

### 2.3 Protected-store verifier

Add `src/Gagamba.Execution.Linux.M1/Installed/FinalAdmissionVerifier.cs`:

- host identity: `/etc/gagamba/host-id`;
- selector: `/opt/gagamba/final-admission-selectors/<artifactSetSha256>.json`;
- record: `/opt/gagamba/final-admissions/<finalRecordSha256>.json`;
- never read `/etc/gagamba/accepted-final-admissions` at runtime;
- use the existing protected descriptor-relative reader with `O_NOFOLLOW`/`openat2`, exact `fstat`, reacquire, and no-follow semantics;
- require root ownership and exact `0755` leaf stores, with all ancestors root-owned and not group/other writable;
- require `0444`, single-link regular files;
- revalidate the original S2 selection after final bytes are read;
- reread all three protected files and reacquire their directory snapshots before returning;
- expose a private immutable grant whose constructor is private and whose only factory is the full verification routine.

### 2.4 Admission separation

Add `src/Gagamba.Execution.Linux.M1/Installed/AdmissionAuthority.cs`:

- pure final-admission eligibility from a verified grant plus the retained S2 selection;
- a distinct production-activation predicate that also requires an activation authorization;
- an unreachable `ProductionActivationAuthorization` type with a private constructor and no factory in this slice;
- no Boolean, legacy selector, bare binding, or validation result may substitute for a grant.

### 2.5 S2 raw-record plumbing

Extend `InstalledResult` with the raw receipt-selected qualification-record hash, populate it only from the trusted `VerifyActive` path, and compare it in `LoadedInstallation.Revalidate`.

### 2.6 Admission gates

Add final-admission gates:

- in `Restricted/B3Qualification.cs`, after existing source/closure checks and before `B3PreparationOwner` construction, which creates the journal directory;
- in `Bootstrap/IntegratedB3Client.cs`, before `RecoveryConnection.Start` can spawn recovery or owner processes;
- refuse `QualifiedInstallation=true` with a missing integrated recovery path, so the legacy non-integrated branch cannot become a fallback;
- convert verifier failures into the existing typed refusal where `TryCreate` already catches them.

### 2.7 Protected directory reader

Extend `InstalledFileSystem` with a protected-directory abstraction and a protected-file read method. The reader must reuse the existing pinned descriptor, no-follow, metadata, capability, sparse-file, and stability checks rather than adding a second filesystem path.

### 2.8 Tests

Add `tests/Gagamba.Execution.Linux.M1.Tests/Installed/FinalAcceptanceTests.cs` covering:

- canonical record/selector positive binding;
- empty and maximum limitations;
- noncanonical CJSON-1 refusal: trailing bytes, BOM, non-ASCII, duplicate keys, multiple objects, escapes, negative/float/exponent/leading-zero integers;
- malformed schema refusal: unknown/missing fields, ordering, literals, host, hashes, timestamps, limits;
- selector refusal: false/non-Boolean acceptance, wrong schema, unknown/missing fields, invalid IDs, bad hashes;
- host identity exact encoding;
- every final binding mismatch dimension;
- S2 Qualified alone is not final or production admission;
- a final binding for another S2 selection is not eligible;
- production activation has no constructible factory in this slice.

## 3. Explicit non-goals

This slice does not implement or authorize:

- an administrative final-record publisher or gate-results/evidence validator;
- protected store provisioning or any real approval, selector, record, or host identity;
- final publication, withdrawal, quarantine, or common authority/install locking;
- full session-bound capability, fixed launcher/authorizer, GAVA purposes, actor/descriptor binding, or a driver;
- a complete production sink map;
- the Q10 pre-RELEASE active-generation reread in source;
- native privilege drop, installed positive, native loader closure, or protected-store native tests;
- S4/B7, production activation, public factory enablement, packages, or push;
- branches, linked worktrees, reset, rebase, stash, clean, prune, or force operations.

## 4. Preservation constraints

- Work directly on canonical `main`.
- Preserve the 44 historical untracked paths and 74 worktree registrations.
- Do not rewrite historical PENDING drafts, independent reviews, reviewer evidence, or prior owner decisions.
- Do not create protected records or host identities on the real host.
- Keep changes consistent with the existing `Installed` and `Restricted` code style.

## 5. Verification plan

Commands are run from the canonical repository on Windows:

```powershell
dotnet build tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --nologo
dotnet test tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --nologo --filter "FullyQualifiedName~FinalAcceptanceTests"
dotnet test tests\Gagamba.Execution.Linux.M1.Tests\Gagamba.Execution.Linux.M1.Tests.csproj -c Debug --no-build --nologo
```

Expected evidence:

- build succeeds with zero warnings and zero errors;
- focused final-acceptance tests pass;
- full M1 suite has zero failures; native skips remain skips and are not counted as passes;
- no source or test file is added to the production closure by the tests;
- `RestrictedExecutionRuntime.Create()` remains unavailable.

## 6. Exit criteria

The slice is complete for independent Q1 when:

1. The exact source and test files are committed on canonical `main`.
2. The focused and full test results are recorded with commands and environment.
3. Every native or protected-store positive is explicitly pending, not inferred.
4. The `platformInventorySha256` mapping to `platform.runtimeInventorySha256` is recorded as an implementation qualification.
5. The false-`QualifiedInstallation` legacy-path behavior is explicitly reviewed.
6. Q10 remains an open non-waiver, not a silent waiver.
7. Preservation of the 44 historical untracked paths and 74 worktrees is rechecked after the commits.
8. No push, activation, publication, privileged operation, or out-of-scope implementation occurs.

The expected disposition is **IMPLEMENTED WITH QUALIFICATIONS — READY FOR INDEPENDENT Q1** unless an actual blocker is found.
