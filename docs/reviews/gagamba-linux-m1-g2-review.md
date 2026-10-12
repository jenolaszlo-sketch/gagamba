# Gagamba Linux M1 G2 independent review (preserved)

- review type: independent G2 architecture/implementation review (before WP04)
- reviewed revision: 1e117e0a64ee473f2ba251818761afc315c8e283
- branch: qualification/ar7-feasibility
- disposition: G2 NOT ACCEPTED - CORRECTION REQUIRED
- blocking findings: G2-01 through G2-10
- nonblocking: G2-11 (evidence correction/maintenance)
- blocking owners:
  - WP02: G2-06, G2-07
  - WP03: G2-01, G2-02, G2-03, G2-04, G2-05, G2-08, G2-09, G2-10
- WP04 remains blocked; no remediation has begun.

# Gagamba Linux M1 — independent G2 review

Date: 2026-10-12 (Asia/Manila)  
Reviewer: Codex, independent review in this chat  
Branch: qualification/ar7-feasibility  
**Exact reviewed revision: 1e117e0a64ee473f2ba251818761afc315c8e283**

All source conclusions and line references below refer to that frozen revision. Links point to its separate, unchanged source export. The original repository was clean at the start and end and still resolved to the exact SHA. No implementation fixes, repository edits, WP04 work, Bubblewrap execution, coordinator execution, or target execution occurred. Reviewer probes and results are outside the repository.

## Overall assessment

**WP02 + WP03 do not satisfy G2.** The copy/binding primitives and the broad primary/recovery-lease design are sound starting points, but publication is disconnected from retained object ownership; allocation recovery is not incrementally composed; journal durability and fencing fail decisive probes; descriptor transfer is insufficiently validated; and recovery can delete replacement staging objects or falsely confirm cleanup.

The selected future WP04 design remains plausible. These findings require corrections within WP02/WP03, not a new launcher architecture or premature WP04 runtime evidence.

## Independently executed evidence

Host: Ubuntu 26.04 WSL2, x64, kernel 6.6.114.1-microsoft-standard-WSL2, glibc 2.43, .NET SDK 10.0.112. Runs used **uid 0**. This is root-native evidence, not unprivileged delegated qualification. LSM status was not established.

The exact Git archive was built in /tmp/g2-review-1e117e0. Owner and test projects were built from that export. SourceLink emitted warnings because the export has no Git metadata; this review therefore does not independently reproduce the author's zero-warning solution-build claim.

| Reviewer run | Result |
|---|---|
| Initial complete M1 suite | **185 passed, 1 failed, 0 skipped** |
| Focused Preparation filter | **75 passed, 0 failed/skipped**; includes preparation identity and four ownership-composition tests |
| Focused Recovery filter | **56 passed, 0 failed/skipped** |
| Complete suite repeat, unchanged source | **186 passed, 0 failed/skipped** |
| Bounded reviewer probes | Reproduced the failures detailed below |

The initial failure was NativeCopyInTests.SparseBelowBudgetStillRejected: FileStream.SetLength threw “Stream does not support seeking” while creating the test input. The isolated preparation group and full repeat passed. The independently reproduced double-close in G2-01 is consistent with concurrent FD corruption, but this review does **not** claim to have proved it caused that particular failure.

The full passing repeat covers 70 WP01 portable, 38 WP02 portable, 22 WP02 native, 42 WP03 portable and 14 WP03 native tests. RecoveryDuplicateIsIndependent is a **portable fake-backend** test, not an additional native test.

The fixture was built, copied, hashed and bound, never executed. Its SHA-256 reproduced the author's value:
ee25a876bad5312afcece19821668b91db0c09f5a2f1aaac74f00c45806e2305.

No timeout rescue was needed. End checks found no review-owned gagamba-wp03-* or g2-review-* cgroups, no probe temporary allocations, and no surviving owner helper. Pre-existing unrelated cgroups were left untouched. Source/build exports and review logs are intentionally retained as review artifacts.

Results: [initial suite](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/g2-m1.trx), [preparation](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/g2-preparation.trx), [recovery](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/g2-recovery.trx), [suite repeat](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/g2-m1-repeat.trx), [probe output](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/probes.txt), [probe source](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/probes/Program.cs).

Reproduction entry points were the supplied build-native.sh, dotnet build of the owner project, and dotnet test of the M1 test project, with the Preparation/Recovery filters above. Each test invocation had an external 90–180 second timeout. The reviewer probe project references the unchanged compiled M1 assemblies and uses the existing friend-assembly boundary. Its directory-fsync failure is explicitly injected after real native create/write/file-fsync/rename operations; it is not a claimed physical-storage failure.

## Blocking findings

### G2-01 — P1 / WP03: the scope double-closes its delegated-parent descriptor

Locations: [CgroupInterop.cs:474](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Native/CgroupInterop.cs:474), [M1RecoveryScope.cs:130](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs:130), [M1RecoveryScope.cs:202](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs:202).

TryCreateDomain puts the scope's original parentFd into an owning backend. AllocateDomain disposes that backend after transfer, closing the descriptor, but leaves the same integer in scope._parentFd. Scope disposal closes it again. If reused, this closes an unrelated live FD; a later scope allocation could also use reused authority.

**Native reproduction:** allocation succeeded, the saved scope-parent FD was already invalid, a /dev/null sentinel was placed in that free slot, and scope.Dispose changed its F_GETFD result from 1 to -1. Probe output: SCOPE_DOUBLE_CLOSE sentinel=72 before=1 after=-1.

**Minimum correction:** make parent ownership explicit with an independently owned duplicate or a true ownership transfer that invalidates the previous holder. Cover success and failure unwind paths, including the send-failure parentEnd double-close. Verify allocation, reuse and disposal cannot close another object's FD.

### G2-02 — P1 / WP03: SCM_RIGHTS reception does not enforce the promised protocol or CLOEXEC

Locations: [FdPassing.cs:139](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Native/FdPassing.cs:139), [CgroupInterop.cs:62](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Native/CgroupInterop.cs:62), [owner Program.cs:74](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1.Owner/Program.cs:74), [OwnerBootstrap.cs:43](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Native/OwnerBootstrap.cs:43).

recvmsg uses flags 0, ignores truncation flags, and does not validate the one-byte payload/version. It accepts a variable count up to seven; the subsequent backend checks only array length. No received role/type/access-mode/control-membership validation exists. The owner derives parent/child identities from whatever it received rather than comparing a complete expected identity. The locally constructed BootstrapMap is not validation of the incoming SCM_RIGHTS payload; it also does not reject one numeric FD assigned to several roles. The receiving bootstrap socket is never closed by the owner.

**Native reproduction:** sending eight FDs was accepted as seven; all received descriptors had FD_CLOEXEC=0. Seven transfers of the same /dev/null object were accepted by FromOrderedFds as a live backend.

**Minimum correction:** implement and validate the fixed version/count/ordered-role contract, reject truncated/extra/missing/malformed/aliased authority, validate descriptors against the expected domain and roles, receive with atomic CLOEXEC, close the bootstrap endpoint after handoff, and dispose every received FD on rejection. Add native malformed-transfer and exec-inheritance tests. Do not transfer release authority.

### G2-03 — P1 / WP03: the real allocation/copy path does not implement incremental recoverability

Locations: [M1RecoveryScope.cs:78](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs:78), [PreparationOwnershipCoordinator.cs:62](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Lifecycle/PreparationOwnershipCoordinator.cs:62), [PreparationOwnershipCoordinator.cs:108](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Lifecycle/PreparationOwnershipCoordinator.cs:108), [OwnershipRecord.cs:43](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Recovery/OwnershipRecord.cs:43).

AllocateDomain creates and transfers a domain without any journal dependency or durable AllocationIntent. The native StartOwner path invokes it that way. The coordinator assumes the domain already exists, writes staging intent, runs **all** SafeCopyIn copying/binding, and only then invokes the recovery sink and writes AllocationRegistered/Prepared.

There is no native implementation of ICleanupAuthoritySink; only a test fake implements it. A copy failure returns before any recovery retention/registration, including when a bound partial closure exists. Staging entries store names/length/digest, not staging parent/child/object dev/inode identities, and the domain record fields are copied from the supplied template. This leaves real creation/copy-to-registration death windows without the selected surviving-scope authority.

**Reproduction/inspection:** native AllocateDomain succeeded without a journal. OwnerDeathBeforeRegisteredRecoversWithoutCapability constructs a synthetic record after the owner exits; it does not read a pre-allocation intent. The staged cleanup test performs copy first and creates a record afterward, in the same process.

**Minimum correction:** compose durable intent before actual domain/staging creation; retain allocator/recovery parent and child authority at creation; capture actual identities and durably register them before dependent copying/publication; retain debt on every failure. Implement the real scope acknowledgement/cleanup sink. Reproduce owner death during actual staging and around registration/acknowledgement, with cleanup through authority that already survived the death.

### G2-04 — P1 / WP03: a failed directory fsync loses the previous complete snapshot

Locations: [OwnershipJournal.cs:118](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Recovery/OwnershipJournal.cs:118), [JournalFileStorage.cs:83](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Native/JournalFileStorage.cs:83), [JournalTests.cs:64](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/tests/Gagamba.Execution.Linux.M1.Tests/Recovery/JournalTests.cs:64).

Rename replaces the only old snapshot before directory fsync. If that fsync fails, Write returns failure but the old complete snapshot is already inaccessible. ReadLatest then accepts the replacement as latest even though its required durability step failed. The previous-snapshot test injects only rename failure, not this boundary.

**Native-storage reproduction with injected final fsync failure:** durable inode=200 was replaced by inode=300; Write returned false, LastSequence remained 1, and ReadLatest returned inode=300.

**Minimum correction:** preserve the last acknowledged durable generation until replacement durability succeeds, within the selected bounded storage budget. Define and test how uncertain final-sync state is represented; never equate a readable replacement or in-memory state with acknowledged durability. Test interruption/failure at every replacement boundary.

### G2-05 — P1 / WP03: flock is advisory to the write path, and writer handoff resets sequence

Locations: [OwnershipJournal.cs:86](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Recovery/OwnershipJournal.cs:86), [OwnershipJournal.cs:136](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Recovery/OwnershipJournal.cs:136), [owner Program.cs:120](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1.Owner/Program.cs:120), [RecoveryHarness.cs:535](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/tests/Gagamba.Execution.Linux.M1.Tests/Recovery/RecoveryHarness.cs:535).

Write/WriteNext do not require locally held writer authority. No real scope bootstrap writer/OwnerAccepted acknowledgement transition governs the owner's acquisition. A new OwnershipJournal starts its sequence at zero; the owner does not read the previous snapshot before WriteNext. Write also accepts regressing sequence values.

**Cross-process native reproduction:** while the parent held flock, a child failed lock acquisition but successfully wrote a new snapshot through OwnershipJournal. A fresh writer overwrote an existing sequence-2 snapshot with sequence 1.

The author's NativeWriterLockIsExclusiveAcrossProcesses test actually creates two storage instances in **one process**. The death-takeover test proves acquisition after owner exit but never challenges it while that owner remains alive.

**Minimum correction:** require an owned writer lease for every mutation; implement scope bootstrap → acknowledged owner transfer → death-proven, locked recovery takeover; initialize/validate monotonic sequence under that fence. Prove a live owner excludes a recovery writer while still permitting urgent domain stop through the existing lease.

### G2-06 — P1 / WP02: the published capability does not own or bind the prepared closure

Locations: [PreparedClosure.cs:214](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Preparation/PreparedClosure.cs:214), [PreparedTransaction.cs:17](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Preparation/PreparedTransaction.cs:17), [PreparationStore.cs:76](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Preparation/PreparationStore.cs:76).

Publication calls Mint with only manifest, identity and expiry. PreparedClosure is returned alongside the capability; neither the capability nor store retains its handles. Discarding or disposing the closure does not invalidate the capability. Thus the selected PrepareHeldAsync(PreparedTransaction, DomainLease, ...) boundary cannot obtain the exact copied FDs from its reference-owned preparation.

**Reproduction using real copied bytes:** publish → DiscardAccounted → TryLookup and TryConsume both returned true while the copied-object handles were closed. Output: CAPABILITY_AFTER_CLOSURE_DISCARD published=True lookup=True consume=True.

**Minimum correction:** atomically associate and own the exact verified closure with the private capability/store entry; tie discard/disposal/invalidation to that lifecycle; expose a controlled internal borrow for launch. Keep identity records non-authoritative. This is a missing WP02 completion obligation, not reopening G1's accepted value-only skeleton.

### G2-07 — P1 / WP02: accounted staging cleanup deletes replacements and discards errors

Locations: [PreparedClosure.cs:97](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Preparation/PreparedClosure.cs:97), [SafeCopyIn.cs:204](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Preparation/SafeCopyIn.cs:204). WP03's native example repeats the pattern at [RecoveryHarness.cs:564](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/tests/Gagamba.Execution.Linux.M1.Tests/Recovery/RecoveryHarness.cs:564).

DiscardAccounted unlinks each recorded name under the retained directory without comparing the current entry to the retained object's dev/inode. It ignores unlink results and then disposes the authority. FinishFailed similarly ignores partial unlink failures. A retained parent bounds the directory, but does not prove a particular child is still the allocated object.

**Native reproduction:** copy/bind a tool, replace its staging entry with a distinct file, call DiscardAccounted; the replacement was deleted.

**Minimum correction:** compare each entry with retained original object identity under exclusive cleanup ownership; refuse replacements, preserve them and report Unknown/debt. Propagate cleanup failures and retain retry authority instead of silently dropping it. Apply the same checked operation in WP03's real staging cleaner.

### G2-08 — P1 / WP03: reconciliation can confirm cleanup without preparation cleanup authority

Location: [Reconciler.cs:133](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs:133).

HasCleanupAuthority is never consulted. If cleaner is null, all recorded staging allocations are skipped; a stopped/removed domain still yields CleanupConfirmed. There is no native production staging cleaner. Repeated reconciliation also has no persisted completed-component handling; the fake backend's removal does not model a real removed cgroup.

**Portable reproduction:** a record containing a staging allocation, HasCleanupAuthority=false, and cleaner=null returned CleanupConfirmed.

**Minimum correction:** derive per-component cleanup results from live authority and actual outcomes. Missing authority for a recorded allocation must remain Unknown/debt. Implement retained-identity staging cleanup and native idempotence, without improving unknown execution results.

### G2-09 — P1 / WP03: domain identity corroboration and mount binding are incomplete

Locations: [RestrictedDomain.cs:79](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Lifecycle/RestrictedDomain.cs:79), [CgroupInterop.cs:273](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Native/CgroupInterop.cs:273), [CgroupInterop.cs:356](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Native/CgroupInterop.cs:356), [Reconciler.cs:48](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs:48).

Corroborates omits ChildDevice and ChildInode despite its complete-identity claim. Native mount discovery selects a cgroup2 line from mountinfo, but no statx mount ID/filesystem validation binds that line to the retained parent/child descriptors. The allocator accepts supplied mount metadata. Resource checks compare only devices; kill/events have no corresponding identity verification. Reconciliation trusts caller-supplied match booleans rather than deriving a record-versus-live-lease match.

**Portable reproduction:** changing both child device and inode still returned Corroborates=true. Existing native replacement-removal tests do pass because RemoveIfIdentityMatches separately checks dev/inode; that does not repair the other boundaries.

**Minimum correction:** derive the mount identity from the actual retained objects, validate control roles/membership, compare every identity component including child dev/inode, and derive recovery eligibility from the real lease/record/boot context. Keep mismatch and all-handle-loss paths non-destructive.

### G2-10 — P1 / WP03: selected unprivileged, pidfd-backed recovery topology is not qualified

Locations: [M1RecoveryScope.cs:134](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs:134), [RecoveryHarness.cs:22](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/tests/Gagamba.Execution.Linux.M1.Tests/Recovery/RecoveryHarness.cs:22), [wp03.md:15](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/docs/evidence/linux-m1/wp03.md:15), plan Sections 10.1, WP03 and G2.

The scope retains a Process/direct-child relationship but has no explicit pidfd acquisition, retention or missing-pidfd refusal. Direct-child wait is useful observation, but does not demonstrate the selected retained-pidfd contract. The harness hardcodes /sys/fs/cgroup and creates its own parents there; author and reviewer native runs used root. It cannot currently select an ordinary pre-delegated non-root parent. Native cases do not run owner-side staging or the actual bootstrap writer/acknowledgement transfer.

**Minimum required proof:** on an already delegated non-root host, run a dedicated scope with recorded UID/capabilities, verified subreaper and retained owner pidfd; demonstrate primary/duplicate authority, live-owner writer exclusion followed by death-proven takeover, and actual staging-owner death around registration/acknowledgement. No host provisioning or target execution is required by this correction.

Finite /bin/sleep is appropriate for the orphan test: it checks /proc ppid equals the designated scope PID and checks waitpid returns the orphan PID. That proves actual adoption and reap, and was independently rerun successfully. Exhaustive later-state fault coverage remains deferred; these concrete WP03 topology/composition gaps do not.

## Evidence correction

**G2-11 — P2 / PLAN/G2, nonblocking by itself.**

Locations: [wp03.md:7](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/docs/evidence/linux-m1/wp03.md:7), [wp03.md:111](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/docs/evidence/linux-m1/wp03.md:111), [wp03.md:153](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/docs/evidence/linux-m1/wp03.md:153), [aggregate evidence:16](C:/Users/Laszlos/source/repos/Solo/review/g2-1e117e0/source/docs/evidence/gagamba-linux-m1.md:16).

The aggregate index retains an obsolete WP02 123/123 count and both “WP03 not started” and “WP03 implemented” rows. WP02/WP03 final-SHA headers tell readers to resolve moving git log output rather than containing a durable exact binding. WP03 overstates previous-snapshot survival, cross-process fencing, control membership and composed allocation durability.

Minimum correction: bind evidence to the tested source revision, correct the aggregate rows/counts, record actual commands/results and privilege inventory, and narrow claims to their real assertions. Handoff/work queue accurately preserve “G2 pending / WP04 not begun,” but their completion language needs reconciliation with this review. These documentation issues do not independently expand the blocking set beyond G2-01–G2-10.

## WP02 binding assessment

Source acquisition is relative to retained, distinct trusted source-root and staging-directory authorities. It uses openat2 with BENEATH, NO_SYMLINKS and NO_MAGICLINKS, plus NOFOLLOW/CLOEXEC. The raw-syscall route remains an openat2 route; there is no path-walk fallback. Symlink, magic-link and root-escape refusals were independently rerun.

Objects are streamed into exclusive private files, with pre-write byte accounting, source growth/truncation checks and sparse-accounting refusal. No hardlink/reflink copying is used. Writable object FDs close before the initial read-only reopen. Binding compares dev/inode, type, size, mode, link count and density, then completely rehashes the copied destination. File and staging-directory fsync precede publication.

The read-only reopen is pre-binding. No post-binding object-path reacquisition substitutes for a retained FD; replacement cannot switch the already retained object. Sampling covered replacement, hardlink-source independence, growth/truncation, sparse refusal, exclusive staging and no writable object FD at publication.

Publication performs manifest matching, acknowledgement, cancellation recheck, then Mint; acknowledgement failure prevents publication. These strengths do not cure the capability/closure lifetime gap or cleanup defects in G2-06/G2-07. WP03 left WP02 copy call sites unchanged, but introduced the cross-process lifetime and recovery failures above.

## Durability and domain authority assessment

The codec encodes a versioned, checksummed snapshot, checks its 64 KiB limit and caps allocation entries at 128. It contains evidence/state rather than executable instructions, release credentials or a serializable PreparedTransaction. Native writes use exclusive temporary creation, complete write, file fsync, rename and directory fsync. These operations alone do not establish the promised replacement durability/fencing/monotonicity; see G2-03–G2-05. The native reader also uses File.ReadAllBytes before the codec's size check, so bounded reading should be included in the journal correction.

Whole-domain stop writes 1 through the retained cgroup.kill FD and polls retained cgroup.events for populated=0 with a finite allowance. No PID enumeration, journal-PID kill, failed-handle-to-path reopen, or same-name adoption fallback was found in this stop path. Owner PID/direct-child observation is separate.

Owner primary descriptors and scope duplicates exist and can stop independently in the successful native cases. Duplicate handles use F_DUPFD_CLOEXEC. Records alone have no API that reconstructs a DomainLease, preparation capability or release authority. Portable all-handle-loss/cross-boot/mismatch refusal cases pass. However, G2-01/G2-02/G2-09 prevent accepting the complete live-authority boundary, and G2-07/G2-08 prevent accepting destructive staging recovery.

## WP04 compatibility assessment

**Atomic admission:** the retained primary backend includes the original domain directory FD. A narrowly scoped lifetime-safe borrow/duplicate can feed the selected adapter-specific posix_spawn cgroup attribute. No post-start migration, cgroup pathname reopen, PID authority, exposure of controls to Bubblewrap, loss of recovery duplicates, or change to OwnedCgroup.Adopt is architecturally necessary. The missing future adapter accessor is not itself a G2 blocker.

**Fixed FD map:** the selected 0–11 layout and temporary relocation to unique FDs ≥64 followed by deterministic dups and closefrom(12) can accommodate the required objects and channels. Domain controls, recovery duplicates, staging directories, journal/lock FDs, bootstrap socket, liveness channels, RELEASE writer and output readers must remain outside that map. The current transfer's non-CLOEXEC descriptors/socket must be fixed under WP03; future closefrom actions are not evidence that today's bootstrap is correct.

**Prepared objects:** tool/input RO FDs already exist in PreparedClosure, but G2-06 must bind them to the private capability accepted by the planned interface. Coordinator/helper/config/filter construction is appropriately future WP04 work.

**Coordinator identity:** current PreparationIdentity supplies execution UUID, preparation UUID, manifest SHA-256 and generation UUID; PreparedTransaction supplies expiry, and the plan supplies the shared CLOCK_MONOTONIC absolute-deadline convention. These values can populate the selected bounded protocol without serializing the capability or giving recovery release authority. Later deadline wiring/native protocol behavior is not graded here.

## End-to-end authority trace

| Boundary | Actual authority / owner | Durable evidence and owner-loss consequence |
|---|---|---|
| Trusted sources → copy | Open source-root and source FDs, copying owner | Names/digests are evidence; source FDs do not freeze caller bytes. Destination verification makes copied bytes authoritative. |
| Copy → bound closure | RO object FDs and private staging-dir handle, PreparedClosure | File/directory sync exists. Incremental staging registration and independent recovery retention are incomplete (G2-03). |
| Bound closure → Prepared | Private reference-owned store capability; closure currently separate | Prepared precedes Mint in the coordinator's successful path, but capability ownership does not include FDs (G2-06). |
| Scope → owner domain | SCM_RIGHTS primary parent/domain/control descriptors | Owner records actual received stat values; validated bootstrap/identity/fencing is incomplete (G2-02/G2-05/G2-09). |
| Owner → surviving recovery | Separately owned duplicate domain handles | Scope can stop after owner exit. Staging cleanup authority is not equivalently integrated; parent lifetime is broken (G2-01/G2-03). |
| Domain → future helper | Retained domain-dir FD via spawn attribute | Future admission evidence; no implementation/execution claimed. |
| Prepared → future held coordinator | Selected RO objects, sealed config/filter, private pipes | UUID/hash/deadline bind protocol messages; only the live owner's capability and RELEASE writer can authorize. Future work. |
| All live holders lost | No authority remains | Records must yield Unknown/quarantine, never pathname reacquisition, capability resurrection, release reconstruction or resumed work. |

## Evidence classification and scope

- **Reviewer-run native:** all 22 WP02 and 14 WP03 native tests on the successful complete repeat; primary lease stop, before/after registration owner exit, takeover after exit, subreaper verification, actual orphan adoption/reap, same-name removal refusal, staging example, native journal operations, plus reviewer FD/fencing/cleanup probes. The staged example proves same-process retained-dirfd unlink, not staging-owner crash recovery.
- **Author evidence inspected but not independently rerun:** Windows full-solution results and zero-warning/error solution build. Historical WP02 123/130 and WP03 183 totals remain historical; final Linux 186 was rerun with the initial failure disclosed above.
- **Portable-only:** detailed injected copy/journal faults, cross-boot and all-handle-loss decisions, lease duplicate fake, integrated acknowledgement/publication order, cleanup-refusal model and state semantics. The reviewer no-cleaner and changed-child-corroboration probes are also portable.
- **Future WPs:** Bubblewrap/coordinator runtime, atomic helper admission, fixed-map native leak audits, actual RELEASE protocol, resources/output, target execution and later exhaustive owner-loss matrices.

Not every possible intent-to-create instruction needs a separate native kill at G2. But the actual allocation/staging/acknowledgement path must first exist and have decisive native death coverage. Its absence is G2-03/G2-10, not an acceptable relabeling of portable evidence. The finite trusted orphan process is acceptable. The “abandoned publication” staged test is useful but insufficient for the selected owner-death cleanup claim.

The implementation diff is confined to internal M1 code, owner/test/native-fixture support, solution/ignore entries and evidence/handoff/queue. No production provider registration, public API expansion, Hufu/workflow coupling, arbitrary target support, export, daemon/service installation, privilege-elevation mechanism, Bubblewrap/coordinator/target execution, release implementation, or Windows/macOS restricted backend was introduced. The native harness writes controllers in its disposable parent under root; that is not evidence of unprivileged pre-existing delegation. OwnedCgroup.Adopt, ADR 0003 and sealed AR-7 evidence were unchanged. G1 accepted semantics were not reopened.

## Final disposition

Blocking findings: **WP02 — G2-06, G2-07. WP03 — G2-01, G2-02, G2-03, G2-04, G2-05, G2-08, G2-09, G2-10.** G2-11 is evidence maintenance, not a separate runtime blocker. No WP04 runtime behavior was required or implemented.

G2 NOT ACCEPTED - CORRECTION REQUIRED

