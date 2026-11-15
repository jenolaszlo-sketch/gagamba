[CmdletBinding()]
param([string]$RepositoryRoot)

$ErrorActionPreference = 'Stop'
# Windows PowerShell -File can evaluate parameter defaults before PSScriptRoot is set.
if (-not $PSBoundParameters.ContainsKey('RepositoryRoot')) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    throw 'RepositoryRoot must be a non-empty path.'
}
$prepRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path.TrimEnd([char[]]'\/')

function Resolve-PreparationLocalPath([string]$BaseDirectory, [string]$Target) {
    if ([string]::IsNullOrWhiteSpace($Target) -or [IO.Path]::IsPathRooted($Target) -or $Target -match '^[A-Za-z]:') {
        throw "Unrecognized absolute or empty local reference: $Target"
    }
    $prepResolved = [IO.Path]::GetFullPath([IO.Path]::Combine($BaseDirectory, $Target))
    $prepPrefix = $prepRoot + [IO.Path]::DirectorySeparatorChar
    if (-not $prepResolved.StartsWith($prepPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        -not $prepResolved.Equals($prepRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Local reference escapes repository: $Target"
    }
    return $prepResolved
}

function Get-PreparationMarkdownSections([string]$Content) {
    # Fence markers can be indented up to three spaces and may sit in block quotes.
    # A closer uses the same character, at least the opening length, and no info string.
    $prepFence = [regex]'^(?:(?<quote> {0,3}>[ \t]?))* {0,3}(?<marker>\x60{3,}|~{3,})(?<tail>.*)$'
    $prepQuotePrefix = [regex]'^(?:(?<quote> {0,3}>[ \t]?))*'
    $prepOpening = $null
    $prepProseStart = 0

    foreach ($prepLineMatch in [regex]::Matches($Content, '[^\r\n]*(?:\r\n|\n|\r|\z)')) {
        if ($prepLineMatch.Length -eq 0) { continue }
        $prepText = $prepLineMatch.Value.TrimEnd([char[]]@(13, 10))
        $prepFenceMatch = $prepFence.Match($prepText)
        if ($null -ne $prepOpening) {
            $prepQuoteDepth = $prepQuotePrefix.Match($prepText).Groups['quote'].Captures.Count
            if ($prepQuoteDepth -lt $prepOpening.Groups['quote'].Captures.Count) {
                # Markdown ends an unclosed quoted fence at the end of the quote.
                $prepOpening = $null
                $prepProseStart = $prepLineMatch.Index
            } else {
                if ($prepFenceMatch.Success -and
                    $prepFenceMatch.Groups['quote'].Captures.Count -eq $prepOpening.Groups['quote'].Captures.Count -and
                    $prepFenceMatch.Groups['marker'].Value[0] -eq $prepOpening.Groups['marker'].Value[0] -and
                    $prepFenceMatch.Groups['marker'].Length -ge $prepOpening.Groups['marker'].Length -and
                    $prepFenceMatch.Groups['tail'].Value -match '^[ \t]*$') {
                    $prepOpening = $null
                    $prepProseStart = $prepLineMatch.Index + $prepLineMatch.Length
                }
                continue
            }
        }

        if ($prepFenceMatch.Success -and
            ($prepFenceMatch.Groups['marker'].Value[0] -eq '~' -or
                $prepFenceMatch.Groups['tail'].Value -notmatch '\x60')) {
            if ($prepLineMatch.Index -gt $prepProseStart) {
                $Content.Substring($prepProseStart, $prepLineMatch.Index - $prepProseStart)
            }
            $prepOpening = $prepFenceMatch
        }
    }

    if ($null -eq $prepOpening -and $prepProseStart -lt $Content.Length) {
        $Content.Substring($prepProseStart)
    }
}

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

# Exceptions apply only to one exact source identity and one exact original reference.
# The inventory records every occurrence and the target identity checked at introduction.
$prepMapPath = Join-Path $prepRoot 'eng/preparation-legacy-links.json'
$prepMap = [IO.File]::ReadAllText($prepMapPath) | ConvertFrom-Json
if ($prepMap.schemaVersion -ne 1 -or @($prepMap.documents).Count -eq 0) {
    throw 'Unsupported or empty historical-reference compatibility map.'
}
$prepMappings = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::Ordinal)
$prepSources = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
foreach ($prepLegacyDocument in $prepMap.documents) {
    $prepSourceName = [string]$prepLegacyDocument.source
    if (-not $prepSources.Add($prepSourceName)) { throw "Duplicate historical source: $prepSourceName" }
    $prepSourcePath = Resolve-PreparationLocalPath $prepRoot $prepSourceName
    $prepBytes = [IO.File]::ReadAllBytes($prepSourcePath)
    $prepSha256 = [Security.Cryptography.SHA256]::Create()
    $prepSha1 = [Security.Cryptography.SHA1]::Create()
    try {
        $prepContentIdentity = [BitConverter]::ToString($prepSha256.ComputeHash($prepBytes)).Replace('-', '').ToLowerInvariant()
        $prepHeader = [Text.Encoding]::UTF8.GetBytes("blob $($prepBytes.Length)" + [char]0)
        $prepBlobBytes = [byte[]]($prepHeader + $prepBytes)
        $prepBlobIdentity = [BitConverter]::ToString($prepSha1.ComputeHash($prepBlobBytes)).Replace('-', '').ToLowerInvariant()
    } finally {
        $prepSha256.Dispose()
        $prepSha1.Dispose()
    }
    if ($prepContentIdentity -cne $prepLegacyDocument.expectedSha256 -or
        $prepBlobIdentity -cne $prepLegacyDocument.expectedGitBlob) {
        throw "Historical source identity mismatch: $prepSourceName"
    }
    foreach ($prepLegacyLink in $prepLegacyDocument.references) {
        if ([string]::IsNullOrWhiteSpace($prepLegacyLink.reference) -or $prepLegacyLink.expectedOccurrences -lt 1) {
            throw "Invalid historical reference mapping: $prepSourceName"
        }
        $prepMappedTarget = Resolve-PreparationLocalPath $prepRoot ([string]$prepLegacyLink.target)
        if (-not (Test-Path -LiteralPath $prepMappedTarget -PathType Leaf)) {
            throw "Historical compatibility target missing: $($prepLegacyLink.target)"
        }
        $prepKey = $prepSourceName + "`n" + [string]$prepLegacyLink.reference
        if ($prepMappings.ContainsKey($prepKey)) { throw "Duplicate historical reference: $prepSourceName" }
        $prepMappings.Add($prepKey, [pscustomobject]@{
            Target = $prepMappedTarget
            Expected = [int]$prepLegacyLink.expectedOccurrences
            Seen = 0
        })
    }
}

$prepDocuments = @(Get-Item -LiteralPath (Join-Path $prepRoot 'README.md'), (Join-Path $prepRoot 'AGENTS.md'))
$prepDocuments += @(Get-ChildItem -LiteralPath (Join-Path $prepRoot 'docs') -File -Recurse -Filter '*.md')
$prepLinks = 0
$prepCompatibleLinks = 0
$prepLineReferences = 0
foreach ($prepDocument in $prepDocuments) {
    $prepContent = [IO.File]::ReadAllText($prepDocument.FullName)
    $prepSourceName = $prepDocument.FullName.Substring($prepRoot.Length).TrimStart([char[]]'\/').Replace('\', '/')
    $prepMatches = foreach ($prepSection in Get-PreparationMarkdownSections $prepContent) {
        [regex]::Matches($prepSection, '\]\(([^)]+)\)')
    }
    foreach ($prepMatch in $prepMatches) {
        $prepTarget = $prepMatch.Groups[1].Value.Trim('<', '>')
        $prepKey = $prepSourceName + "`n" + $prepTarget
        if ($prepMappings.ContainsKey($prepKey)) {
            $prepMappings[$prepKey].Seen++
            $prepCompatibleLinks++
            $prepLinks++
            continue
        }
        if ($prepTarget -match '^/[A-Za-z]:[\\/]') {
            throw "Unrecognized machine-local reference in $prepSourceName"
        }
        if ($prepTarget.StartsWith('#')) { continue }
        $prepTarget = ($prepTarget -split '#', 2)[0]
        # Recognize only positive line citations on known text-file extensions.
        $prepLine = [regex]::Match($prepTarget, '^(?<path>.+\.(?:md|txt|json|ps1|py|c|h|cpp|cs|sh|yml|yaml)):(?<first>[1-9][0-9]*)(?:[-\u2013](?<last>[1-9][0-9]*))?$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $prepCitation = [regex]::Match($prepTarget, '^(?<path>.+\.(?:md|txt|json|ps1|py|c|h|cpp|cs|sh|yml|yaml)):(?<citation>.*)$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $prepLocalCitation = $prepCitation.Success -and $prepCitation.Groups['path'].Value -notmatch '^[a-zA-Z][a-zA-Z0-9+.-]*:'
        if ($prepLocalCitation -and -not $prepLine.Success) { throw "Invalid line reference in $prepSourceName" }
        $prepLocalLineReference = $prepLine.Success -and $prepLocalCitation
        if ($prepTarget -match '^[a-zA-Z][a-zA-Z0-9+.-]*:' -and -not $prepLocalLineReference) { continue }
        if ($prepLocalLineReference) {
            $prepTarget = $prepLine.Groups['path'].Value
            $prepFirst = [long]$prepLine.Groups['first'].Value
            $prepLast = $prepFirst
            if ($prepLine.Groups['last'].Success) { $prepLast = [long]$prepLine.Groups['last'].Value }
            if ($prepLast -lt $prepFirst) { throw "Invalid line range in $prepSourceName" }
        }
        $prepResolvedTarget = Resolve-PreparationLocalPath $prepDocument.DirectoryName $prepTarget
        if (-not (Test-Path -LiteralPath $prepResolvedTarget)) {
            throw "Broken local link in $($prepDocument.Name): $prepTarget"
        }
        if ($prepLocalLineReference) {
            if (-not (Test-Path -LiteralPath $prepResolvedTarget -PathType Leaf) -or
                $prepLast -gt [IO.File]::ReadAllLines($prepResolvedTarget).Length) {
                throw "Unresolved line reference in $prepSourceName"
            }
            $prepLineReferences++
        }
        $prepLinks++
    }
}
foreach ($prepEntry in $prepMappings.GetEnumerator()) {
    if ($prepEntry.Value.Seen -ne $prepEntry.Value.Expected) {
        throw "Historical reference occurrence mismatch: $($prepEntry.Key.Split("`n")[0])"
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
    HistoricalCompatibilityDocuments = $prepSources.Count
    HistoricalCompatibilityReferences = $prepCompatibleLinks
    NormalizedLineReferences = $prepLineReferences
    OriginalProposalSha256 = $prepOriginalHash
    Result = 'Passed'
}
