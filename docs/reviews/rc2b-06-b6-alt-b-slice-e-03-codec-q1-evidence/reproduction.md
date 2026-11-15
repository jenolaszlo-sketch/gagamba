# Reproduction and evidence index

The checkout was created using a transport clone of the canonical local source, with no shared Git objects and no linked worktree. All commands after the clone ran against the detached external checkout. The review bundle and build artifacts are siblings of that checkout. Use fresh writable external locations when reproducing.

```powershell
git clone --no-local --no-checkout -- $canonical $source
git -C $source switch --detach 34eef4c8e7d814e15cb1bfce27df032e75766f19
git -C $source status --short --branch
git -C $source worktree list --porcelain
python "$bundle/verify_identity.py" $source "$out/git-identities.json"
```

Use the bundle directory for $bundle and an empty external results directory for $out. Identity verification exits 0 for the pinned checkout. The review ledger separately binds the commissioned later main snapshot and both inherited preparation/consolidation commits. [Identity ledger](git-identities.json) · [Identity log](git-identities.log) · [Environment](environment.txt)

## Builds and managed outcomes

```powershell
Set-Location -LiteralPath $source
$env:NuGetAudit = 'false'
$env:UseArtifactsOutput = 'true'
$env:ArtifactsPath = $qualified
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
$projects = @(
  'src/Gagamba.Execution.Linux.M1/Gagamba.Execution.Linux.M1.csproj',
  'src/Gagamba.Execution.Linux.M1.Owner/Gagamba.Execution.Linux.M1.Owner.csproj',
  'src/Gagamba.Execution.Linux.M1.Recovery/Gagamba.Execution.Linux.M1.Recovery.csproj',
  'eng/InstallTool/InstallTool.csproj',
  'tests/Gagamba.Execution.Linux.M1.Tests/Gagamba.Execution.Linux.M1.Tests.csproj'
)
foreach ($cfg in @('Debug','Release')) {
  foreach ($project in $projects) {
    dotnet build $project -c $cfg --artifacts-path $qualified --nologo -m:1
    if ($LASTEXITCODE -ne 0) { throw "$cfg build failed for $project" }
  }
}
$b = Join-Path $qualified 'bin'
foreach ($cfg in @('debug','release')) {
  $test = "$b/Gagamba.Execution.Linux.M1.Tests/$cfg/Gagamba.Execution.Linux.M1.Tests.dll"
  dotnet $test -noColor -class '*ValidationPreCodecBootstrapTests' -class '*ValidationPreCodecBinaryTests' -class '*InstallationWireTests' -xml "$out/focused-direct-$cfg.xml"
  "focused $cfg exit $LASTEXITCODE"
}

$releaseDir = Join-Path $mirror 'src/Gagamba.Execution.Linux.M1/bin/Release/net10.0'
New-Item -Force -ItemType Directory $releaseDir | Out-Null
Copy-Item -LiteralPath 'Gagamba.sln' -Destination $mirror
Copy-Item -Path "$b/Gagamba.Execution.Linux.M1/release/*.dll" -Destination $releaseDir
foreach ($cfg in @('debug','release')) {
  $testDir = Join-Path $mirror "test/$cfg"
  New-Item -Force -ItemType Directory $testDir | Out-Null
  Copy-Item -Path "$b/Gagamba.Execution.Linux.M1.Tests/$cfg/*" -Destination $testDir -Recurse
  dotnet "$testDir/Gagamba.Execution.Linux.M1.Tests.dll" -noColor -xml "$out/managed-M1-layout-$cfg.xml"
  "broad $cfg exit $LASTEXITCODE"
}
```

The nested unchanged verifier build inherits the artifact environment variables, so its own bin and obj remain external. The focused and broad direct runs exit 0. The preceding VSTest attempts exited 1 with a Windows testhost connection timeout and are preserved as non-results. The initial global package advisory lookup also failed, and the passing builds used locally cached packages with the advisory lookup disabled. [Managed accounting](managed-accounting.md) · [Build and initial test exits](build-and-test-exit.txt) · [Final direct-run exits](direct-exit.txt) · [Initial NuGet advisory error](initial-advisory-build.log)

## Independent assertions against the actual M1 DLLs

```powershell
$schema = (Get-Item docs/decisions/*-e-03-qe1-bootstrap-base-owner-01-evidence/schema.md).FullName
foreach ($cfg in @('Debug','Release')) {
  $lower = $cfg.ToLowerInvariant()
  $candidate = "$b/Gagamba.Execution.Linux.M1/$lower"
  dotnet run --project "$bundle/probe/Probe.csproj" -c $cfg --artifacts-path $probeArtifacts -p:UseSharedCompilation=false -p:Candidate="$candidate" -- "$out/vectors-$lower" $schema
  "probe $cfg exit $LASTEXITCODE"
}
```

Both exit 1 because 17 assertions disagree with the adopted expectation. The probe DLL uses the existing managed test friend name and only binds to external built DLLs. It installs nothing and invokes no actor, native entry or protected path. Bootstrap ledgers identify every input by SHA-256 and reconstruct mutations from the probe source. Raw full witnesses, binary vectors, the exact failing inputs and expected projections are included. Binary results carry complete hex. [Probe project](probe/Probe.csproj) · [Source](probe/Program.cs) · [Debug log](probe-debug.log) · [Release log](probe-release.log) · [Traceability](traceability.md) · [Findings](findings.md)

## Compiled scan and verifier

```powershell
$files = foreach ($name in @('Gagamba.Execution.Linux.M1','Gagamba.Execution.Linux.M1.Owner','Gagamba.Execution.Linux.M1.Recovery','InstallTool','Gagamba.Execution','Gagamba.Execution.Linux.M1.Tests')) {
  foreach ($cfg in @('debug','release')) {
    if ($name.EndsWith('.Tests')) {
      Get-Item "$b/$name/$cfg/$name.dll","$b/$name/$cfg/Gagamba.Execution.Linux.M1.dll","$b/$name/$cfg/Gagamba.Execution.dll"
    } else { Get-ChildItem "$b/$name/$cfg/*.dll" -File }
  }
}
dotnet build "$bundle/scan/Scan.csproj" -c Release --artifacts-path $scannerArtifacts -p:UseSharedCompilation=false
$inputs = @($files.FullName)
dotnet "$scannerArtifacts/bin/Scan/release/Scan.dll" @inputs
"scanner exit $LASTEXITCODE"
$verifier = "$b/VerifyProductionAssembly/release/VerifyProductionAssembly.dll"
foreach ($cfg in @('release','debug')) {
  dotnet $verifier --production "$b/Gagamba.Execution.Linux.M1/$cfg/Gagamba.Execution.Linux.M1.dll"
  "verifier $cfg exit $LASTEXITCODE"
}
```

Scanner build and scan exit 0, including positive callers under both test configurations. The unchanged verifier returns 0 for Release and 1 for Debug in production mode with the expected qualification diagnostics. [Raw scan](compiled-scan.jsonl) · [Compiled isolation and P4](reachability-and-p4.md) · [RELEASE](verifier-release.log) · [Debug negative control](verifier-debug.log)

The checksum manifest uses raw bytes and relative filenames from the evidence directory. It covers the sibling final report and every file in this evidence package, excluding the manifest itself. From the evidence directory, verify with Python 3.

```python
from hashlib import sha256
from pathlib import Path
for row in Path("SHA256SUMS.txt").read_text(encoding="utf-8").splitlines():
    expected, path = row.split("  ", 1)
    assert sha256(Path(path).read_bytes()).hexdigest() == expected, path
```
