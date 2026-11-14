# Alternative B Slice E-2 owner closure decision

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-CLOSURE-OWNER-01`

Decision date 2026-11-14 (Asia/Manila)

## 1. Authority and repository baseline

This commission records the architecture owner's E-2 milestone disposition. E-2
P2 acceptance §27 reserved this judgment for a separate owner decision after an
independent closure assessment. This decision adopts the supported scope under
that authority. Implementation and later entry require their own commissions.

The canonical checkout is `C:/Users/Laszlos/source/repos/Gagamba`, identified by
`Gagamba.sln`, the Gagamba projects and the accepted decision chain. The sole
registered worktree was on `main`, with clean tracked files and index and zero
stashes. No conflicting writer was observed in the active-task, Git process
and lock checks. These are point-in-time observations.

| Starting identity | Verified value |
|---|---|
| HEAD and assessment commit | `f81369549b3957b030202697eae20735354cdafe` |
| Tree | `07e51dc35997ef3e001c87433e6924468b09626d` |
| Recorded `origin/main` and assessment parent | `a30d1807686ee15d39ef7e9103e8b992062bd971` |
| Ahead/behind against that local reference | 1 / 0 |
| Historical untracked files | 44 / 44 present and SHA-256-identical, no extra untracked paths at baseline |
| Untracked manifest SHA-256 | `7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1` |

The manifest is [untracked-before.json](../evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json).
The local tracking reference was used as recorded. No remote synchronization
was performed.

## 2. Immutable assessment and decision chain

The [closure assessment](../reviews/rc2b-06-b6-alt-b-slice-e-02-closure-assessment-01.md)
at the baseline commit binds report blob
`b75e1a5e2a7b52c393b919ff54ef1dd68ca49779`, whose committed bytes hash to
SHA-256 `f82211d1ee2c53f76279ed3e57bb56ec3ea11a79204d683a068559ea171124de`.
The eight-file [evidence tree](../reviews/rc2b-06-b6-alt-b-slice-e-02-closure-assessment-01-evidence/README.md)
is `eee19b05e903272761dfc0a692d81b76c8f624f4`. The controlling tables are
the full report §§20–23 in that blob. Their committed summary objects are:

| Assessment sections | Evidence summary blob |
|---|---|
| §20, 27 requirements | `0a59dea5202cbb82125a77ab73fdeb8c8843ff11` |
| §21, 39 retained qualifications | `6a45cf136b0fbb77f11cc26103f99502decab746` |
| §22, 23 downstream obligations | `2e297b1672a512acf2afac315a5b390483b50cb5` |
| §23, 15 blocker checks | `a73d513e2f5fe466dd731334e40a10b839aab252` |

The source chain and object identities were resolved locally and their ancestry
verified through the assessment. P1 and P2 source and test blobs match their
accepted producer revisions. The P2 Q1 report identity incorporates its
reference correction. The P2 acceptance wording correction changes current
state references only.

| Record | Commit | Binding blob |
|---|---|---|
| [P0 frozen-schema record](../evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md) | `77101ac18c0e019a9d32712e9fee78b468cc8d02` | `db25949d25babd474d00ae5b2f436e15507f22b3` |
| [P0 acceptance](rc2b-06-b6-alt-b-slice-e-02-p0-owner-acceptance-01.md) | `5b2ef145088818c9b9b145d706816a075951c861` | `8dd20d11202bb4ed7fb0dd3cdf447eaa3c5e54b7` |
| [D1 adoption](rc2b-06-b6-alt-b-slice-e-02-d1.md) | `bdc7fa2cc84f4a43269270405e5ff9fae7273c28` | `83fe8d2649675e00b76457885fd0169b4ae77498` |
| [P1 entry](rc2b-06-b6-alt-b-slice-e-02-entry-owner-02.md) | `2dab45aa4f4be6b0f713a35b4d50bdfa6fc925be` | `f5ab96c6fedc480b427beeb9e1941a64e83a2b43` |
| [P1 implementation](../reviews/rc2b-06-b6-alt-b-slice-e-02-p1.md) | `06006a7035ca3a7615189b867584b1b3955eeb7a` | `8dfe6b8cb9917dbc7018ee2cea4e5584879f8dcd` |
| [P1-Q1](../reviews/rc2b-06-b6-alt-b-slice-e-02-p1-q1.md) | `ab5f7a728a3f04fb6e00dadf28c6a9a7293569bf` | `7ee0b781dbfd74aef873e5649c83d5db28795016` |
| [P1 acceptance](rc2b-06-b6-alt-b-slice-e-02-p1-owner-acceptance-01.md) | `28275ba516ba9326bd7a850357fa3143749ecd03` | `f360932adc4909b4f26e32b6005a592618dd0912` |
| [P2 entry](rc2b-06-b6-alt-b-slice-e-02-p2-entry-owner-01.md) | `dfa570fcfa4b55176d6cc63ad1d8c8930d4a58eb` | `893b48a2a7c8290d817d038d6cbf1eb63e0b4f4a` |
| [P2 implementation](../reviews/rc2b-06-b6-alt-b-slice-e-02-p2.md) | `9b80969ce766b6d8939775710aa77d080c3ff674` | `48f33ca30e8a9859421850beadf08279d45e82a3` |
| [P2-Q1](../reviews/rc2b-06-b6-alt-b-slice-e-02-p2-q1.md) | `285c0165bbd5e52854cdffa456029c1f3114ff20`, with report correction `661800de9e5fc83a71ec28cabbc962d1a228e7a5` and evidence README correction `a79dc608103d6f4395949de01e39bc2115d294b2` | `e785c42ab8e1f955698d4a9d46f144394389b736` |
| [P2 acceptance](rc2b-06-b6-alt-b-slice-e-02-p2-owner-acceptance-01.md) | `24c4cf4247930d7675a2ceb77e0f8d68ac120642` | `fb00cc37f1b8294d18e459154a24c3cc1d1f8dd8` |

Assessment §2 and its preservation summary mislabel `2dab45a` as
E-2 entry owner-01. The [initial reconciliation authorization](rc2b-06-b6-alt-b-slice-e-02-entry-owner-01.md)
was committed at `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`, blob
`4c256e9dd0e7986b4b4d7838f8a8a1f4c85d5e0d`. The ancestry of both correct
records is verified. This reference correction leaves the assessment bytes
and conclusions intact.

## 3. Original scope and requirements determination

The Alternative B [design decision](rc2b-06-b6-integrated-start-authority-owner-decision-01.md)
and [implementation plan](../plans/rc2b-06-b6-integrated-start-authority-impl-01.md)
are bound by blobs `844cb149ffee442e0111210f7befae1cfe2caef0` and
`485ad1ac76e836d2bd6eb826131a01294c9bda8c` respectively. The
[Slice E review §8](../reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md)
(blob `0c311fc84bde05d3c9e897ff51c8e44832aea987`) labels E-2 as protected
K/M/F/S readers plus case policy, refusal-only. The
[Slice E owner decision §§7, 9](rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md)
(blob `7002dcb3f04e4d69ca5d9c6cb8e9ef57f0f7c578`) adopts the planning
sequence and requires frozen-schema provenance (Q-E1), a catalog approval
decision (Q-E2) and atomic enablement (Q-E3).

E-2 entry owner-01 §§7–10 required P0 and D1 with qualified, bounded entry
thereafter. Entry owner-02 §§5–10, 12–16 and 18 authorized P1 only, with
canonical schema and binding verification, refusal, independently qualified
non-authoritative results and a separate acceptance. P2 entry §§6–19 added
supplied detached evidence verification under equivalent governance. Those
entry decisions control implementation scope. The roadmap's protected reader
and case-policy work is reconciled expressly in §5 of this decision.

The owner reviewed every row of assessment §20 against the governing
requirement, package, committed evidence, qualification, owner decision and
closure effect. The 27-row assessment matrix is adopted by the identity in
§2 with the source-reference clarifications below.

| Rows | Satisfied | Satisfied with qualifications | Deferred by accepted decision |
|---|---:|---:|---:|
| R01–R04 (P0) | 1 | 3 | 0 |
| R05–R09 (D1) | 3 | 2 | 0 |
| R10–R16 (P1) | 2 | 5 | 0 |
| R17–R23 (P2) | 1 | 6 | 0 |
| R24–R27 (downstream) | 0 | 0 | 4 |
| Total | 7 | 16 | 4 |

There are zero unsatisfied and zero inconclusive rows. The seven satisfied rows
are R03, R07–R09, R13–R14 and R22. For R04, use Entry owner-01 §§5, 7, 10
and Entry owner-02 §5 for the provenance scope. For R05, the original Q-E2
references are Entry owner-01 §§6, 8, 10 and D1 §§5, 16–17. P0 and D1
evidence includes governance recording and local identity checks within their
accepted bounds. Independent implementation qualification applies to P1 and
P2. Section 4 records the bounds that remain attached to each package.

## 4. Package acceptance and content boundary

**P0 remains accepted with qualifications.** Acceptance §§5–12 binds the
adopted CJSON-1 and M/F/S/K/R/Wc/P provenance record, SHA-256
`11bca1f6add93abd759c5ec4afeca506e4e894e2ab79cc6ccf25da59e3c167d6`.
Q-E1 is satisfied for the E-2 subset. The external package's durability and
independent Git history remain unverified. Preserve the historical `PENDING`
captions, the accepted S2 `ValidId` grammar reference, spot-checked extraction
limits and the single-reviewer governance qualification.

**D1 remains adopted with qualifications.** Under §§5–13 and §§16–18, V is
the independent reviewer producing R, W is the human owner accepting exact
catalog bytes through Wc, and A is the separately authorized root
administrator who provisions protected P and authorizes each live
invocation. This is the adopted procedural model. It forbids delegated,
standing and cross-host approval and preserves separate F02 qualification
authority. Frozen R/Wc/P gain no issuer identity or signature fields.
Reviewer independence is procedural. The trusted-root model excludes
malicious or colluding root, copied-host rollback and hostile same-UID
activity. Protected-root custody and administration, per-instance role
execution and operational refusal encodings remain future obligations.
Q-E2 is satisfied for E-2 governance.

**P1 remains accepted with qualifications.** P1 acceptance §§5–15 and §18
bind the internal CJSON-1 and M/F/S/K code, deterministic refusals, exact
`Hm/Hf/Hs` and accepted bindings, and non-authoritative results. Independent
P1-Q1 used 32 byte vectors, three independently constructed chains, 400
mutations, negative controls and RELEASE checks. P1-Q1-F01 through F07 retain
their distinct dispositions under acceptance §§11.1–11.7.

**P2 remains accepted with qualifications.** P2 acceptance §§6–17 and
§§19–25 bind structural and content verification over supplied K/R/Wc/P.
P2 reuses the accepted P1 catalog and codec functions read-only. P2-Q1
independently constructed B2/pre and B3/post chains, matched 6/6 producer
fixture identities and passed 97/97 reviewer probes. All 11/11 identity and
substitution controls, including the external expected-`Hp` comparison,
23/23 hash-consistent semantic-invalidity controls and 8/8 authority-field
injections produced the expected refusals. P2-Q1-F01 through F06 retain their dispositions
in acceptance §§15.1–15.6. Finite probes and bounded static inspection
support the reviewed claims. Dynamic reachability and bit-identical rebuilds
remain outside the evidence demonstrated.

The accepted graph is **M → F → S → K → R → Wc → P**. CJSON-1 governs the
exact canonical bytes used for `Hm/Hf/Hs/Hk/Hr/Hw/Hp`. F binds M, S binds
M/F, K binds M/F/S, R binds K, Wc binds K/R, and P binds K/R/Wc. P2
verifies the applicable equal-to-K values under the accepted R/Wc/P
comparison sets (11/9/10 total references). P has no intrinsic self-hash.
An externally supplied expected `Hp` is compared with the computed content
identity outside the verifier. A valid untrusted chain may verify
structurally. The frozen `accepted=true` literal yields no approval flag,
eligibility determination, live capability, or execution authority. See
assessment §§11–14 and P2 acceptance §§9–16.

## 5. F-01 owner ruling and accepted deferrals

**RATIFY THE E-2 STRUCTURAL-CORE BOUNDARY WITH QUALIFICATIONS.**

D1 §13 item 3 explicitly identified a fresh E-2 entry assessment and a
separately authorized bounded implementation package as the route to resolving
the protected-root prerequisite. Its specified evidence was implemented
readers/verifiers, negative controls and independent qualification under that
fresh entry scope. Read with the original roadmap label, that language
created a genuine expectation that entry would settle the reader scope.
D1 §13 item 3 expressly made the condition non-blocking for governance
adoption, production-blocking until qualified, and subject to fresh owner
authorization. It left the bounded deliverable to that later decision.

The [entry reassessment §10](../reviews/rc2b-06-b6-alt-b-slice-e-02-entry-reassessment-01.md)
kept the protected-reader work with the atomic set. Entry owner-02 §6 accepted
that reassessment and bounded the fresh authorization to P1. P1 acceptance
§11.6 expressly ruled protected K/M/F/S acquisition and the listed runtime
checks deferred to E-4/E-5/E-6 with Slice N. P2 entry §§10–14 and 24 kept P2
within supplied-content verification. P2 acceptance §27 reserved this
milestone reconciliation and repeated the protected-path deferral.

Plan §3.5 explicitly requires protected K/M/F/S acquisition, exact case
policy and a finite fixture, together with custody, protocol, identity,
sink, generation, stop, ordering, driver and closure dependencies before
exposure. Q-E3 adopts that complete set, and Slice E owner §9 assigns
accountability for its completion to E-6, with the E-7 driver dependency
and separate Slice N qualification still required. These accepted allocations
clarify D1's fresh-entry wording and narrow the earlier roadmap label for
E-2 closure. The controlling basis is the adopted allocation, independent of
the fact that the producer delivered a narrower implementation.

This decision accepts the frozen structural and content-binding core as
E-2's implemented deliverable. The D1 protected-root evidence requirement
survives at the protected implementation and qualification gates.
Implemented protected readers/verifiers, negative controls, independent
qualification, exact runtime case enforcement and required native evidence
remain mandatory. E-4, E-5, E-6 and Slice N retain their individual gates.
E-6 must establish the complete §3.5 dependency set and preserve fail-closed
refusal before protected effects, exact identities and custody, independent
per-invocation authorization, release correlation, and no partial enablement.
The structural refusals answer the implemented part of D1 §13 item 4.
Operational encodings still require invariant-to-refusal evidence at the
proper protected-path gate.

| Deferred row | Accepted basis and continuing gate |
|---|---|
| E2-R24 Protected K/M/F/S acquisition and readers | P1 acceptance §11.6 expressly allocates them to the atomic E-4/E-5/E-6 set with Slice N. D1 §13 item 3 evidence and plan §3.5 remain binding at that gate. |
| E2-R25 Exact case policy | Plan §3.5 and binding Q-E3 make it mandatory for the atomic set. Slice E owner §9 makes E-6 accountable for the complete dependency set. |
| E2-R26 Native and protected-execution qualification | Slice E owner §9, plan §3.6 and P2 acceptance §23 reserve protected native qualification for separately authorized Slice N with exact host, candidate and evidence bindings. |
| E2-R27 Remaining composite Q-E1 | P0 acceptance §§12, 16, D1 §18 and Entry owner-02 §§5–6 exclude these items from the authorized E-2 packages. E-3 owns the v3-pre layout reconciliation, E-4 owns GAVA1 header/wire reconciliation, and the E-3/E-4 gates must account for release correlation. |

## 6. Retained qualifications and downstream obligations

Assessment §21 is adopted by immutable reference with all 39 items under their
original identifiers, source dispositions, triggers and evidence obligations.
The P1-Q1 and P2-Q1 findings remain distinct. The register remains active.
Assessment §22 is adopted by the same reference as the 23-obligation ledger.
Binding allocations include the E-3 v3-pre codec, E-4 GAVA1 and fresh P4 audit,
E-5 launcher/custody interface, E-6 atomic enablement and fresh P4 audit,
E-7 exact driver dispatch and closure, and Slice N protected native
qualification. Operational V/W/A execution and live invocation have an
accepted deferral, with their detailed split among D1 administration and
downstream packages still to be decided. The proposed publication/withdrawal
allocation and operational refusal-encoding allocation remain inferred.
Mechanism A durable permits remain unselected and require a separate decision
if proposed. The ledger conveys no implementation or entry authorization.

**First E-1 production consumer.** Open, unassigned and non-blocking for
E-2. E-2 entry owner-02 §18 and P2 acceptance §18 exclude the consumer.
A separate owner decision must assign a responsible gate before the first
production consumer is authorized. E-1 remains accepted with qualifications
and production-unreachable. At that gate, retain F-01 owner and recovery
role-hash and runtime-inventory binding and fail-closed exception handling.
Apply F-02 parity, branch dominance, verification-to-use, artifact identity,
authority separation, RELEASE, P4 and native proof as applicable, and
reassess the Option A shared-core decision. F-03 qualification covers Windows
PowerShell 5.1 with 51/51 probes. PowerShell 7 qualification is unexecuted.
This disposition supplies neither the consumer nor that gate's evidence.
See E-1 F-01, F-02 and F-03 owner acceptances and assessment §15.

**P0 external-package durability.** Open, unassigned and non-blocking for
E-2. P0 acceptance §6 and §11 item 1 establish the exact repository-scoped
provenance independently of long-term custody of the external package.
Preservation or independent source-history verification remains a binding
residual because loss of the source copies would prevent repeat byte-level
verification. A separate owner decision must assign accountability for
durable custody or independent source-history evidence. A preservation
handoff, loss or replacement risk, or a new need to reproduce the external
inputs calls for that assignment. Adopting the committed extraction supplies
no new external preservation or source-history evidence.

## 7. RELEASE, P4, native and evidence limits

P2 acceptance §§17–21 and assessment §§13, 16–19 support these determinations:

- Production RELEASE verification passed with exit 0. The Debug-as-production
  control failed with exit 1 as expected. Verifier policy is unchanged.
  There are no new forbidden production test hooks or public authority APIs.
  The reachability evidence covers inspected direct calls, type tokens and
  selected strings. This decision makes no binary reproducibility claim.
- **P4-NEUTRAL for the reviewed revision.** Preserve F-R1/O-R1, O-R2, O-R3,
  the observation-to-effect residual, S10/S11 dominance, S28 isolation,
  first protected-effect dominance and qualified-to-legacy downgrade refusal.
  E-4 and E-6 require fresh P4 re-audits under D1 §13 item 11 and P4 coverage
  acceptance §§5, 7. P4 residuals and protected-runtime qualification remain open.
- Independently executed Windows M1 qualification recorded 1,517 passed,
  0 failed and 331 skipped of 1,848. All 331 native skips remain
  unqualified. **NATIVE-PENDING** covers protected Linux and macOS behavior,
  protected-provider completion, positive admission, native sandbox
  isolation and Slice N acceptance. GitHub CI run `42933880787` remains
  operator-reported. It establishes no separate native qualification here.

## 8. Closure-blocker determination

The owner reviewed all 15 checks in assessment §23. The earlier scope
tension and four accepted deferrals are resolved for closure in §5. The
four package decisions, independent Q1 records and required bindings are
present and bound. The exact reviewed source and test revisions are preserved.
The accepted frozen scope is complete. Composite Q-E1 remains open.
No material authority projection, unauthorized consumer, RELEASE weakening,
P4 regression, qualification promotion, native overclaim or conflicting
current-state claim was established. The entry lineage citation correction in
§2 has no scope or evidence effect. The two unassigned obligations are
preserved under §6. No milestone blocker remains.

## 9. Formal owner disposition

**E-2 CLOSED WITH QUALIFICATIONS**

Slice E-2 is formally closed within its accepted frozen-schema provenance,
approval-governance, structural verification and exact content-binding scope,
subject to the retained qualifications and accepted downstream obligations.
The P0, D1, P1 and P2 dispositions retain their existing qualifications.

Closure excludes protected K/M/F/S readers, exact runtime case policy,
provisioning and custody, genuine operational V/W/A acts, R/Wc/P issuance,
an approval store, publication and withdrawal implementation, live invocation
authorization and capability issuance. E-1 has no production consumer.
Composite Q-E1, Slice E completion, native qualification and Slice N
acceptance remain open. Positive validation and protected execution remain
unauthorized. E-3, E-4, E-5, E-6, E-7 and Slice N each remain separately gated,
with no entry or implementation authorization granted here.

## 10. Future decisions and proposed next commission

The next proposed commission is
`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-ENTRY-ASSESSMENT-01`.
Its purpose is to assess the original E-3 v3-pre codec scope, verify this
decision and its inherited qualifications, resolve E-3 prerequisites, reconcile the E-3 portion of composite Q-E1,
identify permitted implementation surfaces and retain F02, E-1, P4 and
RELEASE constraints. The assessment would determine whether entry can be
recommended. Implementation requires a separate owner decision.

Future decisions must address E-4 through E-7 and Slice N under their
own inherited gates, the first E-1 production consumer and P0 durability
assignments, and the detailed governance/implementation split before any
operational approval path. This commission ends with the decision and
current-state documentation.
