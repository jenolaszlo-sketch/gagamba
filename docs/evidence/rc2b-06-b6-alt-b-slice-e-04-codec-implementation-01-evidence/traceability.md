# E-4 implementation and test traceability

The four paths below are the complete non-documentation delta from entry. Build commands and exits are in the raw logs. The [managed replay driver](procedure/Run-E4.ps1) reuses the Q4 candidate layout, accepted E-3 probe inputs and unchanged candidate build script and records the two metadata scans. Its scanner source is preserved. Use fresh external directories and supply the accepted predecessor artifacts and source inputs identified by the Q4 evidence. [Q4 reproduction](../../reviews/rc2b-06-b6-alt-b-slice-e-03-codec-r2-q4-evidence/reproduction.md)

| Authorized path | Responsibility |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Bootstrap/Gava1ContextCodec.cs` | Decode bounded exact GAVA1 frames, match independent route, original values and state, and compare the narrow v3-pre handoff. |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/Gava1ContextData.cs` | Internal roles, types, expectations, refusals, immutable original snapshots and structural results. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/Gava1ContextCodecTests.cs` | Conformance, refusal and ownership evidence and test-only composition with accepted E-3. |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/Gava1ContextVectors.cs` | Independently pack frames and retain fixed synthetic original inputs and release witness. |

| Frozen row | Implementation and measured control |
|---|---|
| G01–G02 / V01 | `Decode` first checks bounded exact length, magic/version, and declared payload length, then interprets fixed header fields with LE or existing .NET GUID representation. Tests cover all truncations and one-byte mutations of each valid route, exact widths, wrong endian, maximum u32 and over-limit lengths. |
| G03–G05 / V02 | `ValidRoute` handles six allowed sender/recipient pairs. `Decode` enforces first pending state, sequence 1 and exact type, original tuple and capability/catalog placement. ACK compares only wire capability and retains separately supplied catalog context. Tests reject other types, wrong routes, missing, empty and incorrect expectations, swapped hashes, sequence changes, surplus ACK and spent state. |
| G06–G07, G09 / V03 | Context and ancillary premises are caller-supplied synthetic facts. Missing association, unexpected ancillary classification and prior-context failure refuse. `Gava1Originals` snapshots valid supplied digests; the outputs return copies. Ownership tests overwrite input, expectation buffers and returned arrays. Refusal assertions require null decoded results. Kernel receipt, actual peer proof, one-shot lifetime, stop service and direct rendezvous remain downstream. |
| G08–G10 / V04 | `MatchPreBootstrap` requires version 3, pre mode and independently selected topology. R2 decoding supplies the B2/integrated test projection. Root `Generation` maps to protocol lease and cannot be supplied by the separate installation-generation SHA-256. Tests substitute each ID, digest and phase/topology and a release SHA-256 witness. Capability/catalog remain distinct from release bytes. The shipping code has no E-3 dependency, release preimage computation, wire opcode or release authority. |
| V05–V06 | Case inventory matches all 185 E-3 focused tests. Exact Q1/Q2 and supplemental outputs and both broad XML files bind regression results. Broad passes grew by exactly 11. |
| V07–V08 | Both configurations built and passed policy/metadata scope controls. Scan positives include test calls to both family entry points. No production caller or outbound cross-family call. The production verifier supports the expected Release/Debug policy contrast. |
| V09 | Only the four allowed code and test paths are new. Reviewed unchanged protected source and exact prior P4 caller inventory. No production consumer, added fallback, authority conversion, operation dispatch or new route to a first protected effect. E-4 producer finding is neutral, with independent re-audit still required. |

[Raw artifact and runner bindings](raw/all-bindings.json) · [Producer identity and results](identity-and-results.json) · [Frozen protocol and qualifications](../../decisions/rc2b-06-b6-alt-b-slice-e-04-entry-auth-01.md)
