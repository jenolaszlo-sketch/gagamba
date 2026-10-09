# AR-7 decision after P01: eliminate LPAC + Job alone; resume independent feasibility

Date: 2026-10-09. Type: architecture review and next Sol handoff; **no implementation or new native experiment**.

Production remains frozen at `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, branch `qualification/ar6-native`, tag `ar6-review-baseline-06c8ac7`. Reviewed P01 source: `ee170724c14d8bd138cec1a461d86eb8bec76ec6`; completed feasibility/evidence revision: `bea39161985b4f2f134eb612d85082a493fc5421`. This decision follows those immutable records; it does not qualify another source revision.

**Decision:** eliminate the tested zero-capability LPAC + Job composition; keep Windows refusal for `restricted-offline-closure-v1`; resume Linux P02 independently. Permit one new Windows P01B feasibility candidate using a full restricting-SID access check composed with LPAC, with the strict stop rules below. VM-backed execution is the preferred fallback direction if P01B or a later mandatory Windows gate fails. No complete supported Windows-native composition has yet been established.

## 1. Interpretation of the P01 counterexample

The [P01 report](../evidence/ar7-P01-windows-lpac.md) and [external observations](../evidence/ar7-feasibility/P01-observations.json) demonstrate **ambient authorization and delegated execution**, not a missing test and not a Job breakaway:

- The proposed execution grant did not include the synthetic ARAP file or broker. Their object DACLs deliberately granted the broad `ALL_RESTRICTED_APPLICATION_PACKAGES` SID (`S-1-15-2-2`). The candidate could read the file and invoke the broker without an execution-specific grant.
- The trusted observer authenticated candidate PID 5736 at the ARAP endpoint. Broker child PID 32372 was outside the candidate Job, inside the independent outer cleanup Job, produced its marker and exited 0. A separate host process created the child; the candidate did not defeat direct-descendant Job inheritance.
- The exact unrestricted fixture reached all eight brokers. Candidate input reads and scratch writes worked. AAP access succeeded under ordinary AppContainer and failed under the LPAC opt-out composition. These controls make the ARAP result interpretable.
- Empty Jobs, absent endpoints/profile/workspace, and an independent process observer support cleanup. The unsupported LPAC boolean token query remains Unknown. `LOCAL` not-found results remain inconclusive. The caller was a filtered administrator at medium integrity, not a separately provisioned standard account.

Those limits do not rescue the candidate: successful ungranted global ARAP delegation is sufficient to falsify its claimed authority ceiling. No real-service exploitation claim is needed to reject a mechanism that fails the designed synthetic counterexample. Conversely, this result does not prove every native Windows composition impossible.

## 2. Original Windows candidate: definitively eliminated in its tested form

**Yes:** zero-capability LPAC + staged ACLs + Jobs is eliminated as sufficient enforcement of this common profile. It must not advance under a renamed acceptance criterion or return to the queue as merely needing more broker tests. AAP opt-out does not opt out of ARAP authority; Job ownership does not follow requests into another host process.

Adding limits, lower integrity, UI restrictions, child-process policy, network filtering or exploit mitigations does not establish denial of this ARAP file/pipe request. Disabling target child creation also does not disable child creation by a reachable host broker. A separate account alone does not exclude an endpoint deliberately admitting broad application classes plus Everyone. Rewriting host ACLs, adding capabilities, enumerating a few known brokers, or relying on user-mode API hooks is not an acceptable replacement boundary.

The original review already named an **extra restricted SID** under AR7-F02 and discussed restricted tokens/desktop restrictions in section D. Therefore P01B instantiates an existing alternative mechanism class; it is a distinct composition that was neither built nor qualified by P01. A custom system-call filter/driver, private token manipulation, process-silo implementation or privileged broker would be a different architecture requiring another decision, not an extension of this probe authorization.

## 3. One remaining native candidate: P01B, full restricting SID + LPAC

### Supported primitives, unproved composition

Microsoft documents `CreateRestrictedToken` with a restricting-SID list: access must satisfy both the ordinary and restricting access checks. This is an additional constraint, unlike adding an AppContainer capability. `WRITE_RESTRICTED` applies the added check only to writes and is therefore unsuitable for a profile that also denies ungranted reads. The documented API also warns that filtering an already restricted token computes an intersection of restricting lists; the actual final list must be inspected. [CreateRestrictedToken](https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-createrestrictedtoken), [restricted tokens](https://learn.microsoft.com/en-us/windows/win32/secauthz/restricted-tokens).

`CreateProcessAsUserW` accepts extended startup attributes and documents the caller-derived restricted-token exception to the assign-primary-token privilege requirement. That does not prove every privilege, LPAC interaction or runtime prerequisite for our composition; measure the actual caller and failure results. Use the documented AppContainer/LPAC startup recipe and creation-time Job association. [CreateProcessAsUserW](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessasuserw), [LPAC launch recipe](https://learn.microsoft.com/en-us/windows/win32/secauthz/implementing-an-appcontainer), [startup attributes](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute).

**Inference to test:** a fresh execution-specific restricting SID, distinct from the package SID and absent from ambient ARAP resources, might make the additional DACL check fail while narrow ACEs on owned closure/scratch objects permit useful work. Public component APIs justify one feasibility probe. They do **not** establish that Windows preserves an independent restrictive check when creating this LPAC, that the loader can run under it, or that it closes all host IPC.

Do not claim to remove ARAP merely by passing its SID to `SidsToDisable`: the public API ignores SIDs absent from the token's ordinary user/group list, and P01 did not establish ARAP as such a removable group. Measure the resulting token and access behavior rather than guessing about implicit lowbox membership.

### Why this is one last bounded probe, not an open-ended native redesign

There is a specific untested kernel access-check hypothesis and an existing decisive oracle. It can be tested with the existing synthetic fixture and a small disposable launcher change, without touching production. If useful startup needs a broader restricting list, capabilities, host ACL changes, an unrestricted initialization token, private APIs or a service, stop. Do not broaden the design until it happens to run.

A trusted browser's sandbox is not proof for arbitrary native executable startup. Chromium describes restricted-token/lowbox composition, but also target cooperation and a less-restricted initialization token in its bootstrap model. This profile treats executable initialization, DLL entry points and TLS callbacks as hostile; lowering authority in target `main()` is too late. [Chromium sandbox design](https://chromium.googlesource.com/chromium/src/+/master/docs/design/sandbox.md).

Null DACLs are another explicit challenge: Microsoft documents that they bypass normal discretionary checks. The candidate must therefore be tested against permissive endpoints and their namespace/device access paths; success against only one ARAP ACE does not establish universal host-IPC denial. An empty DACL is a different case and is not a useful denial positive control. [Null versus empty DACLs](https://learn.microsoft.com/en-us/windows/win32/secauthz/null-dacls-and-empty-dacls).

The experimental `processmodel.dll` APIs are documented as experimental, subject to change, and lacking a public header. Historical GW-1B success does not make them a supported replacement for this decision. Do not pivot P01B to those APIs. [Experimental sandbox API status](https://learn.microsoft.com/en-us/windows/win32/secauthz/createprocessinsandbox).

### P01B scope and acceptance

| Gate | Required observation |
| --- | --- |
| Construction | Derive a fully restricted primary token from the non-elevated caller using public APIs, with one fresh execution SID distinct from the package SID in the restricting list; no `WRITE_RESTRICTED`, `SANDBOX_INERT`, broad restricting-SID fallback or new capability. Preflight the caller's existing restricting list; do not let an empty intersection silently remove the intended constraint. Compose with documented zero-capability LPAC attributes and Jobs at creation. |
| Token establishment | Externally inspect the suspended target's restricted SID list, ordinary groups, privileges, integrity, package/capabilities and Job membership. Verify that composition retained the intended check; do not accept a successful creation call alone. Unknown fields stay Unknown. |
| Useful closure from first target instruction | A small native executable loads, reads a narrow RO input, writes bounded scratch and exchanges only prepared anonymous data channels. All restrictions are active before any target DLL/TLS/CRT code; no privileged startup impersonation or cooperative token lowering. Every extra runtime dependency is recorded, not silently granted. |
| Positive controls | Same nonce/endpoint action succeeds under the ordinary control and under the original LPAC composition where P01 succeeded. An explicitly narrow-granted object exercises the new SID check. The outer fixture permits the tested action. Anonymous-pipe I/O works. Namespace/path failures are distinguished from object-ACL denials without adding broad grants. |
| Decisive repeat | Original ARAP-only sentinel and global ARAP broker are denied with the new layer; external observer records no request/child/effect. Repeat relevant reads, write opens and pipe access modes, not merely the original combined access mask. |
| Obvious authority variants | Synthetic owner/Everyone/AAP/ARAP and package-only resources lacking the new grant; a deliberately permissive/null-DACL endpoint with compatible integrity label; owner/`WRITE_DAC` regrant attempts on disposable denied objects; attempts to recover a less-restricted token/manager handle. No real services or credentials. |
| Descendants | One finite native child retains the full restriction and candidate Job ownership; it cannot repeat the ARAP request or regain the caller token. If the restrictive construction cannot support the profile's useful descendant semantics, report incompatibility rather than banning children silently. |
| Standard-user deployment | Test an existing normal standard-user context when available. A filtered administrator run may falsify the boundary but cannot complete the standard-account prerequisite. Do not elevate, create accounts or introduce alternate credentials to rescue this probe. Missing context remains pending/unsupported on that host. |
| Safety/cleanup | Fresh nonce; small explicit caps on attempts/children/files/bytes; normally <=30-second targets; independent watchdog; outer Job permits controls; retain cleanup identities before release; stop/reap, confirm actual Job emptiness, then remove only owned endpoints/profile/desktop/workspace. Cleanup uncertainty stops all work on that host. |

The narrow-granted diagnostic objects may carry the new execution SID and package SID on **owned fixtures only**; no broad ACE may be added to the candidate closure, runtime or existing host objects to make execution succeed. The intentionally permissive ARAP/Everyone/null-DACL challenge endpoints are disposable negative-control fixtures, as in P01, not runtime grants. A diagnostic exact-grant pipe is not an authorized host-service channel for the production profile. If an additional namespace gate blocks all new named-pipe opens, document that mechanism and use working anonymous-channel and baseline controls; do not widen namespace access solely to make a diagnostic pipe connect.

P01B outcomes:

- **SUPPORTED BY THIS PROBE:** useful closure and finite child run under the final restrictions; required synthetic authority attempts are denied with valid controls; complete cleanup. This preserves the candidate for P03/P04/P07, **not** implementation or full host-broker qualification.
- **BLOCKED BY THIS PROBE:** ARAP/permissive-host authority remains usable, restriction is lost, meaningful work requires prohibited widening, or target startup executes before the final authority boundary. Stop the Windows-native track for this phase; return to VM/refusal planning.
- **INCONCLUSIVE / UNSUPPORTED ON THIS HOST:** missing valid controls, necessary public-API/standard-account context unavailable, or uncertain token/namespace interpretation. Windows continues refusing. No substitute private API, elevation or spontaneous second native design.

One construction/hypothesis is authorized. Bounded harness corrections and a reviewer-identified decisive variant are allowed; an endless search over different token profiles or runtime baselines is not. If a supported-runtime/readiness question requires P05/P06/P08 design work, stop and report that dependency.

## 4. Windows-native versus VM versus refusal

**Immediate behavior: explicit Windows refusal for `restricted-offline-closure-v1`.** The frozen lifecycle API remains its already qualified scope; it cannot be used as a fallback satisfying this required profile.

**Engineering direction: VM-backed isolation is preferred if the bounded native alternative fails.** Hardware-isolated guests separate host object namespaces and broker processes more directly than another host token variation. Microsoft distinguishes Hyper-V isolation from process-isolated containers; this supports the direction, not qualification of a particular Gagamba provider. [Windows container isolation](https://learn.microsoft.com/en-us/virtualization/windowscontainers/manage-containers/hyperv-container), [security boundary guidance](https://learn.microsoft.com/en-us/virtualization/windowscontainers/manage-containers/container-security).

A VM provider still needs its own design and evidence: supported host features/edition and provisioning; immutable guest/runtime; no host pipe/agent/credential/clipboard/device exposure or RW host shares; controlled finite copy-in/export; guest and host-side storage/memory/process/output/deadline bounds; guest broker authority; truthful target status and whole-VM teardown on owner death. Removing a vNIC does not prove denial of guest loopback, and a fixed scratch disk does not bound every writable guest/host log/profile location. These requirements preserve the same profile. A `.wsb` configuration alone is not a completed provider. [Windows Sandbox configuration surfaces](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file).

If suitable virtualization is unavailable or its integration cannot satisfy the unchanged contract, refuse. No VM integration, OS feature installation, privileged service or runtime deployment is authorized by this review.

## 5. Linux P02 should proceed independently now

**Yes.** The P01 result concerns Windows token/object/broker semantics and does not invalidate the Linux namespace/cgroup candidate. The original architecture review already recommended independent platform gates and allowed Linux-first delivery. The previous probe specification's global hard stop was correctly observed; this decision supersedes that cross-platform scheduling hold, not the failed Windows finding.

Resume P02 with a genuinely non-root host UID, actual cgroup-v2 delegation and memory/pids controllers, pinned non-setuid helper, user/mount/PID/network/IPC namespaces, cap drop, `no_new_privs`, closed authority FDs and justified seccomp. The same-UID attack matrix remains mandatory: migration, sibling/ancestor control, limits/controllers, mounts/namespaces, proc/FD recovery, ptrace/process memory/pidfd and synthetic daemon paths. The manager must succeed at the relevant permitted operations inside the same outer fixture.

A Linux boundary counterexample stops the Linux candidate; do not automatically add Landlock, change UID, install setuid, alter sysctls or create a daemon. Missing host delegation/userns/controller prerequisites are explicit unsupported/pending evidence, not denial. No current Linux success is implied by authorizing P02.

## 6. Revised ordered backlog

No platform experiments run concurrently on the shared host.

| Order | Work | Gate / stop |
| --- | --- | --- |
| 1 | **P02 Linux control-plane** | Run next; report exact environment, controls, attacks, cleanup and Luna challenge. Preserve failure/unsupported outcomes. |
| 2 | **P01B Windows full restricting-SID + LPAC** | One bounded distinct composition as specified above. If no documented construction/prerequisites exist, record that and skip execution. A Windows-only failure does not cancel independently viable Linux work. |
| 3 | Updated platform checkpoint | Classify each as viable candidate, viable with unresolved prerequisite, blocked or inconclusive. Only viable candidates advance; standard-user deployment gaps remain gates for Windows. |
| 4 | **P03 Linux, then Windows only if P01B survives** | Complete no-IP plus host-IPC transport matrix and inherited-channel attacks. P01B's named-pipe subset is not P03 qualification. |
| 5 | **P04 on surviving candidates** | Native plus self-contained .NET compatibility, honest memory/task/process accounting, every writable location, bounded metadata/output/FD or handle implications. No silent budget increases; Windows profile/registry escape stops native route. |
| 6 | **P07 on surviving topologies** | Setup/crash/release/death-chain/root-status/descendant/pipe/guardian proof; no released-plus-unowned state. A helper needing substantial patching returns to architecture review. |
| 7 | Consolidate and STOP | Return evidence to Astra. P05/P06 preparation/closure, P08 runtime baseline and P09 qualification remain later assignments; passing these blockers does not authorize implementation. |

If a candidate fails, skip its downstream probes; continue another candidate only after confirmed cleanup and only while the failure is platform-specific. Cleanup uncertainty, inability to execute safely or an issue affecting the shared harness/host stops all experiments. A failure requiring a new mechanism ends that candidate's investigation rather than authorizing architecture repair.

## 7. Changes to restricted-offline-closure-v1

**No weakening and no new authority exception.** Keep staged read-only closure, bounded disposable writable state, no IP including private loopback, no host-service IPC, no credentials/ambient handles, protected controls, immutable pre-release establishment, truthful root/domain status and whole-domain cleanup.

Clarify the architecture/qualification mapping, not the ceiling:

1. Broad object ACLs (including ARAP) are not execution grants. A platform baseline cannot enumerate an unrestricted class of brokers as an exception.
2. The boundary covers requests that delegate work to other processes, not only children that inherit the target's Job/cgroup.
3. Restrictions apply before the first potentially hostile target initialization instruction, including DLL/TLS callbacks; a cooperative later token drop is insufficient.
4. Windows-native identity is now a P01B hypothesis; a future VM identity mapping needs review. Mechanism names such as LPAC are not the common profile's acceptance criterion.
5. An unsupported platform refuses independently. Linux support does not depend on preserving Windows support; macOS remains proposed refusal-only.

All five points restate the original ceiling or clarify its enforcement evidence; none adds ambient authority or relaxes resource/IPC/storage guarantees.

## 8. Exact next Sol handoff

> Continue AR-7 **feasibility only**, starting with P02 Linux. Use `qualification/ar7-feasibility`; preserve the frozen `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56` production branch/tag/seal and all P01 source/evidence. Read this decision, the original architecture/probe handoff and the remediation evidence standard. The P01 LPAC + Job composition is eliminated; do not repair it or reinterpret the result.
>
> Run the ordered backlog in section 6. P02 may proceed despite the unresolved Windows route. After P02 cleanup/evidence, conduct at most the P01B construction defined in section 3, using public supported APIs and no broader authority. A standard-account or prerequisite gap must remain explicit. Then run P03, P04 and P07 only for candidates that survive their preceding gates. A platform-specific candidate failure does not block another viable platform after cleanup; shared-host safety or cleanup failure does.
>
> For each executed probe: commit the disposable source before release; bind raw observations to exact source/harness/helper hashes and host/UID/token context; establish meaningful controls; use an independent external oracle and watchdog; verify complete owned-domain/fixture cleanup; have Luna review read-only; record findings, disposition, changes and non-claims in durable evidence. Use the original conclusion vocabulary. Create `docs/evidence/ar7-P01B-windows-restricting-sid.md`; update each later probe's existing NOT RUN record only when actual new evidence exists. Preserve historical P01 raw evidence and its conclusion.
>
> Do not modify production providers, contracts, Hufu, package metadata or release workflows. Do not widen runtime/closure/host ACLs or add capabilities to rescue a candidate; permissive ACLs are confined to the bounded synthetic negative controls described above. Do not add an unrestricted startup token, private APIs, a privileged service, a setuid helper or a VM implementation. Do not run P05/P06/P08/P09; report a dependency instead. Do not merge, push a release branch, publish or change the frozen refs. Stop after the specified evidence backlog or the applicable hard stop and return the candidate matrix and unresolved decisions to Astra. No implementation is approved.

This handoff is a review deliverable; **no probes were run in this decision turn**.

AR-7 FEASIBILITY MAY CONTINUE — RUN SPECIFIED PROBES
