# RC-1 independent review handoff

Date: 2026-10-24. Producer: Gagamba contract-design role (Astra assignment).
Intended independent reviewer: GLM, **not invoked by this assignment**.
Producer disposition: **RC-1 DESIGN READY WITH QUALIFICATIONS**.
Independent disposition: **PENDING**. No self-certification of acceptance.

## A. Exact Git and consumer binding

| Item | Binding |
|---|---|
| Design branch | `design/gagamba-rc1-restricted-consumer-contract` |
| Dedicated worktree | `C:\Users\Laszlos\source\repos\Solo\gagamba-rc1-contract` |
| Parent / selected restricted baseline | `629a08d2ee82a9ac084d87ed73ad8d584661ea28` |
| Unchanged Gagamba src tree | `9dd0f055eaf48d48bda8683dee3f8dc534a06e5d` |
| Unchanged Gagamba tests tree | `ebcb976a164b212090fbe0958fe4b0c0b243f794` |
| C1 implementation / tests-first | `6fcd8b5f52c7083bfba8a7139264641fc0d8da0f` / `b1825deb898fa6c2390e344d1e477c8cf0a206a3` |
| C2 preservation / independent review | `6a60d2fc490acee18aa161b847a34343144c5b26` / `89464f5f214615ba94051e438d9b4979483ae5bf` |
| D6.7 approved decisions | `01063c164391e0676ae1f6087d2c0865f7692511`; C1–C4 approved; C3 Policy A selected |
| Hufu current read-only planning evidence | `37dcf33362e8e745800a530a478fd406795c3e58` |
| Hufu earlier integration design | `236948509ee2e608e2e9a8461fabc379e037b65b` |
| Hufu unchanged src/tests trees | `2a9a9c60b833d6281f6e8528f767a7750c4411f4` / `955ca47e1d327f262dc516264fbe4bc0ac78429e` |

The exact final design commit and root tree are reported in the external completion
receipt/user response. They cannot be embedded in their own commit without a
self-reference. Resolve and pin them with `git rev-parse HEAD HEAD^{tree}` and
verify `HEAD^` equals the parent above; do not review a later moving branch tip
without recording its actual hash. All changed paths and document hashes are in
the [evidence directory](../evidence/restricted-consumer-contract-rc1/README.md).

Fresh remote and local inventories were checked before selection and retained.
Origin did not advertise C1 or later C1/C3/C4 branches at inspection; local C1
producer was newer than the remote restricted C2 line. Remote main was a separate
line. No main, review or preservation checkout was reset. The canonical Gagamba
checkout had unrelated untracked files and was not used for edits. This design
worktree was created directly from the selected parent.

## B–F. Existing capability, gap and selected contract

**B — Reuse:** one internal schema-1 encoder/model, immutable preparation identity,
private capability/store with retained closure, owner preparation transport,
arbiter/journal and recovery machinery. Result vocabulary and golden/reference
encoder tests are reusable foundations, not public or fully integrated contracts.

**C — AE4-B01:** the current public lifecycle SPI cannot give Hufu supported
restricted-manifest inspection/verification or a correctly bound control path.
Reflection, copying the codec, consumer friend access and private owner messages
are not acceptable dependencies. This is a supported-contract gap; RC-1 did not
execute a runtime defect reproduction.

**D — RC-1A:** existing net10.0 Execution package; sealed read-only projections,
passive preparation reference, exact existing schema-1 binary bytes, SHA-256 and
new bounded strict decoder. One production encoder/validation path; no public
manifest builder or native facade in RC-2A. Unknown/ambiguous input refuses.
The existing 873-byte literal golden hash was independently recalculated from
source text only, not by running Gagamba. Qualification tags are not establishment.

**E — RC-1B:** later real operation returns control before setup, binds the exact
prepared view to its owner/store, permits one physical release attempt and
independent stop/observation/capture. Independently surviving unprivileged scope,
authenticated controller lifeline and stop-only reconnect are required new work.
Records/IDs can inspect evidence but cannot recreate native control.

**F — Ownership:** Hufu owns current policy, durable admission L, dispatch latch,
authority expiry and output O; Gagamba owns native prerequisites, clocks,
arbitration, capture and cleanup facts. Optional approval digest is correlation
only. Preserve C1 ambiguity, C2 stop consumption, C4 duplicate/concurrent-loser
distinction and C3's original deadline/Policy A. No lifecycle fallback; production
offline profile and unsupported platforms continue to refuse.

## G–I. Test/work split and Hufu resumption

**G — Tests:** [test matrix](../plans/restricted-consumer-contract-implementation.md)
contains RC-T01–T14 and nine additional decoder/package/lifetime/backpressure/ACK
cases. Each specifies portable public, internal deterministic and/or later native
scope. All are designed, not run. T14 result API and full T18–T23 belong to RC-2B;
native portions of T07–T13 cannot be counted as RC-2A successes.

**H — Implementation:** RC-2A only public values/codec/reference, strict validation,
deterministic tests and exact local package/clean-consumer qualification. RC-2B
requires a separate commission, accepted relevant C1–C4 implementations and real
WP06–WP09 owner/resources/capture/recovery, followed by applicable G3/G4/G5 and
explicit exact experimental consumer authorization. These do not block passive
RC-2A implementation once its own contract/authorization gates are met.

**I — Hufu:** AE4-B01 remains OPEN. After independent RC-2A acceptance, Sol receives
exact source/API/package/vectors and reviews the Hufu dependency graph and
semantic deltas, then requests D4a authorization. Current Runtime preview plus
new Execution requires compatibility proof; no Hufu references change here.
Accepted Mode is proposed as `work`, with fixed argv `/tool`, not an extra flag.
Build/closure/core/host pins must be concrete for enrollment; synthetic vectors
only support pure D4a matching. D4b is not ready from a value package alone.

## J. GLM review instructions

Start a separate independent review from the exact completed design commit.
Read the original [commission](../evidence/restricted-consumer-contract-rc1/commission.md),
RC-1, work orders and source manifest. Inspect actual source at the recorded
revisions; do not accept the producer's summary as proof. Review documentation
only unless a separate assignment explicitly authorizes code or execution.
No native workload or package publication is needed for contract review.

Answer each question with source locations and a finding disposition:

1. Does RC-1A expose every field Hufu D4a must match, without depending on RC-2B?
   Are passive reference construction and Gagamba-owned vectors sufficient?
2. Do sealed projections, immutable copies, byte grammar and strict reader
   preserve existing canonical identity? Check all null/empty/tag/order cases,
   UTF-8 ordering and the new input ceiling against existing valid vectors.
3. Can the implementation use one canonical encoder/validator without a second
   manifest/codec? Does private owner JSON remain clearly separate?
4. Can IDs, a parsed manifest, a public factory, an approval digest or reconnect
   ever reach native release without the live original operation/store association?
5. Are C1–C4 and C3 Policy A preserved precisely, including queued versus accepted
   stop, ambiguous persistence, concurrent refusal and original deadlines?
6. Can lifecycle providers or ordinary Windows/macOS support accidentally satisfy
   restricted selection? Are pending production qualifications still blocking?
7. Are package/TFM/source/native identity requirements sufficient for an independent
   consumer, including unique local version, clean restore and Hufu's mixed graph?
8. Can RC-2A be implemented/tested without a native facade, current native artifacts
   or an owner/provider result API? Are deferred test portions explicit?
9. Does the later process/handle/lifeline/reconnect/capture contract fit current
   owner architecture? Identify missing implementation rather than assume existing
   `M1RecoveryScope` is already a supported standalone service.
10. Does the test matrix include meaningful negatives/positive controls and preserve
    uncertainty without claiming OS enforcement from pure tests?

Review qualifications explicitly: inspection-only API, 64-KiB public ceiling vs
stricter owner config, newly rejected undefined enums, mode `work`, native clock
metadata outside manifest, trusted host process boundary, proposed scope deployment
and authentication, mixed package graph, and exact experimental pins. A material
contradiction should be an Amend/Reject finding, not hidden as a future TODO.

Return **Accept**, **Accept with qualifications**, **Amend**, or **Reject**, with
exact reviewed commit/root/src/tests/native trees, inspected documents/hashes,
finding priorities, source evidence and which gates remain outstanding. Preserve
the independent report as a distinct source-bound artifact. Acceptance is design
acceptance only; do not begin RC-2A in the review task.

## K. Final producer disposition and limits

**RC-1 DESIGN READY WITH QUALIFICATIONS.** Documentation is ready for independent
review. Implementation, supported package artifact, native facade and release
qualification remain pending. C1 independent acceptance was not found in the
inspected inventories; C2's existing WSL2 evidence retains its original scope;
C3/C4 and wider owner/native qualification remain separate workstreams.

No source/test/project/package changes, native workload, package build,
publication, Hufu edits, push, merge or tag occurred. Documentation validation is
reported in the evidence, not as code-test or native-test success. Stop at the
design commit; wait for independent review and acceptance before implementation.
