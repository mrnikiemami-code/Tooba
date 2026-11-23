<#
.SYNOPSIS
  Portable, idempotent bootstrap for the TOOBA OpenCode isolated worktree.

.DESCRIPTION
  Creates (or reuses) a dedicated git worktree for OpenCode agents, reads the
  OpenCode-specific rules from .opencode/, and activates the SHORT AGENTS.md
  ONLY inside that isolated worktree. It NEVER writes the repository-root
  AGENTS.md and NEVER touches .cursor/**, src/**, or product code.
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,
    [string]$WorktreePath,
    [string]$Branch     = 'opencode/iso',
    [string]$BaseBranch = 'main'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Fail([string]$msg) {
    Write-Error $msg
    exit 1
}

function HashFile([string]$p) {
    if (Test-Path -LiteralPath $p) {
        return (Get-FileHash -Algorithm SHA256 -LiteralPath $p).Hash
    }
    return 'MISSING'
}

# --- 1. Locate the Tooba repository -----------------------------------------
if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}
if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot '.git'))) {
    Fail "Not a git repository root: $RepoRoot"
}
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

foreach ($need in @('.opencode\AGENTS.md', '.opencode\iso-rules', 'AGENTS.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $need))) {
        Fail "Missing required asset: $need"
    }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Fail 'git not found on PATH'
}

# --- Defaults for the isolated worktree path --------------------------------
if ([string]::IsNullOrWhiteSpace($WorktreePath)) {
    $parent = Split-Path -Parent $RepoRoot
    $WorktreePath = Join-Path $parent ((Split-Path -Leaf $RepoRoot) + '-opencode')
}
$WorktreePath = [System.IO.Path]::GetFullPath($WorktreePath)

if ($WorktreePath.TrimEnd('\', '/') -eq $RepoRoot.TrimEnd('\', '/')) {
    Fail 'WorktreePath must differ from RepoRoot (refusing to overwrite the main tree).'
}

# --- Safety snapshot of the main tree ---------------------------------------
$mainAgents   = Join-Path $RepoRoot 'AGENTS.md'
$agentsBefore = HashFile $mainAgents
$cursorBefore = @(git -C $RepoRoot status --porcelain -- .cursor 2>$null | Sort-Object) -join "`n"

# --- 2. Create the dedicated OpenCode worktree ------------------------------
$registeredPaths = @()
foreach ($line in (git -C $RepoRoot worktree list --porcelain)) {
    if ($line -match '^worktree\s+(.+)$') {
        $registeredPaths += ($matches[1].Trim() -replace '/', '\')
    }
}
$target      = $WorktreePath.TrimEnd('\')
$isRegistered = $false
foreach ($p in $registeredPaths) {
    if (($p.TrimEnd('\')) -ieq $target) { $isRegistered = $true; break }
}

if (-not $isRegistered) {
    if (Test-Path -LiteralPath $WorktreePath) {
        Fail "Path exists but is not a registered git worktree: $WorktreePath"
    }
    git -C $RepoRoot rev-parse --verify --quiet "refs/heads/$Branch" 2>$null | Out-Null
    $branchExists = ($LASTEXITCODE -eq 0)

    if ($branchExists) {
        Write-Host "Adding worktree for existing branch '$Branch'..."
        git -C $RepoRoot worktree add $WorktreePath $Branch
    }
    else {
        Write-Host "Creating branch '$Branch' from '$BaseBranch' and adding worktree..."
        git -C $RepoRoot worktree add -b $Branch $WorktreePath $BaseBranch
    }
    if ($LASTEXITCODE -ne 0) {
        Fail "git worktree add failed (exit $LASTEXITCODE)"
    }
}
else {
    Write-Host "Reusing existing worktree: $WorktreePath"
}

# --- 3. Read the canonical OpenCode rules from .opencode/ -------------------
$shortAgentsSrc = Join-Path $RepoRoot '.opencode\AGENTS.md'
$shortAgents    = Get-Content -LiteralPath $shortAgentsSrc -Raw
$ruleFiles      = @(Get-ChildItem -LiteralPath (Join-Path $RepoRoot '.opencode\iso-rules') -Filter *.md -File | Sort-Object Name)
if ($ruleFiles.Count -eq 0) {
    Fail 'No iso-rules found in .opencode/iso-rules'
}
Write-Host ("Loaded {0} OpenCode rule file(s) from .opencode/iso-rules" -f $ruleFiles.Count)

# --- 4. Activate short AGENTS.md ONLY in the isolated worktree --------------
$worktreeAgents = Join-Path $WorktreePath 'AGENTS.md'
$current = if (Test-Path -LiteralPath $worktreeAgents) {
    Get-Content -LiteralPath $worktreeAgents -Raw
}
else {
    $null
}
if ($current -ne $shortAgents) {
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($worktreeAgents, $shortAgents, $utf8NoBom)
    Write-Host 'Activated short AGENTS.md in isolated worktree (uncommitted by design).'
}
else {
    Write-Host 'Short AGENTS.md already active in isolated worktree.'
}

# --- 5. Guarantee the main tree and Cursor files were NOT changed -----------
$agentsAfter = HashFile $mainAgents
if ($agentsAfter -ne $agentsBefore) {
    Fail 'SAFETY VIOLATION: repository-root AGENTS.md changed.'
}
$cursorAfter = @(git -C $RepoRoot status --porcelain -- .cursor 2>$null | Sort-Object) -join "`n"
if ($cursorAfter -ne $cursorBefore) {
    Fail 'SAFETY VIOLATION: .cursor/** changed.'
}

Write-Host ''
Write-Host 'OpenCode isolated worktree ready.'
Write-Host ("  RepoRoot        : {0}" -f $RepoRoot)
Write-Host ("  WorktreePath    : {0}" -f $WorktreePath)
Write-Host ("  Branch          : {0}" -f $Branch)
Write-Host ("  BaseBranch      : {0}" -f $BaseBranch)
Write-Host '  Root AGENTS.md  : unchanged (SHA-256 matched)'
Write-Host '  .cursor/**      : untouched'
Write-Host '  Short AGENTS.md : active in isolated worktree only'
exit 0
