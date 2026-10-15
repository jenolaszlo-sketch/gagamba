# GP-M5 source provenance addendum

Date: 2026-10-16. Review source: `0c1f05e199588622a5987f95a9149374f5bd858b`. This addendum clarifies Linux provenance for the unchanged [final independent review](04-final-independent-review.md); it does not change that review’s pass disposition or qualification boundary.

The Linux runner used a Git archive of the exact commit because the exported workspace had no `.git` metadata. Therefore its provenance is **exact-source export with post-run content verification**, not a native Git-clean checkout claim. The archived tar SHA256 is `b969a00faa873778aadc92403199922db6a8810ccfe3f4ecf26a1eab8f7af1b3`; it matches an independently regenerated `git archive` for the reviewed commit. After the Linux checks, the verifier compared all 217 regular files and 0 symlinks against that archive: 0 mismatches. The machine-readable result is `artifacts/gp-m5/linux-regression-0c1f05e/source-content-verification.json` and is included in `docs/evidence/gp-m5/cross-platform.zip`.

Native macOS and Windows evidence records clean Git source checks at the exact commit. Linux evidence establishes identical committed file content through archive identity plus post-run comparison. No implementation changed after the reviewed SHA. The additional Linux libc inventory records glibc 2.43 on the same WSL2 host described in the Linux qualification report.

Reviewer result: **Linux source provenance is adequate for the exact-source regression claim.** The final independent review’s stated pass remains limited to the documented GP-M5 capture and cross-platform regression scope.
