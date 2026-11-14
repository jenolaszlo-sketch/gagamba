# Alternative B Slice E-3 Q-E1 provenance reconciliation

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-PROVENANCE-RECONCILIATION-01`

Date 2026-11-14 (Asia/Manila)

## 1. Baseline and binding question

Canonical Gagamba `main` started at
`52b932aaebd52f16bd93170b95bb0f8e89241fd2`, tree
`5406e8071008393d05b96f343d274f0114946e19`. The sole worktree was
`C:/Users/Laszlos/source/repos/Gagamba`, with clean tracked files and index,
zero stashes, and 44 of 44 historical untracked paths SHA-256-identical.
The recorded `origin/main` was `a30d1807686ee15d39ef7e9103e8b992062bd971`,
3 ahead and 0 behind. No conflicting writer was observed in the Git process
and lock checks. All checks are local.

The [E-3 entry assessment](rc2b-06-b6-alt-b-slice-e-03-entry-assessment-01.md)
is the starting commit's report, blob `d2cb4e3911f7aeb3e0e9cd955cdfbd12147dfc12`,
evidence tree `196fdd6188c669014861e98a36736d57ebb63467`. E3-ENTRY-F01 blocked
two of sixteen entry checks (10 satisfied, 4 satisfied with qualifications,
2 blocked). Slice E entry review §8 and Slice E owner §§7, 9 require an
authoritative, committed and adopted v3-pre layout record before codec work.
The [E-2 owner closure](../decisions/rc2b-06-b6-alt-b-slice-e-02-closure-owner-01.md),
commit `21179680e7ae5d153c7ddeef5d209e5ac450cdd7` and blob
`3388894f821a4a92aca7c2b2d79c832a0f70a450`, remains binding. No new
evidence invalidates E-2.

## 2. Source recovery and authority

All 12 selected local source and support objects were available. Each
original-byte SHA-256 matched the complete historical identity. The
[source inventory](rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01-evidence/sources-and-verification.md) records the
locations, lengths, encodings, raw hashes, manifest bindings and scope.
The exact F02 recheck manifest, the actor/descriptor contract, the case/action
contract and the Mechanism B capability contract were recovered. The
Alternative B compatibility, handoff and independent assessment files and
their manifest links also matched. The selected F02 source files were
byte-identical to the corresponding `recheck-input` copies. The review
used direct files without archive extraction or line-ending normalization.

The F02 owner decision §2 incorporates the exact 15-file recheck submission
as design evidence. The companion owner matrix C04 and Q04/M05/M06 retains
a separate wire/compatibility amendment gate, and the actor contract lines
46–48 calls the v3 parser a proposed owner-reviewed amendment. Alternative B
§§3.3–3.5 adopts the integrity, phase and sequencing design, with §4 binding
its compatibility source. Slice E and E-2 closure carry the v3 layout
reconciliation explicitly forward. Original `PENDING` captions are preserved.
These sources establish provenance and design scope. They provide neither
independently verified external Git history or authorship attestation nor
an adopted E-3 field-level specification. The [precedence review](rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01-evidence/authority-and-gaps.md)
records the accepted inheritance and the remaining owner decisions.

## 3. Verified extraction and material gaps

The [field extraction matrix](rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01-evidence/layout-and-correlation.md) traces
the directly specified delta, inherited layouts and unresolved properties
to the matched sources and committed v2 bases. The source calls for an
explicit v3 validation-pre bootstrap, an 8,192-byte JSON bound, the two
added authority fields, a separate integrity projection, status byte zero
only in the selected pre layout, and refusal of legacy or mixed phase/version
interpretations. B2 37 and 9/36 inherit the accepted v2 binary layouts except
for that status byte. In B2, 37 status is at offset 216 and the exact 76-byte
9/36 status is at offset 71. Message 10 and B2 confirmation 12 confirm
matched expected digest/context. Their positive result has no S2
qualification meaning.

Two material choices remain:

- **V3 JSON identity and binding.** The frozen source names the inherited
  identity placement, the pre phase, null qualification association and an
  `AuthorityHash` of 64 hex characters. It does not unambiguously map binary
  status zero to the bootstrap JSON identity field name/type/encoding,
  choose the exact v3 JSON representation for the null association, or
  state the exact case grammar and normative context comparator for the
  new hash field. INIT binds capability and catalog digests. Its presence
  alone does not choose the `AuthorityHash` comparator. An exact byte
  acceptance and mismatch rule requires an owner interpretation or a
  separately bound controlling source.
- **Integrated 9/36 echo representation.** The accepted integrated base
  retains a length-prefixed optional UTF-8 profile tail. The B2 amendment
  fixes the B2 form at 76 bytes and expressly leaves integrated v2 alone.
  Existing code provides a two-u8-length, exact profile-string convention.
  The verified design sources do not settle that precise tail encoding for
  a new integrated v3 contract. Bind the exact inherited integrated
  encoding or adopt an explicit v3 decision about tail presence and bounds.
  The B2 rule cannot be extended to integrated v3 by inference.

The source distinguishes the numeric bootstrap record version from inherited
message/protocol constants. There is no evidence for a changed frame header,
new echo result codes, new CJSON-1 dependency, generic canonical JSON byte order,
or a new v3 content hash. The review retains existing strict shape and numeric
rules only where the accepted v2 sources bind them.

### Release correlation and milestone allocation

The exact formula in F02 case/action §5 and Alternative B compatibility lines
32–36 agrees byte for byte. The result is 32 raw bytes in the existing
ReleaseRequest approval-correlation field. Its inputs include the original
session and preparation and the fixed script action. It is not a
bootstrap, 37, 9/36, 10/12, or GAVA1 RELEASE field. E-3's codec preserves
the inherited identity meanings and keeps any later validated context
structurally separate. The accepted sources specify no E-3 RELEASE digest
serialization, computation or comparison. E-4 owns the distinct
GAVA1 wire/header contract and context handoff. E-6 retains operational
owner-side recomputation and enforcement at the atomic gate.
Composite Q-E1 remains open.

### Independent checks and retained gates

Two SHA-256 tools agreed over raw bytes. A mechanical read of the
hash-bound historical S3 B2 vectors confirmed the base lengths, role
status placement and confirmation shapes independently of the production
parser. The actor source supplied the zero-status delta. Separate
primary texts agreed on the correlation formula and its 258-byte input
width. No v3 fixture, implementation test, or derived expected production
bytes were executed or committed.

The active inherited trigger is E-3's composite Q-E1 provenance and exact
extraction. Historical captions and the spot-check/single-reviewer bounds
still apply to their predecessor records. P0/D1/P1/P2 remain accepted under
their original dispositions. There is no demonstrated P1/P2 reuse dependency.
Preserve all 39 retained qualifications and 23 downstream obligations by E-2
owner §§5–7 and assessment §§21–22 at blob
`b75e1a5e2a7b52c393b919ff54ef1dd68ca49779`. First E-1 production-consumer
ownership and P0 external durability remain open and unassigned.

The E-2 F-01 ruling retains protected K/M/F/S readers and exact runtime
case policy at E-4/E-5/E-6 plus Slice N, with E-6 accountable for the complete
plan §3.5 set. RELEASE verifier policy, its Debug negative control, the
accepted P4 boundary and F-R1/O-R1, O-R2, O-R3 and observation-to-effect
residual are preserved. E-4 and E-6 P4 re-audits remain required.
`NATIVE-PENDING` and all 331 unqualified skips remain in force.
These documentation findings establish no operational approval, protected
acquisition, live invocation or protected execution.

## 4. Formal reconciliation disposition

**QE1 NORMATIVE RECONCILIATION REQUIRED**

Source recovery succeeded. The two open wire choices prevent a complete,
implementable field-level record. The evidence matrices contain the verified
extraction and its limits. A standalone proposed frozen specification and a
recommendation for adoption are withheld pending resolution of those choices.

The proposed next commission is
`GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-03-QE1-NORMATIVE-OWNER-01`, a separately
commissioned documentation-only owner ruling on the two choices in §3, or
recognition of exact controlling evidence that settles them. Then complete
the proposed specification and verify its exact bytes before a separate
owner adopts it. E3-ENTRY-F01 stays open until the required adoption.
Entry and implementation authorizations are separate. This commission
grants neither and leaves E-4 through E-7 and Slice N separately gated.
