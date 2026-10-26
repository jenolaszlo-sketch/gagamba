# RC2A-02C owner acceptance — Q2A-S / RC-D04 passive reference

Date: 2026-10-26 (Asia/Manila).
Decision: **Q2A-S / RC-D04 ACCEPTED WITH QUALIFICATIONS (RC2A-02C).**

This decision records the owner disposition of the RC2A-02C focused
qualification of the bounded passive managed-code preparation reference and
the R1-F04 documentation boundary on Windows. It does not accept full
RC2A-02, qualify RC-2B live operations, provider-origin preparation,
deadlines or timing guarantees, release or retained-state behavior, native
confinement, packaging or distribution, reproducibility beyond the
demonstrated qualification profile, Hufu integration, end-to-end execution,
or RC2A-03 or any later milestone.

## 1. Owner decision

The owner accepts Q2A-S / RC-D04 on the basis of the committed RC2A-02C
qualification evidence and the focused independent review preserved on
`review/gagamba-rc2a-02c-q2`. The independent review verdict is
**SUPPORTED WITH QUALIFICATIONS**: all 13 scoped matrix rows are
independently supported, the contract behavior and the exact public API
surface are verified, the Windows Release build and the focused three-test
run were independently reproduced twice (0 warnings / 0 errors; 3 passed /
0 failed / 0 skipped, exit 0), the evidence integrity checks (40 package
digests, 16 document bindings, 14 relative links, source binding, matrix
chronology) were independently confirmed, and no blocking defect was found.

The single review finding, **Q2C-Q2-F01** (a nonblocking P3 documentation
discrepancy in the package checksum count), is accepted and corrected by the
explicit erratum in §4. This is the owner's disposition; it is not a new
qualification, review, or execution run. No production, test, native, or
raw evidence artifact is changed by this decision.

## 2. Acceptance basis

| Basis element | Independently verified record |
|---|---|
| All 13 Q2A-S requirements supported | Review requirement-by-requirement disposition: 13/13 SUPPORTED against RC-1 §RC-D04, the frozen RC2A-01 API, the owner assignment, and the pre-execution matrix |
| Contract behavior verified | Four nonempty GUID identities and exactly 32 digest bytes enforced by parameter name; digest defensively copied and immutable; content equality and equal hash codes for separately allocated equal inputs; inequality when any GUID or digest byte changes |
| Exact public API surface verified | Runtime reflection inventory: sealed type, one constructor `(Guid, Guid, Guid, Guid, ReadOnlySpan<byte>)`, exactly five get-only properties, `IEquatable<T>`, typed/object `Equals`, `GetHashCode`; no operators, events, public fields, or nested types; no temporal, provider, release, retained-state, lookup, or other control member; inherited framework members recorded separately |
| Independent build and focused test reproduction | Release build exit 0 with 0 warnings / 0 errors; focused filter reproduced 3 passed / 0 failed / 0 skipped with exit 0, twice (2026-10-25 and 2026-10-26), matching the committed final TRX test names and outcomes; surface inventory byte-identical across the qualification run and both independent runs |
| Evidence integrity confirmed | 40/40 `SHA256SUMS` digests, 16/16 LF-normalized document bindings, 14/14 relative links, source/test/native tree bindings, pre-execution matrix ancestry and chronology (matrix `66a66bf` precedes every test commit) all independently recomputed |
| No blocking defects | No contract defect, malformed-input acceptance, false-positive surface or support result, misbound evidence, or scope-exceeding claim found; the initial restore failure and the sandboxed testhost timeout remain preserved as environmental observations and are not counted as passing evidence |
| Q2C-Q2-F01 | Accepted as a nonblocking documentation discrepancy and corrected by the erratum in §4, without modifying the immutable evidence set |

The RC2A-02C stage met all eight acceptance criteria recorded by the owner
in the [Q2A-S / RC-D04 assignment](gagamba-q2as-rcd04-owner-disposition.md#6-rc2a-02c-acceptance-criteria):
exact tree binding with a pre-execution matrix; constructor validation and
defensive/immutable digest storage; content equality, hash, and per-field
inequality; the exact approved passive public surface; the R1-F04
documentation statement with D4a/D4b timing sources; a focused Windows-only
managed-code run with captured command, exit code, TRX/log, identities,
environment, and hashes; no failure requiring remediation and no
package/native/RC-2B/Hufu work; and a focused independent review before this
disposition.

## 3. Baselines and Git identities

| Role | Branch | Commit | Tree | Parent |
|---|---|---|---|---|
| Owner assignment of RC2A-02C | `decisions/gagamba-q2as-rcd04-disposition` | `91612b9a42827ffdd76891819670623fa43daeed` | `5f9ec8a9d01627ee3ed0a245ca54642ae1413ea4` | `dc02951902f11031f8b0d225d2afa2cd537296a3` |
| Pre-execution matrix | `qualification/gagamba-rc2a-02c-q2as-rcd04` | `66a66bf56508b23b5730827a012ba730b702b72b` | `9e25e4b66d64276030b8d2d670f33989ad408ac6` | `91612b9a42827ffdd76891819670623fa43daeed` |
| Final tested code | `qualification/gagamba-rc2a-02c-q2as-rcd04` | `c4837b0b434fc75ebc3e08d278bdc09c3965a181` | `d47eb6173ecf7b4de442f0f9c165f3ef9e24c88d` | `e1be8579bcaca806071e88dcdd389fd2a054384b` |
| Qualification evidence (accepted state) | `qualification/gagamba-rc2a-02c-q2as-rcd04` | `62b75f5866d0c339816712baeb04baa386b06f85` | `66f996e1cf973e48487a073b07f1e78d9139c911` | `c4837b0b434fc75ebc3e08d278bdc09c3965a181` |
| Independent review report | `review/gagamba-rc2a-02c-q2` | `9534581c34d55c1219e90ddcd290900583098388` | `ae64a890e00c2b23eecb26d9ee776354e1ce9192` | `62b75f5866d0c339816712baeb04baa386b06f85` |
| Independent re-verification | `review/gagamba-rc2a-02c-q2` | `f8b692e04941462ea592d9653960191cafb937cc` | `2b6c9df6c7e5d55dec977ccc92a2d6078b74d4f9` | `9534581c34d55c1219e90ddcd290900583098388` |

The review report at commit `f8b692e04941462ea592d9653960191cafb937cc` is
`docs/reviews/rc2a-02c-q2-independent-verification.md`, blob
`e85924573a02bfebf1e2cb3aa97bc15ba151d0e2`, with review-only run evidence
under `docs/reviews/rc2a-02c-q2-evidence/`. The review branch is preserved
separately and is neither merged nor copied into this decision branch; its
commits are direct children of the qualification evidence tip, and its tree
leaves `docs/evidence/`, `src`, `tests`, and `native` byte-identical to
`62b75f5866d0c339816712baeb04baa386b06f85`.

At the accepted state, `src` is
`b8c167e3d7340a562221591bdc2b6df7adb372d1`, `tests` is
`9c906b349b0bf0cffdee331fb5700f9b7828c199`, and `native` is
`85631d2f04512376621d3d09f51d67fd90f45335`. The `src` and `native` trees
equal the owner-assignment baseline `91612b9a42827ffdd76891819670623fa43daeed`;
the only code change from that baseline is
`tests/Gagamba.Execution.Tests/Restricted/RestrictedContractTests.cs`. The
authoritative requirement is RC-1 §RC-D04 at
`d609c29d3048bc0fd91583068cb013fb906fdf90` (design blob
`4b300b01397e7f68d978fadbc36164dda17c8721`); the frozen public API contract
blob is `2d6b9d12cab02f092c6b6e0b690138cf6726f12b`, unchanged since the
RC2A-02A acceptance. The evidence package is
[docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04](../evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/README.md).

The owner-decision branch is
`decisions/gagamba-rc2a-02c-owner-acceptance`, created in an isolated
worktree from the exact qualification evidence tip
`62b75f5866d0c339816712baeb04baa386b06f85`. The decision commit is a
documentation-only direct child of that tip; its identity is reported in the
completion record to avoid self-reference.

## 4. Erratum: Q2C-Q2-F01 — package checksum count in `validation.md`

- **Location:** `docs/evidence/restricted-consumer-contract-rc2a-02c-q2as-rcd04/validation.md`,
  "Integrity manifests" section, line 74: "**16/16 document bindings** and
  **39/39 package checksums passed**".
- **Defect:** the package checksum count is understated. The committed
  `SHA256SUMS` manifest contains **40** entries, covering every package file
  except `SHA256SUMS` itself, including `.gitattributes`. The independent
  review recomputed every digest against the checkout and the committed
  blobs: **40/40 match**. No digest is wrong, and no artifact is missing,
  added, or altered; the document-binding count (16/16) is correct.
- **Correction:** read "39/39 package checksums passed" as "**40/40 package
  checksums passed**".
- **Mechanism:** `validation.md` is part of the immutable, hash-bound
  qualification evidence set (it is bound by the package's
  `document-sha256.json` and covered by `SHA256SUMS`). Per the disposition
  instructions, the reviewed artifact and both integrity manifests are
  preserved unmodified; this erratum in the owner decision is the
  authoritative correction, and the original checksum evidence is not
  regenerated, modified, or reinterpreted.
- **Disposition: ACCEPTED AS CORRECTED BY ERRATUM.** The discrepancy is
  nonblocking: it does not affect the digests, the document bindings, the
  run evidence, the code binding, or the qualification verdict.

Binding note: the package's `document-sha256.json` and `SHA256SUMS` bind the
repository tracking documents (`docs/handoff.md`, `docs/work-queue.md`, and
the two plan documents) at their `62b75f5` state. This decision updates
those trackers; the evidence package manifests intentionally remain the
reviewed snapshot and are not regenerated. Package integrity validation
therefore remains exact at commit
`62b75f5866d0c339816712baeb04baa386b06f85`; post-disposition validation of
the updated trackers is bound by this decision commit instead.

## 5. Accepted scope and exclusions

Acceptance applies exclusively to:

- Windows passive-reference behavior of the sealed
  `RestrictedPreparationReference` public managed value;
- the 13 Q2A-S matrix requirements recorded in the pre-execution matrix:
  four nonempty GUID identities and the exactly 32-byte digest; named
  refusal of empty GUIDs and non-32-byte digests; defensive copy of mutable
  digest input; immutable digest storage; content equality and hash-code
  consistency; all-field equality sensitivity; the exact approved passive
  public surface; absence of temporal authority; absence of provider,
  release, retained-state, or control members; absence of any live-provenance
  implication; absence of any digest recomputation or attestation
  requirement; and the R1-F04 documentation boundary (D4a timing from
  explicit enrolled/synthetic data, later D4b timing only from an actual
  provider-origin prepared execution, no temporal or liveness inference from
  the reference);
- the RC-D04 / R1-F04 documentation and qualification slice recorded in the
  evidence package at commit
  `62b75f5866d0c339816712baeb04baa386b06f85`.

Acceptance does not establish: provider-origin preparation; deadlines or
timing guarantees; release or retained-state behavior; native confinement;
packaging or distribution; reproducibility beyond the demonstrated
qualification profile; Hufu integration; end-to-end execution; or behavior on
any platform other than the recorded Windows environment (.NET SDK 10.0.401).

**Full RC2A-02 remains NOT ACCEPTED.** The Q2A-S / RC-D04 obligation is
dispositioned as accepted with qualifications within the recorded scope
only; the remaining RC2A-02 acceptance criteria still require their own
evidence and a separate explicit full-stage owner acceptance. RC2A-03 has
not started and is not authorized by this decision; no next scope is
authorized by this decision.

## 6. Preservation verification

- The qualification branch `qualification/gagamba-rc2a-02c-q2as-rcd04`
  remains at `62b75f5866d0c339816712baeb04baa386b06f85` with a clean
  worktree; no merge, rebase, reset, or push was performed.
- The review branch `review/gagamba-rc2a-02c-q2` remains at
  `f8b692e04941462ea592d9653960191cafb937cc` with a clean worktree.
- This decision changes no file under `docs/evidence/`, `src`, `tests`, or
  `native`; the protected evidence package, its manifests, `validation.md`,
  and all captured run artifacts are byte-identical to
  `62b75f5866d0c339816712baeb04baa386b06f85`.
- Q1 `5bc396c2e5619906ae202afe0a4244dfd6024d9e` and Q2
  `60de1f2d34a7df337a426501cb3954fdd3216a6b` remain unchanged on their own
  branches and are not ancestors of this decision branch.

## 7. Remaining obligations

| Obligation | Status after this decision |
|---|---|
| Q2A-S / RC-D04 (passive reference + R1-F04 documentation) | **ACCEPTED WITH QUALIFICATIONS** within the recorded scope |
| Q2C-Q2-F01 | **ADDRESSED** — accepted as a nonblocking documentation discrepancy, corrected by the §4 erratum |
| Full RC2A-02 acceptance | **PENDING** — requires the remaining stage criteria and a separate explicit owner acceptance |
| RC2A-03 / RC2A-04 | NOT STARTED; require separate owner authorization |
| RC-2B live operations (R1-F02/F03), provider-origin preparation, deadlines, release, retained state | OPEN; outside this and all prior RC2A-02 acceptances |
| Native confinement/sandbox, native Linux/macOS, packaging/clean-consumer, binary reproducibility, Hufu integration, end-to-end execution | NOT QUALIFIED; unchanged by this decision |

## 8. Decision

**Disposition: Q2A-S / RC-D04 ACCEPTED WITH QUALIFICATIONS** on the basis of
the RC2A-02C qualification evidence at
`62b75f5866d0c339816712baeb04baa386b06f85` and the independent review
verdict **SUPPORTED WITH QUALIFICATIONS** at
`f8b692e04941462ea592d9653960191cafb937cc`, with Q2C-Q2-F01 corrected by
erratum. This is a documentation-only disposition; it starts no
implementation, commissioning, packaging, or release activity.
