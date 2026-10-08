# Review remediation plan — handoff to Sol

Date: 2026-10-08. Status updated 2026-10-09: **AR-0/AR-1 implemented and locally Windows-native-tested; AR-2 onward remains planned**. See [AR-1 evidence](evidence/windows-lifecycle-AR-1.md).

User direction: turn the independent review into an actionable implementation handoff in Gagamba's docs. Sol implemented the bounded AR-0/AR-1 slice and stopped. This plan does not claim release or hostile-workload security qualification.

**Mandatory evidence gate for AR-2 through AR-7:** Follow the
[remediation evidence standard](remediation-evidence-standard.md) for each
slice and any future security-sensitive remediation. A slice is not complete
without its own durable document under `docs/evidence/` containing the required
source/host header, baseline-to-fix table, exact test counts, cleanup evidence,
ownership semantics, finding dispositions, exclusions, and model-review facts
when used. Record `IMPLEMENTED — NATIVE QUALIFICATION PENDING` when required
native qualification is unavailable; never treat a skip as qualification.

Review baseline: commit `8dea7d86c8ee2f1c924100acdbc1bf5dc14b2a88`, package source version `0.1.0-preview.4`. Read the [full review](reviews/2026-10-08-independent-review.md) and [evidence manifest](reviews/2026-10-08-evidence/evidence.json). The review's F01–F15 finding IDs and E1–E10 experiment IDs are stable references throughout this plan.

## Outcome and scope

Deliver a dependable, explicitly scoped process-lifecycle substrate first. It must neither release work after shutdown wins nor lose completion/cleanup ownership. It must expose uncertainty honestly, avoid unintended authority transfer, and have repeatable native evidence for its claims.

That milestone is **not** approval to run hostile generated code with host credentials. Filesystem, network, identity and IPC restrictions require a separately qualified restricted-execution profile. Lifecycle fixes alone cannot change the review verdict to a reliable security boundary.

Keep Gagamba standalone. Do not modify sibling repositories, introduce Hufu policy evaluation, implement distributed execution, add containers by default, build a general VFS, or rewrite all providers. Preserve platform differences and required-capability refusal. Prefer internal repairs to the current SPI; when a public change is necessary, document the specific finding/consumer pressure, alternatives, migration and compatibility tests.

The earlier instruction to freeze features pending consumer pressure does not block repairs proved by this review. It still bars speculative expansion. Historical milestones in the queue/handoff are evidence, not instructions to redo completed platform work.

## Start here

1. Read `AGENTS.md`, current handoff/queue, this plan, the review, `design-rules.md` and `execution-domain.md`.
2. Inspect the current Git revision and working tree. Preserve unrelated changes; do not assume the reviewed revision remains HEAD.
3. Complete AR-0, then implement AR-1. Turn E1/E2/E3 into bounded deterministic regression tests before repairing Windows lifecycle code. Do not begin with new sandbox mechanisms or API expansion.
4. Continue through the dependency table only when separately directed. For each AR-2–AR-7 slice, produce the required `docs/evidence/` artifact before marking it complete, then update the handoff and queue to its measured status. If a native host is unavailable, complete portable/internal work and state the exact native qualification still pending; do not mark the slice qualified.

## Dependency and finding map

| Slice | Priority / initial state | Dependencies | Findings | Deliverable |
| --- | --- | --- | --- | --- |
| AR-0 | Immediate / Ready | None | F01, F02, F14 | Correct scope/claim documentation and define trustworthy regression oracles |
| AR-1 | Critical / Ready after AR-0 | AR-0 | F03, F04, F05 | Windows lifecycle ownership and async completion repaired |
| AR-2 | Critical / Pending | AR-1 invariants | F03, F05, F08, F09, F10 | Linux lifecycle, descriptor boundary, cleanup and prerequisites repaired |
| AR-3 | Critical / Pending | AR-1 invariants | F03, F05, F06, F07 | macOS lifecycle and helper observation/termination repaired |
| AR-4 | Hardening / Pending | AR-1–AR-3 state contracts | F02, F11, F12 | Immutable invocation/capability data, portable argv and correct issuance |
| AR-5 | Usability/resource bounds / Pending | AR-1–AR-4 | F13; result gaps in review G | Small deadline/output orchestration and explicit stop/cleanup evidence |
| AR-6 | Release gate / Pending | AR-1–AR-5 applicable claims | F14, F15 | Qualified native conformance and exact-revision package qualification |
| AR-7 | Security-profile decision / Held | Defined workload and AR-0–AR-6 evidence | F01; F02/F08/F13 security implications | Separate restricted-profile decision and enforcement qualification plan |

This covers every review finding. Native test-oracle fixes needed by AR-1–AR-5 belong in those slices; AR-6 is final qualification, not permission to postpone meaningful tests. The sequence does not request parallel agents.

The table preserves the original dependency plan. Current disposition is in
[the work queue](work-queue.md): AR-0 is documentation/test-rule complete;
AR-1 is implemented and locally Windows-native-tested; AR-2 and later were not
started. F01 hostile-workload enforcement, F02 broader capability honesty,
and F14 release qualification remain open beyond the bounded AR-0 wording and
test-oracle corrections.

The evidence gate applies separately to every later slice: AR-2 must separate
root/WSL from delegated unprivileged Linux; AR-3 must separate parser/fake
helper tests from native launchd; AR-4 must include compatibility and invocation
round trips; AR-5 must prove bounded output and distinct stop causes; AR-6 must
bind source and package candidates and negatively test its release gate; AR-7
requires positive and negative controls for each claimed restriction. The
standard gives the full checklist and completion vocabulary.

## AR-0 — Correct the contract and establish regression rules

**Change:** Update README/package-facing usage and current contract docs to say that the shipped native providers control lifecycle, not filesystem/network/credential access. Label offline-process-v1 and the experimental Windows isolation engine as separate, unqualified work. Keep lifecycle capability descriptions precise: Windows direct-descendant job membership does not imply containment of broker-created work; Linux migration and macOS setsid remain explicit bounds.

Do not silently change the meaning of an existing Full grant in a way that leaves callers believing they have broader protection. Record whether narrowing the named contract is sufficient or whether a capability level/version change is required; implement any code change and compatibility tests in AR-4. Do not use a rename or disclaimer to claim that hostile execution is safe.

**Regression rules:** Give each test leg its own workspace/nonce. Require fresh effects after launch and a separate root-exit signal before testing surviving descendants. Use an independent bounded observer/cleanup mechanism. Frozen mtime alone cannot prove death; a prior leg's heartbeat cannot prove the next child ran. Fix shared conformance cases as they become acceptance tests.

**Exit:** Current docs agree on shipped scope; each required regression has an explicit oracle and timeout; historical docs remain identifiable. No production security qualification is claimed.

## AR-1 — Windows lifecycle and completion

Primary files: `WindowsExecutionProvider.cs`, `OwnedJob.cs`, Windows provider tests; SPI docs only where observable behavior needs clarification.

**Required invariants:**

- Every allocated preparation, launch-in-progress, running domain and cleanup-in-progress resource has exactly one owner.
- Disposal prevents new admission and waits for or cancels in-flight admission before completing. A launch must not release its target after disposal has won the release decision.
- A domain cannot disappear between preparation removal and execution registration.
- One per-execution state owner persists root exit status, termination intent and cleanup outcome. Observer cancellation does not cancel that owner.
- Define behavior for two simultaneous waiters and dispose-during-wait. No double-close, invalid-handle polling, unbounded tombstones or lost terminal result.

**Implementation:** Repair check/allocate/register races under a coherent state protocol. Use SafeHandle/reference-safe native lifetime. Make `WaitForCompletionAsync` yield promptly for a live root; classify WAIT_FAILED and exit-code query failure. Serialize terminal ownership and termination intent rather than storing only a waiter-local record. Keep native operations outside long global critical sections where possible, with explicit in-flight ownership that disposal joins.

**Tests:** E1 prompt return; E2 launch paused after prep consumption while Dispose completes; E3 disposal during a live wait. Add prepare/dispose, two waiters, terminate/reap interleavings, root-exit-with-child, assignment/resume failure and repeated cleanup. Use barriers/internal seams, not timing guesses. Finite children and an independent watchdog must clean up even when the implementation fails.

**Exit:** These regressions fail on the baseline and pass after repair; original Windows tests pass; no owned handles/processes remain. Record whether the pre-assignment owner-crash interval needs creation-time job assignment or a explicitly bounded contract. Do not infer crash safety merely from CREATE_SUSPENDED.

## AR-2 — Linux lifecycle, descriptors and cleanup

Primary files: `LinuxExecutionProvider.cs`, `OwnedCgroup.cs`, `NativeMethods.cs`, Linux tests.

**Change:** Apply AR-1 ownership invariants while preserving atomic cgroup placement. Persist reaped status in the execution record/task so E8 cancellation/retry returns the original exit code. Give termination and completion one reaping owner; distinguish ECHILD, EINTR and genuine wait errors.

Close unrelated descriptors atomically in the child through suitable spawn actions or a narrowly scoped native helper. Explicitly define allowed stdin/stdout/stderr. Never close arbitrary descriptors in the host or depend on all embedding libraries choosing CLOEXEC.

Fix disposed-before-Kill ordering. The normal provider currently calls Kill separately: do not overstate the audit as proving all Linux disposal omits termination. Check kill results, wait boundedly for subtree emptiness, remove bottom-up and preserve cleanup failure. Distinguish inaccessible events files from confirmed missing domains; `File.Exists=false` is not proof of absence.

Probe required native entry points/ABI prerequisites and return stable classified refusal when unsupported. Synchronize the once-only placement self-test and publish its completed result atomically. Do not fall back to racy parent-side placement. Document qualified kernel/libc/architecture/delegation conditions.

**Tests:** E8 cancel after root reap while child stays alive; E9 inheritable dummy file/pipe/socket descriptors; concurrent first Prepare; missing symbol/kernel capability; denied kill/read/remove; nested groups and delayed exits. Repeat migration E10 within a unique outer test cgroup and add an unprivileged delegated environment when available. Migration resistance must remain Partial unless target control over the hierarchy is actually denied.

**Exit:** Native qualified Linux tests pass, supported/unsupported hosts classify correctly, descriptor allowlist holds, and test-owned cgroups/processes are removed or an explicit failure is retained. Root WSL evidence must not be labelled unprivileged deployment qualification.

## AR-3 — macOS transport, observation and stop evidence

Primary files: `MacOsExecutionProvider.cs`, `OwnedJob.cs`, `Launchd.cs`, macOS tests.

**Change:** Apply lifecycle ownership invariants. Resolve the trusted helper by absolute path. Drain stdout/stderr concurrently with a deadline covering start/read/exit, bounded retained output, and helper kill/reap on failure. Truncate diagnostic excerpts after parsing, not the state input before parsing.

Represent Running, Terminal, ConfirmedNotFound and ObservationFailed/Unknown separately. Match states exactly; handle recorded exits, signal exits, missing fields and unknown future states. Neither a generic nonzero print status nor a failed helper invocation proves absence. Failed bootout plus failed observation must never produce confirmed successful termination. Retain job ownership when cleanup is uncertain, and prove the promised same-PG terminal state without claiming setsid containment.

**Tests:** Injectable helper transport with timeout, pipe saturation, long output, malformed/unknown state, permission error, unavailable helper and failed bootout. Preserve instant-exit regressions. Run native macOS cases with finite workloads and independent liveness/cleanup evidence; pure parser tests are not sufficient.

**Exit:** Helper calls are bounded; unknown state never becomes confirmed stopped; completion and stop evidence match native same-PG behavior. No native macOS execution happened in the review, so native qualification is mandatory before closing this slice.

## AR-4 — Inputs, capability data and token issuance

Primary files: execution contract/negotiation/platform types, provider launch/discard validation, runtime and contract tests.

**Change:** Freeze shared capability maps and snapshot invocation/environment inputs once before validation/encoding. Validate malformed/null/NUL values and ensure failed validation cannot strand a consumed domain. Fix Linux environment key comparison to ordinal. Add a portable argument-vector entry point with defined Windows serialization; preserve raw command-line behavior only as a clearly named compatibility path. Do not silently reinterpret existing strings.

Require explicit executable/cwd identity in security-sensitive profiles; record ambient lookup behavior for compatibility usage. Preparation is domain negotiation, not authorization of the later invocation. Document the snapshot/binding obligation at the caller seam without importing Hufu.

Make Discard distinguish the issuing instance and its legitimately spent token from a never-issued/foreign token. Preserve idempotence without accumulating unbounded tombstones. Resolve Describe-after-disposal consistently. Implement AR-0's capability-honesty decision with migration notes and required-capability tests.

**Tests:** E4 same-provider foreign-instance discard; forged IDs; concurrent discard/launch; mutable collections; argv round trips including empty/quoted/backslash/Unicode values; malformed input cleanup; executable lookup/replacement in disposable paths. Retain public native-handle opacity.

**Exit:** Same normalized snapshot is dispatched; shared capability data cannot be mutated; issuance behavior is correct; any additive public API has compatibility tests and a documented reason.

## AR-5 — Deadline, output and execution evidence

Start with the smallest helper above the repaired SPI. A deadline must explicitly terminate the execution and await bounded completion; cancelling the observation token alone is not a deadline implementation. Preserve separate user-cancel, deadline, natural-exit, launch-failure and cleanup-failure causes.

Define bounded output behavior before implementation: total/per-stream budget, binary/text handling, concurrent drain, overflow outcome, descendant-held pipe deadline and retention/redaction. Avoid unbounded memory and unbounded hidden macOS files. Apply restrictive permissions to private metadata/logs; do not imply they isolate a same-identity hostile target. Use synthetic secrets in tests.

Expose the least structured evidence needed for reliable consumers: execution correlation, effective capabilities, normalized invocation fingerprint, timestamps/duration, stop cause and confirmed/failed/unknown cleanup. Keep argv/environment/output redacted by default. Add a receipt through an additive API only if internal state plus an orchestration result cannot meet the need; document the pressure on the frozen SPI.

**Tests:** Deadline while root/descendants run; wait cancellation without termination; simultaneous bounded stdout/stderr flood; open descendant pipe after root exit; output overflow; cleanup failure; safe synthetic secret retention. Run resource tests inside an independent outer budget.

**Exit:** A complete documented run/timeout/collect/stop/discard flow works portably. Process-count/memory controls are evaluated for the hostile profile, not advertised as implemented by a deadline/output helper. Broad quotas and telemetry remain deferred.

## AR-6 — Conformance and release qualification

Finish per-execution nonce/root-exit/death oracles; prove them by testing deliberately broken providers. Replace vacuous successful returns with real skipped/unsupported results. Exploratory runs may report skips; qualification must fail if a required host/leg is absent. Correct fake-provider semantics and reuse lifecycle scenarios against native providers.

Create a qualified Linux job/environment with required libc/kernel/cgroup delegation; retain native Windows and macOS jobs. Bind evidence to exact source SHA, dirty state, OS/kernel/libc/architecture, privileges, mechanism and test IDs. Qualify the currently advertised scope, not all aspirational offline-process-v1 claims.

Require matching exact-revision qualification before a tag/manual publish workflow can push packages. Build local package candidates and test them from a source-independent consumer; do not publish during a verification test. Package metadata/source links and dependency versions must correspond to the qualified source.

**Tests:** Missing delegation/launchd capability, omitted required case, stale evidence SHA and unqualified release reference all block qualification/publication. Bounded race repetition supplements deterministic tests; do not accept retry-until-green as proof.

**Exit:** All supported platform claims have native required evidence; installed-package consumers pass; a release cannot bypass that evidence through either trigger. Production publication is a separate action, not implied by this handoff.

## AR-7 — Restricted-execution boundary: held decision gate

F01 remains open as a security-readiness gap after lifecycle repairs. Prepare a narrow design decision for the first actual hostile workload: target identity, filesystem grants/runtime closure, network and local IPC policy, inherited authority, control-file protection, process/memory/output/deadline limits, cleanup/recovery bounds and platform support.

Select mechanisms only after focused feasibility experiments. A disposable workspace or voluntary VFS does not confine arbitrary native code. No container/provider choice is preselected. Unsupported profiles must reject before target release. Do not silently weaken a requested profile on Linux or macOS to match Windows.

**Exit to implementation:** A concrete workload, documented threat model, chosen enforceable profile, positive/negative control suite and explicit implementation scope. Bring material product/security scope decisions back to the user with evidence. This gate is not permission to stall AR-0–AR-6.

**Exit to hostile-workload readiness:** Real cross-boundary file/network/IPC/credential-sentinel tests, link/mount/alias tests, broker/migration/session escape tests and resource limits all match the claimed profile. Only then reassess the original security-boundary verdict. Do not mark F01 fixed merely by rewriting the README.

## Validation and evidence discipline

Original review results: 14/14 contract tests, 14/14 Windows provider tests, five Windows conformance xUnit assertions passed, despite the reproduced defects. E1–E4 are Windows behavior; E5–E7 are pure-code probes; E8–E10 are native Linux root-WSL probes. E5 is synthetic parser input and E6 a fake directory; neither proves native macOS/Linux cleanup behavior. E10 confirms a documented Linux limitation, not a contradiction of a Full escape guarantee.

Archived probes and original results live under [review evidence](reviews/2026-10-08-evidence/README.md). Their project source includes resolve to the current checkout for convenience. For baseline reproduction, use an isolated checkout of the recorded SHA; do not revert this working tree or assume an old binary tests a new source revision. Keep probes test-only and promote targeted cases into proper regression suites.

For AR-2 through AR-7, the [evidence standard](remediation-evidence-standard.md)
is an exit criterion, not a progress-report format. Give each slice a separate
`docs/evidence/` document that follows the standard's platform-specific section.
Bind baseline and final observations to exact source SHAs and dirty state;
identify the tested host, prerequisites, positive/negative controls, exact
passed/failed/skipped/unsupported counts, native-domain and cleanup evidence,
finding dispositions, and what the result does not qualify. Preserve raw logs
under ignored `artifacts/` when useful. An evidence-only documentation commit
may follow the tested source commit, but the document must identify the exact
tested source SHA and must not silently transfer qualification to a later
security-sensitive code change. Update queue/handoff only to the level actually
established: implemented, locally tested, native-qualified,
package-qualified, or release-qualified.

Use finite disposable workloads, bounded test waits and positive controls. Do not access real credentials or run host-exhausting floods. Prepare exact setup/cleanup before privilege-sensitive host changes and check existing task authorization. A test fixture's outer protection does not prove the library enforced the corresponding inner restriction.

## Suggested first implementation handoff prompt

> Continue Gagamba in this repository using docs/review-remediation-sol.md as the current action plan. Read AGENTS.md, docs/handoff.md, docs/work-queue.md and the linked independent review first. Complete AR-0 and then AR-1: correct the lifecycle-only scope and build deterministic E1/E2/E3 regressions before repairing Windows launch/dispose ownership and completion. Keep all native resources owned through in-flight operations; observer cancellation must not lose execution state. Preserve unrelated changes and original proposal bytes. No speculative sandbox platform, policy engine, sibling edits or publication. Record exact test/cleanup evidence and update the queue/handoff after each coherent slice. Continue the ordered remediation backlog; keep unsupported native qualification and AR-7 security-profile decisions explicit.
