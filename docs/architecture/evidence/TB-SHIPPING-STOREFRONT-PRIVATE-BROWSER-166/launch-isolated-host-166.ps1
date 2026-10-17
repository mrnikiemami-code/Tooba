# TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166 — launch the task-owned isolated Host.
param([int]$WatchdogSeconds = 300, [int]$Port = 5421)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$exe = Join-Path $repo '.tmp-166-host-out\Tooba.Host.exe'
$runDir = Join-Path $repo '.tmp-166-run'
$hostDir = Join-Path $repo '.tmp-166-host-out'
$gate = if ($env:TOOBA_166_GATE) { $env:TOOBA_166_GATE } else { 'true' }
$outLog = Join-Path $runDir "isolated-host-166-$gate-stdout.txt"
$errLog = Join-Path $runDir "isolated-host-166-$gate-stderr.txt"

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

"ISOLATED_HOST_PID=$pidHost"
"ISOLATED_HOST_EXE=$exeResolved"
"ISOLATED_HOST_URL=http://127.0.0.1:$Port"
"SCRATCH_GATE=$gate"
"STARTUP_ELAPSED_SECONDS=$elapsed"
"WATCHDOG_HIT=$watchdogHit"
"HAS_EXITED=$($proc.HasExited)"
"HEALTH_200_OBSERVED=$health200"
$pidHost | Set-Content -LiteralPath (Join-Path $runDir "host-$gate.pid") -Encoding ascii
