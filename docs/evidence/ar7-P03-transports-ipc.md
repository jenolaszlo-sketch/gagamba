# AR-7 P03 — Linux transports and host IPC

Date: 2026-10-10. Slice: defensive feasibility only. **Conclusion: SUPPORTED BY THIS PROBE**, limited to this exact host, frozen composition, and tested endpoints.

## Source binding and environment

| Field | Recorded value |
| --- | --- |
| Production baseline | `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`; frozen branch/tag unchanged; no `src`/`eng`/packaging drift |
| Design | [P03 design](../reviews/2026-10-10-ar7-p03-design.md) (reviewed; two clarification deltas: raw-socket leg, io_uring positive gate, metadata high port) |
| Exact executed probe revision | `229de9e1fc14074bf56f256cb3fbae2e3577bee2`, clean at release |
| Prior setup observations (no target released) | `b70ec3f2` (abstract-socket stale-path guard), `a0d51fae` (positive fd/ping gate held release), `522d7d8f` (seccomp-fd exec + argv fix) |
| Nonce (executed run) | `70edf2c2242e4aea905e61854775b4a9` |
| Host | Ubuntu 26.04.1 LTS on WSL2, x86_64, kernel `6.6.114.1-microsoft-standard-WSL2`, glibc 2.43, GCC 15.2.0, Python 3.14.4 |
| Helper | Bubblewrap `0.11.1`, SHA-256 `523da3e7…`, mode `0755` non-setuid (pin-gated before release) |
| Filter | Frozen P02 cBPF bytes, SHA-256 `30bfaf6b…` (matches P02 record: composition byte-identical) |
| Candidate identity | Host UID/GID 65534, zero caps, `NoNewPrivs:1`, `Seccomp:2/filters:1` (externally sampled) |
| Worker (static) SHA-256 | `cd5db2cb…` |

Probe source is [p03.c](../../spikes/Ar7Feasibility/p03.c) with [driver](../../spikes/Ar7Feasibility/p03.py). Durable raw record: [P03 observations](ar7-feasibility/P03/70edf2c2242e4aea905e61854775b4a9.json) plus [independent cleanup](ar7-feasibility/P03/independent-cleanup.json). Full transcripts print to the run record; binaries stay under ignored `artifacts/ar7/P03/`.

## Question and composition

Does the surviving (P02-frozen) Linux candidate prevent all unprepared IP
networking and host-service IPC while preserving explicitly prepared data
channels? The candidate invocation, namespaces, capability drop, mounts,
cgroup leaves, and seccomp filter are byte-identical to P02; P03 adds no
mechanism. The io_uring syscall family is deliberately **not** filtered, so
the ring path tests whether the network namespace, zero-capability ceiling,
and userns-disablement hold without filter help.

## Positive controls (ordinary worker, host netns, unrestricted; gate held)

All ten oracle endpoints exchanged over **both** the direct-syscall path and
the io_uring path (`SOCKET→CONNECT→SEND→RECV` with verified field layout):
IPv4/IPv6 TCP, UDP, DNS over UDP and TCP (ID match), proxy `CONNECT` (200),
simulated metadata at provisioned `169.254.169.254` (200 + document),
pathname/abstract Unix sockets with peer-credential gate, agent framing
(empty identities answer), distinguished daemon token. Unprivileged ICMP echo
to loopback completed direct and via ring. Host `lo` UP at index 1 (netlink
dump). stdin/stdout nonce echo completed with stdio-only FDs. Manager limits
unchanged. Every oracle entry matched; gate `true`; only then was the
candidate released.

## Restricted results (candidate did useful work: input/scratch/echo, exit 0)

| Endpoint | Direct path | io_uring path | Barrier that held |
| --- | --- | --- | --- |
| IPv4/IPv6 TCP | `socket` EPERM | socket created; connect ECONNREFUSED (own net context, nothing listening) | Filter, then net namespace |
| UDP + DNS-UDP | `socket` EPERM | socket created; connected; send buffered; recv ECONNREFUSED | Filter, then net namespace |
| Proxy | `socket` EPERM | socket created; connect ECONNREFUSED | Filter, then net namespace |
| Metadata link-local | `socket` EPERM | socket created; connect ENETUNREACH (no route to link-local) | Filter, then net namespace (no interface route) |
| Pathname Unix / daemon | `socket` EPERM | socket created; connect ENOENT (candidate mount view lacks the paths) | Filter, then mount namespace |
| Abstract Unix | `socket` EPERM | socket created; connect ECONNREFUSED (abstract names are netns-scoped) | Filter, then net namespace |
| Raw (`SOCK_RAW`) | `socket` EPERM (both modes: documented symmetry, not a counted denial) | creation EPERM (want of `CAP_NET_RAW`) | Capability ceiling |
| Unprivileged ping | `socket` EPERM (filter covers nr 41 entirely) | creation denied EACCES; no packet sent | Filter, then capability/group-namespace check |
| netlink / bring-lo-up | `socket` EPERM | not reached (no socket) | Filter (capability check untested, correctly unclaimed) |
| userns/netns/mount/setns | EPERM (or ENOENT visibility for `/sys`, init-root path) | n/a | Filter + namespaces; `setns` authorization comparison stays `INCONCLUSIVE` (positive also fails) |

**Oracle: zero candidate-phase entries, zero anomalies, prepared stdio echo
functional, FD inventory stdio-only, limits unchanged, target domain empty
after return.** The ring path proves socket creation is reachable past the
frozen filter — the denial property comes from the composition (own network
namespace with no host routes/listeners, empty mount view, zero capabilities),
not from one syscall number. `ENETUNREACH`/`ECONNREFUSED`/`ENOENT` describe
those namespace barriers, exactly as the design requires; none is presented
as a filter denial.

## Cleanup and provisioning

Root provisioned only: workspace, static compile, helper pin, cgroup tree
(outer 256 MiB/swap 0/64 tasks; delegated 128 MiB/48 tasks), loopback
metadata address (added rc 0, removed rc 0). No sysctl/user/service/firewall
changes. Kill via outer `cgroup.kill`, reaped, `populated 0` required before
removing only known identities; independent check confirms all four run
nonces' workspaces/cgroups/endpoint names absent. Watchdog never fired.

## Review, consequence, and non-claims

No Luna read-only challenge has yet covered this run; that review is pending
and its absence is recorded here rather than treated as qualification. The
follow-up discipline from P02 (review-driven variants, exact-source binding)
was applied by construction.

This candidate survives P03 for P04/P07 on this host. It does not establish
general Linux sandbox security, complete profile qualification, `.NET`/
resource/storage/crash behavior, other kernels/distros/LSM states, or
production readiness. No candidate repair was performed or is authorized on
failure; no production code changed.

## Historical 2026-10-09 stop snapshot (superseded scheduling only; preserved verbatim below)

# AR-7 P03 — Offline transports and host IPC

Date: 2026-10-09. Slice: architecture-blocking feasibility probes. **Conclusion: NOT RUN.**

## Source binding and environment

- Frozen production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, `qualification/ar6-native`, `ar6-review-baseline-06c8ac7`; unchanged.
- Review/start revision: `8e156774fec28d807b557acae487791289369609`.
- Probe branch: `qualification/ar7-feasibility`. No P03 harness exists and there is **no tested P03 probe commit or helper binary/hash**.
- Last executed probe source: P01 `ee170724c14d8bd138cec1a461d86eb8bec76ec6`, clean at release. P01 evidence checkpoint: `9b95dd58d4a62f4fee258110f58d9000b7e1d220`, clean before these documentation-only records. Neither revision qualifies P03.
- Available caller host: Windows `10.0.26200.0`, x64, PowerShell `7.6.5`, MSVC `14.51.36231`, Windows SDK `10.0.26100.0`, .NET SDK `10.0.401`. This is inventory from P01, not a P03 native run.
- P03 native OS/kernel/libc, runtime, UID/token, privileges, provisioning, prerequisites and helper: **not measured**. No administrator/root setup was performed for P03.
- Scope: honest not-run record after mandatory P01 hard stop. Exclusions: every P03 guarantee and any inherited AR-6 security qualification.

## Question and mechanism

Does a surviving candidate prevent communication beyond explicitly prepared anonymous data channels?

Planned candidate: Surviving Windows/Linux candidates only. No TCP/UDP, DNS/proxy, simulated metadata, inherited socket, Unix/agent-like socket, raw/netlink or further broker transport matrix was run. P01's global ARAP broker counterexample is relevant IPC evidence, but is not a completed P03 matrix.

## Positive control, restricted result and external oracle

None executed. There is no positive control or restricted result for P03; no absent endpoint, prior lifecycle test, successful setup or outer restriction is counted as denial. [P01](ar7-P01-windows-lpac.md) records the actual counterexample and external oracle. Do not relabel it as a passed P03.

## Cleanup

No P03 workloads, listeners, profiles, mounts, cgroups, services or workspaces were created; there is no P03 residue to remove. P01 cleanup was independently verified before this stop record. This is not a P03 cleanup test.

## Stop reason and architecture consequence

The user specification section 24 requires immediate stop when a Windows/Linux candidate contradicts a central Astra assumption. P01's ungranted global ARAP endpoint created a host-authority child outside the candidate Job, contradicting the tested LPAC + Job closure/IPC composition. No architecture patch or additional platform experiment followed. The normal sequential backlog does not override that explicit hard stop.

The tested Windows LPAC + Job composition already permits an ungranted host broker path. It is not a surviving candidate for P03 without Astra revising the architecture. Linux viability remains Pending; no network-denial claim is made.

## Luna review and non-claims

Luna reviewed the completed P01 run read-only and confirmed the counterexample, positive controls, cleanup and narrow interpretation. There is no executed P03 to review; this not-run disposition is not independent native qualification. The consolidated hold/status records receive a final read-only documentation challenge. No final architecture selection, production/public API/release change, Hufu dependency, privileged persistent helper, publication or merge occurred. See [consolidated disposition](restricted-execution-AR-7-feasibility.md).
