# Slice E-1 F-01 implementation evidence — manifest-derived semantic integrity checks

- Date: 2026-11-07 (Asia/Manila)
- Remediation slice: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-IMPL-01`
- Baseline source (starting HEAD): `9a8007fb923174f00dd5954ab47b8fdf4866ebba`, tree `b465805f7d6e50e6f4d59f734cae008cc02be1c8`
- Final source (implementation commit): `965f203eea9b4f0f6df41e2c4979eb5b4a585d3f`, tree `7edfedd6badebda029ce1e3371445d973c6fd82b`
- Source subtree (`src`): `7e914b4a38a9238799a9576904677302a10c4fad`; test subtree (`tests`): `94f441ea4fa5b9831bcf951ec9d486d846ed6b6b`
- Branch: `main` (one canonical worktree); `origin/main` `53850aa` (unrelated concurrent maintenance commit preserved); no push
- Host OS/version: Microsoft Windows NT 10.0.26200.0; Architecture: AMD64 (win-x64); .NET SDK 10.0.401
- Relevant native prerequisites: protected root-owned WSL2/Linux fixed-root installation — **not provisioned / not exercised**; native prepublication success remains `NATIVE-PENDING` (Slice N)
- Qualification scope: source implementation; managed deterministic tests; Debug/RELEASE build identity; RELEASE production-assembly refusal checks; disposable gate-removal control
- Explicit exclusions: E-2 through E-7; F-02 consolidation; F-03 tooling; K/M/F/S; GAVA1; v3-pre; root authorizer/launcher; custody/privilege drop; R/O/C ordering; validation-authority factory; positive validation; Slice N; public API; activation

## 1. Claim

The E-1 prepublication integrity projection's derived fields are now bound to deterministic functions of the authoritative manifest. A structurally valid but semantically incorrect owner closure, recovery closure or runtime inventory is refused. The projection remains refusal-only, production-unreachable, and cannot construct any authority. F-01 is **not** closed by this evidence; a separate Q2 and owner acceptance are required.

## 2. Authoritative semantic contracts

| Relationship | Authoritative value | Source location |
|---|---|---|
| Owner closure | `InstallationIdentity.RoleHash(manifest, 1)` | `src/Gagamba.Execution.Linux.M1/Installed/InstallationIdentity.cs:35–45` (single definition; also `Project:27`) |
| Recovery closure | `InstallationIdentity.RoleHash(manifest, 0)` | same |
| Runtime inventory | `InstallJson.Hex(manifest.Platform, "runtimeInventorySha256")` | `InstallationIdentity.cs:30`, `AdmissionAuthority.cs:61`, `FinalAcceptance.cs:72`; host-bound by `InstalledPayload.cs:145–150` |
| Artifact set | `manifest.Digest` | already enforced, `PrepublicationIntegrityProjection.cs` (`ArtifactSetSha256 == manifest.Digest`) |

`RoleHash` is `SHA-256("gagamba-b6-role-v1\0" ‖ role-byte ‖ owner|recovery-{contract,deps,entry,m1,runtimeconfig} raw hashes)` and throws when a required role artifact is absent. The manifest is authoritative because, in RELEASE, it is supplied only by the fixed-root verifier through `Verify()`.

## 3. Exact implementation delta

Two files, +77 lines (no other production/test change):

- `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs` (+14): in the private `FromEvidence`, after the existing structural checks:
  - `InstallJson.Require(evidence.OwnerClosureSha256 == InstallationIdentity.RoleHash(manifest, 1) && evidence.RecoveryClosureSha256 == InstallationIdentity.RoleHash(manifest, 0), "prepublication role closure mismatch");`
  - `InstallJson.Require(evidence.RuntimeInventorySha256 == InstallJson.Hex(manifest.Platform, "runtimeInventorySha256"), "prepublication runtime inventory mismatch");`
- `tests/Gagamba.Execution.Linux.M1.Tests/Installed/PrepublicationIntegrityProjectionTests.cs` (+63): positive control, the Q1 B5/B6/B7 reproduction theory, and two fail-closed cases.

Untouched: `VerifyActive`, `VerifyActiveIntegrity`, `InstalledFileSystem*.cs`, `InstallationIdentity.cs`, `InstalledIntegrityEvidence`, `eng/test-production-build.ps1`, and every qualified authority/execution path.

## 4. Construction/trust-boundary preservation

`Verify()` → `VerifyActiveIntegrity()` → `FromEvidence` unchanged. In RELEASE the only `FromEvidence` caller remains `Verify()` (IL-confirmed); `ForTest` remains Debug-only and absent from RELEASE. The new checks bind the projection fields to the authoritative manifest rather than comparing two caller values. No new construction route; private constructor, internal visibility, immutability, null qualification association and provenance classifications preserved.

## 5. Positive and negative test matrix (all newly executed)

| Case | Expected | Observed |
|---|---|---|
| Fully consistent evidence (positive) | Accepted | `SemanticGateAcceptsFullyConsistentEvidence` PASS |
| B5: well-formed incorrect owner closure | Refused | `InvalidDataException` (theory row) |
| B6: well-formed incorrect recovery closure | Refused | `InvalidDataException` |
| B7: well-formed incorrect runtime inventory | Refused | `InvalidDataException` |
| Substituted/short/uppercase artifact digest | Refused | existing `StructuralGate…` rows PASS |
| Non-hex closure / undefined provenance / null manifest / null evidence | Refused | existing rows PASS |
| Manifest missing required role material | Refused | `InvalidOperationException` (`RoleHash` `.Single`) |
| Manifest missing runtime-inventory metadata | Refused | `KeyNotFoundException` (`JsonElement.GetProperty`) |
| Mutation after construction / authority promotion / RELEASE seam | Immutable / refused / absent | existing immutability, promotion and RELEASE rows PASS |

`RoleHash`/`InstallJson.Hex` are the established helper failure behaviors; no broad exception handling was added.

## 6. Q1 F-01 reproduction and corrected outcomes

The independent Q1 reflection probe (`docs/reviews/rc2b-06-b6-alt-b-slice-e-01-q1-evidence/probe/`) was re-run against the committed Debug assembly. Q1 recorded B5/B6/B7 as `ACCEPTED`; they are now `refused`:

```
=== COMMITTED candidate (post-fix) ===
B1.consistent-evidence-builds   PASS     projection
B5.wrong-but-wellformed-owner-closure       refused
B6.wrong-but-wellformed-recovery-closure    refused
B7.wrong-but-wellformed-runtime-inventory   refused
```

(The probe's trailing detail string is the original Q1 static text; the observed value is the evidence.)

## 7. Gate-removal negative control (disposable, external)

A scratch copy of `src/`, `Directory.Build.props`, `Directory.Build.targets` and `eng/VerifyProductionAssembly` was built outside the repository with the two semantic `Require` statements removed from `FromEvidence` (canonical source and Git history untouched). The same Q1 probe was run against both Debug assemblies:

```
=== COMMITTED candidate ===        B5 refused  B6 refused  B7 refused
=== MUTATED (gate removed) ===     B5 ACCEPTED B6 ACCEPTED B7 ACCEPTED
```

All other rows (B1–B4, B8–B11) were identical in both. Mutated Debug M1 SHA-256 `7545A67CE0A2C102BE542711C897CBA83444CACD4AEC53DCA9E2EB44265ADDB7`; the committed (restored) candidate is the committed binary in §8 and refuses all three. This demonstrates the semantic gate is load-bearing.

## 8. Debug/RELEASE verification

| Check | Result |
|---|---|
| Debug M1 build | 0 warnings / 0 errors (Debug M1 SHA-256 `1E9AE7C38CE0D5414C6FFD54CEE3EFCC01C88C77BCE9CE30A2F5C9579EFAC14C`) |
| RELEASE M1 build | 0 warnings / 0 errors (Release M1 SHA-256 `142E47BCCD3EE179EFB1497CD9CB47B4A5F6F5598FB3DAF44AF7BDEF76FFFF9D`) |
| RELEASE production-assembly verifier | **PASS** (0/4 hook types, 0/3 hook properties, 14/14 core types, no forbidden factories) |
| Debug negative control (`--production` on Debug) | FAIL as expected, naming `PrepublicationIntegrityProjection::ForTest` |
| Production reachability (RELEASE IL) | only E-1 type bodies reference the E-1 types; `FromEvidence` called only by `Verify`; `Verify` has no caller; `ForTest` present 0× |

## 9. P4 neutrality

`git diff` is confined to the two authorized files; no `Lifecycle/`, `Bootstrap/`, `Restricted/`, `Execution/`, `InstalledFileSystem*.cs` or sink change. No new production caller/sink, no first-protected-effect change, no RELEASE-refusal change, no new downgrade path. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and the 331 unqualified native skips preserved.

## 10. Test counts (newly executed)

| Suite | Result |
|---|---|
| E-1 focused (`PrepublicationIntegrityProjectionTests`) | **23 passed / 0 failed / 0 skipped** (was 17; +6) |
| Integrity/authority/release filter | **238 passed / 32 skipped / 0 failed** (was 232) |
| Full Windows M1 (`Gagamba.Execution.Linux.M1.Tests`) | **1391 passed / 331 skipped / 0 failed** (1722) (was 1385) |

The 331 native skips are unqualified and are **not** passes.

## 11. F-02 / F-03 preservation

- **F-02:** not consolidated; `VerifyActive`/`VerifyActiveIntegrity` untouched; the correction is confined to the projection type. F-02 remains an open tracked obligation, not resolved by these checks.
- **F-03:** `eng/test-production-build.ps1` unchanged; the previously demonstrated PowerShell 5.1 encoding defect remains. The full script was **not** run/claimed; assembly-verifier evidence (§8) stands independently.

## 12. Remaining qualifications

- Native protected `Verify()` success and all protected positives remain `NATIVE-PENDING` (Slice N).
- The runtime loaded-assembly ("verification-to-use") role check remains deferred (later authenticated-session packages).
- F-02 (duplicated verification) and F-03 (harness) remain open.
- F-01 is **not** closed by this implementation; only a subsequent Q2 plus owner acceptance may close it.

## 13. Independent Q2 handoff

See `docs/reviews/rc2b-06-b6-alt-b-slice-e-01-f01-impl-01.md`.

## 14. Source binding

Baseline `9a8007f…` (tree `b465805f…`) is the exact starting revision; the implementation commit `965f203…` (tree `7edfedd6…`) is the exact source this evidence proves. Working tree at evidence time: only the documentation being added; 44 historical untracked paths preserved; no push.
