# Entry evidence, code dependencies and exit checks

## Source and project inspection

All source references are from the canonical baseline
`38e7c212d31211bb817f042bdd2737398726ce90`. This was a read-only
inspection. The producer must prove compiled isolation and execution
outcomes on the future exact implementation revision.

| Source | Observation and consequence |
|---|---|
| [IntegratedB3Client.cs](../../../src/Gagamba.Execution.Linux.M1/Bootstrap/IntegratedB3Client.cs) lines 63–66 | Installed integrated preparation chooses v2 and omits the two B2 arguments. Blob `94d3427cd5956bfbb00fd9d2c05a8ffdf3908cfb`. |
| [RecoveryConnection.cs](../../../src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryConnection.cs) line 33 and [QualifiedRecoveryConnection.cs](../../../src/Gagamba.Execution.Linux.M1/Bootstrap/QualifiedRecoveryConnection.cs) lines 15–25, 74 | Version 2 calls the qualified start path. The copied config retains a null B2 identity and selects a null B2 deadline for integrated. Default serialization includes nullable members. Serializer blob `2f6f779d6b5414c22790a7fb8a892b253e044963`. |
| [RecoveryProtocol.cs](../../../src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryProtocol.cs) lines 129–177 and [RecoveryHost.cs](../../../src/Gagamba.Execution.Linux.M1/Bootstrap/RecoveryHost.cs) line 37 | Live recovery uses the existing decoder. The deserializer recognizes the B2 names as CLR properties despite unmapped-member rejection. The integrated guard examines deserialized null values, so explicit null can pass the presence gate. It accepts version 1 or 2 only. Protocol blob `f19017dccce5b83d42bb9276c6e8839d13564061`. |
| [InstalledManifest.cs](../../../src/Gagamba.Execution.Linux.M1/Installed/InstalledManifest.cs) and [InstallationIdentity.cs](../../../src/Gagamba.Execution.Linux.M1/Installed/InstallationIdentity.cs) | The legacy path uses InstallJson for duplicate handling and InstallationIdentity for its qualified model. Duplicate handling alone does not enforce the v3 contract. The model requires Qualified true and its prior member inventory. These are read-only references, with no dependency authorized for the new codec. |
| [InstallationWire.cs](../../../src/Gagamba.Execution.Linux.M1/Bootstrap/InstallationWire.cs) and [InstallationWireTests.cs](../../../tests/Gagamba.Execution.Linux.M1.Tests/Installation/InstallationWireTests.cs) | Qualified echo and expectation handlers use status 1 and the existing qualified data model. Current tests round-trip B2 and exercise legacy malformed controls. Keep helpers and tests unchanged. They provide compatibility checks only and never define the v3 oracle. Wire blob `35d8c5824590d5cea225a315736519d39b7d5889`. |
| [Owner Program.cs](../../../src/Gagamba.Execution.Linux.M1.Owner/Program.cs) lines 22, 73–75 and [Recovery Program.cs](../../../src/Gagamba.Execution.Linux.M1.Recovery/Program.cs) line 2 | Owner selects B2 lifeline 1/2 and dispatches existing handlers. Recovery enters RecoveryHost. No entry point changes to recognize v3. These hosts directly reference the M1 library. |
| [M1 project](../../../src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj), [M1 tests](../../../tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj), [Directory.Build.props](../../../Directory.Build.props) | SDK compilation in net10.0 includes new C# files under existing globs. Tests reference Debug and currently access internals through [TransactionStateModel.cs](../../../src/Gagamba.Execution.Linux.M1/Execution/TransactionStateModel.cs). Existing owner and recovery friend declarations mean access level alone cannot prove isolation. Release defines GAGAMBA_PRODUCTION. Keep project graph, friend declarations and fixture carrier untouched. |

The independent v3 path can validate decoded names, raw token grammar and
both supplied projections using managed primitives. BB-02 does not block
this package because its nullable legacy model and call chain are excluded
from the new codec. Direct managed tests of existing legacy refusal remain
authorized. Preserve legacy source and behavior. A future proposal to reuse,
expose or repair that path triggers separate review.

## Qualification commands and compiled boundary

Run from canonical Gagamba for the exact implementation revision. Use a
fresh external artifact directory supplied in `$entryArtifacts`. Bind
commands, actual paths, hashes, configuration and results. Build the core
M1 and both host projects in Debug and Release, using the same artifact
directory and explicit configuration so the copied dependencies can be
identified. Representative commands follow.

```powershell
$m1 = 'src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj'
$owner = 'src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj'
$recovery = 'src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj'
$tests = 'tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj'
foreach ($configuration in @('Debug','Release')) {
    foreach ($project in @($m1,$owner,$recovery)) {
        dotnet build $project -c $configuration --artifacts-path $entryArtifacts --nologo
        if ($LASTEXITCODE -ne 0) { throw "Build failed for $project in $configuration" }
    }
}
dotnet test $tests -c Debug --artifacts-path $entryArtifacts --nologo --filter 'FullyQualifiedName~ValidationPreCodec|FullyQualifiedName~InstallationWireTests'
```

Use the [existing verifier](../../../eng/VerifyProductionAssembly/README.md)
against the real, hash-bound Release and Debug M1 outputs, including the
actual Release dependency copies used by the owner and recovery or documented
byte identity to the verified core. Release as production must pass with exit
0. Debug as production must fail with exit 1 and the expected qualification
diagnostic. Exit 2 or missing input is not the required negative control.
The accepted prior command pattern is

```powershell
dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production $releaseM1
dotnet run --project eng/VerifyProductionAssembly -c Release --no-launch-profile -- --production $debugM1
```

The variables must resolve to the exact built M1 files. The verifier's
`--dump` and metadata scans may support the isolation proof. The verifier
does not establish custom codec reachability by itself. Preserve its
production/fixture rules and accepted `eng/test-production-build.ps1`
controls. Use external scratch for additional scanner code and evidence
outputs. At exit verify and record:

1. Source and build graph. Check host/CLI dispatch, registered services,
   factories, public API, native paths, config or feature switches and
   reflection/discovery. Verify the existing production consumers and
   authority sinks remain unchanged and that the test-only calls are
   outside all host/project activation paths.
2. Compiled M1, Owner, Recovery and any further production consumer
   identified by the build/reference graph, in Debug and Release. Inventory
   emitted codec helpers, signatures and internal types. Inspect all
   non-codec production method bodies and metadata for inbound type/member
   references, calls, constructor and delegate/function targets, static
   or module initialization, interfaces, properties, signatures and
   relevant reflection names/configuration strings. Resolve local and
   cross-assembly references. A present internal type with no legitimate
   production use requires evidence of absent inbound reachability, including
   discovery routes. Check dependency identities and a scanner positive
   control with a known matching reference. Record search limits for
   computed reflection and pair the inspection with source/build and
   activation evidence.
3. Confirm the reverse-call and sink boundary for P4. Check F-R1/O-R1's
   S10/S11 parent paths and S28's test-only status. Confirm existing
   qualified-to-legacy refusal and first-effect dominance. O-R3 and the
   observation-to-effect residual remain in scope as retained qualifications.
   Any new production caller or route to an effect is outside this entry.
4. Preserve candidate commit/tree, commands and exit codes, traceable test
   evidence, DLL/evidence SHA-256, positive control results, bounded
   negative diagnostics, unresolved limitations and tracked-state
   preservation. Required qualification is exact-revision evidence.
   A source-only inference, generic verifier pass or skipped native test
   cannot substitute for these checks.

The entry review verified Git object identities with `git rev-parse`,
checked the candidate Git bytes with Python SHA-256 and a raw working-file
comparison, checked local ancestry with `git log`, checked the one worktree,
index, stashes and recorded upstream, and compared the untracked path roster
and every literal file's length and SHA-256 to the frozen inventory. It
verified the production source, tests and tooling delta from E-2 closure
to the entry baseline is empty. It ran no implementation build or tests.

[Entry decision](../rc2b-06-b6-alt-b-slice-e-03-entry-owner-01.md) ·
[Original surfaces](../../reviews/rc2b-06-b6-alt-b-slice-e-03-entry-assessment-01-evidence/dependencies-and-surfaces.md) ·
[P4 coverage acceptance](../rc2b-06-b6-alt-b-p4-coverage-owner-acceptance-01.md).
