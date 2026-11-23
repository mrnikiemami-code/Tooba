# Smoke runner for PRODUCTION-READY-004 — analysis only; no product edits.
$ErrorActionPreference = 'Continue'
$Iso = 'D:\Users\User\source\repos\SarvNewVer-opencode-iso-002'
$Main = 'D:\Users\User\source\repos\SarvNewVer'
$OutDir = Join-Path $Iso 'docs/opencode/smoke/runs'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$beforeMainAgents = (Get-FileHash (Join-Path $Main 'AGENTS.md') -Algorithm SHA256).Hash
git -C $Iso status --porcelain | Set-Content (Join-Path $OutDir 'git-before.txt')

$openspec = & (Join-Path $Iso 'docs/opencode/iso-bin/openspec.cmd') --version 2>&1 | Out-String
$impeccable = & (Join-Path $Iso 'docs/opencode/iso-bin/impeccable.cmd') --version 2>&1 | Out-String
"openspec=$openspec" | Set-Content (Join-Path $OutDir 'tools.txt')
"impeccable=$impeccable" | Add-Content (Join-Path $OutDir 'tools.txt')

& (Join-Path $Iso 'docs/opencode/OPENCODE-ISO-LAUNCH.ps1') -Mode DocumentOnly *>&1 | Set-Content (Join-Path $OutDir 'launch-check.txt')

$cfgPath = Join-Path $Iso '.opencode/opencode.json'
$cfgBackup = Get-Content $cfgPath -Raw
$cfg = $cfgBackup | ConvertFrom-Json
$cfg.permission.edit = 'deny'
($cfg | ConvertTo-Json -Depth 6) | Set-Content $cfgPath -Encoding utf8

$exe = Join-Path $env:APPDATA 'npm\node_modules\opencode-ai\bin\opencode.exe'
$prompt = Get-Content (Join-Path $Iso 'docs/opencode/smoke/SMOKE-004-PROMPT.txt') -Raw -Encoding UTF8
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$jsonl = Join-Path $OutDir "smoke-004-$stamp.jsonl"
$err = Join-Path $OutDir "smoke-004-$stamp.stderr.txt"
$meta = Join-Path $OutDir "smoke-004-$stamp.meta.json"

$sw = [Diagnostics.Stopwatch]::StartNew()
$p = Start-Process -FilePath $exe -ArgumentList @(
  'run','--format','json','--title','smoke-004','-m','metisdeepseek/deepseek-v4-flash',$prompt
) -WorkingDirectory $Iso -RedirectStandardOutput $jsonl -RedirectStandardError $err -Wait -PassThru -NoNewWindow
$sw.Stop()

Set-Content -LiteralPath $cfgPath -Value $cfgBackup -Encoding utf8

$afterMainAgents = (Get-FileHash (Join-Path $Main 'AGENTS.md') -Algorithm SHA256).Hash
git -C $Iso status --porcelain | Set-Content (Join-Path $OutDir 'git-after.txt')

$productDiff = @(git -C $Iso diff --name-only -- src/frontend src/backend .cursor 2>$null)
if ($null -eq $productDiff) { $productDiff = @() }

$text = ''
$tools = 0; $steps = 0; $inputT = 0; $outputT = 0; $lastTotal = 0; $cachePeak = 0
$session = $null
$raw = ''
if (Test-Path $jsonl) { $raw = [IO.File]::ReadAllText($jsonl) }
foreach ($line in ($raw -split "`n")) {
  $line = $line.Trim(); if (-not $line.StartsWith('{')) { continue }
  try { $o = $line | ConvertFrom-Json } catch { continue }
  if ($o.sessionID) { $session = $o.sessionID }
  if ($o.type -eq 'tool_use') { $tools++ }
  if ($o.type -eq 'text' -and $o.part.text) { $text += [string]$o.part.text }
  if ($o.type -eq 'step_finish' -and $o.part.tokens) {
    $steps++
    $t = $o.part.tokens
    $inputT += [int]$t.input
    $outputT += [int]$t.output
    if ($t.cache) { $cr = [int]$t.cache.read; if ($cr -gt $cachePeak) { $cachePeak = $cr } }
    $lastTotal = [int]$t.total
  }
}

$result = [ordered]@{
  exitCode = $p.ExitCode
  elapsedMs = $sw.ElapsedMilliseconds
  sessionId = $session
  steps = $steps
  tools = $tools
  tokens = @{ input = $inputT; output = $outputT; cacheReadPeak = $cachePeak; lastStepTotal = $lastTotal }
  mainAgentsBefore = $beforeMainAgents
  mainAgentsAfter = $afterMainAgents
  mainAgentsUnchanged = ($beforeMainAgents -eq $afterMainAgents -and $afterMainAgents -eq '06DC5A5FEAC45958667BB8F7DE0174BAE478B11F2A7F806C492E33BC6339F090')
  productDiffFiles = @($productDiff)
  openspecVersion = ($openspec.Trim())
  impeccableVersion = ($impeccable.Trim())
  textPreview = if ($text.Length -gt 2500) { $text.Substring(0,2500) + '…' } else { $text }
  headingChecks = @{
    RulesLoaded = [bool]($text -match '##\s*RulesLoaded')
    BackendOwner = [bool]($text -match '##\s*BackendOwner')
    FrontendSurface = [bool]($text -match '##\s*FrontendSurface')
    AmscVerdict = [bool]($text -match '##\s*AmscVerdict')
    Compliance = [bool]($text -match '##\s*Compliance')
    EditsNone = [bool]($text -match '##\s*Edits' -and $text -match 'NONE')
  }
  jsonl = $jsonl
}
$result | ConvertTo-Json -Depth 6 | Set-Content $meta -Encoding utf8
Write-Output "META=$meta"
Write-Output "exit=$($p.ExitCode) elapsedMs=$($sw.ElapsedMilliseconds) tools=$tools lastTotal=$lastTotal"
Write-Output "mainAgentsUnchanged=$($result.mainAgentsUnchanged)"
Write-Output "headings=$($result.headingChecks | ConvertTo-Json -Compress)"
