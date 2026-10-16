# TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158 — launch the task-owned private Next frontend.
param([int]$Port = 3112, [int]$HostPort = 5418, [int]$WatchdogSeconds = 240)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$frontend = Join-Path $repo 'src\frontend'
$runDir = Join-Path $repo '.tmp-158-run'
$outLog = Join-Path $runDir 'isolated-frontend-158-stdout.txt'
$errLog = Join-Path $runDir 'isolated-frontend-158-stderr.txt'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ADMIN-BROWSER-QUOTE-ACCEPTANCE-158'

if ($Port -eq 3000 -or $HostPort -eq 5088) { throw 'private frontend must never reuse the shared ports' }

$env:TOOBA_HOST_ORIGIN = "http://127.0.0.1:$HostPort"
$env:NEXT_PUBLIC_TOOBA_HOST_ORIGIN = "http://127.0.0.1:$HostPort"

$proc = Start-Process -FilePath 'npm.cmd' -ArgumentList @('run', 'dev', '--', '-p', "$Port", '-H', '127.0.0.1') `
    -WorkingDirectory $frontend -PassThru -NoNewWindow -RedirectStandardOutput $outLog -RedirectStandardError $errLog
$pidWeb = $proc.Id

$started = Get-Date
$ready = $false
$watchdogHit = $false
$elapsed = 0
while ($true) {
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    if ($elapsed -ge $WatchdogSeconds) { $watchdogHit = $true; break }
    if ($proc.HasExited) { break }
    try {
        $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/fa/admin/shipping-services" -UseBasicParsing -TimeoutSec 5
        if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) { $ready = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
}

$lines = @(
    "ISOLATED_FRONTEND_PID=$pidWeb",
    "ISOLATED_FRONTEND_URL=http://127.0.0.1:$Port",
    "ISOLATED_FRONTEND_HOST_ORIGIN=http://127.0.0.1:$HostPort",
    "PRIVATE_FRONTEND_BOUND_ONLY_TO_PRIVATE_HOST=True",
    "STARTUP_ELAPSED_SECONDS=$elapsed",
    "WATCHDOG_HIT=$watchdogHit",
    "READY_OBSERVED=$ready"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidenceDir 'frontend-launch-158.txt') -Encoding utf8
$pidWeb | Set-Content -LiteralPath (Join-Path $runDir 'frontend.pid') -Encoding ascii
$lines | ForEach-Object { Write-Output $_ }
