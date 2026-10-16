# TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151 — launch the task-owned isolated Host (single launch, PID recorded).
param([int]$WatchdogSeconds = 420, [int]$Port = 5412)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$exe = Join-Path $repo '.tmp-151-host-out\Tooba.Host.exe'
$runDir = Join-Path $repo '.tmp-151-run'
$hostDir = Join-Path $repo '.tmp-151-host-out'
$outLog = Join-Path $runDir 'isolated-host-151-stdout.txt'
$errLog = Join-Path $runDir 'isolated-host-151-stderr.txt'

if (-not (Test-Path -LiteralPath $exe)) { throw "isolated Host executable missing: $exe" }
$exeResolved = (Resolve-Path -LiteralPath $exe).Path

Copy-Item -LiteralPath (Join-Path $runDir 'appsettings.json') -Destination (Join-Path $hostDir 'appsettings.json') -Force
Copy-Item -LiteralPath (Join-Path $runDir 'appsettings.Development.json') -Destination (Join-Path $hostDir 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"

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
        $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 3
        if ($resp.StatusCode -eq 200) { $health200 = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
}

$healthBody = ''
if ($health200) {
    try { $healthBody = (Invoke-WebRequest -Uri "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 5).Content } catch { }
}

$dir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151'
$lines = @(
    "ISOLATED_HOST_PID=$pidHost",
    "ISOLATED_HOST_EXE=$exeResolved",
    "ISOLATED_HOST_URL=http://127.0.0.1:$Port",
    "ISOLATED_HOST_Edition=SingleStore",
    "LAUNCH_COUNT=1",
    "WATCHDOG_LIMIT_SECONDS=$WatchdogSeconds",
    "STARTUP_ELAPSED_SECONDS=$elapsed",
    "WATCHDOG_HIT=$watchdogHit",
    "HAS_EXITED=$($proc.HasExited)",
    "HEALTH_200_OBSERVED=$health200",
    "HEALTH_BODY=$healthBody"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $dir 'host-launch-151.txt') -Encoding utf8
$pidHost | Set-Content -LiteralPath (Join-Path $runDir 'host.pid') -Encoding ascii
$lines | ForEach-Object { Write-Output $_ }
