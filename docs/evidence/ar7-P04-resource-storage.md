# AR-7 P04 — Resources and complete writable storage

Date: 2026-10-09. Slice: architecture-blocking feasibility probes. **Conclusion: NOT RUN.**

## Source binding and environment

- Frozen production baseline: `06c8ac7eff6b968804a5cdd6c651f1a0c6845d56`, `qualification/ar6-native`, `ar6-review-baseline-06c8ac7`; unchanged.
- Review/start revision: `8e156774fec28d807b557acae487791289369609`.
- Probe branch: `qualification/ar7-feasibility`. No P04 harness exists and there is **no tested P04 probe commit or helper binary/hash**.
- Last executed probe source: P01 `ee170724c14d8bd138cec1a461d86eb8bec76ec6`, clean at release. P01 evidence checkpoint: `9b95dd58d4a62f4fee258110f58d9000b7e1d220`, clean before these documentation-only records. Neither revision qualifies P04.
- Available caller host: Windows `10.0.26200.0`, x64, PowerShell `7.6.5`, MSVC `14.51.36231`, Windows SDK `10.0.26100.0`, .NET SDK `10.0.401`. This is inventory from P01, not a P04 native run.
- P04 native OS/kernel/libc, runtime, UID/token, privileges, provisioning, prerequisites and helper: **not measured**. No administrator/root setup was performed for P04.
- Scope: honest not-run record after mandatory P01 hard stop. Exclusions: every P04 guarantee and any inherited AR-6 security qualification.

## Question and mechanism

Can a surviving candidate enforce finite useful resources and bound every meaningful writable location without an unreviewed privileged service?

Planned candidate: Surviving Windows/Linux candidates only. No finite scratch-store provisioning, profile/registry storage accounting, Job memory/process limits, memory/pids/swap/tmpfs/inode/FD controls, above-budget stress or self-contained .NET workload was run. The P01 Job's kill-on-close and fixed synthetic workload bounds are not P04 limits.

## Positive control, restricted result and external oracle

None executed. There is no positive control or restricted result for P04; no absent endpoint, prior lifecycle test, successful setup or outer restriction is counted as denial. [P01](ar7-P01-windows-lpac.md) records the actual counterexample and external oracle. Do not relabel it as a passed P04.

## Cleanup

No P04 workloads, listeners, profiles, mounts, cgroups, services or workspaces were created; there is no P04 residue to remove. P01 cleanup was independently verified before this stop record. This is not a P04 cleanup test.

## Stop reason and architecture consequence

The user specification section 24 requires immediate stop when a Windows/Linux candidate contradicts a central Astra assumption. P01's ungranted global ARAP endpoint created a host-authority child outside the candidate Job, contradicting the tested LPAC + Job closure/IPC composition. No architecture patch or additional platform experiment followed. The normal sequential backlog does not override that explicit hard stop.

All resource/storage cells remain Pending. Windows writable profile/registry state and Linux task-versus-process accounting need future evidence. The proposed 128 MiB budget has not been measured or raised.

## Luna review and non-claims

Luna reviewed the completed P01 run read-only and confirmed the counterexample, positive controls, cleanup and narrow interpretation. There is no executed P04 to review; this not-run disposition is not independent native qualification. The consolidated hold/status records receive a final read-only documentation challenge. No final architecture selection, production/public API/release change, Hufu dependency, privileged persistent helper, publication or merge occurred. See [consolidated disposition](restricted-execution-AR-7-feasibility.md).
