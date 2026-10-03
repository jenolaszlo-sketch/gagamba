[CmdletBinding()]
param([string]$RepositoryRoot)

$ErrorActionPreference = 'Stop'
# Windows PowerShell -File can evaluate parameter defaults before PSScriptRoot is set.
# Resolve the default in the script body, independently of the caller's directory.
if (-not $PSBoundParameters.ContainsKey('RepositoryRoot')) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    throw 'RepositoryRoot must be a non-empty path.'
}
$prepRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$prepRequired = @(
    'AGENTS.md', 'README.md', 'LICENSE',
    'docs/security-model.md', 'docs/fixture-protocol.md', 'docs/work-queue.md',
    'docs/handoff.md', 'docs/implementation-plan.md', 'docs/testing-and-ci.md',
    'docs/test-environments.md', 'docs/decisions.md',
    'docs/proposals/2026-10-03-original-proposal.md'
)
foreach ($prepRelative in $prepRequired) {
    if (-not (Test-Path -LiteralPath (Join-Path $prepRoot $prepRelative) -PathType Leaf)) {
        throw "Missing preparation artifact: $prepRelative"
    }
}
$prepOriginalHash = '89E830B59C8D8E7529D467D3264A2E620EEE3EF747ADF6DC0496B811BA051160'
$prepArchive = Join-Path $prepRoot 'docs/proposals/2026-10-03-original-proposal.md'
if ((Get-FileHash -LiteralPath $prepArchive -Algorithm SHA256).Hash -ne $prepOriginalHash) {
    throw 'Original proposal no longer matches the supplied bytes.'
}
$prepDocuments = @(Get-Item -LiteralPath (Join-Path $prepRoot 'README.md'), (Join-Path $prepRoot 'AGENTS.md'))
$prepDocuments += @(Get-ChildItem -LiteralPath (Join-Path $prepRoot 'docs') -File -Recurse -Filter '*.md')
$prepLinks = 0
foreach ($prepDocument in $prepDocuments) {
    $prepContent = [IO.File]::ReadAllText($prepDocument.FullName)
    foreach ($prepMatch in [regex]::Matches($prepContent, '\]\(([^)]+)\)')) {
        $prepTarget = $prepMatch.Groups[1].Value.Trim('<', '>')
        if ($prepTarget -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -or $prepTarget.StartsWith('#')) { continue }
        $prepTarget = ($prepTarget -split '#', 2)[0]
        if (-not (Test-Path -LiteralPath (Join-Path $prepDocument.DirectoryName $prepTarget))) {
            throw "Broken local link in $($prepDocument.Name): $prepTarget"
        }
        $prepLinks++
    }
}
$prepProfile = [IO.File]::ReadAllText((Join-Path $prepRoot 'docs/security-model.md'))
$prepQueue = [IO.File]::ReadAllText((Join-Path $prepRoot 'docs/work-queue.md'))
if (-not $prepProfile.Contains('offline-process-v1') -or -not $prepQueue.Contains('GP-1A')) {
    throw 'Preparation profile or next-work identity is missing.'
}
[pscustomobject]@{
    Validation = 'Preparation artifacts only; no backend qualification'
    RequiredFiles = $prepRequired.Count
    MarkdownDocuments = $prepDocuments.Count
    LocalLinks = $prepLinks
    OriginalProposalSha256 = $prepOriginalHash
    Result = 'Passed'
}
