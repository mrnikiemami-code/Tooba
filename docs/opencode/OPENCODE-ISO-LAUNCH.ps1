# Open OpenCode against the isolated worktree (reversible)
# Does NOT stop or retarget existing Desktop sessions (e.g. task 286).
# Does NOT rewrite main AGENTS.md.

param(
  [ValidateSet('DocumentOnly','CliProjectCheck','NewDesktopWindow')]
  [string]$Mode = 'DocumentOnly'
)

$ErrorActionPreference = 'Stop'
$IsoRoot = 'D:\Users\User\source\repos\SarvNewVer-opencode-iso-002'
$MainRoot = 'D:\Users\User\source\repos\SarvNewVer'
$Desktop = 'C:\Users\User\AppData\Local\Programs\@opencode-aidesktop\OpenCode.exe'
$MainAgentsExpected = '06DC5A5FEAC45958667BB8F7DE0174BAE478B11F2A7F806C492E33BC6339F090'

function Assert-Safety {
  if (-not (Test-Path -LiteralPath $IsoRoot)) { throw "Iso worktree missing: $IsoRoot" }
  $h = (Get-FileHash -LiteralPath (Join-Path $MainRoot 'AGENTS.md') -Algorithm SHA256).Hash
  if ($h -ne $MainAgentsExpected) { throw "Main AGENTS.md hash drift: $h" }
  $branch = (git -C $IsoRoot branch --show-current).Trim()
  if ($branch -ne 'opencode/iso-agents-002') { throw "Unexpected iso branch: $branch" }
  Write-Output "OK main AGENTS hash=$h"
  Write-Output "OK iso path=$IsoRoot branch=$branch head=$((git -C $IsoRoot rev-parse --short HEAD).Trim())"
  Write-Output "OK short AGENTS bytes=$((Get-Item (Join-Path $IsoRoot 'AGENTS.md')).Length)"
  1..5 | ForEach-Object {
    $p = Join-Path $IsoRoot ("docs/opencode/iso-rules/{0:D2}-*.md" -f $_)
    # fixed names:
  }
  foreach ($n in @(
    '01-architecture.md','02-security-permissions.md','03-openspec.md',
    '04-opencode-acceptance.md','05-task-layer.md','06-backend-layer.md',
    '07-frontend-layer.md','08-amsc-layer.md'
  )) {
    $fp = Join-Path $IsoRoot "docs/opencode/iso-rules/$n"
    if (-not (Test-Path $fp)) { throw "Missing rule pack $n" }
  }
  Write-Output 'OK iso-rules 01-08 present'
  foreach ($s in @(
    '.cursor/skills/tooba-backend-feature/SKILL.md',
    '.cursor/skills/tooba-frontend-feature/SKILL.md'
  )) {
    if (-not (Test-Path (Join-Path $IsoRoot $s))) { throw "Missing skill $s" }
  }
  Write-Output 'OK FE/BE skills present'
}

Assert-Safety

switch ($Mode) {
  'DocumentOnly' {
    Write-Output 'Mode=DocumentOnly — no Desktop launch (safe beside task 286).'
    Write-Output "To open manually: File → Open Folder → $IsoRoot"
    Write-Output "Or: Start-Process '$Desktop' (then open iso folder in a NEW window)."
  }
  'CliProjectCheck' {
    Write-Output 'Mode=CliProjectCheck — verifies CLI sees iso project without killing Desktop.'
    $oc = 'C:\Users\User\AppData\Roaming\npm\opencode.cmd'
    & $oc --version
    Push-Location $IsoRoot
    try {
      # Non-interactive identity: print cwd + AGENTS size only via shell here
      Write-Output "cwd=$(Get-Location)"
      Write-Output "agents=$((Get-Item .\AGENTS.md).Length)"
    } finally { Pop-Location }
  }
  'NewDesktopWindow' {
    if (-not (Test-Path $Desktop)) { throw "Desktop missing: $Desktop" }
    Write-Output 'Launching NEW Desktop process; do not close existing 286 session.'
    Start-Process -FilePath $Desktop -WorkingDirectory $IsoRoot
    Write-Output "Started OpenCode.exe cwd=$IsoRoot — open this folder if prompted."
  }
}

Write-Output 'Rollback: see docs/opencode/isolated-workspace/WORKSPACE-SELECT-AND-ROLLBACK.md on main.'
