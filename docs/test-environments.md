# Local-first test environments

Updated 2026-10-03 following the user clarification: Windows, Linux, and macOS are all targets. Start locally on Windows. No integration with Hufu is part of this work.

## Recommended order

| Stage | Environment | Purpose |
| --- | --- | --- |
| 1 | This Windows host | API availability, minimal isolated process, I/O and lifetime, then controlled developer fixtures |
| 2 | Separate Ubuntu WSL2 distribution | Fast Linux backend iteration with namespace and seccomp probes |
| 3 | GitHub-hosted macOS image | Capability probe, then Seatbelt and lifetime fixtures; no local Mac currently identified |
| 4 | Native Linux VM/CI and expanded macOS/Windows matrix | Qualify actual OS/kernel/architecture combinations and regressions |
| Secondary | Docker Desktop Linux containers | Reproducible dependencies and nested-container compatibility, separately labeled |

## Local inventory — observed, not inferred

Read-only `wsl --status`, `wsl --list --verbose`, and `docker version` checks succeeded outside the assistant's restricted execution wrapper. Initial access-denied results were execution-context restrictions, not proof of broken installations.

- WSL default version: 2.
- Listed distributions: `docker-desktop` (running) plus a separately
  installed `Ubuntu` (Stopped when idle), both version 2.
- Docker Desktop: 4.75.0; Docker client/server: 29.5.2; Linux engine architecture: amd64.
- Engine kernel: `6.6.114.1-microsoft-standard-WSL2`.
- Prior Windows inventory: build 26200.9457, .NET SDK 10.0.401, `processmodel.dll` 10.0.26100.9444. The export and launch were later tested (GW-1A/GW-1B, see [evidence](evidence/windows-slice4-GW-1B.md)); several engine gates remain open.

## Ubuntu WSL2 setup for GL-1A (installed 2026-10-05, no elevation needed)

Installed via `wsl --install -d Ubuntu --no-launch` (Store distribution,
per-user; the WSL component already existed for Docker, so no
elevation prompt). Verified with `wsl -d Ubuntu -u root` (avoids the
interactive first-run user setup; probing runs as root):

- Distro: Ubuntu 26.04.1 LTS, WSL version 2, real kernel above.
- PID 1 is `systemd` (full system, not container-lite).
- **cgroup v2 unified**: `cgroup2 on /sys/fs/cgroup` (`cgroup2fs`).
  Controllers present: `cpuset cpu io memory hugetlb pids rdma`;
  root `subtree_control` enables `cpu memory hugetlb pids rdma`.
  This decides the GL-1A design space: cgroup-based kill (incl.
  `cgroup.kill`) is available to test against POSIX process-group
  semantics; supervisor death is the critical discriminator.
- Toolchain: `python3` present; no `gcc`, no `dotnet` (GL-1A probes use
  shell + python3; `apt` install only if C becomes necessary).
- Default distribution left as `docker-desktop` (unchanged); the Ubuntu
  instance is addressed explicitly (`-d Ubuntu`) and stays Stopped idle.

Do not develop inside Docker's internal WSL distribution. A separate Ubuntu WSL2 distribution is installed (see above); keep source and fixtures in its Linux filesystem when Linux setup begins. No distribution, image, package, or host security configuration was installed or changed by this planning update (the Ubuntu install itself is recorded above as the GL-1A prerequisite it is).

## WSL2 and Docker serve different test purposes

WSL2 uses a real Linux kernel in a managed VM and is a good first Linux development environment. Keep source and fixtures in its Linux filesystem; `/mnt/c` introduces Windows-backed filesystem semantics that need separate coverage. WSL's host interoperability and mounts must not become escape paths accessible to the target process. Qualification elsewhere still requires its own evidence. [Microsoft WSL comparison](https://learn.microsoft.com/en-us/windows/wsl/compare-versions)

Docker adds an outer security boundary. Default seccomp/capability restrictions can prevent nested namespace or mount operations, so a bubblewrap failure inside a container may reflect Docker configuration. Conversely, an outer Docker denial cannot prove Gagamba itself blocked the operation. Use an unsandboxed control inside the same disposable container for each sentinel, then show the Gagamba child is denied. Record any changed outer privileges separately; do not silently use privileged mode as the default. [Docker seccomp documentation](https://docs.docker.com/engine/security/seccomp/)

Docker on Windows runs Linux workloads and cannot qualify a native macOS backend. Do not mount the Docker socket or host credentials into a sandbox fixture. For the first tests, use controlled local files, not actual private data.

## GitHub macOS feasibility

GitHub provides macOS VMs with administrative tooling; its ARM64 runner limitation on nested virtualization is distinct from imposing Seatbelt restrictions on a child process. This supports trying a process-sandbox probe but is not a guarantee that a specific profile/API works. [GitHub runner reference](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)

There is concrete upstream precedent: Anthropic's sandbox-runtime integration workflow configures macOS Intel and ARM64 jobs and invokes its tests there. This is evidence of configured CI usage, not an audit of successful runs or a qualification of Gagamba. [Upstream integration workflow](https://github.com/anthropic-experimental/sandbox-runtime/blob/main/.github/workflows/integration-tests.yml)

Begin with an explicit macOS version/architecture label available at implementation time. Record the exact image revision, since labels receive updates. First check sandbox availability, then positive and negative access fixtures. A missing mechanism is Unsupported/NotRun, never a passing sandbox test. Do not wrap the Actions agent itself in Gagamba or globally disable runner networking.

Keep probe jobs bounded and use synthetic sentinels with no repository secrets. Test actual target privilege, not just setup privileges. A local remote repository and workflow dispatch are not created by this plan. Intel and Apple Silicon results remain separate claims.

## Acceptance boundaries

- Inventory is complete only for this Windows host's WSL/Docker setup.
- Windows sandbox launch has been tested (GW-1B); Linux namespace creation and macOS CI execution remain untested.
- Unit tests can run on broad CI images; security conformance must run where the selected backend really exists.
- A Windows Server runner result does not establish availability of the experimental Windows 11 API.
- No three-platform support claim until each required backend profile passes independently.