<#
.SYNOPSIS
  Bootstrap GitHub labels + milestones for the Factory الفارس backlog.
.DESCRIPTION
  Run ONCE after you have created the GitHub repo and added it as 'origin'.
  Requires: gh CLI authenticated (`gh auth login`) and run from inside the repo.
.EXAMPLE
  pwsh ./scripts/gh-setup.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

function Ensure-Label($name, $color, $desc) {
    gh label create $name --color $color --description $desc --force | Out-Null
    Write-Host "label: $name" -ForegroundColor DarkGray
}

function Ensure-Milestone($title, $desc) {
    # gh has no native milestone create; use the REST API. Ignore 'already_exists'.
    try {
        gh api "repos/{owner}/{repo}/milestones" -f title="$title" -f state="open" -f description="$desc" | Out-Null
        Write-Host "milestone: $title" -ForegroundColor DarkGray
    } catch {
        Write-Host "milestone exists: $title" -ForegroundColor DarkYellow
    }
}

Write-Host "== Creating labels ==" -ForegroundColor Cyan
# Type
Ensure-Label 'epic'        '6F42C1' 'Large business capability'
Ensure-Label 'feature'     '0E8A16' 'Deliverable user-facing functionality'
Ensure-Label 'user-story'  '1D76DB' 'User-focused requirement'
Ensure-Label 'enabler'     '5319E7' 'Technical/architectural work'
Ensure-Label 'test'        'FBCA04' 'Quality assurance work'
Ensure-Label 'task'        'C5DEF5' 'Implementation task'
Ensure-Label 'in-progress' '1D76DB' 'Work currently in progress'
# Component
Ensure-Label 'backend'        '0052CC' 'ASP.NET Core / EF Core'
Ensure-Label 'frontend'       'D93F0B' 'Angular RTL SPA'
Ensure-Label 'infrastructure' '795548' 'Scaffold / build / migrations'
Ensure-Label 'documentation'  'BFDADC' 'Docs & agent files'
# Priority
Ensure-Label 'priority-critical' 'B60205' 'P0 - critical path'
Ensure-Label 'priority-high'     'D93F0B' 'P1 - core'
Ensure-Label 'priority-medium'   'FBCA04' 'P2 - important, not blocking'
# Value
Ensure-Label 'value-high'   '0E8A16' 'High business value'
Ensure-Label 'value-medium' 'C2E0C6' 'Medium business value'

Write-Host "== Creating milestones ==" -ForegroundColor Cyan
Ensure-Milestone 'M0 Scaffold'   'Solution copy, remove samples, config'
Ensure-Milestone 'M1 Infra'      'BuildingBlocks Grids + Charts + Export (linchpin)'
Ensure-Milestone 'M2 Modules'    'Clients, Expenses, Todos vertical slices'
Ensure-Milestone 'M3 Dashboard'  'DashboardCharts module + registry + default charts'
Ensure-Milestone 'M4 Identity'   'Permissions/roles, single-tenant + admin seed, users grid'
Ensure-Milestone 'M5 API'        'Wiring, migrations, CORS, RTL font'
Ensure-Milestone 'M6 SPA'        'Angular RTL SPA'
Ensure-Milestone 'M7 Docs'       'CLAUDE.md + AGENTS.md'

Write-Host "Done. Next: pwsh ./scripts/gh-create-issues.ps1" -ForegroundColor Green
