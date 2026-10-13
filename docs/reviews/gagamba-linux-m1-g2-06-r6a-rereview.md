# Gagamba Linux M1 G2-06 R6A rereview (preserved)

- review type: independent G2-06 (WP02) rereview (before WP04)
- implementation under test: e5f90ce4c3c6a239775b4af72e26e077b292e6d5
- docs-only HEAD at review time: 6d6296a
- branch: qualification/ar7-feasibility
- disposition: G2-06 RESOLVED - R6A ACCEPTED
- resolved: G2-06 (G2-01, G2-07 and G2-09 were already resolved by the preserved rereview)
- still blocking: G2-02, G2-03, G2-04, G2-05, G2-08, G2-10
- nonblocking: G2-11 (evidence correction/maintenance)
- source: supplied verbatim in the authoring handoff
- G2 overall remains NOT ACCEPTED and WP04 remains blocked. The body below is the report verbatim.

Independent G2-06 Rereview (R6A)
- Scope: G2-06 / WP02 only. No repository modification, no fixes, no WP04, no unrelated findings reviewed.
- Frozen revision: implementation e5f90ce4c3c6a239775b4af72e26e077b292e6d5 (HEAD 6d6296a is docs-only over it; tree verified clean before and after).
- Baseline: preserved original G2 review and preserved G2 rereview (docs/reviews/gagamba-linux-m1-g2-rereview.md); author evidence consulted only after source inspection.
Disposition
Resolved
Source assessment (from source, not author prose)
Preparation/PreparedClosure.cs — the closure owns an enforceable lifecycle, ClosureOwnership { BuilderOwned, StoreOwned, Released }, guarded by a private readonly object _gate:
- TryTransferToStore() performs the single BuilderOwned→StoreOwned transition under the closure lock and returns false for any other state. It is the only path into StoreOwned.
- Builder Dispose() acts only on BuilderOwned (transition to Released, handles closed outside the lock); on StoreOwned/Released it is a no-op — stale builder disposal cannot close store-owned handles.
- ReleaseFromStore() (store-side invalidation/abort) performs one atomic transition to Released and closes handles exactly once; repeat calls are harmless.
- DiscardAccounted refuses unless BuilderOwned. DisposeHandles() is private with exactly two call sites (the two guarded transitions above) — a grep confirms no other production code closes staged handles or inserts store entries (single _entries.Add in Mint).
- No transition out of Released or StoreOwned-to-BuilderOwned exists anywhere.
Preparation/PreparationStore.cs — Mint holds a store _gate across validation → transfer → publication:
- All fallible checks (issuer, generation, digest, expiry, preparation-ID uniqueness) precede the irreversible transfer, so refusal is side-effect free.
- TryTransferToStore() refusal throws ArgumentException; no entry is created, the existing owner's closure is untouched.
- Lock order is store→closure on every path (Mint, borrow, consume, lookup, invalidate, abort; capability Dispose→Invalidate→ReleaseClosure follows the same order). No path acquires closure-then-store, so no deadlock cycle exists. Nothing after a successful transfer can throw (duplicate pre-checked under the same lock), so ownership cannot strand and no rollback is needed.
- TryBorrowClosure requires entry match by reference, non-aborted, non-null closure, and AuthorityLive (defense in depth).
Preparation/PreparedTransaction.cs — unchanged: private constructor, reference-identity borrow delegation, Dispose→Invalidate. Prevention is structural in the closure/store objects, not caller convention.
Independent reproduction
Probe built outside the repo (/tmp-equivalent scratch; repo tree confirmed clean): real 128-byte tool copied/bound through the actual SafeCopyIn.Prepare + LibcCopyHost on Linux (test-assembly helpers used via reflection solely to build the manifest — G1-accepted ground, not the review subject). Exact commands: dotnet build the M1 test project, build the probe, run the probe binary.
Old counterexample, re-executed: publish A → stale builder Dispose (handles stay open) → publish same exact closure as B with an otherwise valid distinct preparation ID → invalidate A. Observed:
secondPublished=False (old: True); no B capability exists; A lookup/borrow/consume succeed on live handles; after invalidating A, handles closed, lookup/borrow refuse. The old secondPublished=True … consume=True state is unreachable.
Further adversarial results, all passing: second Mint with distinct preparation ID refuses (ArgumentException, no entry created, first unaffected); same-identity second Mint refuses with ownership undamaged; digest-invalid Mint refuses pre-transfer and a later valid Mint succeeds; released-closure Mint refuses; stale builder Dispose is a no-op; duplicate invalidation/consume behave per existing semantics; cross-store Mint of the same claimed closure refuses.
Concurrency: 32 concurrent valid direct Mints of one closure → exactly 1 winner, 31 ArgumentException refusals, winner fully usable (lookup/borrow/live handles). Mint-vs-Dispose over 200 iterations → 161/39 split with zero inconsistent end states ( Mint-won ⇒ entry present and handles live; Dispose-won ⇒ no entry and handles closed). Mint-vs-Invalidate: 50/50 concurrent second Mints refused during release; handles closed once, lookup refuses.
Regression assessment
- G1 semantics intact: reference-owned identity, issuer/generation/digest/expiry checks, single-use consume, and the canonical vector (873 bytes, SHA-256 82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3) are untouched.
- G2-07 sampled, no regression: NativeStagingCleanupTests, StagingCleanupTests, CapabilityClosureTests all pass alongside the new ClosureOwnershipTests.
- Filtered suite run on Linux: 85 passed, 0 failed (CanonicalGolden, PreparationIdentityTests, StateModelTests, staging-cleanup, capability-closure, closure-ownership).
No remaining G2-06 defect found; no findings section required. Unrelated G2 blockers (G2-02/03/04/05/08/10, G2-11) were not re-examined.
G2-06 RESOLVED - R6A ACCEPTED
