# Slice E-1 independent Q1 reviewer evidence

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-Q1`
Reviewed implementation: `2259fce59b44b6af6448a38685a0635d560d0900` (tree `77610b49ecae5398d633f1db43d4f01604694ee1`), baseline `61253e9f099637db7796c364be5c985772c6594a` / tree `c0d5ed50d01cc831891e33f0fd00f1168e8ce821`.

This directory retains the independently authored reviewer probes and their observed output. It is not a producer artifact.

## Contents

- `probe/Program.cs`, `probe/E1Probe.csproj` — an independent reflection probe (targets `net10.0`; does **not** reference `Gagamba.Execution.Linux.M1` at compile time). It loads the compiled Debug and RELEASE assemblies, reconstructs the `PrepublicationIntegrityProjection` construction surface, exercises the internal `FromEvidence` cross-check, the Debug `ForTest` seam, `Verify()` refusal, and RELEASE seam separation.
- `probe-results.txt` — the probe's observed output (all sections A–D; `SUMMARY failures=0`; rows B5–B7 record the internal cross-check gap).
- `pssrepro.ps1`, `pssrepro-results.txt` — an isolated reproduction of `eng/test-production-build.ps1`'s `Invoke-Probe` `missing-symbol` step under Windows PowerShell 5.1, demonstrating the mixed-encoding log defect.
- `release-il-callers.txt` — distinct method labels that reference the E-1 types in the compiled RELEASE assembly, extracted from `eng/VerifyProductionAssembly --production <release.dll> --dump` (the IL-level reachability graph).

## Reproduce

From the repository root, with Debug and RELEASE `Gagamba.Execution.Linux.M1.dll` built:

```text
dotnet run --project <evidence>/probe -c Release -- \
  src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll \
  src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll \
  tests/Gagamba.Execution.Linux.M1.Tests/Installation/manifest-v1.golden.json
```

PowerShell discrepancy reproduction:

```text
powershell -NoProfile -ExecutionPolicy Bypass -File <evidence>/pssrepro.ps1
```

IL reachability:

```text
dotnet run --project eng/VerifyProductionAssembly -c Release -- --production <release.dll> --dump > dump.txt
grep -E "PrepublicationIntegrityProjection|InstalledIntegrityEvidence|VerifyActiveIntegrity" dump.txt
```

## Limitations

The probe uses reflection to reach `internal`/`private` members. Reflection is outside the accepted E-1 threat model (ordinary callers and untrusted evidence); it is used only to inspect the boundary, not to claim a bypass. Protected/native positives are not exercised here (Slice N) and remain `NATIVE-PENDING`.
