# CANONICAL-MANIFEST-REVIEW.md — artifact identity and codec

Independent evidence: `probes/inspect_tar.py`, `probes/verify_candidate.py`,
`probes/tar_mode_check.py`, `probes/deps_analysis.py`, `probes/ReviewerProbeTests.cs`
(28 reviewer-authored xUnit probes run in an isolated source copy).

## 1. Archive vs artifact-set identity (distinct objects)

| Object | Measured SHA-256 |
|---|---|
| `candidate-payload.tar.gz` (the archive) | `0f4533b77daf2ae3c9d3a4487bc59d6be5be422393049f1fed5a3c0e54f1486a` |
| `candidate-manifest.json` bytes = **artifact-set identity** | `31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133` |

The two digests identify different objects. The expected artifact-set digest `31d8ebc2…` is exactly
the SHA-256 of the canonical manifest bytes, independently recomputed. It matches.

## 2. Safe archive inspection

`inspect_tar.py` opened the archive without extracting. Result: 20 members (5 directories + 15
files). **Zero** unsafe entries — no absolute paths, no drive paths, no `..`, no symlinks, no
hardlinks, no device/FIFO members. The 15 files are the 14 production artifacts plus
`manifest.json`. No `install-receipt.json`, no qualification record and no operator decision are in
the archive.

## 3. Independent artifact inventory

Extracted with Python `tarfile` into the reviewer workspace; every artifact hashed independently.

| Relative path | Role | Kind | Length (exp=act) | SHA-256 match | Class |
|---|---|---|---|---|---|
| `native/exec-coordinator` | coordinator | native | 878792 | ✅ | production |
| `native/seccomp-filter.bin` | seccomp-filter | policy | 672 | ✅ | production |
| `owner/Gagamba.Execution.dll` | owner-contract | managed | 179712 | ✅ | production |
| `owner/Gagamba.Execution.Linux.M1.dll` | owner-m1 | managed | 652800 | ✅ | production |
| `owner/Gagamba.Execution.Linux.M1.Owner.dll` | owner-entry | managed | 6656 | ✅ | production |
| `owner/Gagamba.Execution.Linux.M1.Owner.deps.json` | owner-deps | config | 1473 | ✅ | production |
| `owner/Gagamba.Execution.Linux.M1.Owner.runtimeconfig.json` | owner-runtimeconfig | config | 361 | ✅ | production |
| `provider/Gagamba.Execution.dll` | provider-contract | managed | 179712 | ✅ | production |
| `provider/Gagamba.Execution.Linux.M1.dll` | provider-m1 | managed | 652800 | ✅ | production |
| `recovery/Gagamba.Execution.dll` | recovery-contract | managed | 179712 | ✅ | production |
| `recovery/Gagamba.Execution.Linux.M1.dll` | recovery-m1 | managed | 652800 | ✅ | production |
| `recovery/Gagamba.Execution.Linux.M1.Recovery.dll` | recovery-entry | managed | 4608 | ✅ | production |
| `recovery/Gagamba.Execution.Linux.M1.Recovery.deps.json` | recovery-deps | config | 1432 | ✅ | production |
| `recovery/Gagamba.Execution.Linux.M1.Recovery.runtimeconfig.json` | recovery-runtimeconfig | config | 361 | ✅ | production |

**14/14 artifacts match** the manifest path, role, kind, length, mode, RID (`linux-x64`),
`buildIdentity=Production`, `consumer`, `required=true`, `survivesController` and SHA-256.
No test-only artifact is present.

Manifest container facts (independently recomputed):

- Raw bytes: 6536, no UTF-8 BOM, final byte `}` (no trailing newline), pure ASCII.
- 14 artifacts; **no** duplicate path; **no** duplicate role; **no** self-digest field.
- Artifact order is strictly ascending by `role` (ordinal), which is the canonical order required by
  the accepted design (§4.5 "sorted artifact entries (role, then path)"; roles are unique).
- `source.commit` = `8b897e4…`, `source.tree` = `af1dc67…` (reviewed revision).
- Shared assembly identity: `Gagamba.Execution.dll` = `5fa3de13…` and `…Linux.M1.dll` = `3bc7e5df…`
  are byte-identical in owner/provider/recovery.

## 4. Archive ownership observation

The archive members carry `uid:gid = 1000:1000` (the unprivileged build user), while the manifest
declares the **installed** `uid:gid = 0:0`. This is correct and not a defect: the candidate archive
is untrusted installer *input*; the privileged installer re-creates every file as root with the
declared mode. Modes are a candidate contract and all match (`0444`, coordinator `0555`); ownership
is applied at install time. See FINDINGS F01.

## 5. Canonical codec probes (independent, reviewer-authored)

Positive control: the golden vector parses and its recomputed digest equals
`8f9527205a66941dba2da98ee473886315a418a201a3f326fd60193e539054b0`. All 28 reviewer probes passed:

| Probe | Mutation | Expected | Result |
|---|---|---|---|
| P01 | artifact SHA-256 uppercased | refuse | Pass |
| P02 | artifact SHA-256 all-zero | refuse | Pass |
| P03 | 63-char hash | refuse | Pass |
| P04 | 65-char hash | refuse | Pass |
| P05 | duplicate nested `source.commit` key | refuse | Pass |
| P06 | artifact fields reordered (`kind` before `role`) | refuse | Pass |
| P07 | mode `"555"` (no leading zero) | refuse | Pass |
| P08 | length as decimal (`878792.0`) | refuse | Pass |
| P09 | `uid` as string `"0"` | refuse | Pass |
| P10 | extra `toolchain` field | refuse | Pass |
| P11 | `required=false` | refuse | Pass |
| P12 | coordinator `survivesController=true` | refuse | Pass |
| P13 | coordinator `consumer=owner` | refuse | Pass |
| P14 | `profileVersion="2"` | refuse | Pass |
| P15 | `ownerProtocol=2` | refuse | Pass |
| P16 | `policy.profileId` mismatch | refuse | Pass |
| P17 | `requiredFeatures` reordered | refuse | Pass |
| P18 | self-consistent triple | accept (positive control) | Pass |
| P19 | receipt `installedByUid=1000` | refuse | Pass |
| P20 | receipt unknown `expectedDigestSource` | refuse | Pass |
| P21 | receipt `manifestFileSha256` mismatch | refuse | Pass |
| P22 | receipt `qualificationId="a/b"` | refuse | Pass |
| P23 | record `buildConfiguration="Debug"` | refuse | Pass |
| P24 | record with 33 limitations | refuse | Pass |
| P25 | record with extra self-asserted `accepted=true` | refuse | Pass |
| P26 | active pointer boundary (no LF, CRLF, uppercase, all-zero, extra bytes) | refuse | Pass |
| P27 | `qualificationId` grammar boundary (1…128, leading/trailing dot, separators) | enforce | Pass |
| P28 | fixed-root inspection does not report Qualified | enforce | Pass |

Plus the producer's own 63 portable cases were independently re-run and all passed (see
REGRESSION-REVIEW.md). The implementation enforces **deterministic serialization** by re-serializing
the parsed JSON and requiring byte equality (`InstallJson.Read(bytes, canonical: true)`), **fixed
field order** (`Keys(..., ordered: true)`), **strict types** (`String`/`Int`/`Hex`), **exact UTF-8**
(printable ASCII only), **input-size bounds** (`MetadataLimit = 256 KiB`), and **no self-digest**.

## 6. Dependency closure

Independently parsed from the published `.deps.json` and `.runtimeconfig.json` inside the candidate:

- `runtimeTarget = .NETCoreApp,Version=v10.0/linux-x64` for both owner and recovery.
- Exactly **three** managed runtime assets per component (entry, `Gagamba.Execution.dll`, `…M1.dll`);
  no `native` group; no `runtimeTargets` group.
- `System.IO.Hashing` and `SourceLink` are **absent** from the published runtime asset graph
  (they appear only in the ordinary build-only dependency metadata).
- runtimeconfig pins `Microsoft.NETCore.App` `10.0.14`, `rollForward=Disable`, and the two safety
  properties false.

The verifier enforces this: `InstalledPayload.CheckDependencies` requires the deps runtime asset set
to equal the installed managed set and rejects any unsupported asset group. A filename absence alone
was not accepted as proof — the actual published dependency metadata and the candidate contents were
parsed independently.

**Verdict (A): the candidate's actual fourteen-artifact closure agrees with the accepted canonical
manifest and the artifact-set digest.** Integrated native-loader/system closure remains outstanding
(see NATIVE-QUALIFICATION-LIMITS.md).
