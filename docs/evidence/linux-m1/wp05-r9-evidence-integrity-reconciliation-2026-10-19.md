# WP05 R9 Evidence Integrity Reconciliation

Date: 2026-10-19
Scope: reconciliation of the three checksum mismatches in the R9 implementation evidence package

## 1. Finding

The independent review's checksum audit is confirmed: of the 23 entries in the R9 `SHA256SUMS`, **20 match the exact committed Git blob bytes and 3 do not**.

Affected files:

- `observer-observation.txt`
- `sigpipe-observation.txt`
- `source-head.txt`

Cause: the three files were created with PowerShell CRLF text writes after checkout. The R9 manifest was computed over those CRLF working-tree bytes before commit. Git stored LF blobs because the repository uses `* text=auto eol=lf` and `core.autocrlf=true`. The committed blobs normalize-equal the working-tree bytes (CRLF replaced by LF); no evidence content was altered.

Classification: **B — manifest-generation error** for each file (digest calculated against different bytes). Representation A (checkout CRLF) is the underlying representation cause; the committed evidence itself is intact. Not C (stale/inconsistent execution) and not D (unresolved).

## 2. Repository binding

- Production baseline: `58cb1ba31c102e0e9d5488721e8c438adb55c614`
- R9 implementation/evidence HEAD: `aa1a7f54d28dc96a8367e2273999fbad7ec03634`
- Independent review: branch `review/wp05-abort-oracle-final-requalification`; Q1 `bac2509b94510a522275fde6a640033f0fe6fbf4`, Q2 `2ee412be5ed5f21f4320d3d08aab4f2ff1141987`, Q3 `d809ac969a84073d117d30b5a80437159741774c`
- R8 introduced all three affected files: `3ba6fde64ea3d450f74da13a6d8ab90a36c25977`
- R9 did not modify the three files; it added the byte-exact probe and evidence updates.
- Repository attributes: `* text=auto eol=lf`; `core.autocrlf=true`.

## 3. Exact reconciliation

Method: SHA-256 over exact committed Git blob bytes (`git cat-file blob HEAD:path`), over current working-tree bytes, and over the manifest's recorded value. Git blob SHA-1 from `git rev-parse HEAD:path`.

| File | Recorded SHA-256 | Committed-byte SHA-256 | Working-tree SHA-256 | Root cause | Qualification impact |
|---|---|---|---|---|---|
| `observer-observation.txt` | `904b3077edaafd9a9aeecaa499c1cf1d351d2a8b748c00022854866f304db26a` | `8880dc44de2d18c2c309a93651b8de3d06b67586c59a3857a0c5efbda0cb145d` | `904b3077edaafd9a9aeecaa499c1cf1d351d2a8b748c00022854866f304db26a` | CRLF working-tree bytes hashed before commit; committed blob is LF; normalized-equal | B; committed observation content intact; manifest entry stale for committed-byte verification; historical R7 observer observation, not independent R9 evidence |
| `sigpipe-observation.txt` | `6b58a73aa968a91c0c546aac858e3e52541c12487ab1efaa0114cb44ac2814e8` | `9113ce3ff5ebf208013421b67b1ce072973a2ea3abc7a0f8477afd99c5cf45d4` | `6b58a73aa968a91c0c546aac858e3e52541c12487ab1efaa0114cb44ac2814e8` | Same CRLF/LF manifest-generation cause | B; committed observation content intact; manifest entry stale; SIGPIPE remains implementation-time evidence, not independently Linux-qualified |
| `source-head.txt` | `1de880bab5b6760af8214607025f8b7415a1cbf58df3061ed5d29e2ed1d44696` | `29f98abfefb0dc200adab86ef2dd64cc262d95da9f11a6aa474ad9a5fa4917f2` | `1de880bab5b6760af8214607025f8b7415a1cbf58df3061ed5d29e2ed1d44696` | Same CRLF/LF manifest-generation cause | B; content is accurate for the R7 run it records (`source_head=6e534c8`), not for R9; R9 package contains R7 execution observations |

Additional committed identities:

- `observer-observation.txt` — 252 committed bytes / 253 working bytes; Git blob `071ea34c705347d651e7ef83e52ca33c3a9a1657`; 2 LF/0 CRLF committed, 2 LF/1 CRLF working.
- `sigpipe-observation.txt` — 124/128 bytes; Git blob `38eade88b4ed10671702e007393dfb2e4bef27bd`; 4 LF/0 CRLF committed, 4 LF/4 CRLF working.
- `source-head.txt` — 176/177 bytes; Git blob `23687c99ea0f465a5f7e015aa93c0882307c4555`; 4 LF/0 CRLF committed, 4 LF/1 CRLF working.
- Normalized-byte equality (committed blob equals working-tree bytes with CRLF replaced by LF) is true for all three.

The other 20 entries match the committed blob and working-tree bytes. They are not affected.

## 4. Revision attribution

`source-head.txt` records:

```text
source_head=6e534c893a84e56e80ca2d53306f57b408ff6c19
source_branch=remediation/wp05-abort-oracle-provenance
tested_date=2026-10-19
boot_id=83799c16-80f7-4588-b29a-1dc9efe920fb
```

It is the R7 execution record for the two full supported-profile runs. R9 only restored the byte-exact Q1 hostile probe and updated docs; no R9 full Linux supported-profile run occurred. The R9 evidence report states the full-profile results at R7 and does not claim an R9 execution. These R7 results remain historical and are not relabeled as R9.

## 5. Impact and acceptance

- The original R9 evidence files and `SHA256SUMS` are unchanged; the three manifest entries remain stale for committed-byte verification and are not treated as verified by the old manifest.
- The committed evidence content is intact and readable; no load-bearing observation was modified. The three entries are not used to claim committed-byte integrity; the exact committed records are preserved in this addendum and the new provenance artifacts.
- The R9 report's earlier claim that all 23 entries verified was based on working-tree bytes; it is superseded by this reconciliation. The old manifest cannot verify a fresh LF checkout for these three entries.
- No production artifact, seccomp identity, manifest, RELEASE vector, or schema is affected.
- A new manifest for newly created artifacts uses the exact committed-byte convention (the new artifacts directory has `* -text`, and hashes are recomputed over committed blobs after commit). It does not retroactively validate the old three entries.

## 6. New artifacts

`docs/evidence/linux-m1/wp05-identity-evidence-integrity-remediation-artifacts/`:

- `hash-audit.txt` — full 23-entry committed/working comparison.
- `git-blob-provenance.txt` — per-file recorded/committed/working hashes, sizes, line endings, Git blob IDs, introducing commit.
- `source-head-attribution.txt` — R7-vs-R9 revision attribution.
- `review-provenance.txt` — Q1–Q3 identities and review paths.
- `original-probe-identity.txt` — preserved hostile probe size/Git blob.
- `commands.txt`, `environment.txt`, `.gitattributes`.
- `SHA256SUMS` is added in R12 over all new artifacts, using committed-byte verification.

## 7. Preservation

The original R9 evidence directory and manifest were not modified. The independent Q1–Q3 commits and review evidence were not modified. R10 is a documentation/investigation-only commit; no production or test behavior changes.
