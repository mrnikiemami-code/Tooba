# TB-STOREFRONT-PERF-SEED-AUTH-SCALE-181 — probe: does the Host's OWN Development startup path
# establish the in-memory admin authority that the separately-run MigrationRunner cannot?
#
# Task-owned: container tooba-tb181-pg, db tooba_181_scratch, ports 55501 / 5452.
$ErrorActionPreference = 'Continue'
$Container = 'tooba-tb181-pg'; $Database = 'tooba_181_scratch'; $MessagingDatabase = 'tooba_181_messaging'
$User = 'scratch181'; $PgPort = 55501; $HostPort = 5452
$repo = (Get-Location).Path
$runDir = Join-Path $repo '.tmp-181-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-SEED-AUTH-SCALE-181'
$password = $env:TOOBA_181_SCRATCH_PASSWORD
$progress = Join-Path $evidenceDir 'progress-181.log'
if (-not $password) { throw 'TOOBA_181_SCRATCH_PASSWORD required' }
New-Item -ItemType Directory -Force -Path $runDir, $evidenceDir | Out-Null
function Log([string]$m) { Add-Content -LiteralPath $progress -Value "$(Get-Date -Format 'HH:mm:ss') $m" -Encoding utf8 }
function Psql([string]$sql) { (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc $sql 2>$null | Out-String).Trim() }

if (Get-NetTCPConnection -LocalPort $PgPort -State Listen -ErrorAction SilentlyContinue) { throw 'pg port busy' }
if (Get-NetTCPConnection -LocalPort $HostPort -State Listen -ErrorAction SilentlyContinue) { throw 'host port busy' }

& docker rm -f $Container 2>$null | Out-Null
& docker run -d --name $Container -e "POSTGRES_USER=$User" -e "POSTGRES_PASSWORD=$password" -e "POSTGRES_DB=$Database" `
    -p "127.0.0.1:${PgPort}:5432" postgres:16-alpine 2>$null | Out-Null
$ready = $false
for ($i = 0; $i -lt 80; $i++) { if ((Psql 'select 1') -eq '1') { $ready = $true; break }; Start-Sleep -Milliseconds 700 }
if (-not $ready) { throw 'postgres not ready' }
& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -c "create database $MessagingDatabase" 2>$null | Out-Null
Log 'postgres ready'

$hostOut = Join-Path $runDir 'host'
dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.Host\Tooba.Host.csproj') -c Debug -o $hostOut 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }
Log 'published'

$env:TOOBA_181_HOST_PORT = "$HostPort"
$env:TOOBA_181_PG_PORT = "$PgPort"
$env:TOOBA_181_DATABASE = $Database
$env:TOOBA_181_MESSAGING_DATABASE = $MessagingDatabase
$env:TOOBA_181_USER = $User
$env:TOOBA_181_ALLOW_SEED = 'true'
node (Join-Path $evidenceDir 'prepare-isolated-target-181.js')
if ($LASTEXITCODE -ne 0) { throw 'isolation guard failed' }
Log 'guard PASS'

Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $hostOut 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $hostOut 'appsettings.Development.json') -Force

# NO MigrationRunner: the Host itself must apply schemas and run its Development bootstraps,
# which is the canonical local-development flow and the only path that owns in-process authority.
$env:ASPNETCORE_URLS = "http://127.0.0.1:$HostPort"
$env:DOTNET_ENVIRONMENT = 'Development'
$stamp = Get-Date -Format 'HHmmss'
$stdout = Join-Path $runDir "host-boot-$stamp.jsonl"
$stderr = Join-Path $runDir "host-boot-$stamp-err.txt"
$h = Start-Process -FilePath (Join-Path $hostOut 'Tooba.Host.exe') -WorkingDirectory $hostOut -PassThru -NoNewWindow `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr
Log "host started pid=$($h.Id)"
$healthy = $false
$started = Get-Date
while (((Get-Date) - $started).TotalSeconds -lt 300) {
    if ($h.HasExited) { Log "host exited early code=$($h.ExitCode)"; break }
    try { if ((Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $healthy = $true; break } } catch { }
    Start-Sleep -Milliseconds 800
}
Log "healthy=$healthy elapsed=$([math]::Round(((Get-Date)-$started).TotalSeconds,1))s"
if (-not $healthy) {
    Get-Content $stderr -Tail 30 | ForEach-Object { Log "ERR $_" }
    throw 'host not healthy'
}

$rowsBefore = Psql "select 'products='||(select count(*) from catalog.products)||' offers='||(select count(*) from offer.offers)||' prices='||(select count(*) from pricing.prices)||' stock='||(select count(*) from inventory.stock_positions)||' parties='||(select count(*) from party.parties)||' categories='||(select count(*) from catalog.categories)"
Log "rows before seed: $rowsBefore"

$actor = Psql "select user_id from identity.login_identifiers where normalized_value='admin-actor@tooba.local' and kind='Email' limit 1"
Log "dev actor=$actor"
if ($actor -notmatch '^[0-9a-fA-F-]{36}$') { throw 'dev actor missing' }

$respFile = Join-Path $runDir 'seed-response.json'
$codeFile = Join-Path $runDir 'seed-code.txt'
Remove-Item -Force -ErrorAction SilentlyContinue $respFile, $codeFile
$seedSw = [System.Diagnostics.Stopwatch]::StartNew()
$curl = Start-Process -FilePath 'curl.exe' -ArgumentList @(
    '--max-time', '60', '-s', '-o', $respFile, '-w', '%{http_code}', '-X', 'POST',
    '-H', 'Host: alpha.localhost', '-H', "X-Tooba-Dev-Actor-User-Id: $actor",
    "http://127.0.0.1:$HostPort/v1/admin/catalog/demo/seed-only") -PassThru -NoNewWindow -RedirectStandardOutput $codeFile
$curl.WaitForExit(75000) | Out-Null
if (-not $curl.HasExited) { try { Stop-Process -Id $curl.Id -Force } catch { }; Log 'seed attempt killed at 60s cap' }
$seedSw.Stop()
$code = if (Test-Path $codeFile) { (Get-Content $codeFile -Raw).Trim() } else { '' }
$body = if (Test-Path $respFile) { Get-Content $respFile -Raw } else { '' }
Log "seed attempt #1 http=$code elapsed=$([math]::Round($seedSw.Elapsed.TotalSeconds,1))s bodyLen=$($body.Length)"
Set-Content -LiteralPath (Join-Path $evidenceDir 'seed-attempt-1-181.txt') -Value "http=$code`n$body" -Encoding utf8
$rowsAfter = Psql "select 'products='||(select count(*) from catalog.products)||' offers='||(select count(*) from offer.offers)||' prices='||(select count(*) from pricing.prices)||' stock='||(select count(*) from inventory.stock_positions)||' parties='||(select count(*) from party.parties)||' categories='||(select count(*) from catalog.categories)"
Log "rows after seed attempt: $rowsAfter"

# startup evidence: did the Host run its own Development seed/authority bootstrap?
$text = if (Test-Path $stdout) { Get-Content $stdout -Raw } else { '' }
$boot = @(($text -split "`n") | Where-Object { $_ -match 'Development|seed|CatalogDemo|workspace|Bootstrap|Authorization' } | Select-Object -First 40)
Set-Content -LiteralPath (Join-Path $evidenceDir 'host-startup-excerpt-181.txt') -Value ($boot -join "`n") -Encoding utf8

try { Stop-Process -Id $h.Id -Force } catch { }
Start-Sleep -Seconds 2
"SEED_HTTP=$code"; "ROWS_BEFORE=$rowsBefore"; "ROWS_AFTER=$rowsAfter"; "BODY=" + $body.Substring(0, [Math]::Min(400, $body.Length))
