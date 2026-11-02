$ErrorActionPreference = 'Stop'
$rev = 'C:\Users\Laszlos\source\repos\Solo\b6-impl-01-s1-q2-review'
$manifestPath = Join-Path $rev 'evidence\MANIFEST-SHA256.txt'
$lines = @()
# All files under the review dir, excluding the manifest itself, bin/obj build
# outputs, .vs, and the materialized source copies' tracked content (source
# identity is already git-bound; their build outputs are listed in custody).
$exclDirs = @('src-original')
Get-ChildItem $rev -Recurse -File | Where-Object {
  $rel = $_.FullName.Substring($rev.Length + 1)
  if ($rel -eq 'evidence\MANIFEST-SHA256.txt') { return $false }
  $top = $rel.Split('\')[0]
  if ($exclDirs -contains $top) { return $false }
  if ($top -eq 'src-remediated') {
    # include only reviewer-generated supplemental evidence; source identity is git-bound
    if ($rel -notmatch '^src-remediated\\work-artifacts\\') { return $false }
  }
  if ($rel -match '(^|\\)(bin|obj)(\\|$)') { return $false }
  if ($rel -match '(^|\\)\.vs(\\|$)') { return $false }
  return $true
} | Sort-Object FullName | ForEach-Object {
  $rel = $_.FullName.Substring($rev.Length + 1)
  $h = (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLower()
  $lines += ("{0} *{1}" -f $h, $rel)
}
$lines | Set-Content $manifestPath
Write-Output ("manifest entries: {0}" -f $lines.Count)
