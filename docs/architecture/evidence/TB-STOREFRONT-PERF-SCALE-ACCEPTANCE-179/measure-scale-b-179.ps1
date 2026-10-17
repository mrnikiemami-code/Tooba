# TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179 — size-B leg: grow the SAME scratch catalog with the
# application-owned demo seeder (POST /v1/admin/catalog/demo/seed-only), then measure the same Home route.
#
# Task-owned resources only: container tooba-tb179-pg, db tooba_179_scratch, ports 55499 / 5450.
# The seeder is additive (seed-only), runs only in Development, and only against the disposable database.
# The destructive /reset-and-seed endpoint is NEVER called.
param(
    [string]$Container = 'tooba-tb179-pg',
    [string]$Database = 'tooba_179_scratch',
    [string]$MessagingDatabase = 'tooba_179_messaging',
    [string]$User = 'scratch179',
    [int]$PgPort = 55499,
    [int]$HostPort = 5450,
    [int]$Requests = 3,
    [string]$Tag = 'size-b',
    [string]$Locale = 'fa'
)

$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-179-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179'
$password = $env:TOOBA_179_SCRATCH_PASSWORD

if (-not $password) { throw 'TOOBA_179_SCRATCH_PASSWORD must come from the environment' }
if ($PgPort -in 5432, 55498 -or $HostPort -in 3000, 5088, 5432) { throw 'refusing shared ports' }
if ($Container -notmatch 'tooba-tb179-pg') { throw 'refusing a non-task container name' }
if ($Database -match 'tooba_alpha') { throw 'refusing a shared database' }

foreach ($port in @($PgPort, $HostPort)) {
    if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { throw "port $port already in use" }
}
New-Item -ItemType Directory -Force -Path $runDir, $evidenceDir | Out-Null

# --- 1. fresh disposable PostgreSQL ---------------------------------------------------------
& docker rm -f $Container 2>$null | Out-Null
& docker run -d --name $Container -e "POSTGRES_USER=$User" -e "POSTGRES_PASSWORD=$password" -e "POSTGRES_DB=$Database" `
    -p "127.0.0.1:${PgPort}:5432" postgres:16-alpine 2>$null | Out-Null
$ready = $false
for ($i = 0; $i -lt 80; $i++) {
    $probe = & docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc 'select 1' 2>$null | Out-String
    if ($probe.Trim() -eq '1') { $ready = $true; break }
    Start-Sleep -Milliseconds 750
}
if (-not $ready) { throw 'scratch PostgreSQL did not become ready' }
& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -v ON_ERROR_STOP=1 -c "create database $MessagingDatabase" 2>$null | Out-Null

# --- 2. publish -----------------------------------------------------------------------------
$hostOut = Join-Path $runDir 'host'
$migratorDir = Join-Path $runDir 'migrator'
dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.Host\Tooba.Host.csproj') -c Debug -o $hostOut 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'host publish failed' }
dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.MigrationRunner\Tooba.MigrationRunner.csproj') -c Debug -o $migratorDir 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'migration runner publish failed' }

# --- 3. guarded overlay (seed gate ON in scratch only) ---------------------------------------
$env:TOOBA_179_HOST_PORT = "$HostPort"
$env:TOOBA_179_PG_PORT = "$PgPort"
$env:TOOBA_179_DATABASE = $Database
$env:TOOBA_179_MESSAGING_DATABASE = $MessagingDatabase
$env:TOOBA_179_USER = $User
$env:TOOBA_179_RUN_LEGACY_BOOTSTRAPS = 'true'
$env:TOOBA_179_ALLOW_SEED = 'true'
node (Join-Path $evidenceDir 'prepare-isolated-target-179.js')
if ($LASTEXITCODE -ne 0) { throw 'isolation guard failed closed' }

Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $migratorDir 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $migratorDir 'appsettings.Development.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $hostOut 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $hostOut 'appsettings.Development.json') -Force

$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $migratorDir
try { $apply = (& .\Tooba.MigrationRunner.exe apply --all-tenants 2>&1 | Out-String) } finally { Pop-Location }
$apply | Set-Content -LiteralPath (Join-Path $evidenceDir 'migrator-apply-179-size-b.txt') -Encoding utf8
if ($LASTEXITCODE -ne 0) { throw 'migration runner apply failed' }

function Start-Host([string]$leg) {
    $stdout = Join-Path $runDir "host-$Tag-$leg-stdout.jsonl"
    $stderr = Join-Path $runDir "host-$Tag-$leg-stderr.txt"
    Remove-Item -Force -ErrorAction SilentlyContinue $stdout, $stderr
    $env:ASPNETCORE_URLS = "http://127.0.0.1:$HostPort"
    $env:DOTNET_ENVIRONMENT = 'Development'
    if ($leg -eq 'diagnostic') { Set-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -Value 'Information' }
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
    if (-not $healthy) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw "host ($leg) did not become healthy" }
    return [pscustomobject]@{ proc = $proc; stdout = $stdout }
}

function Stop-Host($host) {
    try { Stop-Process -Id $host.proc.Id -Force } catch { }
    try { $host.proc.WaitForExit(20000) | Out-Null } catch { }
    Start-Sleep -Milliseconds 400
}

# --- 4. seed the larger catalog with the APPLICATION-OWNED seeder ----------------------------
$boot = Start-Host 'seed'
$devActor = (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc `
    "select user_id from identity.users where lower(email)='admin-actor@tooba.local' limit 1" 2>$null | Out-String).Trim()
if ($devActor -notmatch '^[0-9a-fA-F-]{36}$') {
    Stop-Host $boot
    throw "development admin actor not found (got '$devActor'); seeder requires it"
}
$seedHeaders = @{ Host = 'alpha.localhost'; 'X-Tooba-Dev-Actor-User-Id' = $devActor }
$seedSw = [System.Diagnostics.Stopwatch]::StartNew()
$seedResp = Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/v1/admin/catalog/demo/seed-only" -Method Post `
    -UseBasicParsing -TimeoutSec 600 -Headers $seedHeaders
$seedSw.Stop()
$seedBody = $seedResp.Content
$seedBody | Set-Content -LiteralPath (Join-Path $evidenceDir 'seed-response-179-size-b.json') -Encoding utf8
Stop-Host $boot
Write-Output "SEED_STATUS=$($seedResp.StatusCode) SEED_MS=$([math]::Round($seedSw.Elapsed.TotalMilliseconds,1))"

# --- 5. two logging legs on the SAME enlarged fixture ---------------------------------------
function Invoke-Leg([string]$leg) {
    $host = Start-Host $leg
    $uri = "http://127.0.0.1:$HostPort/v1/storefront/home?locale=$Locale"
    $headers = @{ Host = 'alpha.localhost' }
    $warmSw = [System.Diagnostics.Stopwatch]::StartNew()
    $warm = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 180 -Headers $headers
    $warmSw.Stop()
    $samples = @()
    for ($i = 1; $i -le $Requests; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $resp = Invoke-WebRequest -Uri $uri -UseBasicParsing -TimeoutSec 180 -Headers $headers
        $sw.Stop()
        $body = $resp.Content
        $sha = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new([System.Text.Encoding]::UTF8.GetBytes($body))) -Algorithm SHA256).Hash
        if ($i -eq 1) { $body | Set-Content -LiteralPath (Join-Path $evidenceDir "home-body-179-$Tag-$leg.json") -Encoding utf8 }
        $samples += [pscustomobject]@{
            request = $i; status = $resp.StatusCode
            wallMs = [math]::Round($sw.Elapsed.TotalMilliseconds, 1)
            payloadBytes = [System.Text.Encoding]::UTF8.GetByteCount($body)
            slugCount = ([regex]::Matches($body, '"slug"')).Count
            bodySha256 = $sha
        }
    }
    Stop-Host $host
    $text = if (Test-Path $host.stdout) { [System.IO.File]::ReadAllText($host.stdout) } else { '' }
    $lines = @(($text -split "`n") | Where-Object { $_.Trim().Length -gt 0 })
    return [pscustomobject]@{
        leg = $leg
        efLevel = if ($leg -eq 'diagnostic') { 'Information' } else { '<committed default>' }
        warmup = [pscustomobject]@{ status = $warm.StatusCode; wallMs = [math]::Round($warmSw.Elapsed.TotalMilliseconds, 1); payloadBytes = [System.Text.Encoding]::UTF8.GetByteCount($warm.Content) }
        requests = $Requests
        samples = $samples
        stdoutBytes = [System.Text.Encoding]::UTF8.GetByteCount($text)
        stdoutLines = $lines.Count
        efCategoryLines = @($lines | Where-Object { $_ -match 'Microsoft\.EntityFrameworkCore' }).Count
        efCommandLines = @($lines | Where-Object { $_ -match 'Microsoft\.EntityFrameworkCore\.Database\.Command' }).Count
        errorOrCriticalLines = @($lines | Where-Object { $_ -match '"Level":"(Error|Critical)"' }).Count
    }
}

$legDiagnostic = Invoke-Leg 'diagnostic'
$legDefault = Invoke-Leg 'default'

$rows = (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc `
    "select 'products='||(select count(*) from catalog.products)||';variants='||(select count(*) from catalog.variants)||';offers='||(select count(*) from offer.offers)||';prices='||(select count(*) from pricing.prices)||';stock='||(select count(*) from inventory.stock_positions)||';parties='||(select count(*) from party.parties)||';texts='||(select count(*) from catalog.localized_texts)" 2>$null | Out-String).Trim()

$out = [pscustomobject]@{
    baseSha = (& git rev-parse HEAD)
    tag = $Tag
    route = "/v1/storefront/home?locale=$Locale"
    seedStatus = $seedResp.StatusCode
    seedElapsedMs = [math]::Round($seedSw.Elapsed.TotalMilliseconds, 1)
    fixtureRows = $rows
    diagnosticLeg = $legDiagnostic
    defaultLeg = $legDefault
    sharedPortsTouched = $false
    credentialsInEvidence = $false
}
$out | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $evidenceDir "measurement-179-$Tag.json") -Encoding utf8
$out | ConvertTo-Json -Depth 8
Write-Output "FIXTURE_ROWS=$rows"
