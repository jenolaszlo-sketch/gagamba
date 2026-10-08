# Independent-review evidence, 2026-10-08

Baseline source: `8dea7d86c8ee2f1c924100acdbc1bf5dc14b2a88`, version `0.1.0-preview.4`. See the [review](../2026-10-08-independent-review.md) and [Sol plan](../../review-remediation-sol.md).

- [Manifest](evidence.json): environment, 33 passing existing assertions and E1–E10 observations. This is a summary of observed results, not a fresh run.
- [Input hashes](source-sha256.txt): files copied for the original audit builds.
- [Windows/pure probes](probe/Program.cs) and [project](probe/Probe.csproj): E1–E7.
- [Linux native probes](linux-probe/Program.cs) and [project](linux-probe/LinuxProbe.csproj): E8–E10; [captured output](linux-probe-results.txt).
- Original TRX: [contract](test-results/contract.trx), [Windows](test-results/windows.trx), [Windows conformance](test-results/conformance.trx).

No binaries or source snapshots are archived here. The review originally built an isolated source copy. Only the archived probe project include paths were changed to resolve repository source. Original TRX/manifest machine paths remain historical data; no checkout depends on those paths.

From the repository root, run the Windows probe with:

```text
dotnet run --project docs/reviews/2026-10-08-evidence/probe/Probe.csproj -c Release
```

On a compatible Linux host with explicit rights to create the unique test cgroup parent:

```text
dotnet run --project docs/reviews/2026-10-08-evidence/linux-probe/LinuxProbe.csproj -c Release
```

These probes exercise current source, so outcomes will change as fixes land. To reproduce the original findings, use a separate checkout of the baseline SHA and the matching source hashes; do not reset active development. Build output belongs in ignored bin/obj or an external artifacts directory. These are test-only experiments, not public execution helpers.

E1–E4 ran on Windows. E5–E7 are pure-code observations, not native macOS qualification. E8–E10 ran in root Ubuntu WSL2; that does not qualify unprivileged delegation. The Linux probe creates a uniquely named cgroup parent, confines its migration experiment to that parent, and removes its own resources. Use finite workloads and independent cleanup when promoting cases into regressions. Review output is never proof that a later revision is fixed.