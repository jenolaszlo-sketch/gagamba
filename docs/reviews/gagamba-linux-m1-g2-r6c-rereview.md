# Gagamba Linux M1 G2 R6C rereview (preserved)

- review type: independent G2-04/G2-05 (WP03) rereview (before WP04)
- implementation under test: cbe8739
- evidence-only commit at review time: 5fa1e5355bdeadc0bbdae481ccca34700d1a3ba4
- branch: qualification/ar7-feasibility
- disposition: G2-04 ACCEPTED; G2-05 ACCEPTED
- resolved: G2-04, G2-05 (G2-01, G2-02, G2-06, G2-07 and G2-09 were already resolved)
- still blocking: G2-03, G2-08, G2-10
- nonblocking: G2-11 (evidence correction/maintenance)
- reviewer observation (not a blocker): explicit TryRecoverCommit may adopt a fully staged sealed candidate, while ordinary read/writer acquisition remains conservative and refuses the ambiguous state
- G2 overall remains NOT ACCEPTED and WP04 remains blocked. The body below is the supplied text verbatim.

Independent G2 R6C Rereview — G2-04 / G2-05
Review-only. No repository source, tests, evidence, or documentation was modified; HEAD remains 5fa1e53… and the working tree is clean.
Target binding
Item	Verified value
Repository / branch	Gagamba; qualification/ar7-feasibility
HEAD	5fa1e5355bdeadc0bbdae481ccca34700d1a3ba4
Implementation under review	cbe8739
Evidence/docs commit	5fa1e53
Prior independent baseline for G2-04/G2-05	121c644 (preserved G2 rereview caab2d3/169e633)
Tree state	clean before and after




git show --stat cbe8739 touches exactly the six stated files. git diff cbe8739 5fa1e53 -- src/ tests/ is empty — the docs commit alters no implementation. The G2-04/G2-05 source delta versus the prior baseline is confined to Recovery/OwnershipJournal.cs and Native/JournalFileStorage.cs.
Method and environment
Native host: Ubuntu WSL2 x86_64, kernel 6.6.114.1-microsoft-standard-WSL2, glibc 2.43, .NET SDK/runtime 10.0.112. Non-root class: uid 1000 (gaga), existing gaga-delegated parent, no provisioning.
Independent reviewer probe (all artifacts outside the repo, in %TEMP%\opencode\r6c-probe): a console project named Gagamba.Execution.Linux.M1.Tests (so the M1 InternalsVisibleTo grant applies) that drives the real production OwnershipJournal over the real JournalFileStorage, wrapped by a fault injector that fails a chosen FsyncDirectory/Rename occurrence — the prior reviewer's method. Author tests were not trusted for any verdict; they were run only as regression.
Disposition
Finding	Status
G2-04 — generation acknowledgement / journal durability	ACCEPTED
G2-05 — writer fencing / duplicate acquisition	ACCEPTED




G2-04 — ACCEPTED
Preserved defect: a failed final directory sync occurred after the old slot was unlinked, so ReadLatest promoted the candidate (FSYNC_FAIL_2: write=False … reopened=2 reopenedInode=777); genesis failure also promoted a lone unacknowledged slot.
Source assessment: OwnershipJournal is now a deletion-sealed protocol. A commit creates gen.seal (fsync file + dir) before touching content, stages the candidate into both slots via atomic rename of a complete fsynced temp (never unlinking the old generation), then removes the seal last. Resolve returns explicit uncertainty whenever the seal is present; acknowledgement requires both slots equal with no seal; unequal slots resolve to the lower. FailSealed removes the seal only when the baseline still decodes from a slot. Storage reads are length-bounded before allocation.
Independent reproduction (real storage + injected faults):
PASS [FSYNC_FAIL_final(3)] write.Ok=False err=candidate directory sync failed: reviewer injected fsyncdir #3
   inMemoryLast=1 leaseBaseline=1 reopenedOk=False reopenedErr=commit seal present: durability ambiguous, explicit recovery required
PASS [FSYNC_FAIL_postB(2)] write.Ok=False reopenedOk=True reopenedSeq=1 reopenedInode=200
PASS [FSYNC_FAIL_seal(1)]  write.Ok=False reopenedOk=True reopenedSeq=1 reopenedInode=200
PASS [RENAME_FAIL_1]       write.Ok=False reopenedOk=True reopenedSeq=1 reopenedInode=200
PASS [RENAME_FAIL_2]       write.Ok=False reopenedOk=True reopenedSeq=1 reopenedInode=200
PASS [GENESIS_FSYNC_FAIL(2)] write.Ok=False reopenedOk=False reopenedErr=commit seal present: durability ambiguous...
PASS [GENESIS_FSYNC_FAIL(1)] write.Ok=False reopenedOk=False reopenedErr=commit seal present: durability ambiguous...


The exact old reopened=2/inode 777 result is not reachable: every failed durability step yields either the old acknowledged generation (seq 1/inode 200) or explicit uncertainty, with in-memory LastSequence/lease baseline unchanged. Both directory syncs, both renames, seal sync, and genesis are tested separately. R6C-PROBE FAILURES=0.
Scoped observation (not a blocker): TryRecoverCommit will adopt a fully staged candidate (both slots decode equal) after a failed final content sync — observed recoverOk=True committedSeq=2 committedInode=777. This is an explicit operator-invoked recovery, gated on both slots decoding; no read or acquisition path silently promotes it (pre-recovery ReadLatest refuses and a fresh TryAcquireWriter refuses). This satisfies the minimum correction's "preserve old or explicit uncertainty after every failed durability step." Whether recovery should instead always demote is a policy choice, not the demonstrated defect.
G2-05 — ACCEPTED
Preserved defect: two logical leases over one held lock; disposing one released the kernel fence, a rival acquired, and the surviving lease still wrote (acquire1=True acquire2=True … survivingLeaseWrite=True).
Source assessment: JournalFileStorage keys held fences by full lock identity (directory, name) and refuses a second acquisition of an already-held identity; OwnershipJournal.TryAcquireWriter refuses while a live _activeLease exists; Write requires IsEffectiveWriter, which demands the lease be the active, live holder and the real fence still held. JournalWriterLease.Release is once-only.
Independent reproduction (real native flock):
OVERLAP: acquire1=True acquire2=False secondNull=True err=[writer already active on this journal: dispose the live lease first]
PASS second journal, same storage refused while live
PASS separate native storage flock refused while live   (RIVAL_LOCK: ok=False)
PASS released survivor lease cannot write
PASS rival journal acquires after release
PASS rival writes after release
PASS no overlapping lease object exists (old counterexample impossible)


The old shape cannot be constructed: there is no second lease object. IsEffectiveWriter additionally revokes authority on fence loss (a disposed storage or externally released lock makes writes refuse). The exact prior cross-process takeover/denied-write owner-path tests are unchanged and pass in the full suite (LiveOwnerExcludesRecoveryWriter, DeadOwnerTakeoverContinuesSequence).
Regression and execution results (reviewer-run)
Check	Result
Independent R6C probe	0 failures
Journal-focused (JournalTests+JournalFencingTests)	68 passed, 0 failed
Full root-native Linux M1	326 passed, 0 failed, 16 skipped
Non-root delegated (uid 1000, gaga-delegated)	16 passed, 0 failed, 0 skipped
Windows M1	244 passed, 0 failed, 98 skipped
Windows full solution build	0 warnings, 0 errors




Limitations / non-claims
- Durability faults are injected at the storage seam; no physical power-loss is claimed. The conclusion is about the protocol's acknowledgement honesty, which is exactly what the preserved finding targeted.
- Each finding was judged independently; accepting them does not touch G2-03, G2-08, G2-10, or G2-11, and implies nothing about WP04.
Final
- G2-04: ACCEPTED
- G2-05: ACCEPTED
- Global state unchanged and not accepted:
G2:   NOT ACCEPTED
WP04: BLOCKED


No preserved independent review was altered. No source, test, evidence, or documentation file was changed by this review.
