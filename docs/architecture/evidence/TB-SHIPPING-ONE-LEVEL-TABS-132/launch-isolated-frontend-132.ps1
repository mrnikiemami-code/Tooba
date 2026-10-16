# TB-SHIPPING-ONE-LEVEL-TABS-132 — exactly ONE task-owned isolated frontend process.
# Binds the existing Next frontend to the isolated Host via the repo's own TOOBA_HOST_ORIGIN seam
# (next.config.ts rewrites /v1/* to that origin). No tracked file is modified.
# Starts and RETURNS immediately with the recorded PID; readiness is polled separately.
$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$frontend = Join-Path $repo 'src\frontend'
$runDir = Join-Path $repo '.tmp-132-run'
$outLog = Join-Path $runDir 'isolated-frontend-132-stdout.txt'
$errLog = Join-Path $runDir 'isolated-frontend-132-stderr.txt'

$nextBin = Join-Path $frontend 'node_modules\next\dist\bin\next'
if (-not (Test-Path -LiteralPath $nextBin)) { throw "next binary missing: $nextBin" }

$env:TOOBA_HOST_ORIGIN = 'http://127.0.0.1:5402'
$env:NEXT_TELEMETRY_DISABLED = '1'

$nodeExe = (Get-Command node).Source
$proc = Start-Process -FilePath $nodeExe -ArgumentList @($nextBin, 'dev', '-p', '3102') `
    -WorkingDirectory $frontend -PassThru -NoNewWindow `
    -RedirectStandardOutput $outLog -RedirectStandardError $errLog
$proc.Id | Set-Content -LiteralPath (Join-Path $runDir 'frontend.pid') -Encoding ascii
$nodeExe | Set-Content -LiteralPath (Join-Path $runDir 'frontend-exe.txt') -Encoding ascii
Write-Output "ISOLATED_FRONTEND_PID=$($proc.Id)"
Write-Output "ISOLATED_FRONTEND_EXE=$nodeExe"
Write-Output "ISOLATED_FRONTEND_HOST_ORIGIN=$env:TOOBA_HOST_ORIGIN"
