# TB-185 stage 1 — disposable PG + private Host (diagnostic EF=Information) + bounded owner demo seed.
$ErrorActionPreference = 'Continue'
$repo = (Get-Location).Path
$Container = 'tooba-tb185-pg'; $Database = 'tooba_185_scratch'; $MessagingDatabase = 'tooba_185_messaging'
$User = 'scratch185'; $PgPort = 55506; $HostPort = 5456
$runDir = Join-Path $repo '.tmp-185-run'
$evidenceDir = Join-Path $repo 'docs\architecture\evidence\TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185'
$progress = Join-Path $evidenceDir 'progress-185.log'
$password = $env:TOOBA_185_SCRATCH_PASSWORD
if (-not $password) { throw 'TOOBA_185_SCRATCH_PASSWORD required' }
New-Item -ItemType Directory -Force -Path $runDir, $evidenceDir | Out-Null
function Log([string]$m) { Add-Content -LiteralPath $progress -Value "$(Get-Date -Format 'HH:mm:ss') $m" -Encoding utf8 }
function Psql([string]$sql) { (& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -tAc $sql 2>$null | Out-String).Trim() }
function Wait-PortFree([int]$port) { for ($i=0; $i -lt 40; $i++) { if (-not (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)) { return $true }; Start-Sleep -Milliseconds 400 }; return $false }
if (-not (Wait-PortFree $PgPort) -or -not (Wait-PortFree $HostPort)) { throw 'task ports busy' }

& docker rm -f $Container 2>$null | Out-Null
& docker run -d --name $Container -e "POSTGRES_USER=$User" -e "POSTGRES_PASSWORD=$password" -e "POSTGRES_DB=$Database" -p "127.0.0.1:${PgPort}:5432" postgres:16-alpine 2>$null | Out-Null
$ready = $false
for ($i=0; $i -lt 80; $i++) { if ((Psql 'select 1') -eq '1') { $ready = $true; break }; Start-Sleep -Milliseconds 700 }
if (-not $ready) { throw 'postgres not ready' }
& docker exec -e "PGPASSWORD=$password" $Container psql -U $User -d $Database -c "create database $MessagingDatabase" 2>$null | Out-Null
Log 'postgres ready'

$hostOut = Join-Path $runDir 'host'
dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.Host\Tooba.Host.csproj') -c Debug -o $hostOut 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'publish failed' }
Log 'published'

$env:TOOBA_185_HOST_PORT = "$HostPort"
$env:TOOBA_185_PG_PORT = "$PgPort"
$env:TOOBA_185_DATABASE = $Database
$env:TOOBA_185_MESSAGING_DATABASE = $MessagingDatabase
$env:TOOBA_185_USER = $User
$env:TOOBA_185_ALLOW_SEED = 'true'
$env:TOOBA_185_RUN_LEGACY_BOOTSTRAPS = 'true'
node (Join-Path $evidenceDir 'prepare-isolated-target-185.js')
if ($LASTEXITCODE -ne 0) { throw 'isolation guard failed' }
Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $hostOut 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $hostOut 'appsettings.Development.json') -Force
Log 'isolation guard PASS'

$env:ASPNETCORE_URLS = "http://127.0.0.1:$HostPort"
$env:DOTNET_ENVIRONMENT = 'Development'
Set-Item -Path 'Env:Logging__LogLevel__Microsoft.EntityFrameworkCore' -Value 'Warning'
$stdout = Join-Path $runDir 'h-diag.jsonl'
$stderr = Join-Path $runDir 'h-diag-err.txt'
Remove-Item -Force -ErrorAction SilentlyContinue $stdout, $stderr
$proc = Start-Process -FilePath (Join-Path $hostOut 'Tooba.Host.exe') -WorkingDirectory $hostOut -PassThru -NoNewWindow -RedirectStandardOutput $stdout -RedirectStandardError $stderr
$ok = $false; $t0 = Get-Date
while (((Get-Date) - $t0).TotalSeconds -lt 300) {
    if ($proc.HasExited) { break }
    try { if ((Invoke-WebRequest -Uri "http://127.0.0.1:$HostPort/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $ok = $true; break } } catch { }
    Start-Sleep -Milliseconds 700
}
if (-not $ok) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw 'host not healthy' }
$owner = (Get-NetTCPConnection -LocalPort $HostPort -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess
if ($owner -ne $proc.Id) { try { Stop-Process -Id $proc.Id -Force } catch { }; throw "listener pid $owner != started pid $($proc.Id)" }
Set-Content -LiteralPath (Join-Path $runDir 'host-pid.txt') -Value $proc.Id
Log "host up pid=$($proc.Id)"

Start-Sleep -Seconds 12
$actor = Psql "select user_id from identity.login_identifiers where normalized_value='admin-actor@tooba.local' and kind='Email' limit 1"
if ($actor -notmatch '^[0-9a-fA-F-]{36}$') { Stop-Process -Id $proc.Id -Force; throw 'admin dev actor missing' }
Set-Content -LiteralPath (Join-Path $runDir 'admin-actor.txt') -Value $actor
Log "admin actor=$actor"
$rows0 = Psql "select 'products='||(select count(*) from catalog.products)||' published='||(select count(*) from catalog.products where status='Published')"
Log "baseline: $rows0"

# bounded seed: fire once, poll rows, never repeat
$codeFile = Join-Path $runDir 'seed-code.txt'; $respFile = Join-Path $runDir 'seed-body.json'
$c = Start-Process -FilePath 'curl.exe' -ArgumentList @('--max-time','600','-s','-o',$respFile,'-w','%{http_code}','-X','POST',
    '-H','Host: alpha.localhost','-H',"X-Tooba-Dev-Actor-User-Id: $actor",
    "http://127.0.0.1:$HostPort/v1/admin/catalog/demo/seed-only") -PassThru -NoNewWindow -RedirectStandardOutput $codeFile
Set-Content -LiteralPath (Join-Path $runDir 'seed-curl-pid.txt') -Value $c.Id
$t0 = Get-Date
do {
    Start-Sleep -Seconds 10
    $n = [int](Psql "select count(*) from catalog.products")
} while ($n -lt 190 -and ((Get-Date) - $t0).TotalSeconds -lt 420 -and -not $c.HasExited)
$rows1 = Psql "select 'products='||(select count(*) from catalog.products)||' published='||(select count(*) from catalog.products where status='Published')||' categories='||(select count(*) from catalog.categories)||' variants='||(select count(*) from catalog.variants)"
Log "seed poll done products=$n elapsed=$([math]::Round(((Get-Date)-$t0).TotalSeconds,1))s curlExited=$($c.HasExited) rows=$rows1"
"products=$n elapsed=$([math]::Round(((Get-Date)-$t0).TotalSeconds,1)) curlExited=$($c.HasExited) rows=$rows1"
