# Reviewer-owned fixtures (non-authoritative test content)

**Label: these are reviewer-built test-only bytes for GAGAMBA-RC2B-06-B6-ALT-B-
SLICE-E-02-P2-Q1. They are not genuine issued review, owner-acceptance, or
administrative-approval records, and they are not approved catalog instances.**

Constructed by the reviewer from the adopted P0 record §7/§11 (K, R, Wc, P)
independently of the P2 implementation and of the producer fixtures. Canonical
CJSON-1 bytes were hand-built with ascending keys and hashes computed with the
platform SHA-256 primitive (in the probe). Exact bytes were emitted by the
probe on its final run.

| File | Bytes | SHA-256 (content identity) |
|---|---:|---|
| `k-pre.json` | 1111 | `ee039fad958a3cb290e7bc879347d99907f3674bd61ce0e73d0d6577f9e4e47c` (Hk) |
| `r-pre.json` | 708 | `18a2a09c2abc5c9da90f43afd665b54067a5083f7c45e6574c6523511b4d9691` (Hr) |
| `wc-pre.json` | 534 | `3de6ed37e92256d393e7f658b79eedc02d5a7d08957afc92691969656655066c` (Hw) |
| `p-pre.json` | 595 | `3c425e515a62d3ee99621dd70b4c4cbe0f8b3efb6421d2406cb26f889291016f` (Hp) |
| `k-post.json` | 1178 | `4ae4a4928bad56a22bf74bc3584965588529680afeb2de09b3fd3b9d0fdf9b3c` (Hk) |
| `r-post.json` | 777 | `aa1e4311c10d8967ef7e486ea7a6ef8592b9978094928a476fc1600b2708597c` (Hr) |
| `wc-post.json` | 603 | `5fade07489756b2805b4ba610c3ac9958ec7cc4e730edc3bf60d8a4da64564f7` (Hw) |
| `p-post.json` | 664 | `3829599a98a5bbb9e742449cbf26c10da48e568a153fdaf4ebef2a1b57fc3eb7` (Hp) |

Bindings: `r-*` embeds `catalogSha256 = Hk`; `wc-*` embeds `Hk` and
`Hr`; `p-*` embeds `Hk`, `Hr`, and `Hw`; P has no self-hash (Hp is the external
content identity). Pre chain: B2/pre, qualification pair null. Post chain:
B3/post, qualification `qual-01` / `88…` (64 hex).

Reviewer verification results on these bytes (exact values):
`POS-01`/`POS-02` full chains accepted; `POS-04` Hk/Hr/Hw/Hp returned equal to
the reviewer platform digests; `POS-06a–c` field order equals the accepted
schemas. All refusals quoted in the report were produced from mutations of
these bytes (never through a canonicalizing encoder).

Byte-identity note: files are UTF-8 with no BOM and no trailing newline.
Reproduce with `probe/Program.cs` (emits the same files and prints the digests).
