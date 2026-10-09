# AR-7 P07 — Crash, root status and release ownership

Date: 2026-10-09. Slice: architecture-blocking feasibility probes. **Conclusion: NOT RUN.**

## Source binding and environment

- Frozen production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, `qualification/ar6-native`, `ar6-review-baseline-06c8ac7`; unchanged.
- Review/start revision: `8e156774fec28d807b557acae487791289369609`.
- Probe branch: `qualification/ar7-feasibility`. No P07 harness exists and there is **no tested P07 probe commit or helper binary/hash**.
- Last executed probe source: P01 `ee170724c14d8bd138cec1a461d86eb8bec76ec6`, clean at release. P01 evidence checkpoint: `9b95dd58d4a62f4fee258110f58d9000b7e1d220`, clean before these documentation-only records. Neither revision qualifies P07.
- Available caller host: Windows `10.0.26200.0`, x64, PowerShell `7.6.5`, MSVC `14.51.36231`, Windows SDK `10.0.26100.0`, .NET SDK `10.0.401`. This is inventory from P01, not a P07 native run.
- P07 native OS/kernel/libc, runtime, UID/token, privileges, provisioning, prerequisites and helper: **not measured**. No administrator/root setup was performed for P07.
- Scope: honest not-run record after mandatory P01 hard stop. Exclusions: every P07 guarantee and any inherited AR-6 security qualification.

## Question and mechanism

Can the surviving setup topology avoid released-plus-unowned execution and report target root status separately from complete-domain state?

Planned candidate: Windows creation-time Job comparison and Linux helper/guardian topology after a viable authority candidate. No manager/helper/guardian/init crash, parent-death arming, blocked/drained pipe, root-exit-with-descendants, guardian attack or keepalive-FD experiment was run. P01 creation-time Job association and normal-run Job counts do not qualify these races.

## Positive control, restricted result and external oracle

None executed. There is no positive control or restricted result for P07; no absent endpoint, prior lifecycle test, successful setup or outer restriction is counted as denial. [P01](ar7-P01-windows-lpac.md) records the actual counterexample and external oracle. Do not relabel it as a passed P07.

## Cleanup

No P07 workloads, listeners, profiles, mounts, cgroups, services or workspaces were created; there is no P07 residue to remove. P01 cleanup was independently verified before this stop record. This is not a P07 cleanup test.

## Stop reason and architecture consequence

The user specification section 24 requires immediate stop when a Windows/Linux candidate contradicts a central Astra assumption. P01's ungranted global ARAP endpoint created a host-authority child outside the candidate Job, contradicting the tested LPAC + Job closure/IPC composition. No architecture patch or additional platform experiment followed. The normal sequential backlog does not override that explicit hard stop.

Crash ownership remains Pending. Bubblewrap versus bespoke helper, independently enforceable stop, guardian authority and root-versus-wrapper status remain unresolved; Unknown must remain Unknown.

## Luna review and non-claims

Luna reviewed the completed P01 run read-only and confirmed the counterexample, positive controls, cleanup and narrow interpretation. There is no executed P07 to review; this not-run disposition is not independent native qualification. The consolidated hold/status records receive a final read-only documentation challenge. No final architecture selection, production/public API/release change, Hufu dependency, privileged persistent helper, publication or merge occurred. See [consolidated disposition](restricted-execution-AR-7-feasibility.md).
