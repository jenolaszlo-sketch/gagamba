# CI preparation: preserve exact historical checkout bytes

Date: 2026-11-02 (Asia/Manila)
Scope: checkout byte identity only; no runtime, build-variant or accepted-contract change.
Baseline: `dce71ac2423c2a49a853bef8703018f0220378cb`, tree `becec3f768266be158fb57396f00f328408cd30e`, canonical `main`.
Source binding: baseline plus the exact two implementation inputs hashed below. The containing local fix commit is reported in the final handoff; documentation does not affect checkout conversion.
Host: Windows 11 x64; Python 3.14; Git checkout-index; PowerShell Core for executable verifier checks.
Exclusions: new push, package publication, S2, native qualification and production activation.

## Failure and cause

The preparation job in [CI run 41325323331](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/41325323331) reported `Historical source identity mismatch: docs/decisions/restricted-consumer-rc2a-01-owner-acceptance.md` at `eng/verify-preparation.ps1:72`.

The committed source and this older local checkout already had the expected LF bytes. A fresh Windows-style checkout (`core.autocrlf=true`) converted them to CRLF: the owner decision grew from 11,641 to 11,816 bytes. Git's committed blob stayed unchanged, but the verifier intentionally hashes actual file bytes, so both its SHA-256 and Git-blob calculations changed.

The expected SHA-256 is `84ec983fcbd88667544e335b15829ab5406798a35ed66d99bbc55ccda019b304` and expected blob is `15e008524e4d1c239074b0f204d7c9a0f692fa0c`; the fresh CRLF checkout produced SHA-256 `57abf6d9514a86be43b3e3764f10ec0c86aa3bfc0474e87dd95e9bc3fd5f6333` and blob `2854232bd22b5f322d5827af6e39243529c3a0c8`. The unchanged verifier reproduced the reported line-72 failure.

All five sources in `eng/preparation-legacy-links.json` were affected; the verifier stops at the first. Existing B6 review globs do not cover these exact paths.

## Correction

Add exact-path `-text` entries in `.gitattributes` for:

- `docs/decisions/restricted-consumer-rc2a-01-owner-acceptance.md`
- `docs/reviews/2026-10-10-ar7-independent-evidence-review.md`
- `docs/reviews/2026-10-10-ar7-r2-separate-gate-review.md`
- `docs/evidence/linux-m1/wp03.md`
- `docs/evidence/linux-m1/wp05-release-protocol-implementation-2026-10-18.md`

No normalization or hash fallback was added to the verifier. The identity map, expected hashes, historical document blobs, original proposal and all existing assertions remain unchanged. No broad documentation line-ending policy, renormalization, global Git configuration change, branch or linked worktree was introduced. `core.longpaths=true` and autocrlf settings used for the isolated copies are command-scoped.

The new regression invokes real Git checkout conversion, then verifies both raw identities for every mapped source under `core.autocrlf=true` and `false`; it does not merely inspect attribute text. Temporary copies are ordinary directories, not Git linked worktrees. The regression reads indexed source files and the repository's working-tree attributes, so new/uncommitted attribute fixes can also be tested before committing.

## Measured checks

| Check | Before | After |
| --- | --- | --- |
| `python -m unittest discover -s eng/tests -p test_preparation_checkout.py -v` | 2 tests; CRLF case has 5 failing source subtests, LF case passes | 2/2 pass |
| Fresh full checkout with autocrlf=true, unchanged verifier | Exit 1, same historical identity mismatch at line 72 | Exit 0; 5/5 exact identities preserved |
| Fresh full checkout with autocrlf=false, unchanged verifier | Not separately executed before fix | Exit 0; 5/5 exact identities preserved |
| `python -m unittest discover -s eng/tests -v` | New focused failure above retained | 37/37 pass |
| Change one protected source in an isolated fixed checkout | N/A | Verifier refuses with historical identity mismatch |
| Historical source blobs / verifier / map | Expected original identities | Unchanged |
| `git diff --check` | N/A | Clean |

Both successful full verifier runs checked 267 tracked Markdown documents, 1,036 links, 5 historical compatibility documents, 42 compatibility references and the original proposal hash. The full checkout copies were materialized before staging the new evidence/current-pointer files, so those counts describe their exact recorded source inventory. A final canonical preparation check is recorded separately below in the raw manifest. A fresh CRLF materialization of the complete staged file set also passes: 268 tracked Markdown documents, 1,039 links, five exact-identity documents and 42 compatibility references. The earlier after-true copy was subsequently mutated for the explicit rejection control; the staged-true copy remains the pristine final positive control.

Local Windows PowerShell could not execute this script because its host environment did not expose `Get-FileHash`; that environmental error is retained and is **not** counted as the identity reproduction. PowerShell Core reproduced the actual identity failure and passed both corrected checkout modes. GitHub's Windows PowerShell CI has not been rerun for this new local fix. No green CI claim is made.

The first full copy attempt hit Git's local long-path limit; a new copy with command-scoped long-path support succeeded. Failed copies/logs remain under ignored `artifacts/preparation-checkout-fix/`; no unrelated cleanup occurred. No native resources were created. Temporary directories used by the automated checkout tests are cleaned by TemporaryDirectory.

## Source inputs

| Input | SHA-256 |
| --- | --- |
| `.gitattributes` | `5a62f4a13bbcddebe59384b4674032a701c991e03894b4371438fa12e1b844a7` |
| `eng/tests/test_preparation_checkout.py` | `5b2a64b7e892dc4e3d905bd6addf6184b0b582445926ae69c38e522b5186e1d8` |

## Raw evidence custody

Local ignored directory: `artifacts/preparation-checkout-fix/`. The following SHA-256 values bind the producer observations; they are not independent CI certification.

| File | SHA-256 |
| --- | --- |
| `after-checkouts.json` | `babc9dd375deccdf3b19075afebf21c81f067935b7b7fe4f3f5652c9a23d533f` |
| `after-false-verifier.log` | `867257513c72253e6372f2255266ac75020592700c60a92ac131f91e8543f709` |
| `after-regression.log` | `9424f25af7024855faf61dc4a1093586d3d0bac01ceb6ab61f013060d694ecfe` |
| `after-true-verifier.log` | `867257513c72253e6372f2255266ac75020592700c60a92ac131f91e8543f709` |
| `before-checkout-materialization.log` | `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855` |
| `before-checkout.json` | `3cdc410a3831f97f0e9fd70c8e60bcf01deabe21fbcef5b2feef41b3c695aad6` |
| `before-powershell-verifier.log` | `73ab928d42f7017aec70cca53deecacab6590bdf356cb3296839277d8302e919` |
| `before-pwsh-verifier.log` | `122cbd486f5f0d63ced5655c5b06f1d3b2bbda5b973023b630bc10ab2d14308c` |
| `before-regression.log` | `65b8d1103e0fc117a356b10bf4ccbd98bb333d1b1d55c32d8fd6eb31ef0e3ebf` |
| `before-verifier.log` | `73ab928d42f7017aec70cca53deecacab6590bdf356cb3296839277d8302e919` |
| `eng-tests.log` | `55b4f6550686c8d4178ffce612961bf24b12454458f7e78b5a8711b3c04ea204` |
| `mutation-verifier.log` | `ab587980c17953051a668bdfe20eefc49ccf36d54e6509f25d706addeb276be4` |
| `final-canonical-verifier.log` | `dd9294fb9467a0533af1d01645fb4dcb2d3f7cb77782a5c8adf2585de7a9e577` |
| `final-staged-crlf-verifier.log` | `c6d9b20fad2468680e184bf158d3814dfe7981d7f6374d534027d6aa3d51fd03` |

## Disposition and review

**Checkout-byte correction implemented and locally verified; not pushed, CI rerun pending.** The prior push authorization was explicitly one operation and has already been used. S1 remains remediated with qualifications and requires independent Q2; this fix does not change that owner/reviewer disposition.

Luna independently diagnosed the missing exact-path attributes. Its diagnosis was accepted; the unchanged strict identity checks and historical bytes were preserved. Review should falsify the fix with fresh Windows CRLF/LF checkouts and changed-byte controls, and verify no historical source or expected digest changed.
