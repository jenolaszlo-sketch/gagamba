# B6-SPEC-R1-Q2 — HD-1–HD-4 Decision Audit

Four human decisions are located in the controlling source documents. Their meaning is
taken from the documents themselves (not inferred from identifiers). Locations:
design §10.3 (`77ee441`), R1 remediation evidence §7 (`5b34667`), original B6 evidence
§8 ("Open human decisions"), and the Q1 review §J.

## HD-1 — Install privilege

- **Exact open question.** May B6 qualification use narrowly scoped `sudo` in the
  disposable WSL2 instance to create the root-owned install (`/opt/gagamba`,
  `install-receipt.json`, qualification record, atomic `active`)?
- **Dependent guarantee.** The root of trust itself: without a root-owned install the
  qualified native chain collapses to `Pending`, and the native `Qualified` tests
  cannot run. The plan forbids silent skips.
- **Alternatives/restrictions stated.** No setuid helper, no file capabilities, no
  privileged daemon; runtime stays unprivileged; install scripts run once and are
  SHA-256-recorded; candidate build stays unprivileged.
- **Evidence that would support approval.** Recorded exact instance, commands, paths,
  ownership, cleanup and residue.
- **Prohibited until approval.** Native `Qualified` cases run as `Pending`, not
  `Passed`; no root install in the canonical environment.
- **Current status.** Documented as required owner approval before IMPL-01. **Not
  approved** in any reviewed document.

## HD-2 — Public descriptor exposure

- **Exact open question.** Approve the additive descriptor v2 fields (or keep them
  internal until B7)?
- **Dependent guarantee.** Not a trust guarantee; it governs the public reporting
  surface. B6 does not activate a production provider, so there is no B6 consumer.
- **Alternatives/restrictions stated.** Minimal public surface
  (`InstallationState`, `RefusalReason`+`InstallationDiagnostic`, `InstallationId`,
  `ProfileId/Version`, `BootstrapVersion`, `RuntimeIdentifier`, `Architecture`,
  `Protocol` constants, `QualificationId`/`Scope`/`EvidenceSha256`/`Limitations`,
  `HostPrerequisites`, `CaptureReadbackAvailable`); per-file `Artifacts`,
  `Verification` and full `Platform` remain internal. No public factory/constructor.
- **Evidence that would support approval.** Internal descriptor projecting to the
  minimal public fields; no authority change.
- **Prohibited until approval.** Public-API/schema expansion beyond the minimal
  surface; the decision may be deferred (implement internal first).
- **Current status.** R1 applied the Q1-F08 minimization in design §4.6; the exposure
  decision remains **unapproved** (or deferred).

## HD-3 — Platform scope

- **Exact open question.** Confirm B6 initial qualification remains WSL2 x86-64 only,
  with native Linux-host qualification deferred to B5-L01/B7 and arm64 unsupported
  until a matching seccomp filter/build exists?
- **Dependent guarantee.** Platform honesty: prevents inferring native-Linux or arm64
  guarantees from WSL2/limited coverage.
- **Alternatives/restrictions stated.** Native Linux-host in B6 (overclaims B5 scope);
  add arm64 filter/build now (expands scope without qualification).
- **Evidence that would support approval.** Qualification evidence labelled with the
  platform; explicit WSL2-vs-native separation.
- **Prohibited until approval.** Any native-Linux-host or arm64 claim.
- **Current status.** WSL2-only scope is treated as the working assumption throughout,
  but HD-3 remains an **open** owner decision.

## HD-4 — Generation retention

- **Exact open question.** Confirm old immutable generations are retained until
  explicit removal (no auto-GC) and that removal refuses active/held/in-use
  generations (reporting `Unknown` if holders cannot be determined)?
- **Dependent guarantee.** Prevent silent loss of stop/recovery authority by deleting
  files still mapped by a surviving execution; preserve the active-generation
  pre-RELEASE policy.
- **Alternatives/restrictions stated.** Auto-GC after timeout (rejected); forced
  removal of active (rejected).
- **Evidence that would support approval.** Removal script checks the active pointer
  and holders; operator cleanup procedure and evidence.
- **Prohibited until approval.** Auto-GC or active-generation removal.
- **Current status.** Retention behavior is specified as the design default; the
  decision remains **open**.

## Consistency and no-implicit-approval check

Searched all committed B6 documents and the pointer docs (`PROJECT-STATUS.md`,
`handoff.md`, `work-queue.md`, the R1 evidence and the BIND-01 receipt) for any claim
that an HD decision is approved, accepted or decided:

- `PROJECT-STATUS.md` §8: "HD-1–HD-4 require owner decisions before IMPL-01."
- `handoff.md` §8: "Open owner decisions HD-1–HD-4 … Not authorized/established: B6 implementation …."
- `work-queue.md` §6: "B6 implementation requires a separate `GAGAMBA-RC2B-06-B6-IMPL-01` commission after HD-1–HD-4 owner decisions."
- R1 evidence §7 heading: "Open HD-1–HD-4 decisions (for owner approval)"; §10 disposition defers implementation to "after HD-1–HD-4 owner decisions."
- BIND-01 receipt §3: "HD-1–HD-4 remain open owner decisions."
- Design §10.3: "These are bounded choices, not design contradictions. Implementation must not manufacture approval."
- Q1 review §J recommends choices but explicitly leaves "Owner approval before IMPL?" = Yes.

**No document treats an HD decision as accepted** through default implementation
assumptions, qualification pass criteria, project-status wording, handoff wording,
work-queue readiness labels, or statements of coverage.

## Q2 finding on HD scope (Q2-F04)

The design's §2.7 (native dependency closure) recommends invariant globalization and
says "If the owner decides otherwise (**HD-3 scope**), the non-invariant row applies."
HD-3, however, is the **native-host/platform** decision, not a globalization-mode
decision. The globalization-mode choice is not enumerated among HD-1–HD-4. This is a
cross-reference/labeling defect, not an unsafe default: the recommended default
(invariant globalization) eliminates the ICU conditional dependency, and the
alternative requires the ICU version be recorded as a platform-baseline limitation
with refusal behavior. See `FINDINGS.md` Q2-F04.

## Conditional-design assessment

Each HD decision that is essential to a **mandatory** B6 contract is disclosed as a
condition rather than assumed:

- HD-1 is essential to the native `Qualified` chain; without it those cases are
  `Pending`. The condition is explicit and does not masquerade as implementation
  authority.
- HD-3 is essential to platform claims; the WSL2-only assumption is stated and no
  broader claim is made.
- HD-2 and HD-4 affect surface/retention, both bounded and disclosed.

The specification is therefore independently qualified **as a conditional design**:
no unapproved HD decision is required to make the *design* coherent, and where an HD
decision gates *evidence*, the plan marks the affected cases `Pending` rather than
passed.

## Q2 non-approval

Q2 does not approve, choose or resolve any of HD-1–HD-4, and does not select their
alternatives on the owner's behalf.
