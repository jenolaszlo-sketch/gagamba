# E-2-P1-Q1 independent qualification — evidence index

This directory supports [the P1-Q1 qualification report](../rc2b-06-b6-alt-b-slice-e-02-p1-q1.md).

- **Commission:** `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P1-Q1`
- **Date:** 2026-11-09 (Asia/Manila)
- **Reviewer revision:** HEAD `06006a7035ca3a7615189b867584b1b3955eeb7a`, tree `89330a5cf779ec35bb208e6bf9ec67bf0c12e87a`
- **Reviewed implementation blobs:** `276581c4…` (CanonicalJson), `868ddbee…` (FrozenSchemaRules), `3344069c…` (FrozenContracts), `fdff96b9…` (FrozenContractVerifier)
- **Formal Q1 disposition:** **SUPPORTED WITH QUALIFICATIONS** (report §19)
- **Reviewer-owned probe:** `probe/Program.cs` + `probe/p1q1-probe.csproj` (external-scratch tool; run outside the repository against the built assemblies)
- **Raw probe output:** [reviewer-vectors.txt](reviewer-vectors.txt) — 91 checks, 0 failures

## Files

- [baseline-and-revision.md](baseline-and-revision.md) — verified baseline, reviewed revision identities, build identities.
- [cjson-reconstruction.md](cjson-reconstruction.md) — independent CJSON-1 rule reconstruction, implementation comparison, and the size/depth limit analysis.
- [independent-chains.md](independent-chains.md) — reviewer-owned positive chains and their independently computed digests.
- [semantic-and-substitution-controls.md](semantic-and-substitution-controls.md) — self-consistent semantic-invalidity and identity-substitution controls.
- [f02-compatibility.md](f02-compatibility.md) — F02 parity matrix and the confirmed schema-scoped divergences.
- [failure-and-resource.md](failure-and-resource.md) — failure-handling audit, robustness fuzzing, and resource bounds.
- [reachability-release-p4.md](reachability-release-p4.md) — independent Debug/Release reachability scans, RELEASE verification, and P4 classification.
- [executed-results.txt](executed-results.txt) — exact commands and observed outputs.

## Reviewer evidence versus producer evidence

Everything in this directory was produced by the Q1 review: the probe source was authored for this commission and run in external scratch (`%TEMP%\opencode\p1q1-probe`); the chains and byte vectors are reviewer-constructed and do not reuse producer fixtures. Producer evidence is referenced only for comparison in the report.
