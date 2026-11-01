# VerifyProductionAssembly

Small .NET 10 metadata verifier for the Gagamba M1 assembly. It has no external package references and inspects the PE/CLI metadata without loading or executing the inspected assembly.

## Build and run

```powershell
dotnet build .\VerifyProductionAssembly.csproj -c Release
dotnet .\bin\Release\net10.0\VerifyProductionAssembly.dll --production <path-to-Gagamba.Execution.Linux.M1.dll>
dotnet .\bin\Release\net10.0\VerifyProductionAssembly.dll --instrumented <path-to-Gagamba.Execution.Linux.M1.dll>
```

Exit codes: `0` passes the selected profile; `1` means the binary failed a qualification check; `2` means usage or input failure. Append `--dump` to print the type inventory and decoded IL instruction stream for preservation in a raw review log.

## Checks

Production requires assembly name `Gagamba.Execution.Linux.M1`; instrumented also permits `Gagamba.Execution.Linux.M1.TestFixture`. Both require all 12 accepted production types, valid method bodies, and successful decoding of every method body's IL operand. The scanner resolves type, method, field, member-reference, signature, and user-string tokens, and reports assembly configuration, MVID, and SHA-256.

`--production` requires `AssemblyConfiguration=Release`, excludes the four accepted hook types and three exact hook properties, and rejects hook identifiers in metadata names/references, IL token references, or IL user strings. It also rejects fixture-only type/assembly identities (`TestFixture`, `FakeDomainClock`, `RelativeTestClock`, and `NotAttemptedStopTestFixture`) and the selected canonical INT01 fault case/evidence strings. The production `FixtureConstructorMarker` constant and `gagamba-m1-fixture: ctor-marker` sentinel are intentionally allowed: canonical production code uses that sentinel to detect and refuse unexpected fixture output; its presence is a guard, not an included fixture identity.

`--instrumented` requires `AssemblyConfiguration=Debug` or `Test` and all seven hook surfaces. `TestFixture` identities are allowed in this profile.

## Boundaries

The verifier checks managed assembly metadata and IL. It does not establish native dependency closure, installed-artifact identity, runtime behavior, or cryptographic build provenance. Type-specification and generic-method signature operands are recorded as opaque tokens; referenced type/member names are checked via metadata tables; this is an inspection gate, not a general-purpose IL disassembler.
