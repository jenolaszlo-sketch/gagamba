# REGRESSION-REVIEW.md — S1 separation and B1–B5 preservation

Independent evidence: the reviewer re-built from an isolated `git archive` of `8b897e4` and ran
suites there; raw logs under `evidence/`; producer TRX/logs inspected under
`evidence/verification/`.

## 1. S1 production/test separation

### Independent positive probes

`eng/test-production-build.ps1` was run in the isolated copy with Windows PowerShell. The 24 probes
that completed before a PowerShell-5.1 encoding artifact were all green:

```
order-1..7 (Release/Debug/Release/Release/Debug/Debug/Test)      build + binary inspection  PASS
fresh-debug / inspect-fresh-debug                                                PASS
owner-release/-debug/-debug-inspection, recovery-…                               PASS
solution-release, solution-debug-owner-inspection                                PASS
```

The M1 build emits the S1 gate verdict inline (`Gagamba compilation identity validated … RESULT:
PASS`), which the reviewer observed during the portable-test build.

### Independent negative gate probes

The negative probes were reproduced directly (raw byte capture) with the exact expected diagnostics:

| Probe | Override | Expected | Observed |
|---|---|---|---|
| missing-symbol | `DefineConstants=TRACE` | GAGAMBA002 | ✅ present, build failed |
| empty-symbols | `DefineConstants=` | GAGAMBA002 | ✅ |
| production-and-fixture | `DefineConstants=GAGAMBA_PRODUCTION;GAGAMBA_WP05_D6_4_TEST_FIXTURE` | GAGAMBA004 | ✅ |
| release-fixture-carrier | `GagambaWp05D64FixtureEnabled=true` | GAGAMBA004 | ✅ |
| debug-forced-production | Debug `DefineConstants=GAGAMBA_PRODUCTION` | GAGAMBA003 | ✅ |
| unknown-configuration | `-c Unclassified` | GAGAMBA001 | ✅ |
| fixture-unisolated | fixture + `AssemblyName=…TestFixture` | GAGAMBA008 | ✅ |
| debug-fixture-alone | Debug `DefineConstants=GAGAMBA_WP05_D6_4_TEST_FIXTURE` | GAGAMBA006 | ✅ |

Every negative case exited non-zero and emitted **no** `Gagamba.Execution.Linux.M1.dll`. The
producer's `windows/s1/results.json` reports 51/51 passed; the reviewer independently reproduced the
24 positive and 8 representative negative probes and inspected (but did not re-execute) the publish
family, stale-binary and ref-disabled probes. **The reviewer does not claim all 51 were executed
independently.**

### Production/test boundary

- The S2 diff added `manifest-v1.golden.json` as an embedded resource and a `.gitattributes` rule to
  keep it byte-exact; it added no test-only assembly to any production closure.
- The production verifier denies test-only M1 type/property names by **hash**
  (`InstalledPayload.ForbiddenNames`), so the forbidden literals do not appear in production source
  (S1 production byte gate). `InstalledPayload.Check` requires exactly one `AssemblyConfiguration`
  value `Release` and the M1 production type surface.
- The candidate was independently confirmed to contain no test-only artifact and no build-only
  runtime asset ($CANONICAL-MANIFEST-REVIEW.md §6).

## 2. B1–B5 regression

Independent run of the full M1 test project (isolated copy, Debug):

```
Passed! - Failed: 0, Passed: 1141, Skipped: 270, Total: 1411
```

The reviewer added 28 reviewer-authored probes to the isolated copy, so the producer-comparable
figure is **1141 − 28 = 1113 passed, 0 failed, 270 skipped**, exactly matching the producer's
"Windows M1 Debug: 1,113 passed, 0 failed, 270 skipped". Exact test-name reconciliation, not totals
alone:

- Producer portable subset (`wsl/installation-portable-rerun.trx`) = **63** cases = every
  `InstalledMetadataTests` case. The reviewer's `FullyQualifiedName~Installation` filter matched
  those 63 plus two pre-existing `RecoveryB2Tests` (`MissingOwnerInstallationRefuses`,
  `PassiveRecordCannotReconstructInstallationAuthority`), giving 65; both extra tests are unchanged
  in the S2 range (`git diff … -- RecoveryB2Tests.cs` empty).
- WSL2 transaction suite: **11/11** passed (independently re-run).
- WSL2 native filesystem test: **1/1** passed (independently re-run, non-root).
- Two `ptrace` race probes: producer evidence inspected (not re-run).

## 3. Authority invariants

- `RestrictedExecutionRuntime.Create()` still returns `new UnavailableRestrictedExecutionProvider()`
  (unchanged; `git diff df3d6f0..c9d867d` touches no `src/Gagamba.Runtime/` file).
- No `src/…/Bootstrap/`, `src/…/Lifecycle/`, `src/…/Native/` or `src/Gagamba.Execution/Restricted/`
  file changed in the reviewed range.
- S2 introduces **no** new release authority, capability, deadline, journal schema or recovery path.
  The new code is a read-only verifier plus operator tooling and tests. `InstalledArtifactInspection`
  returns no admission token and `InstalledResult` is internal.
- Recovery stop-only authority, C3 deadline, controller-loss behaviour, protected capture, journal
  authority and cleanup `Unknown` semantics are not referenced or altered.

## 4. Verdict (G) and (H)

- **(G) Production/test boundary preserved.** S1 build/qualification-hook restrictions are intact;
  the positive and representative negative gate probes were independently reproduced; no test-only
  assembly is promoted; production authority is not gained through instrumentation.
- **(H) No B1–B5 regression.** The full M1 suite reproduced the producer's pass/fail/skip counts
  exactly after accounting for the reviewer's added probes; no release, stopping, deadline or cleanup
  authority was altered.
