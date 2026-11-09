# P1-Q1 — reviewer-owned positive chains

Date: 2026-11-09 (Asia/Manila)

Three chains were constructed by the reviewer from the accepted rules using hand-written canonical JSON templates inside `probe/Program.cs` (string composition only; the P1 encoder was never used to create fixture bytes). Digests were computed with the platform SHA-256 primitive over the exact bytes and then independently re-verified by the probe.

| Chain | Artifact | Bytes | SHA-256 |
|---|---|---|---|
| b2-pre (`case-r8`, uid/gid 3000, host `44…4`) | M | 76 | `5889f8bd7f9fb3dd2fc52fdbafff0401257b8ad1b6b00d5553fd5c9e9a74b73f` |
| | F | 525 | `76544d39a8fcbf2d2f0be8a87a5b8450301b8523f91cbb780d99bccbaba31343` |
| | S | 465 | `4cda5ca26acb95d394ce8688c347e06a1631395b0f353c22ed770ee99f4eca00` |
| | K | 1087 | `545177d5aee994d2c8ff0acd9552eb18900e6244ecba14b6d285593d00ec1ca1` |
| b3-no-release-pre (`case-r9`, uid/gid 2000, host `33…3`) | M | 724 | `087362070e94a5c6881980aa53bb70eebf03bf15fa4e42034c57bf419442173f` |
| | F | 2228 | `134c1fa6b33b01187a32864ecd36453f834a1114e5d25fcd9c28423d117dfcfc` |
| | S | 467 | `b7916332614f37a6a038bd71406803b64b1d77442d47bc2310eabd45eba9836d` |
| | K | 1089 | `c66eaba63af2f4c07243411dc3551a5883956d674559d8afdb25730cb6b93cd4` |
| b3-release-post (`case-r7`, qualifications `Q-r`) | M | 724 | `e4f005cb1f0b5218aed32958819ab2187afc7c76bd21c0d508011a91555396c9` |
| | F | 2228 | `59e4aa96411dbb01e7aafefa1b84dcbc65fd8b60bf13790869b8cddcb0d09071` |
| | S | 566 | `f4931e286c6d20ff0a71b27d5fa3c36440d9fcaa817f38f5faa58042da798112` |
| | K | 1152 | `3692c29a542a15a4a532854003e5e25e23a0b1ae86bff3e9d8a6b1836c55711f` |

Displayed values are `Hm/Hf/Hs/Hk` in M→F→S→K order.

## Verification chain

1. The probe computes every digest with `SHA256.HashData` over the exact reviewer bytes (no P1 code).
2. `FrozenContractVerifier.TryVerifyChain` (invoked by reflection) accepts all three chains and returns the same four digests (`chain/b2-reviewer`, `chain/b3-reviewer`, `chain/b3-release-reviewer`, `chain/b3-verifier-digests`).
3. Each artifact is also accepted by its individual verifier (`artifact/reviewer-per-artifact`).
4. Builder-correctness was proven adversarially: an early builder defect that failed to propagate recomputed hashes was detected by the chain verifier (`S fixturePolicySha256 does not bind F`), showing the binding checks work against reviewer-authored bytes, not only producer fixtures.

## Reproduction

```powershell
dotnet run --project <scratch>\p1q1-probe -c Release -- <repo-root>
```

The probe source committed under `probe/` contains the exact templates and recomputes all digests; its output is retained in `reviewer-vectors.txt`.
