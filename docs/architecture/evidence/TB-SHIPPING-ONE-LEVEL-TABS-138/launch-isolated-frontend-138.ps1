# TB-SHIPPING-ONE-LEVEL-TABS-138 — exactly ONE task-owned isolated frontend process on port 3107.
$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$frontend = Join-Path $repo 'src\frontend'
$runDir = Join-Path $repo '.tmp-138-run'
$outLog = Join-Path $runDir 'isolated-frontend-138-stdout.txt'
$errLog = Join-Path $runDir 'isolated-frontend-138-stderr.txt'

$nextBin = Join-Path $frontend 'node_modules\next\dist\bin\next'
if (-not (Test-Path -LiteralPath $nextBin)) { throw "next binary missing: $nextBin" }

$env:TOOBA_HOST_ORIGIN = 'http://127.0.0.1:5407'
$env:NEXT_TELEMETRY_DISABLED = '1'

$nodeExe = (Get-Command node).Source
$proc = Start-Process -FilePath $nodeExe -ArgumentList @($nextBin, 'dev', '-p', '3107') `
    -WorkingDirectory $frontend -PassThru -NoNewWindow `
    -RedirectStandardOutput $outLog -RedirectStandardError $errLog
$proc.Id | Set-Content -LiteralPath (Join-Path $runDir 'frontend.pid') -Encoding ascii
$nodeExe | Set-Content -LiteralPath (Join-Path $runDir 'frontend-exe.txt') -Encoding ascii
Write-Output "ISOLATED_FRONTEND_PID=$($proc.Id)"
Write-Output "ISOLATED_FRONTEND_EXE=$nodeExe"
Write-Output "ISOLATED_FRONTEND_HOST_ORIGIN=$env:TOOBA_HOST_ORIGIN"


