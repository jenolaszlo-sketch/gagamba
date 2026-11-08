# F-02-PARITY-01 evidence index

Commission: `GAGAMBA-RC2B-06-B6-ALT-B-SLICE-E-01-F02-PARITY-01` (test-only parity controls).
Report: [`../rc2b-06-b6-alt-b-slice-e-01-f02-parity-01.md`](../rc2b-06-b6-alt-b-slice-e-01-f02-parity-01.md)

Environment: Windows (win32), .NET 10.0, canonical checkout `C:\Users\Laszlos\source\repos\Gagamba`
on `main`; no push. M1 assembly under test: Debug (instrumented), MVID
`9d312b37-9461-4226-89e8-a03b962d21db`, SHA-256
`5AC95F272B678F140230EF21E32A1BC2C7CFC27A58C87F6896808DB86FE17C29` (post-change source).

| File | Content |
|---|---|
| `baseline.txt` | Verified starting HEAD/tree/branch/worktree/untracked state and authorized change list. |
| `source-hashes.txt` | SHA-256 of the new test file and the two comment-only modified sources (working tree = committed blob content). |
| `production-diff.txt` | `git diff` of the two verification-method files: comment lines only, no code tokens. |
| `parity-focused.txt` | Focused run of the 10 new parity tests: `Passed: 10, Failed: 0, Skipped: 0`. |
| `full-suite.txt` | Full Windows M1 suite excerpt: `Failed: 0, Passed: 1401, Skipped: 331, Total: 1732`; 0 build warning/error lines. |
| `focused-e1-class.txt` | `PrepublicationIntegrityProjectionTests` (E-1/F-01 class): `Passed: 23, Failed: 0, Skipped: 0`. |
| `focused-installed-authority-release.txt` | Focused installed/authority/release-generation/final-acceptance result: `Passed: 248, Failed: 0, Skipped: 32`. |
| `release-verifier.txt` | `eng/VerifyProductionAssembly --production` on Release M1: `RESULT: PASS`, exit 0 (0/4 hook types, 0/3 hook properties, 14/14 core types). |
| `debug-negative-control.txt` | Same verifier on Debug M1: `RESULT: FAIL`, exit 1, naming `PrepublicationIntegrityProjection::ForTest` among forbidden Debug-only factories. |
| `mutation-controls.txt` | Producer-run external mutation controls M-00..M-11: description, observed summary, failing tests, build errors. |
| `mutation-controls-assertions.txt` | Per-mutation extracted failure lines showing each detected mutation failed the intended assertion. |
| `mutation-harness.ps1` | The disposable harness used for the mutation runs (operates only on an external scratch copy). |
| `commands.txt` | Exact commands used for each evidence run. |

## Mutation method and provenance

- Scratch copy: `src/`, `tests/`, `eng/`, `Directory.Build.props`, `Directory.Build.targets` copied
  outside the repository to `C:\Users\Laszlos\AppData\Local\Temp\opencode\f02-parity-mutation`.
  The canonical checkout was never mutated; the scratch tree was hash-compared against canonical
  before the final run and deleted after extraction.
- Each mutation: apply to the scratch copy, run the real parity suite
  (`dotnet test … --filter FullyQualifiedName~InstalledVerificationParityTests`), restore pristine.
- Harness notes (for reproducibility): `Copy-Item` preserves timestamps, so the harness explicitly
  bumps `LastWriteTime` after each restore/mutation to force the M1 rebuild — an early attempt
  without this fix produced a stale-build false result and was discarded. One early injection
  attempt (`_ = InstalledMetadata.AcceptRecord(...)`) was invalid C# (`CS8209`); the corrected
  statement-only injection is what `mutation-harness.ps1` contains and what M-03 records.
- M-08 (move `CheckHost` after `return`) is rejected by the repository build policy
  (`TreatWarningsAsErrors` turns `CS0162` into an error) and never reaches a test run.
- M-09 (call-preserving conditional bypass) is intentionally recorded as **not detected**: the
  static token scan proves call presence/order, not branch reachability (see report section 11).

## Status

F-02 remains **open**. These controls are change-detectors for the accepted Option B contract, not
a proof of behavioral equivalence; provisioned-store behavioral parity and the runtime loaded-role
check remain Slice N (`NATIVE-PENDING`) obligations. Independent qualification must construct its
own mutations (do not rerun this suite only).
