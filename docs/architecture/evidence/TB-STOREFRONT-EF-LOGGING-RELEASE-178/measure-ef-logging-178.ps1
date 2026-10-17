# TB-STOREFRONT-EF-LOGGING-RELEASE-178 — bounded, same-binary A/B measurement of the EF log flood.
#
# Leg A = pre-change EFFECTIVE configuration, reproduced exactly by the documented configuration knob
#         Logging__LogLevel__Microsoft.EntityFrameworkCore=Information (nothing is rebuilt or edited).
# Leg B = post-change committed default (base + Development files at this commit).
# Both legs run the SAME published binary, SAME scratch database, SAME route, SAME request count.
#
# Task-owned resources only: container tooba-tb178-pg, db tooba_178_scratch, ports 55498 / 5440.
# Fail-closed on any shared port (3000 / 5088 / 5432) or shared database name.
param(
    [string]$Container = 'tooba-tb178-pg',
    [string]$Database = 'tooba_178_scratch',
    [string]$MessagingDatabase = 'tooba_178_messaging',
    [string]$User = 'scratch178',
    [int]$PgPort = 55498,
    [int]$HostPort = 5440,
    [int]$Requests = 3
)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-178-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-EF-LOGGING-RELEASE-178'
$password = $env:TOOBA_178_SCRATCH_PASSWORD

if (-not $password) { throw 'TOOBA_178_SCRATCH_PASSWORD must come from the environment' }
if ($PgPort -in 5432, 55496, 55475 -or $HostPort -in 3000, 5088, 5432) { throw 'refusing shared/prior ports' }
if ($Container -notmatch 'tooba-tb178-pg') { throw 'refusing a non-task container name' }
if ($Database -match 'tooba_alpha') { throw 'refusing the shared alpha database' }
if ($evidenceDir -notmatch 'TB-STOREFRONT-EF-LOGGING-RELEASE-178') { throw 'refusing a non-task evidence folder' }

New-Item -ItemType Directory -Force -Path $runDir, $evidenceDir | Out-Null

function Assert-FreePort([int]$port, [string]$label) {
    $used = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($used) { throw "$label port $port is already in use" }
}
Assert-FreePort $PgPort 'scratch postgres'
Assert-FreePort $HostPort 'scratch host'

# --- 1. disposable PostgreSQL ----------------------------------------------------------------
$existing = & docker ps -a --filter "name=^$Container$" --format '{{.Names}}' 2>$null
if (-not [string]::IsNullOrWhiteSpace($existing)) { & docker rm -f $Container | Out-Null }
& docker run -d --name $Container `
    -e POSTGRES_USER=$User -e POSTGRES_PASSWORD=$password -e POSTGRES_DB=$Database `
    -p "127.0.0.1:${PgPort}:5432" postgres:16-alpine | Out-Null

$ready = $false
for ($i = 0; $i -lt 80; $i++) {
    $ping = & docker exec $Container pg_isready -U $User -d $Database 2>&1 | Out-String
    if ($ping -match 'accepting connections') {
        # pg_isready can answer during the init-phase temporary server: require a real login.
        $probe = & docker exec -e PGPASSWORD=$password $Container psql -U $User -d $Database -tAc 'select 1' 2>&1 | Out-String
        if ($probe.Trim() -eq '1') { $ready = $true; break }
    }
    Start-Sleep -Milliseconds 750
}
if (-not $ready) { throw 'scratch PostgreSQL did not become ready' }

$messaging = & docker exec -e PGPASSWORD=$password $Container psql -U $User -d $Database -tAc `
    "select 1 from pg_database where datname='$MessagingDatabase'" 2>&1 | Out-String
if ($messaging.Trim() -ne '1') {
    & docker exec -e PGPASSWORD=$password $Container psql -U $User -d $Database -v ON_ERROR_STOP=1 `
        -c "create database $MessagingDatabase" | Out-Null
}

# --- 2. publish the current tree once, reuse for both legs ------------------------------------
$hostOut = Join-Path $runDir 'host'
$migratorDir = Join-Path $runDir 'migrator'
if (-not (Test-Path (Join-Path $hostOut 'Tooba.Host.exe'))) {
    dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.Host\Tooba.Host.csproj') -c Debug -o $hostOut | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'host publish failed' }
}
if (-not (Test-Path (Join-Path $migratorDir 'Tooba.MigrationRunner.exe'))) {
    dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.MigrationRunner\Tooba.MigrationRunner.csproj') -c Debug -o $migratorDir | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'migration runner publish failed' }
}

# --- 3. isolated configuration built from the COMMITTED files, scratch overlay only ------------
$env:TOOBA_178_HOST_PORT = "$HostPort"
$env:TOOBA_178_PG_PORT = "$PgPort"
$env:TOOBA_178_DATABASE = $Database
$env:TOOBA_178_MESSAGING_DATABASE = $MessagingDatabase
$env:TOOBA_178_USER = $User
node (Join-Path $evidenceDir 'prepare-isolated-target-178.js')
if ($LASTEXITCODE -ne 0) { throw 'isolation guard failed closed' }

Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $migratorDir 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $migratorDir 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $migratorDir
try { $apply = (& .\Tooba.MigrationRunner.exe apply --all-tenants 2>&1 | Out-String) } finally { Pop-Location }
$applyOk = $LASTEXITCODE -eq 0
$apply | Set-Content -LiteralPath (Join-Path $evidenceDir 'migrator-apply-178.txt') -Encoding utf8
if (-not $applyOk) { throw 'migration runner apply failed' }

Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $hostOut 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $hostOut 'appsettings.Development.json') -Force

# --- 4. two legs over the same binary/data/route ----------------------------------------------
function Invoke-Leg([string]$leg, [string]$efOverride) {
    $stdout = Join-Path $runDir "host-$leg-stdout.jsonl"
    $stderr = Join-Path $runDir "host-$leg-stderr.txt"
    Remove-Item -Force -ErrorAction SilentlyContinue $stdout, $stderr

    $env:ASPNETCORE_URLS = "http://127.0.0.1:$HostPort"
    $env:DOTNET_ENVIRONMENT = 'Development'
    if ($efOverride) { Set-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -Value $efOverride }
    else { Remove-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -ErrorAction SilentlyContinue }

    $proc = Start-Process -FilePath (Join-Path $hostOut 'Tooba.Host.exe') -WorkingDirectory $hostOut -PassThru -NoNewWindow `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr

    $healthy = $false
    $started = Get-Date
    while ($true) {
        if ($proc.HasExited) { break }
        if (((Get-Date) - $started).TotalSeconds -ge 180) { break }
        try {
            if ((Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $healthy = $true; break }
        } catch { }
        Start-Sleep -Milliseconds 800
    }
    if (-not $healthy) {
        try { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue } catch { }
        throw "leg $leg host did not become healthy"
    }

    $samples = @()
    for ($i = 1; $i -le $Requests; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/storefront/home" -UseBasicParsing -TimeoutSec 120 `
            -Headers @{ Host = 'alpha.localhost' }
        $sw.Stop()
        $samples += [pscustomobject]@{ request = $i; status = $resp.StatusCode; wallMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 1); payloadBytes = $resp.RawContentLength }
    }

    Stop-Process -Id $proc.Id -Force
    $proc.WaitForExit(20000) | Out-Null
    Start-Sleep -Milliseconds 500

    $text = if (Test-Path $stdout) { [System.IO.File]::ReadAllText($stdout) } else { '' }
    $allLines = ($text -split "`n") | Where-Object { $_.Trim().Length -gt 0 }
    $efLines = $allLines | Where-Object { $_ -match 'Microsoft\.EntityFrameworkCore' }
    $commandLines = $allLines | Where-Object { $_ -match 'Microsoft\.EntityFrameworkCore\.Database\.Command' }
    $errorLines = $allLines | Where-Object { $_ -match '"Level":"(Error|Critical)"' }

    return [pscustomobject]@{
        leg = $leg
        efOverride = if ($efOverride) { $efOverride } else { '<committed default>' }
        requests = $Requests
        httpStatuses = ($samples | ForEach-Object { $_.status }) -join ','
        wallMs = ($samples | ForEach-Object { $_.wallMs }) -join ','
        payloadBytes = ($samples | ForEach-Object { $_.payloadBytes }) -join ','
        stdoutBytes = [System.Text.Encoding]::UTF8.GetByteCount($text)
        stdoutLines = @($allLines).Count
        efCategoryLines = @($efLines).Count
        efCommandLines = @($commandLines).Count
        efCommandBytes = [System.Text.Encoding]::UTF8.GetByteCount((@($commandLines) -join "`n"))
        errorOrCriticalLines = @($errorLines).Count
    }
}

$legA = Invoke-Leg 'A-prechange-effective' 'Information'
$legB = Invoke-Leg 'B-postchange-default' $null

# --- 5. measurement record + cleanup ---------------------------------------------------------
$result = [pscustomobject]@{
    baseSha = (& git rev-parse HEAD)
    route = '/v1/storefront/home'
    hostHeader = 'alpha.localhost'
    environment = 'Development (DOTNET_ENVIRONMENT)'
    scratch = [pscustomobject]@{ container = $Container; database = $Database; pgPort = $PgPort; hostPort = $HostPort }
    legA = $legA
    legB = $legB
    efCommandLinesReduced = $legA.efCommandLines - $legB.efCommandLines
    stdoutBytesReduced = $legA.stdoutBytes - $legB.stdoutBytes
    sharedPortsTouched = $false
    credentialsInEvidence = $false
}
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceDir 'measurement-178.json') -Encoding utf8
$result | ConvertTo-Json -Depth 6

# cleanup: exact task-owned resources only
try { Stop-Process -Id (Get-Process -Name 'Tooba.Host' -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "*$runDir*" }).Id -Force -ErrorAction SilentlyContinue } catch { }
& docker rm -f $Container | Out-Null
Write-Output "CLEANUP_CONTAINER_REMOVED=$Container"
Write-Output 'CLEANUP_SHARED_UNTOUCHED=true'
