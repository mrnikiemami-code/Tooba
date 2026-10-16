# TB-SHIPPING-ONE-LEVEL-TABS-115 — ONE watchdog-bounded isolated Host launch (observation only).
# Isolation rules honoured here:
#   * exactly ONE Start-Process, absolute executable path, no wildcard/name termination;
#   * the ONLY process ever stopped is the recorded PID of the isolated Host;
#   * read-only HTTP GET /health polling, external watchdog <= 180 s;
#   * no shared Host/Next invocation; no HTTP seed endpoint is ever called.
param(
    [int]$WatchdogSeconds = 180
)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$exe = Join-Path $repo '.tmp-115-host-out\Tooba.Host.exe'
$runDir = Join-Path $repo '.tmp-115-run'
$hostDir = Join-Path $repo '.tmp-115-host-out'
$outLog = Join-Path $runDir 'isolated-host-115-stdout.txt'
$errLog = Join-Path $runDir 'isolated-host-115-stderr.txt'
$evidence = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-115'

if (-not (Test-Path -LiteralPath $exe)) { throw "isolated Host executable missing: $exe" }
$exeResolved = (Resolve-Path -LiteralPath $exe).Path

# Effective offline config for the launch (base + isolated overlay), placed next to the Host binary.
Copy-Item -LiteralPath (Join-Path $runDir 'appsettings.json') -Destination (Join-Path $hostDir 'appsettings.json') -Force
Copy-Item -LiteralPath (Join-Path $runDir 'appsettings.Development.json') -Destination (Join-Path $hostDir 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5397'

$launches = 1
$proc = Start-Process -FilePath $exeResolved -WorkingDirectory $hostDir -PassThru -NoNewWindow `
    -RedirectStandardOutput $outLog -RedirectStandardError $errLog
$pid115 = $proc.Id

$started = Get-Date
$health200 = $false
$watchdogHit = $false
$exited = $false
$elapsed = 0
$healthGaveUp = $false

while ($true) {
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    if ($elapsed -ge $WatchdogSeconds) { $watchdogHit = $true; break }
    if ($proc.HasExited) { $exited = $true; break }
    try {
        $resp = Invoke-WebRequest -Uri 'http://127.0.0.1:5397/health' -UseBasicParsing -TimeoutSec 3
        if ($resp.StatusCode -eq 200) { $health200 = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
}

$healthBody = ''
if ($health200) {
    try { $healthBody = (Invoke-WebRequest -Uri 'http://127.0.0.1:5397/health' -UseBasicParsing -TimeoutSec 5).Content } catch { }
}

$killed = $false
if ($watchdogHit -and -not $proc.HasExited) {
    # PID-scoped stop only. Verify the absolute path before stopping.
    $live = Get-Process -Id $pid115 -ErrorAction SilentlyContinue
    if ($live -and $live.Path -eq $exeResolved) {
        Stop-Process -Id $pid115 -Force
        $killed = $true
        Start-Sleep -Seconds 3
    }
}

if (-not $proc.HasExited) { Start-Sleep -Seconds 1 }
$stillRunning = -not $proc.HasExited
$exitCode = ''
try { if ($proc.HasExited) { $exitCode = $proc.ExitCode } } catch { }

$listenersAfter = @(Get-NetTCPConnection -State Listen -LocalPort 5397 -ErrorAction SilentlyContinue).Count

$lines = @(
    "ISOLATED_HOST_PID=$pid115",
    "ISOLATED_HOST_EXE=$exeResolved",
    "LAUNCH_COUNT=$launches",
    "WATCHDOG_LIMIT_SECONDS=$WatchdogSeconds",
    "ELAPSED_SECONDS=$elapsed",
    "HEALTH_200_OBSERVED=$health200",
    "HEALTH_BODY=$healthBody",
    "PROCESS_EXITED=$($proc.HasExited)",
    "EXIT_CODE=$exitCode",
    "STILL_RUNNING_AFTER_STOP=$stillRunning",
    "KILLED_BY_WATCHDOG=$killed",
    "LISTENERS_ON_5397_AFTER_STOP=$listenersAfter"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidence 'watchdog-result.txt') -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }
