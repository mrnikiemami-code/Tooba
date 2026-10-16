# TB-SHIPPING-ONE-LEVEL-TABS-142 — launch the task-owned isolated Marketplace Host (single launch, PID recorded).
param([int]$WatchdogSeconds = 180, [int]$Port = 5410)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$exe = Join-Path $repo '.tmp-140-host-out\Tooba.Host.exe'
$runDir = Join-Path $repo '.tmp-142-run'
$hostDir = Join-Path $repo '.tmp-140-host-out'
$outLog = Join-Path $runDir 'isolated-host-142-stdout.txt'
$errLog = Join-Path $runDir 'isolated-host-142-stderr.txt'

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
$readyStatus = ''
$readyBody = ''
if ($health200) {
    try { $healthBody = (Invoke-WebRequest -Uri "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 5).Content } catch { }
    try {
        $rd = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/ready" -UseBasicParsing -TimeoutSec 10
        $readyStatus = $rd.StatusCode
        $readyBody = $rd.Content
    } catch {
        $readyStatus = "$($_.Exception.Response.StatusCode.value__)"
        try { $readyBody = (New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd() } catch { }
    }
}

$dir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-142'
$lines = @(
    "ISOLATED_HOST_PID=$pidHost",
    "ISOLATED_HOST_EXE=$exeResolved",
    "ISOLATED_HOST_URL=http://127.0.0.1:$Port",
    "ISOLATED_HOST_EDITION=Marketplace",
    "LAUNCH_COUNT=1",
    "WATCHDOG_LIMIT_SECONDS=$WatchdogSeconds",
    "STARTUP_ELAPSED_SECONDS=$elapsed",
    "WATCHDOG_HIT=$watchdogHit",
    "HAS_EXITED=$($proc.HasExited)",
    "HEALTH_200_OBSERVED=$health200",
    "HEALTH_BODY=$healthBody",
    "READY_STATUS=$readyStatus",
    "READY_BODY=$readyBody"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $dir 'host-launch-142.txt') -Encoding utf8
$pidHost | Set-Content -LiteralPath (Join-Path $runDir 'host.pid') -Encoding ascii
$lines | ForEach-Object { Write-Output $_ }
