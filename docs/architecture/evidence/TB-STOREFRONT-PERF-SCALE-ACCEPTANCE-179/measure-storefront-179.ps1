# TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179 — reusable, task-owned storefront measurement.
#
# Task-owned resources only: container tooba-tb179-pg, db tooba_179_scratch, ports 55499 / 5450.
# Fail-closed on shared ports (3000 / 5088 / 5432), shared database names and any prior scratch target.
#
# Usage:
#   $env:TOOBA_179_SCRATCH_PASSWORD='<env-only>'; powershell -File measure-storefront-179.ps1
# Re-running with -KeepDatabase reuses the already-seeded scratch database (same fixture, second size probe).
param(
    [string]$Container = 'tooba-tb179-pg',
    [string]$Database = 'tooba_179_scratch',
    [string]$MessagingDatabase = 'tooba_179_messaging',
    [string]$User = 'scratch179',
    [int]$PgPort = 55499,
    [int]$HostPort = 5450,
    [int]$Requests = 3,
    [switch]$KeepDatabase,
    [string]$Tag = 'size-a',
    [string]$Locale = 'fa'
)

$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-179-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179'
$password = $env:TOOBA_179_SCRATCH_PASSWORD

if (-not $password) { throw 'TOOBA_179_SCRATCH_PASSWORD must come from the environment' }
if ($PgPort -in 5432, 55498, 55496 -or $HostPort -in 3000, 5088, 5432, 5440) { throw 'refusing shared/prior ports' }
if ($Container -notmatch 'tooba-tb179-pg') { throw 'refusing a non-task container name' }
if ($Database -match 'tooba_alpha|tooba_disabled') { throw 'refusing a shared database' }
if ($evidenceDir -notmatch 'TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179') { throw 'refusing a non-task evidence folder' }

New-Item -ItemType Directory -Force -Path $runDir, $evidenceDir | Out-Null

function Assert-FreePort([int]$port, [string]$label) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) {
        throw "$label port $port is already in use"
    }
}
if (-not $KeepDatabase) {
    Assert-FreePort $PgPort 'scratch postgres'
}
Assert-FreePort $HostPort 'scratch host'

# --- 1. disposable PostgreSQL (skipped when reusing a seeded fixture) ------------------------
if (-not $KeepDatabase) {
    $existing = & docker ps -a --filter "name=^$Container$" --format '{{.Names}}' 2>$null
    if (-not [string]::IsNullOrWhiteSpace($existing)) { & docker rm -f $Container | Out-Null }
    & docker run -d --name $Container `
        -e POSTGRES_USER=$User -e POSTGRES_PASSWORD=$password -e POSTGRES_DB=$Database `
        -p "127.0.0.1:${PgPort}:5432" postgres:16-alpine | Out-Null

    $ready = $false
    for ($i = 0; $i -lt 80; $i++) {
        $probe = & docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc 'select 1' 2>$null | Out-String
        if ($probe.Trim() -eq '1') { $ready = $true; break }
        Start-Sleep -Milliseconds 750
    }
    if (-not $ready) { throw 'scratch PostgreSQL did not become ready' }

    & docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -v ON_ERROR_STOP=1 `
        -c "create database $MessagingDatabase" 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'failed to create the scratch messaging database' }
} else {
    $state = & docker ps --filter "name=^$Container$" --format '{{.Names}}' 2>$null
    if ([string]::IsNullOrWhiteSpace($state)) { throw 'KeepDatabase requested but the task container is not running' }
}

# --- 2. publish the current tree once --------------------------------------------------------
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

# --- 3. guarded isolated overlay (app-owned bootstraps only, no invented data) ----------------
$env:TOOBA_179_HOST_PORT = "$HostPort"
$env:TOOBA_179_PG_PORT = "$PgPort"
$env:TOOBA_179_DATABASE = $Database
$env:TOOBA_179_MESSAGING_DATABASE = $MessagingDatabase
$env:TOOBA_179_USER = $User
$env:TOOBA_179_RUN_LEGACY_BOOTSTRAPS = 'true'
node (Join-Path $evidenceDir 'prepare-isolated-target-179.js')
if ($LASTEXITCODE -ne 0) { throw 'isolation guard failed closed' }

Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $migratorDir 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $migratorDir 'appsettings.Development.json') -Force

if (-not $KeepDatabase) {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    Push-Location $migratorDir
    try { $apply = (& .\Tooba.MigrationRunner.exe apply --all-tenants 2>&1 | Out-String) } finally { Pop-Location }
    $apply | Set-Content -LiteralPath (Join-Path $evidenceDir 'migrator-apply-179.txt') -Encoding utf8
    if ($LASTEXITCODE -ne 0) { throw 'migration runner apply failed' }
}

Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $hostOut 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $hostOut 'appsettings.Development.json') -Force

# --- 4. two logging legs over the same binary/data/route -------------------------------------
function Invoke-Leg([string]$leg, [string]$efOverride) {
    $stdout = Join-Path $runDir "host-$Tag-$leg-stdout.jsonl"
    $stderr = Join-Path $runDir "host-$Tag-$leg-stderr.txt"
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
        if (((Get-Date) - $started).TotalSeconds -ge 240) { break }
        try {
            if ((Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $healthy = $true; break }
        } catch { }
        Start-Sleep -Milliseconds 800
    }
    if (-not $healthy) {
        try { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue } catch { }
        throw "leg $leg host did not become healthy"
    }

    $uri = "http://127.0.0.1:$HostPort/v1/storefront/home?locale=$Locale"
    $headers = @{ Host = 'alpha.localhost' }

    # warm-up (excluded from the measured set)
    $warmSw = [System.Diagnostics.Stopwatch]::StartNew()
    $warm = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 180 -Headers $headers
    $warmSw.Stop()

    $samples = @()
    for ($i = 1; $i -le $Requests; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $resp = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 180 -Headers $headers
        $sw.Stop()
        $body = $resp.Content
        $samples += [pscustomobject]@{
            request = $i
            status = $resp.StatusCode
            wallMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 1)
            payloadBytes = [System.Text.Encoding]::UTF8.GetByteCount($body)
            productCount = ([regex]::Matches($body, '"slug"')).Count
            bodySha256 = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($body))) -Algorithm SHA256).Hash
        }
    }

    Stop-Process -Id $proc.Id -Force
    try { $proc.WaitForExit(20000) | Out-Null } catch { }
    Start-Sleep -Milliseconds 500

    $text = if (Test-Path $stdout) { [System.IO.File]::ReadAllText($stdout) } else { '' }
    $allLines = @(($text -split "`n") | Where-Object { $_.Trim().Length -gt 0 })
    $efLines = @($allLines | Where-Object { $_ -match 'Microsoft\.EntityFrameworkCore' })
    $cmdLines = @($allLines | Where-Object { $_ -match 'Microsoft\.EntityFrameworkCore\.Database\.Command' })
    $errLines = @($allLines | Where-Object { $_ -match '"Level":"(Error|Critical)"' })

    return [pscustomobject]@{
        leg = $leg
        efLevel = if ($efOverride) { $efOverride } else { '<committed default>' }
        warmup = [pscustomobject]@{ status = $warm.StatusCode; wallMs = [math]::Round($warmSw.Elapsed.TotalMilliseconds, 1); payloadBytes = [System.Text.Encoding]::UTF8.GetByteCount($warm.Content) }
        requests = $Requests
        samples = $samples
        stdoutBytes = [System.Text.Encoding]::UTF8.GetByteCount($text)
        stdoutLines = $allLines.Count
        efCategoryLines = $efLines.Count
        efCommandLines = $cmdLines.Count
        errorOrCriticalLines = $errLines.Count
    }
}

$legDiagnostic = Invoke-Leg 'diagnostic' 'Information'
$legDefault = Invoke-Leg 'default' $null

$out = [pscustomobject]@{
    baseSha = (& git rev-parse HEAD)
    tag = $Tag
    route = "/v1/storefront/home?locale=$Locale"
    hostHeader = 'alpha.localhost'
    scratch = [pscustomobject]@{ container = $Container; database = $Database; pgPort = $PgPort; hostPort = $HostPort }
    legacyBootstraps = $true
    diagnosticLeg = $legDiagnostic
    defaultLeg = $legDefault
    sharedPortsTouched = $false
    credentialsInEvidence = $false
}
$out | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceDir "measurement-179-$Tag.json") -Encoding utf8
$out | ConvertTo-Json -Depth 8

# stdout evidence: sanitised, bounded (first 40 EF lines of the diagnostic leg + whole default leg)
function San([string]$t) { $t -replace '(?i)(Password=)[^;"]*', '$1<redacted>' -replace '(?i)(Username=)[^;"]*', '$1<redacted>' }
$diagText = if (Test-Path (Join-Path $runDir "host-$Tag-diagnostic-stdout.jsonl")) { Get-Content (Join-Path $runDir "host-$Tag-diagnostic-stdout.jsonl") -Raw } else { '' }
$diagCmd = @(($diagText -split "`n") | Where-Object { $_ -match 'Database\.Command' } | Select-Object -First 40) -join "`n"
$defText = if (Test-Path (Join-Path $runDir "host-$Tag-default-stdout.jsonl")) { Get-Content (Join-Path $runDir "host-$Tag-default-stdout.jsonl") -Raw } else { '' }
("### diagnostic leg (EF=Information) — first 40 Database.Command lines`n" + (San $diagCmd) +
 "`n`n### default leg (committed EF=Warning) — complete stdout`n" + (San $defText)) |
    Set-Content -LiteralPath (Join-Path $evidenceDir "stdout-excerpt-179-$Tag.txt") -Encoding utf8

Write-Output "CLEANUP_NOTE=container kept for the second-size probe; run cleanup-179.ps1 to remove it"
