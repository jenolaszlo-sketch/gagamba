# P2-Q1 — approved P1 reuse, authority construction, and production reachability

All checks below were executed by the reviewer against the compiled `9b80969`
assemblies through the reviewer-owned probe (source in `probe/Program.cs`,
raw output in `executed/probe-run.txt`). The probe re-implements the IL operand
walker; `REACH-01` is its positive control (a known intra-core call token in
`TryVerifyDetachedEvidenceChain` is detected).

## 1. Approved P1 reuse (compiled call tokens)

The reviewer resolved every `InlineMethod`/`InlineTok` call token inside every
method and constructor of the five P2 types and mapped each to its declaring
type and member. The complete target surface is printed in
`executed/probe-run.txt` on the `P1R-SURFACE|` line. The only non-P2, non-BCL
targets are:

- `CanonicalJson.Parse`, `CanonicalJson.Hash`, `CanonicalJson.IsLowerHex`
- `FrozenSchemaRules.IsValidId`
- `FrozenContractVerifier.TryVerifyCatalog`
- `CatalogContract.get_*` reads: `ActionScriptSha256`, `ArtifactSetSha256`,
  `CaseId`, `FixtureManifestSha256`, `FixturePolicySha256`, `HostId`, `Phase`,
  `QualificationId`, `QualificationRecordSha256`, `Topology` (exactly the
  values compared to R/Wc/P; no other catalog member is read)
- compiler-generated closures nested inside P2 types

`P1R-01` PASS: nothing else is called (no `FinalAcceptanceCodec`, no
`Installed`/`Lifecycle`/`Launch`/`Restricted`/`Bootstrap`/`Preparation`/
`Recovery`/`Native` type; no F02 store, no trust store, no authority type).

Source files were verified unchanged by blob identity (see
`baseline-and-revision.md`): the four P1 files match their P1-accepted blobs,
and `FinalAcceptance.cs`/`eng/**` have no later commit.

## 2. Authority-construction audit (P2 surface)

| Check | Result |
|---|---|
| `AUTH-04` — verifier is an internal static class; no public method; all five P2 types internal, none exported | PASS |
| `AUTH-03` — no type or member name contains `Approved`, `Authorized`, `Permit`, `Authority`, `Trusted`, `Capability`, `Eligible`, `VerifyApproval`, `Issue`, `Publish`, `Provision`, `Approve` | PASS (clean) |
| `AUTH-06` — no promotion/factory/issuance method in the 141-member declared surface | PASS |
| `AUTH-02` — P's `accepted` literal is never projected; the P content contract has exactly the 11 accepted content properties, no `Accepted` flag | PASS |
| `P1R-01` — no reference to any existing authority-bearing type (no R/Wc/P issuance, no V/W/A actor, no protected root, no approval store, no invocation permit, no positive-validation factory, no protected execution call) | PASS |
| `SUB-11` — no verification entry accepts a caller-supplied expected digest or authority string; the four `TryVerify*` entries require at least the catalog plus the preceding record bytes | PASS |
| `AUTH-05a–h` — reviewer/owner/administrator identity, timestamp, nonce, signature, and approval-claim injection in canonical ascending position is refused as the exact-field-set rule (`R/Wc/P field set`), never read as authority | PASS |

`internal` visibility was not treated as a safety argument: the audit used
compiled call tokens, signature types, declared member names, and the exact
result-record property surface.

## 3. Production reachability (Debug and compiled RELEASE)

Reviewer IL scans over every method body of every non-Validation type:

| Check | Debug | Release |
|---|---|---|
| `REACH-*-01` zero production direct-callers of P2 member tokens (154 target tokens) | 0 offenders | 0 offenders |
| `REACH-*-02` zero production type-token references to P2 types (reviewer-added) | 0 offenders | 0 offenders |
| `REACH-*-03` zero production string literals naming `DetachedEvidence` (reviewer-added reflection-name probe) | 0 offenders | 0 offenders |
| `REACH-*-04` no Validation-namespace exported type | PASS | PASS |
| `REACH-*-05` all five P2 types present and internal | PASS | PASS |
| `REACH-*-06` no production method/ctor/property/field signature consumes a P2 result type | 0 offenders | 0 offenders |

The producer's own reachability tests (`DetachedEvidenceReachabilityTests`)
were re-executed inside the 33/33 P2 filter and the full suite; the reviewer
additionally ran the independent scans above.

**Retained scope limitation (P1-Q1-F05 lineage):** zero direct-call and
type-token evidence does not constitute a complete transitive or
reflection-aware unreachability proof. No stronger claim is made. Two
reviewer-added probes (type tokens, reflection-name strings) strengthen but do
not close that bound.

## 4. Failure handling and robustness observations

- All 23 malformed-byte vectors and every refusal path returned
  `false` + null contract + exact deterministic diagnostic with **no escaped
  exception** (`escaped=False` on every check).
- The only observed exception escape is a `NullReferenceException` when a
  `null` `byte[]` is passed (`ROB-04`). This argument is outside the accepted
  byte-array domain of the internal `TryVerify*` contract; the reused P1 entry
  shows the identical behavior (`ROB-06`); there is no production caller and no
  malformed-*bytes* path to it. Recorded as bounded observation Q1-F02, not a
  demonstrated defect.
- Inputs are never mutated on success or refusal (`ROB-03`); repeated calls are
  deterministic with stable digests (`ROB-01`, `ROB-02`); no hidden mutable
  state between calls (`ROB-05`); no I/O, environment, process, or network
  dependency exists (code inspection: pure functions over the supplied arrays).
