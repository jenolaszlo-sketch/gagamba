# Sources and raw-byte verification

## Local inventory

All listed candidates were available, read as raw files and matched the full
recorded SHA-256. Expected and observed are identical in each row. Direct
reads involved no archive extraction, rewriting or normalization. All files
strictly decoded as UTF-8, without BOM or bare CR, with terminal LF. Line
locators in the extraction refer to these exact files. Historical source
revisions are identified by content, with no independently verified external
Git revision.

Locations are relative to the recorded artifact root
`C:/Users/Laszlos/source/repos/Solo/`:

- AD: `gagamba-integrated-start-authority-owner-decision-01/`
- RV: `gagamba-integrated-start-authority-owner-decision-q1/`
- F: `gagamba-f02-authority-mechanism-decision-01/`
- FS: `gagamba-f02-authority-mechanism-owner-01/`

| ID | Exact folder/file | Bytes | Expected = observed SHA-256 | LF only / CRLF | Authority and relevance |
|---|---|---:|---|---:|---|
| AB-M | `AD/SHA256SUMS.txt` | 869 | `ceac3f3db543c3de7833782178f28687778aeb6d804384d7e5c7bd9e18086686` | 0 / 9 | Alternative B §4 owner package manifest |
| AB | `AD/validation-bootstrap-compatibility.md` | 15291 | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` | 68 / 0 | Alternative B §4 compatibility delta, §§3.3–3.5 design limitations |
| AB-H | `AD/implementation-and-test-handoff.md` | 18650 | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` | 75 / 0 | Alternative B §4 planning and evidence context |
| AR-M | `RV/SHA256SUMS.txt` | 457 | `e23f2ab14add2d0e0d31a6eef21f174bb407d3d8c188bb992a7fc93696f5cfca` | 5 / 0 | Alternative B §4 independent-review package manifest |
| AR | `RV/validation-bootstrap-assessment.md` | 3501 | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` | 11 / 0 | Alternative B §4 independent review and acceptance chronology |
| F-M | `F/recheck-input-SHA256SUMS.txt` | 1463 | `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` | 15 / 0 | F02 §2 acceptance-bound 15-member manifest |
| FR | `F/independent-recheck.md` | 4470 | `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6` | 22 / 1 | F02 §2 independent recheck and separate wire approval gate |
| F1 | `F/actor-and-descriptor-binding.md` | 18757 | `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93` | 66 / 0 | F02 §2 via F-M line 1, primary pre-layout delta |
| F2 | `F/case-policy-and-action-contract.md` | 30362 | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` | 112 / 0 | F02 §2 via F-M line 4, release correlation and phase bindings |
| F3 | `F/mechanism-b-session-capability.md` | 14325 | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` | 48 / 0 | F02 §2 via F-M line 7, exact capability context |
| FMX | `FS/accepted-contract-and-qualification-matrix.md` | 12042 | `d8ba4a64c48a6fbac2c9ca4245fcfb3147cca63e0c0ee6a3bcadf080d581d808` | 45 / 0 | F02 §2, C04 and Q04/M05/M06 retain wire amendment gate |
| FDB | `FS/final-design-binding.md` | 8598 | `5755748b27a8c11b11f6d4218a077e90ec7da09029256c8653e49b83e7224c0d` | 63 / 0 | F02 §2 exact submission binding and external provenance limitation |

Raw SHA-256 was calculated using Python hashlib and independently compared
with Windows certutil. An earlier Get-FileHash spot check agreed. AB and
AB-H are members of AB-M, AR is a member of AR-M, and F1–F3 are members
of F-M. Those six memberships were matched by name and full digest.
The F1–F3 copies under `F/recheck-input/` were checked byte for byte.
Unrelated package members were not requalified. The exact missing item is
a controlling adopted v3 decision for the unresolved encodings, not a lost
historical package.

## Canonical Git sources

Resolve all blobs at `52b932aaebd52f16bd93170b95bb0f8e89241fd2`.

| ID | Source | Git blob |
|---|---|---|
| F02 | [Mechanism B adoption](../../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) | `5a707d37abba7b10f8dee9bc61ff7507fb3a3613` |
| ALT | [Alternative B owner decision](../../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md) | `844cb149ffee442e0111210f7befae1cfe2caef0` |
| PLAN | [Atomic implementation plan](../../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) | `485ad1ac76e836d2bd6eb826131a01294c9bda8c` |
| SCOPE | [Slice E owner](../../decisions/rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md) | `7002dcb3f04e4d69ca5d9c6cb8e9ef57f0f7c578` |
| ROAD | [Slice E roadmap review](../rc2b-06-b6-alt-b-slice-e-entry-01.md) | `0c311fc84bde05d3c9e897ff51c8e44832aea987` |
| SPEC | [Original installed artifact specification](../../design/rc2b-06-b6-installed-artifact-trust.md) | `7f9f615f75527221e81b7aaa498f4b564acb62b9` |
| WIRE | [Integrated v2 wire adoption](../../decisions/rc2b-06-b6-wire-owner-acceptance.md) | `501487227208d935c3ab1337c9f87055c9e14b8d` |
| WI-DELTA | [Accepted original wire delta](../../evidence/rc2b-06-b6-spec-wire-delta-01.md) | `889f173151c0c6be97033e9e03dcd84c1697a677` |
| B2 | [Accepted B2 amendment](../../design/rc2b-06-b6-s3-b2-wire-delta-01.md) | `95dc3221ae2a5579f966526516073cdd0f392aa9` |
| B2-O | [B2 wire owner decision](../../decisions/rc2b-06-b6-s3-b2-wire-owner-acceptance.md) | `76b70f6832733c7cf58ffaa56afbd3df1a9f014a` |
| GOLD | [Historical synthetic v2 vectors](../../evidence/rc2b-06-b6-impl-01-s3/golden-wire-vectors.json) | `de506adea195bc198680fdfd8d6c70c71a88c7a1` |
| E2 | [E-2 owner closure](../../decisions/rc2b-06-b6-alt-b-slice-e-02-closure-owner-01.md) | `3388894f821a4a92aca7c2b2d79c832a0f70a450` |
| E3 | [E-3 entry assessment](../rc2b-06-b6-alt-b-slice-e-03-entry-assessment-01.md) | `d2cb4e3911f7aeb3e0e9cd955cdfbd12147dfc12` |

B2 committed raw SHA-256 `88beb1a2e40658c19f14acc96b3fd9215c7bc5b94e59f3a961c3ee27f63e3ad6`
matches B2-O and the committed bytes at `92d9ef70ca22b49db1c1e393349d6c7210ba6a8e`.
The local checkout's CRLF-translated B2 file has SHA-256
`4ebcb9990e0f745e20b71a554e1abda9e229de93c507a29bbbd654f402d77acc`.
The owner bound committed bytes. All table extraction from B2 uses the
committed identity.

## Baseline checks

| Read-only check | Result |
|---|---|
| Git root, HEAD, tree, worktree | `C:/Users/Laszlos/source/repos/Gagamba`, `52b932aaebd52f16bd93170b95bb0f8e89241fd2`, `5406e8071008393d05b96f343d274f0114946e19`, one worktree on `main` |
| Recorded upstream and divergence | `a30d1807686ee15d39ef7e9103e8b992062bd971`, 3 / 0 |
| Tracked status and stash list | Both empty |
| E-2 ancestry | E-2 closure is direct parent of starting HEAD. E-2 assessment `f81369549b3957b030202697eae20735354cdafe` is the parent of E-2 closure. |
| Historical untracked manifest | 44 expected / 44 present / 0 extras / 0 hash differences. [Manifest](../../evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json) SHA-256 `7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1`. |
| Diff since accepted P2 source on `src`, `tests`, `eng`, `native` | Empty |
| Git process and lock inspection | No conflicting Gagamba writer observed |
