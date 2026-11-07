# Slice E-1 F-01 independent Q2 qualification report

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-Q2`
Date: 2026-11-07 (Asia/Manila). Reviewer: DeepSeek (independent session). Scope: independent requalification of the F-01 semantic integrity remediation; documentation and reviewer evidence only.

**Disposition: SUPPORTED WITH QUALIFICATIONS.**

The three previously demonstrated semantic integrity gaps (Q1 B5/B6/B7) are independently closed on the committed revision: `FromEvidence` now refuses well-formed but semantically incorrect owner closures, recovery closures and runtime inventory, binds each field to the correct role discriminator, and remains production-unreachable and RELEASE-isolated. Bounded residuals (native-pending; F-02/F-03 open; a non-`InvalidDataException` fail-closed variance for malformed manifests) are preserved. This is evidence for a subsequent owner decision, **not** owner acceptance; F-01 is **not** formally closed here.

Reviewer evidence: `docs/reviews/rc2b-06-b6-alt-b-slice-e-01-f01-q2-evidence/`.

## 1. Verified baseline and reviewed implementation

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD / tree | `db39ff6a61007811e8f874b477d35ceda6bfb356` / `7517f050b174799dd0c1af0d973848524df88ed3` (matches) |
| Reviewed implementation | `965f203eea9b4f0f6df41e2c4979eb5b4a585d3f` / tree `7edfedd6badebda029ce1e3371445d973c6fd82b` |
| `origin/main` | `53850aa158cf26f5a40fb9bd51c606a111188ef3` (local 3 ahead; **remote has not advanced independently**) |
| Worktrees / tracked-index | exactly one; clean |
| Historical untracked paths | 44 preserved |
| Push | none |

`src`/`tests` are unchanged from `965f203` to `db39ff6` (docs-only commits since), so the reviewed implementation is exactly the committed source.

## 2. Semantic-contract reconstruction

- **Authoritative manifest origin.** In RELEASE, `PrepublicationIntegrityProjection.Verify()` calls `InstalledFileSystem.VerifyActiveIntegrity()` (`Installed/InstalledFileSystem.Prepublication.cs:21–70`), which reads the root-owned fixed-root stores (`/opt/gagamba/active` → generation → canonical `manifest.json`), binds the manifest digest to `active` (`SameDigest`, `:41`), enforces the exact payload closure (`Walk`/`EnsureExact`/`CheckDependencies`), stability (`RecheckSelectors`, `:58–60`) and the host/platform baseline (`CheckHost`, `:55`). `Verify()` alone supplies that manifest to `FromEvidence` (`PrepublicationIntegrityProjection.cs:65–67`).
- **Owner/recovery role hashes.** `InstallationIdentity.RoleHash(manifest, role)` (`Installation/InstallationIdentity.cs:35–45`) is the single repository definition: `SHA-256("gagamba-b6-role-v1\0" ‖ role-byte ‖ owner|recovery-{contract,deps,entry,m1,runtimeconfig} raw hashes)`; `role==1` is owner, `role==0` is recovery; it throws when a required role artifact is absent.
- **Runtime inventory.** `InstallJson.Hex(manifest.Platform, "runtimeInventorySha256")`; the manifest value is also host-bound by `InstalledPayload.CheckHost` (`Installed/InstalledPayload.cs:145–150`).
- **New comparisons** (`PrepublicationIntegrityProjection.cs`, private `FromEvidence`, at `965f203`):
  - `OwnerClosureSha256 == InstallationIdentity.RoleHash(manifest, 1) && RecoveryClosureSha256 == InstallationIdentity.RoleHash(manifest, 0)`
  - `RuntimeInventorySha256 == InstallJson.Hex(manifest.Platform, "runtimeInventorySha256")`
- **Coverage / failure path.** Both construction routes flow through `FromEvidence`: `Verify()` (RELEASE) and `ForTest()` (Debug-only). A failed comparison throws `InvalidDataException` via `InstallJson.Require`, so trusted construction is prevented; there is no catch around these checks in `FromEvidence` (the verifier's catch in `InstalledFileSystem.Prepublication.cs:66–69` wraps only platform-primitive exceptions from `VerifyActiveIntegrity`, not `FromEvidence`).
- **Comparison basis.** The checks compare projection fields against deterministic functions of the manifest. In RELEASE the manifest is independently established by the fixed-root verifier; `FromEvidence` itself does not re-derive that authority (recorded boundary, consistent with F-01 entry §6).

## 3. Original independent adversarial probe matrix

Independently authored reflection probe (`...q2-evidence/probe/Program.cs`), run against the committed Debug assembly with the compiled RELEASE assembly for the release cases. Observed output: `probe-results-committed.txt` (`SUMMARY failures=0`).

| Probe | Case | Expected | Observed |
|---|---|---|---|
| P1 | Valid owner/recovery/runtime evidence | Accepted | Accepted |
| P2 | Well-formed incorrect owner closure (B5) | Refused | `InvalidDataException` |
| P3 | Well-formed incorrect recovery closure (B6) | Refused | `InvalidDataException` |
| P4 | Well-formed incorrect runtime inventory (B7) | Refused | `InvalidDataException` |
| P5 | owner RoleHash ≠ recovery RoleHash | Distinct | distinct |
| P5b | **Owner/recovery closures swapped** | Refused | `InvalidDataException` |
| P6 | **Owner-entry artifact substituted** changes the owner RoleHash | Changed | changed |
| P6b | Substituted role artifact with the old owner closure | Refused | `InvalidDataException` |
| P6c | Consistent tuple for the substituted manifest | Accepted | builds |
| P7 | Correctly formatted substituted artifact digest | Refused | `InvalidDataException` |
| P8 | Manifest missing required role material | Fail closed | `InvalidOperationException` |
| P9 | Manifest missing runtime-inventory metadata | Fail closed | `KeyNotFoundException` |
| P10 | Contradictory manifest and evidence | Refused | `InvalidDataException` |
| P11 | Mutation after construction | Immutable | get-only properties |
| P12 | Qualification association | Null | null |
| P13 | Attempted qualification/validation-authority promotion | Refused | no authority-typed member |
| P14–P16 | Private ctor / private `FromEvidence` / no caller-fed factory | Preserved | PASS |
| R1–R4 | RELEASE: no `ForTest`, `Verify` present, private ctors, internal | Preserved | PASS |

P5b and P6b are the strongest independent checks: they show the comparisons bind the **correct role discriminator** and the **manifest's actual role material**, not merely "some hash matches". No probe foreclosed the others via an incidental format failure (P6c and P1 are accepted controls in the same run).

## 4. Q1 B5/B6/B7 requalification

The retained Q1 probe (`...q1-evidence/probe/`) run against the committed Debug assembly (`q1probe-results-committed.txt`):

```
B5.wrong-but-wellformed-owner-closure       refused
B6.wrong-but-wellformed-recovery-closure    refused
B7.wrong-but-wellformed-runtime-inventory   refused
```

This is a semantic-gate refusal (`FromEvidence`'s equality checks), corroborated by my probe P2/P3/P4, not a parsing/setup failure (the same rig accepts consistent evidence in P1/P6c). Q1 originally recorded B5/B6/B7 as `ACCEPTED`.

## 5. Gate-removal negative control

Independently performed with my own disposable mutation:

- Fresh external scratch (`src`, `Directory.Build.props`, `Directory.Build.targets`, `eng/VerifyProductionAssembly` copied; canonical source/history untouched) with the two semantic `Require` statements deleted from `FromEvidence`; Debug M1 built (SHA-256 `76419C5E918570282F232B8CAC1DB3572897D1948034E8C915E32F88DC6ECA2E`).
- My probe against the gate-removed build: `probe-results-gateremoved.txt` — semantic cases no longer refuse (`failures=7`: P2/P3/P4/P5b/P6b/P8/P9), while P1/P6c still build.
- Retained Q1 probe against the gate-removed build: `q1probe-results-gateremoved.txt` — B5/B6/B7 `ACCEPTED`.
- Committed candidate: my probe `failures=0`; Q1 probe B5/B6/B7 `refused`.

Removing the specific comparisons changes the outcome from refuse to accept; restoring the committed source restores refusal. The gate is load-bearing. The producer's equivalent control is corroborated.

## 6. Exception and refusal assessment

| Input | Exception | Prevents construction? |
|---|---|---|
| Incorrect owner/recovery closure | `InvalidDataException` (`InstallJson.Require`) | Yes |
| Incorrect runtime inventory | `InvalidDataException` | Yes |
| Manifest missing a required role artifact | `InvalidOperationException` (`RoleHash` `.Single`) | Yes |
| Manifest missing runtime-inventory metadata | `KeyNotFoundException` (`JsonElement.GetProperty`) | Yes |

- All four prevent construction; none is caught in the E-1 chain; there is **no** production caller of `Verify()`/`FromEvidence` to catch or downgrade them.
- The accepted E-1 contract does not mandate a single exception type; the two non-`InvalidDataException` cases arise from established accepted helpers (`RoleHash`, `InstallJson.Hex`), and the F-01 entry decision explicitly permitted preserving their established failure behavior rather than adding broad exception handling.
- **Classification: future integration obligation (non-blocking).** A future RELEASE consumer must fail closed on *any* exception, and the eventual refusal boundary may normalize to `InvalidDataException`; this is not required for the current refusal-only, production-unreachable scope and is not demanded here for stylistic reasons. No caller catches only a subset today.

## 7. RELEASE inspection

- `eng/VerifyProductionAssembly --production <Release.dll>` → **RESULT: PASS** (0/4 hook types, 0/3 hook properties, 14/14 core types, no forbidden factories); Release M1 SHA-256 `142E47BCCD3EE179EFB1497CD9CB47B4A5F6F5598FB3DAF44AF7BDEF76FFFF9D`.
- `--production` on the Debug assembly → **FAIL** as expected, naming `PrepublicationIntegrityProjection::ForTest`.
- My probe R1–R4 on the compiled RELEASE assembly: no `ForTest`, `Verify` present, all constructors private, type internal.
- No new public API; `FromEvidence` remains private; `QualificationId`/`QualificationRecordSha256` remain `null`.

## 8. Production-reachability analysis

Compiled RELEASE IL (`release-il-callers.txt`): the only methods referencing the E-1 types outside the E-1 type are `InstalledFileSystem::VerifyActiveIntegrity` (evidence construction) and the `InstalledIntegrityEvidence` record members; the only caller of `VerifyActiveIntegrity` is `PrepublicationIntegrityProjection::Verify`; `FromEvidence` is called only by `Verify`; **`Verify` has no caller**; `ForTest` is **absent** (0 references). Source audit agrees: no production consumer, no validation-authority factory, no new protected effect. E-1 remains production-unreachable.

## 9. P4 neutrality

`git diff 9a8007f db39ff6 -- . ':!docs'` is exactly two files (`PrepublicationIntegrityProjection.cs` +14, `PrepublicationIntegrityProjectionTests.cs` +63). No `Lifecycle/`, `Bootstrap/`, `Restricted/`, `Execution/`, `InstalledFileSystem*.cs` change, so F01/F02 enforcement, S10/S11 domination, S28 reachability, B3 constructor authority, provider gates, first protected effects, qualified→legacy downgrade refusal, RELEASE refusal and R/O/C ordering are unchanged. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING` and the 331 unqualified native skips are preserved.

## 10. Build and test results (independently executed)

| Check | Result |
|---|---|
| Debug M1 build | 0 warnings / 0 errors |
| RELEASE M1 build | 0 warnings / 0 errors |
| E-1 tests (`PrepublicationIntegrityProjectionTests`) | **23 passed / 0 failed / 0 skipped** |
| Integrity/authority/release filter | **238 passed / 32 skipped / 0 failed** |
| Full Windows M1 suite | **1391 passed / 331 skipped / 0 failed** (1722) |
| RELEASE production-assembly verifier | PASS |
| Debug negative control | Expected rejection (names the E-1 seam) |
| Independent probe (committed) | `SUMMARY failures=0` |
| Independent probe (my gate-removed) | `failures=7` (semantic cases accepted) |
| Retained Q1 probe (committed / gate-removed) | B5/B6/B7 refused / accepted |

The 331 native skips are unqualified and are **not** passes. `eng/test-production-build.ps1` (F-03) was not run to completion and is not claimed as passing; the standalone assembly verifier is the RELEASE evidence.

## 11. Findings and residual qualifications

- **Q2-F01 (future integration obligation, non-blocking).** Non-`InvalidDataException` fail-closed types (`InvalidOperationException`, `KeyNotFoundException`) for malformed manifests; sound today (no consumer, construction prevented), but a future RELEASE consumer should treat any exception as refusal and may normalize the boundary.
- **Q2-F02 (retained obligation).** F-02 (`VerifyActive`/`VerifyActiveIntegrity` duplication) is not expanded or altered by F-01; remains tracked.
- **Q2-F03 (retained obligation).** F-03 (`eng/test-production-build.ps1` PowerShell 5.1 defect) remains open; unchanged.
- **Native / Slice N.** Protected `Verify()` success and all protected positives remain `NATIVE-PENDING`.
- **Threat model.** Findings rely on reflection into `internal`/`private` members, which is outside the accepted E-1 threat model (ordinary callers and untrusted evidence); no bypass is claimed.

No demonstrated semantic bypass, unauthorized trust promotion, or failed required invariant was found.

## 12. Formal Q2 disposition

**SUPPORTED WITH QUALIFICATIONS.** The F-01 semantic remediation independently satisfies its required invariants: the three relationships are authoritative and correctly implemented with the correct role discriminators; Q1 B5/B6/B7 now refuse; the gate is load-bearing; construction remains fail-closed, immutable, production-unreachable and RELEASE-isolated; P4 is neutral. Residuals (Q2-F01–F-03, native-pending) are bounded and preserved.

## 13. Recommendation

Recommend the separate owner decision `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-OWNER-ACCEPTANCE-01` to evaluate this implementation and Q2 evidence; only that decision may formally close F-01. Do not begin E-2, Slice N, or any production integration automatically; F-01 remains open until owner acceptance.
