# B6-S3 trusted installation identity propagation — resumed implementation

Date: 2026-11-04 (Asia/Manila)
Slice: GAGAMBA-RC2B-06-B6-IMPL-01-S3-RESUME-01
Baseline source: `1fa53da68704210a9ecd7fbd2c65aa5c1ed270ae`, tree `d1110fd9aceee105154399a005928c7794472964`
Final implementation/test source: `5b2fe73567a9c33df50fc7c662436a67814a86f4`, tree `170f3e4913447f8b5bd1d2b1fa0f7e92e16877d9`
Branch/check-out: canonical `main`, `C:\Users\Laszlos\source\repos\Gagamba`
Host: Windows NT 10.0.26200 x64, .NET SDK 10.0.401; non-root WSL2 x86-64, kernel 6.6.114.1-microsoft-standard-WSL2, glibc 2.43, SDK 10.0.114, runtime 10.0.14
Native prerequisites: actual AF_UNIX seqpacket, SO_PEERPIDFD, SCM_CREDENTIALS, pidfd, CLOEXEC and posix_spawn exercised as uid/gid 1000; fixed installation root unavailable/unqualified
Scope: source implementation, deterministic behavior, native components, legacy regression and production binary separation
Exclusions: privileged installation, full installed/helper topology qualification, complete native loader closure, descriptor v2, S4/pre-RELEASE generation enforcement, production activation, packages and push.

**S3 PARTIALLY IMPLEMENTED.** The protocol and qualified-path implementation is committed and the available component/regression runs below pass. Complete B6-T20b production-helper verification and all required subcases of the 28-row qualification matrix are not established. This is a source-bound checkpoint for `GAGAMBA-RC2B-06-B6-IMPL-01-S3-Q1`, not a claim that all 28 full-system cases passed or that S3 owner acceptance is ready.

The historical B2 transport blocker is **resolved by the accepted amendment**, not a new blocker. Its original report and source map are preserved below and in the raw archive. No new wire design or recovery-forwarding alternative was selected.

## A. Source binding, preflight and preservation

Initial HEAD/tree matched exactly, tracked tree/index were clean, and live and tracking `origin/main` both resolved to `401e1ea7f0be06b27bc188c827dd951016b3c62a` (11 behind local main). All 11 preceding local commits remain ancestors. The initial 44 untracked files were inventoried by literal path and SHA-256; all retained their bytes through implementation. All 74 worktree registrations were compared, allowing only canonical main's HEAD to advance. No branch/worktree creation, reset, rebase, stash, clean, push or privileged operation occurred.

The accepted amendment, qualification matrix and independent review were rehashed from committed blobs against the owner decision's `88beb1a2…`, `6c7daf1d…` and `23405a8f…` values. S2 acceptance, F02, the original wire-owner decision and the original S3 commission were read. F02's administrative publication policy, receipt-selected runtime qualification and S2 manifest hashing were retained.

| Checkpoint | Commit | Tree |
|---|---|---|
| Identity, codecs, qualified helper gates and native reaper | `98e1960fecc5a6de82591df0632b7d6cb316f2e5` | `32a673c56ce5daea5c2e58d372405b8c5026a4c3` |
| Mapped-file checks, custody shapes and native controls | `7e31616bd8c5f3c8251935df2bfa59a4cc9bafc0` | `17b5e0ddac6b27a7f2e14bbdd16d04c5122298f1` |
| Qualified verification off safety readers; echo caps | `4230c9ef48bc094417f00f5682fd240b9b525010` | `e925a2a91517abba8d21c0b363c3746ffc46b0cf` |
| Premature evidence refusal | `294145d5cdd67153f0eb5545535fbbe4ddadfec4` | `4cba59155dc48ac3ba0c6f7bafa379f4f1fc4fb6` |
| Exact descriptor cleanup, replacement-object and entry-point controls | `f6565de877f26fe699da89d7857b22435e1bd31d` | `9d6282705c9761f05c1976890099979fec0f9d53` |
| Preserve original integrated writer handoff | `5b2fe73567a9c33df50fc7c662436a67814a86f4` | `170f3e4913447f8b5bd1d2b1fa0f7e92e16877d9` |

[Source, changed-file inventory, committed blobs, test counters and preservation hashes](rc2b-06-b6-impl-01-s3/source-binding.json). The enclosing evidence commit identifies the final documentation receipt; it changes no implementation/test source.

## B. WI-01–WI-06 implementation and limits

| Obligation | Delivered implementation | Evidence/remaining gate |
|---|---|---|
| WI-01 | Internal `InstallationIdentity`; S2 `InstalledResult` retains manifest, immutable projection and qualification ID from the same VerifyActive pass. Full v2 identity in integrated bootstrap; B2 identity/deadline in its accepted separate fields. | Projection/validation exercised. Actual Qualified installed selection and loaded-role positive/mutation controls pending. |
| WI-02 | Strict v1/v2 distinction, recursive duplicate-key refusal, unmapped-member refusal, canonical numbers/digests, required and mutually exclusive identity locations, unchanged 8192-byte cap. Qualified delegated B2 refuses through the actual startup entry before filesystem/native allocation. | Portable/production-core tests pass; both v1 wire meanings retained. Delegated-parent v1 native domain cases pending operator delegation. |
| WI-03 | Direct single-use B2 evidence listener; 24 then 38/11; kernel peer plus actual-record credentials; 37/36; controller confirmation 12; recovery Ready barrier and separate final controller S2/live-pin gate. | Codecs, gates, native peers, rights, half-close, premature data, custody and concurrent inheritance exercised. Full qualified three-actor chronology and loss/stalled-worker matrix pending. |
| WI-04 | Expected identity travels in authenticated recovery bootstrap and owner specification; existing DirectAssociation/DirectBootstrap/GID1 stay in place. Owner verifies locally before journal writer acquisition; WriterReady retains its original writer-held meaning. Direct echo 36 precedes StagedObjects. | Legacy/direct authentication regression passes; complete qualified integrated run pending. |
| WI-05 | Exact narrower shapes and 512-byte ceiling; separate namespaces, topology/direction restrictions and existing per-direction sequencers. Owner 36 is DirectControl/ForbiddenRecovery; 37/38 cannot use generic control/transfer dispatch. | B6-V13 component tests and actual Release-core tests pass; no new recovery type 13 or generic nested owner decoder. |
| WI-06 | Production Release builds/metadata/IL probes, actual Release-core portable and native component runs, explicit fixture separation. | **Incomplete:** actual root-owned production Owner/Recovery full qualified B2/B3 topology qualification remains pending. |

Only argument-free fixed-root `InstalledFileSystem.VerifyActive()` creates the trusted selection used by qualified startup. A wire `Qualified=true`, the public artifact digest, a parsed candidate or the synthetic probe identity cannot create a `LoadedInstallation`. There is no injected production trust source or allow-unqualified switch. Every qualified process re-enters S2 locally. Before confirmation 12 and final B2 return, C checks the same digest, qualification ID and full projection again; no new generation is adopted.

Role hashes use SHA-256 over ASCII `gagamba-b6-role-v1`, NUL, role byte (0 R/1 O), then the five raw SHA-256 values in `contract,deps,entry,m1,runtimeconfig` order. Generation is the full artifact-set digest, distinct from the header's lease GUID. Coordinator/filter/runtime fields come from the same verified S2 manifest.

## C. Wire layouts and sequences

All frames retain the 48-byte LE OwnerProtocol v1 header; recovery XORs octet 0 with 0x20. B2 has no GID1 wrapper. [Full payload and frame golden hex, including expected trace counters](rc2b-06-b6-impl-01-s3/golden-wire-vectors.json). These vectors are synthetic expected bytes, not a captured complete qualified run.

| Message | Exact payload | Rights |
|---|---|---:|
| Recovery 9 | 76: set[32], R closure[32], three LE u16 protocols @64/66/68, role=0 @70, qualified=1 @71, zero u32 @72 | 0 |
| Recovery 10 | 36: success/reason @0/1, zero u16 @2, expected set[32] @4; only (1,0) or (0,1..5) | 0 |
| Owner 38 / Recovery 11 | 40: session Guid @0, C/R/O LE i32 PIDs @16/20/24, zero u32 @28, absolute monotonic LE i64 D @32 | 1 original C / O pidfd copy |
| Owner 37 | 219 + profile/version lengths: session @0; six raw hashes @16..176; four u16 constants @208..214; qualified=1 @216; u8-length exact ASCII profile/version from @217 | 0 |
| Owner 36 | 76: same echo projection as 9, owner closure and role=1 | 0 |
| Recovery 12 | 48: original session Guid @0 and expected set[32] @16 | 0 |

Canonical B2 echoes have no optional tail. The integrated decoder also constrains its supported optional profile tail to the exact compiled profile/version. New IDs are not made generic descriptor-transfer roles. All prior numeric identities retain their meanings. Wrong magic/version/role/direction/topology, exact shape, transaction binding or sequence terminates that positive path.

Expected successful B2 counters: C→R `1.1,10.2,12.3`; R→C `9.1,11.2,2.3`; R→O `23.1,38.2`; O→R `24.1`; C→O `37.1`; O→C `36.1`. Each direction owns its existing sequencer. No resynchronization/reconnect path repairs refusal.

## D. Process-pin custody and actual sender authentication

Qualified spawning uses posix_spawn file actions with child-local fd mappings, atomic CLOEXEC socket creation, CLOEXEC duplicate sources and close-from in the child. Only intended 3/4 mappings and standard streams survive. The original Process object and safe handle are sealed with its retained pidfd; a new Process wrapper for the **same PID**, an unrelated object or a second export cannot replace it. Native single/concurrent spawn tests exercise this custody and exclusive inheritance.

R retains original C and O references. It exports borrowed copies under custody gates after the exclusive 24 exchange. MSG_CMSG_CLOEXEC, exactly one right, fd >2, actual pidfd identity and liveness are required before detach. Every received right remains record-owned until validation and closes on error. Queued copies close with the receiving socket. C retains both helper pins through final selection/Ready checks; O retains its original R stop reference.

B2 authentication checks both the live socket-derived SO_PEERPIDFD against the retained original reference and SO_PEERCRED, plus exact SCM_CREDENTIALS on each received 37/36. The implementation checks procfs, non-root UID/GID, zero effective/permitted capabilities, distinct PIDs, original parent chain and common PID/user/time namespaces, bracketed by live observations. Both proof categories are necessary; neither matching bytes nor matching numeric PID supplies the other.

The native fixture deliberately hands an established owner's socket to another process. The peer remains the original child but its writer's credentials differ. Final raw observations show `Accepted=false`, `Refusal="actual evidence sender refused"`, original child still pinned/CLOEXEC and then reaped with exit 0. The valid control accepts. Early queued 36 refuses before sending 37. Empty packets (including rights), extra records, unknown roles and attached rights refuse. Exact tracked descriptor counts account for listener closure and sentinel-directory rights; no copy is inferred closed solely from process disappearance.

## E. Source chronology, loaded closure and safety

**Qualified B2, source order:** C derives S2 expectation, creates fresh session/listener and absolute D, spawns/pins R, authenticates Associate; R's bounded verification worker verifies S2/actual loaded role and prepares installed O launch, then 9/10 completes. R spawns/pins O on the original private lifeline, validates 24, sends 38 then 11. O authenticates C; C authenticates O before 37. Both credentialled records and peer shutdown/drain are checked. O locally verifies its loaded closure before 36. C validates 36, rechecks S2, sends 12, validates exact clean Ready, then rechecks S2, both live pins, D and pending control before its local commit. No diagnostic file grants readiness.

**Qualified integrated B3, source order:** 9/10 before rendezvous 8; O locally verifies its role from the trusted R bootstrap before acquiring its journal writer; WriterReady still follows actual writer/intent proof. Existing DirectBootstrap 32 and GID1 authenticate the original session/processes. With its independent safety monitor armed, O rechecks installed/loaded identity and sends direct 36 first; C validates before accepting StagedObjects. No B2 evidence channel, 11/12/37/38, or new RELEASE route is used.

Loaded checks force both private dependencies, require the actual entry/private assembly Locations and Release identity, hash protected pinned role files, and compare mapped device/inode identities. Helpers check actual deps selection and explicit runtimeconfig/deps command-line bindings, host, framework and loader. Unexpected helper assemblies, empty/dynamic locations and foreign persisted native code mappings refuse. Required managed IL mappings are checked even without execute permission; CLR-generated memory/IPC is not treated as a disk artifact or claimed to be attested. The trusted embedding controller application is outside the provider's two-file role. Full native package/dlopen/JIT/loader qualification remains open.

One absolute B2 D is bounded by 7000 ms and any earlier bound; echo work is capped at 5000 ms, 24 at 3000 ms. Role/runtime hashing is on a bounded single-worker path rather than a post-24 safety reader. A stuck worker blocks further verification attempts or dies with its helper; it does not own stop service. Existing 30/35 s lifetimes and once-only two-second stop obligation remain. Diagnostic writing uses one background writer and one queued item; it never delays safety/reaping. Startup failure after O could exist closes control/requests stop and leaves detached R responsible for stopping/reaping.

These are **implementation/source chronologies**. No full Qualified B2/B3 installed trace was executed here. Parent/namespace/closure races and stalled-verifier loss cases need the remaining qualification work below.

## F. Executed results and raw evidence

| Executed scope | Pass | Fail | Skip |
|---|---:|---:|---:|
| Windows complete M1 Debug | 1164 | 0 | 290 |
| Windows complete M1 Release runner (explicit Debug dependency, as S1 requires) | 1164 | 0 | 290 |
| Supplemental actual Release M1 | 1133 | 0 | 220 |
| S1 build/binary probes | 51 | 0 | 0 |
| WSL2 actual Release-core S3 batch | 71 | 0 | 0 |
| WSL2 legacy B2 process suite, non-delegated | 22 | 0 | 0 |
| WSL2 direct/safety/C3/S2 native-file mixed subset | 91 | 0 | 0 |
| WSL2 nonprivileged S2 transactions | 11 | 0 | 0 |
| Six other baseline Debug projects combined | 192 | 0 | 20 |
| D6.4 Debug / Release | 3 / 3 | 0 / 0 | 0 / 0 |
| Fixture SelfTest | 15 | 0 | 0 |

The 71-case S3 batch contains **51 portable deterministic cases and 20 native component cases**; running portable cases on Linux does not turn them into native handshake qualification. B6-V13's wire/projection/role components are covered by 24 portable `InstallationWireTests`; 21 `InstallationHandshakeTests` exercise actual production barrier policy, and six mapping-classification cases complete the portable subset. The 20 native cases cover evidence sockets/records, typed pin refusal, original/concurrent spawn custody/reaping and actual CLR map observations. B6-T20b remains **partially verified**, with no complete production-helper positive control.

Against canonical S2 producer counts, +51 portable passes and +20 native Windows skips explain 1113/270 → 1164/290 and supplemental 1082/200 → 1133/220. The supplemental runner retains its existing five hook-dependent file exclusions: 31 passes/70 skips, accounting for the 101-case difference; assertions still run in the full suites. The external Q1's 1141 count included 28 reviewer probes not imported as canonical tests. The 22-case B2 command did not discover the two delegated-parent native domain cases because no operator delegation was supplied: **2 not run**, not 2 passes or hidden skips. Full integrated native confinement/HELD qualification was not replayed. All 61 existing B5 C3 cases pass in the three M1 contexts and selected Linux batch.

Actual Release native components load Release M1 with its production binary gate; the probe is a separate explicit synthetic-identity executable and cannot create a trusted installed result or RELEASE capability. The cross-OS supplemental runner uses the preserved same-entry Linux test apphost stub (`b2dd7d8c…`), not a substituted helper. Final binary hashes, precise TRX class counts, commands and host observations are retained.

Native reaping exposed a real defect during development: a Process.GetProcessById wrapper after posix_spawn cannot provide .NET child-reaping ownership. The failing native test reported “owner was not reapable within the bound.” QualifiedLauncher now retains original-object custody and uses bounded native waitpid with a cached, once-reaped exit result. The same actual spawn/export/reap control passes after repair. Initial process-wide fd-count assertions were also corrected to serialized/exact scoped observations. Failed logs remain in the archive; they are not silently counted as passes. The first Linux source build also hit the existing nested Windows/Linux apphost-output collision; portable IL plus the native test launcher was then used without disabling binary gates.

[Raw logs, TRX, failed-before logs, native process/descriptor facts, commands and preservation snapshots](rc2b-06-b6-impl-01-s3/verification.zip) · [Actual binary hashes](rc2b-06-b6-impl-01-s3/binary-inventory.json).

## G. All 28 adversarial-case dispositions

**D** = deterministic component behavior executed; **N** = actual unprivileged native component behavior executed; **L** = applicable legacy regression executed. **Pending** identifies unexecuted obligations, not a pass. These rows do not aggregate into “28 passed.” The accepted matrix's full prerequisites/control scope remains governing.

| # | Executed evidence | Remaining disposition |
|---:|---|---|
| 1 | D positive controller/recovery barriers; N positive 37/36, peer/record credentials, spawn/export/drain/stop-related primitives | **Pending Q:** complete qualified C→R→O sequence, actual loaded closures and Ready/Stop trace. |
| 2 | N wrong original pin; unrelated and replacement-same-PID Process objects refuse; one-shot export positive | **Pending N/Q:** same-UID sibling with correct installed bytes connecting in original O's place, full original-spawn association. |
| 3 | N correct digest from different actual writer on the original peer socket refuses; D wrong envelope/type 13 refuses | **Pending N:** actual R connector and both forwarding attempts on full control. |
| 4 | D/N wrong owner closure echo refuses | **Pending Q:** actual five-file/loaded-location mutation with valid installed positive control. |
| 5 | D/N wrong artifact-set echo refuses; S2 native payload mutation control | **Pending Q:** another installed artifact changes with otherwise valid role closure. |
| 6 | D/N wrong execution GUID refuses | **Executed component scope**; full qualified pipeline control remains case 1. |
| 7 | D/N wrong protocol lease GUID refuses | **Executed component scope**; full qualified pipeline control remains case 1. |
| 8 | D generation/set validation and no-reopen barrier behavior; source revalidation before 12/final return | **Pending Q:** selector swap at both windows and stale active SHA with current lease. |
| 9 | D duplicate/prior-session phases and sequencer replay; N duplicate actual 36 refuses during drain | **Pending N/Q:** stale connection/prior-session frame and alternate writer variants in the full handshake. |
| 10 | D recovery namespace/type-13 and repeated binding gate refusal | **Pending D/N:** complete nested-forward, duplicate 11 and duplicate 12 record matrix. |
| 11 | D XOR/owner/header identity/version/direction isolation; N wrong execution/lease frame | **Executed component scope**; integrated cross-wrapper injection remains in case 26. |
| 12 | D owner generic-role/transfer refusal; N ReleaseRequest on evidence refuses | **Pending N:** actual 32/38/unknown/wrong-sender-37 injection variants. |
| 13 | D truncation/reserved/qualified mutation loop; N zero-byte record and zero-byte rights refuse | **Pending N/Q:** exhaustive native truncated-header/identity variants and full progression observations. |
| 14 | D exact lengths/profile-tail/oversize; N oversize and rights-bearing records refuse with exact cleanup | **Pending N:** explicit ancillary truncation/unknown cmsg and full profile-bound variants. |
| 15 | N ordinary directory instead of typed pidfd refuses/closes; actual original-pidfd transfer/CLOEXEC/export control | **Pending N:** file, pipe, socket, stale, zero/two-reference transfer substitutions in 11/38. |
| 16 | N original-peer mismatch, actual wrong writer and same-PID replacement object refusal; existing direct-auth checks | **Pending N/Q:** full lineage/reparent/UID/GID/time-namespace/substitute-pin matrix. |
| 17 | D original deadline; N missing record times out | **Pending N/Q:** delayed 24, missing half-close, EINTR and final-byte/expiry races across complete helpers. |
| 18 | D OwnerDeath latches at every controller phase; L actual owner-loss stopping | **Pending N/Q:** qualified O death before/queued 36 and after 12 before final C commit. |
| 19 | D RecoveryDeath latches; L actual R death/lifeline behavior | **Pending N:** death around 38/11, unread queued rights and helper/descriptor observations in qualified handoff. |
| 20 | D ControllerDeath latches; L actual C loss, stop and reaping | **Pending N/Q:** C death during blocked qualified hashing/after 36/after 12. |
| 21 | D missing 12 or Ready cannot complete; response pairing/type checks | **Pending N:** separate actual dropped 10, 12 and Ready with no O/no R Ready/no C result observations. |
| 22 | D lifeline/transfer/stop preconditions; N queued 36 before 37 refuses; L independent lifeline loss/stop | **Pending N:** suppressed 24, premature Ready and 25/26 while qualified verification is deliberately stalled. |
| 23 | N rights on evidence, including empty record, close without becoming authority | **Pending N:** harmless writer-sentinel pipe and overflow/transfer replacement variants. |
| 24 | D recovery has no release message/authority; N ReleaseRequest on evidence refuses; existing release/C3 regression | **Pending N/I:** all extension edges and actual integrated one-attempt physical-RELEASE control. |
| 25 | L 22 non-delegated v1 cases; D actual qualified delegated startup refuses before native/filesystem entry | **Pending L:** both delegated-parent v1 domain cases; full qualified delegated allocation observation. |
| 26 | D topology bans 11/12 on integrated wire and 37/38 on DirectControl; L native GID1/direct authentication | **Pending I/Q:** complete baseline/qualified integrated staging/HELD/release run and B2-only injection matrix. |
| 27 | Existing 61 C3 cases; D first-stop/deadline preservation; L repeated stop/replay/native loss controls | **Pending L/Q:** qualified handshake interleaving with existing durable domain deadline and delayed helper exit. |
| 28 | D clean-Ready/stop/Unknown gates and existing cleanup/uncertainty tests; L missing acknowledgement behavior | **Pending L/I/Q:** all specified missing-proof/cleanup-identity mutations during complete qualified loss. |

## H. Authority preservation and remaining qualification

No recovery RELEASE message, physical writer, bearer or admission callback was added. B2 evidence exposes only 37/36 and zero rights. Only the two borrowed process-reference copies travel on 38/11. RecoveryObligation.ReleaseAuthority remains false; the existing physical RELEASE writer/attempt limiter, C3 durable deadline, journal schemas and public unavailable production factory are unchanged. Source binding inventories show no installer/qualification-store or physical RELEASE implementation changes. B1–B5 tests and conservative Unknown/first-cause behavior remain evidence at their recorded scopes.

The final read-only installed verifier returns **`Unqualified Directory is group/other writable.`**, exit 1. `/` is still `0:0 0777`; `/opt/gagamba` is absent. No permission was changed and no unqualified result was converted to authority. Final process observation found no test-owned Owner/Recovery/B2Controller helper processes remaining. Known fixtures were reaped; private unprivileged build/candidate copies and raw logs are intentionally retained for review, not claimed removed.

Remaining work includes the pending cells above, controlled isolated full-handshake tests with legitimate positive controls, actual production Owner/Recovery loaded-closure mutations, original three-actor lineage and stopping during stalled workers, complete integrated native regression and both delegated v1 domain cases. A suitable separately authorized trusted-root installation is additionally required for Q-class results; the preserved S2 14-artifact digest does not qualify rebuilt S3 binaries. Full native loader/dlopen closure, descriptor v2, complete pre-RELEASE generation enforcement and activation remain separately gated. No S4 was begun.

## I. S3-Q1 handoff

Review the exact final source and independently construct adversarial probes. In particular challenge S2 projection provenance, mapped/loaded role proof, the two kernel authentication checks, original-process custody, ancillary disposal and EOF distinction, post-spawn R survival, stop service under hashing, writer-held WriterReady preservation, final C/R readiness gates and all pending matrix mutations. The trusted launcher/host/kernel boundary is an explicit premise; this does not claim Byzantine-launcher or administrator resistance.

Do not treat green component counts, copied identity bytes or this implementation report as qualification/owner acceptance. **WI-06 and complete B6-T20b/matrix qualification remain open.** The current disposition is therefore **S3 PARTIALLY IMPLEMENTED**, with a concrete locally committed checkpoint and evidence for the next independent review/remediation work.

---

## Historical blocker record — preserved, resolved by the accepted owner amendment

# B6-S3 source map and B2 owner-channel contract conflict

**S3 BLOCKED BY ACCEPTED CONTRACT CONFLICT.**

2026-11-03 · GAGAMBA-RC2B-06-B6-IMPL-01-S3. The user selected **Record the conflict and stop** after reviewing the B2 owner-echo transport question. This report preserves the inspected baseline and the required bounded wire decision.

## A. Source and environment

Canonical `C:\Users\Laszlos\source\repos\Gagamba`, `main`, origin `https://github.com/jenolaszlo-sketch/gagamba.git`. Starting commit `142cc2308dfbe7fbab5231e1db0a6fcecc9922e6`, tree `edc0100214e480185cb9b89faf201f540c8aadb7`. The enclosing documentation commit identifies the final report revision. Source at that commit is identical to the starting implementation. Windows NT 10.0.26200.0, x64, .NET SDK 10.0.401. Inspection and evidence-integrity checks ran on Windows. The tracked tree and index were clean at entry. Live and tracking `origin/main` both resolved to `401e1ea7f0be06b27bc188c827dd951016b3c62a`, eight commits behind the starting HEAD.

All 71 Q1 files were rechecked against original reviewer bytes and committed blobs. The 14-artifact candidate and its per-file hashes were rechecked. The original 44 untracked file hashes and 74 worktree registrations agree with the preceding acceptance inventory. [Source excerpts and Git blob identities](rc2b-06-b6-impl-01-s3/source-map.json)

## B. Accepted authority and conflict

The [wire decision](../decisions/rc2b-06-b6-wire-owner-acceptance.md), particularly WI-03, correctly identifies B2 mapping as an implementation obligation. The [wire delta](rc2b-06-b6-spec-wire-delta-01.md) assigns owner echo 36 to owner→controller and recovery echoes 9/10 to their separate namespace. The S3 commission additionally requires the B2 echo after `B2LifelineReady`, controller validation against independently expected identity, and refusal of owner echo 36 on recovery-only channels. These requirements preserve separate authentication and sequence domains.

The concrete B2 source creates the owner socketpair inside recovery. Recovery retains one end and passes the other to its child. The controller holds only the controller↔recovery channel. The B2 owner sends `B2LifelineReady` to recovery. Recovery then sends a recovery-envelope `Ready` snapshot to the controller. The owner-control loop accepts `B2StopObserved`. No owner endpoint, authenticated direct session or owner-echo relay is delivered to the B2 controller.

The accepted requirement that **the controller validate owner echo 36 on the inherited B2 owner channel** therefore lacks a controller endpoint in this topology. Forwarding raw owner frames onto the existing recovery control channel conflicts with its required envelope/direction gate. Recasting the owner echo as recovery request 9 conflicts with its prescribed role 0 and recovery-closure fields. Treating recovery verification alone as controller verification weakens the requested independent recipient check.

This is an unresolved transport/authentication decision for the qualified B2 extension. Legacy B2 remains supported within its accepted boundary. The finding does not establish a defect in its existing lifecycle behavior. The user directed a stop before selecting a new route.

## C–H. Implementation map

| Required integration | Actual entry and planned gate |
|---|---|
| Controller expected identity | `InstalledFileSystem.VerifyActive` and protected manifest/receipt-selected record. The returned trusted state currently exposes digest and qualification ID. S3 must derive the full identity from the same verified generation. |
| Recovery bootstrap | `RecoveryConfiguration.Decode/Validate` in `Bootstrap/RecoveryProtocol.cs`. Currently version 1 only. `IntegratedRecoverySpecification` has no installation field. |
| Recovery send and controller validation | Insert after Associate authentication in `RecoveryHost.RunCore`, before B2 Ready or entry into integrated preparation. `RecoveryConnection.Start` must validate request 9 and send response 10 before accepting Ready/Rendezvous. |
| B2 owner | `B2OwnerLifeline.Run` sends B2LifelineReady on the recovery-owned socket. Owner closure verification and echo must follow that event. The controller recipient/transport requires the decision above. |
| Integrated owner provenance | `DirectAssociation` and `DirectBootstrapPacket` under `OwnerDirectConnection.Accept` and `DirectSession.Establish`. Process identity currently has no artifact identity. A bounded authenticated extension is required. |
| Integrated owner echo | `OwnerPreparationTransaction` obtains the direct channel before StagedObjects. Its controller counterpart `IntegratedB3Client.Prepare` currently receives StagedObjects first. Both need the qualified echo gate. |
| Existing refusal and stop | `RecoveryObligation`, recovery Stop and pidfd monitoring, `OwnerSafetyMonitor` and `M1RecoveryScope`. Existing original deadlines, Unknown cleanup and owner-only RELEASE remain governing requirements. |
| Wire codecs | `RecoveryWire`, `OwnerProtocol` and existing sequencers. IDs 9/10/36, message-specific shape/direction rules and golden bytes remain to be implemented. |

WI-01 through WI-06 remain open for S3. Checkpoint S3-A has source mapping only. S3-B through S3-E have no implementation completion evidence.

## I–L. Verification and authority limits

No S3 codec, bootstrap, role-closure checker or handshake was implemented. B6-V13, B6-T20b, closure mutations and production-binary qualification were not executed for S3. Existing regression totals remain historical S2 evidence. The current source inventory is a static observation, with source blob identities and exact excerpts available in the linked JSON. It makes no native handshake claim.

The evidence checks read the preserved manifest, compared every original/canonical/committed file, inspected the original candidate archive without extraction, and compared the untracked hash inventory and worktree registrations. Their exit code was 0. Documentation preparation and committed whitespace checks validate this report only. The final handoff supplies the enclosing commit/tree and post-commit preservation result.

## M–N. Host and deferred gates

The accepted S2 observation of a WSL2 `0:0 0777` root remains a native qualification blocker. This commission performed no new Linux qualification run. Root-owned installation, native loader/runtime closure, passive descriptor v2, full pre-RELEASE generation enforcement and production activation remain separately gated. The runtime factory remains unavailable.

## O. Required decision and resumption

A focused wire amendment must select and specify the B2 owner→controller route after B2LifelineReady. The two proposed choices were an explicitly authenticated direct B2 owner/controller channel with bounded non-release descriptor passing, or a specified recovery forwarding contract.

The decision must state the recipient, expected-identity provenance, envelope and role rules, per-channel sequence ownership, timeout, failure/stop behavior and descriptor lifetime. It must preserve recovery’s independent lifeline, one physical RELEASE attempt, original deadlines and recovery’s lack of RELEASE capability. The owner echo itself remains identity evidence with zero transferred authority.

After that decision, resume S3-A through S3-E under the original commission, then produce source-bound golden vectors, handshake and loaded-closure probes, applicable regressions and production/test separation evidence. Independent S3-Q1 remains required after implementation. The present report is a blocker handoff.
