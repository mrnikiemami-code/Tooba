# TB-SHIPPING-ONE-LEVEL-TABS-111 — ONE isolated startup attempt with an OUTER watchdog (<=180s).
# Purpose: prove whether the TB-110 two-phase DevelopmentSchemaMigrator ordering moves the Development
# bootstrap PAST the 109 barrier (`42P01 relation "party.parties" does not exist`) and record the true
# next stage/state. Exactly one launch. Hard watchdog 180s. No retry.
$ErrorActionPreference = 'Stop'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$out = Join-Path $repo '.tmp-111-host-out'
$ev = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-ONE-LEVEL-TABS-111'
$stdout = Join-Path $ev 'isolated-host-111-stdout.txt'
$stderr = Join-Path $ev 'isolated-host-111-stderr.txt'
$watchdog = Join-Path $ev 'watchdog-result.txt'
$watchdogLimitSeconds = 180

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5395'
foreach ($name in @(
    'Tooba__ShippingOneLevelCatalog__Enabled',
    'Tooba__ShippingRateAuthority__Enabled',
    'Tooba__ShippingOperationalShadow__Enabled',
    'Tooba__CatalogDemo__RunLegacyBootstraps',
    'Tooba__CatalogDemo__AllowResetAndSeed')) {
    Remove-Item "Env:$name" -ErrorAction SilentlyContinue
}

# Explicit scratch config root: the standalone Host reads appsettings{,.Development}.json from its content root
# (the executable directory), so the task-owned scratch overlay must be placed there. Only these two files.
$runDir = Join-Path $repo '.tmp-111-run'
Copy-Item (Join-Path $runDir 'appsettings.json') (Join-Path $out 'appsettings.json') -Force
Copy-Item (Join-Path $runDir 'appsettings.Development.json') (Join-Path $out 'appsettings.Development.json') -Force
Write-Output "SCRATCH_CONFIG_COPIED_TO=$out"

Remove-Item $stdout, $stderr -ErrorAction SilentlyContinue
$started = Get-Date
$exe = Join-Path $out 'Tooba.Host.exe'
$absoluteExe = (Resolve-Path $exe).Path
$p = Start-Process -FilePath $absoluteExe -WorkingDirectory $out -PassThru `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr

$healthOk = $false
$exitCode = $null
$elapsed = 0
while ($true) {
    Start-Sleep -Seconds 5
    $elapsed = [int]((Get-Date) - $started).TotalSeconds

    try {
        $h = Invoke-WebRequest -Uri 'http://127.0.0.1:5395/health' -UseBasicParsing -TimeoutSec 3
        if ($h.StatusCode -eq 200) { $healthOk = $true }
    } catch { }

    $p.Refresh()
    if ($p.HasExited) { $exitCode = $p.ExitCode }

    if ($healthOk) { break }
    if ($p.HasExited) { break }
    if ($elapsed -ge $watchdogLimitSeconds) { break }
}

$p.Refresh()
$killed = $false
$pidToStop = $p.Id
if (-not $p.HasExited) {
    # task-owned PID only, after exe-path verification against the task-owned publish directory
    $actual = (Get-Process -Id $pidToStop).Path
    if ($actual -eq $absoluteExe) {
        Stop-Process -Id $pidToStop -Force -ErrorAction SilentlyContinue
        $killed = $true
        Start-Sleep -Seconds 3
    } else {
        Write-Output "REFUSED_STOP_EXE_MISMATCH=$actual"
    }
}

$listeners = (Get-NetTCPConnection -State Listen -LocalPort 5395 -ErrorAction SilentlyContinue | Measure-Object).Count
$text = ''
if (Test-Path $stdout) { $text += (Get-Content $stdout -Raw -ErrorAction SilentlyContinue) }
if (Test-Path $stderr) { $text += (Get-Content $stderr -Raw -ErrorAction SilentlyContinue) }
$diagnosticFound = $false
if ($text -and $text.Contains('Tooba.Catalog.Development.PublishReadiness')) { $diagnosticFound = $true }
$partyRegression = $false
if ($text -and $text.Contains('relation "party.parties" does not exist')) { $partyRegression = $true }

@(
    "ISOLATED_HOST_PID=$pidToStop",
    "ISOLATED_HOST_EXE=$absoluteExe",
    "LAUNCH_COUNT=1",
    "WATCHDOG_LIMIT_SECONDS=$watchdogLimitSeconds",
    "ELAPSED_SECONDS=$elapsed",
    "HEALTH_200_OBSERVED=$healthOk",
    "PROCESS_EXITED=$($p.HasExited)",
    "EXIT_CODE=$exitCode",
    "KILLED_BY_WATCHDOG=$killed",
    "PUBLISH_READINESS_DIAGNOSTIC_SEEN=$diagnosticFound",
    "PARTY_SCHEMA_REGRESSION_SEEN=$partyRegression",
    "LISTENERS_ON_5395_AFTER_STOP=$listeners"
) | Tee-Object -FilePath $watchdog
