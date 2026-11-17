# Independent byte witnesses

Hex below is an evidence representation. The helper takes and returns raw bytes.

## R01/R02 from the accepted frozen record

Use candidate and separate expected projections of S schema 1, pre B3 integrated, case `R01`, exact release steps, both flags true and requested index 2. All context outside the hash is synthetic. Use 32 repeats each of hex `11`, `22`, `33`, `44` in order for the four leading digests, 16 repeats each of hex `55`, `66`, `77`, `88` in protocol order for the four GUIDs, and 32 repeats of hex `99` for the manifest. The index bytes are `02000000`. The raw preimage is the exact published witness. This commission independently rebuilt all 258 bytes as literal concatenations and checked them and the SHA-256 value against the frozen witness. [Accepted R01/R02 preimage and field values](../../reviews/rc2b-06-b6-alt-b-slice-e-03-qe1-frozen-record-prep-01-evidence/binary-vectors.md#release-field-only)

- R01 exact SHA-256 `3737572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e`.
- R02 compare expectation `3637572b7b3d2ad8313d109d6d8a9d0df4b5bc7b20e43c71a0a6061e1988ce3e` yields `DigestMismatch` and null. Null expectation yields `MissingExpectedDigest` and null for the otherwise complete projection.

R01 comes from the adopted formula and field-only witness. It establishes no live ReleaseRequest qualification.

## Mixed GUID endianness witness

Use the same release template with independently supplied complete projections. Use case `mixed.guid-01` and Post. Replay with Pre on both sides: the output bytes remain identical. Raw leading digests are inclusive byte ranges `00..1f`, `20..3f`, `40..5f`, `60..7f`, and final manifest digest is `80..9f`.

| Original GUID value | Exact 16 bytes |
|---|---|
| `00112233-4455-6677-8899-aabbccddeeff` | `33221100554477668899aabbccddeeff` |
| `12345678-90ab-cdef-0123-456789abcdef` | `78563412ab90efcd0123456789abcdef` |
| `89abcdef-0123-4567-89ab-cdef01234567` | `efcdab892301674589abcdef01234567` |
| `fedcba98-7654-3210-fedc-ba9876543210` | `98badcfe54761032fedcba9876543210` |

Exact preimage and digest:

```text
676167616d62612d76616c69646174696f6e2d72656c656173652d763100000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f0200000033221100554477668899aabbccddeeff78563412ab90efcd0123456789abcdefefcdab892301674589abcdef0123456798badcfe54761032fedcba9876543210808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f
d4573bb166ab7a4f73743297a4b3b2b9f2e19c73ef4fb6c36c94cf2a20fd7d90
```

Reconstruction used Python standard-library `UUID.bytes_le`, explicit little-endian index bytes and `hashlib.sha256` over the specified concatenation. This is independent of the future helper. Producer tests hard-code both expected digests and compare all offsets and the full preimages. The reviewer must also construct a new independent mixed-byte vector.
