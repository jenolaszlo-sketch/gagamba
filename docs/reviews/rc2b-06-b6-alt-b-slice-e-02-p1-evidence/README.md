# E-2-P1 evidence index

This directory supports [the E-2-P1 implementation report](../rc2b-06-b6-alt-b-slice-e-02-p1.md).

- **Commission:** `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P1`
- **Date:** 2026-11-09 (Asia/Manila)
- **Baseline HEAD:** `2dab45aa4f4be6b0f713a35b4d50bdfa6fc925be`
- **Baseline tree:** `b458be30925bfc71b7b96fe40b6966126d0bcf74`
- **Binding owner decision:** `docs/decisions/rc2b-06-b6-alt-b-slice-e-02-entry-owner-02.md` (blob `f5ab96c6…`)
- **Producer disposition:** IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION (report §20)
- **Independent P1-Q1:** not performed
- **P1 owner acceptance:** not issued
- **E-2-P2 / E-3–E-7 / Slice N:** unauthorized

## Files

- [contract-matrix.md](contract-matrix.md) — pre-implementation requirement matrix (CJSON-1, K/M/F/S, bindings, prohibited authority work).
- [cjson1-contract.md](cjson1-contract.md) — implemented codec contract, codec-level decisions, and F02 parity findings.
- [positive-fixtures.md](positive-fixtures.md) — independently constructed fixtures, digests, and reproduction instructions.
- [negative-control-matrix.md](negative-control-matrix.md) — implemented negative controls mapped to accepted requirements.
- [reachability-and-release.md](reachability-and-release.md) — Debug and compiled-RELEASE reachability findings and verifier results.
- [executed-results.txt](executed-results.txt) — exact commands and observed results.
- `fixtures/` — generator script, exact fixture bytes (no trailing newline), and independently computed digests.

## What this evidence establishes

- The exact baseline, one-worktree state, and 44 preserved historical untracked paths.
- The implemented E-2-P1 files are limited to the owner-authorized directories, and no existing production, test, or tooling file changed.
- CJSON-1 and K/M/F/S structural/binding verification behavior with independently constructed positive fixtures and deterministic negative controls.
- Direct-call absence in both the Debug assembly and the compiled source-Release assembly, no public export, and no authority-shaped naming.
- RELEASE verifier PASS and the expected Debug-as-production FAIL control.

## What this evidence does not establish

- Any approval, eligibility, invocation, capability, or protected execution authority.
- Any protected-filesystem or native behavior; `NATIVE-PENDING` and the 331 native skips remain.
- Transitive unreachability beyond the inspected direct-call tokens.
- Independent multi-model or human-signed verification.
