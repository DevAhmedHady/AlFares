<#
.SYNOPSIS
  Create one GitHub issue per backlog task from scripts/issues.json.
.DESCRIPTION
  Run AFTER `gh-setup.ps1` (labels + milestones) and AFTER adding the remote.
  Idempotent: skips a task if an open/closed issue with the same title already exists.
  Writes scripts/issue-map.json (taskId -> issue number) for Codex to update statuses.
.EXAMPLE
  pwsh ./scripts/gh-create-issues.ps1
#>
[CmdletBinding()]
param(
    [string]$IssuesFile = "$PSScriptRoot/issues.json",
    [string]$MapFile    = "$PSScriptRoot/issue-map.json"
)

$ErrorActionPreference = 'Stop'

$tasks = Get-Content $IssuesFile -Raw | ConvertFrom-Json

# Existing titles (avoid duplicates on re-run)
$existing = @{}
gh issue list --state all --limit 500 --json number,title |
    ConvertFrom-Json |
    ForEach-Object { $existing[$_.title] = $_.number }

$map = @{}

foreach ($t in $tasks) {
    if ($existing.ContainsKey($t.title)) {
        Write-Host "skip (exists #$($existing[$t.title])): $($t.title)" -ForegroundColor DarkYellow
        $map[$t.id] = $existing[$t.title]
        continue
    }

    $labelArgs = @()
    foreach ($l in $t.labels) { $labelArgs += @('--label', $l) }

    $url = gh issue create `
        --title  $t.title `
        --body   $t.body `
        --milestone $t.milestone `
        @labelArgs

    # gh prints the issue URL; extract trailing number
    $num = ($url -split '/')[-1]
    $map[$t.id] = [int]$num
    Write-Host "created #$num  $($t.id)" -ForegroundColor Green
}

$map | ConvertTo-Json | Set-Content -Path $MapFile -Encoding utf8
Write-Host "Wrote $MapFile ($($map.Count) tasks). Codex uses this to close issues by task id." -ForegroundColor Cyan
