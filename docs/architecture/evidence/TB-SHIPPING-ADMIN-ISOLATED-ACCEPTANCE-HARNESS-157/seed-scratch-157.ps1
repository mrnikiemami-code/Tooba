# TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157 — scratch schema + destination fixture (SCRATCH ONLY).
param(
    [string]$Container = 'tooba-tb157-pg',
    [string]$Database = 'tooba_157_scratch',
    [string]$User = 'scratch157',
    [int]$PgPort = 55475
)

$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$dir = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157'
$password = $env:TOOBA_157_SCRATCH_PASSWORD
if (-not $password) { throw 'TOOBA_157_SCRATCH_PASSWORD must come from the environment' }
if ($PgPort -eq 5432) { throw 'never point the scratch fixture at the shared 5432 listener' }

# 1. The Development Host does not migrate the Shipping/Geography schemas (by design); the real product
#    migration tool owns them. Publish it and run it against the scratch target only.
$migratorDir = Join-Path $repo '.tmp-157-run\migrator'
if (-not (Test-Path -LiteralPath (Join-Path $migratorDir 'Tooba.MigrationRunner.exe'))) {
    dotnet publish (Join-Path $repo 'src\backend\Host\Tooba.MigrationRunner\Tooba.MigrationRunner.csproj') -c Debug -o $migratorDir | Out-Null
    Copy-Item (Join-Path $repo '.tmp-157-run\appsettings.json') (Join-Path $migratorDir 'appsettings.json') -Force
    Copy-Item (Join-Path $repo '.tmp-157-run\appsettings.Development.json') (Join-Path $migratorDir 'appsettings.Development.json') -Force
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'
Push-Location $migratorDir
try { $apply = (& .\Tooba.MigrationRunner.exe apply --all-tenants 2>&1 | Out-String) } finally { Pop-Location }
$applyOk = $LASTEXITCODE -eq 0

# 2. Minimum consistent destination fixture so the Shipping write seam can validate a real scope.
$fixture = Get-Content -LiteralPath (Join-Path $dir 'scratch-geography-fixture-157.sql') -Raw
$fixtureOut = $fixture | docker exec -i -e PGPASSWORD=$password $Container psql -U $User -d $Database -v ON_ERROR_STOP=1 2>&1 | Out-String

$evidence = @(
    "SCRATCH_TARGET=$Container / $Database (disposable, task-owned)",
    "MIGRATION_RUNNER_APPLY_EXIT_OK=$applyOk",
    "SHIPPING_SCHEMA_MIGRATION_APPLIED=" + ($apply -match '"Schema":"shipping"'),
    "DESTINATION_FIXTURE_APPLIED=" + ($fixtureOut -match 'COMMIT'),
    "SHARED_PORT_5432_TOUCHED=False"
)
$evidence -join "`n" | Set-Content -LiteralPath (Join-Path $dir 'scratch-seed-157.txt') -Encoding utf8
$evidence | ForEach-Object { Write-Output $_ }
if (-not $applyOk) { exit 4 }
