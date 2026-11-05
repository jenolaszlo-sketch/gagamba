# B6-F02 Mechanism B owner acceptance

**MECHANISM B ACCEPTED WITH EXPLICIT QUALIFICATIONS — BOUNDED IMPLEMENTATION AUTHORIZED**

Date: 2026-11-05 (Asia/Manila)

Decision: `GAGAMBA-RC2B-06-B6-F02-MECHANISM-B-OWNER-ACCEPTANCE-01`

Parent preparation: `GAGAMBA-RC2B-06-B6-F02-AUTHORITY-MECHANISM-OWNER-01`

Canonical repository: `C:\Users\Laszlos\source\repos\Gagamba`, branch `main`

Canonical baseline: commit `504b6d06ac88591072b11f428ae88cade390c075`, tree `e3f028dc32c48935fb4abdc6a553a2641c960c53`

## 1. Decision

The human owner accepts the independently reviewed Mechanism B design with the explicit qualifications below. The initial Terra High review remains historically **INCOMPLETE** with its original R1/R2 findings. The focused Terra High recheck is **SUPPORTED WITH QUALIFICATIONS** and closes R1 and R2 at design level. That recheck is evidence for this owner decision, not a substitute for it.

The owner authorizes only the bounded first implementation slice described in the [implementation plan](../plans/rc2b-06-b6-f02-mechanism-b-impl-01.md): strict protected final-acceptance verification and fail-closed separation of the existing S2 Qualified state from final admission. The authorization does not activate production, create an approval record, publish a final record, provision protected stores, implement full session-bound capability, implement the installed-profile driver, extend S4/B7, or authorize a push.

## 2. Exact reviewed design binding

The accepted design is the final reviewed 15-file submission. Its frozen recheck manifest is `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82`. The focused independent recheck is `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6`.

The following exact inputs are incorporated as governing design evidence:

| Input | SHA-256 |
|---|---|
| Final-acceptance hash contract | `49d566e1bcae585d4d9566319cdbe5d6f06696dfbbd9bff6c3fda9ce7514b652` |
| Accepted-contract and qualification matrix | `d8ba4a64c48a6fbac2c9ca4245fcfb3147cca63e0c0ee6a3bcadf080d581d808` |
| Negative-test reconciliation | `9deeb26b63918cdc92b782e498ab3a6e870a0ae127f61ad474f3533f72b037ad` |
| Implementation authorization boundary | `9eb472d640335a1af259416b4afd365399977361f50e28a10b0065af6937376b` |
| Final-design binding | `5755748b27a8c11b11f6d4218a077e90ec7da09029256c8653e49b83e7224c0d` |
| Historical initial independent review | `f09bc803728514cb46f2a13de7209839355cde3cc876d353f5171c747f8d38bb` |

The owner's acceptance is of those exact design semantics and qualifications. It does not rewrite the historical PENDING draft, the initial review, the recheck, or any preserved reviewer bytes.

## 3. Mandatory qualifications

All previously prepared Q01-Q10 qualifications remain in force. In particular:

- **Q01:** Mechanism B is selected only for one independently approved live invocation/case. There is no durable one-shot consumption, historical issuance quota, global single-live-case quota, unattended standing approval, process-restart authority, or disk reconstruction of authority. Mechanism A remains unselected.
- **Q02:** Trusted host root, kernel, filesystem, and the original reviewed launcher/controller/recovery/owner topology are premises. Malicious root, copied-host rollback, and hostile same-UID memory isolation are outside the claimed guarantee. Full controller/driver closures and native behavior remain separately qualified.
- **Q03:** The exact final schema, protected independently expected final-record hash, and staged candidate/prepublication/postpublication evidence split are accepted as bounded policy amendments. Provenance is administrative, not inferred from hashes. No final publication or native post-gate pass is created by this decision.
- **Q04:** Private admission context must cover every production admission/allocation/helper/preparation/release path before effects, including legacy or false-qualified branches. This slice implements only the protected final-admission foundation and two explicit gates; the complete sink audit remains future work.
- **Q05:** All future writers must participate in the fixed authority-before-install lock order with bounded waits and no blocking lock or I/O under stop latches. Publication, withdrawal, quarantine, and active-generation races retain distinct meanings. This slice adds no writer.
- **Q06:** Native host/filesystem durability, protected metadata custody, missing-feature refusal, permanent privilege drop, actual loaded closures, pidfds/credentials, direct exchange, original B2/B3 behavior, and inherited stop/cleanup regressions remain unqualified for the new mechanism. Specified tests are not results.
- **Q07:** Input and model identities are recorded role attribution and prior-chat evidence, not cryptographic attestation or human signatures. The canonical baseline and current preservation inventory were verified for this implementation commission.
- **Q08:** All N01-N38 and S01-S09 remain specifications until independently executed. No skipped, missing, or unknown native row is a pass. The owner explicitly retains the active-generation and race expansions in the reconciliation.
- **Q09:** Design acceptance, source implementation, provisioning, installed execution, F02 publication, final publication, S3 acceptance, and production activation are separate gates. This decision opens only the bounded source implementation in the linked plan.
- **Q10:** **Mandatory non-waiver.** The accepted B6 requirement that the controller re-read `active` and require the operation's generation to still be active before sending a new `ReleaseRequest` remains in force. A detected mismatch must prevent a new request and use the existing stop path with the original context and C3 authority. Approval withdrawal is not an active-generation switch. A request already in flight remains subject to the existing owner arbiter, stop precedence, at-most-one physical attempt, and honest Unknown rules; no retroactive cancellation is promised.

## 4. Required separation

The owner explicitly accepts these separations:

- S2 `Qualified` is not final admission.
- Final admission is not production activation.
- A final-admission binding, validation result, Boolean, legacy selector, or caller-supplied hash is not authority by itself.
- The only final-admission runtime grant is constructed by the full protected-store verifier after strict CJSON-1 parsing, exact S2 binding, host binding, protected-store stability rechecks, and fresh S2 revalidation.
- Runtime does not read the administrative `/etc/gagamba/accepted-final-admissions` approval store. Runtime uses the protected selector and record under `/opt/gagamba`; the administrative approval remains a separate publication input.
- `RestrictedExecutionRuntime.Create()` remains default-disabled and unavailable.
- No new RELEASE writer is introduced. Existing owner-only one-attempt release authority, recovery stop-only behavior, and original C3 stopping semantics remain unchanged.

## 5. Bounded implementation authorization

The authorized slice is limited to:

1. A strict CJSON-1 codec and exact schema for the 24-field final record, the 7-field selector, and the 64-lower-hex host identity.
2. A pure binding evaluation against one retained successful S2 selection, with no caller-supplied expected hash and no search, fallback, or `latest` selection.
3. A protected-store verifier over `/etc/gagamba/host-id`, `/opt/gagamba/final-admission-selectors/<artifactSetSha256>.json`, and `/opt/gagamba/final-admissions/<finalRecordSha256>.json`, with descriptor-relative no-follow reads, exact metadata checks, byte/stability rechecks, and a private grant.
4. Fail-closed admission predicates that keep S2 Qualified, final admission, and production activation distinct, with an unreachable future activation type and no factory in this slice.
5. Addition of the raw S2 qualification-record hash to the retained installed result and revalidation path.
6. Explicit final-admission gates in `B3Qualification.TryCreate` and `IntegratedB3Client.Prepare` before journal construction or recovery spawn, plus refusal of the legacy non-integrated path when `QualifiedInstallation=true`.
7. Focused portable tests and a new protected-directory reader needed by the verifier.

## 6. Exclusions and open gates

This decision does not implement or claim:

- A final-record publisher, gate-results document validator, evidence-index validator, approval publication, withdrawal, quarantine, or lock protocol.
- A real protected store on any host, a real final acceptance record, or any host identity.
- Full session-bound authority, GAVA purposes, actor/descriptor binding, installed-profile driver, S4/B7, host provisioning, privilege drop, or native qualification.
- Full production sink coverage. The two gates in this slice are necessary foundation, not proof that every future admission path is covered.
- The pre-RELEASE active-generation reread in source. Q10 remains an explicit open obligation and must not be silently waived.
- Production activation. The public restricted factory remains unavailable.
- Any push, branch, linked worktree, history rewrite, reset, rebase, stash, clean, prune, or force operation.

## 7. Disposition

**MECHANISM B OWNER ACCEPTANCE RECORDED. BOUNDED FIRST IMPLEMENTATION AUTHORIZED.**

The implementation handoff must report the exact source and test state, all executed results, every native-pending item, the `platformInventorySha256` mapping qualification, the false-`QualifiedInstallation` legacy-path question, and the unresolved Q10 pre-RELEASE obligation. It must not report design acceptance, portable tests, or source inspection as native qualification.
