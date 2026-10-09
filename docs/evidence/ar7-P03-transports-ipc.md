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
