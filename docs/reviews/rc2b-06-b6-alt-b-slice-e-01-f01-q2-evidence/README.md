# Slice E-1 F-01 Q2 independent reviewer evidence

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F01-Q2`
Reviewed implementation: `965f203eea9b4f0f6df41e2c4979eb5b4a585d3f` (tree `7edfedd6badebda029ce1e3371445d973c6fd82b`), baseline `db39ff6a61007811e8f874b477d35ceda6bfb356` / tree `7517f050b174799dd0c1af0d973848524df88ed3`.

Independently authored reviewer probes and their observed output. Not producer artifacts.

## Contents

- `probe/Program.cs`, `probe/F01Q2Probe.csproj` — an independent reflection probe (net10.0; no compile-time reference to M1). Cases P1–P16 and R1–R4, including **swapped owner/recovery roles** and **role-specific artifact substitution** not covered by the producer's tests.
- `probe-results-committed.txt` — my probe against the committed Debug assembly (`SUMMARY failures=0`).
- `probe-results-gateremoved.txt` — my probe against my own disposable gate-removed Debug build (semantic cases no longer refuse; `failures=7`).
- `q1probe-results-committed.txt` — the retained Q1 probe against the committed Debug assembly (B5/B6/B7 `refused`).
- `q1probe-results-gateremoved.txt` — the retained Q1 probe against the gate-removed build (B5/B6/B7 `ACCEPTED`).
- `release-il-callers.txt` — distinct method labels referencing the E-1 types in the compiled RELEASE assembly (`eng/VerifyProductionAssembly --production <release.dll> --dump`).

## Reproduce

```text
# independent probe (Debug + Release M1 built):
dotnet run --project <evidence>/probe -c Release -- \
  src/Gagamba.Execution.Linux.M1/bin/Debug/net10.0/Gagamba.Execution.Linux.M1.dll \
  src/Gagamba.Execution.Linux.M1/bin/Release/net10.0/Gagamba.Execution.Linux.M1.dll \
  tests/Gagamba.Execution.Linux.M1.Tests/Installation/manifest-v1.golden.json

# gate-removal: copy src + Directory.Build.{props,targets} + eng/VerifyProductionAssembly to an
# external scratch, delete the two semantic Require statements in FromEvidence, build Debug,
# and run the probes above against the mutated DLL. Mutated Debug M1 SHA-256
# 76419C5E918570282F232B8CAC1DB3572897D1948034E8C915E32F88DC6ECA2E. Canonical source/history untouched.
```

## Limitations

Reflection reaches `internal`/`private` members and is outside the accepted E-1 threat model; it is used to inspect the boundary, not to claim a bypass. Protected/native positives are not exercised (`NATIVE-PENDING`, Slice N). `eng/test-production-build.ps1` (F-03) was not run to completion and is not claimed as passing.
