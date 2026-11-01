# B6-SPEC-R1-Q2 — Claims, Arguments and Evidence (CAE)

Material security/correctness guarantees of the amended B6 design, each assessed as
Claim–Argument–Evidence. The review distinguishes four levels:

1. **Contract defined** — the specification states the property.
2. **Qualification method specified** — a test/probe exists that could observe it.
3. **Implementation exercised** — code runs and is observed.
4. **Security guarantee independently demonstrated** — reproduced by an independent party.

**B6-SPEC-R1 is a specification remediation only.** Every guarantee below is at level 1–2 (contract defined / method specified). **None** is at level 3 or 4. Documentation validation (link/format checks) does not raise any claim's level.

## C1 — Artifact identity is verifiable

- **Claim.** The host can establish the exact identity of an installed artifact set.
- **Argument.** A deterministic canonical manifest over the exact bytes yields an artifact-set digest (§4.1, §4.5); the verifier recomputes it from the installed bytes and compares it to the directory name, `active`, the receipt and the qualification record (§4.5). Path grammar + `openat2` resolve policy + exact-set enumeration remove aliasing/extra/missing files.
- **Evidence (specified).** Plan V01 (canonical codec), V02 (path grammar), V03 (digest), V05 (exact set), T01 (positive control). Positive control: `candidate-A` verifies `Qualified`.
- **Level:** 1 (defined) + 2 (method specified). Not exercised.

## C2 — Installed bytes match the accepted identity

- **Claim.** The bytes consumed are the bytes authorized.
- **Argument.** fd-based hashing with before/after `fstat` identity; immutable root-owned tree; symlink-free; `nlink==1`; no setuid/caps (§5.2 steps 1–3).
- **Evidence (specified).** T01 (installed digest == manifest == receipt == qualification == selection); V04 (file metadata); T02 (one-byte mutation refuses).
- **Level:** 1 + 2.

## C3 — Unauthorized substitution is refused

- **Claim.** A modified/missing/test-only/wrong-protocol/wrong-RID/partial/untrusted installation is refused before target execution.
- **Argument.** Exact-set + per-file digest + deny list (roles/kinds/paths/hashes/assembly names) + protocol/RID/profile comparison + platform-baseline checks (§2.5, §7.6, §8).
- **Evidence (specified).** T02–T10, T12, T19, T21, T24, V05/V06/V11/V12; negative evidence required: no process, no cgroup, no marker, independent inventory (plan §4).
- **Level:** 1 + 2.

## C4 — Trust does not survive invalidating changes

- **Claim.** A successful verification of an earlier artifact does not authorize a subsequently changed artifact.
- **Argument.** Controller re-reads `active` before RELEASE and refuses `ReleaseRequest` if it changed; side-by-side immutable generations with no cross-generation adoption; recovery/owner echo identity before authority; reconnect refused (§5.2 step 8, §6.3, §6.5).
- **Evidence (specified).** T13 (artifact changed after verification), T15 (actor/version mismatch), T16 (controller death), T17 (deployment changed while HELD), T18 (all holders lost).
- **Level:** 1 + 2. The T13 residual (root-only mutation) is explicitly bounded, not claimed as atomic.

## C5 — Unsupported conditions fail conservatively

- **Claim.** Missing, malformed, conflicting, unsupported or unavailable evidence never yields a trusted result.
- **Argument.** §8.1 stage table; typed refusal vocabulary; nine-row §4.5 state table; no optimistic `Qualified`; `UnavailableRestrictedExecutionProvider` default with no fallback (§8.2).
- **Evidence (specified).** T19, T20, T20c, T21, V07/V08/V09/V11.
- **Level:** 1 + 2.

## C6 — Qualification association is deterministic (R1 new)

- **Claim.** Exactly one qualification record authorizes an installed digest.
- **Argument.** Receipt carries `qualificationId` + `qualificationRecordSha256`; verifier selects by ID (no search), verifies the record hash, digest, profile/RID/scope and `buildConfiguration` (§4.5).
- **Evidence (specified).** T20c (nine deterministic mismatch subcases), V08.
- **Level:** 1 + 2. Closes Q1-F13.

## C7 — Native dependency closure is classified (R1 new)

- **Claim.** Every native loader input the qualified profile can load is inventoried or explicitly bounded.
- **Argument.** §2.7 classifies ICU/OpenSSL/zlib/native-runtime-loader with resolution source, minimum version, mandatory/conditional/excluded status and refusal behavior; the `platform` manifest section is extended; the §2.6 overclaim is removed.
- **Evidence (specified).** T20a (static `ldd`/`readelf` + runtime `/proc/<pid>/maps` + substitution + missing-library + roll-forward), V06.
- **Level:** 1 + 2. Closes Q1-F04.

## C8 — Installation identity is wire-bound (R1 new)

- **Claim.** Surviving actors agree on one installed identity/generation before acquiring authority.
- **Argument.** §6.2 specifies message IDs, channels, payload bounds, ordering (recovery echo before `Ready`; owner echo before `StartAuthority`), independent expected-value provenance, timeouts and mismatch behavior; recovery never gains release.
- **Evidence (specified).** T20b (seven mutation subcases), T15.
- **Level:** 1 + 2. Closes Q1-F07.

## C9 — Test-only code cannot be in the qualified production closure (R1 new)

- **Claim.** The qualified production assembly contains no test/fault hooks and cannot activate synthetic faults.
- **Argument.** §2.5 requires compile-time exclusion (Alternative A) or a bounded alternative, with seven executable proof obligations and a `buildConfiguration` label.
- **Evidence (specified).** T24 (reflection type/member-absence subcase + native deny-list subcase), V12.
- **Level:** 1 + 2 (design obligation for IMPL-01). Closes Q1-F02.

## C10 — No new release authority

- **Claim.** B6 adds no RELEASE authority, capability, deadline or recovery path.
- **Argument.** Recovery `ReleaseAuthority => false`; single owner-local RELEASE writer; descriptor is passive; identity echo is detection, not authority (§3.2, §4.6, §6.1–6.2).
- **Evidence (specified).** T16/T18/T20b subcase 7; acceptance criteria §12.5.
- **Level:** 1 + 2 (preservation of accepted B1–B5 semantics).

## Unsupported assertions (explicitly not claimed)

- That an installed-artifact protection is **implemented** or **exercised** — the R1 slice is documentation-only.
- That passing documentation validation proves any security property.
- That WSL2 is native Linux, or that ordinary unelevated tests prove elevated-host guarantees.
- That a valid manifest proves production authorization or sandbox isolation (plan §5 rules).
- Binary reproducibility (explicitly disclaimed, §11).

## CAE verdict

Every material B6 guarantee has (1) a defined contract and (2) a specified qualification method that could distinguish correct from merely-reported-success behavior. **No guarantee is presented as already proven by documentation validation.** The qualification plan supplies positive controls, adversarial cases, independent expected results, reproducible observations, failure classification and bounded acceptance criteria for each mandatory claim (see `QUALIFICATION-TRACEABILITY.md`). This is the correct readiness level for a specification stage.
