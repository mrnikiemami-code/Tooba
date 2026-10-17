# TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171 — task-owned scratch environment.
# Fail-closed: task-specific container/database/ports only; shared :3000/:5088/:5432 are never used.
param(
    [string]$Container = 'tooba-tb171-pg',
    [string]$Database = 'tooba_171_scratch',
    [string]$MessagingDatabase = 'tooba_171_messaging',
    [string]$User = 'scratch171',
    [int]$PgPort = 55490,
    [int]$HostPort = 5425,
    [string]$Gate = 'true'
)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-171-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171'
$password = $env:TOOBA_171_SCRATCH_PASSWORD

if (-not $password) { throw 'TOOBA_171_SCRATCH_PASSWORD must come from the environment' }
if ($PgPort -eq 5432 -or $HostPort -eq 5088 -or $HostPort -eq 3000) { throw 'never reuse the shared ports' }
if ($Container -notmatch 'tooba-tb171-pg') { throw 'refusing a non-task container name' }

New-Item -ItemType Directory -Force -Path $runDir | Out-Null
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null

# --- 1. disposable PostgreSQL ---------------------------------------------------------------
$existing = & docker ps -a --filter "name=^$Container$" --format '{{.Names}}' 2>$null
if (-not [string]::IsNullOrWhiteSpace($existing)) { & docker rm -f $Container | Out-Null }

& docker run -d --name $Container `
    -e POSTGRES_USER=$User -e POSTGRES_PASSWORD=$password -e POSTGRES_DB=$Database `
    -p "127.0.0.1:${PgPort}:5432" postgres:16-alpine | Out-Null

$ready = $false
for ($i = 0; $i -lt 40; $i++) {
    $ping = & docker exec $Container pg_isready -U $User -d $Database 2>&1 | Out-String
    if ($ping -match 'accepting connections') { $ready = $true; break }
    Start-Sleep -Milliseconds 750
}
if (-not $ready) { throw 'scratch PostgreSQL did not become ready' }

$messaging = & docker exec -e PGPASSWORD=$password $Container psql -U $User -d $Database -tAc `
    "select 1 from pg_database where datname='$MessagingDatabase'" 2>&1 | Out-String
if ($messaging.Trim() -ne '1') {
    & docker exec -e PGPASSWORD=$password $Container psql -U $User -d $Database -v ON_ERROR_STOP=1 `
        -c "create database $MessagingDatabase" | Out-Null
}

# --- 2. publish Host + canonical migration runner from the current tree ---------------------
$hostOut = Join-Path $runDir 'host'
$migratorDir = Join-Path $runDir 'migrator'
dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.Host\Tooba.Host.csproj') -c Debug -o $hostOut | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'host publish failed' }
dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.MigrationRunner\Tooba.MigrationRunner.csproj') -c Debug -o $migratorDir | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'migration runner publish failed' }

# --- 3. isolated configuration overlay (gate from parameter) --------------------------------
$env:TOOBA_171_HOST_PORT = "$HostPort"
$env:TOOBA_171_PG_PORT = "$PgPort"
$env:TOOBA_171_DATABASE = $Database
$env:TOOBA_171_MESSAGING_DATABASE = $MessagingDatabase
$env:TOOBA_171_USER = $User
$env:TOOBA_171_GATE = $Gate
node (Join-Path $evidenceDir 'prepare-isolated-target-171.js')
if ($LASTEXITCODE -ne 0) { throw 'isolation guard failed closed' }

Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $migratorDir 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $migratorDir 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $migratorDir
try { $apply = (& .\Tooba.MigrationRunner.exe apply --all-tenants 2>&1 | Out-String) } finally { Pop-Location }
$applyOk = $LASTEXITCODE -eq 0
$apply | Set-Content -LiteralPath (Join-Path $runDir "migrator-apply-171-$Gate.txt") -Encoding utf8

# --- 4. isolated Host -----------------------------------------------------------------------
Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $hostOut 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $hostOut 'appsettings.Development.json') -Force

$env:ASPNETCORE_URLS = "http://127.0.0.1:$HostPort"
$env:DOTNET_ENVIRONMENT = 'Development'
$proc = Start-Process -FilePath (Join-Path $hostOut 'Tooba.Host.exe') -WorkingDirectory $hostOut -PassThru -NoNewWindow `
    -RedirectStandardOutput (Join-Path $runDir "isolated-host-171-$Gate-stdout.txt") `
    -RedirectStandardError (Join-Path $runDir "isolated-host-171-$Gate-stderr.txt")
$proc.Id | Set-Content -LiteralPath (Join-Path $runDir "host-$Gate.pid") -Encoding ascii

$health = $false
$elapsed = 0
$started = Get-Date
while ($true) {
    $elapsed = [int]((Get-Date) - $started).TotalSeconds
    if ($elapsed -ge 180) { break }
    if ($proc.HasExited) { break }
    try {
        $resp = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/health" -UseBasicParsing -TimeoutSec 3
        if ($resp.StatusCode -eq 200) { $health = $true; break }
    } catch { }
    Start-Sleep -Seconds 2
}

$lines = @(
    "SCRATCH_CONTAINER=$Container",
    "SCRATCH_DATABASE=$Database",
    "SCRATCH_MESSAGING_DATABASE=$MessagingDatabase",
    "SCRATCH_PG_PORT=$PgPort",
    "SCRATCH_HOST_PORT=$HostPort",
    "SCRATCH_GATE=$Gate",
    "MIGRATION_RUNNER_APPLY_EXIT_OK=$applyOk",
    "ISOLATED_HOST_PID=$($proc.Id)",
    "ISOLATED_HOST_HEALTH_200=$health",
    "ISOLATED_HOST_STARTUP_SECONDS=$elapsed",
    "SHARED_PORT_5432_TOUCHED=False",
    "SHARED_PORT_5088_TOUCHED=False",
    "SHARED_PORT_3000_TOUCHED=False",
    "CREDENTIALS_WRITTEN_TO_EVIDENCE=false"
)
$lines -join "`n" | Set-Content -LiteralPath (Join-Path $evidenceDir "host-launch-171-$Gate.txt") -Encoding utf8
$lines | ForEach-Object { Write-Output $_ }

if (-not $applyOk -or -not $health) { exit 5 }
