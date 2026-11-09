# ALT-B Slice E-2 P0 — evidence index

This directory supports [the E-2-P0 report](../rc2b-06-b6-alt-b-slice-e-02-p0.md) and the [proposed frozen-schema provenance record](../../evidence/rc2b-06-b6-alt-b-slice-e-02-frozen-schema-provenance-p0.md).

- **Commission:** `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-02-P0`
- **Date:** 2026-11-09 (Asia/Manila)
- **Baseline HEAD:** `4263c8bd85bfed9c05b066d8cbc7524a1ba1a4fe`
- **Baseline tree:** `934bf6567e1ee10bff59d3669bf0ca2a95c6d6d3`
- **P0 status:** reconciliation complete for review; separate P0 owner acceptance pending
- **Q-E1:** formally OPEN
- **Q-E2:** formally OPEN
- **D1:** NOT STARTED
- **E-2 entry / implementation:** UNAUTHORIZED

## What this evidence establishes

- The exact baseline, one-worktree state, and preservation of the 44 historical untracked paths and their bytes.
- SHA-256 identities for the F02 15-file frozen submission, the acceptance-bound manifest and recheck, the F02 owner-package support files, and the Alternative B validation-context inputs.
- The acceptance chain and commit ancestry that bind those bytes to committed owner decisions.
- The exact CJSON-1 and K/M/F/S/R/Wc/P extraction map used by the proposed record.
- The current source and authority state: no E-2/GAVA1/v3-pre schema implementation, no catalog store, no E-1 production consumer, no positive validation.
- The explicit gaps, qualifications, and Q-E1/Q-E2 deferrals.

## Files

| File | Purpose |
|---|---|
| [README.md](README.md) | This index and non-claims |
| [baseline-and-preservation.md](baseline-and-preservation.md) | Exact P0 baseline, preservation digests, and one-worktree state |
| [digest-verification.txt](digest-verification.txt) | Machine-readable PASS lines for F02, Alternative B, support objects, and records |
| [acceptance-mapping.md](acceptance-mapping.md) | Acceptance bindings and commit ancestry |
| [schema-extraction-map.md](schema-extraction-map.md) | Map from each proposed-record section to its exact external source and in-repository use |
| [source-and-authority-state.txt](source-and-authority-state.txt) | Source search results and authority-state checks |
| [reproducibility-commands.md](reproducibility-commands.md) | Exact commands for independent verification |
| [record-hashes.txt](record-hashes.txt) | SHA-256 and size for every record and deliverable used here |

## Non-claims

- This package is not P0 owner acceptance.
- It does not close Q-E1 or Q-E2.
- It does not adopt the validation-catalog approval chain or the V/W/A roles.
- It does not create a schema, catalog, approval, trust anchor, capability, protected store, runtime authority, or production code.
- It does not qualify native behavior or positive validation.
- It does not claim that the external `Solo/` package is a published or durable artifact.
- Hash agreement is not approval, provenance, or authorization by itself.
- This is a single-reviewer governance record, not independent multi-model qualification.

## Reproduction

Start with [reproducibility-commands.md](reproducibility-commands.md). Expected values are in [digest-verification.txt](digest-verification.txt), [baseline-and-preservation.md](baseline-and-preservation.md), and [acceptance-mapping.md](acceptance-mapping.md).
