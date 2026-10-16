# TB-SHIPPING-ONE-LEVEL-TABS-122 — exactly ONE task-owned isolated frontend process.
# Binds the existing Next frontend to the isolated Host via the repo's own TOOBA_HOST_ORIGIN seam
# (next.config.ts rewrites /v1/* to that origin). No tracked file is modified; the frontend is started
# in dev mode on a unique loopback port with an external bound, and is stopped only by verified PID.
param(
    [int]$BoundSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$frontend = Join-Path $repo 'src\frontend'
$runDir = Join-Path $repo '.tmp-122-run'
$outLog = Join-Path $runDir 'isolated-frontend-122-stdout.txt'
$errLog = Join-Path $runDir 'isolated-frontend-122-stderr.txt'
$evidence = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-122'

$nextBin = Join-Path $frontend 'node_modules\next\dist\bin\next'
if (-not (Test-Path -LiteralPath $nextBin)) { throw "next binary missing: $nextBin" }

$env:TOOBA_HOST_ORIGIN = 'http://127.0.0.1:5401'
$env:PORT = '3101'
$env:NEXT_TELEMETRY_DISABLED = '1'

$nodeExe = (Get-Command node).Source
$proc = Start-Process -FilePath $nodeExe -ArgumentList @($nextBin, 'dev', '-p', '3101') `
    -WorkingDirectory $frontend -PassThru -NoNewWindow `
    -RedirectStandardOutput $outLog -RedirectStandardError $errLog
$pidF = $proc.Id

# External bound: poll the frontend readiness only; never restart, never relaunch.
$started = Get-Date
$ready = $false
$elapsed = 0
while ($elapsed -lt 180) {
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    if ($proc.HasExited) { break }
    try {
        $resp = Invoke-WebRequest -Uri 'http://127.0.0.1:3101/fa/shipping' -UseBasicParsing -TimeoutSec 10
        if ($resp.StatusCode -eq 200) { $ready = $true; break }
    } catch { }
    Start-Sleep -Seconds 3
}

$deadline = (Get-Date).AddSeconds($BoundSeconds)
while ((Get-Date) -lt $deadline -and -not $proc.HasExited) { Start-Sleep -Seconds 5 }

$killed = $false
if (-not $proc.HasExited) {
    $live = Get-Process -Id $pidF -ErrorAction SilentlyContinue
    if ($live -and $live.Path -eq $nodeExe) {
        Stop-Process -Id $pidF -Force
        $killed = $true
        Start-Sleep -Seconds 3
    }
}
$listeners = @(Get-NetTCPConnection -State Listen -LocalPort 3101 -ErrorAction SilentlyContinue).Count

$lines = @(
    "ISOLATED_FRONTEND_PID=$pidF",
    "ISOLATED_FRONTEND_EXE=$nodeExe",
    "ISOLATED_FRONTEND_HOST_ORIGIN=$env:TOOBA_HOST_ORIGIN",
    "LAUNCH_COUNT=1",
    "READY_WITHIN_180S=$ready",
    "READY_ELAPSED_SECONDS=$elapsed",
    "STOPPED_BY_RECORDED_PID=$killed",
    "LISTENERS_ON_3101_AFTER_STOP=$listeners"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidence 'frontend-watchdog-result.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
