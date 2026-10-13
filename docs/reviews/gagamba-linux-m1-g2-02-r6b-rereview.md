# Gagamba Linux M1 G2-02 R6B rereview (preserved)

- review type: independent G2-02 (WP03) rereview (before WP04)
- implementation under test: b80e9c28b462e2aa392313ff5dc58063478b955c
- evidence-only commit at review time: d8edd3bca37dc58970660ae265a4bfa769146f11
- branch: qualification/ar7-feasibility
- disposition: G2-02 RESOLVED - R6B ACCEPTED
- resolved: G2-02 (G2-01, G2-06, G2-07 and G2-09 were already resolved)
- still blocking: G2-03, G2-04, G2-05, G2-08, G2-10
- nonblocking: G2-11 (evidence correction/maintenance); F1 hygiene debt (zero-length SCM_RIGHTS rejection leaks received descriptors); F2 test race (BootstrapEndpointClosedAfterSuccessfulHandoff)
- source: reviewer-built pristine export of b80e9c28b462e2aa392313ff5dc58063478b955c plus independent probes outside the repository
- G2 overall remains NOT ACCEPTED and WP04 remains blocked. The body below is the report verbatim.

# Independent G2-02 Rereview (R6B)

Review-only independent verification of G2-02 / WP03. No repository modification, no fixes, no WP04 work performed. The author's implementation report was read only after source inspection and was not used as proof for any claim below.

## Scope

- Branch: `qualification/ar7-feasibility`
- Frozen implementation under review: `b80e9c28b462e2aa392313ff5dc58063478b955c`
- Evidence-only commit: `d8edd3bca37dc58970660ae265a4bfa769146f11`
- Original independent G2 rereview: `docs/reviews/gagamba-linux-m1-g2-rereview.md`
- Previous G2-02 disposition: **Not resolved**
- Reviewer host (native): Linux 6.6.114.1-microsoft-standard-WSL2 x86_64, glibc 2.43, .NET SDK/runtime 10.0.112, UTC 2026-10-13.

## Frozen implementation SHA / evidence-only SHA / clean-tree verification

- `git show --stat b80e9c2` touches exactly three files: `src/Gagamba.Execution.Linux.M1/Native/FdPassing.cs`, `tests/.../Recovery/DomainAuthorityTests.cs`, `tests/.../Recovery/RecoveryHarness.cs`.
- `git diff --name-only b80e9c2 d8edd3b` (and `b80e9c2..HEAD`) touches exactly four documentation files: `docs/evidence/gagamba-linux-m1.md`, `docs/evidence/linux-m1/wp03.md`, `docs/handoff.md`, `docs/work-queue.md`. No source or test behavior differs between the implementation SHA and HEAD.
- `git diff b80e9c2 HEAD -- src/ tests/` is empty; `git status --porcelain=v1` is empty before and after all reviewer execution (all reviewer builds, probes, and cgroup artifacts live in `/tmp`, outside the repository).
- The review target is therefore cleanly bound: source/test behavior under review is exactly `b80e9c2`; `d8edd3b` is documentation/evidence only.
- WP04 remains untouched: work-queue, handoff, and evidence index all record WP04 blocked; no coordinator/helper/target code was added or executed. No WP04 work was begun by this review.

## Disposition

**Resolved** — G2-02 only. All other G2 findings keep their prior dispositions (G2-01/G2-06/G2-07/G2-09 resolved; G2-03/G2-04/G2-05/G2-08/G2-10 blocking; G2-11 maintenance; G2 overall NOT ACCEPTED; WP04 blocked). Two non-blocking findings (F1: bounded fd leak on the zero-length-datagram rejection path; F2: racy new endpoint test) are recorded below with minimum corrections; neither reopens G2-02.

## Source assessment

Implementation: `src/Gagamba.Execution.Linux.M1/Native/FdPassing.cs` (334 lines), `Native/DomainHandleTransfer.cs` (143 lines), `Lifecycle/M1RecoveryScope.cs` (`AllocateDomain`, lines 154–209; spawn close, lines 269–277), `Owner/Program.cs` (receive lines 79–92, decode/validate lines 94–127).

- **Socket creation** (`FdPassing.cs:59–84`): exactly one `socketpair(AF_UNIX, SOCK_SEQPACKET, 0, sv)` call site in all of `src/` (verified by sweep; no `SOCK_STREAM` token remains anywhere in `src/` except a comment naming the rejected alternative). Failure (`rc != 0`) or `DllNotFoundException` returns failure with no fallback. Child end is left inheritable, parent end set `FD_CLOEXEC`.
- **Send path** (`:87–142`): one `sendmsg` of the exact payload plus the supplied fds; requires `rc == payload.Length`, else refuses. `AllocateDomain` always supplies the 51-byte `DomainHandleTransfer.Encode` output.
- **Receive path** (`:147–292`): 51-byte data buffer; control buffer sized exactly for 7 fds; `recvmsg` with `MSG_CMSG_CLOEXEC`; `rc < 0` and `rc == 0` refuse; `MSG_CTRUNC` refuses (after best-effort close of delivered fds); `MSG_TRUNC` **or** `rc != 51` refuses with distinct errors; exact `SCM_RIGHTS` level/type required; exact count/layout required (`cmsgLen == 16+4*count`, `controllen == 16+Align4(4*count)`, `count == 7`); every received fd verified `FD_CLOEXEC` (else all closed, refuse); then an `ioctl(FIONREAD)` pending-frame check refuses on `ioctl != 0` or `pending > 0` (closing all received fds first).
- **Decoder** (`DomainHandleTransfer.cs:64–142`): exact-length 51 required before magic/version/zero-flags/execution-UUID/lease-UUID/count/reserved/exact-ordered-roles checks.
- **Owner chain** (`Owner/Program.cs:80–129`): production receive → unconditional bootstrap-endpoint close → decode → validate → `DomainLease`; decode/validate failure closes all received fds and exits 4/5. No `DomainLease` is constructible from a refused transfer.
- **Lifecycle**: scope closes `parentEnd` in `finally` after send (`M1RecoveryScope.cs:188–194`), closes `childEnd` after spawn (`:269–277`); owner closes `bootstrapFd` unconditionally after receive. No filesystem pathname socket, no reconnect/second-read path exists.

## Exact original counterexample

Independent reproduction through the **actual production methods** (`FdPassing.TrySendTransfer`/`TryReceiveTransfer` via reflection against the built `b80` DLL) with **seven real cgroup v2 descriptors** (parent, domain dir, kill, events, memory.max, memory.swap.max, pids.max opened from a live domain) and the valid 51-byte payload plus trailing `0x55`:

```text
INFO OLD sentBytes=52 received=False rerr=overlong message: record exceeds the exact 51-byte payload domainFds 5->5
```

i.e. `sentBytes=52 received=False decode=N/A(valid prefix never trusted) domainValid=False`, fd-neutral (5→5). The previous result (`received=True decode=True domainValid=True`) is no longer possible. Full production chain on the valid 51-byte transfer accepts (`send=True recv=True decode=True valid=True`, all received fds `FD_CLOEXEC`). Observed identically as root and as uid 1000 (see evidence classes below).

## Message-boundary assessment

`SOCK_SEQPACKET` structurally closes the original stream-prefix defect on this host, verified rather than inferred:

- Raw-socket ground truth (no repo code): `SOCK_STREAM` 52→51-buffer recv returns `rc=51, flags=0` with the extra byte left queued (original defect mechanism reproduced); `SOCK_SEQPACKET` 52→51-buffer recv returns `rc=51, flags=MSG_TRUNC`, record fully consumed (`FIONREAD=0`, second recv `EAGAIN`); exact 51→`rc=51, flags=0`; 4096-byte record→`MSG_TRUNC`; 8 fds into 7-sized control buffer→`MSG_CTRUNC`; `SO_TYPE==5` confirmed.
- Through production code, the overlong error is exactly the `MSG_TRUNC` branch (`"overlong message: record exceeds the exact 51-byte payload"`), proving the kernel set `MSG_TRUNC` (with a production-sized control buffer, `MSG_CTRUNC` is not co-set).
- Boundary matrix through production code (real fds): 51 accept; 0/1/50/52/60/2099-byte refuse; malformed-51 passes transport then fails decode; all refusals fd-neutral.
- Two-record cases: valid+arbitrary refuses (`extra queued frame (2 bytes)`); two valid refuses (`extra queued frame (51 bytes)`); post-accept close makes a late send fail (`EPIPE`) and no second authority obtainable.
- A valid prefix of an invalid larger logical transfer can never be accepted: overlong single records refuse via `MSG_TRUNC`; multi-record transfers refuse via the pending-frame check.

## FIONREAD / extra-record assessment

- **Actual host semantics** (measured): with one record queued, `FIONREAD` equals that record's length (52, 51, 4096 cases); with two records (51+9) queued it returns the **total** (60), dropping to the next record's length (9) after one recv; with a zero-length record queued it returns **0**. So the code comment ("reports the next record's length … 0 means no record is queued") is imprecise in two edges.
- **Correctness of use**: the decision (`refuse iff ioctl != 0 or value > 0`) is correct regardless — any unconsumed bytes refuse, and the total-vs-next distinction cannot cause a false accept. A zero-length second record (with or without descriptors) passes the check, but both endpoints close immediately after handoff and no second `recvmsg` ever runs, so its bytes/descriptors die with the socket and cannot extend or alter accepted authority (demonstrated: 2REC-C).
- **Race**: a second send landing between the receiver's `ioctl` and its endpoint close leaves a queued, never-read record that is destroyed on close. No authority publication, alteration, or later-state survival is possible. Not security-relevant.
- The mechanism is **required** for G2-02 in the multi-record dimension (without it, `valid+extra` would accept the first record and silently drop the second); the single-record overlong dimension is already closed by `MSG_TRUNC`.

## SCM_RIGHTS assessment

Through production code with real descriptors: 6 fds refuse (`descriptor count/layout mismatch (count=6, controllen=40)`); 7 accept; 8 refuse (`MSG_CTRUNC: control data truncated`); no-ancillary refuses (`no SCM_RIGHTS control message`); bogus level/type refuses; ancillary truncation refuses. All are fd-neutral (before/after `/proc/self/fd` domain-target counts equal). Received fds verified `FD_CLOEXEC` by `fcntl(F_GETFD)`; exec probe (`/bin/sh` child) shows control fd present, received authority fd absent. Exception: see F1.

## Endpoint lifecycle assessment

No filesystem pathname socket; single `socketpair`, inherited/private; no reconnect path. Sender end closed after handoff (scope `finally`); receiver end closed after acceptance **and** after rejection (owner unconditional close; failed-handoff owner test passes: `ERR receive`, exit 4). Deterministic live-owner check (reviewer `LiveCheck`, real owner held at REGISTERED): 55 open fds enumerated, bootstrap number absent — endpoint closed after successful handoff. Parent end and all received descriptors are `CLOEXEC`; no bootstrap descriptor enters any target/Bubblewrap map (WP04 not begun); no release authority is representable on this channel.

## Regression assessment

- G2-01: `ScopeDisposalDoesNotCloseReusedDescriptor`, `RepeatedScopeDisposalIsSafe` pass; ownership/close paths untouched by the R6B diff. Not regressed.
- G2-09: `ValidHandlesValidate`, `ParentAsChildRefused`, `SwappedControlsRefused`, `NullDescriptorsRefused`, `SiblingControlRefused`, `MissingCloexecRefused` pass; role/identity code untouched by R6B. Not regressed.
- Full M1 suite root: 275 passed, 17 skipped, 1 failed — the single failure is the author's new `BootstrapEndpointClosedAfterSuccessfulHandoff`, root-caused to a test race (see F2), with the underlying product property verified deterministically. Non-root delegated suite: 16/16 pass.

## Independent evidence

- **Reviewer-run root/native**: raw kernel probes (`raw_seqpacket.py`, all PASS); production-path matrix (`ProdProbe`, 30/31 checks PASS — sole FAIL is F1); `LiveCheck` live-owner endpoint closure PASS; repo suites from a pristine `git archive b80e9c2` export: DomainAuthorityTests 19/19, RecoveryHarness 17/18 (1 racy test, see F2), full M1 275+17+1.
- **Reviewer-run non-root delegated** (existing `gaga` user, no provisioning, no sudo): `ProdProbe` under `/sys/fs/cgroup/gaga-delegated` — identical results including decisive OLD refusal; `NonRootQualificationTests` 16/16 with `GAGAMBA_DELEGATED_CGROUP` set. Inventory: Uid/Gid 1000/1000/1000/1000, Groups 1000, CapInh/Prm/Eff/Amb all `0000000000000000`, CapBnd `000001ffffffffff`, NoNewPrivs 0, cgroup `0::/init.scope`, pre-existing `gaga:gaga` parent with `cpu memory pids` subtree control. (NoNewPrivs=0 differs from the prior review's `setpriv` invocation; the zero-capability unprivileged status is what matters and is recorded honestly.)
- **Reviewer probes/source inspection**: as above; blob-hash-verified export.
- **Author evidence inspected only**: WP03 §19 report, the six new framing tests and two endpoint tests (all confirmed to use the real transport, real `recvmsg`/`SCM_RIGHTS`, and the exact `+0x55` counterexample — except F2's observation race), and claimed counts (root 277+16 vs reviewer 275+17+1 explained by F2; non-root 16/16 reproduced).

## Findings

**F1 (non-blocking hygiene defect, pre-existing — not introduced by R6B): `FdPassing.cs:185–189`.** A zero-length datagram carrying `SCM_RIGHTS` descriptors is refused (`rc == 0 → "peer closed…"`), but the early return runs before `CloseControlFds`, leaking the installed descriptors (measured deterministically +5 domain-target fds per occurrence, root and non-root; leaked set = the five control descriptors; all `CLOEXEC`). Every other rejection path is fd-neutral. Consequence: bounded process-lifetime fd leak only — the transfer still refuses, no `DomainLease` is published, the leaked fds stay in the refusing (intended-recipient) process which exits in the production flow, and the legitimate sender (`AllocateDomain`) always sends exactly 51 bytes so the path requires internal-API misuse or a crafted sender. It does not violate the disposition criteria (fail-closed transfer, no hidden-byte acceptance, no ambiguous authority). **Minimum correction**: parse-and-close control fds on the `rc == 0` path too (safe on genuine peer-close: zeroed control buffer yields a negative count and closes nothing); add a zero-length-with-fds rejection-cleanup regression test. Do not redesign unrelated components.

**F2 (author test race, not a product defect): `RecoveryHarness.cs:361` (`BootstrapEndpointClosedAfterSuccessfulHandoff`).** The test enumerates `/proc/<owner-pid>/fd` after the `after-registered`-barrier owner has exited, producing `DirectoryNotFoundException` (zombie has no fd dir) — 3/3 failures on this host. The product property (bootstrap close after successful handoff) was verified deterministically by holding a real owner alive at REGISTERED and is TRUE. **Minimum correction**: keep the owner alive during enumeration (barrier `none` + `die` after inspection, as in the passing `OwnerHoldsPrimaryLeaseAndStopsDomain` pattern), or treat post-exit `DirectoryNotFoundException` as inconclusive rather than proof either way.

## Review questions — explicit answers

1. Is the transport genuinely SOCK_SEQPACKET? **Yes** (`getsockopt(SO_TYPE)==5`, single creation site, no `SOCK_STREAM` in `src/`).
2. Is there any SOCK_STREAM fallback? **No** (creation fails closed; no fallback code exists).
3. Can a 52-byte logical record ever be accepted as a valid 51-byte transfer? **No** (`MSG_TRUNC` refusal, verified with real fds).
4. Does the exact old +0x55 reproduction refuse with real cgroup descriptors? **Yes** (root and uid-1000; fd-neutral).
5. Is MSG_TRUNC handled correctly? **Yes** (checked, distinct error, descriptors closed).
6. Is MSG_CTRUNC handled correctly? **Yes** (checked first, descriptors closed, fd-neutral).
7. Are extra descriptors rejected and closed? **Yes** (8-fd case fd-neutral).
8. Are received FDs CLOEXEC? **Yes** (verified + exec probe).
9. Does a second logical record affect accepted bootstrap authority? **No** (refused if queued; destroyed unread if late).
10. Is the FIONREAD interpretation correct? **Decision-correct; comment imprecise** (total-vs-next; zero-length edge) — no safety impact.
11. Is any FIONREAD race security-relevant? **No** (endpoints close; no second read exists).
12. Did the framing change regress G2-01 descriptor ownership? **No**.
13. Did it regress G2-09 role/domain validation? **No**.

G2-02 RESOLVED - R6B ACCEPTED
