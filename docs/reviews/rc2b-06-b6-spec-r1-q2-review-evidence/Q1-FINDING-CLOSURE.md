# B6-SPEC-R1-Q2 — Q1 Finding Closure

Finding-by-finding reconciliation of the original Q1 independent review
(`docs/reviews/rc2b-06-b6-spec-q1-independent-review.md` @ `08ebcb5`), against the R1
remediation (`77ee441`) and final provenance state (`5b34667`).

Dispositions used: `CLOSED — SUPPORTED` · `CLOSED WITH QUALIFICATIONS` · `OPEN — REMEDIATION REQUIRED` · `NOT ADDRESSED` · `INSUFFICIENT EVIDENCE`.

A finding is treated as closed only if the **substantive defect** is corrected. Restatement, a new paragraph citing the finding ID, or the existence of the remediation commit is not accepted as closure.

## Summary table

| Q1 ID | Severity | Short name | R1 location | Disposition |
|---|---|---|---|---|
| B6-Q1-F01 | Major | T25 contradicts installed-closure design | plan T25-A/T25-B; design §2.5/D8 | **CLOSED — SUPPORTED** |
| B6-Q1-F02 | Major | Test-only functionality in production M1 | design §2.5 (Alt A–C, 7 proof obligations); plan T24 | **CLOSED — SUPPORTED** |
| B6-Q1-F03 | Minor | FD-based SafeCopyIn overstatement | design §5.2 step 6 | **CLOSED WITH QUALIFICATIONS** (residual wording, Q2-F01) |
| B6-Q1-F04 | Major | Incomplete .NET native dependency closure | design §2.7; plan T20a; §2.6 overclaim removed | **CLOSED — SUPPORTED** |
| B6-Q1-F05 | Minor | Legacy skipped-test accounting ambiguity | plan T25-A | **CLOSED WITH QUALIFICATIONS** (Q2-F05) |
| B6-Q1-F06 | Minor | Ancestor predicate sticky-bit exception | design §5.2 step 1; V04; T01/T04 | **CLOSED — SUPPORTED** |
| B6-Q1-F07 | Major | Installation-identity handshake not wire-specified | design §6.2; plan T20b | **CLOSED — SUPPORTED** (minor precision gaps, Q2-F09) |
| B6-Q1-F08 | Observation | Descriptor v2 expansion larger than needed | design §4.6 | **CLOSED — SUPPORTED** |
| B6-Q1-F09 | Observation | System.IO.Hashing leakage in deps.json | design §2.3, §7.1; plan T10 | **CLOSED — SUPPORTED** |
| B6-Q1-F10 | Observation | Controller load-path wording | design A1 row, §2.3 | **CLOSED — SUPPORTED** |
| B6-Q1-F11 | Observation | Hypothetical recovery restart branch | design §6.5 | **CLOSED — SUPPORTED** |
| B6-Q1-F12 | Observation (no defect) | /proc/self/fd/11 semantics | design unchanged | **CLOSED — SUPPORTED** (preserved) |
| B6-Q1-F13 | Major | Qualification-record association ambiguous | design §4.5; plan T20c | **CLOSED — SUPPORTED** (minor grammar gap, Q2-F06) |
| B6-Q1-F14 | Minor | First-install provenance label | design §4.3 step 3, receipt; plan T20c subcase 1 | **CLOSED — SUPPORTED** |

No Q1 finding is `OPEN — REMEDIATION REQUIRED`, `NOT ADDRESSED` or `INSUFFICIENT EVIDENCE`.

## Per-finding reconciliation

### B6-Q1-F01 — T25 contradicts the installed-closure design (Major)

- **Original concern.** T25 required the previously skipped `B3RestrictedLifecycleTests` B2-install cases to run against the qualified closure, but those tests use `RecoveryConfiguration` v1, `artifacts/b2/install`, `/usr/bin/dotnet` + ambient environment and `FileName="dotnet"` — exactly the legacy path the design (D8) refuses. The test set was unprovable under a "no skip" rule.
- **R1 change.** Plan T25 is split. **T25-A** enumerates the four historical skipped configurations, their source lines and their disposition (all remain legacy regressions, explicitly unqualified, not counted as passes). **T25-B** specifies five new **mandatory native** tests against the integrated `RecoveryConfiguration` v2 with pinned runtime and installation identity, and states it does not claim byte-identical historical replay of the refused legacy topology. Plan §7 exit criteria require T25-A accounting and T25-B zero installation-closure skips.
- **Assessment.** The contradiction is removed: the design no longer requires a test set that exercises the refused path. Historical accounting and new obligations are separated, and the legacy path stays refused. The resolution is testable in principle (T25-A is documentary; T25-B is native).
- **Residual.** T25-A/T25-B are specified but not executed — expected for a specification stage (see `CLAIMS-ARGUMENTS-EVIDENCE.md`).
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F02 — Test-only functionality compiled into production M1 (Major)

- **Original concern.** The design claimed test/fault hooks were excluded by a hash/name deny list, but `Int01QualificationFault`, `OwnerSafetyMonitor.QualificationNativeStop`, `BubblewrapAdapter.QualificationBeforeCaptureStore`, `SafeCopyIn.CopyTestHooks`/`BarrierDeathException` and `JournalFileStorage.QualificationBeforeRename` are compiled into production `Gagamba.Execution.Linux.M1.dll`. A file/assembly deny list cannot remove types/members; T24's positive control was unwritable.
- **R1 change.** Design §2.5 now states plainly that the hooks are compiled into production and that a hash deny list cannot exclude individual types/members. It evaluates three alternatives — **A** compile-time exclusion via a `GAMBA_PRODUCTION`-style define (recommended), **B** a separate non-deployable test-seam assembly, **C** a provably-false runtime guard (not recommended) — and specifies seven **executable** proof obligations, an exact implementation boundary (csproj define, `#if !GAGAMBA_PRODUCTION` wrappers, `Directory.Build.props`) and a `buildConfiguration: "Production"|"Test"` label in the qualification record. Plan T24 is rewritten with a deterministic reflection subcase (assert excluded types/members absent) plus a native deny-list subcase.
- **Assessment.** The false claim is corrected. The obligation is now genuine and testable, not a restatement. The `Production`/`Test` distinction is carried into the qualification record so evidence cannot silently mix builds.
- **Residual.** The separation is an IMPL-01 source change; it is defined, not implemented. This is correctly disclosed.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F03 — SafeCopyIn "verified source fds" overstatement (Minor)

- **Original concern.** The design said coordinator/filter are "copied from verified source fds," but `CopyPlan.SourceRootPath` is a string reopened by path; only the staging parent is a retained fd. Content is claimed-digest bound; the wording overstated the API.
- **R1 change.** Design §5.2 step 6 is corrected to "copied from verified source paths with claimed digests under a verified staging parent," documents the root-only path-replacement residual, and makes a `SourceRootAuthority` fd optional (not required for the trust claim).
- **Assessment.** The primary statement is corrected. **However**, the same overstated phrase remains in two other locations: design §6.3 property #8 ("coordinator/filter staged from verified fds") and plan T13 ("coordinator/filter staged from verified fds"). See **Q2-F01**.
- **Disposition: CLOSED WITH QUALIFICATIONS** (underlying content is correctly bounded; the wording correction is incomplete).

### B6-Q1-F04 — Incomplete .NET native `dlopen` dependency closure (Major)

- **Original concern.** The inventory covered loader/glibc and Bubblewrap libraries but not the .NET runtime's own `dlopen` inputs (ICU/OpenSSL/zlib), and no invariant-globalization stance was taken; the §2.6 claim of "no unidentified loader input" was therefore false.
- **R1 change.** New design §2.7 classifies ICU, OpenSSL, zlib and the native runtime loader with load mechanism, triggering feature, file family, resolution source, deployment control, minimum platform version, mandatory/conditional/excluded status, observation method and refusal behavior. Invariant globalization is the recommended configuration; the alternative records the ICU version as a platform-baseline limitation. The `platform` manifest section is extended (`icuVersion`/`globalizationMode`, `opensslVersion`, `zlibVersion`) and admission verifies it. The §2.6 overclaim is removed and reworded to state the native `dlopen` baseline is "explicit remediation items before B6-S1 can be claimed." Plan T20a adds mandatory static `ldd`/`readelf`, runtime `/proc/<pid>/maps`, controlled substitution, missing-library and roll-forward subcases.
- **Assessment.** The gap is inventoried **and** explicitly bounded, with refusal behavior for substituted/unsupported versions, satisfying the Q1 remedy ("inventory ... or explicitly bound"). No single `ldd` is claimed exhaustive.
- **Residual.** The globalization-mode choice is presented as riding on "HD-3 scope" although HD-3 is the native-host decision; this is a cross-reference defect, not a reopening of F04. See **Q2-F04**.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F05 — Legacy skipped-test accounting ambiguity (Minor)

- **Original concern.** Plan T25 did not distinguish previously skipped cases from new installed-closure cases or state which fixture remains.
- **R1 change.** T25-A enumerates the four skipped configurations and their disposition; T25-B defines the new closure tests. The split removes the counting ambiguity.
- **Assessment.** The ambiguity is resolved at design level. **However**, T25-A asserts "Exact test names, skip reasons, and source lines recorded in the remediation evidence artifact," yet the R1 remediation evidence lists only configurations and a line range (`B3RestrictedLifecycleTests.cs:628–800`) and does not enumerate the exact test method names. This is an evidence-claim mismatch. See **Q2-F05**.
- **Disposition: CLOSED WITH QUALIFICATIONS.**

### B6-Q1-F06 — Ancestor predicate sticky-bit exception (Minor)

- **Original concern.** `ExperimentalRuntimePin.AncestorsProtected` accepts a root-owned ancestor with group/other write when the sticky bit is set, weakening the immutability boundary.
- **R1 change.** Design §5.2 step 1 specifies a stricter predicate: every ancestor to `/` must be root-owned and **not** group-writable and **not** other-writable; the sticky exception is dropped; enforced at admission V04 and verified in T01/T04.
- **Assessment.** The predicate is corrected in the specification and bound to deterministic and native tests. Exact behavior is unambiguous and testable.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F07 — Installation-identity handshake not wire-specified (Major)

- **Original concern.** The identity echo named fields but not exact message IDs, payload bounds, direction, sequence, channel, owner expected-identity delivery, or failure behavior — a security-relevant ordering before `WriterReady`/`StartAuthority`.
- **R1 change.** New design §6.2 fully specifies: `RecoveryConfigV2` (Version 2, required `InstallationIdentity`), `InstallationEchoRequest` (`RecoveryMessageType=6`), `InstallationEchoResponse` (`RecoveryMessageType=7`), `OwnerInstallationEcho` (`OwnerProtocolMessageType=8`), the existing `OwnerStartAuthority` carrying the expected identity, framing (4-byte big-endian length), payload layouts with offsets/sizes, expected-identity provenance (receiver compares against its independently held manifest-derived value), recovery-never-release restriction, ordering (recovery echo before `Ready`; owner echo before `StartAuthority`), 5-second timeouts, no retries, replay/duplication, cancellation and cleanup. Plan T20b exercises seven mutation subcases.
- **Assessment.** The ordering and identity binding are now specified at wire precision, with independent expected-value comparison rather than peer self-assertion. This closes the F07 concern.
- **Residual.** Minor field-set precision gaps between the §6.1 identity struct and the §6.2.3 echo payload (ManifestSchema omission; single role-closure field reused by role). See **Q2-F09**. Message-ID non-collision is flagged as a T20b limits check.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F08 — Descriptor v2 expansion larger than needed (Observation)

- **Original concern.** The public v2 surface included per-file `Artifacts`, `Verification` and `Platform` inventories duplicating internal manifest data.
- **R1 change.** Design §4.6 reduces the public v2 surface to admission/refusal fields and keeps per-file `Artifacts`, `Verification` and full `Platform` internal until a B7 consumer exists.
- **Assessment.** Correctly minimized; no authority change. Consistent with remediation evidence §7 HD-2.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F09 — `System.IO.Hashing` leakage in `deps.json` (Observation)

- **Original concern.** The leak was observed in existing Release output, not a fresh publish.
- **R1 change.** Design §2.3 and §7.1 require inspection of the fresh candidate publish; the design rule (exclude build-only assets or manifest-pin every deps-listed runtime asset; refuse missing) stands. Plan T10 covers deps closure.
- **Assessment.** The rule was already correct; R1 explicitly requires fresh-publish recheck and keeps the refusal. No overclaim remains.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F10 — Controller load-path wording (Observation)

- **Original concern.** "host loads from install root" overstated a load-path requirement.
- **R1 change.** A1 row and §2.3 corrected to "host loads assembly; verifier hashes `Assembly.Location` against manifest provider role"; load path immaterial.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F11 — Hypothetical recovery restart branch (Observation)

- **Original concern.** A "recovery restart after active changed" branch read as a current mechanism although no reconnect listener/relaunch exists.
- **R1 change.** Design §6.5 explicitly marks it hypothetical ("No reconnect listener or post-controller recovery relaunch exists today") and states the conservative actual behavior.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F12 — `/proc/self/fd/11` semantics (Observation, no defect)

- **Original concern.** None; the design's description was accurate.
- **R1 change.** No change; preserved. Remediation evidence §4.12 records it as independently supported.
- **Disposition: CLOSED — SUPPORTED (preserved).**

### B6-Q1-F13 — Qualification-record association ambiguous (Major)

- **Original concern.** The receipt had no `qualificationId`/`qualificationRecordSha256`; record selection among zero/one/multiple `<id>.json` was undefined, so a stale/narrower/broader record for the same digest could be reported `Qualified`.
- **R1 change.** Design §4.5 adds `qualificationId` and `qualificationRecordSha256` to the root-owned receipt (required for `Qualified`). The verifier selects the record by explicit `qualificationId` (no directory search), verifies its SHA-256 against the receipt, verifies `artifactSetSha256` against `active`, profile/RID/scope against the manifest, and `buildConfiguration="Production"` for a qualified production install. A nine-row state table defines deterministic `Unqualified` behavior for missing/mismatched/stale/conflicting records. Plan T20c adds nine deterministic subcases.
- **Assessment.** Selection is now deterministic and fail-closed; no search/latest-file-wins; self-generated reports cannot self-approve. This closes the association gap.
- **Residual.** `qualificationId` is not given an explicit bounded/identifier grammar before being opened as `<qualificationId>.json`; root ownership places this outside the trust boundary, but a format constraint would be defense-in-depth. See **Q2-F06**.
- **Disposition: CLOSED — SUPPORTED.**

### B6-Q1-F14 — First-install provenance label (Minor)

- **Original concern.** `install-candidate.sh --expected-digest` implied an independent expected value at first install.
- **R1 change.** Design §4.3 step 3: first install records `expectedDigestSource="unverified-candidate"` and state `Installed/Unqualified`; the receipt moves to `expectedDigestSource="qualification-record"` only after the separate trusted qualification action. §4.5 enumerates the receipt fields; T20c subcase 1 covers it.
- **Assessment.** The Installed-vs-Qualified distinction is now explicit and cannot be blurred by a self-computed digest.
- **Disposition: CLOSED — SUPPORTED.**

## A2 — Silent contract weakening check

No R1 change narrows a mandatory guarantee, redefines an unsafe outcome as acceptable, excludes a previously required adversarial case, or moves a requirement into an unapproved milestone:

- The F02 production/test separation **tightens** (removes dormant hooks) rather than weakens.
- The F03 and F11 corrections are honest downgrades of overstatement, not removals of a guarantee.
- The F04 §2.6 reword **removes a false completeness claim**; the closure requirement is retained and extended.
- The F08 public-surface reduction removes API surface, not security behavior.
- No mandatory test was removed; T20a/T20b/T20c/T24/T25-A/T25-B were added.

The only "weakening-shaped" change is the F01 T25 split, which stops counting legacy-path tests as qualified-closure passes. That is a **correction of a false positive**, not a weakening of a mandatory guarantee: the mandatory installed-closure obligation is preserved in T25-B.

## A3 — Closure result

Every original Q1 finding receives an individual disposition. Ten are `CLOSED — SUPPORTED`, three `CLOSED WITH QUALIFICATIONS` (F03, F05, F07-residual) and one (`F12`) preserved as no-defect. No mandatory Q1 security finding remains unresolved. The three qualifications are bounded documentation/precision issues recorded as Q2-F01, Q2-F05 and Q2-F09; none reinstates a Q1 security defect and none blocks acceptance.
