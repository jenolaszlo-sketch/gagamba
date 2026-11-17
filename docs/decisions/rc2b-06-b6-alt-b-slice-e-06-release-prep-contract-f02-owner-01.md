# E-6 release preparation contract F02 owner decision

Commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-06-RELEASE-PREP-CONTRACT-F02-OWNER-01`, 18 November 2026 (Asia/Manila).

**B. CONTRACT RECONCILED WITH QUALIFICATIONS, REMEDIATION AUTHORIZED**

The managed projection property is frozen as `ValidationReleaseProjection.SealedLaunchRecordHash`, with constructor parameter `sealedLaunchRecordHash`. It represents the same 32 raw bytes that the protocol names `capabilityHash`, the digest of the exact original sealed launch record created by L for the invocation. The identifier names data supplied for structural comparison. Its presence conveys no permission, approval, possession, authenticity or verified seal. [Exact amendment and mapping](rc2b-06-b6-alt-b-slice-e-06-release-prep-contract-f02-owner-01-evidence/amendment.md)

This additive decision supersedes the managed property name in the authorization's **Managed API and ownership** table and its constructor parameter implied by that table. It also governs references to that managed property and the same-named private snapshot slot. The normative protocol term `capabilityHash`, the full 258-byte preimage and raw 32-byte SHA-256, R01 and mixed-GUID witnesses, refusal behavior, bounded copies and `ReleaseRequest.ApprovalCorrelationSha256` presence semantics retain their accepted definitions. The inherited E-2 naming and reachability assertions remain binding. [Frozen contract](rc2b-06-b6-alt-b-slice-e-06-release-prep-entry-auth-01-evidence/contract.md) · [Verification matrix](rc2b-06-b6-alt-b-slice-e-06-release-prep-contract-f02-owner-01-evidence/verification.md)

## Verified starting revisions

| Revision | Commit | Tree |
|---|---|---|
| E-6 atomic entry assessment | `22a0b7b00364cc7be333ba7dfd9c0359a7fe9f99` | `ee0aa6242cdcc4928518c26486b634a5df8850d4` |
| Original authorization | `405f2d2d45b802f48e08dab64e53e305e29efb6b` | `3a7d80c71a5453fe31e3bd4f4256b1cdbe90c314` |
| F01 reconciliation | `7d298ddd9d83e555a5c16b6c0cab8f36e46b8822` | `27bcf4764d4b0f71b76c4f4741b1f5c54c876d41` |
| Blocked two-file candidate | `53ffe7a21408a7e9674cf50b7098a26887c070b5` | `c5e58a742408783545ae667c0a3506c1dfd3af34` |
| Producer evidence and starting local main | `ce6a6fae1cdf7ec36b977e59050c8ac89db0aea6` | `3d5c7f073a20f6f3fbcbcef06d5b739b4269c759` |
| Published GitHub main | `4dc0ee0998f0def6cb1cba304a76b3a9f59ac904` | `3136117ecdcbd7df91693db2fbc14a148d47607a` |

Preflight verified canonical `main`, a single worktree, zero stashes, a clean tracked checkout and index, actual origin main and all 44 historical untracked files. It verified 1,054 inherited, 19 assessment, eight authorization, four F01 and 188 producer manifest entries. It checked the entire 128-path frozen source inventory at its pinned revision and reconstructed the 93-path predecessor subset. The implementation commit has exactly two additions. The recorded blobs and raw hashes bind those files and the inherited naming test. [Preflight receipt](rc2b-06-b6-alt-b-slice-e-06-release-prep-contract-f02-owner-01-evidence/preflight.json) · [Original authorization](rc2b-06-b6-alt-b-slice-e-06-release-prep-entry-auth-01.md) · [F01](rc2b-06-b6-alt-b-slice-e-06-release-prep-entry-auth-f01.md) · [Blocked producer evidence](../evidence/rc2b-06-b6-alt-b-slice-e-06-release-prep-implementation-01.md)

## Finding and authorized remediation

The historical E-2 naming test passed on F01 and failed on the blocked candidate in both configurations. Read-only reflection on the hash-bound Debug and Release core artifacts identified seven prohibited names across the projection and its private positional snapshot, including generated accessors and backing fields. Substituting the proposed names in memory yielded zero forbidden matches or collisions under the full unchanged rule. The candidate remains blocked until the separate producer remediates and satisfies the entire verification matrix. [Conflict analysis](rc2b-06-b6-alt-b-slice-e-06-release-prep-contract-f02-owner-01-evidence/conflict-analysis.md)

Authorize the separate commission `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-06-RELEASE-PREP-F02-REMEDIATION-01` to modify only the following files relative to the blocked candidate:

- [ValidationReleasePreparation.cs](../../src/Gagamba.Execution.Linux.M1/Validation/ValidationReleasePreparation.cs)
- [ValidationReleasePreparationTests.cs](../../tests/Gagamba.Execution.Linux.M1.Tests/Validation/ValidationReleasePreparationTests.cs)

The authorized delta is the exact name substitution and required references in the amendment. The owner commission adds this decision and annex only. It authorizes no E-2 exemption, alias, namespace movement, test weakening, production consumption, runtime authority, native custody, physical release, protected execution, activation or exposure. Exact-revision P4 verification and the E-6 integration gates remain due at their prescribed stages. [Carry-forward ledger](rc2b-06-b6-alt-b-slice-e-06-release-prep-contract-f02-owner-01-evidence/carry-forward.md)

The next commission must produce a new exact-revision result. Independent Q1 follows successful remediation in a fresh task uninvolved in implementation and producer verification. Separate owner implementation acceptance still follows Q1. This decision is the additive handoff and work-queue record. [Replay and verification](rc2b-06-b6-alt-b-slice-e-06-release-prep-contract-f02-owner-01-evidence/verification.md) · [SHA-256 manifest](rc2b-06-b6-alt-b-slice-e-06-release-prep-contract-f02-owner-01-evidence/SHA256SUMS.txt)
