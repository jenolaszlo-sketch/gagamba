# E-2-P2 evidence index

This directory supports [the E-2-P2 implementation report](../rc2b-06-b6-alt-b-slice-e-02-p2.md).

- **Commission:** `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P2`
- **Date:** 2026-11-09 (Asia/Manila)
- **Baseline HEAD:** `dfa570fcfa4b55176d6cc63ad1d8c8930d4a58eb`
- **Baseline tree:** `e90ec80e6b9ed7ab180ae0572841b79fea440a6d`
- **Binding owner decision:** `docs/decisions/rc2b-06-b6-alt-b-slice-e-02-p2-entry-owner-01.md` (blob `893b48a2…`)
- **Producer disposition:** IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION (report §22)
- **Independent P2-Q1:** not performed
- **P2 owner acceptance:** not issued
- **E-3–E-7 / Slice N:** unauthorized

## Files

- [contract-matrix.md](contract-matrix.md) — pre-implementation R/Wc/P contract reconstruction and normative origin.
- [schema-and-identity.md](schema-and-identity.md) — implemented R/Wc/P schema, Hk/Hr/Hw/Hp identities, binding checks, non-authoritative result design, P1 reuse.
- [fixtures-and-digests.md](fixtures-and-digests.md) — independently constructed positive fixtures, digests, and reproduction steps.
- [negative-control-matrix.md](negative-control-matrix.md) — structural, byte-level, binding/substitution, hash-consistent semantic, and authority-boundary controls.
- [reachability-and-release.md](reachability-and-release.md) — Debug/Release direct-call scans, surface and dependency inspection, E-1/F02 separation, RELEASE verification, P4 neutrality.
- [executed-results.txt](executed-results.txt) — exact commands, outputs, and exit codes.
- `executed/` — raw verifier and full-suite outputs.
- `fixtures/` — generator script, exact fixture bytes (no trailing newline), and independently computed digests.

## What this evidence establishes

- The exact baseline, one-worktree state, and 44 preserved historical untracked paths.
- The implemented E-2-P2 files are limited to the owner-authorized directories, and no existing production, test, or tooling file changed.
- R/Wc/P structural and content-binding verification behavior with hand-constructed canonical fixtures bound to the accepted P1 K2/K3 catalogs.
- Hash-consistent semantic negatives, substituted/incorrect identity bindings, and authority-boundary controls.
- Direct-call absence in both the Debug assembly and the compiled source-Release assembly, no public export, no production signature consuming P2 results, and no authority-shaped naming.
- RELEASE verifier PASS and the expected Debug-as-production FAIL control.

## What this evidence does not establish

- Any approval, eligibility, invocation, capability, custody, or protected execution authority.
- Any genuine issuance of R/Wc/P or independent V/W/A activity; the fixtures are non-authoritative test bytes.
- Any protected-filesystem or native behavior; `NATIVE-PENDING` and the 331 native skips remain.
- Transitive unreachability beyond the inspected direct-call tokens (P1-Q1-F05 retained).
- Independent qualification or owner acceptance.
