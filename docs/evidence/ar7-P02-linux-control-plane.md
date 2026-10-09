# AR-7 P02 — Linux same-UID control-plane denial

Date: 2026-10-09. Slice: architecture-blocking feasibility probes. **Conclusion: NOT RUN.**

## Source binding and environment

- Frozen production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, `qualification/ar6-native`, `ar6-review-baseline-06c8ac7`; unchanged.
- Review/start revision: `8e156774fec28d807b557acae487791289369609`.
- Probe branch: `qualification/ar7-feasibility`. No P02 harness exists and there is **no tested P02 probe commit or helper binary/hash**.
- Last executed probe source: P01 `ee170724c14d8bd138cec1a461d86eb8bec76ec6`, clean at release. P01 evidence checkpoint: `9b95dd58d4a62f4fee258110f58d9000b7e1d220`, clean before these documentation-only records. Neither revision qualifies P02.
- Available caller host: Windows `10.0.26200.0`, x64, PowerShell `7.6.5`, MSVC `14.51.36231`, Windows SDK `10.0.26100.0`, .NET SDK `10.0.401`. This is inventory from P01, not a P02 native run.
- P02 native OS/kernel/libc, runtime, UID/token, privileges, provisioning, prerequisites and helper: **not measured**. No administrator/root setup was performed for P02.
- Scope: honest not-run record after mandatory P01 hard stop. Exclusions: every P02 guarantee and any inherited AR-6 security qualification.

## Question and mechanism

Can a genuinely non-root, same-host-UID namespace/helper composition prevent target access to manager cgroups, namespaces and resources?

Planned candidate: Linux rootless pinned non-setuid Bubblewrap or small-launcher composition. No Linux experiment, UID switch, delegation, cgroup, namespace, seccomp filter or synthetic daemon was created. Current Linux prerequisites and helper hashes were not inventoried in this assignment. Prior AR-2/AR-6 host evidence is lifecycle-only and is not P02 evidence.

## Positive control, restricted result and external oracle

None executed. There is no positive control or restricted result for P02; no absent endpoint, prior lifecycle test, successful setup or outer restriction is counted as denial. [P01](ar7-P01-windows-lpac.md) records the actual counterexample and external oracle. Do not relabel it as a passed P02.

## Cleanup

No P02 workloads, listeners, profiles, mounts, cgroups, services or workspaces were created; there is no P02 residue to remove. P01 cleanup was independently verified before this stop record. This is not a P02 cleanup test.

## Stop reason and architecture consequence

The user specification section 24 requires immediate stop when a Windows/Linux candidate contradicts a central Astra assumption. P01's ungranted global ARAP endpoint created a host-authority child outside the candidate Job, contradicting the tested LPAC + Job closure/IPC composition. No architecture patch or additional platform experiment followed. The normal sequential backlog does not override that explicit hard stop.

Linux same-UID control-plane feasibility remains Pending. Landlock, separate UID, disposable privileged provisioning, VM and Bubblewrap versus bespoke launcher remain decisions for Astra; none was implemented.

## Luna review and non-claims

Luna reviewed the completed P01 run read-only and confirmed the counterexample, positive controls, cleanup and narrow interpretation. There is no executed P02 to review; this not-run disposition is not independent native qualification. The consolidated hold/status records receive a final read-only documentation challenge. No final architecture selection, production/public API/release change, Hufu dependency, privileged persistent helper, publication or merge occurred. See [consolidated disposition](restricted-execution-AR-7-feasibility.md).
