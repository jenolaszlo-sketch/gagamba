# RC2A-03-CQ1 — component qualification record (restricted consumer contract)

Status: **RC2A-03-CQ1 QUALIFICATION PREPARED / READY FOR INDEPENDENT REVIEW.**
This record is a candidate for independent review. It is **not** an
independent review result and **not** an owner sign-off. RC2A-03 owner
sign-off remains pending; RC2A-04 remains not authorized.

Prepared under owner authorization "Proceed with RC2A-03-CQ1 only"
(2026-10-26), resuming after a resolved environmental blocker (drive-full;
recovery report `GAGAMBA-STATUS-RECOVERY-01`, 2026-10-26).

## 1. Git binding

| Item | Identity |
|---|---|
| Repository | `jenolaszlo-sketch/gagamba` (canonical local `C:\Users\Laszlos\source\repos\Gagamba`) |
| Qualification branch | `qualification/gagamba-rc2a-03-cq1` |
| Qualification worktree | `C:\Users\Laszlos\source\repos\Solo\rc2a-03-cq1` |
| Accepted RC2A-02 source baseline (branch point) | `9519e7cb612af6ff69c93c0d128553138533d9fd` (tree `7b162d2b0e00d72c27a2389ed505d531758797f1`, parent `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6`) |
| Accepted disposition | the RC2A-02 full-stage owner acceptance (`docs/decisions/restricted-consumer-contract-rc2a-02-full-stage-owner-acceptance.md` at `9519e7cb…`): ACCEPTED WITH QUALIFICATIONS |
| Roadmap basis | the RC2A-ROADMAP-01 dependency review (`docs/assessments/rc2a-roadmap-01-restricted-execution-dependency-review.md`, preserved on branch `assessment/gagamba-rc2a-roadmap-01` at `d417367499979d38a3c9a498275d8422bf69f3d0`; not merged into this baseline — referenced by identity), recommendation RC2A-03-CQ1 |
| Record commit | recorded in the completion report (self-reference avoided) |

The baseline is the accepted RC2A-02 tip — **not** `main` (local `main`
`0d26adf4` predates the consumer-contract line, which is verified to contain
it; nothing from this track is merged or pushed).

## 2. Component identity and no-drift verification

The component qualified here is the restricted consumer component of the
`Gagamba.Execution` assembly: shared validation/reader, canonical codec, the
twelve public sealed get-only views plus two public enums and
`ManifestDecodeResult`/`ManifestDecodeError`, and the sealed passive
`RestrictedPreparationReference` — exactly the frozen public API contract.

Frozen public API contract:
`docs/design/restricted-consumer-contract-rc2a-01-api.md`, blob
`2d6b9d12cab02f092c6b6e0b690138cf6726f12b`.

**No-drift verification method** (read-only; exact commands recorded in
[source-binding.json](source-binding.json)):

```text
git rev-parse <commit>:src
git rev-parse <commit>:native
git rev-parse <commit>:docs/design/restricted-consumer-contract-rc2a-01-api.md
```

compared byte-exactly as Git object IDs across every checkpoint of the
accepted chain. Results:

| Checkpoint | Role | src tree | native tree | frozen API blob |
|---|---|---|---|---|
| `68b01fbb53eefca9c8ca2f83ff50641521020a2a` | RC2A-01 accepted object | `b8c167e3d7340a562221591bdc2b6df7adb372d1` | `85631d2f04512376621d3d09f51d67fd90f45335` | `2d6b9d12cab02f092c6b6e0b690138cf6726f12b` |
| `71a28afe2623516fb4176e6a4ce7988a3d70df06` | RC2A-02A tested source | identical | identical | identical |
| `dd16baec48663d40cd29504d98e57e30dd0855df` | RC2A-02B qualification | identical | identical | identical |
| `1cd22b9b109ddba4673b11487bceea62d2e92117` | RC2A-02B R1 final | identical | identical | identical |
| `c4837b0b434fc75ebc3e08d278bdc09c3965a181` | RC2A-02C final tested code | identical | identical | identical |
| `62b75f5866d0c339816712baeb04baa386b06f85` | RC2A-02C evidence | identical | identical | identical |
| `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6` | RC2A-02C owner acceptance | identical | identical | identical |
| `058a150130c5f112c7e9b8189bba8842cf0cec52` | full-stage assessment | identical | identical | identical |
| `9519e7cb612af6ff69c93c0d128553138533d9fd` | RC2A-02 full-stage acceptance (baseline) | identical | identical | identical |

**Result: zero production drift, zero native drift, zero public-API identity
drift, zero consumer-contract shape change, no additional authority-related
member, and no change invalidating the accepted compatibility evidence.**
The component being signed off is exactly the component the accepted RC2A-02
evidence qualifies. No stop condition was triggered.

## 3. Evidence traceability

The complete requirement-to-evidence matrix is
[traceability-matrix.md](traceability-matrix.md). Summary of the accepted
chain this record inherits (all independently reviewed and owner-accepted):

- RC2A-01 implementation and focused tests, owner-accepted with follow-up at
  `49ebbfec4abc7e5e1da2bd492f3ff4e43e14ca4e` (review `8e41babfc96afa2e617a53b63e50903d54001194`,
  ACCEPT WITH FOLLOW-UP; R2A-F01/F02/F04 carried into RC2A-02 and closed
  there; R2A-F03 accepted as disclosed, standing).
- RC2A-02A Windows qualification: evidence `c1a01761f8d58136a381ad87269635c6c14ad093`,
  Q1 review `7b6344551c2d4b42832df39822eaf4b8a1c01495` (SUPPORTED WITH
  QUALIFICATIONS), owner acceptance `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c`.
- RC2A-02B portability: qualification `dd16baec48663d40cd29504d98e57e30dd0855df`,
  Q1 `5bc396c2e5619906ae202afe0a4244dfd6024d9e`, R1 correction `1cd22b9b109ddba4673b11487bceea62d2e92117`,
  Q2 verification `60de1f2d34a7df337a426501cb3954fdd3216a6b`, owner acceptance
  `dc02951902f11031f8b0d225d2afa2cd537296a3`.
- RC2A-02C passive-reference slice: evidence `62b75f5866d0c339816712baeb04baa386b06f85`
  (13/13 matrix rows), independent review `9534581c34d55c1219e90ddcd290900583098388`
  with re-verification `f8b692e04941462ea592d9653960191cafb937cc`
  (SUPPORTED WITH QUALIFICATIONS), owner acceptance
  `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6` with the F01 erratum.
- Full-stage assessment `058a150130c5f112c7e9b8189bba8842cf0cec52`
  (READY FOR OWNER ACCEPTANCE) and owner acceptance
  `9519e7cb612af6ff69c93c0d128553138533d9fd` (all eight §5 exit criteria
  dispositioned).

The implementation plan's RC2A-03 pre-acceptance gate — "Q2A-S / RC-D04
RC2A-02C evidence is a pre-acceptance gate" — is satisfied by that chain.

## 4. Security boundary

**Supported by accepted RC2A-02 evidence (inherited verbatim, not
re-claimed here):**

- Canonical consumer-artifact encoding and decoding with a closed refusal
  vocabulary and exact code/offset assertions.
- Strict decode semantics: malformed, noncanonical, oversized, and unknown
  inputs refuse; no normalization; no partial views.
- Immutable passive `RestrictedPreparationReference` with validated,
  defensively copied content; content equality/hash; no temporal, control,
  lookup, release, or establishment member.
- Frozen public API identity (`2d6b9d12…`) and accepted compatibility
  (golden bytes and `ReferenceEncoder` oracle preserved; tests tree
  additions-only plus one strengthened existing file).
- Deterministic behavior for the accepted test profile on the primary
  Windows platform, reproduced independently, and the identical
  assertion set re-verified on WSL2 (labeled `wsl2`).

**Explicitly NOT established by this component:**

- Live execution authority; consumer-facing authority issuance or
  verification.
- Proof of producer provenance, execution establishment, or isolation from
  parsing alone. **Successful decoding grants no authority and proves no
  execution establishment or isolation.**
- Native execution confinement; runtime establishment; stop, termination,
  or correlation guarantees; recovery and lifecycle guarantees.
- Any public restricted-execution facade (RC-2B B1–B7 unbuilt).
- Hufu authority integration (AE4-B01 blocked on package; D4b additionally
  blocked on accepted RC-2B and C1–C4).
- Package-level or clean-consumer compatibility (RC2A-04; not started).

**Platform limitations (inherited):** Windows is the primary qualified
platform. WSL2 evidence is real, separately preserved, and always labeled
`wsl2` — it is **not** native Linux qualification. Native Linux and macOS
remain **NOT RUN** for this consumer qualification. R2A-F03 stands: no
binary-reproducibility, package-identity, or deployment claim is made in
either direction.

## 5. Inherited qualifications and preserved findings

All RC2A-02 qualifications carry forward unchanged; none is closed, weakened,
or re-litigated by this record:

1. **F01 (RC2A-02C):** the hash-bound `validation.md` understated the package
   checksum count as 39/39; the manifest has 40 verified entries.
   **ACCEPTED AS CORRECTED BY ERRATUM** in the RC2A-02C acceptance §4; the
   immutable evidence snapshot is unchanged.
2. **F03 (RC2A-02A):** two large vectors are digest-bound without embedded
   payloads; materialization reproducible from committed recipes.
   **ACCEPT AS DISCLOSED.**
3. **R2A-F03:** no binary-reproducibility or package-identity claim.
4. **RC2A-02B Q1 audit-trail note:** the separate-authorization pointer for
   RC2A-02B is asserted by the qualification matrix, not independently
   authenticated from Git. **PRESERVED AS DISCLOSED.**
5. **Passive-reference boundary:** no provider provenance, live
   authorization, temporal guarantee, release or retained-state authority,
   native confinement, packaging correctness, Hufu integration, or
   end-to-end execution is established.
6. **Q2A02A-F01:** the flip-sweep row stands as the owner-accepted canonical
   rigidity + immutable-region correction.
7. **Not-run platforms:** native Linux and macOS remain unqualified for this
   component.

## 6. Findings from the adversarial evidence audit

A focused claim-versus-evidence audit of the current tracking documents at
the baseline (work queue, handoff, implementation plan, qualification plan,
accepted decisions) found **one** in-scope documentation discrepancy:

- **RC2A03-CQ1-F01 (minor, corrected in this commit):** the implementation
  plan §1 progress-table row for RC2A-02 still carried its pre-full-stage
  status ("…FULL RC2A-02 NOT ACCEPTED"), contradicting the authoritative
  accepted decision `9519e7cb…`. The row is refreshed in this commit; the
  historical wording is replaced, not archived elsewhere, because the table
  is a living status index whose own conventions require updating rows as
  work happens ("Update this table with exact commit… as work happens").
  The accepted decision documents themselves are untouched. Related: the
  RC2A-03 row is updated to reflect this record's status.

No other in-scope issue was found: no statement presents implementation as
independently qualified, testing as owner acceptance, or decoding as
authority; platform claims are correctly scoped; compatibility claims are
bound to the frozen API identity; no unresolved finding is shown as closed.

## 7. Qualification assessment (candidate — pending independent review)

**Evidence-supported candidate disposition:** the restricted consumer
component of `Gagamba.Execution` at frozen identities
(`src` `b8c167e3…`, `native` `85631d2f…`, API blob `2d6b9d12…`) is eligible
for component-level sign-off on the basis of the accepted RC2A-02 evidence
chain, with zero drift since acceptance and all inherited qualifications
preserved.

**Scope of that disposition:** the component's public passive
value/codec/reference behavior as qualified by RC2A-02, on the executed
platforms, subject to §4–§5 exclusions. It adds no new claim beyond the
accepted RC2A-02 record.

**Remaining limitations:** all inherited qualifications (§5) plus the
review-pending status of this record itself. RC2A-03 owner sign-off is
**pending**; RC2A-04 remains **not authorized**.

## 8. Independent-review handoff

See [reviewer-handoff.md](reviewer-handoff.md) for the exact review basis,
required adversarial checks, and questions requiring independent judgment.
