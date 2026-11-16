# Alternative B Slice E-4 implementation entry authorization

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-04-ENTRY-AUTH-01`, 16 November 2026 (Asia/Manila).

**ENTRY AUTHORIZED WITH QUALIFICATIONS**

Authorize a separate implementation commission for the isolated internal managed GAVA1 structural codec for INIT, CONTEXT-ACK, DIRECT-CONTEXT and DIRECT-ACK (types 4–7). The authorization covers decoding, comparison against independently supplied expectations, a structural v3-pre handoff, and deterministic managed tests. The four code and test files and separate evidence paths are fixed in the implementation specification. Existing project globs and test friend access apply. The frozen contract imposes no encoder requirement on this isolated slice. Fixture packing belongs in the test project. [Implementation specification](rc2b-06-b6-alt-b-slice-e-04-entry-auth-01-evidence/implementation-specification.md)

## Verified entry and normative authority

| Binding | Exact identity |
|---|---|
| Canonical starting `main` | Commit `dc43b4360dbe17c644118239d0fd691c639aa0cc`, tree `ae31eb90025e1d7d47f0f984d73329579e133a18` |
| Published E-3 and OWNER-02 | `6892ab0e235b2eaab943f218545cbe35ca95fd1b`, direct parent of the assessment |
| Accepted E-3 R2 and independent Q4 | `8be61181e804b2dfafb0797eb54c92d2f234ff86`, tree `1c9307845858fb25ca0a9473d53d1fd1496018fc`. Q4 `0d64fcd199997bf3bede3b328f44aaf211048daa`. |
| Adopted E-3 frozen record | Blob `f6a71562845d2cfe0aee5495864a509c5caabe7a`, SHA-256 `1948bab694ed37bf5c7c50524328716f3eef18d98dd1ae9971ca12100c40160e` |
| Accepted F02 recheck manifest and actor/descriptor input | SHA-256 `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82` and `ea8c50d92a7bf7fe4bd2dca0f91155fc4533bef314fc4980036535c891822b93`, respectively |
| Committed GAVA1 excerpt, F1 lines 13–32 | Blob `23d6ed7d95e4dd532de7e7f3a0f3a6de665b50a1`, SHA-256 `e6e0730c735c5984c97a46ed1c205131c5ffff6427de87bcb731f00110d568c4` |
| Alternative B owner and P4 refusal-only authority | Blobs `844cb149ffee442e0111210f7befae1cfe2caef0` and `31ae1b08cb4663a6a9d9de657fd972fccb731cf2`, respectively |

Verified one canonical worktree, a clean tracked checkout and index, and all 44 historical untracked paths and contents. The thirteen changes from published E-3 to the entry assessment are documentation only. The accepted R2 implementation and inventory of protected callers match the current revision. Reverified all 397 historical, nine OWNER-01, 132 Q4, six OWNER-02 and nine E-4 assessment checksums. The 15 F02 members match the frozen recheck copies. [Verified prerequisites](rc2b-06-b6-alt-b-slice-e-04-entry-auth-01-evidence/prerequisites-and-obligations.md) · [Identity and checksum audit](rc2b-06-b6-alt-b-slice-e-04-entry-auth-01-evidence/identity-and-audit.json)

E-3 CI run 43802107558 succeeded on the published E-3 commit. The ten applicable jobs passed. The availability probe recorded `MODULE-ABSENT`, so the Windows launch spike skipped. The Linux-only lifeline step skipped on Windows and passed on Ubuntu. This evidence carries its scoped platform limits. [GitHub Actions run](https://github.com/jenolaszlo-sketch/gagamba/actions/runs/43802107558) · [Preserved CI evidence](rc2b-06-b6-alt-b-slice-e-04-entry-owner-01-evidence/github-ci.json)

## Implementation and acceptance gates

Bind exact GAVA1 representation and route, independent original session/execution/lease and applicable digests, bounded refusal and immutable ownership. The handoff uses accepted E-3 v3-pre structural results and original independent expectations. The root `Generation` GUID represents the protocol lease. Keep installation identity and release correlation distinct. Missing, malformed or inconsistent inputs yield refusal with no usable decoded result. Structural agreement conveys no trust or authority. No change to accepted E-3 source, tests or adopted contract is authorized.

The implementation must satisfy the frozen producer matrix in Debug and Release, including the 185 E-3 focused regressions in each, broader M1, all 51 production-build controls, preparation validation, Release PASS, expected Debug-as-production refusal, compiled isolation and exact-revision P4 assessment. A fresh reviewer task uninvolved in implementation must establish its independence before executing the separate qualification matrix and P4 re-audit. The owner acceptance is a subsequent decision. [Frozen verification matrices](rc2b-06-b6-alt-b-slice-e-04-entry-auth-01-evidence/verification-matrices.md)

P4 remains refusal-only. Live transport, production callers, protected execution, native capability qualification, release enforcement and E-6 implementation remain outside this authorization. Preserve BB-02, composite Q-E1, external evidence durability, the first E-1 production consumer gate, `NATIVE-PENDING`, all 331 unqualified native skips per configuration, the 39 E-2 qualifications and 23 downstream obligations, and the documented VSTest timeout and xUnit substitution where applicable. Changes outside the exact scope require a separate decision. [Downstream responsibilities](rc2b-06-b6-alt-b-slice-e-04-entry-auth-01-evidence/prerequisites-and-obligations.md)

Next commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-04-CODEC-IMPLEMENTATION-01`. This decision authorizes it and begins no implementation. The manifest covers the decision and annex except the manifest itself. From the annex, run `sha256sum --check SHA256SUMS.txt` against the committed bytes, using Git blobs if checkout conversion changes line endings. [SHA-256 manifest](rc2b-06-b6-alt-b-slice-e-04-entry-auth-01-evidence/SHA256SUMS.txt)
