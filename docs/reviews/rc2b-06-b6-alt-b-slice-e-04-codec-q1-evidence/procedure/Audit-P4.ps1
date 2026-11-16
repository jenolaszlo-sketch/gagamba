$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../Gagamba')).Path
$log = "$PSScriptRoot/run/logs/p4-source.log"
$base = '8be61181e804b2dfafb0797eb54c92d2f234ff86'
$exact = '3c16b53b288b39de3fb36964eecc8d5cccbc62b6'
$reverse = 'StartOwnerPreparation\(|TrySpawnOwner\(|new PreparationOwnershipCoordinator\(|IntegratedRecoveryHost.Run\(|QualifiedRecoveryConnection.Start\(|RecoveryConnection.Start\(|new HeldReleasePhase\(|new B3PreparationOwner\(|OwnerPreparationTransaction.Run\('
Set-Location -LiteralPath $repo
"git diff --name-status $base $exact -- . ':(exclude)docs/**'" | Set-Content $log
git diff --name-status $base $exact -- . ':(exclude)docs/**' | Add-Content $log
"EXIT $LASTEXITCODE" | Add-Content $log
$inventories = @{}
foreach ($rev in $base, $exact) {
    "git grep -n -E '$reverse' $rev -- src tests" | Add-Content $log
    $out = @(git grep -n -E $reverse $rev -- src tests)
    $code = $LASTEXITCODE
    $out | Add-Content $log
    "EXIT $code" | Add-Content $log
    if ($code) { exit $code }
    $inventories[$rev] = $out -replace "^${rev}:", ''
}
if (Compare-Object $inventories[$base] $inventories[$exact] -CaseSensitive) { throw 'Caller inventory changed' }
'Normalized reverse-call inventory identical' | Add-Content $log
"git grep -n -E Gava1 $exact -- src tests eng" | Add-Content $log
$calls = @(git grep -n -E Gava1 $exact -- src tests eng)
$code = $LASTEXITCODE
$calls | Add-Content $log
"EXIT $code" | Add-Content $log
if ($code -or @($calls | Where-Object { $_ -notmatch ':(src/Gagamba.Execution.Linux.M1/Bootstrap/Gava1Context(Codec|Data)|tests/Gagamba.Execution.Linux.M1.Tests/Bootstrap/Gava1Context(CodecTests|Vectors))\.cs:' }).Count) {
    throw 'Unexpected E-4 source reference'
}

$m1 = 'src/Gagamba.Execution.Linux.M1'
$sites = [ordered]@{
    "$m1/Bootstrap/IntegratedRecoveryHost.cs" = 'RequireIntegrated|VerifyPrivateDirectory|JournalFileStorage|StartOwnerPreparation'
    "$m1/Bootstrap/RecoveryHost.cs" = 'OpenDelegatedParent|QualifiedRecoveryEcho|IntegratedRecoveryHost.Run'
    "$m1/Bootstrap/QualifiedRecoveryEcho.cs" = 'RecoveryInitializationAdmission.Require|return .*launch'
    "$m1/Bootstrap/RecoveryConnection.cs" = 'QualifiedRecoveryConnection.Start'
    "$m1/Bootstrap/IntegratedB3Client.cs" = 'RecoveryConnection.Start'
    "$m1/Lifecycle/M1RecoveryScope.cs" = 'TrySpawnOwner|AllocationIntent|StartOwnerPreparation'
    "$m1/Lifecycle/OwnerPreparationTransaction.cs" = 'RunTransaction|JournalFileStorage|HeldReleasePhase'
    "$m1/Restricted/B3Qualification.cs" = 'B3ProviderAdmission.Require|Directory.CreateDirectory|StartOwnerPreparation'
    'src/Gagamba.Runtime/RestrictedExecutionRuntime.cs' = 'UnavailableRestrictedExecutionProvider'
    "$m1/Lifecycle/PreparationOwnershipCoordinator.cs" = 'PreparationOwnershipCoordinator\('
}
foreach ($path in $sites.Keys) {
    $actual = git rev-parse "${exact}:$path"
    $prior = git rev-parse "${base}:$path"
    if ($LASTEXITCODE -or !$actual -or $actual -ne $prior) { throw "Changed accepted source: $path" }
    "$actual $path ($base and $exact)" | Add-Content $log
    Select-String -LiteralPath $path -Pattern $sites[$path] | ForEach-Object { "$($_.LineNumber) $($_.Line.Trim())" } | Add-Content $log
}
'Existing audited files have identical blobs. EXIT 0' | Add-Content $log
