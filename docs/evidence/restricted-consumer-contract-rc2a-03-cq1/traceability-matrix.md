# RC2A-03-CQ1 component qualification traceability matrix

Every component-level claim binds to an accepted artifact, its commit and
tree identity, the executing platform, the qualification or inspection
method, the independent review result, and the owner acceptance. Inherited
qualifications are preserved explicitly. This matrix adds no new test
execution; it connects existing accepted evidence to the component boundary.

Legend — Status values: **ACCEPTED** (owner-accepted with binding evidence),
**QUAL** (inherited qualification/limitation), **EXCLUDED** (out of component
scope by accepted contract).

## 1. Component capabilities (supported by accepted evidence)

| # | Requirement / contract | Evidence artifact | Commit / tree binding | Platform / method | Independent review | Owner acceptance | Status |
|---|---|---|---|---|---|---|---|
| C1 | RC-D02: all 72 manifest fields exposed as sealed get-only public views; internal mapping preserved | RC2A-02A evidence package: `traceability-matrix.md`, `exports/mutation-coverage-matrix.json`; frozen API §API | `c1a01761f8d58136a381ad87269635c6c14ad093` / tree `8c51c3c2e06e6544f3ad0e8aceabfed8457d8c19`; tested source `71a28afe…` | Windows 11, .NET SDK 10.0.401; deterministic required-set run (457/0/0) and public run (67/0/0) | Q1 `7b6344551c2d4b42832df39822eaf4b8a1c01495`: SUPPORTED WITH QUALIFICATIONS, 103/103 probes, 72/72 fields verified | `e5fff9e8d5a31520f8b5a2c235a05b666a08e16c` | ACCEPTED |
| C2 | RC-D03: canonical encode/decode identity; closed 12-code refusal vocabulary with exact code/offset | 23 vectors (A1–A4), 78-entry A5 refusal catalog, truncation sweep (873), flip sweep (222,615; canonical rigidity per Q2A02A-F01) | same package/commits as C1 | Windows; byte-exact vector recipes; three independent representations (production codec, wire template, `ReferenceEncoder`) plus Q1's own parser | Q1: reproduced sweeps, 21/21 hex digests, 14 refusal categories | `e5fff9e8…` | ACCEPTED |
| C3 | RC-D03/R1-F01: inclusive 65,536-byte ceiling; no partial output; 65,537/1,000,000 refuse | `boundary-results.json`; B1 rows | same as C1 | Windows | Q1 | `e5fff9e8…` | ACCEPTED |
| C4 | RC-D03/R1-F05 + RC1-A §6: digest length rules (0/31/33/64/65,536 false; 32 true; all-zero present-not-wildcard); exact ordinal `Supports`; null programming errors; combined null+wrong-length (R2A-F04) | B4/B3 rows incl. `Q2A_B_11..14`, `Q2A_B_20..23` | same as C1 | Windows | Q1 | `e5fff9e8…` | ACCEPTED |
| C5 | R2A-F01: defensive re-encode branch reachability assessment; exercised as pass-through on every accepted decode | `reencode-branch-assessment.md` | `c1a01761…` | Windows | Q1 | `e5fff9e8…` | ACCEPTED |
| C6 | R2A-F02: clarified API document rebound in the evidence inventory | RC2A-02A `document-sha256.json` (API blob `2d6b9d12…` bound; re-verified in this record) | `c1a01761…`; blob verified at every checkpoint (see README §2) | hash binding | Q1 (29/29 hashes, 18/18 source bindings, 20/20 doc digests) | `e5fff9e8…` | ACCEPTED |
| C7 | R2A-F03: test-vs-binary reproducibility distinction (standing rule) | RC2A-02A E4 disclosure | `c1a01761…` | rule/disclosure | Q1 | `e5fff9e8…` §6 | ACCEPTED (standing) |
| C8 | Q2A-P: identical assertion set re-executed on WSL2 Ubuntu; Windows rerun | RC2A-02B evidence package (per-platform TRX/logs, `SHA256SUMS` 43/43, source binding) | `dd16baec48663d40cd29504d98e57e30dd0855df`; R1 final `1cd22b9b…` / tree `2f2bae94…` | Windows 11 native + WSL2 Ubuntu 26.04.1 (SDK 10.0.113); identical required reruns; matching multisets | Q1 `5bc396c2…` (found F-01/F-02) → R1 correction → Q2 `60de1f2d34a7df337a426501cb3954fdd3216a6b`: VERIFIED WITH QUALIFICATIONS (29 artifact refs, 43/43 manifest, 9/9 doc bindings re-verified) | `dc02951902f11031f8b0d225d2afa2cd537296a3` | ACCEPTED |
| C9 | RC-D04 (passive reference): four nonempty GUIDs + exactly-32-byte digest; named refusal; defensive copy; immutable storage; content equality/hash; all-field inequality | RC2A-02C evidence package (13-row matrix, `public-surface-inventory.json`, runs/windows/final) | `62b75f5866d0c339816712baeb04baa386b06f85` / tree `66f996e1cf973e48487a073b07f1e78d9139c911`; tested code `c4837b0…` / tree `d47eb617…` | Windows; focused Release build 0/0; 3/3 tests; reflection inventory (18 exported types) | Q2 review `9534581c34d55c1219e90ddcd290900583098388` + re-verification `f8b692e04941462ea592d9653960191cafb937cc`: SUPPORTED WITH QUALIFICATIONS, 13/13; build/tests reproduced twice; 40/40 digests, 16/16 bindings, 14/14 links | `a36cb684ad75cebf3ba778a6c86d42d7f5f2d6f6` | ACCEPTED |
| C10 | Exact approved public surface; no temporal/provider/release/retained-state/lookup/control member; inherited framework members recorded separately | RC2A-02C `public-surface-inventory.json` (byte-identical across qualification, review, and rerun inventories) | same as C9 | Windows; reflection | Q2 (13/13 incl. Q2A-S-08/09/10) | `a36cb684…` | ACCEPTED |
| C11 | R1-F04: no temporal fields; consumer-facing documentation (D4a enrolled/synthetic; D4b provider-origin only) | RC2A-02C `documentation-review.md` bound to RC-1 §RC-D04; source XML at `RestrictedPreparationReference.cs` | same as C9 | documentation inspection | Q2 (Q2A-S-13) | `a36cb684…` | ACCEPTED |
| C12 | Stage-wide exit criteria E1–E8 all satisfied (union of executed matrices on the primary platform; vector reproducibility; hygiene; review exit) | the full-stage gap assessment §5 (`docs/assessments/rc2a-roadmap-01-restricted-execution-dependency-review.md` on branch `assessment/gagamba-rc2a-02-full-stage` at `058a150130c5f112c7e9b8189bba8842cf0cec52`, tree `b2211a26…`, referenced by identity); manifests independently recomputed (02A 29/29, 02B 43/43, 02C 40/40) | `058a150…`; parent `a36cb684…` | cross-stage reconciliation | RC2A-02-FSA-01 (this assessment is the independent record) | `9519e7cb612af6ff69c93c0d128553138533d9fd` | ACCEPTED |
| C13 | Component identity: the qualified component is exactly the accepted component (no drift) | No-drift verification across nine chain checkpoints (README §2; `source-binding.json`) | `68b01fbb…` → `9519e7c…` | Git object-ID comparison (read-only) | this record; to be independently re-run at review | pending (this record) | PREPARED — review pending |

## 2. Inherited qualifications and preserved findings

| # | Qualification / finding | Source decision (binding) | Effect on this component record |
|---|---|---|---|
| Q1 | F01 checksum-count erratum (39/39 → 40/40) | RC2A-02C acceptance §4 at `a36cb684…` | carried verbatim; immutable `validation.md` untouched |
| Q2 | F03 digest-bound large vectors (`Q2A-V-19/20`) | RC2A-02A acceptance §6 at `e5fff9e8…` | no embedded-payload or byte-level-audit claim created |
| Q3 | R2A-F03 no-binary-claim standing rule | RC1-A `9a705335701f0c1923bd9867a2612b8a93afa8cb`; RC2A-02A E4 | no binary/package/deployment reproducibility claim |
| Q4 | RC2A-02B Q1 authorization-provenance/audit-trail note | RC2A-02B acceptance §4 at `dc029519…` | PRESERVED AS DISCLOSED |
| Q5 | Passive-reference boundary (no provenance/authority/temporal/release/lifecycle) | RC2A-02C acceptance §5; full-stage acceptance §6 | restated in README §4; no downstream enforcement claim |
| Q6 | Native Linux/macOS NOT RUN; WSL2 never native Linux | RC2A-02B acceptance §5; plan §3 P2; full-stage acceptance | platform limits restated in README §4 |
| Q7 | Q2A02A-F01 canonical-rigidity plan correction | RC2A-02A acceptance §5 at `e5fff9e8…`; plan §8 | used as-is for C2's flip-sweep row |
| Q8 | Broad-filter 41 skips disclosure (F02 clarification) | RC2A-02A acceptance §6 | provenance-only context; precise filter authoritative |
| Q9 | RC2A-02 full-stage qualifications (E2/E6 compatibility) | full-stage acceptance §5–§6 at `9519e7cb…` | inherited wholesale; nothing re-opened |

## 3. Explicit exclusions (out of component scope)

| # | Exclusion | Authoritative source |
|---|---|---|
| X1 | Live execution authority; authority issuance/verification; restricted-execution facade | RC-1 §2 (RC-1B/RC-2B); impl plan §3; full-stage acceptance §8 |
| X2 | Native confinement, runtime establishment, stop/termination/correlation, recovery/lifecycle | plan §6 exclusions; impl plan §3 B1–B7; RC-T14/18–23 |
| X3 | Package-level and clean-consumer compatibility; publication | RC2A-02 plan §1/§5; RC-T12/T17 = RC2A-04 |
| X4 | Hufu authority integration (AE4-B01, D4a/D4b) | impl plan §6; HUFU-R/HUFU-D4B gates |
| X5 | RC2A-03 owner sign-off itself | this record is review-pending; assignment forbids owner acceptance |
