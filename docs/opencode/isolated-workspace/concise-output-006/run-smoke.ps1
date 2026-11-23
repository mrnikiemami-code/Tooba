$ErrorActionPreference = 'Continue'
$iso = 'D:\Users\User\source\repos\SarvNewVer-opencode-iso-002'
$main = 'D:\Users\User\source\repos\SarvNewVer'
$OutDir = Join-Path $main 'docs/opencode/isolated-workspace/concise-output-006'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$pidsBefore = @((Get-Process OpenCode -EA SilentlyContinue).Id)
Write-Output ("PIDs before: " + ($pidsBefore -join ','))

Push-Location $iso
$cfg = & "$env:APPDATA\npm\opencode.cmd" debug config 2>&1 | Out-String
Pop-Location
if ($cfg -notmatch 'concise-chat-output') { throw 'instructions not resolved in iso config' }
Write-Output 'OK instructions resolved'

$prompt = @'
TASK ID for this check: TOOBA-OPENCODE-CONCISE-OUTPUT-006-SMOKE
No product code edits. Do not write files under src/.

1) Read .opencode/instructions/concise-chat-output.md
2) Read only the first 40 lines of src/backend/Modules/Notification/Tooba.Notification.Contracts/Commands/CreateNotificationCommand.cs
3) Your FINAL chat message must obey the concise template exactly (TASK/STATUS/RESULT/BLOCKER/CODE/EVIDENCE/DURATION/STOP_FOR_ARCHITECT).
4) RESULT one sentence naming the owning module. Put nothing else in chat.
'@
$promptFile = Join-Path $OutDir 'smoke-prompt.txt'
Set-Content -LiteralPath $promptFile -Value $prompt -Encoding utf8

$cfgPath = Join-Path $iso '.opencode/opencode.json'
$bak = Get-Content $cfgPath -Raw
$o = $bak | ConvertFrom-Json
$o.permission.edit = 'deny'
($o | ConvertTo-Json -Depth 6) | Set-Content $cfgPath -Encoding utf8

$exe = Join-Path $env:APPDATA 'npm\node_modules\opencode-ai\bin\opencode.exe'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$jsonl = Join-Path $OutDir ("smoke-" + $stamp + ".jsonl")
$err = Join-Path $OutDir ("smoke-" + $stamp + ".stderr.txt")
$sw = [Diagnostics.Stopwatch]::StartNew()
$p = Start-Process -FilePath $exe -ArgumentList @(
  'run','--format','json','--title','concise-006','-m','metisdeepseek/deepseek-v4-flash',(Get-Content $promptFile -Raw)
) -WorkingDirectory $iso -RedirectStandardOutput $jsonl -RedirectStandardError $err -Wait -PassThru -NoNewWindow
$sw.Stop()
Set-Content -LiteralPath $cfgPath -Value $bak -Encoding utf8

$text = ''
$tools = 0
foreach ($line in [IO.File]::ReadAllLines($jsonl)) {
  $tline = $line.Trim()
  if (-not $tline.StartsWith('{')) { continue }
  try { $j = $tline | ConvertFrom-Json } catch { continue }
  if ($j.type -eq 'tool_use') { $tools++ }
  if ($j.type -eq 'text' -and $j.part.text) { $text += [string]$j.part.text }
}

$chars = $text.Length
$hasTemplate = ($text -match 'TASK:' -and $text -match 'STATUS:' -and $text -match 'STOP_FOR_ARCHITECT')
$narration = 0
foreach ($pat in @('I''ll start','Now reading','Next I','Let me ')) {
  if ($text -like ("*" + $pat + "*")) { $narration++ }
}
$productDiff = @(git -C $iso diff --name-only -- src/frontend src/backend .cursor 2>$null)
$agents = (Get-FileHash (Join-Path $main 'AGENTS.md') -Algorithm SHA256).Hash
$pidsAfter = @((Get-Process OpenCode -EA SilentlyContinue).Id)
$preserved = ($pidsBefore | Where-Object { $pidsAfter -contains $_ }).Count

$meta = [ordered]@{
  exitCode = $p.ExitCode
  elapsedMs = $sw.ElapsedMilliseconds
  tools = $tools
  finalTextChars = $chars
  hasTemplate = [bool]$hasTemplate
  narrationPatternHits = $narration
  finalText = $text
  productDiff = @($productDiff)
  mainAgentsSha = $agents
  desktopPidsPreserved = ("$preserved/" + $pidsBefore.Count)
  verbosityKeyInSchema = $false
  officialMechanism = 'instructions[] -> .opencode/instructions/concise-chat-output.md'
}
$meta | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $OutDir 'smoke-result.json') -Encoding utf8
Write-Output ("exit=" + $p.ExitCode + " chars=" + $chars + " template=" + $hasTemplate + " narrationHits=" + $narration + " tools=" + $tools + " ms=" + $sw.ElapsedMilliseconds)
Write-Output ("agents=" + $agents)
Write-Output ("pids=" + $meta.desktopPidsPreserved)
Write-Output 'TEXT_BEGIN'
Write-Output $text
Write-Output 'TEXT_END'
