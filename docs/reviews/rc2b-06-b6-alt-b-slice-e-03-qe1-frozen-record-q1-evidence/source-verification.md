# Q1 source and provenance verification

The [adopted source inventory](../rc2b-06-b6-alt-b-slice-e-03-qe1-provenance-reconciliation-01-evidence/sources-and-verification.md)
is blob `8bb7daa7b2b0e635ba2445c78a7ac1fbf395c1ab`. Its IDs resolve to the
exact twelve historical Gagamba files. Q1 opened only those named artifact
files under the recorded artifact root. Each observed size and full
SHA-256 matched the expected committed entry with no line-ending conversion.
The recalculated digests for AB/AB-H, AR, and F1/F2/F3 matched their
name-and-digest entries in AB-M, AR-M and F-M, respectively. AB-M and AR-M
matched the Alternative B owner decision. F-M, FR, FMX and FDB matched
the F02 owner decision. The review verified content identity of the
available exact byte copies. External Git ancestry has no independent
verification and remains the prior provenance qualification.

| Authority | Application in Q1 |
|---|---|
| F02 §2 and final binding, F1 lines 24, 38, 42–48 and 54 | Accepted context/provenance, version/shape delta and distinct non-Qualified result. F1 leaves its wire amendment to the owner. |
| F2 §§3–5, F3 lines 19–23 | Fixed profile projection, retained original capability identity, separate release preimage. |
| ALT §§3.3–3.5, 4, AB lines 21, 32–44 and 50–52 | Accepted pre/post boundary, compatible layouts and owner correlation. |
| SPEC §§6.1–6.2, WIRE §§C, D.2, B2 §§2–4 and B2-O | Adopted v2 base fields, channels, direction, sizes, sequence and comparators. B2 supersedes the older B2 transport description for the accepted topology. |
| GOLD | Historical synthetic base evidence. Normative meaning comes from B2/SPEC/WIRE and their owner adoptions. |
| SCOPE §§7, 9, E2 §§4–5 and E3 §§2, 8 | Authorized Q-E1 package boundary and deferred responsibilities. |

An independent hash over the committed B2 amendment bytes returned
`88beb1a2e40658c19f14acc96b3fd9215c7bc5b94e59f3a961c3ee27f63e3ad6`,
matching the B2 owner acceptance. Git IDs below refer to the canonical
starting commit and original entry baseline. No external repository was
consulted.

| ID | Bytes | Observed raw SHA-256 |
|---|---:|---|
| AB-M | 869 | `ceac3f3db543c3de7833782178f28687778aeb6d804384d7e5c7bd9e18086686` |
| AB | 15291 | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` |
| AB-H | 18650 | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` |
| AR-M | 457 | `e23f2ab14add2d0e0d31a6eef21f174bb407d3d8c188bb992a7fc93696f5cfca` |
| AR | 3501 | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` |
| F-M | 1463 | `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` |
| FR | 4470 | `efa70562503e75e9e77aa2cdbce54a47d76c2d74ebb8374b1dd7b27d7dca18e6` |
| F1 | 18757 | `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93` |
| F2 | 30362 | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` |
| F3 | 14325 | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` |
| FMX | 12042 | `d8ba4a64c48a6fbac2c9ca4245fcfb3147cca63e0c0ee6a3bcadf080d581d808` |
| FDB | 8598 | `5755748b27a8c11b11f6d4218a077e90ec7da09029256c8653e49b83e7224c0d` |

| Git ID | Blob at entry baseline and Q1 baseline |
|---|---|
| F02 | `5a707d37abba7b10f8dee9bc61ff7507fb3a3613` |
| ALT | `844cb149ffee442e0111210f7befae1cfe2caef0` |
| PLAN | `485ad1ac76e836d2bd6eb826131a01294c9bda8c` |
| SCOPE | `7002dcb3f04e4d69ca5d9c6cb8e9ef57f0f7c578` |
| ROAD | `0c311fc84bde05d3c9e897ff51c8e44832aea987` |
| SPEC | `7f9f615f75527221e81b7aaa498f4b564acb62b9` |
| WIRE | `501487227208d935c3ab1337c9f87055c9e14b8d` |
| WI-DELTA | `889f173151c0c6be97033e9e03dcd84c1697a677` |
| B2 | `95dc3221ae2a5579f966526516073cdd0f392aa9` |
| B2-O | `76b70f6832733c7cf58ffaa56afbd3df1a9f014a` |
| GOLD | `de506adea195bc198680fdfd8d6c70c71a88c7a1` |
| E2 | `3388894f821a4a92aca7c2b2d79c832a0f70a450` |
| E3 | `d2cb4e3911f7aeb3e0e9cd955cdfbd12147dfc12` |
