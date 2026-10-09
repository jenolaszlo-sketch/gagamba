# AR-7 P01 — Windows LPAC authority feasibility

Date: 2026-10-09. Slice: architecture-blocking probe only. **Conclusion: BLOCKED BY THIS PROBE.**

**WINDOWS NATIVE PATH BLOCKED** for the tested unaugmented zero-capability LPAC + Job composition: an ungranted synthetic ARAP file remained readable and an ARAP broker created work outside the candidate Job. This is a counterexample to the proposed explicit-closure/no-host-IPC boundary, not proof that every Windows native composition is impossible or that a real Windows service is exploitable. The assignment's section 24 hard stop applies; no architecture repair or subsequent probe was attempted.

## Source binding and environment

| Field | Recorded value |
| --- | --- |
| Production baseline | `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`; frozen branch/tag unchanged |
| Review/start revision | `8e156774fec28d807b557acae487791289369609` |
| Exact tested probe revision | `ee170724c14d8bd138cec1a461d86eb8bec76ec6` |
| Branch and state at release | `qualification/ar7-feasibility`; clean (`dirty=false`, no source-state entries) |
| Native source SHA-256 | `BBE95A8A3EBD30DDC16759DC4BE06C0FA5767B2D6F2831DFEF34007FD9F5E99A` |
| Driver SHA-256 | `6972BB9F1A735C85CDC286D33A7AA6380AECC9FF8573833E943A28D6BD4147F1` |
| Native executable SHA-256 | `2A8C9D1BFF2E0AB12AE01BD63E935E36F47A5B538ECA94AD75FC1B5C5D6A290E` |
| Host | Windows NT `10.0.26200.0`, x64; PowerShell `7.6.5` |
| Compiler/SDK | MSVC `14.51.36231`; public Windows SDK `10.0.26100.0`; static C runtime (`/MT`) |
| .NET SDK | `10.0.401`, inventory only; this workload is native C++ |
| Caller | `LAPTOP-KBD8D8JG\Laszlos`; medium integrity `S-1-16-8192`, elevation 0 |
| Privilege nuance | Filtered administrator account: Administrators SID `S-1-5-32-544` is deny-only (flags 16), not enabled. This proves non-elevated operation, not a separately provisioned standard-user account. |
| Restricted workers | Same host user SID; low integrity `S-1-16-4096`, AppContainer=1, zero capabilities; only `SeChangeNotifyPrivilege` enabled; `SeIncreaseWorkingSetPrivilege` disabled |
| LPAC token query | Public `TokenIsLessPrivilegedAppContainer` returned error 87 for all tokens; **Unknown**, never interpreted as false/true |
| Provisioning | Per-user unique AppContainer profile and test-owned ACLs only; no administrator token, account/service, global ACL/firewall changes or persistent helper |
| Nonce | `ffe59c6728904a328d4b7f1a79139193` |

Probe source is [p01.cpp](../../spikes/Ar7Feasibility/p01.cpp), with [driver](../../spikes/Ar7Feasibility/run-p01.ps1) and [design/safety notes](../../spikes/Ar7Feasibility/README.md). The durable [observation record](ar7-feasibility/P01-observations.json) retains context, bounded external output, observer results and original-file hashes. Ignored raw files/binary remain under `artifacts/ar7/P01/ffe59c6728904a328d4b7f1a79139193/`; the executable is at `artifacts/ar7/P01/p01.exe`. The evidence commit follows the tested probe revision; it receives no AR-6 seal.

## Question and mechanism

Can a non-elevated caller create a documented restricted Windows identity that runs a useful native closure while denying ungranted protected-file and broker authority?

This new disposable experiment uses public `CreateAppContainerProfile`, `PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES`, LPAC's `PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY` / `PROCESS_CREATION_ALL_APPLICATION_PACKAGES_OPT_OUT`, and `CreateProcessW`. Each worker is associated at creation with an outer cleanup Job and a separate candidate Job via the public Job-list attribute. Jobs set kill-on-close only: they impose no file, token, pipe or broker denial. No standard handles or other ambient handles are inherited. The explicit environment contains synthetic profile/temp paths and Windows runtime location variables, without copying the caller's environment.

Seven synthetic sentinels exist before dispatch; the controller records existence and DACLs. Allowed input/executable have exact-package read/execute rights. Scratch has exact-package full rights and a low mandatory label. User/protected/sibling/profile stand-ins have owner/system rights only. AAP and ARAP stand-ins additionally grant read to `S-1-15-2-1` and `S-1-15-2-2`, respectively; the candidate has no explicit grant to those two files. The profile stand-in is a file, not a complete synthetic profile tree; broader profile traversal remains untested.

Eight finite brokers compare global and `LOCAL` pipe names with Everyone, AAP, ARAP and exact-package DACL variants. The controller uses low labels and rejects remote clients. It validates both actual client PID and nonce before creating a finite native child in the outer Job alone. Exact-package pipes are deliberate reachability controls, not ungranted profile exceptions. No real WMI, SCM, task, COM or RPC service is touched: those legs are **NOT TESTED**.

All targets are this fixed synthetic program: seven reads, two writes, eight pipe attempts, no arbitrary workload. There are three sequential worker roots and at most 24 finite broker children. Children self-expire after 200 ms; workers have a 10-second watchdog; the controller has a 12-second per-worker bound and independent PowerShell observer a 30-second host watchdog. The outer Job protects cleanup and permits the demonstrated broker action. These source bounds are not hostile-workload resource limits.

## Positive controls and restricted observations

Zero means operation succeeded; 5 is `ERROR_ACCESS_DENIED`; 2 is `ERROR_FILE_NOT_FOUND`.

| Operation | Ordinary user (0) | AppContainer (1) | Candidate LPAC (2) |
| --- | --- | --- | --- |
| Allowed input read / scratch write | 0 / 0 | 0 / 0 | 0 / 0 |
| Allowed input write | 0 | 5 | 5 |
| Existing user/protected/sibling/profile stand-in reads | All 0 | All 5 | All 5 |
| Existing AAP-only file read | 0 | 0 | 5 |
| Existing ARAP-only file read | 0 | 0 | **0: ungranted read succeeds** |
| Global Everyone pipe | Request succeeds | 5 | 5 |
| Global AAP pipe | Request succeeds | Request succeeds | 5 |
| Global ARAP pipe | Request succeeds | Request succeeds | **Request succeeds; out-of-candidate-Job child** |
| Global exact-package pipe | Request succeeds | Request succeeds | Request succeeds (positive control) |
| Four `LOCAL` endpoints | All request successfully | All 2 | All 2 |

The exact unrestricted program reached all eight brokers, and all seven sentinels were readable. Both restricted workers ran, read the allowed input and wrote scratch. Global exact-package access proves that the restricted worker can reach this broker implementation/naming surface; Global AAP success in ordinary AppContainer and denial in candidate LPAC also independently demonstrates the opt-out attribute's behavioral effect. The unsupported LPAC token-info query prevents a direct boolean-token claim, but does not erase this observed access difference.

`LOCAL` errors are **INCONCLUSIVE for authority denial**, despite the ordinary control: the package-granted restricted control also returns not-found. They are namespace/reachability observations, not successful restrictions.

## External oracle

The trusted controller queries tokens while roots remain suspended, before release. It observes pipe client identity, child Job membership, the child's host-side marker, exit status and actual Job counts, rather than trusting only the target's return values.

For the decisive ARAP request:

```text
BROKER mode=2 endpoint=2 client=5736 child=32372 outsideCandidate=1 inOuter=1
BROKER_EFFECT mode=2 endpoint=2 exists=1 exit=0
FILE 2 arap read=0
```

The child's effect is outside granted scratch, written by the ordinary controller-created broker child. Its ownership is intentionally outside the candidate Job and inside the independent outer Job. The outer boundary demonstrably permits this operation; it cannot explain the candidate's failure as an outer restriction. The exact-package positive-control child was PID 22564; ordinary root PID 39156, AppContainer root 3196, candidate root 5736; host PID 4304.

## Cleanup and setup failures

Final run: all three workers exited 0; outer Job active count 0 and all three candidate Job counts 0; eight closed endpoints returned not-found on reopening. `DeleteAppContainerProfile` returned success; AC folder absent; SID mapping key returned not-found. All explicitly listed fixture files/directories were removed; root absent. The independent PowerShell observer recorded exit 0, no watchdog expiration, root absent and no surviving process at the exact staged executable path. A separate post-run check found the entire unique package directory absent. No residue was observed.

Earlier setup attempts remain raw observations, not candidate denials: initial compiler environment lacked SDK paths; VS setup removed git from PATH; token-info class 46 failed before profile creation; an AppContainer launch with an incomplete explicit environment returned 203 before dispatch. The diagnostic revisions corrected only the harness, retained query uncertainty, and supplied synthetic profile variables. The last aborted launch cleaned its Job/pipe/profile/workspace; a separate check found its complete package directory absent. No failed setup is counted as an LPAC pass.

## Independent Luna review

| Finding | Decision/reason | Resulting change |
| --- | --- | --- |
| Supported LPAC API path exists, but generic attribute reference omits opt-out key and its OS floor | Accepted: primary launch article is the concrete public recipe; no cross-build support claim | Pin SDK/build and retain support uncertainty |
| Nonempty outer Job must block deletion | Accepted: deletion certainty requires completed stop | Before first release, changed cleanup to preserve resources and return unresolved if outer remains populated |
| Broker outer membership needs explicit proof | Accepted | Before first release, assert and log `inOuter=1` alongside `outsideCandidate=1` |
| All AppContainer/LPAC broker requests should fail | Rejected as a blanket rule: AAP comparison and exact-package reachability are intentional controls; any **ungranted candidate** broker success is blocking | Separate control and ungranted legs; do not equate exit 0 with candidate acceptance |
| Elevation zero does not establish a standard-user account | Accepted | Record deny-only administrator membership and limit non-admin conclusion to effective non-elevated token |
| Endpoint count reported by reviewer as twelve | Corrected by source/raw inventory: two prefixes times four ACLs equals eight | Bound child count to 24; no extra endpoints inferred |

| Post-run finding | Decision/reason | Resulting evidence change |
| --- | --- | --- |
| ARAP request is an observed counterexample to unaugmented LPAC + Job host-IPC denial | Accepted: controller authenticated the requester, verified external child ownership and marker | Mark composition blocked and invoke section 24 stop; no further experiments |
| Real-service exploit/all-native impossibility would be overbroad | Accepted: broker deliberately represents an ARAP-accessible host service | Restrict conclusion to this composition and synthetic endpoint |
| `LOCAL` not-found and unsupported LPAC token query must not become denial/token claims | Accepted | Mark LOCAL legs inconclusive and boolean token state unknown; use AAP behavioral discriminator |
| Filtered administrator does not qualify ordinary standard-account deployment | Accepted | Standard-user profile-creation feasibility remains unproved; no new account provisioning |
| Profile sentinel is not a profile tree/storage test | Accepted | Mark broader tree/registry/storage questions untested; do not promote P01 to P04 |
| Cleanup evidence establishes no observed residue for this run | Accepted: actual Job counts, endpoint/profile checks and independent process observer agree | Preserve raw cleanup and external package-directory check |

No post-run reviewer finding identified a missing variant that could reverse the demonstrated ungranted global ARAP request. No follow-up execution was authorized past the hard stop.

## Consequence and non-claims

Unaugmented LPAC + Job does not satisfy this proposed profile on the measured host. **Do not patch the candidate during this assignment.** Astra must assess a further reviewed native authority mechanism, a precisely limited runtime baseline if compatible with the common profile, VM-backed isolation, or Windows refusal. A blanket ARAP exception would change the closure/IPC contract and cannot be silently introduced.

The probe does not establish real-service exploitation, all-native impossibility, a full filesystem tree/export boundary, network/resource/crash guarantees, separately provisioned standard-account deployment, registry/COM denial, cross-build LPAC support, or production integration. Production providers are unchanged. Linux remains untested; macOS remains proposed refusal-only, without a new qualification claim.

Primary references: [Microsoft LPAC launch recipe](https://learn.microsoft.com/en-us/windows/win32/secauthz/implementing-an-appcontainer), [attribute-list reference](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-updateprocthreadattribute), [token information classes](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ne-winnt-token_information_class), [named-pipe access rights](https://learn.microsoft.com/en-us/windows/win32/ipc/named-pipe-security-and-access-rights). The native observations, rather than documentation alone, establish the blocking result.
