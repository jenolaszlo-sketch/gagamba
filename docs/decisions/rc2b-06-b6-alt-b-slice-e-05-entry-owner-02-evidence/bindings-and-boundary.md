# Artifact bindings and six-file boundary

## Verified baseline

| Item | Verified identity |
|---|---|
| Canonical repository | `C:/Users/Laszlos/source/repos/Gagamba`, `refs/heads/main` |
| DESIGN-OWNER-01 start of this assessment | `1ff4d6e6d1f756646072790478921d9a64f15654` / `0dd0da1ba6e395ac3e7339b07ad4809e9a20b62d` |
| ENTRY-OWNER-01 direct parent | `b309a9ec1a9a0f160ea0c2dd513001c3561a36d9` / `ac1e2aeae8aea96f19972e3fccb06fa94dcf39c9` |
| Published E-4 direct grandparent | `1116c66d7858e6000f0001a0abe39319e4e694be` / `86768fbd5da028fc271fda295a7da5c389aab8bc` |
| Origin and hosted CI | `https://github.com/jenolaszlo-sketch/gagamba.git`. Remote ref, tree and push workflow verified through GitHub; CI run `43879340208` completed successfully at the published SHA, attempt 1. |
| Worktree, index and stash | One canonical worktree. Tracked checkout and index clean. Empty stash list. |
| Historical manifest verification | Verified all 792 entries in the 13 prior manifests and all 12 design entries at their original revisions, at starting HEAD and against checkout bytes. The design commit is exactly the 12 manifest members and its manifest, all documentation additions. [Pinned historical manifest list](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/preflight.json) |
| Historical untracked files | All 44 matched the [pinned inventory](../../evidence/rc2b-06-b6-native-input-freeze-01/untracked-before.json) by path, length and raw SHA-256. |
| Governing sources | Verified the 82 Git blobs and SHA-256 values at the indexed baseline and reassessment start. Ten checkout files had only CRLF conversion. Both F02 copies' 15 design members match the accepted recheck manifest and the Alternative B compatibility bytes match their pinned hash. [Source identities](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/source-identities.json) |

The delivered F02 root's outer manifest has 51 entries and SHA-256 `a9c5dc71eb0c7b7ff9e443ea8107d55f99540cc119ee343fc18ca105b5ce4e6b`. It includes the 15 root members and binds `recheck-input-SHA256SUMS.txt`. That accepted 15-member manifest is byte-identical to `recheck-input/SHA256SUMS.txt`, SHA-256 `5940b98bc4158eb3887b096a047ff6451f6059a3ec45e7bc254f4ed5cc583f82`. All 15 hashes match both sets of design bytes. The E-4 owner decision's historical GAVA1 link discrepancy already appears in the entry publication record. The governing reconciliation is linked here by its verified path. [F02 acceptance](../rc2b-06-b6-f02-mechanism-b-owner-acceptance.md) · [Publication record](../rc2b-06-b6-alt-b-slice-e-05-entry-owner-01-evidence/publication-and-ci.md) · [GAVA1 reconciliation](../rc2b-06-b6-alt-b-slice-e-04-entry-owner-01-evidence/gava1-contract.md)

## Frozen artifacts for authorization

Bind the exact committed blobs from DESIGN-OWNER-01. The SHA-256 hashes below cover raw Git blob contents. The design manifest and its linked source inventory bind the inherited inputs.

| Artifact | Git blob | SHA-256 |
|---|---|---|
| [rc2b-06-b6-alt-b-slice-e-05-design-owner-01.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01.md) | `949bafc58184cff41e55a032070f3d7a3d62192c` | `ffe3cdc3b991c9d693829debe5dd3d418375d65e0bb29389069bab2c80a1e105` |
| [carry-forward.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/carry-forward.md) | `f9c0cb3c2ac4bee1cab357cdd0afbbd7ac646fa3` | `0e92cdd17848201ea2894731881a8486f506ba33c87f370d242a03375b8850a2` |
| [contract.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/contract.md) | `121443efb9e145e2413c5a2c40f59e45a55eaa55` | `8d782d55544a1fcc65a7adbe035e21ce7d7cd10d31658274b1620527b8b02a54` |
| [document-verification.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/document-verification.md) | `a1016214f239519f76210cb4c52fbddf42cc805c` | `f0ee5d51afb72f909978290dad1a5ac3e10bc0ef9bd04aab22ca0a68261ed645` |
| [launcher.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/launcher.md) | `98df570d8e3760deb4ccaf609049ec4db47a9863` | `d117966a3f0c7dcf8614817ff6977a5f737dea9471448574988a02ef1c80091f` |
| [lifecycle.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/lifecycle.md) | `d0562374f3af3d9974da96906d7e3d032309cff4` | `afff705a8de927780840c2608fbdabe01ef8443077cfe53616cce3f598b2ab91` |
| [preflight.json](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/preflight.json) | `0fcb5424638ebac1bc3809351e81c1811175933b` | `db7c195e50075b3c9bc56351ce10f07a7e2bb877ee3b7823be520e60f2178147` |
| [provenance.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/provenance.md) | `12d5cd0e44de1b57ca585e9550092306135637ea` | `0aa820bfb0b23d13795bc40efc00c977e7728278357d9c66dc5a99c974b2d443` |
| [qualification.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/qualification.md) | `5718f975d1ba42082834acff59e7aaeb6a527d5c` | `4b2aea1e56792961e5e342372ea761d937dae94615259ff50bfc9a9a154d1ad5` |
| [requirements.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/requirements.md) | `63ebfb7afed2dfb2883fae237ed64472c2adad66` | `ca9c64bbf85238859fbce8d07e260ab0cbd7940738d7a928d0f2da32ab3587f0` |
| [responsibilities.md](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/responsibilities.md) | `dc7c0d5930371a386eaeb71aff442e5a21fbdc6b` | `75a7db51380168b5c724eeae15d2c003e603616d41690f5bb87c8a1eee06337a` |
| [SHA256SUMS.txt](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/SHA256SUMS.txt) | `76a834798f3d6ab372dc59cb19d7fb911a5ee794` | `e48f0097802d130d6b96038795aa5cc2f91e1e88c6f19e4061f2fc279275f8a1` |
| [source-identities.json](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/source-identities.json) | `58955fde872e1b5a95115ed64fd242ad070fdda2` | `f795227f1063431dfd450288715200bb2e143a98e86e5c57c21803407b9a24d6` |

## Exact additive allowlist

| File | Existing project and purpose |
|---|---|
| `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationCustodyModelData.cs` | M1 immutable binding, expectations/evidence, enums and diagnostic snapshot |
| `src/Gagamba.Execution.Linux.M1/Bootstrap/ValidationCustodyModel.cs` | M1 construction, comparisons, transitions and managed lifetime |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelTests.cs` | M1.Tests structural and independent comparison/refusal controls |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelLifecycleTests.cs` | M1.Tests lifecycle, time, ownership, termination and concurrency |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelCompositionTests.cs` | M1.Tests test-only E-2/E-3/E-4 composition and reachability assertions |
| `tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/ValidationCustodyModelVectors.cs` | M1.Tests separately constructed expectation and observation vectors |

All six paths are absent from the index and checkout. Both parent directories already exist. Both solution projects use `Microsoft.NET.Sdk`, target `net10.0`, and include the existing Bootstrap folders via the default compile glob. Neither project nor the shared props or targets excludes these paths. Read-only MSBuild evaluation confirmed the active compile glob in both Debug and Release, with 37 existing M1 Bootstrap items and 14 M1.Tests Bootstrap items in each configuration. Existing `TransactionStateModel.cs` grants M1.Tests friend access in ordinary Debug and Release. The D6.4 fixture configuration has a separate friend declaration. Both `ValidationPreContext` and `Gava1Ids` are internal readonly value types in the target Bootstrap namespace. The accepted codec/verifier APIs are internal and reachable from M1.Tests through the existing project reference and friend grant. Existing Owner, Recovery and B2Controller friend access is accounted for by the reverse-call audit. [M1 project](../../../src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj) · [M1.Tests project](../../../tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj) · [Build props](../../../Directory.Build.props) · [Build targets](../../../Directory.Build.targets) · [Friend declarations](../../../src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs)

The two production additions use the existing M1 Bootstrap namespace. Only their own data/model types, ordinary BCL value, array and locking facilities, and `ValidationPreContext`, `ValidationPreTopology` and `Gava1Ids` are permitted. Test additions use `Gagamba.Execution.Linux.M1.Tests.Bootstrap`. Tests may call unchanged `FrozenContractVerifier.TryVerifyChain`, `DetachedEvidenceVerifier.TryVerifyDetachedEvidenceChain`, `ValidationPreCodec.DecodeBootstrap`, the existing `ValidationPreCodecBinary` surface and `Gava1ContextCodec.Decode`/`MatchPreBootstrap`. They guard nulls at the accepted E-2 boundary, retain original diagnostics and construct independent expectations before adapting suspect outputs. [Contract dependency rules](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/contract.md#permitted-dependencies)

The method surface fits a sealed internal model with a private constructor, `TryCreate`, instance `Advance` and `Dispose`. Both output parameters begin null. Only `None` publishes a model or snapshot. Immutable operands and a private gate suffice for the pinned priority, deadline and lifecycle rules. Terminal failure closes and clears retained model data; supported invalid inputs yield codes. A recoverable exceptional `Advance` closes the model before propagation with null output. `stopObserved` is a supplied input and needs no async or cancellation-token dependency. Synthetic labels and byte comparisons never yield authentic process evidence. [Contract](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/contract.md) · [Lifecycle](../rc2b-06-b6-alt-b-slice-e-05-design-owner-01-evidence/lifecycle.md)

## Authorization stop conditions

Bind this reassessment and the exact frozen design, recheck the assumed baseline and six absent paths, and stop on unexplained integrity, repository or dependency discrepancies. Stop if any mandated case needs a seventh path, a changed existing source or test, a new visibility grant, dependency, build or project edit, runtime hook, or an authority/effect edge. All existing production and tests, E-1–E-4, installed and runtime entry points, providers, projects, solution, native, CI, build policy and accepted evidence are protected. Stop qualification on any unapproved artifact or membership change, unexplained skip, failed regression or sensitivity control, policy failure, forbidden caller or effect, or unresolved material discrepancy. Candidate approval requires exact-revision producer and independent evidence and separate owner acceptance. [Producer, reviewer and P4 gates](verification-and-p4.md)
