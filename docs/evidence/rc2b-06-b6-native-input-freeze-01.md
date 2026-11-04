# Native input freeze evidence

Date:2026-11-04 (Asia/Manila). Commission:GAGAMBA-RC2B-06-B6-NATIVE-INPUT-FREEZE-01.
Baseline:canonical main `af4300f5dff7e4e49fca98233911a44a15894a43`, tree`2d0ece92e20247d3d61ac84e9215837a713e9921`.
Final production source:unchanged; documentation delivery is the commit containing this receipt (resolve with git log for this path; final commit/tree reported in handoff).
Host:Windows11 x64; Python3.14 offline parsing, existing Git GPG2.4.9, curl; no Linux runtime/build/probe execution.
Relevant native prerequisites:source-only here; actual guest kernel/uid/delegation/root/runtime maps remain unobserved.
Scope:evidence preservation, signed metadata and small package-byte verification, source/planning/owner-draft/driver handoff, docs-only commit.
Exclusions:image download/import; package installation; sudo/root/admin commands; WSL startup or config; cgroups/namespaces; production/build/installer changes; candidate build; qualification publication/activation; driver implementation; S4/B7; branches/worktrees; cleanup/pruning/push.

**INPUT FREEZE READY WITH QUALIFICATIONS.** Image metadata and exact bwrap/libc/host/fxr bytes are established; no native pass or independent acceptance act is added. Full image bytes, dependency transaction/payload set, compiler/linker/static inputs and coordinator reproduction, actual inventories/features/delegation, candidate and authority remain gates. [Closeout](../plans/rc2b-06-b6-native-input-freeze-01.md), [human manifest](../plans/rc2b-06-b6-native-platform-input-manifest.md), [machine contract](../plans/rc2b-06-b6-native-platform-input-manifest.json), [gates](../plans/rc2b-06-b6-native-provisioning-gates.md), [driver handoff](../plans/rc2b-06-b6-native-installed-driver-handoff.md).

## Source and preservation controls

Expected repository/origin/main/HEAD/tree matched. Tracked tree/index clean. Live and cached origin/main21f0aa7... one commit behind plan; no fetch or push. S3 implementation/completion/handoff ancestry passed. Exact src tree`aa6b2678281a45f4dc835da9dd254c77ef294657`, native tree`85631d2f04512376621d3d09f51d67fd90f45335`, props blob`78c01a4335143c58abbafaec15308ab86e25832a`, targets blob`eb1755b3530ce30d64a28ab24d6b1c12e47e7f9c`; no production change since5b2fe735... through starting plan. [Full binding](rc2b-06-b6-native-input-freeze-01/production-source-binding.json).

Starting44 untracked files were hashed by path/length/SHA-256 and74 worktree registrations recorded. Final validation compares all originals and historical registrations; canonical-main HEAD advancement is accounted separately. Source and original reviewer packages are not modified. User authorizes additive documentation/evidence commit, not host execution. Sandbox-access retries for read-only Git, metadata fetch and standard-user GPG do not constitute Linux root/admin execution.

## Terra Q1 preservation

Original`C:\Users\Laszlos\source\repos\Solo\gagamba-native-env-prep-01-q1`: all25 SHA256SUMS entries passed, exact path set matched, original manifest separately hashed as`f5d21890bc2c366452c3dbed2a104c9c04b6678b1fe29b48741f0cdde515a880`. Complete original package26 files,339,176 bytes. The [canonical index](../reviews/rc2b-06-b6-native-env-prep-01-q1-independent-review.md) preserves source/model/disposition and all qualifications. Copies are byte-verified and Git blobs checked. Scoped new evidence-directory attributes disable text/whitespace conversion without changing root attributes or historical evidence. Added preservation metadata is distinguished from the26 original paths.

No review outputs were regenerated or line endings normalized. Report/matrix/findings/handoff retain Terra High's PLAN SUPPORTED WITH QUALIFICATIONS, zero demonstrated BLOCKER/MAJOR/MINOR and O01–O05. Latest S3-Q1 environmental-only disposition remains unchanged. [Original inventory](rc2b-06-b6-native-input-freeze-01/q1-original-inventory.json), [before receipt](rc2b-06-b6-native-input-freeze-01/q1-preservation-before.json), final copy/index verification in validation.json. Owner planning decision remains **PENDING**, recommended acceptance of planning only.

## Offline platform verification actually performed

Only small official metadata/public keys and four package payloads were acquired. No distribution image was downloaded. curl was bounded by per-file maximum and timeout; original checksums/signatures/indexes/packages are retained. Public-key verification used an isolated workspace GPG homedir and `--no-autostart`; no user trust database/keyring or daemon configuration changed. Initial sandbox GPG startup failed; first Windows-style homedir invocation failed, then POSIX path syntax succeeded. Those failed logs remain preserved; no approval rejection is outstanding.

| Check | Actual result / raw evidence |
|---|---|
| Image checksum signature | GPG exit0, VALIDSIG full CD-image fingerprint843938DF228D22F7B3742BC0D94AA3F0EFE21092, matching official tutorial; signed checksum names exact26.04.1 amd64 WSL image. `platform/image-signature-verification.log` and original SHA256SUMS/.gpg. Actual image hash remains null. |
| Repository signatures/index hashes | Base and updates InRelease signatures valid for official archive key F6ECB3762474EDA9D21B7022871920D1991BC93C; both compressed amd64 main index lengths and SHA-256 match signed records. `platform/metadata-chain.json`, signature logs and original indexes. |
| Package metadata selection |60 source records across base/updates,42 proposed exact package choices. Complete transaction is **not solved**; indexes retain extra dependencies for independent replay. `platform/selected-package-records.json` and machine manifest. |
| Bubblewrap | Authenticated .deb0.11.1-1ubuntu0.3 amd64; actual member hash matches exact source pin, ELF64/machine62, archive0:0/0755, direct NEEDED selinux/cap/libc. No binary execution or installed capability check. `platform/bubblewrap-byte-observations.json`. |
| glibc | Authenticated libc6 .deb2.43-2ubuntu2.4; actual ELF export/definition and version lookup finds both posix_spawnattr_*cgroup_np at GLIBC_2.39, compatible with source's stricter>=2.43 requirement. Exact loader/libc hashes recorded; no ABI invocation. `platform/runtime-byte-observations.json`. |
| .NET host/fxr | Two exact10.0.14 .debs authenticated; actual expected host/fxr member paths/hashes and direct NEEDED inspected. SDK/shared runtime remain authenticated metadata only, no directory inventory/role-loaded positive. Same runtime observation JSON. |
| Old package acquisition | Image-list0.11.1-1ubuntu0.1 URL returned404; recorded in package-fetch-results.json. It is not selected, and no current-image bwrap byte claim is made from unsigned package-list metadata. |

`inspect-platform.py` is a new evidence-local offline parser, not production tooling: validates signed-index hash links, parses ar/tar members without executing or unsafe tree extraction, parses ELF sections/dynamic entries/versioned exports, and writes observation JSON. `make-freeze-manifest.py` authors the specification from those observations and nulls; it creates no approval. Their execution checks metadata and bytes only. No build, installer or native qualification test ran. Existing source/native tests were not rerun for documentation.

Luna assisted read-only source contract and future driver/probe specification. Primary source QA corrected the initially missed B2Controller friend assembly and historical-kernel-versus-actual-string distinction before integration. This was drafting assistance, not Terra's subsequent input-freeze review or owner acceptance. No driver/probe implementation was delegated or performed.

## Remaining evidence and validation

The machine manifest deliberately leaves unresolved source delivery, complete package transaction, actual shared runtime/fxr inventories, compiler/linker/static hashes, image bytes/internal build ID, native feature results, numeric runtime identity/actual cgroup/outer budget, candidate hashes, final qualification/owner/admin records and driver artifacts. No value was invented to fill a required field. Fixed source-native pins are expected values, not new candidate hashes.

Validation must verify Markdown links, JSON/schema/status labels,14 candidate paths and14 probe schemas, pending owner/admin records, source-only change scope and staged whitespace. Preservation checks include all26 original Q1 files, copied bytes and staged Git blobs,44 original untracked hashes and74 worktree identities. [validation.json](rc2b-06-b6-native-input-freeze-01/validation.json) records delivery checks; [SHA256SUMS.txt](rc2b-06-b6-native-input-freeze-01/SHA256SUMS.txt) hashes new raw freeze evidence, separately from the original Q1 manifest. Native cleanup is not applicable: no Linux child/domain/environment was created. All source/review/metadata/package evidence is intentionally retained.
