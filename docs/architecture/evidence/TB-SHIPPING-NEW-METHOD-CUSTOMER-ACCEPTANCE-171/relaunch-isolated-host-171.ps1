# TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171 — relaunch the task-owned isolated Host
# ONLY (container/database already migrated). Used for the OFF control in the same scratch context.
param([int]$WatchdogSeconds = 180, [int]$Port = 5425, [string]$Gate = 'false')

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$hostDir = Join-Path $repo '.tmp-171-run\host'
$runDir = Join-Path $repo '.tmp-171-run'
$exe = Join-Path $hostDir 'Tooba.Host.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "isolated Host executable missing: $exe" }

$env:TOOBA_171_GATE = $Gate
node (Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171\prepare-isolated-target-171.js')
if ($LASTEXITCODE -ne 0) { throw 'isolation guard failed closed' }

Copy-Item -LiteralPath (Join-Path $runDir 'appsettings.json') -Destination (Join-Path $hostDir 'appsettings.json') -Force
Copy-Item -LiteralPath (Join-Path $runDir 'appsettings.Development.json') -Destination (Join-Path $hostDir 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"

$proc = Start-Process -FilePath $exe -WorkingDirectory $hostDir -PassThru -NoNewWindow `
    -RedirectStandardOutput (Join-Path $runDir "isolated-host-171-$Gate-stdout.txt") `
    -RedirectStandardError (Join-Path $runDir "isolated-host-171-$Gate-stderr.txt")
$proc.Id | Set-Content -LiteralPath (Join-Path $runDir "host-$Gate.pid") -Encoding ascii

$health = $false
$started = Get-Date
$elapsed = 0
while ($true) {
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    if ($elapsed -ge $WatchdogSeconds) { break }
    if ($proc.HasExited) { break }
    try {
        $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/health" -UseBasicParsing -TimeoutSec 3
        if ($resp.StatusCode -eq 200) { $health = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
}

"ISOLATED_HOST_PID=$($proc.Id)"
"SCRATCH_GATE=$Gate"
"STARTUP_ELAPSED_SECONDS=$elapsed"
"HEALTH_200_OBSERVED=$health"
if (-not $health) { exit 5 }
