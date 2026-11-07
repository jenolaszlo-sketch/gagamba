# Slice E-1 F-01 remediation entry decision — semantic integrity cross-checks

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-REMEDIATION-ENTRY-01`

**REMEDIATION AUTHORIZED WITH QUALIFICATIONS**

Date: 2026-11-07 (Asia/Manila). Recorder and reviewer: DeepSeek. Scope: documentation and source inspection only, on canonical `main`; no push.

This decision authorizes a narrowly bounded F-01 correction package. It does **not** implement the correction, does **not** close F-01, and does **not** authorize E-2 through E-7, Slice N, or activation. F-01 may be closed only by a later owner decision after implementation and independent qualification.

## 1. Verified baseline

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `9ca765c6af9bcc1665bc80e4c28f1a77d3c8d81d` (matches the commission) |
| Starting tree | `2b90208c308a164d733ecaae716db2e8f6671926` (matches the commission) |
| `origin/main` | `9ca765c6af9bcc1665bc80e4c28f1a77d3c8d81d` (synchronized; `0 0` ahead/behind) |
| Worktrees | exactly one canonical checkout |
| Tracked / index | clean |
| Historical untracked paths | 44, all preserved |
| Push | none |

Baseline matched exactly. No reset, clean, rebase, prune or extra worktree was used.

## 2. Q1 finding reconstruction (F-01)

Independent Q1 (`docs/reviews/rc2b-06-b6-alt-b-slice-e-01-q1.md` §6, §12) demonstrated that `PrepublicationIntegrityProjection.FromEvidence` (`src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs:69–91`) accepts **structurally valid but semantically incorrect** evidence for:

1. `OwnerClosureSha256`,
2. `RecoveryClosureSha256`,
3. `RuntimeInventorySha256`.

The only checks at that boundary are non-null, 64-char lowercase hex (`InstallJson.IsHex`), and `ArtifactSetSha256 == manifest.Digest` (`PrepublicationIntegrityProjection.cs:79–85`). There is no cross-check of the three fields against the manifest. Q1's independent reflection probe recorded owner, recovery and runtime fields as `ACCEPTED` when well-formed but wrong (`q1-evidence/probe-results.txt`, rows B5–B7).

Q1 classified F-01 **LOW / non-blocking** because `FromEvidence` is private, the only RELEASE route is `Verify()` (which uses fixed-root verification), and there is no RELEASE production consumer. E-1 was accepted with F-01 as a **mandatory closure prerequisite** before the first production consumer, any reliance on the projection's semantic integrity for an authorization decision, or any expansion of the trusted construction boundary (`docs/decisions/rc2b-06-b6-alt-b-slice-e-01-owner-acceptance-01.md` §6).

## 3. Authoritative semantic relationship matrix

Each relationship is defined by accepted in-repository source; none is external-only. Notation: `M` = the parsed `InstalledManifest`; `RoleHash` = `InstallationIdentity.RoleHash` (`src/Gagamba.Execution.Linux.M1/Installed/InstallationIdentity.cs:35–45`).

| Projection field | Authoritative value | Origin of authority | Required relationship | Established today? |
|---|---|---|---|---|
| `ArtifactSetSha256` | `M.Digest` | Canonical `manifest.json` bytes; bound to the active generation (`InstalledFileSystem.Prepublication.cs:41` `SameDigest`; `InstalledMetadata.Active`) | Equality | **Yes** (`PrepublicationIntegrityProjection.cs:79–81`) |
| `SourceCommit` / `SourceTree` | `M.SourceCommit` / `M.SourceTree` | `M` | Copy (bound by construction) | **Yes** (read directly from `M`, `:88–89`) |
| `OwnerClosureSha256` | `InstallationIdentity.RoleHash(M, 1)` | `M.Artifacts` owner-contract/deps/entry/m1/runtimeconfig raw hashes | **Equality to recomputation** | **No** |
| `RecoveryClosureSha256` | `InstallationIdentity.RoleHash(M, 0)` | `M.Artifacts` recovery-contract/deps/entry/m1/runtimeconfig raw hashes | **Equality to recomputation** | **No** |
| `RuntimeInventorySha256` | `InstallJson.Hex(M.Platform, "runtimeInventorySha256")` | `M.Platform` | **Equality to the manifest value** | **No** |
| `QualificationId` / `QualificationRecordSha256` | `null` | Contract (prepublication has no association) | Always null | **Yes** (expression-bodied) |

Supporting definitions and corroboration:

- `RoleHash` is the single repository definition of the owner/recovery role-closure identity: `SHA-256( ASCII("gagamba-b6-role-v1\0") || role-byte || owner|recovery-{contract,deps,entry,m1,runtimeconfig} raw hash bytes )` (`InstallationIdentity.cs:38–44`). It is used by `InstallationIdentity.Project` (`:27`) and by the E-1 verifier (`InstalledFileSystem.Prepublication.cs:63`).
- `RuntimeInventorySha256` is the manifest's `platform.runtimeInventorySha256`, also used by `InstallationIdentity.Project` (`InstallationIdentity.cs:30`), `AdmissionAuthority.BindingMatches` (`Installed/AdmissionAuthority.cs:61`), `FinalAcceptance` (`Installed/FinalAcceptance.cs:72`) and `LoadedInstallation.Runtime()` (`Installed/LoadedInstallation.cs:47`).
- The manifest-derived runtime inventory is independently bound to the real host by `InstalledPayload.CheckHost` → `CheckPlatform` (`Installed/InstalledPayload.cs:145–150`), which the E-1 verifier runs (`InstalledFileSystem.Prepublication.cs:55`). Thus `projection.RuntimeInventorySha256 == M.Platform.runtimeInventorySha256 == actual runtime inventory`.
- The E-1 verifier already computes exactly these authoritative values when building the evidence: `new InstalledIntegrityEvidence(digest, manifest, RoleHash(manifest,1), RoleHash(manifest,0), InstallJson.Hex(manifest.Platform,"runtimeInventorySha256"))` (`InstalledFileSystem.Prepublication.cs:62–64`).

The required operations are well-defined: **recompute** the role closures from `M` and **read** the runtime inventory from `M.Platform`, then require **equality** with the evidence fields. `RoleHash` throws (fail-closed) if the manifest lacks a required role artifact, so a malformed or incomplete manifest cannot silently produce a projection.

**No provenance gap:** all relationships are defined in the repository; no external/hash-only design dependency is required for F-01.

## 4. Existing versus missing checks

| Check | Location | State |
|---|---|---|
| Evidence non-null; manifest non-null | `PrepublicationIntegrityProjection.cs:76–78` | Present |
| `ArtifactSetSha256` well-formed and equals `M.Digest` | `:79–81` | Present |
| Closure/runtime fields are 64-lower hex | `:82–85` | Present (shape only) |
| Provenance is a defined enum value | `:86–87` | Present |
| **`OwnerClosureSha256 == RoleHash(M,1)`** | `:82–85` (absent) | **Missing (F-01)** |
| **`RecoveryClosureSha256 == RoleHash(M,0)`** | `:82–85` (absent) | **Missing (F-01)** |
| **`RuntimeInventorySha256 == InstallJson.Hex(M.Platform,"runtimeInventorySha256")`** | `:82–85` (absent) | **Missing (F-01)** |
| Qualification association is null | `:54–56` | Present |

## 5. Proposed bounded remediation

Authorize the smallest change that establishes the missing invariant: add the three semantic equality checks inside the private `FromEvidence` (`PrepublicationIntegrityProjection.cs:69–91`), after the existing structural checks. Recommended shape (implementation detail is the implementer's, subject to §6 and §8):

```csharp
// Owner/recovery closures must be the accepted role-closure projection of the
// authoritative manifest; the runtime inventory must be the manifest's value.
// RoleHash throws when a required role artifact is absent, so this fails closed.
var ownerClosure = InstallationIdentity.RoleHash(manifest, 1);
var recoveryClosure = InstallationIdentity.RoleHash(manifest, 0);
var runtimeInventory = InstallJson.Hex(manifest.Platform, "runtimeInventorySha256");
InstallJson.Require(evidence.OwnerClosureSha256 == ownerClosure &&
    evidence.RecoveryClosureSha256 == recoveryClosure,
    "prepublication role closure mismatch");
InstallJson.Require(evidence.RuntimeInventorySha256 == runtimeInventory,
    "prepublication runtime inventory mismatch");
```

Notes on the "avoid redundant recomputation" instruction (commission §5): the recomputation is **not** redundant verification and is **not** safely reusable from the evidence. The evidence field is the value under test; using it as its own authority would be circular. The accepted recomputation is a bounded, deterministic function of the authoritative manifest (`RoleHash` over five small hashes; one `JsonElement` read). The implementer MAY avoid computing each value twice within the same call by assigning locals, as above.

This preserves the accepted evidence shape (`InstalledIntegrityEvidence`), so the negative matrix in §8 remains constructible. A proposal to instead re-shape `InstalledIntegrityEvidence` to derive-only fields would remove the testable inconsistency surface and change an accepted internal shape; it is **not** authorized by this package unless the implementer demonstrates it is no larger, preserves an equivalent negative-testable surface, and obtains explicit owner approval. Primary authorization is the cross-check above.

## 6. Trust-boundary analysis

The correction must not merely compare two caller-controlled values. It does not:

- **Independently authoritative value:** the manifest `M`. Under the accepted construction, `M` is produced only by `InstalledFileSystem.VerifyActiveIntegrity` (`InstalledFileSystem.Prepublication.cs:21–70`), which reads the root-owned fixed-root stores: `/opt/gagamba/active` → generation directory → canonical `manifest.json`, with the manifest digest bound to `active` (`:41` `SameDigest`) and full payload closure (`Walk`/`EnsureExact`/`CheckDependencies`), protected-store stability (`RecheckSelectors`, `:58–60`), and host/platform binding (`CheckHost`, `:55`).
- **Construction route:** in RELEASE, the only caller of `FromEvidence` is `Verify()` (`PrepublicationIntegrityProjection.cs:65–67`), which passes the verifier-produced `M`. `ForTest` (`:100–101`) is compile-time excluded from RELEASE. Independent Q1 confirmed at the compiled-IL level that no production path calls `Verify()` (`q1-evidence/release-il-callers.txt`; Q1 §9).
- **Relationship:** the projection's closure/runtime fields become deterministic functions of the authoritative `M`. A wrong field is refused; a self-consistent tuple can only be produced when `M` is itself the authoritative selection.
- **Residual:** `FromEvidence` cannot re-derive fixed-root authority on its own; the boundary remains "the private route supplies the authoritative manifest, and the projection fields are bound to it." This matches the accepted E-1 design and does not claim more.

## 7. Authorized files and components

Authorized (minimum surface):

- `src/Gagamba.Execution.Linux.M1/Installed/PrepublicationIntegrityProjection.cs` — add the semantic checks in `FromEvidence` (and, only if justified, one narrowly scoped private helper in the same type).
- `tests/Gagamba.Execution.Linux.M1.Tests/Installed/PrepublicationIntegrityProjectionTests.cs` — the regression matrix in §8.

Explicitly **not** authorized (no change expected; touching them requires a separate decision):

- `Installed/InstalledFileSystem.Prepublication.cs` (the verifier already computes the authoritative values; no change needed).
- `Installed/InstalledFileSystem.cs` and the qualified `VerifyActive` path.
- `Installed/InstallationIdentity.cs`, `Installed/InstalledMetadata.cs`, `Installed/InstalledPayload.cs`, `Installed/LoadedInstallation.cs`.
- Any qualified, bootstrap, lifecycle, release or authority file.
- `eng/VerifyProductionAssembly/Program.cs` (the E-1 seam is already forbidden; no change expected). `eng/test-production-build.ps1` is out of scope (F-03).

## 8. Required positive and negative tests

The later `F-01-IMPL-01` must add focused deterministic tests. Existing valid behavior must keep passing (the current helper `Evidence(manifest)` in the test file already derives the correct values via `RoleHash`/`InstallJson.Hex`, and the existing rows assert them).

| Case | Expected behavior |
|---|---|
| Valid authoritative owner closure | Accepted |
| Structurally valid but incorrect owner closure (well-formed 64-hex ≠ `RoleHash(M,1)`) | Refused |
| Valid authoritative recovery closure | Accepted |
| Structurally valid but incorrect recovery closure | Refused |
| Correct runtime inventory binding | Accepted |
| Well-formed incorrect runtime inventory | Refused |
| Correctly formatted but substituted artifact digest | Refused (existing) |
| Manifest-derived contradiction (e.g., wrong artifact digest plus wrong closures) | Refused |
| Missing required semantic evidence (manifest lacking a required role artifact → `RoleHash` throws) | Refused |
| Mutated evidence after verification | No trusted-state corruption (projection immutable) |
| Attempted qualification promotion (`Qualified`/`LoadedInstallation`/`InstalledResult`) | Refused / impossible |
| Attempted validation-authority promotion (`OwnerAuthorityContext.Validation`) | Refused / impossible |
| RELEASE test-hook inspection | No `ForTest` |
| Production reachability inspection | No new production consumer |

Additional required controls:

- **Positive control:** a correctly derived tuple (test-only route) constructs a projection with `DeterministicTestOnly` provenance and null qualification; the existing determinism/immutability assertions remain.
- **Bypass (gate-removal) negative control:** demonstrate the semantic check is load-bearing — e.g., an external mutated binary copy (outside canonical source) without the check accepts a well-formed-wrong closure, while the committed binary refuses it. Do not modify canonical source to demonstrate this.
- **Q1 reproduction:** the exact Q1 rows B5/B6/B7 (well-formed but wrong owner/recovery/runtime) must now be refused.

`F-01-Q2` must construct **original** semantically incorrect but structurally valid inputs (not a reproduction of producer tests), confirm valid construction still works, and verify RELEASE isolation, production unreachability and P4 neutrality on the exact committed revision. A new qualification report must bind to that revision.

## 9. RELEASE and P4 preservation requirements

- **RELEASE separation:** `ForTest` must remain absent from RELEASE; the Release production-assembly verifier must still PASS; no new Debug-only factory or public API.
- **No trust promotion (I-03), no new reachability (I-04), immutable projection (I-05):** unchanged.
- **P4 neutrality (I-07):** the change is confined to the private `FromEvidence` of a production-unreachable type; assess and record: no new protected-sink caller; F01/F02 enforcement unchanged; S10/S11 caller domination unchanged; S28 test-only unchanged; no new qualified→legacy downgrade path; existing RELEASE refusal unchanged; first protected-effect ordering unchanged. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and the 331 unqualified native skips remain preserved.
- **No schema invention (I-08), no activation (I-09):** no new manifest/identity semantics; no positive validation, atomic bootstrap, native protected execution or activation.

## 10. F-02 and F-03 separation

- **F-02 (duplicated verification logic):** the correction is confined to the projection type and does not modify `VerifyActiveIntegrity` or `VerifyActive`, so it does not expand the duplication. F-02 remains a **tracked obligation**: if the implementer finds a shared helper is unavoidable, the proposal must state the minimum affected surface, existing guarantees, parity tests and regression risks, and requires separate authorization. F-01 adding checks does **not** resolve F-02.
- **F-03 (PowerShell 5.1 harness defect):** unrelated to E-1 semantic integrity; **not** authorized here. Implementation and qualification may use independently verified assembly checks while accurately reporting that the full `eng/test-production-build.ps1` harness remains unqualified. F-03 stays separately tracked.

## 11. Explicit exclusions

This decision does not authorize: E-2 through E-7; validation-catalog approval; GAVA1; v3-pre; K/M/F/S protected roots; root authorizer or fixed launcher; privilege drop or custody; DROP-COMMIT; atomic R/O/C ordering; any validation-authority factory; positive validation; Slice N; new public APIs; production activation; `eng/test-production-build.ps1` changes; or any change to `VerifyActive`, `InstallationIdentity`, or the qualified installed-integrity subsystem.

## 12. Implementation and qualification sequence

| Stage | Scope | Disposition authority |
|---|---|---|
| `F-01-IMPL-01` | Implement only the §5/§7 semantic checks and the §8 focused tests | Producer: `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION` / `IMPLEMENTED WITH RESTRICTIONS — QUALIFICATION REQUIRED` / `IMPLEMENTATION BLOCKED`; no owner acceptance |
| `F-01-Q2` | Independent review: exact semantics, Q1 closure, original adversarial probes, valid construction, RELEASE isolation, production unreachability, P4 neutrality, regression results | Reviewer disposition bound to the exact implementation revision |
| `F-01-OWNER-ACCEPTANCE` | Owner evaluates implementation + independent qualification | Only this may formally close F-01 |

F-01 is **not** declared closed by this decision.

## 13. Formal decision

**REMEDIATION AUTHORIZED WITH QUALIFICATIONS.** The accepted contracts support a bounded F-01 semantic correction: the authoritative relationships (`RoleHash` and `platform.runtimeInventorySha256`) are defined in-repository, the trust boundary is the fixed-root verifier's manifest, and the correction can be specified and tested against authoritative evidence without touching the qualified path or expanding reachability. Authorization is limited to §5, §7, §8, §9, §10 and the invariants I-01–I-09.

## 14. Recommended next commission

`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-IMPL-01` — implement the bounded semantic cross-checks and focused regressions only, then hand off for `F-01-Q2` and a later `F-01-OWNER-ACCEPTANCE`. No implementation, remediation, E-2, Slice N or activation starts automatically.
