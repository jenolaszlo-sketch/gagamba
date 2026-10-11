# Gagamba Linux M1 G1 independent review

**Review type:** independent G1 review
**Reviewed revision:** e2c2f119d260eccf8f2b10b2c3d983f2c6da0eeb
**Execution by reviewer:** portable build/tests only; no native sandbox probes
**Disposition:** G1 NOT ACCEPTED - WP01 CORRECTION REQUIRED

---

**WP01 is not ready for G1 acceptance.** It respects the authorized implementation boundary, but its canonical identity and state/result contracts do not fully satisfy ADR 0003 and the approved plan.

Reviewed revision: `e2c2f119d260eccf8f2b10b2c3d983f2c6da0eeb`, including the complete diff from `9b9264d` and correction `6f73ed8`. No repository changes, fixes, WP02 work, or native sandbox probes were performed.

**Findings**

1. **G1-01 — Major — Canonical collection ordering is incorrect.**
   *Classification: implementation defect.*
   Affected: [RestrictedManifest.cs](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution/Restricted/RestrictedManifest.cs:163).

   Collections use `StringComparer.Ordinal`, which compares UTF-16 code units, rather than the required UTF-8 bytes. An executable reproduction sorted U+10000 before U+E000, producing `F0908080, EE8080`; UTF-8 byte order requires the reverse. The codec comment claiming equivalence is incorrect.

   Helper dependencies also retain caller order and accept duplicates. Reversing an otherwise identical dependency inventory changed the digest.

   **Required correction:** use the specified UTF-8 byte ordering consistently; canonicalize and reject duplicates in dependency inventories. Add supplementary-Unicode and permutation tests.

2. **G1-02 — Major — Required manifest and establishment semantics remain incomplete.**
   *Classification: implementation defect.*
   Affected: [RestrictedManifest.cs](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution/Restricted/RestrictedManifest.cs:333) and terminal manifest fields.

   The resource model contains 13 budgets but lacks representation for Section 11’s core-dump and owner-allocation/log budgets. Authority has no explicit production no-IP qualification disposition or executable-mount-policy representation. These required semantic distinctions consequently cannot participate in canonical identity.

   `PublishTerminal` assigns the prepared manifest to both requested and established fields. An immutable requested value is not establishment evidence; the report provides no separate establishment shape.

   **Required correction:** complete the required semantic fields and their canonical encoding. Keep establishment explicitly absent/Unknown until a distinct immutable report supplies observations. Native readback implementation remains outside WP01.

3. **G1-03 — Major — Equivalent authorization identities are rejected.**
   *Classification: implementation defect.*
   Affected: [TransactionStateModel.cs:209](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs:209).

   Authorization uses record equality on `PreparationIdentity`. Its `ImmutableArray<byte>` digest compares backing-array identity rather than digest contents. Reconstructing an identity with identical UUIDs and copied digest bytes produced `Foreign`.

   This conflicts with the planned description/authorization exchange, which returns identity values to resolve an existing capability.

   **Required correction:** compare authorization UUIDs and digest contents explicitly. Preserve the store’s separate reference-identity check on the actual capability.

4. **G1-04 — Blocking — Observation and outcome rules permit contradictory execution history.**
   *Classification: implementation defect.*
   Affected: [TransactionStateModel.cs:296](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs:296).

   Executable reproductions established that:

   - A recorded root exit of 17 can be overwritten by exit 0, yielding `Succeeded`.
   - A known root result can be overwritten by `Unknown`.
   - An accepted post-commit `OutputOverflow` stop can still yield `Succeeded` when the root subsequently exits 0.
   - Helper and capture observations are refused during held/pre-release teardown because their shared guard requires a committed release, although those observations exist independently of target execution.

   These are incompatible with Sections 6/13 and field-wise preservation of knowledge.

   **Required correction:** enforce legal observation progression and intentional duplicate/conflict handling; admit helper/capture observations in their applicable pre-release states; apply stop/enforcement semantics without erasing known root status or reporting successful execution incorrectly.

5. **G1-05 — Blocking — Terminal observations are mutable and lose recorded information.**
   *Classification: implementation defect.*
   Affected: [RestrictedTransaction.cs:281](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution/Restricted/RestrictedTransaction.cs:281) and [TransactionStateModel.cs:442](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs:442).

   `TransactionObservation` retains caller-owned `IReadOnlyList` references. Model-produced observations expose ordinary arrays through those interfaces. Casting `TransitionNames` back to `string[]` and changing an element changed the already-published terminal observation.

   Separately, `AcceptStop` clears `CurrentCapability`; terminal publication then loses preparation identity and manifests. The terminal contract also omits recorded exec errno and later stop causes.

   **Required correction:** publish genuinely immutable collection snapshots; retain immutable identity/manifest metadata independently of live capability validity; preserve the recorded result and stop information in the terminal value.

6. **G1-06 — Major — Cleanup can claim confirmation without evidence.**
   *Classification: implementation defect.*
   Affected: [RestrictedTransaction.cs:191](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution/Restricted/RestrictedTransaction.cs:191).

   `new CleanupReport(CleanupState.Confirmed, null, null, null, null)` succeeds. Thus the contract can represent confirmed cleanup with every component unknown, contrary to Section 5.

   **Required correction:** derive or validate the aggregate cleanup state against required components. Leaving model-generated cleanup Unknown while later mechanisms remain unimplemented is acceptable.

7. **G1-07 — Major — Required executable coverage is incomplete.**
   *Classification: missing/weak tests.*
   Affected: [ManifestTests.cs](C:/Users/Laszlos/source/repos/Gagamba/tests/Gagamba.Execution.Linux.M1.Tests/Contracts/ManifestTests.cs:85), preparation tests and state tests.

   The enum audit found **63 members, 59 numerically pinned**. `ReleaseSendResult.Delivered`, `WouldBlock`, `BrokenPipe` and `Partial` remain unpinned by executable numeric assertions after `6f73ed8`.

   Additional coverage gaps:

   - The mutation test changes a digest array never supplied to its manifest.
   - The fabricated-capability test uses different identifiers, so it does not exercise rejection of a different object carrying matching identifiers.
   - No test supplies an unsatisfied `OwnedPrerequisites`.
   - The “complete transition table” misses the contradictory observation and stop-result sequences reproduced above.
   - The cancellation test exercises a test-owned polling loop; it provides limited evidence about an eventual shared observation API.

   **Required correction:** pin the remaining enum values and add focused tests exercising the actual input objects, matching-identity foreign instances, missing prerequisites, and the corrected contract behavior. Native tests are unnecessary.

8. **G1-08 — Minor — Duplicate-release behavior contradicts the stated contract.**
   *Classification: implementation defect and evidence/docs mismatch.*
   Affected: `TryCommitRelease` and the review request’s duplicate-call limitation.

   A duplicate after successful commitment returns `InvalidStateOrder`. The request says it returns `Consumed`; Section 8 says duplicates return the original release decision without another write.

   **Required correction:** implement the specified retained-decision behavior and test it. The existing behavior prevents a second write, so this is not independently a release-safety blocker. It is not a genuine plan ambiguity.

9. **G1-09 — Editorial — Evidence/index precision needs correction.**
   *Classification: evidence/docs mismatch.*

   The actual test distribution is **11 manifest / 10 preparation / 18 state**, rather than 12/9/18. The final diff is **14 files, +2927**, including the request. `EnumNumbersPinned` is actually named `EnumNumbersArePinned`. Missing `using` cleanup: the test file retains an unused `System.Collections.Immutable` import alongside the needed `System.Text` import. Several coverage claims exceed what the tests exercise.

   Revision instructions based on an unrestricted `git log -1` become inaccurate after HEAD advances. The original Python cross-check is described but its implementation is not retained in the committed package.

   **Required correction:** update counts, mappings, limitations and explicit source binding; retain a reproducible cross-check or qualify its provenance claim.

**Canonical identity assessment**

Not adequate for WP02 to rely on yet. Fixed-width little-endian encoding, u32 lengths/counts, strict UTF-8, raw digests, argument ordering, schema identification and the RFC UUID helper are implemented coherently. Manifest collections and digest inputs are defensively copied.

I independently reproduced the exact **797-byte** vector and SHA-256:

`dee322bcada586327661c5bcbc1c9673b8837e28354838ff9a53cfef01f22530`

That validates this vector, but does not resolve ordering, omitted-field or enum-coverage defects.

**Capability assessment**

The store adequately preserves WP01’s single-threaded, in-process capability semantics: private construction, exact stored-object identity, issuer/generation/digest/expiry checks, and permanent consume/abort invalidation. Matching identifiers alone cannot bypass its reference check.

Single-threaded operation is acceptable for this pure model; WP03+ concurrency mechanics are not a G1 requirement. The authorization-value comparison in G1-03 still requires correction.

**State/result assessment**

Separate release, exec, root, helper, capture and cleanup fields are a sound foundation. Unknown is explicitly represented, and cleanup does not automatically repair execution history. Nevertheless, conflicting observations, stop-to-success behavior, mutable terminal history and lost metadata prevent acceptance.

Unfilled cleanup mechanisms are acceptable with truthful Unknown components. Reusing the requested manifest as established evidence is not an acceptable placeholder.

**Scope assessment**

WP01 stayed inside its authorized implementation boundary. The complete diff introduces the expected contracts, pure model, store, tests, evidence and additive solution wiring. No filesystem preparation, native calls, process launch, real transport, public provider registration or WP02+ mechanism was introduced.

Both new projects are non-packable. Friend assemblies and references are narrowly scoped. Existing public SPI, package versions, orchestrator, ADR, implementation plan and sealed AR-7 evidence were unchanged. No unnecessary general architectural framework was introduced.

**Test/evidence assessment**

I rebuilt the exported exact revision and independently ran:

- WP01: **39/39 passed**
- Existing execution regression suite: **20/20 passed**

The VSTest host connection timed out; direct xUnit execution completed successfully. Build-time network audit/source-link operations were disabled for the isolated copy. Native testing remains **NotApplicable**.

The passing suites establish useful baseline coverage, but are insufficient for G1 because the additional portable reproductions demonstrate defects they miss.

The minimum gate correction is confined to the WP01 identity, observation/result and cleanup contracts identified above, corresponding focused portable regressions, and accurate evidence. No native preparation, launcher, transport, concurrency implementation or broader sandbox reassessment is required.

G1 NOT ACCEPTED - WP01 CORRECTION REQUIRED
