# B6-SPEC-R1-Q2 — Independent Adversarial Contract Probes

Reviewer-owned paper/contract probes. Each states the exact contract clause, initial
state, adversarial change, expected trust decision, required refusal/recovery, deciding
authority, the future qualification test that would observe it, and whether the
specification gives an unambiguous answer. These are specification reviews, not native
execution tests. No result is invented; where the document is silent the case is marked
`UNDERSPECIFIED`.

Design references are to `docs/design/rc2b-06-b6-installed-artifact-trust.md` @ `77ee441`.

## Probe 1 — Valid artifact, valid evidence, authorized consumer

- **Clause.** §4.2–4.5, §8.1.
- **Initial state.** `active` = digest D; generation D root-owned, exact set, receipt has `qualificationId=Q`, record Q matches digest/profile/RID/scope, `buildConfiguration="Production"`.
- **Adversarial change.** None (positive control).
- **Expected decision.** `Qualified`; `CreateOperation` returns an operation; prepare reaches HELD.
- **Refusal/recovery.** n/a.
- **Authority.** Verifier at admission; controller.
- **Future test.** T01 (N).
- **Answer.** **Unambiguous (supported).**

## Probe 2 — Modified artifact with unchanged trust evidence

- **Clause.** §4.5 (recompute digest from installed bytes), §7.6 (`ArtifactIntegrityMismatch`).
- **Initial state.** Qualified generation D.
- **Adversarial change.** One byte mutated in `owner/Gagamba.Execution.Linux.M1.dll`; manifest/receipt/record unchanged.
- **Expected decision.** `UnqualifiedInstallation` with `ArtifactIntegrityMismatch` before target execution.
- **Refusal/recovery.** No domain/owner/recovery; no fallback to the previous generation.
- **Authority.** Verifier.
- **Future test.** T02 (N-root), V04/V05.
- **Answer.** **Unambiguous (supported, specification level).**

## Probe 3 — Wrong artifact under an expected identity/location

- **Clause.** §2.5 deny list, §7.6, §7.7.
- **Initial state.** Exact production coordinator expected.
- **Adversarial change.** Install `wp05-qualification-coordinator` under the exact filename `native/exec-coordinator` (same role/path, different hash/build).
- **Expected decision.** Refused; filename/label is irrelevant; manifest role/hash/build identity control.
- **Refusal/recovery.** No target; no fallback to a test coordinator or PATH.
- **Authority.** Verifier.
- **Future test.** T08 (N), V12.
- **Answer.** **Unambiguous (supported).**

## Probe 4 — Missing, malformed or contradictory trust evidence

- **Clause.** §4.5 state table, §4.6, §8.1–8.2.
- **Initial state.** Readable anchor.
- **Adversarial change.** Remove `active`/receipt/record; make them unreadable; duplicate roles; unsorted artifacts; all-zero digest; wrong hash length; receipt/manifest disagreement; qualification from another digest.
- **Expected decision.** `Unavailable`/`Unqualified`; never best-effort.
- **Refusal/recovery.** No operation; no optimistic descriptor.
- **Authority.** Verifier.
- **Future test.** T20, T21, V07–V09, T20c (record-specific).
- **Answer.** **Unambiguous (supported).**

## Probe 5 — Installation/replacement interrupted between states

- **Clause.** §4.2 (atomic `active`), §5.2 step 4.
- **Initial state.** Complete generation D is active.
- **Adversarial change.** Create a half-populated generation; point `active` at it (or crash mid-install); leave a partial manifest.
- **Expected decision.** `PartialInstallation`/`Unqualified`; a partial generation is never named by `active`.
- **Refusal/recovery.** No mixed-generation admission.
- **Authority.** Installer (atomic rename) + verifier exact-set check.
- **Future test.** T14 (V+N).
- **Answer.** **Unambiguous (supported).**

## Probe 6 — Stale trust evidence after an artifact changes

- **Clause.** §5.2 step 8, §5.4, §6.5.
- **Initial state.** Generation A HELD; release pending.
- **Adversarial change.** Install generation B and flip `active` after HELD, before `ReleaseRequest`; optionally replace A's files (root).
- **Expected decision.** No substituted artifact receives RELEASE; controller refuses release; conservative `Refused`/`Unknown`; generation B not adopted.
- **Refusal/recovery.** Existing stop/lifeline path; no second physical release.
- **Authority.** Controller (re-read `active`) + owner arbiter.
- **Future test.** T17 (I + N-root), T13.
- **Answer.** **Unambiguous (supported).** Root-only residual explicitly documented, not claimed atomic.

## Probe 7 — Unsupported host/platform or unsupported trust format

- **Clause.** §7.6, §8.3, §10.2 D4.
- **Initial state.** All prerequisites present.
- **Adversarial change.** Wrong RID/`e_machine`; missing cgroup2/`openat2`/`pidfd`/`SO_PEERPIDFD`/subreaper/userns/seccomp; non-empty caps; root uid; manifest schema/protocol mismatch; arm64.
- **Expected decision.** `UnsupportedProfile`/`PrerequisitesUnavailable`/`ProtocolIncompatible`/`UnqualifiedInstallation`; no weakened restriction.
- **Refusal/recovery.** No target; no fallback.
- **Authority.** Verifier (platform probe + compiled constants).
- **Future test.** T05, T06, T19, V02/V10.
- **Answer.** **Unambiguous (supported).**

## Probe 8 — Artifact/path alias or substitution between verification and consumption

- **Clause.** §5.2 steps 1–3, §5.2 step 7, §5.5.
- **Initial state.** Regular root-owned files verified.
- **Adversarial change.** Symlink/hardlink/`..`/absolute/empty path component; replace a parent directory with a symlink; replace an artifact between verify and spawn.
- **Expected decision.** Identity-checked refusal; no path escape; managed owner/recovery detect a post-start closure mismatch and refuse before authority.
- **Refusal/recovery.** No HELD/RELEASE/domain.
- **Authority.** Verifier (`openat2` resolve policy, `nlink==1`, `fstat`) + post-start echo.
- **Future test.** T12 (V+N), T13 (N-root).
- **Answer.** **Supported within the declared model; the root-only post-verify replacement is explicitly bounded** (documented residual, not a claim of atomicity).

## Probe 9 — Qualification-record substitution / self-assertion

- **Clause.** §4.5 state table; §6.2.6.
- **Initial state.** Qualified install with a single accepted record.
- **Adversarial change.** Add a second record `Q2` with the same digest but broader scope; tamper the record file; point the receipt at a missing `Q1`; attempt to self-approve with a producer-generated report.
- **Expected decision.** Only the receipt-referenced `Q1` is evaluated (no directory search); hash mismatch → `Unqualified`; missing record → `Unqualified`; a self-generated test report cannot self-approve (separate root action required).
- **Refusal/recovery.** No `Qualified` state.
- **Authority.** Verifier (explicit selection) + root (`qualify-install.sh`).
- **Future test.** T20c subcases 2–5, 8–9.
- **Answer.** **Unambiguous (supported).**

## Probe 10 — Path/format abuse of the qualification identifier

- **Clause.** §4.5 ("Opens `/opt/gagamba/qualifications/<qualificationId>.json` (no directory enumeration)").
- **Initial state.** Root-owned receipt with `qualificationId`.
- **Adversarial change.** A `qualificationId` containing path separators or `..` (e.g. `../../etc/passwd`), or an overlong/empty value.
- **Expected decision.** Should be refused as a malformed identifier before any path open.
- **Refusal/recovery.** `Unqualified`; no path escape.
- **Authority.** Verifier.
- **Future test.** Not explicitly allocated (V02 covers manifest *artifact* paths, not `qualificationId`).
- **Answer.** **UNDERSPECIFIED (minor).** The design constrains artifact-path grammar (V02) but does not state a bounded identifier grammar for `qualificationId`. Because the receipt is root-owned, this is outside the declared trust boundary (a root actor is out of model), so it is not security-critical; a format constraint would still be defense-in-depth. Recorded as Q2-F06.

## Probe 11 — "Trusted owner action" for acceptance

- **Clause.** §4.5 ("record must have `acceptedAtUtc` from trusted owner action"); plan T20c subcase 8.
- **Initial state.** Root-owned record present.
- **Adversarial change.** A record with `acceptedAtUtc` present but not produced by the trusted qualification step.
- **Expected decision.** `Unqualified` if acceptance cannot be established.
- **Refusal/recovery.** No `Qualified`.
- **Authority.** Root ownership of the record file is the actual basis; the verifier cannot distinguish "trusted source" from the field alone.
- **Future test.** T20c subcase 8 asserts the outcome but does not define how "non-trusted source" is detected.
- **Answer.** **Partially specified.** The security basis (root ownership, mode 0444) is correct and sufficient within the model; the phrase "from trusted owner action" is imprecise because it is enforced by file ownership, not by field content. Minor wording precision issue (folded into Q2-F06 cluster / noted here).

## Probe 12 — Echo replay across generations

- **Clause.** §6.2.8 replay/duplication.
- **Initial state.** Operation on generation A.
- **Adversarial change.** Replay a previous generation's `InstallationEcho`.
- **Expected decision.** Generation check fails; controller refuses; no domain.
- **Refusal/recovery.** Abort.
- **Authority.** Controller.
- **Future test.** T20b subcase 6.
- **Answer.** **Unambiguous (supported).**

## Probe summary

| # | Scenario | Outcome |
|---|---|---|
| 1 | Valid/valid/authorized | Supported |
| 2 | Modified artifact, unchanged evidence | Supported |
| 3 | Wrong artifact under expected identity | Supported |
| 4 | Missing/malformed/contradictory evidence | Supported |
| 5 | Interrupted install | Supported |
| 6 | Stale evidence after artifact change | Supported (root-only residual bounded) |
| 7 | Unsupported host/platform/format | Supported |
| 8 | Path alias/substitution verify→consume | Supported, bounded |
| 9 | Qualification-record substitution/self-assertion | Supported |
| 10 | `qualificationId` path/format abuse | **UNDERSPECIFIED (minor, Q2-F06)** |
| 11 | "Trusted owner action" detection | Partially specified (minor wording) |
| 12 | Echo replay across generations | Supported |

Ten probes are supported with unambiguous answers; two are minor precision gaps in
non-security-critical surface (root-owned metadata, outside the declared trust
boundary). **No adversarial probe exposes a security-critical case that the design
fails to answer or that would authorize trust.** No `UNDERSPECIFIED` case is
security-critical, so none is a blocking finding.
