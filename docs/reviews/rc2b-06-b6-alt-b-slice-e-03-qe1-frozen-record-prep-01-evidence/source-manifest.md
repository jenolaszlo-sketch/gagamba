# Source manifest and preparation baseline

The [reconciliation inventory](../../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01-evidence/sources-and-verification.md)
is blob `8bb7daa7b2b0e635ba2445c78a7ac1fbf395c1ab`. Resolve its source
aliases and committed decision identities at the preparation baseline below.
All 12 original local paths were accessible at the same locations. Original
and current locations in the table are identical and are relative to
`C:/Users/Laszlos/source/repos/Solo/`. Each file was read directly as bytes,
strictly decoded as UTF-8 without BOM, and rehashed without normalization.
The listed full expected and observed digests agree. External Git history
remains unverified.

| ID | Exact original/current relative path | Bytes | Expected and observed SHA-256 | LF-only / CRLF | Authority and usage |
|---|---|---:|---|---|---|
| AB-M | `gagamba-integrated-start-authority-owner-decision-01/SHA256SUMS.txt` | 869 | `ceac3f3db543c3de7833782178f28687778aeb6d804384d7e5c7bd9e18086686` | 0 / 9 | Alternative B owner §4, package identity |
| AB | `gagamba-integrated-start-authority-owner-decision-01/validation-bootstrap-compatibility.md` | 15291 | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` | 68 / 0 | Alternative B owner §§3.3–3.5, 4, lines 21/32–44/50–52 for pre and release |
| AB-H | `gagamba-integrated-start-authority-owner-decision-01/implementation-and-test-handoff.md` | 18650 | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` | 75 / 0 | Alternative B owner §4, handoff and planning support |
| AR-M | `gagamba-integrated-start-authority-owner-decision-q1/SHA256SUMS.txt` | 457 | `e23f2ab14add2d0e0d31a6eef21f174bb407d3d8c188bb992a7fc93696f5cfca` | 5 / 0 | Alternative B owner §4, review manifest |
| AR | `gagamba-integrated-start-authority-owner-decision-q1/validation-bootstrap-assessment.md` | 3501 | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` | 11 / 0 | Alternative B owner §4, review and caption chronology |
| F-M | `gagamba-f02-authority-mechanism-decision-01/recheck-input-SHA256SUMS.txt` | 1463 | `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` | 15 / 0 | F02 owner §2, accepted 15-file manifest |
| FR | `gagamba-f02-authority-mechanism-decision-01/independent-recheck.md` | 4470 | `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6` | 22 / 1 | F02 owner §2, original review and wire amendment limit |
| F1 | `gagamba-f02-authority-mechanism-decision-01/actor-and-descriptor-binding.md` | 18757 | `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93` | 66 / 0 | F02 owner §2 via F-M, lines 24/38/42–48/54/58/60 for digest, v3 delta and stage boundary |
| F2 | `gagamba-f02-authority-mechanism-decision-01/case-policy-and-action-contract.md` | 30362 | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` | 112 / 0 | F02 owner §2 via F-M, §5 lines 98–104 for release |
| F3 | `gagamba-f02-authority-mechanism-decision-01/mechanism-b-session-capability.md` | 14325 | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` | 48 / 0 | F02 owner §2 via F-M, lines 19–23 capability byte identity |
| FMX | `gagamba-f02-authority-mechanism-owner-01/accepted-contract-and-qualification-matrix.md` | 12042 | `d8ba4a64c48a6fbac2c9ca4245fcfb3147cca63e0c0ee6a3bcadf080d581d808` | 45 / 0 | F02 owner §2, matrix C04/Q04 wire amendment scope |
| FDB | `gagamba-f02-authority-mechanism-owner-01/final-design-binding.md` | 8598 | `5755748b27a8c11b11f6d4218a077e90ec7da09029256c8653e49b83e7224c0d` | 63 / 0 | F02 owner §2, exact submission and provenance qualification |

| Committed record | Identity |
|---|---|
| Preparation starting HEAD/tree | `f12a84943ea30153ccf96dfef21866a78e3fb934` / `ed4faa7e9a4614b7d90e6dc7a4ee631dc076c923` |
| Owner BJ/IE ruling | `f3639dc619dbfee39859afb7da553e2167d9b83d` at starting HEAD |
| Previous reconciliation | Commit `2a3dbcef99fdd9073e0c8c5db0f6371b5f7a5120`, report blob `a61febbd417ad6628a47bec0cffa48e6bc1c8e0a`, evidence tree `c2205a46c5b5480de36f8dac61c208097dba16b5` |
| Original E-3 entry assessment | Commit `52b932aaebd52f16bd93170b95bb0f8e89241fd2`, blob `d2cb4e3911f7aeb3e0e9cd955cdfbd12147dfc12` |
| E-2 closure | Commit `21179680e7ae5d153c7ddeef5d209e5ac450cdd7`, blob `3388894f821a4a92aca7c2b2d79c832a0f70a450` |
| SPEC, WIRE, B2 | Blobs `7f9f615f75527221e81b7aaa498f4b564acb62b9`, `501487227208d935c3ab1337c9f87055c9e14b8d`, `95dc3221ae2a5579f966526516073cdd0f392aa9` |
| B2 amendment raw bytes from Git | SHA-256 `88beb1a2e40658c19f14acc96b3fd9215c7bc5b94e59f3a961c3ee27f63e3ad6`, matches B2 owner acceptance |
| GOLD accepted-base comparison | Blob `de506adea195bc198680fdfd8d6c70c71a88c7a1` |

Git root was `C:/Users/Laszlos/source/repos/Gagamba`, with one canonical
worktree on `main`, clean tracked files/index, zero stashes and no
conflicting writer observed in process/lock checks. Local `origin/main`
`a30d1807686ee15d39ef7e9103e8b992062bd971` recorded divergence 5 / 0. The four
predecessor commits above formed a direct parent chain. The historical
manifest SHA-256 `7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1`
matched all 44 paths, with no extras or hash differences at baseline.
