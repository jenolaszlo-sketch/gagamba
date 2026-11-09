# E-2-P1 positive fixture generator (test-only, non-authoritative).
# Builds hand-written canonical CJSON-1 fixture bytes for the accepted B2 and B3
# chains, computes SHA-256 digests with the platform primitive, writes the exact
# bytes (UTF-8, no BOM, no trailing newline), and prints a C# snippet for tests.
# These fixtures are test data only; they are not approved catalogs or instances.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

function Sha256Hex([byte[]]$bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    return ([BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant()
}

function Sha256HexText([string]$text) {
    return Sha256Hex ([System.Text.Encoding]::UTF8.GetBytes($text))
}

function ActionHash([string]$actionJson) {
    $domain = [System.Text.Encoding]::ASCII.GetBytes('gagamba-validation-action-v1')
    $body = [System.Text.Encoding]::UTF8.GetBytes($actionJson)
    $input = [byte[]]::new($domain.Length + 1 + $body.Length)
    [Array]::Copy($domain, 0, $input, 0, $domain.Length)
    $input[$domain.Length] = 0
    [Array]::Copy($body, 0, $input, $domain.Length + 1, $body.Length)
    return Sha256Hex $input
}

function Write-Utf8Exact([string]$path, [string]$text) {
    [System.IO.File]::WriteAllText($path, $text, [System.Text.UTF8Encoding]::new($false))
}

$a64 = 'a' * 64
$b64 = 'b' * 64
$c64 = 'c' * 64
$d64 = 'd' * 64
$e64 = 'e' * 64
$f64 = 'f' * 64
$s64_1 = '1' * 64
$s64_2 = '2' * 64
$s64_3 = '3' * 64
$s64_4 = '4' * 64
$commit40 = '0' * 40
$tree40 = '1' * 40

# --- B2 / pre / no-release chain ---
$m2 = '{"caseId":"case-2","objects":[],"schemaVersion":1,"topology":"b2-lifeline"}'
$hm2 = Sha256HexText $m2
$f2 = '{"allowRelease":false,"bootId":"00000000-0000-4000-8000-000000000000","caseId":"case-2","copy":null,"fixtureManifestSha256":"' + $hm2 + '","gid":1000,"hostId":"' + $a64 + '","hostPaths":null,"inputEnrollmentId":null,"invocation":null,"maximumSessionNs":600000000000,"phase":"pre","policyKind":"b2-lifeline-v1","resources":null,"sandbox":null,"schemaVersion":1,"toolEnrollmentId":null,"topology":"b2-lifeline","uid":1000}'
$hf2 = Sha256HexText $f2
$a2_0 = '{"index":0,"op":"connect-b2","timeoutNs":1000000000}'
$a2_1 = '{"index":1,"op":"inspect","timeoutNs":1000000000}'
$a2_2 = '{"index":2,"op":"stop","timeoutNs":1000000000}'
$a2_3 = '{"index":3,"op":"cleanup","timeoutNs":1000000000}'
$s2 = '{"actions":[' + $a2_0 + ',' + $a2_1 + ',' + $a2_2 + ',' + $a2_3 + '],"caseId":"case-2","fixtureManifestSha256":"' + $hm2 + '","fixturePolicySha256":"' + $hf2 + '","phase":"pre","schemaVersion":1,"topology":"b2-lifeline"}'
$hs2 = Sha256HexText $s2
$k2 = '{"actionScriptSha256":"' + $hs2 + '","allowRelease":false,"artifactSetSha256":"' + $b64 + '","caseId":"case-2","controllerClosureSha256":"' + $d64 + '","driverClosureSha256":"' + $e64 + '","fixtureManifestSha256":"' + $hm2 + '","fixturePolicySha256":"' + $hf2 + '","hostId":"' + $a64 + '","maximumSessionNs":600000000000,"phase":"pre","platformInventorySha256":"' + $c64 + '","profileId":"profile","profileVersion":"1.0","qualificationId":null,"qualificationRecordSha256":null,"rid":"linux-x64","schemaVersion":1,"scope":"test","sourceCommit":"' + $commit40 + '","sourceTree":"' + $tree40 + '","topology":"b2-lifeline"}'
$hk2 = Sha256HexText $k2

# --- B3 / post / release chain ---
$m3 = '{"caseId":"case-3","objects":[{"destination":"/coordinator","length":1000,"mode":"0555","path":"coordinator","role":"coordinator","sha256":"' + $s64_1 + '"},{"destination":"fd:6","length":2000,"mode":"0444","path":"filter","role":"filter","sha256":"' + $s64_2 + '"},{"destination":"/input","length":3000,"mode":"0444","path":"input","role":"input","sha256":"' + $s64_3 + '"},{"destination":"/tool","length":4000,"mode":"0555","path":"tool","role":"tool","sha256":"' + $s64_4 + '"}],"schemaVersion":1,"topology":"b3-integrated"}'
$hm3 = Sha256HexText $m3
$resources3 = '{"captureAggregateBytes":131072,"configBytes":16384,"coordinatorStatusBytes":16384,"coreDisabled":true,"corePatternObserved":null,"expiresLeadNs":1000000000,"fileDescriptors":64,"finalDrainNs":1000000000,"helperStatusBytes":16384,"inodeQualification":"PendingQualification","journalBytes":65536,"memoryBytes":134217728,"overallTimeoutNs":30000000000,"receiptBytes":32768,"runtimeDumpsDisabled":false,"setupTimeoutNs":10000000000,"shmBytes":1048576,"stagingBytes":16777216,"stagingObjects":128,"stderrBytes":65536,"stdoutBytes":65536,"stopAllowanceNs":5000000000,"swapBytes":0,"tasks":48,"tmpBytes":7340032}'
$sandbox3 = '{"capabilities":[],"namespaces":["ipc","mount","net","pid","user","uts"],"networkQualification":"PendingQualification","noAmbientEnvironment":true,"noExtraInheritedFds":true,"noIpcEndpoints":true,"noNewPrivs":true,"readOnlyDestinations":["/coordinator","/input","/tool"],"scratchNoexec":false,"toolExecutable":true,"writableStores":[{"bytes":1048576,"hasInodeBudget":false,"inodeBudget":0,"path":"/dev/shm"},{"bytes":7340032,"hasInodeBudget":false,"inodeBudget":0,"path":"/tmp"}]}'
$invocation3 = '{"argv":["--in","/input"],"environment":[{"name":"HOME","value":"/tmp"},{"name":"LANG","value":"C"}],"inputPath":"/input","mode":"work","stdinEof":true,"toolPath":"/tool","workDirectory":"/tmp"}'
$copy3 = '{"aggregateBytes":16777216,"hooks":false,"maximumObjectBytes":16777216,"maximumObjects":4}'
$hostpaths3 = '{"delegatedParent":{"device":8,"gid":1000,"inode":100001,"mode":"0755","path":"/sys/fs/cgroup","uid":1000},"journalParent":{"device":8,"gid":1000,"inode":100002,"mode":"0700","path":"/var/lib/gagamba/journal","uid":1000},"stagingParent":{"device":8,"gid":1000,"inode":100003,"mode":"0700","path":"/var/lib/gagamba/staging","uid":1000}}'
$f3 = '{"allowRelease":true,"bootId":"00000000-0000-4000-8000-000000000000","caseId":"case-3","copy":' + $copy3 + ',"fixtureManifestSha256":"' + $hm3 + '","gid":1000,"hostId":"' + $a64 + '","hostPaths":' + $hostpaths3 + ',"inputEnrollmentId":"input-1","invocation":' + $invocation3 + ',"maximumSessionNs":600000000000,"phase":"post","policyKind":"b3-static-fixture-v1","resources":' + $resources3 + ',"sandbox":' + $sandbox3 + ',"schemaVersion":1,"toolEnrollmentId":"tool-1","topology":"b3-integrated","uid":1000}'
$hf3 = Sha256HexText $f3
$a3 = @(
    '{"index":0,"op":"prepare-b3","timeoutNs":1000000000}',
    '{"index":1,"op":"inspect","timeoutNs":1000000000}',
    '{"index":2,"op":"release","timeoutNs":1000000000}',
    '{"index":3,"op":"inspect","timeoutNs":1000000000}',
    '{"index":4,"op":"stop","timeoutNs":1000000000}',
    '{"index":5,"op":"cleanup","timeoutNs":1000000000}'
)
$s3 = '{"actions":[' + ($a3 -join ',') + '],"caseId":"case-3","fixtureManifestSha256":"' + $hm3 + '","fixturePolicySha256":"' + $hf3 + '","phase":"post","schemaVersion":1,"topology":"b3-integrated"}'
$hs3 = Sha256HexText $s3
$k3 = '{"actionScriptSha256":"' + $hs3 + '","allowRelease":true,"artifactSetSha256":"' + $b64 + '","caseId":"case-3","controllerClosureSha256":"' + $d64 + '","driverClosureSha256":"' + $e64 + '","fixtureManifestSha256":"' + $hm3 + '","fixturePolicySha256":"' + $hf3 + '","hostId":"' + $a64 + '","maximumSessionNs":600000000000,"phase":"post","platformInventorySha256":"' + $c64 + '","profileId":"profile","profileVersion":"1.0","qualificationId":"Q1","qualificationRecordSha256":"' + $f64 + '","rid":"linux-x64","schemaVersion":1,"scope":"test","sourceCommit":"' + $commit40 + '","sourceTree":"' + $tree40 + '","topology":"b3-integrated"}'
$hk3 = Sha256HexText $k3

# --- write exact bytes ---
$b2 = Join-Path $root 'b2-chain'
$b3 = Join-Path $root 'b3-chain'
[System.IO.Directory]::CreateDirectory($b2) | Out-Null
[System.IO.Directory]::CreateDirectory($b3) | Out-Null
Write-Utf8Exact (Join-Path $b2 'm.json') $m2
Write-Utf8Exact (Join-Path $b2 'f.json') $f2
Write-Utf8Exact (Join-Path $b2 's.json') $s2
Write-Utf8Exact (Join-Path $b2 'k.json') $k2
Write-Utf8Exact (Join-Path $b3 'm.json') $m3
Write-Utf8Exact (Join-Path $b3 'f.json') $f3
Write-Utf8Exact (Join-Path $b3 's.json') $s3
Write-Utf8Exact (Join-Path $b3 'k.json') $k3

$digestLines = @(
    '# E-2-P1 test fixtures - independently computed digests',
    '# Method: SHA-256 over the exact fixture bytes (UTF-8, no BOM, no trailing newline), lower-case hex.',
    '# Fixtures are generated by generate-fixtures.ps1 (hand-written canonical JSON templates).',
    '# These are test-only non-authoritative fixtures, not approved catalog instances.',
    'b2-chain/m.json' + "`t" + (Sha256HexText $m2) + "`t" + $m2.Length,
    'b2-chain/f.json' + "`t" + (Sha256HexText $f2) + "`t" + $f2.Length,
    'b2-chain/s.json' + "`t" + (Sha256HexText $s2) + "`t" + $s2.Length,
    'b2-chain/k.json' + "`t" + (Sha256HexText $k2) + "`t" + $k2.Length,
    'b3-chain/m.json' + "`t" + (Sha256HexText $m3) + "`t" + $m3.Length,
    'b3-chain/f.json' + "`t" + (Sha256HexText $f3) + "`t" + $f3.Length,
    'b3-chain/s.json' + "`t" + (Sha256HexText $s3) + "`t" + $s3.Length,
    'b3-chain/k.json' + "`t" + (Sha256HexText $k3) + "`t" + $k3.Length,
    'action b2/0' + "`t" + (ActionHash $a2_0),
    'action b2/1' + "`t" + (ActionHash $a2_1),
    'action b2/2' + "`t" + (ActionHash $a2_2),
    'action b2/3' + "`t" + (ActionHash $a2_3),
    'action b3/0' + "`t" + (ActionHash $a3[0]),
    'action b3/1' + "`t" + (ActionHash $a3[1]),
    'action b3/2' + "`t" + (ActionHash $a3[2]),
    'action b3/3' + "`t" + (ActionHash $a3[3]),
    'action b3/4' + "`t" + (ActionHash $a3[4]),
    'action b3/5' + "`t" + (ActionHash $a3[5])
)
[System.IO.File]::WriteAllLines((Join-Path $root 'digests.txt'), $digestLines, [System.Text.UTF8Encoding]::new($false))

Write-Output '----- C# SNIPPET BEGIN -----'
Write-Output ('internal const string M2 = "' + $m2 + '";')
Write-Output ('internal const string F2 = "' + $f2 + '";')
Write-Output ('internal const string S2 = "' + $s2 + '";')
Write-Output ('internal const string K2 = "' + $k2 + '";')
Write-Output ('internal const string Hm2 = "' + $hm2 + '";')
Write-Output ('internal const string Hf2 = "' + $hf2 + '";')
Write-Output ('internal const string Hs2 = "' + $hs2 + '";')
Write-Output ('internal const string Hk2 = "' + $hk2 + '";')
Write-Output ('internal const string M3 = "' + $m3 + '";')
Write-Output ('internal const string F3 = "' + $f3 + '";')
Write-Output ('internal const string S3 = "' + $s3 + '";')
Write-Output ('internal const string K3 = "' + $k3 + '";')
Write-Output ('internal const string Hm3 = "' + $hm3 + '";')
Write-Output ('internal const string Hf3 = "' + $hf3 + '";')
Write-Output ('internal const string Hs3 = "' + $hs3 + '";')
Write-Output ('internal const string Hk3 = "' + $hk3 + '";')
Write-Output ('internal const string A2_0 = "' + (ActionHash $a2_0) + '";')
Write-Output ('internal const string A2_1 = "' + (ActionHash $a2_1) + '";')
Write-Output ('internal const string A2_2 = "' + (ActionHash $a2_2) + '";')
Write-Output ('internal const string A2_3 = "' + (ActionHash $a2_3) + '";')
Write-Output ('internal const string A3_0 = "' + (ActionHash $a3[0]) + '";')
Write-Output ('internal const string A3_1 = "' + (ActionHash $a3[1]) + '";')
Write-Output ('internal const string A3_2 = "' + (ActionHash $a3[2]) + '";')
Write-Output ('internal const string A3_3 = "' + (ActionHash $a3[3]) + '";')
Write-Output ('internal const string A3_4 = "' + (ActionHash $a3[4]) + '";')
Write-Output ('internal const string A3_5 = "' + (ActionHash $a3[5]) + '";')
Write-Output '----- C# SNIPPET END -----'
