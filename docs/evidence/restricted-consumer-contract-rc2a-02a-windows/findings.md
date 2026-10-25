# RC2A-02A findings register

Date: 2026-10-25 (Asia/Manila). Scope: Windows-executable RC2A-02A qualification
families Q2A-V, Q2A-F, Q2A-B and Q2A-E against test source commit
`71a28afe2623516fb4176e6a4ce7988a3d70df06` (tree
`70b932148cbfa59b0e424229879eebd36df2d80c`). This register classifies only
findings observed in this assignment. No production source changed.

Classification vocabulary (assignment §8.D): `CONFIRMED DEFECT`, `TEST
EXPECTATION ERROR`, `EVIDENCE GAP`, `ENVIRONMENTAL LIMITATION`,
`RECOMMENDATION`.

## Q2A02A-F00 — No confirmed production defect

Classification: **NONE (statement of result)**. Severity: informational.

No confirmed production defect was found. Every executed refusal case matched
its contract-derived `ManifestDecodeError` code and exact construction-derived
offset; every accepted vector and mutation decoded to the expected graph,
re-encoded byte-exactly and produced the independently recomputed SHA-256.
There is no observed counterexample to the accepted schema-1 contract.

## Q2A02A-F01 — Plan B6 single-byte sweep characterization is incorrect

Classification: **TEST EXPECTATION ERROR** (specification row; no production
defect). Severity: Low.

The committed RC2A-02 plan §3 B6 states:

> A single-byte flip sweep over the golden asserts that no mutation outside the
> two raw 32-byte digest fields is accepted; mutations inside digest fields are
> valid present values, and the sweep's survivor set is compared against the
> committed digest-field offset ranges as evidence.

Execution disproves the literal claim. Of 222,615 single-byte mutations over the
873-byte golden (every byte position set to each of its 255 alternative values),
114,378 are accepted. Accepted mutations include:

- the 128 raw digest bytes (four 32-byte fields) and other scalar values
  (`u64`/`u32` budget values, lengths, limits, owner-allocation counters);
- free-text content bytes (mode, units, paths, roles, destinations, build
  identities, namespace/read-only/helper/argv/environment strings);
- free boolean and budget-kind/qualification tag bytes; and
- **length-prefix bytes**, where a mutated declared length produces a
  self-consistent re-segmentation of the same byte string into a *different
  canonical manifest* (for example byte 157, the first byte of the
  `readOnly.item[2]` string length, set to `0x3B`; the parser absorbs 59 bytes
  into the destination string and the remaining tail parses canonically).

Minimal reproduction:

```text
golden = Convert.FromHexString(QualificationGolden.Hex)   // 873 bytes
golden[157] = 0x3B
result = RestrictedManifestCodec.Decode(golden)          // success (no error)
RestrictedManifestCodec.Encode(result.Value) == golden   // true
```

The accepted bytes are canonical for the graph they parse to; the decoder did
not normalize anything. The defensive re-encode comparison (R2A-F01) therefore
remains a pass-through assertion and is not contradicted.

Qualification correction (committed as a documentation correction appended to
the plan, per plan §3's own rule): the sweep now asserts the real invariant —
**canonical rigidity** (every accepted mutation re-encodes byte-exactly) — plus
containment of survivors within the documented immutable regions (schema,
implementation ID, fixed paths, forced-true booleans, pending-only
qualifications). The complete survivor profile is preserved in
[`exports/flip-sweep-survivors.json`](exports/flip-sweep-survivors.json)
(576 survivor positions, accepted value counts per position). The first failing
run is preserved in
[`runs/failed-attempts/b-first-sweep-failure.trx`](runs/failed-attempts/b-first-sweep-failure.trx).

## Q2A02A-F02 — Exact plan filter selects out-of-scope Linux-native classes

Classification: **RECOMMENDATION**. Severity: Low.

The exact committed-plan internal command uses
`FullyQualifiedName~Qualification`. In the current assembly this substring also
matches five pre-existing Linux-native classes outside RC2A-02A
(`CoordinatorProtocolQualificationTests`, `OwnerDeathQualificationTests`,
`NonRootQualificationTests`, `Wp05V3IndependentQualificationTests`,
`Wp05ReleasePathIndependentQualificationTests`). On Windows the exact command
therefore reports 473 passed, 0 failed, **41 skipped** (514 total); all 41 skips
are those unrelated native classes, and none is an RC2A-02A case.

The required RC2A-02A set was additionally executed with the precise filter
`FullyQualifiedName~Tests.Qualification.` (plus the three committed RC2A-01
regression segments): 457 passed, 0 failed, **0 skipped** (runs R3 and R4).
Recommendation: future plan revisions should name a dedicated qualification
namespace token (or use `Tests.Qualification.`) so the declared command selects
pure tests only, as plan §3 P1 intends. No product implication.

## Q2A02A-F03 — Test-authoring corrections discovered during execution

Classification: **TEST EXPECTATION ERROR** (qualification test authoring only).
Severity: informational. No production behaviour was changed.

Preserved intermediate corrections (also listed in the evidence report's failed
attempts):

1. Vector V-19 initially constructed the invocation through a two-step property
   replacement after the baseline environment was applied, transiently exceeding
   the 16 KiB combined cap and throwing during emitter bootstrap. Corrected by
   constructing the complete `InvocationManifest` in one validated constructor
   call (`Q2A-V-19-max-argenv-16384`).
2. The A5 reduced-mode-length case initially wrote a *consistent* shorter mode
   (`length=3` with three content bytes), which is a valid alternative manifest
   and was accepted. Corrected to a genuinely inconsistent declared length
   (claims 3, writes 4 content bytes), which shifts parsing and refuses
   `InvalidLengthOrCount`. The corrected expectation is contract-derived; the
   original observation is retained here rather than hidden.
3. The single-byte sweep's first eligibility map was too narrow and flagged a
   legitimate length-prefix re-segmentation; see Q2A02A-F01.

## Q2A02A-F04 — Windows-only, managed-code qualification scope

Classification: **ENVIRONMENTAL LIMITATION**. Severity: informational.

All results are Windows 11 (NT 10.0.26200.0), AMD64, .NET SDK 10.0.401,
Microsoft.NETCore.App 10.0.12. No native Linux or macOS run was performed;
RC2A-02B portability qualification is outside this assignment. No package was
built, packed or published, and no native fixture, owner, helper or workload was
started. No cross-platform claim is made. The 41 collateral skips in run R2 are
the Windows-unsupported Linux-native classes described in Q2A02A-F02.

## Follow-up reconciliation (R2A-F01/F02/F03/F04)

| Finding | RC2A-02A result |
|---|---|
| R2A-F01 (re-encode branch untested) | **Addressed by assessment**: see [reencode-branch-assessment.md](reencode-branch-assessment.md). The branch executes as a pass-through on every accepted decode (114,378 sweep acceptances plus every vector/case). No observation of its `InvalidValue@0` failure path; no artificial production hook was added. |
| R2A-F02 (API doc digest missing) | **Addressed**: `docs/design/restricted-consumer-contract-rc2a-01-api.md` is bound by LF-normalized SHA-256 in [document-sha256.json](document-sha256.json), together with the preserved review identity, owner acceptance, RC2A-02 plan, field map, plan/queue/handoff and this evidence set. |
| R2A-F03 (DLL bytes not identical) | **Retained as disclosed**: no binary-reproducibility, package-identity or deployment-qualification claim is made in either direction. Only deterministic test outcomes and committed vector bytes/digests are claimed. |
| R2A-F04 (combined null+wrong-length unasserted) | **Addressed**: `Q2A-B-23` asserts `VerifySha256(null manifest, wrong-length digest)` throws `ArgumentNullException` at lengths 31 and 65,536; `Q2A-B-11..14` cover the empty argv item and zero-length argv list distinction. |
