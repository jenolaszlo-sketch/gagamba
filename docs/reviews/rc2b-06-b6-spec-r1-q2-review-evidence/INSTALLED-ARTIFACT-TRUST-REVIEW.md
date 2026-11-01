# B6-SPEC-R1-Q2 — Installed-Artifact Trust Review

Substantive review of the amended B6 installed-artifact trust design
(`docs/design/rc2b-06-b6-installed-artifact-trust.md` @ `77ee441`, unchanged at `5b34667`).
This is a contract/specification review, not native execution. The design is a
specification remediation; it does not implement or exercise an installer, verifier,
provider or native qualification.

## 1. Objects and boundaries defined

| Object | Where | Role in trust |
|---|---|---|
| Installed artifact | §2.2 A1–A16, A30, A31 (application-controlled); A17–A24 (platform baseline) | bytes to be verified |
| Artifact content identity | per-file SHA-256 + length; artifact-set digest = SHA-256 over canonical manifest bytes (§4.1, §4.5) | integrity |
| Installation location | `/opt/gagamba/installations/<artifactSetSha256>/` (§5.2, §7.2) | immutable, root-owned, no symlinks/hardlinks/setuid/caps |
| Producer/publisher identity | none cryptographic; distributor identity is **absent by design** (§4.4 rejects signing, §10.1) | not a trust input |
| Manifest / receipt | `manifest.json`, `install-receipt.json` (root:root 0444) | declarative content + authorization record |
| Trusted reference state | `/opt/gagamba/active` (root:root 0444, 65 bytes) + `install-receipt.json` + `qualifications/<qualificationId>.json` | authoritative expected identity |
| Installer authority | root (uid 0) installing in the disposable WSL2 instance (§3.2, §4.2, §7.4) | authorization decision |
| Runtime consumer | trusted controller host; owner/recovery processes (§3.2, §6) | consumption |
| Host/environment assumptions | kernel, cgroup2, namespaces, `openat2`, `pidfd`, `SO_PEERPIDFD`, subreaper, userns, seccomp, glibc ≥ 2.43, exact .NET host/runtime/hostfxr, bwrap 0.11.1 (§2.4, §5.2 step 5) | platform baseline |
| Evidence authorizing execution/use | root-owned receipt + accepted qualification record bound to the same digest (§4.2, §4.5, §4.7) | authorization |

## 2. B1 — Root of trust

Trust does **not** derive from verified content alone, a signature, a pathname, a filename, a package label or a manifest entry by itself. It derives from the **conjunction**:

1. root-owned, non-group/other-writable, symlink-free immutable generation directory with root-owned ancestors to `/` (§5.2 step 1);
2. a canonical manifest whose artifact-set digest is recomputed from the installed bytes and must equal the directory name, `active`, the receipt and the qualification record (§4.5);
3. a root-owned `install-receipt.json` whose `qualificationId` + `qualificationRecordSha256` select exactly one accepted qualification record (§4.5 F13 remedy);
4. a root-owned accepted qualification record matching digest, profile, RID, scope and `buildConfiguration` (§4.5).

**Authoritative vs descriptive.** §4.1 marks source commit/tree, toolchain and qualification text as *descriptive*; hashes and the root-owned receipt/record are the *authorization*. §4.2 states explicitly that a SHA-256 manifest establishes agreement between bytes and a digest only if the digest has an independently trustworthy source, and that a manifest beside its binaries in a user-writable directory does not establish trust. This directly forecloses "pathname/label/manifest entry silently becomes a guarantee."

**Assessment:** the root of trust is coherent and is the smallest mechanism consistent with the accepted trusted-host model. No self-asserted trust evidence is accepted as authorization: a self-generated test report cannot self-approve (the locally tested candidate → accepted record transition is a separate root action, §4.5).

## 3. B2 — Binding between identity and artifact bytes

- The artifact-set digest is recomputed from the installed bytes, not read from the manifest's own statement (§4.5).
- The verifier opens the generation directory and each artifact **descriptor-relative** with `RESOLVE_BENEATH|RESOLVE_NO_SYMLINKS|RESOLVE_NO_MAGICLINKS`, hashes from the fd, and requires identical `dev/inode/mode/uid/gid/size/mtime/ctime` and `nlink == 1` before and after hashing (§5.2 step 2).
- The exact set is enforced: every regular file once in the manifest; every manifest entry present; symlinks, hardlinks, devices, FIFOs, sockets, sparse/abnormal objects rejected (§5.2 step 3).
- The active pointer is atomically updated (temp + fsync + rename); a partial/concurrent install is never named by `active` (§4.2, §5.2 step 4).

**Identity-vs-bytes substitution scenarios:**

| Scenario | Contract | Answer |
|---|---|---|
| Valid identity applied to another artifact | exact-set + per-file hash recomputation | refused (`ArtifactIntegrityMismatch`) |
| Receipt describes different installed bytes | receipt digest vs recomputed digest | refused |
| Artifact changes after verification | root-owned immutable tree + fd hashing + post-start closure/echo + controller re-read of `active` before RELEASE | refused within model; root-only residual documented |
| Path normalization / alias ambiguity | manifest path grammar (relative, no `.`/`..`, single component) + `RESOLVE_BENEATH` | refused |
| Replacement of an installed artifact | root-owned dir prevents non-root rename; post-start echo | refused within model |
| Untrusted metadata influencing which bytes are checked | compiler/derived policy constants; receipt/record root-owned; target cannot supply paths | refused |

**Platform-specific aliases.** The design's declared initial platform is WSL2 x86-64 with a Linux filesystem; symlinks, hard links and non-regular objects are explicitly rejected. Windows junctions/reparse points are outside the declared B6 scope (Windows host is only the WSL2 launcher; B6 does not claim Windows restricted execution), and the design does not invent defenses for excluded platforms. The exclusion is documented (scope §, plan §1.1/§8).

**Assessment:** the trust decision is bound to the exact object consumed, within the declared model. The residual for a root actor replacing bytes is explicitly documented rather than hidden.

## 4. B3 — Trust lifetime and state transitions

| Transition | Contract | Invalidation behavior |
|---|---|---|
| Initial install | §4.3 step 3 | `Installed/Unqualified`; `expectedDigestSource="unverified-candidate"` |
| Verification | §4.5 | recompute digest; compare to `active`, receipt, record |
| Accepted use | §8.1 | full install verify at `CreateOperation`; generation-still-active check at `PrepareAsync` start |
| Repair/replacement | §4.3, §6.5 | new immutable generation; old trust not extended |
| Upgrade | §6.5 | new generation; no cross-generation adoption |
| Removal | §6.5, §7.5 | refuses active/held/in-use; digest-name match required; `Unknown` if holders undeterminable |
| Interrupted install | §5.2 step 4 | incomplete generation never named by `active` |
| Stale receipt/cached evidence | §4.5 state table | referenced record hash/selection mismatch → `Unqualified` |
| Version/identity mismatch | §7.6, §4.6 | typed refusal |
| Artifact changed after verification, before release | §5.2 step 8, §5.4 | controller re-reads `active`; refuses `ReleaseRequest` |

**Key property:** a successful verification of an earlier artifact does not authorize a subsequently changed artifact — the controller re-reads `active` before RELEASE, and each surviving actor echoes its identity before authority. A deployment change grants no release.

## 5. B4 — Time-of-check/time-of-use

- **Binding:** fd-based hashing with before/after identity comparison; the verified identity is bound to the object actually opened.
- **Immutable boundary:** root-owned, group/other-non-writable, symlink-free tree; ancestors to `/` root-owned and non-group/other-writable with the sticky exception removed (§5.2 step 1, F06 remedy).
- **Revalidation:** controller re-reads `active` before RELEASE; `PrepareAsync` start rechecks the generation.
- **Explicit limitation:** the design states it "does not claim an atomic or race-free guarantee beyond what the OS operations establish," and documents the **root-only residual** (a root/admin or kernel compromise replacing bytes) as outside the accepted model (§5.2, §5.5).

**Assessment:** within the declared environment and accepted security contract, the specification binds verified identity to the consumed object via fd hashing, cleared loader environment, immutable root-owned paths and pre-release recheck. It does **not** claim that a mutable path stays unchanged merely because it was checked; the residual is bounded and disclosed rather than assumed away. This satisfies B4.

## 6. B5 — Failure and refusal semantics

The design uses a consistent, conservative vocabulary and never converts missing/invalid/unsupported evidence into a trusted result:

- `Unavailable` / `Unqualified` / `Qualified` (descriptor state; §4.6).
- Typed diagnostics: `ArtifactIntegrityMismatch`, `InstallationGenerationChanged`, `ProtocolIncompatible`, `RuntimeDependencyMissing`, `UntrustedMetadata`, `PartialInstallation` (§4.6, §8.6).
- Pre-existing refusal reasons reused: `UnsupportedProfile`, `PrerequisitesUnavailable`, `InstallationUnavailable`, `UnqualifiedInstallation`, `InvalidBinding` (§7.6, §8.3).
- Cleanup/recovery vocabulary unchanged: `Confirmed`/`Failed`/`Unknown` (§5.4, §8.5).

| Condition | Contracted result |
|---|---|
| Missing trust evidence | `Unavailable`/`Unqualified`; no operation (§8.1, §8.2) |
| Malformed evidence | refusal; never best-effort (§4.6, plan T21) |
| Conflicting evidence | `Unqualified` (§4.5 state table) |
| Unsupported version | `ProtocolIncompatible`/`UnsupportedProfile` (§7.6) |
| Mismatched artifact | `ArtifactIntegrityMismatch` (§7.6) |
| Untrusted producer/origin | `UntrustedMetadata`; root-owned receipt required (§8.6) |
| Incomplete installation | `PartialInstallation`; never active (§5.2 step 4) |
| Unavailable verification dependency | `RuntimeDependencyMissing`/`InstallationUnavailable` (§8.3, §2.7) |
| Unexpected/failed validation | `Unqualified`; no optimistic state (§4.6) |

**Assessment:** failure semantics are explicit, typed and fail-closed. No documented path treats missing/invalid trust evidence as trusted. The vocabulary is the B6 documents' own (`Unavailable`/`Unqualified`/`Qualified` and the listed diagnostics); Q2 introduces no new production status model.

## 7. B6 — Authority boundary

| Action | Permitted actor | Basis |
|---|---|---|
| Construct the manifest | candidate build (unprivileged) | §7.1 |
| Construct the receipt | root installer | §4.2, §7.3 |
| Verify evidence | verifier at admission | §8.1 |
| Record acceptance | root running `qualify-install.sh` after reviewing evidence | §4.5, §7.3 |
| Authorize consumption | `CreateOperation` (re-verifies); descriptor is not an admission token | §4.6, §8.1 |
| Invalidate prior trust | generation change / removal policy; no cross-generation adoption | §6.5 |
| Override/recover from failure | existing C1–C4 / C2 / Policy A stop paths; recovery retains stop only | §5.4, §6.4 |

- The descriptor is passive: "A descriptor alone never grants `RELEASE`, creates a preparation capability, enrolls sources or changes provider selection" (§4.6).
- Recovery never gets RELEASE (`RecoveryObligation.ReleaseAuthority => false`, §6.1, §6.2.7).
- A component that supplies a plausible receipt or metadata object does not thereby gain authority: the receipt and record are root-owned, and the verifier requires the record hash to match the receipt-supplied `qualificationRecordSha256` (§4.5).
- The receiver of an installation echo compares the peer's claim against its **own independently held expected value** from the manifest, so an actor cannot qualify itself (§6.2.6).

**Assessment:** the authority boundary is coherent. Untrusted caller input cannot grant itself authority; authority requires root-owned artifacts plus a separate trusted qualification action.

## 8. Residuals and honest limitations

Documented, non-hidden:
- Root/admin and kernel compromise; same-UID hostile host processes; compromised trusted bootstrap (§3.1, §11).
- A root actor could replace the source-root path after verification (F03 residual) and could replace installed bytes (root-only residual).
- A root-owned ICU/OpenSSL/zlib update changes a loader input without changing the Gagamba manifest; this is accepted as a platform-baseline residual when non-invariant globalization is selected (§2.7).
- Recovery continuity may be lost after a controller crash plus a deployment change — recorded as conservative `Unknown`, not silent adoption (§6.5).
- No binary reproducibility claim (§4.3, §11).

These are correctly bounded and disclosed.

## 9. Gate B/C/D conclusion

The amended B6 design defines a coherent root of trust (root-owned receipt + accepted qualification record), binds trust to the exact consumed bytes, invalidates trust across generation changes, documents (does not hide) the root-only TOCTOU residual, fails closed on every malformed/missing/unsupported condition, and preserves the controller/owner/recovery authority separation with no new release authority.

**No blocking trust-boundary defect was found.** The design does not accept self-asserted trust evidence where independent authority is required, and no documented path treats missing/invalid evidence as trusted. Residual concerns are documentation/precision only (see `FINDINGS.md`).
