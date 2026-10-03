# Gagamba testing and CI plan

Status: planned, 2026-10-03. No workflows have been created, dispatched or qualified. This plan implements the testing obligations in the [implementation plan](implementation-plan.md).

## Evidence model

Use the same test entrypoints locally and in CI. Test four separate layers: pure managed contracts, real backend enforcement, useful developer workloads, and packaged distribution. A passing build or mocked provider test must never count as OS confinement evidence.

Every result records test ID, profile version, source commit and dirty-tree state, backend/helper version, OS/kernel/build, architecture, filesystem, runner image, actual setup/target privilege, requested/prepared policy hashes, outcome, timing and cleanup state. Raw reports live under ignored `artifacts/`; retain small reviewed summaries in `docs/evidence/`.

Outcome values are Passed, Failed, Unsupported and NotRun. Unsupported is acceptable for an exploratory probe, but is a failure of a required profile qualification gate. Reports must include expected test count and mandatory case IDs so an empty suite, skipped matrix leg or missing artifact cannot appear green.

## Mandatory conformance families

| ID | Cases | Required oracle |
| --- | --- | --- |
| CF-POL | Invalid/unknown rules, conflicting grants, requested unsupported features, policy mutation after preparation | Typed rejection and a target marker proving no dispatch occurred |
| CF-BIND | Different executable/args/environment/cwd, reused/stale/foreign plan, helper replacement | Invalid binding rejected before target release |
| CF-FS | Allowed reads/writes; denied read/write/create/delete/rename/enumeration as specified; private scratch | Sentinel/control operations work outside Gagamba; sandbox effects match policy; denied targets unchanged |
| CF-PATH | Traversal, sibling-prefix confusion, aliases, symlink/junction replacement, hardlinks, mount/reparse changes | Supported cases stay confined; unsupported path classes reject explicitly |
| CF-NET | Direct TCP/UDP, IPv4/IPv6, DNS, host loopback, proxy variables removed, inherited connection | Controlled listeners reachable by the control process, unreachable by target where denied |
| CF-IPC | Inherited handles/FDs, named pipes/Unix sockets, host build/SSH/Docker services | Synthetic host service inaccessible except explicitly declared test I/O |
| CF-TREE | Child/grandchild, root early exit, double-fork/daemonization, job/process-group breakaway | All descendants remain restricted and owned; no surviving writer/worker after stop acknowledgement |
| CF-LIFE | Cancel before/during create, during release/run/drain; timeout; repeated stop/dispose; launcher death | No release after stop wins; bounded completion; kernel-backed ownership and no survivors |
| CF-IO | Simultaneous stdout/stderr flood, binary/invalid text, blocked stdin, inherited open pipe in child | Bounded memory/output, no deadlock, documented overflow and drain outcome |
| CF-FAIL | Missing helper/API, failed setup/pipe/filter/mount/job, stale prerequisites, cleanup failure | No unconfined execution; precise error and recoverable test-owned state |
| CF-CONC | Concurrent sandboxes with different grants, identities, scratch and limits | No cross-sandbox permission inheritance or shared mutable state expansion |
| CF-OBS | Secrets in argv/environment/output and sensitive paths | Default metadata stays redacted; native details remain bounded and structured |
| CF-LIMIT | Every advertised CPU/memory/process limit plus required deadline/output bound | Real boundary enforces claimed limit; unsupported requested limit rejects |

The profile determines mandatory path/network cases; backend-specific omission requires an explicit unsupported contract, not an unexplained skip. For example, systems without IPv6 need separate coverage for any IPv6 claim rather than a false pass.

## Test construction and safety

Run only controlled fixture code, with unique run IDs, private temporary roots, synthetic data, bounded process counts and deadlines. Do not use actual user secrets, external public hosts, or uncontrolled package hooks as escape fixtures. Target the Gagamba boundary, not vulnerabilities in GitHub's infrastructure or the host kernel.

Before a denial test, demonstrate the test resource is accessible to the unsandboxed fixture control under the same outer environment. Where backend identity differs, distinguish pre-existing account denial from enforcement installed by Gagamba. A disconnected runner or Docker firewall cannot serve as proof of Gagamba network denial.

Use worker readiness messages and explicit barriers to reproduce launch/stop races. Check containment ownership alongside fixture heartbeats/output; absence of output alone is insufficient proof of death. Once stop returns, attempt to detect test-owned survivors independently and fail on cleanup uncertainty. Kill only test-owned resources.

Initially repeat each critical lifecycle race 100 times in the extended suite and run at least 100 bounded create/run/stop cycles per provider. These are proposed starting budgets, not proof of race freedom. Preserve first-failure seed and trace. Re-running a failed case successfully does not erase the failure.

Use a test watchdog independent of the launcher and enforce a workflow/job deadline. Cleanup runs on ordinary test failure and cancellation where possible; kernel-backed containment must also handle abrupt supervisor loss, when scripts cannot run. Resource exhaustion tests stay in disposable environments with hard outer limits and cannot establish an inner limit unless that limit is separately observed.

## Workload tests

Restore/build fixture dependencies before entering the denied-network execution phase. Keep the dependency cache read-only or private, with explicit runtime grants. Disable or contain persistent build servers and user startup hooks.

| Workload | Initial scope | Completion evidence |
| --- | --- | --- |
| .NET | Small build and test using prepared dependencies and `--no-restore` | Expected artifact/test output; denied sentinels untouched; no external daemon execution |
| Git | Controlled repository, status/diff; safe config and hooks | Correct results; no unexpected helpers/network; writes only when expressly granted |
| Shell | PowerShell/cmd on Windows, sh/bash on Linux/macOS | Argument/env/cwd handling and descendants confined |
| Python | Small local script if selected for support | Interpreter/runtime grants explicit, same denial fixtures pass |
| Cargo/npm | Later offline fixtures with vetted prepared dependencies | Separate compatibility gates; not prerequisites for the first .NET/Git preview |

`git worktree`, dependency restoration and network package installation are follow-on workloads. They must not be used to widen the initial profile implicitly.

## Planned local entrypoints

Implement these under `eng/` with identical report semantics; names are proposed and commands do not exist yet:

| Entrypoint | Responsibility |
| --- | --- |
| `eng/verify.ps1` / `eng/verify.sh` | Locked restore, Release build, managed tests, analyzers and report validation |
| `eng/probe.ps1` / `eng/probe.sh` | Read prerequisites and run bounded real mechanism probe; emit capability report |
| `eng/conformance.ps1` / `eng/conformance.sh` | Run a selected real backend/profile; require all mandatory test IDs |
| `eng/workloads.ps1` / `eng/workloads.sh` | Execute prepared controlled workload fixtures |
| `eng/qualify-packages.ps1` / `eng/qualify-packages.sh` | Build local candidates and run source-independent consumers |

Keep preparation separate from fixture execution. Local entrypoints must not silently install distributions, elevate, change firewall/ACL policy broadly, or relax AppArmor/sysctl settings. Emit prerequisites and use an explicit documented setup step for such changes.

## CI rollout

| Workflow | Introduced | Trigger | Intended gate |
| --- | --- | --- | --- |
| `ci.yml` | GP-1 | Pull requests and main pushes | Restore/build, managed tests, analyzers, dependency/API checks as they exist; stable aggregate `build-and-unit` |
| `backend-probe.yml` | Platform spikes | Manual; later weekly image-drift check | Inventory and minimal mechanism probe; research evidence, not production pass |
| `conformance.yml` | First GW-2/GL-2/GM-2 | Pull requests/main on disposable hosted runners; trusted dispatch for dedicated machines | Provider test matrix plus aggregate `sandbox-conformance` once all advertised legs are ready |
| `extended-tests.yml` | GQ-1 | Nightly on trusted main and manual | Repeated races, crash recovery, concurrency, Docker and workload matrix |
| `package-qualification.yml` | GR-1 | Manual release candidate and relevant main changes | Exact package/helper contents, isolated consumers, supported RID checks |

These triggers describe future workflow files, not a scheduled automation created now. Add a required check only when its real jobs exist and can run. During platform research, publish an explicit qualification-status report and hold cross-platform readiness rather than manufacturing a passing check for missing backends.

## Initial runner matrix

The following labels were checked against GitHub documentation on 2026-10-03 and must be rechecked when the workflow is implemented. Versioned labels still receive image updates; record the exact image revision. [GitHub runner reference](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)

| Lane | Environment | Test role and limitation |
| --- | --- | --- |
| Windows developer | Current Windows x64 host | First actual API/backend evidence; controlled fixtures only |
| Windows hosted | `windows-2025` x64 | Managed builds and availability/rejection tests; cannot substitute for Windows 11 API conformance |
| Windows qualification | Disposable Windows 11 x64 VM/dedicated runner, if hosted x64 cannot satisfy the backend | Required real provider evidence; provision after the mechanism decision; not the user's everyday machine exposed as a PR runner |
| Linux developer | Separate Ubuntu WSL2 x64, Linux filesystem | Fast local iteration; not native-distro qualification |
| Linux hosted | `ubuntu-24.04` x64 | Primary native Linux conformance candidate; explicitly verify namespace/AppArmor prerequisites |
| macOS ARM64 | `macos-15` | First hosted macOS mechanism probe and later conformance |
| macOS Intel | `macos-15-intel` | Separate x64 qualification before claiming Intel support |
| Docker secondary | Pinned Linux container on Docker Desktop or hosted Linux | Nested-environment compatibility with documented outer restrictions |

Do not use a slim container runner for primary Linux namespace qualification. Windows ARM64, Linux ARM64, other distributions and additional OS releases are later explicit matrix extensions, not implied by successful x64 tests.

## Avoid false green CI

- The matrix uses `fail-fast: false` to collect every platform result; the aggregate still fails if any mandatory leg fails, is cancelled, produces no report, or skips mandatory cases.
- Research probes may report unsupported hosts, but mandatory conformance has no `continue-on-error` path for missing enforcement.
- Run the aggregate check even when dependencies fail. Validate expected legs and test IDs, not just artifact presence. Avoid path filters that cause required checks to disappear.
- Compile the actual native helpers in their target lanes. Validate managed tests do not accidentally select a fake backend or PATH-provided helper from the checkout.
- Treat runner drift as a new qualification condition. A weekly probe alert is actionable evidence, not permission to relax the policy or silently choose another backend.
- Report local-only Windows evidence with exact source identity. Until repeatable Windows 11 automation is available, manual evidence can inform development but the automated three-platform gate is incomplete.
- Test ordinary target privileges as well as setup privileges. Hosted administrator/root capabilities must not hide a missing deployment prerequisite.

Suggested initial budgets: 15 minutes for build/unit, 10 for capability probes, 20 per conformance job and 45 per extended job. Every fixture has a shorter independent deadline. Tune from measured duration; timeout is not a successful skip.

## Workflow trust and artifacts

Use read-only repository permissions for verification, immutable action commit pins, locked dependency resolution, and `persist-credentials: false` for test checkouts. No release credentials or repository secrets enter sandbox fixture jobs. GitHub recommends least privilege and immutable action references. [Secure use reference](https://docs.github.com/en/actions/reference/security/secure-use)

Run untrusted PR code only on disposable hosted machines with constrained credentials. Never use privileged `pull_request_target` execution of a PR checkout. Dedicated Windows/Mac runners accept only reviewed trusted revisions and are reset after runs. A self-hosted runner must not be assumed disposable merely because a job finished.

Upload bounded redacted JSON/TRX reports, logs and environment manifests even on ordinary failure. Proposed retention is 14 days for routine runs and 30 for failed qualification runs; retain compact release summaries longer in the repository. Do not upload memory dumps, credentials, arbitrary user paths or complete process environments by default.

Separate dependency caches from target scratch. Include OS, architecture, SDK, dependency lock and helper identity in cache keys; do not let target-writable caches become trusted helper locations. Package qualification starts clean and cannot consume artifacts uploaded by untrusted PR workflows as release inputs.

Workflow cleanup must not affect the Actions agent or globally disable its network. Prepare dependencies and upload results outside the child sandbox. Any runner configuration change needed for namespaces is explicit, narrowly scoped and recorded as part of the deployment profile.

## Release acceptance checklist

- [ ] Required build/unit and real conformance jobs pass for every advertised platform/architecture.
- [ ] No mandatory tests skipped, missing or masked by infrastructure denial.
- [ ] Extended lifecycle/crash/concurrency suite passes with retained failure-free evidence for the candidate.
- [ ] Representative offline workloads pass with documented grants and limits.
- [ ] Actual package/helper candidates pass isolated allow/deny/stop consumers on each supported RID.
- [ ] Security review findings, installer/cleanup behavior and platform support limitations are resolved or explicitly block affected claims.
- [ ] All evidence binds to the exact candidate source/package identities; later edits invalidate affected qualification.

No package publication is performed by this plan. Publishing remains a separate action after the concrete candidate is qualified.
