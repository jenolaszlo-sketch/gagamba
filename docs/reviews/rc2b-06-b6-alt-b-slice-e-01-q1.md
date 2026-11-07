# Slice E-1 independent Q1 qualification report

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-Q1`
Date: 2026-11-07 (Asia/Manila). Reviewer: DeepSeek (independent session). Scope: independent qualification of the completed E-1 implementation; documentation and reviewer evidence only.

**Disposition: SUPPORTED WITH QUALIFICATIONS.**

The authorized E-1 refusal-only scope is independently satisfied: the projection is a distinct internal immutable type, has a single verified production construction route, cannot be forged or promoted through ordinary caller inputs, is production-unreachable at the compiled-IL level, and is P4-neutral. One non-blocking internal cross-check gap and one non-blocking harness defect are recorded. No blocking defect, unauthorized path, or failed required invariant was demonstrated.

Reviewer evidence: `docs/reviews/rc2b-06-b6-alt-b-slice-e-01-q1-evidence/` (independent reflection probe and output, PowerShell reproduction, IL caller extraction).

---

## 1. Verified baseline and reviewed source revision

| Item | Value |
|---|---|
| Repository / branch | `C:\Users\Laszlos\source\repos\Gagamba`, `main` |
| Starting HEAD | `61253e9f099637db7796c364be5c985772c6594a` (matches the commission) |
| Starting tree | `c0d5ed50d01cc831891e33f0fd00f1168e8ce821` (matches the commission) |
| Implementation commit reviewed | `2259fce59b44b6af6448a38685a0635d560d0900`, tree `77610b49ecae5398d633f1db43d4f01604694ee1` (parent `c7b7621`) |
| Worktrees | exactly one canonical checkout |
| Tracked / index | clean |
| Historical untracked paths | 44 (unchanged) |
| Host | Microsoft Windows NT 10.0.26200.0, AMD64; .NET SDK 10.0.401 |
| Push | none |

Baseline matched exactly. No reset, clean, rebase, prune or extra worktree was used.

## 2. Authority and contract reconstruction

The binding authorization is `docs/decisions/rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md` (SLICE E ENTRY ACCEPTED WITH QUALIFICATIONS — E-1 IMPLEMENTATION AUTHORIZED), read with the entry review, the Alternative B owner decision §3.3/§3.5, Mechanism B acceptance, F02 trust-store decision, the installed-artifact design, and the accepted P4 coverage decision.

E-1's authorized contract (owner decision §4–§5): a distinct immutable prepublication integrity result bound to the actual protected active selection and full fixed-root manifest/payload, platform/host and loaded-role checks; null qualification ID and null record hash; one narrowly controlled internal construction route after all required checks; no production consumer; no caller-provided digest/identity/manifest/`InstalledResult`/Boolean/claimed result/injectable verifier as trust; Debug seam compile-excluded from RELEASE; P4 preservation. The owner decision also anticipates that if the full construction boundary cannot be established, the implementation must remain inert or fail closed and be reported as partial (Q-E3 atomic enablement; Q-E4 ordering; Q-E5 P4 re-audit).

## 3. Independent construction-boundary analysis (Q1-A)

Complete production delta from the parent (`git show 2259fce --stat`): `eng/VerifyProductionAssembly/Program.cs` (+4), new `Installed/InstalledFileSystem.Prepublication.cs` (71), new `Installed/PrepublicationIntegrityProjection.cs` (121), new tests (218). No other production/test change.

Chain reconstructed from source and the compiled assembly:

1. **Raw FS inputs** — `/opt/gagamba/active`, `installations/<digest>/manifest.json`, `installations/<digest>/install-receipt.json`, the exact generation payload set; read descriptor-relatively by the shared private primitives.
2. **Integrity verification** — `InstalledFileSystem.VerifyActiveIntegrity()` (new partial) performs `EnsureLinuxX64`, trusted-ancestor root open, `Active`, generation open, `InstalledManifest.Parse`, `SameDigest`, `Receipt`, `CreateScan`+`Walk`+`EnsureExact`+`CheckDependencies`, `CheckHost`, and `RecheckSelectors`.
3. **Evidence construction** — returns `InstalledIntegrityEvidence(digest, manifest, RoleHash(manifest,1), RoleHash(manifest,0), platform.runtimeInventorySha256)`.
4. **`Verify()`** — `FromEvidence(VerifyActiveIntegrity(), ProtectedActiveSelection)`.
5. **Private `FromEvidence`** — null/shape/provenance checks then the private constructor.
6. **Immutable projection** — sealed, get-only properties; `QualificationId`/`QualificationRecordSha256` expression-bodied `null`.
7. **Consumers/conversions** — none (see §9).

Independent reflection probe results (full output in the evidence dir, `probe-results.txt`):

| Check | Result |
|---|---|
| A1 single private constructor | PASS (`ctors=1`) |
| A2 caller-accessible factories | `Verify()` and (Debug) `ForTest(InstalledIntegrityEvidence)`; private `FromEvidence(InstalledIntegrityEvidence, Provenance)` is not caller-accessible |
| A2b no bool/`InstalledResult` factory | PASS |
| A2c internal, not public API | PASS |
| A3 all properties read-only | PASS |
| A4 evidence record visibility | constructor flags `Public` on an **internal** type (effectively assembly-internal) |

Conclusion Q1-A: the construction boundary is genuine at the RELEASE level. The only RELEASE route is `Verify()`, which re-enters the accepted verifier; the private constructor prevents external construction; no caller-supplied value manufactures a `ProtectedActiveSelection` projection. The Debug `ForTest` seam is present only in Debug and is permanently tagged `DeterministicTestOnly`.

## 4. Verification-parity assessment (Q1-B)

`VerifyActiveIntegrity()` is a self-contained mirror of `VerifyActive()`'s fixed-root read sequence in the same `partial` class, reusing the identical private primitives. Step-by-step comparison against `InstalledFileSystem.VerifyActive`:

- Identical: `EnsureLinuxX64`; trusted-ancestor `OpenAbsoluteDirectory(InstallRoot, missingUnavailable)`; `RequireDirectory`; `installations`/`qualifications` opens; `active` read; `InstalledMetadata.Active`; generation open; `manifest.json` read; `InstalledManifest.Parse`; `SameDigest`; `install-receipt.json` read; `InstalledMetadata.Receipt`; `CreateScan`; metadata artifacts; `Walk`; `EnsureExact`; `CheckDependencies`; `RecheckSelectors` (with `recordName: null`).
- **Stronger:** `CheckHost` runs unconditionally. `VerifyActive`'s fresh/`InstalledUnqualified` branch returns **before** `CheckHost`; the prepublication path requires the full host baseline.
- **Intentionally absent:** the qualification-record association (`SelectedRecordName` + `AcceptRecord`) and the qualified `RecheckSelectors(recordName, record)` step. This is consistent with E-1's contract (prepublication integrity carries a **null** qualification association); the qualified path remains byte-identical and untouched.
- **Failure classification:** `VerifyActiveIntegrity` normalizes `NotSupportedException`/`DllNotFoundException`/`EntryPointNotFoundException`/`BadImageFormatException` to `InvalidDataException`; other refusal exceptions (`InvalidDataException`, `IOException`, …) propagate unchanged. All fail closed; the type is not always normalized to `InvalidDataException` (Observation, §12-O3).
- **Role closure vs runtime loaded-role:** the projection binds the **manifest-derived** role-closure identity (`InstallationIdentity.RoleHash`). The Gate C runtime "verification-to-use" loaded-assembly check is **not** performed. These are not equivalent; the runtime check belongs to the later authenticated session and remains `NATIVE-PENDING`/deferred (consistent with the owner decision and entry review).

No weaker security check and no new refusal gap was found in the shared prefix. The only residual is duplication (Finding F-02).

## 5. Identity and provenance assessment (Q1-C)

- **Immutability:** all projection properties are get-only; the type is sealed; the projection retains only `string`/enum values (no manifest reference, so no aliasing of the mutable parsed manifest). Probe A3 PASS.
- **Null qualification:** `QualificationId`/`QualificationRecordSha256` are always `null` on every construction path (probe C2 for `ForTest`, C4 for the verified route). No code path infers or substitutes a qualification association.
- **Provenance:** `Verify()` hard-codes `ProtectedActiveSelection`; `ForTest` hard-codes `DeterministicTestOnly` (probe C3/C1). An undefined enum value is refused (probe B9). Test data is therefore distinguishable from protected provenance.
- **Promotion:** no member returns or accepts `OwnerAuthorityContext(.Validation/.Qualified/.Legacy)`, `LoadedInstallation`, `InstalledResult`, `FinalAdmissionVerifier.Grant`, `ProductionActivationAuthorization`, or `bool`; the projection cannot satisfy or become any owner authority context. `OwnerAuthorityContext.Validation` remains factory-less in RELEASE (§7).

## 6. Adversarial probe matrix (Q1-D)

Independent probe (`probe/Program.cs`), not a reproduction of the 17 producer tests. All rows observed on the Windows host against the compiled Debug assembly unless noted.

| ID | Initial state | Malformed/attacker input | Operation | Expected | Observed |
|---|---|---|---|---|---|
| B1 | valid manifest + evidence | none (control) | private `FromEvidence` | projection | projection (PASS) |
| B2 | same | artifact digest ≠ manifest digest | `FromEvidence` | refuse | `InvalidDataException` |
| B3 | same | 63-hex digest | `FromEvidence` | refuse | `InvalidDataException` |
| B4 | same | uppercase digest | `FromEvidence` | refuse | `InvalidDataException` |
| **B5** | same | **well-formed but wrong owner closure** | `FromEvidence` | refuse | **ACCEPTED** (Finding F-01) |
| **B6** | same | **well-formed but wrong recovery closure** | `FromEvidence` | refuse | **ACCEPTED** (Finding F-01) |
| **B7** | same | **well-formed but wrong runtime inventory** | `FromEvidence` | refuse | **ACCEPTED** (Finding F-01) |
| B8 | same | non-hex closure | `FromEvidence` | refuse | `InvalidDataException` |
| B9 | same | undefined provenance (999) | `FromEvidence` | refuse | `InvalidDataException` |
| B10 | same | null manifest | `FromEvidence` | refuse | `InvalidDataException` |
| B11 | same | null evidence | `FromEvidence` | refuse | `InvalidDataException` |
| C1 | valid evidence | — | `ForTest` | tagged test-only | `DeterministicTestOnly` |
| C2 | `ForTest` result | — | read qualification props | both null | both null |
| C3/C4 | `Verify`-route result | — | provenance / qualification | protected / null | protected / null |
| C5 | unprovisioned host | — | `Verify()` | refuse | `InvalidDataException` |
| D1–D4 | RELEASE assembly | — | type/`ForTest`/`Verify`/ctors | type present, no `ForTest`, `Verify` present, private ctors | PASS |

Positive controls are genuine accepted-verification shapes only at the deterministic level; B1/C1/C3/C4/D1–D4 use synthetic evidence and are **not** proof of protected-root authorization. `Verify()` against a protected store is `NATIVE-PENDING` (Slice N).

## 7. RELEASE inspection (Q1-E)

- Debug and RELEASE `Gagamba.Execution.Linux.M1` build with 0 errors (the repository sets `TreatWarningsAsErrors=true`, so 0 warnings).
- `eng/VerifyProductionAssembly --production <Release.dll>` → **RESULT: PASS** (0/4 hook types, 0/3 hook properties, 14/14 core types, no forbidden factories). The E-1 projection type is present and asserted (`coreTypes`), confirming it is not accidentally excluded.
- `eng/VerifyProductionAssembly --production <Debug.dll>` (negative control) → **RESULT: FAIL** (exit 1) listing `debug-only authority factories remain: Grant::ForTest, OwnerReleaseGenerationGate::VerifyObserved, PrepublicationIntegrityProjection::ForTest, Qualified::ForTest`.
- RELEASE IL scan: `PrepublicationIntegrityProjection::ForTest` present **0** times; `OwnerAuthorityContext+Validation` exposes only its private `::.ctor` (no factory); no new public API (the projection is `internal`).
- Probe D2: RELEASE has no `ForTest`; D3: `Verify` present; D4: private constructors only.

## 8. PowerShell discrepancy classification

The implementation report records a failure in `eng/test-production-build.ps1` at the `missing-symbol` negative-control step. Independently reproduced with `pssrepro.ps1` under **Windows PowerShell 5.1.26100.9711**:

```
exit=1
contains_GAGAMBA002=False
contains_spaced=False
text_len=1844
first6bytes=64 6F 74 6E 65 74   (ASCII "dotnet")
raw_dotnet_has_GAGAMBA002=True
```

Byte inspection of the log found the UTF-16LE pattern `47 00 41 00 …` ("G\0A\0…") present and the plain-ASCII `GAG` pattern absent.

**Classification: test-harness compatibility / log-encoding defect in `eng/test-production-build.ps1` under Windows PowerShell 5.1, not a verifier correctness defect and not an E-1 defect.** Root cause: `Invoke-Probe` first writes a BOM-less single-byte header via `Set-Content`, then appends native output with `>>`, which under PS 5.1 encodes as UTF-16LE; the resulting mixed-encoding file is mis-decoded by `Get-Content -Raw`, so `$text.Contains('GAGAMBA002')` is false even though the probe emitted `GAGAMBA002` and exited non-zero. The underlying build negative control behaves correctly (`raw_dotnet_has_GAGAMBA002=True`; direct reproduction emits `error GAGAMBA002`). The assembly verifier (`eng/VerifyProductionAssembly`) is unaffected and independently PASSes. `pwsh` (PS 7, which uses UTF-8 redirection) is unavailable in this environment. This is a **retained tooling obligation** and a non-blocking qualification item (Finding F-03). Do not record the full script as PASS.

## 9. Production reachability graph (Q1-F)

IL-level extraction from `eng/VerifyProductionAssembly --production <Release.dll> --dump` (`release-il-callers.txt`):

- Methods referencing the E-1 types: only the E-1 type's own members (`PrepublicationIntegrityProjection::{.ctor, FromEvidence, Verify, get_*}` and the `InstalledIntegrityEvidence` record members) plus `InstalledFileSystem::VerifyActiveIntegrity` (which constructs the evidence value).
- Callers of `InstalledFileSystem::VerifyActiveIntegrity`: only `PrepublicationIntegrityProjection::Verify`.
- Callers of `PrepublicationIntegrityProjection::FromEvidence`: only `PrepublicationIntegrityProjection::Verify`.
- Callers of `PrepublicationIntegrityProjection::Verify`: **none**.
- `PrepublicationIntegrityProjection::ForTest`: **absent** from RELEASE.

Therefore no existing production entry path can use the projection to launch execution, publish installed state, acquire protected resources, construct validation authority, substitute for qualified identity, or bypass admission. The E-1 construction chain is dead code in RELEASE until a later package wires a consumer. This matches the expected E-1 result (production-unreachable preparation).

## 10. P4 neutrality assessment (Q1-G)

`git diff --stat 762cdf2 2259fce -- . ':!docs'` (P4 reconciliation source → E-1 implementation) lists only the four E-1 files. The diff restricted to `Lifecycle/`, `Bootstrap/`, `Restricted/`, `Execution/` and `Installed/InstalledFileSystem.cs` is **empty**.

Consequently, F01/F02, S10/S11 domination, S28 isolation, B3 constructor gates, provider gates, direct-Start/rendezvous ordering, first protected effects, RELEASE refusal and qualified→legacy downgrade refusal are unchanged. No new production caller was added, so no domination claim is invalidated. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING`, and the 331 native skips are preserved. The producer's provisional P4-neutral claim is independently confirmed for the exact revision (owner decision Q-E5 satisfied for E-1).

## 11. Test and build results (independently reproduced)

| Check | Command | Result |
|---|---|---|
| Debug build | `dotnet build src/Gagamba.Execution.Linux.M1/…csproj -c Debug` | 0 warnings / 0 errors |
| RELEASE build | `… -c Release` | 0 warnings / 0 errors |
| E-1 tests | `dotnet test … --filter FullyQualifiedName~PrepublicationIntegrityProjectionTests` | 17 passed / 0 failed / 0 skipped |
| Focused integrity/authority/release | `… --filter "…~Installed|…~Authority|…~ReleaseGenerationGate|…~FinalAcceptance"` | 232 passed / 32 skipped / 0 failed |
| Full Windows M1 | `dotnet test tests/Gagamba.Execution.Linux.M1.Tests/…csproj -c Debug` | **1385 passed / 331 skipped / 0 failed** (1716) |
| RELEASE verifier | `eng/VerifyProductionAssembly --production <Release.dll>` | PASS |
| Debug negative control | `--production <Debug.dll>` | FAIL, names `PrepublicationIntegrityProjection::ForTest` |
| Reviewer reflection probe | `docs/reviews/…q1-evidence/probe` | `SUMMARY failures=0` (B5–B7 recorded as observations) |
| PowerShell reproduction | `pssrepro.ps1` (PS 5.1) | harness encoding defect reproduced |

The full-suite count equals the accepted 1368 + 17 new E-1 rows; the **331 native skips are not passes**. RELEASE M1 SHA-256 observed this session: `C94566ECDBA41615C98FCEDCFBA339BDCDE434164A86ECE07CC6DE79E4D6CB87`.

## 12. Findings and qualifications

**F-01 (Observation; LOW; non-blocking) — internal evidence cross-check gap.** `PrepublicationIntegrityProjection.FromEvidence` does not cross-check `OwnerClosureSha256`/`RecoveryClosureSha256` against `InstallationIdentity.RoleHash(manifest,1/0)`, nor `RuntimeInventorySha256` against `manifest.platform.runtimeInventorySha256`. Well-formed-but-inconsistent evidence is accepted (probe B5/B6/B7). **Impact:** none in RELEASE — the only route is `Verify()`, which derives all three from the real manifest, and the entire chain has no caller (§9). The gap is reachable only through the Debug `ForTest` seam (tagged test-only) or a future internal caller. **Invariant:** "projection identity bound to the actual protected manifest." **Bounded remediation:** in `FromEvidence`, when `provenance == ProtectedActiveSelection`, assert the three digests equal the manifest-derived values before construction. This is defense-in-depth, not a RELEASE defect.

**F-02 (Observation; LOW; non-blocking) — duplicated verification implementation.** `VerifyActiveIntegrity` mirrors `VerifyActive` instead of sharing a core. Today the shared prefix is step-for-step equivalent and the qualified path is byte-identical, but future edits to `VerifyActive` will not propagate. **Recommended:** consolidate the shared read into one internal helper in a later bounded package (the owner decision permits extraction when qualified behavior is preserved).

**F-03 (Test-harness defect; LOW; non-blocking) — `eng/test-production-build.ps1` PS 5.1 encoding bug.** `Invoke-Probe` mixes a BOM-less `Set-Content` header with UTF-16LE `>>` output, so its own diagnostic match fails for negative controls. The underlying probes behave correctly. **Classification:** harness compatibility defect / retained tooling obligation. **Recommended:** fix the harness encoding (redirect to a single writer, or capture the output string in-memory) in a separate tooling change; do not modify the verifier or script under this commission. Not a blocking qualification issue.

**Observations (non-blocking):**
- **O1** The evidence record's constructor flags are `Public` on an `internal` type; it is not externally accessible and is not a projection factory. No action required, but it is not "private."
- **O2** `VerifyActiveIntegrity` intentionally omits the qualification-record association check (consistent with the null-association contract) and runs `CheckHost` unconditionally (stronger than the fresh branch). Documented, not a gap.
- **O3** Failure typing is not uniformly normalized to `InvalidDataException`; all outcomes still fail closed.
- **O4** Confirmation that the compiled RELEASE assembly lacks `ForTest` and the `Validation` factory relies here on IL/metadata inspection and the canonical verifier; no native protected positive was run.

**Deferred (not findings against E-1):** runtime loaded-role/verification-to-use check, K/M/F/S, GAVA1, v3-pre, root authorizer, launcher/custody, validation-context factory, R/O/C ordering, atomic enablement (E-2–E-7); protected native qualification and positive prepublication execution (Slice N, `NATIVE-PENDING`).

## 13. Formal qualification disposition

**SUPPORTED WITH QUALIFICATIONS.** The E-1 implementation independently satisfies its authorized refusal-only scope: distinct immutable internal projection; genuine non-forgeable RELEASE construction boundary; null qualification association preserved on all paths; Debug seam excluded from RELEASE; production-unreachable at the compiled-IL level; P4-neutral; no authority promotion; rebuilds and the full deterministic suite reproduce. Findings F-01–F-03 are non-blocking (no RELEASE defect path; one harness defect; one maintainability item). No demonstrated correctness defect, unauthorized path, or failed required invariant prevents qualification.

This is execution-plus-inspection qualification, not native qualification. `Verify()` success on a protected store and all protected positives remain `NATIVE-PENDING` (Slice N).

## 14. Recommended owner or remediation action

1. **Owner acceptance:** a separate owner acceptance decision may accept E-1 as SUPPORTED WITH QUALIFICATIONS, preserving F-01–F-03 as bounded residuals. This review does not accept it.
2. **Optional bounded hardening (F-01):** add the manifest cross-check in `FromEvidence` (defense-in-depth) — small and independently reviewable; could be folded into the next Slice E package rather than a standalone commission.
3. **Tooling obligation (F-03):** fix `eng/test-production-build.ps1` encoding under PS 5.1 in a separate tooling change.
4. **Do not** enable positive validation, construct validation authority, start E-2, or begin Slice N automatically; atomic enablement and the frozen-schema/approval-chain prerequisites remain in force.

Stop after this independent qualification and its documentation commit.
