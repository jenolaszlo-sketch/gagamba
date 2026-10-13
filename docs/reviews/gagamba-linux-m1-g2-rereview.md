# Gagamba Linux M1 G2 rereview (preserved)

- review type: independent G2 rereview (before WP04)
- implementation under test: 121c644f2aa774119149f598a935a181fc71e137
- evidence-only commit at review time: caab2d326b1e15f2129a804c01aa28da13b2a7a8
- branch: qualification/ar7-feasibility
- disposition: G2 NOT ACCEPTED - CORRECTION REQUIRED
- resolved: G2-01, G2-07, G2-09
- still blocking: G2-02, G2-03, G2-04, G2-05, G2-06, G2-08, G2-10
- nonblocking: G2-11 (evidence correction/maintenance)
- source: C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/G2-rereview-report.md
- WP04 remains blocked; no correction has begun.
- The body below is the report verbatim.

# Gagamba Linux M1 — independent G2 rereview

Date: 2026-10-13, Asia/Manila. Review-only continuation of Astra's partial independent work.

**G2 is not accepted.** The five partial findings reproduce against the frozen implementation. G2-01, G2-07 and G2-09 are resolved within their reviewed boundaries. G2-02, G2-03, G2-04, G2-05, G2-06, G2-08 and G2-10 remain blocking. G2-11 still requires evidence correction; it remains nonblocking documentation maintenance. WP04 must remain blocked.

## Target and integrity

| Item | Verified value |
|---|---|
| Implementation under test | `121c644f2aa774119149f598a935a181fc71e137` |
| Evidence-only commit | `caab2d326b1e15f2129a804c01aa28da13b2a7a8` |
| Branch | `qualification/ar7-feasibility` |
| Original rejected revision | `1e117e0a64ee473f2ba251818761afc315c8e283` |
| Original review | `docs/reviews/gagamba-linux-m1-g2-review.md` |
| Frozen source export | `C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source` |
| Evidence export | `C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/evidence/docs` |

The diff from the implementation to the evidence commit changes exactly five documentation files: the aggregate evidence index, WP02 evidence, WP03 evidence, handoff and work queue. It changes no source or test behavior. The review target is cleanly bound. The original review file is unchanged between its verbatim backfill commit `d3c0700` and `caab2d3`.

The Gagamba repository was clean on inspection and remained clean at completion, with HEAD at the evidence commit. Builds, reviewer harness changes and report artifacts are outside that repository. The fixture was built during the initial native build and was never executed. No implementation correction or WP04 work was performed.

The concrete partial artifacts were inspected before continuing: `probes/Program.cs`, `Probes.csproj`, the copied test support, `/tmp/g2-rereview-probes/probe-results.txt`, and the root/non-root TRX records. The initial probe source is preserved as `probes/AstraProgram.cs.txt`; the continuation adds controls without editing product source.

## Reviewer environment and execution

Native host: Ubuntu WSL2, x86_64; kernel `6.6.114.1-microsoft-standard-WSL2`; glibc `2.43`; .NET SDK `10.0.112`; .NET runtime `10.0.12`. Boot ID observed: `5a6f8a5a-c5ab-4738-b337-d1cf4e29a770`. The Windows workspace and PowerShell were used to inspect files and save reviewer artifacts. Linux execution occurred in separate `/tmp` exports.

Actual non-root qualification inventory:

| Field | Observed value |
|---|---|
| UID: real/effective/saved/filesystem | `1000 / 1000 / 1000 / 1000` |
| GID: real/effective/saved/filesystem | `1000 / 1000 / 1000 / 1000` |
| Groups | `1000(gaga)` |
| CapInh / CapPrm / CapEff / CapAmb | All `0000000000000000` |
| CapBnd | `000001ffffffffff` |
| NoNewPrivs | `1` |
| Qualification process cgroup | `0::/init.scope` |
| Explicit operator-supplied parent | `/sys/fs/cgroup/gaga-delegated` |
| Parent/host ownership | Existing `gaga:gaga` directories |
| Available controllers | `cpu memory hugetlb pids rdma` |
| Existing subtree controllers | `cpu memory pids` |

The bounding set is reported rather than silently treated as empty. Permitted, effective and ambient sets are zero. `wsl -u gaga` selected the existing user; `setpriv --no-new-privs` was unprivileged. No sudo, administrative helper, delegation provisioning, ownership change, ACL change, service installation or controller enabling participated in the non-root tests or probes. The parent and its controller state existed before these runs. An initial attempted unprivileged move into the existing `host` subgroup was denied; it made no change. The tests passed with the qualification process remaining in `/init.scope`, outside the empty child domains it created. Root-native tests are a separate evidence class and do not establish this privilege claim.

Exact successful build/test/probe invocations and archive locations are recorded in [commands](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/commands.md). Reproducible probe source is [Program.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/probes/Program.cs). Raw results are retained in [logs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/logs/test-summary.txt).

| Evidence class | Actually executed / verified |
|---|---|
| Reviewer-run portable | 196 portable cases within the initial Linux suite; continuation samples include 23 and 34 portable cases. The unconfirmed-directory aggregation probe uses the real journal/reconciler with fake domain/storage seams and a real remaining temporary directory. |
| Reviewer-run root-native | Initial M1 suite: **262 passed, 0 failed, 16 skipped**, total 278; 66 executed cases are native. Continuation samples: **31/31** and **47/47**, including 8 staging-cleanup and 13 domain-authority native tests. Native filesystem journal faults, real cross-process locks, real copy/publication and SCM_RIGHTS probes reproduce the partial findings. |
| Reviewer-run non-root delegated | Astra's two **16/16** runs are supported by retained records/outputs; the continuation independently repeats **16/16**. Native probes repeat the partial failures as UID 1000, accept an overlong payload with actual cgroup descriptors, and create/remove a domain through the journal-free allocation API. |
| Author evidence inspected only | R1–R5 narratives, prior Windows/full-solution results and historical author counts. These are not additional reviewer executions. |
| Future-WP evidence | Architectural source/plan inspection only. No Bubblewrap, coordinator, release or target behavior was executed or qualified. |

The complete initial root TRX, the first non-root TRX and the continuation non-root TRX are preserved. A repeated non-root invocation overwrote the second run's same-named TRX in `/tmp`; the first-run TRX remains separate and the continuation console output records the repeat. Counts are not used to infer missing lifecycle guarantees. Source-link warnings resulted from building immutable archive exports without Git metadata.

## Finding-by-finding disposition

All source locations below refer to the frozen source export; full links appear in the detailed assessments.

| Finding | Disposition | Source locations | Independent evidence and reason / limitation |
|---|---|---|---|
| G2-01 — WP03 parent FD ownership | **Resolved** | `Native/CgroupInterop.cs:178, 803`; `Lifecycle/M1RecoveryScope.cs:154, 192, 203` | The backend owns a duplicated parent; primary/recovery descriptors have distinct ownership. Native sentinel and repeated scope-disposal cases pass. Non-root primary/recovery independence passes. This disposition addresses the original scope-parent double-close. |
| G2-02 — WP03 strict SCM_RIGHTS | **Not resolved** | `Native/FdPassing.cs:46, 146, 185`; `Native/DomainHandleTransfer.cs:73` | Six/eight FDs refuse, received FDs are CLOEXEC, `/dev/null` cannot form a domain lease. However, a 52-byte message becomes a valid 51-byte payload and passes complete validation with actual cgroup handles. Transport truncates an overlong stream silently. |
| G2-03 — WP03 incremental composition | **Not resolved** | `Lifecycle/M1RecoveryScope.cs:154, 353, 404, 418, 597`; `Preparation/SafeCopyIn.cs:409, 478` | Integrated ordering and a real recovery sink exist, but the separately callable native owner allocation path creates a domain without an intent/writer. The staging subdirectory is duplicated only after a complete object binds. Required actual staging-actor death coverage is absent. |
| G2-04 — WP03 acknowledged durability | **Not resolved** | `Recovery/OwnershipJournal.cs:311, 317, 389, 394` | Injecting the second directory sync failure yields Write=false, baseline/LastSequence=1, but a fresh reader selects sequence 2/inode 777. Genesis failure also promotes an unacknowledged lone slot. |
| G2-05 — WP03 writer fencing/sequence | **Not resolved** | `Native/JournalFileStorage.cs:152, 178`; `Recovery/OwnershipJournal.cs:101, 154, 189` | Separate-process failed acquisition now yields no lease and cannot mutate bytes. Sequence tests pass. Repeated acquisition on one journal still produces two leases over one held lock; disposing one unlocks it, a separate process acquires a fresh lock, and the surviving lease writes successfully. |
| G2-06 — WP02 closure ownership | **Not resolved** | `Preparation/PreparedClosure.cs:98, 160`; `Preparation/PreparationStore.cs:97, 106, 121, 136` | Stale builder Dispose is correctly harmless. Two distinct capabilities can nevertheless publish the same closure. Invalidating the first closes authority while lookup, borrow and consume of the second all succeed. Transfer lacks a once-only/live-closure guard. |
| G2-07 — WP02 staging object cleanup | **Resolved** | `Preparation/StagingCleanup.cs:53, 72, 87`; `Preparation/PreparedClosure.cs:109` | The 8 native cleanup cases pass, including regular/symlink/same-content replacements, failures and repeats. Non-root replacement preservation passes. Current entry dev/inode is compared; failures retain debt. This does not resolve the separate staging-directory component omission in G2-08. |
| G2-08 — WP03 truthful cleanup | **Not resolved** | `Recovery/Reconciler.cs:267, 269, 272, 399, 416` | A valid round-tripped snapshot with an unconfirmed StagingDirectory and no cleaner returns CleanupConfirmed. The directory still exists. The component is filtered away before aggregation, rather than represented as Unknown. |
| G2-09 — WP03 complete domain identity | **Resolved** | `Lifecycle/RestrictedDomain.cs:80`; `Native/CgroupInterop.cs:344, 467, 611, 646`; `Recovery/Reconciler.cs:78` | Every-field and child-dev/inode tests pass. Native parent-as-child, swapped/sibling controls and CLOEXEC refusals pass; actual non-root handles validate. statx/fstatfs and exact named-control identity/access checks are present. Transport framing remains independently blocked by G2-02. |
| G2-10 — WP03 unprivileged recovery topology | **Not resolved** | `Native/PidFd.cs:60, 105`; `Lifecycle/M1RecoveryScope.cs:216, 317`; `tests/.../NonRootQualificationTests.cs:514, 714, 747, 788` | The actual inventory, delegation, retained pidfd, live exclusion, real owner exit/takeover and orphan adoption/reap are verified. Staging death cases remain same-process exceptions; the separate owner cases do not execute integrated staging. Approved decisive staging-owner death coverage remains missing. |
| G2-11 — PLAN/G2 evidence accuracy | **Not resolved** | Evidence `wp03.md:418, 446, 681`; `wp02.md:350`; `gagamba-linux-m1.md:25`; handoff `:39, 41, 43` | Exact SHAs, root/non-root counts and preserved rejection improve the record. Remaining statements about failed writes, unique writer leases, exact closure transfer and real composed death coverage are contradicted by probes/source. Documentation correction remains nonblocking in its own right. |

No item needs the `Not independently verifiable` disposition: the environment is available, and the unresolved qualification gap is demonstrable from the implemented/tested path. The review does not infer that all possible crash behavior has been disproved; it finds that required acceptance evidence and concrete invariants are still missing or violated.

## WP02 authority assessment — G2-06 and G2-07

Sources: [PreparedClosure.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Preparation/PreparedClosure.cs:98), [PreparationStore.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Preparation/PreparationStore.cs:97), [StagingCleanup.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Preparation/StagingCleanup.cs:53).

The store retains the exact closure reference; successful publication returns no independently disposable closure result. A stale builder's Dispose no longer closes store-owned handles. Reference equality still excludes fabricated equivalent capability identities, and consumption remains single-use. The accepted G1 vector remains 873 bytes with SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`; the canonical/state tests pass in the full suite.

**G2-06 reproduction:** copy an actual 128-byte tool through SafeCopyIn; publish capability A; call the stale builder's Dispose (handle stays open); publish the same exact closure using a second valid identity/preparation ID to obtain B; invalidate A. Output:

```text
STALE_BUILDER: published=True handleClosed=False
SHARED_CLOSURE: secondPublished=True handleClosed=True lookup=True borrow=True consume=True
```

This uses normal publication and invalidation operations, without forging capabilities or directly closing a handle. `TransferToStore` sets a Boolean without refusing an already-owned or disposed closure. Mint checks identity/expiry/preparation-ID uniqueness but never checks exclusive closure ownership. Each store entry can therefore independently release the same authority. B retains a non-null closure reference and its validity predicates do not detect destruction through A.

**Minimum correction — WP02:** make ownership transfer succeed exactly once for a live, unclaimed closure; refuse republishing a claimed/disposed closure; keep capability validity/borrowing consistent with authoritative closure lifetime. Preserve exact-instance G1 semantics and the intended consumed lifetime.

For G2-07, CleanOne uses the retained directory, opens the current name with no-follow resolution, compares dev/inode and checks unlink results. Replacement regular files, symlinks and same-content/different-inode files survive; inspection/removal failures are not counted as confirmed. CleanOne repeats treat absence as success and preserve mismatches. The trusted staging-parent mutation assumptions remain those of the approved plan; no broader hostile-filesystem atomicity is claimed here.

## Journal assessment — G2-04 and G2-05

Sources: [OwnershipJournal.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Recovery/OwnershipJournal.cs:296), [JournalFileStorage.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Native/JournalFileStorage.cs:152).

**G2-04 reproduction:** use a wrapper around the actual native JournalFileStorage that refuses one selected directory-sync call. Seed acknowledged sequence 1/inode 200. Write candidate sequence 2/inode 777. Failing the first directory sync preserves both slots and selects old sequence 1. Failing the second sync occurs after old-slot unlink and produces:

```text
FSYNC_FAIL_2: write=False last=1 baseline=1 reopened=2 reopenedInode=777
error=old retirement unconfirmed: reviewer injected dir fsync failure
GENESIS_FAIL: write=False last=0 reopened=1
```

The fresh reader is a new OwnershipJournal with a separate native storage object. No power-loss behavior is inferred from fault injection; the directly observed failed-operation/reopen behavior already violates the declared acknowledgement contract.

The exact resolution rules are: neither slot present => no snapshot; one valid slot with the other absent => acknowledged; two valid unequal sequences => the lower sequence; two equal sequences or other uninterpretable slot combinations => uncertainty. `gen.tmp` is ignored. These rules cannot distinguish a completed replacement from a candidate made lone by old retirement whose final sync failed. Genesis has the analogous problem after its first failed directory sync. File contents being fsynced do not prove acknowledgement of the directory transition.

The layout remains bounded: two slots plus transient storage, each subject to the 64 KiB codec bound; native reads inspect file length before full allocation. Boundedness is improved, but conservative acknowledged recovery is not established.

The supplied `AstraReplacementCaseKeepsOldAcknowledged` fails every `fsyncdir` call; it stops at the first sync. It does not exercise the second/final retirement sync. That explains its passing result alongside the reproduced blocker.

**Minimum correction — WP03:** make recovery distinguish a fully acknowledged generation from genesis/retirement ambiguity, and preserve the prior acknowledged snapshot or return explicit uncertainty after every failed durability step. Test both directory syncs separately, plus genesis and intermediate crash-visible states. Keep storage bounded.

**G2-05 separate-process control:** A acquires a writer and seeds sequence 1. A starts B as a separate `dotnet` process. B constructs its own JournalFileStorage and attempts a fresh lock open/acquisition. It receives no lease; WriteNext refuses; a SHA-256 comparison of the existing slot bytes is unchanged; sequence remains 1. Thus the original direct failed-lock mutation is corrected.

**G2-05 remaining reproduction:** A calls TryAcquireWriter twice on the same journal/native storage, obtaining L1 and L2. The storage returns success for its already-held lock name, so both leases refer to one underlying lock ownership. A disposes L1. A starts separate process B, which independently opens the lock file and successfully acquires the now-released kernel lock. While B holds that real separate lock and waits on its input, A's L2 remains IsLive and WriteNext succeeds:

```text
OVERLAP: acquire1=True acquire2=True survivorLive=True
RIVAL_LOCK=True leaseNull=False ... survivingLeaseWrite=True
```

B's lease is not a dup/inherited/shared lock-open-file-description. The sharing is between A's two logical leases. One releases the sole kernel fence while the other retains a usable mutation token. Write checks journal reference and the lease's released Boolean, but does not establish that this lease still owns the current fence.

Fresh writer/sequence-regression tests and actual death takeover pass under the single-lease discipline. They do not cure the lease-lifetime defect.

**Minimum correction — WP03:** enforce one active writer capability per actual fence, or a correct shared lifetime that cannot unlock while another usable capability exists; make released/lost fence authority unusable for mutation. Key native lock ownership by the full lock identity. Retain cross-process denied-write and overlapping-lease tests.

## Domain authority assessment — G2-01, G2-02 and G2-09

Sources: [FdPassing.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Native/FdPassing.cs:146), [DomainHandleTransfer.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Native/DomainHandleTransfer.cs:73), [CgroupInterop.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Native/CgroupInterop.cs:467), [RestrictedDomain.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Lifecycle/RestrictedDomain.cs:80).

The corrected parent duplication resolves the original scope-parent double-close. The native reused-descriptor sentinel and repeat-disposal tests pass. SCM_RIGHTS duplicates primary descriptors into the receiver, the sender closes its own copies, and recovery uses independent duplicates. Bootstrap sockets are closed at handoff; the transfer contains no release authority.

The metadata decoder validates magic/version, zero flags/reserved bytes, exact execution and lease-generation UUIDs, count and ordered roles. Receive uses MSG_CMSG_CLOEXEC, checks CLOEXEC and ancillary count/layout, and refuses MSG_CTRUNC/MSG_TRUNC when present. The native/control samples and independent probes show six descriptors refused, eight refused with MSG_CTRUNC, and seven `/dev/null` descriptors unable to form a valid DomainLease. Rejection cleanup was source-inspected and exercised by the native cases; no universal malformed-ancillary fuzzing claim is made.

**G2-02 remaining reproduction:** send the correct 51-byte payload plus byte `0x55`, with the seven correct descriptors. The socket is SOCK_STREAM. Receive allocates a 51-byte buffer; recvmsg returns exactly that valid prefix and leaves the extra byte in the stream without MSG_TRUNC. The decoder's exact-length check then sees 51 and passes. Under actual non-root delegation:

```text
REAL_DOMAIN_OVERLONG: sentBytes=52 receivedBytes=51
received=True decode=True domainValid=True
```

This is a complete transport/metadata/domain-validation acceptance, rather than only acceptance of `/dev/null` transport data. The receiver later closes the bootstrap socket, discarding evidence of the extra byte.

**Minimum correction — WP03:** preserve and validate message boundaries or otherwise detect extra bytes with a bounded, fail-closed framing protocol; require the entire transfer payload to be exactly 51 bytes. Test overlong payloads through the real transport with valid domain handles.

DomainIdentity now corroborates execution, boot, mount/type, parent dev/inode, child name/dev/inode and lease generation. Actual mount ID comes from statx and filesystem type from fstatfs. Each control is compared with the exact expected named object beneath the retained child, including dev/inode and appropriate access mode. The non-root correction opens the cgroup.kill membership probe for write access; it preserves exact control membership checks. Parent-as-child, swapped controls and same-device sibling controls refuse. Reconciler derives eligibility from record/current boot/live lease. Whole-domain stop remains retained cgroup.kill plus cgroup.events, rather than numeric PID authority.

## Recovery composition and truthful reconciliation — G2-03 and G2-08

Sources: [M1RecoveryScope.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs:154), [SafeCopyIn.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Preparation/SafeCopyIn.cs:409), [Reconciler.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Recovery/Reconciler.cs:267).

AllocatePreparation is real internal product code. It writes intent before domain creation, retains a domain recovery duplicate and staging-parent handle, writes registered actual domain/parent identities, performs copy with per-completed-object journal updates, uses a real private ScopeRecoverySink, writes Prepared and then calls Mint. Recovery has no Mint/reconstruction path. These improvements are credited.

**G2-03 remains incomplete in two concrete respects.** First, AllocateDomain has no journal/writer/intent parameter or precondition and remains the internal path used to start real owners. The non-root reviewer probe calls it without ever constructing a journal, successfully creates a native domain and obtains live recovery/owner transfer authority. It then validates the primary transfer and removes the domain through recovery authority. This is an available product allocation path, not an inaccessible test-only primitive.

Second, SafeCopyIn creates the staging subdirectory and destination before the OnObjectAccounted callback. That callback runs only after an entire object has copied, synced and rebound. The scope first captures/duplicates the subdirectory there. A failure/death before the first completed object has no pre-retained subdirectory authority or actual created-object identity in the scope's durable record. Dup/stat failures in that initial callback are not consistently refused before marking the subdirectory confirmed. The later after-create barrier precedes SafeCopyIn's staging-directory creation; it therefore does not test that vulnerable staging creation interval. The integrated path runs in the same process as its recovery scope, so its exception barriers do not establish survival across a separate allocation/staging actor's death.

**Minimum correction — WP03:** make the real owner/allocation path structurally require the scoped intent-first transaction; retain and acknowledge actual staging-directory/created-object cleanup authority at creation before dependent copy/publication; refuse retention failure. Demonstrate decisive creation/registration/mid-staging/acknowledgement deaths in a separate actor while the designated scope survives. Full instruction-by-instruction fault coverage is not required here.

The reconciler correctly separates stop, empty, staging, removal and persistence, and an ordinary recorded staging object with no cleaner now yields Unknown. Mismatch/failure and persistence errors are generally surfaced rather than hidden by domain success. Completed progress is persisted monotonically; existing idempotence tests pass.

**G2-08 remaining reproduction:** create a real temporary `unconfirmed` directory; build a valid snapshot containing `AllocationKind.StagingDirectory`, Confirmed=false; write/read it through the production journal/codec; reconcile with a corroborating live fake domain lease, writer and no staging cleaner. Output:

```text
UNCONFIRMED_DIRECTORY: outcome=CleanupConfirmed staging=Confirmed
directoryStillExists=True
```

The domain/storage seams make this a portable aggregation reproduction. No staging cleanup authority is supplied. The defect is deterministic product logic: line 269 filters unconfirmed directories out; line 272 then treats the remaining empty staging set as confirmed. Final aggregation cannot preserve Unknown for a component it has discarded. An unconfirmed directory may have been created before identity accounting; its unconfirmed status is not proof of absence.

**Minimum correction — WP03:** retain every required staging component in the cleanup/debt evaluation. An unconfirmed directory must remain Unknown until live authority verifies absence or safely confirms cleanup; omission must never establish success. Require confirmed object cleanup before container removal and test final aggregate status with unresolved directory debt.

## Unprivileged qualification assessment — G2-10

Sources: [PidFd.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Native/PidFd.cs:60), [owner spawn/wait](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs:216), [NonRootQualificationTests.cs](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/source/tests/Gagamba.Execution.Linux.M1.Tests/Recovery/NonRootQualificationTests.cs:714).

The explicit parent exists before Gagamba, and TryVerifyDelegatedParent verifies cgroup2 and existing memory/pids delegation without provisioning. Missing delegation refuses. The privilege inventory above is reviewer-observed, not inherited from author prose.

TrySpawnOwner obtains a real pidfd for its direct owner child immediately after spawning, checks CLOEXEC and retains a SafeFileHandle. Missing pidfd terminates/refuses; there is no lifecycle-authority PID polling fallback. WaitForOwner polls the retained pidfd before direct-child completion/wait. The tested lookup uses the retained handle for observation; it does not reopen a PID to decide owner exit. The pidfd is neither serialized nor placed in the seven-role DomainLease transfer and has no target-domain stop role.

The repeated non-root tests demonstrate: live owner/not-exited observation; recovery writer acquisition refused while the owner holds the lock; actual owner exit; pidfd-observed exit and direct-child completion; kernel fence release; takeover baseline 1 and write 2. Primary/recovery lease independence and appropriate non-root control operations pass. A finite trusted `sleep` child is demonstrably adopted by the designated subreaper and reaped with its waitpid. This is accepted WP03 orphan evidence.

**The required staging-owner death evidence is still absent.** DieAt at test line 714 throws BarrierDeathException. The integrated before-registration, mid-staging and Prepared-before-Mint tests at lines 723, 747 and 788 assert those exceptions in the surviving qualification process. They use real files and journaling, but no staging-owner process is killed or exits there. The actual separate owner executable handles domain receipt/registration/commands and does not run the integrated staging transaction. The separate before-registration owner test also constructs its recovery record from the live lease after exit; it does not demonstrate a durable pre-allocation intent for that path.

This distinction is a G2-10 blocker under the approved plan, not a later WP04 request. WP03's native criteria require owner crash around allocation registration/acknowledgement with surviving-scope cleanup. The original rejected review specifically required actual staging-owner death and distinguished it from abandoned same-process publication. The plan's WP03 completion condition requires every allocated object to be recoverable under the surviving-scope model. Real owner exit in a domain-only path plus exception abandonment in another path does not qualify the composed property.

**Minimum correction — WP03 qualification, with PLAN/G2 evidence updated:** run the actual composed staging transaction in a separately killable owner/allocation actor, retain required cleanup authority in the surviving scope beforehand, and exercise decisive death barriers under the existing non-root delegation. Preserve the passing pidfd, direct-child, orphan and domain authority tests. This is bounded required coverage, not exhaustive later-state qualification.

## Evidence assessment — G2-11

Locations: [WP03 evidence](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/evidence/docs/evidence/linux-m1/wp03.md:418), [WP02 evidence](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/evidence/docs/evidence/linux-m1/wp02.md:350), [aggregate index](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/evidence/docs/evidence/gagamba-linux-m1.md:25), [handoff](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/evidence/docs/handoff.md:39), [work queue](C:/Users/Laszlos/source/repos/Solo/review/g2-rereview-121c644/evidence/docs/work-queue.md:5).

The evidence commit is documentation-only; implementation SHAs and root/non-root result classes are explicit. Current index status leaves G2 unaccepted and WP04 blocked. The original rejection and its Astra reviewer body remain preserved, and historical author runs are inspectable rather than substituted for reviewer runs.

Remaining inaccurate current/remediation claims include: `wp03.md:418` says any earlier failure leaves the old generation intact; the following acknowledgement paragraph says failed writes never advance durable sequence; `wp03.md:446` says two independently usable writer leases never exist; WP02 claims exact once-only closure transfer; and the non-root barrier matrix at `wp03.md:681` groups exception abandonment with real owner-death proof. Handoff repeats these claims. The aggregate index says all blockers have been locally remediated and regression-protected, which is too strong after the concrete reproductions.

**Minimum correction — PLAN/G2:** record the remaining failures against literal source SHAs, distinguish exception barriers from process deaths, name the second/final fsync gap and lease/closure ownership defects, and retain root/non-root counts with their actual scope. Preserve the original reviews verbatim. G2-11 does not independently add a runtime blocker.

## WP04 compatibility and legitimate nonclaims

The selected future boundary remains architecturally plausible: atomic cgroup posix_spawn, fixed descriptor map 0–11, relocation of source FDs to unique temporary numbers at least 64, and closefrom(12). The domain controls, recovery duplicates, staging authority, journal lock, bootstrap socket and owner pidfd have no reason to enter that map; caller/recovery lifetime channels remain outside it. The pidfd is observation authority for the recovery host, not a helper-map entry. Existing CLOEXEC behavior supports this separation, but cannot replace the future explicit FD actions/audit.

The ownership, durability and composition blockers above prevent WP04 from beginning safely. No WP04 runtime proof is required for this decision, and no launcher redesign is requested.

Qualification is limited to the measured kernel/libc/architecture/delegation arrangement. LSM state remains Unknown. Simultaneous owner and recovery-scope loss remains unsupported; record-only restart recovery must quarantine without destructive adoption. Exhaustive later-state faults, broader hosts, resource/output enforcement, Bubblewrap/coordinator transport, release and target behavior remain future qualification. Those legitimate nonclaims are not additional G2 blockers. The missing decisive composed owner-death cases are current G2 requirements and cannot be placed in that deferred category.

The reviewer-created delegated child was removed successfully through its retained recovery lease. Final delegated-parent inventory contains only the pre-existing `host` subgroup, and the original Gagamba repository remains clean. Temporary files are retained as reviewer evidence outside the repository; their presence is not counted as product cleanup confirmation.

## Remaining minimum corrections

| Owner | Finding(s) | Required correction |
|---|---|---|
| WP02 | G2-06 | Exclusive, once-only live closure transfer; no valid capability backed by authority destroyed through another entry. |
| WP03 | G2-02 | Reject the entire overlong transfer through bounded framing/message-boundary validation. |
| WP03 | G2-04 | Conservative acknowledged resolution across genesis and the final retirement-sync failure; old generation or explicit uncertainty. |
| WP03 | G2-05 | Tie usable writer capability lifetime to the actual exclusive fence; overlapping logical leases cannot survive an unlock. |
| WP03 | G2-03 | Intent-first ordering on the real owner path and incremental retention of staging authority at creation. |
| WP03 | G2-08 | Preserve unconfirmed staging-directory debt in component and aggregate verdicts. |
| WP03 / PLAN-G2 qualification | G2-10 | Actual separate staging-actor death with surviving non-root recovery at decisive composition barriers. |
| PLAN/G2, nonblocking | G2-11 | Reconcile claims with these reproductions and the actual evidence class. |

These are continuations of the original findings, not new scope beyond G2.

G2 NOT ACCEPTED - CORRECTION REQUIRED
