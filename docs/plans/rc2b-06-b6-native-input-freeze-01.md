# Native qualification input freeze and planning closeout

2026-11-04 (Asia/Manila) · GAGAMBA-RC2B-06-B6-NATIVE-INPUT-FREEZE-01

**INPUT FREEZE READY WITH QUALIFICATIONS.** Exact image checksum metadata, four package payloads and selected signed package identities have been established without provisioning. Complete package transaction, compiler/static-byte identities and coordinator reproduction, full runtime inventories, actual image/root/feature/delegation evidence, new S3 candidate and independent qualification authority remain execution gates. This package does not authorize any execution stage.

## Source and authority

Starting canonical `main` is `af4300f5dff7e4e49fca98233911a44a15894a43`, tree `2d0ece92e20247d3d61ac84e9215837a713e9921`, origin `https://github.com/jenolaszlo-sketch/gagamba.git`. Tracked tree/index were clean; 44 original untracked files and 74 registrations were inventoried. Cached and live origin/main remain `21f0aa7f3afd3a7dbfbc6ecae51d4025b13439b7`, one commit behind. No newer production baseline is substituted.

S3 implementation `5b2fe73567a9c33df50fc7c662436a67814a86f4`, completion `0eb7a7174efb04237a7d69732a6589d1e945125c`, and handoff `21f0aa7f3afd3a7dbfbc6ecae51d4025b13439b7` are ancestors. `src`, `native`, Directory.Build.props and Directory.Build.targets have no changes from the S3 implementation to the frozen plan revision. Exact trees/blobs and complete later-path list are in [production binding](../evidence/rc2b-06-b6-native-input-freeze-01/production-source-binding.json). A later documentation delivery commit changes its own commit/tree identity; the future candidate source stays frozen to **af4300f5.../2d0ece92...** unless independently rebound by explicit decision. Builder requires canonical main with a real `.git` directory, so building that revision after main advances is a concrete source-delivery gate; do not checkout/reset current canonical main or silently build a newer tree. A later commission must authorize and review the exact source-delivery mechanism without creating a linked worktree under this commission.

Normative sources remain the [B6 specification acceptance](../decisions/rc2b-06-b6-spec-owner-acceptance.md), [wire acceptance](../decisions/rc2b-06-b6-wire-owner-acceptance.md), [S1](../decisions/rc2b-06-b6-impl-01-s1-owner-acceptance.md), [S2](../decisions/rc2b-06-b6-impl-01-s2-owner-acceptance.md), [F02](../decisions/rc2b-06-b6-s2-f02-trust-store-owner-decision.md), [B2 amendment](../decisions/rc2b-06-b6-s3-b2-wire-owner-acceptance.md), actual installed/build/runtime code and accepted [qualification plan](rc2b-06-b6-qualification.md). No security contract, historical disposition or production code is changed.

## Review preservation and planning decision

Terra High's fresh-context independent Q1 says **PLAN SUPPORTED WITH QUALIFICATIONS**, zero demonstrated BLOCKER/MAJOR/MINOR and five observations. All 25 manifest entries and the separately hashed original manifest were verified, with no missing or extra paths. The original external package remains unchanged; canonical copies retain original bytes and relative paths, including its earlier supplemental-copy attempt. See the [canonical review index](../reviews/rc2b-06-b6-native-env-prep-01-q1-independent-review.md) and preservation receipt.

The [owner decision draft](../decisions/rc2b-06-b6-native-env-prep-01-owner-acceptance.md) is **PENDING**. Recommended disposition is **ACCEPTED WITH QUALIFICATIONS — PLANNING MILESTONE ONLY**. Preparing that draft does not constitute an owner acceptance act. Independent review and this input freeze do not accept S3 security, qualify native B6 or activate production. Latest S3-Q1 remains **INCONCLUSIVE — ENVIRONMENTAL QUALIFICATION ONLY**; no demonstrated production defect is inferred from unavailable environment evidence.

## What was established now

| Input | Result and exact boundary |
|---|---|
| Ubuntu image | Official release candidate `ubuntu-26.04.1-wsl-amd64.wsl`; expected SHA-256 `48d56724b5c8e60f24893e83e73bbb58c60b3ca22fba3da977075420acd54104`. Checksum signature verified against the full documented Ubuntu CD-image fingerprint. **VERIFIED-METADATA**, not image VERIFIED-BYTES; no image downloaded. |
| Signed package metadata | Frozen resolute and resolute-updates InRelease signatures, SHA-256/length-bound amd64 main indexes, 60 selected records representing 42 proposed package choices. Original signed indexes retained. This is not a solved installation transaction. |
| Bubblewrap | `0.11.1-1ubuntu0.3`, amd64, downloaded 51,032-byte .deb matches signed index; extracted 80,424-byte regular `usr/bin/bwrap` exactly matches accepted SHA-256 `523da3e7399044be5163aee6f57a77a6bef7454376e28f0a0627920bae1b76b6`; archive uid/gid0, mode0755. **VERIFIED-BYTES**. Installed xattrs/ancestry and actual operation remain untested. |
| glibc | Exact `libc6_2.43-2ubuntu2.4_amd64.deb` matches signed metadata. Offline ELF parsing confirms `posix_spawnattr_setcgroup_np` and getcgroup exports at **GLIBC_2.39**. Current Gagamba still requires **glibc>=2.43**. No spawn or Linux library execution occurred. |
| .NET host/fxr | Exact10.0.14 Ubuntu .deb payloads authenticated and inspected offline; expected `/usr/lib/dotnet/dotnet` and `/usr/lib/dotnet/host/fxr/10.0.14/libhostfxr.so` found. Shared runtime and SDK package identities authenticated by metadata only. Actual protected directory inventories/loaded selection remain pending. |
| Toolchain | Signed metadata identifies GCC15.2.0-16ubuntu1 and binutils2.46-3ubuntu2, plus libc6-dev2.43-2ubuntu2.4 and related packages. Compiler/linker/static archive bytes and accepted coordinator reproduction were not checked. |
| Kernel/cgroup | Exact future probe contracts and narrow delegated authority specified; no feature, namespace, cgroup, LSM or systemd operation executed. |

Details, provenance and evidence levels are in the [human input manifest](rc2b-06-b6-native-platform-input-manifest.md) and [machine manifest](rc2b-06-b6-native-platform-input-manifest.json). Windows D: stores the VHDX; trusted Linux files remain under guest ext4 `/opt/gagamba`, never `/mnt/d`. Existing development `/` mode0777 is preserved as a negative control.

## Terra observations reconciled

Original identifiers and severities remain unchanged; partial input evidence is not closure of the whole observation.

| ID | Requirement / now available | Resolution now and later / owner decision / successor |
|---|---|---|
| O01 exact platform bytes | Signed image checksum and repository metadata; exact bwrap, libc, host/fxr package bytes; compiler package versions | **Partially evidenced, OPEN.** Image bytes, transaction/dependency lock, SDK/runtime/compiler/static-library bytes and coordinator equality remain. Continue bounded input acquisition/review before authorizing dependent package/install/build actions; owner approves exact inputs or a separately reviewed pin change. |
| O02 delegation | Source-bound topology/limits and14 future feature probes | **OPEN.** Numeric UID/GID, actual ControlGroup, permissions, outer budget and native positive/negative proofs need scoped operator authorization and N2 probes. No general root runtime. |
| O03 qualification bootstrap | Exact F02 schemas/stores and null approval fields | **OPEN.** Independent authority must decide a truthful staged-publication sequence if required positives need Qualified selection. No producer approval or final record is generated. Separate bounded policy/owner decision before publication. |
| O04 driver | Source/visibility/routes and distinct technical handoff | **OPEN; IMPLEMENTATION-REQUIRED.** Separately assign `GAGAMBA-RC2B-06-B6-NATIVE-INSTALLED-DRIVER-01` to Luna, then independently review it. It must precede installed tests/records claiming them, not necessarily image acquisition or basic platform probes. |
| O05 closure/truncation/capacity | Package/native dependency identities and feature/evidence contracts | **OPEN.** Actual production role maps/dlopen, HD-5 compatibility/configuration if changed, dedicated evidence-reader MSG_CTRUNC and measured build/memory/log/cleanup evidence still require approved tests. Do not infer cleanup or timing from prior synthetic/ptrace samples. |

## Candidate and publication freeze

New candidate is Release/Production linux-x64 from the exact frozen source, SDK10.0.114, runtime10.0.14 with Disable roll-forward, GCC15.2.0 and reviewed actual build inputs. It contains exactly14 artifacts; all per-file and set digests must come from actual reviewed new S3 bytes. Historical S2 archive/set identities are context only. No build ran here, and no final candidate digest is declared. Source delivery to the builder's main/real-.git requirement, selection of `gcc` versus distro default, and reproduction of static coordinator are explicit tooling/input gates; no tooling or outputs were changed to force consistency.

Frozen now: profile `ae4-linux-fixed-fixture-v1`, version1, RIDlinux-x64, scope`WSL2-x86_64`, source commit/tree, production config contract, role layout, native pins, metadata identities and refusal rules. Pending/null: actual artifact-set/archive hashes, actual runtime/fxr inventories/kernel string, qualification ID/raw record/hash, evidence/review/owner references and administrative acceptance. The [gates document](rc2b-06-b6-native-provisioning-gates.md) separates candidate identity, protected installation, independent acceptance, F02 publication and runtime verification.

## Next boundary and stop

Next independent review is Terra High's input-freeze review, not executed by this commission. After that review, owner decisions must separately cover planning acceptance; exact signed image/package/source inputs and any remaining acquisition; named distro/storage/runtime account; literal scoped privileged actions and resource/delegation unit; unprivileged probes; actual candidate; independent qualification inputs/publication sequence; driver implementation/test scope and adversarial mutations; evidence retention and teardown. There is no blanket sudo or unrestricted N1–N7 permission.

No environment was created or started; no packages installed, Linux binaries executed, builds run, namespaces/cgroups created, host permissions/configuration changed, approvals published, restricted execution activated, driver implemented, branches/worktrees created or push performed. Stop after documentation/evidence validation and additive canonical-main commit. Preservation and final delivery identity are recorded in the [evidence receipt](../evidence/rc2b-06-b6-native-input-freeze-01.md).
