# Independent R6D.2b Topology Rereview

## Target binding and preservation

The checkout resolves to branch `qualification/ar7-feasibility`, HEAD `0589dd1716100dd5f6b4a7deda369985cf105b6a`. The R6D.2b source commit is `2b6b9d67ba79b90d102d83150f079c299a601bf9`. The source and test diff between those commits is empty; the evidence commit changes three documentation files. The preserved review files are unchanged, and there are no WP04 source changes.

`git status` itself fails in this environment with “this operation must be run in a work tree.” I checked the index and tracked files separately: staged diff exit 0, with zero modified, deleted, or untracked files. No full D1–D6 qualification was performed.

## B1 residual 1: queued records

**Corrected.** The regression queues `WriterReady` followed by `BarrierReached("after-intent")`. Both records are received separately and sequenced in order; the receiver does not reject the second record merely because it is already queued, drain it, or silently ignore it. The behavior is covered by the protocol tests in [OwnerProtocolTests.cs](C:/Users/Laszlos/source/repos/Gagamba/tests/Gagamba.Execution.Linux.M1.Tests/Recovery/OwnerProtocolTests.cs:23).

The sequencer validates each consumed record. Duplicate and stale sequences, wrong type, identity, or generation, and unexpected messages fail closed. There is no queue-draining path that could hide a surplus record.

## B1 residual 2: equal-length messages

**Corrected.** `BarrierReached("after-registered")` and `StagingAuthority` remain equal-sized 64-byte records. The production receiver decodes the complete record’s header and type before dispatch, so the barrier is treated as a descriptor-free barrier. The following staging-authority record is decoded separately; its FD count and descriptor properties are checked before retention. The relevant receive paths are in [FdPassing.cs](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution.Linux.M1/Native/FdPassing.cs:198) and [OwnerProtocol.cs](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution.Linux.M1/Native/OwnerProtocol.cs:305), with state dispatch in [M1RecoveryScope.cs](C:/Users/Laszlos/source/repos/Gagamba/src/Gagamba.Execution.Linux.M1/Lifecycle/M1RecoveryScope.cs:594).

The old length-directed refusal, “no SCM_RIGHTS control message,” is gone. The collision remains explicitly pinned by the regression.

## B3 disposition

**Corrected.** The renamed `NonRootMidStagingDeathResolvesUnconfirmedAbsentInputAsGenuineAbsence` test matches the production ordering: at the real mid-staging point, the tool is accounted and present, while the input is unconfirmed and genuinely absent. After one targeted owner termination, reconciliation returns `CleanupConfirmed`, removes the tool, and leaves the absent input absent. The test does not fabricate post-death files or change barrier placement. See [NonRootQualificationTests.cs](C:/Users/Laszlos/source/repos/Gagamba/tests/Gagamba.Execution.Linux.M1.Tests/Recovery/NonRootQualificationTests.cs:446).

This is one targeted regression, not G2-03’s D1–D6 qualification matrix.

## Receive, dispatch, and FD safety

The production path receives one bounded `SOCK_SEQPACKET` record, decodes type and identity, validates protocol state and sequence, checks exact payload and FD shape, then dispatches. It retains no stream fallback or length-based semantic selection. Readability is awaited with a bounded timeout; EOF, truncation, malformed ancillary data, and protocol mismatches fail closed.

The Linux protocol/domain checks exercised exact descriptor counts and roles, CLOEXEC, truncation, zero-length `SCM_RIGHTS`, rejected-FD cleanup, wrong-type dispatch, and missing or surplus FDs. A rejected transfer does not advance sequence or leak its received descriptors; accepted ownership transfers once.

## Topology, authority, and writer lifecycle

R6D.2b did not change the owner-owned topology. The owner holds the transaction writer, creates the domain, stages and accounts objects, writes `Prepared`, and owns the `PreparationStore` and `Mint`. The recovery scope remains the external survivor, lifecycle observer and reconciler; it retains recovery duplicates for domain and staging authority and takes over the journal writer only after owner exit proof. It receives a bounded prepared description, not a capability. No scope-local preparation path reappeared.

The intent writer is released before spawn; the owner acquires the live writer while the scope is excluded. After owner death and reap, the scope can take over. Targeted lifecycle samples passed without overlapping usable writer authority. Domain descriptors are validated before retention; the scope’s recovery authority is acknowledged before dependent work proceeds. Staging authority is likewise retained and acknowledged before copying. These findings do not reopen G2-05 through G2-09.

## Barrier reachability

All seven real owner-side barriers were reached in the temporary source archive without running the full death matrix:

| Barrier | Reachable | Durable state | Survivor authority | Honest future kill point |
| --- | --- | --- | --- | --- |
| after-intent | Yes | `AllocationIntent` | Scope and recovery envelope; no domain or staging FD yet | Owner paused after durable intent |
| after-domain-retained | Yes | `AllocationIntent` | Domain recovery authority retained; no staging FD yet | After domain-retention acknowledgement |
| after-registered | Yes | `AllocationRegistered` | Domain recovery authority retained | Before staging authority transfer |
| after-staging-accounted | Yes | `AllocationRegistered` | Domain and staging authority retained; staging directory accounted | Before object copy |
| mid-staging | Yes | `AllocationRegistered` | Domain and staging authority retained; tool accounted, input absent | After tool copy and before input copy |
| after-copy | Yes | `AllocationRegistered` | Domain and staging authority retained; copied objects accounted | After copy, before `Prepared` |
| after-prepared-before-mint | Yes | `Prepared` | Domain and staging authority retained; no capability minted yet | After `Prepared`, before owner-local `Mint` |

The after-intent reachability covers the queued-record correction; after-registered covers the 64-byte dispatch collision. Reaching these points does not establish post-death recovery for D1–D6.

## Execution results

**Linux/WSL:** The broad root M1 selection passed **335**, skipped **13**, and failed **0** (348 selected). Ten owner-termination invocations, including the D1–D6 rows, were excluded. The focused protocol/domain/integrated subset passed **44/44**. The seven-barrier reachability probe passed; two additional accepted-boundary owner-lifecycle samples passed **2/2**.

**Non-root:** The existing WSL user was uid/euid 1000 with effective, permitted, and ambient capabilities zero. The already-existing delegated cgroup was used without provisioning or host changes. The protocol/scope selection passed **28**, skipped **19**, and failed **0**; the skips were root/capability-dependent domain cases. The targeted B3 and replacement-staging controls passed **2/2**. Protocol-only checks do not require delegated cgroup setup; the real delegated owner transaction does.

**Windows/portable:** The independent Windows M1 run passed **257**, skipped **101**, and failed **0** out of 358. The full solution compiled with **0 errors and 48 SourceLink warnings** because the temporary archive had no Git metadata. The author’s recorded zero-warning build could not be reproduced from that archive. The canonical bytes independently measured **873 bytes**, with SHA-256 `82161029848b1a821241af112ca806be8029d3cf2e7b95c6b23ce3e35850d5f3`; a later isolated filter-only rerun stopped during restore, while the completed Windows M1 run included the portable manifest test.

The evidence documents correctly keep G2-03 blocking and distinguish targeted B3 evidence from the full matrix. Their reported root count differs by one pass/skip from this rereview’s run; neither run includes the death matrix.

## Findings and global status

No concrete protocol or topology blocker remains that prevents honest D1–D6 execution against the production owner topology. The receive corrections preserve framing and FD safety, B3 now asserts a reachable state, and all seven barriers are reachable. ADR 0003 and the validated owner-owned architecture were not revised or contradicted.

R6D.2b is accepted for **D1–D6 qualification readiness only**. G2-03 remains blocking pending the actual matrix and independent review. G2-10 remains blocking; G2-11 remains maintenance; G2 remains **NOT ACCEPTED**; WP04 remains **BLOCKED**.

R6D.2 TOPOLOGY ACCEPTED FOR D1-D6 QUALIFICATION - G2-03 REMAINS BLOCKING