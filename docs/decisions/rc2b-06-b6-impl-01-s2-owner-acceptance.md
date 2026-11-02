# B6-S2 owner decision on independent Q1

**S2 OWNER ACCEPTANCE PENDING FOCUSED TRUST-STORE DECISION.**

2026-11-03 · GAGAMBA-RC2B-06-B6-IMPL-01-S2-OWNER-01. Q1 supports the bounded S2 implementation with qualifications. F02 requires a decision on who provisions an additional root-controlled authorization file and what ending that authorization means. The historical review, accepted specification and wire decisions retain their original dispositions.

## 1. Subject and authority

S2 implements canonical artifact identity, exact installed-generation verification, receipt and qualification-record association, trusted-state decisions, transaction ordering and conservative retention. The evidence below supports those individual claims within the indicated test environments. S1 build separation and B1–B5 lifecycle authority continue to apply. Production restricted execution remains unavailable.

The governing records are the [installed-artifact design](../design/rc2b-06-b6-installed-artifact-trust.md), [qualification plan](../plans/rc2b-06-b6-qualification.md), [specification owner decision](rc2b-06-b6-spec-owner-acceptance.md), [wire owner decision](rc2b-06-b6-wire-owner-acceptance.md), [S1 owner acceptance](rc2b-06-b6-impl-01-s1-owner-acceptance.md) and [S2 commission handoff](../plans/rc2b-06-b6-impl-01-s2-commission.md).

## 2. Source binding

Canonical checkout `C:\Users\Laszlos\source\repos\Gagamba`, branch `main`, origin `https://github.com/jenolaszlo-sketch/gagamba.git`. The following four rows are consecutive parent-to-child commits.

| Subject | Commit | Tree |
|---|---|---|
| Accepted S1 baseline | `df3d6f0fdb5f45e8fd3d8f53eb983b2ad6bba4fb` | `f4a6519d5b3a2c9c984346e955e63ccb787a6b2e` |
| Recovery and first S2 implementation | `35e74fda0c2717e70aeaedc686b45c080b50ae15` | `640d8ce764ea22b00da2214b316a422acbc03b5e` |
| Reviewed S2 implementation | `8b897e4ebb8e53385199083a38086a9713f0e07a` | `af1dc67cf6c74979eb24e9395724b86f264f9a3f` |
| S2 evidence and starting HEAD | `c9d867d40e91af2342e85917759af0a6b05538a4` | `8bee84d0d8750d59cea21053ebeb0fea1fbf596b` |

The evidence child changes seven paths under `docs/` only, including the retained archive, manifest and producer verification ZIP. The tracked checkout and index were clean at entry. The 44 individual untracked paths agree with the recovery inventory. All 74 registrations agree, including the revisions of the other 73 worktrees. The observed live and tracking refs for `origin/main` both resolved to `401e1ea7f0be06b27bc188c827dd951016b3c62a`, six commits behind the starting HEAD. [Recovery record](../evidence/rc2b-06-b6-impl-01-s2-recovery.md) · [Producer evidence](../evidence/rc2b-06-b6-impl-01-s2.md)

## 3. Independent Q1

The reviewer tested an isolated copy of the reviewed revision and returned **S2-Q1 SUPPORTED WITH QUALIFICATIONS**. That supports the producer’s **S2 IMPLEMENTED WITH RESTRICTIONS — READY FOR INDEPENDENT Q1** within its original scope. The reviewer recommended owner acceptance and reported zero CRITICAL, MAJOR or MODERATE defects. Sections 11–12 give the owner’s disposition of those findings. [Independent review index](../reviews/rc2b-06-b6-impl-01-s2-q1-independent-review.md)

## 4. Evidence manifest

All 70 listed entries match their original SHA-256. The self-excluded manifest is 7,016 bytes with SHA-256 `99b87ace01ee5194e7d03e6dce5dc9cd821b9032499eb445c399acf878d97a60`. The preservation set totals 71 files and 39,056,448 bytes. It includes the listed source tar. The evidence index identifies the excluded working copies and distinguishes independent execution from inspection. [Original manifest bytes](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/SHA256SUMS.txt)

## 5. Candidate identity

**ARTIFACT IDENTITY ACCEPTED WITH QUALIFICATIONS.** The owner rehashed the retained archive and its 14 named artifacts without extracting or rebuilding the candidate. The 20 archive members comprise five directories, one manifest and 14 regular artifact files. Their paths, lengths, modes and hashes agree with the candidate inventory. Q1 found zero unsafe entries.

| Identity | SHA-256 |
|---|---|
| Original candidate archive | `0f4533b77daf2ae3c9d3a4487bc59d6be5be422393049f1fed5a3c0e54f1486a` |
| Canonical manifest file, 6,536 bytes | `31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133` |
| Artifact set, defined as SHA-256 over those canonical manifest bytes | `31d8ebc21c227e8ed6cf9e6952b69365a71f6ec02786c0cf0fa8a0ab8906e133` |

The manifest binds the implementation commit/tree in §2. Q1 found exactly three managed runtime assets per selected Linux x64 dependency target, no published `System.IO.Hashing` or SourceLink dependency, and .NET 10.0.14 with roll-forward disabled. The runtime loader and host libraries still require native qualification. [Retained archive](../evidence/rc2b-06-b6-impl-01-s2/candidate-payload.tar.gz) · [Manifest with all 14 per-file identities](../evidence/rc2b-06-b6-impl-01-s2/candidate-manifest.json)

## 6. Canonical manifest

Q1’s 28 new cases exercised positive controls, canonical encoding, hash syntax, field order, duplicate and unexpected fields, role and profile semantics, receipt and record binding, and selector boundaries. All passed. The raw probe TRX contains 37 passes because its filter also selected nine older recovery probes. The independent portable run reproduced all 63 producer cases. Two existing recovery cases also matched its filter, giving 65 passes in that TRX. Q1 observed no canonicalization bypass. [Canonical review](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/CANONICAL-MANIFEST-REVIEW.md) · [Raw test results](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/evidence/trx-review/)

## 7. Root of trust and self-assertion

**CANONICAL IDENTITY AND TRUST-ANCHOR REFUSAL ACCEPTED WITH QUALIFICATIONS.** Q1 constructed a user-owned candidate with mutually consistent manifest, receipt and qualification record. Payload and pure association checks succeeded. Changing the record failed its hash check. `check-staged` refused the untrusted ancestors and fixed-root `verify-active` returned `Unqualified` on the writable root. Only `VerifyActive()` can return a trusted installed state. Q1 found no production verifier test seam, environment override or caller-selected anchor. The positive trusted-installation control remains pending. [Trust-anchor probe log](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/evidence/trust-anchor-probe.log)

## 8. Qualification-record association

**QUALIFICATION-RECORD ASSOCIATION ACCEPTED WITH QUALIFICATIONS.** Q1 supports all nine B6-T20c cases through deterministic tests and source inspection. The verifier validates a bounded, case-sensitive single-component ID before lookup. It reads only the selected record and compares its exact bytes with the receipt hash. [Independent matrix](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/QUALIFICATION-RECORD-REVIEW.md)

| Case | Supported result |
|---|---|
| 1. Fresh installation, no qualification | Installed/Unqualified. The receipt has no selected ID or hash. |
| 2. Missing selected record | Unqualified. |
| 3. Selected record hash mismatch | Unqualified. |
| 4. Multiple records, one explicit selection | Only the receipt-selected name is evaluated. |
| 5. Stale or superseded record | A different ID cannot stand in for the selected record. No fallback. |
| 6. Wrong artifact digest | Unqualified. |
| 7. Wrong RID or profile | Unqualified. |
| 8. Unaccepted producer-only evidence | Unqualified. A report cannot approve itself. |
| 9. Conflicting qualification metadata | Unqualified, including conflicting build configuration. |

Actual trusted administrative acceptance and a native positive result are separate gates.

## 9. Native filesystem verification

**NON-ROOT NATIVE FILESYSTEM VERIFICATION SUPPORTED.** Q1 reproduced 1/1 WSL2 test with an intact positive control and refusals for symlinks, symlink ancestors, hardlinks, FIFOs/special files, sparse files, missing or unexpected entries, wrong modes and mutation. Descriptor-relative traversal, pinned descriptors, bounded reads and selector rechecks support the exact-generation claim. Q1 inspected the producer’s two ptrace races without rerunning them. Privileged replacement, kernel compromise and filesystem snapshot inconsistency remain residual risks. [Filesystem review](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/FILESYSTEM-VERIFICATION-REVIEW.md)

## 10. Installation transaction and retention

**INSTALLATION TRANSACTION AND CONSERVATIVE RETENTION IMPLEMENTATION ACCEPTED WITH QUALIFICATIONS.** Q1 reports 11/11 non-root transaction tests. The installer verifies the bounded copy, syncs files and directories, publishes the staged generation with `RENAME_NOREPLACE`, syncs the installations directory, then atomically publishes and syncs `active` last. Interruptions can leave a retained generation or unselected record. Readers refuse an incomplete selected generation. Root-executed crash durability remains unqualified. Removal fails closed under every current holder state. [Transaction review](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/INSTALLATION-TRANSACTION-REVIEW.md) · [Retention review](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/GENERATION-RETENTION-REVIEW.md)

## 11. Finding dispositions and producer addendum

| Q1 finding | Original class | Owner disposition |
|---|---|---|
| F01 Archive ownership | MINOR | **Non-blocking documentation correction.** This addendum qualifies the producer’s “Exact candidate” section at the evidence commit in §2. The archived artifact files have build-user ownership `1000:1000`. The manifest’s `0:0` specifies installed ownership. A root installer creates new files with the specified modes, and the trusted verifier checks ownership. Actual qualified ownership still needs a real installed-generation run. The archived bytes remain the reviewed candidate. |
| F02 Additional acceptance store | MINOR | **B. Bounded design delta.** The verifier keeps the accepted authority. A second protected namespace governs publication, with consequences for provisioning and revocation. Focused owner decision required under §12. |
| F03 Vestigial holder argument | MINOR | **Accepted with bounded cleanup deferred.** `remove_generation(digest)` always passes `Unknown` to `retention_refusal`. Its holder argument grants no authority. Even the helper’s `None` case refuses without independent cleanup proof. Future removal needs an explicit live-holder, ownership and cleanup evidence contract. |
| F04 WSL2 root permissions | QUALIFICATION | **Native root-owned installation qualification pending.** Preserve the refusal and the ancestor predicate. See §14. |
| F05 Later-stage guarantees | QUALIFICATION | **Deferred to the appropriate B6 implementation and native qualification stages.** See §15 for the separate gates. |

[Original reviewer findings](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/FINDINGS.md)

## 12. Focused authority audit of /etc/gagamba

**B. Bounded design delta.** Design §§4.5 and 7.3 require a separate trusted owner/operator acceptance act and a protected, receipt-selected record under `/opt/gagamba`. They do not specify a second persistent authorization store, its writer, or its lifecycle. The producer documents that store in its “Operator boundary” section. The following conclusions come from the reviewed code at the implementation revision in §2. These are source observations, including the failure behavior. Q1 never provisioned the stores as root. [Operator transaction source](../../eng/linux-m1/install_common.py) · [Fixed verifier](../../src/Gagamba.Execution.Linux.M1/Installed/InstalledFileSystem.cs) · [Receipt and record checks](../../src/Gagamba.Execution.Linux.M1/Installed/InstalledMetadata.cs)

| Question | Observed behavior |
|---|---|
| What is under `/etc` and who writes it? | `/etc/gagamba/accepted-qualifications/<id>.json` is a preauthorization for one qualification publication. No reviewed installer, builder or verifier creates it. An explicitly authorized administrator must independently provision it from an accepted review decision. S2 supplies no automated decision writer. |
| Who reads it and what selects it? | The fixed `qualify-install.sh` invokes installed `install_common.py qualify`. After enforcing uid 0, the WSL2 x86-64 host and the installation lock, `qualify` reads the active pointer and proposed record. `_acceptance` reads the fixed `DECISIONS` directory. A caller may supply the proposed record path. Its ID selects one filename only after validation as 1–128 ASCII letters, digits, dot, underscore or hyphen with no leading/trailing dot. No CLI or environment setting chooses the decision directory. |
| What is independently bound? | The decision must have exactly `qualificationId`, `artifactSetSha256`, `qualificationRecordSha256` and Boolean `accepted: true`. Duplicate/extra keys refuse. The ID must match the proposed record, the artifact digest must match root’s active generation, and the record hash must equal SHA-256 of the proposed record’s actual bytes. The administrator supplies the independently accepted expected values. Producer metadata alone cannot establish them. |
| How are paths protected? | `_open_trusted_dir` walks from `/` through `/etc`, `gagamba` and `accepted-qualifications` by directory FD with no-follow. Every component must be a `0:0` directory with no group/other write bits. The code checks these predicates when reading. Presence of a conforming store on this host was never demonstrated. `_trusted_file` requires a `0:0`, exactly `0444`, single-link regular file of at most 256 KiB. It pins with `O_PATH and O_NOFOLLOW`, reopens the pinned inode, and checks identity and stability through the read. |
| What does the operator publish? | `qualify` checks staged contents, platform and receipt, refuses rebinding to a different existing decision, and checks the proposed association. It publishes exact record bytes at `/opt/gagamba/qualifications/<id>.json` with no-replace, permits an identical collision, fsyncs the directory, and binds the receipt by rename last. That receipt selects by ID and record hash. |
| What does the verifier trust? | Argument-free `VerifyActive` reads only the fixed installed hierarchy for this association. It checks the root and descendants, active generation, exact artifacts, manifest, receipt, selected `0:0 0444` record, identity fields, host and selector stability. It never reads `/etc/gagamba`. The extra file introduces a trusted **publication input** under the existing root principal. It adds neither a runtime trust principal nor an alternate route to Qualified. |
| What if the decision is missing or replaced? | Before publication, or on retry, an absent, unsafe, malformed, false or mismatched `/etc` decision refuses the operation. Replacement with exactly the accepted bindings can pass the gate. After successful publication, removal or alteration of `/etc` alone has no effect on `VerifyActive`. It cannot serve as runtime revocation. |
| What if the installed record is missing or replaced? | A fresh receipt remains Installed/Unqualified. A bound receipt selecting a missing, unsafe, wrong-hash or mismatched `/opt` record fails verification, even if the `/etc` decision remains valid. Other IDs cannot substitute. Identical correctly protected record bytes can verify again. A root administrator able to rewrite the whole installed closure remains within the design’s trusted-admin assumption. |

**Approval still required.** The extra gate preserves the intended association and anti-self-approval guarantees. It extends the operator contract with a required second path and a one-time decision lifetime. The accepted design gives no operator a rule for provisioning, superseding or withdrawing that file. A focused amendment should either approve the precise writer, independent source of the accepted hashes, retry/collision behavior and post-publication withdrawal semantics, or commission removal of the extra store while preserving independent approval. Review both phases with missing and substituted records. Accepting reviewer evidence here does not approve that design change. [Accepted design](../design/rc2b-06-b6-installed-artifact-trust.md) · [Q1 trust review](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/TRUST-ANCHOR-REVIEW.md)

## 13. Regression preservation

**S1 PRODUCTION/TEST SEPARATION PRESERVED.** S1 remains accepted with qualifications. The producer ran 51/51 build probes. Q1 reproduced 24/24 positive gates and 8/8 representative negative gates covering GAGAMBA001/002/003/004/006/008. Windows PowerShell 5.1 encoding interrupted the combined script at the first negative case. Q1 captured the negative diagnostics directly. Remaining S1 gates were inspected.

**B1–B5 LIFECYCLE SAFETY PRESERVED WITH EXISTING QUALIFICATIONS.** Windows M1 Debug returned 1,141 passed, 0 failed, 270 skipped, comprising 1,113 producer-comparable passes and 28 new reviewer cases. Owner-only physical RELEASE, one physical attempt, stop-only recovery, original deadlines, controller-loss stopping, protected capture, durable journal and conservative cleanup/Unknown remain in force. The production factory remains unavailable. Environmental skips and unexecuted native lifecycle scenarios remain open. [Reviewer regression accounting](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/REGRESSION-REVIEW.md)

## 14. Remaining native qualification gates

Q1 observed the real WSL2 ext4 root on `/dev/sdf`, owned by `root:root` with mode `0777`. The verifier correctly refused that ancestor before reaching the absent installation. HD-1 requires a separately authorized run on a controlled disposable WSL2 x86-64 host whose entire ancestor chain already passes. Keep the existing refusal, including for a writable sticky root. The trusted-install positive control, actual Qualified state, root-executed durability and full native runtime/loader closure are outstanding. Native Linux-host qualification remains deferred. [Native limits](../reviews/rc2b-06-b6-impl-01-s2-q1-evidence/NATIVE-QUALIFICATION-LIMITS.md)

## 15. Deferred integration and handoff

Resolve F02 first. After the focused decision and S2 owner disposition, separately commission **S3 installation-identity propagation**. Derive the expected identity from S2’s qualified installed-generation result. Preserve the accepted RecoveryConfigV2 bootstrap, recovery messages 9/10 and owner message 36, role/direction, bounds, sequence, legacy v1 behavior and stop authority. Test missing, mismatched, stale and replayed echoes before granting readiness. WI-01–WI-05 and B6-T20b define the wire scope. Production binaries and both process topologies remain subject to WI-06.

Track passive descriptor v2, pre-RELEASE active-generation enforcement, pinned execution with a cleared environment, assembly location, ICU/OpenSSL/zlib/native loader closure and holder-aware removal explicitly. Keep privileged installation qualification and any future production activation separately authorized. HD-2 limits the descriptor to passive reporting, HD-3 limits experimental native scope to WSL2 x86-64, HD-4 retains immutable generations without automatic GC and HD-5 requires compatibility evidence for invariant globalization. [Wire obligations](rc2b-06-b6-wire-owner-acceptance.md) · [Qualification plan](../plans/rc2b-06-b6-qualification.md)

## 16. Authorization boundary

This commission covers local documentation, preservation and verification. Source, tests, build logic, protocol changes, S3 implementation, host permission changes, privileged installation or qualification, restricted-execution activation, publication, worktree changes and push require separate authority.

## 17. Owner disposition

**S2 OWNER ACCEPTANCE PENDING FOCUSED TRUST-STORE DECISION.** Evidence integrity and the bounded technical dispositions above stand. The next owner action is the §12 decision on the qualification publication policy. S3 can be commissioned after that decision resolves F02 and formal S2 acceptance is recorded.
