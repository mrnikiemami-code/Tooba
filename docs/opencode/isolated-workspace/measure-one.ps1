# OpenCode token measurement harness — file redirects (no pipe deadlock).
param(
  [Parameter(Mandatory=$true)][string]$Label,
  [Parameter(Mandatory=$true)][string]$PromptFile,
  [string]$Model = "metisdeepseek/deepseek-v4-flash",
  [string]$OutDir = "docs/opencode/optimization/runs"
)

$ErrorActionPreference = "Stop"
$root = (Get-Location).Path
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outJsonl = Join-Path $OutDir "$Label-$stamp.jsonl"
$outMeta  = Join-Path $OutDir "$Label-$stamp.meta.json"
$outErr   = Join-Path $OutDir "$Label-$stamp.stderr.txt"
$prompt = Get-Content -LiteralPath $PromptFile -Raw -Encoding UTF8

$exe = Join-Path $env:APPDATA "npm\node_modules\opencode-ai\bin\opencode.exe"
if (-not (Test-Path $exe)) { throw "opencode.exe not found at $exe" }

$sw = [Diagnostics.Stopwatch]::StartNew()
$args = @(
  "run",
  "--format","json",
  "--title","opt-$Label",
  "-m",$Model,
  $prompt
)
$p = Start-Process -FilePath $exe -ArgumentList $args -WorkingDirectory $root `
  -RedirectStandardOutput $outJsonl -RedirectStandardError $outErr `
  -Wait -PassThru -NoNewWindow
$sw.Stop()

$stdout = ""
if (Test-Path $outJsonl) { $stdout = [IO.File]::ReadAllText($outJsonl) }

function Sum-Tokens($jsonl) {
  $inputT=0; $outputT=0; $reasonT=0; $cacheRPeak=0; $cacheW=0; $tools=0; $steps=0; $text=""; $lastTotal=0
  $errorRef=$null; $sessionId=$null
  foreach($line in ($jsonl -split "`n")) {
    $line=$line.Trim()
    if(-not $line -or -not $line.StartsWith("{")) { continue }
    try { $o = $line | ConvertFrom-Json } catch { continue }
    if($o.sessionID){ $sessionId=$o.sessionID }
    if($o.type -eq "error"){ $errorRef = $o.error.data.ref; if(-not $errorRef){ $errorRef = $o.error.name } }
    if($o.type -eq "tool_use"){ $tools++ }
    if($o.type -eq "text" -and $o.part.text){ $text += [string]$o.part.text }
    if($o.type -eq "step_finish" -and $o.part.tokens){
      $steps++
      $t=$o.part.tokens
      $inputT  += [int]($t.input)
      $outputT += [int]($t.output)
      $reasonT += [int]($t.reasoning)
      if($t.cache){
        $cr=[int]($t.cache.read); if($cr -gt $cacheRPeak){ $cacheRPeak=$cr }
        $cacheW += [int]($t.cache.write)
      }
      $lastTotal = [int]($t.total)
    }
  }
  [pscustomobject]@{
    sessionId=$sessionId; error=$errorRef; steps=$steps; tools=$tools
    input=$inputT; output=$outputT; reasoning=$reasonT
    cacheReadPeak=$cacheRPeak; cacheWrite=$cacheW; lastStepTotal=$lastTotal
    textPreview= if($text.Length -gt 500){ $text.Substring(0,500)+"…" } else { $text }
  }
}

$stats = Sum-Tokens $stdout
$meta = [ordered]@{
  label=$Label
  model=$Model
  promptFile=$PromptFile
  started=$stamp
  elapsedMs=$sw.ElapsedMilliseconds
  exitCode=$p.ExitCode
  jsonl=$outJsonl
  sessionId=$stats.sessionId
  error=$stats.error
  steps=$stats.steps
  tools=$stats.tools
  tokens=@{
    incrementalInput=$stats.input
    output=$stats.output
    reasoning=$stats.reasoning
    cacheReadPeak=$stats.cacheReadPeak
    cacheWrite=$stats.cacheWrite
    lastStepTotal=$stats.lastStepTotal
  }
  textPreview=$stats.textPreview
}
$meta | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $outMeta -Encoding UTF8
$meta | ConvertTo-Json -Compress
