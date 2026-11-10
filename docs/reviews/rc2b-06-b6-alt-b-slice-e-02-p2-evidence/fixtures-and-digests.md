# E-2-P2 positive fixtures and independently computed digests

Date: 2026-11-09 (Asia/Manila)

**Label: these are test-only, non-authoritative fixture bytes. They are not
genuine issued review, owner-acceptance, or administrative-approval records, and
they are not approved catalog instances.**

## 1. Construction (independent of the P2 verifier)

`fixtures/generate-fixtures.ps1` (blob `ee0f8cd2…`) hand-writes canonical
CJSON-1 R/Wc/P templates bound to the accepted P1 K fixtures
(`tests/Gagamba.Execution.Linux.M1.Tests/Validation/ValidationFixtures.cs`,
read-only):

- **B2/pre chain** — bound to `K2` (`case-2`, `pre`, `b2-lifeline`, `Hk2 =
  793aa2aa…`, qualification pair null).
- **B3/post chain** — bound to `K3` (`case-3`, `post`, `b3-integrated`, `Hk3 =
  255d017e…`, qualification `Q1` / `f×64`).

Each record contains only the accepted frozen fields; `conclusion` and
`decision` carry the required literals; P carries `accepted=true`; `Hr`/`Hw` are
propagated into the downstream records' hash references. The script computes all
digests with the platform `System.Security.Cryptography.SHA256` primitive
(lower-case hex) over the exact UTF-8 bytes (no BOM, no trailing newline) and
writes the bytes verbatim.

`fixtures/digests.txt` (blob `36795e5a…`) records `path`, digest, and byte
length. Fixture JSON blobs:

| File | Blob | Digest | Bytes |
|---|---|---|---|
| `fixtures/b2-chain/r.json` | `67eb986d…` | `82e28e80ac6d664e1621ef8dfe6f31b9769bed0fa83d08f21fc4da9a02ea7362` (Hr2) | 694 |
| `fixtures/b2-chain/wc.json` | `f3054707…` | `a93b8cd4f5d0d8380cdfffa6915de14e4b60a5c9ed5dcc7e54dd3f98a3db9be2` (Hw2) | 520 |
| `fixtures/b2-chain/p.json` | `a5fd801b…` | `9b1cc43b3e03cb91fb227446106df2e99ebf2a1acd7721951fd5567b576db92e` (Hp2) | 581 |
| `fixtures/b3-chain/r.json` | `373f3dcd…` | `7d96bc2f3e6e9075cd2691aabc9588dfd2c14fe557d35281c5d9e24ee56c48ec` (Hr3) | 759 |
| `fixtures/b3-chain/wc.json` | `d018707a…` | `293e4af952e297cea56e80ebc95889d3249f32f585048413d43fb06fac440a75` (Hw3) | 585 |
| `fixtures/b3-chain/p.json` | `228f714d…` | `67c81e9e44b3bf30c24afeda581dfe0e4728b56b190a6a6070d524793a718768` (Hp3) | 646 |

Expected upstream references: `Hk2`/`Hk3` and `Hm2`/`Hf2`/`Hs2`/`Hm3`/`Hf3`/
`Hs3` as recorded by the accepted P1 fixtures.

## 2. Reproduction and integrity checks executed

1. Generator executed from the repository root:
   `powershell -NoProfile -ExecutionPolicy Bypass -File
   docs\reviews\rc2b-06-b6-alt-b-slice-e-02-p2-evidence\fixtures\generate-fixtures.ps1`.
2. Fixture files were hashed before and after a full regeneration; all six
   SHA-256 values were byte-identical (deterministic regeneration).
3. `Get-FileHash` of every fixture matches `digests.txt` and the test constants
   in `tests/Gagamba.Execution.Linux.M1.Tests/Validation/DetachedEvidenceFixtures.cs`
   (blob `72d5fdc5…`).
4. `DetachedEvidenceTests.ContentIdentitiesMatchIndependentPlatformDigests`
   re-derives `Hk/Hr/Hw/Hp` in-test with the platform SHA-256 primitive and with
   `CanonicalJson.Hash`, independently of the P2 verifier.

## 3. Expected verification results (test constants)

- `K2 + R2 + Wc2 + P2` and `K3 + R3 + Wc3 + P3` verify structurally and by
  content binding.
- `Hr`/`Hw` are embedded as `Wc.catalogReviewSha256` and
  `P.ownerCatalogAcceptanceSha256` respectively; `Hp` is asserted externally
  against the fixture digest and is never a P field.

## 4. Untrusted-content labeling

The complete chains are additionally labeled **STRUCTURALLY VALID,
OPERATIONALLY UNTRUSTED TEST CONTENT** in
`DetachedEvidenceTests.UntrustedChainIsStructurallyValidButCarriesNoAuthorityProjection`,
which requires structural success and asserts that no reviewer, owner, or
administrator authority, protected acquisition, custody, eligibility, or
invocation authority is returned, constructed, or implied.
