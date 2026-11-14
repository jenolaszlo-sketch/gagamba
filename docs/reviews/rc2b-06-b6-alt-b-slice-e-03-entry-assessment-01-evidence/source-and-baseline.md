# Normative source manifest and read-only checks

## Committed decision and assessment inputs

All Git blobs below resolve at
`21179680e7ae5d153c7ddeef5d209e5ac450cdd7`.

| Record | Git blob |
|---|---|
| [Alternative B design §§3.3–3.5, 4](../../decisions/rc2b-06-b6-integrated-start-authority-owner-decision-01.md) | `844cb149ffee442e0111210f7befae1cfe2caef0` |
| [Implementation plan §§3.5–3.6](../../plans/rc2b-06-b6-integrated-start-authority-impl-01.md) | `485ad1ac76e836d2bd6eb826131a01294c9bda8c` |
| [Slice E entry review §§4.4, 8–10](../../reviews/rc2b-06-b6-alt-b-slice-e-entry-01.md) | `0c311fc84bde05d3c9e897ff51c8e44832aea987` |
| [Slice E owner §§7, 9](../../decisions/rc2b-06-b6-alt-b-slice-e-01-entry-owner-decision-01.md) | `7002dcb3f04e4d69ca5d9c6cb8e9ef57f0f7c578` |
| [F02 adoption §2](../../decisions/rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) | `5a707d37abba7b10f8dee9bc61ff7507fb3a3613` |
| [P0 §12, 16](../../decisions/rc2b-06-b6-alt-b-slice-e-02-p0-owner-acceptance-01.md) | `8dd20d11202bb4ed7fb0dd3cdf447eaa3c5e54b7` |
| [D1 §§13, 18](../../decisions/rc2b-06-b6-alt-b-slice-e-02-d1.md) | `83fe8d2649675e00b76457885fd0169b4ae77498` |
| [P1 §§11, 13, 15](../../decisions/rc2b-06-b6-alt-b-slice-e-02-p1-owner-acceptance-01.md) | `f360932adc4909b4f26e32b6005a592618dd0912` |
| [P2 §§13–23](../../decisions/rc2b-06-b6-alt-b-slice-e-02-p2-owner-acceptance-01.md) | `fb00cc37f1b8294d18e459154a24c3cc1d1f8dd8` |
| [E-1 F-02](../../decisions/rc2b-06-b6-alt-b-slice-e-01-f02-parity-owner-acceptance-01.md) | `c274409330287c10a2b4e9fe92ad12349feeb3f2` |
| [P4 coverage §§5, 7](../../decisions/rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md) | `31ae1b08cb4663a6a9d9de657fd972fccb731cf2` |
| [E-2 owner closure](../../decisions/rc2b-06-b6-alt-b-slice-e-02-closure-owner-01.md) | `3388894f821a4a92aca7c2b2d79c832a0f70a450` |
| [E-2 assessment §§20–23, 26](../../reviews/rc2b-06-b6-alt-b-slice-e-02-closure-assessment-01.md) | `b75e1a5e2a7b52c393b919ff54ef1dd68ca49779` |

E-2 assessment report SHA-256
`f82211d1ee2c53f76279ed3e57bb56ec3ea11a79204d683a068559ea171124de`.
The eight-file evidence tree is `eee19b05e903272761dfc0a692d81b76c8f624f4`.
The exact qualification and obligation tables are assessment §§21–22. Their
companion summary blobs are `6a45cf136b0fbb77f11cc26103f99502decab746` and
`2e297b1672a512acf2afac315a5b390483b50cb5` respectively.

## Historical external source identities

These hashes are recorded in the committed design §4, F02 acceptance §2
and supporting P0/D1 inventories. They identify source evidence for a new
extraction. The assessment did not reopen the external packages or claim a
fresh digest check of their bytes.

| Input | Recorded SHA-256 |
|---|---|
| `validation-bootstrap-compatibility.md` | `896532d72b39e95a5e6201f74a9c37478baebdab7c7061d0c79522eaf87732bb` |
| `validation-bootstrap-assessment.md` | `3c4307f6e51610c1c2cd067b2eaeaeaf87eadb99d471dc65283468907aebeced` |
| `implementation-and-test-handoff.md` | `9a1a195b69645423e1945b97e2ff26424a014ae785bf7076c5df521631be3054` |
| F02 frozen 15-file recheck manifest | `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` |
| `case-policy-and-action-contract.md` | `667e6118e02b081475ce1a2c851d3bfe9b0a96fdb474a2fb0598f308e9c694f7` |
| `mechanism-b-session-capability.md` | `f5d28dbe2473dd8a47337e4ae80a40c815bce051818e326ace1b647bf0d0b010` |

## Read-only verification

| Command or check | Observed result |
|---|---|
| `git rev-parse --show-toplevel HEAD HEAD^{tree} refs/remotes/origin/main` | `C:/Users/Laszlos/source/repos/Gagamba`; `21179680e7ae5d153c7ddeef5d209e5ac450cdd7`; `f9a4f266dc85ea3574f93ea718728cc4deae9c8c`; `a30d1807686ee15d39ef7e9103e8b992062bd971` |
| `git worktree list --porcelain` | One registered worktree on `refs/heads/main`, at the baseline HEAD |
| `git rev-list --left-right --count HEAD...refs/remotes/origin/main` | `2  0` |
| `git --no-optional-locks status --porcelain=v1 -uno`; `git stash list` | Both empty, exit 0 |
| `git merge-base --is-ancestor f813695 HEAD` | Exit 0; assessment is the direct parent of the owner closure |
| Historical untracked `git ls-files --others --exclude-standard` and per-path SHA-256 | 44 expected, 44 found, 0 path differences, 0 hash mismatches. [Manifest](../../evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json) SHA-256 `7d6abbd93c9703626d07455e1e68c18b2538d45847549cb3d39a4318637fc7e1`. |
| `git diff-tree -r --name-only 9b80969 HEAD -- src tests eng native` | Empty. No intervening implementation change on these surfaces. |
| `git grep -n -i -e ValidatedPreSession -e AuthorityHash -e GAVA1 HEAD -- src tests eng` | No matches, exit 1 |
| Tracked names, local history and committed Markdown content search | No adopted v3-pre format record. Design summaries and external citations lead to Slice E entry review §§2, 4.4, 8–10 and P0 §12. Unrelated v3 records concern other protocols. |
| Git process and lock inspection | No conflicting Gagamba writer observed. The prior owner commission had committed and handed off. |

Key source blobs used for read-only surface assessment:

| Source | Blob | Observation |
|---|---|---|
| `Bootstrap/RecoveryProtocol.cs` | `f19017dccce5b83d42bb9276c6e8839d13564061` | `RecoveryConfiguration.Decode` bounds input; `Validate` requires version 1/2. |
| `Bootstrap/QualifiedRecoveryEcho.cs` | `5147d54e9ccdcbd9e488ac25ee1c5d254ce33650` | Existing qualified flow uses a real `LoadedInstallation` and role checks. |
| `Bootstrap/DirectBootstrapPacket.cs` | `7604ee4a16f6c0effaee75ec82ee1d707018bd93` | Existing direct packet is v1; its layout is separate. |
| `Installed/PrepublicationIntegrityProjection.cs` | `74629703f902f269c7c9c33af97594d0dca11207` | Trusted E-1 surface remains off-limits. |
| `Execution/ReleaseArbiter.cs` | `2eaf47daad1cb38f888e450674ad2f20675a6e1f` | Existing `ApprovalCorrelationSha256` usage supplies no evidence of an implemented v3 validation formula. |
