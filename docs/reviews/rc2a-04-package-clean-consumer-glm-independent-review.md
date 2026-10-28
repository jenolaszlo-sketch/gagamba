# RC2A-04 independent package and clean-consumer qualification review (RC2A-04-GLM-Q1)

Date: 2026-10-28, Asia/Manila. Reviewer: independent GLM review session (OpenCode).
This is one focused review of the completed RC2A-04 local NuGet packaging and
clean-consumer qualification. It is not owner acceptance, not a new implementation
milestone, and it does not authorize publication, tags, RC2B, Hufu work or native work.

## 1. Reviewed identities

- Repository: `jenolaszlo-sketch/gagamba`, reviewed main `ec4e73bb3a922873e7b00cab4a9d85e23bc31d1c`
  (tree `58a5bad05f55b6ee43ee406e4f0930c1d77473a8`), a direct child of the exact package
  source commit `82cf8983a61f60a95d0aed0233ef0efac985148b` (tree
  `d37ad20f2d7e5ee4ee705e4c78b766df32b61f0c`, parent `3a5730a590b9c92901e6e240eeaddd8380958103`).
- Evidence: `docs/evidence/rc2a-04/` in the containing commit. Primary record
  [README](../evidence/rc2a-04/README.md).
- Retained actual artifacts: located and inspected at the sibling producer checkout's
  `.gagamba-rc2a04/attempt-03/` (ordinary directory, outside this repository). Reviewer
  probes were isolated under the reviewer's temporary workspace; no producer artifact
  was modified, and no branch or worktree was created.
- Candidate version: `0.1.0-rc2a.82cf8983a61f60a95d0aed0233ef0efac985148b`.
- Inherited acceptance chain: RC2A-03 R1 `271e34b45fa504f6bb7ef7359c4f2f1a9649e49d`,
  owner decision `1ce5c37f25c3bf49f55fc6a31a930f8430358d24`, Q2 `e060c91225274f13d53421b613a5736225ef6de4`.

The delta `82cf898..ec4e73b` is documentation-only (the evidence package plus a
PROJECT-STATUS status update). At `82cf898` the protected trees independently match
[preservation.json](../evidence/rc2a-04/preservation.json): `src`
`b8c167e3d7340a562221591bdc2b6df7adb372d1`, `native`
`85631d2f04512376621d3d09f51d67fd90f45335`, `tests`
`9c906b349b0bf0cffdee331fb5700f9b7828c199`, `.github`
`ed8758aa675bf31bbb985f6745ac247c3228bacb`, `docs/design`
`a04b900e48b1e17e7450366bf2af91f58b9014a3`, `docs/decisions`
`6c03537e8aff3c9c3f8c53c1ad60263c55e1a713`, `docs/evidence`
`0e5b5509edda5bfa347cb45a7315bc07cd2601cd`, `docs/reviews`
`704d5ad065088b9ba72a94f7ed0182ec0ddccd45`.

## 2. Package binding verification (independently recomputed)

All checksums below were **recomputed by this review from the actual retained package
files**, not copied from manifests. All match [packages.json](../evidence/rc2a-04/packages.json).

| Package | Recomputed SHA-256 |
|---|---|
| Gagamba.Execution nupkg | `c7a2974229ba4f51ee7092ea6776cd6342d65f613e2ca9267f6d5e976489b0b3` |
| Gagamba.Execution snupkg | `5b19313affcd30553530331c414036706827671650b5e01ba68fc3d8c5d03511` |
| Gagamba.Execution.Linux nupkg | `1c23664705fd94cab8b3f9e93af697abf1f2e363c1dce99246811cc0e9165b37` |
| Gagamba.Execution.Linux snupkg | `4b31c430a9c6661f630550b20341211478abd59997fb0ddb4f1dacb8c8e3e58e` |
| Gagamba.Execution.MacOS nupkg | `753e90c740b80b585e1aec5d3cab614a4e62de0b28e81d4c2a0a21c787fa2513` |
| Gagamba.Execution.MacOS snupkg | `b49b18769b7dc305938f2cfc9c0e1533a8457ac8968c6c4b6cc113e2eb843774` |
| Gagamba.Execution.Windows nupkg | `6a91f3193f39b551d8259c583568b183f4994987ad3dddb0b61f880771e62ae8` |
| Gagamba.Execution.Windows snupkg | `b39905eb825560799df61e5d9274a78473e3351f6bcdb00de33c70b29fc2436f` |
| Gagamba.Runtime nupkg | `a0c5ddefc8e40b785eaa023010312ee58da6f9b669ae5137659874506b6cfe0c` |
| Gagamba.Runtime snupkg | `ba6c71ca684fe3f5520d569a84d864e2c15f6c8162cec893c9071a954e3a11f5` |

Additional independently verified binding facts:

- Each nupkg contains exactly its recorded member set (`lib/net10.0/<id>.dll`, README,
  nuspec, rels, content types, metadata); each snupkg contains exactly its portable PDB
  and metadata. No native runtime assets, test harnesses, private M1 assets or
  build/activation targets are present.
- All five packaged DLL SHA-256s were recomputed after extraction and match the
  manifest (Execution `60bd7de0…`, Linux `24c91dcf…`, MacOS `aba70699…`,
  Windows `b61881c0…`, Runtime `2dc11be8…`).
- All five nuspecs record repository commit `82cf8983a61f60a95d0aed0233ef0efac985148b`,
  the same coherent candidate version, and the recorded dependency graph: Execution has
  an empty dependency group; each provider references Execution at the candidate
  version; Runtime references Execution and all three providers at the candidate
  version. License expression metadata present.
- Independent reviewer-written inspection (own tool, not the producer's inspector) of
  the extracted DLL/PDB pairs confirmed, for all five packages: exported type counts
  57/1/1/1/7 exactly as in [all-symbols.json](../evidence/rc2a-04/all-symbols.json); 18
  exported types in `Gagamba.Execution.Restricted.Contracts`; zero public constructors
  on `*View`/`ManifestDecodeResult` types; zero public setters; zero control-method
  names (Launch/Prepare/Release/Stop/Recover/Authorize/Renew/Lookup prefixes); each
  DLL's CodeView PDB id matches its snupkg PDB; exactly one SourceLink document per
  PDB, each binding to `raw.githubusercontent.com/jenolaszlo-sketch/gagamba/82cf898…`.
  The only public constructor in the passive namespace is on
  `RestrictedPreparationReference` — the five-field passive value type that the
  accepted RC2A-03 contract deliberately makes consumer-constructible; it is immutable
  (getters/Equals/GetHashCode only).
- `direct\vectors.json` in the producer artifacts is byte-identical to the committed
  RC2A-02A vector manifest
  (`docs/evidence/restricted-consumer-contract-rc2a-02a-windows/exports/vector-manifest.json`):
  23 vectors, 21 with literal hex bytes, and exactly `Q2A-V-19-max-argenv-16384` and
  `Q2A-V-20-max-manifest-65536` as recipe-only (no byte export).

## 3. Clean consumer — independently reproduced

This review built its own isolated consumer under its own temporary workspace: its own
NuGet.Config (sources and fallback folders cleared, only a reviewer-controlled feed
containing hash-verified copies of the candidate packages), its own isolated global
packages folder, `TreatWarningsAsErrors`, exact bracketed version
`[0.1.0-rc2a.82cf8983a61f60a95d0aed0233ef0efac985148b]`, and no project or source
references. The consumer program (producer's inspectable harness, verified line by
line before use) was executed against the verified vector manifest:

- Result: `PASS`, **1,227 checks, 21 literal vectors**,
  `recipeOnlyNotMaterialized = [Q2A-V-19-max-argenv-16384, Q2A-V-20-max-manifest-65536]`,
  .NET 10.0.12, Microsoft Windows 10.0.26200 — identical to the recorded
  [results.json](../evidence/rc2a-04/results.json) `direct` receipt.
- The review-cache contained only `gagamba.execution` at the candidate version, with
  `.nupkg.metadata` source pointing at the reviewer's controlled feed.
- The harness independently verifies each literal vector's byte count and BCL SHA-256
  before decode, then exercises canonical re-encode, digest compute/verify, mismatched,
  zero-digest and wrong-length digest refusals, decode-input copy, encode-output
  independence, nested public inspection of the golden vector, all 873 proper-prefix
  truncation refusals, trailing-data/unsupported-schema/unknown-implementation
  refusals, the 65,536 constant with inclusive admission and 65,537 refusal,
  Supports/argument-null refusals, and passive reference construction, GUID/digest
  validation, content equality, defensive copying and inequality cases.
- All four expected consumer compile refusals were **independently reproduced** by
  this review with its own consumer project: view construction `CS1729`, schema
  setter `CS0200`, `Codec.Launch` `CS0117`, `reference.Release` `CS1061` — matching
  the recorded negative compilation receipt and the retained producer logs.
- Boundary distinction preserved: the clean consumer tested the size-boundary cases
  (inclusive 65,536 admission of an invalid payload; 65,537 refusal) and did **not**
  construct a valid maximum-size exported payload. V-19/V-20 remain historical
  digest/recipe-bound vectors with no standalone byte export; no independent
  materialization is claimed by this review. The committed recipe-based
  size/encode-limit checks exist inside the precise 457-test portable regression set
  (verified by test-name inspection of the retained TRX), which is exactly how the
  evidence README represents them. The two recipe/digest-bound large vectors are not
  represented anywhere in this record as standalone reproduced byte exports.

## 4. Mixed package graph — independently reproduced

This review restored and ran its own mixed consumer against its controlled feed:

- Exact bracketed references: Gagamba.Execution, Gagamba.Runtime (candidate version)
  and `Penghou.Hufu 0.1.0-preview.11` (package SHA-256 recomputed
  `a9083f1f6dcf17e851f9f89aa0ce49ab9ec711bd59330788a73efad3d233fc25`, matching
  [feed-inputs.json](../evidence/rc2a-04/feed-inputs.json) and
  [hufu-core.json](../evidence/rc2a-04/hufu-core.json)).
- The resolved graph is exactly the five candidate Gagamba packages at the candidate
  version plus Penghou.Hufu 0.1.0-preview.11 — no version conflicts, downgrades,
  unexpected packages or missing providers. The reviewer cache contained exactly six
  package folders, each `.nupkg.metadata` pointing at the reviewer-controlled feed.
- The mixed consumer compiled and loaded Runtime/provider public types and the public
  Hufu enum `Penghou.Hufu.AuthorityAction` via BCL generic collections only, printing
  `0,0,0,0,0` plus the recorded PASS line — byte-for-byte the recorded
  [mixed-hufu-type-run.txt](../evidence/rc2a-04/mixed-hufu-type-run.txt). No provider
  construction, runtime creation or lifecycle call was made, and no native workload
  was started.
- Hufu core's net10.0 dependency group is empty, as recorded.
- **Hufu Sandbox is unavailable as an actual package and its integration graph remains
  unqualified.** Core-package coexistence demonstrates nothing about the anticipated
  Sandbox graph; no Hufu source, constraints or provider selection were modified, and
  loading Hufu core types confers no live authority integration.

## 5. Regression and CI evidence

- The four retained TRX files were hashed and all match
  [results.json](../evidence/rc2a-04/results.json): windows portable
  `aca415df…`, windows public `d0b8e678…`, wsl portable `57333005…`, wsl public
  `66d49a26…`. Counters were re-parsed from the TRX XML, not copied: Windows 48 public
  + 457 portable, executed/passed all, 0 failed, 0 skipped; WSL2 identically 48 + 457,
  0 failed, 0 skipped (505/0/0 per platform as commissioned). Unique test names equal
  the totals; only managed unit-test entries are present; no skips.
- Exact filters verified from the recorded command receipts:
  `FullyQualifiedName~Gagamba.Execution.Tests.Restricted.` (public) and
  `FullyQualifiedName~Contracts.ManifestTests|FullyQualifiedName~Contracts.RestrictedPublicProjectionTests|FullyQualifiedName~PreparationIdentityTests|FullyQualifiedName~Tests.Qualification.`
  (portable). No broad native-inclusive filter or sandbox probe appears.
- Environment receipts match the recorded boundaries: Windows SDK 10.0.401/runtime
  10.0.12 win-x64 10.0.26200; WSL2 Ubuntu 26.04 SDK 10.0.113/runtime 10.0.13, WSL2
  kernel 6.6.114.1. WSL2 remains WSL2 — not native Linux. Native Linux and macOS
  consumer qualification remain **NOT RUN**; this review did not start any native
  workload or unqualified probe.
- CI: run `40649809860` (workflow `ci`, run 37) on `main` at head `ec4e73b…` (tree
  `58a5bad0…`), event push, conclusion `success`; 11 jobs — 10 succeeded, 1 skipped
  (`launch spike (windows)`). Verified via the GitHub API during this review.

## 6. Security boundary and inherited qualifications

- Packaging introduced no new public construction or authority surface in the passive
  contract namespace (independently inspected above). `Gagamba.Execution` also carries
  the older execution lifecycle SPI; only the passive contract was assessed here, and
  the full assembly is not represented as authority-free.
- The complete RC2A-03 owner qualification and exclusion register §§5–6 is inherited
  verbatim from [the owner decision](../decisions/restricted-consumer-contract-rc2a-03-owner-acceptance.md);
  historical evidence was not altered. Successful decoding, matching digests and
  passive-reference equality confer no execution authority, provider provenance,
  establishment, liveness, freshness, expiry, temporal semantics, release/control/
  recovery or runtime confinement. No cross-process hash determinism, binary/package
  byte reproducibility, published distribution, native sandbox, RC2B or Hufu
  integration claim follows from any result in this review. Test-result repeatability
  and recorded package hashes are not a binary reproducibility proof. Production and
  native identities establish preservation only.

## 7. Findings

No blocking findings. Non-blocking review notes:

- **N1 (provenance clarity).** The committed `commands.json` and the retained
  producer receipts contain producer-machine absolute paths. `logs.txt` substitutes
  operational directory paths as documented; the JSON receipts do not. No secrets are
  present. No action required for RC2A-04.
- **N2 (receipt origin).** `results.json` and `hufu-core.json` exist only in the
  evidence commit (no byte-identical copy under `attempt-03/`); their load-bearing
  contents (TRX hashes, package/Hufu hashes, counters) were independently verified
  against the retained artifacts, so provenance holds.
- **N3 (assertion count semantics).** The 1,227 figure is the consumer program's own
  assertion counter. This review reproduced it by executing the same harness after
  line-by-line inspection, against hash-verified vectors and the hash-verified
  candidate package, in its own isolated environment; it is not a separately
  reimplemented assertion suite.
- **N4 (untracked-file check).** "All 30 original untracked files unchanged" is a
  producer self-check without a committed pre-image hash list; it is not independently
  reproducible and is not load-bearing for this qualification.
- **N5 (WSL2 rerun not performed).** This review verified WSL2 evidence by hash and
  parsed counters, and did not rerun the WSL2 legs; nothing in the record depends on a
  reviewer rerun.
- **N6 (re-pack not re-executed).** The full `eng/rc2a04/qualify.py` reproduction
  (fresh re-pack from source in a separate checkout) was not re-executed by this
  review. The recorded pack commands were verified against the retained logs and the
  packages' internal repository/SourceLink metadata; binary reproducibility is not
  claimed in either direction, consistent with R2A-F03.

## 8. Verdict

**SUPPORTED WITH QUALIFICATIONS.**

RC2A-04 is eligible for owner acceptance with the restrictions recorded in the
producer evidence and inherited RC2A-03 §§5–6 register, including: local packages
only (no published distribution), Hufu Sandbox unavailable and its integration graph
unqualified, V-19/V-20 recipe-bound without standalone byte export and no independent
materialization, no valid maximum-size exported payload constructed by the clean
consumer, Windows/WSL2 managed evidence only with native Linux/macOS NOT RUN, no
authority/confinement/recovery/temporal claims from passive results, and no binary
reproducibility claim.

This review authorizes nothing further: no NuGet publication, no release tag, no RC2B
development, no Hufu modification, no native sandbox work and no further milestone.
The next step is the owner decision to accept RC2A-04 with these qualifications or to
require bounded remediation.
