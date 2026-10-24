# Gagamba RC-1: Restricted Execution Consumer Contract

**Assigned model:** Astra
**Role:** Gagamba architecture and API owner
**Task:** Resolve Hufu AE4-B01 integration blocker
**Primary repository:** Gagamba
**Consumer repository:** Penghou.Hufu
**Independent reviewer:** GLM
**Current authorization:** Contract design, source inspection, documentation and implementation planning
**Production code changes:** Defer until contract review and acceptance
**Public NuGet publication:** Not authorized

## 1. Objective

Resolve the missing Gagamba restricted-execution consumer contract identified by Sol during Hufu AE-4 implementation.

Stop finding:

`AE4-B01 — restricted Gagamba consumer contract is not an accepted dependency`

The current public `IExecutionProvider` provides lifecycle execution capabilities but cannot support Hufu's required restricted-execution integration.

Gagamba already has internal restricted-execution manifests, canonical encoding, preparation identities, release arbitration and owner machinery. These must not be accessed through reflection, copied implementation, friend-assembly shortcuts or an invented compatibility facade.

**Design the smallest supported Gagamba-owned external contract that permits Hufu to consume restricted execution correctly.**

The first deliverable is a reviewable contract. Implementation follows independent review.

## 2. Starting evidence

### Gagamba

Repository:

`C:\Users\Laszlos\source\repos\Gagamba`

Known C1 implementation baseline:

`629a08d2ee82a9ac084d87ed73ad8d584661ea28`

C2 preservation:

`6a60d2fc490acee18aa161b847a34343144c5b26`

D6.7 approved the C1–C4 contracts and selected C3 Policy A.

C2 has independently reviewed WSL2 component evidence.

C1 has producer evidence at the supplied baseline; independent implementation review was outstanding at Sol's last check.

C3/C4 implementation and wider Gagamba owner qualification remain separate gates.

Inspect the actual current worktrees and remote branches before selecting a design baseline. Do not assume these remain the latest revisions.

### Hufu

Sol inspected Hufu at:

`236948509ee2e608e2e9a8461fabc379e037b65b`

The relevant consumer is:

`src/Penghou.Hufu.Sandbox`

It currently uses `Gagamba.Runtime 0.1.0-preview.4` and the public lifecycle-oriented `IExecutionProvider`.

Read the Hufu AE-4 integration design, stage register, implementation plan and Sol's AE4-B01 entry-check report.

**Hufu is a consumer for this task. Do not modify Hufu production code.**

## 3. Existing Gagamba components

Inspect the actual code, especially:

- `RestrictedManifest`
- `ManifestCodec`
- `PreparationIdentity`
- `PreparedTransaction`
- `OwnerPreparationTransaction`
- `ReleaseArbiter`
- Restricted execution result contracts
- Linux M1 owner and runtime
- Public `IExecutionProvider`
- WP05 C1–C4 decisions, tests and evidence
- WP06–WP12 plans and current qualification gates

Determine which components are:

1. Internal implementation details.
2. Reusable immutable value contracts.
3. Provider-owned control operations.
4. Qualification-only fixtures.
5. Suitable for a supported external API.

Do not make internal types public merely because a consumer needs some of their information.

## 4. Contract architecture

Separate the external integration into two boundaries.

### RC-1A: Public value and codec contract

This is the immediate prerequisite for Hufu AE4-D4a.

Define a stable, externally consumable way to inspect and verify an immutable restricted-execution manifest.

The contract must support:

- Manifest schema/version identity.
- Exact canonical encoding.
- Digest verification.
- Effective execution facts.
- Restricted-profile qualification states.
- Compatibility checking.
- Preparation identity correlation without exposing a live release capability.

Determine whether the existing manifest model can be safely exposed or should have a carefully defined immutable public representation.

Prefer a single authoritative encoding and validation implementation.

Do not create another Gagamba manifest or a separate Hufu codec.

The exposed contract must distinguish an intended or prepared restriction from a restriction actually established and verified by the provider.

### RC-1B: Restricted execution control facade

This is a later dependency for Hufu AE4-D4b.

Design, but do not implement yet, the supported control interface for:

- Bounded preparation without target initialization.
- Immutable prepared-manifest inspection.
- Exact preparation/capability association.
- Single-use release under accepted authority.
- Stop/cancellation.
- Runtime and outcome observation.
- Capture and cleanup status.
- Evidence-only recovery and reconciliation.

The facade must preserve Gagamba's ownership of native process controls and release arbitration.

External consumers must not receive internal capabilities that allow them to bypass release admission.

Do not implement the runtime facade simply to unblock D4a.

## 5. Required architectural decisions

For each decision, document the current implementation, proposed contract, alternatives, risks, affected files and required tests.

### RC-D01: Public contract ownership

Determine the appropriate assembly/package boundary.

Options include:

- A public contract in the existing Gagamba.Execution assembly.
- A narrowly scoped additional Gagamba contract assembly.
- Another source-supported arrangement.

Avoid a new package unless there is a demonstrated compatibility or dependency reason.

Gagamba must remain independent of Hufu.

### RC-D02: Immutable manifest representation

Specify exactly which fields a consumer can inspect and which remain implementation-private.

Include executable/runtime closure, invocation, filesystem resources, limits, channels, provider identity and qualification state.

Preserve Gagamba's existing canonical field semantics.

Do not expose mutable internal collections or caller-controlled authority fields.

### RC-D03: Codec and versioning

Define:

- Supported schema versions.
- Encoding and decoding operations.
- Strict validation.
- Digest computation and verification.
- Unknown-version refusal.
- Canonical round-trip invariants.
- Compatibility and migration rules.
- Test vectors and byte-exactness evidence.

A consumer must never have to reimplement Gagamba's binary codec.

Reject ambiguous or noncanonical encodings.

### RC-D04: Preparation identity

Define how a consumer binds a manifest to:

- The actual prepared execution.
- Issuer and generation.
- Provider and implementation identity.
- Exact prepared objects.
- The relevant source/native artifact version.

Distinguish identity from authority.

A preparation ID or manifest hash alone must never permit release.

### RC-D05: External authorization boundary

Gagamba should not implement Hufu policy.

The contract must allow a trusted host to inspect the exact effective execution facts and authorize them without modifying the prepared execution.

Define how authorization is bound to the same immutable preparation subsequently considered for release.

Determine whether additional opaque approval binding is necessary and which component owns it.

Do not add a generic policy engine to Gagamba.

### RC-D06: Release and stop semantics

Preserve approved C1–C4 behavior.

In particular:

- First accepted stop consumes release capability.
- Ambiguous ReleaseIntent persists as `MayHaveExecuted`.
- Duplicate and concurrent loser outcomes remain distinct.
- C3 Policy A deadlines and recovery behavior remain authoritative.
- Failed persistence does not restore consumed authority.
- Uncertain execution is not automatically replayed.

Specify which facts can be exposed to consumers without leaking the internal release mechanism.

Do not reopen already approved contracts without a demonstrated contradiction.

### RC-D07: Result and evidence types

Design consumer-visible representations that preserve:

- Release disposition.
- Delivery and exec observations.
- Execution outcome.
- Root/helper results.
- Stop acceptance and completion.
- Domain emptiness.
- Capture completeness and truncation.
- Cleanup success, failure or uncertainty.

Do not collapse these outcomes into one success boolean.

Separate execution facts from Hufu authorization and output-disclosure decisions.

### RC-D08: Restricted versus lifecycle providers

Define how consumers reliably distinguish supported restricted execution from ordinary lifecycle management.

A confinement-required request must fail before target initialization when the selected provider lacks the qualified capability.

Never allow fallback to `IExecutionProvider.Launch` as an equivalent implementation.

Windows/macOS lifecycle support must not be advertised as the current Linux restricted profile.

### RC-D09: Packaging and native artifact identity

Determine how the future contract can be consumed through a supported .NET package.

Include:

- Target frameworks.
- Package identities and dependency closure.
- Native artifact/version binding.
- Codec compatibility.
- Test-only versus production assembly selection.
- Clean-consumer verification.
- Unsupported-platform refusal.

Do not publish a package in this assignment.

## 6. Acceptance tests to design

Provide deterministic tests for the public value/codec contract.

At minimum:

| ID | Required result |
|---|---|
| RC-T01 | Canonical manifest round-trip preserves exact values |
| RC-T02 | Equivalent accepted construction produces identical canonical bytes |
| RC-T03 | A changed execution field changes the canonical identity |
| RC-T04 | Unsupported schema/version refuses |
| RC-T05 | Malformed or noncanonical binary input refuses |
| RC-T06 | Null, empty, ordering and duplicate semantics are preserved |
| RC-T07 | Public values cannot mutate an accepted preparation |
| RC-T08 | Manifest substitution cannot preserve the accepted preparation binding |
| RC-T09 | Identity alone cannot authorize release |
| RC-T10 | Pending mandatory qualification is not represented as established |
| RC-T11 | Ordinary lifecycle providers cannot satisfy restricted contracts |
| RC-T12 | Package consumer can validate the manifest without internal access |
| RC-T13 | Native artifact/version mismatch is refused when required |
| RC-T14 | Consumer-visible result types preserve uncertainty |

For each case specify whether it requires only public value contracts, internal deterministic tests or later native qualification.

No native execution is required to accept the value/codec design.

Do not treat value/codec tests as proof of restricted OS enforcement.

## 7. Implementation plan

Design two independently reviewable implementation stages.

### RC-2A: Public value/codec implementation

This should be the smallest code tranche that can resolve the contract portion of AE4-B01.

It may include:

- Accepted immutable public value types.
- Canonical codec accessibility.
- Strict validation and compatibility checks.
- Deterministic contract tests.
- Negative misuse tests.
- Local NuGet package qualification.
- Clean .NET consumer smoke tests.

Do not implement a native launch/control facade.

Do not change existing C1–C4 release semantics.

### RC-2B: Restricted provider control facade

This later stage requires:

- An accepted external control interface.
- Accepted relevant C1–C4 implementations.
- Actual owner/runtime support.
- Appropriate Gagamba qualification gates.
- A separately approved implementation assignment.

It must remain clearly separated from RC-2A.

### Hufu resumption

Once RC-2A has been independently reviewed and accepted at an exact source/version, Sol may reassess AE4-B01 and request authorization to resume AE4-D4a.

The actual Hufu restricted host adapter, AE4-D4b, remains gated on RC-2B and its independent prerequisites.

Do not declare AE4-D4b ready merely because the value/codec package exists.

## 8. Design deliverables

Create documentation in Gagamba using existing repository conventions.

Suggested documents:

**1. `docs/design/restricted-consumer-contract-rc1.md`**

Complete architecture, selected API contract, value/codec semantics, authority boundary, compatibility and security invariants.

**2. `docs/plans/restricted-consumer-contract-implementation.md`**

Bounded RC-2A and RC-2B work orders with exact prerequisites, file scope, tests, exit criteria and stop conditions.

**3. `docs/reviews/restricted-consumer-contract-rc1-handoff.md`**

Exact Git/source binding, findings, decisions, qualification limitations and independent review instructions.

Use existing documentation directories if they differ from these suggestions.

Update the Gagamba roadmap/work queue with the new dependency and truthful status.

Add a reference to Hufu AE4-B01 without modifying Hufu.

Preserve previous Gagamba reviews and qualification evidence.

## 9. Independent GLM review

After the documentation is committed, stop.

GLM should independently review:

1. Whether the proposed contract exposes sufficient information for Hufu AE4-D4a.
2. Whether the selected public types preserve canonical identity and immutability.
3. Whether the codec contract can be implemented without duplicating the binary encoding.
4. Whether the API leaks release authority or native control capabilities.
5. Whether C1–C4 guarantees and uncertainty are preserved.
6. Whether a lifecycle provider can accidentally satisfy restricted admission.
7. Whether the package/version contract is precise enough for an independent consumer.
8. Whether RC-2A can be implemented without assuming RC-2B already exists.
9. Whether the proposed interface fits current Gagamba owner architecture.
10. Whether the test inventory is sufficient and has no unsupported security claims.

GLM may recommend:

- Accept.
- Accept with qualifications.
- Amend.
- Reject.

Preserve findings and exact review-source binding.

Astra must not self-certify its design as independently accepted.

## 10. Git and execution requirements

Start from the verified current Gagamba restricted-execution development baseline.

Use a dedicated isolated worktree.

Suggested branch:

`design/gagamba-rc1-restricted-consumer-contract`

Do not modify Gagamba main directly.

Do not disturb C1/C2 review or preservation branches.

Do not reset existing worktrees.

Record the exact:

- Branch.
- Parent commit.
- Source/test trees.
- Changed paths.
- Document hashes.
- Validation commands.
- Final commit and tree.

This assignment authorizes documentation changes only.

Do not modify source code, tests, project references or package metadata.

Do not run native workloads.

Do not push, merge, tag or publish.

Finish with a clean worktree and a committed, reviewable design.

## 11. Reporting contract

Provide:

**A. Git binding:** Inspected Gagamba/Hufu commits and design branch.

**B. Existing capability inventory:** What can be reused.

**C. Missing contract:** What AE4-B01 specifically requires.

**D. Public value/codec design:** Types, canonical behavior, validation and compatibility.

**E. Restricted facade proposal:** Interface responsibilities and internal authority protection.

**F. Security boundaries:** Hufu/Gagamba ownership and refusal behavior.

**G. Test plan:** RC-T01–T14 and additional justified cases.

**H. Implementation plan:** RC-2A versus RC-2B, with exact dependencies.

**I. Hufu resumption criteria:** Evidence needed to revisit AE4-B01 and AE4-D4a.

**J. GLM review handoff:** Exact review questions and source binding.

**K. Final disposition:** Design readiness, implementation prerequisites and qualification status separately.

Use one of:

- `RC-1 DESIGN READY FOR INDEPENDENT REVIEW`
- `RC-1 DESIGN READY WITH QUALIFICATIONS`
- `RC-1 OWNER CONTRACT DECISION REQUIRED`
- `RC-1 DESIGN BLOCKED BY MISSING SOURCE EVIDENCE`

**Stop after committing the design documents. Do not begin RC-2A implementation or change Hufu until the Gagamba consumer contract has been independently reviewed and accepted.**
