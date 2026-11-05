# RC2B-06 B6 integrated Start authority owner decision

**ALTERNATIVE B - OWNER-SIDE AUTHORITY ENFORCEMENT - ACCEPTED AS DESIGN WITH EXPLICIT QUALIFICATIONS**

Date: 2026-11-06 (Asia/Manila)

Decision: `GAGAMBA-RC2B-06-B6-INTEGRATED-START-AUTHORITY-OWNER-DECISION-01`

Parent investigation: `GAGAMBA-RC2B-06-B6-INTEGRATED-START-AUTHORITY-01`

Independent design review: `GAGAMBA-RC2B-06-B6-INTEGRATED-START-AUTHORITY-OWNER-DECISION-Q1` (Terra High), disposition **DESIGN-Q1 SUPPORTED WITH QUALIFICATIONS**

Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`, branch `main`

Canonical baseline: commit `cbf1a98b157d06243c488bf723d8eca46518a36e`, tree `0b1966772365fa0336d121a3b1352615f0ad1605`

## 1. Decision

The human owner approves **Alternative B: owner-side authority enforcement**, subject to the qualifications and the wording correction in section 2.

The accepted design decisions are:

1. **Owner-side manifest authorization.** The owner must independently verify the applicable S2 installation identity and final-admission authority before granting new qualified execution authority.
2. **Owner-side Q10.** The owner must independently revalidate the bound active generation at the specified pre-RELEASE boundary. The controller-side Q10 check remains in force. The owner remains the sole physical RELEASE writer. The original C3 deadline and stopping precedence remain unchanged.
3. **Prevalidation owner scan.** Approved as proposed, subject to the accepted observation-to-effect residual and the corrected wording in section 2. A generation switch after the verifier's last relevant observation may escape detection, even if the scan has not yet returned.
4. **Validation-only early direct authentication.** Approved. Authentication may precede validation authorization, but must not by itself grant manifest, staging, HELD or RELEASE authority. Subsequent validation actions require independent live root authorization, exact session and generation binding, and the approved bounded test case.
5. **Stopping and reconciliation.** Stopping, reaping, retained-state reconciliation and honest cleanup reporting must remain available for already admitted operations without renewed general admission.

This is **design acceptance only**. It does not accept an implementation, native qualification, full admission-sink coverage, protected-store provisioning, publication or production activation. The separate implementation handoff is planning input, not implementation authorization.

## 2. Terra High wording correction (F1)

Terra High recorded one MINOR finding, **F1 - overbroad scan-completion race wording**, against the proposed owner release-generation contract.

The rejected wording was:

> A generation switch observable before the final owner scan completes prevents a new physical RELEASE.

That wording is not accepted. A verifier that performs protected filesystem reads is not atomically synchronized with every root-controlled writer. A switch after the verifier's last relevant stable observation can escape detection even if the scan has not yet returned; temporal overlap with an in-progress scan does not establish detection.

The canonical accepted wording is:

> A mismatch actually observed by a successful fresh owner verification refuses a new physical RELEASE. A generation switch after the verifier's last relevant stable observation - including after that observation but before the verifier returns - may escape detection and remains within the accepted trusted-root observation-to-effect residual. Mere temporal overlap with an in-progress scan does not establish detection. No stronger lock spanning the physical write or atomicity with root mutation is promised by this decision.

This correction supersedes only the quoted overbroad sentence. It does not weaken any refusal, stop, deadline or sole-writer requirement, and it does not require a new lock. Native tests must treat a post-observation switch as an accepted residual, not as a promised detection case.

## 3. Accepted contract semantics

### 3.1 Owner-side manifest authorization

The normal qualified owner path must retain the actual verified `LoadedInstallation` from its first owner-local preflight, including the full `InstallationIdentity`, qualification ID and raw qualification-record hash. A later echo or request cannot replace that first tuple.

Before granting new qualified execution authority, the owner must:

- reuse the protected `VerifyFinalAdmission()` and `IsFinalAdmissionEligible(...)` decision for the retained selection;
- require the actual S2 selection plus the private protected final-admission grant, not a controller assertion, bare hash, profile flag, successful Start or caller-supplied Boolean;
- repeat the protected final-admission check for the operation-bound manifest acceptance;
- keep S2 `Qualified`, final admission, validation and production activation as distinct outcomes;
- refuse missing, malformed, unavailable, stale, replaced or mismatched authority before new allocation, staging, capability minting or HELD;
- preserve the distinct validation context, which never converts to general admission;
- preserve the existing legacy v1 experimental behavior within its accepted limits, without treating it as an installed-admission fallback.

The owner's checks are mandatory at owner entry and at manifest commitment. Owner checks alone do not close the complete pre-effect sink inventory.

### 3.2 Owner-side pre-RELEASE generation gate

For a new physical RELEASE attempt, the accepted order is:

1. Authenticate the original request and exact capability identity and preserve the existing correlation, deadline and prerequisite checks.
2. Preserve the existing retained-decision, intent and ambiguity handling. A retained duplicate remains observation only and cannot create a second attempt.
3. After successful durable intent and the existing outside-lock after-intent barrier, perform the fresh owner generation verification outside the runtime gate, journal gate, safety latches and capability-consumption lock.
4. Immediately enter the existing final runtime gate and recheck retained result, stop, deadlines, prerequisites and capability liveness.
5. Consume the locally verified result only for this invocation, consume the capability once, invoke the one existing writer and persist the delivery observation through the existing path.

The fresh comparison must use the owner's retained selection and the full qualified identity plus qualification ID and raw record hash. It must not be cached across requests, supplied by the controller or reconstructed from disk.

For normal and postpublication validation, the check is the existing S2 Qualified identity plus the original qualification ID/hash. For approved prepublication validation, it is the separate original integrity projection with full platform, host, payload and loaded-role checks and the approved null qualification association. A prepublication check must not synthesize `Qualified` or reuse the qualified gate with a forged object.

A refusal prevents a new physical attempt, submits the existing stop path under the original context and C3 authority, and preserves the conservative post-intent result. It must not restore the capability, adopt the observed generation, renew a deadline, create a new writer, retry the write, or require a new controller request to make the owner safe.

The controller-side Q10 check remains additional and unchanged. This decision does not claim stronger global synchronization than the corrected wording in section 2.

### 3.3 Prevalidation owner scan and the accepted residual

Prepublication validation may use the owner-side scan only with:

- the separately authenticated prepublication validation context;
- the original installed-integrity selection, not a synthetic qualified object;
- full fixed-root integrity, active selection, manifest, payload and loaded-role verification;
- the exact approved case policy and finite fixture;
- the original session, execution, lease and actor bindings;
- bounded waits, original earlier deadlines and live stop servicing.

A change of phase, receipt or active identity refuses. It does not upgrade a running prepublication session to postpublication authority. After an S2 publication changes the receipt, a new independently approved postpublication invocation is required.

The accepted root observation-to-effect residual applies exactly as corrected in section 2.

### 3.4 Validation-only early direct authentication

The owner approves the **validation-only direct-context-before-writer sequencing amendment** and no broader early grant.

The accepted sequencing is:

1. The controller crosses accepted B2 consumption and action 0. Recovery independently verifies INIT, catalog and policy before its existing intent/journal effects and single owner spawn.
2. The owner validates inherited INIT, the original recovery identity, local protected inputs and the actual installed role/integrity, then sends the context acknowledgement and receives the existing Start specification. The owner does **not** acquire a journal writer yet.
3. Recovery sends the existing `DirectBootstrap` with the existing original-controller pidfd and safety-lifeline descriptors after Start and before waiting for `WriterReady`. The owner accepts the transfer and arms its existing safety monitor before authentication waits.
4. The owner sends the existing `DirectRendezvous`. Recovery forwards the original rendezvous and owner pidfd to the controller without waiting for `WriterReady`. The controller and owner complete GID1 and the dedicated GAVA direct-context exchange with the original pins. Recovery receives no owner secret or RELEASE capability.
5. Only after matching direct context and the phase-specific installed checks may the owner enter `RunTransaction`, acquire and prove the original writer, send the actual `WriterReady` and receive `StartAuthority`.

Early direct authentication is not manifest authority, staging authority, HELD authority or RELEASE authority. No later manifest, staging, HELD or RELEASE action is authorized by successful authentication alone. Every subsequent validation action requires the independent live root authorization, the exact session and generation binding, and the approved bounded test case.

The amendment changes sequencing for authenticated validation sessions only. It does not change payload shapes, descriptor roles or ordinary v1/v2 behavior. Recovery and owner must change atomically; mixed old/new validation actors must refuse under bounded deadlines with no fallback, retry, second spawn or second owner.

### 3.5 Stopping, reconciliation and compatibility

Already admitted operations retain stopping, reaping, retained-state reconciliation, journal finalization and cleanup under their original authority after final-admission withdrawal, validation expiry or loss, active-generation change, controller loss or administrative withdrawal.

A new admission read, a new general-admission grant, an administrative lock or a validation driver must not be required to perform those duties. Administrative locks must not block stopping or reaping. Missing or uncertain cleanup evidence remains `Unknown`, never a fabricated success.

Legacy v1 experimental behavior remains within its existing bounds. It cannot carry installed identity, obtain normal qualified admission or accept validation through a false flag. A v2 installed association requires installed authority even when `Profile.QualifiedInstallation=false`, and verifier failure never falls back to legacy.

## 4. Evidence binding

The independent review package is `C:\Users\Laszlos\source\repos\Solo\gagamba-integrated-start-authority-owner-decision-q1`. Its `SHA256SUMS.txt` has SHA-256 `e23f2ab14add2d0e0d31a6eef21f174bb407d3d8c188bb992a7fc93696f5cfca`; all 5 listed entries were verified for this decision.

The owner preparation package is `C:\Users\Laszlos\source\repos\Solo\gagamba-integrated-start-authority-owner-decision-01`. Its `SHA256SUMS.txt` has SHA-256 `ceac3f3db543c3de7833782178f28687778aeb6d804384d7e5c7bd9e18086686`; all 9 listed entries were verified for this decision.

Key reviewed inputs:

| Input | SHA-256 |
|---|---|
| Independent review report | `4390ba589be5fe9eb34d66a9316dac01f2c9df5f418cfba7baa7767df7108ad6` |
| Terra F1 contract findings | `a89a9e1faf99ed61475dca0858e280797854d88b054dc072b7c89107b5d7cf28` |
| Owner recommendation | `0cd9d6bc790abf8e4317c4348f179fab8b16cc127b3cf9882d4bad4989b5eb69` |
| Implementation feasibility | `e74d59839fd30737831cc476bdc532402a5b33e2096528f7eb66d7146879af2a` |
| Validation bootstrap assessment | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` |
| Owner decision proposal | `d77839f6399fe3a34cd35c5579398cfae9a1c97a80f4ecb7e51945304be4d6ef` |
| Owner manifest authorization contract | `718e29d97f533b0cd693f970c1aba6c1087bbf9a3beb39e3d88604ed3855ec70` |
| Owner RELEASE generation contract | `27c853422fbd7ca47345d8f2f6e91826cfaf4506ef953214553b3e127c5b24bf` |
| Validation bootstrap compatibility | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` |
| Implementation and test handoff | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` |
| Authority boundary comparison | `bf37d680cc1cc78981c0f7905c8967e6c3d9ad09579113cd3115708bbf34feac` |
| Source evidence and enforcement index | `a6ffdc8fb26dafe1e3d26ba43b213d7f0477646ce4c3b2c7824d5d8d573b21b6` |
| Verification receipt | `43dd7431a4694f5be9cf621c59cf80dbb26107bf790884065e50cde58e3f8c78` |

The earlier integrated Start investigation package remains `C:\Users\Laszlos\source\repos\Solo\gagamba-integrated-start-authority-01`; its manifest entries were checked by the independent review (19/19) and were not rewritten here.

This decision was prepared by read-only source and document inspection plus SHA-256 verification. No Gagamba build, test, native run, protected provisioning, publication or target execution was performed for it.

## 5. Qualifications and non-claims

- **Design acceptance only.** No implementation is accepted or authorized by this decision.
- **Native qualification pending.** Protected-root owner readability, stable protected reads, actual loaded closures, privilege drop and custody, protected-store positives, generation races, descriptor failures and lifecycle regressions remain unqualified. Skipped or missing cases are not passes.
- **Full sink coverage open.** The owner checks do not close Q04/S09. Complete pre-effect admission coverage of Start, recovery spawn, operation construction, helper spawn, journal effects, staging and release remains required before any bootstrap-complete or production claim.
- **Validation bootstrap not implemented.** The independent issuer, fixed launcher, protected catalog/approval chain, GAVA1, v3 prepublication parser, protected readers, original-ID preservation, writer coordination and driver remain accepted design dependencies, not existing authority.
- **No production activation.** `RestrictedExecutionRuntime.Create()` remains unavailable. No approval record, protected store, host identity, package or publication is created.
- **Trusted-root bounds.** Trusted host root, kernel, filesystem and the original reviewed topology are premises. This decision does not claim resistance to malicious root, hostile same-UID memory access or cross-boot rollback.
- **Residual preserved.** The corrected observation-to-effect residual in section 2 is accepted, not waived or hidden.
- **No new issuer or writer.** Recovery remains stop-only. The owner remains the sole physical RELEASE writer. No new durable schema, correlation reset or retry protocol is introduced.

## 6. Preservation and change scope

- Work remains on canonical `main`.
- No branch, linked worktree, reset, rebase, stash, clean, prune, force operation or push is authorized.
- The 44 historical untracked paths remain unchanged. The baseline `git status --porcelain -uall` list contained exactly 44 paths, sorted-list SHA-256 `2e404f7fbc30e10e58810a98d2bc78b4706ae143ff5a45ba5fdeae6d2562ec08`.
- Existing evidence, reviews, prior owner decisions and reviewer bytes remain unchanged.
- This decision modifies documentation only. No production source or test code is changed.

## 7. Disposition and next gate

**ALTERNATIVE B ACCEPTED AS DESIGN WITH EXPLICIT QUALIFICATIONS. DESIGN ACCEPTANCE ONLY - IMPLEMENTATION NOT AUTHORIZED BY THIS DECISION.**

The separate [bounded implementation handoff](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) identifies the refusal-only slices that may safely precede the atomic validation-bootstrap integration, the dependencies that must remain atomic, the required test classes and the native qualification gate. It is planning input only. Implementation, even refusal-only implementation, requires a separate explicit implementation authorization.
