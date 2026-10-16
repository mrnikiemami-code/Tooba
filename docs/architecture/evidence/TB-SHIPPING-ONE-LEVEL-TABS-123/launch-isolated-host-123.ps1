# TB-SHIPPING-ONE-LEVEL-TABS-123 — ONE watchdog-bounded isolated Host launch (handoff proof target).
# Isolation: exactly ONE Start-Process with an absolute exe path; the ONLY process ever stopped is the
# recorded isolated PID (after verifying its absolute path); no wildcard/name termination; shared :5088
# and :3000 are never touched. Read-only HTTP GET polling plus a bounded keep-alive window.
param(
    [int]$WatchdogSeconds = 180,
    [int]$KeepAliveSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$exe = Join-Path $repo '.tmp-123-host-out\Tooba.Host.exe'
$runDir = Join-Path $repo '.tmp-123-run'
$hostDir = Join-Path $repo '.tmp-123-host-out'
$outLog = Join-Path $runDir 'isolated-host-123-stdout.txt'
$errLog = Join-Path $runDir 'isolated-host-123-stderr.txt'
$evidence = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-123'

if (-not (Test-Path -LiteralPath $exe)) { throw "isolated Host executable missing: $exe" }
$exeResolved = (Resolve-Path -LiteralPath $exe).Path

Copy-Item -LiteralPath (Join-Path $runDir 'appsettings.json') -Destination (Join-Path $hostDir 'appsettings.json') -Force
Copy-Item -LiteralPath (Join-Path $runDir 'appsettings.Development.json') -Destination (Join-Path $hostDir 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5402'

$proc = Start-Process -FilePath $exeResolved -WorkingDirectory $hostDir -PassThru -NoNewWindow `
    -RedirectStandardOutput $outLog -RedirectStandardError $errLog
$pidHost = $proc.Id

$started = Get-Date
$health200 = $false
$watchdogHit = $false
$elapsed = 0
while ($true) {
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    if ($elapsed -ge $WatchdogSeconds) { $watchdogHit = $true; break }
    if ($proc.HasExited) { break }
    try {
        $resp = Invoke-WebRequest -Uri 'http://127.0.0.1:5402/health' -UseBasicParsing -TimeoutSec 3
        if ($resp.StatusCode -eq 200) { $health200 = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
}

$healthBody = ''
$commerce = ''
if ($health200) {
    try { $healthBody = (Invoke-WebRequest -Uri 'http://127.0.0.1:5402/health' -UseBasicParsing -TimeoutSec 5).Content } catch { }
    try { $commerce = (Invoke-WebRequest -Uri 'http://127.0.0.1:5402/__platform-commerce' -UseBasicParsing -TimeoutSec 5).Content } catch { }
}

if ($health200) {
    $deadline = (Get-Date).AddSeconds($KeepAliveSeconds)
    while ((Get-Date) -lt $deadline -and -not $proc.HasExited) { Start-Sleep -Seconds 3 }
}

$killed = $false
if (-not $proc.HasExited) {
    $live = Get-Process -Id $pidHost -ErrorAction SilentlyContinue
    if ($live -and $live.Path -eq $exeResolved) {
        Stop-Process -Id $pidHost -Force
        $killed = $true
        Start-Sleep -Seconds 3
    }
}
$stillRunning = -not $proc.HasExited
$listeners = @(Get-NetTCPConnection -State Listen -LocalPort 5402 -ErrorAction SilentlyContinue).Count

$lines = @(
    "ISOLATED_HOST_PID=$pidHost",
    "ISOLATED_HOST_EXE=$exeResolved",
    "LAUNCH_COUNT=1",
    "WATCHDOG_LIMIT_SECONDS=$WatchdogSeconds",
    "KEEP_ALIVE_SECONDS=$KeepAliveSeconds",
    "STARTUP_ELAPSED_SECONDS=$elapsed",
    "WATCHDOG_HIT=$watchdogHit",
    "HEALTH_200_OBSERVED=$health200",
    "HEALTH_BODY=$healthBody",
    "PLATFORM_COMMERCE_BODY=$commerce",
    "STILL_RUNNING_AFTER_STOP=$stillRunning",
    "STOPPED_BY_RECORDED_PID=$killed",
    "LISTENERS_ON_5402_AFTER_STOP=$listeners"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidence 'watchdog-result.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
