# Q1 findings and carried qualifications

## Q1-E01 — Earlier producer probe omits escaped AuthorityHash member spellings

Severity low. Classification evidence limitation. Does not block adoption.
Scope is the independent review of the documentation, with the
accepted schema remaining the authority.

- **Normative rule:** BB §4 matches decoded ordinal field names including
  JSON escapes. BJ-03 restricts the digest value token interior to 64
  literal lowercase hex characters.
- **Candidate location and observed rule:** §§1 and 3 incorporate the owner
  annex and require decoded ordinal names. The record correctly implements
  the distinction in prose.
- **Reproduction:** Owner `mechanical-checks.md` (blob
  `90e97bf480e336f7d17717f4484507161942aca7`, function `check`) searches
  for the literal key bytes with a regular expression. The R1 reproduction
  imports that probe. Q1 J12 demonstrates that the old regular expression
  has no match when the A in the member name is expressed as a valid
  JSON escape. The independent check accepts J12 under the exact schema.
  H14 escapes one hex digit in the value and refuses.
- **Impact and bounded correction:** A conformance suite limited to that
  earlier probe would miss an interoperability requirement. Use the Q1
  controls as the oracle for this distinction and preserve the valid escaped
  name in eventual implementation qualification. Historical measurements
  on their original literal-key inputs remain valid. Candidate edits and
  further normative choices are unnecessary.

## BB-02 — Existing integrated v2 implementation nonconformance

Separate implementation nonconformance confirmed by static source review
under BB §3 and candidate §6. QualifiedRecoveryConnection.Start with
default serialization can emit B2InstallationIdentity and B2DeadlineNs
as null in integrated v2. RecoveryProtocol recognizes both and can let
the null values through the integrated presence check. Normative B2 §2
forbids both members. Per BB-01/03 and candidate §§2/3/6, presence at any
value refuses and that legacy implementation is outside the isolated
v3-pre scope. E-3 cannot rely on the legacy behavior. Preserve the
unremediated issue and require separate authorization and regression
qualification before any affected code is reused or exposed. No
compatibility extension, candidate defect or Q1 adoption blocker follows.

## Accepted limitations and trigger-based qualifications

- Twelve original artifact byte identities and their relevant acceptance
  bindings were verified. Historical external Git ancestry is unverified
  and no new ancestry is claimed.
- Static and synthetic proof concerns structural data only. Future
  implementation must prove refuse-only reachability, exact conformance,
  preserved RELEASE policy and candidate-specific P4 or other required
  regressions before any separate implementation acceptance.
- E-2 remains formally closed with 39 retained qualifications and 23
  downstream obligations. Preserve F-01 protected-reader and exact-case
  policy allocation to the E-4/E-5/E-6 dependency set with Slice N, P0
  external-package durability, unassigned first E-1 production consumer,
  P0/D1/P1/P2 acceptance and E-2's Q-E1 subset.
- Preserve accepted RELEASE/expected Debug refusal, F-R1/O-R1, O-R2,
  O-R3 and the observation-to-effect residual, E-4/E-6 P4 re-audits,
  `NATIVE-PENDING` and all 331 unqualified skips.
- E3-ENTRY-F01 and composite Q-E1 stay open. Owner adoption, entry, codec
  work, downstream implementation, operational approval, positive validation
  and protected execution remain separately gated.

No material candidate-content mismatch, incompatible controlling rules,
unresolved normative encoding or evidence blocker was found. The Q1
evidence limitation is bounded by independently exercised controls.
