# TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169 — task-owned private Next frontend.
param([int]$Port = 3118, [int]$HostPort = 5423, [int]$WatchdogSeconds = 240)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$frontend = Join-Path $repo 'src\frontend'
$runDir = Join-Path $repo '.tmp-169-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ADMIN-MULTILINGUAL-ACTIVATION-169'

if ($Port -eq 3000 -or $HostPort -eq 5088) { throw 'private frontend must never reuse the shared ports' }

$env:TOOBA_HOST_ORIGIN = "http://127.0.0.1:$HostPort"
$env:NEXT_PUBLIC_TOOBA_HOST_ORIGIN = "http://127.0.0.1:$HostPort"

$proc = Start-Process -FilePath 'npm.cmd' -ArgumentList @('run', 'dev', '--', '-p', "$Port", '-H', '127.0.0.1') `
    -WorkingDirectory $frontend -PassThru -NoNewWindow `
    -RedirectStandardOutput (Join-Path $runDir 'isolated-frontend-169-stdout.txt') `
    -RedirectStandardError (Join-Path $runDir 'isolated-frontend-169-stderr.txt')
$proc.Id | Set-Content -LiteralPath (Join-Path $runDir 'frontend.pid') -Encoding ascii

$started = Get-Date
$ready = $false
$watchdogHit = $false
$elapsed = 0
while ($true) {
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    if ($elapsed -ge $WatchdogSeconds) { $watchdogHit = $true; break }
    if ($proc.HasExited) { break }
    try {
        $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/fa/admin/shipping-services" -UseBasicParsing -TimeoutSec 8 -MaximumRedirection 0
        if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) { $ready = $true; break }
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -gt 0) { $ready = $true; break }
    }
    Start-Sleep -Seconds 2
}

$lines = @(
    "ISOLATED_FRONTEND_PID=$($proc.Id)",
    "ISOLATED_FRONTEND_URL=http://127.0.0.1:$Port",
    "ISOLATED_FRONTEND_HOST_ORIGIN=http://127.0.0.1:$HostPort",
    "PRIVATE_FRONTEND_BOUND_ONLY_TO_PRIVATE_HOST=True",
    "STARTUP_ELAPSED_SECONDS=$elapsed",
    "WATCHDOG_HIT=$watchdogHit",
    "READY_OBSERVED=$ready"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidenceDir 'frontend-launch-169.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
