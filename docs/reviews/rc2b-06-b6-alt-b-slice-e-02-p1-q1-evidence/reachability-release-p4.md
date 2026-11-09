# P1-Q1 — independent reachability, RELEASE, and P4 findings

Date: 2026-11-09 (Asia/Manila)

## Independent reachability scanner (reviewer-authored)

The probe implements its own IL operand scanner (no producer code) and scans every method/constructor body of the loaded assembly for `InlineMethod` tokens whose target is a member of `Gagamba.Execution.Linux.M1.Validation`.

| Check | Debug assembly | Reviewer-rebuilt Release assembly |
|---|---|---|
| Validation types present | 13 | 13 |
| Exported public Validation types | 0 | 0 |
| Methods with bodies scanned | 4,253 | 4,100 |
| Call targets matched | 266 tokens | 266 tokens |
| Production caller hits | **0** | **0** |
| Scanner positive control (`TryVerifyChain` → `VerifyManifest`) | detected | detected |
| Forbidden dependency references (File/Directory/FileStream/Path/Environment/Process/Net/Interop) | 0 | 0 |

Assembly identities: Debug rebuilt at review (producer `a50bd575…`/`F63DDE2F…`; reviewer rebuild differs per accepted non-bit-reproducibility); reviewer Release `ec05b2be…` / `5B649DAE…`. The reviewer also scanned the producer-built Release (`19a06c60…` / `321F7131…`) before rebuilding: 0 hits (observed; the file was subsequently replaced by the reviewer rebuild).

## Authority-surface controls (probe)

- No authority-shaped type or member names (`Approved`, `Authorized`, `Permit`, `Authority`, `TrustedCapability`, `Capability`): 0.
- No public or nested-public Validation type: 0.
- No method parameter/return type referencing `OwnerAuthorityContext`, `LoadedInstallation`, `Grant`, or `Approval`: 0.
- No R/Wc/P- or approval-named records: confirmed.

## Scope of the reachability claim

- Proven: absence of **direct call tokens** from other M1 types in both configurations; no public export; no authority-type references.
- Not claimed: general transitive unreachability, or resistance to reflection/dynamic invocation from hostile in-process code (outside the accepted trusted-process threat model, consistent with the E-1 precedent). No E-1 consumer, final-admission consumer, protected-sink caller, or authority factory exists.

## RELEASE verification (reviewer-executed)

| Command | Result |
|---|---|
| `dotnet run --project eng/VerifyProductionAssembly -c Release -- --production <reviewer Release M1>` | RESULT: PASS, exit 0; 0/4 hook types, 0/3 hook properties, 14/14 core types |
| Same with the Debug assembly | RESULT: FAIL, exit 1 (expected), naming the accepted forbidden surfaces |

`eng/` unchanged; no verifier policy modified.

## P4 classification

**P4-NEUTRAL.** The implementation diff adds only four new internal production files and six new test files outside every existing execution path. Independently re-run full suite: 1,484 passed / 0 failed / 331 skipped. No protected-sink caller, admission path, authority gate, lifecycle path, first protected effect, or RELEASE behavior changed; RELEASE verifier PASS; Debug-as-production FAIL preserved. F-R1/O-R1, O-R2, O-R3, the observation-to-effect residual, `NATIVE-PENDING`, and the 331 native skips are preserved; the skips remain unqualified.
