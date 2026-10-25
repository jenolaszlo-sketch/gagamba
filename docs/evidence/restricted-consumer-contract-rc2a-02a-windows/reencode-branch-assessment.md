# R2A-F01 defensive re-encode branch assessment

Date: 2026-10-25 (Asia/Manila). Test source commit
`71a28afe2623516fb4176e6a4ce7988a3d70df06`. Classification: qualification
assessment, not a production change.

## Subject

`ManifestReader.Decode` performs a final exact re-encode comparison after bounded
parsing, immediately before returning a view (`src/Gagamba.Execution/Restricted/
ManifestReader.cs`, `Decode`, the `ManifestCodec.Encode(manifest,
RestrictedManifestCodec.MaxCanonicalBytes)` equality check). On mismatch it
refuses `ManifestDecodeError.InvalidValue` at offset 0. RC2A-01's independent
review (R2A-F01) noted that the branch has no direct triggering test.

## Claim

Under the accepted public input model, the branch's failure path is not
reachable as an independently triggerable rejection; the comparison is a
defensive pass-through assertion on every accepted decode. This assessment is
required by RC2A-02 plan §3 B5 and owner-acceptance follow-up R2A-F01.

## Control-flow reasoning

For an input to reach the comparison, it must already have satisfied the entire
bounded reader and all model constructors:

1. **Schema and implementation identity** are checked as soon as their fields are
   read (`UnsupportedSchema@0`, `UnsupportedImplementation@2`).
2. **Wire order and cardinality** are fixed by the sequential reader: every
   field is read at the canonical grammar position with the same tag widths,
   count widths and fixed spellings the encoder writes.
3. **Canonical set order** is enforced against `Utf8BytewiseComparer`, the same
   comparer the model constructors and encoder use; the reader refuses
   `NonCanonicalOrder` at the second item start and `Duplicate` at the second
   item start. The constructors' sorting is therefore the identity on accepted
   input.
4. **Lengths and counts** are validated against remaining input before
   allocation (`InvalidLengthOrCount`), and every read is bounds-checked
   (`Truncated` at end of input). Trailing bytes are refused (`TrailingData`).
5. **Encodings** are strict UTF-8 (`InvalidUtf8`) with no normalization; for a
   valid UTF-8 sequence decode→encode is byte-identical. Fixed path spellings,
   forced-true booleans and pending-only qualifications are checked at their
   field offsets.
6. **Model invariants** then subsume the remaining canonicality conditions
   (capability set empty, ambient denials true, exact tool/input/work paths,
   stdin EOF, core disabled, inode/network pending, budget kind/value
   consistency, non-empty required strings, unique sets, combined 16 KiB
   argv/environment bound).
7. The bounded encoder shares the same writer and grammar; because the accepted
   input is at most 65,536 bytes and re-encodes to the same length, the
   canonical stream bound cannot fail during the comparison.

Therefore every input that survives the reader re-encodes identically, and the
comparison succeeds. A crafted input that reached the comparison with unequal
re-encoding would be a production defect under the stop conditions; none was
observed.

## Empirical probes (executed in this qualification)

- **Complete single-byte sweep**: 873 byte positions × 255 alternative values =
  222,615 decoder calls. 114,378 accepted, 108,237 refused. Every accepted
  mutation was re-encoded and compared byte-for-byte; all 114,378 matched
  exactly. The refusal `InvalidValue` at offset 0 occurred **zero** times.
  (`Q2A_B_18_SingleByteFlipSweepEnforcesCanonicalRigidity`,
  `Q2A_B_19_ReencodeBranchAssessmentProbe`; evidence
  `exports/flip-sweep-survivors.json`.)
- **All 23 committed vectors**: decode succeeds and `Encode(Decode(x)) == x`.
- **All Q2A-V A5 acceptance counterparts and all Q2A-F valid/boundary/presence
  variants**: decode; graph compare; exact re-encode equality.
- **All 873 proper golden prefixes**: refuse; no partial view; no
  `InvalidValue` refusal is observed at all in the truncation sweep.

The only decoder path that reports `InvalidValue` at offset 0 is the re-encode
comparison itself (other `InvalidValue` reports use the offending field's start
or the reader position after the relevant fields). The sweep observes zero such
refusals, so the branch was not triggered as a rejection in the entire
qualification corpus.

## Exercise mode and exclusions

- The branch **executes** on every accepted decode as a passing assertion; this
  is its recorded exercise mode in this qualification.
- Direct fault injection into the comparison would require production source
  changes, reflection over production internals, or a test-only production
  hook. RC2A-02 forbids all three; no artificial triggering test was authored.
- The reasoning is source-level plus empirical over the committed corpus; it is
  not a formal proof for arbitrary future input, and it is not a claim about
  any other decoder path.

## Conclusion

R2A-F01's defect concern is addressed: the branch is defensive depth with no
identified reachable rejection path under the accepted input model, and its
success path is exercised on every accepted decode with byte-exact
re-encoding. If a future input ever makes the branch reject, the preserved
sweep/catalog harness (`InvalidValue@0` observation) provides a first
counterexample signal; that event would be a production defect requiring a
separate bounded remediation assignment.
