# Research and qualification plan

Status: Windows spikes (GW-1A, GW-1B slices 1–4) executed 2026-10-04; Linux and macOS spikes not started. Windows, Linux, and macOS are target platforms. Local environment inventory completed; Windows launch evidence exists, Linux/macOS remain open. See [work queue](work-queue.md) and [evidence](evidence/windows-slice4-GW-1B.md). Updated 2026-10-04.

The [implementation plan](implementation-plan.md) now defines the canonical delivery order and maps these research gates to implementation work. See [testing and CI](testing-and-ci.md) for automation and evidence acceptance.

## Gates

| Gate | Work | Completion evidence |
| --- | --- | --- |
| G0 | Complete as a documented design baseline | [offline-process-v1](security-model.md); backend qualification pending |
| W1 | Windows API availability and minimal launcher | Exact OS/API/schema versions; clean failure path; captured I/O; no premature target execution |
| L1 | Linux minimal launcher | Exact kernel/distribution/bwrap configuration; isolation setup and failure evidence |
| M1 | macOS runner capability and minimal launcher | Exact runner image/architecture; Seatbelt availability; denied-access and descendant evidence |
| W2 / L2 / M2 | Same offline workload and adversarial fixtures | Allowed effects succeed; denied effects fail; no descendant survivors; separate results per backend |
| G1 | Select mechanisms and revise API | Backend decision with limitations and operational prerequisites |
| G2 | Implement experimental shared library | Immutable preparation, structured errors, lifetime, output bounds, no silent fallback |
| G3 | Cross-platform qualification | Windows AND Linux AND macOS minimum profile, repeatable CI, supported version matrix, independent security review |

Conformance success is necessary evidence, not a proof of absence of vulnerabilities. Failing the common profile on one platform holds the cross-platform claim; it must not reduce policy silently.

## Windows experiments

1. Probe the export and acquire a verified matching schema; distinguish export presence from a successful usable API. Record architecture, build, privilege, filesystem, nested-job, and policy prerequisites.
2. Launch a tiny fixture with network denied, read-only input, writable output, explicit runtime paths and clean environment. Demonstrate stdin/stdout/stderr transport without ambient handle leakage.
3. Establish containment before target code executes. Test child/grandchild launch, breakaway attempts, rapid exit, nested jobs, root exit with survivors, cancellation during create, and launcher crash.
4. Exercise symlink/junction replacement, hardlinks, alternate paths, protected ancestors, and simultaneous sandboxes. Determine whether ACL/profile/network artifacts persist and verify cleanup after failures without damaging existing host ACLs.
5. Test `dotnet build --no-restore`, `dotnet test --no-restore`, Git inspection, PowerShell and `cmd.exe` against small disposable fixtures. Disable persistent build servers and user startup/config hooks where possible; add required runtime reads explicitly.
6. Decide whether the experimental API suffices. If not, assess a restricted-token/dedicated-user implementation and its installation, elevation, ACL rollback, firewall ownership, concurrency, and servicing costs. Assess existing runtimes as alternatives using the same tests.

Initial local inventory observed .NET SDK 10.0.401, registry build 26200.9457 / display version 25H2, and `processmodel.dll` file version 10.0.26100.9444. The registry product label reported Windows 10 despite the newer build; qualify the actual OS with a proper probe. GW-1A (2026-10-03) later confirmed all three `Experimental_*` exports resolve and the schema sources; GW-1B (2026-10-04) launches, denies and captures I/O, with tree-stop, file-handle I/O, dotnet runtime and Git grants still open. See [evidence](evidence/windows-slice4-GW-1B.md).

## Linux experiments

1. Start Linux iteration in a separate Ubuntu WSL2 distribution on this Windows machine. Record kernel, distribution, architecture, bubblewrap version and namespace availability; keep fixtures in its Linux filesystem. Docker is a secondary nested-environment lane. Later run on a native Linux VM/CI runner; WSL2 evidence alone does not qualify other distributions.
2. Build a minimal mount view with explicit read-only runtime/input mounts, isolated scratch/output, appropriate process/IPC/network namespaces, and bounded devices/proc exposure. Do not bind the whole host root read-only as a substitute for denied reads.
3. Establish privilege restrictions and an audited syscall policy before target execution. A small native launcher may be needed; do not apply irreversible restrictions to the trusted multithreaded .NET host itself.
4. Test inherited descriptors, terminal escape paths, Unix sockets, host service access, namespace manipulation, orphan/double-fork behavior and launcher death. Determine the owned-unit termination mechanism and cgroup prerequisites if used.
5. Run the same offline .NET/Git fixture as Windows and record all required grants. Test bash/Python and then optional Cargo/npm fixtures. Dependency restore/networking are separate later profiles.
6. Verify missing helper, disabled namespaces, mount failure, seccomp failure, and unsupported resource limits all reject before target dispatch.

## macOS experiments

1. Probe a versioned GitHub macOS image for Seatbelt/sandbox-exec availability and actual process sandbox creation. Record image version, OS, architecture and runner privilege. Inventory success is not backend qualification.
2. Use disposable fixtures to prove allowed reads/writes, denied reads/writes, network denial, and inherited restrictions in child/grandchild processes. Restrict only the test process tree, preserving the Actions agent and artifact uploader.
3. Test .NET runtime/JIT behavior, dynamic libraries, symlink and path aliases, temporary directories, IPC/Mach services, code signing interactions, and stdin/stdout/stderr.
4. Establish a reliable owned process lifetime and test daemonization, root exit, cancellation, timeout, launcher crash and cleanup. Seatbelt access restrictions alone do not prove tree termination.
5. Expand to the shared offline workload suite. Add Intel and Apple Silicon evidence separately before claiming both architectures. If hosted runners cannot exercise a required feature, retain an explicit open qualification gate and investigate a disposable self-hosted Mac.

No workflow is published or dispatched yet. The first CI job should be a bounded capability probe, not the full production conformance claim.

## Shared fixture matrix

| Fixture | What it must establish |
| --- | --- |
| Denied read sentinel outside grants | Confidential data is inaccessible, not merely read-only |
| Read-only input / writable output | Positive and negative operations under the same policy |
| Runtime dependency closure | Every additional host grant is visible and caller-selected |
| Path replacement/alias suite | Policy survives supported namespace threats or rejects unsupported cases |
| Descendant worker tree | Children inherit restrictions and all owned processes stop |
| Inherited-handle/service probe | No accidental host authority via descriptors, IPC, agents, or daemons |
| Direct networking probe | No proxy bypass via ordinary TCP/UDP, IPv6, DNS, host loopback or inherited sockets |
| Flooded stdout and stderr | Bounded memory, no pipe deadlock, correct overflow result |
| Cancel/timeout/crash races | Stop during setup/run; root exit; launcher crash; verified cleanup |
| Rejected setup marker | Invalid/unsupported policy and backend failure never dispatch the target |
| Two simultaneous sandboxes | No policy/profile/scratch/ACL sharing that expands either boundary |
| Offline developer fixture | Useful build/test/inspection succeeds without unexplained privilege expansion |

Keep escape fixtures inside disposable workspaces/VMs and use non-secret sentinels. Tests must not attempt changes to unrelated host files or security settings.

## Results format

For each run record commit/tool versions, OS/kernel/filesystem, elevation/prerequisites, requested and prepared policy, fixture identity, expected outcome, observed outcome, termination/cleanup status, elapsed time, and redacted diagnostics. Report Passed, Failed, Unsupported, or NotRun distinctly. Required tests cannot become a platform pass through skips.

Do not run package installation scripts or hostile workload fixtures until the boundary itself has passed the relevant containment checks. Initial workloads should be small, controlled local fixtures with prepared dependencies.

## Open choices for the next session

- Minimum Windows versions and whether an administrator-assisted installation is acceptable.
- Linux distributions/kernel baseline and a separate Ubuntu WSL2 test distribution.
- macOS minimum version, Intel/Apple Silicon coverage, and hosted-runner probe results.
- Exact filesystem threat model, runtime-profile ownership, and denial of ungranted reads.
- Whether writes target a disposable workspace copy or a live user workspace for the first profile.
- Supported .NET TFMs and willingness to distribute a native helper alongside managed packages.
- Which operational resource limits must be mandatory beyond timeout and output bounds.

Hufu integration, semantic authorization projection, and replacing Luban are explicitly outside these gates.
